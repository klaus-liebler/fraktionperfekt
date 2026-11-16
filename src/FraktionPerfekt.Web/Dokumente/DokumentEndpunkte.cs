using FraktionPerfekt.Application.Dokumente;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Domain.Sitzungen;

namespace FraktionPerfekt.Web.Dokumente;

/// <summary>Downloads (PDF, DOCX, Dateien) der Module; Rechte prüfen die Dienste.</summary>
internal static class DokumentEndpunkte
{
    public static void MapDokumente(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitzungen/{id:guid}/protokoll.pdf", async (Guid id, SitzungDienst sitzungen, IPdfErzeugung pdf, FraktionPerfekt.Application.Wissen.BildDienst bilder) =>
        {
            if (await sitzungen.DetailAsync(id) is not { } d)
            {
                return Results.NotFound();
            }

            var text = await sitzungen.ProtokolltextAsync(id);
            var bytes = pdf.Erzeugen(new PdfDokument($"Protokoll: {d.Sitzung.Titel}",
                $"{d.Termin.Beginn:dddd, dd.MM.yyyy, HH:mm} Uhr · {d.Termin.Ort} · {d.Sitzung.Status.Anzeigename()}", text, await bilder.InhalteAsync(text)));
            return Results.File(bytes, "application/pdf", $"protokoll-{d.Termin.Beginn:yyyy-MM-dd}.pdf");
        }).RequireAuthorization();

        app.MapGet("/antraege/{id:guid}/antrag.pdf", async (Guid id, FraktionPerfekt.Application.Antraege.AntragDienst antraege) =>
        {
            try
            {
                var (datei, bytes) = await antraege.PdfAsync(id);
                return Results.File(bytes, "application/pdf", datei);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization();
    }
}
