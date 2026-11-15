using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>
/// Themen der Grevener Kommunalpolitik (Stand November 2026, nach öffentlichen Schlagzeilen) für realistische Demodaten.
/// Die Themen sind echt; alle Inhalte, Beschlüsse, Termine und Personen der Demo sind erfunden.
/// </summary>
public static class GrevenerThemen
{
    public static readonly string[] Fraktionstops =
    [
        "Grundschullandschaft: geplante Schließung zweier Grundschulen, Zukunft des Grundstücks der Martin-Luther-Grundschule",
        "Haushalt 2026: wachsendes Defizit, Haltung der Fraktion zu Einsparungen",
        "Prioritätenliste der geplanten Bauinvestitionen (rund 180 Mio. Euro)",
        "Badsanierung: Stadtwerke wollen das Projekt eigenständig stemmen – Folgen für den städtischen Haushalt",
        "Rathausstraße: Radfahrstreifen statt Hochbordradweg – Position der Fraktion",
        "Ganztagsanspruch ab 2026: Reicht der Platz für Grundschulkinder?",
    ];

    public const string ProtokollSchule =
        "Die Verwaltung schlägt vor, zwei Grundschulstandorte aufzugeben; diskutiert wird auch die Nachnutzung des Grundstücks der "
        + "Martin-Luther-Grundschule. Die Fraktion will zuerst wissen, wie sich Schulwege und der Ganztagsanspruch ab 2026 auswirken.";

    public const string ProtokollHaushalt =
        "Der Haushalt 2026 weist ein wachsendes Defizit aus. Bei rund 180 Mio. Euro geplanten Bauinvestitionen fehlt eine verbindliche "
        + "Prioritätenliste. Die Badsanierung wollen die Stadtwerke eigenständig stemmen.";

    public const string AntragElternbefragung1 =
        "## Beschlussvorschlag\n\nDer Rat beauftragt die Verwaltung, vor einer Entscheidung über die Schließung von Grundschulstandorten "
        + "eine Befragung der Eltern durchzuführen.\n\n## Begründung\n\nDie geplante Neuordnung der Grundschullandschaft betrifft viele Familien.";

    public const string AntragElternbefragung2 =
        "## Beschlussvorschlag\n\nDer Rat beauftragt die Verwaltung, vor einer Entscheidung über die Schließung von Grundschulstandorten "
        + "eine Befragung der Eltern durchzuführen. Dabei sind die Schulwege und der Ganztagsanspruch ab 2026 ausdrücklich abzufragen.\n\n"
        + "## Begründung\n\nDie geplante Neuordnung der Grundschullandschaft betrifft viele Familien. Auch die Zukunft des Grundstücks der "
        + "Martin-Luther-Grundschule sollte erst nach der Befragung entschieden werden.";

    public const string AntragGanztag =
        "## Beschlussvorschlag\n\nDie Verwaltung prüft, welche Räume in bestehenden Schulgebäuden für die Ganztagsbetreuung genutzt werden können, "
        + "und legt das Ergebnis bis zum Schuljahr 2026/27 vor.\n\n## Begründung\n\n- Rechtsanspruch auf Ganztag ab 2026\n- knapper Haushalt: "
        + "vorhandene Räume vor Neubau";

    public const string AntragPrioritaeten =
        "## Beschlussvorschlag\n\nFür die geplanten Bauinvestitionen von rund 180 Mio. Euro legt die Verwaltung eine verbindliche Prioritätenliste "
        + "vor, die der Rat jährlich beschließt.\n\n## Begründung\n\nDer Haushalt 2026 weist ein wachsendes Defizit aus. Ohne Prioritäten "
        + "drohen Projekte gleichzeitig zu starten und sich gegenseitig zu verteuern.";

    public const string AntragRadweg = "- Radfahrstreifen statt Hochbordradweg?\n- Sicherheit an den Einmündungen\n- Ortstermin abwarten";

    public static readonly string[] Arbeitskreistops =
    [
        "Ganztagsanspruch ab 2026: Raumbedarf je Grundschule, Rückmeldungen der Schulleitungen",
        "Neuplanung der Grundschullandschaft: Schulwege, Zügigkeit, Gespräch mit Elternvertretungen",
        "Entwurf eines Antrags zur Prüfung von Ganztagsräumen in bestehenden Gebäuden",
    ];

    public static readonly (string Titel, Terminart Art, string? Ort, string Beschreibung)[] OeffentlicheTermine =
    [
        ("Infoabend zur Grundschulplanung", Terminart.Oeffentlich, "Aula einer Grundschule (Ort folgt)",
            "Elterninformation zur geplanten Neuordnung der Grundschulen. Fraktion mit zwei Personen vertreten."),
        ("Ortstermin Rathausstraße: Radfahrstreifen statt Hochbordradweg", Terminart.Oeffentlich, "Rathausstraße (Treffpunkt Rathaus)",
            "Begehung mit Anwohnenden und Radverkehrsinitiative."),
        ("Bürgerinformation Haushalt 2026", Terminart.Oeffentlich, "Rathaus Greven, Ratssaal", "Vorstellung des Haushaltsentwurfs und des Defizits."),
        ("Informationsabend Badsanierung", Terminart.Oeffentlich, "Ballenlager", "Stadtwerke stellen die eigenständige Umsetzung der Badsanierung vor."),
        ("Elternabend Ganztag ab 2026", Terminart.Oeffentlich, "Bürgerhaus Reckenfeld", "Wie geht es mit den Ganztagsplätzen für Grundschulkinder weiter?"),
        ("Jubiläum eines Heimatvereins (fiktiv)", Terminart.Oeffentlich, null, "Einladung liegt im Fraktionsbüro."),
        ("Infostand Marktplatz: Schulen und Ganztag", Terminart.Wahlkampf, "Marktplatz Greven", "Flyer zur Grundschulplanung im Fraktionsbüro abholen."),
        ("Infostand Wochenmarkt Reckenfeld", Terminart.Wahlkampf, "Bürgerhaus Reckenfeld", "Material im Fraktionsbüro abholen."),
    ];
}
