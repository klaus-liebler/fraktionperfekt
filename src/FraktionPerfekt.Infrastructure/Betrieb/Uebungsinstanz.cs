using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Betrieb;

/// <summary>
/// Übungsinstanz zurücksetzen (aus SchoolCraft „instanzen.ps1“ übertragen): eine frische Datenbank in einer temporären Datei aufbauen (Migrationen,
/// dann <paramref name="befuellen"/> – in der Anwendung die Demodaten, mit Datum relativ zu heute) und sie per SQLite-Online-Sicherung über die
/// Datenbank der Übungsinstanz kopieren. Die laufende Anwendung bleibt dabei an; die Datenbank des Echtbetriebs wird nie berührt.
/// </summary>
public static class Uebungsinstanz
{
    public static async Task NeuAufbauenAsync(IServiceProvider dienste, MandantOptionen uebung, Func<IServiceProvider, Task> befuellen, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(dienste);
        ArgumentNullException.ThrowIfNull(uebung);
        ArgumentNullException.ThrowIfNull(befuellen);
        if (!uebung.Uebung)
        {
            throw new InvalidOperationException("Zurücksetzen gibt es nur für Übungsinstanzen.");
        }

        string ziel;
        await using (var bereich = dienste.CreateAsyncScope())
        {
            bereich.ServiceProvider.GetRequiredService<MandantAuswahl>().Festlegen(uebung);
            ziel = bereich.ServiceProvider.GetRequiredService<FraktionDbContext>().Database.GetConnectionString()!;
        }

        var temp = Path.Combine(Path.GetTempPath(), $"fraktionperfekt-uebung-{Guid.NewGuid():N}.db");
        var tempMandant = new MandantOptionen
        {
            Kennung = uebung.Kennung, Name = uebung.Name, Zusatz = uebung.Zusatz, Region = uebung.Region, Logo = uebung.Logo, Absender = uebung.Absender,
            Zeitzone = uebung.Zeitzone, Hosts = uebung.Hosts, Uebung = true, Datenbank = $"Data Source={temp};Pooling=False",
        };
        try
        {
            await using (var bereich = dienste.CreateAsyncScope())
            {
                bereich.ServiceProvider.GetRequiredService<MandantAuswahl>().Festlegen(tempMandant);
                await bereich.ServiceProvider.GetRequiredService<FraktionDbContext>().Database.MigrateAsync(abbruch);
                await befuellen(bereich.ServiceProvider);
            }

            SqliteConnection.ClearAllPools();
            await using (var quelle = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temp, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            await using (var kopie = new SqliteConnection(new SqliteConnectionStringBuilder(ziel) { Pooling = false }.ToString()))
            {
                await quelle.OpenAsync(abbruch);
                await kopie.OpenAsync(abbruch);
                quelle.BackupDatabase(kopie);
            }

            SqliteConnection.ClearAllPools();
        }
        finally
        {
            try
            {
                File.Delete(temp);
                File.Delete(temp + "-wal");
                File.Delete(temp + "-shm");
            }
            catch (IOException)
            {
                // Temporäre Datei; Aufräumen ist nicht kritisch.
            }
        }
    }
}
