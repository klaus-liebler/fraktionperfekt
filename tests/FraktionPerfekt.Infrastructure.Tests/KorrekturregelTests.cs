using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Finanzen;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>30-Minuten-Korrekturregel (Übertragung SchoolCraft, Punkt 10): Auslagen vor Freigabe, Kommentare, Beschlüsse im Sitzungsmodus.</summary>
public sealed class KorrekturregelTests
{
    [Fact]
    public void Regel_eigene_30_Minuten_ohne_Grund_sonst_mit_Grund()
    {
        var am = new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);
        Korrekturregel.OhneGrund(true, am, am.AddMinutes(30)).ShouldBeTrue();
        Korrekturregel.OhneGrund(true, am, am.AddMinutes(31)).ShouldBeFalse();
        Korrekturregel.OhneGrund(false, am, am).ShouldBeFalse();
        Korrekturregel.OhneGrund(true, null, am).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => Korrekturregel.Pruefen(true, am, am.AddMinutes(31), false, " ", "x")).Message.ShouldContain("Begründung");
        Should.Throw<InvalidOperationException>(() => Korrekturregel.Pruefen(false, am, am, false, "Grund", "die Person")).Message.ShouldContain("die Person");
        Korrekturregel.Pruefen(false, am, am.AddDays(3), true, "Doppelt erfasst", "x");
        Korrekturregel.Grund(null).ShouldStartWith("Fehleingabe");
    }

    [Fact]
    public async Task Auslage_und_Kommentar_zurueckziehen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        FinanzDienst F(IServiceProvider d) => d.GetRequiredService<FinanzDienst>();

        await u.AnmeldenAsync("fbaeumer");
        var a = await u.InAsync(d => F(d).AuslageEinreichenAsync(Kasse.Wahlkampf, new DateOnly(2026, 11, 14), 12.5m, "Kabelbinder", null, null, null));
        var b = await u.InAsync(d => F(d).AuslageEinreichenAsync(Kasse.Wahlkampf, new DateOnly(2026, 11, 14), 8m, "Klebeband", null, null, null));
        (await u.InAsync(d => F(d).AuslagenAsync())).Single(z => z.Auslage.Id == a.Id).OhneGrund.ShouldBeTrue();
        await u.InAsync(d => F(d).AuslageZurueckziehenAsync(a.Id, null));
        var zeile = (await u.InAsync(d => F(d).AuslagenAsync())).Single(z => z.Auslage.Id == a.Id);
        zeile.Auslage.Status.ShouldBe(Auslagenstatus.Zurueckgezogen);
        zeile.Zurueckziehen!.Erlaubt.ShouldBeFalse();
        var offen = await u.InAsync(d => d.GetRequiredService<IDaten>().ListeAsync(d.GetRequiredService<IDaten>().Menge<Aufgabe>()
            .Where(x => x.Anlass == FinanzDienst.AufgabenAnlass && x.AnlassId == a.Id && x.Status == Aufgabenstatus.Offen)));
        offen.ShouldBeEmpty(); // Prüfaufgabe des Kassiers ist geschlossen

        u.Uhr.Advance(TimeSpan.FromMinutes(31));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => F(d).AuslageZurueckziehenAsync(b.Id, null)))).Message.ShouldContain("Begründung");
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => F(d).AuslageZurueckziehenAsync(b.Id, "x")))).Message.ShouldContain("Kassier");
        await u.AnmeldenAsync("djoud", Rolle.Kassier);
        await u.InAsync(d => F(d).AuslageZurueckziehenAsync(b.Id, "Doppelt eingereicht"));
        (await u.InAsync(d => F(d).AuslagenAsync())).Single(z => z.Auslage.Id == b.Id).Auslage.Vermerk.ShouldBe("Doppelt eingereicht");

        // Kommentar an einem Antrag
        await u.AnmeldenAsync("lkuehlert");
        var antrag = await u.InAsync(d => d.GetRequiredService<AntragDienst>().AnlegenAsync("Korrektur", "Text", null, null));
        await u.InAsync(d => d.GetRequiredService<AntragDienst>().KommentierenAsync(antrag.Id, null, "Tippfehler im Kommentar"));
        var k = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!.Kommentare.Single();
        k.OhneGrund.ShouldBeTrue();
        await u.AnmeldenAsync("mgries");
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!.Kommentare.Single().Zurueckziehen!.Erlaubt.ShouldBeFalse();
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => d.GetRequiredService<AntragDienst>().KommentarZurueckziehenAsync(k.Kommentar.Id, null));
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!.Kommentare.Single().Kommentar.ZurueckgezogenAm.ShouldNotBeNull();
    }

    [Fact]
    public async Task Zurueckgezogener_Beschluss_zaehlt_nicht_zur_Beschlusslage()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var fraktion = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).First(g => g.Kurzname == "Fraktion");
        var termin = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AnlegenAsync(new NeuerTermin("Fraktionssitzung", Terminart.Fraktionssitzung,
            new DateTime(2026, 11, 16, 19, 0, 0), new DateTime(2026, 11, 16, 21, 0, 0), "Fraktionsbüro", fraktion.Id, null)))).Termin;
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().VorbereitenAsync(termin.Id));
        var sitzung = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().ZuTerminAsync(termin.Id)))!;
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().BeginnenAsync(sitzung.Id));
        var top = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(sitzung.Id)))!.Tops[0].Top;
        var b = await u.InAsync(d => d.GetRequiredService<SitzungDienst>().BeschlussErfassenAsync(top.Id, "Einzigartiger Korrekturbeschluss", 0, 0, 0));
        b.ErfasstVonPersonId.ShouldNotBeNull();
        (await u.InAsync(d => d.GetRequiredService<BeschlussArchiv>().SuchenAsync("Korrekturbeschluss", null, null, null))).ShouldHaveSingleItem();

        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<SitzungDienst>().BeschlussZurueckziehenAsync(b.Id, "x"))))
            .Message.ShouldContain("Zurückziehen dürfen");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().BeschlussZurueckziehenAsync(b.Id, null));
        (await u.InAsync(d => d.GetRequiredService<BeschlussArchiv>().SuchenAsync("Korrekturbeschluss", null, null, null))).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().ProtokolltextAsync(sitzung.Id))).ShouldNotContain("Korrekturbeschluss");
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(sitzung.Id)))!.Tops[0].Beschluesse.Single().Korrekturgrund!.ShouldStartWith("Fehleingabe");
    }
}
