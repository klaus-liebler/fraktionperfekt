using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Gremien;

public enum Gremienart
{
    Rat,
    Ausschuss,
    Beirat,
    Arbeitskreis,

    /// <summary>Interkommunales Gremium (z. B. Zweckverbandsversammlung), nicht vom Rat der Stadt allein besetzt.</summary>
    Zweckverband,

    /// <summary>Die Fraktion selbst (Fraktionssitzungen).</summary>
    Fraktion,
}

/// <summary>
/// Rat, Ausschuss, Beirat oder Arbeitskreis. Gremien der Stadt kommen aus dem Ratsinformationssystem (OParl) und werden
/// hier nur gespiegelt (<see cref="RisId"/>); fraktionsinterne Gremien (Arbeitskreise) werden hier gepflegt.
/// Name, Kurzname und „aktiv“ gelten zeitlich (bitemporal, <see cref="GremiumVersion"/>): Alte Protokolle zeigen den damaligen Namen.
/// <see cref="Name"/>, <see cref="Kurzname"/> und <see cref="Aktiv"/> sind der Stand heute (beim Laden berechnet, nicht gespeichert).
/// </summary>
public sealed class Gremium
{
    private readonly List<GremiumVersion> _versionen = [];

    private Gremium()
    {
        Name = string.Empty;
        Kurzname = string.Empty;
    }

    public Gremium(string name, string kurzname, Gremienart art, int sortierung, string? risId = null, Guid? id = null, DateOnly? gueltigAb = null,
        Erfassung? erfassung = null)
    {
        Id = id ?? Ids.Neu();
        Art = art;
        Sortierung = sortierung;
        RisId = risId;
        var erst = erfassung ?? Erfassung.Erst(DateTimeOffset.UtcNow);
        _versionen.Add(new GremiumVersion(name, kurzname, true, gueltigAb ?? DateOnly.MinValue, null, erst with { Art = Erfassungsart.Ersterfassung }));
        (Name, Kurzname, Aktiv) = (name.Trim(), kurzname.Trim(), true);
    }

    public Guid Id { get; private set; }

    /// <summary>Name (Stand heute).</summary>
    public string Name { get; private set; }

    /// <summary>Kurzname (Stand heute).</summary>
    public string Kurzname { get; private set; }

    public IReadOnlyCollection<GremiumVersion> Versionen => _versionen;

    /// <summary>Name, Kurzname und „aktiv“ am Stichtag nach Wissensstand <paramref name="bekanntAm"/>; vor der ersten Version die früheste.</summary>
    public GremiumVersion? StandAm(DateOnly tag, DateTimeOffset bekanntAm)
        => _versionen.AlsStichtag(tag, bekanntAm) ?? _versionen.AktuellerVerlauf().FirstOrDefault();

    /// <summary>Stand heute aus den Versionen berechnen (beim Laden und nach Änderungen).</summary>
    public void StandHeuteBerechnen(DateTimeOffset jetzt)
    {
        if (StandAm(DateOnly.FromDateTime(jetzt.UtcDateTime), jetzt) is { } v)
        {
            (Name, Kurzname, Aktiv) = (v.Name, v.Kurzname, v.Aktiv);
        }
    }

    public Gremienart Art { get; private set; }

    public int Sortierung { get; private set; }

    /// <summary>Kennung im Ratsinformationssystem; <c>null</c> = fraktionsintern gepflegt.</summary>
    public string? RisId { get; private set; }

    /// <summary>Aktiv (Stand heute).</summary>
    public bool Aktiv { get; private set; } = true;

    public bool AusDemRis => RisId is not null;

    /// <summary>Abgleich mit dem RIS: Name, Kurzname und „aktiv“ ändern sich ab <paramref name="ab"/> (frühere Sitzungen behalten den Namen), die Art folgt der Quelle.</summary>
    public bool AusRisUebernehmen(string name, string kurzname, Gremienart art, DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var bisher = StandAm(ab, erfassung.Am);
        var geaendert = Art != art;
        Art = art;
        if (bisher is null || bisher.Name != name.Trim() || bisher.Kurzname != kurzname.Trim() || !bisher.Aktiv)
        {
            ZeitlicheHistorie.AendernAb(_versionen, new GremiumVersion(name, kurzname, true, ab, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
            geaendert = true;
        }

        StandHeuteBerechnen(erfassung.Am);
        return geaendert;
    }

    /// <summary>Gremium ab Datum nicht mehr aktiv (die Historie bleibt).</summary>
    public void Deaktivieren(DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var v = StandAm(ab, erfassung.Am) ?? throw new InvalidOperationException("Gremium ohne Version.");
        ZeitlicheHistorie.AendernAb(_versionen, new GremiumVersion(v.Name, v.Kurzname, false, ab, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
        StandHeuteBerechnen(erfassung.Am);
    }

    public override string ToString() => Name;
}

/// <summary>Eine Version von Name, Kurzname und „aktiv“ eines Gremiums (bitemporal).</summary>
public sealed class GremiumVersion : BitemporaleVersion<GremiumVersion>
{
    private GremiumVersion()
    {
        Name = string.Empty;
        Kurzname = string.Empty;
    }

    public GremiumVersion(string name, string kurzname, bool aktiv, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(kurzname);
        Name = name.Trim();
        Kurzname = kurzname.Trim();
        Aktiv = aktiv;
    }

    public string Name { get; private set; }

    public string Kurzname { get; private set; }

    public bool Aktiv { get; private set; }

    public override string Datenart => "Gremium";

    public override string Feld => "Name und Kurzname";

    public override string Gegenstand => "gremium";

    public override string Wert => $"{Name} ({Kurzname}){(Aktiv ? string.Empty : " · nicht mehr aktiv")}";

    protected internal override GremiumVersion KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Name, Kurzname, Aktiv, gueltigVon, gueltigBis, erfassung);
}
