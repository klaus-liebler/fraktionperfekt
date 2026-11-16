using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Datenbank-Regel 4: UUIDv7 aus einem zentralen Erzeuger (in Tests deterministisch), binär als 16-Byte-BLOB in Big-Endian.</summary>
public sealed class IdTests
{
    [Fact]
    public void Deterministischer_Erzeuger_liefert_gleiche_zeitlich_sortierte_UUIDv7()
    {
        var start = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
        List<Guid> Folge()
        {
            using var _ = Ids.Verwenden(Ids.Deterministisch(start));
            return Enumerable.Range(0, 5).Select(_ => Ids.Neu()).ToList();
        }

        var a = Folge();
        a.ShouldBe(Folge());
        a.ShouldAllBe(g => g.Version == 7);
        a.Select(g => Convert.ToHexString(g.ToByteArray(true))).ShouldBe(a.Select(g => Convert.ToHexString(g.ToByteArray(true))).Order(StringComparer.Ordinal));
        Ids.Neu().ShouldNotBe(a[0]);   // außerhalb des Bereichs wieder zufällig
    }

    [Fact]
    public async Task Ids_liegen_als_16_Byte_BLOB_und_sortieren_nach_der_Zeit()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var (typ, laenge, hex) = await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var c = (SqliteConnection)db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT typeof(Id), length(Id), hex(Id) FROM Personen ORDER BY Id LIMIT 1";
            await using var r = await cmd.ExecuteReaderAsync();
            await r.ReadAsync();
            return (r.GetString(0), r.GetInt64(1), r.GetString(2));
        });
        (typ, laenge).ShouldBe(("blob", 16L));
        hex[12].ShouldBe('7');   // Versions-Nibble an Byte 6 (Big-Endian)
    }
}
