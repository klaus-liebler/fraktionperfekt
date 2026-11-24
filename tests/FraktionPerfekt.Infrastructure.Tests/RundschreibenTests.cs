using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Postausgang;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Rundschreiben mit Gestaltungen, Anreden und Archiv als Vorlage (Übertragung SchoolCraft, Punkt 12).</summary>
public sealed class RundschreibenTests
{
    private static RundschreibenDienst R(IServiceProvider d) => d.GetRequiredService<RundschreibenDienst>();

    [Fact]
    public async Task Versand_je_Person_mit_eigener_Anrede_Kreis_und_Archiv()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var neu = new NeuesRundschreiben("Klausurtagung im Januar", "Die Klausur findet am 16. Januar statt.", Gestaltung.Einladung, Anredestil.Persoenlich, null,
            Empfaengerkreis.Kampagnenteam);

        // Rechte: nur Vorstand und Geschäftsführung
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).SendenAsync(neu)))).Message.ShouldContain("Vorstand");
        (await u.InAsync(d => R(d).ArchivAsync())).ShouldBeEmpty();

        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var kampagnenteam = await u.InAsync(d => R(d).EmpfaengerAsync(Empfaengerkreis.Kampagnenteam));
        kampagnenteam.ShouldAllBe(p => p.Benutzerkennung == "fbaeumer" || p.Benutzerkennung == "nfeldhaus");
        (await u.InAsync(d => R(d).EmpfaengerAsync(Empfaengerkreis.Ratsmitglieder))).Count.ShouldBe(18);
        (await u.InAsync(d => R(d).EmpfaengerAsync(Empfaengerkreis.Fraktion))).ShouldNotContain(p => p.Benutzerkennung == "fbaeumer");

        var vorschau = await u.InAsync(d => R(d).VorschauAsync(neu));
        vorschau.ShouldStartWith("EINLADUNG · ");
        vorschau.ShouldContain("Die Klausur findet am 16. Januar statt.");
        vorschau.ShouldContain("Johannes Wilp"); // Absender

        var r = await u.InAsync(d => R(d).SendenAsync(neu));
        r.Empfaenger.ShouldBe(kampagnenteam.Count);
        r.VersandtAm.ShouldNotBeNull();
        await u.InAsync(async d =>
        {
            var mails = await d.GetRequiredService<FraktionDbContext>().Set<AusgehendeNachricht>()
                .Where(n => n.Betreff == "Klausurtagung im Januar" && n.Kanal == Kanal.EMail).ToListAsync();
            mails.Count.ShouldBeLessThanOrEqualTo(kampagnenteam.Count);
            mails.ShouldAllBe(m => m.Inhalt.Contains("Hallo ", StringComparison.Ordinal) || m.Inhalt.Contains("Guten Tag", StringComparison.Ordinal)
                || m.Inhalt.Contains("Liebe", StringComparison.Ordinal) || m.Inhalt.Contains("Sehr geehrte", StringComparison.Ordinal));
        });

        // Archiv als Vorlage: neues Rundschreiben verweist auf das alte, förmliche bzw. Sammelanrede
        var archiv = await u.InAsync(d => R(d).ArchivAsync());
        archiv.ShouldHaveSingleItem().Rundschreiben.Id.ShouldBe(r.Id);
        var zweites = neu with { Betreff = "Klausurtagung: Ort", Anredestil = Anredestil.Sammelanrede, Sammelanrede = "Liebe Helferinnen und Helfer", VorlageId = r.Id };
        (await u.InAsync(d => R(d).VorschauAsync(zweites))).ShouldContain("Liebe Helferinnen und Helfer,");
        (await u.InAsync(d => R(d).SendenAsync(zweites))).VorlageId.ShouldBe(r.Id);
        (await u.InAsync(d => R(d).ArchivAsync())).Count.ShouldBe(2);

        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).SendenAsync(zweites with { Sammelanrede = " " })))).Message.ShouldContain("Anrede");
    }

    [Fact]
    public void Anreden_persoenlich_foermlich_und_Gestaltungen()
    {
        var jetzt = DateTimeOffset.UnixEpoch;
        var liz = new Person("Liz", "Kühlert", null, "lk", jetzt, anrede: "Frau");
        var klaus = new Person("Klaus", "Liebler", null, "kl", jetzt, titel: "Dr.", anrede: "Herr");
        klaus.AnspracheEinstellen("Herr", Anredeform.Sie, Begruessungsstil.Foermlich, true, jetzt);
        var r = new Rundschreiben("Info", "Text", Gestaltung.Fraktionsinfo, Anredestil.Persoenlich, null, Empfaengerkreis.Alle, Guid.NewGuid(), jetzt);
        r.MailtextFuer(liz, "CDU-Fraktion Greven", "Johannes Wilp", new DateOnly(2026, 11, 24)).ShouldStartWith("FRAKTIONSINFO · CDU-Fraktion Greven · 24.11.2026");
        r.MailtextFuer(liz, "F", "A", default).ShouldContain("Hallo Liz,");
        r.MailtextFuer(liz, "F", "A", default).ShouldContain("Viele Grüße");
        r.MailtextFuer(klaus, "F", "A", default).ShouldContain("Sehr geehrter Herr Dr. Liebler,");
        r.MailtextFuer(klaus, "F", "A", default).ShouldContain("Mit freundlichen Grüßen");
        var f = new Rundschreiben("Info", "Text", Gestaltung.Schlicht, Anredestil.Foermlich, null, Empfaengerkreis.Alle, Guid.NewGuid(), jetzt);
        f.MailtextFuer(liz, "F", "A", default).ShouldStartWith("Sehr geehrte Frau Kühlert,");
        Empfaengerkreis.Fraktion.Gehoert([Rolle.Helferin]).ShouldBeFalse();
        Empfaengerkreis.Alle.Gehoert([Rolle.Helferin]).ShouldBeTrue();
    }
}
