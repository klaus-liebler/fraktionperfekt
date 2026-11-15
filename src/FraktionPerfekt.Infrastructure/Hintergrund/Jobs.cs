using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application.Vertretung;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FraktionPerfekt.Infrastructure.Hintergrund;

/// <summary>Stellt regelmäßig die wartenden Nachrichten aus dem Postausgang zu.</summary>
[DisallowConcurrentExecution]
internal sealed class PostausgangJob(Zustellung zustellung, ILogger<PostausgangJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("postausgang-zustellen", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var ergebnis = await zustellung.WartendeZustellenAsync(abbruch: cancellationToken);
        if (ergebnis.Zugestellt + ergebnis.Fehler > 0)
        {
            log.LogInformation("Postausgang: {Zugestellt} zugestellt, {Fehler} Fehler", ergebnis.Zugestellt, ergebnis.Fehler);
        }
    }
}

/// <summary>Antwortfristen der Vertretungsanfragen prüfen: weiter zur nächsten Stellvertretung oder an den Vorstand eskalieren.</summary>
[DisallowConcurrentExecution]
internal sealed class VertretungsfristenJob(VertretungDienst vertretung, ILogger<VertretungsfristenJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("vertretung-fristen", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var schritte = await vertretung.FristenPruefenAsync(cancellationToken);
        if (schritte > 0)
        {
            log.LogInformation("Vertretungsfristen: {Schritte} Fälle weitergegeben oder abgeschlossen", schritte);
        }
    }
}

/// <summary>Tägliche Datensicherung mit Wiederherstellungstest.</summary>
[DisallowConcurrentExecution]
internal sealed class SicherungJob(Application.Betrieb.ISicherung sicherung, ILogger<SicherungJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("sicherung", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var e = await sicherung.SichernAsync(cancellationToken);
        log.LogInformation("Sicherung {Datei}: {Ergebnis}", e.Datei, e.Pruefergebnis);
    }
}

/// <summary>Nächtlicher Abgleich mit dem Ratsinformationssystem.</summary>
[DisallowConcurrentExecution]
internal sealed class RisAbgleichJob(RisAbgleich abgleich, ILogger<RisAbgleichJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("ris-abgleich", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var ergebnis = await abgleich.AbgleichenAsync(cancellationToken);
        log.LogInformation("RIS-Abgleich ({Quelle}): {Zusammenfassung}", ergebnis.Quelle, ergebnis.Zusammenfassung);
    }
}
