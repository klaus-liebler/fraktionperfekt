using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Microsoft.EntityFrameworkCore;

namespace FraktionPerfekt.Infrastructure.Datenbank;

/// <summary>
/// Speicher der Fachdaten über einen gemeinsamen DbContext je Arbeitsbereich: Was ein Dienst in mehreren Speichern
/// ändert, wird mit einem einzigen <see cref="SpeichernAsync"/> gemeinsam festgeschrieben (inklusive Postausgang und Protokoll).
/// </summary>
internal sealed class FraktionSpeicher(FraktionDbContext db) : IStammdatenSpeicher, ITerminSpeicher, IVertretungSpeicher
{
    public Task SpeichernAsync(CancellationToken abbruch = default) => db.SaveChangesAsync(abbruch);

    // Stammdaten
    public void Hinzufuegen(Person person) => db.Personen.Add(person);

    public void Hinzufuegen(Gremium gremium) => db.Gremien.Add(gremium);

    public void Hinzufuegen(Mitgliedschaft mitgliedschaft) => db.Mitgliedschaften.Add(mitgliedschaft);

    public async Task<IReadOnlyList<Person>> PersonenAsync(CancellationToken abbruch = default)
        => await db.Personen.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ThenBy(p => p.Id).ToListAsync(abbruch);

    public Task<Person?> PersonAsync(Guid id, CancellationToken abbruch = default) => db.Personen.FirstOrDefaultAsync(p => p.Id == id, abbruch);

    public Task<Person?> PersonNachKennungAsync(string kennung, CancellationToken abbruch = default)
    {
        var k = (kennung ?? string.Empty).Trim().ToLowerInvariant();
        return db.Personen.FirstOrDefaultAsync(p => p.Benutzerkennung == k, abbruch);
    }

    public async Task<IReadOnlyList<Gremium>> GremienAsync(CancellationToken abbruch = default)
        => await db.Gremien.OrderBy(g => g.Sortierung).ThenBy(g => g.Name).ThenBy(g => g.Id).ToListAsync(abbruch);

    public Task<Gremium?> GremiumAsync(Guid id, CancellationToken abbruch = default) => db.Gremien.FirstOrDefaultAsync(g => g.Id == id, abbruch);

    public async Task<IReadOnlyList<Mitgliedschaft>> MitgliedschaftenAsync(CancellationToken abbruch = default)
        => await db.Mitgliedschaften.OrderBy(m => m.Id).ToListAsync(abbruch);

    public async Task<IReadOnlyList<Mitgliedschaft>> MitgliedschaftenDesGremiumsAsync(Guid gremiumId, CancellationToken abbruch = default)
        => await db.Mitgliedschaften.Where(m => m.GremiumId == gremiumId).OrderBy(m => m.Id).ToListAsync(abbruch);

    public Task<Mitgliedschaft?> MitgliedschaftAsync(Guid id, CancellationToken abbruch = default)
        => db.Mitgliedschaften.FirstOrDefaultAsync(m => m.Id == id, abbruch);

    // Termine
    public void Hinzufuegen(Termin termin) => db.Termine.Add(termin);

    public void Hinzufuegen(Teilnahme teilnahme) => db.Teilnahmen.Add(teilnahme);

    public void Hinzufuegen(KalenderAbo abo) => db.KalenderAbos.Add(abo);

    public async Task<IReadOnlyList<Termin>> TermineAsync(DateTime von, DateTime bis, CancellationToken abbruch = default)
        => await db.Termine.Where(t => t.Beginn >= von && t.Beginn < bis).OrderBy(t => t.Beginn).ThenBy(t => t.Id).ToListAsync(abbruch);

    public Task<Termin?> TerminAsync(Guid id, CancellationToken abbruch = default) => db.Termine.FirstOrDefaultAsync(t => t.Id == id, abbruch);

    public async Task<IReadOnlyList<Termin>> TermineMitRisIdAsync(CancellationToken abbruch = default)
        => await db.Termine.Where(t => t.RisId != null).OrderBy(t => t.Id).ToListAsync(abbruch);

    public async Task<IReadOnlyList<Teilnahme>> TeilnahmenAsync(IReadOnlyCollection<Guid> terminIds, CancellationToken abbruch = default)
    {
        var ids = terminIds.ToList();
        return await db.Teilnahmen.Where(t => ids.Contains(t.TerminId)).OrderBy(t => t.Id).ToListAsync(abbruch);
    }

    public Task<Teilnahme?> TeilnahmeAsync(Guid terminId, Guid personId, CancellationToken abbruch = default)
        => db.Teilnahmen.FirstOrDefaultAsync(t => t.TerminId == terminId && t.PersonId == personId, abbruch);

    public Task<KalenderAbo?> AboNachHashAsync(string tokenHash, CancellationToken abbruch = default)
        => db.KalenderAbos.FirstOrDefaultAsync(a => a.TokenHash == tokenHash, abbruch);

    public async Task<IReadOnlyList<KalenderAbo>> AbosDerPersonAsync(Guid personId, CancellationToken abbruch = default)
        => await db.KalenderAbos.Where(a => a.PersonId == personId).OrderBy(a => a.Id).ToListAsync(abbruch);

    // Vertretung
    public void Hinzufuegen(Abwesenheit abwesenheit) => db.Abwesenheiten.Add(abwesenheit);

    public void Hinzufuegen(Vertretungsfall fall) => db.Vertretungsfaelle.Add(fall);

    public void Hinzufuegen(Aufgabe aufgabe) => db.Aufgaben.Add(aufgabe);

    public Task<Abwesenheit?> AbwesenheitAsync(Guid id, CancellationToken abbruch = default) => db.Abwesenheiten.FirstOrDefaultAsync(a => a.Id == id, abbruch);

    public async Task<IReadOnlyList<Abwesenheit>> AbwesenheitenDerPersonAsync(Guid personId, CancellationToken abbruch = default)
        => await db.Abwesenheiten.Where(a => a.PersonId == personId).OrderBy(a => a.Von).ThenBy(a => a.Id).ToListAsync(abbruch);

    public async Task<IReadOnlyList<Abwesenheit>> AbwesenheitenImZeitraumAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default)
        => await db.Abwesenheiten.Where(a => a.ZurueckgezogenAm == null && a.Von <= bis && a.Bis >= von)
            .OrderBy(a => a.Von).ThenBy(a => a.Id).ToListAsync(abbruch);

    public Task<Vertretungsfall?> FallAsync(Guid id, CancellationToken abbruch = default) => db.Vertretungsfaelle.FirstOrDefaultAsync(f => f.Id == id, abbruch);

    public Task<Vertretungsfall?> FallZurAnfrageAsync(Guid anfrageId, CancellationToken abbruch = default)
        => db.Vertretungsfaelle.FirstOrDefaultAsync(f => f.Anfragen.Any(a => a.Id == anfrageId), abbruch);

    public async Task<IReadOnlyList<Vertretungsfall>> FaelleZuTerminenAsync(IReadOnlyCollection<Guid> terminIds, CancellationToken abbruch = default)
    {
        var ids = terminIds.ToList();
        return await db.Vertretungsfaelle.Where(f => ids.Contains(f.TerminId)).OrderBy(f => f.Id).ToListAsync(abbruch);
    }

    public async Task<IReadOnlyList<Vertretungsfall>> FaelleDerAbwesenheitAsync(Guid abwesenheitId, CancellationToken abbruch = default)
        => await db.Vertretungsfaelle.Where(f => f.AbwesenheitId == abwesenheitId).OrderBy(f => f.Id).ToListAsync(abbruch);

    public async Task<IReadOnlyList<Vertretungsfall>> OffeneFaelleAsync(CancellationToken abbruch = default)
        => await db.Vertretungsfaelle.Where(f => f.Status == Vertretungsstatus.Angefragt || f.Status == Vertretungsstatus.Eskaliert)
            .OrderBy(f => f.Id).ToListAsync(abbruch);

    public async Task<IReadOnlyList<Aufgabe>> OffeneAufgabenAsync(Rolle? zustaendig = null, CancellationToken abbruch = default)
        => await db.Aufgaben.Where(a => a.Status == Aufgabenstatus.Offen && (zustaendig == null || a.Zustaendig == zustaendig))
            .OrderBy(a => a.FaelligAm).ThenBy(a => a.Id).ToListAsync(abbruch);

    public Task<Aufgabe?> AufgabeAsync(Guid id, CancellationToken abbruch = default) => db.Aufgaben.FirstOrDefaultAsync(a => a.Id == id, abbruch);

    public Task<Aufgabe?> OffeneAufgabeZumAnlassAsync(string anlass, Guid anlassId, CancellationToken abbruch = default)
        => db.Aufgaben.FirstOrDefaultAsync(a => a.Anlass == anlass && a.AnlassId == anlassId && a.Status == Aufgabenstatus.Offen, abbruch);
}

internal sealed class NachrichtenSpeicher(FraktionDbContext db) : INachrichtenSpeicher
{
    public void Hinzufuegen(AusgehendeNachricht nachricht) => db.Postausgang.Add(nachricht);

    public Task<AusgehendeNachricht?> LadenAsync(Guid id, CancellationToken abbruch = default) => db.Postausgang.FirstOrDefaultAsync(n => n.Id == id, abbruch);

    public async Task<IReadOnlyList<AusgehendeNachricht>> WartendeAsync(int maximal, CancellationToken abbruch = default)
        => await db.Postausgang.Where(n => n.Status == NachrichtenStatus.Wartend).OrderBy(n => n.ErstelltAm).ThenBy(n => n.Id).Take(maximal).ToListAsync(abbruch);

    public async Task<IReadOnlyList<AusgehendeNachricht>> NeuesteAsync(int anzahl, CancellationToken abbruch = default)
        => await db.Postausgang.AsNoTracking().OrderByDescending(n => n.ErstelltAm).ThenByDescending(n => n.Id).Take(anzahl).ToListAsync(abbruch);

    public Task<int> AnzahlAsync(NachrichtenStatus status, CancellationToken abbruch = default) => db.Postausgang.CountAsync(n => n.Status == status, abbruch);

    public Task SpeichernAsync(CancellationToken abbruch = default) => db.SaveChangesAsync(abbruch);
}

internal sealed class AuditProtokoll(FraktionDbContext db, IAktuellerBenutzer benutzer, TimeProvider uhr) : IAuditProtokoll
{
    public async Task ProtokollierenAsync(string aktion, string beschreibung, string? objektTyp = null, Guid? objektId = null, bool erfolgreich = true,
        CancellationToken abbruch = default)
    {
        var wer = await benutzer.ErmittelnAsync(abbruch);
        // Nur Pseudonyme: weder Anmeldename noch Klarname landen im Protokoll.
        db.Protokoll.Add(new AuditEintrag(uhr.GetUtcNow(), wer.PersonId is { } k ? Domain.Gemeinsam.Pseudonym.Kurz(k) : wer.Kennung,
            wer.PersonId is { } p ? Domain.Gemeinsam.Pseudonym.Person(p) : wer.Name,
            wer.Rolle?.Anzeigename(), aktion, objektTyp, objektId, beschreibung, erfolgreich));
    }

    public async Task<IReadOnlyList<AuditEintrag>> NeuesteAsync(int anzahl, CancellationToken abbruch = default)
        => await db.Protokoll.AsNoTracking().OrderByDescending(e => e.Zeitpunkt).ThenByDescending(e => e.Id).Take(anzahl).ToListAsync(abbruch);
}
