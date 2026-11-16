using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;

namespace FraktionPerfekt.Domain.Tests;

public sealed class SitzungTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ablauf_Vorbereitung_Einladung_Sitzung_Entwurf_Freigabe()
    {
        var s = new Sitzung(Guid.NewGuid(), Sitzungsart.Fraktionssitzung, "Fraktionssitzung", null, Guid.NewGuid(), Jetzt);
        Should.Throw<InvalidOperationException>(() => s.Freigeben(Guid.NewGuid(), Jetzt));
        s.Einladen(Jetzt);
        s.TagesordnungAenderbar.ShouldBeTrue();
        s.ProtokollAenderbar.ShouldBeFalse();
        s.Beginnen(Jetzt, Guid.NewGuid());
        s.ProtokollAenderbar.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => s.Einladen(Jetzt));
        s.Beenden(Jetzt);
        s.TagesordnungAenderbar.ShouldBeFalse();
        s.Freigeben(Guid.NewGuid(), Jetzt);
        s.Status.ShouldBe(Sitzungsstatus.Freigegeben);
        s.ProtokollAenderbar.ShouldBeFalse();
    }

    [Fact]
    public void Zusammenlegen_uebernimmt_Beschreibung_und_Notiz()
    {
        var sitzung = Guid.NewGuid();
        var a = new Top(sitzung, 1, "Ganztag", "Raumbedarf", false, null, Jetzt);
        var b = new Top(sitzung, 2, "Grundschulen", null, false, null, Jetzt);
        a.NotizSetzen("Zahlen der Verwaltung fehlen.");

        a.ZusammenlegenMit(b);

        a.Status.ShouldBe(Topstatus.Zusammengelegt);
        b.Beschreibung!.ShouldContain("zusammengelegt mit: Ganztag");
        b.Notiz.ShouldBe("Zahlen der Verwaltung fehlen.");
        Should.Throw<InvalidOperationException>(() => a.Vertagen());
        Should.Throw<InvalidOperationException>(() => b.ZusammenlegenMit(new Top(Guid.NewGuid(), 1, "x", null, false, null, Jetzt)));
    }

    [Fact]
    public void Tagesordnung_aus_dem_RIS_und_Beschlussergebnis()
    {
        Sitzungsvorlagen.AusTagesordnung("Tagesordnung (Auszug):\nTOP 1 Haushalt 2026\nTOP 2 Badsanierung\n\nÖffentliche Sitzung")
            .ShouldBe(["Haushalt 2026", "Badsanierung"]);
        Sitzungsvorlagen.AusTagesordnung(null).ShouldContain("Verschiedenes");
        Beschluss.Auswerten(5, 5).ShouldBe(Beschlussergebnis.Abgelehnt);
        Beschluss.Auswerten(6, 5).ShouldBe(Beschlussergebnis.Angenommen);
    }

    [Fact]
    public void Aufgabe_Korb_uebernehmen_zuweisen_zurueck()
    {
        var a = new Aufgabe("Klären", null, Rolle.Vorstand, "Test", Guid.NewGuid(), "x", Jetzt, Jetzt.AddDays(1));
        var anna = Guid.NewGuid();
        a.Uebernehmen(anna);
        Should.Throw<InvalidOperationException>(() => a.Uebernehmen(Guid.NewGuid())).Message.ShouldContain("jemand anderes");
        a.ZurueckInKorb();
        a.ZugewiesenPersonId.ShouldBeNull();
        a.Zuweisen(anna);
        a.Ueberfaellig(Jetzt.AddDays(2)).ShouldBeTrue();
        a.Erledigen(Jetzt, Guid.NewGuid(), "ok");
        a.Ueberfaellig(Jetzt.AddDays(2)).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => a.Zuweisen(anna));

        var persoenlich = new Aufgabe("Nur für mich", null, null, "Test", Guid.NewGuid(), "x", Jetzt, null, anna);
        Should.Throw<InvalidOperationException>(() => persoenlich.ZurueckInKorb());
        Should.Throw<ArgumentException>(() => new Aufgabe("ohne alles", null, null, "Test", Guid.NewGuid(), "x", Jetzt, null));
    }
}
