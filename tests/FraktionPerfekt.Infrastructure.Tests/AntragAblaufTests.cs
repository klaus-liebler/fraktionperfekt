using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Heute;
using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class AntragAblaufTests
{
    [Fact]
    public async Task Antrag_von_der_Idee_bis_zum_eingereichten_Beschluss()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        AntragDienst A(IServiceProvider d) => d.GetRequiredService<AntragDienst>();

        await u.AnmeldenAsync("lkuehlert");
        var asb = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).Single(g => g.Kurzname == "SchulA");
        var antrag = await u.InAsync(d => A(d).AnlegenAsync("Schulwege sicher machen", "Die Verwaltung legt bis zum Sommer 2026 ein Schulwegkonzept vor.", asb.Id,
            "Schule und Bildung"));
        await u.InAsync(d => A(d).StatusWechselnAsync(antrag.Id, Antragsstatus.Entwurf, null));
        await u.InAsync(d => A(d).FassungSpeichernAsync(antrag.Id, "Die Verwaltung legt bis zum Sommer 2026 ein Schulwegkonzept für alle Grundschulen vor.", "alle Schulen"));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => A(d).FassungSpeichernAsync(antrag.Id,
            "Die Verwaltung legt bis zum Sommer 2026 ein Schulwegkonzept für alle Grundschulen vor.", null)))).Message.ShouldContain("unverändert");

        // Kommentar und Änderungsvorschlag einer anderen Person; nur Autorin oder Vorstand entscheidet.
        await u.AnmeldenAsync("seisenrichter");
        await u.InAsync(d => A(d).KommentierenAsync(antrag.Id, "Sommer 2026", "Knapp, aber machbar."));
        await u.InAsync(d => A(d).VorschlagenAsync(antrag.Id, "Sommer 2026", "Herbst 2026", "Mehr Zeit für die Elternbefragung"));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => A(d).VorschlagenAsync(antrag.Id, "gibt es nicht", "x", null)))).Message.ShouldContain("nicht mehr");
        var detail = (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!;
        detail.Vorschlaege.Single().Entscheiden.Erlaubt.ShouldBeFalse();

        // Vorstand kann erst abstimmen lassen, wenn über Vorschläge entschieden ist.
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!.AbstimmungStarten.Begruendung!.ShouldContain("Änderungsvorschläge");
        await u.InAsync(d => A(d).VorschlagEntscheidenAsync(detail.Vorschlaege.Single().Vorschlag.Id, true));
        detail = (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!;
        detail.Aktuell.Nummer.ShouldBe(3);
        detail.Aktuell.Text.ShouldContain("Herbst 2026");
        Textvergleich.Vergleichen(detail.Fassungen[1].Text, detail.Aktuell.Text).ShouldContain(s => s.Art == Vergleichsart.Eingefuegt && s.Text.StartsWith("Herbst", StringComparison.Ordinal));

        var start = await u.InAsync(d => A(d).AbstimmungStartenAsync(antrag.Id, u.Uhr.GetUtcNow().AddDays(2), 50));
        start.NaechsteSchritte[0].Ziel.ShouldStartWith("abstimmungen/");
        detail = (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!;
        detail.Antrag.Status.ShouldBe(Antragsstatus.InAbstimmung);
        detail.TextBearbeiten.Erlaubt.ShouldBeFalse();
        var umfrageId = detail.Antrag.AbstimmungId!.Value;

        // Abstimmen: Ratsmitglieder ja, sachkundige Bürger:innen nein (Begründung); Stimme änderbar.
        var optionen = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrageId)))!.Optionen;
        Guid Ja() => optionen.Single(o => o.Text == "Ja").Id;
        foreach (var k in new[] { "jwilp", "ebrinkhaus", "djoud", "lkuehlert", "bkurney", "kschwenken", "seisenrichter", "mgries", "thorstmann", "pnientiedt", "mbenning", "kliebler", "tschubert" })
        {
            await u.AnmeldenAsync(k);
            await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrageId, [Ja()]));
        }

        await u.AnmeldenAsync("ssterthues");
        (await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync())).OffeneAbstimmungen.ShouldContain(z => z.Umfrage.Id == umfrageId);
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrageId, [optionen.Single(o => o.Text == "Ja").Id]));
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrageId, [optionen.Single(o => o.Text == "Nein").Id]));
        await u.AnmeldenAsync("uahrens");
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrageId)))!.Abstimmen.Begruendung!.ShouldContain("Ratsmitglieder");

        // Fristablauf: Fristenprüfung schließt ab → beschlossen, Beschluss in der Beschlusslage.
        u.Uhr.Advance(TimeSpan.FromDays(3));
        u.Benutzer.Aktuell = FraktionPerfekt.Application.Sicherheit.Benutzer.Automatik;   // wie der stündliche Fristen-Job
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().FristenPruefenAsync())).ShouldBeGreaterThanOrEqualTo(1);
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        detail = (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!;
        detail.Antrag.Status.ShouldBe(Antragsstatus.Beschlossen);
        detail.Abstimmung!.Ergebnis.Text.ShouldStartWith("Angenommen: 13 ja, 1 nein");
        var beschluesse = await u.InAsync(d => d.GetRequiredService<IDaten>().ListeAsync(d.GetRequiredService<IDaten>().Menge<Beschluss>().Where(b => b.AntragId == antrag.Id)));
        beschluesse.ShouldHaveSingleItem().Ergebnis.ShouldBe(Beschlussergebnis.Angenommen);

        // Einreichen mit Nummer, Sitzung zuordnen, PDF
        var eingereicht = await u.InAsync(d => A(d).StatusWechselnAsync(antrag.Id, Antragsstatus.Eingereicht, null));
        eingereicht.NaechsteSchritte.ShouldContain(s => s.Ziel.EndsWith("antrag.pdf", StringComparison.Ordinal));
        detail = (await u.InAsync(d => A(d).DetailAsync(antrag.Id)))!;
        detail.Antrag.Nummer.ShouldBe($"A-{u.Uhr.GetUtcNow().Year}-002");   // A-…-001 ist der Demo-Antrag
        var sitzungen = await u.InAsync(d => A(d).PassendeSitzungenAsync(antrag.Id));
        sitzungen.ShouldAllBe(t => t.GremiumId == asb.Id);
        await u.InAsync(d => A(d).ZuordnenAsync(antrag.Id, null, sitzungen[0].Id));
        var (datei, pdf) = await u.InAsync(d => A(d).PdfAsync(antrag.Id));
        datei.ShouldContain("A-");
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");
    }

    [Fact]
    public async Task Terminfindung_mit_Mehrfachauswahl_schlaegt_Termin_vor()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        var a = new DateTime(2026, 12, 5, 10, 0, 0);
        var b = new DateTime(2026, 12, 12, 10, 0, 0);
        var umfrage = await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AnlegenAsync(
            new NeueUmfrage("Klausur", null, Umfrageart.Terminfindung, u.Uhr.GetUtcNow().AddDays(2), 0, false, [], [b, a])));
        var o = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage.Id)))!.Optionen;
        o[0].Zeitpunkt.ShouldBe(a);
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrage.Id, [o[0].Id, o[1].Id]));
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrage.Id, [o[1].Id]));
        await u.AnmeldenAsync("fbaeumer");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbstimmenAsync(umfrage.Id, [o[1].Id]))))
            .Message.ShouldContain("Helfer");

        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbschliessenAsync(umfrage.Id)))).Message.ShouldContain("Vorstand");
        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        var e = await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().AbschliessenAsync(umfrage.Id));
        e.Gewinner!.Zeitpunkt.ShouldBe(b);
        var detail = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage.Id)))!;
        detail.NaechsteSchritte[0].Ziel.ShouldStartWith("kalender?neu=1&beginn=2026-12-12T10:00");
        detail.Abstimmen.Erlaubt.ShouldBeFalse();
    }
}
