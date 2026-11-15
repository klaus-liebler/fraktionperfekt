using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Eine fiktive Person der Demofraktion.</summary>
/// <param name="RisId"><c>null</c> = nicht im Ratsinformationssystem (Geschäftsführung, Helfer:innen).</param>
public sealed record Demoperson(string Vorname, string Nachname, string Kennung, string? RisId, string Funktion, params Rolle[] Rollen);

/// <summary>Ein fiktives Gremium der Stadt Greven mit den Sitzen der Demofraktion.</summary>
/// <param name="Stellvertretungen">RIS-Kennungen in Rangfolge (Rang 1 zuerst).</param>
/// <param name="Rhythmus">Sitzungsabstand in Tagen; Sitzungen ab <paramref name="ErsteSitzung"/>.</param>
public sealed record Demogremium(string RisId, string Name, string Kurzname, string Art, string[] Ordentliche, string[] Stellvertretungen,
    DateTime ErsteSitzung, int Rhythmus, int DauerMinuten, string Ort);

/// <summary>
/// Fiktive Demobesetzung der CDU-Fraktion Greven: Alle Personen sind erfunden, Ähnlichkeiten mit realen Personen sind Zufall. Die Gremien sind
/// Beispiele, wie es sie in Greven geben könnte; Zusammensetzung und Sitzungstermine sind ausgedacht.
/// </summary>
public static class Demofraktion
{
    public const string Fraktionsname = "CDU";

    public const string Anzeigename = "CDU-Fraktion Greven";

    public static IReadOnlyList<Demoperson> Personen { get; } =
    [
        new("Katrin", "Albers", "kalbers", null, "Fraktionsgeschäftsführerin", Rolle.Administration),
        new("Thomas", "Brinkmann", "tbrinkmann", "ris-p01", "Fraktionsvorsitzender", Rolle.Vorstand, Rolle.Mitglied),
        new("Sabine", "Hölscher", "shoelscher", "ris-p02", "stellv. Fraktionsvorsitzende", Rolle.Vorstand, Rolle.Mitglied),
        new("Markus", "Terhaar", "mterhaar", "ris-p03", "Kassier", Rolle.Kassier, Rolle.Mitglied),
        new("Anna", "Wewers", "awewers", "ris-p04", "Ratsmitglied", Rolle.Mitglied),
        new("Jens", "Overmeyer", "jovermeyer", "ris-p05", "Ratsmitglied", Rolle.Mitglied),
        new("Petra", "Lammers", "plammers", "ris-p06", "Ratsmitglied", Rolle.Mitglied),
        new("Dirk", "Schulze-Eling", "dschulze", "ris-p07", "Ratsmitglied", Rolle.Mitglied),
        new("Melanie", "Kock", "mkock", "ris-p08", "Ratsmitglied", Rolle.Mitglied),
        new("Stefan", "Grüter", "sgrueter", "ris-p09", "Ratsmitglied", Rolle.Mitglied),
        new("Ralf", "Niehues", "rniehues", "ris-p13", "Ratsmitglied", Rolle.Mitglied),
        new("Lena", "Hagedorn", "lhagedorn", "ris-p10", "sachkundige Bürgerin", Rolle.SachkundigeBuergerin),
        new("Yusuf", "Demir", "ydemir", "ris-p11", "sachkundiger Bürger", Rolle.SachkundigeBuergerin),
        new("Claudia", "Rensing", "crensing", "ris-p12", "sachkundige Bürgerin", Rolle.SachkundigeBuergerin),
        new("Felix", "Bäumer", "fbaeumer", null, "Wahlkampfteam", Rolle.Helferin),
        new("Nora", "Feldhaus", "nfeldhaus", null, "Ortsverband", Rolle.Helferin),
    ];

    /// <summary>Ratsmitglieder anderer Fraktionen: im RIS vorhanden, werden beim Abgleich nicht übernommen.</summary>
    public static IReadOnlyList<(string RisId, string Vorname, string Nachname)> AndereFraktion { get; } =
    [
        ("ris-x01", "Gerd", "Musterhaus"),
        ("ris-x02", "Ilse", "Beispielkamp"),
        ("ris-x03", "Heiner", "Platzhalter"),
    ];

    private static readonly string[] Ratsmitglieder = ["ris-p01", "ris-p02", "ris-p03", "ris-p04", "ris-p05", "ris-p06", "ris-p07", "ris-p08", "ris-p09", "ris-p13"];

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
