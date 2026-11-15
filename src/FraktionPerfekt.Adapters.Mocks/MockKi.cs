using System.Text.RegularExpressions;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Adapters.Mocks;

/// <summary>
/// Regelbasierte Mock-KI für das Wiki. Findet ohne Sprachmodell typische Pflegefälle und schlägt Änderungen vor:
/// <list type="bullet">
/// <item>Dubletten (gleicher oder sehr ähnlicher Titel),</item>
/// <item>veraltete Angaben (Jahreszahlen, die zwei oder mehr Jahre zurückliegen, mit Wörtern wie „aktuell“, „derzeit“, „geplant“),</item>
/// <item>Lücken (neuere Beschlüsse zu einem Thema der Seite, die dort noch nicht stehen),</item>
/// <item>verwaiste Seiten (keine andere Seite verweist darauf).</item>
/// </list>
/// Antworten auf Fragen nennen immer die Quellseiten.
/// </summary>
public sealed partial class MockKi : IKiAssistent
{
    public string Name => "KI (Mock)";

    public Task<IReadOnlyList<KiVorschlag>> PruefenAsync(IReadOnlyList<KiSeite> seiten, IReadOnlyList<KiBeschluss> beschluesse, DateOnly heute,
        CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(seiten);
        ArgumentNullException.ThrowIfNull(beschluesse);
        var vorschlaege = new List<KiVorschlag>();

        // Dubletten: gleicher normalisierter Titel oder ein Titel enthält den anderen vollständig
        foreach (var gruppe in seiten.GroupBy(s => WikiSeite.Normalisiert(s.Titel)).Where(g => g.Count() > 1))
        {
            var ziel = gruppe.OrderBy(s => s.GeaendertAm).First();
            foreach (var d in gruppe.Where(s => s.Id != ziel.Id))
            {
                vorschlaege.Add(new KiVorschlag(d.Id, Vorschlagsart.Dublette,
                    $"„{d.Titel}“ behandelt dasselbe wie „{ziel.Titel}“. Vorschlag: zusammenführen und hier nur noch verweisen.",
                    $"Diese Seite ist mit [[{ziel.Titel}]] zusammengeführt.\n\n{d.Text}"));
            }
        }

        foreach (var s in seiten)
        {
            // Veraltet
            var alt = JahrMitZeitwort().Matches(s.Text).Select(m => int.Parse(m.Groups["jahr"].Value, System.Globalization.CultureInfo.InvariantCulture))
                .Where(j => j <= heute.Year - 2).ToList();
            if (alt.Count > 0)
            {
                vorschlaege.Add(new KiVorschlag(s.Id, Vorschlagsart.Veraltet,
                    $"Die Seite beschreibt Angaben aus {alt.Min()} als aktuell. Bitte prüfen und ggf. auf {heute.Year} aktualisieren.",
                    s.Text + $"\n\n> Hinweis ({heute:dd.MM.yyyy}): Angaben aus {alt.Min()} prüfen."));
            }

            // Lücken aus neueren Beschlüssen
            var stichworte = Stichworte(s.Titel);
            var neu = beschluesse.Where(b => b.Datum > DateOnly.FromDateTime(s.GeaendertAm.UtcDateTime)
                                             && stichworte.Any(w => b.Titel.Contains(w, StringComparison.CurrentCultureIgnoreCase))
                                             && !s.Text.Contains(b.Titel, StringComparison.CurrentCultureIgnoreCase)).Take(3).ToList();
            if (neu.Count > 0)
            {
                vorschlaege.Add(new KiVorschlag(s.Id, Vorschlagsart.Luecke,
                    $"{neu.Count} neuere Beschlüsse zum Thema fehlen auf der Seite (z. B. „{neu[0].Titel}“).",
                    s.Text + "\n\n## Beschlüsse (ergänzt)\n\n" + string.Join("\n", neu.Select(b => $"- {b.Datum:dd.MM.yyyy}: **{b.Titel}** – {b.Text}"))));
            }

            // Verwaist
            if (!seiten.Any(x => x.Id != s.Id && x.Verweise.Contains(s.Titel, StringComparer.CurrentCultureIgnoreCase)))
            {
                var passend = seiten.Where(x => x.Id != s.Id && x.Bereich == s.Bereich).Select(x => x.Titel).FirstOrDefault();
                vorschlaege.Add(new KiVorschlag(s.Id, Vorschlagsart.Verwaist,
                    $"Keine andere Seite verweist auf „{s.Titel}“.{(passend is null ? "" : $" Vorschlag: von „{passend}“ aus verlinken.")}", null));
            }
        }

        return Task.FromResult<IReadOnlyList<KiVorschlag>>(vorschlaege);
    }

    public Task<KiAntwort> FragenAsync(string frage, IReadOnlyList<KiSeite> treffer, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(treffer);
        if (treffer.Count == 0)
        {
            return Task.FromResult(new KiAntwort("Dazu steht im Wiki noch nichts. Vorschlag: eine Seite anlegen.", []));
        }

        var saetze = treffer.Select(t => $"Laut „{t.Titel}“: {ErsterAbsatz(t.Text)}");
        return Task.FromResult(new KiAntwort("(Simulierte Antwort aus den gefundenen Seiten.)\n\n" + string.Join("\n\n", saetze),
            treffer.Select(t => (t.Id, t.Titel)).ToList()));
    }

    private static string ErsterAbsatz(string text)
    {
        var absatz = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(a => !a.StartsWith('#')) ?? text;
        return absatz.Length > 280 ? absatz[..280] + " …" : absatz;
    }

    private static List<string> Stichworte(string titel)
        => titel.Split([' ', ',', ':', '-', '(', ')'], StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length >= 6).ToList();

    [GeneratedRegex(@"(aktuell|derzeit|zurzeit|geplant|ab|bis)\D{0,40}(?<jahr>20\d\d)", RegexOptions.IgnoreCase)]
    private static partial Regex JahrMitZeitwort();
}
