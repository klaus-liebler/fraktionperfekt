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
        dienste.Configure<GeschaeftsordnungOptionen>(konfiguration.GetSection(GeschaeftsordnungOptionen.Abschnitt));
        // Mandanten (Datenbank-Regel 6): je Mandant eigene Datenbank; Auswahl je Bereich (Anfrage), Standard = erster Mandant
        var mandanten = MandantOptionen.Lesen(konfiguration);
        Ortszeit.Festlegen(mandanten[0].Zeitzone);
        dienste.AddSingleton(mandanten);
        dienste.AddScoped(_ => new MandantAuswahl(mandanten));
        dienste.TryAddScoped<IAktuellerMandant>(d => d.GetRequiredService<MandantAuswahl>());
        dienste.TryAddSingleton(TimeProvider.System);
        dienste.AddScoped<PostausgangDienst>();
        dienste.TryAddSingleton(new Postausgang.Demobetrieb(false));
        dienste.AddScoped<Zustellung>();
        dienste.AddScoped<Stammdaten.StammdatenDienst>();
        dienste.AddScoped<Ris.RisAbgleich>();
        dienste.AddScoped<Termine.KalenderDienst>();
        dienste.AddScoped<Vertretung.VertretungDienst>();
        dienste.AddScoped<Aufgaben.AufgabenDienst>();
        dienste.AddScoped<Heute.HeuteDienst>();
        dienste.AddScoped<Heute.HinweisDienst>();
        dienste.AddScoped<Suche.SucheDienst>();
        dienste.AddScoped<Sitzungen.SitzungDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Aufgaben", d.GetRequiredService<Aufgaben.AufgabenDienst>().ErinnernAsync));
        dienste.AddScoped<Abstimmungen.UmfrageDienst>();
        dienste.AddScoped<Antraege.AntragDienst>();
        dienste.AddScoped<Ressourcen.RessourcenDienst>();
        dienste.AddScoped<Dokumentation.DokumentationDienst>();
        dienste.AddScoped<Termine.BesetzungDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Abstimmungen", d.GetRequiredService<Abstimmungen.UmfrageDienst>().FristenPruefenAsync));
        dienste.AddScoped<Abstimmungen.WahlDienst>();
        dienste.AddScoped<Sitzungen.BeamerDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Sitzungsabstimmungen", d.GetRequiredService<Sitzungen.SitzungDienst>().AbstimmungenUebernehmenAsync));
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Wahlen", d.GetRequiredService<Abstimmungen.WahlDienst>().FristenPruefenAsync));
        dienste.AddScoped<Wissen.DateiDienst>();
        dienste.AddScoped<Wissen.WikiDienst>();
        dienste.AddScoped<Wissen.BildDienst>();
        foreach (var b in Wissen.BildBesitzer.Standard())
        {
            dienste.AddSingleton(b);
        }

        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Wiki-Bilder aufräumen", async t => await d.GetRequiredService<Wissen.BildDienst>().AufraeumenAsync(t)));
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Wiki-KI", async t => await d.GetRequiredService<Wissen.WikiDienst>().KiPruefenNachtsAsync(t)));
        dienste.AddScoped<Politik.AnfrageDienst>();
        dienste.AddScoped<Politik.BeschlussArchiv>();
        dienste.AddScoped<Politik.KontaktDienst>();
        dienste.AddScoped<Politik.AnliegenDienst>();
        dienste.AddScoped<Politik.PresseDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Anfragen", d.GetRequiredService<Politik.AnfrageDienst>().FristenPruefenAsync));
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Anliegen-Löschfrist", d.GetRequiredService<Politik.AnliegenDienst>().LoeschfristenAsync));
        dienste.AddScoped<Kampagnen.KampagnenDienst>();
        dienste.AddScoped<Finanzen.FinanzDienst>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Kampagnen", d.GetRequiredService<Kampagnen.KampagnenDienst>().FristenPruefenAsync));
        dienste.AddScoped<Termine.Sitzungserinnerung>();
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Sitzungserinnerung", d.GetRequiredService<Termine.Sitzungserinnerung>().ErinnernAsync));
        dienste.AddScoped<IFristenpruefung>(d => new Fristenpruefung("Abwesenheiten-Löschfrist", d.GetRequiredService<Termine.Sitzungserinnerung>().AbwesenheitenLoeschfristAsync));
        dienste.AddScoped<Postausgang.PushAboDienst>();
        return dienste;
    }
}
