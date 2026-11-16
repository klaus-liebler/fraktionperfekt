using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Eine Version des Namens einer Person (bitemporal): Vorname, Namensvorsatz („von“), Nachname und Namenszusatz („jun.“) gelten gemeinsam
/// ab einem Datum. Der Titel ist ein eigenes Merkmal (nie Teil von Vor- oder Nachname). Laut Ausweis nur ein Name: Vorname leer mit
/// Kennzeichen <see cref="NurEinName"/>.
/// </summary>
public sealed class PersonName : BitemporaleVersion<PersonName>
{
    private PersonName()
    {
        Vorname = string.Empty;
        Nachname = string.Empty;
    }

    public PersonName(string vorname, string nachname, string? vorsatz, string? zusatz, bool nurEinName, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nachname);
        if (string.IsNullOrWhiteSpace(vorname) && !nurEinName)
        {
            throw new ArgumentException("Vorname fehlt. Hat die Person laut Ausweis nur einen Namen, bitte „nur ein Name“ ankreuzen.", nameof(vorname));
        }

        NurEinName = nurEinName;
        Vorname = nurEinName ? string.Empty : vorname.Trim();
        Nachname = nachname.Trim();
        Vorsatz = string.IsNullOrWhiteSpace(vorsatz) ? null : vorsatz.Trim();
        Zusatz = string.IsNullOrWhiteSpace(zusatz) ? null : zusatz.Trim();
    }

    /// <summary>Vorname; leer, wenn die Person laut Ausweis nur einen Namen hat.</summary>
    public string Vorname { get; private set; }

    /// <summary>Auch mehrteilig („Schulte Jochmaring“), ohne Vorsatz; danach wird sortiert.</summary>
    public string Nachname { get; private set; }

    /// <summary>Namensvorsatz („von“, „van der“, „de“).</summary>
    public string? Vorsatz { get; private set; }

    /// <summary>Namenszusatz („jun.“, „sen.“).</summary>
    public string? Zusatz { get; private set; }

    /// <summary>Laut Ausweis nur ein Name (Vorname bewusst leer).</summary>
    public bool NurEinName { get; private set; }

    public string Anzeige => Zusammensetzen(null, Vorname, Vorsatz, Nachname, Zusatz);

    /// <summary>„Dr. Anna von Berg jun.“ (leere Teile entfallen).</summary>
    public static string Zusammensetzen(string? titel, string? vorname, string? vorsatz, string nachname, string? zusatz)
        => string.Join(' ', new[] { titel, vorname, vorsatz, nachname, zusatz }.Where(t => !string.IsNullOrWhiteSpace(t)));

    /// <summary>
    /// Sortierschlüssel nach DIN 5007-2 (Variante 2, Namenslisten): Nachname ohne Vorsatz, dann Vorname, dann Vorsatz; Umlaute als
    /// ae/oe/ue, ß als ss, ohne Groß-/Kleinschreibung. „von Berg“ steht unter B.
    /// </summary>
    public static string Sortierschluessel(string? vorname, string? vorsatz, string nachname)
        => $"{Normalisieren(nachname)} {Normalisieren(vorname)} {Normalisieren(vorsatz)}".TrimEnd();

    /// <summary>Normalform nach DIN 5007-2 (dieselbe Regel wie die SQLite-Collation „DIN5007_2“): klein, ä = ae, ö = oe, ü = ue, ß = ss.</summary>
    public static string Normalisieren(string? t) => (t ?? string.Empty).ToLowerInvariant()
        .Replace("ä", "ae", StringComparison.Ordinal).Replace("ö", "oe", StringComparison.Ordinal).Replace("ü", "ue", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal);

    public override string Datenart => "Name";

    public override string Feld => "Name";

    public override string Gegenstand => "name";

    public override string Wert => IstEndemarke ? "(beendet)" : Anzeige;

    protected internal override PersonName KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Vorname, Nachname, Vorsatz, Zusatz, NurEinName, gueltigVon, gueltigBis, erfassung);

    public override string ToString() => Anzeige;
}

/// <summary>Zeitlich gültige Angaben zur Person außer Name und Rollen.</summary>
public enum PersonMerkmalArt
{
    /// <summary>„Herr“, „Frau“ oder leer (keine Angabe).</summary>
    Anrede,

    /// <summary>Freie Angabe (z. B. weiblich, männlich, divers) oder leer.</summary>
    Geschlecht,

    /// <summary>Funktion in der Fraktion (z. B. „Fraktionsvorsitzender“, „Schatzmeister“) – die Rechte ergeben sich aus den Rollen.</summary>
    Funktion,

    /// <summary>Akademischer Grad („Dr.“), nie Teil von Vor- oder Nachname; Amtsbezeichnungen wie „Prof.“ gehören nicht hierher.</summary>
    Titel,

    /// <summary>„aktiv“ oder „inaktiv“ in der Fraktion (ohne Angabe: aktiv).</summary>
    Aktiv,

    /// <summary>Persönliche Ansprache: „Du“ oder „Sie“ (ohne Angabe: Du).</summary>
    Anredeform,

    /// <summary>Persönliche Ansprache: „Locker“, „Neutral“, „Foermlich“ (ohne Angabe: Locker).</summary>
    Begruessungsstil,

    /// <summary>Persönliche Ansprache: Titel in der Sie-Form nennen, „ja“ oder „nein“ (ohne Angabe: ja).</summary>
    TitelInAnsprache,
}

/// <summary>Eine zeitlich gültige Angabe zur Person (bitemporal); leerer Inhalt = keine Angabe ab diesem Datum.</summary>
public sealed class PersonMerkmal : BitemporaleVersion<PersonMerkmal>
{
    private PersonMerkmal() => Inhalt = string.Empty;

    public PersonMerkmal(PersonMerkmalArt art, string? inhalt, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        Art = art;
        Inhalt = inhalt?.Trim() ?? string.Empty;
    }

    public PersonMerkmalArt Art { get; private set; }

    public string Inhalt { get; private set; }

    public string? WertOderNull => Inhalt.Length == 0 ? null : Inhalt;

    public override string Datenart => Art switch
    {
        PersonMerkmalArt.Funktion => "Funktion",
        PersonMerkmalArt.Titel => "Name",
        PersonMerkmalArt.Anredeform or PersonMerkmalArt.Begruessungsstil or PersonMerkmalArt.TitelInAnsprache => "Ansprache",
        _ => "Person",
    };

    public override string Feld => Art switch
    {
        PersonMerkmalArt.Anrede => "Anrede",
        PersonMerkmalArt.Geschlecht => "Geschlecht",
        PersonMerkmalArt.Titel => "Titel",
        PersonMerkmalArt.Aktiv => "Aktiv in der Fraktion",
        PersonMerkmalArt.Anredeform => "Du oder Sie",
        PersonMerkmalArt.Begruessungsstil => "Stil der Begrüßung",
        PersonMerkmalArt.TitelInAnsprache => "Titel in der Ansprache",
        _ => "Funktion in der Fraktion",
    };

    /// <summary>Bezeichnung einer Merkmalsart für Formulare.</summary>
    public static string Bezeichnung(PersonMerkmalArt art) => new PersonMerkmal(art, null, DateOnly.MinValue, null, Erfassung.Erst(DateTimeOffset.MinValue)).Feld;

    public override string Gegenstand => Art.ToString();

    public override string Wert => IstEndemarke ? "(beendet)" : Inhalt.Length == 0 ? "(keine Angabe)" : Inhalt;

    protected internal override PersonMerkmal KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Art, Inhalt, gueltigVon, gueltigBis, erfassung);
}

/// <summary>Alle Angaben einer Person, wie sie an einem Tag galten, nach einem Wissensstand (Kontakte: bevorzugte zuerst).</summary>
public sealed record PersonenStand(
    DateOnly GueltigAm,
    DateTimeOffset BekanntAm,
    PersonName? Name,
    string? Titel,
    string? Anrede,
    string? Geschlecht,
    string? Funktion,
    IReadOnlyList<Rolle> Rollen,
    IReadOnlyList<Postadresse> Adressen,
    IReadOnlyList<EMailAdresse> EMails,
    IReadOnlyList<Rufnummer> Rufnummern,
    IReadOnlyList<Einwilligung> Einwilligungen)
{
    public string Anzeige => Name is null ? "(noch nicht erfasst)" : PersonName.Zusammensetzen(Titel, Name.Vorname, Name.Vorsatz, Name.Nachname, Name.Zusatz);

    public bool Aktiv { get; init; } = true;

    public Anredeform Anredeform { get; init; }

    public Begruessungsstil Begruessungsstil { get; init; }

    public bool TitelInAnsprache { get; init; } = true;

    /// <summary>Bevorzugte E-Mail-Adresse, sonst die erste.</summary>
    public string? EMail => EMails.FirstOrDefault()?.Adresse;

    /// <summary>Bevorzugte Mobilnummer, sonst die erste mobile.</summary>
    public string? Mobil => Rufnummern.FirstOrDefault(r => r.Art == RufnummerArt.Mobil)?.Nummer;

    public bool Erteilt(Einwilligungszweck zweck) => Einwilligungen.Any(e => e.Zweck == zweck && e.Erteilt);
}
