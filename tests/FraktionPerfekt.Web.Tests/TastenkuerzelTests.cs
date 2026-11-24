using System.Text.RegularExpressions;
using FraktionPerfekt.Web.Dokumentation;

namespace FraktionPerfekt.Web.Tests;

/// <summary>Tastenkürzel als &lt;kbd&gt; (aus SchoolCraft FA-3236): in Hilfe und Oberfläche, Kürzelliste auf /hilfe, sonst kein HTML im Markdown.</summary>
public sealed partial class TastenkuerzelTests
{
    [GeneratedRegex(@"(?<!<kbd>)\b(Strg|Ctrl|Alt|Umschalt)\+(?!<kbd>)")]
    private static partial Regex KuerzelOhneKbd();

    [Fact]
    public void Kbd_wird_dargestellt_anderes_HTML_nicht()
    {
        var html = MarkdownDarstellung.AlsHtml("Drücken Sie <kbd>Strg</kbd>+<kbd>K</kbd> und <b>nicht</b> <kbd><script>x</script></kbd>");
        html.ShouldContain("<kbd>Strg</kbd>+<kbd>K</kbd>");
        html.ShouldNotContain("<b>");
        html.ShouldNotContain("<script>");
    }

    [Fact]
    public void Hilfe_hat_Kuerzelliste_und_Oberflaeche_nutzt_kbd()
    {
        var wurzel = AnspracheTexteTests.RepositoryFuerTests();
        var hilfe = File.ReadAllText(Path.Combine(wurzel, "docs", "hilfe.md"));
        hilfe.ShouldContain("## Tastenkürzel");
        hilfe.ShouldContain("<kbd>Strg</kbd>+<kbd>K</kbd>");
        var funde = Directory.EnumerateFiles(Path.Combine(wurzel, "src", "FraktionPerfekt.Web", "Components"), "*.razor", SearchOption.AllDirectories)
            .Concat([Path.Combine(wurzel, "docs", "hilfe.md"), Path.Combine(wurzel, "docs", "wiki-editor.md")])
            .SelectMany(f => File.ReadLines(f).Select((z, i) => (f, i, z)))
            .Where(t => KuerzelOhneKbd().IsMatch(t.z) && !t.z.Contains("placeholder=", StringComparison.Ordinal) && !t.z.Contains("aria-label=", StringComparison.Ordinal))
            .Select(t => $"{Path.GetFileName(t.f)}:{t.i + 1}: {t.z.Trim()}").ToList();
        funde.ShouldBeEmpty(string.Join("\n", funde));
    }
}
