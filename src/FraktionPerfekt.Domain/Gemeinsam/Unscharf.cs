using System.Globalization;
using System.Text;

namespace FraktionPerfekt.Domain.Gemeinsam;

/// <summary>
/// Fehlertolerante Suche für Eingabehilfen und die Alles-Suche (übernommen aus SchoolCraft FA-3227, FA-3228): vergleicht Suchbegriffe mit Namen so, wie Menschen tippen –
/// ohne Rücksicht auf Groß-/Kleinschreibung, Umlaute (ä = ae = a), ß/ss, Akzente und Bindestriche, mit Präfixen („Bren“ → Brenner) und kleinen
/// Tippfehlern (ein vertauschter, fehlender oder falscher Buchstabe, bei langen Wörtern zwei). Jedes Wort der Eingabe muss zu einem Wort des
/// Namens passen. Rein und deterministisch, damit Server und App gleich treffen.
/// </summary>
public static class Unscharf
{
    /// <summary>Bewertung 0 (kein Treffer) bis 100 (genau gleich); je besser, desto weiter oben.</summary>
    public static int Bewertung(string eingabe, string text)
    {
        var suche = Woerter(eingabe);
        if (suche.Count == 0)
        {
            return 0;
        }

        var woerter = Woerter(text);
        if (woerter.Count == 0)
        {
            return 0;
        }

        var gesamt = 0;
        foreach (var s in suche)
        {
            var beste = woerter.Max(w => Wortbewertung(s, w));
            if (beste == 0)
            {
                return 0;
            }

            gesamt += beste;
        }

        return gesamt / suche.Count;
    }

    /// <summary>Ob <paramref name="text"/> zur Eingabe passt.</summary>
    public static bool Passt(string eingabe, string text) => Bewertung(eingabe, text) > 0;

    /// <summary>Kandidaten nach Bewertung sortiert, nur Treffer.</summary>
    public static IReadOnlyList<T> Ordnen<T>(string eingabe, IEnumerable<T> kandidaten, Func<T, string> text, int hoechstens = 20) =>
        kandidaten.Select(k => (k, b: Bewertung(eingabe, text(k)))).Where(x => x.b > 0)
            .OrderByDescending(x => x.b).ThenBy(x => text(x.k), StringComparer.CurrentCulture).Take(hoechstens).Select(x => x.k).ToList();

    /// <summary>Normalisierte Wörter eines Textes.</summary>
    public static IReadOnlyList<string> Woerter(string text) =>
        Normalisieren(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Kleinbuchstaben, Umlaute und ß ausgeschrieben, Akzente entfernt, Satzzeichen und Bindestriche als Leerzeichen.</summary>
    public static string Normalisieren(string text)
    {
        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text.ToLowerInvariant())
        {
            switch (c)
            {
                case 'ä': sb.Append("ae"); break;
                case 'ö': sb.Append("oe"); break;
                case 'ü': sb.Append("ue"); break;
                case 'ß': sb.Append("ss"); break;
                default:
                    var zerlegt = c.ToString().Normalize(NormalizationForm.FormD);
                    foreach (var z in zerlegt)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(z) == UnicodeCategory.NonSpacingMark)
                        {
                            continue;
                        }

                        sb.Append(char.IsLetterOrDigit(z) ? z : ' ');
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    private static int Wortbewertung(string s, string w)
    {
        if (s == w)
        {
            return 100;
        }

        if (w.StartsWith(s, StringComparison.Ordinal))
        {
            return 80 + Math.Min(15, 15 * s.Length / w.Length);
        }

        // „Mueller“ getippt als „Muller“: Umlaut ohne e
        var ohneE = Vereinfacht(w);
        if (Vereinfacht(s) == ohneE || ohneE.StartsWith(Vereinfacht(s), StringComparison.Ordinal))
        {
            return 75;
        }

        if (s.Length >= 3 && w.Contains(s, StringComparison.Ordinal))
        {
            return 60;
        }

        if (s.Length < 3)
        {
            return 0;
        }

        // Tippfehler: gegen das ganze Wort und gegen den gleich langen Anfang (Präfix mit Fehler, „Brnner“, „Hartm“)
        var erlaubt = s.Length >= 8 ? 2 : 1;
        var abstand = Math.Min(Abstand(s, w), w.Length > s.Length ? Abstand(s, w[..s.Length]) : int.MaxValue);
        return abstand <= erlaubt ? 50 - 10 * abstand : 0;
    }

    private static string Vereinfacht(string w) => w.Replace("ae", "a").Replace("oe", "o").Replace("ue", "u");

    /// <summary>Damerau-Levenshtein (eingeschränkt): Einfügen, Löschen, Ersetzen, Vertauschen zweier Nachbarn.</summary>
    public static int Abstand(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var kosten = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + kosten);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }
        }

        return d[a.Length, b.Length];
    }
}
