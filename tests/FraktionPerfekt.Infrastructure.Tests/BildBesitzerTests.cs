using System.Text;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Politik;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Bilder in Anträgen, Anliegen und PDFs: Rechte je Besitzer, Übernahme aus Entwürfen, Aufräumen über alle Besitzer.</summary>
public sealed class BildBesitzerTests
{
    [Fact]
    public async Task Bild_aus_dem_Entwurf_wird_beim_Anlegen_dem_Antrag_zugeordnet_Rechte_PDF_und_Aufraeumen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        BildDienst B(IServiceProvider d) => d.GetRequiredService<BildDienst>();
        AntragDienst A(IServiceProvider d) => d.GetRequiredService<AntragDienst>();
        Task<bool> Sichtbar(Guid id) => u.InAsync(async d =>
        {
            if (await B(d).OeffnenAsync(id) is not { } o)
            {
                return false;
            }

            await o.Inhalt.DisposeAsync();
            return true;
        });

        // Neues Formular: Bild hängt am Entwurf → nur die hochladende Person sieht es.
        await u.AnmeldenAsync("lkuehlert");
        var bild = await u.InAsync(d => B(d).HochladenAsync(BildBesitzer.Entwurf, Guid.NewGuid(), "Lageplan.jpg", new MemoryStream(BildAblaufTests.JpegMitExif())));
        await u.AnmeldenAsync("thorstmann");
        (await Sichtbar(bild.Id)).ShouldBeFalse();

        // Anlegen mit dem Bild im Text → Bild gehört zum Antrag, Mitglieder sehen es, Helfer:innen (sehen keine Anträge) nicht.
        await u.AnmeldenAsync("lkuehlert");
        var text = $"## Lage\n\n![Lageplan.jpg]({bild.Adresse})\n\nDie Fraktion beantragt eine Prüfung.";
        var antrag = await u.InAsync(d => A(d).AnlegenAsync("Prüfauftrag Radweg", text, null, "Verkehr"));
        await u.AnmeldenAsync("thorstmann");
        (await Sichtbar(bild.Id)).ShouldBeTrue();
        await u.AnmeldenAsync("fbaeumer");
        (await Sichtbar(bild.Id)).ShouldBeFalse();

        // PDF der Druckfassung enthält das Bild (als Bild-Objekt, nicht nur den Alt-Text).
        await u.AnmeldenAsync("lkuehlert");
        var (_, pdf) = await u.InAsync(d => A(d).PdfAsync(antrag.Id));
        var roh = Encoding.Latin1.GetString(pdf);
        roh.ShouldContain("/Image");
        roh.ShouldNotContain("[Bild: Lageplan.jpg]");

        // Neue Fassung ohne Bild: Das Bild bleibt, solange die alte Fassung existiert – auch nach Ablauf der Frist.
        await u.InAsync(d => A(d).FassungSpeichernAsync(antrag.Id, "Die Fraktion beantragt eine Prüfung (ohne Plan).", "Plan entfernt"));
        u.Uhr.Advance(BildDienst.Aufbewahrung + TimeSpan.FromDays(1));
        u.Benutzer.Aktuell = FraktionPerfekt.Application.Sicherheit.Benutzer.Automatik;
        await u.InAsync(d => B(d).AufraeumenAsync());
        await u.AnmeldenAsync("thorstmann");
        (await Sichtbar(bild.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Bild_im_Buergeranliegen_sieht_nur_wer_das_Anliegen_sieht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        BildDienst B(IServiceProvider d) => d.GetRequiredService<BildDienst>();
        AnliegenDienst A(IServiceProvider d) => d.GetRequiredService<AnliegenDienst>();

        await u.AnmeldenAsync("thorstmann");
        var bild = await u.InAsync(d => B(d).HochladenAsync(BildBesitzer.Entwurf, Guid.NewGuid(), "image.png", new MemoryStream(BildAblaufTests.Png1x1)));
        var anliegen = await u.InAsync(d => A(d).ErfassenAsync("Schlagloch Marktstraße", $"Foto anbei:\n\n![Foto]({bild.Adresse})", "Herr Beispiel (fiktiv)", null,
            Eingangskanal.Mail, "Verkehr"));

        foreach (var kennung in new[] { "kalbers", "jwilp", "uahrens", "fbaeumer", "mgries" })
        {
            await u.AnmeldenAsync(kennung);
            var siehtAnliegen = (await u.InAsync(d => A(d).ListeAsync())).Any(z => z.Anliegen.Id == anliegen.Id);
            var siehtBild = await u.InAsync(async d =>
            {
                if (await B(d).OeffnenAsync(bild.Id) is not { } o)
                {
                    return false;
                }

                await o.Inhalt.DisposeAsync();
                return true;
            });
            siehtBild.ShouldBe(siehtAnliegen, kennung);
        }

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => A(d).ListeAsync())).ShouldNotContain(z => z.Anliegen.Id == anliegen.Id);
    }
}
