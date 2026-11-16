using System.Text;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>
/// Datenbank-Regeln (übernommen aus VibeVHS): jede Spalte eingestuft, keine Kopien und JSON-Listen, keine Namen neben Personenverweisen
/// ohne begründete Ausnahme; dazu die automatisch erzeugte Schema-Dokumentation docs/datenbank-schema.md
/// (neu schreiben: Umgebungsvariable FP_DOKU_SCHREIBEN=1 setzen und die Tests ausführen).
/// </summary>
public sealed class DatenbankArchitekturTests
{
    /// <summary>Kopien und JSON-Spalten, die der laufende Umbau noch entfernt. Die Liste muss am Ende leer sein.</summary>
    private static readonly HashSet<string> Uebergang = new(StringComparer.Ordinal)
    {
        "Personen.Vorname", "Personen.Nachname", "Personen.Titel", "Personen.Anrede", "Personen.EMail", "Personen.Mobil",
        "PersonRollen.RollenText", "Vertretungsfaelle.KandidatenText", "Sitzungen.GremiumId", "Sitzungen.Titel",
        "WikiSeiten.Version", "WikiSeiten.GeaendertAm", "Aufgaben.ErledigtVon", "Termine.AngelegtVon", "WikiVorschlaege.Von",
    };

    private static FraktionDbContext Kontext() => new(new DbContextOptionsBuilder<FraktionDbContext>().UseSqlite("Data Source=:memory:").Options);

    private static List<(string Tabelle, IReadOnlyList<IProperty> Spalten)> Modell(FraktionDbContext db)
        => db.Model.GetEntityTypes().Where(e => e.GetTableName() is not null && !e.IsOwned())
            .GroupBy(e => e.GetTableName()!)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (g.Key, (IReadOnlyList<IProperty>)g.SelectMany(e => e.GetProperties()).GroupBy(p => p.GetColumnName()).Select(x => x.First())
                .OrderBy(p => p.IsPrimaryKey() ? 0 : 1).ThenBy(p => p.GetColumnName(), StringComparer.Ordinal).ToList()))
            .ToList();

    [Fact]
    public void Jede_Spalte_ist_eingestuft_und_jede_Einstufung_hat_eine_Spalte()
    {
        using var db = Kontext();
        var ist = Modell(db).SelectMany(t => t.Spalten.Select(p => $"{t.Tabelle}.{p.GetColumnName()}")).ToHashSet(StringComparer.Ordinal);
        var soll = Spalteneinstufung.Tabellen.SelectMany(t => t.Value.Keys.Select(s => $"{t.Key}.{s}")).ToHashSet(StringComparer.Ordinal);
        ist.Except(soll).Order(StringComparer.Ordinal).ShouldBeEmpty("Neue Spalten ohne Einstufung (Spalteneinstufung.cs ergänzen)");
        soll.Except(ist).Order(StringComparer.Ordinal).ShouldBeEmpty("Einstufung für Spalten, die es nicht mehr gibt");
    }

    [Fact]
    public void Keine_Kopien_und_keine_JSON_Spalten_ausser_im_laufenden_Umbau()
    {
        var verboten = Spalteneinstufung.Tabellen
            .SelectMany(t => t.Value.Where(s => s.Value.Art is Spaltenart.Kopie or Spaltenart.Json).Select(s => $"{t.Key}.{s.Key}"))
            .ToHashSet(StringComparer.Ordinal);
        verboten.Except(Uebergang).ShouldBeEmpty("Kopien und JSON-Spalten sind verboten (Regel 1)");
        Uebergang.Except(verboten).ShouldBeEmpty("Erledigte Übergänge aus der Liste streichen");
    }

    [Fact]
    public void Keine_Statistik_Spalten_und_keine_Namen_neben_Personenverweisen_ohne_Begruendung()
    {
        using var db = Kontext();
        var funde = new List<string>();
        foreach (var (tabelle, spalten) in Modell(db))
        {
            var mitPerson = spalten.Any(p => p.GetColumnName().EndsWith("PersonId", StringComparison.Ordinal));
            foreach (var p in spalten)
            {
                var name = p.GetColumnName();
                var einstufung = Spalteneinstufung.Tabellen.GetValueOrDefault(tabelle)?.GetValueOrDefault(name);
                var begruendet = einstufung is { Bedeutung: not null } e && e.Art is not (Spaltenart.Kopie or Spaltenart.Json);
                if (name.StartsWith("Statistik", StringComparison.Ordinal)
                    || (mitPerson && p.ClrType == typeof(string) && (name.EndsWith("Name", StringComparison.Ordinal) || name.EndsWith("Von", StringComparison.Ordinal)) && !begruendet))
                {
                    funde.Add($"{tabelle}.{name}");
                }
            }
        }

        funde.Except(Uebergang).ShouldBeEmpty("Statistik- bzw. Namensspalte neben einem Personenverweis ohne begründete Ausnahme");
    }

    [Fact]
    public void Schema_Dokumentation_ist_aktuell()
    {
        var pfad = Path.Combine(Repository(), "docs", "datenbank-schema.md");
        var neu = Erzeugen();
        if (Environment.GetEnvironmentVariable("FP_DOKU_SCHREIBEN") == "1")
        {
            File.WriteAllText(pfad, neu, new UTF8Encoding(false));
        }

        File.Exists(pfad).ShouldBeTrue("docs/datenbank-schema.md fehlt – mit FP_DOKU_SCHREIBEN=1 erzeugen");
        File.ReadAllText(pfad).Replace("\r\n", "\n", StringComparison.Ordinal).ShouldBe(neu, "docs/datenbank-schema.md ist veraltet – mit FP_DOKU_SCHREIBEN=1 neu erzeugen");
    }

    private static string Erzeugen()
    {
        using var db = Kontext();
        var sb = new StringBuilder();
        sb.Append("# Datenbank – Tabellen und Spalten\n\n");
        sb.Append("Automatisch erzeugt aus dem EF-Modell (`DatenbankArchitekturTests`), nicht von Hand ändern; die Einleitung steht in\n");
        sb.Append("`docs/datenbank-einleitung.md`. Einstufung siehe `Spalteneinstufung.cs`: S = Stammdatum, V = Version, G = Vorgang, B = Beleg (eingefroren),\n");
        sb.Append("A = Anonymisierungsrest; **K** = Kopie und **J** = JSON/Liste sind verboten und nur während des Umbaus vorhanden.\n\n");
        sb.Append(File.ReadAllText(Path.Combine(Repository(), "docs", "datenbank-einleitung.md")).Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd());
        sb.Append("\n\n## Übersicht\n\n");
        var modell = Modell(db);
        foreach (var (tabelle, spalten) in modell)
        {
            sb.Append($"- [{tabelle}](#{tabelle.ToLowerInvariant()}) – {spalten.Count} Spalten\n");
        }

        foreach (var (tabelle, spalten) in modell)
        {
            var typ = db.Model.GetEntityTypes().First(e => e.GetTableName() == tabelle);
            sb.Append($"\n## {tabelle}\n\n");
            sb.Append($"Klasse: `{typ.ClrType.FullName}`" + (Spalteneinstufung.Tabellenhinweise.GetValueOrDefault(tabelle) is { } h ? $" – {h}" : "") + "\n\n");
            sb.Append("| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |\n|---|---|---|---|---|---|\n");
            foreach (var p in spalten)
            {
                var spaltentyp = p.GetColumnType() + (p.GetMaxLength() is { } m ? $"({m})" : "");
                var schluessel = new List<string>();
                if (p.IsPrimaryKey())
                {
                    schluessel.Add("PK");
                }

                schluessel.AddRange(p.GetContainingForeignKeys().Select(fk => $"→ {fk.PrincipalEntityType.GetTableName()}"));
                if (p.GetContainingIndexes().Any(i => i.IsUnique))
                {
                    schluessel.Add("eindeutig");
                }
                else if (p.GetContainingIndexes().Any())
                {
                    schluessel.Add("Index");
                }

                var e = Spalteneinstufung.Tabellen.GetValueOrDefault(tabelle)?.GetValueOrDefault(p.GetColumnName());
                var kurz = e?.Art switch
                {
                    Spaltenart.Stammdatum => "S",
                    Spaltenart.Version => "V",
                    Spaltenart.Vorgang => "G",
                    Spaltenart.Beleg => "B",
                    Spaltenart.Anonymisierungsrest => "A",
                    Spaltenart.Kopie => "**K**",
                    Spaltenart.Json => "**J**",
                    _ => "?",
                };
                sb.Append($"| {p.GetColumnName()} | {spaltentyp} | {(p.IsNullable ? "" : "ja")} | {string.Join(", ", schluessel)} | {kurz} | {e?.Bedeutung} |\n");
            }

            var indizes = typ.GetIndexes().Select(i => $"({string.Join(", ", i.Properties.Select(p => ((IProperty)p).GetColumnName()))}){(i.IsUnique ? " eindeutig" : "")}").ToList();
            if (indizes.Count > 0)
            {
                sb.Append($"\nIndizes: {string.Join("; ", indizes)}\n");
            }
        }

        return sb.ToString();
    }

    private static string Repository()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "FraktionPerfekt.slnx")))
            {
                return d.FullName;
            }
        }

        throw new InvalidOperationException("Repository nicht gefunden.");
    }
}
