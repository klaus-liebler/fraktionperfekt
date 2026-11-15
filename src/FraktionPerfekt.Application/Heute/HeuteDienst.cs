using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Application.Heute;

/// <summary>Eine Schnellaktion der Startseite (Plus-Knopf).</summary>
public sealed record Schnellaktion(string Titel, string Ziel, string Symbol, Freigabe Freigabe);

public sealed record HeuteAnsicht(
    string Name,
    IReadOnlyList<TerminZeile> NaechsteTermine,
    IReadOnlyList<MeineAnfrage> Vertretungsanfragen,
    IReadOnlyList<TerminZeile> OffeneZusagen,
    IReadOnlyList<Aufgabe> Aufgaben,
    IReadOnlyList<AufgabeZeile> MeineAufgaben,
    IReadOnlyList<Abstimmungen.UmfrageZeile> OffeneAbstimmungen,
    IReadOnlyList<BesetzungZeile> OffeneBesetzungen,
    IReadOnlyList<BesetzungZeile> BerichtOffen,
    int OffeneVertretungen,
    IReadOnlyList<Schnellaktion> Schnellaktionen,
    IReadOnlyList<Hinweis>? Hinweise = null)
{
    public bool NichtsZuTun => (Hinweise is null || Hinweise.Count == 0) && Vertretungsanfragen.Count == 0 && OffeneZusagen.Count == 0 && Aufgaben.Count == 0 && MeineAufgaben.Count == 0 && OffeneAbstimmungen.Count == 0 && OffeneBesetzungen.Count == 0 && BerichtOffen.Count == 0;
}

/// <summary>Startseite „Heute“: nur, was jetzt zu tun ist – jeweils mit Ein-Klick-Antwort.</summary>
public sealed class HeuteDienst(
    KalenderDienst kalender,
    VertretungDienst vertretung,
    AufgabenDienst aufgaben,
    Abstimmungen.UmfrageDienst umfragen,
    BesetzungDienst besetzung,
    HinweisDienst hinweise,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public async Task<HeuteAnsicht> LadenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var heute = Ortszeit.Heute(uhr);
        var jetzt = Ortszeit.Jetzt(uhr);
        var zeilen = (await kalender.TermineAsync(heute, heute.AddDays(14), abbruch: abbruch)).Where(z => z.Termin.Ende > jetzt).ToList();
        var meine = zeilen.Where(z => z.Beteiligung is Beteiligung.Ordentlich or Beteiligung.Vertritt
                                      || z.MeinStatus == Teilnahmestatus.Zugesagt
                                      || (z.Beteiligung == Beteiligung.Keine && z.Termin.Art is Terminart.Fraktionssitzung))
            .Where(z => z.MeinStatus != Teilnahmestatus.Abgesagt && !z.Termin.Abgesagt)
            .Take(8).ToList();
        var offeneZusagen = zeilen.Where(z => z.WartetAufAntwort).Take(6).ToList();
        var steuert = Rechte.VertretungenSteuern(wer.Rolle).Erlaubt;
        var politik = wer.Rolle is Domain.Personen.Rolle.Helferin or null
            ? Freigabe.Nein("Nur für Mitglieder, sachkundige Bürger:innen, Vorstand und Geschäftsführung.")
            : Freigabe.Ja;
        return new HeuteAnsicht(
            wer.Name,
            meine,
            await vertretung.MeineAnfragenAsync(abbruch),
            offeneZusagen,
            await aufgaben.KorbAsync(abbruch),
            (await aufgaben.UebersichtAsync(abbruch)).Meine.Take(6).ToList(),
            await umfragen.OffenFuerMichAsync(abbruch),
            wer.Rolle is Domain.Personen.Rolle.Helferin ? [] : await besetzung.OffenFuerMichAsync(abbruch),
            await besetzung.BerichtOffenAsync(abbruch),
            steuert ? (await vertretung.OffeneVertretungenAsync(abbruch)).Count : 0,
            [
                new("Abwesenheit melden", "abwesenheit?neu=1", "event_busy", Rechte.AbwesenheitMelden(wer.Rolle)),
                new("Termin anlegen", "kalender?neu=1", "event", Rechte.TermineAnlegen(wer.Rolle)),
                new("Raum buchen", "ressourcen?neu=1", "meeting_room", Freigabe.Ja),
                new("Antrag starten", "antraege?neu=1", "description",
                    wer.Rolle is Domain.Personen.Rolle.Helferin ? Freigabe.Nein("Helfer:innen schreiben nicht an Anträgen mit.") : Freigabe.Ja),
                new("Fraktionssitzung einladen", "sitzungen", "groups", Rechte.SitzungenSteuern(wer.Rolle)),
                new("Anfrage schreiben", "anfragen", "help", politik),
                new("Bürgeranliegen erfassen", "anliegen", "support_agent", politik),
                new("Pressemitteilung", "presse", "campaign", politik),
                new("Wiki durchsuchen", "wiki", "menu_book", Freigabe.Ja),
                new("Datei ablegen", "dateien", "upload_file", Freigabe.Ja),
                new("Plakat erfassen", "handy/plakat", "add_location_alt", Freigabe.Ja),
                new("Auslage einreichen", "auslagen", "receipt_long", Freigabe.Ja),
            ],
            await hinweise.LadenAsync(abbruch));
    }
}
