using System.Globalization;

namespace FraktionPerfekt.Application.Oberflaeche;

/// <summary>Breite eines Balkens (0–100 %) aus Wert und Maximum; negative Werte ergeben 0, Werte über dem Maximum 100.</summary>
public static class Balkenrechnung
{
    public static double Prozent(double wert, double maximum)
    {
        if (maximum <= 0 || double.IsNaN(wert) || double.IsNaN(maximum) || wert <= 0)
        {
            return 0;
        }

        return Math.Clamp(wert / maximum * 100, 0, 100);
    }

    public static double Prozent(decimal wert, decimal maximum) => Prozent((double)wert, (double)maximum);

    /// <summary>Für das style-Attribut (Punkt als Dezimaltrenner, eine Nachkommastelle).</summary>
    public static string Css(double prozent) => prozent.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    /// <summary>Liegt der Wert außerhalb (negativ oder über dem Maximum)? Dann wird der Balken als Warnung gefärbt.</summary>
    public static bool Ausserhalb(double wert, double maximum) => wert < 0 || (maximum > 0 && wert > maximum);
}
