using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Sitzungen;

public sealed record BeamerTop(int Nummer, string Titel, Topstatus Status, bool Laufend, IReadOnlyList<string> Beschluesse);

public sealed record BeamerAbstimmung(string Titel, bool Laeuft, int Teilnehmende, int Berechtigte, string? Ergebnis);

/// <summary>Was der Beamer zeigt: nur Inhalte der laufenden Sitzung – keine Notizen, keine Unterlagen, keine Namen, keine Abwesenheitsgründe.</summary>
public sealed record BeamerAnsicht(string Titel, DateTime Beginn, string? Ort, Sitzungsstatus Status, string? Beschlussfaehigkeit, IReadOnlyList<BeamerTop> Tops,
    IReadOnlyList<BeamerAbstimmung> Abstimmungen);

public sealed record BeamerLink(Uri Adresse, DateTimeOffset GueltigBis);

/// <summary>
/// Beamer- bzw. Monitoransicht einer Sitzung mit geheimer Adresse (aus SchoolCraft <c>MonitorDienst</c> übertragen): Der Vorstand erzeugt einen Link
/// (nur der Prüfwert wird gespeichert, ein neuer sperrt den alten), der Beamer ruft ihn ohne Anmeldung ab und aktualisiert sich selbst. Gültig bis zum
/// Ende des Sitzungstags, jederzeit sperrbar. Angemeldete mit Sitzungsrechten sehen dieselbe Ansicht als Vorschau.
/// </summary>
public sealed class BeamerDienst(
    IDaten daten,
    ITerminSpeicher termine,
    SitzungDienst sitzungen,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr)
{
    /// <summary>Neuer Beamer-Link (sperrt einen früheren dieser Sitzung). Nur Vorstand, Administration, Sitzungsleitung und Protokollführung.</summary>
    public async Task<BeamerLink> LinkErzeugenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var (wer, s, termin) = await PruefenAsync(sitzungId, abbruch);
        var jetzt = uhr.GetUtcNow();
        foreach (var alt in await ZugaengeAsync(sitzungId, abbruch))
        {
            alt.Sperren(jetzt);
        }

        // Gültig bis Mitternacht nach dem Sitzungstag (Ortszeit), mindestens 12 Stunden ab jetzt
        var tagesende = Ortszeit.Zeitpunkt(termin.Beginn.Date.AddDays(1));
        var bis = tagesende > jetzt.AddHours(12) ? tagesende : jetzt.AddHours(12);
        var (zugang, token) = BeamerZugang.Erzeugen(s.Id, wer.PersonIdPflicht, jetzt, bis);
        daten.Hinzufuegen(zugang);
        await protokoll.ProtokollierenAsync("Sitzung.BeamerLink", $"Beamer-Link erzeugt (gültig bis {Ortszeit.AlsOrtszeit(bis):dd.MM. HH:mm}), frühere gesperrt",
            nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        return new BeamerLink(optionen.Value.Link($"beamer/{token}"), bis);
    }

    public async Task SperrenAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        var (_, s, _) = await PruefenAsync(sitzungId, abbruch);
        foreach (var z in await ZugaengeAsync(sitzungId, abbruch))
        {
            z.Sperren(uhr.GetUtcNow());
        }

        await protokoll.ProtokollierenAsync("Sitzung.BeamerGesperrt", "Beamer-Link gesperrt", nameof(Sitzung), s.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Gibt es einen gültigen Beamer-Link (für die Anzeige auf der Sitzungsseite)?</summary>
    public async Task<DateTimeOffset?> GueltigBisAsync(Guid sitzungId, CancellationToken abbruch = default)
        => (await ZugaengeAsync(sitzungId, abbruch)).FirstOrDefault(z => z.Gueltig(uhr.GetUtcNow()))?.GueltigBis;

    /// <summary>Ansicht zum Token (ohne Anmeldung); <c>null</c> bei unbekanntem, gesperrtem oder abgelaufenem Token.</summary>
    public async Task<BeamerAnsicht?> AnsichtAsync(string token, CancellationToken abbruch = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
        {
            return null;
        }

        var hash = Domain.Termine.KalenderAbo.Hash(token);
        var zugang = await daten.ErsterAsync(daten.Menge<BeamerZugang>().Where(z => z.TokenHash == hash).OrderBy(z => z.Id), abbruch);
        return zugang is not null && zugang.Gueltig(uhr.GetUtcNow()) ? await AnsichtFuerAsync(zugang.SitzungId, abbruch) : null;
    }

    /// <summary>Vorschau für Angemeldete mit Sitzungsrechten.</summary>
    public async Task<BeamerAnsicht?> VorschauAsync(Guid sitzungId, CancellationToken abbruch = default)
    {
        await PruefenAsync(sitzungId, abbruch);
        return await AnsichtFuerAsync(sitzungId, abbruch);
    }

    private async Task<BeamerAnsicht?> AnsichtFuerAsync(Guid sitzungId, CancellationToken abbruch)
    {
        if (await daten.FindenAsync<Sitzung>(sitzungId, abbruch) is not { } s || await termine.TerminAsync(s.TerminId, abbruch) is not { } termin)
        {
            return null;
        }

        var tops = await daten.ListeAsync(daten.Menge<Top>().Where(t => t.SitzungId == sitzungId).OrderBy(t => t.Position).ThenBy(t => t.AngelegtAm).ThenBy(t => t.Id), abbruch);
        var topIds = tops.Select(t => t.Id).ToList();
        var beschluesse = await daten.ListeAsync(daten.Menge<Beschluss>().Where(b => b.SitzungId == sitzungId && b.ZurueckgezogenAm == null).OrderBy(b => b.Id), abbruch);
        var laufend = s.Status == Sitzungsstatus.Laufend ? tops.FirstOrDefault(t => t.Status == Topstatus.Geplant)?.Id : null;
        var nummer = 0;
        var beamerTops = tops.Where(t => t.Status != Topstatus.Zusammengelegt)
            .Select(t => new BeamerTop(++nummer, t.Titel, t.Status, t.Id == laufend,
                beschluesse.Where(b => b.TopId == t.Id).Select(b => $"{b.Ergebnis} ({b.Stimmen})").ToList()))
            .ToList();

        var umfragen = await daten.ListeAsync(daten.Menge<Umfrage>().Where(u => u.TopId != null && topIds.Contains(u.TopId.Value)).OrderBy(u => u.ErstelltAm).ThenBy(u => u.Id), abbruch);
        var abstimmungen = new List<BeamerAbstimmung>();
        foreach (var u in umfragen)
        {
            var teil = u.Geheim
                ? await daten.AnzahlAsync(daten.Menge<Stimmvermerk>().Where(v => v.UmfrageId == u.Id), abbruch)
                : (await daten.ListeAsync(daten.Menge<Stimme>().Where(x => x.UmfrageId == u.Id).OrderBy(x => x.Id).Select(x => x.PersonId), abbruch)).Distinct().Count();
            var offen = u.Status == Umfragestatus.Offen;
            abstimmungen.Add(new BeamerAbstimmung(u.Titel, offen, teil, u.Berechtigte, offen ? null : u.Ergebnis));
        }

        var bf = s.Art == Sitzungsart.Gremienprotokoll || s.Status != Sitzungsstatus.Laufend ? null : (await sitzungen.BeschlussfaehigkeitAsync(s, termin, abbruch)).Text;
        return new BeamerAnsicht(termin.Titel, termin.Beginn, termin.Ort, s.Status, bf, beamerTops, abstimmungen);
    }

    private async Task<List<BeamerZugang>> ZugaengeAsync(Guid sitzungId, CancellationToken abbruch)
        => await daten.ListeAsync(daten.Menge<BeamerZugang>().Where(z => z.SitzungId == sitzungId && z.GesperrtAm == null).OrderBy(z => z.Id), abbruch);

    private async Task<(Benutzer Wer, Sitzung Sitzung, Termin Termin)> PruefenAsync(Guid sitzungId, CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var s = await daten.LadenAsync<Sitzung>(sitzungId, "Sitzung", abbruch);
        var termin = await termine.TerminAsync(s.TerminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        var istLeitung = wer.PersonId is { } ich && (ich == s.LeitungPersonId || ich == s.ProtokollPersonId);
        if (!Rechte.SitzungenSteuern(wer.Rolle).Erlaubt && !istLeitung)
        {
            throw new InvalidOperationException("Den Beamer steuern Vorstand, Administration, Sitzungsleitung und Protokollführung.");
        }

        return (wer, s, termin);
    }
}
