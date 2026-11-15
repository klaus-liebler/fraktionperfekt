namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>Einstellungen der Fraktion (Abschnitt "Fraktion" in appsettings).</summary>
public sealed class FraktionOptionen
{
    public const string Abschnitt = "Fraktion";

    /// <summary>Name der Fraktion, wie er im RIS steht (für die Zuordnung der Mandate).</summary>
    public string Name { get; set; } = "CDU";

    /// <summary>Anzeige in Kopfzeile, Kalender und Mails.</summary>
    public string Anzeigename { get; set; } = "CDU-Fraktion Greven";

    /// <summary>Pfad zur Logodatei (z. B. "logo.svg" in wwwroot); leer = Platzhalter. Die Datei liefert die Fraktion selbst.</summary>
    public string? Logo { get; set; }

    public string Stadt { get; set; } = "Greven";

    /// <summary>Öffentliche Adresse der Anwendung (für Links in Mails, Push und Kalender-Abo).</summary>
    public Uri OeffentlicheAdresse { get; set; } = new("http://localhost:5290/");

    /// <summary>So lange hat eine Stellvertretung Zeit zu antworten (höchstens bis zwei Stunden vor Sitzungsbeginn).</summary>
    public int AntwortfristStunden { get; set; } = 24;

    /// <summary>Wie weit „Sitzungen mit offener Vertretung“ und die Startseite vorausschauen.</summary>
    public int VorschauTage { get; set; } = 42;

    /// <summary>Absolute Adresse zu einem relativen Pfad der Anwendung.</summary>
    public Uri Link(string pfad) => new(OeffentlicheAdresse, (pfad ?? string.Empty).TrimStart('/'));
}
