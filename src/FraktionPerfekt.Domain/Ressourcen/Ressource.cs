using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Ressourcen;

public enum Ressourcenart
{
    Raum,
    Technik,
    Material,
    Fahrzeug,
}

/// <summary>Buchbare Ressource: Fraktionsraum, Beamer, Infostand, Lastenrad, Dienstwagen, Pavillon …</summary>
public sealed class Ressource
{
    private Ressource()
    {
        Name = string.Empty;
    }

    public Ressource(string name, Ressourcenart art, string? beschreibung, bool freigabeNoetig)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        Name = name.Trim();
        Art = art;
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        FreigabeNoetig = freigabeNoetig;
        Aktiv = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Ressourcenart Art { get; private set; }

    public string? Beschreibung { get; private set; }

    /// <summary>Buchungen brauchen eine Freigabe durch die Geschäftsführung (z. B. Dienstwagen).</summary>
    public bool FreigabeNoetig { get; private set; }

    public bool Aktiv { get; private set; }

    public void Aendern(string name, string? beschreibung, bool freigabeNoetig, bool aktiv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        FreigabeNoetig = freigabeNoetig;
        Aktiv = aktiv;
    }
}

public enum Buchungsstatus
{
    /// <summary>Wartet auf Freigabe; blockiert die Ressource bereits.</summary>
    Angefragt,
    Bestaetigt,
    Abgelehnt,
    Storniert,
}

/// <summary>Buchung einer Ressource für einen Zeitraum (Wanduhrzeit), optional an einem Termin.</summary>
public sealed class Buchung
{
    private Buchung()
    {
        Zweck = string.Empty;
    }

    public Buchung(Guid ressourceId, Guid personId, DateTime von, DateTime bis, string zweck, Guid? terminId, bool freigabeNoetig, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zweck);
        if (bis <= von)
        {
            throw new ArgumentException("Das Ende muss nach dem Beginn liegen.", nameof(bis));
        }

        Id = Ids.Neu();
        RessourceId = ressourceId;
        PersonId = personId;
        Von = von;
        Bis = bis;
        Zweck = zweck.Trim();
        TerminId = terminId;
        ErstelltAm = jetzt;
        Status = freigabeNoetig ? Buchungsstatus.Angefragt : Buchungsstatus.Bestaetigt;
    }

    public Guid Id { get; private set; }

    public Guid RessourceId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTime Von { get; private set; }

    public DateTime Bis { get; private set; }

    public string Zweck { get; private set; }

    public Guid? TerminId { get; private set; }

    public Buchungsstatus Status { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public Guid? EntschiedenVonPersonId { get; private set; }

    public string? Vermerk { get; private set; }

    /// <summary>Angefragte und bestätigte Buchungen belegen die Ressource.</summary>
    public bool Blockiert => Status is Buchungsstatus.Angefragt or Buchungsstatus.Bestaetigt;

    public bool Ueberschneidet(DateTime von, DateTime bis) => Von < bis && von < Bis;

    public void Entscheiden(bool bestaetigen, Guid personId, string? vermerk)
    {
        if (Status != Buchungsstatus.Angefragt)
        {
            throw new InvalidOperationException("Über die Buchung ist bereits entschieden.");
        }

        Status = bestaetigen ? Buchungsstatus.Bestaetigt : Buchungsstatus.Abgelehnt;
        EntschiedenVonPersonId = personId;
        Vermerk = string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim();
    }

    public void Stornieren(string? vermerk)
    {
        if (!Blockiert)
        {
            throw new InvalidOperationException("Die Buchung ist nicht mehr aktiv.");
        }

        Status = Buchungsstatus.Storniert;
        Vermerk = string.IsNullOrWhiteSpace(vermerk) ? Vermerk : vermerk.Trim();
    }

    /// <summary>Aktive Buchungen derselben Ressource, die sich mit dem Zeitraum überschneiden.</summary>
    public static IReadOnlyList<Buchung> Konflikte(Guid ressourceId, DateTime von, DateTime bis, IEnumerable<Buchung> bestehende, Guid? ausser = null)
        => bestehende.Where(b => b.RessourceId == ressourceId && b.Blockiert && b.Id != ausser && b.Ueberschneidet(von, bis)).OrderBy(b => b.Von).ToList();
}
