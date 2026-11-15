using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Gremien;

public enum Funktion
{
    /// <summary>Ordentliches Mitglied mit Stimmrecht.</summary>
    Ordentlich,

    /// <summary>Stellvertretendes Mitglied; vertritt ordentliche Mitglieder der Fraktion in der Reihenfolge des Rangs.</summary>
    Stellvertretung,
}

public static class FunktionExtensions
{
    public static string Anzeigename(this Funktion funktion) => funktion switch
    {
        Funktion.Ordentlich => "ordentliches Mitglied",
        Funktion.Stellvertretung => "Stellvertretung",
        _ => funktion.ToString(),
    };
}

/// <summary>
/// Mitgliedschaft einer Person in einem Gremium. Funktion und Vertretungsrang sind bitemporal versioniert: Ein Wechsel
/// (z. B. von Stellvertretung zu ordentlich) gilt ab einem Datum, frühere Sitzungen behalten die damalige Besetzung.
/// Der Rang ist fraktionsintern; die Funktion kommt, wenn vorhanden, aus dem RIS.
/// </summary>
public sealed class Mitgliedschaft
{
    private readonly List<MitgliedschaftsVersion> _versionen = [];

    private Mitgliedschaft()
    {
    }

    public Mitgliedschaft(Guid personId, Guid gremiumId, Funktion funktion, int? rang, DateOnly ab, Erfassung erfassung, string? risId = null)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        Id = Ids.Neu();
        PersonId = personId;
        GremiumId = gremiumId;
        RisId = risId;
        _versionen.Add(new MitgliedschaftsVersion(funktion, RangPruefen(funktion, rang), ab, null, erfassung with { Art = Erfassungsart.Ersterfassung }));
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public Guid GremiumId { get; private set; }

    /// <summary>Kennung der Mitgliedschaft im RIS, falls sie von dort stammt.</summary>
    public string? RisId { get; private set; }

    public IReadOnlyCollection<MitgliedschaftsVersion> Versionen => _versionen;

    /// <summary>Stand am Stichtag nach dem Wissensstand <paramref name="bekanntAm"/>; <c>null</c> = damals kein Mitglied.</summary>
    public MitgliedschaftsVersion? StandAm(DateOnly tag, DateTimeOffset bekanntAm) => _versionen.AlsStichtag(tag, bekanntAm);

    /// <summary>Neue Funktion oder neuer Rang ab einem Datum.</summary>
    public void AendernAb(DateOnly ab, Funktion funktion, int? rang, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        ZeitlicheHistorie.AendernAb(_versionen,
            new MitgliedschaftsVersion(funktion, RangPruefen(funktion, rang), ab, null, erfassung with { Art = Erfassungsart.AenderungAbDatum }));
    }

    /// <summary>Mitgliedschaft endet ab <paramref name="ab"/> (erster Tag ohne Mitgliedschaft).</summary>
    public void BeendenAb(DateOnly ab, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var letzte = _versionen.Where(v => v.IstAktuellerWissensstand && !v.IstEndemarke).OrderBy(v => v.GueltigVon).LastOrDefault()
            ?? throw new InvalidOperationException("Die Mitgliedschaft ist bereits beendet.");
        ZeitlicheHistorie.BeendenAb(_versionen,
            new MitgliedschaftsVersion(letzte.Funktion, letzte.Rang, ab, null, erfassung with { Art = Erfassungsart.Beendet }));
    }

    private static int? RangPruefen(Funktion funktion, int? rang)
    {
        if (funktion == Funktion.Ordentlich)
        {
            return null;
        }

        if (rang is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rang), "Der Vertretungsrang beginnt bei 1.");
        }

        return rang;
    }
}

/// <summary>Eine Version der Mitgliedschaft (Funktion und Rang).</summary>
public sealed class MitgliedschaftsVersion : BitemporaleVersion<MitgliedschaftsVersion>
{
    private MitgliedschaftsVersion()
    {
    }

    public MitgliedschaftsVersion(Funktion funktion, int? rang, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        Funktion = funktion;
        Rang = rang;
    }

    public Funktion Funktion { get; private set; }

    /// <summary>Vertretungsrang (1 = wird zuerst gefragt); nur bei Stellvertretung.</summary>
    public int? Rang { get; private set; }

    public override string Datenart => "Mitgliedschaft";

    public override string Feld => "Funktion und Rang";

    public override string Gegenstand => "mitgliedschaft";

    public override string Wert => Funktion == Funktion.Stellvertretung && Rang is { } r
        ? $"{Funktion.Anzeigename()}, Rang {r}"
        : Funktion.Anzeigename();

    protected internal override MitgliedschaftsVersion KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Funktion, Rang, gueltigVon, gueltigBis, erfassung);
}
