namespace FraktionPerfekt.Web.Spiel;

/// <summary>
/// Easteregg „Joe &amp; Chani und der Gral der Demokratie“: liegt bewusst nicht in wwwroot, sondern wird nur an angemeldete
/// Personen ausgeliefert. Einstieg: Konami-Code (↑↑↓↓←→←→BA) oder siebenmal schnell auf das Logo tippen.
/// </summary>
internal static partial class SpielEndpunkte
{
    private static readonly Dictionary<string, string> Dateien = new(StringComparer.Ordinal)
    {
        ["index.html"] = "text/html; charset=utf-8",
        ["spiel.css"] = "text/css; charset=utf-8",
        ["logik.js"] = "text/javascript; charset=utf-8",
        ["musik.js"] = "text/javascript; charset=utf-8",
        ["grafik.js"] = "text/javascript; charset=utf-8",
        ["darstellung.js"] = "text/javascript; charset=utf-8",
    };

    public static void MapSpiel(this IEndpointRouteBuilder app)
    {
        var inhalt = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var ordner = Path.Combine(inhalt, "Spiel");
        // Grafiken: einzige Quelle ist assets/ im Repository; beim Veröffentlichen landen sie in Spiel/grafik (siehe csproj).
        var grafik = Directory.Exists(Path.Combine(ordner, "grafik"))
            ? Path.Combine(ordner, "grafik")
            : Path.GetFullPath(Path.Combine(inhalt, "..", "..", "assets"));
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

        app.MapGet("/spiel/grafik/{datei}", (string datei, HttpContext http) =>
        {
            if (!GrafikName().IsMatch(datei))
            {
                return Results.NotFound();
            }

            var pfad = Path.Combine(grafik, datei);
            if (!File.Exists(pfad))
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-cache";
            return Results.File(pfad, "image/png");
        }).RequireAuthorization();

        // Hymne „Greven an der Ems“ für den Player im Impressum: öffentlich wie das Impressum selbst.
        // Quelle ist assets/ im Repository; beim Veröffentlichen landet sie in Spiel/musik (siehe csproj).
        var musik = Directory.Exists(Path.Combine(ordner, "musik"))
            ? Path.Combine(ordner, "musik")
            : Path.GetFullPath(Path.Combine(inhalt, "..", "..", "assets"));
        app.MapGet("/hymne.mp3", (HttpContext http) =>
        {
            var pfad = Path.Combine(musik, "greven_an_der_ems.mp3");
            if (!File.Exists(pfad))
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.File(pfad, "audio/mpeg", enableRangeProcessing: true); // Range: Spulen im Player
        }).AllowAnonymous();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9_]+\.png$")]
    private static partial System.Text.RegularExpressions.Regex GrafikName();
}
