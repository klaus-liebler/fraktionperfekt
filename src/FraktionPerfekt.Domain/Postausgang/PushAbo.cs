using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Postausgang;

/// <summary>
/// Web-Push-Abonnement eines Geräts (Browser/PWA). Endpunkt und Schlüssel liefert der Browser beim Aktivieren; die
/// Inhalte werden Ende-zu-Ende verschlüsselt (RFC 8291), der Push-Dienst des Browsers sieht sie nicht.
/// </summary>
public sealed class PushAbo
{
    private PushAbo()
    {
        Endpunkt = string.Empty;
        P256dh = string.Empty;
        Auth = string.Empty;
    }

    public PushAbo(Guid personId, string endpunkt, string p256dh, string auth, string? geraet, DateTimeOffset jetzt)
    {
        if (!Uri.TryCreate(endpunkt, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Der Push-Endpunkt muss eine https-Adresse sein.", nameof(endpunkt));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(p256dh);
        ArgumentException.ThrowIfNullOrWhiteSpace(auth);
        Id = Ids.Neu();
        (PersonId, Endpunkt, P256dh, Auth, ErstelltAm) = (personId, endpunkt, p256dh, auth, jetzt);
        Geraet = string.IsNullOrWhiteSpace(geraet) ? null : geraet.Trim()[..Math.Min(geraet.Trim().Length, 120)];
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public string Endpunkt { get; private set; }

    /// <summary>Öffentlicher Schlüssel des Geräts (P-256, base64url).</summary>
    public string P256dh { get; private set; }

    /// <summary>Authentisierungsgeheimnis (16 Byte, base64url).</summary>
    public string Auth { get; private set; }

    public string? Geraet { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset? ZuletztZugestelltAm { get; private set; }

    public void Zugestellt(DateTimeOffset jetzt) => ZuletztZugestelltAm = jetzt;
}
