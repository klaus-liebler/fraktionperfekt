using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Extensions.Options;

namespace FraktionPerfekt.Adapters.Mocks;

/// <summary>Konfiguration der Testanmeldung (Abschnitt "Anmeldung" in appsettings).</summary>
public sealed class TestanmeldungOptionen
{
    public const string Abschnitt = "Anmeldung";

    /// <summary>
    /// Optionaler gemeinsamer Zugangscode auf der Anmeldeseite. Gehört NICHT ins Repository, sondern in eine
    /// Umgebungsvariable (Anmeldung__Zugangscode).
    /// </summary>
    public string? Zugangscode { get; set; }
}

/// <summary>Prüft den optionalen Zugangscode der Testanmeldung.</summary>
public sealed class Zugangsschutz(IOptions<TestanmeldungOptionen> optionen)
{
    public bool ZugangscodeNoetig => !string.IsNullOrEmpty(optionen.Value.Zugangscode);

    public bool ZugangscodePruefen(string? eingabe)
    {
        if (!ZugangscodeNoetig)
        {
            return true;
        }

        return !string.IsNullOrEmpty(eingabe) && CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(eingabe)),
            SHA256.HashData(Encoding.UTF8.GetBytes(optionen.Value.Zugangscode!)));
    }
}

/// <summary>
/// Testanmeldung: Konto wählen, aktive Rolle oben rechts wechseln. Im Betrieb ersetzt durch Passkeys (ASP.NET Core Identity)
/// mit Anmeldelink per Mail als Rückfall.
/// </summary>
public static class MockAnmeldung
{
    public const string RollenClaim = ClaimTypes.Role;
    public const string KennungClaim = ClaimTypes.NameIdentifier;
    public const string VerfuegbareRolleClaim = "fraktion:rolle-verfuegbar";
    public const string PersonClaim = "fraktion:person";

    /// <summary>Erzeugt die Identität. Die aktive Rolle steht im Rollen-Claim, alle Rollen als "verfügbar".</summary>
    public static ClaimsPrincipal Anmelden(Benutzer benutzer, string authentifizierungsart)
    {
        ArgumentNullException.ThrowIfNull(benutzer);
        if (benutzer.Rolle is not { } aktiv || !benutzer.Rollen.Contains(aktiv))
        {
            throw new InvalidOperationException($"{benutzer.Name}: Die aktive Rolle muss eine der eigenen Rollen sein.");
        }

        var claims = new List<Claim>
        {
            new(KennungClaim, benutzer.Kennung),
            new(ClaimTypes.Name, benutzer.Name),
            new(RollenClaim, aktiv.ToString()),
        };
        if (benutzer.PersonId is { } p)
        {
            claims.Add(new Claim(PersonClaim, p.ToString("D")));
        }

        claims.AddRange(benutzer.Rollen.Select(r => new Claim(VerfuegbareRolleClaim, r.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authentifizierungsart, ClaimTypes.Name, RollenClaim));
    }

    /// <summary>Gleiche Identität mit anderer aktiver Rolle; <c>null</c>, wenn die Rolle nicht zur Person gehört.</summary>
    public static ClaimsPrincipal? RolleWechseln(ClaimsPrincipal principal, Rolle neueRolle, string authentifizierungsart)
    {
        var benutzer = AlsBenutzer(principal);
        return benutzer is null || !benutzer.Rollen.Contains(neueRolle) ? null : Anmelden(benutzer with { Rolle = neueRolle }, authentifizierungsart);
    }

    /// <summary>Liest Benutzer, aktive und verfügbare Rollen sowie die Person aus einer Identität.</summary>
    public static Benutzer? AlsBenutzer(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var kennung = principal.FindFirst(KennungClaim)?.Value ?? principal.Identity.Name ?? "unbekannt";
        var name = principal.Identity.Name ?? kennung;
        Rolle? rolle = Enum.TryParse<Rolle>(principal.FindFirst(RollenClaim)?.Value, out var r) ? r : null;
        var verfuegbar = principal.FindAll(VerfuegbareRolleClaim)
            .Select(c => Enum.TryParse<Rolle>(c.Value, out var v) ? v : (Rolle?)null).OfType<Rolle>().ToList();
        if (verfuegbar.Count == 0 && rolle is { } einzige)
        {
            verfuegbar.Add(einzige);
        }

        Guid? personId = Guid.TryParse(principal.FindFirst(PersonClaim)?.Value, out var p) ? p : null;
        return new Benutzer(kennung, name, rolle, verfuegbar, personId);
    }
}
