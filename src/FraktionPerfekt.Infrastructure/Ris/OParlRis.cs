using System.Globalization;
using System.Text.Json.Nodes;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Ris;
using Microsoft.Extensions.Logging;

namespace FraktionPerfekt.Infrastructure.Ris;

/// <summary>Einstellungen (Abschnitt Ris): <c>Ris:Art=OParl</c>, <c>Ris:OParl:Endpunkt</c> (System- oder Body-URL), optional <c>Ris:OParl:Body</c>.</summary>
public sealed class OParlOptionen
{
    public Uri? Endpunkt { get; set; }

    /// <summary>Id (URL) der Körperschaft, falls das System mehrere hat; sonst die erste.</summary>
    public string? Body { get; set; }

    /// <summary>Höchstzahl der Listenseiten je Abruf (Schutz vor Endlosschleifen).</summary>
    public int MaxSeiten { get; set; } = 50;
}

/// <summary>
/// Echter Adapter für OParl 1.1 (z. B. SD.NET/Sternberg). Liest nur öffentliche Daten: Organization, Person, Membership,
/// Meeting mit eingebetteten AgendaItems. Listen werden über <c>links.next</c> seitenweise gelesen.
/// </summary>
internal sealed class OParlRis(HttpClient http, OParlOptionen optionen, ILogger<OParlRis> log) : IRisSchnittstelle
{
    private JsonObject? _body;

    public string Quelle => $"RIS (OParl, {optionen.Endpunkt?.Host})";

    public async Task<IReadOnlyList<RisGremium>> GremienAsync(CancellationToken abbruch = default)
        => (await ListeAsync("organization", null, abbruch))
            .Where(o => Text(o, "organizationType") is null or "Gremium")
            .Select(o => new RisGremium(Text(o, "id")!, Text(o, "name") ?? "?", Text(o, "shortName") ?? Text(o, "name") ?? "?", Art(Text(o, "classification"))))
            .ToList();

    public async Task<IReadOnlyList<RisPerson>> PersonenAsync(CancellationToken abbruch = default)
    {
        var fraktionen = (await ListeAsync("organization", null, abbruch)).Where(o => Text(o, "organizationType") == "Fraktion")
            .ToDictionary(o => Text(o, "id")!, o => Text(o, "name") ?? "?");
        return (await ListeAsync("person", null, abbruch)).Select(p =>
        {
            var fraktion = (p["membership"] as JsonArray ?? []).OfType<JsonObject>()
                .Where(m => Text(m, "endDate") is null && Text(m, "organization") is { } o && fraktionen.ContainsKey(o))
                .Select(m => fraktionen[Text(m, "organization")!]).FirstOrDefault();
            return new RisPerson(Text(p, "id")!, Text(p, "givenName") ?? string.Empty, Text(p, "familyName") ?? Text(p, "name") ?? "?", fraktion);
        }).ToList();
    }

    public async Task<IReadOnlyList<RisMitgliedschaft>> MitgliedschaftenAsync(CancellationToken abbruch = default)
    {
        var liste = new List<RisMitgliedschaft>();
        foreach (var p in await ListeAsync("person", null, abbruch))
        {
            foreach (var m in (p["membership"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (Text(m, "organization") is not { } org || Datum(Text(m, "startDate")) is not { } beginn)
                {
                    continue;
                }

                var rolle = Text(m, "role") ?? string.Empty;
                var vertretung = rolle.Contains("stellv", StringComparison.OrdinalIgnoreCase) || rolle.Contains("vertret", StringComparison.OrdinalIgnoreCase);
                liste.Add(new RisMitgliedschaft(Text(m, "id") ?? $"{Text(p, "id")}|{org}", Text(p, "id")!, org, vertretung, beginn, Datum(Text(m, "endDate"))));
            }
        }

        return liste;
    }

    public async Task<IReadOnlyList<RisSitzung>> SitzungenAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default)
        => (await SitzungenRohAsync(von, bis, abbruch)).SelectMany(m =>
        {
            var beginn = Zeit(Text(m, "start"))!.Value;
            var ende = Zeit(Text(m, "end")) ?? beginn.AddHours(2);
            var ort = m["location"] is JsonObject l ? Text(l, "description") ?? Text(l, "room") ?? Text(l, "streetAddress") : null;
            var tops = (m["agendaItem"] as JsonArray ?? []).OfType<JsonObject>().Where(a => a["public"]?.GetValue<bool>() != false)
                .Select(a => $"TOP {Text(a, "number")} {Text(a, "name")}").ToList();
            return Organisationen(m).Select(o => new RisSitzung(Text(m, "id")!, o, Text(m, "name") ?? "Sitzung", beginn, ende, ort,
                m["cancelled"]?.GetValue<bool>() == true, tops.Count == 0 ? null : string.Join("\n", tops)));
        }).ToList();

    public async Task<IReadOnlyList<RisBeschluss>> BeschluesseAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default)
        => (await SitzungenRohAsync(von, bis, abbruch)).SelectMany(m =>
        {
            var datum = DateOnly.FromDateTime(Zeit(Text(m, "start"))!.Value);
            var gremium = Organisationen(m).FirstOrDefault();
            return (m["agendaItem"] as JsonArray ?? []).OfType<JsonObject>()
                .Where(a => gremium is not null && a["public"]?.GetValue<bool>() != false && (Text(a, "result") is not null || Text(a, "resolutionText") is not null))
                .Select(a => new RisBeschluss(Text(a, "id") ?? $"{Text(m, "id")}#{Text(a, "number")}", gremium!, datum, Text(a, "name") ?? "?",
                    Text(a, "resolutionText") ?? Text(a, "result") ?? string.Empty, Text(a, "result")));
        }).ToList();

    private async Task<List<JsonObject>> SitzungenRohAsync(DateOnly von, DateOnly bis, CancellationToken abbruch)
    {
        // OParl filtert Listen nur nach Änderungsdatum; Sitzungen im Zeitraum werden danach lokal ausgewählt.
        var seit = von.AddYears(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return (await ListeAsync("meeting", $"modified_since={seit}T00:00:00Z", abbruch))
            .Where(m => Zeit(Text(m, "start")) is { } s && DateOnly.FromDateTime(s) >= von && DateOnly.FromDateTime(s) <= bis && Text(m, "deleted") != "true")
            .ToList();
    }

    private async Task<List<JsonObject>> ListeAsync(string liste, string? filter, CancellationToken abbruch)
    {
        var body = await BodyAsync(abbruch);
        var adresse = Text(body, liste) ?? throw new InvalidOperationException($"OParl-Body ohne Liste „{liste}“.");
        var url = filter is null ? adresse : adresse + (adresse.Contains('?', StringComparison.Ordinal) ? "&" : "?") + filter;
        var alle = new List<JsonObject>();
        for (var seite = 0; url is not null && seite < optionen.MaxSeiten; seite++)
        {
            var antwort = await LadenAsync(new Uri(url), abbruch);
            alle.AddRange((antwort["data"] as JsonArray ?? []).OfType<JsonObject>());
            url = antwort["links"] is JsonObject links ? Text(links, "next") : null;
        }

        if (url is not null)
        {
            log.LogWarning("OParl-Liste {Liste} nach {Seiten} Seiten abgeschnitten", liste, optionen.MaxSeiten);
        }

        return alle;
    }

    private async Task<JsonObject> BodyAsync(CancellationToken abbruch)
    {
        if (_body is not null)
        {
            return _body;
        }

        var start = await LadenAsync(optionen.Endpunkt ?? throw new InvalidOperationException("Ris:OParl:Endpunkt fehlt."), abbruch);
        if (Text(start, "type")?.Contains("/Body", StringComparison.Ordinal) == true)
        {
            return _body = start;
        }

        // System → Liste der Körperschaften
        var liste = await LadenAsync(new Uri(Text(start, "body") ?? throw new InvalidOperationException("OParl-System ohne „body“.")), abbruch);
        var bodies = (liste["data"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        return _body = bodies.FirstOrDefault(b => optionen.Body is null || Text(b, "id") == optionen.Body)
                       ?? throw new InvalidOperationException("Keine passende OParl-Körperschaft gefunden.");
    }

    private async Task<JsonObject> LadenAsync(Uri adresse, CancellationToken abbruch)
    {
        using var antwort = await http.GetAsync(adresse, abbruch);
        antwort.EnsureSuccessStatusCode();
        await using var strom = await antwort.Content.ReadAsStreamAsync(abbruch);
        return await JsonNode.ParseAsync(strom, cancellationToken: abbruch) as JsonObject ?? throw new InvalidOperationException($"Keine OParl-Antwort von {adresse}.");
    }

    private static IEnumerable<string> Organisationen(JsonObject meeting)
        => (meeting["organization"] as JsonArray ?? []).Select(o => o?.GetValueKind() == System.Text.Json.JsonValueKind.String ? o.GetValue<string>() : o?["id"]?.GetValue<string>())
            .OfType<string>();

    private static string? Text(JsonObject o, string feld)
        => o[feld] is JsonValue v && v.GetValueKind() is System.Text.Json.JsonValueKind.String or System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.True
            or System.Text.Json.JsonValueKind.False
            ? v.ToString() is { Length: > 0 } s ? s : null
            : null;

    private static DateOnly? Datum(string? s) => DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var d) ? d : null;

    /// <summary>OParl-Zeiten tragen einen Offset; im Kalender gilt Wanduhrzeit Greven.</summary>
    private static DateTime? Zeit(string? s) => DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var z) ? Ortszeit.AlsOrtszeit(z) : null;

    private static string Art(string? klassifikation)
        => klassifikation is null ? "Ausschuss"
            : klassifikation.Contains("Beirat", StringComparison.OrdinalIgnoreCase) ? "Beirat"
            : klassifikation.Contains("Rat", StringComparison.OrdinalIgnoreCase) && !klassifikation.Contains("Ausschuss", StringComparison.OrdinalIgnoreCase) ? "Rat"
            : "Ausschuss";
}
