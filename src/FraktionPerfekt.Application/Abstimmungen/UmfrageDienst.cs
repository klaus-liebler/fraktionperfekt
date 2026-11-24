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
using Microsoft.Extensions.Options;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Application.Abstimmungen;

public sealed record UmfrageZeile(Umfrage Umfrage, int Teilnehmende, bool IchHabeAbgestimmt);

/// <param name="Ergebnis">Ergebnis bzw. Zwischenstand; <c>null</c> bei einer geheimen Abstimmung, solange sie läuft (kein Zwischenstand).</param>
/// <param name="MeineStimmen">Eigene Auswahl bei namentlichen Abstimmungen; bei geheimen immer leer (die Zuordnung gibt es nicht).</param>
/// <param name="Teilnehmende">Wie viele schon abgestimmt haben.</param>
public sealed record UmfrageDetail(Umfrage Umfrage, IReadOnlyList<UmfrageOption> Optionen, IReadOnlyList<Guid> MeineStimmen, Umfrageergebnis? Ergebnis,
    IReadOnlyList<string> Abgestimmt, Freigabe Abstimmen, Freigabe Abschliessen, IReadOnlyList<NaechsterSchritt> NaechsteSchritte, int Teilnehmende = 0,
    bool IchHabeAbgestimmt = false);

/// <param name="Kandidaten">Bei Wahlgängen: Person je Antwort (gleiche Reihenfolge wie <paramref name="Optionen"/>; Nein/Enthaltung ohne Person).</param>
/// <param name="NurPersonen">Nur diese Personen sind stimmberechtigt (z. B. die Anwesenden einer Sitzung), sofern sie nach Rolle berechtigt sind.</param>
public sealed record NeueUmfrage(string Titel, string? Beschreibung, Umfrageart Art, DateTimeOffset Frist, int QuorumProzent, bool Mehrfachauswahl,
    IReadOnlyList<string> Optionen, IReadOnlyList<DateTime>? Zeitpunkte = null, bool Geheim = false, IReadOnlyList<Guid>? NurPersonen = null,
    IReadOnlyList<Guid?>? Kandidaten = null, Guid? WahlId = null, Guid? TopId = null);

/// <summary>
/// Abstimmungen, Meinungsbilder, Umfragen und Terminfindung (Modul 13). Die Stimmberechtigten werden beim Start als feste Liste gespeichert.
/// Namentlich: Stimmen sind bis zur Frist änderbar, der Zwischenstand ist sichtbar. Geheim: Stimmvermerk (wer) und Stimmzettel (wie) getrennt
/// ohne Verbindung, kein Zwischenstand, die Stimme ist endgültig, Ende automatisch, sobald alle abgestimmt haben. Abgeschlossen wird zur Frist,
/// vorher durch die Erstellerin/den Ersteller oder den Vorstand. Antragsabstimmungen setzen den Antragsstatus und legen einen Fraktionsbeschluss an.
/// </summary>
public sealed class UmfrageDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<GeschaeftsordnungOptionen> go,
    TimeProvider uhr)
{
    public async Task<IReadOnlyList<UmfrageZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return []; // Wie im Menü: Das Kampagnenteam sieht nur die Kampagnen (auch nicht über die direkte Adresse).
        }
        var umfragen = await daten.ListeAsync(daten.Menge<Umfrage>().OrderByDescending(u => u.ErstelltAm).ThenBy(u => u.Id), abbruch);
        var stimmen = await daten.ListeAsync(daten.Menge<Stimme>().OrderBy(s => s.Id).Select(s => new Teilnahme(s.UmfrageId, s.PersonId)), abbruch);
        var vermerke = await daten.ListeAsync(daten.Menge<Stimmvermerk>().OrderBy(v => v.Id).Select(v => new Teilnahme(v.UmfrageId, v.PersonId)), abbruch);
        var teilnahmen = stimmen.Concat(vermerke).Distinct().ToLookup(t => t.UmfrageId, t => t.PersonId);
        return umfragen.Select(u => new UmfrageZeile(u, teilnahmen[u.Id].Count(), wer.PersonId is { } ich && teilnahmen[u.Id].Contains(ich))).ToList();
    }

    /// <summary>Offene Abstimmungen, bei denen die angemeldete Person stimmberechtigt ist und noch nicht abgestimmt hat (Startseite).</summary>
    public async Task<IReadOnlyList<UmfrageZeile>> OffenFuerMichAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var jetzt = uhr.GetUtcNow();
        var offen = (await ListeAsync(abbruch)).Where(z => z.Umfrage.Offen(jetzt) && !z.IchHabeAbgestimmt).ToList();
        if (offen.Count == 0)
        {
            return offen;
        }

        var ids = offen.Select(z => z.Umfrage.Id).ToList();
        var listen = (await daten.ListeAsync(daten.Menge<Stimmberechtigung>().Where(b => ids.Contains(b.UmfrageId)).OrderBy(b => b.Id), abbruch))
            .ToLookup(b => b.UmfrageId, b => b.PersonId);
        return offen.Where(z => Berechtigt(z.Umfrage, listen[z.Umfrage.Id].ToList(), wer)).ToList();
    }

    public async Task<UmfrageDetail?> DetailAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return null; // Wie im Menü: Das Kampagnenteam sieht nur die Kampagnen (auch nicht über die direkte Adresse).
        }
        if (await daten.FindenAsync<Umfrage>(id, abbruch) is not { } u)
        {
            return null;
        }

        var optionen = await OptionenAsync(id, abbruch);
        var abgestimmt = await AbgestimmtAsync(u, abbruch);
        var liste = await BerechtigtenlisteAsync(id, abbruch);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var jetzt = uhr.GetUtcNow();
        var abgeschlossen = u.Status == Umfragestatus.Abgeschlossen;
        // Geheim: kein Zwischenstand – die Stimmzettel werden erst nach dem Ende gezählt.
        var ergebnis = u.Geheim && !abgeschlossen ? null : await ErgebnisAsync(u, optionen, abbruch);
        var ich = wer.PersonId is { } p && abgestimmt.Contains(p);
        var abstimmen = !u.Offen(jetzt) ? Freigabe.Nein(abgeschlossen ? "Die Abstimmung ist abgeschlossen." : "Die Frist ist abgelaufen.")
            : !Berechtigt(u, liste, wer) ? Freigabe.Nein(Nichtberechtigt(u, liste, wer))
            : u.Geheim && ich ? Freigabe.Nein("Die Stimme ist abgegeben. In einer geheimen Abstimmung ist sie endgültig.")
            : Freigabe.Ja;
        var abschliessen = abgeschlossen ? Freigabe.Nein("Bereits abgeschlossen.")
            : wer.PersonId == u.ErstelltVonPersonId || Rechte.SitzungenSteuern(wer.Rolle).Erlaubt ? Freigabe.Ja
            : Freigabe.Nein("Abschließen kann, wer die Abstimmung gestartet hat, oder der Vorstand.");
        IReadOnlyList<Guid> meine = u.Geheim || wer.PersonId is not { } ichId
            ? []
            : (await StimmenAsync(id, abbruch)).Where(s => s.PersonId == ichId).Select(s => s.OptionId).ToList();
        return new UmfrageDetail(u, optionen, meine, ergebnis,
            abgestimmt.Select(x => namen.GetValueOrDefault(x, "?")).Order(StringComparer.CurrentCulture).ToList(),
            abstimmen, abschliessen, ergebnis is null ? [] : Folgeschritte(u, ergebnis), abgestimmt.Count, ich);
    }

    public async Task<Umfrage> AnlegenAsync(NeueUmfrage neu, Guid? antragId = null, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            throw new InvalidOperationException("Das Kampagnenteam startet keine Fraktionsabstimmungen.");
        }

        if (neu.Geheim && neu.Art == Umfrageart.Terminfindung)
        {
            throw new InvalidOperationException("Eine Terminfindung kann nicht geheim sein.");
        }

        var berechtigt = (await BerechtigteAsync(neu.Art, abbruch)).Where(p => neu.NurPersonen is null || neu.NurPersonen.Contains(p.Id)).ToList();
        if (berechtigt.Count == 0)
        {
            throw new InvalidOperationException("Niemand wäre bei dieser Abstimmung stimmberechtigt.");
        }

        var u = new Umfrage(neu.Titel, neu.Beschreibung, neu.Art, neu.Frist, neu.QuorumProzent, neu.Mehrfachauswahl, berechtigt.Count, wer.PersonIdPflicht,
            uhr.GetUtcNow(), antragId, neu.Geheim, neu.WahlId, neu.TopId);
        daten.Hinzufuegen(u);
        foreach (var p in berechtigt)
        {
            daten.Hinzufuegen(new Stimmberechtigung(u.Id, p.Id));
        }

        IReadOnlyList<string> texte = neu.Art is Umfrageart.Antragsabstimmung or Umfrageart.Meinungsbild ? Umfrage.JaNeinEnthaltung : neu.Optionen;
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
            for (var i = 0; i < texte.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(texte[i]))
                {
                    daten.Hinzufuegen(new UmfrageOption(u.Id, ++position, texte[i], kandidatPersonId: neu.Kandidaten is { } k && i < k.Count ? k[i] : null));
                }
            }
        }

        if (position < 2)
        {
            throw new InvalidOperationException("Bitte mindestens zwei Antwortmöglichkeiten angeben.");
        }

        var geheim = neu.Geheim ? "Geheime " : "";
        foreach (var p in berechtigt.Where(p => p.Id != wer.PersonId))
        {
            await postausgang.PushEinreihenAsync(p, geheim + Art(neu.Art) + ": " + neu.Titel,
                $"Bitte bis {Ortszeit.AlsOrtszeit(neu.Frist):dd.MM. HH:mm} Uhr abstimmen.", $"abstimmungen/{u.Id}", "Abstimmen", abbruch);
        }

        await protokoll.ProtokollierenAsync("Umfrage.Gestartet", $"{geheim}{Art(neu.Art)} gestartet ({berechtigt.Count} Berechtigte, Quorum {neu.QuorumProzent} %)",
            nameof(Umfrage), u.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return u;
    }

    /// <summary>
    /// Stimme abgeben. Namentlich: ersetzt die bisherige Auswahl der Person. Geheim: Stimmvermerk und Stimmzettel in einem Speichervorgang
    /// (eine Transaktion), danach nicht mehr änderbar; haben alle Stimmberechtigten abgestimmt, endet die Abstimmung sofort.
    /// </summary>
    public async Task AbstimmenAsync(Guid umfrageId, IReadOnlyCollection<Guid> optionIds, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(optionIds);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            throw new InvalidOperationException("Das Kampagnenteam nimmt an Fraktionsabstimmungen nicht teil.");
        }

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
        if (detail.Umfrage.Geheim)
        {
            // Wer (Vermerk) und wie (Stimmzettel mit zufälliger Id, ohne Person und Zeit) – in einem Speichervorgang, aber ohne Verbindung.
            daten.Hinzufuegen(new Stimmvermerk(umfrageId, personId, uhr.GetUtcNow()));
            foreach (var o in optionIds.Distinct())
            {
                daten.Hinzufuegen(new Stimmzettel(umfrageId, o));
            }
        }
        else
        {
            foreach (var alt in await daten.ListeAsync(daten.Menge<Stimme>().Where(s => s.UmfrageId == umfrageId && s.PersonId == personId).OrderBy(s => s.Id), abbruch))
            {
                daten.Entfernen(alt);
            }

            foreach (var o in optionIds.Distinct())
            {
                daten.Hinzufuegen(new Stimme(umfrageId, o, personId, uhr.GetUtcNow()));
            }
        }

        // Protokoll, Logs und Push nennen nie die gewählte Option – nur, dass abgestimmt wurde.
        await protokoll.ProtokollierenAsync("Umfrage.Abgestimmt", $"{Pseudonym.Person(personId)} hat abgestimmt", nameof(Umfrage), umfrageId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);

        if (detail.Umfrage.Geheim && detail.Teilnehmende + 1 >= detail.Umfrage.Berechtigte)
        {
            await AbschliessenInternAsync(umfrageId, "alle Stimmberechtigten haben abgestimmt", abbruch);
        }
    }

    public async Task<Umfrageergebnis> AbschliessenAsync(Guid umfrageId, CancellationToken abbruch = default)
    {
        var detail = await DetailAsync(umfrageId, abbruch) ?? throw new InvalidOperationException("Abstimmung nicht gefunden.");
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && detail.Abschliessen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        return await AbschliessenInternAsync(umfrageId, null, abbruch);
    }

    /// <summary>Fristenprüfung: abgelaufene Abstimmungen automatisch abschließen.</summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var jetzt = uhr.GetUtcNow();
        var faellig = (await daten.ListeAsync(daten.Menge<Umfrage>().Where(u => u.Status == Umfragestatus.Offen).OrderBy(u => u.Id), abbruch))
            .Where(u => u.Frist <= jetzt).ToList();
        foreach (var u in faellig)
        {
            await AbschliessenInternAsync(u.Id, "Frist abgelaufen", abbruch);
        }

        return faellig.Count;
    }

    /// <summary>
    /// Stimmberechtigt nach Rolle: Antragsabstimmungen die Ratsmitglieder (Rolle Mitglied), sonst alle außer dem Kampagnenteam; sachkundige
    /// Bürger:innen nur bei den Arten, die die Geschäftsordnung vorsieht (⚑ Einstellung <see cref="GeschaeftsordnungOptionen.SachkundigeStimmenMit"/>).
    /// </summary>
    public static bool Stimmberechtigt(Umfrageart art, IReadOnlyList<Rolle> rollen, GeschaeftsordnungOptionen? regeln = null)
    {
        ArgumentNullException.ThrowIfNull(rollen);
        var sachkundigeMit = (regeln ?? new GeschaeftsordnungOptionen()).SachkundigeStimmenMit.Contains(art);
        return art is Umfrageart.Antragsabstimmung or Umfrageart.Wahl
            ? rollen.Contains(Rolle.Mitglied) || (sachkundigeMit && rollen.Contains(Rolle.SachkundigeBuergerin))
            : rollen.Any(r => r is not Rolle.Helferin && (r is not Rolle.SachkundigeBuergerin || sachkundigeMit));
    }

    private async Task<Umfrageergebnis> AbschliessenInternAsync(Guid umfrageId, string? anlass, CancellationToken abbruch)
    {
        var u = await daten.LadenAsync<Umfrage>(umfrageId, "Abstimmung", abbruch);
        if (u.Status == Umfragestatus.Abgeschlossen)
        {
            throw new InvalidOperationException("Die Abstimmung ist bereits abgeschlossen.");
        }

        var ergebnis = await ErgebnisAsync(u, await OptionenAsync(umfrageId, abbruch), abbruch);
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

        await protokoll.ProtokollierenAsync("Umfrage.Abgeschlossen", $"Abgeschlossen{(anlass is null ? "" : $" ({anlass})")}: {ergebnis.Text}", nameof(Umfrage), u.Id,
            abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return ergebnis;
    }

    /// <summary>Berechtigt ist, wer auf der beim Start gespeicherten Liste steht; ältere Abstimmungen ohne Liste prüfen die Rolle.</summary>
    private bool Berechtigt(Umfrage u, IReadOnlyList<Guid> liste, Benutzer wer)
        => wer.Rolle is not Rolle.Helferin
           && (liste.Count > 0 ? wer.PersonId is { } p && liste.Contains(p) : Stimmberechtigt(u.Art, wer.Rollen, go.Value));

    private string Nichtberechtigt(Umfrage u, IReadOnlyList<Guid> liste, Benutzer wer)
        => wer.Rolle is Rolle.Helferin ? "Das Kampagnenteam nimmt an Fraktionsabstimmungen nicht teil."
            : liste.Count > 0 && Stimmberechtigt(u.Art, wer.Rollen, go.Value) ? "Stimmberechtigt sind nur die Personen, die beim Start der Abstimmung festgelegt wurden."
            : u.Art == Umfrageart.Antragsabstimmung ? "Über Anträge stimmen die Ratsmitglieder der Fraktion ab (Rolle Mitglied)."
            : u.Art == Umfrageart.Wahl ? "Wählen dürfen die Ratsmitglieder der Fraktion (Rolle Mitglied)."
            : "Bei dieser Abstimmungsart stimmen sachkundige Bürger:innen laut Geschäftsordnung nicht mit.";

    private async Task<IReadOnlyList<Person>> BerechtigteAsync(Umfrageart art, CancellationToken abbruch)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        return (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && Stimmberechtigt(art, p.RollenAm(heute, jetzt), go.Value)).ToList();
    }

    private Task<List<Stimmvermerk>> VermerkeAsync(Guid id, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Stimmvermerk>().Where(v => v.UmfrageId == id).OrderBy(v => v.Id), abbruch);

    /// <summary>Feste Liste der Stimmberechtigten (leer bei Abstimmungen, die vor der Einführung der Liste gestartet wurden).</summary>
    private async Task<IReadOnlyList<Guid>> BerechtigtenlisteAsync(Guid id, CancellationToken abbruch)
        => (await daten.ListeAsync(daten.Menge<Stimmberechtigung>().Where(b => b.UmfrageId == id).OrderBy(b => b.Id), abbruch)).Select(b => b.PersonId).ToList();

    /// <summary>Personen, die abgestimmt haben: bei geheimen Abstimmungen aus den Stimmvermerken, sonst aus den Stimmen.</summary>
    private async Task<IReadOnlyList<Guid>> AbgestimmtAsync(Umfrage u, CancellationToken abbruch)
        => u.Geheim
            ? (await VermerkeAsync(u.Id, abbruch)).Select(v => v.PersonId).ToList()
            : (await StimmenAsync(u.Id, abbruch)).Select(s => s.PersonId).Distinct().ToList();

    /// <summary>
    /// Vollständiges Ergebnis. Bei geheimen Abstimmungen werden nur hier die Stimmzettel gezählt – ohne Bezug zu Personen; aufgerufen nur für
    /// abgeschlossene Abstimmungen und beim Abschließen.
    /// </summary>
    private async Task<Umfrageergebnis> ErgebnisAsync(Umfrage u, IReadOnlyList<UmfrageOption> optionen, CancellationToken abbruch)
    {
        if (!u.Geheim)
        {
            return Auswertung.Auswerten(u, optionen, await StimmenAsync(u.Id, abbruch));
        }

        var teilnehmende = await daten.AnzahlAsync(daten.Menge<Stimmvermerk>().Where(v => v.UmfrageId == u.Id), abbruch);
        var zettel = await daten.ListeAsync(daten.Menge<Stimmzettel>().Where(z => z.UmfrageId == u.Id).OrderBy(z => z.Id).Select(z => z.OptionId), abbruch);
        return Auswertung.Auswerten(u, optionen, teilnehmende, zettel.GroupBy(o => o).ToDictionary(g => g.Key, g => g.Count()), go.Value.GeheimMindeststimmen);
    }

    private sealed record Teilnahme(Guid UmfrageId, Guid PersonId);

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

        if (u.WahlId is { } w)
        {
            schritte.Add(new NaechsterSchritt("Zur Wahl (Ergebnis und Annahme)", $"wahlen/{w}", Hauptschritt: true));
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
        Umfrageart.Wahl => "Wahlgang",
        _ => "Umfrage",
    };

    private Task<List<UmfrageOption>> OptionenAsync(Guid id, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<UmfrageOption>().Where(o => o.UmfrageId == id).OrderBy(o => o.Position).ThenBy(o => o.Id), abbruch);

    private Task<List<Stimme>> StimmenAsync(Guid id, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Stimme>().Where(s => s.UmfrageId == id).OrderBy(s => s.Id), abbruch);
}
