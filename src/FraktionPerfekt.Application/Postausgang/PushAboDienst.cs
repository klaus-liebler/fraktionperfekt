using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Postausgang;

namespace FraktionPerfekt.Application.Postausgang;

/// <summary>VAPID-Schlüsselpaar (base64url) und Kontakt (mailto: oder https:) des Absenders. Nur registriert, wenn Push:Art=WebPush.</summary>
public sealed record VapidSchluessel(string Oeffentlich, string Privat, string Betreff);

/// <summary>Web-Push auf einem Gerät aktivieren und abmelden. Ohne VAPID-Schlüssel (Mock-Betrieb) ist Push nur simuliert.</summary>
public sealed class PushAboDienst(IDaten daten, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr, VapidSchluessel? vapid = null)
{
    /// <summary>Öffentlicher VAPID-Schlüssel für <c>pushManager.subscribe</c>; <c>null</c> im Mock-Betrieb.</summary>
    public string? OeffentlicherSchluessel => vapid?.Oeffentlich;

    public async Task<IReadOnlyList<PushAbo>> MeineAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return wer.PersonId is { } p
            ? await daten.ListeAsync(daten.Menge<PushAbo>().Where(a => a.PersonId == p).OrderBy(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch)
            : [];
    }

    public async Task AnmeldenAsync(string endpunkt, string p256dh, string auth, string? geraet, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.ErsterAsync(daten.Menge<PushAbo>().Where(a => a.Endpunkt == endpunkt).OrderBy(a => a.Id), abbruch) is { } alt)
        {
            daten.Entfernen(alt);
        }

        var abo = new PushAbo(wer.PersonIdPflicht, endpunkt, p256dh, auth, geraet, uhr.GetUtcNow());
        daten.Hinzufuegen(abo);
        await protokoll.ProtokollierenAsync("Push.Angemeldet", "Web-Push auf einem Gerät aktiviert", nameof(PushAbo), abo.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task AbmeldenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<PushAbo>(id, abbruch) is { } abo && abo.PersonId == wer.PersonId)
        {
            daten.Entfernen(abo);
            await protokoll.ProtokollierenAsync("Push.Abgemeldet", "Web-Push auf einem Gerät abgemeldet", nameof(PushAbo), id, abbruch: abbruch);
            await daten.SpeichernAsync(abbruch);
        }
    }
}
