using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Vertretung;

public enum Vertretungsstatus
{
    /// <summary>Eine Stellvertretung ist gefragt und hat noch nicht geantwortet.</summary>
    Angefragt,

    /// <summary>Jemand hat zugesagt.</summary>
    Besetzt,

    /// <summary>Alle Stellvertretungen haben abgelehnt oder nicht rechtzeitig geantwortet; der Vorstand ist gefragt.</summary>
    Eskaliert,

    /// <summary>Die Vertretung wird nicht mehr gebraucht (Abwesenheit zurückgezogen, Sitzung abgesagt).</summary>
    Entfallen,
}

public enum Anfragestatus
{
    Offen,
    Zugesagt,
    Abgelehnt,
    Abgelaufen,
    Zurueckgezogen,
}

/// <summary>Was ein Schritt im Vertretungsablauf bewirkt hat; die Anwendung verschickt danach die passenden Nachrichten.</summary>
public sealed record Vertretungsschritt(Vertretungsstatus Status, Vertretungsanfrage? NeueAnfrage = null, Guid? Vertreter = null);

/// <summary>
/// Vertretung eines ordentlichen Mitglieds in einer Gremiensitzung. Die Stellvertretungen werden in der Reihenfolge ihres
/// Rangs nacheinander gefragt: Zusage trägt ein, Ablehnung oder Fristablauf geht zur nächsten Person, ohne Zusage
/// eskaliert der Fall an den Vorstand.
/// </summary>
public sealed class Vertretungsfall
{
    private readonly List<Vertretungsanfrage> _anfragen = [];
    private readonly List<Vertretungskandidat> _kandidaten = [];

    private Vertretungsfall()
    {
    }

    private Vertretungsfall(Guid terminId, Guid gremiumId, Guid vertretenePersonId, Guid? abwesenheitId, IReadOnlyList<Guid> kandidaten,
        DateTimeOffset jetzt)
    {
        Id = Ids.Neu();
        TerminId = terminId;
        GremiumId = gremiumId;
        VertretenePersonId = vertretenePersonId;
        AbwesenheitId = abwesenheitId;
        _kandidaten.AddRange(kandidaten.Select((k, i) => new Vertretungskandidat(Id, i + 1, k)));
        ErstelltAm = jetzt;
        Status = Vertretungsstatus.Angefragt;
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public Guid GremiumId { get; private set; }

    /// <summary>Das ordentliche Mitglied, das fehlt.</summary>
    public Guid VertretenePersonId { get; private set; }

    public Guid? AbwesenheitId { get; private set; }

    public Vertretungsstatus Status { get; private set; }

    public Guid? VertreterPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset? AbgeschlossenAm { get; private set; }

    /// <summary>Stellvertretungen in Rangfolge, festgehalten beim Start (eigene Tabelle, eine Zeile je Person).</summary>
    public IReadOnlyList<Guid> Kandidaten => _kandidaten.OrderBy(k => k.Position).Select(k => k.PersonId).ToList();

    public IReadOnlyCollection<Vertretungskandidat> KandidatenZeilen => _kandidaten;

    public IReadOnlyCollection<Vertretungsanfrage> Anfragen => _anfragen;

    public Vertretungsanfrage? OffeneAnfrage => _anfragen.FirstOrDefault(a => a.Status == Anfragestatus.Offen);

    public bool IstOffen => Status is Vertretungsstatus.Angefragt or Vertretungsstatus.Eskaliert;

    /// <summary>
    /// Startet den Ablauf und fragt die erste Stellvertretung. Ohne Kandidat:innen eskaliert der Fall sofort.
    /// </summary>
    public static (Vertretungsfall Fall, Vertretungsschritt Schritt) Starten(Guid terminId, Guid gremiumId, Guid vertretenePersonId,
        Guid? abwesenheitId, IReadOnlyList<Guid> kandidatenInRangfolge, DateTimeOffset jetzt, DateTimeOffset frist)
    {
        ArgumentNullException.ThrowIfNull(kandidatenInRangfolge);
        var fall = new Vertretungsfall(terminId, gremiumId, vertretenePersonId, abwesenheitId,
            kandidatenInRangfolge.Where(k => k != vertretenePersonId).Distinct().ToList(), jetzt);
        return (fall, fall.NaechsteFragen(jetzt, frist));
    }

    /// <summary>Antwort der gefragten Person. Nur wer gerade gefragt ist, kann antworten.</summary>
    public Vertretungsschritt Antworten(Guid personId, bool zusage, DateTimeOffset jetzt, DateTimeOffset fristFuerNaechste)
    {
        var anfrage = OffeneAnfrage;
        if (anfrage is null || anfrage.PersonId != personId)
        {
            throw new InvalidOperationException(Status == Vertretungsstatus.Besetzt
                ? "Die Vertretung ist bereits besetzt."
                : "Diese Anfrage ist nicht mehr offen.");
        }

        if (zusage)
        {
            anfrage.Beantworten(Anfragestatus.Zugesagt, jetzt);
            return Besetzen(personId, jetzt);
        }

        anfrage.Beantworten(Anfragestatus.Abgelehnt, jetzt);
        return NaechsteFragen(jetzt, fristFuerNaechste);
    }

    /// <summary>Ist die Frist der offenen Anfrage abgelaufen, geht der Fall zur nächsten Person; sonst <c>null</c>.</summary>
    public Vertretungsschritt? FristPruefen(DateTimeOffset jetzt, DateTimeOffset fristFuerNaechste)
    {
        if (OffeneAnfrage is not { } anfrage || anfrage.FristBis > jetzt)
        {
            return null;
        }

        anfrage.Beantworten(Anfragestatus.Abgelaufen, jetzt);
        return NaechsteFragen(jetzt, fristFuerNaechste);
    }

    /// <summary>Der Vorstand trägt nach einer Eskalation selbst eine Vertretung ein (z. B. nach Telefonat).</summary>
    public Vertretungsschritt ManuellBesetzen(Guid personId, DateTimeOffset jetzt)
    {
        if (!IstOffen)
        {
            throw new InvalidOperationException("Die Vertretung ist nicht mehr offen.");
        }

        if (personId == VertretenePersonId)
        {
            throw new InvalidOperationException("Eine Person kann sich nicht selbst vertreten.");
        }

        OffeneAnfrage?.Beantworten(Anfragestatus.Zurueckgezogen, jetzt);
        return Besetzen(personId, jetzt);
    }

    /// <summary>Vertretung wird nicht mehr gebraucht; eine offene Anfrage wird zurückgezogen.</summary>
    public void Entfallen(DateTimeOffset jetzt)
    {
        if (Status is Vertretungsstatus.Entfallen)
        {
            return;
        }

        OffeneAnfrage?.Beantworten(Anfragestatus.Zurueckgezogen, jetzt);
        Status = Vertretungsstatus.Entfallen;
        AbgeschlossenAm = jetzt;
    }

    /// <summary>
    /// Antwortfrist: höchstens <paramref name="stunden"/> Stunden, spätestens zwei Stunden vor Sitzungsbeginn; ist das schon
    /// vorbei, bleibt eine Stunde (höchstens bis zum Beginn).
    /// </summary>
    public static DateTimeOffset FristBerechnen(DateTimeOffset jetzt, DateTimeOffset sitzungsbeginn, int stunden)
    {
        var frist = jetzt.AddHours(stunden);
        var spaetestens = sitzungsbeginn.AddHours(-2);
        if (spaetestens < frist)
        {
            frist = spaetestens;
        }

        if (frist <= jetzt)
        {
            frist = sitzungsbeginn > jetzt.AddHours(1) ? jetzt.AddHours(1) : sitzungsbeginn;
        }

        return frist <= jetzt ? jetzt : frist;
    }

    private Vertretungsschritt NaechsteFragen(DateTimeOffset jetzt, DateTimeOffset frist)
    {
        var gefragt = _anfragen.Select(a => a.PersonId).ToHashSet();
        var naechste = Kandidaten.Select((id, i) => (Id: id, Rang: i + 1)).FirstOrDefault(k => !gefragt.Contains(k.Id));
        if (naechste.Id == Guid.Empty)
        {
            Status = Vertretungsstatus.Eskaliert;
            return new Vertretungsschritt(Status);
        }

        var anfrage = new Vertretungsanfrage(Id, naechste.Id, naechste.Rang, jetzt, frist);
        _anfragen.Add(anfrage);
        Status = Vertretungsstatus.Angefragt;
        return new Vertretungsschritt(Status, anfrage);
    }

    private Vertretungsschritt Besetzen(Guid personId, DateTimeOffset jetzt)
    {
        Status = Vertretungsstatus.Besetzt;
        VertreterPersonId = personId;
        AbgeschlossenAm = jetzt;
        return new Vertretungsschritt(Status, Vertreter: personId);
    }
}

/// <summary>Anfrage an eine Stellvertretung mit Antwortfrist.</summary>
public sealed class Vertretungsanfrage
{
    private Vertretungsanfrage()
    {
    }

    internal Vertretungsanfrage(Guid fallId, Guid personId, int reihenfolge, DateTimeOffset gestelltAm, DateTimeOffset fristBis)
    {
        Id = Ids.Neu();
        VertretungsfallId = fallId;
        PersonId = personId;
        Reihenfolge = reihenfolge;
        GestelltAm = gestelltAm;
        FristBis = fristBis;
        Status = Anfragestatus.Offen;
    }

    public Guid Id { get; private set; }

    public Guid VertretungsfallId { get; private set; }

    public Guid PersonId { get; private set; }

    /// <summary>Position in der Rangfolge beim Start (1 = zuerst gefragt).</summary>
    public int Reihenfolge { get; private set; }

    public DateTimeOffset GestelltAm { get; private set; }

    public DateTimeOffset FristBis { get; private set; }

    public Anfragestatus Status { get; private set; }

    public DateTimeOffset? BeantwortetAm { get; private set; }

    internal void Beantworten(Anfragestatus status, DateTimeOffset jetzt)
    {
        if (Status != Anfragestatus.Offen)
        {
            throw new InvalidOperationException("Die Anfrage ist bereits beantwortet.");
        }

        Status = status;
        BeantwortetAm = jetzt;
    }
}

/// <summary>Eine Stellvertretung in der beim Start festgehaltenen Rangfolge eines Vertretungsfalls (Position 1 = zuerst gefragt).</summary>
public sealed class Vertretungskandidat
{
    private Vertretungskandidat()
    {
    }

    public Vertretungskandidat(Guid vertretungsfallId, int position, Guid personId)
    {
        Id = Ids.Neu();
        VertretungsfallId = vertretungsfallId;
        Position = position;
        PersonId = personId;
    }

    public Guid Id { get; private set; }

    public Guid VertretungsfallId { get; private set; }

    public int Position { get; private set; }

    public Guid PersonId { get; private set; }
}
