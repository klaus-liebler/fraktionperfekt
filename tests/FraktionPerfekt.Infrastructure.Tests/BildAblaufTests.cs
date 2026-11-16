using System.Text;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wissen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class BildAblaufTests
{
    /// <summary>Gültiges PNG 1×1 Pixel.</summary>
    internal static readonly byte[] Png1x1 = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>Minimales JPEG-Gerüst 640×480 mit EXIF-Block (APP1) und GPS-Text darin.</summary>
    internal static byte[] JpegMitExif()
    {
        var exif = Encoding.ASCII.GetBytes("Exif\0\0GPS 52.09N 7.61E");
        var b = new List<byte> { 0xFF, 0xD8 };
        b.AddRange([0xFF, 0xE0, 0x00, 0x10, .. Encoding.ASCII.GetBytes("JFIF\0"), 1, 1, 0, 0, 1, 0, 1, 0, 0]);
        b.AddRange([0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)((exif.Length + 2) & 0xFF), .. exif]);
        b.AddRange([0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0xE0, 0x02, 0x80, 0x03, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1]);
        b.AddRange([0xFF, 0xDA, 0x00, 0x08, 1, 1, 0, 0, 0x3F, 0, 0x12, 0x34, 0xFF, 0xD9]);
        return [.. b];
    }

    [Fact]
    public void Bildpruefung_erkennt_an_der_Signatur_und_entfernt_EXIF()
    {
        Bildpruefung.Erkennen(Png1x1).ShouldBe(new Bildformat("image/png", ".png", 1, 1));
        var jpeg = JpegMitExif();
        Bildpruefung.Erkennen(jpeg).ShouldBe(new Bildformat("image/jpeg", ".jpg", 640, 480));
        var sauber = Bildpruefung.MetadatenEntfernen(jpeg, "image/jpeg");
        Encoding.ASCII.GetString(sauber).ShouldNotContain("GPS");
        Bildpruefung.Erkennen(sauber).ShouldBe(new Bildformat("image/jpeg", ".jpg", 640, 480));

        // Text mit Bild-Endung, SVG (könnte Skript enthalten) und Unsinn sind keine Bilder.
        Bildpruefung.Erkennen(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")).ShouldBeNull();
        Bildpruefung.Erkennen(Encoding.UTF8.GetBytes("Hallo, ich bin ein PNG.")).ShouldBeNull();
        Bildpruefung.Erkennen([]).ShouldBeNull();
    }

    [Fact]
    public void Eingefuegte_Bilder_ohne_Namen_bekommen_Datum_als_Namen()
    {
        var am = new DateTimeOffset(2026, 11, 16, 18, 45, 0, TimeSpan.Zero);
        BildDienst.OriginalName("image.png", ".png", am).ShouldBe("Eingefügt 2026-11-16 19-45.png");
        BildDienst.OriginalName(null, ".png", am).ShouldStartWith("Eingefügt ");
        BildDienst.OriginalName("..\\Plakat Ortsmitte.jpg", ".jpg", am).ShouldBe("Plakat Ortsmitte.jpg");
    }

    [Fact]
    public async Task Hochladen_Dedupe_Rechte_und_Aufraeumen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        BildDienst B(IServiceProvider d) => d.GetRequiredService<BildDienst>();
        WikiDienst W(IServiceProvider d) => d.GetRequiredService<WikiDienst>();
        Task<bool> Sichtbar(Guid id) => u.InAsync(async d =>
        {
            if (await B(d).OeffnenAsync(id) is not { } o)
            {
                return false;
            }

            await o.Inhalt.DisposeAsync();
            return true;
        });

        await u.AnmeldenAsync("awewers");
        var seite = (await u.InAsync(d => W(d).SeiteAsync("Fraktionssitzung")))!.Seite;
        var bild = await u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "image.png", new MemoryStream(Png1x1)));
        bild.OriginalName.ShouldStartWith("Eingefügt ");
        (bild.Breite, bild.Hoehe, bild.Typ).ShouldBe((1, 1, "image/png"));
        bild.Schluessel.ShouldBe($"Bilder/{bild.Id:N}.png");
        bild.Id.Version.ShouldBe(7);

        // Gleicher Inhalt → dasselbe Bild, nur einmal gespeichert; auf einer zweiten Seite nur eine weitere Verwendung.
        var zweite = (await u.InAsync(d => W(d).SeiteAsync("Antrag stellen")))!.Seite;
        (await u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, zweite.Id, "Logo.png", new MemoryStream(Png1x1)))).Id.ShouldBe(bild.Id);
        (await u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "nochmal.png", new MemoryStream(Png1x1)))).Id.ShouldBe(bild.Id);
        (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.IDaten>().AnzahlAsync(
            d.GetRequiredService<FraktionPerfekt.Application.IDaten>().Menge<Bild>()))).ShouldBe(1);
        (await u.InAsync(d => B(d).BilderAsync(BildDienst.Wiki, zweite.Id, null))).Single().Bild.Id.ShouldBe(bild.Id);

        // Kein Bild, zu groß
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "fake.png",
            new MemoryStream(Encoding.UTF8.GetBytes("kein Bild")))))).Message.ShouldContain("kein unterstütztes Bild");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "riesig.png",
            new MemoryStream(new byte[BildDienst.MaxBytes + 1]))))).Message.ShouldContain("größer");

        // EXIF/GPS kommt nicht in die Ablage
        var foto = await u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "Foto.jpg", new MemoryStream(JpegMitExif())));
        var gespeichert = await u.InAsync(async d =>
        {
            using var s = (await B(d).OeffnenAsync(foto.Id))!.Value.Inhalt;
            using var m = new MemoryStream();
            await s.CopyToAsync(m);
            return m.ToArray();
        });
        Encoding.ASCII.GetString(gespeichert).ShouldNotContain("GPS");

        // Rechte: Bild auf einer vertraulichen Seite sieht nur, wer die Seite sieht. Helfer:innen laden keine Bilder hoch.
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var vertraulich = await u.InAsync(d => W(d).AnlegenAsync("Hintergrund", "Personalinterna Test", "Leer", true, null));
        vertraulich.Vertraulich.ShouldBeTrue();
        var geheim = await u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, vertraulich.Id, "Organigramm.jpg", new MemoryStream(JpegMitExif()[..^2].Concat(new byte[] { 0x56, 0xFF, 0xD9 }).ToArray())));
        (await Sichtbar(geheim.Id)).ShouldBeTrue();
        await u.AnmeldenAsync("lhagedorn");
        (await Sichtbar(geheim.Id)).ShouldBeFalse();
        (await Sichtbar(bild.Id)).ShouldBeTrue();
        (await u.InAsync(d => B(d).BilderAsync(BildDienst.Wiki, vertraulich.Id, null))).ShouldBeEmpty();
        await u.AnmeldenAsync("fbaeumer");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => B(d).HochladenAsync(BildDienst.Wiki, seite.Id, "x.png", new MemoryStream(Png1x1)))))
            .Message.ShouldContain("Helfer");

        // Aufräumen: nach der Frist verschwinden Bilder, die in keinem Seitentext stehen; benutzte bleiben.
        await u.AnmeldenAsync("awewers");
        var text = (await u.InAsync(d => W(d).SeiteAsync("Fraktionssitzung")))!.Aktuell.Text + $"\n\n![{bild.OriginalName}]({bild.Adresse})";
        await u.InAsync(d => W(d).SpeichernAsync(seite.Id, text, "Bild eingefügt"));
        u.Uhr.Advance(BildDienst.Aufbewahrung + TimeSpan.FromDays(1));
        (await u.InAsync(d => B(d).AufraeumenAsync())).ShouldBe(2); // Foto und Organigramm
        (await Sichtbar(bild.Id)).ShouldBeTrue();
        (await Sichtbar(foto.Id)).ShouldBeFalse();
        (await u.InAsync(d => B(d).BilderAsync(BildDienst.Wiki, seite.Id, text))).Single().InText.ShouldBeTrue();
    }
}
