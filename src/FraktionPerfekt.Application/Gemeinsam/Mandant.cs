using Microsoft.Extensions.Configuration;

namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Ein Mandant (eine Fraktion) mit eigener Datenbank – keine MandantId-Spalten (Datenbank-Regel 6). Vorbereitung: Konfiguration je Mandant,
/// Auflösung je Anfrage über den Host; im Betrieb gibt es bisher genau einen Mandanten.
/// </summary>
public sealed class MandantOptionen
{
    public string Kennung { get; set; } = "standard";

    /// <summary>Anzeige, Briefkopf und Absender von Mails.</summary>
    public string Name { get; set; } = "CDU-Fraktion Greven";

    /// <summary>Zweite Zeile des Briefkopfs.</summary>
    public string? Zusatz { get; set; } = "im Rat der Stadt Greven";

    /// <summary>Region bzw. Bundesland (z. B. für Feiertage, Kommunalwahlrecht).</summary>
    public string Region { get; set; } = "NW";

    /// <summary>Druckfassung des Logos (Pfad relativ zum Web-Stammverzeichnis).</summary>
    public string? Logo { get; set; } = "marke/logo-druck.png";

    /// <summary>Absenderadresse für Mails (leer = Standard des Mailkanals).</summary>
    public string? Absender { get; set; }

    /// <summary>IANA-Zeitzone für Anzeige und Eingabe (leer = Systemzeitzone).</summary>
    public string? Zeitzone { get; set; } = "Europe/Berlin";

    /// <summary>Verbindungszeichenfolge der eigenen Datenbank (leer = ConnectionStrings:FraktionPerfekt).</summary>
    public string? Datenbank { get; set; }

    /// <summary>
    /// Übungsinstanz (aus SchoolCraft FK-14): eigene Datenbank mit Demodaten, zurücksetzbar, Touren und Testanmeldung aktiv, nie echte Mails.
    /// </summary>
    public bool Uebung { get; set; }

    /// <summary>Hostnamen, unter denen der Mandant erreichbar ist (leer = Standardmandant).</summary>
    public IReadOnlyList<string> Hosts { get; set; } = [];

    /// <summary>
    /// Mandanten aus der Konfiguration: Abschnitt „Mandanten“ (Liste) oder „Mandant“ (einer); ohne beides ein Standardmandant mit den
    /// bisherigen Einstellungen (Fraktion:Anzeigename).
    /// </summary>
    public static IReadOnlyList<MandantOptionen> Lesen(IConfiguration konfiguration)
    {
        ArgumentNullException.ThrowIfNull(konfiguration);
        var liste = konfiguration.GetSection("Mandanten").Get<List<MandantOptionen>>();
        if (liste is { Count: > 0 })
        {
            return liste;
        }

        var einer = konfiguration.GetSection("Mandant").Get<MandantOptionen>() ?? new MandantOptionen();
        if (konfiguration["Fraktion:Anzeigename"] is { Length: > 0 } name && konfiguration.GetSection("Mandant:Name").Value is null)
        {
            einer.Name = name;
        }

        return [einer];
    }
}

/// <summary>Der Mandant der laufenden Anfrage bzw. des laufenden Hintergrundauftrags.</summary>
public interface IAktuellerMandant
{
    MandantOptionen Mandant { get; }
}

/// <summary>Mandantenauswahl je Bereich: Standard ist der erste Mandant; die Webanwendung legt ihn je Anfrage über den Host fest.</summary>
public sealed class MandantAuswahl(IReadOnlyList<MandantOptionen> mandanten) : IAktuellerMandant
{
    private MandantOptionen? _festgelegt;

    public IReadOnlyList<MandantOptionen> Alle => mandanten;

    public MandantOptionen Mandant => _festgelegt ?? mandanten[0];

    public void Festlegen(MandantOptionen mandant) => _festgelegt = mandant;

    /// <summary>Wurde der Mandant in diesem Bereich schon ausdrücklich festgelegt (z. B. für einen Hintergrundauftrag oder die Übungsinstanz)?</summary>
    public bool Festgelegt => _festgelegt is not null;

    /// <summary>Mandant zum Host (ohne Port); unbekannter Host = Standardmandant.</summary>
    public MandantOptionen ZumHost(string? host)
        => mandanten.FirstOrDefault(m => m.Hosts.Contains(host ?? string.Empty, StringComparer.OrdinalIgnoreCase)) ?? mandanten[0];
}
