using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Geheime Abstimmung (Übertragung aus SchoolCraft, Punkt 1): getrennte Speicherung, kein Zwischenstand, endgültige Stimme, Rechte.</summary>
public sealed class GeheimeAbstimmungTests
{
    private static UmfrageDienst U(IServiceProvider d) => d.GetRequiredService<UmfrageDienst>();

    private static async Task<Umfrage> StartenAsync(TestUmgebung u, string titel, Umfrageart art = Umfrageart.Umfrage, IReadOnlyList<string>? optionen = null,
        IReadOnlyList<Guid>? nur = null)
    {
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        return await u.InAsync(d => U(d).AnlegenAsync(new NeueUmfrage(titel, null, art, u.Uhr.GetUtcNow().AddDays(2), 0, false,
            optionen ?? ["Kandidatin Rot", "Kandidat Blau"], Geheim: true, NurPersonen: nur)));
    }

    private static async Task AbstimmenAsync(TestUmgebung u, Guid umfrageId, string kennung, string option)
    {
        await u.AnmeldenAsync(kennung);
        var o = (await u.InAsync(d => U(d).DetailAsync(umfrageId)))!.Optionen.Single(x => x.Text == option).Id;
        await u.InAsync(d => U(d).AbstimmenAsync(umfrageId, [o]));
    }

    [Fact]
    public async Task Geheim_kein_Zwischenstand_Stimme_endgueltig_und_nirgends_mit_der_Person_verbunden()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var umfrage = await StartenAsync(u, "Wahl der Sprecherin im Schulausschuss");
        await AbstimmenAsync(u, umfrage.Id, "lkuehlert", "Kandidatin Rot");
        await AbstimmenAsync(u, umfrage.Id, "mgries", "Kandidat Blau");
        await AbstimmenAsync(u, umfrage.Id, "bkurney", "Kandidatin Rot");

        // Kein Zwischenstand, auch nicht für den Vorstand; wer abgestimmt hat, ist sichtbar (Wählerverzeichnis).
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var detail = (await u.InAsync(d => U(d).DetailAsync(umfrage.Id)))!;
        detail.Ergebnis.ShouldBeNull();
        detail.Teilnehmende.ShouldBe(3);
        detail.Abgestimmt.Count.ShouldBe(3);
        detail.Umfrage.Status.ShouldBe(Umfragestatus.Offen);

        // Stimme endgültig: kein zweites Mal, keine eigene Auswahl sichtbar.
        await u.AnmeldenAsync("lkuehlert");
        detail = (await u.InAsync(d => U(d).DetailAsync(umfrage.Id)))!;
        detail.MeineStimmen.ShouldBeEmpty();
        detail.IchHabeAbgestimmt.ShouldBeTrue();
        detail.Abstimmen.Begruendung!.ShouldContain("endgültig");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => U(d).AbstimmenAsync(umfrage.Id, [detail.Optionen[1].Id]))))
            .Message.ShouldContain("endgültig");

        // Datenbank: Vermerke mit Person, Stimmzettel ohne Person; keine namentlichen Stimmen; Ids der Stimmzettel zufällig (v4).
        await u.InAsync(async d =>
        {
            var db = d.GetRequiredService<FraktionDbContext>();
            (await db.Set<Stimmvermerk>().CountAsync(v => v.UmfrageId == umfrage.Id)).ShouldBe(3);
            (await db.Set<Stimme>().CountAsync(s => s.UmfrageId == umfrage.Id)).ShouldBe(0);
            var zettel = await db.Set<Stimmzettel>().Where(z => z.UmfrageId == umfrage.Id).ToListAsync();
            zettel.Count.ShouldBe(3);
            zettel.ShouldAllBe(z => z.Id.Version == 4);
            (await db.Set<Stimmberechtigung>().CountAsync(b => b.UmfrageId == umfrage.Id)).ShouldBeGreaterThan(15);

            // Protokoll und Postausgang nennen nie die Option.
            var protokoll = await db.Set<AuditEintrag>().Select(e => e.Beschreibung).ToListAsync();
            protokoll.ShouldNotContain(t => t.Contains("Rot", StringComparison.Ordinal) || t.Contains("Blau", StringComparison.Ordinal));
            protokoll.Count(t => t.EndsWith("hat abgestimmt", StringComparison.Ordinal)).ShouldBe(3);
            var post = await db.Set<AusgehendeNachricht>().Select(n => n.Betreff + " " + n.Inhalt).ToListAsync();
            post.ShouldNotContain(t => t.Contains("Rot", StringComparison.Ordinal) || t.Contains("Blau", StringComparison.Ordinal));
        });

        // Nach dem Ende (Vorstand) gibt es das Ergebnis.
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var e = await u.InAsync(d => U(d).AbschliessenAsync(umfrage.Id));
        e.Text.ShouldStartWith("Geheim abgestimmt. Meiste Stimmen: Kandidatin Rot (2)");
        (await u.InAsync(d => U(d).DetailAsync(umfrage.Id)))!.Ergebnis!.Optionen.Select(o => o.Stimmen).ShouldBe([2, 1]);
    }

    [Fact]
    public async Task Ende_automatisch_wenn_alle_abgestimmt_haben_und_unter_drei_Stimmen_keine_Auswertung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var ids = new List<Guid>();
        foreach (var k in new[] { "lkuehlert", "mgries", "bkurney" })
        {
            ids.Add((await u.AnmeldenAsync(k)).Id);
        }

        var drei = await StartenAsync(u, "Benennung Aufsichtsrat", Umfrageart.Meinungsbild, nur: ids);
        drei.Berechtigte.ShouldBe(3);
        await AbstimmenAsync(u, drei.Id, "lkuehlert", "Ja");
        await AbstimmenAsync(u, drei.Id, "mgries", "Nein");
        (await u.InAsync(d => U(d).DetailAsync(drei.Id)))!.Umfrage.Status.ShouldBe(Umfragestatus.Offen);
        await AbstimmenAsync(u, drei.Id, "bkurney", "Ja");
        var detail = (await u.InAsync(d => U(d).DetailAsync(drei.Id)))!;
        detail.Umfrage.Status.ShouldBe(Umfragestatus.Abgeschlossen);
        detail.Ergebnis!.Text.ShouldBe("Geheim abgestimmt. Angenommen: 2 ja, 1 nein, 0 Enthaltung (3 von 3 haben abgestimmt).");

        // Zwei Personen: Ergebnis wäre durchschaubar → keine Verteilung, nicht angenommen.
        var zwei = await StartenAsync(u, "Kleine Runde", Umfrageart.Meinungsbild, nur: ids.Take(2).ToList());
        await AbstimmenAsync(u, zwei.Id, "lkuehlert", "Ja");
        await AbstimmenAsync(u, zwei.Id, "mgries", "Ja");
        var klein = (await u.InAsync(d => U(d).DetailAsync(zwei.Id)))!;
        klein.Umfrage.Status.ShouldBe(Umfragestatus.Abgeschlossen);
        klein.Ergebnis!.ZuWenigeStimmen.ShouldBeTrue();
        klein.Ergebnis.Angenommen.ShouldBe(false);
        klein.Ergebnis.Text.ShouldStartWith("Zu wenige Stimmen für eine geheime Auswertung");
        klein.Ergebnis.Optionen.ShouldAllBe(o => o.Stimmen == 0);
    }

    [Fact]
    public async Task Rechte_feste_Liste_Kampagnenteam_Sachkundige_und_Terminfindung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var kuehlert = await u.AnmeldenAsync("lkuehlert");
        var umfrage = await StartenAsync(u, "Nur eine Person", Umfrageart.Meinungsbild, nur: [kuehlert.Id]);

        // Nicht auf der beim Start festgelegten Liste
        await u.AnmeldenAsync("mgries");
        var detail = (await u.InAsync(d => U(d).DetailAsync(umfrage.Id)))!;
        detail.Abstimmen.Begruendung!.ShouldContain("beim Start");
        (await u.InAsync(d => U(d).OffenFuerMichAsync())).ShouldNotContain(z => z.Umfrage.Id == umfrage.Id);

        // Kampagnenteam sieht und darf nichts
        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => U(d).DetailAsync(umfrage.Id))).ShouldBeNull();
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => U(d).AbstimmenAsync(umfrage.Id, [Guid.NewGuid()])))).Message.ShouldContain("Kampagnenteam");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => U(d).AnlegenAsync(new NeueUmfrage("x", null, Umfrageart.Meinungsbild,
            u.Uhr.GetUtcNow().AddDays(1), 0, false, [], Geheim: true))))).Message.ShouldContain("Kampagnenteam");

        // Sachkundige stimmen bei Antragsabstimmungen nicht mit (Einstellung der Geschäftsordnung)
        UmfrageDienst.Stimmberechtigt(Umfrageart.Antragsabstimmung, [Rolle.SachkundigeBuergerin]).ShouldBeFalse();
        UmfrageDienst.Stimmberechtigt(Umfrageart.Meinungsbild, [Rolle.SachkundigeBuergerin]).ShouldBeTrue();
        UmfrageDienst.Stimmberechtigt(Umfrageart.Meinungsbild, [Rolle.SachkundigeBuergerin],
            new Application.Gemeinsam.GeschaeftsordnungOptionen { SachkundigeStimmenMit = [] }).ShouldBeFalse();
        UmfrageDienst.Stimmberechtigt(Umfrageart.Antragsabstimmung, [Rolle.SachkundigeBuergerin],
            new Application.Gemeinsam.GeschaeftsordnungOptionen { SachkundigeStimmenMit = [Umfrageart.Antragsabstimmung] }).ShouldBeTrue();

        // Terminfindung ist nie geheim
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => U(d).AnlegenAsync(new NeueUmfrage("Klausur", null, Umfrageart.Terminfindung,
            u.Uhr.GetUtcNow().AddDays(1), 0, false, [], [new DateTime(2026, 12, 1, 18, 0, 0), new DateTime(2026, 12, 2, 18, 0, 0)], Geheim: true)))))
            .Message.ShouldContain("Terminfindung");
    }

    [Fact]
    public async Task Geheime_Antragsabstimmung_setzt_Status_und_Beschluss_ohne_Zwischenstand_am_Antrag()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("lkuehlert");
        var antrag = await u.InAsync(d => d.GetRequiredService<AntragDienst>().AnlegenAsync("Geheim beraten", "Text", null, null));
        await u.InAsync(d => d.GetRequiredService<AntragDienst>().StatusWechselnAsync(antrag.Id, Antragsstatus.Entwurf, null));
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => d.GetRequiredService<AntragDienst>().AbstimmungStartenAsync(antrag.Id, u.Uhr.GetUtcNow().AddDays(1), 0, geheim: true));
        var umfrageId = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!.Abstimmung!.Umfrage.Id;
        foreach (var k in new[] { "lkuehlert", "mgries", "bkurney", "kschwenken" })
        {
            await AbstimmenAsync(u, umfrageId, k, "Ja");
        }

        await u.AnmeldenAsync("uahrens");
        (await u.InAsync(d => U(d).DetailAsync(umfrageId)))!.Abstimmen.Begruendung!.ShouldContain("Ratsmitglieder");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var amAntrag = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!.Abstimmung!;
        amAntrag.Ergebnis.ShouldBeNull();
        amAntrag.Teilnehmende.ShouldBe(4);

        u.Uhr.Advance(TimeSpan.FromDays(2));
        await u.InAsync(d => U(d).FristenPruefenAsync());
        var nachher = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag.Id)))!;
        nachher.Antrag.Status.ShouldBe(Antragsstatus.Beschlossen);
        nachher.Abstimmung!.Ergebnis!.Text.ShouldContain("Angenommen: 4 ja");
    }

    [Fact]
    public async Task Namentliche_Abstimmung_bleibt_wie_bisher_mit_Zwischenstand()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var umfrage = await u.InAsync(d => U(d).AnlegenAsync(new NeueUmfrage("Offen", null, Umfrageart.Meinungsbild, u.Uhr.GetUtcNow().AddDays(1), 0, false, [])));
        await AbstimmenAsync(u, umfrage.Id, "lkuehlert", "Ja");
        await AbstimmenAsync(u, umfrage.Id, "lkuehlert", "Nein"); // änderbar
        var detail = (await u.InAsync(d => U(d).DetailAsync(umfrage.Id)))!;
        detail.Ergebnis!.Text.ShouldContain("0 ja, 1 nein");
        detail.MeineStimmen.ShouldHaveSingleItem();
    }
}
