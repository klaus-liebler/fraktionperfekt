using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Gremien;

public enum Gremienart
{
    Rat,
    Ausschuss,
    Beirat,
    Arbeitskreis,

    /// <summary>Die Fraktion selbst (Fraktionssitzungen).</summary>
    Fraktion,
}

/// <summary>
/// Rat, Ausschuss, Beirat oder Arbeitskreis. Gremien der Stadt kommen aus dem Ratsinformationssystem (OParl) und werden
/// hier nur gespiegelt (<see cref="RisId"/>); fraktionsinterne Gremien (Arbeitskreise) werden hier gepflegt.
/// </summary>
public sealed class Gremium
{
    private Gremium()
    {
        Name = string.Empty;
        Kurzname = string.Empty;
    }

    public Gremium(string name, string kurzname, Gremienart art, int sortierung, string? risId = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(kurzname);
        Id = id ?? Ids.Neu();
        Name = name.Trim();
        Kurzname = kurzname.Trim();
        Art = art;
        Sortierung = sortierung;
        RisId = risId;
        Aktiv = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Kurzname { get; private set; }

    public Gremienart Art { get; private set; }

    public int Sortierung { get; private set; }

    /// <summary>Kennung im Ratsinformationssystem; <c>null</c> = fraktionsintern gepflegt.</summary>
    public string? RisId { get; private set; }

    public bool Aktiv { get; private set; }

    public bool AusDemRis => RisId is not null;

    /// <summary>Abgleich mit dem RIS: Name und Art folgen der Quelle.</summary>
    public bool AusRisUebernehmen(string name, string kurzname, Gremienart art)
    {
        var geaendert = Name != name || Kurzname != kurzname || Art != art || !Aktiv;
        Name = name;
        Kurzname = kurzname;
        Art = art;
        Aktiv = true;
        return geaendert;
    }

    public void Deaktivieren() => Aktiv = false;

    public override string ToString() => Name;
}
