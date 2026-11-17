using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Kampagnen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Application.Kampagnen;

public sealed record KampagnenZeile(Kampagne Kampagne, int Plakate, int PlakateHaengen, int GebieteOffen, int TermineKommend, int TicketsOffen);

public sealed record PlakatZeile(Plakat Plakat, string ErfasstVon, double? EntfernungMeter);

public sealed record Abhaengliste(DateOnly? Wahltag, DateOnly? FristBis, bool NachDerWahl, IReadOnlyList<PlakatZeile> Plakate);

public sealed record GebietZeile(Gebiet Gebiet, string? Zugeteilt, Freigabe Zuteilen, Freigabe Erledigen);

/// <param name="Namen">Eingebuchte Personen (Zusage).</param>
/// <param name="Frei">Freie Plätze; leer = unbegrenzt.</param>
public sealed record KampagnenTerminZeile(Termin Termin, IReadOnlyList<string> Namen, int? Frei, bool Ich, Freigabe Einbuchen, int TodosOffen, int TodosGesamt);

public sealed record TodoZeile(TerminTodo Todo, string? Zustaendig, string? ErledigtVon);

public sealed record VorlageZeile(TodoVorlage Vorlage, IReadOnlyList<TodoVorlagenPunkt> Punkte);

public sealed record TicketZeile(Ticket Ticket, string? Zustaendig, string ErstelltVon, string? Bezug, int Kommentare);

public sealed record TicketKommentarZeile(TicketKommentar Kommentar, string Name);

public sealed record TicketDetail(TicketZeile Zeile, IReadOnlyList<TicketKommentarZeile> Kommentare);

public sealed record NeuesTicket(string Titel, string? Beschreibung, Ticketprioritaet Prioritaet, Guid? ZustaendigPersonId, DateOnly? Faellig,
    Guid? GebietId = null, Guid? PlakatId = null, Guid? TerminId = null);

/// <summary>
/// Kampagnen (früher „Wahlkampf“): Jede Kampagne hat Plakate (Handy-Erfassung mit Foto und Standort, Abhängliste nach dem Wahltag),
/// Gebiete (zuteilen, abhaken), Termine (normale Kalendertermine mit Kampagnenbezug: einbuchen über die Zusage, ToDo-Liste je Termin,
/// ToDos aus Vorlagen) und Tickets. Das Kampagnenteam (Rolle „Helferin“) arbeitet hier voll mit; steuern (anlegen, zuteilen) dürfen
/// Vorstand und Geschäftsführung.
/// </summary>
public sealed class KampagnenDienst(
    IDaten daten,
    IDateiablage ablage,
    IStammdatenSpeicher stammdaten,
    AufgabenDienst aufgaben,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    IAktuellerBenutzer benutzer,
    IOptions<FraktionOptionen> optionen,
    TimeProvider uhr,
    IServiceProvider dienste)
{
    public const string AbhaengAnlass = "Plakate abhängen";

    public const int LoeschfristTage = 183;

    private static readonly Dictionary<string, string> Bildtypen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".heic"] = "image/heic",
    };

    // ---------------- Kampagnen ----------------

    public async Task<IReadOnlyList<KampagnenZeile>> KampagnenAsync(CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var ab = Ortszeit.Heute(uhr).ToDateTime(TimeOnly.MinValue);
        var kampagnen = await daten.ListeAsync(daten.Menge<Kampagne>().OrderBy(k => k.Status).ThenByDescending(k => k.Beginn).ThenBy(k => k.Name).ThenBy(k => k.Id), abbruch);
        var plakate = await daten.ListeAsync(daten.Menge<Plakat>().Select(p => new { p.KampagneId, p.Status }), abbruch);
        var gebiete = await daten.ListeAsync(daten.Menge<Gebiet>().Where(g => g.Status != Gebietsstatus.Erledigt).Select(g => g.KampagneId), abbruch);
        var termine = await daten.ListeAsync(daten.Menge<Termin>().Where(t => t.KampagneId != null && t.Ende >= ab && !t.Abgesagt).Select(t => t.KampagneId), abbruch);
        var tickets = await daten.ListeAsync(daten.Menge<Ticket>().Where(t => t.Status != Ticketstatus.Erledigt).Select(t => t.KampagneId), abbruch);
        return kampagnen.Select(k => new KampagnenZeile(k,
            plakate.Count(p => p.KampagneId == k.Id),
            plakate.Count(p => p.KampagneId == k.Id && (p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt)),
            gebiete.Count(g => g == k.Id), termine.Count(t => t == k.Id), tickets.Count(t => t == k.Id))).ToList();
    }

    public async Task<Kampagne?> KampagneAsync(Guid id, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        return await daten.FindenAsync<Kampagne>(id, abbruch);
    }

    /// <summary>Die laufende Kampagne für die Handy-Erfassung: die jüngste aktive (sonst die jüngste in Planung).</summary>
    public async Task<Kampagne?> AktuelleAsync(CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        return await daten.ErsterAsync(daten.Menge<Kampagne>().Where(k => k.Status != Kampagnenstatus.Abgeschlossen)
            .OrderBy(k => k.Status == Kampagnenstatus.Aktiv ? 0 : 1).ThenByDescending(k => k.AngelegtAm).ThenBy(k => k.Id), abbruch);
    }

    public async Task<Freigabe> SteuernAsync(CancellationToken abbruch = default) => Rechte.KampagnenSteuern((await PersonPflichtAsync(abbruch)).Rolle);

    public async Task<(Kampagne Kampagne, IReadOnlyList<NaechsterSchritt> NaechsteSchritte)> KampagneAnlegenAsync(string name, string? beschreibung, DateOnly? beginn,
        DateOnly? ende, DateOnly? wahltag, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        var k = new Kampagne(name, beschreibung, beginn, ende, wahltag, wer.PersonId, uhr.GetUtcNow());
        daten.Hinzufuegen(k);
        await protokoll.ProtokollierenAsync("Kampagne.Angelegt", $"Kampagne „{k.Name}“ angelegt", nameof(Kampagne), k.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await BilderAbgleichenAsync("Kampagne", k.Id, beschreibung, abbruch);
        return (k, [
            new NaechsterSchritt("Kampagne öffnen", $"kampagnen/{k.Id}", Hauptschritt: true),
            new NaechsterSchritt("Ersten Termin anlegen", $"kampagnen/{k.Id}?ansicht=termine"),
            new NaechsterSchritt("Gebiete anlegen", $"kampagnen/{k.Id}?ansicht=gebiete"),
        ]);
    }

    public async Task KampagneAendernAsync(Guid id, string name, string? beschreibung, DateOnly? beginn, DateOnly? ende, DateOnly? wahltag, Kampagnenstatus status,
        CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        var k = await daten.LadenAsync<Kampagne>(id, "Kampagne", abbruch);
        k.Aendern(name, beschreibung, beginn, ende, wahltag);
        k.StatusSetzen(status);
        await protokoll.ProtokollierenAsync("Kampagne.Geaendert", $"Kampagne „{k.Name}“ geändert ({status})", nameof(Kampagne), k.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await BilderAbgleichenAsync("Kampagne", k.Id, beschreibung, abbruch);
    }

    /// <summary>Nächste Schritte für die Übersicht einer Kampagne (was als Nächstes zu tun ist).</summary>
    public async Task<IReadOnlyList<NaechsterSchritt>> NaechsteSchritteAsync(Guid kampagneId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var k = await daten.LadenAsync<Kampagne>(kampagneId, "Kampagne", abbruch);
        var heute = Ortszeit.Heute(uhr);
        var ab = heute.ToDateTime(TimeOnly.MinValue);
        var liste = new List<NaechsterSchritt>();
        var meineTickets = await daten.AnzahlAsync(daten.Menge<Ticket>().Where(t => t.KampagneId == kampagneId && t.ZustaendigPersonId == wer.PersonId && t.Status != Ticketstatus.Erledigt), abbruch);
        if (meineTickets > 0)
        {
            liste.Add(new NaechsterSchritt($"{meineTickets} Ticket(s) für mich", $"kampagnen/{kampagneId}?ansicht=tickets&meine=1", Hauptschritt: true));
        }

        var termine = (await TermineAsync(kampagneId, abbruch)).Where(z => z.Termin.Beginn >= ab).ToList();
        if (termine.FirstOrDefault(z => z.Frei > 0 && !z.Ich) is { } frei)
        {
            liste.Add(new NaechsterSchritt($"Einbuchen: {frei.Termin.Titel} am {frei.Termin.Beginn:dd.MM.}", $"kampagnen/{kampagneId}?ansicht=termine",
                $"{frei.Frei} Plätze frei", frei.Einbuchen));
        }

        if (Rechte.KampagnenSteuern(wer.Rolle).Erlaubt)
        {
            var offen = await daten.AnzahlAsync(daten.Menge<Gebiet>().Where(g => g.KampagneId == kampagneId && g.Status == Gebietsstatus.Offen), abbruch);
            if (offen > 0)
            {
                liste.Add(new NaechsterSchritt($"{offen} Gebiet(e) zuteilen", $"kampagnen/{kampagneId}?ansicht=gebiete"));
            }

            if (termine.Count == 0)
            {
                liste.Add(new NaechsterSchritt("Termin anlegen (z. B. Infostand)", $"kampagnen/{kampagneId}?ansicht=termine"));
            }

            if (k.Status == Kampagnenstatus.Planung)
            {
                liste.Add(new NaechsterSchritt("Kampagne auf „aktiv“ setzen", $"kampagnen/{kampagneId}?ansicht=uebersicht", "Dann erscheint sie am Handy für die Plakaterfassung."));
            }
        }

        if (k.Wahltag is { } w && heute > w)
        {
            var haengen = await daten.AnzahlAsync(daten.Menge<Plakat>().Where(p => p.KampagneId == kampagneId && (p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt)), abbruch);
            if (haengen > 0)
            {
                liste.Insert(0, new NaechsterSchritt($"{haengen} Plakate abhängen (bis {w.AddDays(optionen.Value.AbhaengenTage):dd.MM.yyyy})", "handy/plakat?abhaengen=1", Hauptschritt: true));
            }
        }

        return liste;
    }

    // ---------------- Plakate ----------------

    /// <param name="kampagneId">Leer = alle nicht abgeschlossenen Kampagnen (Handy).</param>
    public async Task<IReadOnlyList<PlakatZeile>> PlakateAsync(Guid? kampagneId = null, double? breite = null, double? laenge = null, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var ids = kampagneId is { } k ? [k] : await daten.ListeAsync(daten.Menge<Kampagne>().Where(x => x.Status != Kampagnenstatus.Abgeschlossen).Select(x => x.Id), abbruch);
        var liste = (await daten.ListeAsync(daten.Menge<Plakat>().Where(p => ids.Contains(p.KampagneId)).OrderByDescending(p => p.GeaendertAm).ThenBy(p => p.Id), abbruch))
            .Select(p => new PlakatZeile(p, namen.GetValueOrDefault(p.ErfasstVonPersonId, "?"), breite is { } b && laenge is { } l ? p.EntfernungMeter(b, l) : null));
        return (breite is null ? liste : liste.OrderBy(z => z.EntfernungMeter)).ToList();
    }

    /// <param name="kampagneId">Leer = die laufende Kampagne (Handy).</param>
    public async Task<Plakat> PlakatErfassenAsync(Guid? kampagneId, double breite, double laenge, string? standort, Plakatart art, Plakatstatus status, string? motiv,
        Stream? foto, string? fotoName, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var kampagne = kampagneId is { } id ? await daten.LadenAsync<Kampagne>(id, "Kampagne", abbruch)
            : await AktuelleAsync(abbruch) ?? throw new InvalidOperationException("Es gibt keine laufende Kampagne. Bitte zuerst eine Kampagne anlegen.");
        var p = new Plakat(kampagne.Id, breite, laenge, standort ?? string.Empty, art, status, wer.PersonIdPflicht, uhr.GetUtcNow(), motiv);
        if (foto is not null && fotoName is not null)
        {
            await FotoSpeichernAsync(p, foto, fotoName, abbruch);
        }

        daten.Hinzufuegen(p);
        await protokoll.ProtokollierenAsync("Kampagne.Plakat", $"Plakat erfasst ({status})", nameof(Plakat), p.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        if (status == Plakatstatus.Beschaedigt)
        {
            await SchadenTicketAsync(p, abbruch);
        }

        return p;
    }

    /// <summary>Status setzen; „beschädigt“ erzeugt ein Ticket (wenn zu diesem Plakat noch keins offen ist).</summary>
    public async Task StatusSetzenAsync(Guid id, Plakatstatus status, Stream? foto = null, string? fotoName = null, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var p = await daten.LadenAsync<Plakat>(id, "Plakat", abbruch);
        var vorher = p.Status;
        p.StatusSetzen(status, wer.PersonIdPflicht, uhr.GetUtcNow());
        if (foto is not null && fotoName is not null)
        {
            await FotoSpeichernAsync(p, foto, fotoName, abbruch);
        }

        await protokoll.ProtokollierenAsync("Kampagne.PlakatStatus", $"Plakat: {status}", nameof(Plakat), p.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        if (status == Plakatstatus.Beschaedigt && vorher != Plakatstatus.Beschaedigt)
        {
            await SchadenTicketAsync(p, abbruch);
        }
    }

    public async Task<(Stream Inhalt, string Typ)?> FotoAsync(Guid id, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        return await daten.FindenAsync<Plakat>(id, abbruch) is { FotoSchluessel: { } s, FotoTyp: { } t } && await ablage.OeffnenAsync(s, abbruch) is { } strom
            ? (strom, t)
            : null;
    }

    /// <summary>Alle Plakate, die noch hängen (auch beschädigte), nach Entfernung vom eigenen Standort; Wahltag aus der Kampagne.</summary>
    public async Task<Abhaengliste> AbhaenglisteAsync(Guid? kampagneId = null, double? breite = null, double? laenge = null, CancellationToken abbruch = default)
    {
        var kampagne = kampagneId is { } id ? await daten.FindenAsync<Kampagne>(id, abbruch) : await AktuelleAsync(abbruch);
        var wahltag = kampagne?.Wahltag;
        var heute = Ortszeit.Heute(uhr);
        var plakate = (await PlakateAsync(kampagneId, breite, laenge, abbruch)).Where(z => z.Plakat.HaengtNoch).ToList();
        return new Abhaengliste(wahltag, wahltag?.AddDays(optionen.Value.AbhaengenTage), wahltag is { } w && heute > w,
            breite is null ? plakate.OrderBy(z => z.Plakat.Standort, StringComparer.CurrentCulture).ToList() : plakate);
    }

    // ---------------- Gebiete ----------------

    public async Task<IReadOnlyList<GebietZeile>> GebieteAsync(Guid kampagneId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var steuern = Rechte.KampagnenSteuern(wer.Rolle);
        return (await daten.ListeAsync(daten.Menge<Gebiet>().Where(g => g.KampagneId == kampagneId).OrderBy(g => g.Art).ThenBy(g => g.Name).ThenBy(g => g.Id), abbruch))
            .Select(g => GebietZeile(g, namen, steuern, wer)).ToList();
    }

    public async Task<Gebiet> GebietAnlegenAsync(Guid kampagneId, string name, Gebietsart art, string? beschreibung, double? breite, double? laenge, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        await daten.LadenAsync<Kampagne>(kampagneId, "Kampagne", abbruch);
        var g = new Gebiet(kampagneId, name, art, beschreibung, breite, laenge);
        daten.Hinzufuegen(g);
        await daten.SpeichernAsync(abbruch);
        return g;
    }

    public async Task ZuteilenAsync(Guid gebietId, Guid? personId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var g = await daten.LadenAsync<Gebiet>(gebietId, "Gebiet", abbruch);
        Pruefen(GebietZeile(g, await NamenAsync(abbruch), Rechte.KampagnenSteuern(wer.Rolle), wer).Zuteilen);
        g.Zuteilen(personId);
        if (personId is { } p && await stammdaten.PersonAsync(p, abbruch) is { } person)
        {
            await postausgang.PushEinreihenAsync(person, "Kampagne: Gebiet zugeteilt", $"{g.Name} ({g.Art})", $"kampagnen/{g.KampagneId}?ansicht=gebiete", abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Kampagne.Gebiet", "Gebiet zugeteilt", nameof(Gebiet), g.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task GebietErledigenAsync(Guid gebietId, string? vermerk, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var g = await daten.LadenAsync<Gebiet>(gebietId, "Gebiet", abbruch);
        Pruefen(GebietZeile(g, await NamenAsync(abbruch), Rechte.KampagnenSteuern(wer.Rolle), wer).Erledigen);
        g.Erledigen(uhr.GetUtcNow(), vermerk);
        await daten.SpeichernAsync(abbruch);
    }

    private static GebietZeile GebietZeile(Gebiet g, Dictionary<Guid, string> namen, Freigabe steuern, Benutzer wer)
        => new(g, g.ZugeteiltPersonId is { } p ? namen.GetValueOrDefault(p) : null,
            g.Status == Gebietsstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.") : steuern,
            g.Status == Gebietsstatus.Erledigt ? Freigabe.Nein("Bereits erledigt.")
            : g.ZugeteiltPersonId is null ? Freigabe.Nein("Erst zuteilen.")
            : g.ZugeteiltPersonId == wer.PersonId || steuern.Erlaubt ? Freigabe.Ja : Freigabe.Nein("Abhaken kann, wem das Gebiet zugeteilt ist."));

    // ---------------- Termine (Kalendertermine mit Kampagnenbezug) ----------------

    /// <summary>Termine der Kampagne ab gestern; Einbuchen = Zusage im Kalender (Plätze begrenzt).</summary>
    public async Task<IReadOnlyList<KampagnenTerminZeile>> TermineAsync(Guid kampagneId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        var ab = Ortszeit.Heute(uhr).AddDays(-1).ToDateTime(TimeOnly.MinValue);
        var jetzt = Ortszeit.Jetzt(uhr);
        var termine = await daten.ListeAsync(daten.Menge<Termin>().Where(t => t.KampagneId == kampagneId && t.Ende >= ab).OrderBy(t => t.Beginn).ThenBy(t => t.Id), abbruch);
        var ids = termine.Select(t => t.Id).ToList();
        var zusagen = await daten.ListeAsync(daten.Menge<Teilnahme>().Where(x => ids.Contains(x.TerminId) && x.Status == Teilnahmestatus.Zugesagt).OrderBy(x => x.GeantwortetAm).ThenBy(x => x.Id), abbruch);
        var todos = await daten.ListeAsync(daten.Menge<TerminTodo>().Where(x => ids.Contains(x.TerminId)).Select(x => new { x.TerminId, x.ErledigtAm }), abbruch);
        return termine.Select(t =>
        {
            var gebucht = zusagen.Where(x => x.TerminId == t.Id).ToList();
            var ich = gebucht.Any(x => x.PersonId == wer.PersonId);
            int? frei = t.Plaetze is { } n ? Math.Max(0, n - gebucht.Count) : null;
            var einbuchen = KalenderDienst.AntwortFreigabe(t, jetzt) is { Erlaubt: false } nein ? nein
                : ich || frei is null or > 0 ? Freigabe.Ja
                : Freigabe.Nein($"Alle {t.Plaetze} Plätze sind vergeben.");
            return new KampagnenTerminZeile(t, gebucht.Select(x => namen.GetValueOrDefault(x.PersonId, "?")).ToList(), frei, ich, einbuchen,
                todos.Count(x => x.TerminId == t.Id && x.ErledigtAm == null), todos.Count(x => x.TerminId == t.Id));
        }).ToList();
    }

    /// <summary>Termin anlegen (erscheint im Kalender); optional gleich ToDos aus einer Vorlage erzeugen.</summary>
    public async Task<Termin> TerminAnlegenAsync(Guid kampagneId, string titel, string? ort, DateTime beginn, DateTime ende, int? plaetze, string? beschreibung,
        Guid? vorlageId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        await daten.LadenAsync<Kampagne>(kampagneId, "Kampagne", abbruch);
        var t = new Termin(titel, Terminart.Kampagne, beginn, ende, ort, null, wer.PersonId, uhr.GetUtcNow(), beschreibung, kampagneId: kampagneId, plaetze: plaetze);
        daten.Hinzufuegen(t);
        if (vorlageId is { } v)
        {
            await TodosAusVorlageHinzufuegenAsync(t.Id, v, 0, abbruch);
        }

        await protokoll.ProtokollierenAsync("Termin.Angelegt", $"Kampagnentermin am {t.Beginn:dd.MM.yyyy HH:mm} angelegt", nameof(Termin), t.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await BilderAbgleichenAsync("Termin", t.Id, beschreibung, abbruch);
        return t;
    }

    /// <summary>Einbuchen bzw. (wenn schon eingebucht) austragen – über die Zusage im Kalender. Liefert, ob die Person jetzt eingebucht ist.</summary>
    public async Task<bool> EinbuchenAsync(Guid terminId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var t = await daten.LadenAsync<Termin>(terminId, "Termin", abbruch);
        var zeile = (await TermineAsync(t.KampagneId ?? throw new InvalidOperationException("Kein Kampagnentermin."), abbruch)).FirstOrDefault(z => z.Termin.Id == terminId)
            ?? throw new InvalidOperationException("Termin nicht gefunden.");
        if (!zeile.Ich)
        {
            Pruefen(zeile.Einbuchen);
        }

        _ = wer;
        await dienste.GetRequiredService<KalenderDienst>().AntwortenAsync(terminId, !zeile.Ich, abbruch);
        return !zeile.Ich;
    }

    // ---------------- ToDos je Termin ----------------

    public async Task<IReadOnlyList<TodoZeile>> TodosAsync(Guid terminId, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var namen = await NamenAsync(abbruch);
        return (await daten.ListeAsync(daten.Menge<TerminTodo>().Where(x => x.TerminId == terminId).OrderBy(x => x.Position).ThenBy(x => x.Id), abbruch))
            .Select(x => new TodoZeile(x, x.ZustaendigPersonId is { } z ? namen.GetValueOrDefault(z) : null, x.ErledigtVonPersonId is { } e ? namen.GetValueOrDefault(e) : null))
            .ToList();
    }

    public async Task TodoHinzufuegenAsync(Guid terminId, string text, Guid? zustaendigPersonId, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        await KampagnenTerminAsync(terminId, abbruch);
        var position = await NaechstePositionAsync(terminId, abbruch);
        daten.Hinzufuegen(new TerminTodo(terminId, position, text, zustaendigPersonId));
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>ToDos aus einer Vorlage erzeugen (Kopiervorlage: die ToDos sind danach eigenständig). Liefert die Anzahl.</summary>
    public async Task<int> TodosAusVorlageAsync(Guid terminId, Guid vorlageId, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        await KampagnenTerminAsync(terminId, abbruch);
        var n = await TodosAusVorlageHinzufuegenAsync(terminId, vorlageId, await NaechstePositionAsync(terminId, abbruch), abbruch);
        await daten.SpeichernAsync(abbruch);
        return n;
    }

    public async Task TodoUmschaltenAsync(Guid todoId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        (await daten.LadenAsync<TerminTodo>(todoId, "ToDo", abbruch)).Umschalten(wer.PersonIdPflicht, uhr.GetUtcNow());
        await daten.SpeichernAsync(abbruch);
    }

    public async Task TodoZuweisenAsync(Guid todoId, Guid? personId, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        (await daten.LadenAsync<TerminTodo>(todoId, "ToDo", abbruch)).Zuweisen(personId);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task TodoLoeschenAsync(Guid todoId, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        daten.Entfernen(await daten.LadenAsync<TerminTodo>(todoId, "ToDo", abbruch));
        await daten.SpeichernAsync(abbruch);
    }

    private async Task<int> TodosAusVorlageHinzufuegenAsync(Guid terminId, Guid vorlageId, int start, CancellationToken abbruch)
    {
        await daten.LadenAsync<TodoVorlage>(vorlageId, "ToDo-Vorlage", abbruch);
        var punkte = await daten.ListeAsync(daten.Menge<TodoVorlagenPunkt>().Where(p => p.VorlageId == vorlageId).OrderBy(p => p.Position).ThenBy(p => p.Id), abbruch);
        foreach (var (p, i) in punkte.Select((p, i) => (p, i)))
        {
            daten.Hinzufuegen(new TerminTodo(terminId, start + i, p.Text));
        }

        return punkte.Count;
    }

    private async Task<int> NaechstePositionAsync(Guid terminId, CancellationToken abbruch)
        => (await daten.ListeAsync(daten.Menge<TerminTodo>().Where(x => x.TerminId == terminId).Select(x => x.Position), abbruch)).DefaultIfEmpty(-1).Max() + 1;

    private async Task<Termin> KampagnenTerminAsync(Guid terminId, CancellationToken abbruch)
    {
        var t = await daten.LadenAsync<Termin>(terminId, "Termin", abbruch);
        return t.KampagneId is null ? throw new InvalidOperationException("ToDos gibt es nur bei Kampagnenterminen.") : t;
    }

    // ---------------- ToDo-Vorlagen ----------------

    public async Task<IReadOnlyList<VorlageZeile>> VorlagenAsync(CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var vorlagen = await daten.ListeAsync(daten.Menge<TodoVorlage>().OrderBy(v => v.Name).ThenBy(v => v.Id), abbruch);
        var punkte = await daten.ListeAsync(daten.Menge<TodoVorlagenPunkt>().OrderBy(p => p.Position).ThenBy(p => p.Id), abbruch);
        return vorlagen.Select(v => new VorlageZeile(v, punkte.Where(p => p.VorlageId == v.Id).ToList())).ToList();
    }

    /// <summary>Vorlage anlegen oder ändern; <paramref name="eintraege"/> ersetzt die bisherigen Einträge (eine Zeile je ToDo).</summary>
    public async Task<TodoVorlage> VorlageSpeichernAsync(Guid? id, string name, IReadOnlyList<string> eintraege, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(eintraege);
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        var texte = eintraege.Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
        if (texte.Count == 0)
        {
            throw new ArgumentException("Bitte mindestens einen Eintrag angeben (eine Zeile je ToDo).", nameof(eintraege));
        }

        TodoVorlage v;
        if (id is { } vid)
        {
            v = await daten.LadenAsync<TodoVorlage>(vid, "ToDo-Vorlage", abbruch);
            v.Umbenennen(name);
            foreach (var alt in await daten.ListeAsync(daten.Menge<TodoVorlagenPunkt>().Where(p => p.VorlageId == vid).OrderBy(p => p.Id), abbruch))
            {
                daten.Entfernen(alt);
            }
        }
        else
        {
            v = new TodoVorlage(name);
            daten.Hinzufuegen(v);
        }

        for (var i = 0; i < texte.Count; i++)
        {
            daten.Hinzufuegen(new TodoVorlagenPunkt(v.Id, i, texte[i]));
        }

        await daten.SpeichernAsync(abbruch);
        return v;
    }

    public async Task VorlageLoeschenAsync(Guid id, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        Pruefen(Rechte.KampagnenSteuern(wer.Rolle));
        foreach (var p in await daten.ListeAsync(daten.Menge<TodoVorlagenPunkt>().Where(p => p.VorlageId == id).OrderBy(p => p.Id), abbruch))
        {
            daten.Entfernen(p);
        }

        daten.Entfernen(await daten.LadenAsync<TodoVorlage>(id, "ToDo-Vorlage", abbruch));
        await daten.SpeichernAsync(abbruch);
    }

    // ---------------- Tickets ----------------

    public async Task<IReadOnlyList<TicketZeile>> TicketsAsync(Guid kampagneId, bool nurMeine = false, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var abfrage = daten.Menge<Ticket>().Where(t => t.KampagneId == kampagneId);
        if (nurMeine)
        {
            abfrage = abfrage.Where(t => t.ZustaendigPersonId == wer.PersonId);
        }

        var tickets = await daten.ListeAsync(abfrage.OrderBy(t => t.Status == Ticketstatus.Erledigt).ThenByDescending(t => t.Prioritaet).ThenBy(t => t.Nummer).ThenBy(t => t.Id), abbruch);
        return await TicketZeilenAsync(tickets, abbruch);
    }

    public async Task<TicketDetail?> TicketAsync(Guid id, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        if (await daten.FindenAsync<Ticket>(id, abbruch) is not { } t)
        {
            return null;
        }

        var namen = await NamenAsync(abbruch);
        var kommentare = (await daten.ListeAsync(daten.Menge<TicketKommentar>().Where(k => k.TicketId == id).OrderBy(k => k.Am).ThenBy(k => k.Id), abbruch))
            .Select(k => new TicketKommentarZeile(k, namen.GetValueOrDefault(k.PersonId, "?"))).ToList();
        return new TicketDetail((await TicketZeilenAsync([t], abbruch))[0], kommentare);
    }

    public async Task<Ticket> TicketAnlegenAsync(Guid kampagneId, NeuesTicket neu, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(neu);
        var wer = await PersonPflichtAsync(abbruch);
        await daten.LadenAsync<Kampagne>(kampagneId, "Kampagne", abbruch);
        var nummer = (await daten.ListeAsync(daten.Menge<Ticket>().Where(t => t.KampagneId == kampagneId).Select(t => t.Nummer), abbruch)).DefaultIfEmpty(0).Max() + 1;
        var t = new Ticket(kampagneId, nummer, neu.Titel, neu.Beschreibung, neu.Prioritaet, wer.PersonIdPflicht, uhr.GetUtcNow());
        t.Bearbeiten(neu.Titel, neu.Beschreibung, neu.Prioritaet, neu.Faellig, uhr.GetUtcNow());
        t.Beziehen(neu.GebietId, neu.PlakatId, neu.TerminId);
        daten.Hinzufuegen(t);
        await protokoll.ProtokollierenAsync("Ticket.Angelegt", $"Ticket #{t.Nummer} angelegt", nameof(Ticket), t.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await BilderAbgleichenAsync("Ticket", t.Id, neu.Beschreibung, abbruch);
        if (neu.ZustaendigPersonId is { } z)
        {
            await TicketZuweisenAsync(t.Id, z, abbruch);
        }

        return t;
    }

    public async Task TicketBearbeitenAsync(Guid id, string titel, string? beschreibung, Ticketprioritaet prioritaet, DateOnly? faellig, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var t = await daten.LadenAsync<Ticket>(id, "Ticket", abbruch);
        t.Bearbeiten(titel, beschreibung, prioritaet, faellig, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Ticket.Geaendert", $"Ticket #{t.Nummer} bearbeitet", nameof(Ticket), t.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
        await BilderAbgleichenAsync("Ticket", t.Id, beschreibung, abbruch);
    }

    public async Task TicketStatusAsync(Guid id, Ticketstatus status, CancellationToken abbruch = default)
    {
        await PersonPflichtAsync(abbruch);
        var t = await daten.LadenAsync<Ticket>(id, "Ticket", abbruch);
        t.StatusSetzen(status, uhr.GetUtcNow());
        await protokoll.ProtokollierenAsync("Ticket.Status", $"Ticket #{t.Nummer}: {status}", nameof(Ticket), t.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    /// <summary>Zuständige Person setzen; sie wird per Push und (falls hinterlegt) Mail benachrichtigt.</summary>
    public async Task TicketZuweisenAsync(Guid id, Guid? personId, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var t = await daten.LadenAsync<Ticket>(id, "Ticket", abbruch);
        t.Zuweisen(personId, uhr.GetUtcNow());
        if (personId is { } p && p != wer.PersonId && await stammdaten.PersonAsync(p, abbruch) is { } person)
        {
            var link = $"kampagnen/{t.KampagneId}?ansicht=tickets&ticket={t.Id}";
            await postausgang.BenachrichtigenAsync(person, $"Ticket #{t.Nummer} für dich", t.Titel,
                $"Dir wurde in der Kampagne das Ticket #{t.Nummer} „{t.Titel}“ zugewiesen.\n\n{optionen.Value.Link(link)}", link, abbruch: abbruch);
        }

        await protokoll.ProtokollierenAsync("Ticket.Zugewiesen", $"Ticket #{t.Nummer} zugewiesen", nameof(Ticket), t.Id, abbruch: abbruch);
        await daten.SpeichernAsync(abbruch);
    }

    public async Task KommentierenAsync(Guid ticketId, string text, CancellationToken abbruch = default)
    {
        var wer = await PersonPflichtAsync(abbruch);
        var t = await daten.LadenAsync<Ticket>(ticketId, "Ticket", abbruch);
        daten.Hinzufuegen(new TicketKommentar(ticketId, wer.PersonIdPflicht, text, uhr.GetUtcNow()));
        t.StatusSetzen(t.Status, uhr.GetUtcNow());
        await daten.SpeichernAsync(abbruch);
    }

    private async Task<IReadOnlyList<TicketZeile>> TicketZeilenAsync(IReadOnlyList<Ticket> tickets, CancellationToken abbruch)
    {
        var namen = await NamenAsync(abbruch);
        var ids = tickets.Select(t => t.Id).ToList();
        var kommentare = await daten.ListeAsync(daten.Menge<TicketKommentar>().Where(k => ids.Contains(k.TicketId)).Select(k => k.TicketId), abbruch);
        var gebiete = tickets.Where(t => t.GebietId is not null).Select(t => t.GebietId!.Value).ToList();
        var plakate = tickets.Where(t => t.PlakatId is not null).Select(t => t.PlakatId!.Value).ToList();
        var termine = tickets.Where(t => t.TerminId is not null).Select(t => t.TerminId!.Value).ToList();
        var gebietNamen = (await daten.ListeAsync(daten.Menge<Gebiet>().Where(g => gebiete.Contains(g.Id)), abbruch)).ToDictionary(g => g.Id, g => $"Gebiet {g.Name}");
        var plakatNamen = (await daten.ListeAsync(daten.Menge<Plakat>().Where(p => plakate.Contains(p.Id)), abbruch)).ToDictionary(p => p.Id, p => $"Plakat {p.Standort}");
        var terminNamen = (await daten.ListeAsync(daten.Menge<Termin>().Where(x => termine.Contains(x.Id)), abbruch)).ToDictionary(x => x.Id, x => $"Termin {x.Titel} ({x.Beginn:dd.MM.})");
        return tickets.Select(t => new TicketZeile(t,
            t.ZustaendigPersonId is { } z ? namen.GetValueOrDefault(z) : null,
            namen.GetValueOrDefault(t.ErstelltVonPersonId, "?"),
            t.GebietId is { } g ? gebietNamen.GetValueOrDefault(g) : t.PlakatId is { } p ? plakatNamen.GetValueOrDefault(p) : t.TerminId is { } x ? terminNamen.GetValueOrDefault(x) : null,
            kommentare.Count(k => k == t.Id))).ToList();
    }

    private async Task SchadenTicketAsync(Plakat p, CancellationToken abbruch)
    {
        if (await daten.AnzahlAsync(daten.Menge<Ticket>().Where(t => t.PlakatId == p.Id && t.Status != Ticketstatus.Erledigt), abbruch) > 0)
        {
            return;
        }

        await TicketAnlegenAsync(p.KampagneId, new NeuesTicket($"Plakat beschädigt: {p.Standort}", "Bitte ersetzen oder reparieren.", Ticketprioritaet.Hoch, null,
            Ortszeit.Heute(uhr).AddDays(3), PlakatId: p.Id), abbruch);
    }

    // ---------------- Fristen ----------------

    /// <summary>
    /// Fristenprüfung je Kampagne mit Wahltag: nach der Wahl eine Aufgabe „Plakate abhängen“ für den Vorstand; Löschfrist: abgehängte
    /// Plakate (mit Fotos) und Zusagen vergangener Kampagnentermine nach einem halben Jahr löschen.
    /// </summary>
    public async Task<int> FristenPruefenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var n = 0;
        foreach (var k in await daten.ListeAsync(daten.Menge<Kampagne>().Where(k => k.Wahltag != null).OrderBy(k => k.Id), abbruch))
        {
            var w = k.Wahltag!.Value;
            if (heute <= w)
            {
                continue;
            }

            var haengen = await daten.AnzahlAsync(daten.Menge<Plakat>().Where(p => p.KampagneId == k.Id && (p.Status == Plakatstatus.Haengt || p.Status == Plakatstatus.Beschaedigt)), abbruch);
            if (haengen > 0 && await daten.AnzahlAsync(daten.Menge<Aufgabe>().Where(a => a.Anlass == AbhaengAnlass && a.AnlassId == k.Id), abbruch) == 0)
            {
                await aufgaben.AnlegenAsync($"Plakate abhängen: {k.Name} ({haengen} hängen noch)", "Abhängliste in der Kampagne bzw. am Handy.", null, Rolle.Vorstand, AbhaengAnlass,
                    k.Id, $"kampagnen/{k.Id}?ansicht=abhaengen", w.AddDays(optionen.Value.AbhaengenTage), null, abbruch);
                n++;
            }
        }

        var grenze = uhr.GetUtcNow().AddDays(-LoeschfristTage);
        foreach (var p in (await daten.ListeAsync(daten.Menge<Plakat>().Where(p => p.Status == Plakatstatus.Abgehaengt).OrderBy(p => p.Id), abbruch)).Where(p => p.GeaendertAm < grenze))
        {
            if (await daten.AnzahlAsync(daten.Menge<Ticket>().Where(t => t.PlakatId == p.Id), abbruch) > 0)
            {
                continue; // Bezug aus einem Ticket: bleibt, bis die Kampagne aufgeräumt wird
            }

            if (p.FotoSchluessel is { } s)
            {
                await ablage.LoeschenAsync(s, abbruch);
            }

            daten.Entfernen(p);
            n++;
        }

        var terminGrenze = heute.AddDays(-LoeschfristTage).ToDateTime(TimeOnly.MinValue);
        var alte = await daten.ListeAsync(daten.Menge<Termin>().Where(t => t.KampagneId != null && t.Ende < terminGrenze).Select(t => t.Id), abbruch);
        foreach (var e in await daten.ListeAsync(daten.Menge<Teilnahme>().Where(e => alte.Contains(e.TerminId)).OrderBy(e => e.Id), abbruch))
        {
            daten.Entfernen(e);
            n++;
        }

        if (n > 0)
        {
            await protokoll.ProtokollierenAsync("Kampagne.Fristen", $"Kampagnen-Fristen: {n} Einträge bearbeitet bzw. nach Löschfrist entfernt", abbruch: abbruch);
            await daten.SpeichernAsync(abbruch);
        }

        return n;
    }

    // ---------------- intern ----------------

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

        var schluessel = $"Kampagnen/Plakate/{p.Id:N}-{Domain.Gemeinsam.Ids.Neu():N}{endung.ToLowerInvariant()}";
        await ablage.SpeichernAsync(schluessel, puffer, abbruch);
        p.FotoSetzen(schluessel, typ);
    }

    private Task BilderAbgleichenAsync(string art, Guid id, string? text, CancellationToken abbruch)
        => dienste.GetRequiredService<BildDienst>().VerwendungenAbgleichenAsync(art, id, text, abbruch);

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
