using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Wahlen und Ämter mit Amtszeit (Übertragung SchoolCraft, Punkt 6).</summary>
public sealed class WahlAblaufTests
{
    private static WahlDienst W(IServiceProvider d) => d.GetRequiredService<WahlDienst>();

    private static async Task StimmeAsync(TestUmgebung u, Guid umfrageId, string kennung, string option)
    {
        await u.AnmeldenAsync(kennung);
        var o = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrageId)))!.Optionen.Single(x => x.Text.Contains(option, StringComparison.Ordinal)).Id;
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrageId, [o]));
    }

    [Fact]
    public async Task Geheime_Wahl_mit_Stichwahl_Annahme_setzt_Rolle_und_Funktion_ab_Datum()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var ab = new DateOnly(2026, 12, 1);
        var waehler = new List<Guid>();
        foreach (var k in new[] { "lkuehlert", "mgries", "bkurney", "kschwenken", "seisenrichter" })
        {
            waehler.Add((await u.AnmeldenAsync(k)).Id);
        }

        // Nur der Vorstand legt Wahlen an
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).AnlegenAsync(Fraktionsamt.Vorsitz, null, null, ab, null)))).Message.ShouldContain("Vorstand");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var wahl = await u.InAsync(d => W(d).AnlegenAsync(Fraktionsamt.Vorsitz, null, null, ab, null));
        wahl.Geheim.ShouldBeTrue();
        wahl.AmtBis.ShouldBe(new DateOnly(2030, 10, 31));

        // Kandidaturen: selbst und durch den Vorstand; Sachkundige und Kampagnenteam nicht
        var kuehlert = await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => W(d).KandidaturAsync(wahl.Id, kuehlert.Id));
        var gries = await u.AnmeldenAsync("mgries");
        await u.AnmeldenAsync("uahrens");
        (await u.InAsync(d => W(d).DetailAsync(wahl.Id)))!.Kandidieren.Begruendung!.ShouldContain("Ratsmitglieder");
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => W(d).DetailAsync(wahl.Id))).ShouldBeNull();
        (await u.InAsync(d => W(d).ListeAsync())).ShouldBeEmpty();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => W(d).KandidaturAsync(wahl.Id, gries.Id));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => W(d).KandidaturAsync(wahl.Id, gries.Id)))).Message.ShouldContain("kandidiert bereits");

        // 1. Wahlgang: 2 : 2 : 1 Enthaltung → keine Mehrheit → Stichwahl
        var gang1 = await u.InAsync(d => W(d).WahlgangStartenAsync(wahl.Id, u.Uhr.GetUtcNow().AddHours(1), waehler));
        gang1.Geheim.ShouldBeTrue();
        await StimmeAsync(u, gang1.Id, "lkuehlert", "Kühlert");
        await StimmeAsync(u, gang1.Id, "mgries", "Gries");
        await StimmeAsync(u, gang1.Id, "bkurney", "Kühlert");
        await StimmeAsync(u, gang1.Id, "kschwenken", "Gries");
        await StimmeAsync(u, gang1.Id, "seisenrichter", "Enthaltung"); // alle haben abgestimmt → Ende
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var d1 = (await u.InAsync(d => W(d).DetailAsync(wahl.Id)))!;
        d1.Wahl.Status.ShouldBe(Wahlstatus.Stichwahl);
        d1.Wahl.Ergebnis!.ShouldContain("Stichwahl zwischen");

        // Stichwahl 3 : 2
        var gang2 = await u.InAsync(d => W(d).WahlgangStartenAsync(wahl.Id, u.Uhr.GetUtcNow().AddHours(1), waehler));
        gang2.Titel.ShouldContain("Stichwahl");
        await StimmeAsync(u, gang2.Id, "lkuehlert", "Kühlert");
        await StimmeAsync(u, gang2.Id, "mgries", "Gries");
        await StimmeAsync(u, gang2.Id, "bkurney", "Kühlert");
        await StimmeAsync(u, gang2.Id, "kschwenken", "Gries");
        await StimmeAsync(u, gang2.Id, "seisenrichter", "Kühlert");
        await u.AnmeldenAsync("mgries");
        var d2 = (await u.InAsync(d => W(d).DetailAsync(wahl.Id)))!;
        d2.Wahl.Status.ShouldBe(Wahlstatus.Gewaehlt);
        d2.Wahl.GewaehltPersonId.ShouldBe(kuehlert.Id);
        d2.Annehmen.Erlaubt.ShouldBeFalse(); // nur die Gewählte oder der Vorstand

        // Annahme: Amtszeit, Rolle Vorstand und Funktion ab 01.12.2026 (vorher nicht), Wahlprotokoll
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => W(d).AnnehmenAsync(wahl.Id));
        var person = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonAsync(kuehlert.Id)))!;
        person.RollenAm(ab, u.Uhr.GetUtcNow()).ShouldContain(Rolle.Vorstand);
        person.RollenAm(ab.AddDays(-1), u.Uhr.GetUtcNow()).ShouldNotContain(Rolle.Vorstand);
        person.StandAm(ab, u.Uhr.GetUtcNow()).Funktion.ShouldBe("Fraktionsvorsitz");
        var fertig = (await u.InAsync(d => W(d).DetailAsync(wahl.Id)))!;
        fertig.Wahlprotokoll.ShouldContain("Stichwahl");
        fertig.Wahlprotokoll.ShouldContain("angenommen");
        (await u.InAsync(d => W(d).AemterAsync(ab))).ShouldContain(a => a.Amtszeit.PersonId == kuehlert.Id && a.Amt == "Fraktionsvorsitz");

        // Neuwahl: Gries wird ab 01.03.2027 offen gewählt (eine Kandidatur, Ja/Nein) → Amtszeit und Vorstandsrolle von Kühlert enden am Vortag
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var neu = await u.InAsync(d => W(d).AnlegenAsync(Fraktionsamt.Vorsitz, null, null, new DateOnly(2027, 3, 1), null, geheim: false));
        await u.InAsync(d => W(d).KandidaturAsync(neu.Id, gries.Id));
        var gang = await u.InAsync(d => W(d).WahlgangStartenAsync(neu.Id, u.Uhr.GetUtcNow().AddHours(1), waehler));
        await StimmeAsync(u, gang.Id, "lkuehlert", "Gries");
        await StimmeAsync(u, gang.Id, "bkurney", "Nein");
        await StimmeAsync(u, gang.Id, "kschwenken", "Gries");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbschliessenAsync(gang.Id));
        (await u.InAsync(d => W(d).DetailAsync(neu.Id)))!.Wahl.Status.ShouldBe(Wahlstatus.Gewaehlt);
        await u.InAsync(d => W(d).AnnehmenAsync(neu.Id));
        person = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonAsync(kuehlert.Id)))!;
        person.RollenAm(new DateOnly(2027, 3, 1), u.Uhr.GetUtcNow()).ShouldNotContain(Rolle.Vorstand);
        person.RollenAm(new DateOnly(2027, 2, 28), u.Uhr.GetUtcNow()).ShouldContain(Rolle.Vorstand);
        (await u.InAsync(d => W(d).AemterAsync(new DateOnly(2027, 3, 1)))).Single(a => a.Amt == "Fraktionsvorsitz").Amtszeit.PersonId.ShouldBe(gries.Id);
    }

    [Fact]
    public async Task Vor_Ende_der_Amtszeit_Aufgabe_Neuwahl_einmal()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var kassier = await u.AnmeldenAsync("bkurney");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var wahl = await u.InAsync(d => W(d).AnlegenAsync(Fraktionsamt.Kassier, null, null, new DateOnly(2026, 11, 16), new DateOnly(2026, 12, 31), geheim: false));
        await u.InAsync(d => W(d).KandidaturAsync(wahl.Id, kassier.Id));
        var gang = await u.InAsync(d => W(d).WahlgangStartenAsync(wahl.Id, u.Uhr.GetUtcNow().AddHours(1)));
        await StimmeAsync(u, gang.Id, "bkurney", "Kurney");
        u.Uhr.Advance(TimeSpan.FromHours(2));
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().FristenPruefenAsync());
        await u.InAsync(d => W(d).FristenPruefenAsync());
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => W(d).AnnehmenAsync(wahl.Id));
        (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonAsync(kassier.Id)))!.RollenAm(new DateOnly(2026, 11, 20), u.Uhr.GetUtcNow())
            .ShouldContain(Rolle.Kassier);

        await u.InAsync(d => W(d).FristenPruefenAsync());
        await u.InAsync(d => W(d).FristenPruefenAsync());
        var aufgaben = await u.InAsync(d => d.GetRequiredService<IDaten>().ListeAsync(d.GetRequiredService<IDaten>().Menge<Aufgabe>().Where(a => a.Anlass == WahlDienst.Anlass)));
        aufgaben.ShouldHaveSingleItem().Titel.ShouldBe("Neuwahl vorbereiten: Schatzmeister/Kassier");
    }
}
