using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Vertretung;
using Microsoft.AspNetCore.Mvc;

namespace FraktionPerfekt.Web.Handy;

/// <summary>
/// Formularziele der statisch gerenderten Handy-Seiten: klassischer POST mit Antiforgery-Token, danach Umleitung mit
/// kurzer Rückmeldung (Post/Redirect/Get). So funktionieren die Ein-Tipp-Antworten ohne SignalR-Verbindung.
/// </summary>
internal static class HandyEndpunkte
{
    public static void MapHandy(this IEndpointRouteBuilder app)
    {
        var handy = app.MapGroup("/handy-aktion").RequireAuthorization();

        handy.MapPost("/termin/{id:guid}/antwort", async (Guid id, [FromForm] string zusage, KalenderDienst kalender) =>
        {
            var ja = zusage == "ja";
            try
            {
                var ergebnis = await kalender.AntwortenAsync(id, ja);
                var vertretung = ergebnis.NaechsteSchritte.FirstOrDefault(s => s.Hauptschritt && s.Ziel.StartsWith("vertretungen/termin/", StringComparison.Ordinal));
                return Zurueck("handy", ja ? "Zugesagt." : vertretung is null ? "Abgesagt." : "Abgesagt. Sie sind ordentliches Mitglied – Vertretung jetzt anfragen?",
                    vertretung is null ? null : id);
            }
            catch (InvalidOperationException ex)
            {
                return Zurueck("handy", ex.Message);
            }
        });

        handy.MapPost("/termin/{id:guid}/vertretung", async (Guid id, VertretungDienst vertretung) =>
        {
            try
            {
                await vertretung.FuerTerminAnfragenAsync(id);
                return Zurueck("handy", "Vertretung angefragt. Die Stellvertretungen werden der Reihe nach gefragt.");
            }
            catch (InvalidOperationException ex)
            {
                return Zurueck("handy", ex.Message);
            }
        });

        handy.MapPost("/anfrage/{id:guid}/antwort", async (Guid id, [FromForm] string zusage, VertretungDienst vertretung) =>
        {
            try
            {
                var ergebnis = await vertretung.AntwortenAsync(id, zusage == "ja");
                return Zurueck($"handy/anfrage/{id}", ergebnis.Meldung);
            }
            catch (InvalidOperationException ex)
            {
                return Zurueck($"handy/anfrage/{id}", ex.Message);
            }
        });

        handy.MapPost("/abwesenheit", async ([FromForm] DateOnly von, [FromForm] DateOnly bis, [FromForm] string? grund, VertretungDienst vertretung) =>
        {
            try
            {
                var ergebnis = await vertretung.EintragenAsync(von, bis, grund);
                return Results.LocalRedirect($"~/handy/abwesenheit?id={ergebnis.Abwesenheit.Id}");
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Zurueck("handy/abwesenheit", ex.Message);
            }
        });

        handy.MapPost("/abwesenheit/{id:guid}/anfragen", async (Guid id, VertretungDienst vertretung) =>
        {
            try
            {
                var faelle = await vertretung.AnfragenAsync(id);
                return Results.LocalRedirect($"~/handy/abwesenheit?id={id}&meldung={Uri.EscapeDataString($"Vertretung für {faelle.Count} Sitzung(en) angefragt.")}");
            }
            catch (InvalidOperationException ex)
            {
                return Zurueck("handy/abwesenheit", ex.Message);
            }
        });
    }

    private static IResult Zurueck(string pfad, string meldung, Guid? vertretungFuer = null)
        => Results.LocalRedirect($"~/{pfad}?meldung={Uri.EscapeDataString(meldung)}" + (vertretungFuer is { } t ? $"&vertretung={t}" : string.Empty));
}
