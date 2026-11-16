using FraktionPerfekt.Application.Dokumente;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace FraktionPerfekt.Infrastructure.Dokumente;

/// <summary>PDF im Fraktionslayout (wie die Briefe in VibeVHS): Logo rechts oben, Absender links, Seitenzahl unten.</summary>
internal sealed class MarkdownPdf(Briefkopf briefkopf) : IPdfErzeugung
{
    static MarkdownPdf()
    {
        if (OperatingSystem.IsWindows())
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
    }

    public byte[] Erzeugen(PdfDokument dokument)
    {
        ArgumentNullException.ThrowIfNull(dokument);
        var dok = new Document();
        dok.Info.Title = dokument.Titel;
        dok.Info.Author = briefkopf.Absender;
        var normal = dok.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 10.5;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
        var h1 = dok.Styles[StyleNames.Heading1]!;
        h1.Font.Size = 16;
        h1.Font.Bold = true;
        h1.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        var h3 = dok.Styles[StyleNames.Heading3]!;
        h3.Font.Size = 11;
        h3.Font.Bold = true;
        h3.ParagraphFormat.SpaceBefore = Unit.FromPoint(8);
        var h2 = dok.Styles[StyleNames.Heading2]!;
        h2.Font.Size = 12.5;
        h2.Font.Bold = true;
        h2.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        h2.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);

        var s = dok.AddSection();
        s.PageSetup.PageFormat = PageFormat.A4;
        s.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        s.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
        s.PageSetup.RightMargin = Unit.FromCentimeter(2);
        s.PageSetup.BottomMargin = Unit.FromCentimeter(2.2);

        var fuss = s.Footers.Primary.AddParagraph();
        fuss.Format.Font.Size = 8;
        fuss.Format.Font.Color = Colors.Gray;
        fuss.AddText($"{briefkopf.Absender} · {dokument.Titel} · Seite ");
        fuss.AddPageField();
        fuss.AddText(" von ");
        fuss.AddNumPagesField();

        // Kopf: Absender links, Logo rechts (Logo der Fraktion, nur skaliert)
        var kt = s.AddTable();
        kt.AddColumn(Unit.FromCentimeter(10));
        kt.AddColumn(Unit.FromCentimeter(6.5));
        var kz = kt.AddRow();
        kz.BottomPadding = Unit.FromCentimeter(0.8);
        var kopf = kz.Cells[0].AddParagraph();
        kopf.AddFormattedText(briefkopf.Absender, new Font { Size = 13, Bold = true });
        kopf.AddLineBreak();
        kopf.AddFormattedText(briefkopf.Zusatz, new Font { Size = 9, Color = Colors.Gray });
        if (briefkopf.Logo is { } logo)
        {
            var la = kz.Cells[1].AddParagraph();
            la.Format.Alignment = ParagraphAlignment.Right;
            var bild = la.AddImage("base64:" + Convert.ToBase64String(logo));
            bild.Height = Unit.FromCentimeter(0.9);
            bild.LockAspectRatio = true;
        }

        s.AddParagraph(dokument.Titel, StyleNames.Heading1);
        var unter = s.AddParagraph(dokument.Untertitel);
        unter.Format.Font.Color = Colors.DimGray;
        unter.Format.SpaceAfter = Unit.FromPoint(10);

        new MarkdownNachMigraDoc(s, dokument.Bilder ?? new Dictionary<Guid, byte[]>()).Schreiben(dokument.Markdown);

        var renderer = new PdfDocumentRenderer { Document = dok };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms);
        return ms.ToArray();
    }
}
