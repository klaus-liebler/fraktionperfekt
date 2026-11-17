using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Wissen;

/// <summary>Bild in der Mediathek einer Seite.</summary>
public sealed record BildZeile(Bild Bild, string HochgeladenVon, bool InText);

/// <summary>
/// Bilder in Texten (Wiki, Anträge, Protokolle, Anfragen, Pressemitteilungen, Bürgeranliegen), eingefügt per Zwischenablage,
/// Ziehen oder Hochladen-Knopf im Editor.
/// <list type="bullet">
/// <item>Prüfung an der Dateisignatur (PNG, JPEG, GIF, WebP), höchstens 10 MB, Metadaten (EXIF/GPS, XMP) werden entfernt.</item>
/// <item>Speicherung über <see cref="IDateiablage"/> unter „Bilder/{GUID v7}.{Endung}“; Originalname, Typ, Größe, Pixelmaße,
/// SHA-256, wer/wann als Metadaten. Gleicher Inhalt wird nur einmal gespeichert.</item>
/// <item>Ein Bild hat einen oder mehrere Besitzer (<see cref="BildVerwendung"/>). Lesen darf, wer mindestens einen davon sehen darf
/// (Rechte des jeweiligen Moduls, siehe <see cref="BildBesitzer"/>), oder wer es hochgeladen hat.</item>
/// <item>Aufräumen: Bilder, die in keinem Text eines Besitzers mehr vorkommen (bei Anträgen: in keiner Fassung), werden nach einer Frist gelöscht.</item>
/// </list>
/// </summary>
public sealed partial class BildDienst(
    IDaten daten,
    IDateiablage ablage,
    IEnumerable<BildBesitzer> besitzer,
    IServiceProvider dienste,
    IStammdatenSpeicher stammdaten,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>Bilder größer als das werden im Browser vor dem Hochladen verkleinert (längste Kante).</summary>
    public const int MaxKante = 2000;

    public static readonly TimeSpan Aufbewahrung = TimeSpan.FromDays(30);

    public const string Wiki = "Wiki";

    public async Task<Bild> HochladenAsync(string bezugArt, Guid bezugId, string? dateiname, Stream inhalt, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(inhalt);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin && bezugArt is not ("Kampagne" or "Ticket" or BildBesitzer.Entwurf))
        {
            throw new InvalidOperationException("Das Kampagnenteam fügt Bilder nur in Kampagnen und Tickets ein.");
        }

        if (Besitzer(bezugArt) is not { } b || (b.Art != BildBesitzer.Entwurf && !await b.Sichtbar(dienste, bezugId, abbruch)))
        {
            throw new InvalidOperationException("Hierzu lassen sich keine Bilder einfügen (nicht gefunden oder keine Rechte).");
        }

        using var puffer = new MemoryStream();
        await inhalt.CopyToAsync(puffer, abbruch);
        if (puffer.Length > MaxBytes)
        {
            throw new InvalidOperationException($"Das Bild ist größer als {MaxBytes / 1024 / 1024} MB.");
        }

        var roh = puffer.ToArray();
        var format = Bildpruefung.Erkennen(roh) ?? throw new InvalidOperationException("Das ist kein unterstütztes Bild (PNG, JPEG, GIF oder WebP).");
        var sauber = Bildpruefung.MetadatenEntfernen(roh, format.Typ);
        var sha = Convert.ToHexStringLower(SHA256.HashData(sauber));
        var jetzt = uhr.GetUtcNow();

        var bild = await daten.ErsterAsync(daten.Menge<Bild>().Where(x => x.Sha256 == sha && x.GeloeschtAm == null), abbruch);
        if (bild is null)
        {
            var name = OriginalName(dateiname, format.Endung, jetzt);
            bild = new Bild(name, format.Typ, format.Endung, sauber.Length, format.Breite, format.Hoehe, sha, wer.PersonIdPflicht, jetzt);
            using var speicher = new MemoryStream(sauber);
            await ablage.SpeichernAsync(bild.Schluessel, speicher, abbruch);
            daten.Hinzufuegen(bild);
            await protokoll.ProtokollierenAsync("Bild.Hochgeladen", $"Bild {format.Breite}×{format.Hoehe} ({sauber.Length / 1024} KB)", nameof(Bild), bild.Id, abbruch: abbruch);
        }

        if (await daten.ErsterAsync(daten.Menge<BildVerwendung>().Where(v => v.BildId == bild.Id && v.BezugArt == bezugArt && v.BezugId == bezugId), abbruch) is null)
        {
            daten.Hinzufuegen(new BildVerwendung(bild.Id, bezugArt, bezugId, jetzt));
        }

        await daten.SpeichernAsync(abbruch);
        return bild;
    }

    /// <summary>Bild ausliefern, wenn die Person es sehen darf; sonst <c>null</c> (auch bei unbekannter Id).</summary>
    public async Task<(Bild Bild, Stream Inhalt)?> OeffnenAsync(Guid id, CancellationToken abbruch = default)
    {
        if (!await DarfSehenAsync(id, abbruch) || await daten.FindenAsync<Bild>(id, abbruch) is not { } bild)
        {
            return null;
        }

        return await ablage.OeffnenAsync(bild.Schluessel, abbruch) is { } s ? (bild, s) : null;
    }

    /// <summary>Mediathek: Bilder eines Besitzers mit Originalnamen; ob sie im aktuellen Text noch vorkommen.</summary>
    public async Task<IReadOnlyList<BildZeile>> BilderAsync(string bezugArt, Guid bezugId, string? aktuellerText, CancellationToken abbruch = default)
    {
        if (!await SichtbarAsync(bezugArt, bezugId, abbruch))
        {
            return [];
        }

        var ids = await daten.ListeAsync(daten.Menge<BildVerwendung>().Where(v => v.BezugArt == bezugArt && v.BezugId == bezugId).Select(v => v.BildId), abbruch);
        var bilder = await daten.ListeAsync(daten.Menge<Bild>().Where(b => ids.Contains(b.Id) && b.GeloeschtAm == null).OrderBy(b => b.Am).ThenBy(b => b.Id), abbruch);
        var personen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        return bilder.Select(b => new BildZeile(b, personen.GetValueOrDefault(b.HochgeladenVonPersonId, "?"),
            (aktuellerText ?? string.Empty).Contains(b.Id.ToString(), StringComparison.OrdinalIgnoreCase))).ToList();
    }

    /// <summary>
    /// Nach dem Speichern eines Textes: Bilder, die darin vorkommen, dem Besitzer zuordnen (aus einem Entwurf übernommen oder
    /// aus einem anderen Text kopiert). Nur Bilder, die die Person selbst sehen darf.
    /// </summary>
    public async Task VerwendungenAbgleichenAsync(string bezugArt, Guid bezugId, string? text, CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        var neu = false;
        foreach (var id in BildIds(text))
        {
            if (!await DarfSehenAsync(id, abbruch)
                || await daten.ErsterAsync(daten.Menge<BildVerwendung>().Where(v => v.BildId == id && v.BezugArt == bezugArt && v.BezugId == bezugId), abbruch) is not null)
            {
                continue;
            }

            daten.Hinzufuegen(new BildVerwendung(id, bezugArt, bezugId, jetzt));
            neu = true;
        }

        if (neu)
        {
            await daten.SpeichernAsync(abbruch);
        }
    }

    /// <summary>Bilder eines Markdown-Textes für den PDF-Export (nur, was die Person sehen darf).</summary>
    public async Task<IReadOnlyDictionary<Guid, byte[]>> InhalteAsync(string? text, CancellationToken abbruch = default)
    {
        var ergebnis = new Dictionary<Guid, byte[]>();
        foreach (var id in BildIds(text))
        {
            if (await OeffnenAsync(id, abbruch) is { } o)
            {
                await using var s = o.Inhalt;
                using var m = new MemoryStream();
                await s.CopyToAsync(m, abbruch);
                ergebnis[id] = m.ToArray();
            }
        }

        return ergebnis;
    }

    /// <summary>Löscht Bilder, die älter als die Frist sind und in keinem Text eines Besitzers mehr vorkommen. Läuft nachts.</summary>
    public async Task<int> AufraeumenAsync(CancellationToken abbruch = default)
    {
        var grenze = uhr.GetUtcNow() - Aufbewahrung;
        var kandidaten = (await daten.ListeAsync(daten.Menge<Bild>().Where(b => b.GeloeschtAm == null), abbruch)).Where(b => b.Am < grenze).ToList();
        if (kandidaten.Count == 0)
        {
            return 0;
        }

        var benutzt = new HashSet<Guid>();
        foreach (var b in besitzer)
        {
            foreach (var t in await b.Texte(daten, abbruch))
            {
                benutzt.UnionWith(BildIds(t));
            }
        }

        var geloescht = 0;
        foreach (var b in kandidaten.Where(b => !benutzt.Contains(b.Id)))
        {
            b.Loeschen(uhr.GetUtcNow());
            await ablage.LoeschenAsync(b.Schluessel, abbruch);
            foreach (var v in await daten.ListeAsync(daten.Menge<BildVerwendung>().Where(v => v.BildId == b.Id), abbruch))
            {
                daten.Entfernen(v);
            }

            await protokoll.ProtokollierenAsync("Bild.Aufgeraeumt", "Unbenutztes Bild nach Ablauf der Frist gelöscht", nameof(Bild), b.Id, abbruch: abbruch);
            geloescht++;
        }

        await daten.SpeichernAsync(abbruch);
        return geloescht;
    }

    /// <summary>Originalname bereinigen; eingefügte Bilder ohne Namen heißen „Eingefügt JJJJ-MM-TT HH-MM.png“.</summary>
    public static string OriginalName(string? dateiname, string endung, DateTimeOffset jetzt)
    {
        var name = string.IsNullOrWhiteSpace(dateiname) || dateiname.StartsWith("image.", StringComparison.OrdinalIgnoreCase) || dateiname == "blob"
            ? $"Eingefügt {Ortszeit.AlsOrtszeit(jetzt).ToString("yyyy-MM-dd HH-mm", CultureInfo.InvariantCulture)}{endung}"
            : Datei.Bereinigen(dateiname);
        return Path.HasExtension(name) ? name : name + endung;
    }

    /// <summary>Alle Bild-Ids, auf die ein Text verweist (dateien/bild/{guid}).</summary>
    public static IReadOnlyList<Guid> BildIds(string? text)
        => BildVerweis().Matches(text ?? string.Empty).Select(m => Guid.TryParse(m.Groups[1].Value, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty).Distinct().ToList();

    [GeneratedRegex(@"dateien/bild/([0-9a-fA-F-]{36})")]
    private static partial Regex BildVerweis();

    private BildBesitzer? Besitzer(string art) => besitzer.FirstOrDefault(b => b.Art == art);

    private async Task<bool> DarfSehenAsync(Guid id, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Bild>(id, abbruch) is not { GeloeschtAm: null } bild)
        {
            return false;
        }

        if (bild.HochgeladenVonPersonId == wer.PersonId)
        {
            return true;
        }

        foreach (var v in await daten.ListeAsync(daten.Menge<BildVerwendung>().Where(v => v.BildId == id), abbruch))
        {
            if (await SichtbarAsync(v.BezugArt, v.BezugId, abbruch))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> SichtbarAsync(string bezugArt, Guid bezugId, CancellationToken abbruch)
        => Besitzer(bezugArt) is { } b && await b.Sichtbar(dienste, bezugId, abbruch);
}
