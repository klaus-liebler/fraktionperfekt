using Microsoft.Extensions.DependencyInjection.Extensions;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Ris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Adapters.Mocks;

public static class Registrierung
{
    /// <summary>Registriert Testanmeldung, simulierte Zustellkanäle und das simulierte RIS. Echte Adapter ersetzen sie später je Kanal.</summary>
    public static IServiceCollection AddFraktionMocks(this IServiceCollection dienste, IConfiguration konfiguration)
    {
        dienste.Configure<TestanmeldungOptionen>(konfiguration.GetSection(TestanmeldungOptionen.Abschnitt));
        dienste.AddSingleton<Zugangsschutz>();
        dienste.AddSingleton<IZustellKanal, MockMailKanal>();
        if (!string.Equals(konfiguration["Push:Art"], "WebPush", StringComparison.OrdinalIgnoreCase))
        {
            dienste.AddSingleton<IZustellKanal, MockPushKanal>();
        }
        dienste.TryAddSingleton<IRisSchnittstelle, MockRis>();
        dienste.AddScoped<Testdaten.Demodaten>();
        dienste.TryAddSingleton<Application.Wissen.IKiAssistent, MockKi>();
        return dienste;
    }
}
