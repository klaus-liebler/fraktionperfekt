using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Sitzungen;

public enum Sitzungsart
{
    /// <summary>Eigene Fraktionssitzung mit Einladung, TOP-Anmeldung und Protokoll.</summary>
    Fraktionssitzung,

    /// <summary>Eigenes Protokoll einer Rats- oder Ausschusssitzung (TOPs aus dem RIS).</summary>
    Gremienprotokoll,

    Arbeitskreis,
}

public enum Sitzungsstatus
{
    Vorbereitung,
    Eingeladen,
    Laufend,

    /// <summary>Sitzung beendet, Protokoll als Entwurf.</summary>
    Entwurf,

    /// <summary>Protokoll durch die Sitzungsleitung freigegeben und versandt.</summary>
    Freigegeben,
}

public static class SitzungsstatusExtensions
{
    public static string Anzeigename(this Sitzungsstatus s) => s switch
    {
        Sitzungsstatus.Vorbereitung => "in Vorbereitung",
        Sitzungsstatus.Eingeladen => "eingeladen",
        Sitzungsstatus.Laufend => "läuft",
        Sitzungsstatus.Entwurf => "Protokoll-Entwurf",
        Sitzungsstatus.Freigegeben => "Protokoll freigegeben",
        _ => s.ToString(),
    };
}

/// <summary>
/// Sitzung mit Tagesordnung und Protokoll. Eine Protokoll-Engine für Fraktionssitzungen und eigene Ausschussprotokolle,
/// nur die Vorlage unterscheidet sich. Ablauf: Vorbereitung → Einladung → Sitzungsmodus → Entwurf → Freigabe durch die Leitung.
/// </summary>
public sealed class Sitzung
{
    private Sitzung()
    {
        Vorlage = string.Empty;
    }

    /// <summary>Titel und Gremium stehen am Termin (keine Kopie in der Sitzung, Datenbank-Regel 1).</summary>
    public Sitzung(Guid terminId, Sitzungsart art, string vorlage, DateTimeOffset? topAnmeldungBis, Guid? leitungPersonId, DateTimeOffset angelegtAm)
    {
        Id = Ids.Neu();
        TerminId = terminId;
        Art = art;
        Vorlage = vorlage;
        TopAnmeldungBis = topAnmeldungBis;
        LeitungPersonId = leitungPersonId;
        AngelegtAm = angelegtAm;
        Status = Sitzungsstatus.Vorbereitung;
    }

    public Guid Id { get; private set; }

    public Guid TerminId { get; private set; }

    public Sitzungsart Art { get; private set; }

    /// <summary>Name der Vorlage, aus der die festen TOPs stammen.</summary>
    public string Vorlage { get; private set; }

    public Sitzungsstatus Status { get; private set; }

    /// <summary>Bis wann Mitglieder TOPs anmelden können.</summary>
    public DateTimeOffset? TopAnmeldungBis { get; private set; }

    /// <summary>Sitzungsleitung: gibt das Protokoll frei.</summary>
    public Guid? LeitungPersonId { get; private set; }

    public Guid? ProtokollPersonId { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    public DateTimeOffset? EingeladenAm { get; private set; }

    public DateTimeOffset? BegonnenAm { get; private set; }

    public DateTimeOffset? BeendetAm { get; private set; }

    public DateTimeOffset? FreigegebenAm { get; private set; }

    public Guid? FreigegebenVonPersonId { get; private set; }

    public bool TagesordnungAenderbar => Status is Sitzungsstatus.Vorbereitung or Sitzungsstatus.Eingeladen or Sitzungsstatus.Laufend;

    public bool ProtokollAenderbar => Status is Sitzungsstatus.Laufend or Sitzungsstatus.Entwurf;

    public void LeitungFestlegen(Guid? leitung, Guid? protokoll)
    {
        LeitungPersonId = leitung;
        ProtokollPersonId = protokoll;
    }

    public void Einladen(DateTimeOffset jetzt)
    {
        if (Status is not (Sitzungsstatus.Vorbereitung or Sitzungsstatus.Eingeladen))
        {
            throw new InvalidOperationException("Die Sitzung hat bereits begonnen.");
        }

        Status = Sitzungsstatus.Eingeladen;
        EingeladenAm = jetzt;
    }

    public void Beginnen(DateTimeOffset jetzt, Guid? protokollPersonId)
    {
        if (Status is not (Sitzungsstatus.Vorbereitung or Sitzungsstatus.Eingeladen))
        {
            throw new InvalidOperationException("Die Sitzung läuft bereits oder ist beendet.");
        }

        Status = Sitzungsstatus.Laufend;
        BegonnenAm = jetzt;
        ProtokollPersonId ??= protokollPersonId;
    }

    public void Beenden(DateTimeOffset jetzt)
    {
        if (Status != Sitzungsstatus.Laufend)
        {
            throw new InvalidOperationException("Nur eine laufende Sitzung kann beendet werden.");
        }

        Status = Sitzungsstatus.Entwurf;
        BeendetAm = jetzt;
    }

    public void Freigeben(Guid personId, DateTimeOffset jetzt)
    {
        if (Status != Sitzungsstatus.Entwurf)
        {
            throw new InvalidOperationException("Freigeben lässt sich nur ein Protokoll-Entwurf.");
        }

        Status = Sitzungsstatus.Freigegeben;
        FreigegebenAm = jetzt;
        FreigegebenVonPersonId = personId;
    }
}

public enum Topstatus
{
    Geplant,
    Behandelt,

    /// <summary>Vertagt: wandert in die Einladung der nächsten Sitzung.</summary>
    Vertagt,

    /// <summary>Mit einem anderen TOP zusammengelegt.</summary>
    Zusammengelegt,
}

/// <summary>Tagesordnungspunkt mit Notizen des Protokolls.</summary>
public sealed class Top
{
    private Top()
    {
        Titel = string.Empty;
    }

    public Top(Guid sitzungId, int position, string titel, string? beschreibung, bool fest, Guid? angemeldetVonPersonId, DateTimeOffset angelegtAm,
        Guid? antragId = null, Guid? vertagtAusTopId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        SitzungId = sitzungId;
        Position = position;
        Titel = titel.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        Fest = fest;
        AngemeldetVonPersonId = angemeldetVonPersonId;
        AngelegtAm = angelegtAm;
        AntragId = antragId;
        VertagtAusTopId = vertagtAusTopId;
        Status = Topstatus.Geplant;
    }

    public Guid Id { get; private set; }

    public Guid SitzungId { get; private set; }

    /// <summary>Reihenfolge (1, 2, 3 …); die Nummer im Protokoll ergibt sich daraus.</summary>
    public int Position { get; private set; }

    public string Titel { get; private set; }

    public string? Beschreibung { get; private set; }

    /// <summary>Fester TOP aus der Vorlage (z. B. „Bericht des Vorstands“).</summary>
    public bool Fest { get; private set; }

    public Guid? AngemeldetVonPersonId { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    public Guid? AntragId { get; private set; }

    public Guid? VertagtAusTopId { get; private set; }

    /// <summary>Bei vertagten TOPs: in welche Sitzung übernommen.</summary>
    public Guid? UebernommenInSitzungId { get; private set; }

    public Guid? ZusammengelegtMitTopId { get; private set; }

    public Topstatus Status { get; private set; }

    public string? Notiz { get; private set; }

    public void PositionSetzen(int position) => Position = position;

    public void Aendern(string titel, string? beschreibung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Titel = titel.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
    }

    public void NotizSetzen(string? notiz) => Notiz = string.IsNullOrWhiteSpace(notiz) ? null : notiz.Trim();

    public void AlsBehandeltMarkieren() => Status = Topstatus.Behandelt;

    public void Vertagen()
    {
        if (Status == Topstatus.Zusammengelegt)
        {
            throw new InvalidOperationException("Ein zusammengelegter TOP kann nicht vertagt werden.");
        }

        Status = Topstatus.Vertagt;
    }

    public void InSitzungUebernommen(Guid sitzungId) => UebernommenInSitzungId = sitzungId;

    /// <summary>Legt diesen TOP mit <paramref name="ziel"/> zusammen: Notizen und Beschreibung wandern mit.</summary>
    public void ZusammenlegenMit(Top ziel)
    {
        ArgumentNullException.ThrowIfNull(ziel);
        if (ziel.Id == Id || ziel.SitzungId != SitzungId)
        {
            throw new InvalidOperationException("Zusammenlegen geht nur mit einem anderen TOP derselben Sitzung.");
        }

        ziel.Beschreibung = string.Join("\n", new[] { ziel.Beschreibung, $"(zusammengelegt mit: {Titel})", Beschreibung }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (Notiz is not null)
        {
            ziel.Notiz = string.Join("\n", new[] { ziel.Notiz, Notiz }.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        Status = Topstatus.Zusammengelegt;
        ZusammengelegtMitTopId = ziel.Id;
    }
}

public enum Anwesenheitsstatus
{
    Anwesend,
    Entschuldigt,
    Fehlt,
}

public sealed class Sitzungsanwesenheit
{
    private Sitzungsanwesenheit()
    {
    }

    public Sitzungsanwesenheit(Guid sitzungId, Guid personId, Anwesenheitsstatus status)
    {
        Id = Ids.Neu();
        SitzungId = sitzungId;
        PersonId = personId;
        Status = status;
    }

    public Guid Id { get; private set; }

    public Guid SitzungId { get; private set; }

    public Guid PersonId { get; private set; }

    public Anwesenheitsstatus Status { get; private set; }

    public void Setzen(Anwesenheitsstatus status) => Status = status;
}

public enum Beschlussquelle
{
    Fraktion,
    Rat,
    Ausschuss,
}

public enum Beschlussergebnis
{
    Angenommen,
    Abgelehnt,
    Kenntnisnahme,
    Vertagt,
}

/// <summary>Beschluss mit Abstimmungsergebnis; aus eigenen Protokollen oder dem RIS. Grundlage des Beschlusslage-Archivs.</summary>
public sealed class Beschluss
{
    private Beschluss()
    {
        Titel = string.Empty;
        Text = string.Empty;
    }

    public Beschluss(Beschlussquelle quelle, DateOnly datum, string titel, string text, Beschlussergebnis ergebnis, int ja, int nein, int enthaltung,
        Guid? sitzungId = null, Guid? topId = null, Guid? gremiumId = null, Guid? antragId = null, string? risId = null, Guid? umfrageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        if (ja < 0 || nein < 0 || enthaltung < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ja), "Stimmenzahlen dürfen nicht negativ sein.");
        }

        Id = Ids.Neu();
        Quelle = quelle;
        Datum = datum;
        Titel = titel.Trim();
        Text = text?.Trim() ?? string.Empty;
        Ergebnis = ergebnis;
        Ja = ja;
        Nein = nein;
        Enthaltung = enthaltung;
        SitzungId = sitzungId;
        TopId = topId;
        GremiumId = gremiumId;
        AntragId = antragId;
        RisId = risId;
        UmfrageId = umfrageId;
    }

    public Guid Id { get; private set; }

    public Beschlussquelle Quelle { get; private set; }

    public DateOnly Datum { get; private set; }

    public string Titel { get; private set; }

    public string Text { get; private set; }

    public Beschlussergebnis Ergebnis { get; private set; }

    public int Ja { get; private set; }

    public int Nein { get; private set; }

    public int Enthaltung { get; private set; }

    public Guid? SitzungId { get; private set; }

    public Guid? TopId { get; private set; }

    public Guid? GremiumId { get; private set; }

    public Guid? AntragId { get; private set; }

    public string? RisId { get; private set; }

    /// <summary>Beschluss aus einer (geheimen) Abstimmung im Sitzungsmodus.</summary>
    public Guid? UmfrageId { get; private set; }

    public string Stimmen => Ja + Nein + Enthaltung == 0 ? "ohne Zählung" : $"{Ja} ja, {Nein} nein, {Enthaltung} Enthaltung";

    /// <summary>Ergebnis aus den Stimmen: Mehrheit der abgegebenen Ja- gegenüber Nein-Stimmen.</summary>
    public static Beschlussergebnis Auswerten(int ja, int nein) => ja > nein ? Beschlussergebnis.Angenommen : Beschlussergebnis.Abgelehnt;
}

/// <summary>Vorlagen für die Tagesordnung je Sitzungsart.</summary>
public static class Sitzungsvorlagen
{
    public static readonly string[] Fraktionssitzung =
    [
        "Begrüßung und Feststellung der Beschlussfähigkeit",
        "Genehmigung des Protokolls der letzten Sitzung",
        "Bericht des Vorstands",
        "Vorbereitung der Ausschuss- und Ratssitzungen",
        "Anträge",
        "Verschiedenes",
    ];

    public static readonly string[] Arbeitskreis = ["Bericht und Rückblick", "Themen", "Verschiedenes"];

    /// <summary>TOPs aus einer RIS-Tagesordnung (Zeilen „TOP 1 …“) für das eigene Ausschussprotokoll.</summary>
    public static IReadOnlyList<string> AusTagesordnung(string? beschreibung)
    {
        var tops = (beschreibung ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(z => z.StartsWith("TOP ", StringComparison.OrdinalIgnoreCase))
            .Select(z => z[4..].TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' ', ':'))
            .Where(z => z.Length > 0)
            .ToList();
        return tops.Count > 0 ? tops : ["Tagesordnung laut RIS", "Verschiedenes"];
    }
}
