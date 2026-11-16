using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Eine Person der Demodaten (echte Fraktionsmitglieder aus <see cref="FraktionGreven"/> oder fiktive Mitarbeitende).</summary>
/// <param name="RisId"><c>null</c> = nicht im Ratsinformationssystem (Geschäftsführung, Helfer:innen).</param>
/// <param name="EMail"><c>null</c> = „{Kennung}@demo.invalid“ (nicht zustellbar).</param>
public sealed record Demoperson(string Vorname, string Nachname, string Kennung, string? RisId, string Funktion, Rolle[] Rollen, string? Anrede = null,
    string? Titel = null, string? EMail = null)
{
    public string Adresse => EMail ?? $"{Kennung}@demo.invalid";
}

/// <summary>Ein fiktives Gremium der Stadt Greven mit den Sitzen der Demofraktion.</summary>
/// <param name="Stellvertretungen">RIS-Kennungen in Rangfolge (Rang 1 zuerst).</param>
/// <param name="Rhythmus">Sitzungsabstand in Tagen; Sitzungen ab <paramref name="ErsteSitzung"/>.</param>
public sealed record Demogremium(string RisId, string Name, string Kurzname, string Art, string[] Ordentliche, string[] Stellvertretungen,
    DateTime ErsteSitzung, int Rhythmus, int DauerMinuten, string Ort);

/// <summary>
/// Demobesetzung der CDU-Fraktion Greven: die echten Mitglieder aus <see cref="FraktionGreven"/>, dazu fiktive Mitarbeitende
/// (Geschäftsführung, Wahlkampfteam). Gremien, Sitze, Termine und alle Vorgänge sind erfunden (Demo).
/// </summary>
public static class Demofraktion
{
    public const string Fraktionsname = "CDU";

    public const string Anzeigename = "CDU-Fraktion Greven";

    /// <summary>Fiktive Mitarbeitende ohne Mandat (als Demo gekennzeichnet).</summary>
    public static IReadOnlyList<Demoperson> Mitarbeitende { get; } =
    [
        new("Katrin", "Albers", "kalbers", null, "Fraktionsgeschäftsführerin (fiktiv, Demo)", [Rolle.Administration], "Frau"),
        new("Felix", "Bäumer", "fbaeumer", null, "Wahlkampfteam (fiktiv, Demo)", [Rolle.Helferin], "Herr"),
        new("Nora", "Feldhaus", "nfeldhaus", null, "Ortsverband (fiktiv, Demo)", [Rolle.Helferin], "Frau"),
    ];

    /// <summary>Alle Personen der Demodaten; Klaus Liebler zuerst (Haupt-Login der Demo).</summary>
    public static IReadOnlyList<Demoperson> Personen { get; } = [.. FraktionGreven.Ratsmitglieder, .. FraktionGreven.SachkundigeBuerger, .. Mitarbeitende];

    /// <summary>Ratsmitglieder anderer Fraktionen: im RIS vorhanden, werden beim Abgleich nicht übernommen.</summary>
    public static IReadOnlyList<(string RisId, string Vorname, string Nachname)> AndereFraktion { get; } =
    [
        ("ris-x01", "Gerd", "Musterhaus"),
        ("ris-x02", "Ilse", "Beispielkamp"),
        ("ris-x03", "Heiner", "Platzhalter"),
    ];

    private static readonly string[] Ratsmitglieder = FraktionGreven.Ratsmitglieder.Select(p => p.RisId!).ToArray();

    public static IReadOnlyList<Demogremium> Gremien { get; } =
    [
        new("ris-g01", "Rat der Stadt Greven", "Rat", "Rat", Ratsmitglieder, [], new DateTime(2026, 1, 15, 17, 0, 0), 42, 210, "Rathaus Greven, Ratssaal"),
        new("ris-g02", "Haupt- und Finanzausschuss", "HFA", "Ausschuss", ["ris-p01", "ris-p02", "ris-p03", "ris-p05"], ["ris-p04", "ris-p06", "ris-p07"],
            new DateTime(2026, 1, 6, 17, 0, 0), 42, 150, "Rathaus Greven, Ratssaal"),
        new("ris-g03", "Ausschuss für Stadtentwicklung und Verkehr", "ASV", "Ausschuss", ["ris-p05", "ris-p07", "ris-p10"],
            ["ris-p08", "ris-p11", "ris-p09"], new DateTime(2026, 1, 13, 17, 30, 0), 35, 150, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g04", "Ausschuss für Schule und Bildung", "ASB", "Ausschuss", ["ris-p04", "ris-p11", "ris-p08"], ["ris-p12", "ris-p06", "ris-p02"],
            new DateTime(2026, 1, 20, 18, 0, 0), 42, 120, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g05", "Ausschuss für Soziales, Familie und Senioren", "ASFS", "Ausschuss", ["ris-p06", "ris-p12", "ris-p09"], ["ris-p10", "ris-p13", "ris-p05"],
            new DateTime(2026, 1, 21, 17, 0, 0), 42, 120, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g06", "Ausschuss für Klima, Umwelt und Digitales", "AKUD", "Ausschuss", ["ris-p13", "ris-p10", "ris-p02"], ["ris-p11", "ris-p04", "ris-p03"],
            new DateTime(2026, 1, 27, 18, 0, 0), 35, 120, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g07", "Rechnungsprüfungsausschuss", "RPA", "Ausschuss", ["ris-p03", "ris-p09"], ["ris-p07", "ris-p01"],
            new DateTime(2026, 2, 3, 16, 30, 0), 63, 90, "Rathaus Greven, Sitzungsraum 2"),
    ];
}
