using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Politik;
using FraktionPerfekt.Domain.Wahlkampf;
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

        // Wahlkampf: eigene Schichten (7 Tage) und zugeteilte Gebiete
        var bis = jetzt.AddDays(7);
        var meineSchichten = await daten.ListeAsync(daten.Menge<Schichteintrag>().Where(e => e.PersonId == ich).Select(e => e.SchichtId), abbruch);
        foreach (var s in await daten.ListeAsync(daten.Menge<Schicht>().Where(s => meineSchichten.Contains(s.Id) && s.Bis > jetzt && s.Von < bis).OrderBy(s => s.Von).ThenBy(s => s.Id), abbruch))
        {
            liste.Add(new Hinweis("Wahlkampf", $"Schicht {s.Von:ddd dd.MM., HH:mm}–{s.Bis:HH:mm}", s.Ort, "wahlkampf?ansicht=schichten"));
        }

        foreach (var g in await daten.ListeAsync(daten.Menge<Gebiet>().Where(g => g.ZugeteiltPersonId == ich && g.Status == Gebietsstatus.Zugeteilt).OrderBy(g => g.Name).ThenBy(g => g.Id), abbruch))
        {
            liste.Add(new Hinweis("Wahlkampf", $"Gebiet „{g.Name}“ ({g.Art})", g.Beschreibung, "wahlkampf?ansicht=gebiete"));
        }

        if (optionen.Value.Wahltag is { } wahl && heute > wahl)
        {
            var haengen = await daten.AnzahlAsync(daten.Menge<Plakat>().Where(p => p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt), abbruch);
            if (haengen > 0)
            {
                var frist = wahl.AddDays(optionen.Value.AbhaengenTage);
                liste.Add(new Hinweis("Wahlkampf", $"{haengen} Plakate hängen noch", $"Abhängen bis {frist:dd.MM.yyyy}", "handy/plakat?abhaengen=1",
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
