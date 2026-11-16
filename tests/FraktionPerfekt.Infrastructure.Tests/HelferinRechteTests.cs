using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Was das Menü für Helfer:innen sperrt, liefern auch die Dienste nicht (keine Lücke über die direkte Adresse).</summary>
public sealed class HelferinRechteTests
{
    [Fact]
    public async Task Helferinnen_bekommen_gesperrte_Module_auch_ueber_die_Dienste_nicht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);

        // Ids als Mitglied ermitteln
        await u.AnmeldenAsync("lkuehlert");
        var antrag = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().ListeAsync()))[0].Antrag.Id;
        var sitzung = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().SitzungenAsync()))[0].Sitzung.Id;
        var umfrage = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().ListeAsync()))[0].Umfrage.Id;

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().SitzungenAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(sitzung))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<AnfrageDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<AnliegenDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<PresseDienst>().MitteilungenAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<KontaktDienst>().ListeAsync(null))).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<BeschlussArchiv>().SuchenAsync(null, null, null, null))).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<BesetzungDienst>().ListeAsync())).ShouldBeEmpty();
    }
}
