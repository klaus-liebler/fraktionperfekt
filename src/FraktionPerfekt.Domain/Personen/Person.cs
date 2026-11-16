using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Eine Person der Fraktion (Ratsmitglied, sachkundige:r Bürger:in, Helfer:in) – nach dem Vorbild von VibeVHS: Die Person ist eine
/// Identität (Id); Name (Vorname, Nachname, Titel), Anrede, Geschlecht, Kontaktdaten, Funktion und Rollen gelten zeitlich und sind
/// bitemporal versioniert (gültig ab/bis und erfasst/ersetzt). Unterschieden wird „Änderung ab Datum“ (z. B. Heirat, neue Funktion)
/// von „Korrektur“ (Tippfehler: die betroffene Version wird berichtigt, die alte bleibt als ersetzt sichtbar). Maßgeblich ist
/// <see cref="StandAm"/>; <see cref="Vorname"/>, <see cref="Nachname"/>, <see cref="Titel"/>, <see cref="Anrede"/>, <see cref="EMail"/>
/// und <see cref="Mobil"/> sind ein Zwischenstand (heute gültig) für Listen, Suche und Anmeldung.
/// </summary>
public sealed class Person
{
    private readonly List<PersonRollen> _rollen = [];
    private readonly List<PersonName> _namen = [];
    private readonly List<PersonMerkmal> _merkmale = [];
    private readonly List<Postadresse> _adressen = [];
    private readonly List<EMailAdresse> _emails = [];
    private readonly List<Rufnummer> _rufnummern = [];
    private readonly List<Einwilligung> _einwilligungen = [];

    private Person()
    {
        Vorname = string.Empty;
        Nachname = string.Empty;
    }

    /// <param name="gueltigAb">Ab wann die Angaben gelten (Standard: Tag der Anlage).</param>
    public Person(string vorname, string nachname, string? eMail, string? benutzerkennung, DateTimeOffset angelegtAm, string? risId = null,
        Guid? id = null, string? titel = null, string? anrede = null, DateOnly? gueltigAb = null, string? funktion = null, string? von = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vorname);
        ArgumentException.ThrowIfNullOrWhiteSpace(nachname);
        Id = id ?? Ids.Neu();
        Vorname = vorname.Trim();
        Nachname = nachname.Trim();
        Benutzerkennung = string.IsNullOrWhiteSpace(benutzerkennung) ? null : benutzerkennung.Trim().ToLowerInvariant();
        RisId = risId;
        AngelegtAm = angelegtAm;
        Aktiv = true;
        var ab = gueltigAb ?? DateOnly.FromDateTime(angelegtAm.UtcDateTime);
        var erst = Erfassung.Erst(angelegtAm, von);
        _namen.Add(new PersonName(vorname, nachname, titel, ab, null, erst));
        _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.Anrede, AnredePruefen(anrede), ab, null, erst));
        if (!string.IsNullOrWhiteSpace(eMail))
        {
            _emails.Add(new EMailAdresse(Ids.Neu(), Kontaktbereich.Privat, eMail, true, ab, null, erst));
        }

        if (!string.IsNullOrWhiteSpace(funktion))
        {
            _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.Funktion, funktion, ab, null, erst));
        }

        ZwischenstandAktualisieren(angelegtAm);
    }

    public Guid Id { get; private set; }

    public string Vorname { get; private set; }

    /// <summary>Nachname, auch mehrteilig („Große Maestrup“, „Schulte Jochmaring“); danach wird sortiert.</summary>
    public string Nachname { get; private set; }

    /// <summary>Akademischer Grad („Dr.“) für förmliche Darstellungen (Listen, PDF, „Herr Dr. Schubert“); nicht sortiert und nie in der
    /// Du-Ansprache. Amtsbezeichnungen wie „Prof.“ gehören nicht hierher.</summary>
    public string? Titel { get; private set; }

    public string? EMail { get; private set; }

    /// <summary>Bevorzugte Mobilnummer (Zwischenstand) für SMS/Push-Zuordnung; optional.</summary>
    public string? Mobil { get; private set; }

    /// <summary>Anmeldename; ohne Kennung kann sich die Person nicht anmelden.</summary>
    public string? Benutzerkennung { get; private set; }

    /// <summary>Kennung der Person im Ratsinformationssystem (OParl), falls bekannt.</summary>
    public string? RisId { get; private set; }

    public bool Aktiv { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    /// <summary>„Herr“, „Frau“ oder <c>null</c> (keine Angabe/divers).</summary>
    public string? Anrede { get; private set; }

    /// <summary>Persönliche Einstellung: Du oder Sie (Standard: Du).</summary>
    public Anredeform Anredeform { get; private set; }

    /// <summary>Persönliche Einstellung: Stil der Begrüßung (Standard: locker).</summary>
    public Begruessungsstil Begruessungsstil { get; private set; }

    /// <summary>Persönliche Einstellung: akademischen Grad in der Sie-Form nennen (Standard: ja).</summary>
    public bool TitelInAnsprache { get; private set; } = true;

    /// <summary>
    /// Wie die Person angesprochen werden möchte (Benutzereinstellung, gilt sofort). Ändert sich dabei die Anrede (Personendatum),
    /// wird sie ab heute geändert; die bisherige bleibt in der Historie.
    /// </summary>
    public void AnspracheEinstellen(string? anrede, Anredeform form, Begruessungsstil stil, bool titelInAnsprache, DateTimeOffset? jetzt = null)
    {
        var neu = AnredePruefen(anrede);
        if (neu != Anrede)
        {
            var am = jetzt ?? DateTimeOffset.UtcNow;
            AngabeAendern(PersonMerkmalArt.Anrede, neu, DateOnly.FromDateTime(am.UtcDateTime), Erfassung.Aenderung(am, "Eigene Angabe in den Einstellungen"), false);
        }

        (Anredeform, Begruessungsstil, TitelInAnsprache) = (form, stil, titelInAnsprache);
    }

    private static string? AnredePruefen(string? anrede) => anrede?.Trim() switch
    {
        "Herr" => "Herr",
        "Frau" => "Frau",
        null or "" => null,
        var a => throw new ArgumentException($"Unbekannte Anrede „{a}“ (Herr, Frau oder keine Angabe).", nameof(anrede)),
    };

    public string Anzeigename => Titel is null ? $"{Vorname} {Nachname}" : $"{Titel} {Vorname} {Nachname}";

    public string Sortiername => $"{Nachname}, {Vorname}";

    /// <summary>Initialen für Avatare: erster Buchstabe des Vornamens und des Nachnamens („Große Maestrup“ → „JG“).</summary>
    public string Initialen => $"{char.ToUpperInvariant(Vorname[0])}{char.ToUpperInvariant(Nachname[0])}";

    /// <summary>Titel ab heute (Kurzform von <see cref="NameAendernAb"/>).</summary>
    public void TitelSetzen(string? titel, DateTimeOffset? jetzt = null)
    {
        var am = jetzt ?? DateTimeOffset.UtcNow;
        NameAendernAb(Vorname, Nachname, titel, DateOnly.FromDateTime(am.UtcDateTime), Erfassung.Aenderung(am, "Titel"));
    }

    public IReadOnlyCollection<PersonName> Namen => _namen;

    public IReadOnlyCollection<PersonMerkmal> Merkmale => _merkmale;

    public IReadOnlyCollection<Postadresse> Adressen => _adressen;

    public IReadOnlyCollection<EMailAdresse> EMails => _emails;

    public IReadOnlyCollection<Rufnummer> Rufnummern => _rufnummern;

    public IReadOnlyCollection<Einwilligung> Einwilligungen => _einwilligungen;

    /// <summary>Alle Versionen aller Angaben (für die Datenhistorie).</summary>
    public IEnumerable<IHistorienwert> AlleVersionen => _namen.Cast<IHistorienwert>().Concat(_merkmale).Concat(_rollen)
        .Concat(_adressen).Concat(_emails).Concat(_rufnummern).Concat(_einwilligungen);

    // ---------- Stichtag ----------

    /// <summary>Alle Angaben, wie sie am <paramref name="gueltigAm"/> galten, nach dem Wissensstand vom <paramref name="bekanntAm"/>.</summary>
    public PersonenStand StandAm(DateOnly gueltigAm, DateTimeOffset bekanntAm)
    {
        string? Merkmal(PersonMerkmalArt art) => _merkmale.Where(m => m.Art == art).AlsStichtag(gueltigAm, bekanntAm)?.WertOderNull;
        var adressen = Gueltige(_adressen, gueltigAm, bekanntAm).OrderByDescending(a => a.Bevorzugt).ThenBy(a => a.Bereich).ToList();
        var emails = Gueltige(_emails, gueltigAm, bekanntAm).OrderByDescending(e => e.Bevorzugt).ThenBy(e => e.Bereich).ToList();
        var nummern = Gueltige(_rufnummern, gueltigAm, bekanntAm).OrderByDescending(r => r.Bevorzugt).ThenBy(r => r.Art).ToList();
        var einwilligungen = Enum.GetValues<Einwilligungszweck>()
            .Select(z => _einwilligungen.Where(e => e.Zweck == z).AlsStichtag(gueltigAm, bekanntAm))
            .OfType<Einwilligung>().Where(e => !e.IstEndemarke).ToList();
        return new PersonenStand(gueltigAm, bekanntAm, _namen.AlsStichtag(gueltigAm, bekanntAm), Merkmal(PersonMerkmalArt.Anrede),
            Merkmal(PersonMerkmalArt.Geschlecht), Merkmal(PersonMerkmalArt.Funktion), RollenAm(gueltigAm, bekanntAm), adressen, emails, nummern,
            einwilligungen);
    }

    /// <summary>Je Gegenstand (Adresse, Nummer …) die am Stichtag gültige Version, ohne aufgegebene.</summary>
    private static List<T> Gueltige<T>(IEnumerable<T> versionen, DateOnly tag, DateTimeOffset bekanntAm)
        where T : BitemporaleVersion<T>
        => versionen.GroupBy(v => v.Gegenstand).Select(g => g.AlsStichtag(tag, bekanntAm)).OfType<T>().Where(v => !v.IstEndemarke).ToList();

    /// <summary>Ist am Stichtag die Einwilligung für den Zweck erteilt (nach Wissensstand <paramref name="bekanntAm"/>)?</summary>
    public bool EinwilligungAm(Einwilligungszweck zweck, DateOnly am, DateTimeOffset bekanntAm)
        => _einwilligungen.Where(e => e.Zweck == zweck).AlsStichtag(am, bekanntAm) is { IstEndemarke: false, Erteilt: true };

    /// <summary>Name zum damaligen Stand (z. B. Autor:in eines alten Antrags); vor der ersten Erfassung der früheste bekannte Name.</summary>
    public string AnzeigenameAm(DateOnly tag, DateTimeOffset bekanntAm)
        => (_namen.AlsStichtag(tag, bekanntAm) ?? _namen.AktuellerVerlauf().FirstOrDefault())?.Anzeige ?? Anzeigename;

    // ---------- Änderungen ----------

    /// <summary>Namensänderung ab einem Datum (z. B. Heirat, neuer Titel). Ältere Vorgänge behalten den damaligen Namen.</summary>
    public void NameAendernAb(string vorname, string nachname, string? titel, DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        ZeitlicheHistorie.AendernAb(_namen, new PersonName(vorname, nachname, titel, ab, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        ZwischenstandAktualisieren(erfassung.Am);
    }

    /// <summary>Korrektur eines falsch erfassten Namens für den gesamten Zeitraum der am <paramref name="betroffenAm"/> gültigen Version.</summary>
    public void NameKorrigieren(string vorname, string nachname, string? titel, DateOnly betroffenAm, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        ZeitlicheHistorie.Korrigieren(_namen, betroffenAm,
            (gv, gb) => new PersonName(vorname, nachname, titel, gv, gb, erfassung with { Art = Erfassungsart.Korrektur }), erfassung.Am);
        ZwischenstandAktualisieren(erfassung.Am);
    }

    /// <summary>Anrede, Geschlecht oder Funktion: Änderung ab Datum oder Korrektur der am Datum gültigen Angabe (leer = keine Angabe).</summary>
    public void AngabeAendern(PersonMerkmalArt art, string? wert, DateOnly datum, Erfassung erfassung, bool korrektur)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var inhalt = art == PersonMerkmalArt.Anrede ? AnredePruefen(wert) : wert?.Trim();
        if (!_merkmale.Any(m => m.Art == art))
        {
            _merkmale.Add(new PersonMerkmal(art, inhalt, korrektur ? DateOnly.MinValue : datum, null, erfassung with { Art = Erfassungsart.Ersterfassung }));
        }
        else if (korrektur)
        {
            ZeitlicheHistorie.Korrigieren(_merkmale, datum, (gv, gb) => new PersonMerkmal(art, inhalt, gv, gb, erfassung with { Art = Erfassungsart.Korrektur }),
                erfassung.Am, m => m.Art == art);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_merkmale, new PersonMerkmal(art, inhalt, datum, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        }

        ZwischenstandAktualisieren(erfassung.Am);
    }

    /// <summary>
    /// Zwischenstand (heute gültige Werte) für Listen und Suche neu berechnen. Läuft nach jeder Änderung und nachts, damit
    /// künftige Änderungen („gültig ab“ in der Zukunft) am Stichtag sichtbar werden.
    /// </summary>
    public void ZwischenstandAktualisieren(DateTimeOffset jetzt)
    {
        var stand = StandAm(DateOnly.FromDateTime(jetzt.UtcDateTime), jetzt);
        var name = stand.Name ?? _namen.AktuellerVerlauf().FirstOrDefault();
        if (name is not null)
        {
            (Vorname, Nachname, Titel) = (name.Vorname, name.Nachname, name.Titel);
        }

        (Anrede, EMail, Mobil) = (stand.Anrede, stand.EMail, stand.Mobil);

    }

    public IReadOnlyCollection<PersonRollen> RollenVersionen => _rollen;

    /// <summary>Rollen am Stichtag nach dem Wissensstand <paramref name="bekanntAm"/>.</summary>
    public IReadOnlyList<Rolle> RollenAm(DateOnly tag, DateTimeOffset bekanntAm)
        => _rollen.AlsStichtag(tag, bekanntAm)?.Rollen ?? [];

    /// <summary>Rollen ab einem Datum festlegen (Ersterfassung oder Änderung ab Datum).</summary>
    public void RollenFestlegenAb(DateOnly ab, IEnumerable<Rolle> rollen, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var liste = rollen.Distinct().Order().ToList();
        var art = _rollen.Count == 0 ? Erfassungsart.Ersterfassung : Erfassungsart.AenderungAbDatum;
        var neu = new PersonRollen(liste, ab, null, erfassung with { Art = art });
        if (_rollen.Count == 0)
        {
            _rollen.Add(neu);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_rollen, neu);
        }
    }

    // ---------- Adressen, E-Mails, Rufnummern, Einwilligungen (nach VibeVHS) ----------

    public Guid AdresseHinzufuegen(Kontaktbereich bereich, string? zusatz, string strasse, string postleitzahl, string ort, bool bevorzugt,
        DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var id = Ids.Neu();
        _adressen.Add(new Postadresse(id, bereich, zusatz, strasse, postleitzahl, ort, bevorzugt, ab, null, erfassung with { Art = Erfassungsart.Ersterfassung }));
        if (bevorzugt)
        {
            AndereAdressenNichtBevorzugt(id, ab, erfassung);
        }

        return id;
    }

    /// <summary>Adresse ab Datum ändern (z. B. neue Hausnummer nach Umbenennung) oder korrigieren (Tippfehler).</summary>
    public void AdresseAendern(Guid adressId, Kontaktbereich bereich, string? zusatz, string strasse, string postleitzahl, string ort, bool bevorzugt,
        DateOnly datum, Erfassung erfassung, bool korrektur)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        Pruefen(_adressen, a => a.AdressId == adressId, "Adresse");
        if (korrektur)
        {
            ZeitlicheHistorie.Korrigieren(_adressen, datum, (gv, gb) => new Postadresse(adressId, bereich, zusatz, strasse, postleitzahl, ort, bevorzugt, gv, gb,
                erfassung with { Art = Erfassungsart.Korrektur }), erfassung.Am, a => a.AdressId == adressId);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_adressen, new Postadresse(adressId, bereich, zusatz, strasse, postleitzahl, ort, bevorzugt, datum, null,
                erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        }

        if (bevorzugt)
        {
            AndereAdressenNichtBevorzugt(adressId, datum, erfassung);
        }
    }

    /// <summary>Die Adresse gilt ab <paramref name="ab"/> nicht mehr (z. B. nach Umzug); die Historie bleibt.</summary>
    public void AdresseAufgebenAb(Guid adressId, DateOnly ab, Erfassung erfassung)
        => Aufgeben(_adressen, a => a.AdressId == adressId, ab, erfassung, "Adresse");

    public Guid EMailHinzufuegen(Kontaktbereich bereich, string adresse, bool bevorzugt, DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var id = Ids.Neu();
        _emails.Add(new EMailAdresse(id, bereich, adresse, bevorzugt || !_emails.Any(), ab, null, erfassung with { Art = Erfassungsart.Ersterfassung }));
        if (bevorzugt)
        {
            AndereEMailsNichtBevorzugt(id, ab, erfassung);
        }

        ZwischenstandAktualisieren(erfassung.Am);
        return id;
    }

    public void EMailAendern(Guid adressId, Kontaktbereich bereich, string adresse, bool bevorzugt, DateOnly datum, Erfassung erfassung, bool korrektur)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        Pruefen(_emails, e => e.AdressId == adressId, "E-Mail-Adresse");
        if (korrektur)
        {
            ZeitlicheHistorie.Korrigieren(_emails, datum, (gv, gb) => new EMailAdresse(adressId, bereich, adresse, bevorzugt, gv, gb,
                erfassung with { Art = Erfassungsart.Korrektur }), erfassung.Am, e => e.AdressId == adressId);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_emails, new EMailAdresse(adressId, bereich, adresse, bevorzugt, datum, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        }

        if (bevorzugt)
        {
            AndereEMailsNichtBevorzugt(adressId, datum, erfassung);
        }

        ZwischenstandAktualisieren(erfassung.Am);
    }

    public void EMailAufgebenAb(Guid adressId, DateOnly ab, Erfassung erfassung)
    {
        Aufgeben(_emails, e => e.AdressId == adressId, ab, erfassung, "E-Mail-Adresse");
        ZwischenstandAktualisieren(erfassung.Am);
    }

    public Guid RufnummerHinzufuegen(RufnummerArt art, string nummer, Messagingdienst dienste, bool einwilligung, bool bevorzugt, DateOnly ab,
        Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var id = Ids.Neu();
        _rufnummern.Add(new Rufnummer(id, art, nummer, dienste, einwilligung, bevorzugt, ab, null, erfassung with { Art = Erfassungsart.Ersterfassung }));
        if (bevorzugt)
        {
            AndereNummernNichtBevorzugt(id, ab, erfassung);
        }

        ZwischenstandAktualisieren(erfassung.Am);
        return id;
    }

    /// <summary>Neue Angaben zu einer Nummer ab Datum (z. B. Einwilligung für Signal erteilt) oder Korrektur (Zahlendreher).</summary>
    public void RufnummerAendern(Guid nummerId, RufnummerArt art, string nummer, Messagingdienst dienste, bool einwilligung, bool bevorzugt,
        DateOnly datum, Erfassung erfassung, bool korrektur)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        Pruefen(_rufnummern, r => r.NummerId == nummerId, "Rufnummer");
        if (korrektur)
        {
            ZeitlicheHistorie.Korrigieren(_rufnummern, datum, (gv, gb) => new Rufnummer(nummerId, art, nummer, dienste, einwilligung, bevorzugt, gv, gb,
                erfassung with { Art = Erfassungsart.Korrektur }), erfassung.Am, r => r.NummerId == nummerId);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_rufnummern, new Rufnummer(nummerId, art, nummer, dienste, einwilligung, bevorzugt, datum, null,
                erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        }

        if (bevorzugt)
        {
            AndereNummernNichtBevorzugt(nummerId, datum, erfassung);
        }

        ZwischenstandAktualisieren(erfassung.Am);
    }

    public void RufnummerAbmeldenAb(Guid nummerId, DateOnly ab, Erfassung erfassung)
    {
        Aufgeben(_rufnummern, r => r.NummerId == nummerId, ab, erfassung, "Rufnummer");
        ZwischenstandAktualisieren(erfassung.Am);
    }

    /// <summary>Einwilligung ab Datum erteilen oder widerrufen; die frühere Angabe bleibt in der Historie (Nachweispflicht).</summary>
    public void EinwilligungSetzenAb(Einwilligungszweck zweck, bool erteilt, DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var erstes = !_einwilligungen.Any(e => e.Zweck == zweck);
        var neu = new Einwilligung(zweck, erteilt, ab, null, erfassung with { Art = erstes ? Erfassungsart.Ersterfassung : Erfassungsart.AenderungAbDatum });
        if (erstes)
        {
            _einwilligungen.Add(neu);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_einwilligungen, neu);
        }
    }

    private void AndereAdressenNichtBevorzugt(Guid ausser, DateOnly ab, Erfassung erfassung)
    {
        foreach (var a in Gueltige(_adressen, ab, erfassung.Am).Where(a => a.AdressId != ausser && a.Bevorzugt))
        {
            ZeitlicheHistorie.AendernAb(_adressen, new Postadresse(a.AdressId, a.Bereich, a.Zusatz, a.Strasse, a.Postleitzahl, a.Ort, false, ab, null,
                erfassung with { Art = Erfassungsart.AenderungAbDatum, Grund = "andere Adresse bevorzugt" }));
        }
    }

    private void AndereEMailsNichtBevorzugt(Guid ausser, DateOnly ab, Erfassung erfassung)
    {
        foreach (var e in Gueltige(_emails, ab, erfassung.Am).Where(e => e.AdressId != ausser && e.Bevorzugt))
        {
            ZeitlicheHistorie.AendernAb(_emails, new EMailAdresse(e.AdressId, e.Bereich, e.Adresse, false, ab, null,
                erfassung with { Art = Erfassungsart.AenderungAbDatum, Grund = "andere E-Mail-Adresse bevorzugt" }));
        }
    }

    private void AndereNummernNichtBevorzugt(Guid ausser, DateOnly ab, Erfassung erfassung)
    {
        foreach (var r in Gueltige(_rufnummern, ab, erfassung.Am).Where(r => r.NummerId != ausser && r.Bevorzugt))
        {
            ZeitlicheHistorie.AendernAb(_rufnummern, new Rufnummer(r.NummerId, r.Art, r.Nummer, r.Dienste, r.Einwilligung, false, ab, null,
                erfassung with { Art = Erfassungsart.AenderungAbDatum, Grund = "andere Rufnummer bevorzugt" }));
        }
    }

    private static void Pruefen<T>(IEnumerable<T> versionen, Func<T, bool> betrifft, string was)
    {
        if (!versionen.Any(betrifft))
        {
            throw new InvalidOperationException($"{was} nicht gefunden.");
        }
    }

    private static void Aufgeben<T>(List<T> versionen, Func<T, bool> betrifft, DateOnly ab, Erfassung erfassung, string was)
        where T : BitemporaleVersion<T>
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var eigene = versionen.Where(betrifft).ToList();
        var aktuell = eigene.AlsStichtag(ab, erfassung.Am) ?? eigene.AlsStichtag(ab.AddDays(-1), erfassung.Am)
            ?? throw new InvalidOperationException($"{was} gilt am {ab:dd.MM.yyyy} nicht.");
        ZeitlicheHistorie.BeendenAb(versionen, aktuell.KopieMit(ab, null, erfassung with { Art = Erfassungsart.Beendet }));
    }

    public void RisZuordnen(string? risId) => RisId = string.IsNullOrWhiteSpace(risId) ? null : risId.Trim();

    public void Deaktivieren() => Aktiv = false;

    public void Aktivieren() => Aktiv = true;

    public override string ToString() => Anzeigename;
}

/// <summary>Eine Version der Rollen einer Person (bitemporal).</summary>
public sealed class PersonRollen : BitemporaleVersion<PersonRollen>
{
    private PersonRollen()
    {
        RollenText = string.Empty;
    }

    public PersonRollen(IReadOnlyList<Rolle> rollen, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentNullException.ThrowIfNull(rollen);
        RollenText = string.Join(',', rollen.Distinct().Order());
    }

    /// <summary>Gespeichert als kommagetrennte technische Namen, z. B. "Vorstand,Mitglied".</summary>
    public string RollenText { get; private set; }

    public IReadOnlyList<Rolle> Rollen => RollenText.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(r => Enum.TryParse<Rolle>(r, out var x) ? x : (Rolle?)null).OfType<Rolle>().ToList();

    public override string Datenart => "Rollen";

    public override string Feld => "Rollen in der Fraktion";

    public override string Gegenstand => "rollen";

    public override string Wert => Rollen.Count == 0 ? "keine" : string.Join(", ", Rollen.Select(r => r.Anzeigename()));

    protected internal override PersonRollen KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Rollen, gueltigVon, gueltigBis, erfassung);
}
