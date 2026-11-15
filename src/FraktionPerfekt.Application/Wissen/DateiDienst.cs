using System.Security.Cryptography;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Wissen;

public sealed record Ordner(string Pfad, string Anzeige, Guid? GremiumId, int Dateien);

public sealed record DateiZeile(Datei Datei, string HochgeladenVon, Freigabe Loeschen);

/// <summary>
/// Dateiablage (Modul 8): Ordner je Gremium, Arbeitskreis und Projekt mit Rechten aus den Gremien; Anhänge an Anträgen,
/// Protokollen und Terminen. Höchstens 20 MB je Datei, nur gängige Büro- und Bildformate.
/// </summary>
public sealed class DateiDienst(
    IDaten daten,
    IDateiablage ablage,
    IStammdatenSpeicher stammdaten,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const long MaxBytes = 20 * 1024 * 1024;

    private static readonly Dictionary<string, string> Typen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".odt"] = "application/vnd.oasis.opendocument.text",
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
    };

    public string Speicher => ablage.Name;

    public static bool Erlaubt(string dateiname) => Typen.ContainsKey(Path.GetExtension(dateiname));

    /// <summary>Ordnerbaum: Fraktion, je Gremium, Anträge, Protokolle, Wahlkampf – nur Ordner, die die Person sehen darf.</summary>
    public async Task<IReadOnlyList<Ordner>> OrdnerAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var gremien = await stammdaten.GremienAsync(abbruch);
        var sichtbar = await SichtbarAsync(wer, abbruch);
        var anzahl = (await daten.ListeAsync(daten.Menge<Datei>().Where(d => d.GeloeschtAm == null).Select(d => new { d.Ordner, d.GremiumId, d.Vertraulich }), abbruch))
            .Where(d => sichtbar(d.GremiumId, d.Vertraulich) && (wer.Rolle is not Rolle.Helferin || d.Ordner == "Wahlkampf")).GroupBy(d => d.Ordner).ToDictionary(g => g.Key, g => g.Count());
        var liste = new List<Ordner>();
        void Hinzu(string pfad, string anzeige, Guid? gremium)
        {
            if (sichtbar(gremium, false))
            {
                liste.Add(new Ordner(pfad, anzeige, gremium, anzahl.GetValueOrDefault(pfad)));
            }
        }

        if (wer.Rolle is not Rolle.Helferin)
        {
            Hinzu("Fraktion", "Fraktion (alle)", null);
            Hinzu("Anträge", "Anträge", null);
            Hinzu("Protokolle", "Protokolle", null);
            foreach (var g in gremien.Where(g => g.Aktiv && g.Art != Gremienart.Fraktion))
            {
                Hinzu($"{(g.Art == Gremienart.Arbeitskreis ? "Arbeitskreise" : "Gremien")}/{g.Kurzname}", g.Name, g.Id);
            }
        }

        liste.Add(new Ordner("Wahlkampf", "Wahlkampf", null, anzahl.GetValueOrDefault("Wahlkampf")));
        foreach (var weitere in anzahl.Keys.Where(k => liste.All(o => o.Pfad != k)).Order())
        {
            liste.Add(new Ordner(weitere, weitere, null, anzahl[weitere]));
        }

        return liste;
    }

    public async Task<IReadOnlyList<DateiZeile>> ListeAsync(string? ordner, string? bezugArt = null, Guid? bezugId = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var sichtbar = await SichtbarAsync(wer, abbruch);
        var abfrage = daten.Menge<Datei>().Where(d => d.GeloeschtAm == null);
        abfrage = bezugId is { } b ? abfrage.Where(d => d.BezugArt == bezugArt && d.BezugId == b) : abfrage.Where(d => d.Ordner == ordner);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        return (await daten.ListeAsync(abfrage.OrderByDescending(d => d.Am).ThenBy(d => d.Id), abbruch))
            .Where(d => sichtbar(d.GremiumId, d.Vertraulich) && (wer.Rolle is not Rolle.Helferin || d.Ordner == "Wahlkampf"))
            .Select(d => new DateiZeile(d, namen.GetValueOrDefault(d.HochgeladenVonPersonId, "?"),
                d.HochgeladenVonPersonId == wer.PersonId || wer.Rolle is Rolle.Administration ? Freigabe.Ja
                    : Freigabe.Nein("Löschen kann, wer hochgeladen hat, oder die Geschäftsführung.")))
            .ToList();
    }

    /// <summary>Dateien nach Namen finden (globale Suche), mit denselben Rechten wie die Ordneransicht.</summary>
    public async Task<IReadOnlyList<DateiZeile>> SuchenAsync(IReadOnlyCollection<string> woerter, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var sichtbar = await SichtbarAsync(wer, abbruch);
        return (await daten.ListeAsync(daten.Menge<Datei>().Where(d => d.GeloeschtAm == null).OrderByDescending(d => d.Am).ThenBy(d => d.Id), abbruch))
            .Where(d => sichtbar(d.GremiumId, d.Vertraulich) && (wer.Rolle is not Rolle.Helferin || d.Ordner == "Wahlkampf")
                        && woerter.All(w => d.Name.Contains(w, StringComparison.CurrentCultureIgnoreCase) || d.Ordner.Contains(w, StringComparison.CurrentCultureIgnoreCase)))
            .Select(d => new DateiZeile(d, string.Empty, Freigabe.Nein("In der Ordneransicht löschen.")))
            .ToList();
    }

    public async Task<Datei> HochladenAsync(string ordner, string dateiname, Stream inhalt, Guid? gremiumId, bool vertraulich, string? bezugArt = null,
        Guid? bezugId = null, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(inhalt);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var name = Datei.Bereinigen(dateiname);
        if (!Erlaubt(name))
        {
            throw new InvalidOperationException($"Dateityp {Path.GetExtension(name)} ist nicht erlaubt (PDF, Office, Text, Bilder).");
        }

        if (wer.Rolle is Rolle.Helferin && ordner != "Wahlkampf")
        {
            throw new InvalidOperationException("Helfer:innen laden nur in den Ordner „Wahlkampf“ hoch.");
        }

        if (gremiumId is null && (ordner.StartsWith("Gremien/", StringComparison.Ordinal) || ordner.StartsWith("Arbeitskreise/", StringComparison.Ordinal)))
        {
            var kurz = ordner[(ordner.IndexOf('/', StringComparison.Ordinal) + 1)..];
            gremiumId = (await stammdaten.GremienAsync(abbruch)).FirstOrDefault(g => g.Kurzname == kurz)?.Id;
        }

        using var puffer = new MemoryStream();
        await inhalt.CopyToAsync(puffer, abbruch);
        if (puffer.Length > MaxBytes)
        {
            throw new InvalidOperationException($"Die Datei ist größer als {MaxBytes / 1024 / 1024} MB.");
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(puffer.ToArray()));
        var schluessel = $"{ordner.Trim('/')}/{Domain.Gemeinsam.Ids.Neu():N}-{name}";
        puffer.Position = 0;
        await ablage.SpeichernAsync(schluessel, puffer, abbruch);
        var datei = new Datei(ordner, name, Typen[Path.GetExtension(name)], puffer.Length, schluessel, sha, wer.PersonIdPflicht, uhr.GetUtcNow(), gremiumId, vertraulich,
            bezugArt, bezugId);
        daten.Hinzufuegen(datei);
        await protokoll.ProtokollierenAsync("Datei.Hochgeladen", $"Datei in „{ordner}“ hochgeladen ({puffer.Length / 1024} KB)", nameof(Datei), datei.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return datei;
    }

    public async Task<(Datei Datei, Stream Inhalt)?> OeffnenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Datei>(id, abbruch) is not { GeloeschtAm: null } d || !(await SichtbarAsync(wer, abbruch))(d.GremiumId, d.Vertraulich)
            || (wer.Rolle is Rolle.Helferin && d.Ordner != "Wahlkampf"))
        {
            return null;
        }

        return await ablage.OeffnenAsync(d.Schluessel, abbruch) is { } s ? (d, s) : null;
    }

    public async Task LoeschenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await daten.LadenAsync<Datei>(id, "Datei", abbruch);
        if (d.HochgeladenVonPersonId != wer.PersonId && wer.Rolle is not Rolle.Administration)
        {
            throw new InvalidOperationException("Löschen kann, wer hochgeladen hat, oder die Geschäftsführung.");
        }

        d.Loeschen(uhr.GetUtcNow());
        await ablage.LoeschenAsync(d.Schluessel, abbruch);
        await protokoll.ProtokollierenAsync("Datei.Geloescht", $"Datei in „{d.Ordner}“ gelöscht", nameof(Datei), d.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Sichtbarkeit: Fraktionsdateien alle außer Helfer:innen; Gremiendateien Mitglieder, Vorstand, Administration; vertrauliche nur Gremium und Leitung.</summary>
    private async Task<Func<Guid?, bool, bool>> SichtbarAsync(Benutzer wer, CancellationToken abbruch)
    {
        var leitung = wer.Rolle is Rolle.Vorstand or Rolle.Administration;
        var meine = wer.PersonId is { } p
            ? (await stammdaten.MitgliedschaftenAsync(abbruch)).Where(m => m.PersonId == p && m.StandAm(Ortszeit.Heute(uhr), uhr.GetUtcNow()) is not null)
                .Select(m => m.GremiumId).ToHashSet()
            : [];
        return (gremium, vertraulich) => gremium is null
            ? !vertraulich || leitung
            : leitung || meine.Contains(gremium.Value) || (!vertraulich && wer.Rolle is Rolle.Mitglied or Rolle.SachkundigeBuergerin or Rolle.Kassier);
    }
}
