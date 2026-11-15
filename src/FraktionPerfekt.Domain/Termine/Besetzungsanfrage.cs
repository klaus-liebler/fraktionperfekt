using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Termine;

public enum Besetzungsmodus
{
    /// <summary>Mitglieder melden Interesse, der Vorstand bestätigt.</summary>
    VorstandBestaetigt,

    /// <summary>Die ersten N Meldungen sind gesetzt, weitere stehen auf der Warteliste.</summary>
    WerZuerstKommt,
}

public enum Meldungsstatus
{
    Interessiert,
    Bestaetigt,
    Abgelehnt,
    Zurueckgezogen,
}

/// <summary>
/// Terminbesetzung (Modul 5): Für eine Einladung (z. B. Vereinsjubiläum, Bürgerversammlung) werden N Vertreter:innen
/// gesucht. Bestätigte Personen haben den Termin im Kalender; nach dem Termin entsteht ein Kurzbericht.
/// </summary>
public sealed class Besetzungsanfrage
{
    private Besetzungsanfrage()
    {
    }

    public Besetzungsanfrage(Guid terminId, int plaetze, DateTimeOffset frist, Besetzungsmodus modus, Guid erstelltVonPersonId, DateTimeOffset jetzt, string? hinweis)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(plaetze, 1);
        Id = Ids.Neu();
        TerminId = terminId;
        Plaetze = plaetze;
        Frist = frist;
        Modus = modus;
        ErstelltVonPersonId = erstelltVonPersonId;
        ErstelltAm = jetzt;
        Hinweis = string.IsNullOrWhiteSpace(hinweis) ? null : hinweis.Trim();
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public int Plaetze { get; private set; }

    public DateTimeOffset Frist { get; private set; }

    public Besetzungsmodus Modus { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public string? Hinweis { get; private set; }

    public string? Bericht { get; private set; }

    public Guid? BerichtVonPersonId { get; private set; }

    public DateTimeOffset? BerichtAm { get; private set; }

    public bool MeldungMoeglich(DateTimeOffset jetzt) => jetzt < Frist;

    public void BerichtSchreiben(string text, Guid personId, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Bericht = text.Trim();
        BerichtVonPersonId = personId;
        BerichtAm = jetzt;
    }

    /// <summary>
    /// Ordnet die Meldungen neu: Bei „wer zuerst kommt“ rücken Interessierte in Reihenfolge der Meldung nach, solange Plätze frei sind.
    /// Liefert die Meldungen, die dadurch bestätigt wurden.
    /// </summary>
    public IReadOnlyList<Besetzungsmeldung> Nachruecken(IReadOnlyList<Besetzungsmeldung> meldungen)
    {
        ArgumentNullException.ThrowIfNull(meldungen);
        if (Modus != Besetzungsmodus.WerZuerstKommt)
        {
            return [];
        }

        var frei = Plaetze - meldungen.Count(m => m.Status == Meldungsstatus.Bestaetigt);
        var nach = meldungen.Where(m => m.Status == Meldungsstatus.Interessiert).OrderBy(m => m.Am).ThenBy(m => m.Id).Take(Math.Max(0, frei)).ToList();
        nach.ForEach(m => m.Bestaetigen());
        return nach;
    }

    public bool PlatzFrei(IReadOnlyList<Besetzungsmeldung> meldungen)
        => meldungen.Count(m => m.Status == Meldungsstatus.Bestaetigt) < Plaetze;
}

public sealed class Besetzungsmeldung
{
    private Besetzungsmeldung()
    {
    }

    public Besetzungsmeldung(Guid anfrageId, Guid personId, DateTimeOffset am)
    {
        Id = Ids.Neu();
        AnfrageId = anfrageId;
        PersonId = personId;
        Am = am;
        Status = Meldungsstatus.Interessiert;
    }

    public Guid Id { get; private set; }

    public Guid AnfrageId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public Meldungsstatus Status { get; private set; }

    public void Bestaetigen() => Status = Meldungsstatus.Bestaetigt;

    public void Ablehnen() => Status = Meldungsstatus.Abgelehnt;

    public void Zurueckziehen() => Status = Meldungsstatus.Zurueckgezogen;

    public void ErneutMelden(DateTimeOffset am)
    {
        Status = Meldungsstatus.Interessiert;
        Am = am;
    }
}
