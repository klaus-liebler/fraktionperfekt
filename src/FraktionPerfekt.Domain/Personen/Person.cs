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
            _merkmale.Add(new PersonMerkmal(PersonMerkmalArt.EMail, eMail, ab, null, erst));
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

    /// <summary>Mobilnummer für spätere SMS/Push-Zuordnung; optional.</summary>
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

    /// <summary>Alle Versionen aller Angaben (für die Datenhistorie).</summary>
    public IEnumerable<IHistorienwert> AlleVersionen => _namen.Cast<IHistorienwert>().Concat(_merkmale).Concat(_rollen);

    // ---------- Stichtag ----------

    /// <summary>Alle Angaben, wie sie am <paramref name="gueltigAm"/> galten, nach dem Wissensstand vom <paramref name="bekanntAm"/>.</summary>
    public PersonenStand StandAm(DateOnly gueltigAm, DateTimeOffset bekanntAm)
    {
        string? Merkmal(PersonMerkmalArt art) => _merkmale.Where(m => m.Art == art).AlsStichtag(gueltigAm, bekanntAm)?.WertOderNull;
        return new PersonenStand(gueltigAm, bekanntAm, _namen.AlsStichtag(gueltigAm, bekanntAm), Merkmal(PersonMerkmalArt.Anrede),
            Merkmal(PersonMerkmalArt.Geschlecht), Merkmal(PersonMerkmalArt.EMail), Merkmal(PersonMerkmalArt.Mobil), Merkmal(PersonMerkmalArt.Funktion),
            RollenAm(gueltigAm, bekanntAm));
    }

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

    /// <summary>Anrede, Geschlecht, E-Mail, Mobil oder Funktion: Änderung ab Datum oder Korrektur der am Datum gültigen Angabe (leer = keine Angabe).</summary>
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

    /// <summary>Kontaktdaten ab heute (Kurzform von <see cref="AngabeAendern"/>); unveränderte Angaben bleiben ohne neue Version.</summary>
    public void KontaktAendern(string? eMail, string? mobil, DateTimeOffset? jetzt = null)
    {
        var am = jetzt ?? DateTimeOffset.UtcNow;
        var heute = DateOnly.FromDateTime(am.UtcDateTime);
        if ((string.IsNullOrWhiteSpace(eMail) ? null : eMail.Trim()) != EMail)
        {
            AngabeAendern(PersonMerkmalArt.EMail, eMail, heute, Erfassung.Aenderung(am, "Kontaktdaten"), false);
        }

        if ((string.IsNullOrWhiteSpace(mobil) ? null : mobil.Trim()) != Mobil)
        {
            AngabeAendern(PersonMerkmalArt.Mobil, mobil, heute, Erfassung.Aenderung(am, "Kontaktdaten"), false);
        }
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
