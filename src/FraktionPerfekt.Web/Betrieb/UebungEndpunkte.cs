using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Infrastructure.Betrieb;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FraktionPerfekt.Web.Betrieb;

/// <summary>
/// Übungsinstanz zurücksetzen (aus SchoolCraft FK-14): Nur in einem Mandanten mit <c>Uebung=true</c>, nur Administration oder Vorstand. Baut frische
/// Demodaten (Datum relativ zu heute) und kopiert sie über die Übungsdatenbank; danach melden sich alle neu an.
/// </summary>
public static class UebungEndpunkte
{
    public static Task NeuAufbauenAsync(IServiceProvider dienste, MandantOptionen uebung)
        => Uebungsinstanz.NeuAufbauenAsync(dienste, uebung, async d => await d.GetRequiredService<Demodaten>().AnlegenAsync());

    public static void MapUebung(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost("/uebung/zuruecksetzen", async ([FromForm] string? bestaetigt, HttpContext http, Betriebsart betriebsart, IAktuellerMandant mandant,
            IAktuellerBenutzer benutzer, ILoggerFactory logs) =>
        {
            var wer = await benutzer.ErmittelnAsync();
            if (!betriebsart.IstUebung || wer.Rolle is not (Rolle.Administration or Rolle.Vorstand))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            await NeuAufbauenAsync(app.Services, mandant.Mandant);
            logs.CreateLogger("Uebung").LogInformation("Übungsinstanz {Kennung} zurückgesetzt", mandant.Mandant.Kennung);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect("~/anmelden");
        }).RequireAuthorization();
    }
}
