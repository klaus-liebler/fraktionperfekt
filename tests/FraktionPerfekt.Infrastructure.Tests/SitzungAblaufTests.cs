using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Dokumente;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class SitzungAblaufTests
{
    private static async Task<(Termin Erste, Termin Zweite)> ZweiFraktionssitzungenAsync(TestUmgebung u)
    {
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var fraktion = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).First(g => g.Kurzname == "Fraktion");
        var erste = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AnlegenAsync(new NeuerTermin("Fraktionssitzung", Terminart.Fraktionssitzung,
            new DateTime(2026, 11, 30, 19, 0, 0), new DateTime(2026, 11, 30, 21, 0, 0), "Fraktionsbüro", fraktion.Id, null)));
        var zweite = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AnlegenAsync(new NeuerTermin("Fraktionssitzung", Terminart.Fraktionssitzung,
            new DateTime(2026, 12, 14, 19, 0, 0), new DateTime(2026, 12, 14, 21, 0, 0), "Fraktionsbüro", fraktion.Id, null)));
        erste.NaechsteSchritte.ShouldContain(s => s.Ziel == $"sitzungen?termin={erste.Termin.Id}");
        return (erste.Termin, zweite.Termin);
    }

    [Fact]
    public async Task Einladung_Sitzungsmodus_Freigabe_und_vertagter_TOP_wandert_weiter()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var (erste, zweite) = await ZweiFraktionssitzungenAsync(u);
        SitzungDienst Dienst(IServiceProvider d) => d.GetRequiredService<SitzungDienst>();

        // Vorbereiten aus Vorlage
        var vorbereitet = await u.InAsync(d => Dienst(d).VorbereitenAsync(erste.Id));
        vorbereitet.NaechsteSchritte[0].Titel.ShouldContain("Einladung");
        var sitzung = (await u.InAsync(d => Dienst(d).ZuTerminAsync(erste.Id)))!;
        var detail = (await u.InAsync(d => Dienst(d).DetailAsync(sitzung.Id)))!;
        detail.Tops.Where(t => t.Top.Fest).Select(t => t.Top.Titel).ShouldBe(Sitzungsvorlagen.Fraktionssitzung);
        detail.Tops.ShouldContain(t => t.Top.AntragId != null && t.Top.Titel.StartsWith("Antrag: ", StringComparison.Ordinal));   // anstehende Anträge hängen an

        // Ein Mitglied meldet einen TOP an; er landet vor „Verschiedenes“.
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => Dienst(d).TopAnmeldenAsync(sitzung.Id, "Rathausstraße: Radfahrstreifen", "Bitte vor der Ausschusssitzung klären"));
        detail = (await u.InAsync(d => Dienst(d).DetailAsync(sitzung.Id)))!;
        detail.Tops[^1].Top.Titel.ShouldBe("Verschiedenes");
        detail.Tops[^2].Top.Titel.ShouldBe("Rathausstraße: Radfahrstreifen");
        detail.Tops[^2].AngemeldetVon.ShouldBe("Liz Kühlert");
        detail.Einladen.Erlaubt.ShouldBeFalse();   // Mitglied darf nicht einladen

        // Nach der Frist: Mitglied kann nicht mehr anmelden (Begründung nennt die Frist).
        u.Uhr.SetUtcNow(new DateTimeOffset(2026, 11, 28, 12, 0, 0, TimeSpan.Zero));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => Dienst(d).TopAnmeldenAsync(sitzung.Id, "Zu spät", null)))).Message.ShouldContain("Frist");

        // Vorstand lädt ein: Mail und Push an alle Mitglieder der Fraktion plus Geschäftsführung.
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var einladung = await u.InAsync(d => Dienst(d).EinladenAsync(sitzung.Id));
        einladung.Meldung.ShouldContain("51 Personen");
        var post = await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(200));
        post.Count(n => n.Kanal == Kanal.EMail && n.Betreff.StartsWith("Einladung", StringComparison.Ordinal)).ShouldBe(51);
        post.First(n => n.Kanal == Kanal.EMail && n.Betreff.StartsWith("Einladung", StringComparison.Ordinal)).Inhalt.ShouldContain("Rathausstraße: Radfahrstreifen");

        // Zusage einer Person, dann Sitzungsmodus: Anwesenheit aus Rückmeldungen.
        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AntwortenAsync(erste.Id, true));
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => Dienst(d).BeginnenAsync(sitzung.Id));
        detail = (await u.InAsync(d => Dienst(d).DetailAsync(sitzung.Id)))!;
        detail.Anwesenheit.Single(a => a.Name == "Liz Kühlert").Status.ShouldBe(Anwesenheitsstatus.Anwesend);

        var schulTop = detail.Tops.Single(t => t.Top.Titel == "Bericht des Vorstands").Top;
        var rathausTop = detail.Tops.Single(t => t.Top.Titel.StartsWith("Rathausstraße", StringComparison.Ordinal)).Top;
        await u.InAsync(d => Dienst(d).NotizSetzenAsync(schulTop.Id, "Schulwegeprüfung zur Grundschulplanung angeregt.", true));
        var beschluss = await u.InAsync(d => Dienst(d).BeschlussErfassenAsync(schulTop.Id, "Schulwegeprüfung beantragen", 10, 1, 1));
        beschluss.Ergebnis.ShouldBe(Beschlussergebnis.Angenommen);
        var anna = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("lkuehlert"));
        await u.InAsync(d => Dienst(d).AufgabeZuweisenAsync(schulTop.Id, anna!.Id, "Fragen an die Schulverwaltung", new DateOnly(2026, 12, 10)));
        await u.InAsync(d => Dienst(d).TopVertagenAsync(rathausTop.Id));
        await u.InAsync(d => Dienst(d).BeendenAsync(sitzung.Id));

        // Freigabe nur durch Leitung/Vorstand; danach unveränderlich, Protokoll per Mail.
        await u.AnmeldenAsync("lkuehlert");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => Dienst(d).FreigebenAsync(sitzung.Id)))).Message.ShouldContain("Sitzungsleitung");
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var freigabe = await u.InAsync(d => Dienst(d).FreigebenAsync(sitzung.Id));
        freigabe.NaechsteSchritte[0].Titel.ShouldContain("Aufgaben");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => Dienst(d).NotizSetzenAsync(schulTop.Id, "später", false)))).Message.ShouldContain("freigegeben");
        var text = await u.InAsync(d => Dienst(d).ProtokolltextAsync(sitzung.Id));
        text.ShouldContain("**Beschluss:** Schulwegeprüfung beantragen – Angenommen (10 ja, 1 nein, 1 Enthaltung)");
        text.ShouldContain("Aufgabe: Fragen an die Schulverwaltung (bis 10.12.2026)");
        (await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(300))).ShouldContain(n => n.Betreff.StartsWith("Protokoll:", StringComparison.Ordinal));

        // PDF im Fraktionslayout
        var pdf = u.Dienste.GetRequiredService<IPdfErzeugung>().Erzeugen(new PdfDokument("Protokoll", "Test", text));
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");

        // Vertagter TOP erscheint beim Vorbereiten der nächsten Sitzung, Volltextsuche findet den Beschluss.
        await u.InAsync(d => Dienst(d).VorbereitenAsync(zweite.Id));
        var naechste = (await u.InAsync(d => Dienst(d).ZuTerminAsync(zweite.Id)))!;
        var tops = (await u.InAsync(d => Dienst(d).DetailAsync(naechste.Id)))!.Tops;
        tops.ShouldContain(t => t.Top.Titel.StartsWith("Rathausstraße", StringComparison.Ordinal) && t.Top.VertagtAusTopId == rathausTop.Id);
        tops[^1].Top.Titel.ShouldBe("Verschiedenes");
        (await u.InAsync(d => Dienst(d).SuchenAsync("schulwegeprüfung"))).ShouldHaveSingleItem().SitzungId.ShouldBe(sitzung.Id);

        // Aufgabe aus dem Protokoll: Anna sieht sie, Erinnerung kommt einmal.
        await u.AnmeldenAsync("lkuehlert");
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().UebersichtAsync())).Meine.ShouldContain(z => z.Aufgabe.Titel == "Fragen an die Schulverwaltung");
        u.Uhr.SetUtcNow(new DateTimeOffset(2026, 12, 10, 8, 0, 0, TimeSpan.Zero));
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().ErinnernAsync())).ShouldBeGreaterThanOrEqualTo(1);   // auch Demo-Aufgaben
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().ErinnernAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task Gremienprotokoll_mit_TOPs_aus_dem_RIS_und_Korb_mit_Themenpaten()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var hfa = (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TermineAsync(new DateTime(2026, 12, 8, 17, 0, 0), new DateTime(2026, 12, 8, 17, 1, 0)))).Single();
        await u.InAsync(d => d.GetRequiredService<SitzungDienst>().VorbereitenAsync(hfa.Id));
        var s = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().ZuTerminAsync(hfa.Id)))!;
        s.Art.ShouldBe(Sitzungsart.Gremienprotokoll);
        var detail = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(s.Id)))!;
        detail.Tops.ShouldContain(t => t.Top.Titel.Contains("Haushalt 2026", StringComparison.Ordinal) || t.Top.Titel.Contains("Bauinvestitionen", StringComparison.Ordinal));
        detail.TopAnmelden.Erlaubt.ShouldBeTrue();

        // Themenpate bekommt passende Aufgabe direkt; Korbaufgabe: übernehmen, erledigen.
        var markus = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("djoud"));
        await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().ThemenpateSetzenAsync("Haushalt und Finanzen", markus!.Id, null));
        var a = await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().AnlegenAsync("Haushaltsrede vorbereiten", null, null, null, "Test", Guid.Empty, "aufgaben",
            null, "Haushalt und Finanzen"));
        a.ZugewiesenPersonId.ShouldBe(markus!.Id);
        var korb = await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().AnlegenAsync("Presse informieren", null, null, Rolle.Vorstand, "Test", Guid.Empty, "aufgaben", null));
        var sicht = await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().UebersichtAsync());
        var zeile = sicht.Korb.Single(z => z.Aufgabe.Id == korb.Id);
        zeile.Uebernehmen.Erlaubt.ShouldBeTrue();
        await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().UebernehmenAsync(korb.Id));
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().UebersichtAsync())).Meine.ShouldContain(z => z.Aufgabe.Id == korb.Id);
        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        var andere = (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().UebersichtAsync())).BeiAnderen.Single(z => z.Aufgabe.Id == korb.Id);
        andere.Erledigen.Erlaubt.ShouldBeFalse();
        andere.Erledigen.Begruendung!.ShouldContain("übernehmen");
        (await u.InAsync(d => d.GetRequiredService<IDaten>().AnzahlAsync(d.GetRequiredService<IDaten>().Menge<Aufgabe>()))).ShouldBe(2);
    }
}
