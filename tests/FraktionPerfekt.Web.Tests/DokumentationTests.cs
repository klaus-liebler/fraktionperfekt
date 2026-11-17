using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Web.Dokumentation;

namespace FraktionPerfekt.Web.Tests;

/// <summary>Hilfe, Konzept und Doku in der Anwendung (aus VibeVHS übernommen): Katalog, Rechte, sichere Links, Abschnitte.</summary>
public sealed class DokumentationTests
{
    [Fact]
    public void Hilfe_fuer_alle_Doku_nur_fuer_Administration_und_Vorstand()
    {
        var katalog = new Dokumentkatalog();

        katalog.Laden(Dokumentkatalog.Finden(Dokumentkatalog.Hilfe)!, Rolle.Helferin).ShouldNotBeNull();
        Should.Throw<InvalidOperationException>(() => katalog.Laden(Dokumentkatalog.Finden("doku/betrieb")!, Rolle.Mitglied));
        katalog.Laden(Dokumentkatalog.Finden("doku/betrieb")!, Rolle.Vorstand).ShouldNotBeNull();
    }

    [Fact]
    public void Jedes_Dokument_ausser_dem_noch_nicht_uebernommenen_Konzept_wird_mit_ausgeliefert()
    {
        var katalog = new Dokumentkatalog();
        foreach (var d in Dokumentkatalog.Alle.Where(d => d.Schluessel != "spezifikation/konzept"))
        {
            katalog.Vorhanden(d).ShouldBeTrue($"{d.Datei} fehlt unter Dokumentation/ (csproj: docs/**/*.md, README.md)");
            katalog.Laden(d, Rolle.Administration).ShouldNotBeNull().ShouldNotBeEmpty();
        }
    }

    [Fact]
    public void Relative_Links_zeigen_auf_Seiten_der_Anwendung_oder_entfallen()
    {
        var gesamt = Dokumentkatalog.Finden("doku/abnahme-gesamt")!;
        var readme = Dokumentkatalog.Finden("doku/ueberblick")!;

        Dokumentkatalog.LinkZiel(gesamt, "abnahme-1.md").ShouldBe("dokumentation/doku/abnahme-1");
        Dokumentkatalog.LinkZiel(gesamt, "betrieb.md#x").ShouldBe("dokumentation/doku/betrieb#x");
        Dokumentkatalog.LinkZiel(readme, "docs/sicherheit.md").ShouldBe("dokumentation/doku/sicherheit");
        Dokumentkatalog.LinkZiel(gesamt, "../src/Geheim.cs").ShouldBeNull();
    }

    [Fact]
    public void Html_und_gefaehrliche_Links_werden_nicht_uebernommen()
    {
        var html = MarkdownDarstellung.AlsHtml("<script>x</script> [a](javascript:alert(1)) [b](https://cdu-greven.de)");

        html.ShouldNotContain("<script");
        html.ShouldNotContain("javascript:", Case.Insensitive);
        html.ShouldContain("href=\"https://cdu-greven.de\"");
    }
}
