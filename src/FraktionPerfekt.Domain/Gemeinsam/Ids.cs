namespace FraktionPerfekt.Domain.Gemeinsam;

/// <summary>
/// Zentraler Id-Erzeuger: GUID Version 7 (zeitlich sortierbar; gespeichert als 16-Byte-BLOB in Big-Endian-Reihenfolge, damit die
/// Sortierung im Index der Zeit folgt). In Tests deterministisch über <see cref="Verwenden"/> (gilt nur im aktuellen asynchronen Ablauf).
/// </summary>
public static class Ids
{
    private static readonly AsyncLocal<Func<Guid>?> Erzeuger = new();

    public static Guid Neu() => Erzeuger.Value?.Invoke() ?? Guid.CreateVersion7();

    /// <summary>Für die Dauer des Bereichs (und nur im aktuellen asynchronen Ablauf) einen eigenen Erzeuger verwenden.</summary>
    public static IDisposable Verwenden(Func<Guid> erzeuger)
    {
        ArgumentNullException.ThrowIfNull(erzeuger);
        var vorher = Erzeuger.Value;
        Erzeuger.Value = erzeuger;
        return new Zuruecksetzen(() => Erzeuger.Value = vorher);
    }

    /// <summary>
    /// Deterministische UUIDv7: 48 Bit Millisekunden ab <paramref name="start"/> (je Id eine Millisekunde weiter), Version 7, Variante RFC 9562,
    /// Rest aus einem Zähler – gleiche Folge bei jedem Lauf, zeitlich sortiert.
    /// </summary>
    public static Func<Guid> Deterministisch(DateTimeOffset start)
    {
        long n = 0;
        var basis = start.ToUnixTimeMilliseconds();
        return () =>
        {
            var i = Interlocked.Increment(ref n);
            var ms = basis + i;
            Span<byte> b = stackalloc byte[16];
            for (var k = 0; k < 6; k++)
            {
                b[k] = (byte)(ms >> (8 * (5 - k)));
            }

            b[6] = 0x70;
            b[7] = 0;
            b[8] = 0x80;
            for (var k = 9; k < 16; k++)
            {
                b[k] = (byte)(i >> (8 * (15 - k)));
            }

            return new Guid(b, bigEndian: true);
        };
    }

    private sealed class Zuruecksetzen(Action aktion) : IDisposable
    {
        public void Dispose() => aktion();
    }
}
