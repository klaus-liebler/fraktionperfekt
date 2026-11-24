using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Suche;

/// <summary>Eine Funktion, die die Alles-Suche anbietet („Abwesenheit melden“), mit Stichwörtern und Recht.</summary>
public sealed record Funktion(string Titel, string Stichwoerter, string Ziel, Func<Rolle?, Freigabe> Recht);

/// <summary>
/// Funktionen für die Alles-Suche (aus SchoolCraft FA-3231): Tippen „abwes“ bietet „Abwesenheit melden“ an. Angeboten wird nur, was die
/// aktive Rolle darf (dieselben Regeln wie Menü und Schnellaktionen).
/// </summary>
public static class Funktionskatalog
{
    private static Freigabe KeinKampagnenteam(Rolle? r) => r is Rolle.Helferin or null ? Freigabe.Nein("Nur für die Fraktion, nicht für das Kampagnenteam.") : Freigabe.Ja;

    private static Freigabe JedeRolle(Rolle? r) => r is null ? Freigabe.Nein("Bitte anmelden.") : Freigabe.Ja;

    public static IReadOnlyList<Funktion> Funktionen { get; } =
    [
        new("Abwesenheit melden", "abwesend urlaub krank verhindert fehlen", "abwesenheit?neu=1", Rechte.AbwesenheitMelden),
        new("Termin anlegen", "termin kalender neu eintragen", "kalender?neu=1", Rechte.TermineAnlegen),
        new("Raum buchen", "raum ressource beamer lastenrad dienstwagen buchung", "ressourcen?neu=1", JedeRolle),
        new("Antrag starten", "antrag neu schreiben", "antraege?neu=1", KeinKampagnenteam),
        new("Abstimmung starten", "abstimmung umfrage meinungsbild terminfindung geheim", "abstimmungen", KeinKampagnenteam),
        new("Wahl vorbereiten", "wahl wahlen amt vorstand vorsitz kassier sprecher benennung kandidatur", "wahlen", Rechte.SitzungenSteuern),
        new("Wahlen und Ämter", "wahl wahlen amt aemter kandidieren kandidatur amtszeit", "wahlen", KeinKampagnenteam),
        new("Fraktionssitzung einladen", "sitzung einladung tagesordnung", "sitzungen", Rechte.SitzungenSteuern),
        new("Anfrage an die Verwaltung schreiben", "anfrage verwaltung", "anfragen", KeinKampagnenteam),
        new("Bürgeranliegen erfassen", "anliegen buerger buergerin", "anliegen", KeinKampagnenteam),
        new("Pressemitteilung schreiben", "presse mitteilung", "presse", KeinKampagnenteam),
        new("Kontakt anlegen", "kontakt adresse adressbuch", "kontakte", KeinKampagnenteam),
        new("Datei ablegen", "datei hochladen upload dokument", "dateien", JedeRolle),
        new("Wiki-Seite anlegen", "wiki seite wissen", "wiki", JedeRolle),
        new("Plakat erfassen", "plakat kampagne wahlkampf", "handy/plakat", JedeRolle),
        new("Auslage einreichen", "auslage beleg erstattung quittung", "auslagen", JedeRolle),
        new("Kassenbericht", "kasse finanzen budget bericht", "finanzen", Rechte.FinanzenEinsehen),
        new("Kalender-Abo einrichten", "kalender abo ics push handy mitteilungen", "kalender-abo", JedeRolle),
        new("Hilfe und Tastenkürzel", "hilfe tastenkuerzel kuerzel tastatur bedienung", "hilfe", JedeRolle),
    ];

    /// <summary>Passende Funktionen, die die Rolle ausführen darf, beste zuerst.</summary>
    public static IReadOnlyList<Funktion> Suchen(string? eingabe, Rolle? rolle, int hoechstens = 5)
        => string.IsNullOrWhiteSpace(eingabe)
            ? []
            : Unscharf.Ordnen(eingabe, Funktionen.Where(f => f.Recht(rolle).Erlaubt), f => $"{f.Titel} {f.Stichwoerter}", hoechstens);
}
