using System.Diagnostics;
using FraktionPerfekt.Application.Heute;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Application.Suche;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>
/// Laufzeitmessungen für docs/messungen.md (Median aus 7 Läufen nach einem Aufwärmlauf, SQLite-Datei wie im Betrieb).
/// Nur auf Anforderung: FP_MESSUNG=1 (Demodaten, realistisch) bzw. FP_MESSUNG=10 (zehnfache Personen, Mandate und Termine).
/// Ohne Variable ist der Test sofort grün. Ergebnis: %TEMP%/fraktionperfekt-messung-{faktor}.txt.
/// </summary>
public sealed class MessungTests
{
    [Fact]
    public async Task Laufzeiten_messen()
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("FP_MESSUNG"), out var faktor) || faktor <= 0)
        {
            return;
        }

        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var aufbau = Stopwatch.StartNew();
        if (faktor > 1)
        {
            await VervielfachenAsync(u, faktor);
        }

        aufbau.Stop();
        await u.AnmeldenAsync("lkuehlert");
        var heute = FraktionPerfekt.Application.Gemeinsam.Ortszeit.Heute(u.Uhr);
        var personen = await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().PersonenAsync());
        var person = personen.First(p => p.Name.Contains("Kühlert", StringComparison.Ordinal)).Id;
        var gremium = (await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().GremienAsync())).First(g => g.Kurzname == "HFWA").Id;
        var termin = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(heute, heute.AddDays(60), new Kalenderfilter(GremiumId: gremium))))[0].Termin.Id;
        var abo = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboAnlegenAsync());
        var token = abo.Https.Segments[^1].Replace(".ics", "", StringComparison.Ordinal);

        var ergebnisse = new List<(string Name, double Median)>();
        async Task Messen(string name, Func<IServiceProvider, Task> arbeit)
        {
            var zeiten = new List<double>();
            for (var lauf = 0; lauf < 8; lauf++)
            {
                var sw = Stopwatch.StartNew();
                await u.InAsync(arbeit);
                if (lauf > 0)
                {
                    zeiten.Add(sw.Elapsed.TotalMilliseconds);
                }
            }

            zeiten.Sort();
            ergebnisse.Add((name, zeiten[zeiten.Count / 2]));
        }

        await Messen("Liste: Personen (alle, mit Rollen und Mandatszahl)", d => d.GetRequiredService<StammdatenDienst>().PersonenAsync());
        await Messen("Liste: Kalender, aktueller Monat", d => d.GetRequiredService<KalenderDienst>().TermineAsync(heute, heute.AddDays(30)));
        await Messen("Folgeseite: Kalender, Folgemonat", d => d.GetRequiredService<KalenderDienst>().TermineAsync(heute.AddDays(30), heute.AddDays(60)));
        await Messen("Filter: Kalender nach Gremium (3 Monate)", d => d.GetRequiredService<KalenderDienst>().TermineAsync(heute, heute.AddDays(90), new Kalenderfilter(GremiumId: gremium)));
        await Messen("Suche „kühlert“ (alle Module)", d => d.GetRequiredService<SucheDienst>().SuchenAsync("kühlert"));
        await Messen("Detail: Person (Stand heute, Mandate, Verlauf)", d => d.GetRequiredService<StammdatenDienst>().PersonAsync(person));
        await Messen("Detail: Termin", d => d.GetRequiredService<KalenderDienst>().TerminAsync(termin));
        await Messen("Ermittlung: Mandatsübersicht (alle Gremien mit Besetzung)", d => d.GetRequiredService<StammdatenDienst>().GremienAsync());
        await Messen("Ermittlung: Startseite „Heute“", d => d.GetRequiredService<HeuteDienst>().LadenAsync());
        await Messen("Ermittlung: Offene Vertretungen", d => d.GetRequiredService<VertretungDienst>().OffeneVertretungenAsync());
        await Messen("Ermittlung: Kalender-Abo (ICS) einer Person", d => d.GetRequiredService<KalenderDienst>().AboKalenderAsync(token));

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var groesse = new FileInfo(u.Datenbankdatei).Length + (File.Exists(u.Datenbankdatei + "-wal") ? new FileInfo(u.Datenbankdatei + "-wal").Length : 0);
        var anzahl = await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            return (await db.Personen.CountAsync(), await db.Termine.CountAsync(), await db.Mitgliedschaften.CountAsync());
        });
        var zeilen = string.Join("\n", ergebnisse.Select(e => $"| {e.Name} | {e.Median:0.0} ms |"));
        var bericht = $"MESSUNG Faktor {faktor}: {anzahl.Item1} Personen, {anzahl.Item2} Termine, {anzahl.Item3} Mitgliedschaften, Datei {groesse / 1024} KB (Aufbau {aufbau.Elapsed.TotalSeconds:0} s)\n{zeilen}";
        await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), $"fraktionperfekt-messung-{faktor}.txt"), bericht, TestContext.Current.CancellationToken);
        TestContext.Current.SendDiagnosticMessage(bericht);
    }

    /// <summary>
    /// Personenliste mit vielen Personen (FP_MESSUNG_PERSONEN=30000): Sortierung nach DIN 5007-2 in SQL über die Collation und ihren Index,
    /// dazu das Laden aller Personen mit allen Versionen. Ergebnis: %TEMP%/fraktionperfekt-messung-personen-{anzahl}.txt.
    /// </summary>
    [Fact]
    public async Task Personenliste_mit_vielen_Personen()
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("FP_MESSUNG_PERSONEN"), out var anzahl) || anzahl <= 0)
        {
            return;
        }

        await using var u = await TestUmgebung.ErzeugenAsync();
        var zufall = new Random(5007);
        string[] vor = ["Anna", "Jürgen", "Özlem", "Klaus", "Liz", "Bernd", "Ute", "Hüseyin", "Maria", "Jan"];
        string[] nach = ["Müller", "Mueller", "Schulte", "Große Maestrup", "Bäumer", "Berg", "Öztürk", "Kühlert", "Abel", "Zweers", "Schulze Höping Pellengahr"];
        var aufbau = Stopwatch.StartNew();
        var jetzt = u.Uhr.GetUtcNow();
        for (var paket = 0; paket < anzahl; paket += 2000)
        {
            await u.InAsync(async d =>
            {
                var db = d.GetRequiredService<FraktionDbContext>();
                for (var i = paket; i < Math.Min(anzahl, paket + 2000); i++)
                {
                    db.Personen.Add(new Person(vor[zufall.Next(vor.Length)], $"{nach[zufall.Next(nach.Length)]}{(i % 7 == 0 ? "" : $" {i % 997}")}", null, null, jetzt,
                        vorsatz: i % 13 == 0 ? "von" : null, gueltigAb: new DateOnly(2025, 1, 1)));
                }

                await db.SaveChangesAsync();
            });
        }

        aufbau.Stop();
        var heute = FraktionPerfekt.Application.Gemeinsam.Ortszeit.Heute(u.Uhr);
        var ergebnisse = new List<(string Name, double Median)>();
        async Task Messen(string name, Func<IServiceProvider, Task> arbeit)
        {
            var zeiten = new List<double>();
            for (var lauf = 0; lauf < 6; lauf++)
            {
                var sw = Stopwatch.StartNew();
                await u.InAsync(arbeit);
                if (lauf > 0)
                {
                    zeiten.Add(sw.Elapsed.TotalMilliseconds);
                }
            }

            zeiten.Sort();
            ergebnisse.Add((name, zeiten[zeiten.Count / 2]));
        }

        await Messen("Namensliste sortiert (SQL, Collation DIN5007_2, Index)", d => d.GetRequiredService<FraktionSpeicher>().NamenslisteAsync(heute));
        await Messen("Erste Seite (50 Namen, SQL)", d => d.GetRequiredService<FraktionSpeicher>().NamenslisteAsync(heute, hoechstens: 50));
        await Messen("Alle Personen laden (alle Versionen, Stand heute, sortiert)", d => d.GetRequiredService<FraktionPerfekt.Application.IStammdatenSpeicher>().PersonenAsync());
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var groesse = new FileInfo(u.Datenbankdatei).Length + (File.Exists(u.Datenbankdatei + "-wal") ? new FileInfo(u.Datenbankdatei + "-wal").Length : 0);
        var zeilen = string.Join("\n", ergebnisse.Select(e => $"| {e.Name} | {e.Median:0.0} ms |"));
        var bericht = $"MESSUNG {anzahl} Personen, Datei {groesse / 1024} KB (Aufbau {aufbau.Elapsed.TotalSeconds:0} s)\n{zeilen}";
        await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), $"fraktionperfekt-messung-personen-{anzahl}.txt"), bericht, TestContext.Current.CancellationToken);
        TestContext.Current.SendDiagnosticMessage(bericht);
    }

    /// <summary>Personen, Mandate und Termine (faktor − 1)-mal zusätzlich anlegen (Namen mit Nummer), wie eine größere Fraktion.</summary>
    private static async Task VervielfachenAsync(TestUmgebung u, int faktor)
    {
        await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            var jetzt = u.Uhr.GetUtcNow();
            var heute = FraktionPerfekt.Application.Gemeinsam.Ortszeit.Heute(u.Uhr);
            var personen = await db.Personen.ToListAsync();
            foreach (var p in personen)
            {
                p.StandHeuteBerechnen(jetzt);   // Stand heute wird beim Laden berechnet (nicht gespeichert)
            }

            var mitgliedschaften = await db.Mitgliedschaften.ToListAsync();
            var termine = await db.Termine.AsNoTracking().ToListAsync();
            for (var i = 1; i < faktor; i++)
            {
                foreach (var p in personen)
                {
                    var kopie = new Person(p.Vorname, $"{p.Nachname} {i}", null, p.Benutzerkennung is null ? null : $"{p.Benutzerkennung}{i}", jetzt,
                        titel: p.Titel, anrede: p.Anrede, gueltigAb: new DateOnly(2025, 11, 1));
                    kopie.RollenFestlegenAb(new DateOnly(2025, 11, 1), p.RollenAm(heute, jetzt), Erfassung.Erst(jetzt, "Messung"));
                    db.Personen.Add(kopie);
                    foreach (var m in mitgliedschaften.Where(m => m.PersonId == p.Id))
                    {
                        if (m.StandAm(heute, jetzt) is { } stand)
                        {
                            db.Mitgliedschaften.Add(new Mitgliedschaft(kopie.Id, m.GremiumId, stand.Funktion, stand.Rang, new DateOnly(2025, 11, 1), Erfassung.Erst(jetzt, "Messung")));
                        }
                    }
                }

                foreach (var t in termine)
                {
                    db.Termine.Add(new Termin($"{t.Titel} ({i})", t.Art, t.Beginn, t.Ende, t.Ort, t.GremiumId, null, jetzt, t.Beschreibung));
                }

                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        });
    }
}
