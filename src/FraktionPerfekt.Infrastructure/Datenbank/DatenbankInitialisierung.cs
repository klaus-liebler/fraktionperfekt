using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FraktionPerfekt.Infrastructure.Datenbank;

public static class DatenbankInitialisierung
{
    /// <summary>
    /// Legt das Datenverzeichnis an, spielt Migrationen ein und schaltet SQLite in den WAL-Modus
    /// (paralleles Lesen während geschrieben wird; die Einstellung bleibt in der Datei gespeichert).
    /// </summary>
    public static async Task InitialisierenAsync(IServiceProvider dienste, CancellationToken abbruch = default)
    {
        // Jede Mandanten-Datenbank migrieren (Datenbank-Regel 6: je Mandant eine eigene Datenbank)
        foreach (var mandant in dienste.GetRequiredService<IReadOnlyList<Application.Gemeinsam.MandantOptionen>>())
        {
            await MandantInitialisierenAsync(dienste, mandant, abbruch);
        }
    }

    private static async Task MandantInitialisierenAsync(IServiceProvider dienste, Application.Gemeinsam.MandantOptionen mandant, CancellationToken abbruch)
    {
        await using var scope = dienste.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<Application.Gemeinsam.MandantAuswahl>().Festlegen(mandant);
        var db = scope.ServiceProvider.GetRequiredService<FraktionDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatenbankInitialisierung));

        var verbindung = new SqliteConnectionStringBuilder(db.Database.GetConnectionString());
        var datei = Path.GetFullPath(verbindung.DataSource);
        var verzeichnis = Path.GetDirectoryName(datei);
        if (!string.IsNullOrEmpty(verzeichnis))
        {
            Directory.CreateDirectory(verzeichnis);
        }

        await db.Database.MigrateAsync(abbruch);
        var modus = await db.Database.SqlQueryRaw<string>("PRAGMA journal_mode=WAL").ToListAsync(abbruch);
        log.LogInformation("Datenbank {Datei} bereit, Journal-Modus {Modus}", datei, modus.FirstOrDefault());
    }
}

/// <summary>Nur für "dotnet ef migrations add ..." zur Entwicklungszeit.</summary>
internal sealed class DesignZeitFabrik : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<FraktionDbContext>
{
    public FraktionDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<FraktionDbContext>().UseSqlite("Data Source=designzeit.db").Konfigurieren(false).Options);
}
