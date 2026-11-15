using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Aufgaben;

public sealed record AufgabeZeile(Aufgabe Aufgabe, string? Bearbeitet, bool Ueberfaellig, Freigabe Uebernehmen, Freigabe Erledigen, Freigabe ZurueckInKorb);

/// <summary>Aufgabenlisten aus Sicht der angemeldeten Person (wie „Was steht an?“ in VibeVHS).</summary>
public sealed record Aufgabenuebersicht(
    IReadOnlyList<AufgabeZeile> Meine,
    IReadOnlyList<AufgabeZeile> Korb,
    IReadOnlyList<AufgabeZeile> BeiAnderen,
    IReadOnlyList<AufgabeZeile> KuerzlichErledigt);

public sealed record ThemenpateZeile(Themenpate Pate, string Person, string? Vertretung);

/// <summary>
/// Aufgaben und Zuständigkeiten (Modul 12): Körbe je Rolle, Übernehmen, Zuweisen, Zurück in den Korb, Erledigen mit Vermerk,
/// Fristen mit Erinnerung, Themenpaten je Politikfeld.
/// </summary>
public sealed class AufgabenDienst(
    IVertretungSpeicher speicher,
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    /// <summary>Offene Aufgaben im Korb der aktiven Rolle (Administration sieht alle Körbe).</summary>
    public async Task<IReadOnlyList<Aufgabe>> KorbAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return wer.Rolle switch
        {
            Rolle.Administration => (await speicher.OffeneAufgabenAsync(null, abbruch)).Where(a => a.Zustaendig is not null && a.ZugewiesenPersonId is null).ToList(),
            { } rolle => (await speicher.OffeneAufgabenAsync(rolle, abbruch)).Where(a => a.ZugewiesenPersonId is null).ToList(),
            _ => [],
        };
    }

    public async Task<Aufgabenuebersicht> UebersichtAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var jetzt = uhr.GetUtcNow();
        var offen = await daten.ListeAsync(daten.Menge<Aufgabe>().Where(a => a.Status == Aufgabenstatus.Offen)
            .OrderBy(a => a.FaelligAm == null).ThenBy(a => a.FaelligAm).ThenBy(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch);
        var seit = jetzt.AddDays(-14);
        var erledigt = (await daten.ListeAsync(daten.Menge<Aufgabe>().Where(a => a.Status == Aufgabenstatus.Erledigt)
            .OrderByDescending(a => a.ErledigtAm).ThenBy(a => a.Id).Take(200), abbruch)).Where(a => a.ErledigtAm >= seit).ToList();
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        AufgabeZeile Zeile(Aufgabe a) => new(a, a.ZugewiesenPersonId is { } p && namen.TryGetValue(p, out var n) ? n : null, a.Ueberfaellig(jetzt),
            UebernehmenFreigabe(a, wer), ErledigenFreigabe(a, wer), a.Zustaendig is null ? Freigabe.Nein("Diese Aufgabe gehört keinem Korb.")
                : a.ZugewiesenPersonId is null ? Freigabe.Nein("Die Aufgabe liegt bereits im Korb.") : Freigabe.Ja);
        var meinKorb = wer.Rolle is Rolle.Administration ? (Func<Aufgabe, bool>)(a => a.Zustaendig is not null) : a => a.Zustaendig == wer.Rolle;
        return new Aufgabenuebersicht(
            offen.Where(a => a.ZugewiesenPersonId == wer.PersonId).Select(Zeile).ToList(),
            offen.Where(a => a.ZugewiesenPersonId is null && meinKorb(a)).Select(Zeile).ToList(),
            offen.Where(a => a.ZugewiesenPersonId is { } p && p != wer.PersonId && (meinKorb(a) || Rechte.VertretungenSteuern(wer.Rolle).Erlaubt)).Select(Zeile).ToList(),
            erledigt.Where(a => a.ZugewiesenPersonId == wer.PersonId || meinKorb(a)).Select(Zeile).ToList());
    }

    public static Freigabe UebernehmenFreigabe(Aufgabe a, Benutzer wer)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(wer);
        if (!a.IstOffen)
        {
            return Freigabe.Nein("Die Aufgabe ist erledigt.");
        }

        if (a.ZugewiesenPersonId == wer.PersonId)
        {
            return Freigabe.Nein("Sie bearbeiten die Aufgabe bereits.");
        }

        return a.ZugewiesenPersonId is null ? Freigabe.Ja : Freigabe.Nein("Die Aufgabe bearbeitet bereits jemand anderes; bitte zuweisen lassen.");
    }

    public static Freigabe ErledigenFreigabe(Aufgabe a, Benutzer wer)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(wer);
        if (!a.IstOffen)
        {
            return Freigabe.Nein("Die Aufgabe ist bereits erledigt.");
        }

        return a.ZugewiesenPersonId == wer.PersonId || wer.Rolle is Rolle.Administration || (a.ZugewiesenPersonId is null && a.Zustaendig == wer.Rolle)
            ? Freigabe.Ja
            : Freigabe.Nein("Bitte zuerst übernehmen – erledigen kann, wer die Aufgabe bearbeitet.");
    }

    public async Task UebernehmenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var a = await daten.LadenAsync<Aufgabe>(id, "Aufgabe", abbruch);
        if (UebernehmenFreigabe(a, wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        a.Uebernehmen(wer.PersonIdPflicht);
        await protokoll.ProtokollierenAsync("Aufgabe.Uebernommen", $"Aufgabe [{Kurz(a)}] übernommen", nameof(Aufgabe), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task ZuweisenAsync(Guid id, Guid personId, CancellationToken abbruch = default)
    {
        var a = await daten.LadenAsync<Aufgabe>(id, "Aufgabe", abbruch);
        a.Zuweisen(personId);
        if (await stammdaten.PersonAsync(personId, abbruch) is { } p)
        {
            await postausgang.PushEinreihenAsync(p, "Aufgabe zugewiesen", a.Titel, "aufgaben", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Aufgabe.Zugewiesen", $"Aufgabe [{Kurz(a)}] an {Pseudonym.Person(personId)} zugewiesen", nameof(Aufgabe), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task ZurueckInKorbAsync(Guid id, CancellationToken abbruch = default)
    {
        var a = await daten.LadenAsync<Aufgabe>(id, "Aufgabe", abbruch);
        a.ZurueckInKorb();
        await protokoll.ProtokollierenAsync("Aufgabe.ZurueckInKorb", $"Aufgabe [{Kurz(a)}] zurück in den Korb", nameof(Aufgabe), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task FristSetzenAsync(Guid id, DateOnly? frist, CancellationToken abbruch = default)
    {
        var a = await daten.LadenAsync<Aufgabe>(id, "Aufgabe", abbruch);
        a.FristSetzen(frist is { } f ? Ortszeit.Zeitpunkt(f.ToDateTime(new TimeOnly(18, 0))) : null);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task ErledigenAsync(Guid id, string? vermerk, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var aufgabe = await daten.LadenAsync<Aufgabe>(id, "Aufgabe", abbruch);
        if (ErledigenFreigabe(aufgabe, wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        aufgabe.Erledigen(uhr.GetUtcNow(), wer.Anzeige, vermerk);
        await protokoll.ProtokollierenAsync("Aufgabe.Erledigt", $"Aufgabe [{Kurz(aufgabe)}] erledigt", nameof(Aufgabe), aufgabe.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Neue persönliche Aufgabe (z. B. aus Antrag oder Anliegen) – optional an den Themenpaten des Politikfelds.</summary>
    public async Task<Aufgabe> AnlegenAsync(string titel, string? beschreibung, Guid? personId, Rolle? korb, string anlass, Guid anlassId, string ziel,
        DateOnly? frist, string? thema = null, CancellationToken abbruch = default)
    {
        if (personId is null && korb is null && thema is not null && await PateAsync(thema, abbruch) is { } pate)
        {
            personId = pate.PersonId;
        }

        var a = new Aufgabe(titel, beschreibung, personId is null ? korb ?? Rolle.Vorstand : korb, anlass, anlassId, ziel, uhr.GetUtcNow(),
            frist is { } f ? Ortszeit.Zeitpunkt(f.ToDateTime(new TimeOnly(18, 0))) : null, personId).MitThema(thema);
        daten.Hinzufuegen(a);
        if (personId is { } p && await stammdaten.PersonAsync(p, abbruch) is { } person)
        {
            await postausgang.PushEinreihenAsync(person, "Neue Aufgabe", titel, "aufgaben", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Aufgabe.Angelegt", $"Aufgabe [{Kurz(a)}] angelegt ({anlass})", nameof(Aufgabe), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return a;
    }

    /// <summary>Hintergrundjob: einmalige Erinnerung 24 Stunden vor Fälligkeit an die bearbeitende Person.</summary>
    public async Task<int> ErinnernAsync(CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        var grenze = jetzt.AddHours(24);
        var faellig = (await daten.ListeAsync(daten.Menge<Aufgabe>().Where(a => a.Status == Aufgabenstatus.Offen && a.ZugewiesenPersonId != null && a.ErinnertAm == null)
            .OrderBy(a => a.Id), abbruch)).Where(a => a.FaelligAm is { } f && f <= grenze).ToList();
        foreach (var a in faellig)
        {
            if (await stammdaten.PersonAsync(a.ZugewiesenPersonId!.Value, abbruch) is { } p)
            {
                await postausgang.PushEinreihenAsync(p, a.Ueberfaellig(jetzt) ? "Aufgabe überfällig" : "Aufgabe bald fällig", a.Titel, "aufgaben", abbruch: abbruch);
            }

            a.ErinnerungVermerken(jetzt);
        }

        if (faellig.Count > 0)
        {
            await daten.SpeichernAsync(abbruch);
        }

        return faellig.Count;
    }

    public async Task<IReadOnlyList<ThemenpateZeile>> ThemenpatenAsync(CancellationToken abbruch = default)
    {
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        return (await daten.ListeAsync(daten.Menge<Themenpate>().OrderBy(t => t.Politikfeld).ThenBy(t => t.Id), abbruch))
            .Select(t => new ThemenpateZeile(t, namen.GetValueOrDefault(t.PersonId, "?"), t.VertretungPersonId is { } v ? namen.GetValueOrDefault(v) : null)).ToList();
    }

    public async Task<Themenpate?> PateAsync(string politikfeld, CancellationToken abbruch = default)
        => (await daten.ListeAsync(daten.Menge<Themenpate>().OrderBy(t => t.Id), abbruch))
            .FirstOrDefault(t => string.Equals(t.Politikfeld, politikfeld.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public async Task ThemenpateSetzenAsync(string politikfeld, Guid personId, Guid? vertretungPersonId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.SitzungenSteuern(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (await PateAsync(politikfeld, abbruch) is { } pate)
        {
            pate.Aendern(personId, vertretungPersonId);
        }
        else
        {
            daten.Hinzufuegen(new Themenpate(politikfeld, personId, vertretungPersonId));
        }

        await daten.SpeichernAsync(abbruch);
    }

    private static string Kurz(Aufgabe a) => $"{a.Anlass}-{a.Id.ToString("N")[^8..]}";
}
