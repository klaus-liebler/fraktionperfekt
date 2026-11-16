using FraktionPerfekt.Application;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Sortierung nach DIN 5007-2 in SQL über die Collation „DIN5007_2“ (kein gespeicherter Sortierschlüssel) und ihr Index.</summary>
public sealed class Din5007Tests
{
    [Fact]
    public async Task Personenliste_sortiert_in_SQL_nach_DIN_5007_2_ueber_den_Index()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        await u.InAsync(async d =>
        {
            var s = d.GetRequiredService<IStammdatenSpeicher>();
            var jetzt = u.Uhr.GetUtcNow();
            s.Hinzufuegen(new Person("Anna", "Berg", null, null, jetzt, vorsatz: "von", gueltigAb: new DateOnly(2025, 1, 1)));
            s.Hinzufuegen(new Person("Jens", "Bärmann", null, null, jetzt, gueltigAb: new DateOnly(2025, 1, 1)));
            s.Hinzufuegen(new Person("Carl", "baer", null, null, jetzt, gueltigAb: new DateOnly(2025, 1, 1)));
            s.Hinzufuegen(new Person("Alfons", "Schulze Höping Pellengahr", null, null, jetzt, gueltigAb: new DateOnly(2025, 1, 1)));
            await s.SpeichernAsync();
        });

        var namen = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync())).Select(p => p.Anzeigename).ToList();
        var b = namen.Where(n => n is "Carl baer" or "Jens Bärmann" or "Anna von Berg").ToList();
        b.ShouldBe(["Carl baer", "Jens Bärmann", "Anna von Berg"]);   // baer < baermann < berg; „von Berg“ unter B
        namen.IndexOf("Alfons Schulze Höping Pellengahr").ShouldBeGreaterThan(namen.IndexOf("Anna von Berg"));   // mehrteilig, kein Vorsatz

        // Abfrageplan: die Liste nutzt den Index mit der Collation und sortiert nicht extra
        var plan = await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var tag = new DateOnly(2026, 11, 15);
            var sql = db.Set<PersonName>().Where(n => n.ErsetztAm == null && n.GueltigVon <= tag && (n.GueltigBis == null || n.GueltigBis > tag))
                .OrderBy(n => n.Nachname).ThenBy(n => n.Vorname).ThenBy(n => n.Vorsatz).ThenBy(n => EF.Property<Guid>(n, "PersonId"))
                .Select(n => EF.Property<Guid>(n, "PersonId")).ToQueryString();
            var c = (SqliteConnection)db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync();
            await using var cmd = c.CreateCommand();
            // ToQueryString liefert die Parameter als „.param set @name wert“-Zeilen vor dem SQL
            var zeilenSql = sql.Replace("\r", "", StringComparison.Ordinal).Split('\n');
            var rumpf = string.Join('\n', zeilenSql.Where(z => !z.StartsWith('.')));
            foreach (var par in zeilenSql.Where(z => z.StartsWith(".param set ", StringComparison.Ordinal)).Select(z => z.Split(' ', 4)))
            {
                cmd.Parameters.AddWithValue(par[2], par[3].Trim().Trim('\''));
            }

            cmd.CommandText = "EXPLAIN QUERY PLAN " + rumpf;
            var zeilen = new List<string>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                zeilen.Add(r.GetString(3));
            }

            return string.Join(" | ", zeilen);
        });
        plan.ShouldContain("IX_PersonNamen_DIN5007");
    }

    /// <summary>
    /// Verhalten externer Werkzeuge ohne die Collation (z. B. DB Browser for SQLite, sqlite3): Lesen geht, Sortieren/Vergleichen über die
    /// Namensspalten und jedes Schreiben in PersonNamen (Indexpflege) scheitert mit „no such collation sequence“.
    /// </summary>
    [Fact]
    public async Task Ohne_Collation_lesen_ja_sortieren_und_schreiben_nein()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        SqliteConnection.ClearAllPools();
        await using var c = new SqliteConnection($"Data Source={u.Datenbankdatei};Pooling=False");
        await c.OpenAsync(TestContext.Current.CancellationToken);
        async Task<string?> Versuch(string sql)
        {
            try
            {
                await using var cmd = c.CreateCommand();
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                return null;
            }
            catch (SqliteException e)
            {
                return e.Message;
            }
        }

        (await Versuch("SELECT * FROM PersonNamen")).ShouldBeNull();
        (await Versuch("SELECT COUNT(*) FROM Personen")).ShouldBeNull();
        (await Versuch("SELECT * FROM PersonNamen ORDER BY Nachname")).ShouldNotBeNull().ShouldContain("no such collation sequence");
        (await Versuch("UPDATE PersonNamen SET GueltigBis = GueltigBis")).ShouldBeNull();   // Spalten ohne Collation und ohne Index darauf: geht
        (await Versuch("UPDATE PersonNamen SET Nachname = Nachname")).ShouldNotBeNull().ShouldContain("no such collation sequence");
        (await Versuch("INSERT INTO PersonNamen SELECT * FROM PersonNamen WHERE 0")).ShouldNotBeNull().ShouldContain("no such collation sequence");   // schon das Vorbereiten scheitert
        Din5007Kollation.Registrieren(c);
        (await Versuch("SELECT * FROM PersonNamen ORDER BY Nachname")).ShouldBeNull();
        _ = Erfassung.System;
    }
}

/// <summary>Datenbank-Regel 3: Ein Speichervorgang ist ein Änderungsvorgang; er verweist auf den Protokolleintrag desselben Speicherns.</summary>
public sealed class AenderungsvorgangTests
{
    [Fact]
    public async Task Eine_Aenderung_ist_ein_Vorgang_mit_Verweis_aufs_Protokoll()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var liz = await u.AnmeldenAsync("lkuehlert");
        var heute = FraktionPerfekt.Application.Gemeinsam.Ortszeit.Heute(u.Uhr);
        await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>()
            .RufnummerSpeichernAsync(liz.Id, null, RufnummerArt.Mobil, "+49 151 0000077", Messagingdienst.Anruf, false, true, heute, false, "neue Nummer"));
        await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var nummer = await db.Set<Rufnummer>().Where(r => r.Nummer == "+49 151 0000077").SingleAsync();
            nummer.Vorgang.Grund.ShouldBe("neue Nummer");
            nummer.Vorgang.ProtokollId.ShouldNotBeNull();
            var eintrag = await db.Protokoll.SingleAsync(p => p.Id == nummer.Vorgang.ProtokollId);
            eintrag.Aktion.ShouldBe("Person.KontaktGeaendert");
            (await db.Set<Erfassung>().CountAsync(e => e.ProtokollId == eintrag.Id)).ShouldBe(1);
        });
    }
}
