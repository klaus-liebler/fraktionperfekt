using FraktionPerfekt.Application.Gemeinsam;
using Microsoft.Extensions.Configuration;

namespace FraktionPerfekt.Application.Tests;

/// <summary>Übungsinstanz über die Mandanten (Übertragung SchoolCraft, Punkt 9): Touren und Testanmeldung nur dort, Demo-Schutz.</summary>
public sealed class BetriebsartTests
{
    private static readonly MandantOptionen Echt = new() { Kennung = "greven", Hosts = ["fraktion.example"] };
    private static readonly MandantOptionen Uebung = new() { Kennung = "uebung", Hosts = ["uebung.example"], Uebung = true };

    private static Betriebsart Art(MandantOptionen aktuell, IReadOnlyList<MandantOptionen> alle, Dictionary<string, string?>? konfig = null)
    {
        var auswahl = new MandantAuswahl(alle);
        auswahl.Festlegen(aktuell);
        return new Betriebsart(auswahl, alle, new ConfigurationBuilder().AddInMemoryCollection(konfig ?? []).Build());
    }

    [Fact]
    public void Ohne_Uebungsinstanz_wie_bisher()
    {
        var a = Art(Echt, [Echt]);
        a.IstUebung.ShouldBeFalse();
        a.TourenAktiv.ShouldBeTrue(); // Abnahmemodus:Aktiv Standard an
        a.TestanmeldungErlaubt.ShouldBeTrue();
        a.DemoSchutz(false).ShouldBeFalse();
        Art(Echt, [Echt], new() { ["Abnahmemodus:Aktiv"] = "false" }).TourenAktiv.ShouldBeFalse();
    }

    [Fact]
    public void Mit_Uebungsinstanz_Touren_nur_dort_und_Demo_Schutz()
    {
        var echt = Art(Echt, [Echt, Uebung], new() { ["Anmeldung:TestanmeldungNurInUebung"] = "true" });
        echt.TourenAktiv.ShouldBeFalse();
        echt.TestanmeldungErlaubt.ShouldBeFalse();
        echt.DemoSchutz(false).ShouldBeFalse();

        var uebung = Art(Uebung, [Echt, Uebung], new() { ["Abnahmemodus:Aktiv"] = "false", ["Anmeldung:TestanmeldungNurInUebung"] = "true" });
        uebung.IstUebung.ShouldBeTrue();
        uebung.TourenAktiv.ShouldBeTrue();
        uebung.TestanmeldungErlaubt.ShouldBeTrue();
        uebung.DemoSchutz(false).ShouldBeTrue();

        new MandantAuswahl([Echt, Uebung]).ZumHost("uebung.example").ShouldBe(Uebung);
        new MandantAuswahl([Echt, Uebung]).ZumHost("unbekannt").ShouldBe(Echt);
    }
}
