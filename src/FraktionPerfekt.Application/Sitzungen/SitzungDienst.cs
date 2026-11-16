using System.Globalization;
using System.Text;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Sitzungen;

public sealed record SitzungZeile(Sitzung Sitzung, Termin Termin, string? Gremium, int Tops);

/// <summary>Termin, für den noch keine Sitzung vorbereitet ist.</summary>
public sealed record OffenerSitzungstermin(Termin Termin, string? Gremium, Sitzungsart Art);

public sealed record TopAnsicht(Top Top, int Nummer, string? AngemeldetVon, IReadOnlyList<Beschluss> Beschluesse, IReadOnlyList<Aufgabe> Aufgaben);

public sealed record AnwesenheitZeile(Guid PersonId, string Name, Anwesenheitsstatus? Status);

public sealed record SitzungDetail(
    Sitzung Sitzung,
    Termin Termin,
    string? Gremium,
    string? Leitung,
    IReadOnlyList<TopAnsicht> Tops,
    IReadOnlyList<AnwesenheitZeile> Anwesenheit,
    Freigabe Bearbeiten,
    Freigabe TopAnmelden,
    Freigabe Einladen,
    Freigabe Beginnen,
    Freigabe Protokollieren,
    Freigabe Beenden,
    Freigabe Freigeben);

public sealed record Sitzungsergebnis(string Meldung, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

public sealed record Protokolltreffer(Guid SitzungId, string Sitzung, DateOnly Datum, string Top, string Ausschnitt);

/// <summary>
/// Sitzungen und Protokolle (Module 7 und 9): Tagesordnung aus Vorlage, TOP-Anmeldung bis Frist, Einladung über den
/// Postausgang, Sitzungsmodus (Anwesenheit, Notizen, Beschlüsse, Aufgaben), TOPs umsortieren/einfügen/vertagen/zusammenlegen,
/// Entwurf → Freigabe durch die Sitzungsleitung → Versand. Vertagte TOPs wandern in die nächste Sitzung desselben Gremiums.
/// </summary>
public sealed class SitzungDienst(
    IDaten daten,
    Wissen.BildDienst bilder,
    ITerminSpeicher termine,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Protokoll";

    public async Task<IReadOnlyList<SitzungZeile>> SitzungenAsync(CancellationToken abbruch = default)
    {
        if ((await benutzer.ErmittelnAsync(abbruch)).Rolle is Rolle.Helferin)
        {
            return []; // Wie im Menü: Helfer:innen sehen nur den Wahlkampf (auch nicht über die direkte Adresse).
        }

        var sitzungen = await daten.ListeAsync(daten.Menge<Sitzung>().OrderBy(s => s.AngelegtAm).ThenBy(s => s.Id), abbruch);
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id, g => g.Name);
        var zeilen = new List<SitzungZeile>();
        foreach (var s in sitzungen)
        {
            if (await termine.TerminAsync(s.TerminId, abbruch) is { } t)
            {
                var anzahl = await daten.AnzahlAsync(daten.Menge<Top>().Where(x => x.SitzungId == s.Id && x.Status != Topstatus.Zusammengelegt), abbruch);
                zeilen.Add(new SitzungZeile(s, t, s.GremiumId is { } g && gremien.TryGetValue(g, out var n) ? n : null, anzahl));
            }
        }

        return zeilen.OrderByDescending(z => z.Termin.Beginn).ToList();
    }

    /// <summary>Kommende Fraktions-, Arbeitskreis- und Gremiensitzungen (6 Wochen), für die noch keine Sitzung vorbereitet ist.</summary>
    public async Task<IReadOnlyList<OffenerSitzungstermin>> OffeneTermineAsync(CancellationToken abbruch = default)
    {
        var jetzt = Ortszeit.Jetzt(uhr);
        var vorbereitet = (await daten.ListeAsync(daten.Menge<Sitzung>().Select(s => s.TerminId), abbruch)).ToHashSet();
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id, g => g.Name);
        return (await termine.TermineAsync(jetzt.Date, jetzt.Date.AddDays(42), abbruch))
            .Where(t => !t.Abgesagt && !vorbereitet.Contains(t.Id) && ArtFuer(t) is not null)
            .Select(t => new OffenerSitzungstermin(t, t.GremiumId is { } g && gremien.TryGetValue(g, out var n) ? n : null, ArtFuer(t)!.Value))
            .ToList();
    }

    public async Task<Sitzung?> ZuTerminAsync(Guid terminId, CancellationToken abbruch = default)
        => await daten.ErsterAsync(daten.Menge<Sitzung>().Where(s => s.TerminId == terminId).OrderBy(s => s.AngelegtAm).ThenBy(s => s.Id), abbruch);

    /// <summary>Sitzung zu einem Termin anlegen: Tagesordnung aus der Vorlage, vertagte TOPs der letzten Sitzung, Frist für TOP-Anmeldungen.</summary>
    public async Task<Sitzungsergebnis> VorbereitenAsync(Guid terminId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (VorbereitenFreigabe(wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var termin = await termine.TerminAsync(terminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (await ZuTerminAsync(terminId, abbruch) is { } vorhanden)
        {
            return new Sitzungsergebnis("Für diesen Termin gibt es die Sitzung schon.", [new("Sitzung öffnen", $"sitzungen/{vorhanden.Id}", Hauptschritt: true)]);
        }

        var art = ArtFuer(termin) ?? throw new InvalidOperationException("Für diesen Termin gibt es kein Sitzungsprotokoll (nur Fraktion, Arbeitskreise, Rat und Ausschüsse).");
        var jetzt = uhr.GetUtcNow();
        var (vorlage, tops) = art switch
        {
            Sitzungsart.Fraktionssitzung => ("Fraktionssitzung", (IReadOnlyList<string>)Sitzungsvorlagen.Fraktionssitzung),
            Sitzungsart.Arbeitskreis => ("Arbeitskreis", Sitzungsvorlagen.Arbeitskreis),
            _ => ("Tagesordnung aus dem RIS", Sitzungsvorlagen.AusTagesordnung(termin.Beschreibung)),
        };
        var frist = art == Sitzungsart.Gremienprotokoll ? (DateTimeOffset?)null : Ortszeit.Zeitpunkt(termin.Beginn).AddDays(-3);
        var sitzung = new Sitzung(termin.Id, termin.GremiumId, art, termin.Titel, vorlage, frist, await StandardLeitungAsync(art, abbruch), jetzt);
        daten.Hinzufuegen(sitzung);

        var position = 0;
        var neu = new List<Top>();
        foreach (var titel in tops)
        {
            neu.Add(new Top(sitzung.Id, ++position, titel, null, true, null, jetzt));
        }

        // Vertagtes aus früheren Sitzungen desselben Gremiums übernehmen (vor „Verschiedenes“).
        var uebernommen = 0;
        if (termin.GremiumId is { } gremiumId)
        {
            var frueher = await daten.ListeAsync(daten.Menge<Sitzung>().Where(s => s.GremiumId == gremiumId && s.Id != sitzung.Id).Select(s => s.Id), abbruch);
            var vertagt = await daten.ListeAsync(daten.Menge<Top>()
                .Where(t => frueher.Contains(t.SitzungId) && t.Status == Topstatus.Vertagt && t.UebernommenInSitzungId == null)
                .OrderBy(t => t.AngelegtAm).ThenBy(t => t.Id), abbruch);
            foreach (var t in vertagt)
            {
                neu.Add(new Top(sitzung.Id, ++position, t.Titel, t.Beschreibung, false, t.AngemeldetVonPersonId, jetzt, t.AntragId, t.Id));
                t.InSitzungUebernommen(sitzung.Id);
                uebernommen++;
            }
        }

        // Anstehende Anträge hängen automatisch an der Fraktionssitzung (je ein TOP).
        if (art == Sitzungsart.Fraktionssitzung)
        {
            var antraege = await daten.ListeAsync(daten.Menge<Domain.Antraege.Antrag>()
                .Where(a => a.Status == Domain.Antraege.Antragsstatus.Entwurf || a.Status == Domain.Antraege.Antragsstatus.InAbstimmung)
                .OrderBy(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch);
            foreach (var a in antraege)
            {
                neu.Add(new Top(sitzung.Id, ++position, $"Antrag: {a.Titel}", $"Status: {a.Status}", false, a.AutorPersonId, jetzt, a.Id));
            }
        }

        neu.ForEach(daten.Hinzufuegen);
        await NeuNummerierenAsync(sitzung.Id, letzterBleibt: "Verschiedenes", abbruch, neu);
        await protokoll.ProtokollierenAsync("Sitzung.Vorbereitet", $"Sitzung zum Termin am {termin.Beginn:dd.MM.yyyy} vorbereitet ({position} TOPs, {uebernommen} vertagt übernommen)",
            nameof(Sitzung), sitzung.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return new Sitzungsergebnis($"Tagesordnung aus der Vorlage „{vorlage}“ angelegt{(uebernommen > 0 ? $", {uebernommen} vertagte TOPs übernommen" : "")}.",
        [
            new("Einladung mit Tagesordnung versenden", $"sitzungen/{sitzung.Id}?reiter=einladung", "Mitglieder können bis zur Frist eigene TOPs anmelden.", Hauptschritt: true),
            new("Raum buchen", $"ressourcen?termin={termin.Id}"),
            new("Tagesordnung bearbeiten", $"sitzungen/{sitzung.Id}"),
        ]);
    }

    public async Task<SitzungDetail?> DetailAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return null; // Wie im Menü: Helfer:innen sehen nur den Wahlkampf (auch nicht über die direkte Adresse).
        }
        if (await daten.FindenAsync<Sitzung>(sitzungId, abbruch) is not { } s || await termine.TerminAsync(s.TerminId, abbruch) is not { } termin)
        {
            return null;
        }

        var personen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        string? Name(Guid? id) => id is { } i && personen.TryGetValue(i, out var p) ? p.Anzeigename : null;
        var tops = await TopsAsync(sitzungId, abbruch);
        var topIds = tops.Select(t => t.Id).ToList();
        var beschluesse = await daten.ListeAsync(daten.Menge<Beschluss>().Where(b => b.SitzungId == sitzungId).OrderBy(b => b.Id), abbruch);
        var aufgaben = await daten.ListeAsync(daten.Menge<Aufgabe>().Where(a => a.Anlass == AufgabenAnlass && a.AnlassId == sitzungId).OrderBy(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch);
        var anwesenheit = await daten.ListeAsync(daten.Menge<Sitzungsanwesenheit>().Where(a => a.SitzungId == sitzungId).OrderBy(a => a.Id), abbruch);
        var teilnehmende = await TeilnehmendeAsync(s, termin, abbruch);
        var nummer = 0;
        var ansicht = tops.Select(t => new TopAnsicht(t, t.Status == Topstatus.Zusammengelegt ? 0 : ++nummer, Name(t.AngemeldetVonPersonId),
            beschluesse.Where(b => b.TopId == t.Id).ToList(),
            aufgaben.Where(a => a.Beschreibung?.Contains($"[TOP {t.Id:N}]", StringComparison.Ordinal) == true).ToList())).ToList();
        var gremium = s.GremiumId is { } g ? await stammdaten.GremiumAsync(g, abbruch) : null;
        var istLeitung = wer.PersonId is { } ich && (ich == s.LeitungPersonId || ich == s.ProtokollPersonId);
        var steuert = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt || istLeitung;

        return new SitzungDetail(s, termin, gremium?.Name, Name(s.LeitungPersonId), ansicht,
            teilnehmende.Select(p => new AnwesenheitZeile(p.Id, p.Anzeigename, anwesenheit.FirstOrDefault(a => a.PersonId == p.Id)?.Status))
                .OrderBy(a => a.Name, StringComparer.CurrentCulture).ToList(),
            steuert ? (s.TagesordnungAenderbar ? Freigabe.Ja : Freigabe.Nein("Die Tagesordnung ist nach Sitzungsende nicht mehr änderbar.")) : NurLeitung,
            TopAnmeldenFreigabe(s, wer),
            steuert ? (s.Status is Sitzungsstatus.Vorbereitung or Sitzungsstatus.Eingeladen ? Freigabe.Ja : Freigabe.Nein("Die Sitzung hat bereits begonnen.")) : NurLeitung,
            steuert ? (s.Status is Sitzungsstatus.Vorbereitung or Sitzungsstatus.Eingeladen ? Freigabe.Ja : Freigabe.Nein("Die Sitzung läuft bereits oder ist beendet.")) : NurLeitung,
            steuert ? (s.ProtokollAenderbar ? Freigabe.Ja : Freigabe.Nein(s.Status == Sitzungsstatus.Freigegeben
                ? "Das Protokoll ist freigegeben und unveränderlich."
                : "Notizen und Beschlüsse werden erst im Sitzungsmodus erfasst („Sitzung beginnen“).")) : NurLeitung,
            steuert ? (s.Status == Sitzungsstatus.Laufend ? Freigabe.Ja : Freigabe.Nein("Nur eine laufende Sitzung kann beendet werden.")) : NurLeitung,
            FreigebenFreigabe(s, wer));
    }

    public Freigabe FreigebenFreigabe(Sitzung s, Benutzer wer)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(wer);
        if (s.Status != Sitzungsstatus.Entwurf)
        {
            return Freigabe.Nein(s.Status == Sitzungsstatus.Freigegeben ? "Das Protokoll ist bereits freigegeben." : "Erst die Sitzung beenden; dann liegt ein Entwurf vor.");
        }

        return wer.PersonId == s.LeitungPersonId || wer.Rolle == Rolle.Vorstand
            ? Freigabe.Ja
            : Freigabe.Nein("Das Protokoll gibt die Sitzungsleitung frei (oder der Vorstand).");
    }

    private static Freigabe NurLeitung => Freigabe.Nein("Nur für Vorstand, Administration, Sitzungsleitung und Protokollführung.");

    private Freigabe VorbereitenFreigabe(Benutzer wer) => Rechte.SitzungenSteuern(wer.Rolle);

    private Freigabe TopAnmeldenFreigabe(Sitzung s, Benutzer wer)
    {
        if (wer.Rolle is Rolle.Helferin)
        {
            return Freigabe.Nein("Helfer:innen melden keine Tagesordnungspunkte an.");
        }

        if (!s.TagesordnungAenderbar)
        {
            return Freigabe.Nein("Die Sitzung ist beendet.");
        }

        return s.TopAnmeldungBis is { } frist && uhr.GetUtcNow() > frist && !Rechte.SitzungenSteuern(wer.Rolle).Erlaubt
            ? Freigabe.Nein($"Die Frist für TOP-Anmeldungen ist am {Ortszeit.AlsOrtszeit(frist):dd.MM. HH:mm} abgelaufen. Bitte an den Vorstand wenden.")
            : Freigabe.Ja;
    }

    public async Task<Top> TopAnmeldenAsync(Guid sitzungId, string titel, string? beschreibung, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        if (TopAnmeldenFreigabe(s, wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var tops = await TopsAsync(sitzungId, abbruch);
        var top = new Top(sitzungId, tops.Count + 1, titel, beschreibung, false, wer.PersonId, uhr.GetUtcNow());
        daten.Hinzufuegen(top);
        await NeuNummerierenAsync(sitzungId, "Verschiedenes", abbruch, [.. tops, top]);
        await protokoll.ProtokollierenAsync("Sitzung.TopAngemeldet", $"TOP angemeldet von {(wer.PersonId is { } p ? Pseudonym.Person(p) : wer.Name)}", nameof(Sitzung), sitzungId,
            abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return top;
    }

    /// <summary>TOP um eine Position nach oben (-1) oder unten (+1) verschieben.</summary>
    public async Task TopVerschiebenAsync(Guid topId, int richtung, CancellationToken abbruch = default)
    {
        var (s, top) = await TopZumBearbeitenAsync(topId, abbruch);
        var tops = (await TopsAsync(s.Id, abbruch)).Where(t => t.Status != Topstatus.Zusammengelegt).ToList();
        var i = tops.FindIndex(t => t.Id == top.Id);
        var j = i + Math.Sign(richtung);
        if (j < 0 || j >= tops.Count)
        {
            return;
        }

        (tops[i], tops[j]) = (tops[j], tops[i]);
        for (var k = 0; k < tops.Count; k++)
        {
            tops[k].PositionSetzen(k + 1);
        }

        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Neuen TOP an einer Stelle einfügen (Position 1 = ganz vorne).</summary>
    public async Task<Top> TopEinfuegenAsync(Guid sitzungId, string titel, string? beschreibung, int position, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        await BearbeitenPruefenAsync(s, abbruch);
        var tops = (await TopsAsync(sitzungId, abbruch)).Where(t => t.Status != Topstatus.Zusammengelegt).ToList();
        var top = new Top(sitzungId, 0, titel, beschreibung, false, wer.PersonId, uhr.GetUtcNow());
        daten.Hinzufuegen(top);
        tops.Insert(Math.Clamp(position - 1, 0, tops.Count), top);
        for (var k = 0; k < tops.Count; k++)
        {
            tops[k].PositionSetzen(k + 1);
        }

        await daten.SpeichernAsync(abbruch);
        return top;
    }

    public async Task TopAendernAsync(Guid topId, string titel, string? beschreibung, CancellationToken abbruch = default)
    {
        var (_, top) = await TopZumBearbeitenAsync(topId, abbruch);
        top.Aendern(titel, beschreibung);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task TopVertagenAsync(Guid topId, CancellationToken abbruch = default)
    {
        var (s, top) = await TopZumBearbeitenAsync(topId, abbruch);
        top.Vertagen();
        await protokoll.ProtokollierenAsync("Sitzung.TopVertagt", $"TOP „{top.Titel}“ vertagt", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task TopZusammenlegenAsync(Guid topId, Guid mitTopId, CancellationToken abbruch = default)
    {
        var (s, top) = await TopZumBearbeitenAsync(topId, abbruch);
        var ziel = await daten.LadenAsync<Top>(mitTopId, "TOP", abbruch);
        top.ZusammenlegenMit(ziel);
        await NeuNummerierenAsync(s.Id, null, abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Einladung mit Tagesordnung an alle Mitglieder des Gremiums (Postausgang: Mail und Push).</summary>
    public async Task<Sitzungsergebnis> EinladenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        await SteuernPruefenAsync(s, abbruch);
        var termin = await termine.TerminAsync(s.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        var tops = (await TopsAsync(sitzungId, abbruch)).Where(t => t.Status != Topstatus.Zusammengelegt).ToList();
        var erneut = s.EingeladenAm is not null;
        s.Einladen(uhr.GetUtcNow());

        var text = new StringBuilder();
        text.Append(CultureInfo.CurrentCulture, $"Einladung: {s.Titel} am {termin.Beginn:dddd, dd.MM.yyyy} um {termin.Beginn:HH:mm} Uhr, {termin.Ort ?? "Ort folgt"}\n\nTagesordnung\n");
        for (var i = 0; i < tops.Count; i++)
        {
            text.Append(CultureInfo.CurrentCulture, $"TOP {i + 1} {tops[i].Titel}\n");
        }

        if (s.TopAnmeldungBis is { } frist)
        {
            text.Append(CultureInfo.CurrentCulture, $"\nWeitere TOPs bitte bis {Ortszeit.AlsOrtszeit(frist):dd.MM. HH:mm} Uhr anmelden: {optionen.Value.Link($"sitzungen/{s.Id}")}\n");
        }

        text.Append(CultureInfo.CurrentCulture, $"\nBitte zu- oder absagen: {optionen.Value.Link($"termine/{termin.Id}")}");
        var empfaenger = await TeilnehmendeAsync(s, termin, abbruch);
        foreach (var p in empfaenger)
        {
            await postausgang.BenachrichtigenAsync(p, $"{(erneut ? "Aktualisierte " : "")}Einladung: {s.Titel} am {termin.Beginn:dd.MM.}",
                $"{s.Titel} am {termin.Beginn:dd.MM.} um {termin.Beginn:HH:mm} Uhr – bitte zu- oder absagen.", $"Hallo {p.Vorname},\n\n{text}",
                $"termine/{termin.Id}", "Zusagen", abbruch);
        }

        await protokoll.ProtokollierenAsync("Sitzung.Eingeladen", $"Einladung an {empfaenger.Count} Personen in den Postausgang gelegt", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return new Sitzungsergebnis($"Einladung an {empfaenger.Count} Personen versandt (Postausgang).",
        [
            new("Rückmeldungen ansehen", $"termine/{termin.Id}", Hauptschritt: true),
            new("Raum buchen", $"ressourcen?termin={termin.Id}"),
            new("Zur Sitzung", $"sitzungen/{s.Id}"),
        ]);
    }

    /// <summary>Sitzungsmodus starten; Anwesenheit wird aus den Rückmeldungen vorbelegt.</summary>
    public async Task BeginnenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        await SteuernPruefenAsync(s, abbruch);
        var termin = await termine.TerminAsync(s.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        s.Beginnen(uhr.GetUtcNow(), wer.PersonId);
        var teilnahmen = await termine.TeilnahmenAsync([termin.Id], abbruch);
        foreach (var p in await TeilnehmendeAsync(s, termin, abbruch))
        {
            var status = teilnahmen.FirstOrDefault(t => t.PersonId == p.Id)?.Status switch
            {
                Teilnahmestatus.Abgesagt => Anwesenheitsstatus.Entschuldigt,
                Teilnahmestatus.Zugesagt or Teilnahmestatus.Vertritt => Anwesenheitsstatus.Anwesend,
                _ => (Anwesenheitsstatus?)null,
            };
            if (status is { } st)
            {
                daten.Hinzufuegen(new Sitzungsanwesenheit(s.Id, p.Id, st));
            }
        }

        await protokoll.ProtokollierenAsync("Sitzung.Begonnen", "Sitzungsmodus gestartet", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task AnwesenheitSetzenAsync(Guid sitzungId, Guid personId, Anwesenheitsstatus status, CancellationToken abbruch = default)
    {
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        await ProtokollierenPruefenAsync(s, abbruch);
        var a = await daten.ErsterAsync(daten.Menge<Sitzungsanwesenheit>().Where(x => x.SitzungId == sitzungId && x.PersonId == personId).OrderBy(x => x.Id), abbruch);
        if (a is null)
        {
            daten.Hinzufuegen(new Sitzungsanwesenheit(sitzungId, personId, status));
        }
        else
        {
            a.Setzen(status);
        }

        await daten.SpeichernAsync(abbruch);
    }

    public async Task NotizSetzenAsync(Guid topId, string? notiz, bool behandelt, CancellationToken abbruch = default)
    {
        var top = await daten.LadenAsync<Top>(topId, "TOP", abbruch);
        var s = await daten.LadenAsync<Sitzung>(top.SitzungId, "Sitzung", abbruch);
        await ProtokollierenPruefenAsync(s, abbruch);
        top.NotizSetzen(notiz);
        if (behandelt && top.Status == Topstatus.Geplant)
        {
            top.AlsBehandeltMarkieren();
        }

        await daten.SpeichernAsync(abbruch);
        await bilder.VerwendungenAbgleichenAsync("Sitzung", s.Id, notiz, abbruch);
    }

    public async Task<Beschluss> BeschlussErfassenAsync(Guid topId, string text, int ja, int nein, int enthaltung, CancellationToken abbruch = default)
    {
        var top = await daten.LadenAsync<Top>(topId, "TOP", abbruch);
        var s = await daten.LadenAsync<Sitzung>(top.SitzungId, "Sitzung", abbruch);
        await ProtokollierenPruefenAsync(s, abbruch);
        var termin = await termine.TerminAsync(s.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        var quelle = s.Art switch
        {
            Sitzungsart.Gremienprotokoll when termin.Art == Terminart.Ratssitzung => Beschlussquelle.Rat,
            Sitzungsart.Gremienprotokoll => Beschlussquelle.Ausschuss,
            _ => Beschlussquelle.Fraktion,
        };
        var ergebnis = ja + nein + enthaltung == 0 ? Beschlussergebnis.Kenntnisnahme : Beschluss.Auswerten(ja, nein);
        var b = new Beschluss(quelle, termin.Tag, top.Titel, text, ergebnis, ja, nein, enthaltung, s.Id, top.Id, s.GremiumId, top.AntragId);
        daten.Hinzufuegen(b);
        top.AlsBehandeltMarkieren();
        await protokoll.ProtokollierenAsync("Sitzung.Beschluss", $"Beschluss zu TOP „{top.Titel}“: {ergebnis} ({b.Stimmen})", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return b;
    }

    /// <summary>Aufgabe aus dem Protokoll an eine Person (z. B. „Klären bis nächste Sitzung“).</summary>
    public async Task<Aufgabe> AufgabeZuweisenAsync(Guid topId, Guid personId, string titel, DateOnly? frist, CancellationToken abbruch = default)
    {
        var top = await daten.LadenAsync<Top>(topId, "TOP", abbruch);
        var s = await daten.LadenAsync<Sitzung>(top.SitzungId, "Sitzung", abbruch);
        await ProtokollierenPruefenAsync(s, abbruch);
        var aufgabe = new Aufgabe(titel, $"Aus {s.Titel}, TOP „{top.Titel}“ [TOP {top.Id:N}]", null, AufgabenAnlass, s.Id, $"sitzungen/{s.Id}", uhr.GetUtcNow(),
            frist is { } f ? Ortszeit.Zeitpunkt(f.ToDateTime(new TimeOnly(18, 0))) : null, personId);
        daten.Hinzufuegen(aufgabe);
        if (await stammdaten.PersonAsync(personId, abbruch) is { } p)
        {
            await postausgang.PushEinreihenAsync(p, "Neue Aufgabe aus der Sitzung", titel, "aufgaben", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Aufgabe.Zugewiesen", $"Aufgabe aus Protokoll an {Pseudonym.Person(personId)}", nameof(Aufgabe), aufgabe.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return aufgabe;
    }

    public async Task BeendenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        await SteuernPruefenAsync(s, abbruch);
        s.Beenden(uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Sitzung.Beendet", "Sitzung beendet, Protokoll als Entwurf", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Freigabe durch die Sitzungsleitung: Protokoll an alle Teilnehmenden, vertagte TOPs in die nächste Sitzung.</summary>
    public async Task<Sitzungsergebnis> FreigebenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        if (FreigebenFreigabe(s, wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var termin = await termine.TerminAsync(s.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        s.Freigeben(wer.PersonIdPflicht, uhr.GetUtcNow());
        var text = await ProtokolltextAsync(s.Id, abbruch);
        var empfaenger = await TeilnehmendeAsync(s, termin, abbruch);
        foreach (var p in empfaenger.Where(p => p.EMail is not null))
        {
            await postausgang.MailEinreihenAsync(p.EMail!, p.Anzeigename, $"Protokoll: {s.Titel} vom {termin.Beginn:dd.MM.yyyy}", text, p.Id,
                $"sitzungen/{s.Id}", abbruch);
        }

        // Vertagte TOPs: in eine bereits vorbereitete nächste Sitzung desselben Gremiums, sonst beim Vorbereiten.
        var vertagt = (await TopsAsync(s.Id, abbruch)).Where(t => t.Status == Topstatus.Vertagt && t.UebernommenInSitzungId is null).ToList();
        var naechste = await NaechsteSitzungAsync(s, termin, abbruch);
        if (naechste is not null && naechste.TagesordnungAenderbar)
        {
            var tops = await TopsAsync(naechste.Id, abbruch);
            foreach (var t in vertagt)
            {
                daten.Hinzufuegen(new Top(naechste.Id, tops.Count + 1, t.Titel, t.Beschreibung, false, t.AngemeldetVonPersonId, uhr.GetUtcNow(), t.AntragId, t.Id));
                t.InSitzungUebernommen(naechste.Id);
            }
        }

        await protokoll.ProtokollierenAsync("Sitzung.Freigegeben", $"Protokoll freigegeben und an {empfaenger.Count} Personen versandt", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        if (naechste is not null)
        {
            await NeuNummerierenAsync(naechste.Id, "Verschiedenes", abbruch);
            await daten.SpeichernAsync(abbruch);
        }

        var schritte = new List<NaechsterSchritt> { new("Aufgaben verteilen und prüfen", "aufgaben?ansicht=alle", Hauptschritt: true) };
        if (vertagt.Count > 0)
        {
            schritte.Add(naechste is null
                ? new NaechsterSchritt($"{vertagt.Count} vertagte TOPs: nächste Sitzung vorbereiten", "sitzungen", "Beim Vorbereiten werden die vertagten TOPs übernommen.")
                : new NaechsterSchritt($"{vertagt.Count} vertagte TOPs in die nächste Einladung übernommen", $"sitzungen/{naechste.Id}"));
        }

        schritte.Add(new NaechsterSchritt("Protokoll als PDF", $"sitzungen/{s.Id}/protokoll.pdf"));
        return new Sitzungsergebnis($"Protokoll freigegeben und an {empfaenger.Count} Personen versandt.", schritte);
    }

    /// <summary>Protokoll als Text (Markdown), z. B. für Mail und PDF.</summary>
    public async Task<string> ProtokolltextAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(sitzungId, abbruch) ?? throw new InvalidOperationException("Sitzung nicht gefunden.");
        var sb = new StringBuilder();
        sb.Append(CultureInfo.CurrentCulture, $"# Protokoll: {d.Sitzung.Titel}\n\n");
        sb.Append(CultureInfo.CurrentCulture, $"{d.Termin.Beginn:dddd, dd.MM.yyyy}, {d.Termin.Beginn:HH:mm} Uhr, {d.Termin.Ort}\n");
        sb.Append(CultureInfo.CurrentCulture, $"Sitzungsleitung: {d.Leitung ?? "–"} · Status: {d.Sitzung.Status.Anzeigename()}\n\n");
        sb.Append(CultureInfo.CurrentCulture, $"Anwesend: {string.Join(", ", d.Anwesenheit.Where(a => a.Status == Anwesenheitsstatus.Anwesend).Select(a => a.Name))}\n");
        sb.Append(CultureInfo.CurrentCulture, $"Entschuldigt: {string.Join(", ", d.Anwesenheit.Where(a => a.Status == Anwesenheitsstatus.Entschuldigt).Select(a => a.Name))}\n\n");
        foreach (var t in d.Tops.Where(t => t.Top.Status != Topstatus.Zusammengelegt))
        {
            sb.Append(CultureInfo.CurrentCulture, $"## TOP {t.Nummer} {t.Top.Titel}{(t.Top.Status == Topstatus.Vertagt ? " (vertagt)" : "")}\n\n");
            if (t.Top.Notiz is { } n)
            {
                sb.Append(n).Append("\n\n");
            }

            foreach (var b in t.Beschluesse)
            {
                sb.Append(CultureInfo.CurrentCulture, $"**Beschluss:** {b.Text} – {b.Ergebnis} ({b.Stimmen})\n\n");
            }

            foreach (var a in t.Aufgaben)
            {
                sb.Append(CultureInfo.CurrentCulture, $"- Aufgabe: {a.Titel}{(a.FaelligAm is { } f ? $" (bis {Ortszeit.AlsOrtszeit(f):dd.MM.yyyy})" : "")}\n");
            }

            if (t.Aufgaben.Count > 0)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>Volltextsuche über freigegebene Protokolle (TOP-Titel, Notizen, Beschlüsse).</summary>
    public async Task<IReadOnlyList<Protokolltreffer>> SuchenAsync(string suche, CancellationToken abbruch = default)
    {
        if (string.IsNullOrWhiteSpace(suche))
        {
            return [];
        }

        var muster = suche.Trim();
        var sitzungen = (await daten.ListeAsync(daten.Menge<Sitzung>().Where(s => s.Status == Sitzungsstatus.Freigegeben).OrderBy(s => s.Id), abbruch)).ToDictionary(s => s.Id);
        var ids = sitzungen.Keys.ToList();
        var tops = await daten.ListeAsync(daten.Menge<Top>().Where(t => ids.Contains(t.SitzungId)).OrderBy(t => t.Id), abbruch);
        var beschluesse = await daten.ListeAsync(daten.Menge<Beschluss>().Where(b => b.SitzungId != null && ids.Contains(b.SitzungId.Value)).OrderBy(b => b.Id), abbruch);
        var treffer = new List<Protokolltreffer>();
        foreach (var t in tops)
        {
            var text = string.Join(" · ", new[] { t.Titel, t.Notiz }.Concat(beschluesse.Where(b => b.TopId == t.Id).Select(b => b.Text)).Where(x => !string.IsNullOrWhiteSpace(x)));
            var pos = text.IndexOf(muster, StringComparison.CurrentCultureIgnoreCase);
            if (pos >= 0 && await termine.TerminAsync(sitzungen[t.SitzungId].TerminId, abbruch) is { } termin)
            {
                var start = Math.Max(0, pos - 60);
                treffer.Add(new Protokolltreffer(t.SitzungId, sitzungen[t.SitzungId].Titel, termin.Tag, t.Titel,
                    (start > 0 ? "…" : "") + text.Substring(start, Math.Min(text.Length - start, 180))));
            }
        }

        return treffer.OrderByDescending(t => t.Datum).ToList();
    }

    private async Task<Sitzung?> NaechsteSitzungAsync(Sitzung s, Termin termin, CancellationToken abbruch)
    {
        if (s.GremiumId is not { } g)
        {
            return null;
        }

        var kandidaten = await daten.ListeAsync(daten.Menge<Sitzung>().Where(x => x.GremiumId == g && x.Id != s.Id).OrderBy(x => x.Id), abbruch);
        Sitzung? beste = null;
        DateTime? besterBeginn = null;
        foreach (var k in kandidaten)
        {
            if (await termine.TerminAsync(k.TerminId, abbruch) is { } t && t.Beginn > termin.Beginn && (besterBeginn is null || t.Beginn < besterBeginn))
            {
                (beste, besterBeginn) = (k, t.Beginn);
            }
        }

        return beste;
    }

    /// <summary>Wer zur Sitzung gehört: Mitglieder des Gremiums (ordentlich und Stellvertretung) am Sitzungstag, dazu die Geschäftsführung.</summary>
    private async Task<IReadOnlyList<Person>> TeilnehmendeAsync(Sitzung s, Termin termin, CancellationToken abbruch)
    {
        var personen = (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv).ToList();
        var jetzt = uhr.GetUtcNow();
        var heute = Ortszeit.Heute(uhr);
        if (s.GremiumId is not { } g)
        {
            return personen.Where(p => p.RollenAm(heute, jetzt).Any(r => r is not Rolle.Helferin)).ToList();
        }

        var sitze = Besetzung.Am(await stammdaten.MitgliedschaftenDesGremiumsAsync(g, abbruch), g, termin.Tag, jetzt).Select(x => x.PersonId).ToHashSet();
        return personen.Where(p => sitze.Contains(p.Id) || (s.Art == Sitzungsart.Fraktionssitzung && p.RollenAm(heute, jetzt).Contains(Rolle.Administration)))
            .OrderBy(p => p.Nachname, StringComparer.CurrentCulture).ToList();
    }

    private async Task<Guid?> StandardLeitungAsync(Sitzungsart art, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (art != Sitzungsart.Fraktionssitzung)
        {
            return wer.PersonId;
        }

        // Fraktionssitzungen leitet der Vorstand; der erste Vorstand in alphabetischer Reihenfolge ist voreingestellt.
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        return (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv && p.RollenAm(heute, jetzt).Contains(Rolle.Vorstand))
            .OrderBy(p => p.Nachname, StringComparer.CurrentCulture).Select(p => (Guid?)p.Id).FirstOrDefault() ?? wer.PersonId;
    }

    private static Sitzungsart? ArtFuer(Termin t) => t.Art switch
    {
        Terminart.Fraktionssitzung => Sitzungsart.Fraktionssitzung,
        Terminart.Arbeitskreis => Sitzungsart.Arbeitskreis,
        Terminart.Ratssitzung or Terminart.Ausschusssitzung => Sitzungsart.Gremienprotokoll,
        _ => null,
    };

    private async Task<List<Top>> TopsAsync(Guid sitzungId, CancellationToken abbruch)
        => await daten.ListeAsync(daten.Menge<Top>().Where(t => t.SitzungId == sitzungId).OrderBy(t => t.Position).ThenBy(t => t.AngelegtAm).ThenBy(t => t.Id), abbruch);

    /// <summary>Positionen lückenlos neu vergeben; „Verschiedenes“ bleibt am Ende.</summary>
    private async Task NeuNummerierenAsync(Guid sitzungId, string? letzterBleibt, CancellationToken abbruch, IReadOnlyList<Top>? tops = null)
    {
        var liste = (tops ?? await TopsAsync(sitzungId, abbruch)).Where(t => t.Status != Topstatus.Zusammengelegt).OrderBy(t => t.Position).ToList();
        if (letzterBleibt is not null && liste.FirstOrDefault(t => t.Fest && t.Titel == letzterBleibt) is { } ende)
        {
            liste.Remove(ende);
            liste.Add(ende);
        }

        for (var k = 0; k < liste.Count; k++)
        {
            liste[k].PositionSetzen(k + 1);
        }
    }

    private async Task<(Sitzung, Top)> TopZumBearbeitenAsync(Guid topId, CancellationToken abbruch)
    {
        var top = await daten.LadenAsync<Top>(topId, "TOP", abbruch);
        var s = await daten.LadenAsync<Sitzung>(top.SitzungId, "Sitzung", abbruch);
        await BearbeitenPruefenAsync(s, abbruch);
        return (s, top);
    }

    private async Task BearbeitenPruefenAsync(Sitzung s, CancellationToken abbruch)
    {
        await SteuernPruefenAsync(s, abbruch);
        if (!s.TagesordnungAenderbar)
        {
            throw new InvalidOperationException("Die Tagesordnung ist nach Sitzungsende nicht mehr änderbar.");
        }
    }

    private async Task SteuernPruefenAsync(Sitzung s, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && !Rechte.SitzungenSteuern(wer.Rolle).Erlaubt && wer.PersonId != s.LeitungPersonId && wer.PersonId != s.ProtokollPersonId)
        {
            throw new InvalidOperationException(NurLeitung.Begruendung);
        }
    }

    private async Task ProtokollierenPruefenAsync(Sitzung s, CancellationToken abbruch)
    {
        await SteuernPruefenAsync(s, abbruch);
        if (!s.ProtokollAenderbar)
        {
            throw new InvalidOperationException(s.Status == Sitzungsstatus.Freigegeben ? "Das Protokoll ist freigegeben und unveränderlich." : "Erst „Sitzung beginnen“.");
        }
    }
}
