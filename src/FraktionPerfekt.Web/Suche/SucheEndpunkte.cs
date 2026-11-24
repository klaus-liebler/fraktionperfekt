using FraktionPerfekt.Application.Suche;

namespace FraktionPerfekt.Web.Suche;

/// <summary>Vorschläge der Alles-Suche beim Tippen (Kopfzeile am Rechner, <c>wwwroot/suche.js</c>); Rechte prüft der Dienst je Bereich.</summary>
public static class SucheEndpunkte
{
    public static void MapSuche(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/suche/vorschlaege", async (string? q, SucheDienst suche, HttpContext http) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var treffer = (q?.Trim().Length ?? 0) < 2 ? [] : await suche.VorschlaegeAsync(q);
            return Results.Json(treffer.Select(t => new { bereich = t.Bereich, titel = t.Titel, text = t.Text, ziel = t.Ziel }));
        }).RequireAuthorization();
    }
}
