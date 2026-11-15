using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Domain.Postausgang;

namespace FraktionPerfekt.Infrastructure.Postausgang;

/// <summary>
/// Web Push ohne Fremdbibliothek: Inhaltsverschlüsselung nach RFC 8291 (aes128gcm, RFC 8188) und VAPID nach RFC 8292
/// (JWT mit ES256). Alles mit System.Security.Cryptography (ECDH P-256, HKDF, AES-GCM, ECDSA).
/// </summary>
public static class WebPush
{
    public static string B64(ReadOnlySpan<byte> daten) => Convert.ToBase64String(daten).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] AusB64(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + ((4 - (s.Length % 4)) % 4), '='));
    }

    /// <summary>Neues VAPID-Schlüsselpaar (öffentlich: 65 Byte unkomprimierter Punkt, privat: 32 Byte), base64url.</summary>
    public static (string Oeffentlich, string Privat) SchluesselErzeugen()
    {
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ec.ExportParameters(true);
        return (B64([0x04, .. p.Q.X!, .. p.Q.Y!]), B64(p.D!));
    }

    /// <summary>
    /// Verschlüsselt den Inhalt für ein Gerät. Ergebnis ist der vollständige Rumpf: salt(16) | rs(4) | idlen(1) |
    /// keyid = öffentlicher Schlüssel des Absenders (65) | Chiffrat mit Tag.
    /// </summary>
    public static byte[] Verschluesseln(byte[] inhalt, byte[] geraetSchluessel, byte[] authGeheimnis, byte[]? salt = null, ECDiffieHellman? absender = null)
    {
        ArgumentNullException.ThrowIfNull(inhalt);
        salt ??= RandomNumberGenerator.GetBytes(16);
        using var eigener = absender is null ? ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256) : null;
        var ich = absender ?? eigener!;
        var ichParameter = ich.ExportParameters(false);
        byte[] ichOeffentlich = [0x04, .. ichParameter.Q.X!, .. ichParameter.Q.Y!];
        using var geraet = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = geraetSchluessel[1..33], Y = geraetSchluessel[33..65] },
        });
        var gemeinsam = ich.DeriveRawSecretAgreement(geraet.PublicKey);

        // RFC 8291 3.4: IKM = HKDF(auth, ecdh, "WebPush: info" 0x00 | ua_public | as_public, 32)
        byte[] keyInfo = [.. "WebPush: info\0"u8, .. geraetSchluessel, .. ichOeffentlich];
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, gemeinsam, 32, authGeheimnis, keyInfo);
        var cek = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt, "Content-Encoding: nonce\0"u8.ToArray());

        byte[] klartext = [.. inhalt, 0x02]; // Trenner: letzter (einziger) Datensatz
        var chiffrat = new byte[klartext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(cek, 16))
        {
            aes.Encrypt(nonce, klartext, chiffrat, tag);
        }

        const int rs = 4096;
        return [.. salt, (byte)(rs >> 24), (byte)(rs >> 16), (byte)(rs >> 8), rs & 0xFF, 65, .. ichOeffentlich, .. chiffrat, .. tag];
    }

    /// <summary>VAPID-Kopfzeile: <c>vapid t=&lt;JWT&gt;, k=&lt;öffentlicher Schlüssel&gt;</c>; Zielgruppe ist der Ursprung des Endpunkts.</summary>
    public static string VapidKopf(Uri endpunkt, VapidSchluessel vapid, DateTimeOffset jetzt)
    {
        ArgumentNullException.ThrowIfNull(endpunkt);
        ArgumentNullException.ThrowIfNull(vapid);
        var kopf = B64(Encoding.UTF8.GetBytes("""{"typ":"JWT","alg":"ES256"}"""));
        var anspruch = B64(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["aud"] = endpunkt.GetLeftPart(UriPartial.Authority),
            ["exp"] = jetzt.AddHours(12).ToUnixTimeSeconds(),
            ["sub"] = vapid.Betreff,
        }));
        var oeffentlich = AusB64(vapid.Oeffentlich);
        using var ec = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = AusB64(vapid.Privat),
            Q = new ECPoint { X = oeffentlich[1..33], Y = oeffentlich[33..65] },
        });
        var signatur = ec.SignData(Encoding.ASCII.GetBytes($"{kopf}.{anspruch}"), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"vapid t={kopf}.{anspruch}.{B64(signatur)}, k={vapid.Oeffentlich}";
    }
}

/// <summary>Zustellkanal Web Push (aktiv mit Push:Art=WebPush). Abgelaufene Abos (404/410) werden entfernt.</summary>
internal sealed class WebPushKanal(IDaten daten, VapidSchluessel vapid, HttpClient http, TimeProvider uhr) : IZustellKanal
{
    public Kanal Kanal => Kanal.Push;

    public async Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default)
    {
        ArgumentNullException.ThrowIfNull(nachricht);
        if (nachricht.PersonId is not { } person)
        {
            return "kein Empfänger";
        }

        var abos = await daten.ListeAsync(daten.Menge<PushAbo>().Where(a => a.PersonId == person).OrderBy(a => a.Id), abbruch);
        if (abos.Count == 0)
        {
            return "kein Gerät für Push angemeldet";
        }

        var inhalt = JsonSerializer.SerializeToUtf8Bytes(new { titel = nachricht.Betreff, text = nachricht.Inhalt, link = nachricht.Link, aktion = nachricht.Aktion });
        int ok = 0, weg = 0;
        foreach (var abo in abos)
        {
            var ziel = new Uri(abo.Endpunkt);
            using var anfrage = new HttpRequestMessage(HttpMethod.Post, ziel)
            {
                Content = new ByteArrayContent(WebPush.Verschluesseln(inhalt, WebPush.AusB64(abo.P256dh), WebPush.AusB64(abo.Auth))),
            };
            anfrage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            anfrage.Content.Headers.ContentEncoding.Add("aes128gcm");
            anfrage.Headers.TryAddWithoutValidation("Authorization", WebPush.VapidKopf(ziel, vapid, uhr.GetUtcNow()));
            anfrage.Headers.Add("TTL", "86400");
            anfrage.Headers.Add("Urgency", "normal");
            using var antwort = await http.SendAsync(anfrage, abbruch);
            if (antwort.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                daten.Entfernen(abo);
                weg++;
            }
            else
            {
                antwort.EnsureSuccessStatusCode();
                abo.Zugestellt(uhr.GetUtcNow());
                ok++;
            }
        }

        await daten.SpeichernAsync(abbruch);
        return $"Web Push an {ok} Gerät(e){(weg > 0 ? $", {weg} abgelaufene Anmeldung(en) entfernt" : "")}";
    }
}
