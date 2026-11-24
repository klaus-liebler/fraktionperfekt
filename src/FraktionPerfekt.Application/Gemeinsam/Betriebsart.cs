using Microsoft.Extensions.Configuration;

namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Echtbetrieb oder Übungsinstanz (aus SchoolCraft FK-14 übertragen, ohne Spielzeit): Eine Übungsinstanz ist ein Mandant mit <c>Uebung=true</c>,
/// eigener Datenbank (Kopie der Demodaten, zurücksetzbar) und eigenem Host. Dort sind geführte Touren und Testanmeldung immer an und der Demo-Schutz
/// verhindert echte Mails. Sobald es eine Übungsinstanz gibt, laufen die Touren nur noch dort; die Testanmeldung lässt sich für die übrigen Mandanten
/// mit <c>Anmeldung:TestanmeldungNurInUebung</c> abschalten (sobald es Passkeys gibt).
/// </summary>
public sealed class Betriebsart(IAktuellerMandant mandant, IReadOnlyList<MandantOptionen> mandanten, IConfiguration konfiguration)
{
    public bool IstUebung => mandant.Mandant.Uebung;

    private bool GibtUebung => mandanten.Any(m => m.Uebung);

    /// <summary>Geführte Touren auf der Hilfeseite (lösen echte Aktionen aus und brauchen Demodaten).</summary>
    public bool TourenAktiv => IstUebung || (!GibtUebung && konfiguration.GetValue("Abnahmemodus:Aktiv", true));

    /// <summary>Testanmeldung (Konto wählen ohne Passwort).</summary>
    public bool TestanmeldungErlaubt => IstUebung || !konfiguration.GetValue("Anmeldung:TestanmeldungNurInUebung", false);

    /// <summary>Kein echter Mailversand (Demodaten mit echten Namen bzw. Übungsinstanz).</summary>
    public bool DemoSchutz(bool demobetrieb) => demobetrieb || IstUebung;
}
