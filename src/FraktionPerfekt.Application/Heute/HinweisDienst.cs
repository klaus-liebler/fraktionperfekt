using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Politik;
using FraktionPerfekt.Domain.Kampagnen;
using FraktionPerfekt.Domain.Wissen;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Heute;

/// <summary>Hinweis auf der Startseite aus einem Modul (was ansteht, ohne eigene Aufgabe). <paramref name="Stil"/>: info, warn, krit.</summary>
public sealed record Hinweis(string Bereich, string Titel, string? Text, string Ziel, string Stil = "info");

/// <summary>
/// Sammelt für die Startseite, was in den Modulen der Stufe 2 ansteht und nicht schon als Aufgabe im Korb liegt:
/// eigene Schichten und Gebiete, überfällige Anfragen, KI-Vorschläge, Abhängen nach der Wahl, offene Zuwendungsbestätigungen,
/// Pressemitteilungen kurz vor dem Termin.
/// </summary>
public sealed class HinweisDienst(IDaten daten, IAktuellerBenutzer benutzer, IOptions<FraktionOptionen> optionen, TimeProvider uhr)
{
    public async Task<IReadOnlyList<Hinweis>> LadenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.PersonId is not { } ich)
        {
            return [];
        }

        var heute = Ortszeit.Heute(uhr);
        var jetzt = Ortszeit.Jetzt(uhr);
        var leitung = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt;
        var liste = new List<Hinweis>();

        // Kampagnen: eigene Einbuchungen (7 Tage), zugeteilte Gebiete, eigene offene Tickets, Abhängen nach dem Wahltag
        var bis = jetzt.AddDays(7);
        var meineTermine = await daten.ListeAsync(daten.Menge<Domain.Termine.Teilnahme>()
            .Where(e => e.PersonId == ich && e.Status == Domain.Termine.Teilnahmestatus.Zugesagt).Select(e => e.TerminId), abbruch);
        foreach (var t in await daten.ListeAsync(daten.Menge<Domain.Termine.Termin>()
                     .Where(t => t.KampagneId != null && meineTermine.Contains(t.Id) && t.Ende > jetzt && t.Beginn < bis && !t.Abgesagt).OrderBy(t => t.Beginn).ThenBy(t => t.Id), abbruch))
        {
            liste.Add(new Hinweis("Kampagnen", $"{t.Titel} {t.Beginn:ddd dd.MM., HH:mm}–{t.Ende:HH:mm}", t.Ort, $"kampagnen/{t.KampagneId}?ansicht=termine"));
        }

        foreach (var g in await daten.ListeAsync(daten.Menge<Gebiet>().Where(g => g.ZugeteiltPersonId == ich && g.Status == Gebietsstatus.Zugeteilt).OrderBy(g => g.Name).ThenBy(g => g.Id), abbruch))
        {
            liste.Add(new Hinweis("Kampagnen", $"Gebiet „{g.Name}“ ({g.Art})", g.Beschreibung, $"kampagnen/{g.KampagneId}?ansicht=gebiete"));
        }

        foreach (var t in await daten.ListeAsync(daten.Menge<Ticket>().Where(t => t.ZustaendigPersonId == ich && t.Status != Ticketstatus.Erledigt)
                     .OrderByDescending(t => t.Prioritaet).ThenBy(t => t.Nummer).ThenBy(t => t.Id), abbruch))
        {
            liste.Add(new Hinweis("Kampagnen", $"Ticket #{t.Nummer}: {t.Titel}", t.Faellig is { } f ? $"fällig {f:dd.MM.yyyy}" : null,
                $"kampagnen/{t.KampagneId}?ansicht=tickets&ticket={t.Id}", t.Faellig is { } ff && ff < heute ? "warn" : "info"));
        }

        foreach (var k in await daten.ListeAsync(daten.Menge<Kampagne>().Where(k => k.Wahltag != null).OrderBy(k => k.Id), abbruch))
        {
            var wahl = k.Wahltag!.Value;
            if (heute <= wahl)
            {
                continue;
            }

            var haengen = await daten.AnzahlAsync(daten.Menge<Plakat>().Where(p => p.KampagneId == k.Id && (p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt)), abbruch);
            if (haengen > 0)
            {
                var frist = wahl.AddDays(optionen.Value.AbhaengenTage);
                liste.Add(new Hinweis("Kampagnen", $"{haengen} Plakate hängen noch ({k.Name})", $"Abhängen bis {frist:dd.MM.yyyy}", "handy/plakat?abhaengen=1",
                    heute > frist ? "krit" : "warn"));
            }
        }

        if (wer.Rolle is Rolle.Helferin)
        {
            return liste;
        }

        // Anfragen: eigene überfällige (Leitung: alle)
        var anfragen = (await daten.ListeAsync(daten.Menge<Anfrage>().Where(a => a.Status == Anfragestatus.Eingereicht && (leitung || a.AutorPersonId == ich))
            .OrderBy(a => a.FristBis).ThenBy(a => a.Id), abbruch)).Where(a => a.Ueberfaellig(heute)).ToList();
        if (anfragen.Count > 0)
        {
            liste.Add(new Hinweis("Anfragen", anfragen.Count == 1 ? $"Anfrage überfällig: {anfragen[0].Titel}" : $"{anfragen.Count} Anfragen überfällig",
                $"Frist seit {anfragen[0].FristBis:dd.MM.} verstrichen", "anfragen", "warn"));
        }

        // Presse: geplante Mitteilungen der nächsten 3 Tage ohne Freigabe (eigene; Vorstand: alle)
        foreach (var m in (await daten.ListeAsync(daten.Menge<Pressemitteilung>().Where(m => (m.Status == Pressestatus.Entwurf || m.Status == Pressestatus.ZurFreigabe)
                     && (wer.Rolle == Rolle.Vorstand || m.AutorPersonId == ich)).OrderBy(m => m.Id), abbruch))
                 .Where(m => m.GeplantAm is { } g && g >= heute && g <= heute.AddDays(3)))
        {
            liste.Add(new Hinweis("Presse", $"Pressemitteilung am {m.GeplantAm:dd.MM.}: {m.Titel}", m.Status == Pressestatus.Entwurf ? "noch Entwurf" : "wartet auf Freigabe", "presse", "warn"));
        }

        // Wiki: offene KI-Vorschläge für die Verantwortlichen
        if (leitung && await daten.AnzahlAsync(daten.Menge<WikiVorschlag>().Where(v => v.Status == Wikivorschlagsstatus.Offen), abbruch) is > 0 and var n)
        {
            liste.Add(new Hinweis("Wiki", $"{n} Änderungsvorschläge der KI", "Vorher/Nachher prüfen und entscheiden", "wiki/vorschlaege"));
        }

        // Kassier: offene Zuwendungsbestätigungen
        if (wer.Rolle is Rolle.Kassier)
        {
            var offen = (await daten.ListeAsync(daten.Menge<Spende>().Where(s => s.Spender != null && s.QuittungAm == null).OrderBy(s => s.Id), abbruch)).Count(s => s.QuittungFaellig);
            if (offen > 0)
            {
                liste.Add(new Hinweis("Finanzen", $"{offen} Zuwendungsbestätigungen offen", "Spenden ab 300 Euro", "finanzen?kasse=Wahlkampf", "warn"));
            }
        }

        return liste;
    }
}
