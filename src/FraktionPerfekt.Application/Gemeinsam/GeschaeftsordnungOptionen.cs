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
}
