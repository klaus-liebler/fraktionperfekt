using FraktionPerfekt.Domain.Ressourcen;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Domain.Tests;

public sealed class RessourcenTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Tag = new(2026, 11, 20, 0, 0, 0);

    [Fact]
    public void Konflikte_nur_bei_Ueberschneidung_aktiver_Buchungen_derselben_Ressource()
    {
        var raum = Guid.NewGuid();
        var b = new Buchung(raum, Guid.NewGuid(), Tag.AddHours(18), Tag.AddHours(20), "Fraktion", null, false, Jetzt);
        var angefragt = new Buchung(raum, Guid.NewGuid(), Tag.AddHours(9), Tag.AddHours(10), "AK", null, true, Jetzt);
        angefragt.Status.ShouldBe(Buchungsstatus.Angefragt);

        Buchung.Konflikte(raum, Tag.AddHours(19), Tag.AddHours(21), [b, angefragt]).ShouldBe([b]);
        Buchung.Konflikte(raum, Tag.AddHours(20), Tag.AddHours(21), [b]).ShouldBeEmpty();      // Ende exklusiv
        Buchung.Konflikte(raum, Tag.AddHours(9.5), Tag.AddHours(11), [angefragt]).ShouldHaveSingleItem();  // Anfrage blockiert
        Buchung.Konflikte(Guid.NewGuid(), Tag.AddHours(19), Tag.AddHours(21), [b]).ShouldBeEmpty();
        b.Stornieren(null);
        Buchung.Konflikte(raum, Tag.AddHours(19), Tag.AddHours(21), [b]).ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => new Buchung(raum, Guid.NewGuid(), Tag, Tag, "x", null, false, Jetzt));
    }

    [Fact]
    public void Wer_zuerst_kommt_mit_Nachruecken()
    {
        var a = new Besetzungsanfrage(Guid.NewGuid(), 2, Jetzt.AddDays(3), Besetzungsmodus.WerZuerstKommt, Guid.NewGuid(), Jetzt, null);
        var m1 = new Besetzungsmeldung(a.Id, Guid.NewGuid(), Jetzt);
        var m2 = new Besetzungsmeldung(a.Id, Guid.NewGuid(), Jetzt.AddMinutes(1));
        var m3 = new Besetzungsmeldung(a.Id, Guid.NewGuid(), Jetzt.AddMinutes(2));
        a.Nachruecken([m1, m2, m3]).ShouldBe([m1, m2]);
        m3.Status.ShouldBe(Meldungsstatus.Interessiert);
        a.PlatzFrei([m1, m2, m3]).ShouldBeFalse();
        m1.Zurueckziehen();
        a.Nachruecken([m1, m2, m3]).ShouldBe([m3]);

        var vorstand = new Besetzungsanfrage(Guid.NewGuid(), 1, Jetzt.AddDays(3), Besetzungsmodus.VorstandBestaetigt, Guid.NewGuid(), Jetzt, null);
        vorstand.Nachruecken([new Besetzungsmeldung(vorstand.Id, Guid.NewGuid(), Jetzt)]).ShouldBeEmpty();
    }
}
