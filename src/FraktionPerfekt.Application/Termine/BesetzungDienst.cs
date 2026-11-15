using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Application.Termine;

public sealed record MeldungZeile(Besetzungsmeldung Meldung, string Person, Freigabe Bestaetigen);

public sealed record BesetzungZeile(Besetzungsanfrage Anfrage, Termin Termin, int Bestaetigt, int Interessiert, Besetzungsmeldung? Meine);

public sealed record BesetzungDetail(
    Besetzungsanfrage Anfrage,
    Termin Termin,
    IReadOnlyList<MeldungZeile> Meldungen,
    Besetzungsmeldung? Meine,
    Freigabe InteresseMelden,
    Freigabe Zurueckziehen,
    Freigabe BerichtSchreiben)
{
    public int Bestaetigt => Meldungen.Count(m => m.Meldung.Status == Meldungsstatus.Bestaetigt);

    public int Frei => Math.Max(0, Anfrage.Plaetze - Bestaetigt);
}

/// <summary>
/// Terminbesetzung (Modul 5): Einladung mit N Plätzen an alle, Interesse melden, Vorstand bestätigt oder „wer zuerst kommt“
/// (mit Nachrücken), bestätigte Personen haben den Termin im Kalender (Zusage), nach dem Termin ein Kurzbericht.
/// </summary>
public sealed class BesetzungDienst(
    IDaten daten,
    ITerminSpeicher termine,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public async Task<IReadOnlyList<BesetzungZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var anfragen = await daten.ListeAsync(daten.Menge<Besetzungsanfrage>().OrderBy(a => a.Frist).ThenBy(a => a.Id), abbruch);
        var meldungen = await daten.ListeAsync(daten.Menge<Besetzungsmeldung>().OrderBy(m => m.Am).ThenBy(m => m.Id), abbruch);
        var zeilen = new List<BesetzungZeile>();
        foreach (var a in anfragen)
        {
            if (await termine.TerminAsync(a.TerminId, abbruch) is { } t)
            {
                var m = meldungen.Where(x => x.AnfrageId == a.Id).ToList();
                zeilen.Add(new BesetzungZeile(a, t, m.Count(x => x.Status == Meldungsstatus.Bestaetigt), m.Count(x => x.Status == Meldungsstatus.Interessiert),
                    m.FirstOrDefault(x => x.PersonId == wer.PersonId)));
            }
        }

        return zeilen.OrderBy(z => z.Termin.Beginn).ToList();
    }

    /// <summary>Offene Anfragen mit freien Plätzen, bei denen ich mich noch nicht gemeldet habe (Startseite).</summary>
    public async Task<IReadOnlyList<BesetzungZeile>> OffenFuerMichAsync(CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        return (await ListeAsync(abbruch)).Where(z => z.Anfrage.MeldungMoeglich(jetzt) && z.Meine is null && z.Bestaetigt < z.Anfrage.Plaetze).ToList();
    }

    /// <summary>Vergangene Termine, bei denen ich bestätigt war und noch kein Bericht vorliegt (Folgeschritt „Kurzbericht anlegen“).</summary>
    public async Task<IReadOnlyList<BesetzungZeile>> BerichtOffenAsync(CancellationToken abbruch = default)
    {
        var jetzt = Ortszeit.Jetzt(uhr);
        return (await ListeAsync(abbruch))
            .Where(z => z.Termin.Ende < jetzt && z.Anfrage.Bericht is null && z.Meine?.Status == Meldungsstatus.Bestaetigt).ToList();
    }

    public async Task<BesetzungDetail?> DetailAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Besetzungsanfrage>(id, abbruch) is not { } a || await termine.TerminAsync(a.TerminId, abbruch) is not { } t)
        {
            return null;
        }

        var meldungen = await MeldungenAsync(id, abbruch);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var meine = meldungen.FirstOrDefault(m => m.PersonId == wer.PersonId);
        var jetzt = uhr.GetUtcNow();
        var vorstand = Rechte.SitzungenSteuern(wer.Rolle);
        return new BesetzungDetail(a, t,
            meldungen.Select(m => new MeldungZeile(m, namen.GetValueOrDefault(m.PersonId, "?"),
                m.Status != Meldungsstatus.Interessiert ? Freigabe.Nein("Bereits entschieden.")
                : !a.PlatzFrei(meldungen) ? Freigabe.Nein($"Alle {a.Plaetze} Plätze sind besetzt.")
                : vorstand)).ToList(),
            meine,
            !a.MeldungMoeglich(jetzt) ? Freigabe.Nein("Die Meldefrist ist abgelaufen.")
                : meine is { Status: not Meldungsstatus.Zurueckgezogen } ? Freigabe.Nein("Sie haben sich bereits gemeldet.")
                : Freigabe.Ja,
            meine is { Status: Meldungsstatus.Interessiert or Meldungsstatus.Bestaetigt } ? Freigabe.Ja : Freigabe.Nein("Sie haben sich nicht gemeldet."),
            t.Ende > Ortszeit.Jetzt(uhr) ? Freigabe.Nein("Den Kurzbericht gibt es nach dem Termin.")
                : meine?.Status == Meldungsstatus.Bestaetigt || vorstand.Erlaubt ? Freigabe.Ja
                : Freigabe.Nein("Den Bericht schreiben die Teilnehmenden oder der Vorstand."));
    }

    public async Task<BesetzungDetail?> ZuTerminAsync(Guid terminId, CancellationToken abbruch = default)
        => await daten.ErsterAsync(daten.Menge<Besetzungsanfrage>().Where(a => a.TerminId == terminId).OrderBy(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch) is { } a
            ? await DetailAsync(a.Id, abbruch)
            : null;

    /// <summary>Besetzungsanfrage für einen öffentlichen Termin anlegen und an alle Mitglieder schicken.</summary>
    public async Task<Besetzungsanfrage> AnlegenAsync(Guid terminId, int plaetze, DateTimeOffset frist, Besetzungsmodus modus, string? hinweis,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.SitzungenSteuern(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var t = await termine.TerminAsync(terminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (await ZuTerminAsync(terminId, abbruch) is not null)
        {
            throw new InvalidOperationException("Für diesen Termin gibt es bereits eine Besetzungsanfrage.");
        }

        var a = new Besetzungsanfrage(terminId, plaetze, frist, modus, wer.PersonIdPflicht, uhr.GetUtcNow(), hinweis);
        daten.Hinzufuegen(a);
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        foreach (var p in (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && p.Id != wer.PersonId
                     && p.RollenAm(heute, jetzt).Any(r => r is Rolle.Mitglied or Rolle.SachkundigeBuergerin or Rolle.Vorstand)))
        {
            await postausgang.PushEinreihenAsync(p, $"Wer geht hin? {t.Titel}", $"{t.Beginn:dd.MM. HH:mm} Uhr, {plaetze} Plätze – Interesse melden.",
                $"besetzung/{a.Id}", "Ich gehe hin", abbruch);
        }

        await protokoll.ProtokollierenAsync("Besetzung.Angelegt", $"Besetzungsanfrage mit {plaetze} Plätzen ({modus})", nameof(Besetzungsanfrage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return a;
    }

    public async Task<string> InteresseMeldenAsync(Guid anfrageId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await DetailAsync(anfrageId, abbruch) ?? throw new InvalidOperationException("Anfrage nicht gefunden.");
        if (d.InteresseMelden is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var meldungen = await MeldungenAsync(anfrageId, abbruch);
        var meine = meldungen.FirstOrDefault(m => m.PersonId == wer.PersonId);
        if (meine is null)
        {
            meine = new Besetzungsmeldung(anfrageId, wer.PersonIdPflicht, uhr.GetUtcNow());
            daten.Hinzufuegen(meine);
            meldungen.Add(meine);
        }
        else
        {
            meine.ErneutMelden(uhr.GetUtcNow());
        }

        var anfrage = await daten.LadenAsync<Besetzungsanfrage>(anfrageId, "Anfrage", abbruch);
        foreach (var m in anfrage.Nachruecken(meldungen))
        {
            await ZusagenAsync(d.Termin, m.PersonId, abbruch);
        }

        await daten.SpeichernAsync(abbruch);
        return meine.Status == Meldungsstatus.Bestaetigt
            ? "Sie sind dabei – der Termin steht in Ihrem Kalender."
            : anfrage.Modus == Besetzungsmodus.WerZuerstKommt ? "Alle Plätze sind vergeben; Sie stehen auf der Warteliste." : "Interesse gemeldet; der Vorstand bestätigt.";
    }

    public async Task BestaetigenAsync(Guid meldungId, bool bestaetigen, CancellationToken abbruch = default)
    {
        var m = await daten.LadenAsync<Besetzungsmeldung>(meldungId, "Meldung", abbruch);
        var d = await DetailAsync(m.AnfrageId, abbruch) ?? throw new InvalidOperationException("Anfrage nicht gefunden.");
        if (bestaetigen && d.Meldungen.Single(x => x.Meldung.Id == meldungId).Bestaetigen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (bestaetigen)
        {
            m.Bestaetigen();
            await ZusagenAsync(d.Termin, m.PersonId, abbruch);
        }
        else
        {
            m.Ablehnen();
        }

        if (await stammdaten.PersonAsync(m.PersonId, abbruch) is { } p)
        {
            await postausgang.PushEinreihenAsync(p, bestaetigen ? $"Sie sind dabei: {d.Termin.Titel}" : $"Diesmal nicht: {d.Termin.Titel}",
                bestaetigen ? $"{d.Termin.Beginn:dd.MM. HH:mm} Uhr, {d.Termin.Ort}" : "Danke für die Meldung.", $"besetzung/{d.Anfrage.Id}", abbruch: abbruch);
        }

        await daten.SpeichernAsync(abbruch);
    }

    public async Task ZurueckziehenAsync(Guid anfrageId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var meldungen = await MeldungenAsync(anfrageId, abbruch);
        var meine = meldungen.FirstOrDefault(m => m.PersonId == wer.PersonId) ?? throw new InvalidOperationException("Sie haben sich nicht gemeldet.");
        var warBestaetigt = meine.Status == Meldungsstatus.Bestaetigt;
        meine.Zurueckziehen();
        var anfrage = await daten.LadenAsync<Besetzungsanfrage>(anfrageId, "Anfrage", abbruch);
        var termin = await termine.TerminAsync(anfrage.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (warBestaetigt && await termine.TeilnahmeAsync(termin.Id, meine.PersonId, abbruch) is { } t)
        {
            t.Setzen(Teilnahmestatus.Abgesagt, uhr.GetUtcNow());
        }

        foreach (var m in anfrage.Nachruecken(meldungen))
        {
            await ZusagenAsync(termin, m.PersonId, abbruch);
            if (await stammdaten.PersonAsync(m.PersonId, abbruch) is { } p)
            {
                await postausgang.PushEinreihenAsync(p, $"Nachgerückt: {termin.Titel}", $"{termin.Beginn:dd.MM. HH:mm} Uhr – Sie sind dabei.", $"besetzung/{anfrageId}",
                    abbruch: abbruch);
            }
        }

        await daten.SpeichernAsync(abbruch);
    }

    public async Task BerichtAsync(Guid anfrageId, string text, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await DetailAsync(anfrageId, abbruch) ?? throw new InvalidOperationException("Anfrage nicht gefunden.");
        if (d.BerichtSchreiben is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        (await daten.LadenAsync<Besetzungsanfrage>(anfrageId, "Anfrage", abbruch)).BerichtSchreiben(text, wer.PersonIdPflicht, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Besetzung.Bericht", "Kurzbericht geschrieben", nameof(Besetzungsanfrage), anfrageId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    private async Task ZusagenAsync(Termin termin, Guid personId, CancellationToken abbruch)
    {
        if (await termine.TeilnahmeAsync(termin.Id, personId, abbruch) is { } t)
        {
            t.Setzen(Teilnahmestatus.Zugesagt, uhr.GetUtcNow());
        }
        else
        {
            termine.Hinzufuegen(new Teilnahme(termin.Id, personId, Teilnahmestatus.Zugesagt, uhr.GetUtcNow()));
        }

        await protokoll.ProtokollierenAsync("Besetzung.Bestaetigt", $"{Pseudonym.Person(personId)} vertritt die Fraktion beim Termin", nameof(Termin), termin.Id, abbruch: abbruch);
    }

    private Task<List<Besetzungsmeldung>> MeldungenAsync(Guid anfrageId, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Besetzungsmeldung>().Where(m => m.AnfrageId == anfrageId).OrderBy(m => m.Am).ThenBy(m => m.Id), abbruch);
}
