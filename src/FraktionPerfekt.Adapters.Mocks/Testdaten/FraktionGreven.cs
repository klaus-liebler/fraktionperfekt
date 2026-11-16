using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>
/// Die echten Mitglieder der CDU-Fraktion Greven (Ratsmitglieder und sachkundige Bürger:innen, Stand der Personenübersicht im RIS,
/// November 2026). Zentrale Liste: Ergänzungen und Änderungen nur hier. Funktionen nur, soweit sie in der Liste stehen
/// (Vorsitz, Stellvertretungen, Beisitz, Schatzmeister, Medienbeauftragte, 1. stellv. Bürgermeisterin). E-Mail-Adressen nur, soweit
/// geliefert; alle anderen bekommen „…@demo.invalid“ (nie erfundene echte Adressen). Ausschusssitze und alle Vorgänge in den Demodaten sind erfunden
/// und neutral gehalten (keine Krankheitsgründe, keine Geldbeträge auf echte Personen).
/// </summary>
public static class FraktionGreven
{
    public static IReadOnlyList<Demoperson> Ratsmitglieder { get; } =
    [
        new("Klaus", "Liebler", "kliebler", "ris-p14", "Ratsmitglied", [Rolle.Mitglied], "Herr", "Prof. Dr."),
        new("Johannes", "Wilp", "jwilp", "ris-p01", "Fraktionsvorsitzender", [Rolle.Vorstand, Rolle.Mitglied], "Herr", EMail: "johannes.wilp@cdu-greven.de"),
        new("Eike", "Brinkhaus", "ebrinkhaus", "ris-p02", "1. stellv. Fraktionsvorsitzender", [Rolle.Vorstand, Rolle.Mitglied], "Herr", EMail: "eike.brinkhaus@cdu-greven.de"),
        new("Philipp", "Nientiedt", "pnientiedt", "ris-p15", "2. stellv. Fraktionsvorsitzender", [Rolle.Vorstand, Rolle.Mitglied], "Herr", EMail: "philipp.wimber@cdu-greven.de"),
        new("Martinus", "Benning", "mbenning", "ris-p16", "Beisitzer im Fraktionsvorstand", [Rolle.Vorstand, Rolle.Mitglied], "Herr", EMail: "martinus.benning@cdu-greven.de"),
        new("Dames", "Joud", "djoud", "ris-p03", "Schatzmeister", [Rolle.Kassier, Rolle.Mitglied], "Herr", EMail: "dames.joud@cdu-greven.de"),
        new("Chantal", "Sluka", "csluka", "ris-p19", "Medienbeauftragte", [Rolle.Mitglied], "Frau", EMail: "chantal.sluka@cdu-greven.de"),
        new("Liz", "Kühlert", "lkuehlert", "ris-p04", "Ratsmitglied", [Rolle.Mitglied], "Frau"),
        new("Thomas", "Horstmann", "thorstmann", "ris-p05", "Ratsmitglied", [Rolle.Mitglied], "Herr"),
        new("Bettina", "Kurney", "bkurney", "ris-p06", "Ratsmitglied", [Rolle.Mitglied], "Frau"),
        new("Michael", "Gries", "mgries", "ris-p07", "Ratsmitglied", [Rolle.Mitglied], "Herr"),
        new("Silke", "Eisenrichter", "seisenrichter", "ris-p08", "Ratsmitglied", [Rolle.Mitglied], "Frau"),
        new("Klaus", "Schwenken", "kschwenken", "ris-p09", "Ratsmitglied", [Rolle.Mitglied], "Herr"),
        new("Steve", "Sterthues", "ssterthues", "ris-p13", "Ratsmitglied", [Rolle.Mitglied], "Herr"),
        new("Tim", "Schubert", "tschubert", "ris-p17", "Ratsmitglied", [Rolle.Mitglied], "Herr", "Dr."),
        new("Alfons", "Schulte Jochmaring", "aschultejochmaring", "ris-p18", "Ratsmitglied", [Rolle.Mitglied], "Herr"),
        new("Anika", "Stöcker", "astoecker", "ris-p20", "Ratsmitglied", [Rolle.Mitglied], "Frau"),
        new("Christa", "Waschkowitz-Biggeleben", "cwaschkowitzbiggeleben", "ris-p21", "Ratsmitglied, 1. stellv. Bürgermeisterin", [Rolle.Mitglied], "Frau"),
    ];

    public static IReadOnlyList<Demoperson> SachkundigeBuerger { get; } =
    [
        new("Uta", "Ahrens", "uahrens", "ris-p10", "Sachkundige Bürgerin", [Rolle.SachkundigeBuergerin], "Frau"),
        new("Hüseyin", "Akkus", "hakkus", "ris-p11", "Sachkundiger Bürger", [Rolle.SachkundigeBuergerin], "Herr"),
        new("Alexandra", "Cleve", "acleve", "ris-p12", "Sachkundige Bürgerin", [Rolle.SachkundigeBuergerin], "Frau"),
        .. Weitere(22,
            ("Tobias", "Ahlert", "Herr"), ("Wilfried", "Averbeck", "Herr"), ("Dietmar", "Beinker", "Herr"), ("Petra", "Benning", "Frau"),
            ("Thomas", "Benning", "Herr"), ("Jens", "Beuing", "Herr"), ("Antonius", "Brüning", "Herr"), ("Karl-Bernd", "Darpel", "Herr"),
            ("Florian", "Dittert", "Herr"), ("Mathias", "Drunkemühle", "Herr"), ("Thomas", "Feldhove", "Herr"), ("Jan Felix", "Große Maestrup", "Herr"),
            ("Gerwin", "Hennig", "Herr"), ("Johannes", "Hennigfeld", "Herr"), ("Alfons", "Kaup", "Herr"), ("Gerold", "Kublik", "Herr"),
            ("Matthias", "Langkamp", "Herr"), ("Adolf", "Liesenkötter", "Herr"), ("Frank", "Näfelt", "Herr"), ("Matthias", "Pache", "Herr"),
            ("Felix", "Rickermann", "Herr"), ("Markus", "Rogner", "Herr"), ("Burkhard", "Schulze Höping Pellengahr", "Herr"), ("Jeffrey", "Stolte", "Herr"),
            ("Daniel", "Tappe", "Herr"), ("Ralph", "Uennigmann", "Herr"), ("Christoph", "Wesselmann", "Herr"), ("Richard", "Wewer", "Herr"),
            ("Lukas", "Wirz", "Herr")),
    ];

    /// <summary>
    /// Gremienzugehörigkeit der Ratsmitglieder wie im RIS (Personenansicht kopiert, Stand 16.11.2026): je Person ein Block aus
    /// Namenszeile und Zeilen „Gremium TAB Funktion TAB Seit“. Weitere Personen einfach als neuen Block anhängen; gelesen wird
    /// mit <see cref="RisTabelle.Parsen"/>. Unbekannte Funktionsbezeichnungen bleiben als Text erhalten.
    /// </summary>
    public const string Gremienzugehoerigkeit = """
        Martinus Benning
        Rat	Ratsmitglied	15.04.2024
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Bauerschaften	2. Stellv. Ausschussvorsitzender	11.03.2026
        Bezirksausschuss für die Ortschaft Reckenfeld	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Chancengerechtigkeit und Integration	Stellv. Ausschussmitglied	15.07.2026
        Betriebsausschuss	Ausschussvorsitzender	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	05.11.2025
        Sozialausschuss	Ausschussmitglied	04.11.2026
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Eike Brinkhaus
        Rat	Ratsmitglied	01.06.2014
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Betriebsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Ausschussmitglied	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Ausschussvorsitzender	05.11.2025

        Silke Eisenrichter
        Rat	Ratsmitglied	01.11.2025
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Chancengerechtigkeit und Integration	Stellv. Ausschussmitglied	25.02.2026
        Jugendhilfeausschuss	1. Stellv. Ausschussvorsitzende	04.11.2026
        Ausschuss für Kultur, Sport und Freizeit	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	2. Stellv. Ausschussvorsitzende	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Michael Gries
        Rat	Ratsmitglied	01.11.2020
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Jugendhilfeausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Stellv. Ausschussmitglied	05.11.2025

        Thomas Horstmann
        Rat	Ratsmitglied	04.11.2020
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Betriebsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025
        Schulausschuss	Ausschussmitglied	04.11.2026

        Dames Joud
        Rat	Ratsmitglied	01.11.2020
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Chancengerechtigkeit und Integration	Ausschussmitglied	05.11.2025
        Betriebsausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Sozialausschuss	Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Liz Kühlert
        Rat	Ratsmitglied	01.10.1999
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Ausschussmitglied	05.11.2025
        Schulausschuss	Ausschussvorsitzende	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Bettina Kurney
        Rat	Ratsmitglied	01.11.2025
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Bauerschaften	Ausschussmitglied	05.11.2025
        Betriebsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Ausschussmitglied	05.11.2025

        Klaus Liebler
        Rat	Ratsmitglied	01.11.2025
        Wahlausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	1. Stellv. Ausschussvorsitzender	05.11.2025

        Philipp Nientiedt
        Rat	Ratsmitglied	20.02.2023
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Betriebsausschuss	1. Stellv. Ausschussvorsitzender	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Ausschussmitglied	05.11.2025

        Tim Schubert
        Rat	Ratsmitglied	01.11.2025
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	04.11.2026

        Alfons Schulte Jochmaring
        Rat	Ratsmitglied	04.11.2020
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Bauerschaften	Ausschussvorsitzender	11.03.2026
        Betriebsausschuss	2. Stellv. Ausschussvorsitzender	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	04.11.2026
        Planungs- und Bauausschuss für den Rathausneubau	Ausschussmitglied	05.11.2025
        Umlegungsausschuss	Ausschussmitglied	05.11.2025

        Klaus Schwenken
        Rat	Ratsmitglied	04.11.2020
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Ortschaft Reckenfeld	Ausschussmitglied	05.11.2025
        Betriebsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Ausschussmitglied	05.11.2025

        Chantal Sluka
        Rat	Ratsmitglied	01.11.2025
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Chancengerechtigkeit und Integration	Ausschussmitglied	15.07.2026
        Ausschuss für Kultur, Sport und Freizeit	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	04.11.2026
        Sozialausschuss	Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Steve Sterthues
        Rat	Ratsmitglied	01.11.2025
        Haupt-/Finanz- und Wirtschaftsausschuss	Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Ortschaft Reckenfeld	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025

        Anika Stöcker
        Rat	Ratsmitglied	04.11.2020
        Bezirksausschuss für die Ortschaft Reckenfeld	1. Stellv. Ausschussvorsitzende	11.02.2026
        Jugendhilfeausschuss	Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	04.11.2026
        Sozialausschuss	Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	Stellv. Ausschussmitglied	05.11.2025

        Christa Waschkowitz-Biggeleben
        Rat	1. Stellv. Bürgermeisterin	04.11.2020
        Haupt-/Finanz- und Wirtschaftsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Betriebsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Kultur, Sport und Freizeit	Stellv. Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Ausschussmitglied	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	1. Stellv. Ausschussvorsitzende	05.11.2025
        Zweckverbandsversammlung Musikschule Greven/Emsdetten/Saerbeck	Mitglied ZVM	05.11.2025

        Johannes Wilp
        Rat	Ratsmitglied	01.06.2014
        Haupt-/Finanz- und Wirtschaftsausschuss	1. Stellv. Ausschussvorsitzender	10.12.2025
        Rechnungsprüfungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Bezirksausschuss für die Ortschaft Gimbte	Ausschussvorsitzender	27.05.2026
        Bezirksausschuss für die Bauerschaften	1. Stellv. Ausschussvorsitzender	11.03.2026
        Betriebsausschuss	Stellv. Ausschussmitglied	05.11.2025
        Wahlprüfungsausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Stellv. Ausschussmitglied	05.11.2025
        Schulausschuss	Stellv. Ausschussmitglied	04.11.2026
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025
        Planungs- und Bauausschuss für den Rathausneubau	2. Stellv. Ausschussvorsitzender	05.11.2025
        Umlegungsausschuss	Stellv. Ausschussmitglied	05.11.2025
        """;

    /// <summary>Erfundene Ausschusssitze der sachkundigen Bürger:innen (Demo), bis echte Daten vorliegen. Gleiches Format.</summary>
    public const string SitzeSachkundigeDemo = """
        Uta Ahrens
        Ausschuss für Stadtentwicklung, Klima und Umwelt	Ausschussmitglied	05.11.2025
        Sozialausschuss	Stellv. Ausschussmitglied	05.11.2025

        Hüseyin Akkus
        Schulausschuss	Ausschussmitglied	05.11.2025
        Ausschuss für Chancengerechtigkeit und Integration	Stellv. Ausschussmitglied	05.11.2025

        Alexandra Cleve
        Sozialausschuss	Ausschussmitglied	05.11.2025
        Jugendhilfeausschuss	Stellv. Ausschussmitglied	05.11.2025
        """;

    /// <summary>Alle Zugehörigkeiten mit der passenden Person (Zuordnung über Vor- und Nachnamen, Titel und Anrede werden ignoriert).</summary>
    public static IReadOnlyList<(Demoperson Person, RisZugehoerigkeit Zugehoerigkeit)> Zugehoerigkeiten { get; } =
        RisTabelle.Parsen(Gremienzugehoerigkeit).Concat(RisTabelle.Parsen(SitzeSachkundigeDemo)).Select(z => (PersonZu(z.Person), z)).ToList();

    /// <summary>Person zum Namen aus der RIS-Tabelle („Prof. Dr. Klaus Liebler“, „Herr Thomas Horstmann“ …).</summary>
    public static Demoperson PersonZu(string name)
    {
        var n = " " + string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)) + " ";
        var treffer = Ratsmitglieder.Concat(SachkundigeBuerger).Where(p => n.EndsWith($" {p.Vorname} {p.Nachname} ", StringComparison.OrdinalIgnoreCase)).ToList();
        return treffer.Count == 1 ? treffer[0] : throw new InvalidOperationException($"Person „{name}“ nicht eindeutig gefunden ({treffer.Count} Treffer).");
    }

    /// <summary>Anmeldekennung aus dem Namen: erster Buchstabe des Vornamens + Nachname, klein, ohne Umlaute, Leer- und Bindestriche.</summary>
    public static string Kennung(string vorname, string nachname)
    {
        var n = (vorname[..1] + nachname).ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        return new string(n.Where(char.IsAsciiLetterOrDigit).ToArray());
    }

    private static IEnumerable<Demoperson> Weitere(int ersteNummer, params (string Vorname, string Nachname, string Anrede)[] liste)
        => liste.Select((p, i) => new Demoperson(p.Vorname, p.Nachname, Kennung(p.Vorname, p.Nachname), $"ris-p{ersteNummer + i}",
            p.Anrede == "Frau" ? "Sachkundige Bürgerin" : "Sachkundiger Bürger", [Rolle.SachkundigeBuergerin], p.Anrede));
}
