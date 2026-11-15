namespace FraktionPerfekt.Application.Postausgang;

/// <summary>Speicher für den Postausgang (Outbox-Tabelle).</summary>
public interface INachrichtenSpeicher
{
    void Hinzufuegen(AusgehendeNachricht nachricht);

    Task<AusgehendeNachricht?> LadenAsync(Guid id, CancellationToken abbruch = default);

    Task<IReadOnlyList<AusgehendeNachricht>> WartendeAsync(int maximal, CancellationToken abbruch = default);

    Task<IReadOnlyList<AusgehendeNachricht>> NeuesteAsync(int anzahl, CancellationToken abbruch = default);

    Task<int> AnzahlAsync(NachrichtenStatus status, CancellationToken abbruch = default);

    Task SpeichernAsync(CancellationToken abbruch = default);
}

/// <summary>
/// Stellt Nachrichten eines Kanals tatsächlich zu (SMTP, Web Push …). In der Entwicklung gibt es dafür Mocks, die nichts
/// nach außen senden.
/// </summary>
public interface IZustellKanal
{
    Kanal Kanal { get; }

    /// <summary>Stellt zu und liefert eine Rückmeldung. Fehler als Ausnahme.</summary>
    Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default);
}
