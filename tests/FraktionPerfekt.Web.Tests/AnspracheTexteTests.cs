using System.Text.RegularExpressions;

namespace FraktionPerfekt.Web.Tests;

/// <summary>
/// Oberflächentexte sprechen niemanden fest mit „Sie“/„Ihr…“ an: Persönliche Nachrichten richten sich nach der Du/Sie-Einstellung
/// (<c>Ansprache.DuSie</c>, <c>Ansprache.Begruessung</c>), alle übrigen Erklärtexte, Hinweise und Begründungen sind neutral.
/// </summary>
public sealed partial class AnspracheTexteTests
{
    [GeneratedRegex(@"""[^""\n]*\b(Ihr|Ihre|Ihrem|Ihren|Ihrer|Ihnen|Sie (haben|sind|vertreten|dürfen|können|bearbeiten)|Wählen Sie|versuchen Sie)\b[^""\n]*""|>[^<\n]*\b(Ihr|Ihre|Ihrem|Ihren|Ihrer|Ihnen)\b[^<\n]*<")]
    private static partial Regex FesteSieForm();

    [Fact]
    public void Keine_feste_Sie_Ansprache_ausserhalb_von_DuSie()
    {
        var src = Path.Combine(Repository(), "src");
        var funde = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".razor", StringComparison.Ordinal) || f.EndsWith(".cs", StringComparison.Ordinal))
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains("Testdaten", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((z, i) => (f, i, z)))
            .Where(t => !t.z.Contains("DuSie", StringComparison.Ordinal) && !t.z.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && FesteSieForm().IsMatch(t.z))
            .Select(t => $"{Path.GetRelativePath(src, t.f)}:{t.i + 1}: {t.z.Trim()}")
            .ToList();
        funde.ShouldBeEmpty(string.Join("\n", funde));
    }

    internal static string RepositoryFuerTests() => Repository();

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

/// <summary>
/// Blazor: „@bind-Value:event“ an einer Komponente (z. B. RadzenTextBox) gibt ChangeEventArgs an ValueChanged(string) weiter und beendet
/// die Sitzung („ChangeEventArgs cannot be converted to type String“). Für Eingabe beim Tippen gibt es bei Radzen Immediate="true".
/// </summary>
public sealed class BlazorBindungTests
{
    [Fact]
    public void Kein_bind_Value_event_an_Komponenten()
    {
        var src = Path.Combine(AnspracheTexteTests.RepositoryFuerTests(), "src");
        var funde = Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadLines(f).Select((z, i) => (f, i, z)))
            .Where(t => t.z.Contains("@bind-Value:event", StringComparison.Ordinal))
            .Select(t => $"{Path.GetRelativePath(src, t.f)}:{t.i + 1}")
            .ToList();
        funde.ShouldBeEmpty(string.Join("\n", funde));
    }
}
