using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Termine;

/// <summary>Wie eine Person an einem Termin beteiligt ist.</summary>
public enum Beteiligung
{
    /// <summary>Kein fester Platz (z. B. öffentlicher Termin): Zusage = Interesse.</summary>
    Keine,

    /// <summary>Ordentliches Mitglied des Gremiums.</summary>
    Ordentlich,

    /// <summary>Stellvertretung im Gremium: nur bei Bedarf gefragt.</summary>
    Stellvertretung,

    /// <summary>Vertritt ein ordentliches Mitglied.</summary>
    Vertritt,
}

public sealed record TerminZeile(
    Termin Termin,
    string? Gremium,
    string? GremiumKurz,
    Beteiligung Beteiligung,
    Teilnahmestatus? MeinStatus,
    bool Abwesend,
    int Zusagen,
    int Absagen,
    int OffeneVertretungen,
    Freigabe Antworten)
{
    /// <summary>Wartet der Termin auf eine Antwort dieser Person?</summary>
    public bool WartetAufAntwort => Beteiligung == Beteiligung.Ordentlich && MeinStatus is null && !Abwesend && Antworten.Erlaubt;
}

public sealed record TerminTeilnehmer(Guid PersonId, string Name, Beteiligung Beteiligung, int? Rang, Teilnahmestatus? Status, bool Abwesend,
    string? Vertritt);

public sealed record TerminDetail(TerminZeile Zeile, IReadOnlyList<TerminTeilnehmer> Teilnehmende, IReadOnlyList<Vertretungsfall> Vertretungen);

/// <param name="GremiumId">Nur Termine dieses Gremiums.</param>
/// <param name="PersonId">Nur Termine, an denen diese Person beteiligt ist (Sitz, Vertretung oder Zusage).</param>
public sealed record Kalenderfilter(Guid? GremiumId = null, Guid? PersonId = null);

public sealed record NeuerTermin(string Titel, Terminart Art, DateTime Beginn, DateTime Ende, string? Ort, Guid? GremiumId, string? Beschreibung);

public sealed record Antwortergebnis(Teilnahmestatus Status, IReadOnlyList<NaechsterSchritt> NaechsteSchritte);

public sealed record AboLink(Guid AboId, Uri Webcal, Uri Https);

/// <summary>
/// Gemeinsamer Kalender: Termine der Fraktion, Rats- und Ausschusssitzungen, öffentliche Termine. Zusage/Absage je Termin
/// und persönliches Kalender-Abo (webcal) mit widerrufbarem Token.
/// </summary>
public sealed class KalenderDienst(
    ITerminSpeicher termine,
    IStammdatenSpeicher stammdaten,
    IVertretungSpeicher vertretung,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr,
    IDaten daten,
    IServiceProvider dienste)
{
    /// <summary>Termine im Zeitraum [von, bis] (Tage einschließlich) aus Sicht der angemeldeten Person.</summary>
    public async Task<IReadOnlyList<TerminZeile>> TermineAsync(DateOnly von, DateOnly bis, Kalenderfilter? filter = null, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return await ZeilenAsync(wer.PersonId, wer.Rolle, von, bis, filter ?? new Kalenderfilter(), abbruch);
    }

    public async Task<TerminDetail?> TerminAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (await termine.TerminAsync(id, abbruch) is not { } termin || !Rechte.SiehtTermin(wer.Rolle, termin.Art))
        {
            return null;
        }

        var kontext = await KontextAsync(termin.Tag, termin.Tag, [termin], abbruch);
        var zeile = Zeile(termin, wer.PersonId, kontext);
        var teilnehmende = new List<TerminTeilnehmer>();
        var faelle = await vertretung.FaelleZuTerminenAsync([termin.Id], abbruch);
        if (termin.GremiumId is { } gremiumId)
        {
            foreach (var sitz in Besetzung.Am(kontext.Mitgliedschaften, gremiumId, termin.Tag, kontext.Jetzt))
            {
                var t = kontext.Teilnahmen.FirstOrDefault(x => x.TerminId == termin.Id && x.PersonId == sitz.PersonId);
                teilnehmende.Add(new TerminTeilnehmer(sitz.PersonId, kontext.Name(sitz.PersonId),
                    t?.Status == Teilnahmestatus.Vertritt ? Beteiligung.Vertritt
                    : sitz.Funktion == Funktion.Ordentlich ? Beteiligung.Ordentlich : Beteiligung.Stellvertretung,
                    sitz.Rang, t?.Status, kontext.IstAbwesend(sitz.PersonId, termin.Tag),
                    t?.VertrittPersonId is { } v ? kontext.Name(v) : null));
            }
        }

        foreach (var t in kontext.Teilnahmen.Where(x => x.TerminId == termin.Id && teilnehmende.All(p => p.PersonId != x.PersonId)))
        {
            teilnehmende.Add(new TerminTeilnehmer(t.PersonId, kontext.Name(t.PersonId),
                t.Status == Teilnahmestatus.Vertritt ? Beteiligung.Vertritt : Beteiligung.Keine, null, t.Status,
                kontext.IstAbwesend(t.PersonId, termin.Tag), t.VertrittPersonId is { } v ? kontext.Name(v) : null));
        }

        return new TerminDetail(zeile, teilnehmende, faelle);
    }

    public static Freigabe AntwortFreigabe(Termin termin, DateTime jetzt)
    {
        ArgumentNullException.ThrowIfNull(termin);
        if (termin.Abgesagt)
        {
            return Freigabe.Nein("Der Termin ist abgesagt.");
        }

        return termin.Ende <= jetzt ? Freigabe.Nein("Der Termin liegt in der Vergangenheit.") : Freigabe.Ja;
    }

    /// <summary>Zu- oder Absage der angemeldeten Person (Ein-Klick-Antwort).</summary>
    public async Task<Antwortergebnis> AntwortenAsync(Guid terminId, bool zusage, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var personId = wer.PersonIdPflicht;
        var termin = await termine.TerminAsync(terminId, abbruch) ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (AntwortFreigabe(termin, Ortszeit.Jetzt(uhr)) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var status = zusage ? Teilnahmestatus.Zugesagt : Teilnahmestatus.Abgesagt;
        var teilnahme = await termine.TeilnahmeAsync(terminId, personId, abbruch);
        if (teilnahme is null)
        {
            termine.Hinzufuegen(new Teilnahme(terminId, personId, status, uhr.GetUtcNow()));
        }
        else
        {
            teilnahme.Setzen(status, uhr.GetUtcNow());
        }

        await protokoll.ProtokollierenAsync(zusage ? "Termin.Zusage" : "Termin.Absage",
            $"{Pseudonym.Person(personId)} hat für den Termin {(zusage ? "zugesagt" : "abgesagt")}", nameof(Termin), terminId, abbruch: abbruch);
        await termine.SpeichernAsync(abbruch);

        var schritte = new List<NaechsterSchritt>();
        if (!zusage && termin.GremiumId is { } gremiumId && termin.Art.IstGremiensitzung())
        {
            var mitgliedschaften = await stammdaten.MitgliedschaftenDesGremiumsAsync(gremiumId, abbruch);
            if (Besetzung.OrdentlicheAm(mitgliedschaften, gremiumId, termin.Tag, uhr.GetUtcNow()).Contains(personId))
            {
                schritte.Add(new NaechsterSchritt("Vertretung anfragen", $"vertretungen/termin/{terminId}",
                    "Sie sind ordentliches Mitglied: Ihre Stellvertretungen werden in Rangfolge gefragt.", Hauptschritt: true));
            }
        }

        schritte.Add(new NaechsterSchritt("Zum Kalender", "kalender"));
        return new Antwortergebnis(status, schritte);
    }

    /// <summary>Termin anlegen (Fraktionssitzung, Arbeitskreis, öffentlicher Termin …).</summary>
    public async Task<(Termin Termin, IReadOnlyList<NaechsterSchritt> NaechsteSchritte)> AnlegenAsync(NeuerTermin neu, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (Rechte.TermineAnlegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var termin = new Termin(neu.Titel, neu.Art, neu.Beginn, neu.Ende, neu.Ort, neu.GremiumId, wer.Anzeige, uhr.GetUtcNow(), neu.Beschreibung);
        termine.Hinzufuegen(termin);
        await protokoll.ProtokollierenAsync("Termin.Angelegt", $"{termin.Art.Anzeigename()} am {termin.Beginn:dd.MM.yyyy HH:mm} angelegt",
            nameof(Termin), termin.Id, abbruch: abbruch);
        await termine.SpeichernAsync(abbruch);
        await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Wissen.BildDienst>(dienste)
            .VerwendungenAbgleichenAsync("Termin", termin.Id, neu.Beschreibung, abbruch);

        var schritte = new List<NaechsterSchritt>
        {
            new("Termin ansehen", $"termine/{termin.Id}", Hauptschritt: true),
        };
        if (neu.Art == Terminart.Fraktionssitzung)
        {
            schritte.Add(new("Tagesordnung aus Vorlage anlegen und einladen", $"sitzungen?termin={termin.Id}", "Feste TOPs aus der Vorlage, vertagte TOPs der letzten Sitzung."));
            schritte.Add(new("Raum buchen", $"ressourcen?termin={termin.Id}"));
        }
        else if (neu.Art.IstOeffentlich())
        {
            schritte.Add(new("Besetzungsanfrage mit N Plätzen an alle", $"besetzung?termin={termin.Id}"));
        }

        schritte.Add(new("Weiteren Termin anlegen", "kalender?neu=1"));
        return (termin, schritte);
    }

    /// <summary>Neues Kalender-Abo; ein bestehendes wird dabei widerrufen (immer nur ein gültiger Link je Person).</summary>
    public async Task<AboLink> AboAnlegenAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var personId = wer.PersonIdPflicht;
        var jetzt = uhr.GetUtcNow();
        foreach (var alt in (await termine.AbosDerPersonAsync(personId, abbruch)).Where(a => a.Aktiv))
        {
            alt.Widerrufen(jetzt);
        }

        var (abo, token) = KalenderAbo.Erzeugen(personId, jetzt);
        termine.Hinzufuegen(abo);
        await protokoll.ProtokollierenAsync("Kalender.AboAngelegt", $"Kalender-Abo für {Pseudonym.Person(personId)} angelegt (frühere widerrufen)",
            nameof(KalenderAbo), abo.Id, abbruch: abbruch);
        await termine.SpeichernAsync(abbruch);
        var https = optionen.Value.Link($"kalender/abo/{token}.ics");
        var webcal = new UriBuilder(https) { Scheme = "webcal", Port = https.IsDefaultPort ? -1 : https.Port }.Uri;
        return new AboLink(abo.Id, webcal, https);
    }

    public async Task AboWiderrufenAsync(Guid aboId, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var personId = wer.PersonIdPflicht;
        var abo = (await termine.AbosDerPersonAsync(personId, abbruch)).FirstOrDefault(a => a.Id == aboId)
            ?? throw new InvalidOperationException("Dieses Abo gehört nicht zu Ihnen.");
        abo.Widerrufen(uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Kalender.AboWiderrufen", $"Kalender-Abo von {Pseudonym.Person(personId)} widerrufen",
            nameof(KalenderAbo), abo.Id, abbruch: abbruch);
        await termine.SpeichernAsync(abbruch);
    }

    public async Task<IReadOnlyList<KalenderAbo>> MeineAbosAsync(CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        return wer.PersonId is { } p
            ? (await termine.AbosDerPersonAsync(p, abbruch)).OrderByDescending(a => a.ErstelltAm).ThenBy(a => a.Id).ToList()
            : [];
    }

    /// <summary>
    /// Kalenderdatei zum Abo-Token (ohne Anmeldung abrufbar). <c>null</c>, wenn das Token unbekannt oder widerrufen ist.
    /// Enthält Termine, an denen die Person beteiligt ist, von 60 Tagen zurück bis ein Jahr voraus.
    /// </summary>
    public async Task<string?> AboKalenderAsync(string token, CancellationToken abbruch = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100
            || await termine.AboNachHashAsync(KalenderAbo.Hash(token), abbruch) is not { Aktiv: true } abo
            || await stammdaten.PersonAsync(abo.PersonId, abbruch) is not { Aktiv: true } person)
        {
            return null;
        }

        var jetzt = uhr.GetUtcNow();
        var heute = Ortszeit.Heute(uhr);
        var rolle = person.RollenAm(heute, jetzt) is { Count: > 0 } rollen && rollen.All(r => r == Rolle.Helferin) ? Rolle.Helferin : (Rolle?)null;
        var zeilen = await ZeilenAsync(person.Id, rolle, heute.AddDays(-60), heute.AddDays(365), new Kalenderfilter(PersonId: person.Id), abbruch);
        var schichten = await SchichtenAsync(person.Id, heute.AddDays(-60), abbruch);
        abo.AbrufVermerken(jetzt);
        await termine.SpeichernAsync(abbruch);
        return Kalenderdatei.Erzeugen($"{optionen.Value.Name} – {person.Vorname}",
            zeilen.Where(z => z.MeinStatus != Teilnahmestatus.Abgesagt).Select(z => new Kalendereintrag(z.Termin.Id,
                z.GremiumKurz is { } k && !z.Termin.Titel.Contains(k, StringComparison.Ordinal) ? $"{z.Termin.Titel} ({k})" : z.Termin.Titel,
                z.Termin.Beginn, z.Termin.Ende, z.Termin.Ort,
                (z.Beteiligung == Beteiligung.Vertritt ? "Sie vertreten in dieser Sitzung.\n" : string.Empty) + optionen.Value.Link($"termine/{z.Termin.Id}"),
                z.Termin.Abgesagt)).Concat(schichten),
            jetzt);
    }

    /// <summary>Einzelner Termin als Kalenderdatei (Download „In meinen Kalender“) – für alle Terminarten, die die Person sieht.</summary>
    public async Task<string?> TerminKalenderAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await TerminAsync(id, abbruch) is not { } d)
        {
            return null;
        }

        var t = d.Zeile.Termin;
        return Kalenderdatei.Erzeugen(optionen.Value.Name, [new Kalendereintrag(t.Id, t.Titel, t.Beginn, t.Ende, t.Ort,
            (t.Beschreibung is null ? "" : t.Beschreibung + "\n\n") + optionen.Value.Link($"termine/{t.Id}"), t.Abgesagt)], uhr.GetUtcNow());
    }

    /// <summary>Infostand-Schichten, in die sich die Person eingetragen hat (auch Helfer:innen ohne Gremium).</summary>
    private async Task<IReadOnlyList<Kalendereintrag>> SchichtenAsync(Guid personId, DateOnly ab, CancellationToken abbruch)
    {
        var start = ab.ToDateTime(TimeOnly.MinValue);
        var ids = await daten.ListeAsync(daten.Menge<Domain.Wahlkampf.Schichteintrag>().Where(e => e.PersonId == personId).Select(e => e.SchichtId), abbruch);
        return (await daten.ListeAsync(daten.Menge<Domain.Wahlkampf.Schicht>().Where(x => ids.Contains(x.Id) && x.Bis >= start).OrderBy(x => x.Von).ThenBy(x => x.Id), abbruch))
            .Select(x => new Kalendereintrag(x.Id, $"Wahlkampf: {x.Ort}", x.Von, x.Bis, x.Ort, optionen.Value.Link("wahlkampf?ansicht=schichten").ToString(), false))
            .ToList();
    }

    internal async Task<IReadOnlyList<TerminZeile>> ZeilenAsync(Guid? personId, Rolle? rolle, DateOnly von, DateOnly bis, Kalenderfilter filter,
        CancellationToken abbruch)
    {
        var liste = (await termine.TermineAsync(von.ToDateTime(TimeOnly.MinValue), bis.AddDays(1).ToDateTime(TimeOnly.MinValue), abbruch))
            .Where(t => Rechte.SiehtTermin(rolle, t.Art) && (filter.GremiumId is null || t.GremiumId == filter.GremiumId))
            .ToList();
        var kontext = await KontextAsync(von, bis, liste, abbruch);
        var zeilen = liste.Select(t => Zeile(t, personId, kontext)).ToList();
        if (filter.PersonId is { } p)
        {
            zeilen = zeilen.Where(z => p == personId
                ? z.Beteiligung is Beteiligung.Ordentlich or Beteiligung.Vertritt || z.MeinStatus == Teilnahmestatus.Zugesagt
                : BeteiligungErmitteln(z.Termin, p, kontext) is Beteiligung.Ordentlich or Beteiligung.Vertritt
                  || kontext.Teilnahmen.Any(x => x.TerminId == z.Termin.Id && x.PersonId == p && x.Status == Teilnahmestatus.Zugesagt)).ToList();
        }

        return zeilen;
    }

    private async Task<Kontext> KontextAsync(DateOnly von, DateOnly bis, IReadOnlyList<Termin> liste, CancellationToken abbruch)
    {
        var ids = liste.Select(t => t.Id).ToList();
        return new Kontext(
            uhr.GetUtcNow(),
            Ortszeit.Jetzt(uhr),
            (await stammdaten.GremienAsync(abbruch)).ToDictionary(g => g.Id),
            (await stammdaten.PersonenAsync(abbruch)).ToDictionary(p => p.Id),
            await stammdaten.MitgliedschaftenAsync(abbruch),
            await termine.TeilnahmenAsync(ids, abbruch),
            await vertretung.AbwesenheitenImZeitraumAsync(von, bis, abbruch),
            await vertretung.FaelleZuTerminenAsync(ids, abbruch));
    }

    private static TerminZeile Zeile(Termin t, Guid? personId, Kontext k)
    {
        var gremium = t.GremiumId is { } g && k.Gremien.TryGetValue(g, out var gr) ? gr : null;
        var mein = personId is { } p ? k.Teilnahmen.FirstOrDefault(x => x.TerminId == t.Id && x.PersonId == p) : null;
        var eigene = k.Teilnahmen.Where(x => x.TerminId == t.Id).ToList();
        return new TerminZeile(t, gremium?.Name, gremium?.Kurzname,
            personId is { } q ? BeteiligungErmitteln(t, q, k) : Beteiligung.Keine,
            mein?.Status,
            personId is { } a && k.IstAbwesend(a, t.Tag),
            eigene.Count(x => x.Status is Teilnahmestatus.Zugesagt or Teilnahmestatus.Vertritt),
            eigene.Count(x => x.Status == Teilnahmestatus.Abgesagt),
            k.Faelle.Count(f => f.TerminId == t.Id && f.IstOffen),
            AntwortFreigabe(t, k.Ortsjetzt));
    }

    private static Beteiligung BeteiligungErmitteln(Termin t, Guid personId, Kontext k)
    {
        if (k.Teilnahmen.Any(x => x.TerminId == t.Id && x.PersonId == personId && x.Status == Teilnahmestatus.Vertritt))
        {
            return Beteiligung.Vertritt;
        }

        if (t.GremiumId is not { } g)
        {
            return Beteiligung.Keine;
        }

        var stand = k.Mitgliedschaften.Where(m => m.GremiumId == g && m.PersonId == personId)
            .Select(m => m.StandAm(t.Tag, k.Jetzt)).FirstOrDefault(s => s is not null);
        return stand?.Funktion switch
        {
            Funktion.Ordentlich => Beteiligung.Ordentlich,
            Funktion.Stellvertretung => Beteiligung.Stellvertretung,
            _ => Beteiligung.Keine,
        };
    }

    private sealed record Kontext(
        DateTimeOffset Jetzt,
        DateTime Ortsjetzt,
        IReadOnlyDictionary<Guid, Gremium> Gremien,
        IReadOnlyDictionary<Guid, Person> Personen,
        IReadOnlyList<Mitgliedschaft> Mitgliedschaften,
        IReadOnlyList<Teilnahme> Teilnahmen,
        IReadOnlyList<Abwesenheit> Abwesenheiten,
        IReadOnlyList<Vertretungsfall> Faelle)
    {
        public string Name(Guid personId) => Personen.TryGetValue(personId, out var p) ? p.Anzeigename : "?";

        public bool IstAbwesend(Guid personId, DateOnly tag) => Abwesenheiten.Any(a => a.PersonId == personId && a.Umfasst(tag));
    }
}
