using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Stammdaten;

public sealed record PersonZeile(Guid Id, string Name, string Sortiername, string? EMail, IReadOnlyList<Rolle> Rollen, bool Aktiv,
    int Ordentlich, int Stellvertretung, bool AusDemRis);

public sealed record GremiumZeile(Guid Id, string Name, string Kurzname, Gremienart Art, bool AusDemRis, int Ordentlich, int Stellvertretung);

public sealed record MitgliedZeile(Guid MitgliedschaftId, Guid PersonId, string Name, Funktion Funktion, int? Rang, bool AusDemRis,
    IReadOnlyList<MitgliedschaftsVersion> Verlauf);

public sealed record GremiumDetail(Gremium Gremium, IReadOnlyList<MitgliedZeile> Ordentliche, IReadOnlyList<MitgliedZeile> Stellvertretungen,
    IReadOnlyList<MitgliedZeile> Ehemalige);

public sealed record PersonMitgliedschaft(Guid MitgliedschaftId, Guid GremiumId, string Gremium, Funktion? Funktion, int? Rang,
    IReadOnlyList<MitgliedschaftsVersion> Verlauf);

public sealed record PersonDetail(Person Person, IReadOnlyList<Rolle> Rollen, IReadOnlyList<PersonRollen> RollenVerlauf,
    IReadOnlyList<PersonMitgliedschaft> Mitgliedschaften);

/// <summary>Personen, Gremien und Mitgliedschaften (Mandatsübersicht). Funktionen kommen aus dem RIS, der Rang ist fraktionsintern.</summary>
public sealed class StammdatenDienst(IStammdatenSpeicher speicher, IAuditProtokoll protokoll, IAktuellerBenutzer benutzer, TimeProvider uhr)
{
    public async Task<IReadOnlyList<PersonZeile>> PersonenAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await speicher.MitgliedschaftenAsync(abbruch);
        var staende = mitgliedschaften.Select(m => (m.PersonId, Stand: m.StandAm(heute, jetzt))).Where(x => x.Stand is not null).ToList();
        return (await speicher.PersonenAsync(abbruch))
            .Select(p => new PersonZeile(p.Id, p.Anzeigename, p.Sortiername, p.EMail, p.RollenAm(heute, jetzt), p.Aktiv,
                staende.Count(s => s.PersonId == p.Id && s.Stand!.Funktion == Funktion.Ordentlich),
                staende.Count(s => s.PersonId == p.Id && s.Stand!.Funktion == Funktion.Stellvertretung),
                p.RisId is not null))
            .OrderBy(p => p.Sortiername, StringComparer.CurrentCulture).ThenBy(p => p.Id)
            .ToList();
    }

    public async Task<PersonDetail?> PersonAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await speicher.PersonAsync(id, abbruch) is not { } person)
        {
            return null;
        }

        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var gremien = (await speicher.GremienAsync(abbruch)).ToDictionary(g => g.Id);
        var mitgliedschaften = (await speicher.MitgliedschaftenAsync(abbruch)).Where(m => m.PersonId == id)
            .Select(m =>
            {
                var stand = m.StandAm(heute, jetzt);
                return new PersonMitgliedschaft(m.Id, m.GremiumId, gremien.TryGetValue(m.GremiumId, out var g) ? g.Name : "?", stand?.Funktion,
                    stand?.Rang, Verlauf(m));
            })
            .OrderBy(m => m.Funktion is null).ThenBy(m => m.Funktion).ThenBy(m => m.Gremium, StringComparer.CurrentCulture)
            .ToList();
        var rollenVerlauf = person.RollenVersionen.OrderByDescending(v => v.ErfasstAm).ThenByDescending(v => v.GueltigVon).ToList();
        return new PersonDetail(person, person.RollenAm(heute, jetzt), rollenVerlauf, mitgliedschaften);
    }

    public async Task<IReadOnlyList<GremiumZeile>> GremienAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var mitgliedschaften = await speicher.MitgliedschaftenAsync(abbruch);
        return (await speicher.GremienAsync(abbruch)).Where(g => g.Aktiv)
            .Select(g =>
            {
                var sitze = Besetzung.Am(mitgliedschaften, g.Id, heute, jetzt);
                return new GremiumZeile(g.Id, g.Name, g.Kurzname, g.Art, g.AusDemRis,
                    sitze.Count(s => s.Funktion == Funktion.Ordentlich), sitze.Count(s => s.Funktion == Funktion.Stellvertretung));
            })
            .ToList();
    }

    public async Task<GremiumDetail?> GremiumAsync(Guid id, CancellationToken abbruch = default)
    {
        if (await speicher.GremiumAsync(id, abbruch) is not { } gremium)
        {
            return null;
        }

        var heute = Ortszeit.Heute(uhr);
        var jetzt = uhr.GetUtcNow();
        var personen = (await speicher.PersonenAsync(abbruch)).ToDictionary(p => p.Id);
        var zeilen = (await speicher.MitgliedschaftenDesGremiumsAsync(id, abbruch))
            .Select(m => (m, Stand: m.StandAm(heute, jetzt)))
            .Select(x => new MitgliedZeile(x.m.Id, x.m.PersonId, personen.TryGetValue(x.m.PersonId, out var p) ? p.Anzeigename : "?",
                x.Stand?.Funktion ?? Funktion.Ordentlich, x.Stand?.Rang, x.m.RisId is not null, Verlauf(x.m)))
            .ToList();
        var aktuell = (await speicher.MitgliedschaftenDesGremiumsAsync(id, abbruch)).Where(m => m.StandAm(heute, jetzt) is not null)
            .Select(m => m.Id).ToHashSet();
        return new GremiumDetail(gremium,
            zeilen.Where(z => aktuell.Contains(z.MitgliedschaftId) && z.Funktion == Funktion.Ordentlich)
                .OrderBy(z => z.Name, StringComparer.CurrentCulture).ToList(),
            zeilen.Where(z => aktuell.Contains(z.MitgliedschaftId) && z.Funktion == Funktion.Stellvertretung)
                .OrderBy(z => z.Rang ?? int.MaxValue).ThenBy(z => z.Name, StringComparer.CurrentCulture).ToList(),
            zeilen.Where(z => !aktuell.Contains(z.MitgliedschaftId)).OrderBy(z => z.Name, StringComparer.CurrentCulture).ToList());
    }

    /// <summary>Den Vertretungsrang einer Stellvertretung ab einem Datum ändern (fraktionsintern, auch bei RIS-Gremien).</summary>
    public async Task RangAendernAsync(Guid mitgliedschaftId, int rang, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var m = await speicher.MitgliedschaftAsync(mitgliedschaftId, abbruch) ?? throw new InvalidOperationException("Mitgliedschaft nicht gefunden.");
        var stand = m.StandAm(ab, uhr.GetUtcNow()) ?? throw new InvalidOperationException($"Am {ab:dd.MM.yyyy} besteht keine Mitgliedschaft.");
        if (stand.Funktion != Funktion.Stellvertretung)
        {
            throw new InvalidOperationException("Einen Vertretungsrang haben nur Stellvertretungen.");
        }

        m.AendernAb(ab, Funktion.Stellvertretung, rang, Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Mitgliedschaft.RangGeaendert",
            $"Vertretungsrang von {Pseudonym.Person(m.PersonId)} ab {ab:dd.MM.yyyy} auf {rang} gesetzt", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Ob eine Mitgliedschaft in diesem Gremium von Hand gepflegt werden darf.</summary>
    public static Freigabe MitgliedschaftPflegen(Rolle? rolle, Gremium gremium)
    {
        ArgumentNullException.ThrowIfNull(gremium);
        return Rechte.StammdatenPflegen(rolle).Und(() => gremium.AusDemRis
            ? Freigabe.Nein("Mitgliedschaften in städtischen Gremien kommen aus dem Ratsinformationssystem und werden dort gepflegt. "
                + "Hier lässt sich nur der fraktionsinterne Vertretungsrang ändern.")
            : Freigabe.Ja);
    }

    /// <summary>Mitglied in einem fraktionsinternen Gremium (Arbeitskreis, Fraktion) aufnehmen.</summary>
    public async Task<Mitgliedschaft> MitgliedAufnehmenAsync(Guid gremiumId, Guid personId, Funktion funktion, int? rang, DateOnly ab,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var gremium = await speicher.GremiumAsync(gremiumId, abbruch) ?? throw new InvalidOperationException("Gremium nicht gefunden.");
        if (MitgliedschaftPflegen(wer.Rolle, gremium) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        var m = new Mitgliedschaft(personId, gremiumId, funktion, rang, ab, Erfassung.Erst(uhr.GetUtcNow(), wer.Anzeige, "Aufnahme in das Gremium"));
        speicher.Hinzufuegen(m);
        await protokoll.ProtokollierenAsync("Mitgliedschaft.Angelegt",
            $"{Pseudonym.Person(personId)} ab {ab:dd.MM.yyyy} als {funktion.Anzeigename()} aufgenommen", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
        return m;
    }

    /// <summary>Mitgliedschaft in einem fraktionsinternen Gremium beenden (ab = erster Tag ohne Mitgliedschaft).</summary>
    public async Task MitgliedschaftBeendenAsync(Guid mitgliedschaftId, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        var m = await speicher.MitgliedschaftAsync(mitgliedschaftId, abbruch) ?? throw new InvalidOperationException("Mitgliedschaft nicht gefunden.");
        var gremium = await speicher.GremiumAsync(m.GremiumId, abbruch) ?? throw new InvalidOperationException("Gremium nicht gefunden.");
        if (MitgliedschaftPflegen(wer.Rolle, gremium) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        m.BeendenAb(ab, Erfassung.Ende(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Mitgliedschaft.Beendet",
            $"Mitgliedschaft von {Pseudonym.Person(m.PersonId)} endet zum {ab:dd.MM.yyyy}", nameof(Mitgliedschaft), m.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Rollen einer Person ab einem Datum ändern (z. B. Wahl in den Vorstand).</summary>
    public async Task RollenAendernAsync(Guid personId, IReadOnlyCollection<Rolle> rollen, DateOnly ab, string grund, CancellationToken abbruch = default)
    {
        var wer = await PruefenAsync(abbruch);
        var person = await speicher.PersonAsync(personId, abbruch) ?? throw new InvalidOperationException("Person nicht gefunden.");
        person.RollenFestlegenAb(ab, rollen, Erfassung.Aenderung(uhr.GetUtcNow(), Grund(grund), wer.Anzeige));
        await protokoll.ProtokollierenAsync("Person.RollenGeaendert",
            $"Rollen von {Pseudonym.Person(personId)} ab {ab:dd.MM.yyyy}: {string.Join(", ", rollen.Select(r => r.Anzeigename()))}",
            nameof(Person), personId, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    /// <summary>Die eigene Person (für „Meine Einstellungen“).</summary>
    public async Task<Person?> IchAsync(CancellationToken abbruch = default)
        => (await benutzer.ErmittelnAsync(abbruch)).PersonId is { } id ? await speicher.PersonAsync(id, abbruch) : null;

    /// <summary>Jede Person stellt ihre Ansprache selbst ein (Du/Sie, Stil, Titel, Anrede).</summary>
    public async Task AnspracheSpeichernAsync(string? anrede, Anredeform form, Begruessungsstil stil, bool titelInAnsprache, CancellationToken abbruch = default)
    {
        var person = await IchAsync(abbruch) ?? throw new InvalidOperationException("Ihrem Konto ist keine Person zugeordnet.");
        person.AnspracheEinstellen(anrede, form, stil, titelInAnsprache);
        await protokoll.ProtokollierenAsync("Person.AnspracheGeaendert", "Eigene Ansprache eingestellt", nameof(Person), person.Id, abbruch: abbruch);
        await speicher.SpeichernAsync(abbruch);
    }

    private async Task<Benutzer> PruefenAsync(CancellationToken abbruch)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        if (!wer.IstAutomatik && Rechte.StammdatenPflegen(wer.Rolle) is { Erlaubt: false } nein)
        {
            throw new InvalidOperationException(nein.Begruendung);
        }

        return wer;
    }

    private static string Grund(string? grund) => string.IsNullOrWhiteSpace(grund) ? "ohne Angabe" : grund.Trim();

    private static IReadOnlyList<MitgliedschaftsVersion> Verlauf(Mitgliedschaft m)
        => m.Versionen.OrderByDescending(v => v.ErfasstAm).ThenByDescending(v => v.GueltigVon).ToList();
}
