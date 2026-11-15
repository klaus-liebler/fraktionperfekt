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
        dienste.AddSingleton<IZustellKanal, MockPushKanal>();
        dienste.AddSingleton<IRisSchnittstelle, MockRis>();
        dienste.AddScoped<Testdaten.Demodaten>();
        return dienste;
    }
}
