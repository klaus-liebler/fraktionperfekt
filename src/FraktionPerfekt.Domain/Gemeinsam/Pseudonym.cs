using System.Security.Cryptography;

namespace FraktionPerfekt.Domain.Gemeinsam;

/// <summary>
/// Pseudonyme für das Protokoll: Einträge nennen nie Klarnamen, nur eine kurze, stabile Kennung.
/// Die Kennung wird aus einem Hash der Id gebildet (GUID v7 beginnt mit der Uhrzeit; deren Anfang wäre nicht eindeutig).
/// </summary>
public static class Pseudonym
{
    public static string Kurz(Guid id) => Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..8];

    public static string Person(Guid id) => $"Person #{Kurz(id)}";
}
