namespace FraktionPerfekt.Web.Spiel;

/// <summary>
/// Easteregg „Joe &amp; Chani und der Gral der Demokratie“: liegt bewusst nicht in wwwroot, sondern wird nur an angemeldete
/// Personen ausgeliefert (Pixel-Fassung unter /spiel/, 3D-Fassung unter /spiel3d/). Einstieg: Konami-Code (↑↑↓↓←→←→BA) oder siebenmal schnell auf das Logo tippen.
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

        // 3D-Fassung (three.js, Akt 1–2): eigene Dateien in Spiel3d/, nutzt logik.js, grafik.js, musik.js und spiel.css unter /spiel/.
        // Nur flache Dateinamen; three.js liegt lokal bei (CSP: Skripte nur von 'self').
        var ordner3d = Path.Combine(inhalt, "Spiel3d");
        app.MapGet("/spiel3d/{datei?}", (string? datei, HttpContext http) =>
        {
            if (string.IsNullOrEmpty(datei) && !http.Request.Path.Value!.EndsWith('/'))
            {
                return Results.LocalRedirect("~/spiel3d/");
            }

            var name = string.IsNullOrEmpty(datei) ? "index.html" : datei;
            var treffer = Datei3dName().Match(name);
            var pfad = Path.Combine(ordner3d, name);
            if (!treffer.Success || !File.Exists(pfad))
            {
                return Results.NotFound();
            }

            var typ = treffer.Groups[1].Value switch
            {
                "html" => "text/html; charset=utf-8",
                "css" => "text/css; charset=utf-8",
                "js" => "text/javascript; charset=utf-8",
                _ => "application/json; charset=utf-8",
            };
            http.Response.Headers.CacheControl = "no-cache";
            return Results.File(pfad, typ);
        }).RequireAuthorization();

        // Spickzettel mit Direkteinstiegen in die 3D-Fassung (kurze Adresse /cheatsheet)
        app.MapGet("/cheatsheet", () => Results.LocalRedirect("~/spiel3d/cheatsheet.html")).RequireAuthorization();

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

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9_]+\.(html|css|js|json)$")]
    private static partial System.Text.RegularExpressions.Regex Datei3dName();
}
