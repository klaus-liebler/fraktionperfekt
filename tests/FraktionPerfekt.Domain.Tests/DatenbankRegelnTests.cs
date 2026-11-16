using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Tests;

/// <summary>Datenbank-Regeln (aus VibeVHS): bitemporale Beispiele, Namen nach Regel 7, Merkmale statt Kopien.</summary>
public sealed class DatenbankRegelnTests
{
    private static DateTimeOffset Am(int j, int m, int t) => new(j, m, t, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Regel 2, Beispiel Frau Muster: Lindenstraße seit 2020; am 15.03.2026 Umzug zum 01.08. in den Bergweg gemeldet; am 20.09. stellt
    /// sich heraus, dass sie schon zum 01.07. umgezogen ist. Zeilen A–E wie in der Übergabe.
    /// </summary>
    [Fact]
    public void Umzug_mit_nachtraeglicher_Korrektur_des_Umzugsdatums_Frau_Muster()
    {
        var p = new Person("Erika", "Muster", null, null, Am(2020, 1, 10), gueltigAb: new DateOnly(2020, 1, 1), anrede: "Frau");
        var linden = p.AdresseHinzufuegen(Kontaktbereich.Privat, null, "Lindenstr. 5", "48268", "Greven", true, new DateOnly(2020, 1, 1), Erfassung.Erst(Am(2020, 1, 10)));
        // 15.03.2026: Umzug zum 01.08. gemeldet
        p.AdresseAufgebenAb(linden, new DateOnly(2026, 8, 1), Erfassung.Aenderung(Am(2026, 3, 15), "Umzug gemeldet"));
        var berg = p.AdresseHinzufuegen(Kontaktbereich.Privat, null, "Bergweg 2", "48268", "Greven", true, new DateOnly(2026, 8, 1), Erfassung.Aenderung(Am(2026, 3, 15), "Umzug ab 01.08."));
        // 20.09.2026: Umzug war schon zum 01.07.
        p.UmzugsdatumKorrigieren(linden, berg, new DateOnly(2026, 7, 1), Erfassung.Korrektur(Am(2026, 9, 20), "Brief kam zurück, Umzug schon zum 01.07."));

        var zehnterJuli = new DateOnly(2026, 7, 10);
        // „Wo wohnte sie am 10.07.?“ – heutiger Wissensstand (nur nicht ersetzte Zeilen D, E)
        p.StandAm(zehnterJuli, Am(2026, 11, 16)).Adressen.Single().Strasse.ShouldBe("Bergweg 2");
        // „Wohin hätten wir am 10.07. geschrieben?“ – Wissensstand 10.07. (Zeilen B, C)
        p.StandAm(zehnterJuli, Am(2026, 7, 10)).Adressen.Single().Strasse.ShouldBe("Lindenstr. 5");

        // Zeilen A–E (ohne Endemarken): A ersetzt am 15.03., B und C ersetzt am 20.09., D und E aktuell
        var zeilen = p.Adressen.Where(a => !a.IstEndemarke).OrderBy(a => a.ErfasstAm).ThenBy(a => a.Strasse, StringComparer.Ordinal).ToList();
        string Zeile(Postadresse a) => $"{a.Strasse}|{a.GueltigVon:yyyy-MM-dd}|{a.GueltigBis?.AddDays(-1).ToString("yyyy-MM-dd") ?? "offen"}|{a.ErfasstAm:yyyy-MM-dd}|{a.ErsetztAm?.ToString("yyyy-MM-dd") ?? "–"}";
        zeilen.Select(Zeile).ShouldBe(
        [
            "Lindenstr. 5|2020-01-01|offen|2020-01-10|2026-03-15",      // A
            "Bergweg 2|2026-08-01|offen|2026-03-15|2026-09-20",         // C
            "Lindenstr. 5|2020-01-01|2026-07-31|2026-03-15|2026-09-20", // B
            "Bergweg 2|2026-07-01|offen|2026-09-20|–",                  // E
            "Lindenstr. 5|2020-01-01|2026-06-30|2026-09-20|–",          // D
        ]);
    }

    [Fact]
    public void Sortierung_nach_DIN_5007_2_von_Berg_steht_unter_B_Umlaute_als_ae()
    {
        var namen = new[]
        {
            new Person("Anna", "Berg", null, null, Am(2025, 1, 1), vorsatz: "von"),
            new Person("Jens", "Bärmann", null, null, Am(2025, 1, 1)),
            new Person("Carl", "Baer", null, null, Am(2025, 1, 1)),
            new Person("Dora", "Abel", null, null, Am(2025, 1, 1)),
        };
        namen.OrderBy(p => p.Sortierschluessel, StringComparer.Ordinal).Select(p => p.Anzeigename)
            .ShouldBe(["Dora Abel", "Carl Baer", "Jens Bärmann", "Anna von Berg"]);
        namen[0].Sortiername.ShouldBe("Berg, Anna von");
    }

    [Fact]
    public void Titel_ist_eigenes_Merkmal_Vorsatz_und_Zusatz_im_Namen_Anrede_mit_Vorsatz()
    {
        var p = new Person("Anna", "Berg", null, null, Am(2025, 1, 1), titel: "Dr.", anrede: "Frau", vorsatz: "von", zusatz: "jun.");
        p.Anzeigename.ShouldBe("Dr. Anna von Berg jun.");
        p.Merkmale.Single(m => m.Art == PersonMerkmalArt.Titel).Inhalt.ShouldBe("Dr.");
        p.Namen.Single().Anzeige.ShouldBe("Anna von Berg jun.");
        p.AnspracheEinstellen("Frau", Anredeform.Sie, Begruessungsstil.Foermlich, true, Am(2025, 2, 1));
        Ansprache.Begruessung(p).ShouldBe("Sehr geehrte Frau Dr. von Berg");
        // Ansprache ist ein zeitliches Merkmal: vorher galt Du
        p.StandAm(new DateOnly(2025, 1, 15), Am(2025, 2, 2)).Anredeform.ShouldBe(Anredeform.Du);
        p.StandAm(new DateOnly(2025, 2, 1), Am(2025, 2, 2)).Anredeform.ShouldBe(Anredeform.Sie);
    }

    [Fact]
    public void Nur_ein_Name_laut_Ausweis_braucht_das_Kennzeichen()
    {
        Should.Throw<ArgumentException>(() => new Person("", "Suharto", null, null, Am(2025, 1, 1)));
        var p = new Person("", "Suharto", null, null, Am(2025, 1, 1), nurEinName: true);
        p.Anzeigename.ShouldBe("Suharto");
        p.Namen.Single().NurEinName.ShouldBeTrue();
        Ansprache.Begruessung(p).ShouldBe("Hallo Suharto");
    }

    [Fact]
    public void Erkennung_fragt_nach_Titel_und_Vorsatz_Prof_ist_kein_Titel()
    {
        Namenserkennung.TitelImVornamen("Dr. med. Klaus").ShouldBe(("Dr. med.", "Klaus"));
        Namenserkennung.TitelImVornamen("Prof. Klaus").ShouldBeNull();
        Namenserkennung.TitelImVornamen("Klaus").ShouldBeNull();
        Namenserkennung.VorsatzImNachnamen("van der Berg").ShouldBe(("van der", "Berg"));
        Namenserkennung.VorsatzImNachnamen("Schulte Jochmaring").ShouldBeNull();
    }

    [Fact]
    public void Aktiv_ist_ein_zeitliches_Merkmal()
    {
        var p = new Person("Liz", "Kühlert", null, null, Am(2025, 1, 1));
        p.Deaktivieren(new DateOnly(2026, 1, 1), Erfassung.Aenderung(Am(2025, 6, 1), "Austritt"));
        p.StandAm(new DateOnly(2025, 12, 31), Am(2025, 7, 1)).Aktiv.ShouldBeTrue();
        p.StandAm(new DateOnly(2026, 1, 1), Am(2025, 7, 1)).Aktiv.ShouldBeFalse();
    }
}

public sealed class GremienZeitlichTests
{
    [Fact]
    public void Umbenennung_im_RIS_gilt_ab_Datum_alte_Protokolle_behalten_den_Namen_Amt_ist_zeitlich()
    {
        var erst = new DateTimeOffset(2025, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var g = new FraktionPerfekt.Domain.Gremien.Gremium("Ausschuss für Umwelt", "UA", FraktionPerfekt.Domain.Gremien.Gremienart.Ausschuss, 10, "ris-1",
            gueltigAb: new DateOnly(2020, 11, 1), erfassung: Erfassung.Erst(erst));
        var am = erst.AddDays(30);
        g.AusRisUebernehmen("Ausschuss für Klima und Umwelt", "AKU", FraktionPerfekt.Domain.Gremien.Gremienart.Ausschuss, new DateOnly(2026, 1, 1),
            Erfassung.Aenderung(am, "RIS")).ShouldBeTrue();
        g.StandAm(new DateOnly(2025, 12, 31), am)!.Name.ShouldBe("Ausschuss für Umwelt");
        g.StandAm(new DateOnly(2026, 1, 1), am)!.Kurzname.ShouldBe("AKU");
        g.StandHeuteBerechnen(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        g.Name.ShouldBe("Ausschuss für Klima und Umwelt");

        var m = new FraktionPerfekt.Domain.Gremien.Mitgliedschaft(Guid.NewGuid(), g.Id, FraktionPerfekt.Domain.Gremien.Funktion.Ordentlich, null, new DateOnly(2020, 11, 1),
            Erfassung.Erst(erst), amt: FraktionPerfekt.Domain.Gremien.Gremienamt.Keins);
        m.AmtAendernAb(new DateOnly(2025, 11, 5), FraktionPerfekt.Domain.Gremien.Gremienamt.Vorsitz, "Ausschussvorsitzender", Erfassung.Aenderung(am, "RIS")).ShouldBeTrue();
        m.StandAm(new DateOnly(2025, 11, 4), am)!.Amt.ShouldBe(FraktionPerfekt.Domain.Gremien.Gremienamt.Keins);
        m.StandAm(new DateOnly(2025, 11, 5), am)!.FunktionText.ShouldBe("Ausschussvorsitzender");
    }
}
