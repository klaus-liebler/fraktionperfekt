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

/// <summary>Ein Gremium der Stadt Greven (echter Name, erfundene Sitzungstermine).</summary>
/// <param name="Rhythmus">Sitzungsabstand in Tagen; Sitzungen ab <paramref name="ErsteSitzung"/>.</param>
/// <param name="RisName">Abweichende Bezeichnung in der RIS-Tabelle (z. B. „Rat“).</param>
public sealed record Demogremium(string RisId, string Name, string Kurzname, string Art, DateTime ErsteSitzung, int Rhythmus, int DauerMinuten, string Ort,
    string? RisName = null);

/// <summary>
/// Demobesetzung der CDU-Fraktion Greven: die echten Mitglieder aus <see cref="FraktionGreven"/>, dazu fiktive Mitarbeitende
/// (Geschäftsführung, Kampagnenteam). Gremien, Sitze, Termine und alle Vorgänge sind erfunden (Demo).
/// </summary>
public static class Demofraktion
{
    public const string Fraktionsname = "CDU";

    public const string Anzeigename = "CDU-Fraktion Greven";

    /// <summary>Fiktive Mitarbeitende ohne Mandat (als Demo gekennzeichnet).</summary>
    public static IReadOnlyList<Demoperson> Mitarbeitende { get; } =
    [
        new("Katrin", "Albers", "kalbers", null, "Fraktionsgeschäftsführerin (fiktiv, Demo)", [Rolle.Administration], "Frau"),
        new("Felix", "Bäumer", "fbaeumer", null, "Kampagnenteam (fiktiv, Demo)", [Rolle.Helferin], "Herr"),
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

    /// <summary>
    /// Die Gremien der Stadt Greven (Namen wie im RIS, Kurzbezeichnungen für die Oberfläche). Besetzung der Ratsmitglieder aus
    /// <see cref="FraktionGreven.Gremienzugehoerigkeit"/>; Sitzungstermine, Orte und Tagesordnungen sind erfunden (Demo).
    /// </summary>
    public static IReadOnlyList<Demogremium> Gremien { get; } =
    [
        new("ris-g01", "Rat der Stadt Greven", "Rat", "Rat", new DateTime(2026, 1, 15, 17, 0, 0), 42, 210, "Rathaus Greven, Ratssaal", "Rat"),
        new("ris-g02", "Haupt-/Finanz- und Wirtschaftsausschuss", "HFWA", "Ausschuss", new DateTime(2026, 1, 6, 17, 0, 0), 42, 150, "Rathaus Greven, Ratssaal"),
        new("ris-g03", "Ausschuss für Stadtentwicklung, Klima und Umwelt", "ASKU", "Ausschuss", new DateTime(2026, 1, 13, 17, 30, 0), 35, 150, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g04", "Schulausschuss", "SchulA", "Ausschuss", new DateTime(2026, 1, 20, 18, 0, 0), 42, 120, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g05", "Sozialausschuss", "SozA", "Ausschuss", new DateTime(2026, 1, 21, 17, 0, 0), 42, 120, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g06", "Ausschuss für Kultur, Sport und Freizeit", "AKSF", "Ausschuss", new DateTime(2026, 1, 27, 18, 0, 0), 42, 120, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g07", "Rechnungsprüfungsausschuss", "RPA", "Ausschuss", new DateTime(2026, 2, 3, 16, 30, 0), 63, 90, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g08", "Ausschuss für Chancengerechtigkeit und Integration", "ACI", "Ausschuss", new DateTime(2026, 1, 28, 18, 0, 0), 56, 120, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g09", "Jugendhilfeausschuss", "JHA", "Ausschuss", new DateTime(2026, 2, 4, 17, 0, 0), 56, 120, "Rathaus Greven, Sitzungsraum 1"),
        new("ris-g10", "Betriebsausschuss", "BetrA", "Ausschuss", new DateTime(2026, 2, 10, 17, 0, 0), 56, 90, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g11", "Planungs- und Bauausschuss für den Rathausneubau", "PBR", "Ausschuss", new DateTime(2026, 2, 12, 17, 30, 0), 42, 120, "Rathaus Greven, Ratssaal"),
        new("ris-g12", "Bezirksausschuss für die Bauerschaften", "BA Bauerschaften", "Ausschuss", new DateTime(2026, 2, 17, 19, 0, 0), 84, 120, "Gaststätte in den Bauerschaften (Ort fiktiv)"),
        new("ris-g13", "Bezirksausschuss für die Ortschaft Reckenfeld", "BA Reckenfeld", "Ausschuss", new DateTime(2026, 2, 18, 19, 0, 0), 84, 120, "Bürgerhaus Reckenfeld"),
        new("ris-g14", "Bezirksausschuss für die Ortschaft Gimbte", "BA Gimbte", "Ausschuss", new DateTime(2026, 2, 19, 19, 0, 0), 84, 120, "Gimbte (Ort fiktiv)"),
        new("ris-g15", "Wahlausschuss", "WA", "Ausschuss", new DateTime(2026, 3, 3, 17, 0, 0), 182, 60, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g16", "Wahlprüfungsausschuss", "WPA", "Ausschuss", new DateTime(2026, 3, 4, 17, 0, 0), 182, 60, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g17", "Umlegungsausschuss", "UmlA", "Ausschuss", new DateTime(2026, 3, 10, 16, 0, 0), 91, 60, "Rathaus Greven, Sitzungsraum 2"),
        new("ris-g18", "Zweckverbandsversammlung Musikschule Greven/Emsdetten/Saerbeck", "ZV Musikschule", "Zweckverband", new DateTime(2026, 3, 12, 18, 0, 0), 91, 90,
            "Musikschule (Ort fiktiv)"),
    ];

    /// <summary>Gremium zum Namen, wie er in der RIS-Tabelle steht („Rat“ oder voller Name).</summary>
    public static Demogremium GremiumZu(string risName)
        => Gremien.FirstOrDefault(g => string.Equals(g.Name, risName, StringComparison.OrdinalIgnoreCase) || string.Equals(g.RisName, risName, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"Unbekanntes Gremium „{risName}“ – bitte in Demofraktion.Gremien ergänzen.");
}
