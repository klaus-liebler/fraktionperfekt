using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Stammdaten;

public sealed record PersonZeile(Guid Id, string Name, string Sortiername, string? EMail, IReadOnlyList<Rolle> Rollen, bool Aktiv,
    int Ordentlich, int Stellvertretung, bool AusDemRis);

public sealed record GremiumZeile(Guid Id, string Name, string Kurzname, Gremienart Art, bool AusDemRis, int Ordentlich, int Stellvertretung);

public sealed record MitgliedZeile(Guid MitgliedschaftId, Guid PersonId, string Name, Funktion Funktion, int? Rang, bool AusDemRis,
    IReadOnlyList<MitgliedschaftsVersion> Verlauf);

public sealed record GremiumDetail(Gremium Gremium, IReadOnlyList<MitgliedZeile> Ordentliche, IReadOnlyList<MitgliedZeile> Stellvertretungen,
    IReadOnlyList<MitgliedZeile> Ehemalige);

public sealed record PersonMitgliedschaft(Guid MitgliedschaftId, Guid GremiumId, string Gremium, Funktion? Funktion, int? Rang,
    IReadOnlyList<MitgliedschaftsVersion> Verlauf);

/// <param name="Heute">Alle Angaben, wie sie heute gelten (maßgeblich, nicht der Zwischenstand).</param>
/// <param name="KontakteSichtbar">Adressen, Rufnummern und E-Mails sehen die Person selbst, die Geschäftsführung und – mit Einwilligung
/// „Kontaktdaten in der Fraktion weitergeben“ – alle in der Fraktion.</param>
/// <param name="KontakteBearbeitbar">Kontakte und Einwilligungen pflegen die Person selbst und die Geschäftsführung.</param>
public sealed record PersonDetail(Person Person, IReadOnlyList<Rolle> Rollen, IReadOnlyList<PersonRollen> RollenVerlauf,
    IReadOnlyList<PersonMitgliedschaft> Mitgliedschaften, PersonenStand? Heute = null, bool KontakteSichtbar = false, bool KontakteBearbeitbar = false);

/// <summary>Datenhistorie einer Person (Zeitleiste aller Erfassungen); ohne Kontaktdaten, wenn sie nicht sichtbar sind.</summary>
public sealed record PersonHistorie(Person Person, IReadOnlyList<HistorienEintrag> Eintraege, bool KontakteSichtbar);


/// <summary>Personen, Gremien und Mitgliedschaften (Mandatsübersicht). Funktionen kommen aus dem RIS, der Rang ist fraktionsintern.</summary>
public sealed class StammdatenDienst(IStammdatenSpeicher speicher, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr)
{
    public async Task<IReadOnlyList<PersonZeile>> PersonenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var mitgliedschaften = await speicher.MitgliedschaftenAsync(abbruch);
        var staende = mitgliedschaften.Select(m => (m.PersonId, Stand: m.StandAm(heute, jetzt))).Where(x => x.Stand is not null).ToList();
        return (await speicher.PersonenAsync(abbruch))
            .Select(p => new PersonZeile(p.Id, p.Anzeigename, p.Sortiername, KontakteSichtbar(wer, p, heute, jetzt) ? p.EMail : null, p.RollenAm(heute, jetzt), p.Aktiv,
                staende.Count(s => s.PersonId == p.Id && s.Stand!.Funktion == Funktion.Ordentlich),
                staende.Count(s => s.PersonId == p.Id && s.Stand!.Funktion == Funktion.Stellvertretung),
                p.RisId is not null))
            .ToList();
    }

    public async Task<PersonDetail?> PersonAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await speicher.PersonAsync(id, abbruch) is not { } person)
        {
            return null;
        }

        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var gremien = (await speicher.GremienAsync(abbruch)).ToDictionary(g => g.Id);
        var mitgliedschaften = (await speicher.MitgliedschaftenAsync(abbruch)).Where(m => m.PersonId == id)
            .Select(m =>
            {
                var stand = m.StandAm(heute, jetzt);
                return new PersonMitgliedschaft(m.Id, m.GremiumId, gremien.TryGetValue(m.GremiumId, out var g) ? g.Name : "?", stand?.Funktion,
                    stand?.Rang, Verlauf(m));
            })
            .OrderBy(m => m.Funktion is null).ThenBy(m => m.Funktion).ThenBy(m => m.Gremium, StringComparer.CurrentCulture)
            .ToList();
        var rollenVerlauf = person.RollenVersionen.OrderByDescending(v => v.ErfasstAm).ThenByDescending(v => v.GueltigVon).ToList();
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return new PersonDetail(person, person.RollenAm(heute, jetzt), rollenVerlauf, mitgliedschaften, person.StandAm(heute, jetzt),
            KontakteSichtbar(wer, person, heute, jetzt), KontakteBearbeiten(wer, person.Id).Erlaubt);
    }

    /// <summary>Datenhistorie (Seite „Datenhistorie“): alle Erfassungen, Kontaktdaten nur, wenn sie sichtbar sind.</summary>
    public async Task<PersonHistorie?> HistorieAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await speicher.PersonAsync(id, abbruch) is not { } person)
        {
            return null;
        }

        var sichtbar = KontakteSichtbar(await benutzer.ErmittelnAsync(abbruch), person, Ortszeit.Heute(uhr), uhr.GetUtcNow());
        return new PersonHistorie(person, Datenhistorie.Erstellen(person, sichtbar), sichtbar);
    }

    /// <summary>Kontaktdaten sehen: die Person selbst, die Geschäftsführung, sonst nur mit Einwilligung zur Weitergabe in der Fraktion.</summary>
    public static bool KontakteSichtbar(Benutzer wer, Person person, DateOnly heute, DateTimeOffset jetzt)
    {
        ArgumentNullException.ThrowIfNull(wer);
        ArgumentNullException.ThrowIfNull(person);
        return wer.IstAutomatik || wer.PersonId == person.Id || Rechte.StammdatenPflegen(wer.Rolle).Erlaubt
               || person.EinwilligungAm(Einwilligungszweck.WeitergabeInDerFraktion, heute, jetzt);
    }

    /// <summary>Kontakte und Einwilligungen pflegen: die Person selbst und die Geschäftsführung.</summary>
    public static Freigabe KontakteBearbeiten(Benutzer wer, Guid personId)
    {
        ArgumentNullException.ThrowIfNull(wer);
        return wer.IstAutomatik || wer.PersonId == personId || Rechte.StammdatenPflegen(wer.Rolle).Erlaubt
            ? Freigabe.Ja
            : Freigabe.Nein("Kontaktdaten pflegen nur die Person selbst und die Geschäftsführung.");
    }

    /// <summary>Postadresse anlegen (<paramref name="adressId"/> leer) oder ab Datum ändern bzw. korrigieren.</summary>
    public async Task<Guid> AdresseSpeichernAsync(Guid personId, Guid? adressId, Kontaktbereich bereich, string? zusatz, string strasse, string postleitzahl,
        string ort, bool bevorzugt, DateOnly datum, bool korrektur, string grund, CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, korrektur, grund, abbruch);
        var id = adressId ?? person.AdresseHinzufuegen(bereich, zusatz, strasse, postleitzahl, ort, bevorzugt, datum, erfassung);
        if (adressId is { } vorhanden)
        {
            person.AdresseAendern(vorhanden, bereich, zusatz, strasse, postleitzahl, ort, bevorzugt, datum, erfassung, korrektur);
        }

        await KontaktProtokollierenAsync(personId, "Postadresse", adressId is null ? "angelegt" : korrektur ? "korrigiert" : $"geändert ab {datum:dd.MM.yyyy}", abbruch);
        return id;
    }

    /// <summary>E-Mail-Adresse anlegen oder ab Datum ändern bzw. korrigieren; die bevorzugte erhält die Mails der Fraktion.</summary>
    public async Task<Guid> EMailSpeichernAsync(Guid personId, Guid? adressId, Kontaktbereich bereich, string adresse, bool bevorzugt, DateOnly datum,
        bool korrektur, string grund, CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, korrektur, grund, abbruch);
        var id = adressId ?? person.EMailHinzufuegen(bereich, adresse, bevorzugt, datum, erfassung);
        if (adressId is { } vorhanden)
        {
            person.EMailAendern(vorhanden, bereich, adresse, bevorzugt, datum, erfassung, korrektur);
        }

        await KontaktProtokollierenAsync(personId, "E-Mail-Adresse", adressId is null ? "angelegt" : korrektur ? "korrigiert" : $"geändert ab {datum:dd.MM.yyyy}", abbruch);
        return id;
    }

    /// <summary>Rufnummer anlegen oder ab Datum ändern bzw. korrigieren (mit Messaging-Diensten und Einwilligung).</summary>
    public async Task<Guid> RufnummerSpeichernAsync(Guid personId, Guid? nummerId, RufnummerArt art, string nummer, Messagingdienst dienste, bool einwilligung,
        bool bevorzugt, DateOnly datum, bool korrektur, string grund, CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, korrektur, grund, abbruch);
        var id = nummerId ?? person.RufnummerHinzufuegen(art, nummer, dienste, einwilligung, bevorzugt, datum, erfassung);
        if (nummerId is { } vorhanden)
        {
            person.RufnummerAendern(vorhanden, art, nummer, dienste, einwilligung, bevorzugt, datum, erfassung, korrektur);
        }

        await KontaktProtokollierenAsync(personId, "Rufnummer", nummerId is null ? "angelegt" : korrektur ? "korrigiert" : $"geändert ab {datum:dd.MM.yyyy}", abbruch);
        return id;
    }

    /// <summary>Adresse, E-Mail oder Rufnummer gilt ab Datum nicht mehr (die Historie bleibt).</summary>
    public async Task KontaktAufgebenAsync(Guid personId, Kontaktart art, Guid id, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, false, grund, abbruch);
        switch (art)
        {
            case Kontaktart.Adresse:
                person.AdresseAufgebenAb(id, ab, erfassung);
                break;
            case Kontaktart.EMail:
                person.EMailAufgebenAb(id, ab, erfassung);
                break;
            default:
                person.RufnummerAbmeldenAb(id, ab, erfassung);
                break;
        }

        await KontaktProtokollierenAsync(personId, art.ToString(), $"aufgegeben ab {ab:dd.MM.yyyy}", abbruch);
    }

    /// <summary>Einwilligung ab Datum erteilen oder widerrufen.</summary>
    public async Task EinwilligungSetzenAsync(Guid personId, Einwilligungszweck zweck, bool erteilt, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, false, grund, abbruch);
        person.EinwilligungSetzenAb(zweck, erteilt, ab, erfassung);
        await KontaktProtokollierenAsync(personId, $"Einwilligung {zweck}", $"{(erteilt ? "erteilt" : "widerrufen")} ab {ab:dd.MM.yyyy}", abbruch);
    }

    private async Task<(Person Person, Erfassung Erfassung)> KontaktVorbereitenAsync(Guid personId, bool korrektur, string grund, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (KontakteBearbeiten(wer, personId) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var person = await speicher.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        var jetzt = uhr.GetUtcNow();
        return (person, korrektur ? Erfassung.Korrektur(jetzt, Grund(grund), wer.Anzeige) : Erfassung.Aenderung(jetzt, Grund(grund), wer.Anzeige));
    }

    private async Task KontaktProtokollierenAsync(Guid personId, string was, string wie, CancellationToken abbruch)
    {
        await protokoll.ProtokollierenAsync("Person.KontaktGeaendert", $"{was} von {Pseudonym.Person(personId)} {wie}", nameof(Person), personId, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    public async Task<IReadOnlyList<GremiumZeile>> GremienAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await speicher.MitgliedschaftenAsync(abbruch);
        return (await speicher.GremienAsync(abbruch)).Where(g => g.Aktiv)
            .Select(g =>
            {
                var sitze = Besetzung.Am(mitgliedschaften, g.Id, heute, jetzt);
                return new GremiumZeile(g.Id, g.Name, g.Kurzname, g.Art, g.AusDemRis,
                    sitze.Count(s => s.Funktion == Funktion.Ordentlich), sitze.Count(s => s.Funktion == Funktion.Stellvertretung));
            })
            .ToList();
    }

    public async Task<GremiumDetail?> GremiumAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await speicher.GremiumAsync(id, abbruch) is not { } gremium)
        {
            return null;
        }

        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var personen = (await speicher.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        var zeilen = (await speicher.MitgliedschaftenDesGremiumsAsync(id, abbruch))
            .Select(m => (m, Stand: m.StandAm(heute, jetzt)))
            .Select(x => new MitgliedZeile(x.m.Id, x.m.PersonId, personen.TryGetValue(x.m.PersonId, out var p) ? p.Anzeigename : "?",
                x.Stand?.Funktion ?? Funktion.Ordentlich, x.Stand?.Rang, x.m.RisId is not null, Verlauf(x.m)))
            .ToList();
        var aktuell = (await speicher.MitgliedschaftenDesGremiumsAsync(id, abbruch)).Where(m => m.StandAm(heute, jetzt) is not null)
            .Select(m => m.Id).ToHashSet();
        return new GremiumDetail(gremium,
            zeilen.Where(z => aktuell.Contains(z.MitgliedschaftId) && z.Funktion == Funktion.Ordentlich)
                .OrderBy(z => z.Name, StringComparer.CurrentCulture).ToList(),
            zeilen.Where(z => aktuell.Contains(z.MitgliedschaftId) && z.Funktion == Funktion.Stellvertretung)
                .OrderBy(z => z.Rang ?? int.MaxValue).ThenBy(z => z.Name, StringComparer.CurrentCulture).ToList(),
            zeilen.Where(z => !aktuell.Contains(z.MitgliedschaftId)).OrderBy(z => z.Name, StringComparer.CurrentCulture).ToList());
    }

    /// <summary>Den Vertretungsrang einer Stellvertretung ab einem Datum ändern (fraktionsintern, auch bei RIS-Gremien).</summary>
    public async Task RangAendernAsync(Guid mitgliedschaftId, int rang, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var m = await speicher.MitgliedschaftAsync(mitgliedschaftId, abbruch) ?? throw new InvalidOperationException("Mitgliedschaft nicht gefunden.");
        var stand = m.StandAm(ab, uhr.GetUtcNow()) ?? throw new InvalidOperationException($"Am {ab:dd.MM.yyyy} besteht keine Mitgliedschaft.");
        if (stand.Funktion != Funktion.Stellvertretung)
        {
            throw new InvalidOperationException("Einen Vertretungsrang haben nur Stellvertretungen.");
        }

        m.AendernAb(ab, Funktion.Stellvertretung, rang, Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Mitgliedschaft.RangGeaendert",
            $"Vertretungsrang von {Pseudonym.Person(m.PersonId)} ab {ab:dd.MM.yyyy} auf {rang} gesetzt", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Ob eine Mitgliedschaft in diesem Gremium von Hand gepflegt werden darf.</summary>
    public static Freigabe MitgliedschaftPflegen(Rolle? rolle, Gremium gremium)
    {
        ArgumentNullException.ThrowIfNull(gremium);
        return Rechte.StammdatenPflegen(rolle).Und(() => gremium.AusDemRis
            ? Freigabe.Nein("Mitgliedschaften in städtischen Gremien kommen aus dem Ratsinformationssystem und werden dort gepflegt. "
                + "Hier lässt sich nur der fraktionsinterne Vertretungsrang ändern.")
            : Freigabe.Ja);
    }

    /// <summary>Mitglied in einem fraktionsinternen Gremium (Arbeitskreis, Fraktion) aufnehmen.</summary>
    public async Task<Mitgliedschaft> MitgliedAufnehmenAsync(Guid gremiumId, Guid personId, Funktion funktion, int? rang, DateOnly ab,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var gremium = await speicher.GremiumAsync(gremiumId, abbruch) ?? throw new InvalidOperationException("Gremium nicht gefunden.");
        if (MitgliedschaftPflegen(wer.Rolle, gremium) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var m = new Mitgliedschaft(personId, gremiumId, funktion, rang, ab, Erfassung.Erst(uhr.GetUtcNow(), wer.Anzeige, "Aufnahme in das Gremium"));
        speicher.Hinzufuegen(m);
        await protokoll.ProtokollierenAsync("Mitgliedschaft.Angelegt",
            $"{Pseudonym.Person(personId)} ab {ab:dd.MM.yyyy} als {funktion.Anzeigename()} aufgenommen", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
        return m;
    }

    /// <summary>Mitgliedschaft in einem fraktionsinternen Gremium beenden (ab = erster Tag ohne Mitgliedschaft).</summary>
    public async Task MitgliedschaftBeendenAsync(Guid mitgliedschaftId, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var m = await speicher.MitgliedschaftAsync(mitgliedschaftId, abbruch) ?? throw new InvalidOperationException("Mitgliedschaft nicht gefunden.");
        var gremium = await speicher.GremiumAsync(m.GremiumId, abbruch) ?? throw new InvalidOperationException("Gremium nicht gefunden.");
        if (MitgliedschaftPflegen(wer.Rolle, gremium) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        m.BeendenAb(ab, Erfassung.Ende(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Mitgliedschaft.Beendet",
            $"Mitgliedschaft von {Pseudonym.Person(m.PersonId)} endet zum {ab:dd.MM.yyyy}", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Rollen einer Person ab einem Datum ändern (z. B. Wahl in den Vorstand).</summary>
    public async Task RollenAendernAsync(Guid personId, IReadOnlyCollection<Rolle> rollen, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var person = await speicher.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        person.RollenFestlegenAb(ab, rollen, Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Person.RollenGeaendert",
            $"Rollen von {Pseudonym.Person(personId)} ab {ab:dd.MM.yyyy}: {string.Join(", ", rollen.Select(r => r.Anzeigename()))}",
            nameof(Person), personId, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Die eigene Person (für „Meine Einstellungen“).</summary>
    public async Task<Person?> IchAsync(CancellationToken abbruch = default)
        => (await benutzer.ErmittelnAsync(abbruch)).PersonId is { } id ? await speicher.PersonAsync(id, abbruch) : null;

    /// <summary>Jede Person stellt ihre Ansprache selbst ein (Du/Sie, Stil, Titel, Anrede).</summary>
    public async Task AnspracheSpeichernAsync(string? anrede, Anredeform form, Begruessungsstil stil, bool titelInAnsprache, CancellationToken abbruch = default)
    {
        var person = await IchAsync(abbruch) ?? throw new InvalidOperationException("Diesem Konto ist keine Person zugeordnet.");
        person.AnspracheEinstellen(anrede, form, stil, titelInAnsprache, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Person.AnspracheGeaendert", "Eigene Ansprache eingestellt", nameof(Person), person.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Stand einer Person an einem Stichtag (nach heutigem Wissensstand oder dem Wissensstand <paramref name="bekanntAm"/>).</summary>
    public async Task<PersonenStand?> StandAmAsync(Guid personId, DateOnly tag, DateTimeOffset? bekanntAm = null, CancellationToken abbruch = default)
        => await speicher.PersonAsync(personId, abbruch) is { } p ? p.StandAm(tag, bekanntAm ?? uhr.GetUtcNow()) : null;

    /// <summary>Name ändern ab Datum (z. B. Heirat, neuer Titel) oder korrigieren (Tippfehler, gesamter Zeitraum der betroffenen Version).</summary>
    public async Task NameAendernAsync(Guid personId, string vorname, string nachname, string? titel, DateOnly datum, bool korrektur, string grund,
        CancellationToken abbruch = default, string? vorsatz = null, string? zusatz = null, bool nurEinName = false)
    {
        var wer = await PruefenAsync(abbruch);
        var person = await speicher.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        var erfassung = korrektur ? Erfassung.Korrektur(uhr.GetUtcNow(), Grund(grund), wer.Anzeige) : Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige);
        if (korrektur)
        {
            person.NameKorrigieren(vorname, nachname, titel, datum, erfassung, vorsatz, zusatz, nurEinName);
        }
        else
        {
            person.NameAendernAb(vorname, nachname, titel, datum, erfassung, vorsatz, zusatz, nurEinName);
        }

        await protokoll.ProtokollierenAsync(korrektur ? "Person.NameKorrigiert" : "Person.NameGeaendert",
            $"Name von {Pseudonym.Person(personId)} {(korrektur ? "korrigiert" : $"geändert ab {datum:dd.MM.yyyy}")}", nameof(Person), personId, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Anrede, Geschlecht oder Funktion ändern ab Datum oder korrigieren.</summary>
    public async Task AngabeAendernAsync(Guid personId, PersonMerkmalArt art, string? wert, DateOnly datum, bool korrektur, string grund,
        CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var person = await speicher.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        var erfassung = korrektur ? Erfassung.Korrektur(uhr.GetUtcNow(), Grund(grund), wer.Anzeige) : Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige);
        person.AngabeAendern(art, wert, datum, erfassung, korrektur);
        await protokoll.ProtokollierenAsync("Person.AngabeGeaendert",
            $"{art} von {Pseudonym.Person(personId)} {(korrektur ? "korrigiert" : $"geändert ab {datum:dd.MM.yyyy}")}", nameof(Person), personId, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Gültigkeitszeitraum einer Adresse, E-Mail oder Rufnummer korrigieren (z. B. falsches Umzugsdatum).</summary>
    public async Task KontaktZeitraumKorrigierenAsync(Guid personId, Kontaktart art, Guid id, DateOnly betroffenAm, DateOnly neuVon, DateOnly? neuBis, string grund,
        CancellationToken abbruch = default)
    {
        var (person, erfassung) = await KontaktVorbereitenAsync(personId, true, grund, abbruch);
        person.ZeitraumKorrigieren(art, id, betroffenAm, neuVon, neuBis, erfassung);
        await KontaktProtokollierenAsync(personId, art.ToString(), $"Zeitraum korrigiert ({neuVon:dd.MM.yyyy}–{(neuBis is { } b ? b.AddDays(-1).ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture) : "offen")})", abbruch);
    }

    private async Task<Benutzer> PruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && Rechte.StammdatenPflegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        return wer;
    }

    private static string Grund(string? grund) => string.IsNullOrWhiteSpace(grund) ? "ohne Angabe" : grund.Trim();

    private static IReadOnlyList<MitgliedschaftsVersion> Verlauf(Mitgliedschaft m)
        => m.Versionen.OrderByDescending(v => v.ErfasstAm).ThenByDescending(v => v.GueltigVon).ToList();
}
