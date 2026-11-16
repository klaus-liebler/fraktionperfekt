using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Datenbank-Regel 6 (Vorbereitung): je Mandant eine eigene Datenbank, Auswahl je Bereich über den Host, Briefkopf je Mandant.</summary>
public sealed class MandantTests
{
    [Fact]
    public void Verbindung_und_Briefkopf_folgen_dem_Mandanten_der_Anfrage()
    {
        var konfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mandanten:0:Kennung"] = "greven",
            ["Mandanten:0:Name"] = "CDU-Fraktion Greven",
            ["Mandanten:0:Datenbank"] = "Data Source=greven.db",
            ["Mandanten:0:Zeitzone"] = "Europe/Berlin",
            ["Mandanten:1:Kennung"] = "emsdetten",
            ["Mandanten:1:Name"] = "CDU-Fraktion Emsdetten",
            ["Mandanten:1:Datenbank"] = "Data Source=emsdetten.db",
            ["Mandanten:1:Hosts:0"] = "emsdetten.fraktion.test",
        }).Build();
        using var dienste = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(konfiguration)
            .AddSingleton<Application.Sicherheit.IAktuellerBenutzer>(new TestBenutzer())
            .AddFraktionAnwendung(konfiguration).AddFraktionInfrastruktur(konfiguration, Path.GetTempPath(), hintergrundjobs: false).BuildServiceProvider();

        using (var standard = dienste.CreateScope())
        {
            standard.ServiceProvider.GetRequiredService<FraktionDbContext>().Database.GetConnectionString()!.ShouldContain("greven.db");
            standard.ServiceProvider.GetRequiredService<Application.Dokumente.Briefkopf>().Absender.ShouldBe("CDU-Fraktion Greven");
        }

        using var anfrage = dienste.CreateScope();
        var auswahl = anfrage.ServiceProvider.GetRequiredService<MandantAuswahl>();
        auswahl.Festlegen(auswahl.ZumHost("emsdetten.fraktion.test"));
        anfrage.ServiceProvider.GetRequiredService<FraktionDbContext>().Database.GetConnectionString()!.ShouldContain("emsdetten.db");
        anfrage.ServiceProvider.GetRequiredService<Application.Dokumente.Briefkopf>().Absender.ShouldBe("CDU-Fraktion Emsdetten");
        auswahl.ZumHost("unbekannt.test").Kennung.ShouldBe("greven");
    }
}
