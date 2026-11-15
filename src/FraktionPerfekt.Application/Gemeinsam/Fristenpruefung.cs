namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Eine regelmäßige Fristenprüfung eines Moduls (Erinnerungen, Überfälligkeit, Löschfristen). Der stündliche Job ruft alle
/// registrierten Prüfungen nacheinander auf.
/// </summary>
public interface IFristenpruefung
{
    string Name { get; }

    /// <summary>Liefert die Zahl der bearbeiteten Vorgänge.</summary>
    Task<int> PruefenAsync(CancellationToken abbruch = default);
}

/// <summary>Fristenprüfung aus einer Methode eines Dienstes.</summary>
public sealed class Fristenpruefung(string name, Func<CancellationToken, Task<int>> pruefen) : IFristenpruefung
{
    public string Name => name;

    public Task<int> PruefenAsync(CancellationToken abbruch = default) => pruefen(abbruch);
}
