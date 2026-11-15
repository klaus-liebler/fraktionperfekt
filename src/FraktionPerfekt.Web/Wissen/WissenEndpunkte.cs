using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Wissen;
using FraktionPerfekt.Web.Sicherheit;

namespace FraktionPerfekt.Web.Wissen;

/// <summary>
/// Dateien herunterladen und KI-Schnittstelle zum Wiki.
/// <list type="bullet">
/// <item><c>GET /dateien/{id}</c> – Download (Rechte prüft der <see cref="DateiDienst"/>).</item>
/// <item><c>/api/wiki/*</c> – REST für eine externe KI: Seiten lesen, suchen, Vorschläge anlegen. Bearer-Schlüssel aus <c>Ki:Schluessel</c>.</item>
/// <item><c>POST /mcp</c> – derselbe Zugriff als MCP-Server (JSON-RPC 2.0, Werkzeuge wiki_liste, wiki_lesen, wiki_suchen, wiki_vorschlagen).</item>
/// </list>
/// Die KI arbeitet mit eigenem Konto („ki“), sieht keine vertraulichen Seiten und kann nichts direkt ändern.
/// </summary>
internal static class WissenEndpunkte
{
    public static readonly Benutzer KiBenutzer = new(WikiDienst.KiKonto, "KI-Schnittstelle", null);

    public static void MapWissen(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dateien/{id:guid}", async (Guid id, DateiDienst dateien) =>
            await dateien.OeffnenAsync(id) is { } d
                ? Results.File(d.Inhalt, d.Datei.Typ, d.Datei.Name, enableRangeProcessing: true)
                : Results.NotFound()).RequireAuthorization();

        app.MapGet("/termine/{id:guid}/termin.ics", async (Guid id, FraktionPerfekt.Application.Termine.KalenderDienst kalender) =>
            await kalender.TerminKalenderAsync(id) is { } ics
                ? Results.File(Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", "termin.ics")
                : Results.NotFound()).RequireAuthorization();

        // Web Push: Gerät anmelden (Abo aus pushManager.subscribe)
        app.MapPost("/push/abo", async (PushAnmeldung a, FraktionPerfekt.Application.Postausgang.PushAboDienst push) =>
        {
            if (push.OeffentlicherSchluessel is null)
            {
                return Results.BadRequest(new { Fehler = "Push ist im Simulationsbetrieb." });
            }

            try
            {
                await push.AnmeldenAsync(a.Endpoint, a.Keys.P256dh, a.Keys.Auth, a.Geraet);
                return Results.Ok(new { Meldung = "Push ist auf diesem Gerät aktiv." });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Fehler = ex.Message });
            }
        }).RequireAuthorization();

        var api = app.MapGroup("/api/wiki").AddEndpointFilter(KiSchluesselPruefen).DisableAntiforgery();
        api.MapGet("/seiten", async (WikiDienst wiki) => Results.Ok((await wiki.KiSeitenAsync()).Select(Kurz)));
        api.MapGet("/seiten/{id:guid}", async (Guid id, WikiDienst wiki) =>
            (await wiki.KiSeitenAsync()).FirstOrDefault(s => s.Id == id) is { } s ? Results.Ok(s) : Results.NotFound());
        api.MapGet("/suche", async (string q, WikiDienst wiki) => Results.Ok(await SuchenAsync(wiki, q)));
        api.MapPost("/vorschlaege", async (VorschlagAnfrage a, WikiDienst wiki) =>
        {
            try
            {
                var v = await wiki.VorschlagAnlegenAsync(a.SeiteId, a.Art, a.Begruendung, a.NeuerText, "KI-Schnittstelle");
                return Results.Created($"/wiki/vorschlaege#{v.Id}", new { v.Id, Status = v.Status.ToString() });
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Results.BadRequest(new { Fehler = ex.Message });
            }
        });

        app.MapPost("/mcp", Mcp).AddEndpointFilter(KiSchluesselPruefen).DisableAntiforgery();
    }

    private sealed record PushSchluesselTeil(string P256dh, string Auth);

    private sealed record PushAnmeldung(string Endpoint, PushSchluesselTeil Keys, string? Geraet);

    private sealed record VorschlagAnfrage(Guid SeiteId, Vorschlagsart Art, string Begruendung, string? NeuerText);

    private static object Kurz(KiSeite s) => new { s.Id, s.Bereich, s.Titel, s.Version, s.GeaendertAm };

    private static async Task<IReadOnlyList<object>> SuchenAsync(WikiDienst wiki, string q)
        => (await wiki.SuchenAsync(q)).Select(t => (object)new { t.Seite.Id, t.Seite.Titel, t.Seite.Bereich, t.Ausschnitt }).ToList();

    /// <summary>Prüft den Bearer-Schlüssel und setzt das KI-Konto als Benutzer für Rechte und Protokoll.</summary>
    private static async ValueTask<object?> KiSchluesselPruefen(EndpointFilterInvocationContext kontext, EndpointFilterDelegate weiter)
    {
        var http = kontext.HttpContext;
        var erwartet = http.RequestServices.GetRequiredService<IConfiguration>()["Ki:Schluessel"];
        var kopf = http.Request.Headers.Authorization.ToString();
        var gegeben = kopf.StartsWith("Bearer ", StringComparison.Ordinal) ? kopf[7..] : string.Empty;
        if (string.IsNullOrEmpty(erwartet)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(gegeben), Encoding.UTF8.GetBytes(erwartet)))
        {
            return Results.Unauthorized();
        }

        http.RequestServices.GetRequiredService<BenutzerKontext>().Gesetzt = KiBenutzer;
        return await weiter(kontext);
    }

    /// <summary>Minimaler MCP-Server (Streamable HTTP, nur Anfrage/Antwort ohne Ereignisstrom).</summary>
    private static async Task<IResult> Mcp(HttpContext http, WikiDienst wiki)
    {
        JsonNode? anfrage;
        try
        {
            anfrage = await JsonNode.ParseAsync(http.Request.Body);
        }
        catch (JsonException)
        {
            return Results.Json(Fehler(null, -32700, "Ungültiges JSON."));
        }

        var id = anfrage?["id"]?.DeepClone();
        var methode = anfrage?["method"]?.GetValue<string>();
        if (id is null)
        {
            return Results.Accepted(); // Benachrichtigung (z. B. notifications/initialized)
        }

        try
        {
            object ergebnis = methode switch
            {
                "initialize" => new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "FraktionPerfekt-Wiki", version = "1.0" },
                    instructions = "Wiki der Fraktion lesen, durchsuchen und Änderungen vorschlagen. Änderungen werden nie direkt übernommen; ein Mensch entscheidet.",
                },
                "ping" => new { },
                "tools/list" => new { tools = Werkzeuge },
                "tools/call" => await WerkzeugAsync(wiki, anfrage!["params"]),
                _ => throw new McpFehler(-32601, $"Unbekannte Methode „{methode}“."),
            };
            return Results.Json(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = JsonSerializer.SerializeToNode(ergebnis) });
        }
        catch (McpFehler f)
        {
            return Results.Json(Fehler(id, f.Code, f.Message));
        }
    }

    private static readonly object[] Werkzeuge =
    [
        new { name = "wiki_liste", description = "Alle für die KI freigegebenen Wiki-Seiten (Id, Bereich, Titel, Version).", inputSchema = new { type = "object", properties = new { } } },
        new
        {
            name = "wiki_lesen", description = "Text einer Wiki-Seite (Markdown) lesen.",
            inputSchema = new { type = "object", properties = new { titel = new { type = "string", description = "Titel oder Id der Seite" } }, required = new[] { "titel" } },
        },
        new
        {
            name = "wiki_suchen", description = "Volltextsuche im Wiki; liefert Treffer mit Ausschnitt.",
            inputSchema = new { type = "object", properties = new { anfrage = new { type = "string" } }, required = new[] { "anfrage" } },
        },
        new
        {
            name = "wiki_vorschlagen",
            description = "Änderung an einer Seite vorschlagen (Art: Veraltet, Widerspruch, Dublette, Verwaist, Struktur, Luecke, Sonstiges). Ein Mensch prüft Vorher/Nachher und entscheidet.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    titel = new { type = "string" },
                    art = new { type = "string", @enum = Enum.GetNames<Vorschlagsart>() },
                    begruendung = new { type = "string" },
                    neuer_text = new { type = "string", description = "Vollständiger neuer Seitentext (optional)" },
                },
                required = new[] { "titel", "art", "begruendung" },
            },
        },
    ];

    private static async Task<object> WerkzeugAsync(WikiDienst wiki, JsonNode? p)
    {
        var name = p?["name"]?.GetValue<string>();
        var arg = p?["arguments"];
        string Text(string feld) => arg?[feld]?.GetValue<string>() ?? throw new McpFehler(-32602, $"Parameter „{feld}“ fehlt.");
        var seiten = await wiki.KiSeitenAsync();
        KiSeite Seite(string t) => seiten.FirstOrDefault(s => s.Id.ToString() == t || string.Equals(s.Titel, t, StringComparison.CurrentCultureIgnoreCase))
                                   ?? throw new McpFehler(-32602, $"Seite „{t}“ nicht gefunden oder nicht freigegeben.");
        string ausgabe = name switch
        {
            "wiki_liste" => string.Join("\n", seiten.Select(s => $"- {s.Titel} ({s.Bereich}, v{s.Version}, {s.Id})")),
            "wiki_lesen" => Seite(Text("titel")) is var s ? $"# {s.Titel}\n\n{s.Text}" : "",
            "wiki_suchen" => string.Join("\n", (await wiki.SuchenAsync(Text("anfrage"))).Select(t => $"- {t.Seite.Titel}: {t.Ausschnitt}")) is { Length: > 0 } x ? x : "Keine Treffer.",
            "wiki_vorschlagen" => await VorschlagenAsync(wiki, Seite(Text("titel")), Text("art"), Text("begruendung"), arg?["neuer_text"]?.GetValue<string>()),
            _ => throw new McpFehler(-32602, $"Unbekanntes Werkzeug „{name}“."),
        };
        return new { content = new[] { new { type = "text", text = ausgabe } }, isError = false };
    }

    private static async Task<string> VorschlagenAsync(WikiDienst wiki, KiSeite seite, string art, string begruendung, string? neuerText)
    {
        if (!Enum.TryParse<Vorschlagsart>(art, true, out var a))
        {
            throw new McpFehler(-32602, $"Unbekannte Art „{art}“.");
        }

        var v = await wiki.VorschlagAnlegenAsync(seite.Id, a, begruendung, neuerText, "KI-Schnittstelle (MCP)");
        return $"Vorschlag {v.Id} angelegt. Er wartet auf die Entscheidung einer verantwortlichen Person.";
    }

    private static JsonObject Fehler(JsonNode? id, int code, string text)
        => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = text } };

    private sealed class McpFehler(int code, string text) : Exception(text)
    {
        public int Code { get; } = code;
    }
}
