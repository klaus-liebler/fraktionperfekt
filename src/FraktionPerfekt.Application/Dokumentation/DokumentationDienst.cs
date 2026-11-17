using System.Text;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Dokumentation;

namespace FraktionPerfekt.Application.Dokumentation;

/// <summary>Kommentar mit aufgelösten Namen (zur Laufzeit aus den Personen, nicht gespeichert).</summary>
public sealed record KommentarZeile(Kommentar Kommentar, string Verfasst, string? ErledigtVon);

/// <summary>Ein Faden: erster Kommentar und seine Antworten (älteste zuerst).</summary>
public sealed record Kommentarfaden(KommentarZeile Start, IReadOnlyList<KommentarZeile> Antworten);

/// <summary>
/// Kommentare zu Hilfe, Konzept und Doku (Administration und Vorstand) sowie das Impressum (Administration).
/// Rechte werden hier geprüft, nicht nur in der Oberfläche (übernommen aus VibeVHS, dort FA-159/FA-160).
/// </summary>
public sealed class DokumentationDienst(IDaten daten, IStammdatenSpeicher stammdaten, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr)
{
    public async Task<Freigabe> RechtAsync(CancellationToken abbruch = default)
        => Rechte.DokumentationKommentieren((await benutzer.ErmittelnAsync(abbruch)).Rolle);

    /// <summary>Fäden eines Dokuments je Abschnitt (Schlüssel = Anker, leer = ganzes Dokument).</summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<Kommentarfaden>>> ZuDokumentAsync(string dokument, CancellationToken abbruch = default)
    {
        await PruefenAsync(abbruch);
        var liste = await daten.ListeAsync(daten.Menge<Kommentar>().Where(k => k.Dokument == dokument).OrderBy(k => k.Id), abbruch);
        return (await FaedenAsync(liste, abbruch)).GroupBy(f => f.Start.Kommentar.Abschnitt)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Kommentarfaden>)g.ToList());
    }

    public async Task<IReadOnlyList<Kommentarfaden>> AlleAsync(CancellationToken abbruch = default)
    {
        await PruefenAsync(abbruch);
        return await FaedenAsync(await daten.ListeAsync(daten.Menge<Kommentar>().OrderBy(k => k.Id), abbruch), abbruch);
    }

    public async Task<Kommentar> SchreibenAsync(string dokument, string abschnitt, string abschnittTitel, string text, Guid? antwortAuf = null,
        CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        if (antwortAuf is { } id)
        {
            var start = await daten.LadenAsync<Kommentar>(id, "Kommentar", abbruch);
            if (start.AntwortAuf is { } oberster)
            {
                start = await daten.LadenAsync<Kommentar>(oberster, "Kommentar", abbruch);
            }

            (dokument, abschnitt, abschnittTitel, antwortAuf) = (start.Dokument, start.Abschnitt, start.AbschnittTitel, start.Id);
        }

        var k = new Kommentar(dokument, abschnitt, abschnittTitel, text, wer.PersonId, uhr.GetUtcNow(), antwortAuf);
        daten.Hinzufuegen(k);
        await protokoll.ProtokollierenAsync("Dokumentation.Kommentar",
            $"{(antwortAuf is null ? "Kommentar" : "Antwort")} zu {k.Dokument}{(k.Abschnitt.Length > 0 ? "#" + k.Abschnitt : "")}", nameof(Kommentar), k.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return k;
    }

    public async Task ErledigtSetzenAsync(Guid id, bool erledigt, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var k = await daten.LadenAsync<Kommentar>(id, "Kommentar", abbruch);
        k.ErledigtSetzen(erledigt, wer.PersonId, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Dokumentation.Kommentar", $"Kommentar zu {k.Dokument} {(erledigt ? "erledigt" : "wieder geöffnet")}", nameof(Kommentar), k.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Alle Kommentare als Markdown (zum Einarbeiten), offene zuerst.</summary>
    public async Task<string> AlsMarkdownAsync(CancellationToken abbruch = default)
    {
        var faeden = await AlleAsync(abbruch);
        var sb = new StringBuilder("# Kommentare zu Konzept, Hilfe und Doku\n\n");
        sb.Append($"Stand: {Ortszeit.AlsOrtszeit(uhr.GetUtcNow()):dd.MM.yyyy HH:mm} · {faeden.Count(f => !f.Start.Kommentar.Erledigt)} offen, {faeden.Count(f => f.Start.Kommentar.Erledigt)} erledigt\n");
        foreach (var gruppe in faeden.OrderBy(f => f.Start.Kommentar.Erledigt).GroupBy(f => f.Start.Kommentar.Erledigt))
        {
            sb.Append($"\n## {(gruppe.Key ? "Erledigt" : "Offen")}\n");
            foreach (var dokument in gruppe.GroupBy(f => f.Start.Kommentar.Dokument).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                sb.Append($"\n### {dokument.Key}\n");
                foreach (var f in dokument)
                {
                    var k = f.Start.Kommentar;
                    var wo = k.AbschnittTitel.Length > 0 ? $"„{k.AbschnittTitel}“ (#{k.Abschnitt})" : "ganzes Dokument";
                    sb.Append($"\n- **{wo}** – {Zeile(f.Start)}");
                    if (k.Erledigt)
                    {
                        sb.Append($" _(erledigt von {f.Start.ErledigtVon}, {Ortszeit.AlsOrtszeit(k.ErledigtAm!.Value):dd.MM.yyyy})_");
                    }

                    sb.Append('\n').Append(Eingerueckt(k.Text, "  "));
                    foreach (var a in f.Antworten)
                    {
                        sb.Append($"  - Antwort: {Zeile(a)}\n").Append(Eingerueckt(a.Kommentar.Text, "    "));
                    }
                }
            }
        }

        return sb.ToString();

        static string Zeile(KommentarZeile z) => $"{z.Verfasst}, {Ortszeit.AlsOrtszeit(z.Kommentar.Am):dd.MM.yyyy HH:mm}";
        static string Eingerueckt(string text, string einzug) => string.Concat(text.Split('\n').Select(z => $"{einzug}> {z}\n"));
    }

    // ---------- Impressum ----------

    /// <summary>Text des Impressums (öffentlich, ohne Anmeldung); null = noch nicht gepflegt.</summary>
    public async Task<string?> ImpressumAsync(CancellationToken abbruch = default)
        => (await daten.FindenAsync<Impressum>(Impressum.EinzigeId, abbruch))?.Text;

    public async Task ImpressumSetzenAsync(string? text, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.ImpressumPflegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var jetzt = uhr.GetUtcNow();
        var i = await daten.FindenAsync<Impressum>(Impressum.EinzigeId, abbruch);
        if (i is null)
        {
            i = new Impressum(jetzt);
            daten.Hinzufuegen(i);
        }

        i.Setzen(text, wer.PersonId, jetzt);
        await protokoll.ProtokollierenAsync("Impressum", i.Text is null ? "Impressum geleert (Platzhalter)" : $"Impressum gespeichert ({i.Text.Length} Zeichen)",
            nameof(Impressum), i.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    private async Task<Benutzer> PruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var recht = Rechte.DokumentationKommentieren(wer.Rolle);
        return recht.Erlaubt ? wer : throw new InvalidOperationException("Konzept, Doku und Kommentare: " + recht.Begruendung);
    }

    private async Task<List<Kommentarfaden>> FaedenAsync(IReadOnlyList<Kommentar> alle, CancellationToken abbruch)
    {
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        string Name(Guid? id) => id is { } x && namen.TryGetValue(x, out var n) ? n : "unbekannt";
        KommentarZeile Zeile(Kommentar k) => new(k, Name(k.VerfasstVonPersonId), k.Erledigt ? Name(k.ErledigtVonPersonId) : null);

        var antworten = alle.Where(k => k.AntwortAuf is not null).ToLookup(k => k.AntwortAuf!.Value);
        return alle.Where(k => k.AntwortAuf is null)
            .OrderBy(k => k.Dokument, StringComparer.Ordinal).ThenBy(k => k.Am).ThenBy(k => k.Id)
            .Select(k => new Kommentarfaden(Zeile(k), antworten[k.Id].OrderBy(a => a.Am).ThenBy(a => a.Id).Select(Zeile).ToList()))
            .ToList();
    }
}
