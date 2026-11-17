namespace FraktionPerfekt.Domain.Personen;

/// <summary>
/// Rollen in der Fraktion. Eine Person kann mehrere haben; nach der Anmeldung wählt sie die aktive Rolle.
/// Neue Rollen nur am Ende anfügen (gespeichert wird der Name, die Reihenfolge bestimmt die Anzeige).
/// </summary>
public enum Rolle
{
    /// <summary>Fraktionsgeschäftsführung: Mitglieder, Gremien, Ressourcen und Rechte verwalten.</summary>
    Administration,

    /// <summary>Fraktionsvorsitz und Stellvertretung: Freigaben, Abstimmungen eröffnen, Terminbesetzung, Eskalationen.</summary>
    Vorstand,

    /// <summary>Kassenverantwortliche Person (Finanzmodul folgt in Stufe 3).</summary>
    Kassier,

    /// <summary>Ratsmitglied der Fraktion.</summary>
    Mitglied,

    /// <summary>Mitglied in Ausschüssen ohne Ratsmandat.</summary>
    SachkundigeBuergerin,

    /// <summary>Kampagnenteam (Ortsverband, Unterstützer:innen): nur Kampagnen und öffentliche Termine. Technischer Name bleibt „Helferin“.</summary>
    Helferin,
}

public static class RollenExtensions
{
    public static string Anzeigename(this Rolle rolle) => rolle switch
    {
        Rolle.Administration => "Administration",
        Rolle.Vorstand => "Vorstand",
        Rolle.Kassier => "Schatzmeister/Kassier",
        Rolle.Mitglied => "Mitglied",
        Rolle.SachkundigeBuergerin => "Sachkundige Bürger:innen",
        Rolle.Helferin => "Kampagnenteam",
        _ => rolle.ToString(),
    };

    /// <summary>Liest eine Rolle aus Anzeigename oder technischem Namen.</summary>
    public static bool TryParse(string? text, out Rolle rolle)
    {
        rolle = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var r in Enum.GetValues<Rolle>())
        {
            if (string.Equals(r.Anzeigename(), text.Trim(), StringComparison.OrdinalIgnoreCase)
                || (r == Rolle.Helferin && string.Equals("Helfer:innen", text.Trim(), StringComparison.OrdinalIgnoreCase))
                || string.Equals(r.ToString(), text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                rolle = r;
                return true;
            }
        }

        return false;
    }
}
