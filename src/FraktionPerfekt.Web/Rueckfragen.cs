using Radzen;

namespace FraktionPerfekt.Web;

/// <summary>
/// Einheitliche Rückfragen (aus SchoolCraft FA-3230): Löschen, Absagen, Abschließen und alles Unumkehrbare erst nach ausdrücklicher Bestätigung –
/// die Frage nennt, was betroffen ist und was folgt, die Schaltfläche heißt wie die Folge („Löschen“, nicht „OK“), ein Klick daneben schließt
/// nicht. Auf den statisch gerenderten Handy-Seiten übernimmt das <c>data-rueckfrage</c> in <c>verhalten.js</c>.
/// </summary>
public static class Rueckfragen
{
    public static async Task<bool> BestaetigenAsync(this DialogService dialoge, string frage, string titel, string ja = "Ja", string nein = "Abbrechen")
    {
        ArgumentNullException.ThrowIfNull(dialoge);
        return await dialoge.Confirm(frage, titel,
            new ConfirmOptions { OkButtonText = ja, CancelButtonText = nein, CloseDialogOnOverlayClick = false, Width = "min(480px, 94vw)" }) == true;
    }

    /// <summary>„… löschen?“ mit Hinweis auf die Folge.</summary>
    public static Task<bool> LoeschenAsync(this DialogService dialoge, string was, string? folge = null)
        => dialoge.BestaetigenAsync($"{was} wird gelöscht.{(folge is null ? "" : " " + folge)}", $"{was} löschen?", "Löschen");

    /// <summary>Führt <paramref name="aktion"/> nur nach Bestätigung aus (für Schaltflächen, die direkt eine Aktion auslösen).</summary>
    public static async Task WennBestaetigtAsync(this DialogService dialoge, string frage, string titel, string ja, Func<Task> aktion)
    {
        ArgumentNullException.ThrowIfNull(aktion);
        if (await dialoge.BestaetigenAsync(frage, titel, ja))
        {
            await aktion();
        }
    }

    /// <summary>
    /// Zurückziehen nach der Korrekturregel: Rückfrage, bei Bedarf mit Begründung. Liefert <c>null</c> bei Abbruch, sonst das Ergebnis mit Begründung
    /// (leer, wenn keine nötig ist).
    /// </summary>
    public static async Task<Components.Bausteine.GrundDialog.Ergebnis?> ZurueckziehenAsync(this DialogService dialoge, string titel, string frage, bool grundNoetig)
    {
        ArgumentNullException.ThrowIfNull(dialoge);
        var antwort = await dialoge.OpenAsync<Components.Bausteine.GrundDialog>(titel,
            new Dictionary<string, object?> { ["Frage"] = frage, ["GrundNoetig"] = grundNoetig },
            new DialogOptions { CloseDialogOnOverlayClick = false, Width = "min(480px, 94vw)" });
        return antwort as Components.Bausteine.GrundDialog.Ergebnis;
    }
}
