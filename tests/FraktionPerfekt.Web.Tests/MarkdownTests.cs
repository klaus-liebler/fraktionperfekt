using FraktionPerfekt.Web.Components.Bausteine;

namespace FraktionPerfekt.Web.Tests;

/// <summary>Markdown-Darstellung (Markdig) mit Bereinigung gegen XSS.</summary>
public sealed class MarkdownTests
{
    [Fact]
    public void Markdown_rendert_Tabellen_Bilder_und_Wiki_Links()
    {
        var html = Markdown.Html("# Titel\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\nSiehe [[Antrag stellen]].\n\n![Plan.png](dateien/bild/0196a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b)");
        html.ShouldContain("<h2");
        html.ShouldContain("<table>");
        html.ShouldContain("href=\"wiki/seite/Antrag%20stellen\"");
        html.ShouldContain("src=\"dateien/bild/0196a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b\"");
        html.ShouldContain("alt=\"Plan.png\"");
    }

    [Theory]
    [InlineData("<script>alert(1)</script>", "<script")]
    [InlineData("<img src=x onerror=alert(1)>", "<img")]
    [InlineData("[klick](javascript:alert(1))", "javascript:")]
    [InlineData("[klick](JaVaScRiPt:alert(1))", "JaVaScRiPt:")]
    [InlineData("[klick](data:text/html;base64,PHNjcmlwdD4=)", "data:")]
    [InlineData("![x](https://boese.example/spur.png)", "boese.example")]
    [InlineData("![x](//boese.example/spur.png)", "boese.example")]
    [InlineData("<javascript:alert(1)>", "href=\"javascript")]
    public void Markdown_entfernt_gefaehrliches_HTML_und_Adressen(string eingabe, string verboten)
        => Markdown.Html(eingabe).ShouldNotContain(verboten);

    [Fact]
    public void Erlaubte_Links_bleiben_erhalten()
    {
        Markdown.Html("[Stadt](https://www.greven.de)").ShouldContain("href=\"https://www.greven.de\"");
        Markdown.Html("[Mail](mailto:info@example.org)").ShouldContain("href=\"mailto:info@example.org\"");
        Markdown.Html("[Wiki](wiki/seite/Haushalt)").ShouldContain("href=\"wiki/seite/Haushalt\"");
    }
}
