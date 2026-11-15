namespace FraktionPerfekt.Application;

/// <summary>
/// Allgemeiner Datenzugang für die Module ab Stufe 2: Abfragen werden als <see cref="IQueryable{T}"/> formuliert und über
/// diesen Port ausgeführt (die Anwendung kennt EF Core nicht). Gleiche Arbeitseinheit wie die übrigen Speicher:
/// <see cref="IArbeitsbereich.SpeichernAsync"/> schreibt fachliche Änderung, Postausgang und Protokoll gemeinsam fest.
/// Begrenzte Abfragen müssen eindeutig sortiert sein (in Entwicklung und Tests bricht sonst die Abfrage ab).
/// </summary>
public interface IDaten : IArbeitsbereich
{
    IQueryable<T> Menge<T>()
        where T : class;

    void Hinzufuegen<T>(T eintrag)
        where T : class;

    /// <summary>Nur für Einträge, die fachlich gelöscht werden dürfen (z. B. eine zurückgezogene Meldung); nie für Versionen oder Protokoll.</summary>
    void Entfernen<T>(T eintrag)
        where T : class;

    /// <summary>Eintrag über seinen Schlüssel „Id“; <c>null</c>, wenn es ihn nicht gibt.</summary>
    Task<T?> FindenAsync<T>(Guid id, CancellationToken abbruch = default)
        where T : class;

    Task<List<T>> ListeAsync<T>(IQueryable<T> abfrage, CancellationToken abbruch = default);

    Task<T?> ErsterAsync<T>(IQueryable<T> abfrage, CancellationToken abbruch = default);

    Task<int> AnzahlAsync<T>(IQueryable<T> abfrage, CancellationToken abbruch = default);
}

public static class DatenErweiterungen
{
    /// <summary>Lädt einen Eintrag über seine Id oder wirft eine verständliche Ausnahme.</summary>
    public static async Task<T> LadenAsync<T>(this IDaten daten, Guid id, string bezeichnung, CancellationToken abbruch = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(daten);
        return await daten.FindenAsync<T>(id, abbruch) ?? throw new InvalidOperationException($"{bezeichnung} nicht gefunden.");
    }
}
