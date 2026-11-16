using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Tests;

/// <summary>Kontakte nach VibeVHS: mehrere Adressen, E-Mails und Rufnummern mit Art und „bevorzugt“, zeitlich gültig; Einwilligungen.</summary>
public sealed class PersonKontakteTests
{
    private static readonly DateTimeOffset Anlage = new(2025, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Ab = new(2025, 11, 1);

    private static Person Albers() => new("Katrin", "Albers", "katrin@demo.invalid", "kalbers", Anlage, gueltigAb: Ab);

    [Fact]
    public void Bevorzugte_E_Mail_wechselt_ab_Datum_vorher_bleibt_die_alte_bevorzugt()
    {
        var p = Albers();
        var wechsel = new DateOnly(2026, 2, 1);
        p.EMailHinzufuegen(Kontaktbereich.Dienstlich, "fraktion@demo.invalid", true, wechsel, Erfassung.Aenderung(Anlage.AddDays(1), "neue Dienstadresse"));

        p.StandAm(wechsel.AddDays(-1), Anlage.AddDays(2)).EMail.ShouldBe("katrin@demo.invalid");
        var danach = p.StandAm(wechsel, Anlage.AddDays(2));
        danach.EMail.ShouldBe("fraktion@demo.invalid");
        danach.EMails.Count.ShouldBe(2);
        danach.EMails.Single(e => e.Adresse == "katrin@demo.invalid").Bevorzugt.ShouldBeFalse();

        p.StandHeuteBerechnen(new DateTimeOffset(2026, 2, 2, 3, 0, 0, TimeSpan.Zero));
        p.EMail.ShouldBe("fraktion@demo.invalid");
    }

    [Fact]
    public void Umzug_alte_Adresse_aufgeben_neue_ab_Datum_der_Verlauf_bleibt()
    {
        var p = Albers();
        var alt = p.AdresseHinzufuegen(Kontaktbereich.Privat, null, "Musterweg 12", "48268", "Greven", true, Ab, Erfassung.Erst(Anlage));
        var umzug = new DateOnly(2026, 5, 1);
        var am = Anlage.AddDays(10);
        p.AdresseAufgebenAb(alt, umzug, Erfassung.Aenderung(am, "Umzug"));
        p.AdresseHinzufuegen(Kontaktbereich.Privat, null, "Probeweg 7", "48268", "Greven", true, umzug, Erfassung.Aenderung(am, "Umzug"));

        p.StandAm(umzug.AddDays(-1), am).Adressen.Single().Strasse.ShouldBe("Musterweg 12");
        p.StandAm(umzug, am).Adressen.Single().Strasse.ShouldBe("Probeweg 7");
        p.Adressen.Count(a => a.IstEndemarke).ShouldBe(1);
    }

    [Fact]
    public void Korrektur_einer_Rufnummer_der_alte_Wissensstand_bleibt_abrufbar()
    {
        var p = Albers();
        var id = p.RufnummerHinzufuegen(RufnummerArt.Mobil, "+49 151 0000010", Messagingdienst.Anruf | Messagingdienst.Signal, true, true, Ab, Erfassung.Erst(Anlage));
        p.Mobil.ShouldBe("+49 151 0000010");
        var korrigiert = Anlage.AddDays(5);
        p.RufnummerAendern(id, RufnummerArt.Mobil, "+49 151 0000001", Messagingdienst.Anruf | Messagingdienst.Signal, true, true, Ab.AddDays(3),
            Erfassung.Korrektur(korrigiert, "Zahlendreher"), true);

        p.Mobil.ShouldBe("+49 151 0000001");
        p.StandAm(Ab, korrigiert).Mobil.ShouldBe("+49 151 0000001");                 // auch rückwirkend richtig
        p.StandAm(Ab, Anlage.AddDays(1)).Mobil.ShouldBe("+49 151 0000010");         // so war es damals bekannt
        p.Rufnummern.Single(r => !r.IstAktuellerWissensstand).Wert.ShouldContain("Signal");
    }

    [Fact]
    public void Einwilligung_erteilt_und_spaeter_widerrufen_beide_Angaben_bleiben_nachweisbar()
    {
        var p = Albers();
        var widerruf = new DateOnly(2026, 3, 1);
        p.EinwilligungAm(Einwilligungszweck.WeitergabeInDerFraktion, Ab, Anlage).ShouldBeFalse();     // ohne Angabe: nein
        p.EinwilligungSetzenAb(Einwilligungszweck.WeitergabeInDerFraktion, true, Ab, Erfassung.Erst(Anlage));
        p.EinwilligungSetzenAb(Einwilligungszweck.WeitergabeInDerFraktion, false, widerruf, Erfassung.Aenderung(Anlage.AddDays(1), "Widerruf per Mail"));

        p.EinwilligungAm(Einwilligungszweck.WeitergabeInDerFraktion, widerruf.AddDays(-1), Anlage.AddDays(2)).ShouldBeTrue();
        p.EinwilligungAm(Einwilligungszweck.WeitergabeInDerFraktion, widerruf, Anlage.AddDays(2)).ShouldBeFalse();
        p.Einwilligungen.Count(e => e.Erfassungsart != Erfassungsart.Abgrenzung).ShouldBe(2);
    }
}
