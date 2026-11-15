using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;

namespace FraktionPerfekt.Infrastructure.Wissen;

/// <summary>Dateispeicher im lokalen Datenverzeichnis (Standard). Schlüssel sind relative Pfade; „..“ ist ausgeschlossen.</summary>
internal sealed class LokaleDateiablage(string wurzel) : IDateiablage
{
    public string Name => "lokal";

    public async Task SpeichernAsync(string schluessel, Stream inhalt, CancellationToken abbruch = default)
    {
        var pfad = Pfad(schluessel);
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
        await using var datei = File.Create(pfad);
        await inhalt.CopyToAsync(datei, abbruch);
    }

    public Task<Stream?> OeffnenAsync(string schluessel, CancellationToken abbruch = default)
    {
        var pfad = Pfad(schluessel);
        return Task.FromResult<Stream?>(File.Exists(pfad) ? File.OpenRead(pfad) : null);
    }

    public Task LoeschenAsync(string schluessel, CancellationToken abbruch = default)
    {
        var pfad = Pfad(schluessel);
        if (File.Exists(pfad))
        {
            File.Delete(pfad);
        }

        return Task.CompletedTask;
    }

    private string Pfad(string schluessel)
    {
        var voll = Path.GetFullPath(Path.Combine(wurzel, schluessel.Replace('/', Path.DirectorySeparatorChar)));
        if (!voll.StartsWith(Path.GetFullPath(wurzel), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ungültiger Dateipfad.");
        }

        return voll;
    }
}

/// <summary>Einstellungen für Nextcloud/WebDAV (Abschnitt Dateiablage:WebDav). Zugangsdaten gehören in Umgebungsvariablen.</summary>
public sealed class WebDavOptionen
{
    /// <summary>z. B. https://cloud.example.de/remote.php/dav/files/fraktion/FraktionPerfekt/</summary>
    public Uri? Adresse { get; set; }

    public string? Benutzer { get; set; }

    public string? Passwort { get; set; }
}

/// <summary>
/// Dateispeicher in Nextcloud über WebDAV (PUT/GET/DELETE, MKCOL für Ordner). Aktiv mit Dateiablage:Art=WebDav; die Ordner
/// erscheinen dann auch im Nextcloud-Desktop-Client (Explorer, Finder) und in der Dateien-App am Handy.
/// </summary>
internal sealed class WebDavDateiablage(HttpClient http, WebDavOptionen optionen) : IDateiablage
{
    public string Name => "Nextcloud (WebDAV)";

    public async Task SpeichernAsync(string schluessel, Stream inhalt, CancellationToken abbruch = default)
    {
        var teile = schluessel.Split('/');
        for (var i = 1; i < teile.Length; i++)
        {
            using var mk = Anfrage(new HttpMethod("MKCOL"), string.Join('/', teile.Take(i)) + "/");
            using var r = await http.SendAsync(mk, abbruch);
            if (!r.IsSuccessStatusCode && r.StatusCode is not HttpStatusCode.MethodNotAllowed)
            {
                r.EnsureSuccessStatusCode();
            }
        }

        using var put = Anfrage(HttpMethod.Put, schluessel);
        put.Content = new StreamContent(inhalt);
        (await http.SendAsync(put, abbruch)).EnsureSuccessStatusCode();
    }

    public async Task<Stream?> OeffnenAsync(string schluessel, CancellationToken abbruch = default)
    {
        using var get = Anfrage(HttpMethod.Get, schluessel);
        var r = await http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, abbruch);
        if (r.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        r.EnsureSuccessStatusCode();
        return await r.Content.ReadAsStreamAsync(abbruch);
    }

    public async Task LoeschenAsync(string schluessel, CancellationToken abbruch = default)
    {
        using var del = Anfrage(HttpMethod.Delete, schluessel);
        using var r = await http.SendAsync(del, abbruch);
        if (r.StatusCode != HttpStatusCode.NotFound)
        {
            r.EnsureSuccessStatusCode();
        }
    }

    private HttpRequestMessage Anfrage(HttpMethod methode, string schluessel)
    {
        var basis = optionen.Adresse ?? throw new InvalidOperationException("Dateiablage:WebDav:Adresse fehlt.");
        var pfad = string.Join('/', schluessel.Split('/').Select(Uri.EscapeDataString));
        var anfrage = new HttpRequestMessage(methode, new Uri(basis, pfad));
        if (optionen.Benutzer is { } b)
        {
            anfrage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{b}:{optionen.Passwort}")));
        }

        return anfrage;
    }
}

/// <summary>Volltextsuche über Wiki-Seiten mit SQLite FTS5 (virtuelle Tabelle „WikiSuche“, beim Start angelegt).</summary>
internal sealed class WikiSuche(FraktionDbContext db) : IWikiSuche
{
    public const string Anlegen = "CREATE VIRTUAL TABLE IF NOT EXISTS WikiSuche USING fts5(SeiteId UNINDEXED, Titel, Text, tokenize = 'unicode61 remove_diacritics 2')";

    public async Task AktualisierenAsync(Guid seiteId, string titel, string text, CancellationToken abbruch = default)
    {
        await EntfernenAsync(seiteId, abbruch);
        await db.Database.ExecuteSqlAsync($"INSERT INTO WikiSuche (SeiteId, Titel, Text) VALUES ({seiteId.ToString("D")}, {titel}, {text})", abbruch);
    }

    public async Task EntfernenAsync(Guid seiteId, CancellationToken abbruch = default)
        => await db.Database.ExecuteSqlAsync($"DELETE FROM WikiSuche WHERE SeiteId = {seiteId.ToString("D")}", abbruch);

    public async Task<IReadOnlyList<(Guid SeiteId, string Ausschnitt)>> SuchenAsync(string anfrage, int maximal, CancellationToken abbruch = default)
    {
        var woerter = (anfrage ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray())).Where(w => w.Length > 1).Take(8).ToList();
        if (woerter.Count == 0)
        {
            return [];
        }

        var ausdruck = string.Join(" OR ", woerter.Select(w => $"\"{w}\"*"));
        var verbindung = db.Database.GetDbConnection();
        var offen = verbindung.State == System.Data.ConnectionState.Open;
        if (!offen)
        {
            await verbindung.OpenAsync(abbruch);
        }

        try
        {
            await using var cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SeiteId, snippet(WikiSuche, 2, '**', '**', '…', 14) FROM WikiSuche WHERE WikiSuche MATCH $q ORDER BY rank LIMIT $n";
            Parameter(cmd, "$q", ausdruck);
            Parameter(cmd, "$n", maximal);
            var treffer = new List<(Guid, string)>();
            await using var leser = await cmd.ExecuteReaderAsync(abbruch);
            while (await leser.ReadAsync(abbruch))
            {
                treffer.Add((Guid.Parse(leser.GetString(0)), leser.GetString(1)));
            }

            return treffer;
        }
        finally
        {
            if (!offen)
            {
                await verbindung.CloseAsync();
            }
        }
    }

    private static void Parameter(DbCommand cmd, string name, object wert)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = wert;
        cmd.Parameters.Add(p);
    }
}
