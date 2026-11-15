using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Ris;

public sealed record RisAbgleichErgebnis(
    string Quelle,
    int GremienNeu,
    int GremienGeaendert,
    int MitgliedschaftenNeu,
    int MitgliedschaftenGeaendert,
    int MitgliedschaftenBeendet,
    int SitzungenNeu,
    int SitzungenGeaendert,
    IReadOnlyList<string> NichtZugeordnet,
    int BeschluesseNeu = 0)
{
    public string Zusammenfassung =>
        $"Gremien: {GremienNeu} neu, {GremienGeaendert} geändert · Mitgliedschaften: {MitgliedschaftenNeu} neu, {MitgliedschaftenGeaendert} geändert, "
        + $"{MitgliedschaftenBeendet} beendet · Sitzungen: {SitzungenNeu} neu, {SitzungenGeaendert} geändert · Beschlüsse: {BeschluesseNeu} neu";
}

/// <summary>
/// Spiegelt Gremien, Mandate (Mitgliedschaften der eigenen Fraktion) und Sitzungen aus dem RIS. Die Funktion
/// (ordentlich/Stellvertretung) folgt dem RIS; der Vertretungsrang ist fraktionsintern und bleibt erhalten. Neue
/// Stellvertretungen bekommen den nächsten freien Rang. Der Abgleich ist idempotent.
/// </summary>
public sealed class RisAbgleich(
    IRisSchnittstelle ris,
    IStammdatenSpeicher stammdaten,
    ITerminSpeicher termine,
    IDaten daten,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    public async Task<RisAbgleichErgebnis> AbgleichenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var jetzt = uhr.GetUtcNow();
        var heute = Ortszeit.Heute(uhr);

        // Gremien
        var gremien = (await stammdaten.GremienAsync(abbruch)).Where(g => g.RisId is not null).ToDictionary(g => g.RisId!);
        int gremienNeu = 0, gremienGeaendert = 0;
        var risGremien = await ris.GremienAsync(abbruch);
        for (var i = 0; i < risGremien.Count; i++)
        {
            var rg = risGremien[i];
            var art = ArtAus(rg.Art);
            if (gremien.TryGetValue(rg.RisId, out var g))
            {
                gremienGeaendert += g.AusRisUebernehmen(rg.Name, rg.Kurzname, art) ? 1 : 0;
            }
            else
            {
                g = new Gremium(rg.Name, rg.Kurzname, art, (i + 1) * 10, rg.RisId);
                stammdaten.Hinzufuegen(g);
                gremien[rg.RisId] = g;
                gremienNeu++;
            }
        }

        // Mandate der eigenen Fraktion
        var personen = (await stammdaten.PersonenAsync(abbruch)).Where(p => p.RisId is not null).ToDictionary(p => p.RisId!);
        var risPersonen = await ris.PersonenAsync(abbruch);
        var eigene = risPersonen.Where(p => string.Equals(p.Fraktion, optionen.Value.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        var nichtZugeordnet = eigene.Where(p => !personen.ContainsKey(p.RisId)).Select(p => $"{p.Vorname} {p.Nachname}").ToList();

        var alle = (await stammdaten.MitgliedschaftenAsync(abbruch)).ToList();
        var vorhanden = alle.Where(m => m.RisId is not null).ToDictionary(m => m.RisId!);
        int mNeu = 0, mGeaendert = 0, mBeendet = 0;
        var risMitgliedschaften = (await ris.MitgliedschaftenAsync(abbruch)).Where(m => personen.ContainsKey(m.PersonRisId) && gremien.ContainsKey(m.GremiumRisId)).ToList();
        // Stabil nach Beginn sortiert: Bei gleichem Beginn bestimmt die Reihenfolge im RIS den Startrang neuer Stellvertretungen.
        foreach (var rm in risMitgliedschaften.OrderBy(m => m.Beginn))
        {
            var person = personen[rm.PersonRisId];
            var gremium = gremien[rm.GremiumRisId];
            var funktion = rm.Stellvertretung ? Funktion.Stellvertretung : Funktion.Ordentlich;
            var erfassung = new Erfassung(jetzt, wer.Anzeige, Erfassungsart.Ersterfassung, $"Abgleich mit {ris.Quelle}");
            if (!vorhanden.TryGetValue(rm.RisId, out var m))
            {
                if (rm.Ende is { } e && e <= rm.Beginn)
                {
                    continue;
                }

                m = new Mitgliedschaft(person.Id, gremium.Id, funktion,
                    funktion == Funktion.Stellvertretung ? NaechsterRang(alle, gremium.Id, rm.Beginn, jetzt) : null, rm.Beginn, erfassung, rm.RisId);
                if (rm.Ende is { } ende)
                {
                    m.BeendenAb(ende, erfassung with { Art = Erfassungsart.Beendet, Grund = $"Ende laut {ris.Quelle}" });
                }

                stammdaten.Hinzufuegen(m);
                alle.Add(m);
                vorhanden[rm.RisId] = m;
                mNeu++;
                continue;
            }

            var stichtag = heute < rm.Beginn ? rm.Beginn : heute;
            var stand = m.StandAm(stichtag, jetzt);
            if (stand is not null && stand.Funktion != funktion && (rm.Ende is null || stichtag < rm.Ende))
            {
                m.AendernAb(stichtag, funktion, funktion == Funktion.Stellvertretung ? NaechsterRang(alle, gremium.Id, stichtag, jetzt) : null,
                    erfassung with { Art = Erfassungsart.AenderungAbDatum, Grund = $"Funktion laut {ris.Quelle}" });
                mGeaendert++;
            }

            if (rm.Ende is { } bis && m.StandAm(bis, jetzt) is not null)
            {
                m.BeendenAb(bis, erfassung with { Art = Erfassungsart.Beendet, Grund = $"Ende laut {ris.Quelle}" });
                mBeendet++;
            }
        }

        // Im RIS nicht mehr vorhandene Mandate enden heute.
        var imRis = risMitgliedschaften.Select(m => m.RisId).ToHashSet(StringComparer.Ordinal);
        foreach (var m in alle.Where(m => m.RisId is not null && !imRis.Contains(m.RisId) && m.StandAm(heute, jetzt) is not null))
        {
            m.BeendenAb(heute, Erfassung.Ende(jetzt, $"Nicht mehr in {ris.Quelle}", wer.Anzeige));
            mBeendet++;
        }

        // Sitzungen: ein Vierteljahr zurück, ein Jahr voraus.
        var sitzungen = await ris.SitzungenAsync(heute.AddDays(-90), heute.AddDays(365), abbruch);
        var vorhandeneTermine = (await termine.TermineMitRisIdAsync(abbruch)).ToDictionary(t => t.RisId!);
        int sNeu = 0, sGeaendert = 0;
        foreach (var s in sitzungen.Where(s => gremien.ContainsKey(s.GremiumRisId)))
        {
            var gremium = gremien[s.GremiumRisId];
            if (vorhandeneTermine.TryGetValue(s.RisId, out var t))
            {
                sGeaendert += t.AusRisUebernehmen(s.Name, s.Beginn, s.Ende, s.Ort, s.Abgesagt) ? 1 : 0;
                continue;
            }

            t = new Termin(s.Name, gremium.Art == Gremienart.Rat ? Terminart.Ratssitzung : Terminart.Ausschusssitzung, s.Beginn, s.Ende, s.Ort,
                gremium.Id, ris.Quelle, jetzt, (s.Tagesordnung is null ? string.Empty : "Tagesordnung (Auszug):\n" + s.Tagesordnung + "\n\n")
                + "Öffentliche Sitzung laut Ratsinformationssystem. Unterlagen im RIS (Mandatsträger-Zugang).", s.RisId);
            if (s.Abgesagt)
            {
                t.Absagen();
            }

            termine.Hinzufuegen(t);
            vorhandeneTermine[s.RisId] = t;
            sNeu++;
        }

        // Beschlüsse des Rates und der Ausschüsse (letztes Jahr) für das Beschlusslage-Archiv; idempotent über die RIS-Id.
        var bekannt = (await daten.ListeAsync(daten.Menge<Domain.Sitzungen.Beschluss>().Where(b => b.RisId != null).Select(b => b.RisId!), abbruch)).ToHashSet();
        var bNeu = 0;
        foreach (var b in (await ris.BeschluesseAsync(heute.AddDays(-365), heute, abbruch)).Where(b => gremien.ContainsKey(b.GremiumRisId) && !bekannt.Contains(b.RisId)))
        {
            var gremium = gremien[b.GremiumRisId];
            daten.Hinzufuegen(new Domain.Sitzungen.Beschluss(gremium.Art == Gremienart.Rat ? Domain.Sitzungen.Beschlussquelle.Rat : Domain.Sitzungen.Beschlussquelle.Ausschuss,
                b.Datum, b.Titel, b.Text, Ergebnis(b.Ergebnis), 0, 0, 0, gremiumId: gremium.Id, risId: b.RisId));
            bNeu++;
        }

        var ergebnis = new RisAbgleichErgebnis(ris.Quelle, gremienNeu, gremienGeaendert, mNeu, mGeaendert, mBeendet, sNeu, sGeaendert, nichtZugeordnet, bNeu);
        await protokoll.ProtokollierenAsync("Ris.Abgleich", $"Abgleich mit {ris.Quelle}: {ergebnis.Zusammenfassung}"
            + (nichtZugeordnet.Count > 0 ? $"; {nichtZugeordnet.Count} Personen der Fraktion im RIS ohne Zuordnung" : string.Empty), abbruch: abbruch);
        await stammdaten.SpeichernAsync(abbruch);
        return ergebnis;
    }

    /// <summary>Ergebnis-Freitext des RIS grob einordnen.</summary>
    public static Domain.Sitzungen.Beschlussergebnis Ergebnis(string? text)
    {
        var t = (text ?? string.Empty).ToLowerInvariant();
        return t.Contains("abgelehnt", StringComparison.Ordinal) ? Domain.Sitzungen.Beschlussergebnis.Abgelehnt
            : t.Contains("vertagt", StringComparison.Ordinal) || t.Contains("zurückgestellt", StringComparison.Ordinal) ? Domain.Sitzungen.Beschlussergebnis.Vertagt
            : t.Contains("kenntnis", StringComparison.Ordinal) ? Domain.Sitzungen.Beschlussergebnis.Kenntnisnahme
            : Domain.Sitzungen.Beschlussergebnis.Angenommen;
    }

    private static int NaechsterRang(IEnumerable<Mitgliedschaft> alle, Guid gremiumId, DateOnly tag, DateTimeOffset jetzt)
        => (alle.Where(m => m.GremiumId == gremiumId).Select(m => m.StandAm(tag, jetzt)?.Rang).Max() ?? 0) + 1;

    private static Gremienart ArtAus(string art) => art.Trim().ToUpperInvariant() switch
    {
        "RAT" => Gremienart.Rat,
        "BEIRAT" => Gremienart.Beirat,
        "ARBEITSKREIS" => Gremienart.Arbeitskreis,
        _ => Gremienart.Ausschuss,
    };
}
