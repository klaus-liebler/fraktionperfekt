using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Tests;

/// <summary>Person = Identität; Name, Anrede, Kontakte und Funktion gelten zeitlich (bitemporal), wie in VibeVHS.</summary>
public sealed class PersonZeitlichTests
{
    private static readonly DateTimeOffset Anlage = new(2025, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private static Person Liz() => new("Liz", "Kühlert", "liz@demo.invalid", "lkuehlert", Anlage, anrede: "Frau", gueltigAb: new DateOnly(1999, 10, 1),
        funktion: "Ratsmitglied");

    [Fact]
    public void Namensaenderung_ab_Datum_laesst_die_Vergangenheit_unveraendert()
    {
        var p = Liz();
        var am = Anlage.AddDays(30);
        p.NameAendernAb("Liz", "Kühlert-Sommer", "Dr.", new DateOnly(2026, 6, 1), Erfassung.Aenderung(am, "Heirat und Promotion"));

        p.StandAm(new DateOnly(2026, 5, 31), am).Anzeige.ShouldBe("Liz Kühlert");
        p.StandAm(new DateOnly(2026, 6, 1), am).Anzeige.ShouldBe("Dr. Liz Kühlert-Sommer");
        p.AnzeigenameAm(new DateOnly(2000, 1, 1), am).ShouldBe("Liz Kühlert");
        // Zwischenstand: Stichtag der Erfassung (01.12.2025) – die Änderung gilt erst ab Juni 2026
        (p.Nachname, p.Titel).ShouldBe(("Kühlert", null));
        p.Namen.Count(n => n.IstAktuellerWissensstand).ShouldBe(2);
    }

    [Fact]
    public void Kuenftige_Aenderung_wird_am_Stichtag_im_Zwischenstand_sichtbar()
    {
        var p = Liz();
        p.AngabeAendern(PersonMerkmalArt.Funktion, "Fraktionsvorsitzende", new DateOnly(2026, 1, 1), Erfassung.Aenderung(Anlage, "Wahl"), false);
        p.AngabeAendern(PersonMerkmalArt.EMail, "liz.neu@demo.invalid", new DateOnly(2026, 1, 1), Erfassung.Aenderung(Anlage, "neue Adresse"), false);
        p.EMail.ShouldBe("liz@demo.invalid");                       // heute (Nov. 2025) noch die alte
        p.ZwischenstandAktualisieren(new DateTimeOffset(2026, 1, 2, 3, 0, 0, TimeSpan.Zero));   // Nachtlauf nach dem Stichtag
        p.EMail.ShouldBe("liz.neu@demo.invalid");
        p.StandAm(new DateOnly(2025, 12, 31), Anlage).Funktion.ShouldBe("Ratsmitglied");
        p.StandAm(new DateOnly(2026, 1, 1), Anlage).Funktion.ShouldBe("Fraktionsvorsitzende");
    }

    [Fact]
    public void Korrektur_ersetzt_die_Version_fuer_den_ganzen_Zeitraum_der_alte_Wissensstand_bleibt_abrufbar()
    {
        var p = new Person("Alfons", "Schulze Jochmaring", null, "aschulzejochmaring", Anlage, gueltigAb: new DateOnly(2020, 11, 4));
        var korrigiertAm = Anlage.AddDays(15);
        p.NameKorrigieren("Alfons", "Schulte Jochmaring", null, new DateOnly(2025, 1, 1), Erfassung.Korrektur(korrigiertAm, "Tippfehler im Nachnamen"));

        p.StandAm(new DateOnly(2021, 1, 1), korrigiertAm).Anzeige.ShouldBe("Alfons Schulte Jochmaring");   // auch rückwirkend richtig
        p.StandAm(new DateOnly(2021, 1, 1), Anlage.AddDays(1)).Anzeige.ShouldBe("Alfons Schulze Jochmaring"); // so war es damals bekannt
        p.Nachname.ShouldBe("Schulte Jochmaring");
        p.Namen.Count(n => !n.IstAktuellerWissensstand).ShouldBe(1);
        p.Namen.Single(n => n.IstAktuellerWissensstand).Erfassungsart.ShouldBe(Erfassungsart.Korrektur);
    }

    [Fact]
    public void Anrede_aus_den_Einstellungen_wird_ab_heute_geaendert_die_bisherige_bleibt_in_der_Historie()
    {
        var p = Liz();
        var heute = new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);
        p.AnspracheEinstellen(null, Anredeform.Sie, Begruessungsstil.Neutral, true, heute);
        p.Anrede.ShouldBeNull();
        p.StandAm(new DateOnly(2026, 11, 15), heute).Anrede.ShouldBe("Frau");
        p.StandAm(new DateOnly(2026, 11, 16), heute).Anrede.ShouldBeNull();
        Ansprache.Begruessung(p).ShouldBe("Guten Tag Liz Kühlert");
    }
}
