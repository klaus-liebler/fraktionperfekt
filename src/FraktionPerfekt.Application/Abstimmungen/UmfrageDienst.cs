using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Application.Abstimmungen;

public sealed record UmfrageZeile(Umfrage Umfrage, int Teilnehmende, bool IchHabeAbgestimmt);

public sealed record UmfrageDetail(Umfrage Umfrage, IReadOnlyList<UmfrageOption> Optionen, IReadOnlyList<Guid> MeineStimmen, Umfrageergebnis Ergebnis,
    IReadOnlyList<string> Abgestimmt, Freigabe Abstimmen, Freigabe Abschliessen, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

public sealed record NeueUmfrage(string Titel, string? Beschreibung, Umfrageart Art, DateTimeOffset Frist, int QuorumProzent, bool Mehrfachauswahl,
    IReadOnlyList<string> Optionen, IReadOnlyList<DateTime>? Zeitpunkte = null);

/// <summary>
/// Abstimmungen, Meinungsbilder, Umfragen und Terminfindung (Modul 13). Stimmen sind bis zur Frist änderbar; danach (oder
/// vorher durch die Erstellerin/den Vorstand) wird abgeschlossen. Antragsabstimmungen setzen den Antragsstatus und legen
/// einen Fraktionsbeschluss für die Beschlusslage an.
/// </summary>
public sealed class UmfrageDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public async Task<IReadOnlyList<UmfrageZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var umfragen = await daten.ListeAsync(daten.Menge<Umfrage>().OrderByDescending(u => u.ErstelltAm).ThenBy(u => u.Id), abbruch);
        var stimmen = await daten.ListeAsync(daten.Menge<Stimme>().OrderBy(s => s.Id), abbruch);
        return umfragen.Select(u => new UmfrageZeile(u, stimmen.Where(s => s.UmfrageId == u.Id).Select(s => s.PersonId).Distinct().Count(),
            stimmen.Any(s => s.UmfrageId == u.Id && s.PersonId == wer.PersonId))).ToList();
    }

    /// <summary>Offene Abstimmungen, bei denen die angemeldete Person stimmberechtigt ist und noch nicht abgestimmt hat (Startseite).</summary>
    public async Task<IReadOnlyList<UmfrageZeile>> OffenFuerMichAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var jetzt = uhr.GetUtcNow();
        return (await ListeAsync(abbruch)).Where(z => z.Umfrage.Offen(jetzt) && !z.IchHabeAbgestimmt && Stimmberechtigt(z.Umfrage.Art, wer.Rollen)).ToList();
    }

    public async Task<UmfrageDetail?> DetailAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Umfrage>(id, abbruch) is not { } u)
        {
            return null;
        }

        var optionen = await OptionenAsync(id, abbruch);
        var stimmen = await StimmenAsync(id, abbruch);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var jetzt = uhr.GetUtcNow();
        var ergebnis = Auswertung.Auswerten(u, optionen, stimmen);
        var abstimmen = !u.Offen(jetzt) ? Freigabe.Nein(u.Status == Umfragestatus.Abgeschlossen ? "Die Abstimmung ist abgeschlossen." : "Die Frist ist abgelaufen.")
            : !Stimmberechtigt(u.Art, wer.Rollen) ? Freigabe.Nein(u.Art == Umfrageart.Antragsabstimmung
                ? "Über Anträge stimmen die Ratsmitglieder der Fraktion ab (Rolle Mitglied)."
                : "Helfer:innen nehmen an Fraktionsabstimmungen nicht teil.")
            : Freigabe.Ja;
        var abschliessen = u.Status == Umfragestatus.Abgeschlossen ? Freigabe.Nein("Bereits abgeschlossen.")
            : wer.PersonId == u.ErstelltVonPersonId || Rechte.SitzungenSteuern(wer.Rolle).Erlaubt ? Freigabe.Ja
            : Freigabe.Nein("Abschließen kann, wer die Abstimmung gestartet hat, oder der Vorstand.");
        return new UmfrageDetail(u, optionen, stimmen.Where(s => s.PersonId == wer.PersonId).Select(s => s.OptionId).ToList(), ergebnis,
            stimmen.Select(s => s.PersonId).Distinct().Select(p => namen.GetValueOrDefault(p, "?")).Order(StringComparer.CurrentCulture).ToList(),
            abstimmen, abschliessen, Folgeschritte(u, ergebnis));
    }

    public async Task<Umfrage> AnlegenAsync(NeueUmfrage neu, Guid? antragId = null, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            throw new InvalidOperationException("Helfer:innen starten keine Fraktionsabstimmungen.");
        }

        var berechtigt = await BerechtigteAsync(neu.Art, abbruch);
        var u = new Umfrage(neu.Titel, neu.Beschreibung, neu.Art, neu.Frist, neu.QuorumProzent, neu.Mehrfachauswahl, berechtigt.Count, wer.PersonIdPflicht,
            uhr.GetUtcNow(), antragId);
        daten.Hinzufuegen(u);
        var texte = neu.Art is Umfrageart.Antragsabstimmung or Umfrageart.Meinungsbild ? Umfrage.JaNeinEnthaltung : neu.Optionen;
        var position = 0;
        if (neu.Art == Umfrageart.Terminfindung)
        {
            foreach (var z in (neu.Zeitpunkte ?? []).Order())
            {
                daten.Hinzufuegen(new UmfrageOption(u.Id, ++position, z.ToString("ddd dd.MM.yyyy, HH:mm 'Uhr'", System.Globalization.CultureInfo.CurrentCulture), z));
            }
        }
        else
        {
            foreach (var t in texte.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                daten.Hinzufuegen(new UmfrageOption(u.Id, ++position, t));
            }
        }

        if (position < 2)
        {
            throw new InvalidOperationException("Bitte mindestens zwei Antwortmöglichkeiten angeben.");
        }

        foreach (var p in berechtigt.Where(p => p.Id != wer.PersonId))
        {
            await postausgang.PushEinreihenAsync(p, Art(neu.Art) + ": " + neu.Titel,
                $"Bitte bis {Ortszeit.AlsOrtszeit(neu.Frist):dd.MM. HH:mm} Uhr abstimmen.", $"abstimmungen/{u.Id}", "Abstimmen", abbruch);
        }

        await protokoll.ProtokollierenAsync("Umfrage.Gestartet", $"{Art(neu.Art)} gestartet ({berechtigt.Count} Berechtigte, Quorum {neu.QuorumProzent} %)",
            nameof(Umfrage), u.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return u;
    }

    /// <summary>Stimme abgeben oder ändern (ersetzt die bisherige Auswahl der Person).</summary>
    public async Task AbstimmenAsync(Guid umfrageId, IReadOnlyCollection<Guid> optionIds, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(optionIds);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var detail = await DetailAsync(umfrageId, abbruch) ?? throw new InvalidOperationException("Abstimmung nicht gefunden.");
        if (detail.Abstimmen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (optionIds.Count == 0 || (!detail.Umfrage.Mehrfachauswahl && optionIds.Count > 1) || optionIds.Any(o => detail.Optionen.All(x => x.Id != o)))
        {
            throw new InvalidOperationException(detail.Umfrage.Mehrfachauswahl ? "Bitte mindestens eine Antwort wählen." : "Bitte genau eine Antwort wählen.");
        }

        var personId = wer.PersonIdPflicht;
        foreach (var alt in await daten.ListeAsync(daten.Menge<Stimme>().Where(s => s.UmfrageId == umfrageId && s.PersonId == personId).OrderBy(s => s.Id), abbruch))
        {
            daten.Entfernen(alt);
        }

        foreach (var o in optionIds.Distinct())
        {
            daten.Hinzufuegen(new Stimme(umfrageId, o, personId, uhr.GetUtcNow()));
        }

        // Geheim: im Protokoll steht nur, dass abgestimmt wurde, nicht wie.
        await protokoll.ProtokollierenAsync("Umfrage.Abgestimmt", $"{Pseudonym.Person(personId)} hat abgestimmt", nameof(Umfrage), umfrageId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<Umfrageergebnis> AbschliessenAsync(Guid umfrageId, CancellationToken abbruch = default)
    {
        var detail = await DetailAsync(umfrageId, abbruch) ?? throw new InvalidOperationException("Abstimmung nicht gefunden.");
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && detail.Abschliessen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var u = await daten.LadenAsync<Umfrage>(umfrageId, "Abstimmung", abbruch);
        var ergebnis = detail.Ergebnis;
        u.Abschliessen(ergebnis, uhr.GetUtcNow());
        if (u.AntragId is { } antragId && await daten.FindenAsync<Antrag>(antragId, abbruch) is { Status: Antragsstatus.InAbstimmung } antrag)
        {
            var angenommen = ergebnis.Angenommen == true;
            antrag.Wechseln(angenommen ? Antragsstatus.Beschlossen : Antragsstatus.Abgelehnt, uhr.GetUtcNow(), ergebnis.Text);
            int Zahl(string t) => ergebnis.Optionen.FirstOrDefault(o => o.Text == t)?.Stimmen ?? 0;
            daten.Hinzufuegen(new Beschluss(Beschlussquelle.Fraktion, Ortszeit.Heute(uhr), antrag.Titel, $"Fraktionsabstimmung über den Antrag: {ergebnis.Text}",
                angenommen ? Beschlussergebnis.Angenommen : Beschlussergebnis.Abgelehnt, Zahl("Ja"), Zahl("Nein"), Zahl("Enthaltung"), antragId: antragId));
            if (await stammdaten.PersonAsync(antrag.AutorPersonId, abbruch) is { } autor)
            {
                await postausgang.PushEinreihenAsync(autor, $"Antrag {(angenommen ? "beschlossen" : "abgelehnt")}", antrag.Titel, $"antraege/{antrag.Id}", abbruch: abbruch);
            }
        }

        await protokoll.ProtokollierenAsync("Umfrage.Abgeschlossen", $"Abgeschlossen: {ergebnis.Text}", nameof(Umfrage), u.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return ergebnis;
    }

    /// <summary>Fristenprüfung: abgelaufene Abstimmungen automatisch abschließen.</summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        var faellig = (await daten.ListeAsync(daten.Menge<Umfrage>().Where(u => u.Status == Umfragestatus.Offen).OrderBy(u => u.Id), abbruch))
            .Where(u => u.Frist <= jetzt).ToList();
        foreach (var u in faellig)
        {
            await AbschliessenAsync(u.Id, abbruch);
        }

        return faellig.Count;
    }

    public static bool Stimmberechtigt(Umfrageart art, IReadOnlyList<Rolle> rollen)
    {
        ArgumentNullException.ThrowIfNull(rollen);
        return art == Umfrageart.Antragsabstimmung ? rollen.Contains(Rolle.Mitglied) : rollen.Any(r => r != Rolle.Helferin);
    }

    private async Task<IReadOnlyList<Person>> BerechtigteAsync(Umfrageart art, CancellationToken abbruch)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        return (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && Stimmberechtigt(art, p.RollenAm(heute, jetzt))).ToList();
    }

    private static List<NaechsterSchritt> Folgeschritte(Umfrage u, Umfrageergebnis e)
    {
        var schritte = new List<NaechsterSchritt>();
        if (u.Status != Umfragestatus.Abgeschlossen)
        {
            return schritte;
        }

        if (u.Art == Umfrageart.Terminfindung && e.Gewinner?.Zeitpunkt is { } z)
        {
            schritte.Add(new NaechsterSchritt($"Termin am {z:dd.MM. HH:mm} anlegen", $"kalender?neu=1&beginn={z:yyyy-MM-ddTHH:mm}&titel={Uri.EscapeDataString(u.Titel)}",
                Hauptschritt: true));
        }

        if (u.AntragId is { } a)
        {
            schritte.Add(new NaechsterSchritt("Zum Antrag", $"antraege/{a}", Hauptschritt: e.Angenommen == true));
        }

        return schritte;
    }

    private static string Art(Umfrageart art) => art switch
    {
        Umfrageart.Antragsabstimmung => "Abstimmung über Antrag",
        Umfrageart.Meinungsbild => "Meinungsbild",
        Umfrageart.Terminfindung => "Terminfindung",
        _ => "Umfrage",
    };

    private Task<List<UmfrageOption>> OptionenAsync(Guid id, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<UmfrageOption>().Where(o => o.UmfrageId == id).OrderBy(o => o.Position).ThenBy(o => o.Id), abbruch);

    private Task<List<Stimme>> StimmenAsync(Guid id, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Stimme>().Where(s => s.UmfrageId == id).OrderBy(s => s.Id), abbruch);
}
