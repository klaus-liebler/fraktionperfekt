using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Infrastructure.Datenbank;
using FraktionPerfekt.Infrastructure.Hintergrund;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace FraktionPerfekt.Infrastructure;

public static class Registrierung
{
    /// <param name="strengeAbfragepruefung">Abfragen ohne eindeutige Sortierung brechen ab (Entwicklung, Tests) statt nur protokolliert zu werden.</param>
    /// <param name="basisVerzeichnis">Relative Datenbankpfade werden hierzu aufgelöst (Inhaltsverzeichnis der Anwendung).</param>
    /// <param name="hintergrundjobs">Quartz-Jobs starten (in Tests aus: dort werden die Routinen direkt aufgerufen).</param>
    public static IServiceCollection AddFraktionInfrastruktur(this IServiceCollection dienste, IConfiguration konfiguration, string basisVerzeichnis,
        bool strengeAbfragepruefung = false, bool hintergrundjobs = true)
    {
        var verbindung = new SqliteConnectionStringBuilder(konfiguration.GetConnectionString("FraktionPerfekt") ?? "Data Source=data/fraktionperfekt.db");
        if (!Path.IsPathRooted(verbindung.DataSource) && verbindung.DataSource != ":memory:")
        {
            verbindung.DataSource = Path.GetFullPath(Path.Combine(basisVerzeichnis, verbindung.DataSource));
        }

        dienste.AddDbContext<FraktionDbContext>(o => o.UseSqlite(verbindung.ToString()).Konfigurieren(strengeAbfragepruefung));
        dienste.AddScoped<FraktionSpeicher>();
        dienste.AddScoped<IStammdatenSpeicher>(d => d.GetRequiredService<FraktionSpeicher>());
        dienste.AddScoped<ITerminSpeicher>(d => d.GetRequiredService<FraktionSpeicher>());
        dienste.AddScoped<IVertretungSpeicher>(d => d.GetRequiredService<FraktionSpeicher>());
        dienste.AddScoped<INachrichtenSpeicher, NachrichtenSpeicher>();
        dienste.AddScoped<IDaten, Daten>();
        dienste.AddSingleton<Application.Dokumente.Briefkopf>();
        dienste.AddSingleton<Application.Dokumente.IPdfErzeugung, Dokumente.MarkdownPdf>();
        dienste.AddScoped<IAuditProtokoll, AuditProtokoll>();
        dienste.AddScoped<Application.Betrieb.ISicherung, Betrieb.SqliteSicherung>();

        if (!hintergrundjobs)
        {
            return dienste;
        }

        var takt = konfiguration.GetValue("Postausgang:TaktSekunden", 15);
        dienste.AddQuartz(q =>
        {
            q.AddJob<PostausgangJob>(j => j.WithIdentity(PostausgangJob.Schluessel));
            q.AddTrigger(t => t.ForJob(PostausgangJob.Schluessel).WithIdentity("postausgang-takt", "automatik").StartNow()
                .WithSimpleSchedule(s => s.WithInterval(TimeSpan.FromSeconds(takt)).RepeatForever()));

            q.AddJob<VertretungsfristenJob>(j => j.WithIdentity(VertretungsfristenJob.Schluessel));
            q.AddTrigger(t => t.ForJob(VertretungsfristenJob.Schluessel).WithIdentity("vertretung-fuenfminuetlich", "automatik")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(20)).WithSimpleSchedule(s => s.WithInterval(TimeSpan.FromMinutes(5)).RepeatForever()));

            q.AddJob<RisAbgleichJob>(j => j.WithIdentity(RisAbgleichJob.Schluessel));
            q.AddTrigger(t => t.ForJob(RisAbgleichJob.Schluessel).WithIdentity("ris-nachts", "automatik").WithCronSchedule("0 30 2 * * ?"));

            q.AddJob<FristenJob>(j => j.WithIdentity(FristenJob.Schluessel));
            q.AddTrigger(t => t.ForJob(FristenJob.Schluessel).WithIdentity("fristen-stuendlich", "automatik").WithCronSchedule("0 7 * * * ?"));

            if (konfiguration.GetValue("Sicherung:Aktiv", true))
            {
                q.AddJob<SicherungJob>(j => j.WithIdentity(SicherungJob.Schluessel));
                q.AddTrigger(t => t.ForJob(SicherungJob.Schluessel).WithIdentity("sicherung-nachts", "automatik")
                    .WithCronSchedule(konfiguration["Sicherung:Zeitplan"] ?? "0 30 1 * * ?"));
            }
        });
        dienste.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
        return dienste;
    }
}
