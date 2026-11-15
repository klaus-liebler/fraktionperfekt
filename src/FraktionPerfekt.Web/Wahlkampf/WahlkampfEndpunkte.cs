using System.Globalization;
using FraktionPerfekt.Application.Dokumente;
using FraktionPerfekt.Application.Finanzen;
using FraktionPerfekt.Application.Wahlkampf;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Wahlkampf;
using Microsoft.AspNetCore.Mvc;

namespace FraktionPerfekt.Web.Wahlkampf;

/// <summary>Fotos, Belege, Kassenbericht und die Formularziele der Handy-Seiten „Plakate“ und „Auslage“.</summary>
internal static class WahlkampfEndpunkte
{
    public static void MapWahlkampf(this IEndpointRouteBuilder app)
    {
        app.MapGet("/wahlkampf/plakat/{id:guid}/foto", async (Guid id, WahlkampfDienst w) =>
            await w.FotoAsync(id) is { } f ? Results.File(f.Inhalt, f.Typ) : Results.NotFound()).RequireAuthorization();

        app.MapGet("/finanzen/beleg/{id:guid}", async (Guid id, FinanzDienst f) =>
            await f.BelegAsync(id) is { } b ? Results.File(b.Inhalt, b.Typ, b.Name) : Results.NotFound()).RequireAuthorization();

        app.MapGet("/finanzen/kassenbericht.pdf", async (Kasse kasse, int jahr, FinanzDienst f, IPdfErzeugung pdf) =>
        {
            try
            {
                var text = await f.KassenberichtAsync(kasse, jahr);
                var bytes = pdf.Erzeugen(new PdfDokument($"Kassenbericht {jahr}", $"{(kasse == Kasse.Fraktion ? "Fraktionskasse" : "Wahlkampfkasse")} · Stand {DateTime.Now:dd.MM.yyyy}", text));
                return Results.File(bytes, "application/pdf", $"kassenbericht-{kasse.ToString().ToLowerInvariant()}-{jahr}.pdf");
            }
            catch (InvalidOperationException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization();

        var handy = app.MapGroup("/handy-aktion").RequireAuthorization();
        handy.MapPost("/plakat", async ([FromForm] string? breite, [FromForm] string? laenge, [FromForm] string? standort, [FromForm] Plakatart art,
            [FromForm] Plakatstatus status, IFormFile? foto, WahlkampfDienst w) =>
        {
            if (!Zahl(breite, out var b) || !Zahl(laenge, out var l))
            {
                return Zurueck("handy/plakat", "Bitte zuerst „Standort übernehmen“ (GPS erlauben).");
            }

            try
            {
                await using var strom = foto?.OpenReadStream();
                await w.PlakatErfassenAsync(b, l, standort, art, status, null, strom, foto?.FileName);
                return Zurueck("handy/plakat", "Plakat gespeichert.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Zurueck("handy/plakat", ex.Message);
            }
        });

        handy.MapPost("/plakat/{id:guid}/status", async (Guid id, [FromForm] Plakatstatus status, [FromForm] string? zurueck, WahlkampfDienst w) =>
        {
            var ziel = zurueck is "handy/plakat?abhaengen=1" ? zurueck : "handy/plakat";
            try
            {
                await w.StatusSetzenAsync(id, status);
                return Zurueck(ziel, status == Plakatstatus.Abgehaengt ? "Abgehängt – danke!" : "Status gespeichert.");
            }
            catch (InvalidOperationException ex)
            {
                return Zurueck(ziel, ex.Message);
            }
        });

        handy.MapPost("/auslage", async ([FromForm] string zweck, [FromForm] string betrag, [FromForm] DateOnly datum, [FromForm] Kasse kasse, IFormFile? beleg,
            FinanzDienst f) =>
        {
            if (!decimal.TryParse(betrag.Replace('.', ','), NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"), out var euro))
            {
                return Zurueck("handy/auslage", "Bitte den Betrag als Zahl angeben, z. B. 12,50.");
            }

            try
            {
                await using var strom = beleg?.OpenReadStream();
                await f.AuslageEinreichenAsync(kasse, datum, decimal.Round(euro, 2), zweck, null, strom, beleg?.FileName);
                return Zurueck("handy/auslage", "Eingereicht. Der Kassier prüft und meldet sich per Push.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Zurueck("handy/auslage", ex.Message);
            }
        });
    }

    private static bool Zahl(string? text, out double wert)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out wert);

    private static IResult Zurueck(string pfad, string meldung)
        => Results.LocalRedirect($"~/{pfad}{(pfad.Contains('?', StringComparison.Ordinal) ? "&" : "?")}meldung={Uri.EscapeDataString(meldung)}");
}
