using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Domain.Sitzungen;

namespace FraktionPerfekt.Web.Beamer;

/// <summary>
/// Beamer-Ansicht einer Sitzung (aus SchoolCraft „Info-Monitore“): <c>/beamer/{token}</c> ohne Anmeldung (geheime Adresse, nur Prüfwert gespeichert,
/// Rate-Limit), <c>/beamer/vorschau/{sitzungId}</c> für Angemeldete mit Sitzungsrechten. Schlichtes HTML ohne Skript, große Schrift, lädt sich alle
/// zehn Sekunden neu; zeigt nur Tagesordnung, laufenden TOP, Beschlussfähigkeit und Abstimmungsergebnisse.
/// </summary>
public static class BeamerEndpunkte
{
    public static void MapBeamer(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/beamer/vorschau/{sitzungId:guid}", async (Guid sitzungId, BeamerDienst beamer, HttpContext http) =>
        {
            try
            {
                return Seite(await beamer.VorschauAsync(sitzungId), http, vorschau: true);
            }
            catch (InvalidOperationException)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }).RequireAuthorization();

        app.MapGet("/beamer/{token}", async (string token, BeamerDienst beamer, HttpContext http) => Seite(await beamer.AnsichtAsync(token), http, vorschau: false))
            .AllowAnonymous();
    }

    private static IResult Seite(BeamerAnsicht? a, HttpContext http, bool vorschau)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        if (a is null)
        {
            return Results.Content(Html(http.Request.PathBase.Value ?? "", "Beamer", "<main class=\"fp-beamer\"><h1>Dieser Beamer-Link gilt nicht (mehr).</h1>"
                + "<p>Bitte in der Sitzung einen neuen Link erzeugen.</p></main>", 0), "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);
        }

        var e = HtmlEncoder.Default;
        var de = CultureInfo.GetCultureInfo("de-DE");
        var sb = new StringBuilder();
        sb.Append("<main class=\"fp-beamer\">");
        if (vorschau)
        {
            sb.Append("<p class=\"fp-beamer-vorschau\">Vorschau – so sieht es auf dem Beamer aus</p>");
        }

        sb.Append(de, $"<h1>{e.Encode(a.Titel)}</h1><p class=\"fp-beamer-sub\">{a.Beginn.ToString("dddd, dd.MM.yyyy, HH:mm", de)} Uhr{(a.Ort is { } o ? " · " + e.Encode(o) : "")}"
            + $" · {Status(a.Status)}</p>");
        if (a.Beschlussfaehigkeit is { } bf)
        {
            sb.Append(de, $"<p class=\"fp-beamer-bf\">{e.Encode(bf)}</p>");
        }

        foreach (var ab in a.Abstimmungen.Where(x => x.Laeuft))
        {
            sb.Append(de, $"<section class=\"fp-beamer-abstimmung\"><h2>Abstimmung läuft</h2><p>{e.Encode(ab.Titel)}</p>"
                + $"<p class=\"fp-beamer-zahl\">{ab.Teilnehmende} von {ab.Berechtigte} haben abgestimmt</p></section>");
        }

        foreach (var ab in a.Abstimmungen.Where(x => !x.Laeuft).TakeLast(2))
        {
            sb.Append(de, $"<section class=\"fp-beamer-ergebnis\"><h2>Ergebnis</h2><p>{e.Encode(ab.Titel)}</p><p class=\"fp-beamer-zahl\">{e.Encode(ab.Ergebnis ?? "")}</p></section>");
        }

        sb.Append("<h2>Tagesordnung</h2><ol class=\"fp-beamer-tops\">");
        foreach (var t in a.Tops)
        {
            var klasse = t.Laufend ? "laufend" : t.Status == Topstatus.Behandelt ? "erledigt" : t.Status == Topstatus.Vertagt ? "vertagt" : "";
            sb.Append(de, $"<li class=\"{klasse}\" value=\"{t.Nummer}\">{e.Encode(t.Titel)}{(t.Laufend ? " <b>← jetzt</b>" : t.Status == Topstatus.Vertagt ? " (vertagt)" : "")}");
            foreach (var b in t.Beschluesse)
            {
                sb.Append(de, $"<div class=\"fp-beamer-beschluss\">Beschluss: {e.Encode(b)}</div>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ol></main>");
        return Results.Content(Html(http.Request.PathBase.Value ?? "", a.Titel, sb.ToString(), 10), "text/html; charset=utf-8");
    }

    private static string Status(Sitzungsstatus s) => s switch
    {
        Sitzungsstatus.Laufend => "Sitzung läuft",
        Sitzungsstatus.Entwurf or Sitzungsstatus.Freigegeben => "Sitzung beendet",
        _ => "Sitzung beginnt in Kürze",
    };

    private static string Html(string basis, string titel, string inhalt, int neuLadenSekunden)
        => "<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
           + (neuLadenSekunden > 0 ? $"<meta http-equiv=\"refresh\" content=\"{neuLadenSekunden}\">" : "")
           + "<meta name=\"robots\" content=\"noindex, nofollow\">"
           + $"<title>{HtmlEncoder.Default.Encode(titel)} · Beamer</title><link rel=\"stylesheet\" href=\"{HtmlEncoder.Default.Encode(basis)}/app.css\"></head>"
           + $"<body class=\"fp-beamer-seite\">{inhalt}</body></html>";
}
