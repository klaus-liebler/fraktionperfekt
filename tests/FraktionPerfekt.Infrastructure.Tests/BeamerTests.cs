using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Beamer-Ansicht der Sitzung mit Token (Übertragung SchoolCraft, Punkt 8).</summary>
public sealed class BeamerTests
{
    [Fact]
    public async Task Link_nur_fuer_Sitzungsrechte_neuer_sperrt_alten_Ansicht_ohne_Notizen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        BeamerDienst B(IServiceProvider d) => d.GetRequiredService<BeamerDienst>();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var fraktion = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).First(g => g.Kurzname == "Fraktion");
        var termin = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AnlegenAsync(new NeuerTermin("Fraktionssitzung", Terminart.Fraktionssitzung,
            new DateTime(2026, 11, 16, 19, 0, 0), new DateTime(2026, 11, 16, 21, 0, 0), "Fraktionsbüro", fraktion.Id, null)))).Termin;
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().VorbereitenAsync(termin.Id));
        var sitzung = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().ZuTerminAsync(termin.Id)))!;
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().BeginnenAsync(sitzung.Id));
        var tops = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(sitzung.Id)))!.Tops;
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().NotizSetzenAsync(tops[0].Top.Id, "Vertraulich: Personalfrage", true));

        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => B(d).LinkErzeugenAsync(sitzung.Id)))).Message.ShouldContain("Vorstand");

        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var erster = await u.InAsync(d => B(d).LinkErzeugenAsync(sitzung.Id));
        var zweiter = await u.InAsync(d => B(d).LinkErzeugenAsync(sitzung.Id));
        string Token(BeamerLink l) => l.Adresse.Segments[^1];

        // Ohne Anmeldung (Automatik-Bereich): nur mit gültigem Token
        u.Benutzer.Aktuell = Benutzer.Automatik;
        (await u.InAsync(d => B(d).AnsichtAsync(Token(erster)))).ShouldBeNull(); // durch den neuen gesperrt
        var ansicht = (await u.InAsync(d => B(d).AnsichtAsync(Token(zweiter))))!;
        ansicht.Status.ShouldBe(Sitzungsstatus.Laufend);
        ansicht.Tops[0].Status.ShouldBe(Topstatus.Behandelt);
        ansicht.Tops[1].Laufend.ShouldBeTrue(); // erster noch nicht behandelter TOP
        ansicht.Beschlussfaehigkeit!.ShouldContain("von 18 Stimmberechtigten");
        System.Text.Json.JsonSerializer.Serialize(ansicht).ShouldNotContain("Personalfrage");
        (await u.InAsync(d => B(d).AnsichtAsync("falsch"))).ShouldBeNull();

        // Gültig bis Ende des Sitzungstags bzw. mindestens 12 Stunden; danach und nach dem Sperren nicht mehr
        u.Uhr.Advance(TimeSpan.FromHours(48));
        (await u.InAsync(d => B(d).AnsichtAsync(Token(zweiter)))).ShouldBeNull();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var dritter = await u.InAsync(d => B(d).LinkErzeugenAsync(sitzung.Id));
        (await u.InAsync(d => B(d).GueltigBisAsync(sitzung.Id))).ShouldNotBeNull();
        await u.InAsync(d => B(d).SperrenAsync(sitzung.Id));
        (await u.InAsync(d => B(d).AnsichtAsync(Token(dritter)))).ShouldBeNull();
        (await u.InAsync(d => B(d).VorschauAsync(sitzung.Id))).ShouldNotBeNull();
    }
}
