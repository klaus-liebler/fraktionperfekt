using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Finanzen;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Kampagnen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Kampagnen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SBenutzer = FraktionPerfekt.Application.Sicherheit.Benutzer;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class KampagnenFinanzenTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task Kampagne_Plakat_am_Handy_Abhaengliste_Gebiete_Termin_mit_Vorlage_ToDos_Einbuchen_und_Tickets()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        KampagnenDienst W(IServiceProvider d) => d.GetRequiredService<KampagnenDienst>();
        await u.AnmeldenAsync("fbaeumer");
        var wahl = (await u.InAsync(d => W(d).KampagnenAsync())).Single(z => z.Kampagne.Name.StartsWith("Infostände", StringComparison.Ordinal)).Kampagne;
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).PlakatErfassenAsync(null, 52.1, 7.6, "x", Plakatart.Hohlkammer, Plakatstatus.Haengt, null,
            new MemoryStream([1]), "virus.exe")))).Message.ShouldContain("Foto");
        var neu = await u.InAsync(d => W(d).PlakatErfassenAsync(null, 52.0918, 7.6085, "Marktplatz Nord", Plakatart.Hohlkammer, Plakatstatus.Haengt, null, new MemoryStream(Png), "IMG_1.jpg"));
        neu.KampagneId.ShouldBe(wahl.Id); // Handy: laufende Kampagne
        (await u.InAsync(d => W(d).FotoAsync(neu.Id))).ShouldNotBeNull();

        // Abhängliste vom Bahnhof aus: das Bahnhofsplakat zuerst, Reckenfeld (5 km) zuletzt, abgehängte fehlen.
        var liste = await u.InAsync(d => W(d).AbhaenglisteAsync(wahl.Id, 52.0965, 7.6159));
        liste.Plakate[0].Plakat.Standort.ShouldStartWith("Bahnhof");
        liste.Plakate[^1].Plakat.Standort.ShouldStartWith("Reckenfeld");
        liste.Plakate.ShouldAllBe(z => z.Plakat.HaengtNoch);
        await u.InAsync(d => W(d).StatusSetzenAsync(liste.Plakate[0].Plakat.Id, Plakatstatus.Abgehaengt));
        (await u.InAsync(d => W(d).AbhaenglisteAsync(wahl.Id, 52.0965, 7.6159))).Plakate.Count.ShouldBe(liste.Plakate.Count - 1);

        // Beschädigt → Ticket mit Plakatbezug
        await u.InAsync(d => W(d).StatusSetzenAsync(neu.Id, Plakatstatus.Beschaedigt));
        (await u.InAsync(d => W(d).TicketsAsync(wahl.Id))).ShouldContain(t => t.Ticket.PlakatId == neu.Id && t.Ticket.Prioritaet == Ticketprioritaet.Hoch);

        // Gebiete: zuteilen nur Vorstand/Geschäftsführung, abhaken die zugeteilte Person.
        var gimbte = (await u.InAsync(d => W(d).GebieteAsync(wahl.Id))).Single(z => z.Gebiet.Name == "Gimbte");
        gimbte.Zuteilen.Erlaubt.ShouldBeFalse();
        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        await u.InAsync(d => W(d).ZuteilenAsync(gimbte.Gebiet.Id, u.Benutzer.Aktuell.PersonId));

        // Termin mit Vorlage-ToDos, 1 Platz
        var vorlage = (await u.InAsync(d => W(d).VorlagenAsync())).Single(v => v.Vorlage.Name == "Infostand");
        var termin = await u.InAsync(d => W(d).TerminAnlegenAsync(wahl.Id, "Infostand Bahnhof", "Bahnhof", new DateTime(2026, 11, 20, 16, 0, 0), new DateTime(2026, 11, 20, 18, 0, 0), 1,
            null, vorlage.Vorlage.Id));
        var todos = await u.InAsync(d => W(d).TodosAsync(termin.Id));
        todos.Select(t => t.Todo.Text).ShouldBe(vorlage.Punkte.Select(p => p.Text));
        await u.InAsync(d => W(d).TodoUmschaltenAsync(todos[0].Todo.Id));
        (await u.InAsync(d => W(d).TodosAsync(termin.Id)))[0].ErledigtVon.ShouldNotBeNull();

        var baeumer = await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).GebieteAsync(wahl.Id))).Single(z => z.Gebiet.Name == "Gimbte").Erledigen.Begruendung!.ShouldContain("zugeteilt");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).TerminAnlegenAsync(wahl.Id, "x", null, DateTime.Today.AddDays(1), DateTime.Today.AddDays(1).AddHours(1), null, null, null))))
            .Message.ShouldContain("Vorstand");
        (await u.InAsync(d => W(d).EinbuchenAsync(termin.Id))).ShouldBeTrue();
        (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20), new Kalenderfilter(PersonId: baeumer.Id))))
            .ShouldContain(z => z.Termin.Id == termin.Id); // im eigenen Kalender
        await u.AnmeldenAsync("nfeldhaus");
        var voll = (await u.InAsync(d => W(d).TermineAsync(wahl.Id))).Single(z => z.Termin.Id == termin.Id);
        voll.Einbuchen.Begruendung!.ShouldContain("vergeben");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).EinbuchenAsync(termin.Id)));
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<KalenderDienst>().AntwortenAsync(termin.Id, true)));

        // Ticket an Felix: Benachrichtigung, Kommentar, „meine“
        var ticket = await u.InAsync(d => W(d).TicketAnlegenAsync(wahl.Id, new NeuesTicket("Kabelbinder fehlen", "Bitte **200** Stück.", Ticketprioritaet.Normal, baeumer.Id, null, TerminId: termin.Id)));
        await u.InAsync(d => W(d).KommentierenAsync(ticket.Id, "Bringe ich mit."));
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).TicketsAsync(wahl.Id, nurMeine: true))).ShouldContain(t => t.Ticket.Id == ticket.Id && t.Kommentare == 1 && t.Bezug!.StartsWith("Termin", StringComparison.Ordinal));
        await u.InAsync(d => W(d).TicketStatusAsync(ticket.Id, Ticketstatus.Erledigt));
        (await u.InAsync(d => W(d).TicketAsync(ticket.Id)))!.Zeile.Ticket.Offen.ShouldBeFalse();
    }

    [Fact]
    public async Task Fristen_nach_dem_Wahltag_der_Kampagne_mit_Loeschfrist()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        KampagnenDienst W(IServiceProvider d) => d.GetRequiredService<KampagnenDienst>();
        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        var wahl = (await u.InAsync(d => W(d).KampagnenAsync())).Single(z => z.Kampagne.Name.StartsWith("Infostände", StringComparison.Ordinal)).Kampagne;

        // Wahl war am 10.11.: Aufgabe „Plakate abhängen“ für den Vorstand, nur einmal.
        await u.InAsync(d => W(d).KampagneAendernAsync(wahl.Id, wahl.Name, wahl.Beschreibung, wahl.Beginn, null, new DateOnly(2026, 11, 10), Kampagnenstatus.Aktiv));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBe(1);
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBe(0);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldContain(a => a.Anlass == KampagnenDienst.AbhaengAnlass);
        (await u.InAsync(d => W(d).AbhaenglisteAsync(wahl.Id))).NachDerWahl.ShouldBeTrue();
        (await u.InAsync(d => W(d).NaechsteSchritteAsync(wahl.Id)))[0].Titel.ShouldContain("abhängen");

        // Ein halbes Jahr später: abgehängte Plakate sind gelöscht.
        var vorher = (await u.InAsync(d => W(d).PlakateAsync(wahl.Id))).Count;
        u.Uhr.Advance(TimeSpan.FromDays(200));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBeGreaterThan(1);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var danach = await u.InAsync(d => W(d).PlakateAsync(wahl.Id));
        danach.Count.ShouldBe(vorher - 1);
        danach.ShouldNotContain(z => z.Plakat.Status == Plakatstatus.Abgehaengt);
    }

    [Fact]
    public async Task Finanzen_Kassier_bucht_Spendenregeln_Auslage_mit_Beleg_vier_Augen_und_Kassenbericht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        FinanzDienst F(IServiceProvider d) => d.GetRequiredService<FinanzDienst>();
        AufgabenDienst T(IServiceProvider d) => d.GetRequiredService<AufgabenDienst>();
        var jahr = 2026;

        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => F(d).UebersichtAsync(Kasse.Fraktion, jahr)))).Message.ShouldContain("Kassier");
        var auslage = await u.InAsync(d => F(d).AuslageEinreichenAsync(Kasse.Fraktion, new DateOnly(2026, 11, 14), 42.10m, "Raummiete Bürgersprechstunde", null,
            new MemoryStream(Png), "beleg.jpg"));
        var ohneBeleg = await u.InAsync(d => F(d).AuslageEinreichenAsync(Kasse.Fraktion, new DateOnly(2026, 11, 14), 5m, "Parkticket", null, null, null));
        (await u.InAsync(d => F(d).BelegAsync(auslage.Id))).ShouldNotBeNull();
        await u.AnmeldenAsync("thorstmann");
        (await u.InAsync(d => F(d).BelegAsync(auslage.Id))).ShouldBeNull();
        (await u.InAsync(d => F(d).AuslagenAsync())).ShouldNotContain(z => z.Auslage.Id == auslage.Id);

        // Kassier: Aufgabe im Korb, ohne Beleg keine Freigabe, freigeben, auszahlen → Buchung
        var kassier = await u.AnmeldenAsync("djoud", Rolle.Kassier);
        (await u.InAsync(d => T(d).KorbAsync())).ShouldContain(a => a.AnlassId == auslage.Id);
        (await u.InAsync(d => F(d).AuslagenAsync())).Single(z => z.Auslage.Id == ohneBeleg.Id).Entscheiden.Begruendung!.ShouldContain("Beleg");
        await u.InAsync(d => F(d).AuslageEntscheidenAsync(auslage.Id, true, null));
        await u.InAsync(d => F(d).AuszahlenAsync(auslage.Id));
        var uebersicht = await u.InAsync(d => F(d).UebersichtAsync(Kasse.Fraktion, jahr));
        uebersicht.Buchungen.ShouldContain(b => b.Quelle == Buchungsquelle.Auslage && b.QuelleId == auslage.Id && b.Betrag == 42.10m);
        (await u.InAsync(d => T(d).KorbAsync())).ShouldNotContain(a => a.AnlassId == auslage.Id);

        // Spenden: anonym über 500 € unzulässig, sonst Einnahme in der Wahlkampfkasse
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => F(d).SpendeErfassenAsync(new DateOnly(2026, 11, 15), null, null, 600m, "Wahlkampf"))))
            .Message.ShouldContain("§ 25 PartG");
        Should.Throw<InvalidOperationException>(() => new Spende(Kasse.Fraktion, new DateOnly(2026, 11, 15), "X", null, 10m, "x", Guid.NewGuid()));
        var spende = await u.InAsync(d => F(d).SpendeErfassenAsync(new DateOnly(2026, 11, 15), "Max Beispiel (fiktiv)", "Greven", 400m, "Wahlkampf"));
        spende.QuittungFaellig.ShouldBeTrue();
        var wk = await u.InAsync(d => F(d).UebersichtAsync(Kasse.Wahlkampf, jahr));
        wk.Buchungen.ShouldContain(b => b.Quelle == Buchungsquelle.Spende && b.QuelleId == spende.Id);

        // Budget: Ausgabe auf Posten erhöht das Ist
        var posten = wk.Posten.Single(p => p.Posten.Bezeichnung == "Plakate und Material");
        await u.InAsync(d => F(d).BuchenAsync(Kasse.Wahlkampf, new DateOnly(2026, 11, 15), false, 100m, posten.Posten.Id, "Kleister"));
        (await u.InAsync(d => F(d).UebersichtAsync(Kasse.Wahlkampf, jahr))).Posten.Single(p => p.Posten.Id == posten.Posten.Id).Ist.ShouldBe(posten.Ist + 100m);

        // Eigene Auslage des Kassiers gibt der Vorstand frei
        var eigene = await u.InAsync(d => F(d).AuslageEinreichenAsync(Kasse.Wahlkampf, new DateOnly(2026, 11, 15), 12m, "Porto", null, new MemoryStream(Png), "porto.png"));
        (await u.InAsync(d => F(d).AuslagenAsync())).Single(z => z.Auslage.Id == eigene.Id).Entscheiden.Begruendung!.ShouldContain("Vier-Augen");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => F(d).AuslageEntscheidenAsync(eigene.Id, true, null));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => F(d).AuszahlenAsync(eigene.Id)))).Message.ShouldContain("Kassier");

        var bericht = await u.InAsync(d => F(d).KassenberichtAsync(Kasse.Wahlkampf, jahr));
        bericht.ShouldContain("Ausgaben je Posten");
        bericht.ShouldContain("Plakate und Material");
        bericht.ShouldContain("Spenden");
        _ = kassier;
    }
}
