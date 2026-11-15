using FraktionPerfekt.Domain.Vertretung;

namespace FraktionPerfekt.Domain.Tests;

public sealed class VertretungsfallTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 11, 15, 9, 0, 0, TimeSpan.FromHours(1));
    private static readonly Guid Termin = Guid.NewGuid(), Gremium = Guid.NewGuid(), Fehlt = Guid.NewGuid();
    private static readonly Guid Rang1 = Guid.NewGuid(), Rang2 = Guid.NewGuid(), Rang3 = Guid.NewGuid();

    private static (Vertretungsfall Fall, Vertretungsschritt Schritt) Starten(params Guid[] kandidaten)
        => Vertretungsfall.Starten(Termin, Gremium, Fehlt, null, kandidaten, Jetzt, Jetzt.AddHours(24));

    [Fact]
    public void Fragt_zuerst_Rang_1()
    {
        var (fall, schritt) = Starten(Rang1, Rang2, Rang3);

        fall.Status.ShouldBe(Vertretungsstatus.Angefragt);
        schritt.NeueAnfrage!.PersonId.ShouldBe(Rang1);
        schritt.NeueAnfrage.Reihenfolge.ShouldBe(1);
        fall.OffeneAnfrage!.FristBis.ShouldBe(Jetzt.AddHours(24));
    }

    [Fact]
    public void Ablehnung_geht_zur_naechsten_Person()
    {
        var (fall, _) = Starten(Rang1, Rang2, Rang3);

        var schritt = fall.Antworten(Rang1, false, Jetzt.AddHours(1), Jetzt.AddHours(25));

        schritt.NeueAnfrage!.PersonId.ShouldBe(Rang2);
        fall.Anfragen.Single(a => a.PersonId == Rang1).Status.ShouldBe(Anfragestatus.Abgelehnt);
    }

    [Fact]
    public void Fristablauf_geht_zur_naechsten_Person_vorher_nicht()
    {
        var (fall, _) = Starten(Rang1, Rang2);

        fall.FristPruefen(Jetzt.AddHours(23), Jetzt.AddHours(47)).ShouldBeNull();
        var schritt = fall.FristPruefen(Jetzt.AddHours(24), Jetzt.AddHours(48));

        schritt!.NeueAnfrage!.PersonId.ShouldBe(Rang2);
        fall.Anfragen.Single(a => a.PersonId == Rang1).Status.ShouldBe(Anfragestatus.Abgelaufen);
    }

    [Fact]
    public void Zusage_traegt_ein_und_beendet_den_Ablauf()
    {
        var (fall, _) = Starten(Rang1, Rang2);
        fall.Antworten(Rang1, false, Jetzt, Jetzt.AddHours(24));

        var schritt = fall.Antworten(Rang2, true, Jetzt.AddHours(2), Jetzt.AddHours(26));

        schritt.Status.ShouldBe(Vertretungsstatus.Besetzt);
        fall.VertreterPersonId.ShouldBe(Rang2);
        fall.IstOffen.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => fall.Antworten(Rang2, true, Jetzt, Jetzt)).Message.ShouldContain("bereits besetzt");
    }

    [Fact]
    public void Ohne_Zusage_eskaliert_der_Fall()
    {
        var (fall, _) = Starten(Rang1, Rang2);
        fall.Antworten(Rang1, false, Jetzt, Jetzt.AddHours(24));

        var schritt = fall.Antworten(Rang2, false, Jetzt, Jetzt.AddHours(24));

        schritt.Status.ShouldBe(Vertretungsstatus.Eskaliert);
        fall.OffeneAnfrage.ShouldBeNull();
        fall.IstOffen.ShouldBeTrue();
    }

    [Fact]
    public void Ohne_Kandidaten_eskaliert_sofort_und_die_vertretene_Person_ist_nie_Kandidat()
    {
        var (fall, schritt) = Starten(Fehlt);

        schritt.Status.ShouldBe(Vertretungsstatus.Eskaliert);
        fall.Kandidaten.ShouldBeEmpty();
    }

    [Fact]
    public void Nur_die_gefragte_Person_kann_antworten()
    {
        var (fall, _) = Starten(Rang1, Rang2);

        Should.Throw<InvalidOperationException>(() => fall.Antworten(Rang2, true, Jetzt, Jetzt));
    }

    [Fact]
    public void Vorstand_kann_nach_Eskalation_selbst_besetzen()
    {
        var (fall, _) = Starten();
        var jemand = Guid.NewGuid();

        fall.ManuellBesetzen(jemand, Jetzt).Status.ShouldBe(Vertretungsstatus.Besetzt);
        fall.VertreterPersonId.ShouldBe(jemand);
    }

    [Fact]
    public void Entfallen_zieht_die_offene_Anfrage_zurueck()
    {
        var (fall, _) = Starten(Rang1);

        fall.Entfallen(Jetzt);

        fall.Status.ShouldBe(Vertretungsstatus.Entfallen);
        fall.Anfragen.Single().Status.ShouldBe(Anfragestatus.Zurueckgezogen);
    }

    [Fact]
    public void Frist_hoechstens_bis_zwei_Stunden_vor_Beginn()
    {
        var beginn = Jetzt.AddHours(10);
        Vertretungsfall.FristBerechnen(Jetzt, beginn, 24).ShouldBe(beginn.AddHours(-2));
        Vertretungsfall.FristBerechnen(Jetzt, Jetzt.AddDays(5), 24).ShouldBe(Jetzt.AddHours(24));
        // Kurzfristig: eine Stunde, höchstens bis Beginn
        Vertretungsfall.FristBerechnen(Jetzt, Jetzt.AddMinutes(90), 24).ShouldBe(Jetzt.AddHours(1));
        Vertretungsfall.FristBerechnen(Jetzt, Jetzt.AddMinutes(30), 24).ShouldBe(Jetzt.AddMinutes(30));
    }
}
