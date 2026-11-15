using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Postausgang;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FraktionPerfekt.Application;

public static class Registrierung
{
    public static IServiceCollection AddFraktionAnwendung(this IServiceCollection dienste, IConfiguration konfiguration)
    {
        dienste.Configure<FraktionOptionen>(konfiguration.GetSection(FraktionOptionen.Abschnitt));
        dienste.TryAddSingleton(TimeProvider.System);
        dienste.AddScoped<PostausgangDienst>();
        dienste.AddScoped<Zustellung>();
        dienste.AddScoped<Stammdaten.StammdatenDienst>();
        dienste.AddScoped<Ris.RisAbgleich>();
        dienste.AddScoped<Termine.KalenderDienst>();
        dienste.AddScoped<Vertretung.VertretungDienst>();
        dienste.AddScoped<Aufgaben.AufgabenDienst>();
        dienste.AddScoped<Heute.HeuteDienst>();
        dienste.AddScoped<Sitzungen.SitzungDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Aufgaben", d.GetRequiredService<Aufgaben.AufgabenDienst>().ErinnernAsync));
        dienste.AddScoped<Abstimmungen.UmfrageDienst>();
        dienste.AddScoped<Antraege.AntragDienst>();
        dienste.AddScoped<Ressourcen.RessourcenDienst>();
        dienste.AddScoped<Termine.BesetzungDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Abstimmungen", d.GetRequiredService<Abstimmungen.UmfrageDienst>().FristenPruefenAsync));
        return dienste;
    }
}
