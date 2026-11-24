using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Infrastructure.Betrieb;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Übungsinstanz zurücksetzen: frische Demodaten über die Übungsdatenbank, echte Datenbank unberührt.</summary>
public sealed class UebungsinstanzTests
{
    [Fact]
    public async Task Zuruecksetzen_ersetzt_nur_die_Uebungsdatenbank()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var datei = Path.Combine(Path.GetDirectoryName(u.Datenbankdatei)!, "uebung.db");
        var uebung = new MandantOptionen { Kennung = "uebung", Uebung = true, Datenbank = $"Data Source={datei};Pooling=False" };
        async Task<int> PersonenAsync(MandantOptionen? m) => await u.InAsync(async d =>
        {
            if (m is not null)
            {
                d.GetRequiredService<MandantAuswahl>().Festlegen(m);
            }

            return (await d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync()).Count;
        });

        var echtVorher = await PersonenAsync(null);
        await Uebungsinstanz.NeuAufbauenAsync(u.Dienste, uebung, async d => await d.GetRequiredService<Demodaten>().AnlegenAsync(), TestContext.Current.CancellationToken);
        var nachErstemAufbau = await PersonenAsync(uebung);
        nachErstemAufbau.ShouldBeGreaterThan(20);

        // In der Übung wird etwas verändert; Zurücksetzen bringt den Ausgangsstand
        await u.InAsync(async d =>
        {
            d.GetRequiredService<MandantAuswahl>().Festlegen(uebung);
            var s = d.GetRequiredService<IStammdatenSpeicher>();
            s.Hinzufuegen(new Domain.Personen.Person("Übung", "Teilnehmerin", null, "uebung1", u.Uhr.GetUtcNow()));
            await s.SpeichernAsync();
        });
        (await PersonenAsync(uebung)).ShouldBe(nachErstemAufbau + 1);
        await Uebungsinstanz.NeuAufbauenAsync(u.Dienste, uebung, async d => await d.GetRequiredService<Demodaten>().AnlegenAsync(), TestContext.Current.CancellationToken);
        (await PersonenAsync(uebung)).ShouldBe(nachErstemAufbau);
        (await PersonenAsync(null)).ShouldBe(echtVorher);

        await Should.ThrowAsync<InvalidOperationException>(() => Uebungsinstanz.NeuAufbauenAsync(u.Dienste, new MandantOptionen(), _ => Task.CompletedTask));
    }
}
