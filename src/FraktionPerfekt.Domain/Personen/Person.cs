using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Eine Person der Fraktion (Ratsmitglied, sachkundige:r Bürger:in, Helfer:in). Die Rollen sind bitemporal versioniert:
/// Wer wann Vorstand war, bleibt nachvollziehbar.
/// </summary>
public sealed class Person
{
    private readonly List<PersonRollen> _rollen = [];

    private Person()
    {
        Vorname = string.Empty;
        Nachname = string.Empty;
    }

    public Person(string vorname, string nachname, string? eMail, string? benutzerkennung, DateTimeOffset angelegtAm, string? risId = null,
        Guid? id = null, string? titel = null, string? anrede = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vorname);
        ArgumentException.ThrowIfNullOrWhiteSpace(nachname);
        Id = id ?? Ids.Neu();
        Vorname = vorname.Trim();
        Nachname = nachname.Trim();
        EMail = string.IsNullOrWhiteSpace(eMail) ? null : eMail.Trim();
        Benutzerkennung = string.IsNullOrWhiteSpace(benutzerkennung) ? null : benutzerkennung.Trim().ToLowerInvariant();
        RisId = risId;
        Titel = string.IsNullOrWhiteSpace(titel) ? null : titel.Trim();
        Anrede = AnredePruefen(anrede);
        AngelegtAm = angelegtAm;
        Aktiv = true;
    }

    public Guid Id { get; private set; }

    public string Vorname { get; private set; }

    /// <summary>Nachname, auch mehrteilig („Große Maestrup“, „Schulte Jochmaring“); danach wird sortiert.</summary>
    public string Nachname { get; private set; }

    /// <summary>Akademischer Grad („Dr.“) für förmliche Darstellungen (Listen, PDF, „Herr Dr. Schubert“); nicht sortiert und nie in der
    /// Du-Ansprache. Amtsbezeichnungen wie „Prof.“ gehören nicht hierher.</summary>
    public string? Titel { get; private set; }

    public string? EMail { get; private set; }

    /// <summary>Mobilnummer für spätere SMS/Push-Zuordnung; optional.</summary>
    public string? Mobil { get; private set; }

    /// <summary>Anmeldename; ohne Kennung kann sich die Person nicht anmelden.</summary>
    public string? Benutzerkennung { get; private set; }

    /// <summary>Kennung der Person im Ratsinformationssystem (OParl), falls bekannt.</summary>
    public string? RisId { get; private set; }

    public bool Aktiv { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    /// <summary>„Herr“, „Frau“ oder <c>null</c> (keine Angabe/divers).</summary>
    public string? Anrede { get; private set; }

    /// <summary>Persönliche Einstellung: Du oder Sie (Standard: Du).</summary>
    public Anredeform Anredeform { get; private set; }

    /// <summary>Persönliche Einstellung: Stil der Begrüßung (Standard: locker).</summary>
    public Begruessungsstil Begruessungsstil { get; private set; }

    /// <summary>Persönliche Einstellung: akademischen Grad in der Sie-Form nennen (Standard: ja).</summary>
    public bool TitelInAnsprache { get; private set; } = true;

    /// <summary>Wie die Person angesprochen werden möchte; jede Person stellt das für sich ein.</summary>
    public void AnspracheEinstellen(string? anrede, Anredeform form, Begruessungsstil stil, bool titelInAnsprache)
    {
        Anrede = AnredePruefen(anrede);
        (Anredeform, Begruessungsstil, TitelInAnsprache) = (form, stil, titelInAnsprache);
    }

    private static string? AnredePruefen(string? anrede) => anrede?.Trim() switch
    {
        "Herr" => "Herr",
        "Frau" => "Frau",
        null or "" => null,
        var a => throw new ArgumentException($"Unbekannte Anrede „{a}“ (Herr, Frau oder keine Angabe).", nameof(anrede)),
    };

    public string Anzeigename => Titel is null ? $"{Vorname} {Nachname}" : $"{Titel} {Vorname} {Nachname}";

    public string Sortiername => $"{Nachname}, {Vorname}";

    /// <summary>Initialen für Avatare: erster Buchstabe des Vornamens und des Nachnamens („Große Maestrup“ → „JG“).</summary>
    public string Initialen => $"{char.ToUpperInvariant(Vorname[0])}{char.ToUpperInvariant(Nachname[0])}";

    public void TitelSetzen(string? titel) => Titel = string.IsNullOrWhiteSpace(titel) ? null : titel.Trim();

    public IReadOnlyCollection<PersonRollen> RollenVersionen => _rollen;

    /// <summary>Rollen am Stichtag nach dem Wissensstand <paramref name="bekanntAm"/>.</summary>
    public IReadOnlyList<Rolle> RollenAm(DateOnly tag, DateTimeOffset bekanntAm)
        => _rollen.AlsStichtag(tag, bekanntAm)?.Rollen ?? [];

    /// <summary>Rollen ab einem Datum festlegen (Ersterfassung oder Änderung ab Datum).</summary>
    public void RollenFestlegenAb(DateOnly ab, IEnumerable<Rolle> rollen, Erfassung erfassung)
    {
        ArgumentNullException.ThrowIfNull(erfassung);
        var liste = rollen.Distinct().Order().ToList();
        var art = _rollen.Count == 0 ? Erfassungsart.Ersterfassung : Erfassungsart.AenderungAbDatum;
        var neu = new PersonRollen(liste, ab, null, erfassung with { Art = art });
        if (_rollen.Count == 0)
        {
            _rollen.Add(neu);
        }
        else
        {
            ZeitlicheHistorie.AendernAb(_rollen, neu);
        }
    }

    public void KontaktAendern(string? eMail, string? mobil)
    {
        EMail = string.IsNullOrWhiteSpace(eMail) ? null : eMail.Trim();
        Mobil = string.IsNullOrWhiteSpace(mobil) ? null : mobil.Trim();
    }

    public void RisZuordnen(string? risId) => RisId = string.IsNullOrWhiteSpace(risId) ? null : risId.Trim();

    public void Deaktivieren() => Aktiv = false;

    public void Aktivieren() => Aktiv = true;

    public override string ToString() => Anzeigename;
}

/// <summary>Eine Version der Rollen einer Person (bitemporal).</summary>
public sealed class PersonRollen : BitemporaleVersion<PersonRollen>
{
    private PersonRollen()
    {
        RollenText = string.Empty;
    }

    public PersonRollen(IReadOnlyList<Rolle> rollen, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentNullException.ThrowIfNull(rollen);
        RollenText = string.Join(',', rollen.Distinct().Order());
    }

    /// <summary>Gespeichert als kommagetrennte technische Namen, z. B. "Vorstand,Mitglied".</summary>
    public string RollenText { get; private set; }

    public IReadOnlyList<Rolle> Rollen => RollenText.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(r => Enum.TryParse<Rolle>(r, out var x) ? x : (Rolle?)null).OfType<Rolle>().ToList();

    public override string Datenart => "Rollen";

    public override string Feld => "Rollen in der Fraktion";

    public override string Gegenstand => "rollen";

    public override string Wert => Rollen.Count == 0 ? "keine" : string.Join(", ", Rollen.Select(r => r.Anzeigename()));

    protected internal override PersonRollen KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Rollen, gueltigVon, gueltigBis, erfassung);
}
