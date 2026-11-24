using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Application.Gemeinsam;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FraktionPerfekt.Infrastructure.Hintergrund;

/// <summary>Stellt regelmäßig die wartenden Nachrichten aus dem Postausgang zu.</summary>
[DisallowConcurrentExecution]
internal sealed class PostausgangJob(IServiceScopeFactory bereiche, IReadOnlyList<MandantOptionen> mandanten, ILogger<PostausgangJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("postausgang-zustellen", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        => await JeMandant.AusfuehrenAsync(bereiche, mandanten, async (d, m) =>
        {
            var ergebnis = await d.GetRequiredService<Zustellung>().WartendeZustellenAsync(abbruch: cancellationToken);
            if (ergebnis.Zugestellt + ergebnis.Fehler > 0)
            {
                log.LogInformation("Postausgang {Mandant}: {Zugestellt} zugestellt, {Fehler} Fehler", m.Kennung, ergebnis.Zugestellt, ergebnis.Fehler);
            }
        });
}

/// <summary>
/// Hintergrundaufträge für jeden Mandanten in einem eigenen Bereich (eigene Datenbank) – z. B. auch für die Übungsinstanz.
/// </summary>
internal static class JeMandant
{
    public static async Task AusfuehrenAsync(IServiceScopeFactory bereiche, IReadOnlyList<MandantOptionen> mandanten, Func<IServiceProvider, MandantOptionen, Task> arbeit)
    {
        foreach (var m in mandanten)
        {
            await using var bereich = bereiche.CreateAsyncScope();
            bereich.ServiceProvider.GetRequiredService<MandantAuswahl>().Festlegen(m);
            await arbeit(bereich.ServiceProvider, m);
        }
    }
}

/// <summary>Antwortfristen der Vertretungsanfragen prüfen: weiter zur nächsten Stellvertretung oder an den Vorstand eskalieren.</summary>
[DisallowConcurrentExecution]
internal sealed class VertretungsfristenJob(IServiceScopeFactory bereiche, IReadOnlyList<MandantOptionen> mandanten, ILogger<VertretungsfristenJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("vertretung-fristen", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        => await JeMandant.AusfuehrenAsync(bereiche, mandanten, async (d, m) =>
        {
            var schritte = await d.GetRequiredService<VertretungDienst>().FristenPruefenAsync(cancellationToken);
            if (schritte > 0)
            {
                log.LogInformation("Vertretungsfristen {Mandant}: {Schritte} Fälle weitergegeben oder abgeschlossen", m.Kennung, schritte);
            }
        });
}

/// <summary>Stündlich: Erinnerungen an bald fällige Aufgaben und weitere Fristen der Module.</summary>
[DisallowConcurrentExecution]
internal sealed class FristenJob(IServiceScopeFactory bereiche, IReadOnlyList<MandantOptionen> mandanten, ILogger<FristenJob> log) : IJob
{
    public static readonly JobKey Schluessel = new("fristen", "automatik");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        => await JeMandant.AusfuehrenAsync(bereiche, mandanten, async (d, m) =>
        {
            foreach (var p in d.GetServices<IFristenpruefung>())
            {
                var n = await p.PruefenAsync(cancellationToken);
                if (n > 0)
                {
                    log.LogInformation("Fristen {Pruefung} {Mandant}: {Anzahl} Vorgänge", p.Name, m.Kennung, n);
                }
            }
        });
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
