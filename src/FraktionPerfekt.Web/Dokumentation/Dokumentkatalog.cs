using System.Collections.Concurrent;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Web.Dokumentation;

/// <summary>Ein Dokument in der Anwendung (Datei unter Dokumentation/, beim Bauen aus docs/ bzw. README.md kopiert).</summary>
/// <param name="Schluessel">Adresse und Kommentarschlüssel, z. B. „doku/betrieb“ → /dokumentation/doku/betrieb.</param>
/// <param name="Datei">Pfad relativ zu docs/ (README.md liegt im Repo-Wurzelordner und wird mitkopiert).</param>
/// <param name="Archiv">Frühere Fassung als Claude-Dokument (nur Archiv).</param>
/// <param name="FuerAlle">Hilfe: alle angemeldeten Rollen; sonst nur Administration und Vorstand.</param>
public sealed record Dokument(string Schluessel, string Titel, string Gruppe, string Datei, string Beschreibung, string? Archiv = null, bool FuerAlle = false)
{
    public string Adresse => Schluessel == Dokumentkatalog.Hilfe ? "hilfe" : $"dokumentation/{Schluessel}";
}

/// <summary>Welche Dokumente die Anwendung zeigt, wer sie lesen darf, und das Laden (mit Zwischenspeicher je Dateistand). Aus VibeVHS übernommen.</summary>
public sealed class Dokumentkatalog
{
    public const string Hilfe = "hilfe";

    public const string GruppeKonzept = "Konzept";

    public const string GruppeTechnik = "Umsetzung, Abnahme und Betrieb";

    public static readonly IReadOnlyList<Dokument> Alle =
    [
        new(Hilfe, "Hilfe zur Bedienung", "Hilfe", "hilfe.md", "Grundprinzipien und Abläufe Schritt für Schritt", FuerAlle: true),
        // Erscheint, sobald das Konzept als Markdown im Repo liegt (docs/spezifikation/konzept.md); bis dahin nur der Archivlink.
        new("spezifikation/konzept", "Konzept für ein Fraktionssystem", GruppeKonzept, "spezifikation/konzept.md",
            "Ziel und Rollen, Module, Bedienung, Datenmodell, Architektur, Datenschutz, Ausbaustufen", "https://claude.ai/artifact/F6Ln4qK4YndXtGJrB8F7Mb"),
        new("doku/ueberblick", "Überblick und Iterationen", GruppeTechnik, "README.md", "Starten, Testkonten, Aufbau, Leitlinien, was je Iteration umgesetzt ist"),
        new("doku/schnittstellen", "Schnittstellen nach außen", GruppeTechnik, "schnittstellen.md", "RIS, Mail, Push, Kalender, KI, Dateien – mit Stand"),
        new("doku/abnahme-gesamt", "Abnahme – Gesamtdurchgang", GruppeTechnik, "abnahme-gesamt.md", "Ein Fraktionsmonat in 13 Szenen (auch als geführte Touren in der Hilfe)"),
        new("doku/abnahme-1", "Abnahme 1 – Heute, Abwesenheit, Vertretung", GruppeTechnik, "abnahme-1.md", "Klick-Drehbuch"),
        new("doku/abnahme-2", "Abnahme 2 – Sitzungen und Protokolle", GruppeTechnik, "abnahme-2.md", "Klick-Drehbuch"),
        new("doku/abnahme-3", "Abnahme 3 – Anträge und Abstimmungen", GruppeTechnik, "abnahme-3.md", "Klick-Drehbuch"),
        new("doku/abnahme-4", "Abnahme 4 – Ressourcen und Terminbesetzung", GruppeTechnik, "abnahme-4.md", "Klick-Drehbuch"),
        new("doku/abnahme-5", "Abnahme 5 – Dateien, Wiki, KI", GruppeTechnik, "abnahme-5.md", "Klick-Drehbuch"),
        new("doku/abnahme-6", "Abnahme 6 – Anfragen, Beschlüsse, Anliegen, Presse", GruppeTechnik, "abnahme-6.md", "Klick-Drehbuch"),
        new("doku/abnahme-7", "Abnahme 7 – Kampagnen und Finanzen", GruppeTechnik, "abnahme-7.md", "Klick-Drehbuch"),
        new("doku/abnahme-8", "Abnahme 8 – Feinschliff", GruppeTechnik, "abnahme-8.md", "Klick-Drehbuch"),
        new("doku/kampagnen", "Kampagnen", GruppeTechnik, "kampagnen.md", "Plakate, Gebiete, Termine, ToDo-Listen"),
        new("doku/wiki-editor", "Wiki-Editor", GruppeTechnik, "wiki-editor.md", "Bearbeiten, Bilder, Vorschläge der KI"),
        new("doku/betrieb", "Betrieb", GruppeTechnik, "betrieb.md", "Sicherung, Wiederherstellung"),
        new("doku/deploy", "Bereitstellung", GruppeTechnik, "deploy.md", "Testsystem, Migrationen, Ausrollen"),
        new("doku/sicherheit", "Sicherheit", GruppeTechnik, "sicherheit.md", "OWASP-Checkliste und Schutzmaßnahmen"),
        new("doku/datenbank-einleitung", "Datenbank – Einleitung", GruppeTechnik, "datenbank-einleitung.md", "Regeln der Datenhaltung"),
        new("doku/datenbank-schema", "Datenbank – Tabellen", GruppeTechnik, "datenbank-schema.md", "Alle Tabellen und Spalten (erzeugt)"),
        new("doku/personen-zeitlich", "Personen zeitlich", GruppeTechnik, "personen-zeitlich.md", "Bitemporale Personenangaben"),
    ];

    private readonly string _wurzel;
    private readonly ConcurrentDictionary<string, (DateTime Stand, IReadOnlyList<Abschnitt> Abschnitte)> _zwischenspeicher = new();

    public Dokumentkatalog(string? wurzel = null) => _wurzel = wurzel ?? Path.Combine(AppContext.BaseDirectory, "Dokumentation");

    public static Dokument? Finden(string schluessel) => Alle.FirstOrDefault(d => d.Schluessel == schluessel);

    public static Freigabe Lesbar(Dokument dokument, Rolle? rolle) => dokument.FuerAlle ? Freigabe.Ja : Rechte.DokumentationKommentieren(rolle);

    /// <summary>Liegt die Datei in dieser Auslieferung vor?</summary>
    public bool Vorhanden(Dokument dokument) => File.Exists(Pfad(dokument));

    /// <summary>Lädt das Dokument in Abschnitten; null, wenn die Datei fehlt. Prüft das Leserecht (auch hier, nicht nur in der Seite).</summary>
    public IReadOnlyList<Abschnitt>? Laden(Dokument dokument, Rolle? rolle)
    {
        var recht = Lesbar(dokument, rolle);
        if (!recht.Erlaubt)
        {
            throw new InvalidOperationException(recht.Begruendung);
        }

        var datei = Pfad(dokument);
        if (!File.Exists(datei))
        {
            return null;
        }

        var stand = File.GetLastWriteTimeUtc(datei);
        if (_zwischenspeicher.TryGetValue(dokument.Schluessel, out var gespeichert) && gespeichert.Stand == stand)
        {
            return gespeichert.Abschnitte;
        }

        var abschnitte = MarkdownDarstellung.InAbschnitte(File.ReadAllText(datei), ziel => LinkZiel(dokument, ziel));
        _zwischenspeicher[dokument.Schluessel] = (stand, abschnitte);
        return abschnitte;
    }

    private string Pfad(Dokument d) => Path.Combine(_wurzel, d.Datei.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Relativer Link aus einem Dokument (z. B. „abnahme-1.md#x“) → Seite in der Anwendung, falls das Ziel im Katalog steht.</summary>
    internal static string? LinkZiel(Dokument von, string ziel)
    {
        var (pfad, anker) = ziel.IndexOf('#') is var i and >= 0 ? (ziel[..i], ziel[i..]) : (ziel, string.Empty);
        if (pfad.Length == 0)
        {
            return anker.Length > 0 ? von.Adresse + anker : null;
        }

        // README.md liegt im Repo-Wurzelordner und verlinkt docs/… – beides auf denselben Ordner abbilden
        if (von.Datei == "README.md" && pfad.StartsWith("docs/", StringComparison.Ordinal))
        {
            pfad = pfad["docs/".Length..];
        }

        var basis = Path.GetDirectoryName(von.Datei.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var aufgeloest = Path.GetFullPath(Path.Combine(Path.DirectorySeparatorChar + "docs", basis, pfad.Replace('/', Path.DirectorySeparatorChar)));
        var praefix = Path.GetFullPath(Path.DirectorySeparatorChar + "docs") + Path.DirectorySeparatorChar;
        if (!aufgeloest.StartsWith(praefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativ = aufgeloest[praefix.Length..].Replace(Path.DirectorySeparatorChar, '/');
        return Alle.FirstOrDefault(d => string.Equals(d.Datei, relativ, StringComparison.OrdinalIgnoreCase)) is { } treffer
            ? treffer.Adresse + anker
            : null;
    }
}
