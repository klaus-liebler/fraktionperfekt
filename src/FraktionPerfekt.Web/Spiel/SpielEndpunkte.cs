namespace FraktionPerfekt.Web.Spiel;

/// <summary>
/// Easteregg „Joe &amp; Chani und Grevens Gral der Demokratie“ (3D, three.js): liegt bewusst nicht in wwwroot, sondern wird nur an
/// angemeldete Personen ausgeliefert (/spiel3d/; die frühere Adresse /spiel/ leitet dorthin um). Einstieg: Konami-Code
/// (↑↑↓↓←→←→BA) oder siebenmal schnell auf das Logo tippen.
/// </summary>
internal static partial class SpielEndpunkte
{
    public static void MapSpiel(this IEndpointRouteBuilder app)
    {
        var inhalt = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var ordner = Path.Combine(inhalt, "Spiel");
        // Frühere Pixel-Fassung: alte Lesezeichen und Links führen zur 3D-Fassung
        foreach (var alt in new[] { "/spiel", "/spiel/{**rest}" })
        {
            app.MapGet(alt, () => Results.LocalRedirect("~/spiel3d/")).RequireAuthorization();
        }

        // Alle Dateien des Spiels liegen in Spiel3d/ (Spiellogik, Darstellung, Musik, Modelle, Texturen, Weltdaten).
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
                "glb" => "model/gltf-binary",
                "jpg" => "image/jpeg",
                _ => "application/json; charset=utf-8",
            };
            http.Response.Headers.CacheControl = "no-cache";
            return Results.File(pfad, typ);
        }).RequireAuthorization();

        // Spickzettel mit Direkteinstiegen (?start=…) (kurze Adresse /cheatsheet)
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

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9_]+\.(html|css|js|json|glb|jpg)$")]
    private static partial System.Text.RegularExpressions.Regex Datei3dName();
}
