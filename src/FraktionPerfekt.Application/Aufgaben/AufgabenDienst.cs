using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Aufgaben;

namespace FraktionPerfekt.Application.Aufgaben;

/// <summary>Aufgabenkorb je Rolle (in Iteration 1: Eskalationen offener Vertretungen an den Vorstand).</summary>
public sealed class AufgabenDienst(IVertretungSpeicher speicher, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr)
{
    /// <summary>Offene Aufgaben im Korb der aktiven Rolle (Administration sieht alle).</summary>
    public async Task<IReadOnlyList<Aufgabe>> KorbAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return wer.Rolle switch
        {
            Domain.Personen.Rolle.Administration => await speicher.OffeneAufgabenAsync(null, abbruch),
            { } rolle => await speicher.OffeneAufgabenAsync(rolle, abbruch),
            _ => [],
        };
    }

    public async Task ErledigenAsync(Guid id, string? vermerk, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var aufgabe = await speicher.AufgabeAsync(id, abbruch) ?? throw new InvalidOperationException("Aufgabe nicht gefunden.");
        if (wer.Rolle != aufgabe.Zustaendig && wer.Rolle != Domain.Personen.Rolle.Administration)
        {
            throw new InvalidOperationException("Die Aufgabe liegt im Korb einer anderen Rolle.");
        }

        aufgabe.Erledigen(uhr.GetUtcNow(), wer.Anzeige, vermerk);
        await protokoll.ProtokollierenAsync("Aufgabe.Erledigt", $"Aufgabe [{aufgabe.Anlass}-{aufgabe.Id.ToString("N")[^8..]}] erledigt", nameof(Aufgabe),
            aufgabe.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }
}
