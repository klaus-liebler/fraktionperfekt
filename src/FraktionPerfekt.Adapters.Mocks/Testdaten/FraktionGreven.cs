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
    /// <summary>RIS-Kennungen p01–p13 tragen die (erfundenen) Ausschusssitze der Demodaten, siehe <see cref="Demofraktion.Gremien"/>.</summary>
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
