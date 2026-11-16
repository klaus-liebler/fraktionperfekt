using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Gremien;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Echte Gremienzugehörigkeiten aus dem RIS-Text (FraktionGreven.Gremienzugehoerigkeit).</summary>
public sealed class GremienDatenTests
{
    [Fact]
    public void Parser_liest_RIS_Text_mit_Tabulator_oder_Strich_und_deutet_Funktionen()
    {
        var z = RisTabelle.Parsen("""
            Herr Martinus Benning
            Gremium	Funktion	Seit
            Rat	Ratsmitglied	15.04.2024
            - Bezirksausschuss für die Bauerschaften | 2. Stellv. Ausschussvorsitzender | 11.03.2026
            Betriebsausschuss	Ausschussvorsitzender	05.11.2025
            Schulausschuss	Stellv. Ausschussmitglied	05.11.2025

            Frau Christa Waschkowitz-Biggeleben
            Rat	1. Stellv. Bürgermeisterin	04.11.2020
            Zweckverbandsversammlung Musikschule Greven/Emsdetten/Saerbeck	Mitglied ZVM	05.11.2025
            Jugendhilfeausschuss	1. Stellv. Ausschussvorsitzende	04.11.2026
            """);
        z.Count.ShouldBe(7);
        z[0].ShouldBe(new RisZugehoerigkeit("Herr Martinus Benning", "Rat", "Ratsmitglied", new DateOnly(2024, 4, 15)));
        (z[1].Funktion, z[1].Amt).ShouldBe((Funktion.Ordentlich, Gremienamt.ZweiteStellvertretung));
        (z[2].Funktion, z[2].Amt).ShouldBe((Funktion.Ordentlich, Gremienamt.Vorsitz));
        (z[3].Funktion, z[3].Amt).ShouldBe((Funktion.Stellvertretung, Gremienamt.Keins));
        (z[4].Person, z[4].Amt).ShouldBe(("Frau Christa Waschkowitz-Biggeleben", Gremienamt.ErsteStellvertretung));
        (z[5].Funktion, z[5].FunktionText).ShouldBe((Funktion.Ordentlich, "Mitglied ZVM")); // unbekannt → Text bleibt, gilt als Mitglied
        z[6].Amt.ShouldBe(Gremienamt.ErsteStellvertretung);                                 // weibliche Bezeichnung
    }

    [Fact]
    public void Alle_Ratsmitglieder_haben_echte_Daten_und_je_Gremium_hoechstens_einen_Vorsitz()
    {
        var z = FraktionGreven.Zugehoerigkeiten;
        FraktionGreven.Ratsmitglieder.ShouldAllBe(p => z.Any(x => x.Person == p && x.Zugehoerigkeit.Gremium == "Rat"));
        z.ShouldAllBe(x => Demofraktion.GremiumZu(x.Zugehoerigkeit.Gremium) != null);
        foreach (var gruppe in z.Where(x => x.Zugehoerigkeit.Amt != Gremienamt.Keins).GroupBy(x => (x.Zugehoerigkeit.Gremium, x.Zugehoerigkeit.Amt)))
        {
            gruppe.Count().ShouldBe(1, $"{gruppe.Key.Gremium}: {gruppe.Key.Amt} mehrfach");
        }

        z.Where(x => x.Person.Kennung == "mbenning").Select(x => x.Zugehoerigkeit.Gremium).Count().ShouldBe(12);
    }

    [Fact]
    public async Task Abgleich_uebernimmt_Funktion_Amt_und_Beginn_aus_dem_RIS()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var gremien = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync());
        var personen = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync());
        var mitgliedschaften = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().MitgliedschaftenAsync());
        var benning = personen.Single(p => p.Benutzerkennung == "mbenning");
        var betrA = gremien.Single(g => g.Kurzname == "BetrA");
        var m = mitgliedschaften.Single(x => x.PersonId == benning.Id && x.GremiumId == betrA.Id);
        (m.Amt, m.FunktionText).ShouldBe((Gremienamt.Vorsitz, "Ausschussvorsitzender"));
        m.Versionen.Single().GueltigVon.ShouldBe(new DateOnly(2025, 11, 5));
        var rat = mitgliedschaften.Single(x => x.PersonId == benning.Id && x.GremiumId == gremien.Single(g => g.Kurzname == "Rat").Id);
        rat.Versionen.Single().GueltigVon.ShouldBe(new DateOnly(2024, 4, 15));
    }
}
