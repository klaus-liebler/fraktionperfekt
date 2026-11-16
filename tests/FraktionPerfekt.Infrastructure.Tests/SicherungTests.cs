using FraktionPerfekt.Application.Betrieb;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class SicherungTests
{
    [Fact]
    public async Task Sicherung_mit_Pruefsumme_Wiederherstellungstest_und_Rechtepruefung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);

        // Nur die Administration (oder die Automatik) sichert.
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<ISicherung>().SichernAsync())))
            .Message.ShouldContain("Administration");

        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        var e = await u.InAsync(d => d.GetRequiredService<ISicherung>().SichernAsync());
        e.Wiederherstellbar.ShouldBe(true);
        e.Pruefergebnis!.ShouldContain("Personen 53");
        e.Sha256.Length.ShouldBe(64);

        var liste = await u.InAsync(d => d.GetRequiredService<ISicherung>().ListeAsync());
        liste.ShouldHaveSingleItem().Datei.ShouldBe(e.Datei);
        (await u.InAsync(d => d.GetRequiredService<IAuditProtokoll>().NeuesteAsync(20))).ShouldContain(x => x.Aktion == "Betrieb.Sicherung");
    }
}
