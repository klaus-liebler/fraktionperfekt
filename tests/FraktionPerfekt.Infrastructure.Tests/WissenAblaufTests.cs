using System.Text;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wissen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class WissenAblaufTests
{
    [Fact]
    public async Task Dateien_Rechte_nach_Gremium_vertraulich_Typen_und_Loeschen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        DateiDienst D(IServiceProvider d) => d.GetRequiredService<DateiDienst>();

        // Vorstand sieht im HFA-Ordner auch die vertrauliche Stellungnahme; eine sachkundige Bürgerin ohne HFA-Mandat nicht.
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        (await u.InAsync(d => D(d).ListeAsync("Gremien/HFWA"))).Select(z => z.Datei.Name).ShouldContain("Stellungnahme Stadtwerke Bad.txt");
        await u.AnmeldenAsync("uahrens");
        var hfa = await u.InAsync(d => D(d).ListeAsync("Gremien/HFWA"));
        hfa.Select(z => z.Datei.Name).ShouldContain("Haushalt 2026 – Eckdaten.csv");
        hfa.Select(z => z.Datei.Name).ShouldNotContain("Stellungnahme Stadtwerke Bad.txt");

        // Das Kampagnenteam sieht nur die Kampagnen-Ordner.
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => D(d).OrdnerAsync())).Select(o => o.Pfad).ShouldBe(["Kampagnen"]);
        (await u.InAsync(d => D(d).ListeAsync("Fraktion"))).ShouldBeEmpty();

        // Hochladen: Typprüfung, Anhang an einem Bezug, Download liefert den Inhalt, Löschen nur durch Hochladende.
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => D(d).HochladenAsync("Fraktion", "virus.exe", new MemoryStream([1, 2]), null, false))))
            .Message.ShouldContain("nicht erlaubt");
        var antragId = Guid.NewGuid();
        var datei = await u.InAsync(d => D(d).HochladenAsync("Anträge", "..\\..\\Begründung.txt", new MemoryStream(Encoding.UTF8.GetBytes("Elternbefragung")), null, false,
            "Antrag", antragId));
        datei.Name.ShouldBe("Begründung.txt");
        (await u.InAsync(d => D(d).ListeAsync(null, "Antrag", antragId))).Single().Datei.Id.ShouldBe(datei.Id);
        var geoeffnet = await u.InAsync(async d =>
        {
            var o = (await D(d).OeffnenAsync(datei.Id))!.Value;
            using var leser = new StreamReader(o.Inhalt);
            return await leser.ReadToEndAsync();
        });
        geoeffnet.ShouldBe("Elternbefragung");

        await u.AnmeldenAsync("thorstmann");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => D(d).LoeschenAsync(datei.Id)))).Message.ShouldContain("hochgeladen");
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => D(d).LoeschenAsync(datei.Id));
        (await u.InAsync(d => D(d).OeffnenAsync(datei.Id))).ShouldBeNull();
    }

    [Fact]
    public async Task Wiki_Versionen_Suche_Verweise_und_Dubletten_beim_Anlegen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        WikiDienst W(IServiceProvider d) => d.GetRequiredService<WikiDienst>();
        await u.AnmeldenAsync("lkuehlert");

        (await u.InAsync(d => W(d).SuchenAsync("Grundschule Ganztag"))).Select(t => t.Seite.Titel).ShouldContain("Grundschullandschaft");
        (await u.InAsync(d => W(d).SuchenAsync("Stellenplan"))).ShouldBeEmpty(); // vertraulich, kein Gremium
        var seite = (await u.InAsync(d => W(d).SeiteAsync("Fraktionssitzung")))!;
        seite.Rueckverweise.Select(s => s.Titel).ShouldContain("Antrag stellen");

        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).AnlegenAsync("Abläufe", "fraktions-sitzung", "Leer", false, null))))
            .Message.ShouldContain("gibt es schon");
        var neu = await u.InAsync(d => W(d).AnlegenAsync("Politikfelder", "Badsanierung", "Politikfeld", false, null));
        await u.InAsync(d => W(d).SpeichernAsync(neu.Id, "Die Stadtwerke wollen das Bad eigenständig sanieren. Siehe [[Haushaltsberatungen]] und [[Bäderkonzept]].", "Erster Stand"));
        var detail = (await u.InAsync(d => W(d).SeiteAsync("Badsanierung")))!;
        detail.Versionen.Count.ShouldBe(2);
        detail.FehlendeVerweise.ShouldBe(["Bäderkonzept"]);
        (await u.InAsync(d => W(d).SuchenAsync("eigenständig"))).Single().Seite.Id.ShouldBe(neu.Id);
        (await u.InAsync(d => W(d).SeiteAsync("Haushaltsberatungen")))!.Rueckverweise.Select(s => s.Titel).ShouldContain("Badsanierung");

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).SeiteAsync("Badsanierung")))!.Bearbeiten.Erlaubt.ShouldBeFalse();
    }

    [Fact]
    public async Task KI_schlaegt_nur_vor_Mensch_nimmt_an_vertrauliches_bleibt_unsichtbar()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        WikiDienst W(IServiceProvider d) => d.GetRequiredService<WikiDienst>();

        // Die KI sieht keine vertraulichen Seiten.
        u.Benutzer.Aktuell = new Benutzer(WikiDienst.KiKonto, "KI", null);
        (await u.InAsync(d => W(d).KiSeitenAsync())).Select(s => s.Titel).ShouldNotContain("Personalfragen der Fraktion");
        (await u.InAsync(d => W(d).SeitenAsync())).Select(s => s.Seite.Titel).ShouldNotContain("Personalfragen der Fraktion");

        // Ein Mitglied darf die Prüfung nicht anstoßen; der Vorstand schon (oder nachts die Automatik).
        await u.AnmeldenAsync("lkuehlert");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).KiPruefenAsync()));
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var anzahl = await u.InAsync(d => W(d).KiPruefenAsync());
        anzahl.ShouldBeGreaterThan(3);
        var vorschlaege = await u.InAsync(d => W(d).VorschlaegeAsync());
        vorschlaege.ShouldContain(z => z.Vorschlag.Art == Vorschlagsart.Dublette && z.Seite.Titel == "Presse-Arbeit");
        vorschlaege.ShouldContain(z => z.Vorschlag.Art == Vorschlagsart.Veraltet && z.Seite.Titel == "Haushaltsberatungen");
        vorschlaege.ShouldContain(z => z.Vorschlag.Art == Vorschlagsart.Luecke && z.Seite.Titel == "Grundschullandschaft");
        vorschlaege.ShouldContain(z => z.Vorschlag.Art == Vorschlagsart.Verwaist && z.Seite.Titel == "Radverkehr Rathausstraße");
        vorschlaege.ShouldNotContain(z => z.Seite.Vertraulich);
        (await u.InAsync(d => W(d).KiPruefenAsync())).ShouldBe(0); // keine doppelten offenen Vorschläge

        // Nichts ist geändert, bis ein Mensch annimmt; danach neue Version ohne Person, mit Verweis auf den Vorschlag.
        var veraltet = vorschlaege.Single(z => z.Vorschlag.Art == Vorschlagsart.Veraltet && z.Seite.Titel == "Haushaltsberatungen");
        (await u.InAsync(d => W(d).SeiteAsync("Haushaltsberatungen")))!.Seite.Version.ShouldBe(1);
        await u.AnmeldenAsync("lkuehlert");
        (await u.InAsync(d => W(d).VorschlaegeAsync())).First().Entscheiden.Erlaubt.ShouldBeFalse();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => W(d).VorschlagEntscheidenAsync(veraltet.Vorschlag.Id, true));
        var haushalt = (await u.InAsync(d => W(d).SeiteAsync("Haushaltsberatungen")))!;
        haushalt.Aktuell.Nummer.ShouldBe(2);
        haushalt.Aktuell.PersonId.ShouldBeNull();
        haushalt.Aktuell.VorschlagId.ShouldBe(veraltet.Vorschlag.Id);
        haushalt.Aktuell.Text.ShouldContain("Angaben aus 2024 prüfen");

        // Vorschlag auf alter Version: Annehmen gesperrt mit Begründung.
        var luecke = vorschlaege.Single(z => z.Vorschlag.Art == Vorschlagsart.Luecke && z.Seite.Titel == "Grundschullandschaft");
        await u.InAsync(d => W(d).SpeichernAsync(luecke.Seite.Id, luecke.AktuellerText + "\n\nErgänzt von Hand.", null));
        var gesperrt = (await u.InAsync(d => W(d).VorschlaegeAsync())).Single(z => z.Vorschlag.Id == luecke.Vorschlag.Id);
        gesperrt.Veraltet.ShouldBeTrue();
        gesperrt.Entscheiden.Begruendung!.ShouldContain("älter");

        // Frage mit Quellen
        var antwort = await u.InAsync(d => W(d).FragenAsync("Fristen für Anträge"));
        antwort.Quellen.Select(q => q.Titel).ShouldContain("Antrag stellen");
    }
}
