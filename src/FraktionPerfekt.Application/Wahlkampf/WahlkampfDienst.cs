using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Wahlkampf;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Wahlkampf;

public sealed record PlakatZeile(Plakat Plakat, string ErfasstVon, double? EntfernungMeter);

public sealed record Abhaengliste(DateOnly? Wahltag, DateOnly? FristBis, bool NachDerWahl, IReadOnlyList<PlakatZeile> Plakate);

public sealed record GebietZeile(Gebiet Gebiet, string? Zugeteilt, Freigabe Zuteilen, Freigabe Erledigen);

public sealed record SchichtZeile(Schicht Schicht, IReadOnlyList<string> Namen, int Frei, bool Ich, Freigabe Eintragen);

/// <summary>
/// Wahlkampf (Modul 17): Plakatkarte mit Handy-Erfassung (Foto, Standort), Status und Abhängliste nach der Wahl; Gebiete
/// zuteilen und abhaken; Infostand-Schichten zum Selbsteintragen. Helfer:innen arbeiten hier voll mit.
/// </summary>
public sealed class WahlkampfDienst(
    IDaten daten,
    IDateiablage ablage,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    public const string AbhaengAnlass = "Plakate abhängen";

    public const int LoeschfristTage = 183;

    private static readonly Dictionary<string, string> Bildtypen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".heic"] = "image/heic",
    };

    public async Task<IReadOnlyList<PlakatZeile>> PlakateAsync(double? breite = null, double? laenge = null, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var liste = (await daten.ListeAsync(daten.Menge<Plakat>().OrderByDescending(p => p.GeaendertAm).ThenBy(p => p.Id), abbruch))
            .Select(p => new PlakatZeile(p, namen.GetValueOrDefault(p.ErfasstVonPersonId, "?"), breite is { } b && laenge is { } l ? p.EntfernungMeter(b, l) : null));
        return (breite is null ? liste : liste.OrderBy(z => z.EntfernungMeter)).ToList();
    }

    public async Task<Plakat> PlakatErfassenAsync(double breite, double laenge, string? standort, Plakatart art, Plakatstatus status, string? motiv, Stream? foto,
        string? fotoName, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var p = new Plakat(breite, laenge, standort ?? string.Empty, art, status, wer.PersonIdPflicht, uhr.GetUtcNow(), motiv);
        if (foto is not null && fotoName is not null)
        {
            await FotoSpeichernAsync(p, foto, fotoName, abbruch);
        }

        daten.Hinzufuegen(p);
        await protokoll.ProtokollierenAsync("Wahlkampf.Plakat", $"Plakat erfasst ({status})", nameof(Plakat), p.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return p;
    }

    public async Task StatusSetzenAsync(Guid id, Plakatstatus status, Stream? foto = null, string? fotoName = null, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var p = await daten.LadenAsync<Plakat>(id, "Plakat", abbruch);
        p.StatusSetzen(status, wer.PersonIdPflicht, uhr.GetUtcNow());
        if (foto is not null && fotoName is not null)
        {
            await FotoSpeichernAsync(p, foto, fotoName, abbruch);
        }

        await protokoll.ProtokollierenAsync("Wahlkampf.PlakatStatus", $"Plakat: {status}", nameof(Plakat), p.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<(Stream Inhalt, string Typ)?> FotoAsync(Guid id, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        return await daten.FindenAsync<Plakat>(id, abbruch) is { FotoSchluessel: { } s, FotoTyp: { } t } && await ablage.OeffnenAsync(s, abbruch) is { } strom
            ? (strom, t)
            : null;
    }

    /// <summary>Alle Plakate, die noch hängen (auch beschädigte), sortiert nach Entfernung vom eigenen Standort.</summary>
    public async Task<Abhaengliste> AbhaenglisteAsync(double? breite = null, double? laenge = null, CancellationToken abbruch = default)
    {
        var wahltag = optionen.Value.Wahltag;
        var heute = Ortszeit.Heute(uhr);
        var plakate = (await PlakateAsync(breite, laenge, abbruch)).Where(z => z.Plakat.HaengtNoch).ToList();
        return new Abhaengliste(wahltag, wahltag?.AddDays(optionen.Value.AbhaengenTage), wahltag is { } w && heute > w,
            breite is null ? plakate.OrderBy(z => z.Plakat.Standort, StringComparer.CurrentCulture).ToList() : plakate);
    }

    public async Task<IReadOnlyList<GebietZeile>> GebieteAsync(CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var steuern = Rechte.WahlkampfSteuern(wer.Rolle);
        return (await daten.ListeAsync(daten.Menge<Gebiet>().OrderBy(g => g.Art).ThenBy(g => g.Name).ThenBy(g => g.Id), abbruch))
            .Select(g => new GebietZeile(g, g.ZugeteiltPersonId is { } p ? namen.GetValueOrDefault(p) : null,
                g.Status == Gebietsstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.") : steuern,
                g.Status == Gebietsstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.")
                : g.ZugeteiltPersonId is null ? Freigabe.Nein("Erst zuteilen.")
                : g.ZugeteiltPersonId == wer.PersonId || steuern.Erlaubt ? Freigabe.Ja : Freigabe.Nein("Abhaken kann, wem das Gebiet zugeteilt ist.")))
            .ToList();
    }

    public async Task<Gebiet> GebietAnlegenAsync(string name, Gebietsart art, string? beschreibung, double? breite, double? laenge, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.WahlkampfSteuern(wer.Rolle));
        var g = new Gebiet(name, art, beschreibung, breite, laenge);
        daten.Hinzufuegen(g);
        await daten.SpeichernAsync(abbruch);
        return g;
    }

    public async Task ZuteilenAsync(Guid gebietId, Guid? personId, CancellationToken abbruch = default)
    {
        var zeile = (await GebieteAsync(abbruch)).FirstOrDefault(z => z.Gebiet.Id == gebietId) ?? throw new InvalidOperationException("Gebiet nicht gefunden.");
        Pruefen(zeile.Zuteilen);
        var g = await daten.LadenAsync<Gebiet>(gebietId, "Gebiet", abbruch);
        g.Zuteilen(personId);
        if (personId is { } p && await stammdaten.PersonAsync(p, abbruch) is { } person)
        {
            await postausgang.PushEinreihenAsync(person, "Wahlkampf: Gebiet zugeteilt", $"{g.Name} ({g.Art})", "wahlkampf", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Wahlkampf.Gebiet", "Gebiet zugeteilt", nameof(Gebiet), g.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task GebietErledigenAsync(Guid gebietId, string? vermerk, CancellationToken abbruch = default)
    {
        var zeile = (await GebieteAsync(abbruch)).FirstOrDefault(z => z.Gebiet.Id == gebietId) ?? throw new InvalidOperationException("Gebiet nicht gefunden.");
        Pruefen(zeile.Erledigen);
        (await daten.LadenAsync<Gebiet>(gebietId, "Gebiet", abbruch)).Erledigen(uhr.GetUtcNow(), vermerk);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<IReadOnlyList<SchichtZeile>> SchichtenAsync(CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var ab = Ortszeit.Heute(uhr).ToDateTime(TimeOnly.MinValue);
        var schichten = await daten.ListeAsync(daten.Menge<Schicht>().Where(s => s.Bis >= ab).OrderBy(s => s.Von).ThenBy(s => s.Id), abbruch);
        var ids = schichten.Select(s => s.Id).ToList();
        var eintraege = await daten.ListeAsync(daten.Menge<Schichteintrag>().Where(e => ids.Contains(e.SchichtId)).OrderBy(e => e.Am).ThenBy(e => e.Id), abbruch);
        return schichten.Select(s =>
        {
            var meine = eintraege.Where(e => e.SchichtId == s.Id).ToList();
            var ich = meine.Any(e => e.PersonId == wer.PersonId);
            var frei = s.Plaetze - meine.Count;
            return new SchichtZeile(s, meine.Select(e => namen.GetValueOrDefault(e.PersonId, "?")).ToList(), frei, ich,
                ich ? Freigabe.Ja : frei <= 0 ? Freigabe.Nein("Die Schicht ist voll.") : Freigabe.Ja);
        }).ToList();
    }

    public async Task<Schicht> SchichtAnlegenAsync(string ort, DateTime von, DateTime bis, int plaetze, Guid? terminId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.WahlkampfSteuern(wer.Rolle));
        var s = new Schicht(ort, von, bis, plaetze, terminId);
        daten.Hinzufuegen(s);
        await daten.SpeichernAsync(abbruch);
        return s;
    }

    /// <summary>Eintragen oder (wenn schon eingetragen) austragen.</summary>
    public async Task<bool> EintragenAsync(Guid schichtId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var zeile = (await SchichtenAsync(abbruch)).FirstOrDefault(z => z.Schicht.Id == schichtId) ?? throw new InvalidOperationException("Schicht nicht gefunden.");
        if (zeile.Ich)
        {
            daten.Entfernen((await daten.ErsterAsync(daten.Menge<Schichteintrag>().Where(e => e.SchichtId == schichtId && e.PersonId == wer.PersonId).OrderBy(e => e.Id), abbruch))!);
        }
        else
        {
            Pruefen(zeile.Eintragen);
            daten.Hinzufuegen(new Schichteintrag(schichtId, wer.PersonIdPflicht, uhr.GetUtcNow()));
        }

        await daten.SpeichernAsync(abbruch);
        return !zeile.Ich;
    }

    /// <summary>
    /// Fristenprüfung: nach der Wahl eine Aufgabe „Plakate abhängen“ für den Vorstand; Löschfrist: abgehängte Plakate (mit
    /// Fotos) und Schichteinträge vergangener Schichten nach einem halben Jahr löschen.
    /// </summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var n = 0;
        if (optionen.Value.Wahltag is { } w && heute > w)
        {
            var haengen = await daten.AnzahlAsync(daten.Menge<Plakat>().Where(p => p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt), abbruch);
            var offen = await daten.AnzahlAsync(daten.Menge<Aufgabe>().Where(a => a.Anlass == AbhaengAnlass && a.Status == Aufgabenstatus.Offen), abbruch);
            if (haengen > 0 && offen == 0 && await daten.AnzahlAsync(daten.Menge<Aufgabe>().Where(a => a.Anlass == AbhaengAnlass), abbruch) == 0)
            {
                await aufgaben.AnlegenAsync($"Plakate abhängen ({haengen} hängen noch)", "Abhängliste unter Wahlkampf bzw. am Handy.", null, Rolle.Vorstand, AbhaengAnlass,
                    Guid.Empty, "wahlkampf?ansicht=abhaengen", w.AddDays(optionen.Value.AbhaengenTage), null, abbruch);
                n++;
            }
        }

        var grenze = uhr.GetUtcNow().AddDays(-LoeschfristTage);
        foreach (var p in (await daten.ListeAsync(daten.Menge<Plakat>().Where(p => p.Status == Plakatstatus.Abgehaengt).OrderBy(p => p.Id), abbruch)).Where(p => p.GeaendertAm < grenze))
        {
            if (p.FotoSchluessel is { } s)
            {
                await ablage.LoeschenAsync(s, abbruch);
            }

            daten.Entfernen(p);
            n++;
        }

        var schichtGrenze = heute.AddDays(-LoeschfristTage).ToDateTime(TimeOnly.MinValue);
        var alteSchichten = await daten.ListeAsync(daten.Menge<Schicht>().Where(s => s.Bis < schichtGrenze).Select(s => s.Id), abbruch);
        foreach (var e in await daten.ListeAsync(daten.Menge<Schichteintrag>().Where(e => alteSchichten.Contains(e.SchichtId)).OrderBy(e => e.Id), abbruch))
        {
            daten.Entfernen(e);
            n++;
        }

        if (n > 0)
        {
            await protokoll.ProtokollierenAsync("Wahlkampf.Fristen", $"Wahlkampf-Fristen: {n} Einträge bearbeitet bzw. nach Löschfrist entfernt", abbruch: abbruch);
            await daten.SpeichernAsync(abbruch);
        }

        return n;
    }

    private async Task FotoSpeichernAsync(Plakat p, Stream foto, string name, CancellationToken abbruch)
    {
        var endung = Path.GetExtension(name);
        if (!Bildtypen.TryGetValue(endung, out var typ))
        {
            throw new InvalidOperationException("Bitte ein Foto (JPG, PNG, WEBP, HEIC) aufnehmen.");
        }

        using var puffer = new MemoryStream();
        await foto.CopyToAsync(puffer, abbruch);
        if (puffer.Length > 15 * 1024 * 1024)
        {
            throw new InvalidOperationException("Das Foto ist größer als 15 MB.");
        }

        puffer.Position = 0;
        if (p.FotoSchluessel is { } alt)
        {
            await ablage.LoeschenAsync(alt, abbruch);
        }

        var schluessel = $"Wahlkampf/Plakate/{p.Id:N}-{Domain.Gemeinsam.Ids.Neu():N}{endung.ToLowerInvariant()}";
        await ablage.SpeichernAsync(schluessel, puffer, abbruch);
        p.FotoSetzen(schluessel, typ);
    }

    private async Task<Benutzer> PersonPflichtAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return wer.PersonId is null ? throw new InvalidOperationException("Bitte anmelden.") : wer;
    }

    private async Task<Dictionary<Guid, string>> NamenAsync(CancellationToken abbruch)
        => (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);

    private static void Pruefen(Freigabe f)
    {
        if (!f.Erlaubt)
        {
            throw new InvalidOperationException(f.Begruendung);
        }
    }
}
