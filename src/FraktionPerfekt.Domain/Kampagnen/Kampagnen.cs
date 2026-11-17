using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Kampagnen;

public enum Plakatart
{
    Hohlkammer,
    Dreieck,
    Grossflaeche,
}

public enum Plakatstatus
{
    Geplant,
    Haengt,
    Beschaedigt,
    Abgehaengt,
}

/// <summary>
/// Plakatstandort einer Kampagne mit Koordinaten, Foto und Status. Am Handy erfasst (Standort per GPS, Foto mit der Kamera);
/// nach der Wahl entsteht daraus die Abhängliste.
/// </summary>
public sealed class Plakat
{
    private Plakat()
    {
        Standort = string.Empty;
    }

    public Plakat(Guid kampagneId, double breite, double laenge, string standort, Plakatart art, Plakatstatus status, Guid erfasstVon, DateTimeOffset jetzt, string? motiv = null)
    {
        if (breite is < -90 or > 90 || laenge is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(breite), "Ungültige Koordinaten.");
        }

        Id = Ids.Neu();
        KampagneId = kampagneId;
        Breite = breite;
        Laenge = laenge;
        Standort = string.IsNullOrWhiteSpace(standort) ? $"{breite:F5}, {laenge:F5}" : standort.Trim();
        Art = art;
        Motiv = string.IsNullOrWhiteSpace(motiv) ? null : motiv.Trim();
        Status = status;
        ErfasstVonPersonId = erfasstVon;
        ErfasstAm = jetzt;
        GeaendertAm = jetzt;
        GeaendertVonPersonId = erfasstVon;
    }

    public Guid Id { get; private set; }

    public Guid KampagneId { get; private set; }

    public double Breite { get; private set; }

    public double Laenge { get; private set; }

    public string Standort { get; private set; }

    public Plakatart Art { get; private set; }

    public string? Motiv { get; private set; }

    public Plakatstatus Status { get; private set; }

    public string? FotoSchluessel { get; private set; }

    public string? FotoTyp { get; private set; }

    public Guid ErfasstVonPersonId { get; private set; }

    public DateTimeOffset ErfasstAm { get; private set; }

    public Guid GeaendertVonPersonId { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public bool HaengtNoch => Status is Plakatstatus.Haengt or Plakatstatus.Beschaedigt;

    public void StatusSetzen(Plakatstatus status, Guid personId, DateTimeOffset jetzt)
        => (Status, GeaendertVonPersonId, GeaendertAm) = (status, personId, jetzt);

    public void FotoSetzen(string schluessel, string typ) => (FotoSchluessel, FotoTyp) = (schluessel, typ);

    public void FotoEntfernen() => (FotoSchluessel, FotoTyp) = (null, null);

    /// <summary>Entfernung in Metern (Haversine), für „nächste Plakate“ und die Abhängliste.</summary>
    public double EntfernungMeter(double breite, double laenge)
    {
        const double r = 6_371_000;
        var dB = (breite - Breite) * Math.PI / 180;
        var dL = (laenge - Laenge) * Math.PI / 180;
        var a = Math.Sin(dB / 2) * Math.Sin(dB / 2) + Math.Cos(Breite * Math.PI / 180) * Math.Cos(breite * Math.PI / 180) * Math.Sin(dL / 2) * Math.Sin(dL / 2);
        return 2 * r * Math.Asin(Math.Sqrt(a));
    }
}

public enum Gebietsart
{
    Plakatieren,
    Haustuer,
    Flyer,
}

public enum Gebietsstatus
{
    Offen,
    Zugeteilt,
    Erledigt,
}

/// <summary>Gebiet (Straßenzüge, Ortsteil) zum Plakatieren, für Haustürbesuche oder Flyer; wird Personen zugeteilt.</summary>
public sealed class Gebiet
{
    private Gebiet()
    {
        Name = string.Empty;
    }

    public Gebiet(Guid kampagneId, string name, Gebietsart art, string? beschreibung, double? breite, double? laenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        KampagneId = kampagneId;
        Name = name.Trim();
        Art = art;
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        (Breite, Laenge) = (breite, laenge);
        Status = Gebietsstatus.Offen;
    }

    public Guid Id { get; private set; }

    public Guid KampagneId { get; private set; }

    public string Name { get; private set; }

    public Gebietsart Art { get; private set; }

    public string? Beschreibung { get; private set; }

    public double? Breite { get; private set; }

    public double? Laenge { get; private set; }

    public Gebietsstatus Status { get; private set; }

    public Guid? ZugeteiltPersonId { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public string? Vermerk { get; private set; }

    public void Zuteilen(Guid? personId)
    {
        if (Status == Gebietsstatus.Erledigt)
        {
            throw new InvalidOperationException("Das Gebiet ist bereits erledigt.");
        }

        (ZugeteiltPersonId, Status) = (personId, personId is null ? Gebietsstatus.Offen : Gebietsstatus.Zugeteilt);
    }

    public void Erledigen(DateTimeOffset jetzt, string? vermerk)
    {
        if (ZugeteiltPersonId is null)
        {
            throw new InvalidOperationException("Erst zuteilen.");
        }

        (Status, ErledigtAm, Vermerk) = (Gebietsstatus.Erledigt, jetzt, string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim());
    }

    public void Wiedereroeffnen() => (Status, ErledigtAm) = (ZugeteiltPersonId is null ? Gebietsstatus.Offen : Gebietsstatus.Zugeteilt, null);
}

public enum Kampagnenstatus
{
    Planung,
    Aktiv,
    Abgeschlossen,
}

/// <summary>
/// Eine Kampagne (Wahlkampf, Haustüraktion, Themenkampagne …) mit Plakaten, Gebieten, Terminen und Tickets. Der Wahltag (optional)
/// bestimmt die Abhängliste der Plakate.
/// </summary>
public sealed class Kampagne
{
    private Kampagne()
    {
        Name = string.Empty;
    }

    public Kampagne(string name, string? beschreibung, DateOnly? beginn, DateOnly? ende, DateOnly? wahltag, Guid? angelegtVonPersonId, DateTimeOffset jetzt)
    {
        Id = Ids.Neu();
        Name = string.Empty;
        Aendern(name, beschreibung, beginn, ende, wahltag);
        Status = Kampagnenstatus.Planung;
        (AngelegtVonPersonId, AngelegtAm) = (angelegtVonPersonId, jetzt);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>Markdown.</summary>
    public string? Beschreibung { get; private set; }

    public DateOnly? Beginn { get; private set; }

    public DateOnly? Ende { get; private set; }

    /// <summary>Wahltag (nur bei Wahlkämpfen): danach Abhängliste und Aufgabe „Plakate abhängen“.</summary>
    public DateOnly? Wahltag { get; private set; }

    public Kampagnenstatus Status { get; private set; }

    public Guid? AngelegtVonPersonId { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    public void Aendern(string name, string? beschreibung, DateOnly? beginn, DateOnly? ende, DateOnly? wahltag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (beginn is { } b && ende is { } e && e < b)
        {
            throw new ArgumentException("Das Ende liegt vor dem Beginn.", nameof(ende));
        }

        Name = name.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        (Beginn, Ende, Wahltag) = (beginn, ende, wahltag);
    }

    public void StatusSetzen(Kampagnenstatus status) => Status = status;
}

/// <summary>ToDo-Vorlage (z. B. „Infostand“). Eine Kopiervorlage: Aus ihr erzeugte ToDos sind eigenständig und ändern sich nicht mit.</summary>
public sealed class TodoVorlage
{
    private TodoVorlage()
    {
        Name = string.Empty;
    }

    public TodoVorlage(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public void Umbenennen(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}

/// <summary>Ein Eintrag einer ToDo-Vorlage.</summary>
public sealed class TodoVorlagenPunkt
{
    private TodoVorlagenPunkt()
    {
        Text = string.Empty;
    }

    public TodoVorlagenPunkt(Guid vorlageId, int position, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        (VorlageId, Position, Text) = (vorlageId, position, text.Trim());
    }

    public Guid Id { get; private set; }

    public Guid VorlageId { get; private set; }

    public int Position { get; private set; }

    public string Text { get; private set; }
}

/// <summary>ToDo zu einem Termin (Pavillon holen, Flyer mitnehmen …), optional mit zuständiger Person; erledigt mit wer/wann.</summary>
public sealed class TerminTodo
{
    private TerminTodo()
    {
        Text = string.Empty;
    }

    public TerminTodo(Guid terminId, int position, string text, Guid? zustaendigPersonId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        (TerminId, Position, Text, ZustaendigPersonId) = (terminId, position, text.Trim(), zustaendigPersonId);
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public int Position { get; private set; }

    public string Text { get; private set; }

    public Guid? ZustaendigPersonId { get; private set; }

    public Guid? ErledigtVonPersonId { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public bool Erledigt => ErledigtAm is not null;

    public void Zuweisen(Guid? personId) => ZustaendigPersonId = personId;

    /// <summary>Abhaken bzw. wieder öffnen.</summary>
    public void Umschalten(Guid personId, DateTimeOffset jetzt)
        => (ErledigtVonPersonId, ErledigtAm) = Erledigt ? ((Guid?)null, (DateTimeOffset?)null) : (personId, jetzt);
}

public enum Ticketstatus
{
    Neu,
    InArbeit,
    Wartet,
    Erledigt,
}

public enum Ticketprioritaet
{
    Niedrig,
    Normal,
    Hoch,
    Dringend,
}

/// <summary>
/// Ticket einer Kampagne (Aufgabe, Problem, Schadensmeldung): Status, Priorität, zuständige Person, Fälligkeit und optional ein Bezug
/// auf ein Gebiet, ein Plakat oder einen Termin derselben Kampagne. Nummer fortlaufend je Kampagne.
/// </summary>
public sealed class Ticket
{
    private Ticket()
    {
        Titel = string.Empty;
    }

    public Ticket(Guid kampagneId, int nummer, string titel, string? beschreibung, Ticketprioritaet prioritaet, Guid erstelltVonPersonId, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        (KampagneId, Nummer, Titel, Prioritaet, ErstelltVonPersonId, ErstelltAm, GeaendertAm) = (kampagneId, nummer, titel.Trim(), prioritaet, erstelltVonPersonId, jetzt, jetzt);
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        Status = Ticketstatus.Neu;
    }

    public Guid Id { get; private set; }

    public Guid KampagneId { get; private set; }

    public int Nummer { get; private set; }

    public string Titel { get; private set; }

    /// <summary>Markdown (Bilder per Strg+V).</summary>
    public string? Beschreibung { get; private set; }

    public Ticketstatus Status { get; private set; }

    public Ticketprioritaet Prioritaet { get; private set; }

    public Guid? ZustaendigPersonId { get; private set; }

    public DateOnly? Faellig { get; private set; }

    public Guid? GebietId { get; private set; }

    public Guid? PlakatId { get; private set; }

    public Guid? TerminId { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public bool Offen => Status != Ticketstatus.Erledigt;

    public void Bearbeiten(string titel, string? beschreibung, Ticketprioritaet prioritaet, DateOnly? faellig, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        (Titel, Prioritaet, Faellig, GeaendertAm) = (titel.Trim(), prioritaet, faellig, jetzt);
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
    }

    public void Beziehen(Guid? gebietId, Guid? plakatId, Guid? terminId) => (GebietId, PlakatId, TerminId) = (gebietId, plakatId, terminId);

    public void StatusSetzen(Ticketstatus status, DateTimeOffset jetzt) => (Status, GeaendertAm) = (status, jetzt);

    public void Zuweisen(Guid? personId, DateTimeOffset jetzt) => (ZustaendigPersonId, GeaendertAm) = (personId, jetzt);
}

/// <summary>Kommentar zu einem Ticket.</summary>
public sealed class TicketKommentar
{
    private TicketKommentar()
    {
        Text = string.Empty;
    }

    public TicketKommentar(Guid ticketId, Guid personId, string text, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        (TicketId, PersonId, Text, Am) = (ticketId, personId, text.Trim(), am);
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid PersonId { get; private set; }

    /// <summary>Markdown.</summary>
    public string Text { get; private set; }

    public DateTimeOffset Am { get; private set; }
}
