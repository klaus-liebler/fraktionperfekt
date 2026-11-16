using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Ressourcen;
using FraktionPerfekt.Domain.Termine;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Demodaten Iteration 4: Ressourcen mit Buchungen, Besetzungsanfragen zu öffentlichen Terminen, ein vergangener Termin ohne Bericht.</summary>
internal static class RessourcenDemodaten
{
    public static async Task AnlegenAsync(IDaten daten, ITerminSpeicher termine, IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt,
        CancellationToken abbruch)
    {
        Guid P(string k) => personen[k].Id;
        var raum = new Ressource("Fraktionsbüro, Rathaus Greven", Ressourcenart.Raum, "12 Plätze, Bildschirm", false);
        var saal = new Ressource("Besprechungsraum Bürgerhaus (fiktiv)", Ressourcenart.Raum, "30 Plätze", true);
        var beamer = new Ressource("Beamer", Ressourcenart.Technik, null, false);
        var infostand = new Ressource("Infostand mit Pavillon", Ressourcenart.Material, "Pavillon 3×3 m, Tisch, Sonnenschirm", false);
        var lastenrad = new Ressource("Lastenrad", Ressourcenart.Fahrzeug, "für Plakate und Infostand", false);
        var wagen = new Ressource("Dienstwagen", Ressourcenart.Fahrzeug, "nur mit Freigabe der Geschäftsführung", true);
        foreach (var r in new[] { raum, saal, beamer, infostand, lastenrad, wagen })
        {
            daten.Hinzufuegen(r);
        }

        var kommende = await termine.TermineAsync(heute.ToDateTime(TimeOnly.MinValue), heute.AddDays(60).ToDateTime(TimeOnly.MinValue), abbruch);
        foreach (var t in kommende.Where(t => t.Art == Terminart.Fraktionssitzung).Take(3))
        {
            daten.Hinzufuegen(new Buchung(raum.Id, P("kalbers"), t.Beginn.AddMinutes(-30), t.Ende.AddMinutes(30), "Fraktionssitzung", t.Id, false, jetzt));
        }

        if (kommende.FirstOrDefault(t => t.Art == Terminart.Wahlkampf) is { } infostandTermin)
        {
            daten.Hinzufuegen(new Buchung(infostand.Id, P("fbaeumer"), infostandTermin.Beginn.AddHours(-1), infostandTermin.Ende.AddHours(1), infostandTermin.Titel,
                infostandTermin.Id, false, jetzt));
            daten.Hinzufuegen(new Buchung(lastenrad.Id, P("fbaeumer"), infostandTermin.Beginn.AddHours(-1), infostandTermin.Ende.AddHours(1), "Transport Infostand",
                infostandTermin.Id, false, jetzt));
        }

        if (kommende.FirstOrDefault(t => t.Titel.StartsWith("Infoabend", StringComparison.Ordinal)) is { } infoabend)
        {
            var b = new Buchung(wagen.Id, P("lkuehlert"), infoabend.Beginn.AddHours(-1), infoabend.Ende.AddHours(1), "Fahrt zum Infoabend Grundschulplanung", infoabend.Id, true, jetzt);
            daten.Hinzufuegen(b);
            daten.Hinzufuegen(new Aufgabe($"Buchung freigeben: Dienstwagen am {b.Von:dd.MM. HH:mm}", "Fahrt zum Infoabend Grundschulplanung (Liz Kühlert)", Rolle.Administration,
                "Buchung", b.Id, "ressourcen?ansicht=freigaben", jetzt, jetzt.AddDays(3)));

            // Besetzung „Vorstand bestätigt“ mit zwei Interessierten
            var a = new Besetzungsanfrage(infoabend.Id, 2, Application.Gemeinsam.Ortszeit.Zeitpunkt(infoabend.Beginn).AddDays(-2), Besetzungsmodus.VorstandBestaetigt,
                P("jwilp"), jetzt, "Bitte jemand aus dem Ausschuss für Schule und Bildung.");
            daten.Hinzufuegen(a);
            daten.Hinzufuegen(new Besetzungsmeldung(a.Id, P("lkuehlert"), jetzt));
            daten.Hinzufuegen(new Besetzungsmeldung(a.Id, P("hakkus"), jetzt.AddMinutes(5)));
        }

        if (kommende.FirstOrDefault(t => t.Titel.StartsWith("Jubiläum", StringComparison.Ordinal)) is { } jubilaeum)
        {
            // „Wer zuerst kommt“: ein Platz, schon vergeben, eine Person auf der Warteliste
            var a = new Besetzungsanfrage(jubilaeum.Id, 1, Application.Gemeinsam.Ortszeit.Zeitpunkt(jubilaeum.Beginn).AddDays(-1), Besetzungsmodus.WerZuerstKommt,
                P("ebrinkhaus"), jetzt, null);
            daten.Hinzufuegen(a);
            var m1 = new Besetzungsmeldung(a.Id, P("bkurney"), jetzt);
            m1.Bestaetigen();
            daten.Hinzufuegen(m1);
            daten.Hinzufuegen(new Besetzungsmeldung(a.Id, P("kschwenken"), jetzt.AddMinutes(10)));
            termine.Hinzufuegen(new Teilnahme(jubilaeum.Id, P("bkurney"), Teilnahmestatus.Zugesagt, jetzt));
        }

        // Vergangener öffentlicher Termin: Anna war dabei, Bericht fehlt noch („Kurzbericht anlegen“)
        var vorbei = heute.AddDays(-3).ToDateTime(new TimeOnly(18, 0));
        var t2 = new Termin("Bürgerversammlung zur Grundschulplanung", Terminart.Oeffentlich, vorbei, vorbei.AddHours(2), "Aula einer Grundschule", null, null,
            jetzt.AddDays(-14));
        termine.Hinzufuegen(t2);
        var a2 = new Besetzungsanfrage(t2.Id, 2, jetzt.AddDays(-5), Besetzungsmodus.VorstandBestaetigt, P("jwilp"), jetzt.AddDays(-14), null);
        daten.Hinzufuegen(a2);
        foreach (var k in new[] { "lkuehlert", "seisenrichter" })
        {
            var m = new Besetzungsmeldung(a2.Id, P(k), jetzt.AddDays(-12));
            m.Bestaetigen();
            daten.Hinzufuegen(m);
            termine.Hinzufuegen(new Teilnahme(t2.Id, P(k), Teilnahmestatus.Zugesagt, jetzt.AddDays(-12)));
        }
    }
}
