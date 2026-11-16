using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Termine;

public enum Terminart
{
    Fraktionssitzung,
    Ratssitzung,
    Ausschusssitzung,
    Arbeitskreis,

    /// <summary>Öffentlicher Termin (Vereinsjubiläum, Bürgerversammlung …).</summary>
    Oeffentlich,
    Wahlkampf,
    Sonstiges,
}

public static class TerminartExtensions
{
    public static string Anzeigename(this Terminart art) => art switch
    {
        Terminart.Fraktionssitzung => "Fraktionssitzung",
        Terminart.Ratssitzung => "Ratssitzung",
        Terminart.Ausschusssitzung => "Ausschusssitzung",
        Terminart.Arbeitskreis => "Arbeitskreis",
        Terminart.Oeffentlich => "Öffentlicher Termin",
        Terminart.Wahlkampf => "Wahlkampf",
        Terminart.Sonstiges => "Sonstiges",
        _ => art.ToString(),
    };

    /// <summary>Gremiensitzungen haben eine feste Besetzung (ordentliche Mitglieder) und brauchen bei Abwesenheit Vertretung.</summary>
    public static bool IstGremiensitzung(this Terminart art) => art is Terminart.Ratssitzung or Terminart.Ausschusssitzung;

    /// <summary>Für Helfer:innen sichtbar.</summary>
    public static bool IstOeffentlich(this Terminart art) => art is Terminart.Oeffentlich or Terminart.Wahlkampf;
}

/// <summary>
/// Ein Termin im gemeinsamen Kalender. Beginn und Ende sind Wanduhrzeit in Greven (Europe/Berlin).
/// Sitzungen aus dem RIS tragen dessen Kennung und werden beim Abgleich aktualisiert.
/// </summary>
public sealed class Termin
{
    private Termin()
    {
        Titel = string.Empty;
    }

    public Termin(string titel, Terminart art, DateTime beginn, DateTime ende, string? ort, Guid? gremiumId, Guid? angelegtVonPersonId,
        DateTimeOffset angelegtAm, string? beschreibung = null, string? risId = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        if (ende <= beginn)
        {
            throw new ArgumentException("Das Ende muss nach dem Beginn liegen.", nameof(ende));
        }

        Id = id ?? Ids.Neu();
        Titel = titel.Trim();
        Art = art;
        Beginn = beginn;
        Ende = ende;
        Ort = string.IsNullOrWhiteSpace(ort) ? null : ort.Trim();
        GremiumId = gremiumId;
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        RisId = risId;
        AngelegtVonPersonId = angelegtVonPersonId;
        AngelegtAm = angelegtAm;
    }

    public Guid Id { get; private set; }

    public string Titel { get; private set; }

    public Terminart Art { get; private set; }

    public DateTime Beginn { get; private set; }

    public DateTime Ende { get; private set; }

    public string? Ort { get; private set; }

    public string? Beschreibung { get; private set; }

    public Guid? GremiumId { get; private set; }

    public string? RisId { get; private set; }

    public bool Abgesagt { get; private set; }

    /// <summary>Wer den Termin angelegt hat (Personenverweis; leer = RIS-Abgleich bzw. Automatik).</summary>
    public Guid? AngelegtVonPersonId { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    public DateOnly Tag => DateOnly.FromDateTime(Beginn);

    /// <summary>Abgleich mit dem RIS: Zeit, Ort und Titel folgen der Quelle. Liefert, ob sich etwas geändert hat.</summary>
    public bool AusRisUebernehmen(string titel, DateTime beginn, DateTime ende, string? ort, bool abgesagt)
    {
        var geaendert = Titel != titel || Beginn != beginn || Ende != ende || Ort != ort || Abgesagt != abgesagt;
        Titel = titel;
        Beginn = beginn;
        Ende = ende;
        Ort = ort;
        Abgesagt = abgesagt;
        return geaendert;
    }

    public void Absagen() => Abgesagt = true;
}

public enum Teilnahmestatus
{
    Zugesagt,
    Abgesagt,

    /// <summary>Nimmt als Vertretung eines ordentlichen Mitglieds teil.</summary>
    Vertritt,
}

/// <summary>Antwort einer Person auf einen Termin. Ohne Eintrag gilt eine Person als eingeladen (noch keine Antwort).</summary>
public sealed class Teilnahme
{
    private Teilnahme()
    {
    }

    public Teilnahme(Guid terminId, Guid personId, Teilnahmestatus status, DateTimeOffset am, Guid? vertrittPersonId = null)
    {
        Id = Ids.Neu();
        TerminId = terminId;
        PersonId = personId;
        Setzen(status, am, vertrittPersonId);
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public Guid PersonId { get; private set; }

    public Teilnahmestatus Status { get; private set; }

    /// <summary>Bei <see cref="Teilnahmestatus.Vertritt"/>: wen die Person vertritt.</summary>
    public Guid? VertrittPersonId { get; private set; }

    public DateTimeOffset GeantwortetAm { get; private set; }

    public void Setzen(Teilnahmestatus status, DateTimeOffset am, Guid? vertrittPersonId = null)
    {
        if (status == Teilnahmestatus.Vertritt && vertrittPersonId is null)
        {
            throw new ArgumentException("Bei einer Vertretung muss feststehen, wer vertreten wird.", nameof(vertrittPersonId));
        }

        Status = status;
        VertrittPersonId = status == Teilnahmestatus.Vertritt ? vertrittPersonId : null;
        GeantwortetAm = am;
    }
}

/// <summary>Vermerk, dass eine Person an einen Termin erinnert wurde (genau einmal).</summary>
public sealed class TerminErinnerung
{
    private TerminErinnerung()
    {
    }

    public TerminErinnerung(Guid terminId, Guid personId, DateTimeOffset am)
    {
        Id = Gemeinsam.Ids.Neu();
        (TerminId, PersonId, Am) = (terminId, personId, am);
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }
}
