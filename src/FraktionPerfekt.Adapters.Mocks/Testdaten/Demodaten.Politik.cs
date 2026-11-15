using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Politik;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Anfragen, Kontakte, Bürgeranliegen und Pressearbeit zu Grevener Themen (alle Personen und Kontakte erfunden).</summary>
internal static class PolitikDemodaten
{
    public static async Task AnlegenAsync(IDaten daten, IStammdatenSpeicher stammdaten, IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt,
        CancellationToken abbruch)
    {
        Guid P(string k) => personen[k].Id;
        var gremien = await stammdaten.GremienAsync(abbruch);
        Guid? G(string kurz) => gremien.FirstOrDefault(g => g.Kurzname == kurz)?.Id;

        // Anfragen: eine beantwortet, eine überfällig, eine offen, ein Entwurf
        var schulwege = new Anfrage("Schulwege bei Schließung zweier Grundschulstandorte",
            "1. Wie verlängern sich die Schulwege der betroffenen Kinder?\n2. Welche Kosten entstehen für Schülerbeförderung?", P("awewers"), G("ASB"), "Schule und Bildung",
            jetzt.AddDays(-50));
        schulwege.Einreichen(heute.AddDays(-45), heute.AddDays(-17));
        schulwege.Beantworten("Die Verwaltung rechnet mit bis zu 1,8 km längeren Wegen; Beförderungskosten rund 90.000 Euro jährlich (fiktiv).", heute.AddDays(-20));
        var bad = new Anfrage("Finanzierung der Badsanierung durch die Stadtwerke",
            "1. Welche Bürgschaften oder Zuschüsse der Stadt sind vorgesehen?\n2. Wie wirkt sich das auf den Haushalt 2026 aus?", P("mterhaar"), G("HFA"), "Haushalt und Finanzen",
            jetzt.AddDays(-40));
        bad.Einreichen(heute.AddDays(-35), heute.AddDays(-7));
        var radweg = new Anfrage("Unfallzahlen Rathausstraße", "Wie viele Unfälle mit Radfahrenden gab es seit 2021 an den Einmündungen der Rathausstraße?",
            P("jovermeyer"), G("ASV"), "Stadtentwicklung und Verkehr", jetzt.AddDays(-10));
        radweg.Einreichen(heute.AddDays(-8), heute.AddDays(20));
        var ganztag = new Anfrage("Raumbedarf für den Ganztag", "Welche Räume fehlen an den Grundschulen für den Rechtsanspruch ab 2026?", P("mkock"), G("ASB"),
            "Schule und Bildung", jetzt.AddDays(-2));
        foreach (var a in new[] { schulwege, bad, radweg, ganztag })
        {
            daten.Hinzufuegen(a);
        }

        // Kontakte (fiktiv)
        foreach (var (name, org, fkt, mail, themen) in new[]
                 {
                     ("Dr. Hanna Vogt", "Stadt Greven (fiktiv)", "Fachdienst Schule", "schule@stadt.example", "Schule, Ganztag"),
                     ("Bernd Kortmann", "Stadtwerke (fiktiv)", "Geschäftsführung Bäder", "baeder@stadtwerke.example", "Bad, Haushalt"),
                     ("Lisa Brockmann", "Lokalzeitung (fiktiv)", "Redaktion Greven", "redaktion@zeitung.example", "Presse"),
                     ("Werner Pohlmann", "Heimatverein (fiktiv)", "Vorsitzender", "info@heimatverein.example", "Ortsbild, Ehrenamt"),
                     ("Sandra Elsing", "Schulpflegschaft Grundschule (fiktiv)", "Vorsitzende", null, "Schule, Elternbefragung"),
                 })
        {
            daten.Hinzufuegen(new Kontakt(name, org, fkt, mail, null, themen, null, P("kalbers"), jetzt.AddDays(-30)));
        }

        // Bürgeranliegen: neu, zugewiesen, erledigt
        var ampel = new Buergeranliegen("Querungshilfe an der Rathausstraße", "Mein Sohn quert täglich die Rathausstraße auf dem Schulweg. Gibt es Pläne für eine Ampel?",
            "Familie Terwey (fiktiv)", "terwey@mail.example", Eingangskanal.Mail, "Stadtentwicklung und Verkehr", jetzt.AddDays(-1));
        var schule = new Buergeranliegen("Zukunft der Grundschule im Ortsteil", "Wir machen uns Sorgen, dass unsere Grundschule geschlossen wird.",
            "Herr Lütke-Wöstmann (fiktiv)", "0257 1234", Eingangskanal.Sprechstunde, "Schule und Bildung", jetzt.AddDays(-6));
        schule.Zuweisen(P("awewers"));
        var bank = new Buergeranliegen("Bank am Emsradweg defekt", "Die Bank am Emsradweg ist seit Wochen kaputt.", "Frau Hölker (fiktiv)", null, Eingangskanal.Telefon,
            "Klima und Umwelt", jetzt.AddDays(-25));
        bank.Zuweisen(P("rniehues"));
        bank.Erledigen("An den Baubetriebshof weitergegeben; Reparatur ist erfolgt.", jetzt.AddDays(-12));
        foreach (var a in new[] { ampel, schule, bank })
        {
            daten.Hinzufuegen(a);
        }

        daten.Hinzufuegen(new Aufgabe($"Anliegen zuweisen: {ampel.Betreff}", "Bürgeranliegen eingegangen – bitte einer Person zuweisen.", Rolle.Administration, "Anliegen",
            ampel.Id, "anliegen", jetzt.AddDays(-1), jetzt.AddDays(1)));
        daten.Hinzufuegen(new Aufgabe($"Anliegen beantworten: {schule.Betreff}", schule.Text, null, "Anliegen", schule.Id, "anliegen", jetzt.AddDays(-5), jetzt.AddDays(9),
            P("awewers")).MitThema("Schule und Bildung"));

        // Presse: versendet mit Resonanz, zur Freigabe, Entwurf
        var pmSchule = new Pressemitteilung("CDU-Fraktion fordert Elternbefragung vor Schulschließungen",
            "Die CDU-Fraktion Greven beantragt, vor jeder Entscheidung über Grundschulstandorte die Eltern zu befragen (fiktive Mitteilung).",
            heute.AddDays(-14), "Schule und Bildung", P("kalbers"), jetzt.AddDays(-16));
        pmSchule.ZurFreigabe();
        pmSchule.Freigeben(P("tbrinkmann"), jetzt.AddDays(-15));
        pmSchule.Veroeffentlichen(jetzt.AddDays(-14));
        var pmHaushalt = new Pressemitteilung("Haushalt 2026: Prioritäten statt Gießkanne",
            "Die Fraktion trägt den Haushalt nur mit einer verbindlichen Prioritätenliste der Bauinvestitionen mit (fiktive Mitteilung).", heute.AddDays(3),
            "Haushalt und Finanzen", P("mterhaar"), jetzt.AddDays(-2));
        pmHaushalt.ZurFreigabe();
        var pmRad = new Pressemitteilung("Ortstermin an der Rathausstraße", "Einladung zum Ortstermin mit Anliegern (Entwurf).", heute.AddDays(10), "Stadtentwicklung und Verkehr",
            P("jovermeyer"), jetzt.AddDays(-1));
        foreach (var m in new[] { pmSchule, pmHaushalt, pmRad })
        {
            daten.Hinzufuegen(m);
        }

        daten.Hinzufuegen(new Aufgabe($"Pressemitteilung freigeben: {pmHaushalt.Titel}", null, Rolle.Vorstand, "Pressemitteilung", pmHaushalt.Id, "presse", jetzt.AddDays(-2),
            jetzt.AddDays(2)));
        daten.Hinzufuegen(new Presseartikel("Lokalzeitung (fiktiv)", "Eltern sollen bei Grundschulen mitreden", heute.AddDays(-13), null,
            "Bericht über den Antrag der CDU-Fraktion zur Elternbefragung.", "Schule und Bildung", Tonlage.Positiv, pmSchule.Id, P("kalbers")));
        daten.Hinzufuegen(new Presseartikel("Lokalradio (fiktiv)", "Streit um Grundschulstandorte", heute.AddDays(-12), null,
            "Kurzbeitrag; Kritik der Verwaltung an der Verzögerung.", "Schule und Bildung", Tonlage.Kritisch, pmSchule.Id, P("kalbers")));
        await daten.SpeichernAsync(abbruch);
    }
}
