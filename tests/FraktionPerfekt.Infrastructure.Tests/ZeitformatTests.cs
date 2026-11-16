using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Datenbank-Regel 5: Zeitpunkte UTC-ms, Kalendertage JJJJMMTT (offen 99991231), Termine je Termin von Ortszeit nach UTC.</summary>
public sealed class ZeitformatTests
{
    [Fact]
    public async Task Serientermin_dienstags_19_Uhr_bleibt_ueber_beide_Zeitumstellungen_auf_19_Uhr_Ortszeit()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        // Fraktionssitzung jeweils dienstags 19:00 vor und nach der Umstellung (29.03.2026 Sommerzeit, 25.10.2026 Winterzeit)
        var tage = new[] { new DateTime(2026, 3, 24, 19, 0, 0), new DateTime(2026, 3, 31, 19, 0, 0), new DateTime(2026, 10, 20, 19, 0, 0), new DateTime(2026, 10, 27, 19, 0, 0) };
        var ids = await u.InAsync(async d =>
        {
            var s = d.GetRequiredService<ITerminSpeicher>();
            var termine = tage.Select(t => new Termin("Fraktionssitzung", Terminart.Fraktionssitzung, t, t.AddHours(2), "Rathaus", null, null, u.Uhr.GetUtcNow())).ToList();
            foreach (var t in termine)
            {
                s.Hinzufuegen(t);
            }

            await s.SpeichernAsync();
            return termine.Select(t => t.Id).ToList();
        });

        var gespeichert = await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var c = (SqliteConnection)db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync();
            var ergebnis = new List<(long Ms, string Typ)>();
            foreach (var id in ids)
            {
                await using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Beginn, typeof(Beginn) FROM Termine WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id.ToByteArray(true));
                await using var r = await cmd.ExecuteReaderAsync();
                await r.ReadAsync();
                ergebnis.Add((r.GetInt64(0), r.GetString(1)));
            }

            return ergebnis;
        });
        gespeichert.ShouldAllBe(x => x.Typ == "integer");
        gespeichert.Select(x => DateTimeOffset.FromUnixTimeMilliseconds(x.Ms).UtcDateTime.Hour).ShouldBe([18, 17, 17, 18]);   // UTC: Winter 18, Sommer 17

        var geladen = await u.InAsync(async d => (await d.GetRequiredService<FraktionDbContext>().Termine.AsNoTracking().Where(t => ids.Contains(t.Id)).ToListAsync())
            .OrderBy(t => t.Beginn).Select(t => t.Beginn).ToList());
        geladen.ShouldBe(tage);   // Ortszeit unverändert 19:00
    }

    [Fact]
    public async Task Tage_als_JJJJMMTT_offenes_Ende_99991231_Zeitpunkte_als_Millisekunden()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var (von, bis, typVon, erfasst) = await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var c = (SqliteConnection)db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT n.GueltigVon, n.GueltigBis, typeof(n.GueltigVon), v.Am FROM PersonNamen n JOIN Aenderungsvorgaenge v ON v.Id = n.VorgangId LIMIT 1";
            await using var r = await cmd.ExecuteReaderAsync();
            await r.ReadAsync();
            return (r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetInt64(3));
        });
        typVon.ShouldBe("integer");
        von.ShouldBe(20261115L);   // Testpersonen gelten ab dem Tag der Anlage
        bis.ShouldBe(99991231L);
        DateTimeOffset.FromUnixTimeMilliseconds(erfasst).ShouldBe(new DateTimeOffset(2026, 11, 15, 8, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(5));
    }
}
