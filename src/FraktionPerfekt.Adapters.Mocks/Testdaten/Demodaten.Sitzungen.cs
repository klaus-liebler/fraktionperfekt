using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Demodaten Iteration 2: ein freigegebenes Protokoll, eine vorbereitete nächste Fraktionssitzung, Aufgaben, Themenpaten.</summary>
internal static class SitzungsDemodaten
{
    public static async Task AnlegenAsync(IDaten daten, ITerminSpeicher termine, IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt,
        CancellationToken abbruch)
    {
        var fraktionssitzungen = (await termine.TermineAsync(heute.AddDays(-40).ToDateTime(TimeOnly.MinValue), heute.AddDays(30).ToDateTime(TimeOnly.MinValue), abbruch))
            .Where(t => t.Art == Terminart.Fraktionssitzung).ToList();
        var vergangen = fraktionssitzungen.LastOrDefault(t => t.Tag < heute);
        var naechste = fraktionssitzungen.FirstOrDefault(t => t.Tag >= heute);
        var vorsitz = personen["jwilp"].Id;
        Top? vertagt = null;

        // Freigegebenes Protokoll der letzten Fraktionssitzung
        if (vergangen is not null)
        {
            var s = new Sitzung(vergangen.Id, Sitzungsart.Fraktionssitzung, "Fraktionssitzung", null, vorsitz, jetzt.AddDays(-20));
            s.LeitungFestlegen(vorsitz, personen["kalbers"].Id);
            s.Einladen(jetzt.AddDays(-14));
            s.Beginnen(jetzt.AddDays(-10), personen["kalbers"].Id);
            daten.Hinzufuegen(s);
            var position = 0;
            Top Top(string titel, string? notiz, bool fest = true)
            {
                var t = new Top(s.Id, ++position, titel, null, fest, fest ? null : personen["lkuehlert"].Id, jetzt.AddDays(-20));
                t.NotizSetzen(notiz);
                t.AlsBehandeltMarkieren();
                daten.Hinzufuegen(t);
                return t;
            }

            Top("Begrüßung und Feststellung der Beschlussfähigkeit", "Die Fraktion ist beschlussfähig.");
            Top("Genehmigung des Protokolls der letzten Sitzung", "Ohne Änderungen genehmigt.");
            Top("Bericht des Vorstands", "Gespräch mit der Verwaltung zur Grundschulplanung; Termine der Haushaltsberatungen.");
            var schule = Top("Grundschullandschaft: geplante Schließung zweier Grundschulen", GrevenerThemen.ProtokollSchule, fest: false);
            var haushalt = Top("Haushalt 2026: Haltung der Fraktion", GrevenerThemen.ProtokollHaushalt);
            var rathausstrasse = new Top(s.Id, ++position, "Rathausstraße: Radfahrstreifen statt Hochbordradweg", "Fachliche Fragen offen; Ortstermin abwarten.", false,
                personen["ssterthues"].Id, jetzt.AddDays(-20));
            rathausstrasse.Vertagen();
            daten.Hinzufuegen(rathausstrasse);
            vertagt = rathausstrasse;
            Top("Verschiedenes", null);

            daten.Hinzufuegen(new Beschluss(Beschlussquelle.Fraktion, vergangen.Tag, schule.Titel,
                "Die Fraktion beantragt eine Elternbefragung, bevor über Schließungen entschieden wird.", Beschlussergebnis.Angenommen, 11, 0, 1, s.Id, schule.Id, vergangen.GremiumId));
            daten.Hinzufuegen(new Beschluss(Beschlussquelle.Fraktion, vergangen.Tag, haushalt.Titel,
                "Die Fraktion trägt den Haushaltsentwurf nur mit einer Prioritätenliste der Bauinvestitionen mit.", Beschlussergebnis.Angenommen, 9, 2, 1, s.Id, haushalt.Id,
                vergangen.GremiumId));
            foreach (var (k, status) in new[] { ("jwilp", Anwesenheitsstatus.Anwesend), ("ebrinkhaus", Anwesenheitsstatus.Anwesend), ("djoud", Anwesenheitsstatus.Anwesend),
                         ("lkuehlert", Anwesenheitsstatus.Anwesend), ("thorstmann", Anwesenheitsstatus.Entschuldigt), ("bkurney", Anwesenheitsstatus.Anwesend),
                         ("mgries", Anwesenheitsstatus.Anwesend), ("seisenrichter", Anwesenheitsstatus.Anwesend), ("kschwenken", Anwesenheitsstatus.Anwesend),
                         ("ssterthues", Anwesenheitsstatus.Anwesend), ("uahrens", Anwesenheitsstatus.Anwesend), ("hakkus", Anwesenheitsstatus.Anwesend),
                         ("acleve", Anwesenheitsstatus.Entschuldigt), ("kalbers", Anwesenheitsstatus.Anwesend) })
            {
                daten.Hinzufuegen(new Sitzungsanwesenheit(s.Id, personen[k].Id, status));
            }

            daten.Hinzufuegen(new Aufgabe("Fragen zur Elternbefragung an die Schulverwaltung schicken", $"Aus {vergangen.Titel}, TOP „{schule.Titel}“ [TOP {schule.Id:N}]", null,
                "Protokoll", s.Id, $"sitzungen/{s.Id}", jetzt.AddDays(-10), jetzt.AddDays(5), personen["lkuehlert"].Id).MitThema("Schule und Bildung"));
            daten.Hinzufuegen(new Aufgabe("Prioritätenliste Bauinvestitionen mit der Kämmerei besprechen", $"Aus {vergangen.Titel}, TOP „{haushalt.Titel}“ [TOP {haushalt.Id:N}]", null,
                "Protokoll", s.Id, $"sitzungen/{s.Id}", jetzt.AddDays(-10), jetzt.AddDays(-1), personen["djoud"].Id).MitThema("Haushalt und Finanzen"));
            s.Beenden(jetzt.AddDays(-10).AddHours(3));
            s.Freigeben(vorsitz, jetzt.AddDays(-9));
        }

        // Nächste Fraktionssitzung: vorbereitet, noch nicht eingeladen; mit angemeldetem TOP
        if (naechste is not null)
        {
            var s = new Sitzung(naechste.Id, Sitzungsart.Fraktionssitzung, "Fraktionssitzung",
                Application.Gemeinsam.Ortszeit.Zeitpunkt(naechste.Beginn).AddDays(-3), vorsitz, jetzt);
            daten.Hinzufuegen(s);
            var position = 0;
            foreach (var titel in Sitzungsvorlagen.Fraktionssitzung.Take(4))
            {
                daten.Hinzufuegen(new Top(s.Id, ++position, titel, null, true, null, jetzt));
            }

            daten.Hinzufuegen(new Top(s.Id, ++position, "Badsanierung: Stadtwerke wollen das Projekt eigenständig stemmen", "Was bedeutet das für den Haushalt?", false,
                personen["djoud"].Id, jetzt));
            if (vertagt is not null)
            {
                daten.Hinzufuegen(new Top(s.Id, ++position, vertagt.Titel, vertagt.Beschreibung, false, vertagt.AngemeldetVonPersonId, jetzt, null, vertagt.Id));
                vertagt.InSitzungUebernommen(s.Id);
            }

            daten.Hinzufuegen(new Top(s.Id, ++position, "Anträge", null, true, null, jetzt));
            daten.Hinzufuegen(new Top(s.Id, ++position, "Verschiedenes", null, true, null, jetzt));
        }

        // Korbaufgabe für den Vorstand und Themenpaten
        daten.Hinzufuegen(new Aufgabe("Pressetermin zur Grundschulplanung abstimmen", "Mit Elternvertretungen und der Presse einen gemeinsamen Termin finden.", Rolle.Vorstand,
            "Demo", Guid.Empty, "aufgaben", jetzt, jetzt.AddDays(7)).MitThema("Schule und Bildung"));
        foreach (var (feld, k, v) in new[]
                 {
                     ("Schule und Bildung", "lkuehlert", "seisenrichter"), ("Haushalt und Finanzen", "djoud", "jwilp"),
                     ("Stadtentwicklung und Verkehr", "mgries", "ssterthues"), ("Soziales und Familie", "bkurney", "acleve"),
                     ("Klima und Umwelt", "ssterthues", "uahrens"),
                 })
        {
            daten.Hinzufuegen(new Themenpate(feld, personen[k].Id, personen[v].Id));
        }
    }
}
