namespace FraktionPerfekt.Application.Dokumente;

/// <summary>Ein Dokument im Fraktionslayout. Der Inhalt ist einfaches Markdown (#, ##, -, **fett**, Absätze).</summary>
public sealed record PdfDokument(string Titel, string Untertitel, string Markdown);

/// <summary>Erzeugt PDFs im Fraktionslayout (Briefkopf mit Logo, Seitenzahlen). Umsetzung mit PDFsharp/MigraDoc.</summary>
public interface IPdfErzeugung
{
    byte[] Erzeugen(PdfDokument dokument);
}

/// <summary>Briefkopf der Fraktion: Absender und Logo (beim Start aus <c>wwwroot/marke/logo-druck.png</c> geladen).</summary>
public sealed class Briefkopf
{
    public string Absender { get; set; } = "CDU-Fraktion Greven";

    public string Zusatz { get; set; } = "im Rat der Stadt Greven";

    public byte[]? Logo { get; set; }
}
