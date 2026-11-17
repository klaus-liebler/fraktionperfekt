using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Ressourcen;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Application.Ressourcen;

public sealed record BuchungZeile(Buchung Buchung, string Ressource, string Person, string? Termin, Freigabe Entscheiden, Freigabe Stornieren);

public sealed record Buchungsergebnis(Buchung Buchung, string Meldung, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

/// <summary>Vorbelegung des Buchungsformulars aus einem Termin („Raum buchen“ als Folgeschritt).</summary>
public sealed record Buchungsvorschlag(Guid? TerminId, string Zweck, DateTime Von, DateTime Bis);

/// <summary>
/// Ressourcen- und Raumbuchung (Modul 2): Belegungsplan, Buchung mit Konfliktprüfung, optionale Freigabe durch die
/// Geschäftsführung (Aufgabe im Korb Administration), Buchung am Termin.
/// </summary>
public sealed class RessourcenDienst(
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    ITerminSpeicher termine,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    TimeProvider uhr)
{
    public const string AufgabenAnlass = "Buchung";

    public Task<List<Ressource>> RessourcenAsync(CancellationToken abbruch = default)
        => daten.ListeAsync(daten.Menge<Ressource>().OrderBy(r => r.Art).ThenBy(r => r.Name).ThenBy(r => r.Id), abbruch);

    /// <summary>Buchungen im Zeitraum (alle Ressourcen oder eine), mit Freigaben aus Sicht der angemeldeten Person.</summary>
    public async Task<IReadOnlyList<BuchungZeile>> BelegungAsync(DateTime von, DateTime bis, Guid? ressourceId = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var buchungen = await daten.ListeAsync(daten.Menge<Buchung>().Where(b => b.Von < bis && von < b.Bis && (ressourceId == null || b.RessourceId == ressourceId))
            .OrderBy(b => b.Von).ThenBy(b => b.Id), abbruch);
        return await ZeilenAsync(buchungen, wer, abbruch);
    }

    /// <summary>Buchungen, die auf Freigabe warten (für die Administration).</summary>
    public async Task<IReadOnlyList<BuchungZeile>> OffeneFreigabenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var buchungen = await daten.ListeAsync(daten.Menge<Buchung>().Where(b => b.Status == Buchungsstatus.Angefragt).OrderBy(b => b.Von).ThenBy(b => b.Id), abbruch);
        return await ZeilenAsync(buchungen, wer, abbruch);
    }

    public async Task<Buchungsvorschlag?> VorschlagAusTerminAsync(Guid terminId, CancellationToken abbruch = default)
        => await termine.TerminAsync(terminId, abbruch) is { } t ? new Buchungsvorschlag(t.Id, t.Titel, t.Beginn.AddMinutes(-30), t.Ende.AddMinutes(30)) : null;

    public async Task<Buchungsergebnis> BuchenAsync(Guid ressourceId, DateTime von, DateTime bis, string zweck, Guid? terminId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (wer.Rolle is Rolle.Helferin && !(await daten.FindenAsync<Ressource>(ressourceId, abbruch) is { Art: Ressourcenart.Material or Ressourcenart.Technik }))
        {
            throw new InvalidOperationException("Das Kampagnenteam bucht nur Kampagnenmaterial und Technik (z. B. Infostand, Pavillon).");
        }

        var r = await daten.LadenAsync<Ressource>(ressourceId, "Ressource", abbruch);
        if (!r.Aktiv)
        {
            throw new InvalidOperationException("Diese Ressource ist nicht buchbar.");
        }

        if (von < Ortszeit.Jetzt(uhr).AddMinutes(-5))
        {
            throw new InvalidOperationException("Der Zeitraum liegt in der Vergangenheit.");
        }

        var bestehende = await daten.ListeAsync(daten.Menge<Buchung>().Where(b => b.RessourceId == ressourceId && b.Von < bis && von < b.Bis).OrderBy(b => b.Von).ThenBy(b => b.Id), abbruch);
        if (Buchung.Konflikte(ressourceId, von, bis, bestehende) is { Count: > 0 } konflikte)
        {
            var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
            var k = konflikte[0];
            throw new InvalidOperationException(
                $"{r.Name} ist von {k.Von:dd.MM. HH:mm} bis {k.Bis:HH:mm} bereits {(k.Status == Buchungsstatus.Angefragt ? "angefragt" : "gebucht")} ({k.Zweck}, {namen.GetValueOrDefault(k.PersonId, "?")}).");
        }

        var b = new Buchung(ressourceId, wer.PersonIdPflicht, von, bis, zweck, terminId, r.FreigabeNoetig, uhr.GetUtcNow());
        daten.Hinzufuegen(b);
        if (r.FreigabeNoetig)
        {
            await aufgaben.AnlegenAsync($"Buchung freigeben: {r.Name} am {von:dd.MM. HH:mm}", $"{zweck} ({wer.Name})", null, Rolle.Administration, AufgabenAnlass, b.Id,
                "ressourcen?ansicht=freigaben", DateOnly.FromDateTime(von.AddDays(-1)), abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Ressource.Gebucht", $"{r.Name} von {von:dd.MM.yyyy HH:mm} bis {bis:dd.MM.yyyy HH:mm} {(r.FreigabeNoetig ? "angefragt" : "gebucht")}",
            nameof(Buchung), b.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        var schritte = new List<NaechsterSchritt>();
        if (terminId is { } t)
        {
            schritte.Add(new NaechsterSchritt("Zum Termin", $"termine/{t}", Hauptschritt: true));
        }

        schritte.Add(new NaechsterSchritt("Weitere Ressource buchen (z. B. Beamer)", terminId is { } tt ? $"ressourcen?termin={tt}" : "ressourcen"));
        return new Buchungsergebnis(b, r.FreigabeNoetig ? $"{r.Name} angefragt – die Geschäftsführung gibt frei." : $"{r.Name} gebucht.", schritte);
    }

    public async Task EntscheidenAsync(Guid buchungId, bool bestaetigen, string? vermerk, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.StammdatenPflegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var b = await daten.LadenAsync<Buchung>(buchungId, "Buchung", abbruch);
        b.Entscheiden(bestaetigen, wer.PersonIdPflicht, vermerk);
        if (await daten.ErsterAsync(daten.Menge<Aufgabe>().Where(a => a.Anlass == AufgabenAnlass && a.AnlassId == buchungId && a.Status == Aufgabenstatus.Offen)
                .OrderBy(a => a.Id), abbruch) is { } aufgabe)
        {
            aufgabe.Erledigen(uhr.GetUtcNow(), wer.PersonId, bestaetigen ? "freigegeben" : "abgelehnt");
        }

        if (await stammdaten.PersonAsync(b.PersonId, abbruch) is { } p)
        {
            var r = await daten.LadenAsync<Ressource>(b.RessourceId, "Ressource", abbruch);
            await postausgang.PushEinreihenAsync(p, $"Buchung {(bestaetigen ? "bestätigt" : "abgelehnt")}: {r.Name}", $"{b.Von:dd.MM. HH:mm} – {b.Zweck}{(vermerk is null ? "" : $" ({vermerk})")}",
                "ressourcen", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Ressource.Entschieden", $"Buchung {(bestaetigen ? "bestätigt" : "abgelehnt")}", nameof(Buchung), b.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task StornierenAsync(Guid buchungId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var b = await daten.LadenAsync<Buchung>(buchungId, "Buchung", abbruch);
        if (StornoFreigabe(b, wer) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        b.Stornieren(null);
        await protokoll.ProtokollierenAsync("Ressource.Storniert", "Buchung storniert", nameof(Buchung), b.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task<Ressource> RessourceAnlegenAsync(string name, Ressourcenart art, string? beschreibung, bool freigabeNoetig, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.StammdatenPflegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var r = new Ressource(name, art, beschreibung, freigabeNoetig);
        daten.Hinzufuegen(r);
        await daten.SpeichernAsync(abbruch);
        return r;
    }

    private static Freigabe StornoFreigabe(Buchung b, Benutzer wer)
        => !b.Blockiert ? Freigabe.Nein("Die Buchung ist nicht mehr aktiv.")
            : b.PersonId == wer.PersonId || wer.Rolle == Rolle.Administration ? Freigabe.Ja
            : Freigabe.Nein("Stornieren kann, wer gebucht hat, oder die Geschäftsführung.");

    private async Task<IReadOnlyList<BuchungZeile>> ZeilenAsync(IReadOnlyList<Buchung> buchungen, Benutzer wer, CancellationToken abbruch)
    {
        var ressourcen = (await RessourcenAsync(abbruch)).ToDictionary(r => r.Id, r => r.Name);
        var namen = (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id, p => p.Anzeigename);
        var zeilen = new List<BuchungZeile>();
        foreach (var b in buchungen)
        {
            var termin = b.TerminId is { } t ? await termine.TerminAsync(t, abbruch) : null;
            zeilen.Add(new BuchungZeile(b, ressourcen.GetValueOrDefault(b.RessourceId, "?"), namen.GetValueOrDefault(b.PersonId, "?"), termin?.Titel,
                b.Status != Buchungsstatus.Angefragt ? Freigabe.Nein("Über die Buchung ist entschieden.") : Rechte.StammdatenPflegen(wer.Rolle),
                StornoFreigabe(b, wer)));
        }

        return zeilen;
    }
}
