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
    public static Freigabe VerwaltungEinsehen(Rolle? rolle) => Freigabe.NurFuer(rolle, Rolle.Administration, Rolle.Vorstand);

    /// <summary>Abwesenheit melden: alle mit Gremiensitzen oder Fraktionsterminen.</summary>
    public static Freigabe AbwesenheitMelden(Rolle? rolle)
        => rolle is Rolle.Helferin
            ? Freigabe.Nein("Helfer:innen haben keine Gremiensitze; Abwesenheiten betreffen nur Mitglieder und sachkundige Bürger:innen.")
            : Freigabe.Ja;

    /// <summary>Helfer:innen sehen nur öffentliche Termine und Wahlkampftermine.</summary>
    public static bool SiehtTermin(Rolle? rolle, Terminart art) => rolle is not Rolle.Helferin || art.IstOeffentlich();

    /// <summary>Der Abwesenheitsgrund kann Gesundheitsdaten enthalten: nur die Person selbst und die Administration.</summary>
    public static bool SiehtAbwesenheitsgrund(Benutzer benutzer, Guid personId)
        => benutzer.PersonId == personId || benutzer.Rolle == Rolle.Administration;
}
