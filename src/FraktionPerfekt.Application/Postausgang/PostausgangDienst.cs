using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Postausgang;

/// <summary>
/// Einziger Weg, Nachrichten nach außen zu schicken. Legt Mails und Push-Mitteilungen in den Postausgang (Outbox) und
/// protokolliert den Vorgang (nur mit Pseudonym). Gespeichert wird erst mit dem Speichern der fachlichen Änderung.
/// </summary>
public sealed class PostausgangDienst(
    INachrichtenSpeicher speicher,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    /// <summary>
    /// Benachrichtigt eine Person: Push-Mitteilung (knapp, ohne vertrauliche Details für den Sperrbildschirm) und
    /// zusätzlich eine Mail, wenn eine Adresse hinterlegt ist.
    /// </summary>
    public async Task<IReadOnlyList<AusgehendeNachricht>> BenachrichtigenAsync(Person person, string betreff, string pushText, string mailText,
        string? link, string? aktion = null, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(person);
        var ergebnis = new List<AusgehendeNachricht>
        {
            await PushEinreihenAsync(person, betreff, pushText, link, aktion, abbruch),
        };
        if (person.EMail is { } mail)
        {
            ergebnis.Add(await MailEinreihenAsync(mail, person.Anzeigename, betreff, mailText, person.Id, link, abbruch));
        }

        return ergebnis;
    }

    public async Task<AusgehendeNachricht> MailEinreihenAsync(string eMail, string? anzeigename, string betreff, string text,
        Guid? personId = null, string? link = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var nachricht = new AusgehendeNachricht(Kanal.EMail, eMail, anzeigename, betreff, text, uhr.GetUtcNow(), wer.Anzeige, personId, link);
        speicher.Hinzufuegen(nachricht);
        await protokoll.ProtokollierenAsync("Postausgang.MailEingereiht",
            $"Mail an {Empfaenger(personId)} in den Postausgang gelegt", nameof(AusgehendeNachricht), nachricht.Id, abbruch: abbruch);
        return nachricht;
    }

    public async Task<AusgehendeNachricht> PushEinreihenAsync(Person person, string titel, string text, string? link, string? aktion = null,
        CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(person);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var nachricht = new AusgehendeNachricht(Kanal.Push, $"person:{person.Id:N}", person.Anzeigename, titel, text, uhr.GetUtcNow(),
            wer.Anzeige, person.Id, link, aktion);
        speicher.Hinzufuegen(nachricht);
        await protokoll.ProtokollierenAsync("Postausgang.PushEingereiht",
            $"Push-Mitteilung an {Empfaenger(person.Id)} in den Postausgang gelegt", nameof(AusgehendeNachricht), nachricht.Id, abbruch: abbruch);
        return nachricht;
    }

    /// <summary>Fehlgeschlagene Nachricht nach Prüfung erneut einreihen.</summary>
    public async Task ErneutVersuchenAsync(Guid id, CancellationToken abbruch = default)
    {
        var nachricht = await speicher.LadenAsync(id, abbruch) ?? throw new InvalidOperationException("Nachricht nicht gefunden.");
        nachricht.ErneutVersuchen();
        await protokoll.ProtokollierenAsync("Postausgang.ErneutVersuchen", "Nachricht erneut zur Zustellung freigegeben",
            nameof(AusgehendeNachricht), id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    private static string Empfaenger(Guid? personId) => personId is { } p ? Pseudonym.Person(p) : "externe Adresse";
}
