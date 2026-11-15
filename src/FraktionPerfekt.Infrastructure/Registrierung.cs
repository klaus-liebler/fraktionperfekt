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
        dienste.AddScoped<Application.Wissen.IWikiSuche, Wissen.WikiSuche>();
        if (string.Equals(konfiguration["Dateiablage:Art"], "WebDav", StringComparison.OrdinalIgnoreCase))
        {
            var webdav = new Wissen.WebDavOptionen
            {
                Adresse = konfiguration["Dateiablage:WebDav:Adresse"] is { Length: > 0 } a ? new Uri(a.EndsWith('/') ? a : a + "/") : null,
                Benutzer = konfiguration["Dateiablage:WebDav:Benutzer"],
                Passwort = konfiguration["Dateiablage:WebDav:Passwort"],
            };
            dienste.AddSingleton<Application.Wissen.IDateiablage>(new Wissen.WebDavDateiablage(new HttpClient { Timeout = TimeSpan.FromMinutes(2) }, webdav));
        }
        else
        {
            var wurzel = konfiguration["Dateiablage:Pfad"] ?? "data/dateien";
            wurzel = Path.IsPathRooted(wurzel) ? wurzel : Path.GetFullPath(Path.Combine(basisVerzeichnis, wurzel));
            dienste.AddSingleton<Application.Wissen.IDateiablage>(new Wissen.LokaleDateiablage(wurzel));
        }


        // Echter Web Push (VAPID) nur auf Wunsch; Schlüssel aus der Konfiguration oder einmalig erzeugt in data/vapid.json.
        if (string.Equals(konfiguration["Push:Art"], "WebPush", StringComparison.OrdinalIgnoreCase))
        {
            var vapid = VapidLaden(konfiguration, basisVerzeichnis);
            var pushClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            dienste.AddSingleton(vapid);
            dienste.AddScoped<IZustellKanal>(d => new Postausgang.WebPushKanal(d.GetRequiredService<IDaten>(), vapid, pushClient, d.GetRequiredService<TimeProvider>()));
        }

        // Echtes RIS über OParl nur auf ausdrücklichen Wunsch; sonst registrieren die Mocks das simulierte RIS.
        if (string.Equals(konfiguration["Ris:Art"], "OParl", StringComparison.OrdinalIgnoreCase))
        {
            var oparl = new Ris.OParlOptionen
            {
                Endpunkt = konfiguration["Ris:OParl:Endpunkt"] is { Length: > 0 } e ? new Uri(e) : null,
                Body = konfiguration["Ris:OParl:Body"],
            };
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FraktionPerfekt/1.0");
            dienste.AddSingleton<Application.Ris.IRisSchnittstelle>(d => new Ris.OParlRis(client, oparl,
                d.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Ris.OParlRis>>()));
        }

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

    private static VapidSchluessel VapidLaden(IConfiguration konfiguration, string basisVerzeichnis)
    {
        var betreff = konfiguration["Push:Vapid:Betreff"] ?? "mailto:geschaeftsstelle@fraktion.example";
        if (konfiguration["Push:Vapid:Oeffentlich"] is { Length: > 0 } o && konfiguration["Push:Vapid:Privat"] is { Length: > 0 } p)
        {
            return new VapidSchluessel(o, p, betreff);
        }

        var datei = Path.GetFullPath(Path.Combine(basisVerzeichnis, konfiguration["Push:Vapid:Datei"] ?? "data/vapid.json"));
        if (File.Exists(datei))
        {
            var gespeichert = System.Text.Json.JsonSerializer.Deserialize<VapidSchluessel>(File.ReadAllText(datei))!;
            return gespeichert with { Betreff = betreff };
        }

        var (oeffentlich, privat) = Postausgang.WebPush.SchluesselErzeugen();
        Directory.CreateDirectory(Path.GetDirectoryName(datei)!);
        File.WriteAllText(datei, System.Text.Json.JsonSerializer.Serialize(new VapidSchluessel(oeffentlich, privat, betreff)));
        return new VapidSchluessel(oeffentlich, privat, betreff);
    }
}
