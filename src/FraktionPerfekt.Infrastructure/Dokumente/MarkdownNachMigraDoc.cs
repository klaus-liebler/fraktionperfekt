using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MigraDoc.DocumentObjectModel;
using MdTabelle = Markdig.Extensions.Tables.Table;
using MdZeile = Markdig.Extensions.Tables.TableRow;
using MdZelle = Markdig.Extensions.Tables.TableCell;

namespace FraktionPerfekt.Infrastructure.Dokumente;

/// <summary>
/// Überträgt Markdown (Markdig, ohne Roh-HTML) in ein MigraDoc-Dokument: Überschriften, Absätze, Aufzählungen, Nummerierungen,
/// Aufgabenlisten, Zitate, Code, Tabellen, Trennlinien, **fett**, *kursiv*, Links (Text mit Adresse), [[Wiki-Links]] und
/// eingebettete Bilder aus der eigenen Ablage (dateien/bild/{guid}); fehlende Bilder erscheinen als Hinweis mit Alt-Text.
/// Die erste Überschrift der Ebene 1 entfällt (der Titel steht schon im Kopf).
/// </summary>
internal sealed partial class MarkdownNachMigraDoc(Section abschnitt, IReadOnlyDictionary<Guid, byte[]> bilder)
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml().UsePipeTables().UseEmphasisExtras().UseTaskLists().UseSoftlineBreakAsHardlineBreak().Build();

    /// <summary>Nutzbare Breite auf A4 mit den Rändern des Fraktionslayouts.</summary>
    private static readonly Unit Breite = Unit.FromCentimeter(16.5);

    public void Schreiben(string? markdown)
    {
        var quelle = WikiLink().Replace(markdown ?? string.Empty, "$1");
        var dokument = Markdown.Parse(quelle, Pipeline);
        var titelUebersprungen = false;
        foreach (var block in dokument)
        {
            if (block is HeadingBlock { Level: 1 } && !titelUebersprungen)
            {
                titelUebersprungen = true;
                continue;
            }

            Block(block, 0);
        }
    }

    private void Block(Block block, int einzug)
    {
        switch (block)
        {
            case HeadingBlock h:
                var u = abschnitt.AddParagraph(string.Empty, h.Level <= 2 ? StyleNames.Heading2 : StyleNames.Heading3);
                Inlines(u, h.Inline, false, false);
                break;
            case ParagraphBlock p:
                var absatz = abschnitt.AddParagraph();
                absatz.Format.LeftIndent = Unit.FromCentimeter(0.6 * einzug);
                Inlines(absatz, p.Inline, false, false);
                break;
            case ListBlock liste:
                ListeSchreiben(liste, einzug);
                break;
            case QuoteBlock zitat:
                foreach (var kind in zitat)
                {
                    if (kind is ParagraphBlock zp)
                    {
                        var z = abschnitt.AddParagraph();
                        z.Format.LeftIndent = Unit.FromCentimeter((0.6 * einzug) + 0.6);
                        z.Format.Font.Color = Colors.DimGray;
                        Inlines(z, zp.Inline, false, true);
                    }
                    else
                    {
                        Block(kind, einzug + 1);
                    }
                }

                break;
            case CodeBlock code:
                var c = abschnitt.AddParagraph(string.Join("\n", code.Lines.Lines.Take(code.Lines.Count).Select(l => l.ToString())));
                c.Format.Font.Name = "Courier New";
                c.Format.Font.Size = 9;
                c.Format.Shading.Color = Colors.WhiteSmoke;
                break;
            case ThematicBreakBlock:
                var linie = abschnitt.AddParagraph();
                linie.Format.Borders.Bottom.Width = 0.5;
                linie.Format.Borders.Bottom.Color = Colors.Gray;
                break;
            case MdTabelle tabelle:
                TabelleSchreiben(tabelle);
                break;
            case ContainerBlock container:
                foreach (var kind in container)
                {
                    Block(kind, einzug);
                }

                break;
        }
    }

    private void ListeSchreiben(ListBlock liste, int einzug)
    {
        var nr = int.TryParse(liste.OrderedStart, out var start) ? start : 1;
        foreach (var eintrag in liste.OfType<ListItemBlock>())
        {
            var marke = liste.IsOrdered ? $"{nr++}. " : "• ";
            var erster = true;
            foreach (var kind in eintrag)
            {
                if (erster && kind is ParagraphBlock ersterAbsatz)
                {
                    var zeile = abschnitt.AddParagraph();
                    zeile.Format.LeftIndent = Unit.FromCentimeter(0.6 * (einzug + 1));
                    zeile.Format.FirstLineIndent = Unit.FromCentimeter(-0.45);
                    zeile.AddText(ersterAbsatz.Inline?.FirstChild is TaskList t ? (t.Checked ? "[x] " : "[ ] ") : marke);
                    Inlines(zeile, ersterAbsatz.Inline, false, false);
                }
                else
                {
                    Block(kind, einzug + 1);
                }

                erster = false;
            }
        }
    }

    private void TabelleSchreiben(MdTabelle quelle)
    {
        var zeilen = quelle.OfType<MdZeile>().ToList();
        var spalten = zeilen.Count == 0 ? 0 : zeilen.Max(z => z.Count);
        if (spalten == 0)
        {
            return;
        }

        var t = abschnitt.AddTable();
        t.Borders.Width = 0.5;
        t.Borders.Color = Colors.Gray;
        for (var i = 0; i < spalten; i++)
        {
            t.AddColumn(Unit.FromPoint(Breite.Point / spalten));
        }

        foreach (var z in zeilen)
        {
            var zeile = t.AddRow();
            zeile.HeadingFormat = z.IsHeader;
            for (var i = 0; i < z.Count; i++)
            {
                if (z[i] is MdZelle zelle)
                {
                    foreach (var p in zelle.OfType<ParagraphBlock>())
                    {
                        Inlines(zeile.Cells[i].AddParagraph(), p.Inline, z.IsHeader, false);
                    }
                }
            }
        }

        abschnitt.AddParagraph().Format.SpaceAfter = Unit.FromPoint(2);
    }

    private void Inlines(Paragraph p, ContainerInline? inlines, bool fett, bool kursiv)
    {
        if (inlines is null)
        {
            return;
        }

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline l:
                    Text(p, l.Content.ToString(), fett, kursiv);
                    break;
                case CodeInline c:
                    p.AddFormattedText(c.Content, new Font("Courier New"));
                    break;
                case LineBreakInline:
                    p.AddLineBreak();
                    break;
                case EmphasisInline e:
                    var istFett = e.DelimiterCount >= 2 && e.DelimiterChar is '*' or '_';
                    var istKursiv = e.DelimiterCount == 1 && e.DelimiterChar is '*' or '_';
                    Inlines(p, e, fett || istFett, kursiv || istKursiv);
                    break;
                case LinkInline { IsImage: true } bild:
                    Bild(p, bild);
                    break;
                case LinkInline link:
                    Inlines(p, link, fett, kursiv);
                    if (link.Url is { } url && (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)))
                    {
                        p.AddFormattedText($" ({url})", new Font { Size = 8, Color = Colors.Gray });
                    }

                    break;
                case AutolinkInline auto:
                    Text(p, auto.Url, fett, kursiv);
                    break;
                case TaskList:
                    break; // Kästchen setzt die Liste
                case HtmlEntityInline h:
                    Text(p, h.Transcoded.ToString(), fett, kursiv);
                    break;
                case ContainerInline c:
                    Inlines(p, c, fett, kursiv);
                    break;
            }
        }
    }

    private static void Text(Paragraph p, string text, bool fett, bool kursiv)
    {
        if (!fett && !kursiv)
        {
            p.AddText(text);
            return;
        }

        var f = p.AddFormattedText(text);
        f.Bold = fett;
        f.Italic = kursiv;
    }

    private void Bild(Paragraph p, LinkInline link)
    {
        var alt = string.Concat(link.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
        var treffer = BildAdresse().Match(link.Url ?? string.Empty);
        if (treffer.Success && Guid.TryParse(treffer.Groups[1].Value, out var id) && bilder.TryGetValue(id, out var inhalt))
        {
            var b = p.AddImage("base64:" + Convert.ToBase64String(inhalt));
            b.LockAspectRatio = true;
            // natürliche Größe bei 96 dpi, höchstens Satzspiegel
            var breitePunkte = Application.Wissen.Bildpruefung.Erkennen(inhalt) is { } fmt ? fmt.Breite * 0.75 : Breite.Point;
            b.Width = Unit.FromPoint(Math.Min(breitePunkte, Breite.Point));
            return;
        }

        p.AddFormattedText($"[Bild: {alt}]", new Font { Italic = true, Color = Colors.Gray });
    }

    [GeneratedRegex(@"\[\[([^\[\]\n]+?)\]\]")]
    private static partial Regex WikiLink();

    [GeneratedRegex(@"dateien/bild/([0-9a-fA-F-]{36})")]
    private static partial Regex BildAdresse();
}
