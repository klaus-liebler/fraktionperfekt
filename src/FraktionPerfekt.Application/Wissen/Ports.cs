using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Wissen;

/// <summary>
/// Dateispeicher (Modul 8). Standard: lokal im Datenverzeichnis. Per Konfiguration (Dateiablage:Art=WebDav) Nextcloud über
/// WebDAV; dann erscheinen die Ordner auch im Nextcloud-Desktop-Client (Explorer/Finder).
/// </summary>
public interface IDateiablage
{
    string Name { get; }

    Task SpeichernAsync(string schluessel, Stream inhalt, CancellationToken abbruch = default);

    Task<Stream?> OeffnenAsync(string schluessel, CancellationToken abbruch = default);

    Task LoeschenAsync(string schluessel, CancellationToken abbruch = default);
}

/// <summary>Volltextsuche über Wiki-Seiten (SQLite FTS5).</summary>
public interface IWikiSuche
{
    Task AktualisierenAsync(Guid seiteId, string titel, string text, CancellationToken abbruch = default);

    Task EntfernenAsync(Guid seiteId, CancellationToken abbruch = default);

    /// <summary>Treffer (Seiten-Id, Ausschnitt mit Markierung **…**), beste zuerst.</summary>
    Task<IReadOnlyList<(Guid SeiteId, string Ausschnitt)>> SuchenAsync(string anfrage, int maximal, CancellationToken abbruch = default);
}

/// <summary>Seite mit aktuellem Text, wie die KI sie sieht (nur freigegebene Bereiche).</summary>
public sealed record KiSeite(Guid Id, string Bereich, string Titel, string Text, DateTimeOffset GeaendertAm, int Version, IReadOnlyList<string> Verweise);

/// <summary>Ein Beschluss als Kontext für die Prüfung auf veraltete Angaben.</summary>
public sealed record KiBeschluss(DateOnly Datum, string Titel, string Text);

public sealed record KiVorschlag(Guid SeiteId, Vorschlagsart Art, string Begruendung, string? NeuerText);

public sealed record KiAntwort(string Antwort, IReadOnlyList<(Guid SeiteId, string Titel)> Quellen);

/// <summary>
/// KI-Assistent für das Wiki. Er liest nur freigegebene Bereiche und liefert ausschließlich Vorschläge; ein Mensch nimmt an.
/// Vorerst Mock (regelbasiert); ein echtes Modell (Anbieter mit AVV oder selbst gehostet) kommt als weiterer Adapter.
/// </summary>
public interface IKiAssistent
{
    string Name { get; }

    Task<IReadOnlyList<KiVorschlag>> PruefenAsync(IReadOnlyList<KiSeite> seiten, IReadOnlyList<KiBeschluss> beschluesse, DateOnly heute,
        CancellationToken abbruch = default);

    Task<KiAntwort> FragenAsync(string frage, IReadOnlyList<KiSeite> treffer, CancellationToken abbruch = default);
}
