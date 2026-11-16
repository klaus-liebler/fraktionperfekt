using System.Text;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Wissen;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>
/// Wiki-Seiten und Dateien zu Grevener Themen. Absichtlich mit Pflegefällen für die KI: eine Dublette („Pressearbeit“/„Presse-Arbeit“),
/// veraltete Angaben (Stand 2023/2024), eine Seite ohne neuere Fraktionsbeschlüsse und eine verwaiste Seite.
/// </summary>
internal static class WissenDemodaten
{
    public static async Task AnlegenAsync(IDaten daten, IStammdatenSpeicher stammdaten, IWikiSuche suche, IDateiablage ablage,
        IReadOnlyDictionary<string, Person> personen, DateTimeOffset jetzt, CancellationToken abbruch)
    {
        Guid P(string k) => personen[k].Id;
        var gremien = await stammdaten.GremienAsync(abbruch);
        Guid? G(string kurz) => gremien.FirstOrDefault(g => g.Kurzname == kurz)?.Id;

        var seiten = new (string Bereich, string Titel, string Text, int TageAlt, string Autor, bool Vertraulich, string? Gremium)[]
        {
            ("Abläufe", "Fraktionssitzung",
                "Die Fraktion tagt in der Regel montags vor den Rats- und Ausschusssitzungen im Fraktionsbüro im Rathaus Greven.\n\n"
                + "## Ablauf\n\n- Einladung mit Tagesordnung eine Woche vorher\n- Vorbesprechung der Ausschuss-Tagesordnungen\n- Beschlüsse der Fraktion festhalten\n- Protokoll innerhalb einer Woche freigeben\n\n"
                + "## Siehe auch\n\n- [[Antrag stellen]]\n- [[Vertretung im Ausschuss]]\n- [[Haushaltsberatungen]]", 120, "kalbers", false, null),
            ("Abläufe", "Antrag stellen",
                "Anträge entstehen gemeinsam im Antragsmodul: Entwurf, Kommentare, Abstimmung in der Fraktion, Einreichung bei der Verwaltung.\n\n"
                + "## Fristen\n\n- Einreichung spätestens **zwei Wochen** vor der Ratssitzung\n- Dringlichkeitsanträge nur nach Absprache mit dem Vorstand\n\n"
                + "## Siehe auch\n\n- [[Fraktionssitzung]]", 90, "kalbers", false, null),
            ("Abläufe", "Vertretung im Ausschuss",
                "Wer verhindert ist, meldet die Abwesenheit in FraktionPerfekt. Die Stellvertretungen werden automatisch angefragt; wer zuerst zusagt, vertritt.\n\n"
                + "Siehe [[Fraktionssitzung]].", 60, "ebrinkhaus", false, null),
            ("Politikfelder", "Grundschullandschaft",
                "## Position der Fraktion\n\nWir wollen wohnortnahe Grundschulen erhalten und eine Elternbefragung vor jeder Schließung.\n\n"
                + "## Aktuelle Themen\n\n- Neuordnung der Grundschulstandorte\n- Nachnutzung des Grundstücks der Martin-Luther-Grundschule\n- Ganztagsanspruch ab 2026\n\n"
                + "## Ansprechpartner (Themenpate)\n\nLiz Kühlert, [[Arbeitskreis Schule und Ganztag]]", 240, "lkuehlert", false, null),
            ("Politikfelder", "Haushaltsberatungen",
                "## Position der Fraktion\n\nKeine neuen Projekte ohne Prioritätenliste. Der Haushalt muss genehmigungsfähig bleiben.\n\n"
                + "## Aktuelle Themen\n\nDer Haushalt ist derzeit (Stand 2024) ausgeglichen; Investitionen von aktuell 120 Mio. Euro sind bis 2024 geplant.\n\n"
                + "## Siehe auch\n\n- [[Fraktionssitzung]]", 400, "djoud", false, null),
            ("Politikfelder", "Radverkehr Rathausstraße",
                "Die Verwaltung plant einen Radfahrstreifen statt des Hochbordradwegs. Die Fraktion will vorher einen Ortstermin mit Anliegern.\n\n"
                + "Geplant ist der Umbau ab 2023.", 500, "thorstmann", false, null),
            ("Gremien", "Arbeitskreis Schule und Ganztag",
                "## Aufgaben des Gremiums\n\nBereitet die Themen für den Ausschuss für Schule und Bildung vor.\n\n## Laufende Themen\n\n- [[Grundschullandschaft]]\n- Ganztag",
                80, "lkuehlert", false, "AK Schule"),
            ("Gremien", "Haupt-/Finanz- und Wirtschaftsausschuss",
                "## Aufgaben des Gremiums\n\nHaushalt, Finanzen, Beteiligungen (u. a. Stadtwerke Greven).\n\n## Laufende Themen\n\n- [[Haushaltsberatungen]]\n- Badsanierung durch die Stadtwerke",
                70, "jwilp", false, "HFWA"),
            ("Positionen", "Pressearbeit",
                "Pressemitteilungen gibt der Vorstand frei. Ansprechpartner für die Grevener Zeitung ist der Fraktionsvorsitzende.", 150, "jwilp", false, null),
            ("Positionen", "Presse-Arbeit",
                "Pressemitteilungen schreibt die Geschäftsführung; der Vorstand gibt frei. Fotos nur mit Einverständnis.", 30, "kalbers", false, null),
            ("Hintergrund", "Personalfragen der Fraktion",
                "Vertraulich: Stellenplan der Geschäftsstelle, Vertragslaufzeiten.", 20, "kalbers", true, null),
        };

        foreach (var s in seiten)
        {
            var am = jetzt.AddDays(-s.TageAlt);
            var seite = new WikiSeite(s.Bereich, s.Titel, s.Gremium is null ? null : G(s.Gremium), s.Vertraulich, am);
            seite.NeueVersion(am);
            daten.Hinzufuegen(seite);
            daten.Hinzufuegen(new WikiVersion(seite.Id, 1, s.Text, P(s.Autor), "Erste Fassung", am));
            await suche.AktualisierenAsync(seite.Id, seite.Titel, s.Text, abbruch);
        }

        var dateien = new (string Ordner, string Name, string Inhalt, string Von, string? Gremium, bool Vertraulich)[]
        {
            ("Fraktion", "Geschäftsordnung der Fraktion.md", "# Geschäftsordnung der Fraktion (fiktiv)\n\n§ 1 Die Fraktion tagt in der Regel montags.\n", "kalbers", null, false),
            ("Gremien/HFWA", "Haushalt 2026 – Eckdaten.csv", "Bereich;Ansatz 2026 (Mio. Euro)\nSchulen;48\nStraßen;22\nBäder;15\n", "djoud", "HFWA", false),
            ("Gremien/HFWA", "Stellungnahme Stadtwerke Bad.txt", "Fiktive Stellungnahme zur Badsanierung durch die Stadtwerke (nichtöffentlich).", "jwilp", "HFWA", true),
            ("Arbeitskreise/AK Schule", "Schulwege-Übersicht.txt", "Fiktive Übersicht der Schulwege bei Schließung zweier Grundschulstandorte.", "lkuehlert", "AK Schule", false),
            ("Wahlkampf", "Plakatstandorte-Hinweise.txt", "Plakate nicht an Ampeln und Kreuzungen; Abstand zu Schulen beachten.", "fbaeumer", null, false),
        };
        foreach (var d in dateien)
        {
            var bytes = Encoding.UTF8.GetBytes(d.Inhalt);
            var schluessel = $"{d.Ordner}/{Domain.Gemeinsam.Ids.Neu():N}-{d.Name}";
            using var strom = new MemoryStream(bytes);
            await ablage.SpeichernAsync(schluessel, strom, abbruch);
            var typ = Path.GetExtension(d.Name) switch { ".md" => "text/markdown", ".csv" => "text/csv", _ => "text/plain" };
            daten.Hinzufuegen(new Datei(d.Ordner, d.Name, typ, bytes.Length, schluessel,
                Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)), P(d.Von), jetzt.AddDays(-3), d.Gremium is null ? null : G(d.Gremium), d.Vertraulich));
        }

        await daten.SpeichernAsync(abbruch);
    }
}
