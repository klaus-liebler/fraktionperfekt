using System.Globalization;
using System.Security.Cryptography;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Wissen;

/// <summary>Bild in der Mediathek einer Seite.</summary>
public sealed record BildZeile(Bild Bild, string HochgeladenVon, bool InText);

/// <summary>
/// Bilder im Wiki (eingefügt per Zwischenablage, Ziehen oder Hochladen-Knopf im Editor).
/// <list type="bullet">
/// <item>Prüfung an der Dateisignatur (PNG, JPEG, GIF, WebP), höchstens 10 MB, Metadaten (EXIF/GPS, XMP) werden entfernt.</item>
/// <item>Speicherung über <see cref="IDateiablage"/> unter „Bilder/{GUID v7}.{Endung}“; Originalname, Typ, Größe, Pixelmaße,
/// SHA-256, wer/wann als Metadaten. Gleicher Inhalt wird nur einmal gespeichert.</item>
/// <item>Lesen darf, wer eine Seite sehen darf, die das Bild benutzt (vertrauliche Seiten beachtet), oder wer es hochgeladen hat.</item>
/// <item>Aufräumen: Bilder, die in keinem aktuellen Seitentext mehr vorkommen, werden nach einer Frist gelöscht.</item>
/// </list>
/// </summary>
public sealed class BildDienst(
    IDaten daten,
    IDateiablage ablage,
    WikiDienst wiki,
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
        await BearbeitenPruefenAsync(bezugArt, bezugId, abbruch);

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

        var bild = await daten.ErsterAsync(daten.Menge<Bild>().Where(b => b.Sha256 == sha && b.GeloeschtAm == null), abbruch);
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
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Bild>(id, abbruch) is not { GeloeschtAm: null } bild)
        {
            return null;
        }

        var erlaubt = bild.HochgeladenVonPersonId == wer.PersonId;
        if (!erlaubt)
        {
            var bezuege = await daten.ListeAsync(daten.Menge<BildVerwendung>().Where(v => v.BildId == id), abbruch);
            foreach (var v in bezuege)
            {
                if (await SichtbarAsync(v.BezugArt, v.BezugId, abbruch))
                {
                    erlaubt = true;
                    break;
                }
            }
        }

        return erlaubt && await ablage.OeffnenAsync(bild.Schluessel, abbruch) is { } s ? (bild, s) : null;
    }

    /// <summary>Mediathek: Bilder einer Seite mit Originalnamen; ob sie im aktuellen Text noch vorkommen.</summary>
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
    /// Löscht Bilder, die älter als die Frist sind und in keinem aktuellen Seitentext (auch keinem vertraulichen) mehr vorkommen.
    /// Läuft nachts; gibt die Anzahl gelöschter Bilder zurück.
    /// </summary>
    public async Task<int> AufraeumenAsync(CancellationToken abbruch = default)
    {
        var grenze = uhr.GetUtcNow() - Aufbewahrung;
        var kandidaten = await daten.ListeAsync(daten.Menge<Bild>().Where(b => b.GeloeschtAm == null), abbruch);
        kandidaten = kandidaten.Where(b => b.Am < grenze).ToList();
        if (kandidaten.Count == 0)
        {
            return 0;
        }

        var versionen = await daten.ListeAsync(daten.Menge<WikiVersion>().OrderBy(v => v.SeiteId).ThenBy(v => v.Nummer).ThenBy(v => v.Id), abbruch);
        var texte = string.Join("\n", versionen.GroupBy(v => v.SeiteId).Select(g => g.Last().Text));
        var geloescht = 0;
        foreach (var b in kandidaten.Where(b => !texte.Contains(b.Id.ToString(), StringComparison.OrdinalIgnoreCase)))
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

    private async Task BearbeitenPruefenAsync(string bezugArt, Guid bezugId, CancellationToken abbruch)
    {
        if (bezugArt != Wiki)
        {
            throw new InvalidOperationException($"Bilder gibt es bisher nur im Wiki, nicht für „{bezugArt}“.");
        }

        var seite = await wiki.SeiteAsync(bezugId.ToString(), abbruch) ?? throw new InvalidOperationException("Seite nicht gefunden.");
        if (seite.Bearbeiten is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }
    }

    private async Task<bool> SichtbarAsync(string bezugArt, Guid bezugId, CancellationToken abbruch)
        => bezugArt == Wiki && await wiki.SeiteAsync(bezugId.ToString(), abbruch) is not null;
}
