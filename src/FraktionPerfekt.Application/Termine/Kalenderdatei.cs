using System.Globalization;
using System.Text;
using FraktionPerfekt.Application.Gemeinsam;

namespace FraktionPerfekt.Application.Termine;

/// <summary>Ein Eintrag für die Kalenderdatei.</summary>
public sealed record Kalendereintrag(Guid Id, string Titel, DateTime Beginn, DateTime Ende, string? Ort, string? Beschreibung, bool Abgesagt);

/// <summary>Erzeugt eine iCalendar-Datei (RFC 5545) für das persönliche Kalender-Abo.</summary>
public static class Kalenderdatei
{
    public static string Erzeugen(string name, IEnumerable<Kalendereintrag> eintraege, DateTimeOffset jetzt)
    {
        ArgumentNullException.ThrowIfNull(eintraege);
        var sb = new StringBuilder();
        Zeile(sb, "BEGIN:VCALENDAR");
        Zeile(sb, "VERSION:2.0");
        Zeile(sb, "PRODID:-//FraktionPerfekt//Kalender//DE");
        Zeile(sb, "CALSCALE:GREGORIAN");
        Zeile(sb, "METHOD:PUBLISH");
        Zeile(sb, "X-WR-CALNAME:" + Text(name));
        Zeile(sb, "X-WR-TIMEZONE:Europe/Berlin");
        Zeile(sb, "REFRESH-INTERVAL;VALUE=DURATION:PT1H");
        Zeile(sb, "X-PUBLISHED-TTL:PT1H");
        foreach (var e in eintraege.OrderBy(e => e.Beginn).ThenBy(e => e.Id))
        {
            Zeile(sb, "BEGIN:VEVENT");
            Zeile(sb, $"UID:{e.Id:N}@fraktionperfekt");
            Zeile(sb, "DTSTAMP:" + Utc(jetzt));
            Zeile(sb, "DTSTART:" + Utc(Ortszeit.Zeitpunkt(e.Beginn)));
            Zeile(sb, "DTEND:" + Utc(Ortszeit.Zeitpunkt(e.Ende)));
            Zeile(sb, "SUMMARY:" + Text((e.Abgesagt ? "ABGESAGT: " : string.Empty) + e.Titel));
            if (e.Ort is { Length: > 0 } ort)
            {
                Zeile(sb, "LOCATION:" + Text(ort));
            }

            if (e.Beschreibung is { Length: > 0 } beschreibung)
            {
                Zeile(sb, "DESCRIPTION:" + Text(Klartext(beschreibung)));
            }

            if (e.Abgesagt)
            {
                Zeile(sb, "STATUS:CANCELLED");
            }

            Zeile(sb, "END:VEVENT");
        }

        Zeile(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    private static string Utc(DateTimeOffset zeitpunkt) => zeitpunkt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Text(string s) => s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

    /// <summary>Zeilen über 75 Oktette werden nach RFC 5545 gefaltet (hier vereinfacht nach Zeichen, mit Reserve für Umlaute).</summary>
    private static void Zeile(StringBuilder sb, string zeile)
    {
        while (zeile.Length > 60)
        {
            sb.Append(zeile, 0, 60).Append("\r\n ");
            zeile = zeile[60..];
        }

        sb.Append(zeile).Append("\r\n");
    }

    /// <summary>Markdown als Klartext für Kalender-Apps (keine Formatierungszeichen, Bilder als Alt-Text).</summary>
    public static string Klartext(string markdown)
        => Markdig.Markdown.ToPlainText(markdown ?? string.Empty, Markdig.MarkdownExtensions.UsePipeTables(Markdig.MarkdownExtensions.DisableHtml(new Markdig.MarkdownPipelineBuilder())).Build()).Trim();
}
