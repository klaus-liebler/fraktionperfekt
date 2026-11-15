using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Application.Sicherheit;

/// <summary>Wer handelt gerade? Für automatische Abläufe ist das <see cref="Benutzer.Automatik"/>.</summary>
public interface IAktuellerBenutzer
{
    ValueTask<Benutzer> ErmittelnAsync(CancellationToken abbruch = default);
}

/// <param name="Kennung">Anmeldename.</param>
/// <param name="Name">Anzeigename.</param>
/// <param name="Rolle">Aktive Rolle (eine von <see cref="VerfuegbareRollen"/>).</param>
/// <param name="VerfuegbareRollen">Alle Rollen der Person; zwischen ihnen kann sie wechseln.</param>
/// <param name="PersonId">Die Person der Fraktion hinter dem Konto.</param>
public sealed record Benutzer(
    string Kennung,
    string Name,
    Rolle? Rolle,
    IReadOnlyList<Rolle>? VerfuegbareRollen = null,
    Guid? PersonId = null)
{
    public static Benutzer Automatik { get; } = new("automatik", "Automatik", null);

    public bool IstAutomatik => Kennung == Automatik.Kennung;

    public string Anzeige => Rolle is { } r ? $"{Name} ({r.Anzeigename()})" : Name;

    public IReadOnlyList<Rolle> Rollen => VerfuegbareRollen ?? (Rolle is { } r ? [r] : []);

    /// <summary>Für Dienste, die eine angemeldete Person brauchen.</summary>
    public Guid PersonIdPflicht => PersonId ?? throw new InvalidOperationException("Diese Aktion braucht eine angemeldete Person.");
}
