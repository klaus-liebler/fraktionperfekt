namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Hinweise beim Erfassen eines Namens: steht ein akademischer Grad im Vornamen („Dr. Klaus“) oder ein Namensvorsatz im Nachnamen
/// („von Berg“), fragt die Oberfläche „Als Titel übernehmen?“ bzw. „Als Vorsatz übernehmen?“ (Datenbank-Regel 7). Amtsbezeichnungen
/// wie „Prof.“ gelten nicht als Titel.
/// </summary>
public static class Namenserkennung
{
    private static readonly string[] Vorsaetze = ["von und zu", "von der", "von dem", "van der", "van den", "van de", "von", "van", "zu", "vom", "de", "di", "da"];

    /// <summary>Akademischer Grad am Anfang des Vornamens („Dr.“, „Dr. med.“, „Dr.-Ing.“, „Dipl.-Ing.“) und der Rest; sonst <c>null</c>.</summary>
    public static (string Titel, string Ohne)? TitelImVornamen(string? vorname)
    {
        var teile = (vorname ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var n = 0;
        while (n < teile.Length && IstGrad(teile[n], n > 0))
        {
            n++;
        }

        return n == 0 || n == teile.Length ? null : (string.Join(' ', teile[..n]), string.Join(' ', teile[n..]));
    }

    /// <summary>Namensvorsatz am Anfang des Nachnamens („von Berg“ → „von“, „Berg“); sonst <c>null</c>.</summary>
    public static (string Vorsatz, string Ohne)? VorsatzImNachnamen(string? nachname)
    {
        var n = (nachname ?? string.Empty).Trim();
        foreach (var v in Vorsaetze)
        {
            if (n.Length > v.Length + 1 && n.StartsWith(v + " ", StringComparison.Ordinal))
            {
                return (v, n[(v.Length + 1)..].Trim());
            }
        }

        return null;
    }

    private static bool IstGrad(string t, bool folgend)
        => t.StartsWith("Dr.", StringComparison.Ordinal) || t.StartsWith("Dipl.", StringComparison.Ordinal)
           || (folgend && t is "med." or "rer." or "nat." or "phil." or "jur." or "h.c." or "dent." or "pol." or "oec.");
}
