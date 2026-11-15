using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Demodaten Iteration 3: Anträge in allen Phasen zu Grevener Themen, offene Abstimmungen und eine Terminfindung.</summary>
internal static class AntragsDemodaten
{
    public static async Task AnlegenAsync(IDaten daten, IStammdatenSpeicher stammdaten, ITerminSpeicher termine, IReadOnlyDictionary<string, Person> personen,
        DateOnly heute, DateTimeOffset jetzt, CancellationToken abbruch)
    {
        var gremien = (await stammdaten.GremienAsync(abbruch)).Where(g => g.RisId is not null).ToDictionary(g => g.Kurzname);
        var mitglieder = personen.Values.Where(p => p.RollenAm(heute, jetzt).Contains(Rolle.Mitglied)).ToList();
        Guid P(string k) => personen[k].Id;

        // 1. Beschlossen und eingereicht, der nächsten ASB-Sitzung zugeordnet
        var asb = gremien["ASB"];
        var naechsteAsb = (await termine.TermineAsync(heute.ToDateTime(TimeOnly.MinValue), heute.AddDays(90).ToDateTime(TimeOnly.MinValue), abbruch))
            .FirstOrDefault(t => t.GremiumId == asb.Id);
        var a1 = new Antrag("Elternbefragung vor der Schließung von Grundschulstandorten", P("awewers"), asb.Id, "Schule und Bildung", jetzt.AddDays(-30));
        daten.Hinzufuegen(a1);
        daten.Hinzufuegen(new AntragFassung(a1.Id, 1, GrevenerThemen.AntragElternbefragung1, P("awewers"), jetzt.AddDays(-30), "Erste Fassung"));
        daten.Hinzufuegen(new AntragFassung(a1.Id, 2, GrevenerThemen.AntragElternbefragung2, P("awewers"), jetzt.AddDays(-25), "Schulwege und Ganztag ergänzt"));
        a1.Wechseln(Antragsstatus.Entwurf, jetzt.AddDays(-25));
        var u1 = new Umfrage($"Antrag: {a1.Titel}", "Abgestimmt wird Fassung 2.", Umfrageart.Antragsabstimmung, jetzt.AddDays(-20).AddHours(1), 50, false,
            mitglieder.Count, P("tbrinkmann"), jetzt.AddDays(-23), a1.Id);
        daten.Hinzufuegen(u1);
        var optionen1 = Umfrage.JaNeinEnthaltung.Select((t, i) => new UmfrageOption(u1.Id, i + 1, t)).ToList();
        optionen1.ForEach(daten.Hinzufuegen);
        var stimmen1 = mitglieder.Take(9).Select((m, i) => new Stimme(u1.Id, optionen1[i < 8 ? 0 : 2].Id, m.Id, jetzt.AddDays(-21))).ToList();
        stimmen1.ForEach(daten.Hinzufuegen);
        a1.AbstimmungZuordnen(u1.Id, jetzt.AddDays(-23));
        var e1 = Auswertung.Auswerten(u1, optionen1, stimmen1);
        u1.Abschliessen(e1, jetzt.AddDays(-20));
        a1.Wechseln(Antragsstatus.Beschlossen, jetzt.AddDays(-20), e1.Text);
        daten.Hinzufuegen(new Beschluss(Beschlussquelle.Fraktion, heute.AddDays(-20), a1.Titel, $"Fraktionsabstimmung über den Antrag: {e1.Text}",
            Beschlussergebnis.Angenommen, 8, 0, 1, antragId: a1.Id));
        a1.Einreichen($"A-{heute.Year}-001", jetzt.AddDays(-18));
        a1.Zuordnen(asb.Id, naechsteAsb?.Id, jetzt.AddDays(-18));

        // 2. Entwurf mit Kommentar und offenem Änderungsvorschlag
        var a2 = new Antrag("Prüfauftrag: Ganztagsräume in bestehenden Schulgebäuden", P("ydemir"), asb.Id, "Schule und Bildung", jetzt.AddDays(-6));
        daten.Hinzufuegen(a2);
        daten.Hinzufuegen(new AntragFassung(a2.Id, 1, GrevenerThemen.AntragGanztag, P("ydemir"), jetzt.AddDays(-6), "Erste Fassung"));
        a2.Wechseln(Antragsstatus.Entwurf, jetzt.AddDays(-5));
        daten.Hinzufuegen(new AntragKommentar(a2.Id, 1, P("mkock"), "bis zum Schuljahr 2026/27", "Ist das realistisch? Die Verwaltung braucht Vorlauf für die Planung.", jetzt.AddDays(-3)));
        daten.Hinzufuegen(new Aenderungsvorschlag(a2.Id, 1, P("shoelscher"), "bis zum Schuljahr 2026/27", "bis zu den Sommerferien 2026",
            "Dann liegt das Ergebnis vor dem Start des Rechtsanspruchs vor.", jetzt.AddDays(-2)));

        // 3. In Abstimmung (offen, einige Stimmen)
        var hfa = gremien["HFA"];
        var a3 = new Antrag("Verbindliche Prioritätenliste für die Bauinvestitionen", P("mterhaar"), hfa.Id, "Haushalt und Finanzen", jetzt.AddDays(-10));
        daten.Hinzufuegen(a3);
        daten.Hinzufuegen(new AntragFassung(a3.Id, 1, GrevenerThemen.AntragPrioritaeten, P("mterhaar"), jetzt.AddDays(-10), "Erste Fassung"));
        a3.Wechseln(Antragsstatus.Entwurf, jetzt.AddDays(-8));
        var u3 = new Umfrage($"Antrag: {a3.Titel}", "Abgestimmt wird Fassung 1.", Umfrageart.Antragsabstimmung, jetzt.AddDays(3), 50, false, mitglieder.Count,
            P("tbrinkmann"), jetzt.AddDays(-1), a3.Id);
        daten.Hinzufuegen(u3);
        var optionen3 = Umfrage.JaNeinEnthaltung.Select((t, i) => new UmfrageOption(u3.Id, i + 1, t)).ToList();
        optionen3.ForEach(daten.Hinzufuegen);
        foreach (var (k, o) in new[] { ("tbrinkmann", 0), ("mterhaar", 0), ("plammers", 0), ("rniehues", 1) })
        {
            daten.Hinzufuegen(new Stimme(u3.Id, optionen3[o].Id, P(k), jetzt.AddHours(-20)));
        }

        a3.AbstimmungZuordnen(u3.Id, jetzt.AddDays(-1));

        // 4. Idee
        var a4 = new Antrag("Sichere Radwegführung an der Rathausstraße", P("dschulze"), gremien["ASV"].Id, "Stadtentwicklung und Verkehr", jetzt.AddDays(-2));
        daten.Hinzufuegen(a4);
        daten.Hinzufuegen(new AntragFassung(a4.Id, 1, GrevenerThemen.AntragRadweg, P("dschulze"), jetzt.AddDays(-2), "Stichpunkte"));

        // Meinungsbild und Terminfindung
        var m = new Umfrage("Soll die Fraktion die eigenständige Badsanierung durch die Stadtwerke unterstützen?", "Meinungsbild vor der HFA-Sitzung.",
            Umfrageart.Meinungsbild, jetzt.AddDays(4), 0, false, personen.Values.Count(p => p.RollenAm(heute, jetzt).Any(r => r != Rolle.Helferin)), P("shoelscher"), jetzt);
        daten.Hinzufuegen(m);
        var mo = Umfrage.JaNeinEnthaltung.Select((t, i) => new UmfrageOption(m.Id, i + 1, t)).ToList();
        mo.ForEach(daten.Hinzufuegen);
        daten.Hinzufuegen(new Stimme(m.Id, mo[0].Id, P("mterhaar"), jetzt));
        daten.Hinzufuegen(new Stimme(m.Id, mo[1].Id, P("rniehues"), jetzt));

        var t = new Umfrage("Klausurtagung zum Haushalt 2026", "Ganztägig, Fraktionsbüro.", Umfrageart.Terminfindung, jetzt.AddDays(5), 0, true,
            personen.Values.Count(p => p.RollenAm(heute, jetzt).Any(r => r != Rolle.Helferin)), P("tbrinkmann"), jetzt);
        daten.Hinzufuegen(t);
        var samstag = heute.AddDays(((int)DayOfWeek.Saturday - (int)heute.DayOfWeek + 7) % 7 + 14);
        var to = new[] { samstag, samstag.AddDays(7), samstag.AddDays(14) }
            .Select((tag, i) => new UmfrageOption(t.Id, i + 1, tag.ToDateTime(new TimeOnly(10, 0)).ToString("ddd dd.MM.yyyy, HH:mm 'Uhr'", System.Globalization.CultureInfo.GetCultureInfo("de-DE")),
                tag.ToDateTime(new TimeOnly(10, 0)))).ToList();
        to.ForEach(daten.Hinzufuegen);
        daten.Hinzufuegen(new Stimme(t.Id, to[1].Id, P("tbrinkmann"), jetzt));
        daten.Hinzufuegen(new Stimme(t.Id, to[2].Id, P("tbrinkmann"), jetzt));
        daten.Hinzufuegen(new Stimme(t.Id, to[1].Id, P("shoelscher"), jetzt));
    }
}
