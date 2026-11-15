using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;

namespace FraktionPerfekt.Domain.Tests;

public sealed class MitgliedschaftTests
{
    private static readonly DateTimeOffset Erfasst = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static Mitgliedschaft Stellvertretung(int rang)
        => new(Guid.NewGuid(), Guid.NewGuid(), Funktion.Stellvertretung, rang, new DateOnly(2025, 11, 1), Erfassung.Erst(Erfasst, "Test"));

    [Fact]
    public void Rangaenderung_ab_Datum_laesst_die_Vergangenheit_unveraendert()
    {
        var m = Stellvertretung(2);
        var spaeter = Erfasst.AddDays(30);

        m.AendernAb(new DateOnly(2026, 3, 1), Funktion.Stellvertretung, 1, Erfassung.Aenderung(spaeter, "Neue Rangfolge"));

        m.StandAm(new DateOnly(2026, 2, 28), spaeter)!.Rang.ShouldBe(2);
        m.StandAm(new DateOnly(2026, 3, 1), spaeter)!.Rang.ShouldBe(1);
        // Bitemporal: Was war am 01.01. über den März bekannt? Damals noch Rang 2.
        m.StandAm(new DateOnly(2026, 3, 1), Erfasst)!.Rang.ShouldBe(2);
    }

    [Fact]
    public void Wechsel_zu_ordentlich_entfernt_den_Rang_und_Ende_beendet()
    {
        var m = Stellvertretung(1);
        var t = Erfasst.AddDays(1);

        m.AendernAb(new DateOnly(2026, 2, 1), Funktion.Ordentlich, 3, Erfassung.Aenderung(t, "Nachgerückt"));
        m.BeendenAb(new DateOnly(2026, 6, 1), Erfassung.Ende(t.AddDays(1), "Mandat niedergelegt"));

        m.StandAm(new DateOnly(2026, 2, 1), t.AddDays(2))!.Funktion.ShouldBe(Funktion.Ordentlich);
        m.StandAm(new DateOnly(2026, 2, 1), t.AddDays(2))!.Rang.ShouldBeNull();
        m.StandAm(new DateOnly(2026, 6, 1), t.AddDays(2)).ShouldBeNull();
        m.Versionen.Count(v => v.IstAktuellerWissensstand).ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Rang_beginnt_bei_eins()
        => Should.Throw<ArgumentOutOfRangeException>(() => Stellvertretung(0));

    [Fact]
    public void Rollen_sind_bitemporal()
    {
        var p = new Person("Test", "Person", null, "test", Erfasst);
        p.RollenFestlegenAb(new DateOnly(2025, 11, 1), [Rolle.Mitglied], Erfassung.Erst(Erfasst, "Test"));
        p.RollenFestlegenAb(new DateOnly(2026, 4, 1), [Rolle.Mitglied, Rolle.Vorstand], Erfassung.Aenderung(Erfasst.AddDays(1), "Wahl in den Vorstand"));

        p.RollenAm(new DateOnly(2026, 3, 31), Erfasst.AddDays(2)).ShouldBe([Rolle.Mitglied]);
        p.RollenAm(new DateOnly(2026, 4, 1), Erfasst.AddDays(2)).ShouldBe([Rolle.Vorstand, Rolle.Mitglied], ignoreOrder: true);
    }

    [Fact]
    public void Kalenderabo_speichert_nur_den_Hash_und_ist_widerrufbar()
    {
        var (abo, token) = KalenderAbo.Erzeugen(Guid.NewGuid(), Erfasst);

        abo.TokenHash.ShouldNotContain(token);
        abo.TokenHash.ShouldBe(KalenderAbo.Hash(token));
        token.Length.ShouldBeGreaterThanOrEqualTo(40);
        abo.Widerrufen(Erfasst);
        abo.Aktiv.ShouldBeFalse();
    }

    [Fact]
    public void Abwesenheit_umfasst_beide_Grenztage_und_nach_Rueckzug_keinen()
    {
        var a = new Abwesenheit(Guid.NewGuid(), new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), "krank", Erfasst);
        a.Umfasst(new DateOnly(2026, 12, 1)).ShouldBeTrue();
        a.Umfasst(new DateOnly(2026, 12, 3)).ShouldBeTrue();
        a.Umfasst(new DateOnly(2026, 12, 4)).ShouldBeFalse();
        a.Zurueckziehen(Erfasst);
        a.Umfasst(new DateOnly(2026, 12, 2)).ShouldBeFalse();
    }

    [Fact]
    public void Pseudonym_ist_stabil_und_unterscheidet_zeitnah_erzeugte_Ids()
    {
        var ids = Enumerable.Range(0, 200).Select(_ => Ids.Neu()).ToList();
        ids.Select(Pseudonym.Kurz).Distinct().Count().ShouldBe(200);
        Pseudonym.Person(ids[0]).ShouldBe(Pseudonym.Person(ids[0]));
    }
}
