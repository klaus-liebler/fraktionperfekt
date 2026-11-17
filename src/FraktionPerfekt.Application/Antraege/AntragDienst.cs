using System.Globalization;
using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Dokumente;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Antraege;

public sealed record AntragZeile(Antrag Antrag, string Autor, string? Gremium, int Fassungen, int OffeneVorschlaege);

public sealed record KommentarZeile(AntragKommentar Kommentar, string Person);

public sealed record VorschlagZeile(Aenderungsvorschlag Vorschlag, string Person, Freigabe Entscheiden);

public sealed record Statusschritt(Antragsstatus Ziel, string Titel, Freigabe Freigabe);

public sealed record AntragDetail(
    Antrag Antrag,
    string Autor,
    string? Gremium,
    Termin? Sitzung,
    IReadOnlyList<AntragFassung> Fassungen,
    IReadOnlyList<KommentarZeile> Kommentare,
    IReadOnlyList<VorschlagZeile> Vorschlaege,
    UmfrageDetail? Abstimmung,
    Freigabe TextBearbeiten,
    Freigabe AbstimmungStarten,
    IReadOnlyList<Statusschritt> Statusschritte)
{
    public AntragFassung Aktuell => Fassungen[^1];
}

public sealed record Antragsergebnis(string Meldung, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

/// <summary>
/// Anträge gemeinsam entwickeln (Modul 3): Status, Fassungen mit Vergleich, Kommentare am Text, Änderungsvorschläge
/// (annehmen/ablehnen), Fraktionsabstimmung mit Frist und Quorum, Einreichen mit Nummer, PDF im Fraktionslayout,
/// Zuordnung zu Gremium und Sitzung.
/// </summary>
public sealed class AntragDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    ITerminSpeicher termine,
    UmfrageDienst umfragen,
    IPdfErzeugung pdf,
    Wissen.BildDienst bilder,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    public async Task<IReadOnlyList<AntragZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        if ((await benutzer.ErmittelnAsync(abbruch)).Rolle is Rolle.Helferin)
        {
            return []; // Wie im Menü: Das Kampagnenteam sieht nur die Kampagnen (auch nicht über die direkte Adresse).
        }

        var antraege = await daten.ListeAsync(daten.Menge<Antrag>().OrderByDescending(a => a.GeaendertAm).ThenBy(a => a.Id), abbruch);
        var fassungen = await daten.ListeAsync(daten.Menge<AntragFassung>().Select(f => f.AntragId), abbruch);
        var vorschlaege = await daten.ListeAsync(daten.Menge<Aenderungsvorschlag>().Where(v => v.Status == Vorschlagsstatus.Offen).Select(v => v.AntragId), abbruch);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id, g => g.Kurzname);
        return antraege.Select(a => new AntragZeile(a, namen.GetValueOrDefault(a.AutorPersonId, "?"),
            a.ZielGremiumId is { } g ? gremien.GetValueOrDefault(g) : null, fassungen.Count(f => f == a.Id), vorschlaege.Count(v => v == a.Id))).ToList();
    }

    public async Task<AntragDetail?> DetailAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return null; // Wie im Menü: Das Kampagnenteam sieht nur die Kampagnen (auch nicht über die direkte Adresse).
        }
        if (await daten.FindenAsync<Antrag>(id, abbruch) is not { } a)
        {
            return null;
        }

        // Namen zum damaligen Stand (Tag der Antragstellung), wie VibeVHS es bei alten Dokumenten macht
        var damals = DateOnly.FromDateTime(Ortszeit.AlsOrtszeit(a.ErstelltAm).Date);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.AnzeigenameAm(damals, uhr.GetUtcNow()));
        var fassungen = await daten.ListeAsync(daten.Menge<AntragFassung>().Where(f => f.AntragId == id).OrderBy(f => f.Nummer).ThenBy(f => f.Id), abbruch);
        var kommentare = await daten.ListeAsync(daten.Menge<AntragKommentar>().Where(k => k.AntragId == id).OrderBy(k => k.Am).ThenBy(k => k.Id), abbruch);
        var vorschlaege = await daten.ListeAsync(daten.Menge<Aenderungsvorschlag>().Where(v => v.AntragId == id).OrderBy(v => v.Am).ThenBy(v => v.Id), abbruch);
        var gremium = a.ZielGremiumId is { } g ? await stammdaten.GremiumAsync(g, abbruch) : null;
        var sitzung = a.SitzungTerminId is { } t ? await termine.TerminAsync(t, abbruch) : null;
        var istAutorOderVorstand = wer.PersonId == a.AutorPersonId || Rechte.SitzungenSteuern(wer.Rolle).Erlaubt;
        var textFrei = !a.Status.TextAenderbar() ? Freigabe.Nein($"Im Status „{a.Status.Anzeigename()}“ ist der Text festgeschrieben. Änderungen nur über einen neuen Entwurf.")
            : wer.Rolle is Rolle.Helferin ? Freigabe.Nein("Das Kampagnenteam schreibt nicht an Anträgen mit.") : Freigabe.Ja;
        var vorschlagEntscheiden = !a.Status.TextAenderbar() ? Freigabe.Nein("Der Text ist festgeschrieben.")
            : istAutorOderVorstand ? Freigabe.Ja : Freigabe.Nein("Über Änderungsvorschläge entscheidet die Autorin/der Autor oder der Vorstand.");
        return new AntragDetail(a, namen.GetValueOrDefault(a.AutorPersonId, "?"), gremium?.Name, sitzung, fassungen,
            kommentare.Select(k => new KommentarZeile(k, namen.GetValueOrDefault(k.PersonId, "?"))).ToList(),
            vorschlaege.Select(v => new VorschlagZeile(v, namen.GetValueOrDefault(v.PersonId, "?"),
                v.Status != Vorschlagsstatus.Offen ? Freigabe.Nein("Bereits entschieden.") : vorschlagEntscheiden)).ToList(),
            a.AbstimmungId is { } u ? await umfragen.DetailAsync(u, abbruch) : null,
            textFrei,
            a.Status != Antragsstatus.Entwurf ? Freigabe.Nein("Abstimmen lässt sich ein Antrag im Status „Entwurf“.")
                : !Rechte.SitzungenSteuern(wer.Rolle).Erlaubt ? Freigabe.Nein("Abstimmungen eröffnet der Vorstand.")
                : vorschlaege.Any(v => v.Status == Vorschlagsstatus.Offen) ? Freigabe.Nein("Erst über die offenen Änderungsvorschläge entscheiden.")
                : Freigabe.Ja,
            Statusschritte(a, wer, istAutorOderVorstand));
    }

    private static List<Statusschritt> Statusschritte(Antrag a, Benutzer wer, bool istAutorOderVorstand)
    {
        var vorstand = Rechte.SitzungenSteuern(wer.Rolle);
        var schritte = new List<Statusschritt>();
        foreach (var ziel in Antrag.MoeglicheUebergaenge(a.Status))
        {
            var (titel, freigabe) = ziel switch
            {
                Antragsstatus.Entwurf when a.Status == Antragsstatus.Idee => ("Zum Entwurf machen", istAutorOderVorstand ? Freigabe.Ja : Freigabe.Nein("Nur Autorin/Autor oder Vorstand.")),
                Antragsstatus.Entwurf => ("Zurück in den Entwurf", vorstand),
                Antragsstatus.Idee => ("Zurück zur Idee", istAutorOderVorstand ? Freigabe.Ja : Freigabe.Nein("Nur Autorin/Autor oder Vorstand.")),
                Antragsstatus.InAbstimmung => ("Abstimmung starten", Freigabe.Nein("Bitte über „Fraktionsabstimmung starten“ mit Frist und Quorum.")),
                Antragsstatus.Beschlossen or Antragsstatus.Abgelehnt => ("Abstimmung abschließen", Freigabe.Nein("Ergibt sich aus der Abstimmung (Abschließen oder Fristablauf).")),
                Antragsstatus.Eingereicht => ("Beim Rat einreichen", vorstand),
                Antragsstatus.ImGremium => ("Im Gremium beraten", vorstand),
                Antragsstatus.Erledigt => ("Als erledigt markieren", vorstand),
                Antragsstatus.Zurueckgezogen => ("Zurückziehen", istAutorOderVorstand ? Freigabe.Ja : Freigabe.Nein("Nur Autorin/Autor oder Vorstand.")),
                _ => (ziel.Anzeigename(), vorstand),
            };
            schritte.Add(new Statusschritt(ziel, titel, freigabe));
        }

        return schritte;
    }

    public async Task<Antrag> AnlegenAsync(string titel, string text, Guid? zielGremiumId, string? thema, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            throw new InvalidOperationException("Das Kampagnenteam schreibt nicht an Anträgen mit.");
        }

        var jetzt = uhr.GetUtcNow();
        var a = new Antrag(titel, wer.PersonIdPflicht, zielGremiumId, thema, jetzt);
        daten.Hinzufuegen(a);
        daten.Hinzufuegen(new AntragFassung(a.Id, 1, text, wer.PersonIdPflicht, jetzt, "Erste Fassung"));
        await protokoll.ProtokollierenAsync("Antrag.Angelegt", "Antrag angelegt", nameof(Antrag), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await bilder.VerwendungenAbgleichenAsync("Antrag", a.Id, text, abbruch);
        return a;
    }

    public async Task<AntragFassung> FassungSpeichernAsync(Guid antragId, string text, string? notiz, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        if (d.TextBearbeiten is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (d.Aktuell.Text == text)
        {
            throw new InvalidOperationException("Der Text ist unverändert.");
        }

        var wer = await benutzer.ErmittelnAsync(abbruch);
        var f = new AntragFassung(antragId, d.Aktuell.Nummer + 1, text, wer.PersonIdPflicht, uhr.GetUtcNow(), notiz);
        daten.Hinzufuegen(f);
        (await daten.LadenAsync<Antrag>(antragId, "Antrag", abbruch)).Umbenennen(d.Antrag.Titel, d.Antrag.Thema, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Antrag.Fassung", $"Fassung {f.Nummer} gespeichert", nameof(Antrag), antragId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await bilder.VerwendungenAbgleichenAsync("Antrag", antragId, text, abbruch);
        return f;
    }

    public async Task KommentierenAsync(Guid antragId, string? zitat, string text, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        daten.Hinzufuegen(new AntragKommentar(antragId, d.Aktuell.Nummer, wer.PersonIdPflicht, zitat, text, uhr.GetUtcNow()));
        await daten.SpeichernAsync(abbruch);
    }

    public async Task KommentarErledigtAsync(Guid kommentarId, CancellationToken abbruch = default)
    {
        (await daten.LadenAsync<AntragKommentar>(kommentarId, "Kommentar", abbruch)).AlsErledigtMarkieren();
        await daten.SpeichernAsync(abbruch);
    }

    public async Task VorschlagenAsync(Guid antragId, string alt, string neu, string? begruendung, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        if (!d.Antrag.Status.TextAenderbar())
        {
            throw new InvalidOperationException("Der Text ist festgeschrieben; Vorschläge sind nicht mehr möglich.");
        }

        var v = new Aenderungsvorschlag(antragId, d.Aktuell.Nummer, wer.PersonIdPflicht, alt, neu, begruendung, uhr.GetUtcNow());
        v.Anwenden(d.Aktuell.Text); // prüft, dass die Stelle eindeutig vorkommt
        daten.Hinzufuegen(v);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Annehmen erzeugt eine neue Fassung mit der ersetzten Stelle.</summary>
    public async Task VorschlagEntscheidenAsync(Guid vorschlagId, bool annehmen, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var v = await daten.LadenAsync<Aenderungsvorschlag>(vorschlagId, "Vorschlag", abbruch);
        var d = await DetailAsync(v.AntragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        if (d.Vorschlaege.Single(x => x.Vorschlag.Id == vorschlagId).Entscheiden is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (annehmen)
        {
            var text = v.Anwenden(d.Aktuell.Text);
            daten.Hinzufuegen(new AntragFassung(v.AntragId, d.Aktuell.Nummer + 1, text, wer.PersonIdPflicht, uhr.GetUtcNow(), "Änderungsvorschlag übernommen"));
        }

        v.Entscheiden(annehmen, wer.PersonIdPflicht);
        await protokoll.ProtokollierenAsync("Antrag.Vorschlag", $"Änderungsvorschlag {(annehmen ? "angenommen" : "abgelehnt")}", nameof(Antrag), v.AntragId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<Antragsergebnis> AbstimmungStartenAsync(Guid antragId, DateTimeOffset frist, int quorumProzent, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        if (d.AbstimmungStarten is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var u = await umfragen.AnlegenAsync(new NeueUmfrage($"Antrag: {d.Antrag.Titel}", $"Abgestimmt wird Fassung {d.Aktuell.Nummer}.", Umfrageart.Antragsabstimmung,
            frist, quorumProzent, false, []), antragId, abbruch);
        (await daten.LadenAsync<Antrag>(antragId, "Antrag", abbruch)).AbstimmungZuordnen(u.Id, uhr.GetUtcNow());
        await daten.SpeichernAsync(abbruch);
        return new Antragsergebnis($"Fraktionsabstimmung gestartet (Frist {Ortszeit.AlsOrtszeit(frist):dd.MM. HH:mm} Uhr, Quorum {quorumProzent} %).",
            [new NaechsterSchritt("Zur Abstimmung", $"abstimmungen/{u.Id}", Hauptschritt: true)]);
    }

    public async Task<Antragsergebnis> StatusWechselnAsync(Guid antragId, Antragsstatus ziel, string? vermerk, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        var schritt = d.Statusschritte.FirstOrDefault(s => s.Ziel == ziel) ?? throw new InvalidOperationException("Dieser Statuswechsel ist nicht möglich.");
        if (schritt.Freigabe is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var a = await daten.LadenAsync<Antrag>(antragId, "Antrag", abbruch);
        var jetzt = uhr.GetUtcNow();
        if (ziel == Antragsstatus.Eingereicht)
        {
            var jahr = Ortszeit.Heute(uhr).Year;
            var bisher = await daten.AnzahlAsync(daten.Menge<Antrag>().Where(x => x.Nummer != null && x.Nummer.StartsWith($"A-{jahr}-")), abbruch);
            a.Einreichen($"A-{jahr}-{bisher + 1:D3}", jetzt);
        }
        else
        {
            a.Wechseln(ziel, jetzt, vermerk);
        }

        await protokoll.ProtokollierenAsync("Antrag.Status", $"Status: {ziel.Anzeigename()}", nameof(Antrag), antragId, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return new Antragsergebnis($"Status: {ziel.Anzeigename()}.", Folgeschritte(a));
    }

    public async Task ZuordnenAsync(Guid antragId, Guid? gremiumId, Guid? sitzungTerminId, CancellationToken abbruch = default)
    {
        (await daten.LadenAsync<Antrag>(antragId, "Antrag", abbruch)).Zuordnen(gremiumId, sitzungTerminId, uhr.GetUtcNow());
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Kommende Sitzungen des Zielgremiums (für „der passenden Ratssitzung zuordnen“).</summary>
    public async Task<IReadOnlyList<Termin>> PassendeSitzungenAsync(Guid antragId, CancellationToken abbruch = default)
    {
        var a = await daten.LadenAsync<Antrag>(antragId, "Antrag", abbruch);
        var ab = Ortszeit.Jetzt(uhr);
        return (await termine.TermineAsync(ab, ab.AddDays(120), abbruch))
            .Where(t => t.Art.IstGremiensitzung() && (a.ZielGremiumId is null || t.GremiumId == a.ZielGremiumId)).Take(12).ToList();
    }

    /// <summary>Druckfassung im Fraktionslayout: aktuelle Fassung mit Kopf und Begründungsteil.</summary>
    public async Task<(string Datei, byte[] Pdf)> PdfAsync(Guid antragId, CancellationToken abbruch = default)
    {
        var d = await DetailAsync(antragId, abbruch) ?? throw new InvalidOperationException("Antrag nicht gefunden.");
        var kopf = $"{(d.Antrag.Nummer is null ? "Entwurf" : $"Antrag {d.Antrag.Nummer}")} · {optionen.Value.Anzeigename} · "
                   + $"{(d.Gremium is null ? "" : $"an: {d.Gremium} · ")}{Ortszeit.AlsOrtszeit(d.Antrag.GeaendertAm).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)}";
        var bytes = pdf.Erzeugen(new PdfDokument(d.Antrag.Titel, kopf,
            $"An die Bürgermeisterin / den Bürgermeister der Stadt {optionen.Value.Stadt}\n\n{d.Aktuell.Text}\n\nFür die Fraktion: {d.Autor}",
            await bilder.InhalteAsync(d.Aktuell.Text, abbruch)));
        return ($"antrag-{d.Antrag.Nummer ?? "entwurf"}.pdf", bytes);
    }

    private static List<NaechsterSchritt> Folgeschritte(Antrag a) => a.Status switch
    {
        Antragsstatus.Beschlossen =>
        [
            new("Druckfassung (PDF) erzeugen", $"antraege/{a.Id}/antrag.pdf"),
            new("Beim Rat einreichen", $"antraege/{a.Id}", Hauptschritt: true),
            new("Der passenden Ratssitzung zuordnen", $"antraege/{a.Id}#zuordnen"),
        ],
        Antragsstatus.Eingereicht =>
        [
            new("Druckfassung (PDF) erzeugen", $"antraege/{a.Id}/antrag.pdf", Hauptschritt: true),
            new("Der passenden Ratssitzung zuordnen", $"antraege/{a.Id}#zuordnen"),
        ],
        _ => [new("Zur Antragsliste", "antraege")],
    };
}
