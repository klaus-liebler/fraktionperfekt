using FraktionPerfekt.Domain.Gremien;

namespace FraktionPerfekt.Application.Stammdaten;

/// <summary>Ein Sitz in einem Gremium an einem Stichtag.</summary>
public sealed record Sitz(Guid MitgliedschaftId, Guid PersonId, Funktion Funktion, int? Rang);

/// <summary>Besetzung eines Gremiums an einem Tag (nach aktuellem Wissensstand).</summary>
public static class Besetzung
{
    public static IReadOnlyList<Sitz> Am(IEnumerable<Mitgliedschaft> mitgliedschaften, Guid gremiumId, DateOnly tag, DateTimeOffset bekanntAm)
        => mitgliedschaften.Where(m => m.GremiumId == gremiumId)
            .Select(m => (m, Stand: m.StandAm(tag, bekanntAm)))
            .Where(x => x.Stand is not null)
            .Select(x => new Sitz(x.m.Id, x.m.PersonId, x.Stand!.Funktion, x.Stand.Rang))
            .OrderBy(s => s.Funktion).ThenBy(s => s.Rang ?? int.MaxValue).ThenBy(s => s.PersonId)
            .ToList();

    public static IReadOnlyList<Guid> OrdentlicheAm(IEnumerable<Mitgliedschaft> mitgliedschaften, Guid gremiumId, DateOnly tag, DateTimeOffset bekanntAm)
        => Am(mitgliedschaften, gremiumId, tag, bekanntAm).Where(s => s.Funktion == Funktion.Ordentlich).Select(s => s.PersonId).ToList();

    /// <summary>Stellvertretungen in Rangfolge (Rang 1 zuerst; ohne Rang ans Ende).</summary>
    public static IReadOnlyList<Guid> StellvertretungenAm(IEnumerable<Mitgliedschaft> mitgliedschaften, Guid gremiumId, DateOnly tag,
        DateTimeOffset bekanntAm)
        => Am(mitgliedschaften, gremiumId, tag, bekanntAm).Where(s => s.Funktion == Funktion.Stellvertretung).Select(s => s.PersonId).ToList();
}
