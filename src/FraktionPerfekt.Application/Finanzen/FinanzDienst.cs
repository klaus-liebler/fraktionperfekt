using System.Globalization;
using System.Text;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Finanzen;

public sealed record PostenZeile(Haushaltsposten Posten, decimal Ist, decimal Rest, double Anteil);

/// <param name="Zurueckziehen">Korrekturregel: eigene Auslage 30 Minuten ohne Begründung, danach und für den Kassier mit Begründung; nur vor der Entscheidung.</param>
public sealed record AuslageZeile(Auslage Auslage, string Person, string? Posten, Freigabe Entscheiden, Freigabe Auszahlen, Freigabe? Zurueckziehen = null,
    bool OhneGrund = false);

public sealed record Finanzuebersicht(
    Kasse Kasse,
    int Jahr,
    IReadOnlyList<PostenZeile> Posten,
    IReadOnlyList<Kassenbuchung> Buchungen,
    decimal Einnahmen,
    decimal Ausgaben,
    decimal Kassenstand,
    IReadOnlyList<Spende> Spenden,
    IReadOnlyList<AuslageZeile> Auslagen,
    Freigabe Buchen);

/// <summary>
/// Finanzen (Modul 18) mit der Rolle Kassier: Budget je Posten, Kassenbuch, Spenden (nur Wahlkampfkasse, Regeln des
/// Parteiengesetzes), Auslagen mit Belegfoto und Freigabe nach dem Vier-Augen-Prinzip, Kassenbericht als PDF.
/// </summary>
public sealed class FinanzDienst(
    IDaten daten,
    IDateiablage ablage,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Auslage";

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private static readonly Dictionary<string, string> Belegtypen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".heic"] = "image/heic", [".pdf"] = "application/pdf",
    };

    public async Task<Finanzuebersicht> UebersichtAsync(Kasse kasse, int jahr, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Pruefen(Rechte.FinanzenEinsehen(wer.Rolle));
        var posten = await daten.ListeAsync(daten.Menge<Haushaltsposten>().Where(p => p.Kasse == kasse && p.Jahr == jahr).OrderBy(p => p.Bezeichnung).ThenBy(p => p.Id), abbruch);
        var alle = await daten.ListeAsync(daten.Menge<Kassenbuchung>().Where(b => b.Kasse == kasse).OrderByDescending(b => b.Datum).ThenByDescending(b => b.ErfasstAm).ThenBy(b => b.Id),
            abbruch);
        var imJahr = alle.Where(b => b.Datum.Year == jahr).ToList();
        var spenden = (await daten.ListeAsync(daten.Menge<Spende>().Where(s => s.Kasse == kasse).OrderByDescending(s => s.Datum).ThenBy(s => s.Id), abbruch))
            .Where(s => s.Datum.Year == jahr).ToList();
        return new Finanzuebersicht(kasse, jahr,
            posten.Select(p =>
            {
                var ist = imJahr.Where(b => b.PostenId == p.Id && !b.Einnahme).Sum(b => b.Betrag);
                return new PostenZeile(p, ist, p.Ansatz - ist, p.Ansatz == 0 ? 0 : (double)(ist / p.Ansatz));
            }).ToList(),
            imJahr, imJahr.Where(b => b.Einnahme).Sum(b => b.Betrag), imJahr.Where(b => !b.Einnahme).Sum(b => b.Betrag), alle.Sum(b => b.Wirkung), spenden,
            (await AuslagenAsync(abbruch)).Where(z => z.Auslage.Kasse == kasse && z.Auslage.Status is Auslagenstatus.Eingereicht or Auslagenstatus.Freigegeben).ToList(),
            Rechte.FinanzenBuchen(wer.Rolle));
    }

    public async Task<IReadOnlyList<Haushaltsposten>> PostenAsync(Kasse kasse, int jahr, CancellationToken abbruch = default)
        => await daten.ListeAsync(daten.Menge<Haushaltsposten>().Where(p => p.Kasse == kasse && p.Jahr == jahr).OrderBy(p => p.Bezeichnung).ThenBy(p => p.Id), abbruch);

    public async Task<Haushaltsposten> PostenAnlegenAsync(Kasse kasse, int jahr, string bezeichnung, decimal ansatz, CancellationToken abbruch = default)
    {
        await BuchenPruefenAsync(abbruch);
        var p = new Haushaltsposten(kasse, jahr, bezeichnung, ansatz);
        daten.Hinzufuegen(p);
        await protokoll.ProtokollierenAsync("Finanzen.Posten", $"Haushaltsposten {jahr} angelegt", nameof(Haushaltsposten), p.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return p;
    }

    public async Task<Kassenbuchung> BuchenAsync(Kasse kasse, DateOnly datum, bool einnahme, decimal betrag, Guid? postenId, string text, CancellationToken abbruch = default)
    {
        var wer = await BuchenPruefenAsync(abbruch);
        var b = new Kassenbuchung(kasse, datum, einnahme, betrag, postenId, text, wer.PersonIdPflicht, uhr.GetUtcNow());
        daten.Hinzufuegen(b);
        await protokoll.ProtokollierenAsync("Finanzen.Buchung", $"{(einnahme ? "Einnahme" : "Ausgabe")} gebucht ({kasse})", nameof(Kassenbuchung), b.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return b;
    }

    public async Task<Spende> SpendeErfassenAsync(DateOnly datum, string? spender, string? anschrift, decimal betrag, string zweck, CancellationToken abbruch = default)
    {
        var wer = await BuchenPruefenAsync(abbruch);
        var s = new Spende(Kasse.Wahlkampf, datum, spender, anschrift, betrag, zweck, wer.PersonIdPflicht);
        daten.Hinzufuegen(s);
        daten.Hinzufuegen(new Kassenbuchung(Kasse.Wahlkampf, datum, true, betrag, null, $"Spende{(s.Spender is null ? " (anonym)" : "")}: {s.Zweck}", wer.PersonIdPflicht,
            uhr.GetUtcNow(), Buchungsquelle.Spende, s.Id));
        await protokoll.ProtokollierenAsync("Finanzen.Spende", "Spende erfasst", nameof(Spende), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return s;
    }

    public async Task QuittungVermerkenAsync(Guid spendeId, CancellationToken abbruch = default)
    {
        await BuchenPruefenAsync(abbruch);
        (await daten.LadenAsync<Spende>(spendeId, "Spende", abbruch)).QuittungVermerken(Ortszeit.Heute(uhr));
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Auslagen: wer Finanzen sieht, sieht alle; sonst nur die eigenen.</summary>
    public async Task<IReadOnlyList<AuslageZeile>> AuslagenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var alle = Rechte.FinanzenEinsehen(wer.Rolle).Erlaubt;
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var posten = (await daten.ListeAsync(daten.Menge<Haushaltsposten>().Select(p => new { p.Id, p.Bezeichnung }), abbruch)).ToDictionary(p => p.Id, p => p.Bezeichnung);
        var abfrage = daten.Menge<Auslage>();
        if (!alle)
        {
            abfrage = abfrage.Where(a => a.PersonId == wer.PersonId);
        }

        var entscheiden = wer.Rolle is Rolle.Kassier or Rolle.Vorstand ? Freigabe.Ja : Freigabe.Nein("Auslagen geben Kassier oder Vorstand frei.");
        return (await daten.ListeAsync(abfrage.OrderByDescending(a => a.EingereichtAm).ThenBy(a => a.Id), abbruch))
            .Select(a => new AuslageZeile(a, namen.GetValueOrDefault(a.PersonId, "?"), a.PostenId is { } p ? posten.GetValueOrDefault(p) : null,
                a.Status != Auslagenstatus.Eingereicht ? Freigabe.Nein("Bereits entschieden.")
                : a.PersonId == wer.PersonId ? Freigabe.Nein("Vier-Augen-Prinzip: Die eigene Auslage gibt jemand anderes frei.")
                : a.BelegSchluessel is null ? Freigabe.Nein("Ohne Beleg keine Erstattung.")
                : entscheiden,
                a.Status != Auslagenstatus.Freigegeben ? Freigabe.Nein("Erst nach der Freigabe.") : Rechte.FinanzenBuchen(wer.Rolle),
                a.Status != Auslagenstatus.Eingereicht ? Freigabe.Nein("Über die Auslage ist entschieden; danach nicht mehr zurückziehbar.")
                : a.PersonId == wer.PersonId || wer.Rolle is Rolle.Kassier ? Freigabe.Ja
                : Freigabe.Nein("Zurückziehen kann, wer eingereicht hat, oder der Kassier."),
                Korrekturregel.OhneGrund(a.PersonId == wer.PersonId, a.EingereichtAm, uhr.GetUtcNow())))
            .ToList();
    }

    public async Task<Auslage> AuslageEinreichenAsync(Kasse kasse, DateOnly datum, decimal betrag, string zweck, Guid? postenId, Stream? beleg, string? belegName,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var a = new Auslage(wer.PersonIdPflicht, kasse, datum, betrag, zweck, postenId, uhr.GetUtcNow());
        if (beleg is not null && belegName is not null)
        {
            var name = Domain.Wissen.Datei.Bereinigen(belegName);
            if (!Belegtypen.TryGetValue(Path.GetExtension(name), out var typ))
            {
                throw new InvalidOperationException("Beleg bitte als Foto (JPG, PNG, WEBP, HEIC) oder PDF.");
            }

            using var puffer = new MemoryStream();
            await beleg.CopyToAsync(puffer, abbruch);
            if (puffer.Length > 15 * 1024 * 1024)
            {
                throw new InvalidOperationException("Der Beleg ist größer als 15 MB.");
            }

            puffer.Position = 0;
            var schluessel = $"Finanzen/Belege/{a.Id:N}-{name}";
            await ablage.SpeichernAsync(schluessel, puffer, abbruch);
            a.BelegSetzen(schluessel, name, typ);
        }

        daten.Hinzufuegen(a);
        await protokoll.ProtokollierenAsync("Finanzen.Auslage", $"Auslage eingereicht ({betrag.ToString("C", De)})", nameof(Auslage), a.Id, abbruch: abbruch);
        await aufgaben.AnlegenAsync($"Auslage prüfen: {a.Zweck} ({betrag.ToString("C", De)})", a.BelegSchluessel is null ? "Achtung: ohne Beleg." : null, null, Rolle.Kassier,
            AufgabenAnlass, a.Id, "finanzen", Ortszeit.Heute(uhr).AddDays(7), null, abbruch);
        return a;
    }

    /// <summary>Auslage vor der Entscheidung zurückziehen (Korrekturregel); bleibt als zurückgezogen erhalten, die Prüfaufgabe schließt sich.</summary>
    public async Task AuslageZurueckziehenAsync(Guid id, string? grund, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var a = await daten.LadenAsync<Auslage>(id, "Auslage", abbruch);
        Korrekturregel.Pruefen(a.PersonId == wer.PersonId, a.EingereichtAm, uhr.GetUtcNow(), wer.Rolle is Rolle.Kassier, grund, "wer eingereicht hat, und der Kassier");
        a.Zurueckziehen(Korrekturregel.Grund(grund));
        await AufgabenSchliessenAsync(id, "zurückgezogen", abbruch);
        await protokoll.ProtokollierenAsync("Finanzen.AuslageZurueckgezogen", "Auslage zurückgezogen", nameof(Auslage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task AuslageEntscheidenAsync(Guid id, bool freigeben, string? vermerk, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var zeile = (await AuslagenAsync(abbruch)).FirstOrDefault(z => z.Auslage.Id == id) ?? throw new InvalidOperationException("Auslage nicht gefunden.");
        if (freigeben || zeile.Auslage.PersonId == wer.PersonId)
        {
            Pruefen(zeile.Entscheiden);
        }
        else if (wer.Rolle is not (Rolle.Kassier or Rolle.Vorstand))
        {
            throw new InvalidOperationException("Auslagen geben Kassier oder Vorstand frei.");
        }

        var a = await daten.LadenAsync<Auslage>(id, "Auslage", abbruch);
        a.Entscheiden(freigeben, wer.PersonIdPflicht, vermerk);
        if (!freigeben)
        {
            await AufgabenSchliessenAsync(id, "abgelehnt", abbruch);
        }

        if (await stammdaten.PersonAsync(a.PersonId, abbruch) is { } p)
        {
            await postausgang.PushEinreihenAsync(p, freigeben ? "Auslage freigegeben" : "Auslage abgelehnt", $"{a.Zweck}: {a.Betrag.ToString("C", De)}{(vermerk is null ? "" : $" ({vermerk})")}",
                "auslagen", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Finanzen.AuslageEntschieden", freigeben ? "Auslage freigegeben" : "Auslage abgelehnt", nameof(Auslage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task AuszahlenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await BuchenPruefenAsync(abbruch);
        var a = await daten.LadenAsync<Auslage>(id, "Auslage", abbruch);
        a.Auszahlen(uhr.GetUtcNow());
        daten.Hinzufuegen(new Kassenbuchung(a.Kasse, Ortszeit.Heute(uhr), false, a.Betrag, a.PostenId, $"Erstattung Auslage: {a.Zweck}", wer.PersonIdPflicht, uhr.GetUtcNow(),
            Buchungsquelle.Auslage, a.Id));
        await AufgabenSchliessenAsync(id, "ausgezahlt", abbruch);
        await protokoll.ProtokollierenAsync("Finanzen.Ausgezahlt", "Auslage ausgezahlt", nameof(Auslage), a.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<(Stream Inhalt, string Typ, string Name)?> BelegAsync(Guid auslageId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await daten.FindenAsync<Auslage>(auslageId, abbruch) is not { BelegSchluessel: { } s } a
            || (a.PersonId != wer.PersonId && !Rechte.FinanzenEinsehen(wer.Rolle).Erlaubt))
        {
            return null;
        }

        return await ablage.OeffnenAsync(s, abbruch) is { } strom ? (strom, a.BelegTyp!, a.BelegName!) : null;
    }

    /// <summary>Kassenbericht (Markdown für das PDF mit Briefkopf): Einnahmen und Ausgaben je Posten gegen den Ansatz.</summary>
    public async Task<string> KassenberichtAsync(Kasse kasse, int jahr, CancellationToken abbruch = default)
    {
        var u = await UebersichtAsync(kasse, jahr, abbruch);
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"## Übersicht {jahr}").AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Einnahmen: **{Euro(u.Einnahmen)}**");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Ausgaben: **{Euro(u.Ausgaben)}**");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Ergebnis: **{Euro(u.Einnahmen - u.Ausgaben)}**");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Kassenstand (alle Jahre): **{Euro(u.Kassenstand)}**").AppendLine();
        sb.AppendLine("## Ausgaben je Posten").AppendLine();
        foreach (var p in u.Posten)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- {p.Posten.Bezeichnung}: {Euro(p.Ist)} von {Euro(p.Posten.Ansatz)} ({p.Anteil:P0}){(p.Rest < 0 ? " – **überzogen**" : "")}");
        }

        var ohne = u.Buchungen.Where(b => !b.Einnahme && b.PostenId is null).Sum(b => b.Betrag);
        if (ohne > 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- ohne Posten: {Euro(ohne)}");
        }

        if (u.Spenden.Count > 0)
        {
            sb.AppendLine().AppendLine("## Spenden").AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Anzahl: {u.Spenden.Count}, Summe {Euro(u.Spenden.Sum(s => s.Betrag))}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- davon anonym: {Euro(u.Spenden.Where(s => s.Spender is null).Sum(s => s.Betrag))}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Zuwendungsbestätigungen offen: {u.Spenden.Count(s => s.QuittungFaellig)}");
        }

        sb.AppendLine().AppendLine("## Kassenbuch").AppendLine();
        foreach (var b in u.Buchungen.OrderBy(b => b.Datum))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- {b.Datum:dd.MM.yyyy} · {(b.Einnahme ? "+" : "−")}{Euro(b.Betrag)} · {b.Text}");
        }

        return sb.ToString();
    }

    private static string Euro(decimal betrag) => betrag.ToString("C", De);

    private async Task<Benutzer> BuchenPruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        Pruefen(Rechte.FinanzenBuchen(wer.Rolle));
        return wer;
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

    private static void Pruefen(Freigabe f)
    {
        if (!f.Erlaubt)
        {
            throw new InvalidOperationException(f.Begruendung);
        }
    }
}
