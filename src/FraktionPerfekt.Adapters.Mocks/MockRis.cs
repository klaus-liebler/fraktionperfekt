using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application.Ris;

namespace FraktionPerfekt.Adapters.Mocks;

/// <summary>
/// Simuliertes Ratsinformationssystem mit fiktiven Grevener Gremien. Liefert dieselben Strukturen wie ein OParl-Adapter
/// (Organization, Person, Membership, Meeting). Sitzungen folgen einem festen Rhythmus ab einem festen Datum, sind also
/// reproduzierbar und unabhängig vom Tag des Abrufs.
/// </summary>
public sealed class MockRis : IRisSchnittstelle
{
    public string Quelle => "RIS Greven (Mock)";

    public Task<IReadOnlyList<RisGremium>> GremienAsync(CancellationToken abbruch = default)
        => Task.FromResult<IReadOnlyList<RisGremium>>(Demofraktion.Gremien.Select(g => new RisGremium(g.RisId, g.Name, g.Kurzname, g.Art)).ToList());

    public Task<IReadOnlyList<RisPerson>> PersonenAsync(CancellationToken abbruch = default)
        => Task.FromResult<IReadOnlyList<RisPerson>>(Demofraktion.Personen.Where(p => p.RisId is not null)
            .Select(p => new RisPerson(p.RisId!, p.Vorname, p.Nachname, Demofraktion.Fraktionsname))
            .Concat(Demofraktion.AndereFraktion.Select(p => new RisPerson(p.RisId, p.Vorname, p.Nachname, "Andere Fraktion")))
            .ToList());

    public Task<IReadOnlyList<RisMitgliedschaft>> MitgliedschaftenAsync(CancellationToken abbruch = default)
    {
        var beginn = new DateOnly(2025, 11, 1);
        var liste = new List<RisMitgliedschaft>();
        foreach (var g in Demofraktion.Gremien)
        {
            liste.AddRange(g.Ordentliche.Select(p => new RisMitgliedschaft($"{g.RisId}-{p}", p, g.RisId, false, beginn, null)));
            liste.AddRange(g.Stellvertretungen.Select(p => new RisMitgliedschaft($"{g.RisId}-{p}-v", p, g.RisId, true, beginn, null)));
        }

        // Andere Fraktion: sitzt im Rat, wird aber nicht übernommen.
        liste.AddRange(Demofraktion.AndereFraktion.Select(p => new RisMitgliedschaft($"ris-g01-{p.RisId}", p.RisId, "ris-g01", false, beginn, null)));
        return Task.FromResult<IReadOnlyList<RisMitgliedschaft>>(liste);
    }

    public Task<IReadOnlyList<RisSitzung>> SitzungenAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default)
    {
        var liste = new List<RisSitzung>();
        foreach (var g in Demofraktion.Gremien)
        {
            for (var (beginn, nr) = (g.ErsteSitzung, 1); DateOnly.FromDateTime(beginn) <= bis; beginn = beginn.AddDays(g.Rhythmus), nr++)
            {
                if (DateOnly.FromDateTime(beginn) < von)
                {
                    continue;
                }

                var name = g.Art == "Rat" ? $"{nr}. Sitzung des Rates" : $"{nr}. Sitzung {g.Name}";
                liste.Add(new RisSitzung($"{g.RisId}-s{nr:D3}", g.RisId, name, beginn, beginn.AddMinutes(g.DauerMinuten), g.Ort, false,
                    Tagesordnung(g.Kurzname, nr)));
            }
        }

        return Task.FromResult<IReadOnlyList<RisSitzung>>(liste.OrderBy(s => s.Beginn).ToList());
    }

    /// <summary>Beschlüsse zu den Tagesordnungspunkten vergangener Sitzungen (fiktive Ergebnisse, fester Rhythmus).</summary>
    public async Task<IReadOnlyList<RisBeschluss>> BeschluesseAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default)
    {
        string[] ergebnisse = ["mehrheitlich beschlossen", "einstimmig beschlossen", "zur Kenntnis genommen", "vertagt", "mehrheitlich abgelehnt"];
        var liste = new List<RisBeschluss>();
        foreach (var s in await SitzungenAsync(von, bis, abbruch))
        {
            if (s.Tagesordnung is null || DateOnly.FromDateTime(s.Beginn) > bis)
            {
                continue;
            }

            var punkte = s.Tagesordnung.Split('\n');
            for (var i = 0; i < punkte.Length; i++)
            {
                var titel = punkte[i].Contains(' ', StringComparison.Ordinal) ? punkte[i][(punkte[i].IndexOf(' ', 4) + 1)..] : punkte[i];
                var ergebnis = ergebnisse[(s.RisId.Length + i + s.Beginn.Day) % ergebnisse.Length];
                liste.Add(new RisBeschluss($"{s.RisId}-b{i + 1}", s.GremiumRisId, DateOnly.FromDateTime(s.Beginn), titel,
                    $"{s.Name}: {titel} – {ergebnis}.", ergebnis));
            }
        }

        return liste;
    }

    /// <summary>Fiktive Tagesordnungspunkte zu echten Grevener Themen (Stand November 2026).</summary>
    private static string? Tagesordnung(string kurzname, int nr)
    {
        string[] punkte = kurzname switch
        {
            "Rat" => ["Haushaltssatzung 2026", "Neuordnung der Grundschullandschaft", "Prioritätenliste Bauinvestitionen"],
            "HFA" => ["Haushalt 2026: Entwicklung des Defizits", "Prioritätenliste der Bauinvestitionen (rund 180 Mio. Euro)", "Badsanierung durch die Stadtwerke"],
            "ASB" => ["Neuplanung der Grundschullandschaft, Schließung zweier Standorte", "Martin-Luther-Grundschule: Nachnutzung des Grundstücks", "Ganztagsanspruch ab 2026: Raumbedarf"],
            "ASV" => ["Rathausstraße: Radfahrstreifen statt Hochbordradweg", "Prioritäten im Straßen- und Radwegebau"],
            "ASFS" => ["Ganztagsbetreuung und Familienangebote", "Bericht Seniorenbeirat"],
            _ => [],
        };
        return punkte.Length == 0 ? null : string.Join("\n", punkte.Select((p, i) => $"TOP {i + 1} {p}").Skip(nr % 2).Take(2));
    }
}
