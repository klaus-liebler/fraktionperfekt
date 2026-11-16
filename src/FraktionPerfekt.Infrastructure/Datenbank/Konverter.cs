using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraktionPerfekt.Infrastructure.Datenbank;

/// <summary>Ids als 16-Byte-BLOB in Big-Endian-Reihenfolge (RFC 9562): die BLOB-Sortierung folgt bei UUIDv7 der Zeit (Datenbank-Regel 4).</summary>
public sealed class GuidZuBlob() : ValueConverter<Guid, byte[]>(g => g.ToByteArray(true), b => new Guid(b, true));

/// <summary>Zeitpunkte nur in UTC als int64 Millisekunden seit 1970 (Datenbank-Regel 5).</summary>
public sealed class ZeitpunktZuMillisekunden() : ValueConverter<DateTimeOffset, long>(
    v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));

/// <summary>Kalendertage ohne Uhrzeit als int JJJJMMTT (Datenbank-Regel 5); offenes Ende ist 99991231 (<see cref="DateOnly.MaxValue"/>).</summary>
public sealed class TagZuZahl() : ValueConverter<DateOnly, int>(
    d => (d.Year * 10000) + (d.Month * 100) + d.Day, z => new DateOnly(z / 10000, z / 100 % 100, z % 100));

/// <summary>
/// Termine mit Uhrzeit: im Programm Wanduhrzeit (Ortszeit des Mandanten), gespeichert als UTC in Millisekunden – je Wert einzeln mit dem
/// an diesem Tag gültigen Versatz umgerechnet (Serientermine bleiben über die Zeitumstellung auf derselben Ortszeit).
/// </summary>
public sealed class OrtszeitZuMillisekunden() : ValueConverter<DateTime, long>(
    d => Application.Gemeinsam.Ortszeit.Zeitpunkt(d).ToUnixTimeMilliseconds(),
    v => Application.Gemeinsam.Ortszeit.AlsOrtszeit(DateTimeOffset.FromUnixTimeMilliseconds(v)));
