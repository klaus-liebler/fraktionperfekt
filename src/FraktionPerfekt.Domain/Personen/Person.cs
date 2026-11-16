using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Eine Person der Fraktion (Ratsmitglied, sachkundige:r Bürger:in, Helfer:in) – nach dem Vorbild von VibeVHS: Die Person ist eine
/// Identität (Id); Name (Vorname, Nachname, Titel), Anrede, Geschlecht, Kontaktdaten, Funktion und Rollen gelten zeitlich und sind
/// bitemporal versioniert (gültig ab/bis und erfasst/ersetzt). Unterschieden wird „Änderung ab Datum“ (z. B. Heirat, neue Funktion)
/// von „Korrektur“ (Tippfehler: die betroffene Version wird berichtigt, die alte bleibt als ersetzt sichtbar). Maßgeblich ist
/// <see cref="StandAm"/>. <see cref="Vorname"/>, <see cref="Nachname"/>, <see cref="Titel"/>, <see cref="Anrede"/>, <see cref="EMail"/>,
/// <see cref="Mobil"/>, <see cref="Aktiv"/> und die Ansprache sind der Stand heute: nicht gespeichert, sondern beim Laden aus den
/// Versionen berechnet (<see cref="StandHeuteBerechnen"/>, Datenbank-Regel 1 „keine Kopien“).
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
    /// <param name="nurEinName">Laut Ausweis nur ein Name: Vorname bleibt leer (bewusst, mit Kennzeichen).</param>
    public Person(string vorname, string nachname, string? eMail, string? benutzerkennung, DateTimeOffset angelegtAm, string? risId = null,
        Guid? id = null, string? titel = null, string? anrede = null, DateOnly? gueltigAb = null, string? funktion = null, string? von = null,
        string? vorsatz = null, string? zusatz = null, bool nurEinName = false)
    {
        Id = id ?? Ids.Neu();
        Vorname = string.Empty;
        Nachname = string.Empty;
        Benutzerkennung = string.IsNullOrWhiteSpace(benutzerkennung) ? null : benutzerkennung.Trim().ToLowerInvariant();
        RisId = risId;
        AngelegtAm = angelegtAm;
        var ab = gueltigAb ?? DateOnly.FromDateTime(angelegtAm.UtcDateTime);
        var erst = Erfassung.Erst(angelegtAm, von);
        _namen.Add(new PersonName(vorname, nachname, vorsatz, zusatz, nurEinName, ab, null, erst));
        _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.Anrede, AnredePruefen(anrede), ab, null, erst));
        if (!string.IsNullOrWhiteSpace(titel))
        {
            _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.Titel, titel, ab, null, erst));
        }

        if (!string.IsNullOrWhiteSpace(eMail))
        {
            _emails.Add(new EMailAdresse(Ids.Neu(), Kontaktbereich.Privat, eMail, true, ab, null, erst));
        }

        if (!string.IsNullOrWhiteSpace(funktion))
        {
            _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.Funktion, funktion, ab, null, erst));
        }

        StandHeuteBerechnen(angelegtAm);
    }

    public Guid Id { get; private set; }

    /// <summary>Vorname (Stand heute); leer bei „nur ein Name laut Ausweis“.</summary>
    public string Vorname { get; private set; }

    /// <summary>Nachname ohne Vorsatz, auch mehrteilig („Große Maestrup“, „Schulte Jochmaring“); danach wird sortiert (Stand heute).</summary>
    public string Nachname { get; private set; }

    /// <summary>Namensvorsatz („von“, „van der“) – bei der Sortierung nach DIN 5007-2 hinten (Stand heute).</summary>
    public string? Vorsatz { get; private set; }

    /// <summary>Namenszusatz („jun.“) (Stand heute).</summary>
    public string? Zusatz { get; private set; }

    /// <summary>Nachname mit Vorsatz für die Anrede („Herr von Berg“).</summary>
    public string NachnameMitVorsatz => Vorsatz is null ? Nachname : $"{Vorsatz} {Nachname}";

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

    /// <summary>Aktiv in der Fraktion (Stand heute; zeitliches Merkmal, Standard: aktiv).</summary>
    public bool Aktiv { get; private set; } = true;

    public DateTimeOffset AngelegtAm { get; private set; }

    /// <summary>„Herr“, „Frau“ oder <c>null</c> (keine Angabe/divers).</summary>
    public string? Anrede { get; private set; }

    /// <summary>Persönliche Ansprache: Du oder Sie (Merkmal, Standard: Du).</summary>
    public Anredeform Anredeform { get; private set; }

    /// <summary>Persönliche Ansprache: Stil der Begrüßung (Merkmal, Standard: locker).</summary>
    public Begruessungsstil Begruessungsstil { get; private set; }

    /// <summary>Persönliche Ansprache: akademischen Grad in der Sie-Form nennen (Merkmal, Standard: ja).</summary>
    public bool TitelInAnsprache { get; private set; } = true;

    /// <summary>
    /// Wie die Person angesprochen werden möchte (eigene Angabe). Anrede, Du/Sie, Stil und „Titel nennen“ sind zeitliche Merkmale und
    /// werden ab heute geändert; die bisherigen bleiben in der Historie. Unveränderte Angaben erzeugen keine neue Version.
    /// </summary>
    public void AnspracheEinstellen(string? anrede, Anredeform form, Begruessungsstil stil, bool titelInAnsprache, DateTimeOffset? jetzt = null)
    {
        var am = jetzt ?? DateTimeOffset.UtcNow;
        var heute = DateOnly.FromDateTime(am.UtcDateTime);
        StandHeuteBerechnen(am);
        var erfassung = Erfassung.Aenderung(am, "Eigene Angabe in den Einstellungen");
        if (AnredePruefen(anrede) != Anrede)
        {
            AngabeAendern(PersonMerkmalArt.Anrede, anrede, heute, erfassung, false);
        }

        if (form != Anredeform)
        {
            AngabeAendern(PersonMerkmalArt.Anredeform, form.ToString(), heute, erfassung, false);
        }

        if (stil != Begruessungsstil)
        {
            AngabeAendern(PersonMerkmalArt.Begruessungsstil, stil.ToString(), heute, erfassung, false);
        }

        if (titelInAnsprache != TitelInAnsprache)
        {
            AngabeAendern(PersonMerkmalArt.TitelInAnsprache, titelInAnsprache ? "ja" : "nein", heute, erfassung, false);
        }
    }

    private static string? AnredePruefen(string? anrede) => anrede?.Trim() switch
    {
        "Herr" => "Herr",
        "Frau" => "Frau",
        null or "" => null,
        var a => throw new ArgumentException($"Unbekannte Anrede „{a}“ (Herr, Frau oder keine Angabe).", nameof(anrede)),
    };

    /// <summary>„Dr. Anna von Berg jun.“</summary>
    public string Anzeigename => PersonName.Zusammensetzen(Titel, Vorname, Vorsatz, Nachname, Zusatz);

    /// <summary>„Berg, Anna von“ – Anzeige in sortierten Listen.</summary>
    public string Sortiername => string.IsNullOrEmpty(Vorname) && Vorsatz is null ? Nachname
        : $"{Nachname}, {string.Join(' ', new[] { Vorname, Vorsatz }.Where(t => !string.IsNullOrEmpty(t)))}";

    /// <summary>Sortierschlüssel nach DIN 5007-2 (Namenslisten): Nachname ohne Vorsatz, dann Vorname, dann Vorsatz; ä = ae, ß = ss.</summary>
    public string Sortierschluessel => PersonName.Sortierschluessel(Vorname, Vorsatz, Nachname);

    /// <summary>Initialen für Avatare: erster Buchstabe des Vornamens und des Nachnamens („Große Maestrup“ → „JG“).</summary>
    public string Initialen => string.IsNullOrEmpty(Vorname)
        ? Nachname[..Math.Min(2, Nachname.Length)].ToUpperInvariant()
        : $"{char.ToUpperInvariant(Vorname[0])}{char.ToUpperInvariant(Nachname[0])}";

    /// <summary>Titel ab heute (Kurzform von <see cref="AngabeAendern"/>).</summary>
    public void TitelSetzen(string? titel, DateTimeOffset? jetzt = null)
    {
        var am = jetzt ?? DateTimeOffset.UtcNow;
        AngabeAendern(PersonMerkmalArt.Titel, titel, DateOnly.FromDateTime(am.UtcDateTime), Erfassung.Aenderung(am, "Titel"), false);
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
        return new PersonenStand(gueltigAm, bekanntAm, _namen.AlsStichtag(gueltigAm, bekanntAm), Merkmal(PersonMerkmalArt.Titel), Merkmal(PersonMerkmalArt.Anrede),
            Merkmal(PersonMerkmalArt.Geschlecht), Merkmal(PersonMerkmalArt.Funktion), RollenAm(gueltigAm, bekanntAm), adressen, emails, nummern,
            einwilligungen)
        {
            Aktiv = Merkmal(PersonMerkmalArt.Aktiv) != "inaktiv",
            Anredeform = Enum.TryParse<Anredeform>(Merkmal(PersonMerkmalArt.Anredeform), out var f) ? f : Anredeform.Du,
            Begruessungsstil = Enum.TryParse<Begruessungsstil>(Merkmal(PersonMerkmalArt.Begruessungsstil), out var s) ? s : Begruessungsstil.Locker,
            TitelInAnsprache = Merkmal(PersonMerkmalArt.TitelInAnsprache) != "nein",
        };
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
    {
        var name = _namen.AlsStichtag(tag, bekanntAm) ?? _namen.AktuellerVerlauf().FirstOrDefault();
        var titel = _merkmale.Where(m => m.Art == PersonMerkmalArt.Titel).AlsStichtag(tag, bekanntAm)?.WertOderNull;
        return name is null ? Anzeigename : PersonName.Zusammensetzen(titel, name.Vorname, name.Vorsatz, name.Nachname, name.Zusatz);
    }

    // ---------- Änderungen ----------

    /// <summary>
    /// Namensänderung ab einem Datum (z. B. Heirat). Ältere Vorgänge behalten den damaligen Namen. Der Titel ist ein eigenes Merkmal;
    /// weicht er ab, wird er im selben Vorgang mit geändert.
    /// </summary>
    public void NameAendernAb(string vorname, string nachname, string? titel, DateOnly ab, Erfassung erfassung, string? vorsatz = null, string? zusatz = null,
        bool nurEinName = false)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var bisher = _namen.AlsStichtag(ab, erfassung.Am);
        if (bisher is null || bisher.Vorname != (vorname ?? string.Empty).Trim() || bisher.Nachname != nachname.Trim() || bisher.Vorsatz != Leer(vorsatz)
            || bisher.Zusatz != Leer(zusatz) || bisher.NurEinName != nurEinName)
        {
            ZeitlicheHistorie.AendernAb(_namen, new PersonName(vorname ?? string.Empty, nachname, vorsatz, zusatz, nurEinName, ab, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        }

        if (Leer(titel) != _merkmale.Where(m => m.Art == PersonMerkmalArt.Titel).AlsStichtag(ab, erfassung.Am)?.WertOderNull)
        {
            AngabeAendern(PersonMerkmalArt.Titel, titel, ab, erfassung, false);
        }

        StandHeuteBerechnen(erfassung.Am);
    }

    /// <summary>Korrektur eines falsch erfassten Namens (und Titels) für den gesamten Zeitraum der am <paramref name="betroffenAm"/> gültigen Version.</summary>
    public void NameKorrigieren(string vorname, string nachname, string? titel, DateOnly betroffenAm, Erfassung erfassung, string? vorsatz = null, string? zusatz = null,
        bool nurEinName = false)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var bisher = _namen.AlsStichtag(betroffenAm, erfassung.Am);
        if (bisher is null || bisher.Vorname != (vorname ?? string.Empty).Trim() || bisher.Nachname != nachname.Trim() || bisher.Vorsatz != Leer(vorsatz)
            || bisher.Zusatz != Leer(zusatz) || bisher.NurEinName != nurEinName)
        {
            ZeitlicheHistorie.Korrigieren(_namen, betroffenAm,
                (gv, gb) => new PersonName(vorname ?? string.Empty, nachname, vorsatz, zusatz, nurEinName, gv, gb, erfassung with { Art = Erfassungsart.Korrektur }), erfassung.Am);
        }

        if (Leer(titel) != _merkmale.Where(m => m.Art == PersonMerkmalArt.Titel).AlsStichtag(betroffenAm, erfassung.Am)?.WertOderNull)
        {
            AngabeAendern(PersonMerkmalArt.Titel, titel, betroffenAm, erfassung, true);
        }

        StandHeuteBerechnen(erfassung.Am);
    }

    private static string? Leer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Titel, Anrede, Geschlecht, Funktion, Aktiv, Ansprache: Änderung ab Datum oder Korrektur der am Datum gültigen Angabe (leer = keine Angabe).</summary>
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

        StandHeuteBerechnen(erfassung.Am);
    }

    /// <summary>
    /// Stand heute (Vorname, Nachname, Titel, Anrede, E-Mail, Mobil, Aktiv, Ansprache) aus den Versionen berechnen. Wird nicht gespeichert:
    /// der Speicher ruft es beim Laden mit der aktuellen Uhrzeit auf, Änderungen rufen es nach jeder Änderung auf.
    /// </summary>
    public void StandHeuteBerechnen(DateTimeOffset jetzt)
    {
        var stand = StandAm(DateOnly.FromDateTime(jetzt.UtcDateTime), jetzt);
        var name = stand.Name ?? _namen.AktuellerVerlauf().FirstOrDefault();
        if (name is not null)
        {
            (Vorname, Nachname, Vorsatz, Zusatz) = (name.Vorname, name.Nachname, name.Vorsatz, name.Zusatz);
        }

        (Titel, Anrede, EMail, Mobil, Aktiv) = (stand.Titel, stand.Anrede, stand.EMail, stand.Mobil, stand.Aktiv);
        (Anredeform, Begruessungsstil, TitelInAnsprache) = (stand.Anredeform, stand.Begruessungsstil, stand.TitelInAnsprache);
    }

    /// <summary>
    /// Korrektur eines Gültigkeitszeitraums (z. B. „Umzugsdatum korrigieren“): Die am <paramref name="betroffenAm"/> gültige Version des
    /// Gegenstands wird ersetzt durch denselben Wert mit neuem Beginn bzw. Ende; die alte bleibt mit früherem Wissensstand abrufbar.
    /// </summary>
    public void ZeitraumKorrigieren(Kontaktart art, Guid id, DateOnly betroffenAm, DateOnly neuVon, DateOnly? neuBis, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var korrektur = erfassung with { Art = Erfassungsart.Korrektur };
        switch (art)
        {
            case Kontaktart.Adresse:
                ZeitlicheHistorie.ZeitraumKorrigieren(_adressen, betroffenAm, neuVon, neuBis, korrektur, a => a.AdressId == id);
                break;
            case Kontaktart.EMail:
                ZeitlicheHistorie.ZeitraumKorrigieren(_emails, betroffenAm, neuVon, neuBis, korrektur, e => e.AdressId == id);
                break;
            default:
                ZeitlicheHistorie.ZeitraumKorrigieren(_rufnummern, betroffenAm, neuVon, neuBis, korrektur, r => r.NummerId == id);
                break;
        }

        StandHeuteBerechnen(erfassung.Am);
    }

    /// <summary>
    /// Umzugsdatum korrigieren: Die alte Adresse endet, die neue beginnt am <paramref name="neuesDatum"/> statt am bisher erfassten Tag
    /// (Beispiel Frau Muster in docs/datenbank-einleitung.md).
    /// </summary>
    public void UmzugsdatumKorrigieren(Guid alteAdressId, Guid neueAdressId, DateOnly neuesDatum, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var neu = _adressen.Where(a => a.AdressId == neueAdressId && a.IstAktuellerWissensstand && !a.IstEndemarke).OrderBy(a => a.GueltigVon).FirstOrDefault()
            ?? throw new InvalidOperationException("Neue Adresse nicht gefunden.");
        var bisherigesDatum = neu.GueltigVon;
        var korrektur = erfassung with { Art = Erfassungsart.Korrektur };
        // alte Adresse: Version bis zum Umzug und Endemarke ab Umzug
        var alt = _adressen.Single(a => a.AdressId == alteAdressId && a.IstAktuellerWissensstand && !a.IstEndemarke && a.GueltigBis == bisherigesDatum);
        var ende = _adressen.SingleOrDefault(a => a.AdressId == alteAdressId && a.IstAktuellerWissensstand && a.IstEndemarke && a.GueltigVon == bisherigesDatum);
        ZeitlicheHistorie.ZeitraumKorrigieren(_adressen, alt.GueltigVon, alt.GueltigVon, neuesDatum, korrektur, a => a.AdressId == alteAdressId && !a.IstEndemarke);
        if (ende is not null)
        {
            ZeitlicheHistorie.ZeitraumKorrigieren(_adressen, ende.GueltigVon, neuesDatum, ende.GueltigBis, korrektur with { Art = Erfassungsart.Beendet },
                a => a.AdressId == alteAdressId && a.IstEndemarke);
        }

        ZeitlicheHistorie.ZeitraumKorrigieren(_adressen, bisherigesDatum, neuesDatum, neu.GueltigBis, korrektur, a => a.AdressId == neueAdressId);
        StandHeuteBerechnen(erfassung.Am);
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

        StandHeuteBerechnen(erfassung.Am);
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

        StandHeuteBerechnen(erfassung.Am);
    }

    public void EMailAufgebenAb(Guid adressId, DateOnly ab, Erfassung erfassung)
    {
        Aufgeben(_emails, e => e.AdressId == adressId, ab, erfassung, "E-Mail-Adresse");
        StandHeuteBerechnen(erfassung.Am);
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

        StandHeuteBerechnen(erfassung.Am);
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

        StandHeuteBerechnen(erfassung.Am);
    }

    public void RufnummerAbmeldenAb(Guid nummerId, DateOnly ab, Erfassung erfassung)
    {
        Aufgeben(_rufnummern, r => r.NummerId == nummerId, ab, erfassung, "Rufnummer");
        StandHeuteBerechnen(erfassung.Am);
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

    /// <summary>Ab Datum nicht mehr aktiv in der Fraktion (zeitliches Merkmal; die Historie bleibt).</summary>
    public void Deaktivieren(DateOnly ab, Erfassung erfassung) => AngabeAendern(PersonMerkmalArt.Aktiv, "inaktiv", ab, erfassung, false);

    /// <summary>Ab Datum wieder aktiv.</summary>
    public void Aktivieren(DateOnly ab, Erfassung erfassung) => AngabeAendern(PersonMerkmalArt.Aktiv, "aktiv", ab, erfassung, false);

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
