using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;

namespace FraktionPerfekt.Application;

/// <summary>
/// Gemeinsamer Arbeitsbereich: Alle Speicher eines Bereichs (Anfrage, Hintergrundjob) teilen sich eine Datenbankverbindung.
/// <see cref="SpeichernAsync"/> schreibt fachliche Änderung, Postausgang und Protokoll gemeinsam fest.
/// </summary>
public interface IArbeitsbereich
{
    Task SpeichernAsync(CancellationToken abbruch = default);
}

/// <summary>Personen, Gremien und Mitgliedschaften.</summary>
public interface IStammdatenSpeicher : IArbeitsbereich
{
    void Hinzufuegen(Person person);

    void Hinzufuegen(Gremium gremium);

    void Hinzufuegen(Mitgliedschaft mitgliedschaft);

    Task<IReadOnlyList<Person>> PersonenAsync(CancellationToken abbruch = default);

    Task<Person?> PersonAsync(Guid id, CancellationToken abbruch = default);

    Task<Person?> PersonNachKennungAsync(string kennung, CancellationToken abbruch = default);

    Task<IReadOnlyList<Gremium>> GremienAsync(CancellationToken abbruch = default);

    Task<Gremium?> GremiumAsync(Guid id, CancellationToken abbruch = default);

    /// <summary>Alle Mitgliedschaften mit allen Versionen (für Stichtagsabfragen).</summary>
    Task<IReadOnlyList<Mitgliedschaft>> MitgliedschaftenAsync(CancellationToken abbruch = default);

    Task<IReadOnlyList<Mitgliedschaft>> MitgliedschaftenDesGremiumsAsync(Guid gremiumId, CancellationToken abbruch = default);

    Task<Mitgliedschaft?> MitgliedschaftAsync(Guid id, CancellationToken abbruch = default);
}

/// <summary>Termine, Teilnahmen und Kalender-Abos.</summary>
public interface ITerminSpeicher : IArbeitsbereich
{
    void Hinzufuegen(Termin termin);

    void Hinzufuegen(Teilnahme teilnahme);

    void Hinzufuegen(KalenderAbo abo);

    /// <summary>Termine, die im Zeitraum beginnen (Beginn ≥ von und &lt; bis), nach Beginn sortiert.</summary>
    Task<IReadOnlyList<Termin>> TermineAsync(DateTime von, DateTime bis, CancellationToken abbruch = default);

    Task<Termin?> TerminAsync(Guid id, CancellationToken abbruch = default);

    Task<IReadOnlyList<Termin>> TermineMitRisIdAsync(CancellationToken abbruch = default);

    Task<IReadOnlyList<Teilnahme>> TeilnahmenAsync(IReadOnlyCollection<Guid> terminIds, CancellationToken abbruch = default);

    Task<Teilnahme?> TeilnahmeAsync(Guid terminId, Guid personId, CancellationToken abbruch = default);

    Task<KalenderAbo?> AboNachHashAsync(string tokenHash, CancellationToken abbruch = default);

    Task<IReadOnlyList<KalenderAbo>> AbosDerPersonAsync(Guid personId, CancellationToken abbruch = default);
}

/// <summary>Abwesenheiten, Vertretungsfälle und Aufgaben.</summary>
public interface IVertretungSpeicher : IArbeitsbereich
{
    void Hinzufuegen(Abwesenheit abwesenheit);

    void Hinzufuegen(Vertretungsfall fall);

    void Hinzufuegen(Aufgabe aufgabe);

    Task<Abwesenheit?> AbwesenheitAsync(Guid id, CancellationToken abbruch = default);

    Task<IReadOnlyList<Abwesenheit>> AbwesenheitenDerPersonAsync(Guid personId, CancellationToken abbruch = default);

    /// <summary>Aktive Abwesenheiten, die sich mit dem Zeitraum überschneiden.</summary>
    Task<IReadOnlyList<Abwesenheit>> AbwesenheitenImZeitraumAsync(DateOnly von, DateOnly bis, CancellationToken abbruch = default);

    Task<Vertretungsfall?> FallAsync(Guid id, CancellationToken abbruch = default);

    Task<Vertretungsfall?> FallZurAnfrageAsync(Guid anfrageId, CancellationToken abbruch = default);

    Task<IReadOnlyList<Vertretungsfall>> FaelleZuTerminenAsync(IReadOnlyCollection<Guid> terminIds, CancellationToken abbruch = default);

    Task<IReadOnlyList<Vertretungsfall>> FaelleDerAbwesenheitAsync(Guid abwesenheitId, CancellationToken abbruch = default);

    /// <summary>Fälle mit Status „angefragt“ oder „eskaliert“.</summary>
    Task<IReadOnlyList<Vertretungsfall>> OffeneFaelleAsync(CancellationToken abbruch = default);

    Task<IReadOnlyList<Aufgabe>> OffeneAufgabenAsync(Rolle? zustaendig = null, CancellationToken abbruch = default);

    Task<Aufgabe?> AufgabeAsync(Guid id, CancellationToken abbruch = default);

    Task<Aufgabe?> OffeneAufgabeZumAnlassAsync(string anlass, Guid anlassId, CancellationToken abbruch = default);
}
