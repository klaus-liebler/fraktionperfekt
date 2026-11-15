namespace FraktionPerfekt.Web.Spiel;

/// <summary>
/// Easteregg „Joe &amp; Chani und der Gral der Demokratie“: liegt bewusst nicht in wwwroot, sondern wird nur an angemeldete
/// Personen ausgeliefert. Einstieg: Konami-Code (↑↑↓↓←→←→BA) oder siebenmal schnell auf das Logo tippen.
/// </summary>
internal static class SpielEndpunkte
{
    private static readonly Dictionary<string, string> Dateien = new(StringComparer.Ordinal)
    {
        ["index.html"] = "text/html; charset=utf-8",
        ["spiel.css"] = "text/css; charset=utf-8",
        ["logik.js"] = "text/javascript; charset=utf-8",
        ["musik.js"] = "text/javascript; charset=utf-8",
        ["pixel.js"] = "text/javascript; charset=utf-8",
        ["figuren.js"] = "text/javascript; charset=utf-8",
        ["szenen.js"] = "text/javascript; charset=utf-8",
        ["darstellung.js"] = "text/javascript; charset=utf-8",
    };

    public static void MapSpiel(this IEndpointRouteBuilder app)
    {
        var ordner = Path.Combine(app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "Spiel");
        app.MapGet("/spiel/{datei?}", (string? datei, HttpContext http) =>
        {
            if (string.IsNullOrEmpty(datei) && !http.Request.Path.Value!.EndsWith('/'))
            {
                return Results.LocalRedirect("~/spiel/"); // relative Pfade im Spiel brauchen den Schrägstrich
            }

            var name = string.IsNullOrEmpty(datei) ? "index.html" : datei;
            if (!Dateien.TryGetValue(name, out var typ))
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-cache";
            return Results.File(Path.Combine(ordner, name), typ);
        }).RequireAuthorization();
    }
}
