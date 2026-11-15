using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Vertretung;

/// <summary>Eine Gremiensitzung, in der eine abwesende Person ordentliches Mitglied ist.</summary>
public sealed record BetroffeneSitzung(Termin Termin, Gremium Gremium, Vertretungsfall? Fall);

public sealed record Abwesenheitsergebnis(Abwesenheit Abwesenheit, IReadOnlyList<BetroffeneSitzung> Sitzungen, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

public sealed record AbwesenheitZeile(Abwesenheit Abwesenheit, string? Grund, int Sitzungen, int Offen, int Besetzt);

/// <summary>Zeile der Übersicht „Sitzungen mit offener Vertretung“.</summary>
/// <param name="Fall"><c>null</c> = abwesend, aber noch nicht angefragt.</param>
/// <param name="Gefragt">Wer gerade gefragt ist (bei Status „angefragt“).</param>
public sealed record OffeneVertretung(Termin Termin, Gremium Gremium, Guid VertretenePersonId, string Vertretene, Vertretungsfall? Fall,
    string? Gefragt, DateTimeOffset? Frist, int Kandidaten, int Gefragte);

/// <summary>Offene Vertretungsanfrage an die angemeldete Person (Startseite, Handy).</summary>
public sealed record MeineAnfrage(Guid AnfrageId, Guid FallId, Termin Termin, string Gremium, string Vertretene, DateTimeOffset Frist);

public sealed record PersonAuswahl(Guid Id, string Name);

public sealed record AnfrageZeile(Guid Id, string Person, int Reihenfolge, Anfragestatus Status, DateTimeOffset Gestellt, DateTimeOffset Frist,
    DateTimeOffset? Beantwortet);

public sealed record FallDetail(Vertretungsfall Fall, Termin Termin, Gremium Gremium, string Vertretene, IReadOnlyList<AnfrageZeile> Anfragen,
    IReadOnlyList<PersonAuswahl> NochNichtGefragt, string? Vertreter, IReadOnlyList<PersonAuswahl> Waehlbar, Freigabe ManuellBesetzen);

public sealed record SitzVertretung(Guid PersonId, string Name, bool Abwesend, bool Abgesagt, Vertretungsfall? Fall, string? Vertreter, Freigabe Anfragen);

public sealed record StellvertretungZeile(string Name, int? Rang, bool Abwesend);

public sealed record TerminVertretungen(Termin Termin, Gremium Gremium, IReadOnlyList<SitzVertretung> Sitze, IReadOnlyList<StellvertretungZeile> Stellvertretungen);

public sealed record Anfrageergebnis(Vertretungsstatus Status, string Meldung, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

/// <summary>
/// Abwesenheit und Vertretung in Gremiensitzungen: Abwesenheit eintragen → betroffene Sitzungen finden → Stellvertretungen
/// in Rangfolge anfragen (Postausgang, Push) → Zusage trägt ein, Ablehnung oder Fristablauf geht zur nächsten Person →
/// ohne Zusage Aufgabe an den Vorstand.
/// </summary>
public sealed class VertretungDienst(
    IVertretungSpeicher speicher,
    ITerminSpeicher termine,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Vertretung";

    /// <summary>Abwesenheit eintragen. Der Grund ist optional und privat (nie in Protokoll oder Nachrichten).</summary>
    public async Task<Abwesenheitsergebnis> EintragenAsync(DateOnly von, DateOnly bis, string? grund, Guid? fuerPersonId = null,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.AbwesenheitMelden(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var personId = fuerPersonId ?? wer.PersonIdPflicht;
        if (personId != wer.PersonId && wer.Rolle != Rolle.Administration)
        {
            throw new InvalidOperationException("Abwesenheiten anderer Personen trägt nur die Administration ein.");
        }

        if (bis < Ortszeit.Heute(uhr))
        {
            throw new InvalidOperationException("Die Abwesenheit liegt vollständig in der Vergangenheit.");
        }

        var abwesenheit = new Abwesenheit(personId, von, bis, grund, uhr.GetUtcNow());
        speicher.Hinzufuegen(abwesenheit);
        await protokoll.ProtokollierenAsync("Abwesenheit.Eingetragen",
            $"Abwesenheit von {Pseudonym.Person(personId)} vom {von:dd.MM.yyyy} bis {bis:dd.MM.yyyy} eingetragen", nameof(Abwesenheit), abwesenheit.Id,
            abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);

        var sitzungen = await BetroffeneSitzungenAsync(abwesenheit, abbruch);
        return new Abwesenheitsergebnis(abwesenheit, sitzungen, Schritte(abwesenheit, sitzungen));
    }

    public async Task<Abwesenheitsergebnis?> AbwesenheitAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await speicher.AbwesenheitAsync(id, abbruch) is not { } a || (a.PersonId != wer.PersonId && Rechte.VertretungenSteuern(wer.Rolle) is { Erlaubt: false }))
        {
            return null;
        }

        var sitzungen = await BetroffeneSitzungenAsync(a, abbruch);
        return new Abwesenheitsergebnis(a, sitzungen, Schritte(a, sitzungen));
    }

    public async Task<IReadOnlyList<AbwesenheitZeile>> MeineAbwesenheitenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.PersonId is not { } personId)
        {
            return [];
        }

        var ergebnis = new List<AbwesenheitZeile>();
        foreach (var a in (await speicher.AbwesenheitenDerPersonAsync(personId, abbruch)).OrderByDescending(a => a.Von).ThenBy(a => a.Id))
        {
            var sitzungen = a.Aktiv ? await BetroffeneSitzungenAsync(a, abbruch) : [];
            ergebnis.Add(new AbwesenheitZeile(a, Rechte.SiehtAbwesenheitsgrund(wer, a.PersonId) ? a.Grund : null, sitzungen.Count,
                sitzungen.Count(s => s.Fall is null || s.Fall.IstOffen), sitzungen.Count(s => s.Fall?.Status == Vertretungsstatus.Besetzt)));
        }

        return ergebnis;
    }

    /// <summary>„Ich bin doch da“: Abwesenheit zurückziehen; laufende Vertretungen entfallen, Gefragte werden informiert.</summary>
    public async Task ZurueckziehenAsync(Guid abwesenheitId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var a = await speicher.AbwesenheitAsync(abwesenheitId, abbruch) ?? throw new InvalidOperationException("Abwesenheit nicht gefunden.");
        if (a.PersonId != wer.PersonId && wer.Rolle != Rolle.Administration)
        {
            throw new InvalidOperationException("Nur die Person selbst oder die Administration kann die Abwesenheit zurückziehen.");
        }

        var jetzt = uhr.GetUtcNow();
        a.Zurueckziehen(jetzt);
        foreach (var fall in (await speicher.FaelleDerAbwesenheitAsync(a.Id, abbruch)).Where(f => f.IstOffen))
        {
            await EntfallenAsync(fall, "Die Abwesenheit wurde zurückgezogen", abbruch);
        }

        await protokoll.ProtokollierenAsync("Abwesenheit.Zurueckgezogen", $"Abwesenheit von {Pseudonym.Person(a.PersonId)} zurückgezogen",
            nameof(Abwesenheit), a.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Für alle betroffenen Sitzungen ohne laufenden Fall die Stellvertretungen anfragen.</summary>
    public async Task<IReadOnlyList<Vertretungsfall>> AnfragenAsync(Guid abwesenheitId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var a = await speicher.AbwesenheitAsync(abwesenheitId, abbruch) ?? throw new InvalidOperationException("Abwesenheit nicht gefunden.");
        if (a.PersonId != wer.PersonId && Rechte.VertretungenSteuern(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var faelle = new List<Vertretungsfall>();
        foreach (var s in (await BetroffeneSitzungenAsync(a, abbruch)).Where(s => s.Fall is null))
        {
            faelle.Add(await StartenAsync(s.Termin, s.Gremium, a.PersonId, a.Id, abbruch));
        }

        await speicher.SpeichernAsync(abbruch);
        return faelle;
    }

    /// <summary>Vertretung für eine einzelne Sitzung anfragen (z. B. nach einer Absage ohne eingetragene Abwesenheit).</summary>
    public async Task<Vertretungsfall> FuerTerminAnfragenAsync(Guid terminId, Guid? fuerPersonId = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var personId = fuerPersonId ?? wer.PersonIdPflicht;
        var termin = await termine.TerminAsync(terminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (FuerTerminFreigabe(termin, personId, wer, await stammdaten.MitgliedschaftenAsync(abbruch),
                await speicher.FaelleZuTerminenAsync([terminId], abbruch)) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var gremium = await stammdaten.GremiumAsync(termin.GremiumId!.Value, abbruch) ?? throw new InvalidOperationException("Gremium nicht gefunden.");
        var fall = await StartenAsync(termin, gremium, personId, null, abbruch);
        await speicher.SpeichernAsync(abbruch);
        return fall;
    }

    /// <summary>Darf für diese Person in dieser Sitzung eine Vertretung angefragt werden?</summary>
    public Freigabe FuerTerminFreigabe(Termin termin, Guid personId, Benutzer wer, IEnumerable<Mitgliedschaft> mitgliedschaften,
        IEnumerable<Vertretungsfall> faelle)
    {
        ArgumentNullException.ThrowIfNull(termin);
        ArgumentNullException.ThrowIfNull(wer);
        if (personId != wer.PersonId && Rechte.VertretungenSteuern(wer.Rolle) is { Erlaubt: false } recht)
        {
            return recht;
        }

        if (!termin.Art.IstGremiensitzung() || termin.GremiumId is not { } gremiumId)
        {
            return Freigabe.Nein("Vertretungen gibt es nur für Rats- und Ausschusssitzungen.");
        }

        if (termin.Abgesagt)
        {
            return Freigabe.Nein("Die Sitzung ist abgesagt.");
        }

        if (termin.Beginn <= Ortszeit.Jetzt(uhr))
        {
            return Freigabe.Nein("Die Sitzung hat bereits begonnen.");
        }

        if (!Besetzung.OrdentlicheAm(mitgliedschaften, gremiumId, termin.Tag, uhr.GetUtcNow()).Contains(personId))
        {
            return Freigabe.Nein("Vertreten werden nur ordentliche Mitglieder des Gremiums.");
        }

        return faelle.Any(f => f.TerminId == termin.Id && f.VertretenePersonId == personId && f.Status != Vertretungsstatus.Entfallen)
            ? Freigabe.Nein("Für diese Sitzung läuft bereits eine Vertretungsanfrage.")
            : Freigabe.Ja;
    }

    /// <summary>Antwort der gefragten Stellvertretung (Ein-Klick: „Ich vertrete“ / „Kann nicht“).</summary>
    public async Task<Anfrageergebnis> AntwortenAsync(Guid anfrageId, bool zusage, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var personId = wer.PersonIdPflicht;
        var fall = await speicher.FallZurAnfrageAsync(anfrageId, abbruch) ?? throw new InvalidOperationException("Anfrage nicht gefunden.");
        var anfrage = fall.Anfragen.First(a => a.Id == anfrageId);
        if (anfrage.PersonId != personId)
        {
            throw new InvalidOperationException("Diese Anfrage richtet sich an eine andere Person.");
        }

        if (anfrage.Status != Anfragestatus.Offen)
        {
            return new Anfrageergebnis(fall.Status, anfrage.Status switch
            {
                Anfragestatus.Zugesagt => "Sie haben bereits zugesagt.",
                Anfragestatus.Abgelehnt => "Sie haben bereits abgesagt.",
                Anfragestatus.Abgelaufen => "Die Antwortfrist ist abgelaufen; die Anfrage ging an die nächste Person.",
                _ => "Die Anfrage wurde zurückgezogen; die Vertretung wird nicht mehr gebraucht.",
            }, [new NaechsterSchritt("Zur Startseite", "")]);
        }

        var termin = await termine.TerminAsync(fall.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        var schritt = fall.Antworten(personId, zusage, uhr.GetUtcNow(), Frist(termin));
        await protokoll.ProtokollierenAsync(zusage ? "Vertretung.Zugesagt" : "Vertretung.Abgelehnt",
            $"{Pseudonym.Person(personId)} hat die Vertretung {(zusage ? "zugesagt" : "abgelehnt")}", nameof(Vertretungsfall), fall.Id, abbruch: abbruch);
        await SchrittVerarbeitenAsync(fall, termin, schritt, abbruch);
        await speicher.SpeichernAsync(abbruch);

        return zusage
            ? new Anfrageergebnis(fall.Status, $"Danke! Sie vertreten am {termin.Beginn:dd.MM.yyyy} um {termin.Beginn:HH:mm} Uhr. Der Termin steht in Ihrem Kalender.",
                [new NaechsterSchritt("Termin ansehen", $"termine/{termin.Id}", Hauptschritt: true), new NaechsterSchritt("Zur Startseite", "")])
            : new Anfrageergebnis(fall.Status, "Danke für die Rückmeldung. Die Anfrage geht an die nächste Stellvertretung.",
                [new NaechsterSchritt("Zur Startseite", "", Hauptschritt: true)]);
    }

    /// <summary>Hintergrundjob: abgelaufene Fristen weitergeben, vergangene oder abgesagte Sitzungen abschließen.</summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        var ortsjetzt = Ortszeit.Jetzt(uhr);
        var schritte = 0;
        foreach (var fall in await speicher.OffeneFaelleAsync(abbruch))
        {
            if (await termine.TerminAsync(fall.TerminId, abbruch) is not { } termin)
            {
                continue;
            }

            if (termin.Abgesagt || termin.Beginn <= ortsjetzt)
            {
                await EntfallenAsync(fall, termin.Abgesagt ? "Die Sitzung wurde abgesagt" : "Die Sitzung hat begonnen", abbruch);
                schritte++;
                continue;
            }

            if (fall.FristPruefen(jetzt, Frist(termin)) is { } schritt)
            {
                await protokoll.ProtokollierenAsync("Vertretung.FristAbgelaufen", "Antwortfrist abgelaufen, Anfrage geht weiter", nameof(Vertretungsfall),
                    fall.Id, abbruch: abbruch);
                await SchrittVerarbeitenAsync(fall, termin, schritt, abbruch);
                schritte++;
            }
        }

        if (schritte > 0)
        {
            await speicher.SpeichernAsync(abbruch);
        }

        return schritte;
    }

    /// <summary>Vorstand trägt nach einer Eskalation selbst eine Vertretung ein.</summary>
    public async Task ManuellBesetzenAsync(Guid fallId, Guid vertreterId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.VertretungenSteuern(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var fall = await speicher.FallAsync(fallId, abbruch) ?? throw new InvalidOperationException("Vertretungsfall nicht gefunden.");
        var termin = await termine.TerminAsync(fall.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        var schritt = fall.ManuellBesetzen(vertreterId, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Vertretung.ManuellBesetzt", $"Vertretung von Hand mit {Pseudonym.Person(vertreterId)} besetzt",
            nameof(Vertretungsfall), fall.Id, abbruch: abbruch);
        await SchrittVerarbeitenAsync(fall, termin, schritt, abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Übersicht „Sitzungen mit offener Vertretung“ (auch: abwesend, aber noch nicht angefragt).</summary>
    public async Task<IReadOnlyList<OffeneVertretung>> OffeneVertretungenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var ortsjetzt = Ortszeit.Jetzt(uhr);
        var bis = heute.AddDays(optionen.Value.VorschauTage);
        var sitzungen = (await termine.TermineAsync(ortsjetzt, bis.AddDays(1).ToDateTime(TimeOnly.MinValue), abbruch))
            .Where(t => t.Art.IstGremiensitzung() && t.GremiumId is not null && !t.Abgesagt).ToList();
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id);
        var personen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        var mitgliedschaften = await stammdaten.MitgliedschaftenAsync(abbruch);
        var abwesenheiten = await speicher.AbwesenheitenImZeitraumAsync(heute, bis, abbruch);
        var faelle = await speicher.FaelleZuTerminenAsync(sitzungen.Select(t => t.Id).ToList(), abbruch);
        var teilnahmen = await termine.TeilnahmenAsync(sitzungen.Select(t => t.Id).ToList(), abbruch);
        string Name(Guid id) => personen.TryGetValue(id, out var p) ? p.Anzeigename : "?";

        var liste = new List<OffeneVertretung>();
        foreach (var t in sitzungen)
        {
            var gremium = gremien[t.GremiumId!.Value];
            foreach (var personId in Besetzung.OrdentlicheAm(mitgliedschaften, gremium.Id, t.Tag, jetzt))
            {
                var fall = faelle.Where(f => f.TerminId == t.Id && f.VertretenePersonId == personId && f.Status != Vertretungsstatus.Entfallen)
                    .OrderByDescending(f => f.ErstelltAm).FirstOrDefault();
                var fehlt = abwesenheiten.Any(a => a.PersonId == personId && a.Umfasst(t.Tag))
                    || teilnahmen.Any(x => x.TerminId == t.Id && x.PersonId == personId && x.Status == Teilnahmestatus.Abgesagt);
                if (fall is { Status: Vertretungsstatus.Besetzt } || (fall is null && !fehlt))
                {
                    continue;
                }

                var offen = fall?.OffeneAnfrage;
                liste.Add(new OffeneVertretung(t, gremium, personId, Name(personId), fall, offen is null ? null : Name(offen.PersonId), offen?.FristBis,
                    fall?.Kandidaten.Count ?? Besetzung.StellvertretungenAm(mitgliedschaften, gremium.Id, t.Tag, jetzt).Count, fall?.Anfragen.Count ?? 0));
            }
        }

        return liste.OrderBy(v => v.Termin.Beginn).ThenBy(v => v.Gremium.Sortierung).ThenBy(v => v.Vertretene, StringComparer.CurrentCulture).ToList();
    }

    /// <summary>Offene Anfragen an die angemeldete Person.</summary>
    public async Task<IReadOnlyList<MeineAnfrage>> MeineAnfragenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.PersonId is not { } personId)
        {
            return [];
        }

        var ergebnis = new List<MeineAnfrage>();
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id);
        foreach (var fall in (await speicher.OffeneFaelleAsync(abbruch)).Where(f => f.OffeneAnfrage?.PersonId == personId))
        {
            if (await termine.TerminAsync(fall.TerminId, abbruch) is not { } termin)
            {
                continue;
            }

            var vertretene = await stammdaten.PersonAsync(fall.VertretenePersonId, abbruch);
            ergebnis.Add(new MeineAnfrage(fall.OffeneAnfrage!.Id, fall.Id, termin, gremien.TryGetValue(fall.GremiumId, out var g) ? g.Name : "?",
                vertretene?.Anzeigename ?? "?", fall.OffeneAnfrage.FristBis));
        }

        return ergebnis.OrderBy(a => a.Termin.Beginn).ThenBy(a => a.AnfrageId).ToList();
    }

    /// <summary>Anfrage mit Termin für die Antwortseite (nur für die gefragte Person).</summary>
    public async Task<(MeineAnfrage Anfrage, Anfragestatus Status)?> AnfrageAsync(Guid anfrageId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await speicher.FallZurAnfrageAsync(anfrageId, abbruch) is not { } fall
            || fall.Anfragen.First(a => a.Id == anfrageId) is not { } anfrage || anfrage.PersonId != wer.PersonId
            || await termine.TerminAsync(fall.TerminId, abbruch) is not { } termin)
        {
            return null;
        }

        var gremium = await stammdaten.GremiumAsync(fall.GremiumId, abbruch);
        var vertretene = await stammdaten.PersonAsync(fall.VertretenePersonId, abbruch);
        return (new MeineAnfrage(anfrage.Id, fall.Id, termin, gremium?.Name ?? "?", vertretene?.Anzeigename ?? "?", anfrage.FristBis), anfrage.Status);
    }

    /// <summary>Verlauf eines Vertretungsfalls mit allen Anfragen (Vorstand, Administration und die vertretene Person).</summary>
    public async Task<FallDetail?> FallAsync(Guid fallId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await speicher.FallAsync(fallId, abbruch) is not { } fall || await termine.TerminAsync(fall.TerminId, abbruch) is not { } termin
            || await stammdaten.GremiumAsync(fall.GremiumId, abbruch) is not { } gremium)
        {
            return null;
        }

        var steuert = Rechte.VertretungenSteuern(wer.Rolle);
        if (!steuert.Erlaubt && wer.PersonId != fall.VertretenePersonId && fall.Anfragen.All(a => a.PersonId != wer.PersonId))
        {
            return null;
        }

        var personen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        string Name(Guid id) => personen.TryGetValue(id, out var p) ? p.Anzeigename : "?";
        var gefragt = fall.Anfragen.Select(a => a.PersonId).ToHashSet();
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var ordentliche = Besetzung.OrdentlicheAm(await stammdaten.MitgliedschaftenAsync(abbruch), gremium.Id, termin.Tag, jetzt).ToHashSet();
        var waehlbar = personen.Values
            .Where(p => p.Aktiv && p.Id != fall.VertretenePersonId && !ordentliche.Contains(p.Id)
                        && p.RollenAm(heute, jetzt).Any(r => r is Rolle.Mitglied or Rolle.SachkundigeBuergerin))
            .OrderBy(p => p.Nachname, StringComparer.CurrentCulture).Select(p => new PersonAuswahl(p.Id, p.Sortiername)).ToList();
        return new FallDetail(fall, termin, gremium, Name(fall.VertretenePersonId),
            fall.Anfragen.OrderBy(a => a.Reihenfolge).Select(a => new AnfrageZeile(a.Id, Name(a.PersonId), a.Reihenfolge, a.Status, a.GestelltAm, a.FristBis,
                a.BeantwortetAm)).ToList(),
            fall.Kandidaten.Where(k => !gefragt.Contains(k)).Select(k => new PersonAuswahl(k, Name(k))).ToList(),
            fall.VertreterPersonId is { } v ? Name(v) : null,
            waehlbar,
            steuert.Und(() => fall.IstOffen ? Freigabe.Ja : Freigabe.Nein("Der Fall ist abgeschlossen.")));
    }

    /// <summary>Vertretungslage einer Sitzung: je ordentlichem Mitglied, ob es fehlt und wie die Vertretung steht.</summary>
    public async Task<TerminVertretungen?> TerminAsync(Guid terminId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await termine.TerminAsync(terminId, abbruch) is not { GremiumId: { } gremiumId } termin
            || await stammdaten.GremiumAsync(gremiumId, abbruch) is not { } gremium)
        {
            return null;
        }

        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await stammdaten.MitgliedschaftenAsync(abbruch);
        var personen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        string Name(Guid id) => personen.TryGetValue(id, out var p) ? p.Anzeigename : "?";
        var faelle = await speicher.FaelleZuTerminenAsync([terminId], abbruch);
        var teilnahmen = await termine.TeilnahmenAsync([terminId], abbruch);
        var abwesend = (await speicher.AbwesenheitenImZeitraumAsync(termin.Tag, termin.Tag, abbruch)).Select(a => a.PersonId).ToHashSet();
        var sitze = Besetzung.OrdentlicheAm(mitgliedschaften, gremiumId, termin.Tag, jetzt).Select(id =>
        {
            var fall = faelle.Where(f => f.VertretenePersonId == id && f.Status != Vertretungsstatus.Entfallen).OrderByDescending(f => f.ErstelltAm).FirstOrDefault();
            return new SitzVertretung(id, Name(id), abwesend.Contains(id),
                teilnahmen.Any(t => t.PersonId == id && t.Status == Teilnahmestatus.Abgesagt), fall,
                fall?.VertreterPersonId is { } v ? Name(v) : null,
                FuerTerminFreigabe(termin, id, wer, mitgliedschaften, faelle));
        }).OrderBy(s => s.Name, StringComparer.CurrentCulture).ToList();
        var stellvertretungen = Besetzung.Am(mitgliedschaften, gremiumId, termin.Tag, jetzt).Where(s => s.Funktion == Funktion.Stellvertretung)
            .Select(s => new StellvertretungZeile(Name(s.PersonId), s.Rang, abwesend.Contains(s.PersonId))).ToList();
        return new TerminVertretungen(termin, gremium, sitze, stellvertretungen);
    }

    private async Task<IReadOnlyList<BetroffeneSitzung>> BetroffeneSitzungenAsync(Abwesenheit a, CancellationToken abbruch)
    {
        var ab = Ortszeit.Jetzt(uhr);
        var vonZeit = a.Von.ToDateTime(TimeOnly.MinValue);
        var sitzungen = (await termine.TermineAsync(vonZeit > ab ? vonZeit : ab, a.Bis.AddDays(1).ToDateTime(TimeOnly.MinValue), abbruch))
            .Where(t => t.Art.IstGremiensitzung() && t.GremiumId is not null && !t.Abgesagt).ToList();
        if (sitzungen.Count == 0)
        {
            return [];
        }

        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await stammdaten.MitgliedschaftenAsync(abbruch);
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id);
        var faelle = await speicher.FaelleZuTerminenAsync(sitzungen.Select(t => t.Id).ToList(), abbruch);
        return sitzungen
            .Where(t => Besetzung.OrdentlicheAm(mitgliedschaften, t.GremiumId!.Value, t.Tag, jetzt).Contains(a.PersonId))
            .Select(t => new BetroffeneSitzung(t, gremien[t.GremiumId!.Value],
                faelle.Where(f => f.VertretenePersonId == a.PersonId && f.TerminId == t.Id && f.Status != Vertretungsstatus.Entfallen)
                    .OrderByDescending(f => f.ErstelltAm).FirstOrDefault()))
            .ToList();
    }

    private static IReadOnlyList<NaechsterSchritt> Schritte(Abwesenheit a, IReadOnlyList<BetroffeneSitzung> sitzungen)
    {
        var ohneFall = sitzungen.Count(s => s.Fall is null);
        var schritte = new List<NaechsterSchritt>();
        if (ohneFall > 0)
        {
            schritte.Add(new NaechsterSchritt(
                ohneFall == 1 ? "In 1 Sitzung fehlt dann jemand: Vertretung anfragen" : $"In {ohneFall} Sitzungen fehlt dann jemand: Vertretung anfragen",
                $"abwesenheit/{a.Id}?anfragen=1", "Ihre Stellvertretungen werden in der Reihenfolge ihres Rangs gefragt.", Hauptschritt: true));
        }

        schritte.Add(new NaechsterSchritt("Sitzungen mit offener Vertretung", "vertretungen"));
        schritte.Add(new NaechsterSchritt("Zur Startseite", ""));
        return schritte;
    }

    private async Task<Vertretungsfall> StartenAsync(Termin termin, Gremium gremium, Guid vertretenePersonId, Guid? abwesenheitId, CancellationToken abbruch)
    {
        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await stammdaten.MitgliedschaftenAsync(abbruch);
        var abwesend = (await speicher.AbwesenheitenImZeitraumAsync(termin.Tag, termin.Tag, abbruch)).Select(a => a.PersonId).ToHashSet();
        var bereitsVertretend = (await termine.TeilnahmenAsync([termin.Id], abbruch))
            .Where(t => t.Status is Teilnahmestatus.Vertritt or Teilnahmestatus.Abgesagt).Select(t => t.PersonId).ToHashSet();
        var personen = (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv).Select(p => p.Id).ToHashSet();
        var kandidaten = Besetzung.StellvertretungenAm(mitgliedschaften, gremium.Id, termin.Tag, jetzt)
            .Where(id => personen.Contains(id) && !abwesend.Contains(id) && !bereitsVertretend.Contains(id))
            .ToList();
        var (fall, schritt) = Vertretungsfall.Starten(termin.Id, gremium.Id, vertretenePersonId, abwesenheitId, kandidaten, jetzt, Frist(termin));
        speicher.Hinzufuegen(fall);
        await protokoll.ProtokollierenAsync("Vertretung.Gestartet",
            $"Vertretung für {Pseudonym.Person(vertretenePersonId)} gesucht ({kandidaten.Count} Stellvertretungen in Rangfolge)",
            nameof(Vertretungsfall), fall.Id, abbruch: abbruch);
        await SchrittVerarbeitenAsync(fall, termin, schritt, abbruch);
        return fall;
    }

    /// <summary>Nach jedem Schritt die passenden Nachrichten und Aufgaben anlegen (alles über den Postausgang).</summary>
    private async Task SchrittVerarbeitenAsync(Vertretungsfall fall, Termin termin, Vertretungsschritt schritt, CancellationToken abbruch)
    {
        var gremium = await stammdaten.GremiumAsync(fall.GremiumId, abbruch);
        var kurz = gremium?.Kurzname ?? "Sitzung";
        var wann = $"{termin.Beginn:dd.MM.} um {termin.Beginn:HH:mm} Uhr";
        var vertretene = await stammdaten.PersonAsync(fall.VertretenePersonId, abbruch);
        switch (schritt.Status)
        {
            case Vertretungsstatus.Angefragt when schritt.NeueAnfrage is { } anfrage:
                if (await stammdaten.PersonAsync(anfrage.PersonId, abbruch) is { } gefragt)
                {
                    var link = $"handy/anfrage/{anfrage.Id}";
                    await postausgang.BenachrichtigenAsync(gefragt, $"Vertretung gesucht: {kurz} am {wann}",
                        $"Kannst du am {wann} im {kurz} vertreten?",
                        $"Hallo {gefragt.Vorname},\n\n{vertretene?.Vorname ?? "ein Mitglied"} kann an der Sitzung „{termin.Titel}“ am {termin.Beginn:dddd, dd.MM.yyyy} "
                        + $"um {termin.Beginn:HH:mm} Uhr ({termin.Ort ?? "Ort folgt"}) nicht teilnehmen. Du bist als Stellvertretung an der Reihe.\n\n"
                        + $"Bitte antworte bis {Ortszeit.AlsOrtszeit(anfrage.FristBis):dd.MM. HH:mm} Uhr: {optionen.Value.Link(link)}\n\n"
                        + "Ohne Antwort geht die Anfrage an die nächste Stellvertretung.",
                        link, "Ich vertrete", abbruch);
                }

                break;

            case Vertretungsstatus.Besetzt when schritt.Vertreter is { } vertreterId:
                var teilnahme = await termine.TeilnahmeAsync(termin.Id, vertreterId, abbruch);
                if (teilnahme is null)
                {
                    termine.Hinzufuegen(new Teilnahme(termin.Id, vertreterId, Teilnahmestatus.Vertritt, uhr.GetUtcNow(), fall.VertretenePersonId));
                }
                else
                {
                    teilnahme.Setzen(Teilnahmestatus.Vertritt, uhr.GetUtcNow(), fall.VertretenePersonId);
                }

                var vertreter = await stammdaten.PersonAsync(vertreterId, abbruch);
                if (vertretene is not null)
                {
                    await postausgang.BenachrichtigenAsync(vertretene, $"Vertretung steht: {kurz} am {wann}",
                        $"{vertreter?.Vorname ?? "Jemand"} vertritt dich im {kurz} am {wann}.",
                        $"Hallo {vertretene.Vorname},\n\n{vertreter?.Anzeigename ?? "Eine Stellvertretung"} vertritt dich in der Sitzung „{termin.Titel}“ "
                        + $"am {termin.Beginn:dd.MM.yyyy} um {termin.Beginn:HH:mm} Uhr.\n\nGib bitte deine Unterlagen und Hinweise weiter.",
                        $"termine/{termin.Id}", abbruch: abbruch);
                }

                await AufgabeErledigenAsync(fall, "Vertretung besetzt", abbruch);
                break;

            case Vertretungsstatus.Eskaliert:
                await EskalierenAsync(fall, termin, gremium, vertretene, abbruch);
                break;
        }
    }

    private async Task EskalierenAsync(Vertretungsfall fall, Termin termin, Gremium? gremium, Person? vertretene, CancellationToken abbruch)
    {
        if (await speicher.OffeneAufgabeZumAnlassAsync(AufgabenAnlass, fall.Id, abbruch) is not null)
        {
            return;
        }

        var titel = $"Vertretung offen: {gremium?.Kurzname ?? "Sitzung"} am {termin.Beginn:dd.MM.yyyy}";
        var beschreibung = fall.Kandidaten.Count == 0
            ? $"Für {vertretene?.Anzeigename ?? "ein Mitglied"} gibt es keine verfügbare Stellvertretung."
            : $"Alle {fall.Kandidaten.Count} Stellvertretungen für {vertretene?.Anzeigename ?? "ein Mitglied"} haben abgesagt oder nicht rechtzeitig geantwortet.";
        speicher.Hinzufuegen(new Aufgabe(titel, beschreibung, Rolle.Vorstand, AufgabenAnlass, fall.Id, $"vertretungen/fall/{fall.Id}", uhr.GetUtcNow(),
            Ortszeit.Zeitpunkt(termin.Beginn).AddHours(-2)));
        await protokoll.ProtokollierenAsync("Vertretung.Eskaliert", "Keine Zusage: Aufgabe für den Vorstand angelegt", nameof(Vertretungsfall), fall.Id,
            abbruch: abbruch);

        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        foreach (var v in (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && p.RollenAm(heute, jetzt).Contains(Rolle.Vorstand)))
        {
            await postausgang.PushEinreihenAsync(v, titel, "Keine Stellvertretung hat zugesagt. Bitte kümmern.", $"vertretungen/fall/{fall.Id}",
                abbruch: abbruch);
        }
    }

    private async Task EntfallenAsync(Vertretungsfall fall, string grund, CancellationToken abbruch)
    {
        var offen = fall.OffeneAnfrage;
        fall.Entfallen(uhr.GetUtcNow());
        if (offen is not null && await stammdaten.PersonAsync(offen.PersonId, abbruch) is { } gefragt
            && await termine.TerminAsync(fall.TerminId, abbruch) is { } termin && termin.Beginn > Ortszeit.Jetzt(uhr))
        {
            await postausgang.PushEinreihenAsync(gefragt, "Vertretung nicht mehr nötig",
                $"Die Anfrage für den {termin.Beginn:dd.MM.} hat sich erledigt. Danke!", $"termine/{termin.Id}", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Vertretung.Entfallen", grund, nameof(Vertretungsfall), fall.Id, abbruch: abbruch);
        await AufgabeErledigenAsync(fall, grund, abbruch);
    }

    private async Task AufgabeErledigenAsync(Vertretungsfall fall, string vermerk, CancellationToken abbruch)
    {
        if (await speicher.OffeneAufgabeZumAnlassAsync(AufgabenAnlass, fall.Id, abbruch) is { } aufgabe)
        {
            aufgabe.Erledigen(uhr.GetUtcNow(), "Automatik", vermerk);
        }
    }

    private DateTimeOffset Frist(Termin termin)
        => Vertretungsfall.FristBerechnen(uhr.GetUtcNow(), Ortszeit.Zeitpunkt(termin.Beginn), optionen.Value.AntwortfristStunden);
}
