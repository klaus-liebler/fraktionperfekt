using System.Security.Cryptography;
using System.Text;
using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Termine;

/// <summary>
/// Persönliches Kalender-Abo (webcal). Der Link enthält ein zufälliges Token; gespeichert wird nur dessen SHA-256-Hash.
/// Wer den Link verliert, widerruft das Abo und legt ein neues an.
/// </summary>
public sealed class KalenderAbo
{
    private KalenderAbo()
    {
        TokenHash = string.Empty;
    }

    private KalenderAbo(Guid personId, string tokenHash, DateTimeOffset erstelltAm)
    {
        Id = Ids.Neu();
        PersonId = personId;
        TokenHash = tokenHash;
        ErstelltAm = erstelltAm;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    /// <summary>SHA-256 des Tokens als Hex; das Token selbst wird nie gespeichert.</summary>
    public string TokenHash { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset? WiderrufenAm { get; private set; }

    public DateTimeOffset? ZuletztAbgerufenAm { get; private set; }

    public bool Aktiv => WiderrufenAm is null;

    /// <summary>Legt ein Abo an und liefert das Token genau einmal (für den Link).</summary>
    public static (KalenderAbo Abo, string Token) Erzeugen(Guid personId, DateTimeOffset jetzt)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        return (new KalenderAbo(personId, Hash(token), jetzt), token);
    }

    public static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public void Widerrufen(DateTimeOffset jetzt) => WiderrufenAm ??= jetzt;

    public void AbrufVermerken(DateTimeOffset jetzt) => ZuletztAbgerufenAm = jetzt;

    private static string Base64Url(byte[] daten) => Convert.ToBase64String(daten).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
