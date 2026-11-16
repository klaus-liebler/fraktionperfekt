using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Stammdaten;

/// <summary>Ein Eintrag der Zeitleiste: eine Erfassung zu einem Feld (nach VibeVHS).</summary>
public sealed record HistorienEintrag(
    string Datenart,
    string Feld,
    string? AlterWert,
    string NeuerWert,
    DateOnly GueltigVon,
    DateOnly? GueltigBis,
    DateTimeOffset ErfasstAm,
    string ErfasstVon,
    Erfassungsart Art,
    string Grund,
    DateTimeOffset? ErsetztAm)
{
    /// <summary>Durch eine spätere Erfassung ersetzt (z. B. Korrektur); wird blass angezeigt.</summary>
    public bool Ersetzt => ErsetztAm is not null;

    public string ArtAnzeige => Erfassung.Anzeigename(Art);
}

/// <summary>
/// Baut aus allen Versionen aller Angaben einer Person eine chronologische Zeitleiste (neueste Erfassung oben), wie VibeVHS.
/// Technische Abgrenzungen (verkürzte Kopien nach einer Änderung ab Datum) werden weggelassen.
/// </summary>
public static class Datenhistorie
{
    /// <summary>Reihenfolge der Filter-Chips.</summary>
    public static IReadOnlyList<string> Datenarten { get; } = ["Name", "Person", "Ansprache", "Funktion", "Rollen", "Adressen", "Telefon", "E-Mail", "Einwilligungen"];

    /// <summary>Datenarten mit Kontaktdaten (sichtbar nur für die Person selbst, die Geschäftsführung oder mit Einwilligung).</summary>
    public static IReadOnlySet<string> Kontaktdaten { get; } = new HashSet<string>(["Adressen", "Telefon", "E-Mail"], StringComparer.Ordinal);

    public static IReadOnlyList<HistorienEintrag> Erstellen(Person person, bool mitKontakten = true)
    {
        ArgumentNullException.ThrowIfNull(person);
        var alle = person.AlleVersionen.Where(v => mitKontakten || !Kontaktdaten.Contains(v.Datenart)).ToList();
        return alle
            .Where(v => v.Erfassungsart != Erfassungsart.Abgrenzung)
            .Select((v, i) => (v, i))
            .OrderByDescending(x => x.v.ErfasstAm)
            .ThenBy(x => Rang(x.v.Datenart))
            .ThenBy(x => x.i)
            .Select(x =>
            {
                var ende = Fortsetzung(x.v, alle);
                return new HistorienEintrag(x.v.Datenart, x.v.Feld, AlterWert(x.v, alle), x.v.Wert, x.v.GueltigVon, ende.GueltigBis,
                    x.v.ErfasstAm, x.v.ErfasstVon, x.v.Erfassungsart, x.v.Grund, ende.ErsetztAm);
            })
            .ToList();
    }

    /// <summary>Nur technisch abgegrenzte Versionen gelten fachlich weiter (bis zum neuen Ende); erst eine Korrektur ersetzt sie wirklich.</summary>
    private static IHistorienwert Fortsetzung(IHistorienwert v, List<IHistorienwert> alle)
    {
        var aktuell = v;
        while (aktuell.ErsetztAm is { } ersetzt
               && alle.FirstOrDefault(x => x.Erfassungsart is Erfassungsart.Abgrenzung && x.GetType() == aktuell.GetType()
                                           && x.Gegenstand == aktuell.Gegenstand && x.ErfasstAm == ersetzt
                                           && x.GueltigVon == aktuell.GueltigVon && x.Wert == aktuell.Wert) is { } weiter)
        {
            aktuell = weiter;
        }

        return aktuell;
    }

    private static int Rang(string datenart)
    {
        var i = Datenarten.ToList().IndexOf(datenart);
        return i < 0 ? int.MaxValue : i;
    }

    /// <summary>Was galt für denselben Gegenstand unmittelbar vor dieser Erfassung?</summary>
    private static string? AlterWert(IHistorienwert v, List<IHistorienwert> alle)
    {
        if (v.Erfassungsart == Erfassungsart.Ersterfassung)
        {
            return null;
        }

        var kurzVorher = v.ErfasstAm.AddTicks(-1);
        var tag = v.Erfassungsart == Erfassungsart.Korrektur || v.GueltigVon == DateOnly.MinValue ? v.GueltigVon : v.GueltigVon.AddDays(-1);
        return alle.FirstOrDefault(x => x.GetType() == v.GetType() && x.Gegenstand == v.Gegenstand && !x.IstEndemarke
                                        && x.GiltAm(tag) && x.BekanntAm(kurzVorher))?.Wert;
    }
}
