using System.Globalization;
using FraktionPerfekt.Adapters.Mocks;
using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Infrastructure;
using FraktionPerfekt.Infrastructure.Datenbank;
using FraktionPerfekt.Web.Components;
using FraktionPerfekt.Web.Sicherheit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Radzen;
using Serilog;

var deutsch = CultureInfo.GetCultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = deutsch;
CultureInfo.DefaultThreadCurrentUICulture = deutsch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((dienste, log) => log
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(dienste)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: deutsch)
    .WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Speicher:Logs"] ?? "logs", "fraktionperfekt-.log"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, formatProvider: deutsch));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

// Anmeldung: vorerst Testanmeldung (Konto + Rolle wählen). Später Passkeys mit Anmeldelink per Mail als Rückfall.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/anmelden";
        o.AccessDeniedPath = "/anmelden";
        o.Cookie.Name = "fraktionperfekt.anmeldung";
        o.ExpireTimeSpan = TimeSpan.FromDays(14);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BenutzerKontext>();
builder.Services.AddScoped<IAktuellerBenutzer, AktuellerBenutzer>();
builder.Services.AddScoped<Vorgang>();
builder.Services.AddRateLimits(builder.Configuration);

// Handy-Test im lokalen Netz (Profil "lan"): HTTPS auf allen Schnittstellen, öffentliche Adresse = LAN-IP.
FraktionPerfekt.Web.LanBetrieb.Einrichten(builder);

builder.Services
    .AddFraktionAnwendung(builder.Configuration)
    .AddFraktionInfrastruktur(builder.Configuration, builder.Environment.ContentRootPath, builder.Environment.IsDevelopment(),
        builder.Configuration.GetValue("Hintergrundjobs:Aktiv", true))
    .AddFraktionMocks(builder.Configuration);

// Schlüssel für Anmelde-Cookies dauerhaft im Datenverzeichnis halten, damit ein Neustart niemanden abmeldet.
builder.Services.AddDataProtection()
    .SetApplicationName("FraktionPerfekt")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,
        builder.Configuration["Speicher:Schluessel"] ?? "data/schluessel")));

var app = builder.Build();

await DatenbankInitialisierung.InitialisierenAsync(app.Services);
if (app.Configuration.GetValue("Demodaten:Anlegen", false))
{
    await using var bereich = app.Services.CreateAsyncScope();
    if (await bereich.ServiceProvider.GetRequiredService<Demodaten>().AnlegenAsync() is { } demo)
    {
        app.Logger.LogInformation("Demodaten angelegt: {Personen} Personen, {Gremien} Gremien, {Termine} Termine, {Faelle} Vertretungsfälle",
            demo.Personen, demo.Gremien, demo.Termine, demo.Vertretungsfaelle);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseSicherheitsheader();
app.UseRateLimiter();
app.UseSerilogRequestLogging();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRequestLocalization("de-DE");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPost("/abmelden", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("~/anmelden");
});
app.MapPost("/rolle-wechseln", async (HttpContext http, [FromForm] string rolle, [FromForm] string? zurueck, IAuditProtokoll protokoll,
    FraktionDbContext db) =>
{
    if (!Enum.TryParse<Rolle>(rolle, out var neueRolle)
        || MockAnmeldung.RolleWechseln(http.User, neueRolle, CookieAuthenticationDefaults.AuthenticationScheme) is not { } neu)
    {
        return Results.Forbid();
    }

    await protokoll.ProtokollierenAsync("Anmeldung.RolleGewechselt", $"Aktive Rolle gewechselt zu {neueRolle.Anzeigename()}");
    await db.SaveChangesAsync();
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, neu);
    return Results.LocalRedirect("~/" + Ruecksprung.Lokal(zurueck));
}).RequireAuthorization();

// Persönliches Kalender-Abo (webcal): ohne Anmeldung, das Token im Pfad ist der Schlüssel (gespeichert nur als Hash).
app.MapGet("/kalender/abo/{token}.ics", async (string token, KalenderDienst kalender, HttpContext http) =>
{
    var inhalt = await kalender.AboKalenderAsync(token);
    if (inhalt is null)
    {
        return Results.NotFound();
    }

    http.Response.Headers.CacheControl = "no-store";
    return Results.File(System.Text.Encoding.UTF8.GetBytes(inhalt), "text/calendar; charset=utf-8", "fraktion.ics");
}).AllowAnonymous();

FraktionPerfekt.Web.Handy.HandyEndpunkte.MapHandy(app);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();

/// <summary>Für Tests mit dem echten Host.</summary>
public partial class Program;
