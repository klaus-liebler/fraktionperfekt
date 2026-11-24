using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Wissen;

namespace FraktionPerfekt.Application.Wissen;

public sealed record WikiZeile(WikiSeite Seite, int Rueckverweise, int OffeneVorschlaege);

public sealed record WikiDetail(WikiSeite Seite, WikiVersion Aktuell, IReadOnlyList<WikiVersion> Versionen, IReadOnlyList<WikiSeite> Rueckverweise,
    IReadOnlyList<string> FehlendeVerweise, IReadOnlyList<WikiVorschlag> Vorschlaege, Freigabe Bearbeiten);

public sealed record VorschlagZeile(WikiVorschlag Vorschlag, WikiSeite Seite, string AktuellerText, bool Veraltet, Freigabe Entscheiden);

public sealed record Suchtreffer(WikiSeite Seite, string Ausschnitt);

/// <summary>
/// Wissensbereich (Modul 20): Seiten mit Versionen, Vorlagen, Verlinkung [[Titel]], Volltextsuche (FTS5), Rechte nach Gremium.
/// KI-Schnittstelle: Die KI arbeitet mit eigenem Konto, sieht nur nicht vertrauliche Seiten und liefert ausschließlich
/// Änderungsvorschläge mit Vorher-nachher-Ansicht; ein Mensch (Vorstand, Administration, Autor:in) nimmt an.
/// </summary>
public sealed class WikiDienst(
    IDaten daten,
    IServiceProvider dienste,
    IWikiSuche suche,
    IKiAssistent ki,
    IStammdatenSpeicher stammdaten,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string KiKonto = "ki";

    public static readonly string[] Bereiche = ["Abläufe", "Politikfelder", "Gremien", "Positionen", "Hintergrund"];

    public async Task<IReadOnlyList<WikiZeile>> SeitenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var seiten = await SichtbareAsync(wer, abbruch);
        var texte = await AktuelleTexteAsync(seiten, abbruch);
        var offen = await daten.ListeAsync(daten.Menge<WikiVorschlag>().Where(v => v.Status == Wikivorschlagsstatus.Offen).Select(v => v.SeiteId), abbruch);
        return seiten.Select(s => new WikiZeile(s, texte.Count(t => t.Key != s.Id && WikiSeite.Verweise(t.Value).Contains(s.Titel, StringComparer.CurrentCultureIgnoreCase)),
            offen.Count(o => o == s.Id))).ToList();
    }

    public async Task<WikiDetail?> SeiteAsync(string titelOderId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var seiten = await SichtbareAsync(wer, abbruch);
        var seite = Guid.TryParse(titelOderId, out var id)
            ? seiten.FirstOrDefault(s => s.Id == id)
            : seiten.FirstOrDefault(s => string.Equals(s.Titel, titelOderId, StringComparison.CurrentCultureIgnoreCase));
        if (seite is null)
        {
            return null;
        }

        var versionen = await daten.ListeAsync(daten.Menge<WikiVersion>().Where(v => v.SeiteId == seite.Id).OrderBy(v => v.Nummer).ThenBy(v => v.Id), abbruch);
        var texte = await AktuelleTexteAsync(seiten, abbruch);
        var titel = seiten.Select(s => s.Titel).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        return new WikiDetail(seite, versionen[^1], versionen,
            seiten.Where(s => s.Id != seite.Id && WikiSeite.Verweise(texte.GetValueOrDefault(s.Id, "")).Contains(seite.Titel, StringComparer.CurrentCultureIgnoreCase)).ToList(),
            WikiSeite.Verweise(versionen[^1].Text).Where(v => !titel.Contains(v)).ToList(),
            await daten.ListeAsync(daten.Menge<WikiVorschlag>().Where(v => v.SeiteId == seite.Id && v.Status == Wikivorschlagsstatus.Offen).OrderBy(v => v.Am).ThenBy(v => v.Id), abbruch),
            wer.Rolle is Rolle.Helferin ? Freigabe.Nein("Das Kampagnenteam liest das Wiki, bearbeitet es aber nicht.") : Freigabe.Ja);
    }

    public async Task<WikiSeite> AnlegenAsync(string bereich, string titel, string vorlage, bool vertraulich, Guid? gremiumId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            throw new InvalidOperationException("Das Kampagnenteam legt keine Wiki-Seiten an.");
        }

        if ((await daten.ListeAsync(daten.Menge<WikiSeite>().Select(s => s.Titel), abbruch)).Any(t => WikiSeite.Normalisiert(t) == WikiSeite.Normalisiert(titel)))
        {
            throw new InvalidOperationException($"Eine Seite „{titel}“ gibt es schon.");
        }

        var jetzt = uhr.GetUtcNow();
        var seite = new WikiSeite(bereich, titel, gremiumId, vertraulich, jetzt);
        seite.NeueVersion(jetzt);
        var text = Wikivorlagen.Alle.GetValueOrDefault(vorlage, string.Empty);
        daten.Hinzufuegen(seite);
        daten.Hinzufuegen(new WikiVersion(seite.Id, 1, text, wer.PersonId, $"Angelegt aus Vorlage „{vorlage}“", jetzt));
        await suche.AktualisierenAsync(seite.Id, seite.Titel, text, abbruch);
        await daten.SpeichernAsync(abbruch);
        return seite;
    }

    public async Task SpeichernAsync(Guid seiteId, string text, string? kommentar, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var d = await SeiteAsync(seiteId.ToString(), abbruch) ?? throw new InvalidOperationException("Seite nicht gefunden.");
        if (d.Bearbeiten is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        await NeueVersionAsync(seiteId, text, wer.PersonId, kommentar, null, abbruch);
        await daten.SpeichernAsync(abbruch);
        // Bilder aus dem Text der Seite zuordnen (z. B. aus einer anderen Seite kopiert); spät aufgelöst, BildDienst prüft Rechte über das Wiki
        await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<BildDienst>(dienste)
            .VerwendungenAbgleichenAsync(BildDienst.Wiki, seiteId, text, abbruch);
    }

    public async Task<IReadOnlyList<Suchtreffer>> SuchenAsync(string anfrage, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var seiten = (await SichtbareAsync(wer, abbruch)).ToDictionary(s => s.Id);
        return (await suche.SuchenAsync(anfrage, 50, abbruch)).Where(t => seiten.ContainsKey(t.SeiteId))
            .Select(t => new Suchtreffer(seiten[t.SeiteId], t.Ausschnitt)).ToList();
    }

    /// <summary>Frage an die KI: Antwort mit Verweis auf die Quellseiten (nur freigegebene Bereiche).</summary>
    public async Task<KiAntwort> FragenAsync(string frage, CancellationToken abbruch = default)
    {
        var freigegeben = await KiSeitenAsync(abbruch);
        var treffer = (await suche.SuchenAsync(frage, 10, abbruch)).Select(t => freigegeben.FirstOrDefault(s => s.Id == t.SeiteId)).OfType<KiSeite>().Take(3).ToList();
        return await ki.FragenAsync(frage, treffer, abbruch);
    }

    /// <summary>KI prüft die Wissensfragmente und legt Vorschläge an (je Seite und Art höchstens einer offen).</summary>
    public async Task<int> KiPruefenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && Rechte.SitzungenSteuern(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var seiten = await KiSeitenAsync(abbruch);
        var beschluesse = (await daten.ListeAsync(daten.Menge<Beschluss>().Where(b => b.ZurueckgezogenAm == null).OrderByDescending(b => b.Datum).ThenBy(b => b.Id).Take(100), abbruch))
            .Select(b => new KiBeschluss(b.Datum, b.Titel, b.Text)).ToList();
        var offen = await daten.ListeAsync(daten.Menge<WikiVorschlag>().Where(v => v.Status == Wikivorschlagsstatus.Offen).OrderBy(v => v.Id), abbruch);
        var neu = 0;
        foreach (var v in await ki.PruefenAsync(seiten, beschluesse, Ortszeit.Heute(uhr), abbruch))
        {
            if (offen.Any(o => o.SeiteId == v.SeiteId && o.Art == v.Art) || seiten.FirstOrDefault(s => s.Id == v.SeiteId) is not { } s)
            {
                continue;
            }

            daten.Hinzufuegen(new WikiVorschlag(s.Id, s.Version, v.Art, v.Begruendung, s.Text, v.NeuerText, ki.Name, uhr.GetUtcNow()));
            neu++;
        }

        await protokoll.ProtokollierenAsync("Wiki.KiPruefung", $"{ki.Name}: {neu} neue Vorschläge", abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return neu;
    }

    /// <summary>Fristenprüfung (stündlich): die KI prüft einmal je Woche, montags nach 6 Uhr.</summary>
    public async Task<int> KiPruefenNachtsAsync(CancellationToken abbruch = default)
    {
        var jetzt = Ortszeit.AlsOrtszeit(uhr.GetUtcNow());
        if (jetzt.DayOfWeek != DayOfWeek.Monday || jetzt.Hour != 6)
        {
            return 0;
        }

        return await KiPruefenAsync(abbruch);
    }

    /// <summary>Vorschlag über die API (KI-Konto) anlegen; die KI ändert nie direkt.</summary>
    public async Task<WikiVorschlag> VorschlagAnlegenAsync(Guid seiteId, Vorschlagsart art, string begruendung, string? neuerText, string von,
        CancellationToken abbruch = default)
    {
        var s = (await KiSeitenAsync(abbruch)).FirstOrDefault(x => x.Id == seiteId) ?? throw new InvalidOperationException("Seite nicht gefunden oder nicht freigegeben.");
        var v = new WikiVorschlag(seiteId, s.Version, art, begruendung, s.Text, neuerText, von, uhr.GetUtcNow());
        daten.Hinzufuegen(v);
        await protokoll.ProtokollierenAsync("Wiki.Vorschlag", $"Vorschlag ({art}) von {von}", nameof(WikiVorschlag), v.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return v;
    }

    public async Task<IReadOnlyList<VorschlagZeile>> VorschlaegeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var seiten = (await SichtbareAsync(wer, abbruch)).ToDictionary(s => s.Id);
        var texte = await AktuelleTexteAsync(seiten.Values.ToList(), abbruch);
        var entscheiden = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt
            ? Freigabe.Ja
            : Freigabe.Nein("Über KI-Vorschläge entscheiden Vorstand und Geschäftsführung (verantwortliche Personen).");
        return (await daten.ListeAsync(daten.Menge<WikiVorschlag>().Where(v => v.Status == Wikivorschlagsstatus.Offen).OrderBy(v => v.Am).ThenBy(v => v.Id), abbruch))
            .Where(v => seiten.ContainsKey(v.SeiteId))
            .Select(v => new VorschlagZeile(v, seiten[v.SeiteId], texte.GetValueOrDefault(v.SeiteId, ""), seiten[v.SeiteId].Version != v.BasisVersion,
                seiten[v.SeiteId].Version != v.BasisVersion && v.Nachher is not null
                    ? Freigabe.Nein("Die Seite wurde inzwischen geändert; der Vorschlag bezieht sich auf eine ältere Version. Bitte ablehnen und neu prüfen lassen.")
                    : entscheiden))
            .ToList();
    }

    public async Task VorschlagEntscheidenAsync(Guid vorschlagId, bool annehmen, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var zeile = (await VorschlaegeAsync(abbruch)).FirstOrDefault(z => z.Vorschlag.Id == vorschlagId) ?? throw new InvalidOperationException("Vorschlag nicht gefunden.");
        if (annehmen && zeile.Entscheiden is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        if (!annehmen && Rechte.SitzungenSteuern(wer.Rolle) is { Erlaubt: false } nein2)
        {
            throw new InvalidOperationException(nein2.Begruendung);
        }

        var v = await daten.LadenAsync<WikiVorschlag>(vorschlagId, "Vorschlag", abbruch);
        v.Entscheiden(annehmen, wer.PersonIdPflicht, uhr.GetUtcNow());
        if (annehmen && v.Nachher is { } text)
        {
            await NeueVersionAsync(v.SeiteId, text, null, $"KI-Vorschlag übernommen ({v.Art}): {v.Begruendung}", v.Id, abbruch);
        }

        await protokoll.ProtokollierenAsync("Wiki.VorschlagEntschieden", $"Vorschlag ({v.Art}) {(annehmen ? "angenommen" : "abgelehnt")}", nameof(WikiVorschlag), v.Id,
            abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Was die KI sehen darf: nicht vertraulich, nicht archiviert.</summary>
    public async Task<IReadOnlyList<KiSeite>> KiSeitenAsync(CancellationToken abbruch = default)
    {
        var seiten = await daten.ListeAsync(daten.Menge<WikiSeite>().Where(s => !s.Vertraulich && !s.Archiviert).OrderBy(s => s.Bereich).ThenBy(s => s.Titel).ThenBy(s => s.Id), abbruch);
        var texte = await AktuelleTexteAsync(seiten, abbruch);
        return seiten.Select(s => new KiSeite(s.Id, s.Bereich, s.Titel, texte.GetValueOrDefault(s.Id, ""), s.GeaendertAm, s.Version,
            WikiSeite.Verweise(texte.GetValueOrDefault(s.Id, "")))).ToList();
    }

    private async Task NeueVersionAsync(Guid seiteId, string text, Guid? personId, string? kommentar, Guid? vorschlagId, CancellationToken abbruch)
    {
        var seite = await daten.LadenAsync<WikiSeite>(seiteId, "Seite", abbruch);
        var letzte = await daten.ListeAsync(daten.Menge<WikiVersion>().Where(v => v.SeiteId == seiteId).OrderByDescending(v => v.Nummer).ThenBy(v => v.Id).Take(1), abbruch);
        if (letzte.Count > 0)
        {
            seite.StandAusVersion(letzte[0].Nummer, letzte[0].Am);
        }

        seite.NeueVersion(uhr.GetUtcNow());
        daten.Hinzufuegen(new WikiVersion(seite.Id, seite.Version, text, personId, kommentar, uhr.GetUtcNow(), vorschlagId));
        await suche.AktualisierenAsync(seite.Id, seite.Titel, text, abbruch);
    }

    private async Task<List<WikiSeite>> SichtbareAsync(Benutzer wer, CancellationToken abbruch)
    {
        var alle = await daten.ListeAsync(daten.Menge<WikiSeite>().Where(s => !s.Archiviert).OrderBy(s => s.Bereich).ThenBy(s => s.Titel).ThenBy(s => s.Id), abbruch);
        if (wer.Kennung == KiKonto)
        {
            return alle.Where(s => !s.Vertraulich).ToList();
        }

        if (wer.Rolle is Rolle.Vorstand or Rolle.Administration)
        {
            return alle;
        }

        var meine = wer.PersonId is { } p
            ? (await stammdaten.MitgliedschaftenAsync(abbruch)).Where(m => m.PersonId == p && m.StandAm(Ortszeit.Heute(uhr), uhr.GetUtcNow()) is not null).Select(m => m.GremiumId).ToHashSet()
            : [];
        return alle.Where(s => !s.Vertraulich || (s.GremiumId is { } g && meine.Contains(g))).ToList();
    }

    private async Task<Dictionary<Guid, string>> AktuelleTexteAsync(IReadOnlyCollection<WikiSeite> seiten, CancellationToken abbruch)
    {
        var ids = seiten.Select(s => s.Id).ToList();
        var versionen = await daten.ListeAsync(daten.Menge<WikiVersion>().Where(v => ids.Contains(v.SeiteId)).OrderBy(v => v.SeiteId).ThenBy(v => v.Nummer).ThenBy(v => v.Id), abbruch);
        var neueste = versionen.GroupBy(v => v.SeiteId).ToDictionary(g => g.Key, g => g.Last());
        foreach (var s in seiten)
        {
            if (neueste.TryGetValue(s.Id, out var v))
            {
                s.StandAusVersion(v.Nummer, v.Am);   // Version und Änderungszeit stehen nur in den Versionen
            }
        }

        return neueste.ToDictionary(x => x.Key, x => x.Value.Text);
    }
}
