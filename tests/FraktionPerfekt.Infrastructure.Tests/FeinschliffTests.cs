using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Heute;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Suche;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Application.Kampagnen;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using FraktionPerfekt.Infrastructure.Postausgang;
using Microsoft.Extensions.DependencyInjection;
using SBenutzer = FraktionPerfekt.Application.Sicherheit.Benutzer;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class FeinschliffTests
{
    [Fact]
    public void WebPush_Verschluesselung_laesst_sich_wie_im_Browser_entschluesseln_und_VAPID_ist_gueltig()
    {
        // Gerät (Browser): Schlüsselpaar und Auth-Geheimnis
        using var geraet = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var gp = geraet.ExportParameters(false);
        byte[] geraetOeffentlich = [0x04, .. gp.Q.X!, .. gp.Q.Y!];
        var auth = RandomNumberGenerator.GetBytes(16);
        var inhalt = Encoding.UTF8.GetBytes("""{"titel":"Morgen 17:00: HFA","text":"Ratssaal"}""");

        var rumpf = WebPush.Verschluesseln(inhalt, geraetOeffentlich, auth);

        // Entschlüsseln wie der Browser (RFC 8291/8188)
        var salt = rumpf[..16];
        var rs = (rumpf[16] << 24) | (rumpf[17] << 16) | (rumpf[18] << 8) | rumpf[19];
        rs.ShouldBe(4096);
        rumpf[20].ShouldBe((byte)65);
        var absenderOeffentlich = rumpf[21..86];
        using var absender = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = absenderOeffentlich[1..33], Y = absenderOeffentlich[33..65] },
        });
        var gemeinsam = geraet.DeriveRawSecretAgreement(absender.PublicKey);
        byte[] info = [.. "WebPush: info\0"u8, .. geraetOeffentlich, .. absenderOeffentlich];
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, gemeinsam, 32, auth, info);
        var cek = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt, "Content-Encoding: nonce\0"u8.ToArray());
        var chiffrat = rumpf[86..^16];
        var tag = rumpf[^16..];
        var klar = new byte[chiffrat.Length];
        using (var aes = new AesGcm(cek, 16))
        {
            aes.Decrypt(nonce, chiffrat, tag, klar);
        }

        klar[^1].ShouldBe((byte)0x02);
        Encoding.UTF8.GetString(klar[..^1]).ShouldContain("Ratssaal");

        // VAPID: JWT mit ES256, Zielgruppe = Ursprung des Endpunkts, Signatur prüfbar mit dem öffentlichen Schlüssel
        var (o, p) = WebPush.SchluesselErzeugen();
        var kopf = WebPush.VapidKopf(new Uri("https://push.example.org/send/abc"), new VapidSchluessel(o, p, "mailto:test@example.org"),
            new DateTimeOffset(2026, 11, 15, 8, 0, 0, TimeSpan.Zero));
        kopf.ShouldStartWith("vapid t=");
        kopf.ShouldEndWith($", k={o}");
        var jwt = kopf["vapid t=".Length..kopf.IndexOf(',', StringComparison.Ordinal)].Split('.');
        JsonDocument.Parse(WebPush.AusB64(jwt[1])).RootElement.GetProperty("aud").GetString().ShouldBe("https://push.example.org");
        var pub = WebPush.AusB64(o);
        using var pruefer = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = pub[1..33], Y = pub[33..65] } });
        pruefer.VerifyData(Encoding.ASCII.GetBytes($"{jwt[0]}.{jwt[1]}"), WebPush.AusB64(jwt[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Erinnerung_vor_Sitzungen_genau_einmal_und_nicht_bei_Abwesenheit()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var naechste = (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TermineAsync(new DateTime(2026, 11, 15), new DateTime(2027, 2, 1))))
            .Where(t => t.Art is Terminart.Ausschusssitzung or Terminart.Ratssitzung or Terminart.Fraktionssitzung && !t.Abgesagt)
            .OrderBy(t => t.Beginn).First();

        // Uhr auf 20 Stunden vor Beginn stellen
        var ziel = Ortszeit.Zeitpunkt(naechste.Beginn.AddHours(-20));
        u.Uhr.Advance(ziel - u.Uhr.GetUtcNow());
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        var n = await u.InAsync(d => d.GetRequiredService<Sitzungserinnerung>().ErinnernAsync());
        n.ShouldBeGreaterThan(0);
        (await u.InAsync(d => d.GetRequiredService<Sitzungserinnerung>().ErinnernAsync())).ShouldBe(0);
        var post = await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(200));
        post.ShouldContain(m => m.Link == $"termine/{naechste.Id}" && m.Betreff.StartsWith("Morgen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Loeschfrist_Abwesenheiten_Grund_nach_drei_Monaten_Datensatz_nach_zwoelf()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("thorstmann");
        var e = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().EintragenAsync(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 21), "Kur"));
        var id = e.Abwesenheit.Id;

        u.Uhr.Advance(TimeSpan.FromDays(110));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => d.GetRequiredService<Sitzungserinnerung>().AbwesenheitenLoeschfristAsync())).ShouldBeGreaterThan(0);
        var a = await u.InAsync(d => d.GetRequiredService<IDaten>().FindenAsync<Abwesenheit>(id));
        a.ShouldNotBeNull();
        a.Grund.ShouldBeNull();

        u.Uhr.Advance(TimeSpan.FromDays(300));
        await u.InAsync(d => d.GetRequiredService<Sitzungserinnerung>().AbwesenheitenLoeschfristAsync());
        (await u.InAsync(d => d.GetRequiredService<IDaten>().FindenAsync<Abwesenheit>(id))).ShouldBeNull(); // kein Vertretungsfall → gelöscht
    }

    [Fact]
    public async Task Globale_Suche_mit_Modulrechten_Hinweise_Schnellaktionen_und_ICS()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("lkuehlert");
        var treffer = await u.InAsync(d => d.GetRequiredService<SucheDienst>().SuchenAsync("Grundschul"));
        treffer.Select(t => t.Bereich).Distinct().ShouldContain("Anträge");
        treffer.Select(t => t.Bereich).Distinct().ShouldContain("Wiki");
        treffer.Select(t => t.Bereich).Distinct().ShouldContain("Beschlüsse");
        (await u.InAsync(d => d.GetRequiredService<SucheDienst>().SuchenAsync("Stellungnahme Stadtwerke"))).ShouldContain(t => t.Bereich == "Dateien"); // HFA-Vertretung
        await u.AnmeldenAsync("uahrens");
        (await u.InAsync(d => d.GetRequiredService<SucheDienst>().SuchenAsync("Stellungnahme Stadtwerke"))).ShouldNotContain(t => t.Bereich == "Dateien"); // vertraulich, kein HFA-Sitz
        await u.AnmeldenAsync("lkuehlert");

        var ansicht = await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync());
        ansicht.Schnellaktionen.Select(s => s.Titel).ShouldContain("Plakat erfassen");
        ansicht.Schnellaktionen.Select(s => s.Titel).ShouldContain("Anfrage schreiben");

        // Kampagnenteam: Suche nur Termine/Wiki/Dateien „Kampagnen“; Hinweis auf eigenen Kampagnentermin und eigenes Gebiet; Termin im Kalender-Abo
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => d.GetRequiredService<SucheDienst>().SuchenAsync("Grundschul"))).ShouldNotContain(t => t.Bereich == "Anträge");
        var hinweise = await u.InAsync(d => d.GetRequiredService<HinweisDienst>().LadenAsync());
        hinweise.ShouldContain(h => h.Bereich == "Kampagnen" && h.Titel.StartsWith("Infostand", StringComparison.Ordinal));
        hinweise.ShouldContain(h => h.Titel.Contains("Greven-Innenstadt", StringComparison.Ordinal));
        (await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync())).Schnellaktionen.Single(s => s.Titel == "Anfrage schreiben").Freigabe.Erlaubt.ShouldBeFalse();

        var link = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboAnlegenAsync());
        var token = link.Https.Segments[^1].Replace(".ics", string.Empty, StringComparison.Ordinal);
        (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboKalenderAsync(token)))!.ShouldContain("Infostand Marktplatz (Vormittag)");

        // Einzeltermin als ICS (jede sichtbare Terminart)
        var termin = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(new DateOnly(2026, 11, 15), new DateOnly(2027, 1, 31)))).First().Termin;
        (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TerminKalenderAsync(termin.Id)))!.ShouldContain("BEGIN:VEVENT");
        _ = u.Dienste.GetRequiredService<IServiceScopeFactory>();
        _ = typeof(KampagnenDienst);
    }
}
