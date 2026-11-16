using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Politik;
using FraktionPerfekt.Domain.Sitzungen;

namespace FraktionPerfekt.Application.Politik;

internal static class Politikrechte
{
    public static Freigabe Mitarbeiten(Rolle? r) => r is Rolle.Helferin or null
        ? Freigabe.Nein("Nur für Mitglieder, sachkundige Bürger:innen, Vorstand und Geschäftsführung.")
        : Freigabe.Ja;

    public static void Pruefen(Freigabe f)
    {
        if (!f.Erlaubt)
        {
            throw new InvalidOperationException(f.Begruendung);
        }
    }
}

public sealed record AnfrageZeile(Anfrage Anfrage, string Autor, string? Gremium, bool Ueberfaellig, Freigabe Einreichen, Freigabe Beantworten);

/// <summary>
/// Anfragen an die Verwaltung (Modul 13). Die Fraktion reicht über Vorstand oder Geschäftsführung ein; die Antwortfrist
/// (Standard vier Wochen) wird überwacht. Überfällig: einmal Push an die Autor:in und Aufgabe „nachhaken“ im Korb Administration.
/// </summary>
public sealed class AnfrageDienst(
    IDaten daten,
    Wissen.BildDienst bilder,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Anfrage";

    public const int StandardFristTage = 28;

    public async Task<IReadOnlyList<AnfrageZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return [];
        }

        var heute = Ortszeit.Heute(uhr);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id, g => g.Kurzname);
        var einreichen = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt ? Freigabe.Ja : Freigabe.Nein("Anfragen reicht der Vorstand oder die Geschäftsführung im Namen der Fraktion ein.");
        return (await daten.ListeAsync(daten.Menge<Anfrage>().OrderByDescending(a => a.ErstelltAm).ThenBy(a => a.Id), abbruch))
            .Select(a => new AnfrageZeile(a, namen.GetValueOrDefault(a.AutorPersonId, "?"), a.GremiumId is { } g ? gremien.GetValueOrDefault(g) : null, a.Ueberfaellig(heute),
                a.Status != Anfragestatus.Entwurf ? Freigabe.Nein("Bereits eingereicht.") : einreichen,
                a.Status != Anfragestatus.Eingereicht ? Freigabe.Nein("Nur eingereichte Anfragen bekommen eine Antwort.")
                    : a.AutorPersonId == wer.PersonId || einreichen.Erlaubt ? Freigabe.Ja : Freigabe.Nein("Die Antwort erfassen Autor:in, Vorstand oder Geschäftsführung.")))
            .ToList();
    }

    public async Task<Anfrage> AnlegenAsync(string titel, string text, Guid? gremiumId, string? thema, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Politikrechte.Mitarbeiten(wer.Rolle));
        var a = new Anfrage(titel, text, wer.PersonIdPflicht, gremiumId, thema, uhr.GetUtcNow());
        daten.Hinzufuegen(a);
        await protokoll.ProtokollierenAsync("Anfrage.Angelegt", "Anfrage an die Verwaltung angelegt", nameof(Anfrage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await bilder.VerwendungenAbgleichenAsync("Anfrage", a.Id, text, abbruch);
        return a;
    }

    public async Task EinreichenAsync(Guid id, DateOnly? frist, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Rechte.SitzungenSteuern(wer.Rolle));
        var a = await daten.LadenAsync<Anfrage>(id, "Anfrage", abbruch);
        var heute = Ortszeit.Heute(uhr);
        a.Einreichen(heute, frist ?? heute.AddDays(StandardFristTage));
        await protokoll.ProtokollierenAsync("Anfrage.Eingereicht", $"Anfrage eingereicht, Frist {a.FristBis:dd.MM.yyyy}", nameof(Anfrage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task BeantwortenAsync(Guid id, string antwort, CancellationToken abbruch = default)
    {
        await BeantwortenOhneBilderAsync(id, antwort, abbruch);
        await bilder.VerwendungenAbgleichenAsync("Anfrage", id, antwort, abbruch);
    }

    private async Task BeantwortenOhneBilderAsync(Guid id, string antwort, CancellationToken abbruch)
    {
        var zeile = (await ListeAsync(abbruch)).FirstOrDefault(z => z.Anfrage.Id == id) ?? throw new InvalidOperationException("Anfrage nicht gefunden.");
        Politikrechte.Pruefen(zeile.Beantworten);
        var a = await daten.LadenAsync<Anfrage>(id, "Anfrage", abbruch);
        a.Beantworten(antwort, Ortszeit.Heute(uhr));
        await AufgabenSchliessenAsync(id, "Antwort eingegangen", abbruch);
        if (await stammdaten.PersonAsync(a.AutorPersonId, abbruch) is { } autor)
        {
            await postausgang.PushEinreihenAsync(autor, "Antwort der Verwaltung", a.Titel, "anfragen", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Anfrage.Beantwortet", "Antwort der Verwaltung erfasst", nameof(Anfrage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Fristenprüfung: überfällige Anfragen einmal anmahnen.</summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var faellig = (await daten.ListeAsync(daten.Menge<Anfrage>().Where(a => a.Status == Anfragestatus.Eingereicht && a.ErinnertAm == null).OrderBy(a => a.Id), abbruch))
            .Where(a => a.Ueberfaellig(heute)).ToList();
        foreach (var a in faellig)
        {
            a.ErinnerungVermerken(uhr.GetUtcNow());
            if (await stammdaten.PersonAsync(a.AutorPersonId, abbruch) is { } autor)
            {
                await postausgang.PushEinreihenAsync(autor, "Anfrage unbeantwortet", $"{a.Titel} – Frist {a.FristBis:dd.MM.} verstrichen", "anfragen", abbruch: abbruch);
            }

            await aufgaben.AnlegenAsync($"Bei der Verwaltung nachhaken: {a.Titel}", $"Antwortfrist {a.FristBis:dd.MM.yyyy} verstrichen.", null, Rolle.Administration,
                AufgabenAnlass, a.Id, "anfragen", heute.AddDays(3), a.Thema, abbruch);
        }

        await daten.SpeichernAsync(abbruch);
        return faellig.Count;
    }

    private async Task AufgabenSchliessenAsync(Guid anlassId, string vermerk, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        foreach (var a in await daten.ListeAsync(daten.Menge<Aufgabe>().Where(x => x.Anlass == AufgabenAnlass && x.AnlassId == anlassId && x.Status == Aufgabenstatus.Offen)
                     .OrderBy(x => x.Id), abbruch))
        {
            a.Erledigen(uhr.GetUtcNow(), wer.PersonId, vermerk);
        }
    }
}

public sealed record BeschlussZeile(Beschluss Beschluss, string? Gremium);

/// <summary>Beschlusslage-Archiv (Modul 12): Beschlüsse aus eigenen Protokollen und dem RIS, durchsuchbar.</summary>
public sealed class BeschlussArchiv(IDaten daten, IStammdatenSpeicher stammdaten, IAktuellerBenutzer benutzer)
{
    public async Task<IReadOnlyList<BeschlussZeile>> SuchenAsync(string? text, Beschlussquelle? quelle, Guid? gremiumId, int? jahr, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin)
        {
            return [];
        }

        var abfrage = daten.Menge<Beschluss>();
        if (quelle is { } q)
        {
            abfrage = abfrage.Where(b => b.Quelle == q);
        }

        if (gremiumId is { } g)
        {
            abfrage = abfrage.Where(b => b.GremiumId == g);
        }

        var gremien = (await stammdaten.GremienAsync(abbruch)).ToDictionary(x => x.Id, x => x.Kurzname);
        var woerter = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (await daten.ListeAsync(abfrage.OrderByDescending(b => b.Datum).ThenBy(b => b.Id), abbruch))
            .Where(b => jahr is null || b.Datum.Year == jahr)
            .Where(b => woerter.All(w => b.Titel.Contains(w, StringComparison.CurrentCultureIgnoreCase) || b.Text.Contains(w, StringComparison.CurrentCultureIgnoreCase)))
            .Select(b => new BeschlussZeile(b, b.GremiumId is { } id ? gremien.GetValueOrDefault(id) : null))
            .ToList();
    }
}

public sealed record KontaktZeile(Kontakt Kontakt, Freigabe Loeschen);

/// <summary>Kontakte (Modul 14): gemeinsames Adressbuch der Fraktion.</summary>
public sealed class KontaktDienst(IDaten daten, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr)
{
    public async Task<IReadOnlyList<KontaktZeile>> ListeAsync(string? suche, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!Politikrechte.Mitarbeiten(wer.Rolle).Erlaubt)
        {
            return [];
        }

        var woerter = (suche ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (await daten.ListeAsync(daten.Menge<Kontakt>().OrderBy(k => k.Name).ThenBy(k => k.Id), abbruch))
            .Where(k => woerter.All(w => $"{k.Name} {k.Organisation} {k.Funktion} {k.Themen}".Contains(w, StringComparison.CurrentCultureIgnoreCase)))
            .Select(k => new KontaktZeile(k, k.AngelegtVonPersonId == wer.PersonId || wer.Rolle is Rolle.Administration
                ? Freigabe.Ja
                : Freigabe.Nein("Löschen kann, wer angelegt hat, oder die Geschäftsführung.")))
            .ToList();
    }

    public async Task<Kontakt> SpeichernAsync(Guid? id, string name, string? organisation, string? funktion, string? eMail, string? telefon, string? themen, string? notiz,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Politikrechte.Mitarbeiten(wer.Rolle));
        Kontakt k;
        if (id is { } vorhanden)
        {
            k = await daten.LadenAsync<Kontakt>(vorhanden, "Kontakt", abbruch);
            k.Aendern(name, organisation, funktion, eMail, telefon, themen, notiz, uhr.GetUtcNow());
        }
        else
        {
            k = new Kontakt(name, organisation, funktion, eMail, telefon, themen, notiz, wer.PersonIdPflicht, uhr.GetUtcNow());
            daten.Hinzufuegen(k);
        }

        await protokoll.ProtokollierenAsync(id is null ? "Kontakt.Angelegt" : "Kontakt.Geaendert", "Kontakt gespeichert", nameof(Kontakt), k.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return k;
    }

    public async Task LoeschenAsync(Guid id, CancellationToken abbruch = default)
    {
        var zeile = (await ListeAsync(null, abbruch)).FirstOrDefault(z => z.Kontakt.Id == id) ?? throw new InvalidOperationException("Kontakt nicht gefunden.");
        Politikrechte.Pruefen(zeile.Loeschen);
        daten.Entfernen(zeile.Kontakt);
        await protokoll.ProtokollierenAsync("Kontakt.Geloescht", "Kontakt gelöscht", nameof(Kontakt), id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }
}

public sealed record AnliegenZeile(Buergeranliegen Anliegen, string? Zustaendig, string? Vorschlag, Guid? VorschlagPersonId, Freigabe Zuweisen, Freigabe Bearbeiten);

/// <summary>
/// Bürgeranliegen (Modul 15): Eingang → Aufgabe „zuweisen“ im Korb Administration → Zuweisung (Vorschlag: Themenpate) mit
/// Aufgabe und Push → Antwort erledigt alles. Löschfrist: Name und Kontaktweg zwölf Monate nach Erledigung entfernen.
/// </summary>
public sealed class AnliegenDienst(
    IDaten daten,
    Wissen.BildDienst bilder,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Anliegen";

    public const int LoeschfristMonate = 12;

    public async Task<IReadOnlyList<AnliegenZeile>> ListeAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!Politikrechte.Mitarbeiten(wer.Rolle).Erlaubt)
        {
            return [];
        }

        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var paten = await aufgaben.ThemenpatenAsync(abbruch);
        var leitung = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt;
        return (await daten.ListeAsync(daten.Menge<Buergeranliegen>().OrderByDescending(a => a.Eingang).ThenBy(a => a.Id), abbruch))
            .Select(a =>
            {
                var pate = a.Thema is null ? null : paten.FirstOrDefault(p => string.Equals(p.Pate.Politikfeld, a.Thema, StringComparison.CurrentCultureIgnoreCase));
                return new AnliegenZeile(a, a.ZugewiesenPersonId is { } z ? namen.GetValueOrDefault(z) : null, pate?.Person, pate?.Pate.PersonId,
                    a.Status == Anliegenstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.") : leitung ? Freigabe.Ja : Freigabe.Nein("Zuweisen können Vorstand und Geschäftsführung."),
                    a.Status == Anliegenstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.")
                    : a.ZugewiesenPersonId is null ? Freigabe.Nein("Erst zuweisen.")
                    : a.ZugewiesenPersonId == wer.PersonId || leitung ? Freigabe.Ja : Freigabe.Nein("Bearbeiten kann die zuständige Person."));
            })
            .ToList();
    }

    public async Task<Buergeranliegen> ErfassenAsync(string betreff, string text, string name, string? kontakt, Eingangskanal kanal, string? thema,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Politikrechte.Mitarbeiten(wer.Rolle));
        var a = new Buergeranliegen(betreff, text, name, kontakt, kanal, thema, uhr.GetUtcNow());
        daten.Hinzufuegen(a);
        await protokoll.ProtokollierenAsync("Anliegen.Erfasst", $"Bürgeranliegen erfasst ({kanal})", nameof(Buergeranliegen), a.Id, abbruch: abbruch);
        await aufgaben.AnlegenAsync($"Anliegen zuweisen: {a.Betreff}", "Bürgeranliegen eingegangen – bitte einer Person zuweisen.", null, Rolle.Administration,
            AufgabenAnlass, a.Id, "anliegen", Ortszeit.Heute(uhr).AddDays(2), null, abbruch);
        await bilder.VerwendungenAbgleichenAsync("Anliegen", a.Id, text, abbruch);
        return a;
    }

    public async Task ZuweisenAsync(Guid id, Guid personId, CancellationToken abbruch = default)
    {
        var zeile = (await ListeAsync(abbruch)).FirstOrDefault(z => z.Anliegen.Id == id) ?? throw new InvalidOperationException("Anliegen nicht gefunden.");
        Politikrechte.Pruefen(zeile.Zuweisen);
        var a = await daten.LadenAsync<Buergeranliegen>(id, "Anliegen", abbruch);
        a.Zuweisen(personId);
        await AufgabenSchliessenAsync(id, "zugewiesen", abbruch);
        await protokoll.ProtokollierenAsync("Anliegen.Zugewiesen", "Bürgeranliegen zugewiesen", nameof(Buergeranliegen), a.Id, abbruch: abbruch);
        await aufgaben.AnlegenAsync($"Anliegen beantworten: {a.Betreff}", a.Text, personId, null, AufgabenAnlass, a.Id, "anliegen", Ortszeit.Heute(uhr).AddDays(14), a.Thema,
            abbruch);
    }

    public async Task InBearbeitungAsync(Guid id, CancellationToken abbruch = default)
    {
        var zeile = (await ListeAsync(abbruch)).FirstOrDefault(z => z.Anliegen.Id == id) ?? throw new InvalidOperationException("Anliegen nicht gefunden.");
        Politikrechte.Pruefen(zeile.Bearbeiten);
        (await daten.LadenAsync<Buergeranliegen>(id, "Anliegen", abbruch)).InBearbeitung();
        await daten.SpeichernAsync(abbruch);
    }

    public async Task ErledigenAsync(Guid id, string antwort, CancellationToken abbruch = default)
    {
        await ErledigenOhneBilderAsync(id, antwort, abbruch);
        await bilder.VerwendungenAbgleichenAsync("Anliegen", id, antwort, abbruch);
    }

    private async Task ErledigenOhneBilderAsync(Guid id, string antwort, CancellationToken abbruch)
    {
        var zeile = (await ListeAsync(abbruch)).FirstOrDefault(z => z.Anliegen.Id == id) ?? throw new InvalidOperationException("Anliegen nicht gefunden.");
        Politikrechte.Pruefen(zeile.Bearbeiten);
        var a = await daten.LadenAsync<Buergeranliegen>(id, "Anliegen", abbruch);
        a.Erledigen(antwort, uhr.GetUtcNow());
        await AufgabenSchliessenAsync(id, "beantwortet", abbruch);
        await protokoll.ProtokollierenAsync("Anliegen.Erledigt", "Bürgeranliegen beantwortet", nameof(Buergeranliegen), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Löschfrist: erledigte Anliegen nach zwölf Monaten anonymisieren.</summary>
    public async Task<int> LoeschfristenAsync(CancellationToken abbruch = default)
    {
        var grenze = uhr.GetUtcNow().AddMonths(-LoeschfristMonate);
        var alt = (await daten.ListeAsync(daten.Menge<Buergeranliegen>().Where(a => a.Status == Anliegenstatus.Erledigt && !a.Anonymisiert).OrderBy(a => a.Id), abbruch))
            .Where(a => a.ErledigtAm < grenze).ToList();
        foreach (var a in alt)
        {
            a.Anonymisieren();
        }

        if (alt.Count > 0)
        {
            await protokoll.ProtokollierenAsync("Anliegen.Anonymisiert", $"{alt.Count} erledigte Bürgeranliegen nach Löschfrist anonymisiert", abbruch: abbruch);
            await daten.SpeichernAsync(abbruch);
        }

        return alt.Count;
    }

    private async Task AufgabenSchliessenAsync(Guid anlassId, string vermerk, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        foreach (var t in await daten.ListeAsync(daten.Menge<Aufgabe>().Where(x => x.Anlass == AufgabenAnlass && x.AnlassId == anlassId && x.Status == Aufgabenstatus.Offen)
                     .OrderBy(x => x.Id), abbruch))
        {
            t.Erledigen(uhr.GetUtcNow(), wer.PersonId, vermerk);
        }
    }
}

public sealed record PresseZeile(Pressemitteilung Mitteilung, string Autor, int Artikel, Freigabe Bearbeiten, Freigabe Einreichen, Freigabe Freigeben,
    Freigabe Veroeffentlichen);

public sealed record SpiegelZeile(Presseartikel Artikel, string? Mitteilung);

/// <summary>
/// Pressearbeit (Modul 16): Redaktionskalender, Entwürfe mit Freigabe durch den Vorstand (Vier-Augen-Prinzip), Pressespiegel
/// mit Bezug auf eigene Mitteilungen.
/// </summary>
public sealed class PresseDienst(
    IDaten daten,
    Wissen.BildDienst bilder,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Pressemitteilung";

    public async Task<IReadOnlyList<PresseZeile>> MitteilungenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!Politikrechte.Mitarbeiten(wer.Rolle).Erlaubt)
        {
            return [];
        }

        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var artikel = await daten.ListeAsync(daten.Menge<Presseartikel>().Where(a => a.PressemitteilungId != null).Select(a => a.PressemitteilungId!.Value), abbruch);
        var vorstand = wer.Rolle is Rolle.Vorstand;
        var leitung = Rechte.SitzungenSteuern(wer.Rolle).Erlaubt;
        return (await daten.ListeAsync(daten.Menge<Pressemitteilung>().OrderBy(m => m.GeplantAm == null).ThenBy(m => m.GeplantAm).ThenByDescending(m => m.ErstelltAm)
                .ThenBy(m => m.Id), abbruch))
            .Select(m => new PresseZeile(m, namen.GetValueOrDefault(m.AutorPersonId, "?"), artikel.Count(x => x == m.Id),
                m.Status == Pressestatus.Veroeffentlicht ? Freigabe.Nein("Bereits veröffentlicht.")
                : m.AutorPersonId == wer.PersonId || leitung ? Freigabe.Ja : Freigabe.Nein("Bearbeiten können Autor:in, Vorstand und Geschäftsführung."),
                m.Status != Pressestatus.Entwurf ? Freigabe.Nein("Nur Entwürfe gehen zur Freigabe.")
                : m.AutorPersonId == wer.PersonId || leitung ? Freigabe.Ja : Freigabe.Nein("Zur Freigabe gibt die Autor:in."),
                m.Status != Pressestatus.ZurFreigabe ? Freigabe.Nein("Liegt nicht zur Freigabe vor.")
                : !vorstand ? Freigabe.Nein("Pressemitteilungen gibt der Vorstand frei.")
                : m.AutorPersonId == wer.PersonId ? Freigabe.Nein("Vier-Augen-Prinzip: Die eigene Mitteilung gibt jemand anderes aus dem Vorstand frei.")
                : Freigabe.Ja,
                m.Status != Pressestatus.Freigegeben ? Freigabe.Nein("Erst nach der Freigabe.")
                : leitung ? Freigabe.Ja : Freigabe.Nein("Versand an die Presse durch Vorstand oder Geschäftsführung.")))
            .ToList();
    }

    public async Task<Pressemitteilung> SpeichernAsync(Guid? id, string titel, string text, DateOnly? geplantAm, string? thema, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Politikrechte.Mitarbeiten(wer.Rolle));
        Pressemitteilung m;
        if (id is { } vorhanden)
        {
            var zeile = (await MitteilungenAsync(abbruch)).FirstOrDefault(z => z.Mitteilung.Id == vorhanden) ?? throw new InvalidOperationException("Mitteilung nicht gefunden.");
            Politikrechte.Pruefen(zeile.Bearbeiten);
            m = await daten.LadenAsync<Pressemitteilung>(vorhanden, "Pressemitteilung", abbruch);
            m.Aendern(titel, text, geplantAm, uhr.GetUtcNow());
        }
        else
        {
            m = new Pressemitteilung(titel, text, geplantAm, thema, wer.PersonIdPflicht, uhr.GetUtcNow());
            daten.Hinzufuegen(m);
        }

        await protokoll.ProtokollierenAsync("Presse.Gespeichert", "Pressemitteilung gespeichert", nameof(Pressemitteilung), m.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await bilder.VerwendungenAbgleichenAsync("Presse", m.Id, text, abbruch);
        return m;
    }

    public async Task ZurFreigabeAsync(Guid id, CancellationToken abbruch = default)
    {
        var zeile = (await MitteilungenAsync(abbruch)).FirstOrDefault(z => z.Mitteilung.Id == id) ?? throw new InvalidOperationException("Mitteilung nicht gefunden.");
        Politikrechte.Pruefen(zeile.Einreichen);
        var m = await daten.LadenAsync<Pressemitteilung>(id, "Pressemitteilung", abbruch);
        m.ZurFreigabe();
        await protokoll.ProtokollierenAsync("Presse.ZurFreigabe", "Pressemitteilung zur Freigabe gegeben", nameof(Pressemitteilung), m.Id, abbruch: abbruch);
        await aufgaben.AnlegenAsync($"Pressemitteilung freigeben: {m.Titel}", m.GeplantAm is { } g ? $"Geplant für {g:dd.MM.yyyy}" : null, null, Rolle.Vorstand,
            AufgabenAnlass, m.Id, "presse", m.GeplantAm?.AddDays(-1), null, abbruch);
    }

    public async Task EntscheidenAsync(Guid id, bool freigeben, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var zeile = (await MitteilungenAsync(abbruch)).FirstOrDefault(z => z.Mitteilung.Id == id) ?? throw new InvalidOperationException("Mitteilung nicht gefunden.");
        Politikrechte.Pruefen(zeile.Freigeben);
        var m = await daten.LadenAsync<Pressemitteilung>(id, "Pressemitteilung", abbruch);
        if (freigeben)
        {
            m.Freigeben(wer.PersonIdPflicht, uhr.GetUtcNow());
        }
        else
        {
            m.Zurueckweisen();
        }

        foreach (var t in await daten.ListeAsync(daten.Menge<Aufgabe>().Where(x => x.Anlass == AufgabenAnlass && x.AnlassId == id && x.Status == Aufgabenstatus.Offen)
                     .OrderBy(x => x.Id), abbruch))
        {
            t.Erledigen(uhr.GetUtcNow(), wer.PersonId, freigeben ? "freigegeben" : "zurückgewiesen");
        }

        if (await stammdaten.PersonAsync(m.AutorPersonId, abbruch) is { } autor)
        {
            await postausgang.PushEinreihenAsync(autor, freigeben ? "Pressemitteilung freigegeben" : "Pressemitteilung zurückgewiesen", m.Titel, "presse", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Presse.Entschieden", freigeben ? "Pressemitteilung freigegeben" : "Pressemitteilung zurückgewiesen", nameof(Pressemitteilung), m.Id,
            abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task VeroeffentlichenAsync(Guid id, CancellationToken abbruch = default)
    {
        var zeile = (await MitteilungenAsync(abbruch)).FirstOrDefault(z => z.Mitteilung.Id == id) ?? throw new InvalidOperationException("Mitteilung nicht gefunden.");
        Politikrechte.Pruefen(zeile.Veroeffentlichen);
        var m = await daten.LadenAsync<Pressemitteilung>(id, "Pressemitteilung", abbruch);
        m.Veroeffentlichen(uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Presse.Veroeffentlicht", "Pressemitteilung als versendet markiert", nameof(Pressemitteilung), m.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<IReadOnlyList<SpiegelZeile>> SpiegelAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!Politikrechte.Mitarbeiten(wer.Rolle).Erlaubt)
        {
            return [];
        }

        var titel = await daten.ListeAsync(daten.Menge<Pressemitteilung>().Select(m => new { m.Id, m.Titel }), abbruch);
        return (await daten.ListeAsync(daten.Menge<Presseartikel>().OrderByDescending(a => a.Datum).ThenBy(a => a.Id), abbruch))
            .Select(a => new SpiegelZeile(a, titel.FirstOrDefault(t => t.Id == a.PressemitteilungId)?.Titel)).ToList();
    }

    public async Task<Presseartikel> ArtikelErfassenAsync(string medium, string titel, DateOnly datum, string? link, string? zusammenfassung, string? thema, Tonlage tonlage,
        Guid? pressemitteilungId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Politikrechte.Pruefen(Politikrechte.Mitarbeiten(wer.Rolle));
        var a = new Presseartikel(medium, titel, datum, link, zusammenfassung, thema, tonlage, pressemitteilungId, wer.PersonIdPflicht);
        daten.Hinzufuegen(a);
        await protokoll.ProtokollierenAsync("Presse.Spiegel", "Artikel im Pressespiegel erfasst", nameof(Presseartikel), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return a;
    }
}
