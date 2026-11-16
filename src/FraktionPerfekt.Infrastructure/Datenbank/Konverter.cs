using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraktionPerfekt.Infrastructure.Datenbank;

/// <summary>Ids als 16-Byte-BLOB in Big-Endian-Reihenfolge (RFC 9562): die BLOB-Sortierung folgt bei UUIDv7 der Zeit (Datenbank-Regel 4).</summary>
public sealed class GuidZuBlob() : ValueConverter<Guid, byte[]>(g => g.ToByteArray(true), b => new Guid(b, true));
