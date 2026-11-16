using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>Eine Version des Namens einer Person (bitemporal): Vorname, Nachname und akademischer Grad gelten gemeinsam ab einem Datum.</summary>
public sealed class PersonName : BitemporaleVersion<PersonName>
{
    private PersonName()
    {
        Vorname = string.Empty;
        Nachname = string.Empty;
    }

    public PersonName(string vorname, string nachname, string? titel, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vorname);
        ArgumentException.ThrowIfNullOrWhiteSpace(nachname);
        Vorname = vorname.Trim();
        Nachname = nachname.Trim();
        Titel = string.IsNullOrWhiteSpace(titel) ? null : titel.Trim();
    }

    public string Vorname { get; private set; }

    /// <summary>Auch mehrteilig („Schulte Jochmaring“); danach wird sortiert.</summary>
    public string Nachname { get; private set; }

    /// <summary>Akademischer Grad („Dr.“), kein Amt.</summary>
    public string? Titel { get; private set; }

    public string Anzeige => Titel is null ? $"{Vorname} {Nachname}" : $"{Titel} {Vorname} {Nachname}";

    public override string Datenart => "Name";

    public override string Feld => "Name";

    public override string Gegenstand => "name";

    public override string Wert => IstEndemarke ? "(beendet)" : Anzeige;

    protected internal override PersonName KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Vorname, Nachname, Titel, gueltigVon, gueltigBis, erfassung);

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

    public override string Datenart => Art == PersonMerkmalArt.Funktion ? "Funktion" : "Person";

    public override string Feld => Art switch
    {
        PersonMerkmalArt.Anrede => "Anrede",
        PersonMerkmalArt.Geschlecht => "Geschlecht",
        _ => "Funktion in der Fraktion",
    };

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
    string? Anrede,
    string? Geschlecht,
    string? Funktion,
    IReadOnlyList<Rolle> Rollen,
    IReadOnlyList<Postadresse> Adressen,
    IReadOnlyList<EMailAdresse> EMails,
    IReadOnlyList<Rufnummer> Rufnummern,
    IReadOnlyList<Einwilligung> Einwilligungen)
{
    public string Anzeige => Name?.Anzeige ?? "(noch nicht erfasst)";

    /// <summary>Bevorzugte E-Mail-Adresse, sonst die erste.</summary>
    public string? EMail => EMails.FirstOrDefault()?.Adresse;

    /// <summary>Bevorzugte Mobilnummer, sonst die erste mobile.</summary>
    public string? Mobil => Rufnummern.FirstOrDefault(r => r.Art == RufnummerArt.Mobil)?.Nummer;

    public bool Erteilt(Einwilligungszweck zweck) => Einwilligungen.Any(e => e.Zweck == zweck && e.Erteilt);
}
