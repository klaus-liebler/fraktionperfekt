using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Postausgang;

/// <summary>Gestaltung eines Rundschreibens (Rahmen um den Text, aus SchoolCraft FA-820 ff. übertragen).</summary>
public enum Gestaltung
{
    /// <summary>Anrede, Text, Gruß.</summary>
    Schlicht,

    /// <summary>Kopf „Fraktionsinfo“ mit Fraktionsname und Datum, Gruß mit Absender und Fraktion.</summary>
    Fraktionsinfo,

    /// <summary>Kopf „Einladung“, Hinweis auf Zu- und Absage.</summary>
    Einladung,
}

/// <summary>Wie die Empfänger:innen angesprochen werden.</summary>
public enum Anredestil
{
    /// <summary>Je Person nach ihrer Einstellung (Du/Sie, Begrüßungsstil), z. B. „Hallo Liz“ oder „Sehr geehrte Frau Kühlert“.</summary>
    Persoenlich,

    /// <summary>Für alle förmlich mit Namen („Sehr geehrte Frau Kühlert“), unabhängig von der Einstellung.</summary>
    Foermlich,

    /// <summary>Eine gemeinsame Anrede für alle (z. B. „Liebe Kolleginnen und Kollegen“).</summary>
    Sammelanrede,
}

/// <summary>An wen das Rundschreiben geht.</summary>
public enum Empfaengerkreis
{
    /// <summary>Ratsmitglieder der Fraktion (Rolle Mitglied).</summary>
    Ratsmitglieder,

    /// <summary>Alle aus der Fraktion: Mitglieder, sachkundige Bürger:innen, Vorstand, Geschäftsführung – ohne Kampagnenteam.</summary>
    Fraktion,

    Kampagnenteam,

    /// <summary>Fraktion und Kampagnenteam.</summary>
    Alle,
}

public static class RundschreibenTexte
{
    public static string Anzeigename(this Empfaengerkreis k) => k switch
    {
        Empfaengerkreis.Ratsmitglieder => "Ratsmitglieder",
        Empfaengerkreis.Fraktion => "Fraktion (ohne Kampagnenteam)",
        Empfaengerkreis.Kampagnenteam => "Kampagnenteam",
        _ => "Fraktion und Kampagnenteam",
    };

    /// <summary>Gehört eine Person mit diesen Rollen zum Kreis?</summary>
    public static bool Gehoert(this Empfaengerkreis k, IReadOnlyList<Rolle> rollen)
    {
        ArgumentNullException.ThrowIfNull(rollen);
        var fraktion = rollen.Any(r => r is not Rolle.Helferin);
        return k switch
        {
            Empfaengerkreis.Ratsmitglieder => rollen.Contains(Rolle.Mitglied),
            Empfaengerkreis.Fraktion => fraktion,
            Empfaengerkreis.Kampagnenteam => rollen.Contains(Rolle.Helferin),
            _ => rollen.Count > 0,
        };
    }

    /// <summary>Förmliche Anrede mit Namen, unabhängig von der persönlichen Einstellung.</summary>
    public static string Foermlich(Person p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var titel = p.Titel is { } t ? $"{t} " : string.Empty;
        return p.Anrede switch
        {
            "Frau" => $"Sehr geehrte Frau {titel}{p.NachnameMitVorsatz}",
            "Herr" => $"Sehr geehrter Herr {titel}{p.NachnameMitVorsatz}",
            _ => $"Guten Tag {titel}{p.Anzeigename}",
        };
    }
}

/// <summary>
/// Rundschreiben an Mitglieder und Kampagnenteam (aus SchoolCraft FA-820 ff.): Betreff, Text, Gestaltung, Anredestil, Empfängerkreis; versandt über den
/// Postausgang (je Person mit eigener Anrede). Versandte Rundschreiben bilden das Archiv und dienen als Vorlage für neue.
/// </summary>
public sealed class Rundschreiben
{
    private Rundschreiben()
    {
        Betreff = string.Empty;
        Text = string.Empty;
    }

    public Rundschreiben(string betreff, string text, Gestaltung gestaltung, Anredestil anredestil, string? sammelanrede, Empfaengerkreis kreis,
        Guid erstelltVonPersonId, DateTimeOffset jetzt, Guid? vorlageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(betreff);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (anredestil == Anredestil.Sammelanrede && string.IsNullOrWhiteSpace(sammelanrede))
        {
            throw new ArgumentException("Bitte die gemeinsame Anrede angeben.", nameof(sammelanrede));
        }

        Id = Ids.Neu();
        Betreff = betreff.Trim();
        Text = text.Trim();
        Gestaltung = gestaltung;
        Anredestil = anredestil;
        Sammelanrede = anredestil == Anredestil.Sammelanrede ? sammelanrede!.Trim() : null;
        Kreis = kreis;
        ErstelltVonPersonId = erstelltVonPersonId;
        ErstelltAm = jetzt;
        VorlageId = vorlageId;
    }

    public Guid Id { get; private set; }

    public string Betreff { get; private set; }

    /// <summary>Text (Markdown, in der Mail als lesbarer Text).</summary>
    public string Text { get; private set; }

    public Gestaltung Gestaltung { get; private set; }

    public Anredestil Anredestil { get; private set; }

    public string? Sammelanrede { get; private set; }

    public Empfaengerkreis Kreis { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    /// <summary>Aus welchem archivierten Rundschreiben dieses als Vorlage entstanden ist.</summary>
    public Guid? VorlageId { get; private set; }

    public DateTimeOffset? VersandtAm { get; private set; }

    /// <summary>Zahl der Empfänger:innen beim Versand (Beleg).</summary>
    public int Empfaenger { get; private set; }

    public void Versandt(int empfaenger, DateTimeOffset jetzt) => (Empfaenger, VersandtAm) = (empfaenger, jetzt);

    /// <summary>Mailtext für eine Person: Kopf je Gestaltung, Anrede, Text, Gruß.</summary>
    public string MailtextFuer(Person p, string fraktion, string absender, DateOnly datum)
    {
        ArgumentNullException.ThrowIfNull(p);
        var anrede = Anredestil switch
        {
            Anredestil.Persoenlich => Ansprache.Begruessung(p),
            Anredestil.Foermlich => RundschreibenTexte.Foermlich(p),
            _ => Sammelanrede!,
        };
        var du = Anredestil == Anredestil.Persoenlich && p.Anredeform == Anredeform.Du;
        var kopf = Gestaltung switch
        {
            Gestaltung.Fraktionsinfo => $"FRAKTIONSINFO · {fraktion} · {datum:dd.MM.yyyy}\n{new string('=', 40)}\n\n",
            Gestaltung.Einladung => $"EINLADUNG · {fraktion}\n{new string('=', 40)}\n\n",
            _ => string.Empty,
        };
        var hinweis = Gestaltung == Gestaltung.Einladung
            ? (du ? "\n\nBitte sag in FraktionPerfekt kurz zu oder ab." : "\n\nUm Zu- oder Absage in FraktionPerfekt wird gebeten.")
            : string.Empty;
        var gruss = Anredestil == Anredestil.Persoenlich && du ? "Viele Grüße" : "Mit freundlichen Grüßen";
        return $"{kopf}{anrede},\n\n{Text}{hinweis}\n\n{gruss}\n{absender}\n{fraktion}";
    }
}
