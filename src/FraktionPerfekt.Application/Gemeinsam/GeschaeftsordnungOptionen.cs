using FraktionPerfekt.Domain.Abstimmungen;

namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Regeln aus der Geschäftsordnung der Fraktion (Abschnitt „Geschaeftsordnung“ in appsettings). ⚑ Alle Werte sind Annahmen und mit der
/// tatsächlichen Geschäftsordnung abzugleichen; deshalb Einstellung statt fester Regel im Code.
/// </summary>
public sealed class GeschaeftsordnungOptionen
{
    public const string Abschnitt = "Geschaeftsordnung";

    /// <summary>Geheime Abstimmungen werden erst ab so vielen Stimmen ausgewertet (darunter wäre die Stimme Einzelner erkennbar). ⚑</summary>
    public int GeheimMindeststimmen { get; set; } = Auswertung.GeheimMindeststimmenStandard;

    /// <summary>Bei diesen Abstimmungsarten stimmen sachkundige Bürger:innen mit (Antragsabstimmungen standardmäßig nur Ratsmitglieder). ⚑</summary>
    public Umfrageart[] SachkundigeStimmenMit { get; set; } = [Umfrageart.Meinungsbild, Umfrageart.Umfrage, Umfrageart.Terminfindung];

    /// <summary>Beschlussfähig, wenn mehr als dieser Anteil (Prozent) der Stimmberechtigten anwesend ist (Standard: mehr als die Hälfte). ⚑</summary>
    public int BeschlussfaehigMehrAlsProzent { get; set; } = 50;

    /// <summary>Frist einer geheimen Abstimmung im Sitzungsmodus in Minuten (alle stimmen gleichzeitig am Handy). ⚑</summary>
    public int SitzungAbstimmungMinuten { get; set; } = 10;

    /// <summary>Wahlen: Enthaltungen zählen bei der Mehrheit mit (Standard: nein, Mehrheit der gültigen Stimmen ohne Enthaltungen). ⚑</summary>
    public bool WahlEnthaltungenZaehlen { get; set; }

    /// <summary>Wahlen: Erreicht im ersten Wahlgang niemand die Mehrheit, folgt eine Stichwahl der beiden Bestplatzierten. ⚑</summary>
    public bool WahlStichwahl { get; set; } = true;

    /// <summary>Wahlen: Wahlgänge standardmäßig geheim (Personenwahl). ⚑</summary>
    public bool WahlGeheimStandard { get; set; } = true;

    /// <summary>Letzter Tag der Wahlperiode (Kommunalwahl NRW 2030 angenommen) – Standardende einer Amtszeit. ⚑</summary>
    public DateOnly WahlperiodeEnde { get; set; } = new(2030, 10, 31);

    /// <summary>So viele Tage vor Ende einer Amtszeit legt die Fristenprüfung die Aufgabe „Neuwahl vorbereiten“ an.</summary>
    public int NeuwahlVorlaufTage { get; set; } = 56;
}
