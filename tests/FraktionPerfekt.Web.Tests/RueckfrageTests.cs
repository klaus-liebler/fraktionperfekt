using System.Text.RegularExpressions;

namespace FraktionPerfekt.Web.Tests;

/// <summary>
/// Leitlinie „Rückfrage vor Folgenreichem“ (aus SchoolCraft FA-3230): Jede Schaltfläche zum Löschen, Entfernen, Stornieren, Widerrufen oder
/// Zurückziehen fragt vorher nach – am Rechner per Dialog (<c>Dialoge.</c>), auf den statischen Handy-Seiten per <c>data-rueckfrage</c>.
/// </summary>
public sealed partial class RueckfrageTests
{
    [GeneratedRegex(@"(Text=""[^""]*|>\s*)(Löschen|löschen|entfernen|stornieren|Widerrufen|Doch nicht|zurückziehen|Kann nicht|[Aa]bgehängt|Sitzung beenden|Jetzt abschließen|Freigeben und versenden)\b")]
    private static partial Regex FolgenreicheSchaltflaeche();

    [Fact]
    public void Folgenreiche_Schaltflaechen_fragen_nach()
    {
        var src = Path.Combine(AnspracheTexteTests.RepositoryFuerTests(), "src", "FraktionPerfekt.Web", "Components");
        var funde = new List<string>();
        foreach (var datei in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
        {
            var zeilen = File.ReadAllLines(datei);
            var mitDialog = zeilen.Any(z => z.Contains("Dialoge.", StringComparison.Ordinal));
            for (var i = 0; i < zeilen.Length; i++)
            {
                var z = zeilen[i];
                if (!FolgenreicheSchaltflaeche().IsMatch(z) || z.Contains("Abbrechen", StringComparison.Ordinal) || !(z.Contains("<button", StringComparison.Ordinal) || z.Contains("Button", StringComparison.Ordinal)))
                {
                    continue;
                }

                var statischesFormular = z.Contains("type=\"submit\"", StringComparison.Ordinal);
                if (statischesFormular ? !z.Contains("data-rueckfrage", StringComparison.Ordinal) && !z.Contains("value=\"Haengt\"", StringComparison.Ordinal) : !mitDialog)
                {
                    funde.Add($"{Path.GetRelativePath(src, datei)}:{i + 1}: {z.Trim()}");
                }
            }
        }

        funde.ShouldBeEmpty(string.Join("\n", funde));
        File.ReadAllText(Path.Combine(src, "..", "wwwroot", "verhalten.js")).ShouldContain("data-rueckfrage");
    }
}
