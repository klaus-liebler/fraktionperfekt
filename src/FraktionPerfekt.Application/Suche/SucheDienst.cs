using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Suche;

public sealed record Suchergebnis(string Bereich, string Titel, string? Text, string Ziel);

/// <summary>
/// Globale Suche über alle Module (Kopfzeile). Jeder Bereich sucht mit den Rechten seines Moduls: Was man im Modul nicht
/// sieht, findet man auch hier nicht. Höchstens acht Treffer je Bereich.
/// </summary>
public sealed class SucheDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    KalenderDienst kalender,
    WikiDienst wiki,
    DateiDienst dateien,
    BeschlussArchiv beschluesse,
    KontaktDienst kontakte,
    AnfrageDienst anfragen,
    AnliegenDienst anliegen,
    PresseDienst presse,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const int JeBereich = 8;

    public async Task<IReadOnlyList<Suchergebnis>> SuchenAsync(string? anfrage, CancellationToken abbruch = default)
    {
        var woerter = (anfrage ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(w => w.Length >= 2).ToArray();
        if (woerter.Length == 0)
        {
            return [];
        }

        bool Passt(params string?[] felder) => woerter.All(w => felder.Any(f => f?.Contains(w, StringComparison.CurrentCultureIgnoreCase) == true));
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var helfer = wer.Rolle is Rolle.Helferin or null;
        var heute = Ortszeit.Heute(uhr);
        var treffer = new List<Suchergebnis>();
        void Hinzu(IEnumerable<Suchergebnis> e) => treffer.AddRange(e.Take(JeBereich));

        Hinzu((await kalender.TermineAsync(heute.AddDays(-365), heute.AddDays(365), abbruch: abbruch))
            .Where(z => Passt(z.Termin.Titel, z.Termin.Ort, z.Termin.Beschreibung, z.GremiumKurz))
            .OrderBy(z => Math.Abs((z.Termin.Beginn.Date - heute.ToDateTime(TimeOnly.MinValue)).TotalDays))
            .Select(z => new Suchergebnis("Termine", z.Termin.Titel, $"{z.Termin.Beginn:ddd dd.MM.yyyy, HH:mm} · {z.Termin.Ort}", $"termine/{z.Termin.Id}")));

        if (!helfer)
        {
            Hinzu((await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && Passt(p.Vorname, p.Nachname, p.Anzeigename))
                .Select(p => new Suchergebnis("Personen", p.Anzeigename, null, $"personen/{p.Id}")));
            Hinzu((await stammdaten.GremienAsync(abbruch)).Where(g => g.Aktiv && Passt(g.Name, g.Kurzname))
                .Select(g => new Suchergebnis("Gremien", g.Name, g.Kurzname, $"gremien/{g.Id}")));
            Hinzu((await daten.ListeAsync(daten.Menge<Antrag>().OrderByDescending(a => a.Id), abbruch)).Where(a => Passt(a.Titel, a.Nummer, a.Thema))
                .Select(a => new Suchergebnis("Anträge", a.Titel, $"{a.Nummer} {a.Status.Anzeigename()}".Trim(), $"antraege/{a.Id}")));
            Hinzu((await beschluesse.SuchenAsync(anfrage, null, null, null, abbruch))
                .Select(b => new Suchergebnis("Beschlüsse", b.Beschluss.Titel, $"{b.Beschluss.Datum:dd.MM.yyyy} · {b.Beschluss.Quelle}{(b.Gremium is null ? "" : $" {b.Gremium}")}",
                    $"beschluesse?q={Uri.EscapeDataString(anfrage!)}")));
            Hinzu((await anfragen.ListeAsync(abbruch)).Where(z => Passt(z.Anfrage.Titel, z.Anfrage.Text, z.Anfrage.Thema))
                .Select(z => new Suchergebnis("Anfragen", z.Anfrage.Titel, z.Anfrage.Status.ToString(), "anfragen")));
            Hinzu((await anliegen.ListeAsync(abbruch)).Where(z => Passt(z.Anliegen.Betreff, z.Anliegen.Text, z.Anliegen.Thema))
                .Select(z => new Suchergebnis("Bürgeranliegen", z.Anliegen.Betreff, z.Anliegen.Status.ToString(), "anliegen")));
            Hinzu((await presse.MitteilungenAsync(abbruch)).Where(z => Passt(z.Mitteilung.Titel, z.Mitteilung.Text, z.Mitteilung.Thema))
                .Select(z => new Suchergebnis("Presse", z.Mitteilung.Titel, z.Mitteilung.Status.ToString(), "presse")));
            Hinzu((await kontakte.ListeAsync(anfrage, abbruch))
                .Select(z => new Suchergebnis("Kontakte", z.Kontakt.Name, $"{z.Kontakt.Organisation} {z.Kontakt.Funktion}".Trim(), "kontakte")));
        }

        Hinzu((await wiki.SuchenAsync(anfrage!, abbruch)).Select(t => new Suchergebnis("Wiki", t.Seite.Titel, t.Ausschnitt.Replace("**", "", StringComparison.Ordinal),
            $"wiki/seite/{Uri.EscapeDataString(t.Seite.Titel)}")));
        Hinzu((await dateien.SuchenAsync(woerter, abbruch)).Select(z => new Suchergebnis("Dateien", z.Datei.Name, z.Datei.Ordner, $"dateien/{z.Datei.Id}")));
        return treffer;
    }
}
