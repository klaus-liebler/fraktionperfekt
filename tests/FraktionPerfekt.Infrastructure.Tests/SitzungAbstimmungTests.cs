using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Abstimmen im Sitzungsmodus (Übertragung SchoolCraft, Punkt 7): Beschlussfähigkeit, geheim abstimmen nur für Anwesende, Beschluss.</summary>
public sealed class SitzungAbstimmungTests
{
    private static SitzungDienst S(IServiceProvider d) => d.GetRequiredService<SitzungDienst>();

    [Fact]
    public async Task Geheim_abstimmen_nur_Anwesende_Ergebnis_wird_Beschluss()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var fraktion = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).First(g => g.Kurzname == "Fraktion");
        var termin = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AnlegenAsync(new NeuerTermin("Fraktionssitzung", Terminart.Fraktionssitzung,
            new DateTime(2026, 11, 30, 19, 0, 0), new DateTime(2026, 11, 30, 21, 0, 0), "Fraktionsbüro", fraktion.Id, null)))).Termin;
        await u.InAsync(d => S(d).VorbereitenAsync(termin.Id));
        var sitzung = (await u.InAsync(d => S(d).ZuTerminAsync(termin.Id)))!;
        var detail = (await u.InAsync(d => S(d).DetailAsync(sitzung.Id)))!;
        var top = detail.Tops[0].Top;
        detail.GeheimAbstimmen!.Erlaubt.ShouldBeFalse(); // erst in der laufenden Sitzung

        await u.InAsync(d => S(d).BeginnenAsync(sitzung.Id));
        var anwesend = new[] { "lkuehlert", "mgries", "bkurney", "kschwenken" };
        var personen = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync());
        foreach (var a in (await u.InAsync(d => S(d).DetailAsync(sitzung.Id)))!.Anwesenheit)
        {
            var kennung = personen.Single(p => p.Id == a.PersonId).Benutzerkennung;
            await u.InAsync(d => S(d).AnwesenheitSetzenAsync(sitzung.Id, a.PersonId, anwesend.Contains(kennung) ? Anwesenheitsstatus.Anwesend : Anwesenheitsstatus.Entschuldigt));
        }

        detail = (await u.InAsync(d => S(d).DetailAsync(sitzung.Id)))!;
        detail.Beschlussfaehigkeit!.Anwesend.ShouldBe(4);
        detail.Beschlussfaehigkeit.Beschlussfaehig.ShouldBeFalse();
        detail.Beschlussfaehigkeit.Text.ShouldContain("Nicht beschlussfähig: 4 von 18");

        // Mitglied darf nicht starten; Vorstand startet für die vier Anwesenden
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => S(d).GeheimAbstimmenAsync(top.Id, "Zustimmung")))).Message.ShouldContain("Vorstand");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var umfrage = await u.InAsync(d => S(d).GeheimAbstimmenAsync(top.Id, "Zustimmung zur Vorlage"));
        umfrage.Geheim.ShouldBeTrue();
        umfrage.Berechtigte.ShouldBe(4);
        (umfrage.Frist - umfrage.ErstelltAm).TotalMinutes.ShouldBe(10, 1);
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => S(d).GeheimAbstimmenAsync(top.Id, "Noch eine")))).Message.ShouldContain("läuft schon");

        // Abwesende stimmen nicht mit
        await u.AnmeldenAsync("seisenrichter");
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage.Id)))!.Abstimmen.Erlaubt.ShouldBeFalse();

        foreach (var (k, wahl) in new[] { ("lkuehlert", "Ja"), ("mgries", "Ja"), ("bkurney", "Nein"), ("kschwenken", "Ja") })
        {
            await u.AnmeldenAsync(k);
            var o = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage.Id)))!.Optionen.Single(x => x.Text == wahl).Id;
            await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrage.Id, [o]));
        }

        // Alle Anwesenden haben abgestimmt → Ende → Beschluss im Protokoll
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        detail = (await u.InAsync(d => S(d).DetailAsync(sitzung.Id)))!;
        var b = detail.Tops[0].Beschluesse.ShouldHaveSingleItem();
        b.Ergebnis.ShouldBe(Beschlussergebnis.Angenommen);
        b.Stimmen.ShouldBe("3 ja, 1 nein, 0 Enthaltung");
        b.Text.ShouldContain("Geheim abgestimmt");
        detail.Abstimmungen!.Single().Umfrage.Status.ShouldBe(Umfragestatus.Abgeschlossen);
        (await u.InAsync(d => S(d).AbstimmungenUebernehmenAsync())).ShouldBe(0); // nur einmal übernommen

        // Rückfall Stimmzettel: Plausibilität und Vermerk
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => S(d).BeschlussErfassenAsync(top.Id, "Stimmzettel", 3, 2, 0, stimmzettel: true))))
            .Message.ShouldContain("nur 4 anwesende");
        (await u.InAsync(d => S(d).BeschlussErfassenAsync(top.Id, "Stimmzettel", 2, 1, 1, stimmzettel: true))).Text.ShouldEndWith("(geheim mit Stimmzetteln abgestimmt)");
    }
}
