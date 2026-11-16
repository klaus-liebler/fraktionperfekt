using System.Buffers.Binary;
using System.Text;

namespace FraktionPerfekt.Application.Wissen;

/// <summary>Erkanntes Bildformat mit Pixelmaßen.</summary>
public sealed record Bildformat(string Typ, string Endung, int Breite, int Hoehe);

/// <summary>
/// Prüft Bilder an der Dateisignatur (nicht an der Endung) und entfernt Metadaten (EXIF mit GPS, XMP, Textblöcke).
/// Erlaubt sind PNG, JPEG, GIF und WebP – kein SVG (könnte Skripte enthalten).
/// </summary>
public static class Bildpruefung
{
    /// <summary>Format und Maße aus den Kopfdaten; <c>null</c>, wenn es kein unterstütztes Bild ist.</summary>
    public static Bildformat? Erkennen(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 24 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) && Ascii(b, 12, "IHDR"))
        {
            return Format("image/png", ".png", BinaryPrimitives.ReadInt32BigEndian(b[16..]), BinaryPrimitives.ReadInt32BigEndian(b[20..]));
        }

        if (b.Length >= 10 && (Ascii(b, 0, "GIF87a") || Ascii(b, 0, "GIF89a")))
        {
            return Format("image/gif", ".gif", BinaryPrimitives.ReadUInt16LittleEndian(b[6..]), BinaryPrimitives.ReadUInt16LittleEndian(b[8..]));
        }

        if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            for (var i = 2; i + 9 < b.Length;)
            {
                if (b[i] != 0xFF) { return null; }
                var m = b[i + 1];
                if (m is 0xD8 or 0x01 || m is >= 0xD0 and <= 0xD7) { i += 2; continue; }
                var laenge = BinaryPrimitives.ReadUInt16BigEndian(b[(i + 2)..]);
                if (m is (>= 0xC0 and <= 0xC3) or (>= 0xC5 and <= 0xC7) or (>= 0xC9 and <= 0xCB) or (>= 0xCD and <= 0xCF))
                {
                    return Format("image/jpeg", ".jpg", BinaryPrimitives.ReadUInt16BigEndian(b[(i + 7)..]), BinaryPrimitives.ReadUInt16BigEndian(b[(i + 5)..]));
                }

                i += 2 + laenge;
            }

            return null;
        }

        if (b.Length >= 30 && Ascii(b, 0, "RIFF") && Ascii(b, 8, "WEBP"))
        {
            if (Ascii(b, 12, "VP8X"))
            {
                return Format("image/webp", ".webp", 1 + (b[24] | (b[25] << 8) | (b[26] << 16)), 1 + (b[27] | (b[28] << 8) | (b[29] << 16)));
            }

            if (Ascii(b, 12, "VP8 "))
            {
                return Format("image/webp", ".webp", BinaryPrimitives.ReadUInt16LittleEndian(b[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(b[28..]) & 0x3FFF);
            }

            if (Ascii(b, 12, "VP8L") && b[20] == 0x2F)
            {
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(b[21..]);
                return Format("image/webp", ".webp", (int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
            }
        }

        return null;
    }

    /// <summary>Entfernt Metadaten: JPEG APP1 (EXIF/GPS, XMP) und APP13 (IPTC), PNG-Text-/EXIF-Blöcke, WebP-EXIF/XMP.</summary>
    public static byte[] MetadatenEntfernen(byte[] daten, string typ)
    {
        ArgumentNullException.ThrowIfNull(daten);
        try
        {
            return typ switch
            {
                "image/jpeg" => Jpeg(daten),
                "image/png" => Png(daten),
                "image/webp" => WebP(daten),
                _ => daten,
            };
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            return daten; // beschädigte Struktur: unverändert lassen, Erkennen hat sie trotzdem als Bild bestätigt
        }
    }

    private static byte[] Jpeg(byte[] b)
    {
        using var aus = new MemoryStream(b.Length);
        aus.Write(b, 0, 2);
        var i = 2;
        while (i + 4 <= b.Length && b[i] == 0xFF)
        {
            var m = b[i + 1];
            if (m == 0xDA) { break; } // ab Start of Scan alles übernehmen
            var laenge = (b[i + 2] << 8) | b[i + 3];
            if (m is not (0xE1 or 0xED)) { aus.Write(b, i, 2 + laenge); }
            i += 2 + laenge;
        }

        aus.Write(b, i, b.Length - i);
        return aus.ToArray();
    }

    private static readonly string[] PngWeg = ["eXIf", "tEXt", "zTXt", "iTXt", "tIME"];

    private static byte[] Png(byte[] b)
    {
        using var aus = new MemoryStream(b.Length);
        aus.Write(b, 0, 8);
        var i = 8;
        while (i + 12 <= b.Length)
        {
            var laenge = BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(i));
            var art = Encoding.ASCII.GetString(b, i + 4, 4);
            if (!PngWeg.Contains(art)) { aus.Write(b, i, 12 + laenge); }
            i += 12 + laenge;
            if (art == "IEND") { break; }
        }

        return aus.ToArray();
    }

    private static byte[] WebP(byte[] b)
    {
        using var aus = new MemoryStream(b.Length);
        aus.Write(b, 0, 12);
        var i = 12;
        while (i + 8 <= b.Length)
        {
            var art = Encoding.ASCII.GetString(b, i, 4);
            var laenge = (int)BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(i + 4));
            var gesamt = 8 + laenge + (laenge & 1);
            if (art is not ("EXIF" or "XMP "))
            {
                var start = aus.Position;
                aus.Write(b, i, Math.Min(gesamt, b.Length - i));
                if (art == "VP8X") { aus.Position = start + 8; var f = b[i + 8] & ~0x0C; aus.WriteByte((byte)f); aus.Position = aus.Length; }
            }

            i += gesamt;
        }

        var ergebnis = aus.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(ergebnis.AsSpan(4), (uint)(ergebnis.Length - 8));
        return ergebnis;
    }

    private static Bildformat? Format(string typ, string endung, int breite, int hoehe)
        => breite is > 0 and <= 30000 && hoehe is > 0 and <= 30000 ? new Bildformat(typ, endung, breite, hoehe) : null;

    private static bool Ascii(ReadOnlySpan<byte> b, int ab, string text)
        => b.Length >= ab + text.Length && Encoding.ASCII.GetString(b.Slice(ab, text.Length)) == text;
}
