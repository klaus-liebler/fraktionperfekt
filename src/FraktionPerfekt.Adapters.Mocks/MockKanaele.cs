using FraktionPerfekt.Application.Postausgang;

namespace FraktionPerfekt.Adapters.Mocks;

/// <summary>Simulierter Mailversand: Es verlässt nichts das System. Die Mail bleibt mit vollem Text im Postausgang sichtbar.</summary>
public sealed class MockMailKanal : IZustellKanal
{
    public Kanal Kanal => Kanal.EMail;

    public bool Simuliert => true;

    public Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default)
        => Task.FromResult<string?>("simuliert, nicht versendet");
}

/// <summary>
/// Simulierte Push-Mitteilung (später Web Push/VAPID bzw. APNs/FCM). Nichts verlässt das System; im Postausgang
/// steht, was auf dem Sperrbildschirm erschienen wäre.
/// </summary>
public sealed class MockPushKanal : IZustellKanal
{
    public Kanal Kanal => Kanal.Push;

    public bool Simuliert => true;

    public Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default)
        => Task.FromResult<string?>(nachricht.Aktion is null ? "Push simuliert" : $"Push simuliert, Aktionsknopf „{nachricht.Aktion}“");
}
