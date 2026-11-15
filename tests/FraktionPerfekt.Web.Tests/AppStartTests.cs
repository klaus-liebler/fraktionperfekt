using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FraktionPerfekt.Web.Tests;

/// <summary>
/// Startet die gebaute Anwendung als eigenen Prozess (Entwicklungsumgebung mit Demodaten, eigene SQLite-Datei, freier Port)
/// und prüft per HTTP: Anmeldeseite, Testanmeldung, Startseiten, Handy-Antwort per Formular und Kalender-Abo.
/// </summary>
public sealed partial class AppStartTests : IAsyncLifetime
{
    private Process? _app;
    private string _ordner = string.Empty;
    private Uri _adresse = null!;

    public async ValueTask InitializeAsync()
    {
        var port = FreierPort();
        _adresse = new Uri($"http://127.0.0.1:{port}/");
        _ordner = Path.Combine(Path.GetTempPath(), "fraktionperfekt-web-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ordner);
        var dll = Path.Combine(AppContext.BaseDirectory, "FraktionPerfekt.Web.dll");
        var start = new ProcessStartInfo("dotnet", $"\"{dll}\" --urls {_adresse}")
        {
            WorkingDirectory = WebProjektordner(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["ConnectionStrings__FraktionPerfekt"] = $"Data Source={Path.Combine(_ordner, "web.db")}";
        start.Environment["Speicher__Schluessel"] = Path.Combine(_ordner, "schluessel");
        start.Environment["Speicher__Logs"] = Path.Combine(_ordner, "logs");
        start.Environment["Hintergrundjobs__Aktiv"] = "false";
        start.Environment["Dateiablage__Pfad"] = Path.Combine(_ordner, "dateien");
        _app = Process.Start(start)!;
        _app.OutputDataReceived += (_, _) => { };
        _app.ErrorDataReceived += (_, _) => { };
        _app.BeginOutputReadLine();
        _app.BeginErrorReadLine();

        using var http = new HttpClient { BaseAddress = _adresse };
        for (var i = 0; i < 120; i++)
        {
            try
            {
                if ((await http.GetAsync("anmelden", TestContext.Current.CancellationToken)).IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Startet noch.
            }

            if (_app.HasExited)
            {
                throw new InvalidOperationException($"Die Anwendung hat sich beendet (Code {_app.ExitCode}).");
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("Die Anwendung ist nicht rechtzeitig gestartet.");
    }

    public ValueTask DisposeAsync()
    {
        if (_app is { HasExited: false })
        {
            _app.Kill(entireProcessTree: true);
            _app.WaitForExit(10_000);
        }

        _app?.Dispose();
        try
        {
            Directory.Delete(_ordner, recursive: true);
        }
        catch (IOException)
        {
            // Temporärer Ordner.
        }
        catch (UnauthorizedAccessException)
        {
            // Temporärer Ordner.
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Seiten_laden_Anmeldung_und_Ein_Tipp_Antwort_auf_dem_Handy_funktionieren()
    {
        var abbruch = TestContext.Current.CancellationToken;
        using var http = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = true }) { BaseAddress = _adresse };

        // Ohne Anmeldung: Umleitung zur Anmeldung; Kalender-Abo mit falschem Token: 404.
        (await http.GetStringAsync("", abbruch)).ShouldContain("Testanmeldung");
        (await http.GetAsync("kalender/abo/falsch.ics", abbruch)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await http.GetAsync("manifest.webmanifest", abbruch)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await http.GetAsync("sw.js", abbruch)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await http.GetAsync("offline.html", abbruch)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await http.GetStringAsync("spiel/", abbruch)).ShouldContain("Testanmeldung"); // Easteregg nur angemeldet

        // Sicherheitsheader (CSP mit Hash der Import-Map, kein Framing) und no-store auf dem Kalender-Abo.
        var kopf = await http.GetAsync("anmelden", abbruch);
        kopf.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("script-src 'self' 'sha256-");
        kopf.Headers.GetValues("X-Frame-Options").Single().ShouldBe("DENY");
        (await http.GetAsync("kalender/abo/falsch.ics", abbruch)).Headers.CacheControl!.NoStore.ShouldBeTrue();

        // Testanmeldung als Anna Wewers (Demodaten: für den HFA als Vertretung gefragt).
        var anmelden = await http.GetStringAsync("anmelden", abbruch);
        anmelden.ShouldContain("CDU-Fraktion Greven");
        anmelden.ShouldContain("Anna Wewers");
        var antwort = await http.PostAsync("anmelden", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(anmelden),
            ["_handler"] = "anmelden",
            ["Login"] = "awewers",
        }), abbruch);
        antwort.IsSuccessStatusCode.ShouldBeTrue();

        // Alle Hauptseiten laden (vorgerendert, Status 200).
        foreach (var seite in new[] { "", "kalender", "kalender-abo", "abwesenheit", "vertretungen", "gremien", "personen", "handy", "handy/abwesenheit", "sitzungen", "aufgaben", "antraege", "abstimmungen", "ressourcen", "besetzung", "wahlkampf", "auslagen", "handy/plakat", "handy/auslage", "anfragen", "beschluesse", "anliegen", "presse", "kontakte", "suche?q=Haushalt", "wiki", "wiki/vorschlaege", "wiki/seite/Fraktionssitzung", "dateien" })
        {
            var r = await http.GetAsync(seite, abbruch);
            r.StatusCode.ShouldBe(HttpStatusCode.OK, $"Seite /{seite}");
        }

        // Easteregg-Spiel (angemeldet)
        (await http.GetStringAsync("spiel/", abbruch)).ShouldContain("Gral der Demokratie");
        (await http.GetAsync("spiel/logik.js", abbruch)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await http.GetAsync("spiel/SpielEndpunkte.cs", abbruch)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // KI-Schnittstelle: nur mit Schlüssel, eigenes Konto ohne vertrauliche Seiten; MCP mit Werkzeugen.
        using var ki = new HttpClient { BaseAddress = _adresse };
        (await ki.GetAsync("api/wiki/seiten", abbruch)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ki.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "demo-ki-schluessel");
        var kiSeiten = await ki.GetStringAsync("api/wiki/seiten", abbruch);
        kiSeiten.ShouldContain("Grundschullandschaft");
        kiSeiten.ShouldNotContain("Personalfragen");
        (await ki.GetStringAsync("api/wiki/suche?q=Grundschule", abbruch)).ShouldContain("Grundschullandschaft");
        var mcp = await ki.PostAsync("mcp", new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json"), abbruch);
        (await mcp.Content.ReadAsStringAsync(abbruch)).ShouldContain("wiki_vorschlagen");
        var aufruf = await ki.PostAsync("mcp", new StringContent("""{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"wiki_lesen","arguments":{"titel":"Antrag stellen"}}}""",
            System.Text.Encoding.UTF8, "application/json"), abbruch);
        (await aufruf.Content.ReadAsStringAsync(abbruch)).ShouldContain("zwei Wochen");

        // Handy: Vertretungsanfrage mit Ein-Tipp-Antwort „Ich vertrete“.
        var handy = await http.GetStringAsync("handy", abbruch);
        handy.ShouldContain("Vertretung gesucht");
        var ziel = AnfrageZiel().Match(handy).Groups[1].Value;
        ziel.ShouldNotBeNullOrEmpty();
        var bestaetigung = await http.PostAsync(ziel, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(handy),
            ["zusage"] = "ja",
        }), abbruch);
        var seiteDanach = await bestaetigung.Content.ReadAsStringAsync(abbruch);
        seiteDanach.ShouldContain("Sie haben zugesagt");
    }

    private static string Token(string html) => TokenMuster().Match(html).Groups[1].Value;

    private static int FreierPort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        return ((IPEndPoint)l.LocalEndpoint).Port;
    }

    /// <summary>Projektordner der Web-App (für appsettings und statische Dateien), sonst das Testausgabeverzeichnis.</summary>
    private static string WebProjektordner()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var kandidat = Path.Combine(d.FullName, "src", "FraktionPerfekt.Web");
            if (Directory.Exists(kandidat))
            {
                return kandidat;
            }
        }

        return AppContext.BaseDirectory;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenMuster();

    [GeneratedRegex("action=\"(handy-aktion/anfrage/[0-9a-f-]+/antwort)\"")]
    private static partial Regex AnfrageZiel();
}
