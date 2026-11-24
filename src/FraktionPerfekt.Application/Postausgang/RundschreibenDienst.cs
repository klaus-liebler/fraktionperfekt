using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Postausgang;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Application.Postausgang;

public sealed record NeuesRundschreiben(string Betreff, string Text, Gestaltung Gestaltung, Anredestil Anredestil, string? Sammelanrede, Empfaengerkreis Kreis,
    Guid? VorlageId = null);

public sealed record RundschreibenZeile(Rundschreiben Rundschreiben, string Absender);

/// <summary>
/// Rundschreiben (aus SchoolCraft FA-820 ff.): Vorstand und Geschäftsführung schreiben an Ratsmitglieder, Fraktion oder Kampagnenteam – Gestaltung
/// (schlicht, Fraktionsinfo, Einladung), Anrede je Person nach ihrer Einstellung, förmlich oder als Sammelanrede. Versand über den Postausgang (Mail,
/// dazu ein knapper Push ohne Inhalt); das Archiv der versandten Rundschreiben dient als Vorlage.
/// </summary>
public sealed class RundschreibenDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IAktuellerMandant mandant,
    TimeProvider uhr)
{
    public static Freigabe Recht(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Vorstand, Rolle.Administration);

    /// <summary>Archiv: versandte Rundschreiben, neueste zuerst (nur für Vorstand und Geschäftsführung).</summary>
    public async Task<IReadOnlyList<RundschreibenZeile>> ArchivAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!Recht(wer.Rolle).Erlaubt)
        {
            return [];
        }

        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        return (await daten.ListeAsync(daten.Menge<Rundschreiben>().Where(r => r.VersandtAm != null).OrderByDescending(r => r.ErstelltAm).ThenBy(r => r.Id), abbruch))
            .Select(r => new RundschreibenZeile(r, namen.GetValueOrDefault(r.ErstelltVonPersonId, "?"))).ToList();
    }

    /// <summary>Wer das Rundschreiben bekäme (aktive Personen im Kreis).</summary>
    public async Task<IReadOnlyList<Person>> EmpfaengerAsync(Empfaengerkreis kreis, CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        return (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && kreis.Gehoert(p.RollenAm(heute, jetzt)))
            .OrderBy(p => p.Sortierschluessel, StringComparer.Ordinal).ThenBy(p => p.Id).ToList();
    }

    /// <summary>Vorschau des Mailtexts für eine Person aus dem Kreis (die erste, sonst die angemeldete Person).</summary>
    public async Task<string> VorschauAsync(NeuesRundschreiben neu, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await PruefenAsync(abbruch);
        var r = Erzeugen(neu, wer);
        var beispiel = (await EmpfaengerAsync(neu.Kreis, abbruch)).FirstOrDefault() ?? await stammdaten.PersonAsync(wer.PersonIdPflicht, abbruch)
            ?? throw new InvalidOperationException("Niemand im Empfängerkreis.");
        return r.MailtextFuer(beispiel, mandant.Mandant.Name, wer.Name, Ortszeit.Heute(uhr));
    }

    public async Task<Rundschreiben> SendenAsync(NeuesRundschreiben neu, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await PruefenAsync(abbruch);
        var r = Erzeugen(neu, wer);
        var empfaenger = await EmpfaengerAsync(neu.Kreis, abbruch);
        if (empfaenger.Count == 0)
        {
            throw new InvalidOperationException("Im gewählten Empfängerkreis ist niemand.");
        }

        daten.Hinzufuegen(r);
        var heute = Ortszeit.Heute(uhr);
        foreach (var p in empfaenger)
        {
            // Push nur als knapper Hinweis (Sperrbildschirm), der Text kommt per Mail
            await postausgang.BenachrichtigenAsync(p, r.Betreff, $"Rundschreiben: {r.Betreff}", r.MailtextFuer(p, mandant.Mandant.Name, wer.Name, heute), null,
                abbruch: abbruch);
        }

        r.Versandt(empfaenger.Count, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Rundschreiben.Versandt", $"Rundschreiben an {empfaenger.Count} Personen ({r.Kreis.Anzeigename()}) in den Postausgang gelegt",
            nameof(Rundschreiben), r.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return r;
    }

    private Rundschreiben Erzeugen(NeuesRundschreiben neu, Benutzer wer)
    {
        try
        {
            return new Rundschreiben(neu.Betreff, neu.Text, neu.Gestaltung, neu.Anredestil, neu.Sammelanrede, neu.Kreis, wer.PersonIdPflicht, uhr.GetUtcNow(), neu.VorlageId);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(ex.Message.Split(" (Parameter", 2)[0], ex);
        }
    }

    private async Task<Benutzer> PruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return Recht(wer.Rolle) is { Erlaubt: false } nein ? throw new InvalidOperationException(nein.Begruendung) : wer;
    }
}
