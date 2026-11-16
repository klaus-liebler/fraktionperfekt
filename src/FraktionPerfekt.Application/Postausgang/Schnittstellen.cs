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

    /// <summary><c>true</c> = simuliert, nichts verlässt das System (Mocks). Im Demobetrieb sind nur solche Kanäle für E-Mail erlaubt.</summary>
    bool Simuliert => false;

    /// <summary>Stellt zu und liefert eine Rückmeldung. Fehler als Ausnahme.</summary>
    Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default);
}

/// <summary>
/// Demobetrieb (Demodaten mit echten Namen und teils echten Adressen): Mails gehen dann nie über einen echten Kanal hinaus,
/// sondern werden blockiert. Wird aus der Konfiguration „Demodaten:Anlegen“ gesetzt.
/// </summary>
public sealed record Demobetrieb(bool Aktiv);
