using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Finanzen;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Wahlkampf;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wahlkampf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SBenutzer = FraktionPerfekt.Application.Sicherheit.Benutzer;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class WahlkampfFinanzenTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task Plakat_am_Handy_mit_Foto_Abhaengliste_nach_Entfernung_Gebiete_und_Schichten()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        WahlkampfDienst W(IServiceProvider d) => d.GetRequiredService<WahlkampfDienst>();
        await u.AnmeldenAsync("fbaeumer");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).PlakatErfassenAsync(52.1, 7.6, "x", Plakatart.Hohlkammer, Plakatstatus.Haengt, null,
            new MemoryStream([1]), "virus.exe")))).Message.ShouldContain("Foto");
        var neu = await u.InAsync(d => W(d).PlakatErfassenAsync(52.0918, 7.6085, "Marktplatz Nord", Plakatart.Hohlkammer, Plakatstatus.Haengt, null, new MemoryStream(Png), "IMG_1.jpg"));
        (await u.InAsync(d => W(d).FotoAsync(neu.Id))).ShouldNotBeNull();

        // Abhängliste vom Bahnhof aus: das Bahnhofsplakat zuerst, Reckenfeld (5 km) zuletzt, abgehängte fehlen.
        var liste = await u.InAsync(d => W(d).AbhaenglisteAsync(52.0965, 7.6159));
        liste.Plakate[0].Plakat.Standort.ShouldStartWith("Bahnhof");
        liste.Plakate[^1].Plakat.Standort.ShouldStartWith("Reckenfeld");
        liste.Plakate.ShouldAllBe(z => z.Plakat.HaengtNoch);
        await u.InAsync(d => W(d).StatusSetzenAsync(liste.Plakate[0].Plakat.Id, Plakatstatus.Abgehaengt));
        (await u.InAsync(d => W(d).AbhaenglisteAsync(52.0965, 7.6159))).Plakate.Count.ShouldBe(liste.Plakate.Count - 1);

        // Gebiete: zuteilen nur Vorstand/Geschäftsführung, abhaken die zugeteilte Person.
        var gimbte = (await u.InAsync(d => W(d).GebieteAsync())).Single(z => z.Gebiet.Name == "Gimbte");
        gimbte.Zuteilen.Erlaubt.ShouldBeFalse();
        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        await u.InAsync(d => W(d).ZuteilenAsync(gimbte.Gebiet.Id, u.Benutzer.Aktuell.PersonId));
        var baeumer = await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).GebieteAsync())).Single(z => z.Gebiet.Name == "Gimbte").Erledigen.Begruendung!.ShouldContain("zugeteilt");
        await u.InAsync(async d => await W(d).GebietErledigenAsync((await W(d).GebieteAsync()).Single(z => z.Gebiet.Name == "Greven-Innenstadt").Gebiet.Id, "alle hängen"));

        // Schichten: eintragen bis voll, austragen.
        var schicht = (await u.InAsync(d => W(d).SchichtenAsync())).First(z => z.Frei == 1);
        schicht.Ich.ShouldBeTrue(); // Felix Bäumer steht in den Demodaten schon drin
        (await u.InAsync(d => W(d).EintragenAsync(schicht.Schicht.Id))).ShouldBeFalse(); // erneuter Klick trägt aus
        _ = baeumer;
    }

    [Fact]
    public async Task Schicht_voll_und_Fristen_nach_der_Wahl_mit_Loeschfrist()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        WahlkampfDienst W(IServiceProvider d) => d.GetRequiredService<WahlkampfDienst>();
        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        var s = await u.InAsync(d => W(d).SchichtAnlegenAsync("Infostand Bahnhof", new DateTime(2026, 11, 20, 16, 0, 0), new DateTime(2026, 11, 20, 18, 0, 0), 1, null));
        await u.AnmeldenAsync("nfeldhaus");
        (await u.InAsync(d => W(d).EintragenAsync(s.Id))).ShouldBeTrue();
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).SchichtenAsync())).Single(z => z.Schicht.Id == s.Id).Eintragen.Begruendung!.ShouldContain("voll");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).EintragenAsync(s.Id)));

        // Wahl war am 10.11.: Aufgabe „Plakate abhängen“ für den Vorstand, nur einmal.
        u.Dienste.GetRequiredService<IOptions<FraktionOptionen>>().Value.Wahltag = new DateOnly(2026, 11, 10);
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBe(1);
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBe(0);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldContain(a => a.Anlass == WahlkampfDienst.AbhaengAnlass);
        (await u.InAsync(d => W(d).AbhaenglisteAsync())).NachDerWahl.ShouldBeTrue();

        // Ein halbes Jahr später: abgehängte Plakate und alte Schichteinträge sind gelöscht.
        var vorher = (await u.InAsync(d => W(d).PlakateAsync())).Count;
        u.Uhr.Advance(TimeSpan.FromDays(200));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => W(d).FristenPruefenAsync())).ShouldBeGreaterThan(1);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var danach = await u.InAsync(d => W(d).PlakateAsync());
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
