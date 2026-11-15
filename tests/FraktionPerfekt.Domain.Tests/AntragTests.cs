using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;

namespace FraktionPerfekt.Domain.Tests;

public sealed class AntragTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Statuswechsel_nur_entlang_des_Ablaufs()
    {
        var a = new Antrag("Test", Guid.NewGuid(), null, null, Jetzt);
        Should.Throw<InvalidOperationException>(() => a.Wechseln(Antragsstatus.Eingereicht, Jetzt)).Message.ShouldContain("Idee");
        a.Wechseln(Antragsstatus.Entwurf, Jetzt);
        a.AbstimmungZuordnen(Guid.NewGuid(), Jetzt);
        a.Status.ShouldBe(Antragsstatus.InAbstimmung);
        a.Wechseln(Antragsstatus.Beschlossen, Jetzt);
        a.Einreichen("A-2026-001", Jetzt);
        a.Nummer.ShouldBe("A-2026-001");
        a.Wechseln(Antragsstatus.ImGremium, Jetzt);
        a.Wechseln(Antragsstatus.Erledigt, Jetzt);
        Antrag.MoeglicheUebergaenge(Antragsstatus.Erledigt).ShouldBeEmpty();
        Antragsstatus.Entwurf.TextAenderbar().ShouldBeTrue();
        Antragsstatus.Beschlossen.TextAenderbar().ShouldBeFalse();
    }

    [Fact]
    public void Aenderungsvorschlag_verlangt_eindeutige_Textstelle()
    {
        var v = new Aenderungsvorschlag(Guid.NewGuid(), 1, Guid.NewGuid(), "bis 2026/27", "bis Sommer 2026", null, Jetzt);
        v.Anwenden("Ergebnis bis 2026/27 vorlegen.").ShouldBe("Ergebnis bis Sommer 2026 vorlegen.");
        Should.Throw<InvalidOperationException>(() => v.Anwenden("ohne Treffer")).Message.ShouldContain("nicht mehr");
        Should.Throw<InvalidOperationException>(() => v.Anwenden("bis 2026/27 und bis 2026/27")).Message.ShouldContain("mehrfach");
        v.Entscheiden(true, Guid.NewGuid());
        Should.Throw<InvalidOperationException>(() => v.Entscheiden(false, Guid.NewGuid()));
    }

    [Fact]
    public void Textvergleich_markiert_Woerter_und_setzt_beide_Texte_wieder_zusammen()
    {
        var alt = "Die Verwaltung prüft die Räume bis 2026.";
        var neu = "Die Verwaltung prüft schnell die Räume bis Sommer 2026.";
        var v = Textvergleich.Vergleichen(alt, neu);
        v.ShouldContain(s => s.Art == Vergleichsart.Eingefuegt && s.Text.Contains("schnell", StringComparison.Ordinal));
        string.Concat(v.Where(s => s.Art != Vergleichsart.Eingefuegt).Select(s => s.Text)).ShouldBe(alt);
        string.Concat(v.Where(s => s.Art != Vergleichsart.Entfernt).Select(s => s.Text)).ShouldBe(neu);
    }

    [Fact]
    public void Auswertung_mit_Quorum_und_Mehrheit()
    {
        var u = new Umfrage("Antrag", null, Umfrageart.Antragsabstimmung, Jetzt.AddDays(1), 50, false, 10, Guid.NewGuid(), Jetzt);
        var o = Umfrage.JaNeinEnthaltung.Select((t, i) => new UmfrageOption(u.Id, i + 1, t)).ToList();
        Stimme S(int option) => new(u.Id, o[option].Id, Guid.NewGuid(), Jetzt);

        var wenige = Auswertung.Auswerten(u, o, [S(0), S(0), S(1)]);
        wenige.QuorumErreicht.ShouldBeFalse();
        wenige.Angenommen.ShouldBe(false);
        wenige.Text.ShouldContain("Quorum");

        var genug = Auswertung.Auswerten(u, o, [S(0), S(0), S(0), S(1), S(2)]);
        genug.QuorumErreicht.ShouldBeTrue();
        genug.Angenommen.ShouldBe(true);
        genug.Text.ShouldStartWith("Angenommen: 3 ja, 1 nein, 1 Enthaltung");

        Should.Throw<ArgumentException>(() => new Umfrage("x", null, Umfrageart.Umfrage, Jetzt, 0, false, 1, Guid.NewGuid(), Jetzt));
        new Umfrage("t", null, Umfrageart.Terminfindung, Jetzt.AddDays(1), 0, false, 1, Guid.NewGuid(), Jetzt).Mehrfachauswahl.ShouldBeTrue();
    }
}
