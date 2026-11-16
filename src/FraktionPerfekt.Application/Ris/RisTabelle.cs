using System.Globalization;
using FraktionPerfekt.Domain.Gremien;

namespace FraktionPerfekt.Application.Ris;

/// <summary>Eine Zeile „Gremium – Funktion – Seit“ aus der Personenansicht des Ratsinformationssystems.</summary>
public sealed record RisZugehoerigkeit(string Person, string Gremium, string FunktionText, DateOnly Seit)
{
    public Funktion Funktion => Gremienfunktion.Deuten(FunktionText).Funktion;

    public Gremienamt Amt => Gremienfunktion.Deuten(FunktionText).Amt;
}

/// <summary>
/// Liest die Gremienzugehörigkeiten so, wie sie im RIS stehen (kopiert aus der Personenansicht): Blöcke aus einer Zeile mit dem
/// Namen und darunter je Gremium eine Zeile „Gremium TAB Funktion TAB Seit“ (auch „Gremium | Funktion | Seit“ oder mit „- “ davor).
/// Kopfzeilen („Gremium Funktion Seit“) und Leerzeilen werden übersprungen.
/// </summary>
public static class RisTabelle
{
    public static IReadOnlyList<RisZugehoerigkeit> Parsen(string text)
    {
        var liste = new List<RisZugehoerigkeit>();
        string? person = null;
        foreach (var roh in (text ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var zeile = roh.Trim();
            if (zeile.StartsWith("- ", StringComparison.Ordinal))
            {
                zeile = zeile[2..].Trim();
            }

            if (zeile.Length == 0)
            {
                continue;
            }

            var teile = (zeile.Contains('\t', StringComparison.Ordinal) ? zeile.Split('\t') : zeile.Split('|')).Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
            if (teile.Length >= 3 && DateOnly.TryParseExact(teile[^1], "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var seit))
            {
                if (person is null)
                {
                    throw new FormatException($"Zeile „{zeile}“ steht vor dem ersten Namen.");
                }

                liste.Add(new RisZugehoerigkeit(person, teile[0], string.Join(" ", teile[1..^1]), seit));
            }
            else if (teile.Length == 1 || !zeile.StartsWith("Gremium", StringComparison.OrdinalIgnoreCase))
            {
                person = teile.Length == 1 ? teile[0] : zeile; // neue Person (Kopfzeilen der Tabelle werden ignoriert)
            }
        }

        return liste;
    }
}

/// <summary>Bildet Funktionsbezeichnungen des RIS auf Funktion (Mitglied/Stellvertretung) und Amt (Vorsitz) ab.
/// Unbekannte Bezeichnungen bleiben als Text erhalten und gelten als ordentliche Mitgliedschaft.</summary>
public static class Gremienfunktion
{
    public static (Funktion Funktion, Gremienamt Amt) Deuten(string? text)
    {
        var t = (text ?? string.Empty).ToLowerInvariant();
        var stellv = t.Contains("stellv", StringComparison.Ordinal) || t.Contains("stellvertret", StringComparison.Ordinal);
        var vorsitz = t.Contains("vorsitz", StringComparison.Ordinal) || t.Contains("bürgermeister", StringComparison.Ordinal);
        if (vorsitz)
        {
            var amt = !stellv ? Gremienamt.Vorsitz : t.StartsWith("2.", StringComparison.Ordinal) ? Gremienamt.ZweiteStellvertretung : Gremienamt.ErsteStellvertretung;
            return (Funktion.Ordentlich, amt);
        }

        return (stellv ? Funktion.Stellvertretung : Funktion.Ordentlich, Gremienamt.Keins);
    }
}
