using FraktionPerfekt.Application.Oberflaeche;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Application.Sicherheit;

/// <summary>Wer darf was? Jede Regel liefert eine <see cref="Freigabe"/> mit Begründung für die Oberfläche.</summary>
public static class Rechte
{
    /// <summary>Personen, Gremien, Mitgliedschaften und Rang pflegen, RIS-Abgleich starten.</summary>
    public static Freigabe StammdatenPflegen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration);

    /// <summary>Termine im gemeinsamen Kalender anlegen.</summary>
    public static Freigabe TermineAnlegen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration, Rolle.Vorstand, Rolle.Mitglied);

    /// <summary>Eskalierte Vertretungen bearbeiten und Aufgaben des Vorstands erledigen.</summary>
    public static Freigabe VertretungenSteuern(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Vorstand, Rolle.Administration);

    /// <summary>Sitzungen vorbereiten, einladen und führen (zusätzlich: Sitzungsleitung und Protokollführung der jeweiligen Sitzung).</summary>
    public static Freigabe SitzungenSteuern(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Vorstand, Rolle.Administration);

    /// <summary>Datensicherung auslösen, prüfen und einsehen.</summary>
    public static Freigabe BetriebSteuern(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration);

    /// <summary>Postausgang und Protokoll einsehen.</summary>
    /// <summary>Finanzen einsehen: Kassier, Vorstand, Geschäftsführung.</summary>
    public static Freigabe FinanzenEinsehen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Kassier, Rolle.Vorstand, Rolle.Administration);

    /// <summary>Buchen, Spenden erfassen, auszahlen: nur der Kassier.</summary>
    public static Freigabe FinanzenBuchen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Kassier);

    /// <summary>Kampagnen steuern (anlegen, Gebiete zuteilen, Termine und Vorlagen anlegen): Vorstand und Geschäftsführung.</summary>
    public static Freigabe KampagnenSteuern(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Vorstand, Rolle.Administration);

    public static Freigabe VerwaltungEinsehen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration, Rolle.Vorstand);

    /// <summary>Abwesenheit melden: alle mit Gremiensitzen oder Fraktionsterminen.</summary>
    public static Freigabe AbwesenheitMelden(Rolle? rolle)
        => rolle is Rolle.Helferin
            ? Freigabe.Nein("Das Kampagnenteam hat keine Gremiensitze; Abwesenheiten betreffen nur Mitglieder und sachkundige Bürger:innen.")
            : Freigabe.Ja;

    /// <summary>Das Kampagnenteam sieht nur öffentliche Termine und Kampagnentermine.</summary>
    public static bool SiehtTermin(Rolle? rolle, Terminart art) => rolle is not Rolle.Helferin || art.IstOeffentlich();

    /// <summary>Konzept und technische Doku lesen und kommentieren (Hilfe lesen dürfen alle).</summary>
    public static Freigabe DokumentationKommentieren(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration, Rolle.Vorstand);

    /// <summary>Impressum der Fraktion pflegen.</summary>
    public static Freigabe ImpressumPflegen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration);

    /// <summary>Der Abwesenheitsgrund kann Gesundheitsdaten enthalten: nur die Person selbst und die Administration.</summary>
    public static bool SiehtAbwesenheitsgrund(Benutzer benutzer, Guid personId)
        => benutzer.PersonId == personId || benutzer.Rolle == Rolle.Administration;
}
