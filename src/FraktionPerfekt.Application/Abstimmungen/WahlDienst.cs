using System.Globalization;
using System.Text;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Abstimmungen;

public sealed record WahlZeile(Wahl Wahl, int Kandidaturen, string? Gewaehlt);

public sealed record KandidaturZeile(Kandidatur Kandidatur, string Name, Freigabe Zurueckziehen);

public sealed record WahlgangZeile(Umfrage Umfrage, int Nummer, bool Stichwahl, int Teilnehmende);

public sealed record AmtZeile(Amtszeit Amtszeit, string Person, string Amt);

public sealed record WahlDetail(
    Wahl Wahl,
    string? Gremium,
    IReadOnlyList<KandidaturZeile> Kandidaturen,
    IReadOnlyList<WahlgangZeile> Wahlgaenge,
    IReadOnlyList<(Guid Id, string Name)> Gleichstand,
    Freigabe KandidaturHinzufuegen,
    Freigabe Kandidieren,
    Freigabe WahlgangStarten,
    Freigabe LosErfassen,
    Freigabe Annehmen,
    string Wahlprotokoll,
    IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

/// <summary>
/// Wahlen und Ämter mit Amtszeit (aus SchoolCraft Modul 33): Wahl anlegen (Vorstand), Kandidaturen (Vorschlag durch den Vorstand oder selbst),
/// Wahlgänge als Abstimmung der Art <see cref="Umfrageart.Wahl"/> (geheim nach Nr. 1 oder offen), Mehrheit und Stichwahl nach den Regeln der
/// Geschäftsordnung (⚑ Einstellung), Los bei Gleichstand, Annahme der Wahl durch die gewählte Person. Die Annahme setzt Rolle und Funktion
/// bitemporal ab Beginn der Amtszeit (mit der Wahl als Grund) und beendet die Amtszeit der bisherigen Person; vor Ablauf einer Amtszeit legt die
/// Fristenprüfung die Aufgabe „Neuwahl vorbereiten“ an. Wahlberechtigt ist, wer bei Wahlgängen stimmberechtigt ist (abgeleitet, nicht gepflegt).
/// </summary>
public sealed class WahlDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    UmfrageDienst umfragen,
    AufgabenDienst aufgaben,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<GeschaeftsordnungOptionen> go,
    TimeProvider uhr)
{
    public const string Anlass = "Wahl";

    public async Task<IReadOnlyList<WahlZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin or null)
        {
            return [];
        }

        var namen = await NamenAsync(abbruch);
        var wahlen = await daten.ListeAsync(daten.Menge<Wahl>().OrderByDescending(w => w.ErstelltAm).ThenBy(w => w.Id), abbruch);
        var kandidaturen = await daten.ListeAsync(daten.Menge<Kandidatur>().Where(k => k.ZurueckgezogenAm == null).OrderBy(k => k.Id), abbruch);
        return wahlen.Select(w => new WahlZeile(w, kandidaturen.Count(k => k.WahlId == w.Id), w.GewaehltPersonId is { } g ? namen.GetValueOrDefault(g) : null)).ToList();
    }

    /// <summary>Ämter, die am Stichtag gelten (Übersicht „Wer hat welches Amt?“).</summary>
    public async Task<IReadOnlyList<AmtZeile>> AemterAsync(DateOnly? stichtag = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin or null)
        {
            return [];
        }

        var tag = stichtag ?? Ortszeit.Heute(uhr);
        var namen = await NamenAsync(abbruch);
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id, g => g.Kurzname ?? g.Name);
        return (await daten.ListeAsync(daten.Menge<Amtszeit>().OrderBy(a => a.Amt).ThenBy(a => a.Von).ThenBy(a => a.Id), abbruch))
            .Where(a => a.GiltAm(tag))
            .Select(a => new AmtZeile(a, namen.GetValueOrDefault(a.PersonId, "?"), a.Amt.Anzeigename()
                + (a.GremiumId is { } g ? $" {gremien.GetValueOrDefault(g)}" : "") + (a.Bezeichnung is { } b ? $": {b}" : "")))
            .ToList();
    }

    public async Task<WahlDetail?> DetailAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin or null || await daten.FindenAsync<Wahl>(id, abbruch) is not { } w)
        {
            return null;
        }

        await FortschreibenAsync(w, abbruch);
        var namen = await NamenAsync(abbruch);
        var gremium = w.GremiumId is { } gid ? (await stammdaten.GremiumAsync(gid, abbruch))?.Name : null;
        var steuert = Rechte.SitzungenSteuern(wer.Rolle);
        var vorbereitung = w.Status is Wahlstatus.Vorbereitung or Wahlstatus.OhneErgebnis;
        var kandidaturen = await KandidaturenAsync(w.Id, abbruch);
        var gaenge = await WahlgaengeAsync(w.Id, abbruch);
        var gleichstand = w.Status == Wahlstatus.Los && gaenge.LastOrDefault() is { } letzter
            ? (await AuswertenAsync(w, letzter, abbruch)).Gleichstand.Select(p => (p, namen.GetValueOrDefault(p, "?"))).ToList()
            : [];
        var ichKandidiere = kandidaturen.Any(k => k.PersonId == wer.PersonId);
        var gewaehlt = w.GewaehltPersonId == wer.PersonId && wer.PersonId is not null;
        var zeilen = kandidaturen.Select(k => new KandidaturZeile(k, namen.GetValueOrDefault(k.PersonId, "?"),
            !vorbereitung ? Freigabe.Nein("Während eines Wahlgangs und nach der Wahl lassen sich Kandidaturen nicht zurückziehen.")
            : k.PersonId == wer.PersonId || steuert.Erlaubt ? Freigabe.Ja
            : Freigabe.Nein("Zurückziehen kann die kandidierende Person oder der Vorstand."))).ToList();
        var detail = new WahlDetail(w, gremium, zeilen,
            gaenge.Select((u, i) => new WahlgangZeile(u, i + 1, IstStichwahl(u), 0)).ToList(),
            gleichstand,
            !vorbereitung ? Freigabe.Nein("Kandidaturen gibt es vor dem ersten Wahlgang bzw. nach einer Wahl ohne Ergebnis.") : steuert,
            !vorbereitung ? Freigabe.Nein("Kandidaturen gibt es vor dem ersten Wahlgang bzw. nach einer Wahl ohne Ergebnis.")
                : ichKandidiere ? Freigabe.Nein("Die Kandidatur ist schon eingetragen.")
                : !UmfrageDienst.Stimmberechtigt(Umfrageart.Wahl, wer.Rollen, go.Value) ? Freigabe.Nein("Kandidieren können die Ratsmitglieder der Fraktion (Rolle Mitglied).")
                : Freigabe.Ja,
            steuert.Und(() => w.Status switch
            {
                Wahlstatus.Wahlgang => Freigabe.Nein("Ein Wahlgang läuft schon."),
                Wahlstatus.Stichwahl => Freigabe.Ja,
                Wahlstatus.Vorbereitung or Wahlstatus.OhneErgebnis when kandidaturen.Count == 0 => Freigabe.Nein("Erst braucht es mindestens eine Kandidatur."),
                Wahlstatus.Vorbereitung or Wahlstatus.OhneErgebnis => Freigabe.Ja,
                _ => Freigabe.Nein("Die Wahl ist entschieden."),
            }),
            w.Status != Wahlstatus.Los ? Freigabe.Nein("Das Los entscheidet nur bei Gleichstand in der Stichwahl.") : steuert,
            w.Status != Wahlstatus.Gewaehlt ? Freigabe.Nein("Annehmen lässt sich eine Wahl, sobald jemand gewählt ist.")
                : gewaehlt || steuert.Erlaubt ? Freigabe.Ja
                : Freigabe.Nein("Die Wahl annehmen (oder ablehnen) kann die gewählte Person oder für sie der Vorstand."),
            await WahlprotokollAsync(w, gremium, kandidaturen, gaenge, namen, abbruch),
            w.Status == Wahlstatus.Angenommen ? [new NaechsterSchritt("Zur Person", $"personen/{w.GewaehltPersonId}", Hauptschritt: true)]
                : gaenge.LastOrDefault() is { Status: Umfragestatus.Offen } offen ? [new NaechsterSchritt("Zum laufenden Wahlgang", $"abstimmungen/{offen.Id}", Hauptschritt: true)]
                : []);
        return detail;
    }

    public async Task<Wahl> AnlegenAsync(Fraktionsamt amt, string? bezeichnung, Guid? gremiumId, DateOnly amtAb, DateOnly? amtBis, bool? geheim = null,
        CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var w = new Wahl(amt, bezeichnung, gremiumId, amtAb, amtBis ?? go.Value.WahlperiodeEnde, geheim ?? go.Value.WahlGeheimStandard, wer.PersonIdPflicht,
            uhr.GetUtcNow());
        daten.Hinzufuegen(w);
        await protokoll.ProtokollierenAsync("Wahl.Angelegt", $"Wahl „{w.Titel}“ angelegt (Amtszeit ab {amtAb:dd.MM.yyyy})", nameof(Wahl), w.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return w;
    }

    /// <summary>Kandidatur eintragen: selbst oder durch den Vorstand für eine wahlberechtigte Person.</summary>
    public async Task KandidaturAsync(Guid wahlId, Guid personId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await DetailAsync(wahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        var recht = personId == wer.PersonId ? d.Kandidieren : d.KandidaturHinzufuegen;
        if (recht is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var person = await stammdaten.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        if (!UmfrageDienst.Stimmberechtigt(Umfrageart.Wahl, person.RollenAm(Ortszeit.Heute(uhr), uhr.GetUtcNow()), go.Value) || !person.Aktiv)
        {
            throw new InvalidOperationException($"{person.Anzeigename} ist nicht wählbar (wählbar sind die Ratsmitglieder der Fraktion).");
        }

        if (d.Kandidaturen.Any(k => k.Kandidatur.PersonId == personId))
        {
            throw new InvalidOperationException($"{person.Anzeigename} kandidiert bereits.");
        }

        daten.Hinzufuegen(new Kandidatur(wahlId, personId, wer.PersonIdPflicht, uhr.GetUtcNow()));
        await protokoll.ProtokollierenAsync("Wahl.Kandidatur", $"Kandidatur von {Pseudonym.Person(personId)}", nameof(Wahl), wahlId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task KandidaturZurueckziehenAsync(Guid kandidaturId, CancellationToken abbruch = default)
    {
        var k = await daten.LadenAsync<Kandidatur>(kandidaturId, "Kandidatur", abbruch);
        var d = await DetailAsync(k.WahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        if (d.Kandidaturen.FirstOrDefault(z => z.Kandidatur.Id == kandidaturId)?.Zurueckziehen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        k.Zurueckziehen(uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Wahl.KandidaturZurueckgezogen", $"Kandidatur von {Pseudonym.Person(k.PersonId)} zurückgezogen", nameof(Wahl), k.WahlId,
            abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Wahlgang starten (Vorstand): alle Kandidaturen bzw. bei der Stichwahl die Bestplatzierten; bei einer Kandidatur mit Nein.</summary>
    /// <param name="nurPersonen">Nur diese Personen stimmen ab (z. B. die Anwesenden der Sitzung).</param>
    public async Task<Umfrage> WahlgangStartenAsync(Guid wahlId, DateTimeOffset frist, IReadOnlyList<Guid>? nurPersonen = null, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(wahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        if (d.WahlgangStarten is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var w = d.Wahl;
        var stichwahl = w.Status == Wahlstatus.Stichwahl;
        var kandidaten = stichwahl
            ? (await AuswertenAsync(w, d.Wahlgaenge[^1].Umfrage, abbruch)).Stichwahl
            : d.Kandidaturen.Select(k => k.Kandidatur.PersonId).ToList();
        var namen = await NamenAsync(abbruch);
        var texte = kandidaten.Select(k => namen.GetValueOrDefault(k, "?")).ToList();
        var personen = kandidaten.Select(k => (Guid?)k).ToList();
        if (kandidaten.Count == 1)
        {
            texte.Add(Mehrheit.Nein);
            personen.Add(null);
        }

        texte.Add(Mehrheit.Enthaltung);
        personen.Add(null);
        var nummer = d.Wahlgaenge.Count + 1;
        var u = await umfragen.AnlegenAsync(new NeueUmfrage($"{w.Titel} – {(stichwahl ? "Stichwahl" : $"{nummer}. Wahlgang")}", stichwahl ? StichwahlText : null,
            Umfrageart.Wahl, frist, 0, false, texte, Geheim: w.Geheim, NurPersonen: nurPersonen, Kandidaten: personen, WahlId: w.Id), abbruch: abbruch);
        (await daten.LadenAsync<Wahl>(wahlId, "Wahl", abbruch)).WahlgangGestartet();
        await protokoll.ProtokollierenAsync("Wahl.Wahlgang", $"{(stichwahl ? "Stichwahl" : $"{nummer}. Wahlgang")} gestartet ({(w.Geheim ? "geheim" : "offen")})",
            nameof(Wahl), wahlId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return u;
    }

    /// <summary>Gleichstand in der Stichwahl: Vorstand erfasst, wen das Los bestimmt hat.</summary>
    public async Task LosErfassenAsync(Guid wahlId, Guid personId, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(wahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        if (d.LosErfassen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (d.Gleichstand.All(g => g.Id != personId))
        {
            throw new InvalidOperationException("Das Los kann nur zwischen den Personen mit Gleichstand entscheiden.");
        }

        var w = await daten.LadenAsync<Wahl>(wahlId, "Wahl", abbruch);
        w.Festhalten(Wahlstatus.Gewaehlt, personId, $"{w.Ergebnis} Gezogen: {d.Gleichstand.First(g => g.Id == personId).Name}.");
        await protokoll.ProtokollierenAsync("Wahl.Los", $"Losentscheid: {Pseudonym.Person(personId)}", nameof(Wahl), wahlId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Die gewählte Person (oder für sie der Vorstand) nimmt an: Amtszeit, Rolle und Funktion ab Beginn der Amtszeit.</summary>
    public async Task AnnehmenAsync(Guid wahlId, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(wahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        if (d.Annehmen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var wer = await benutzer.ErmittelnAsync(abbruch);
        var w = await daten.LadenAsync<Wahl>(wahlId, "Wahl", abbruch);
        await AmtUebernehmenAsync(w, w.GewaehltPersonId!.Value, $"Wahl: {w.Titel}", wer, abbruch);
        await protokoll.ProtokollierenAsync("Wahl.Angenommen", $"{Pseudonym.Person(w.GewaehltPersonId!.Value)} hat die Wahl angenommen (Amtszeit ab {w.AmtAb:dd.MM.yyyy})",
            nameof(Wahl), wahlId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await stammdaten.SpeichernAsync(abbruch);
    }

    /// <summary>
    /// Amt ohne Wahlgang direkt zuweisen (Vorstand, Geschäftsführung) – z. B. per Akklamation in der Sitzung oder wenn die Geschäftsordnung keine
    /// Wahl verlangt (⚑). Wie nach einer angenommenen Wahl: Amtszeit, Rolle und Funktion bitemporal ab <paramref name="ab"/> mit dem Grund, die
    /// bisherige Amtszeit endet am Vortag. Im Wahlprotokoll steht der Grund statt der Wahlgänge.
    /// </summary>
    public async Task<Wahl> AmtZuweisenAsync(Fraktionsamt amt, Guid personId, DateOnly ab, DateOnly? bis, string grund, Guid? gremiumId = null,
        string? bezeichnung = null, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        if (string.IsNullOrWhiteSpace(grund))
        {
            throw new InvalidOperationException("Bitte den Grund angeben (z. B. „per Akklamation in der Fraktionssitzung am …“).");
        }

        var person = await stammdaten.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        if (!person.Aktiv)
        {
            throw new InvalidOperationException($"{person.Anzeigename} ist nicht mehr aktiv.");
        }

        Wahl w;
        try
        {
            w = new Wahl(amt, bezeichnung, gremiumId, ab, bis ?? go.Value.WahlperiodeEnde, false, wer.PersonIdPflicht, uhr.GetUtcNow());
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(ex.Message.Split(" (Parameter", 2)[0], ex);
        }

        w.Festhalten(Wahlstatus.Gewaehlt, personId, $"Ohne Wahlgang zugewiesen: {grund.Trim()}.");
        daten.Hinzufuegen(w);
        await AmtUebernehmenAsync(w, personId, $"{w.Titel}: {grund.Trim()}", wer, abbruch);
        await protokoll.ProtokollierenAsync("Wahl.AmtZugewiesen", $"{w.Titel} an {Pseudonym.Person(personId)} ab {ab:dd.MM.yyyy} ohne Wahlgang zugewiesen",
            nameof(Wahl), w.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await stammdaten.SpeichernAsync(abbruch);
        return w;
    }

    /// <summary>Amtszeit anlegen, Vorgänger:in ablösen, Rolle und Funktion bitemporal ab Beginn der Amtszeit setzen (Annahme bzw. direkte Zuweisung).</summary>
    private async Task AmtUebernehmenAsync(Wahl w, Guid gewaehlt, string grund, Benutzer wer, CancellationToken abbruch)
    {
        var jetzt = uhr.GetUtcNow();
        var erfassung = Erfassung.Aenderung(jetzt, grund, wer.Name);

        // Bisherige Amtszeit desselben Amts endet am Vortag; die Person verliert die Rolle, wenn sie sie durch kein anderes Amt mehr hat.
        var alle = await daten.ListeAsync(daten.Menge<Amtszeit>().OrderBy(a => a.Id), abbruch);
        if (w.Amt.Einzeln() || w.Amt == Fraktionsamt.Sprecher)
        {
            foreach (var alt in alle.Where(a => a.Amt == w.Amt && a.GremiumId == w.GremiumId && a.PersonId != gewaehlt && a.GiltAm(w.AmtAb)))
            {
                alt.EndetVor(w.AmtAb);
                if (w.Amt.AmtsRolle() is { } rolle && !alle.Any(a => a.PersonId == alt.PersonId && a.Id != alt.Id && a.GiltAm(w.AmtAb) && a.Amt.AmtsRolle() == rolle)
                    && await stammdaten.PersonAsync(alt.PersonId, abbruch) is { } bisher)
                {
                    var rollen = bisher.RollenAm(w.AmtAb, jetzt).Where(r => r != rolle).ToList();
                    if (rollen.Count > 0)
                    {
                        bisher.RollenFestlegenAb(w.AmtAb, rollen, erfassung);
                    }

                    bisher.AngabeAendern(PersonMerkmalArt.Funktion, rollen.Contains(Rolle.Mitglied) ? "Ratsmitglied" : null, w.AmtAb, erfassung, false);
                }
            }
        }

        daten.Hinzufuegen(new Amtszeit(gewaehlt, w.Amt, w.Bezeichnung, w.GremiumId, w.AmtAb, w.AmtBis, w.Id));
        var person = await stammdaten.PersonAsync(gewaehlt, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        if (w.Amt.AmtsRolle() is { } neu)
        {
            person.RollenFestlegenAb(w.AmtAb, person.RollenAm(w.AmtAb, jetzt).Append(neu), erfassung);
            person.AngabeAendern(PersonMerkmalArt.Funktion, w.Amt.Anzeigename(), w.AmtAb, erfassung, false);
        }

        w.Annehmen(jetzt);
    }

    public async Task AblehnenAsync(Guid wahlId, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(wahlId, abbruch) ?? throw new InvalidOperationException("Wahl nicht gefunden.");
        if (d.Annehmen is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var w = await daten.LadenAsync<Wahl>(wahlId, "Wahl", abbruch);
        var gewaehlt = w.GewaehltPersonId!.Value;
        var name = (await stammdaten.PersonAsync(gewaehlt, abbruch))?.Anzeigename ?? "?";
        w.Ablehnen($"{w.Ergebnis} {name} hat die Wahl nicht angenommen – neuer Wahlgang nötig.");
        foreach (var k in await daten.ListeAsync(daten.Menge<Kandidatur>().Where(k => k.WahlId == wahlId && k.PersonId == gewaehlt).OrderBy(k => k.Id), abbruch))
        {
            k.Zurueckziehen(uhr.GetUtcNow());
        }

        await protokoll.ProtokollierenAsync("Wahl.Abgelehnt", $"{Pseudonym.Person(gewaehlt)} hat die Wahl nicht angenommen", nameof(Wahl), wahlId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Fristenprüfung: beendete Wahlgänge auswerten; vor Ende einer Amtszeit die Aufgabe „Neuwahl vorbereiten“ (einmal) anlegen.</summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var n = 0;
        foreach (var w in await daten.ListeAsync(daten.Menge<Wahl>().Where(w => w.Status == Wahlstatus.Wahlgang).OrderBy(w => w.Id), abbruch))
        {
            n += await FortschreibenAsync(w, abbruch) ? 1 : 0;
        }

        var grenze = Ortszeit.Heute(uhr).AddDays(go.Value.NeuwahlVorlaufTage);
        var namen = await NamenAsync(abbruch);
        foreach (var a in (await daten.ListeAsync(daten.Menge<Amtszeit>().Where(a => a.NeuwahlErinnertAm == null).OrderBy(a => a.Id), abbruch))
                 .Where(a => a.Bis is { } bis && bis <= grenze && bis >= Ortszeit.Heute(uhr)))
        {
            a.NeuwahlErinnert(uhr.GetUtcNow());
            await aufgaben.AnlegenAsync($"Neuwahl vorbereiten: {a.Amt.Anzeigename()}{(a.Bezeichnung is { } b ? $" ({b})" : "")}",
                $"Die Amtszeit von {namen.GetValueOrDefault(a.PersonId, "?")} endet am {a.Bis:dd.MM.yyyy}.", null, Rolle.Vorstand, Anlass, a.Id, "wahlen",
                a.Bis!.Value.AddDays(-14), abbruch: abbruch);
            n++;
        }

        await daten.SpeichernAsync(abbruch);
        return n;
    }

    private const string StichwahlText = "Stichwahl";

    private static bool IstStichwahl(Umfrage u) => u.Beschreibung == StichwahlText;

    /// <summary>Ist der laufende Wahlgang beendet, Ergebnis nach Mehrheitsregel feststellen.</summary>
    private async Task<bool> FortschreibenAsync(Wahl w, CancellationToken abbruch)
    {
        if (w.Status != Wahlstatus.Wahlgang || (await WahlgaengeAsync(w.Id, abbruch)).LastOrDefault() is not { Status: Umfragestatus.Abgeschlossen } letzter)
        {
            return false;
        }

        var e = await AuswertenAsync(w, letzter, abbruch);
        w.Festhalten(e.Status, e.Gewaehlt, e.Text);
        await protokoll.ProtokollierenAsync("Wahl.Ergebnis", e.Text.Split(" (", 2)[0], nameof(Wahl), w.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return true;
    }

    private async Task<Wahlgangergebnis> AuswertenAsync(Wahl w, Umfrage wahlgang, CancellationToken abbruch)
    {
        var optionen = await daten.ListeAsync(daten.Menge<UmfrageOption>().Where(o => o.UmfrageId == wahlgang.Id).OrderBy(o => o.Position).ThenBy(o => o.Id), abbruch);
        var ergebnis = (await umfragen.DetailAsync(wahlgang.Id, abbruch))?.Ergebnis ?? throw new InvalidOperationException("Der Wahlgang ist noch nicht ausgewertet.");
        int Zahl(Guid option) => ergebnis.Optionen.FirstOrDefault(o => o.OptionId == option)?.Stimmen ?? 0;
        var namen = await NamenAsync(abbruch);
        var stimmen = optionen.Where(o => o.KandidatPersonId is not null).ToDictionary(o => o.KandidatPersonId!.Value, o => Zahl(o.Id));
        return Mehrheit.Auswerten(stimmen, optionen.Where(o => o.KandidatPersonId is null && o.Text == Mehrheit.Nein).Sum(o => Zahl(o.Id)),
            optionen.Where(o => o.KandidatPersonId is null && o.Text == Mehrheit.Enthaltung).Sum(o => Zahl(o.Id)), IstStichwahl(wahlgang),
            go.Value.WahlEnthaltungenZaehlen, go.Value.WahlStichwahl, p => namen.GetValueOrDefault(p, "?"), ergebnis.ZuWenigeStimmen);
    }

    private async Task<string> WahlprotokollAsync(Wahl w, string? gremium, IReadOnlyList<Kandidatur> kandidaturen, IReadOnlyList<Umfrage> gaenge,
        IReadOnlyDictionary<Guid, string> namen, CancellationToken abbruch)
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        var sb = new StringBuilder();
        sb.Append(de, $"**Wahl:** {w.Titel}{(gremium is null ? "" : $" ({gremium})")}  \n");
        sb.Append(de, $"**Amtszeit:** {w.AmtAb:dd.MM.yyyy} bis {(w.AmtBis is { } bis ? bis.ToString("dd.MM.yyyy", de) : "offen")}  \n");
        sb.Append(de, $"**Verfahren:** {(w.Geheim ? "geheim" : "offen")}; Mehrheit der gültigen Stimmen{(go.Value.WahlEnthaltungenZaehlen ? " einschließlich Enthaltungen" : " ohne Enthaltungen")}"
            + $"{(go.Value.WahlStichwahl ? ", sonst Stichwahl der Bestplatzierten, bei Gleichstand Los" : "")}.\n\n");
        sb.Append(de, $"**Kandidaturen:** {(kandidaturen.Count == 0 ? "keine" : string.Join(", ", kandidaturen.Select(k => namen.GetValueOrDefault(k.PersonId, "?"))))}\n\n");
        var nr = 0;
        foreach (var u in gaenge)
        {
            nr++;
            var teil = (await umfragen.DetailAsync(u.Id, abbruch))?.Teilnehmende ?? 0;
            sb.Append(de, $"- **{(IstStichwahl(u) ? "Stichwahl" : $"{nr}. Wahlgang")}** ({Ortszeit.AlsOrtszeit(u.ErstelltAm):dd.MM.yyyy HH:mm} Uhr, {teil} von {u.Berechtigte} haben abgestimmt): "
                + $"{(u.Status == Umfragestatus.Offen ? "läuft" : u.Ergebnis)}\n");
        }

        if (w.Ergebnis is { } e)
        {
            sb.Append(de, $"\n**Feststellung:** {e}\n");
        }

        if (w.AngenommenAm is { } am && w.GewaehltPersonId is { } g)
        {
            var name = namen.GetValueOrDefault(g, "?");
            var datum = Ortszeit.AlsOrtszeit(am).ToString("dd.MM.yyyy", de);
            sb.Append(gaenge.Count == 0
                ? $"\n**Zuweisung:** Amt am {datum} an {name} übertragen (ohne Wahlgang).\n"
                : $"\n**Annahme:** {name} hat die Wahl am {datum} angenommen.\n");
        }

        return sb.ToString();
    }

    private Task<List<Umfrage>> WahlgaengeAsync(Guid wahlId, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Umfrage>().Where(u => u.WahlId == wahlId).OrderBy(u => u.ErstelltAm).ThenBy(u => u.Id), abbruch);

    private Task<List<Kandidatur>> KandidaturenAsync(Guid wahlId, CancellationToken abbruch)
        => daten.ListeAsync(daten.Menge<Kandidatur>().Where(k => k.WahlId == wahlId && k.ZurueckgezogenAm == null).OrderBy(k => k.Am).ThenBy(k => k.Id), abbruch);

    private async Task<IReadOnlyDictionary<Guid, string>> NamenAsync(CancellationToken abbruch)
        => (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);

    private async Task<Benutzer> PruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return Rechte.SitzungenSteuern(wer.Rolle) is { Erlaubt: false } nein ? throw new InvalidOperationException(nein.Begruendung) : wer;
    }
}
