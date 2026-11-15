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
