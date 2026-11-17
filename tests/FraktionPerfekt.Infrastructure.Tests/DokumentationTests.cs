using FraktionPerfekt.Application.Dokumentation;
using FraktionPerfekt.Domain.Dokumentation;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Kommentare zu Konzept/Hilfe/Doku und Impressum (aus VibeVHS übernommen): Rechte im Dienst, Fäden, Namen zur Laufzeit.</summary>
public sealed class DokumentationTests
{
    [Fact]
    public async Task Administration_und_Vorstand_kommentieren_Mitglieder_nicht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);

        await u.AnmeldenAsync("kalbers");
        var k = await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().SchreibenAsync("hilfe", "aufbau", "Aufbau der Oberfläche", "Hilfe-Link fehlt am Handy."));

        await u.AnmeldenAsync("mbenning", Rolle.Vorstand);
        await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().SchreibenAsync("egal", "egal", "egal", "Bitte ergänzen.", k.Id));

        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().AlleAsync())))
            .Message.ShouldContain("Nur für die Rolle");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().SchreibenAsync("hilfe", "", "", "x")));
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().AlsMarkdownAsync()));

        await u.AnmeldenAsync("kalbers");
        var faeden = await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ZuDokumentAsync("hilfe"));
        var faden = faeden["aufbau"].Single();
        faden.Start.Verfasst.ShouldBe("Katrin Albers"); // Name zur Laufzeit, gespeichert ist nur die PersonId
        faden.Antworten.Single().Kommentar.Dokument.ShouldBe("hilfe");
        faden.Antworten.Single().Kommentar.Abschnitt.ShouldBe("aufbau");

        await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ErledigtSetzenAsync(k.Id, true));
        var md = await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().AlsMarkdownAsync());
        md.ShouldContain("0 offen, 1 erledigt");
        md.ShouldContain("„Aufbau der Oberfläche“ (#aufbau)");
        md.ShouldContain("erledigt von Katrin Albers");
    }

    [Fact]
    public async Task Leere_und_zu_lange_Kommentare_werden_abgelehnt()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("kalbers");

        await Should.ThrowAsync<ArgumentException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().SchreibenAsync("hilfe", "", "", "  ")));
        await Should.ThrowAsync<ArgumentException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().SchreibenAsync("hilfe", "", "", new string('a', Kommentar.TextMaxZeichen + 1))));
    }

    [Fact]
    public async Task Impressum_pflegt_nur_die_Administration_und_ist_ohne_Anmeldung_lesbar()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        (await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumAsync())).ShouldBeNull();

        await u.AnmeldenAsync("mbenning", Rolle.Vorstand);
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumSetzenAsync("**CDU-Fraktion**")));

        await u.AnmeldenAsync("kalbers");
        await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumSetzenAsync("  **CDU-Fraktion Greven**\r\nRathausstraße  "));
        (await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumAsync())).ShouldBe("**CDU-Fraktion Greven**\nRathausstraße");

        await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumSetzenAsync("   "));
        (await u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumAsync())).ShouldBeNull();
        await Should.ThrowAsync<ArgumentException>(() => u.InAsync(d => d.GetRequiredService<DokumentationDienst>().ImpressumSetzenAsync(new string('x', Impressum.MaxZeichen + 1))));
    }
}
