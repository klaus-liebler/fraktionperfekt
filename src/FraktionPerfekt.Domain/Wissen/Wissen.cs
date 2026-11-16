using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Wissen;

/// <summary>
/// Datei in der Ablage (Modul 8): Ordner je Gremium, Arbeitskreis und Projekt; Anhang an Antrag, Protokoll oder Termin.
/// Der Inhalt liegt im Dateispeicher (lokal oder Nextcloud/WebDAV), hier nur Metadaten und Prüfsumme.
/// </summary>
public sealed class Datei
{
    private Datei()
    {
        Ordner = string.Empty;
        Name = string.Empty;
        Typ = string.Empty;
        Schluessel = string.Empty;
        Sha256 = string.Empty;
    }

    public Datei(string ordner, string name, string typ, long groesse, string schluessel, string sha256, Guid hochgeladenVon, DateTimeOffset am, Guid? gremiumId,
        bool vertraulich, string? bezugArt = null, Guid? bezugId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ordner);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        Ordner = ordner.Trim('/');
        Name = name;
        Typ = typ;
        Groesse = groesse;
        Schluessel = schluessel;
        Sha256 = sha256;
        HochgeladenVonPersonId = hochgeladenVon;
        Am = am;
        GremiumId = gremiumId;
        Vertraulich = vertraulich;
        BezugArt = bezugArt;
        BezugId = bezugId;
    }

    public Guid Id { get; private set; }

    /// <summary>Ordnerpfad, z. B. „Gremien/HFA“ oder „Anträge“.</summary>
    public string Ordner { get; private set; }

    public string Name { get; private set; }

    public string Typ { get; private set; }

    public long Groesse { get; private set; }

    /// <summary>Schlüssel im Dateispeicher (Pfad), nie aus dem Dateinamen allein gebildet.</summary>
    public string Schluessel { get; private set; }

    public string Sha256 { get; private set; }

    public Guid HochgeladenVonPersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    /// <summary>Rechte nach Gremium: nur dessen Mitglieder (und Vorstand/Administration); <c>null</c> = ganze Fraktion.</summary>
    public Guid? GremiumId { get; private set; }

    /// <summary>Nichtöffentliche Unterlagen: auch innerhalb der Fraktion nur Gremienmitglieder, Vorstand, Administration.</summary>
    public bool Vertraulich { get; private set; }

    /// <summary>„Antrag“, „Sitzung“, „Termin“ …</summary>
    public string? BezugArt { get; private set; }

    public Guid? BezugId { get; private set; }

    public DateTimeOffset? GeloeschtAm { get; private set; }

    public void Loeschen(DateTimeOffset jetzt) => GeloeschtAm ??= jetzt;

    /// <summary>Sicherer Dateiname: keine Pfadanteile, keine Steuerzeichen.</summary>
    public static string Bereinigen(string name)
    {
        var n = Path.GetFileName((name ?? string.Empty).Replace('\\', '/'));
        n = new string(n.Where(c => !char.IsControl(c) && c is not ('/' or ':' or '*' or '?' or '"' or '<' or '>' or '|')).ToArray()).Trim();
        return n.Length == 0 ? "datei" : n.Length > 150 ? n[^150..] : n;
    }
}

/// <summary>Wiki-Seite (Modul 20) mit Versionen; Bereiche für Abläufe, Themen, Gremien, Positionen.</summary>
public sealed class WikiSeite
{
    private WikiSeite()
    {
        Bereich = string.Empty;
        Titel = string.Empty;
    }

    public WikiSeite(string bereich, string titel, Guid? gremiumId, bool vertraulich, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bereich);
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        Bereich = bereich.Trim();
        Titel = titel.Trim();
        GremiumId = gremiumId;
        Vertraulich = vertraulich;
        ErstelltAm = jetzt;
    }

    public Guid Id { get; private set; }

    public string Bereich { get; private set; }

    public string Titel { get; private set; }

    public Guid? GremiumId { get; private set; }

    /// <summary>Vertraulich: nur Vorstand, Administration und Gremienmitglieder; für die KI unsichtbar.</summary>
    public bool Vertraulich { get; private set; }

    /// <summary>Nummer der neuesten Version – nicht gespeichert, aus den <see cref="WikiVersion"/>-Zeilen gesetzt (<see cref="StandAusVersion"/>).</summary>
    public int Version { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    private DateTimeOffset? _geaendertAm;

    /// <summary>Zeitpunkt der neuesten Version (nicht gespeichert); ohne Version der Zeitpunkt der Anlage.</summary>
    public DateTimeOffset GeaendertAm => _geaendertAm ?? ErstelltAm;

    /// <summary>Stand aus der neuesten Version übernehmen (Datenbank-Regel 1: keine Kopie in der Seite).</summary>
    public void StandAusVersion(int nummer, DateTimeOffset am) => (Version, _geaendertAm) = (nummer, am);

    public bool Archiviert { get; private set; }

    public void NeueVersion(DateTimeOffset jetzt)
    {
        Version++;
        _geaendertAm = jetzt;
    }

    public void Verschieben(string bereich, string titel, bool vertraulich, Guid? gremiumId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bereich);
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Bereich = bereich.Trim();
        Titel = titel.Trim();
        Vertraulich = vertraulich;
        GremiumId = gremiumId;
    }

    public void Archivieren() => Archiviert = true;

    /// <summary>Verweise [[Titel]] im Text.</summary>
    public static IReadOnlyList<string> Verweise(string text)
        => Regex.Matches(text ?? string.Empty, @"\[\[(.+?)\]\]").Select(m => m.Groups[1].Value.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

    /// <summary>Vergleichsform eines Titels (Groß/klein, Umlaute, Satzzeichen) zum Finden von Dubletten.</summary>
    public static string Normalisiert(string titel)
    {
        var sb = new StringBuilder();
        foreach (var c in (titel ?? string.Empty).ToLower(CultureInfo.GetCultureInfo("de-DE")).Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Replace("ss", "s", StringComparison.Ordinal);
    }
}

public sealed class WikiVersion
{
    private WikiVersion()
    {
        Text = string.Empty;
    }

    public WikiVersion(Guid seiteId, int nummer, string text, Guid? personId, string? kommentar, DateTimeOffset am, Guid? vorschlagId = null)
    {
        Id = Ids.Neu();
        SeiteId = seiteId;
        Nummer = nummer;
        Text = text ?? string.Empty;
        PersonId = personId;
        Kommentar = string.IsNullOrWhiteSpace(kommentar) ? null : kommentar.Trim();
        Am = am;
        VorschlagId = vorschlagId;
    }

    public Guid Id { get; private set; }

    public Guid SeiteId { get; private set; }

    public int Nummer { get; private set; }

    public string Text { get; private set; }

    /// <summary><c>null</c> = übernommener KI-Vorschlag (siehe <see cref="VorschlagId"/>).</summary>
    public Guid? PersonId { get; private set; }

    public string? Kommentar { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public Guid? VorschlagId { get; private set; }
}

public enum Vorschlagsart
{
    Veraltet,
    Widerspruch,
    Dublette,
    Verwaist,
    Struktur,
    Luecke,
    Sonstiges,
}

public enum Wikivorschlagsstatus
{
    Offen,
    Angenommen,
    Abgelehnt,
}

/// <summary>
/// Änderungsvorschlag der KI (oder einer Person) für eine Wiki-Seite. Die KI ändert nie direkt: Eine verantwortliche Person
/// sieht Vorher und Nachher und nimmt an oder lehnt ab.
/// </summary>
public sealed class WikiVorschlag
{
    private WikiVorschlag()
    {
        Begruendung = string.Empty;
        Quelle = string.Empty;
        Vorher = string.Empty;
    }

    public WikiVorschlag(Guid seiteId, int basisVersion, Vorschlagsart art, string begruendung, string vorher, string? nachher, string quelle, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(begruendung);
        Id = Ids.Neu();
        SeiteId = seiteId;
        BasisVersion = basisVersion;
        Art = art;
        Begruendung = begruendung.Trim();
        Vorher = vorher ?? string.Empty;
        Nachher = nachher;
        Quelle = quelle;
        Am = am;
        Status = Wikivorschlagsstatus.Offen;
    }

    public Guid Id { get; private set; }

    public Guid SeiteId { get; private set; }

    /// <summary>Version, auf die sich der Vorschlag bezieht; ist die Seite inzwischen weiter, muss neu geprüft werden.</summary>
    public int BasisVersion { get; private set; }

    public Vorschlagsart Art { get; private set; }

    public string Begruendung { get; private set; }

    public string Vorher { get; private set; }

    /// <summary>Vorgeschlagener neuer Text; <c>null</c> = nur Hinweis (z. B. „verwaist, bitte einordnen“).</summary>
    public string? Nachher { get; private set; }

    /// <summary>Welches KI-System den Vorschlag gemacht hat („KI (Mock)“, „KI-Schnittstelle“) – keine Person.</summary>
    public string Quelle { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public Wikivorschlagsstatus Status { get; private set; }

    public Guid? EntschiedenVonPersonId { get; private set; }

    public DateTimeOffset? EntschiedenAm { get; private set; }

    public void Entscheiden(bool annehmen, Guid personId, DateTimeOffset jetzt)
    {
        if (Status != Wikivorschlagsstatus.Offen)
        {
            throw new InvalidOperationException("Über den Vorschlag ist bereits entschieden.");
        }

        Status = annehmen ? Wikivorschlagsstatus.Angenommen : Wikivorschlagsstatus.Abgelehnt;
        EntschiedenVonPersonId = personId;
        EntschiedenAm = jetzt;
    }
}

/// <summary>Vorlagen für neue Wiki-Seiten.</summary>
public static class Wikivorlagen
{
    public static readonly IReadOnlyDictionary<string, string> Alle = new Dictionary<string, string>
    {
        ["Leer"] = string.Empty,
        ["Ablauf"] = "## Worum geht es?\n\n## Wer ist zuständig?\n\n## Schritte\n\n- \n\n## Fristen\n\n## Siehe auch\n\n- [[Fraktionssitzung]]",
        ["Politikfeld"] = "## Position der Fraktion\n\n## Aktuelle Themen\n\n- \n\n## Beschlüsse\n\n## Ansprechpartner (Themenpate)\n\n## Siehe auch\n",
        ["Gremium"] = "## Aufgaben des Gremiums\n\n## Unsere Mitglieder\n\n## Laufende Themen\n\n## Siehe auch\n",
    };
}
