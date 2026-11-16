namespace FraktionPerfekt.Application.Ris;

/// <summary>
/// Port zum Ratsinformationssystem der Stadt (SD.NET, später über OParl 1.1). Liefert nur öffentliche Inhalte und nur lesend.
/// Bis die OParl-Schnittstelle in Greven freigeschaltet ist, steht dahinter ein Mock mit fiktiven Grevener Gremien.
/// </summary>
public interface IRisSchnittstelle
{
    /// <summary>Name der Quelle für Anzeige und Protokoll, z. B. „RIS Greven (Mock)“.</summary>
    string Quelle { get; }

    Task<IReadOnlyList<RisGremium>> GremienAsync(CancellationToken abbruch = default);

    Task<IReadOnlyList<RisPerson>> PersonenAsync(CancellationToken abbruch = default);

    Task<IReadOnlyList<RisMitgliedschaft>> MitgliedschaftenAsync(CancellationToken abbruch = default);

    /// <summary>Sitzungen mit Beginn im Zeitraum (Wanduhrzeit Greven).</summary>
    Task<IReadOnlyList<RisSitzung>> SitzungenAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default);

    /// <summary>Beschlüsse (OParl: AgendaItem mit result/resolutionText) aus Sitzungen im Zeitraum.</summary>
    Task<IReadOnlyList<RisBeschluss>> BeschluesseAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default);
}

/// <summary>OParl „AgendaItem“ mit Ergebnis. <paramref name="Ergebnis"/> ist der Freitext des RIS (z. B. „einstimmig beschlossen“).</summary>
public sealed record RisBeschluss(string RisId, string GremiumRisId, DateOnly Datum, string Titel, string Text, string? Ergebnis);

/// <summary>OParl „Organization“ (Gremium).</summary>
/// <param name="Art">„Rat“, „Ausschuss“ oder „Beirat“ (OParl: organizationType/classification).</param>
public sealed record RisGremium(string RisId, string Name, string Kurzname, string Art);

/// <summary>OParl „Person“.</summary>
public sealed record RisPerson(string RisId, string Vorname, string Nachname, string? Fraktion);

/// <summary>OParl „Membership“: Rolle „Mitglied“ oder „Stellvertretendes Mitglied“; Ende exklusiv nicht gesetzt = laufend.</summary>
public sealed record RisMitgliedschaft(string RisId, string PersonRisId, string GremiumRisId, bool Stellvertretung, DateOnly Beginn, DateOnly? Ende,
    string? Rolle = null);

/// <summary>OParl „Meeting“.</summary>
public sealed record RisSitzung(string RisId, string GremiumRisId, string Name, DateTime Beginn, DateTime Ende, string? Ort, bool Abgesagt,
    string? Tagesordnung = null);
