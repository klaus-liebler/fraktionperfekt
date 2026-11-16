using System.Net;
using System.Text;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Politik;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Infrastructure.Ris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SBenutzer = FraktionPerfekt.Application.Sicherheit.Benutzer;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class PolitikAblaufTests
{
    [Fact]
    public async Task Anfrage_mit_Frist_Erinnerung_nachhaken_und_Antwort()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        AnfrageDienst A(IServiceProvider d) => d.GetRequiredService<AnfrageDienst>();
        await u.AnmeldenAsync("lkuehlert");
        var anfrage = await u.InAsync(d => A(d).AnlegenAsync("Sanierungsstau an Grundschulen", "Welche Maßnahmen sind bis 2028 geplant?", null, "Schule und Bildung"));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => A(d).EinreichenAsync(anfrage.Id, null)))).Message.ShouldContain("Vorstand");

        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        await u.InAsync(d => A(d).EinreichenAsync(anfrage.Id, null));
        var z = (await u.InAsync(d => A(d).ListeAsync())).Single(x => x.Anfrage.Id == anfrage.Id);
        z.Anfrage.FristBis.ShouldBe(new DateOnly(2026, 11, 15).AddDays(AnfrageDienst.StandardFristTage));

        u.Uhr.Advance(TimeSpan.FromDays(30));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => A(d).FristenPruefenAsync())).ShouldBeGreaterThanOrEqualTo(1);
        (await u.InAsync(d => A(d).FristenPruefenAsync())).ShouldBe(0); // nur einmal erinnern
        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldContain(a => a.AnlassId == anfrage.Id && a.Titel.StartsWith("Bei der Verwaltung nachhaken", StringComparison.Ordinal));
        (await u.InAsync(d => A(d).ListeAsync())).Single(x => x.Anfrage.Id == anfrage.Id).Ueberfaellig.ShouldBeTrue();

        await u.AnmeldenAsync("lkuehlert");
        await u.InAsync(d => A(d).BeantwortenAsync(anfrage.Id, "Drei Schulen werden bis 2028 saniert (fiktiv)."));
        (await u.InAsync(d => A(d).ListeAsync())).Single(x => x.Anfrage.Id == anfrage.Id).Anfrage.Status.ShouldBe(Anfragestatus.Beantwortet);
        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldNotContain(a => a.AnlassId == anfrage.Id);
    }

    [Fact]
    public async Task Buergeranliegen_Eingang_Zuweisung_an_Themenpaten_Antwort_und_Loeschfrist()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        AnliegenDienst A(IServiceProvider d) => d.GetRequiredService<AnliegenDienst>();
        AufgabenDienst T(IServiceProvider d) => d.GetRequiredService<AufgabenDienst>();
        var wewers = await u.AnmeldenAsync("thorstmann");
        var anliegen = await u.InAsync(d => A(d).ErfassenAsync("Toiletten an der Grundschule", "Die Toiletten sind seit Monaten defekt.", "Frau Muster (fiktiv)",
            "muster@mail.example", Eingangskanal.Mail, "Schule und Bildung"));

        var zeile = (await u.InAsync(d => A(d).ListeAsync())).Single(z => z.Anliegen.Id == anliegen.Id);
        zeile.Zuweisen.Erlaubt.ShouldBeFalse();
        zeile.Vorschlag.ShouldBe("Liz Kühlert"); // Themenpatin Schule und Bildung

        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        (await u.InAsync(d => T(d).KorbAsync())).ShouldContain(a => a.AnlassId == anliegen.Id);
        await u.InAsync(d => A(d).ZuweisenAsync(anliegen.Id, zeile.VorschlagPersonId!.Value));
        (await u.InAsync(d => T(d).KorbAsync())).ShouldNotContain(a => a.AnlassId == anliegen.Id);

        await u.AnmeldenAsync("thorstmann");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => A(d).ErledigenAsync(anliegen.Id, "x")))).Message.ShouldContain("zuständige");
        await u.AnmeldenAsync("lkuehlert");
        (await u.InAsync(d => T(d).UebersichtAsync())).Meine.ShouldContain(a => a.Aufgabe.AnlassId == anliegen.Id);
        await u.InAsync(d => A(d).ErledigenAsync(anliegen.Id, "Mit der Schulverwaltung gesprochen; Reparatur beauftragt."));
        (await u.InAsync(d => T(d).UebersichtAsync())).Meine.ShouldNotContain(a => a.Aufgabe.AnlassId == anliegen.Id);

        u.Uhr.Advance(TimeSpan.FromDays(400));
        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => A(d).LoeschfristenAsync())).ShouldBeGreaterThanOrEqualTo(1);
        await u.AnmeldenAsync("lkuehlert");
        var danach = (await u.InAsync(d => A(d).ListeAsync())).Single(z => z.Anliegen.Id == anliegen.Id).Anliegen;
        danach.Name.ShouldBe("(gelöscht)");
        danach.Kontaktweg.ShouldBeNull();
        danach.Antwort.ShouldNotBeNull();
        _ = wewers;
    }

    [Fact]
    public async Task Pressemitteilung_vier_Augen_Freigabe_Aenderung_setzt_zurueck_Pressespiegel()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        PresseDienst P(IServiceProvider d) => d.GetRequiredService<PresseDienst>();
        await u.AnmeldenAsync("jwilp", Rolle.Vorstand);
        var m = await u.InAsync(d => P(d).SpeichernAsync(null, "Ehrenamt stärken", "Text", new DateOnly(2026, 12, 1), "Ehrenamt"));
        await u.InAsync(d => P(d).ZurFreigabeAsync(m.Id));
        (await u.InAsync(d => P(d).MitteilungenAsync())).Single(z => z.Mitteilung.Id == m.Id).Freigeben.Begruendung!.ShouldContain("Vier-Augen");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => P(d).EntscheidenAsync(m.Id, true)));

        await u.AnmeldenAsync("lkuehlert");
        (await u.InAsync(d => P(d).MitteilungenAsync())).Single(z => z.Mitteilung.Id == m.Id).Freigeben.Begruendung!.ShouldContain("Vorstand");

        await u.AnmeldenAsync("ebrinkhaus", Rolle.Vorstand);
        await u.InAsync(d => P(d).EntscheidenAsync(m.Id, true));
        (await u.InAsync(d => P(d).MitteilungenAsync())).Single(z => z.Mitteilung.Id == m.Id).Mitteilung.Status.ShouldBe(Pressestatus.Freigegeben);

        // Textänderung nach der Freigabe: zurück auf Entwurf
        await u.InAsync(d => P(d).SpeichernAsync(m.Id, "Ehrenamt stärken", "Neuer Text", new DateOnly(2026, 12, 1), null));
        (await u.InAsync(d => P(d).MitteilungenAsync())).Single(z => z.Mitteilung.Id == m.Id).Mitteilung.Status.ShouldBe(Pressestatus.Entwurf);

        await u.InAsync(d => P(d).ArtikelErfassenAsync("Lokalzeitung (fiktiv)", "Fraktion will Ehrenamt stärken", new DateOnly(2026, 12, 2), "https://zeitung.example/a",
            null, null, Tonlage.Positiv, m.Id));
        (await u.InAsync(d => P(d).MitteilungenAsync())).Single(z => z.Mitteilung.Id == m.Id).Artikel.ShouldBe(1);
        await Should.ThrowAsync<ArgumentException>(() => u.InAsync(d => P(d).ArtikelErfassenAsync("X", "Y", new DateOnly(2026, 12, 2), "javascript:alert(1)", null, null,
            Tonlage.Neutral, null)));

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => P(d).MitteilungenAsync())).ShouldBeEmpty();
    }

    [Fact]
    public async Task Beschlusslage_aus_Protokollen_und_RIS_durchsuchbar_Abgleich_idempotent()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("lkuehlert");
        BeschlussArchiv B(IServiceProvider d) => d.GetRequiredService<BeschlussArchiv>();
        var alle = await u.InAsync(d => B(d).SuchenAsync(null, null, null, null));
        alle.ShouldContain(z => z.Beschluss.Quelle == Beschlussquelle.Fraktion);
        alle.ShouldContain(z => z.Beschluss.Quelle == Beschlussquelle.Ausschuss && z.Beschluss.RisId != null);
        (await u.InAsync(d => B(d).SuchenAsync("Grundschul", null, null, null))).Select(z => z.Beschluss.Quelle).Distinct().Count().ShouldBeGreaterThan(1);

        u.Benutzer.Aktuell = SBenutzer.Automatik;
        (await u.InAsync(d => d.GetRequiredService<RisAbgleich>().AbgleichenAsync())).BeschluesseNeu.ShouldBe(0);

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => B(d).SuchenAsync(null, null, null, null))).ShouldBeEmpty();
    }

    [Fact]
    public async Task OParl_Adapter_liest_System_Body_Listen_mit_Seiten_und_Beschluesse()
    {
        var antworten = new Dictionary<string, string>
        {
            ["https://ris.example/oparl/v1"] = """{"id":"https://ris.example/oparl/v1","type":"https://schema.oparl.org/1.1/System","body":"https://ris.example/oparl/v1/bodies"}""",
            ["https://ris.example/oparl/v1/bodies"] = """{"data":[{"id":"https://ris.example/b/1","type":"https://schema.oparl.org/1.1/Body","organization":"https://ris.example/b/1/org","person":"https://ris.example/b/1/person","meeting":"https://ris.example/b/1/meeting"}],"links":{}}""",
            ["https://ris.example/b/1/org"] = """{"data":[{"id":"o1","name":"Rat der Stadt","shortName":"Rat","organizationType":"Gremium","classification":"Rat"},{"id":"o2","name":"Haupt- und Finanzausschuss","shortName":"HFA","organizationType":"Gremium","classification":"Ausschuss"}],"links":{"next":"https://ris.example/b/1/org?page=2"}}""",
            ["https://ris.example/b/1/org?page=2"] = """{"data":[{"id":"f1","name":"CDU-Fraktion","organizationType":"Fraktion"}],"links":{}}""",
            ["https://ris.example/b/1/person"] = """{"data":[{"id":"p1","givenName":"Anna","familyName":"Wewers","membership":[{"id":"m1","organization":"o2","role":"Stellvertretendes Mitglied","startDate":"2025-11-01"},{"id":"m2","organization":"f1","role":"Mitglied","startDate":"2025-11-01"}]}],"links":{}}""",
        };
        var handler = new FakeHandler(url => url.StartsWith("https://ris.example/b/1/meeting", StringComparison.Ordinal)
            ? """{"data":[{"id":"s1","name":"5. Sitzung HFA","start":"2026-11-10T17:00:00+01:00","end":"2026-11-10T19:00:00+01:00","organization":["o2"],"location":{"description":"Ratssaal"},"agendaItem":[{"id":"t1","number":"1","name":"Haushalt 2026","result":"mehrheitlich beschlossen","resolutionText":"Der Ausschuss empfiehlt den Haushalt."},{"id":"t2","number":"2","name":"Personalsache","public":false,"result":"beschlossen"}]}],"links":{}}"""
            : antworten[url]);
        var ris = new OParlRis(new HttpClient(handler), new OParlOptionen { Endpunkt = new Uri("https://ris.example/oparl/v1") }, NullLogger<OParlRis>.Instance);

        var gremien = await ris.GremienAsync(TestContext.Current.CancellationToken);
        gremien.Select(g => (g.Kurzname, g.Art)).ShouldBe([("Rat", "Rat"), ("HFA", "Ausschuss")]);
        (await ris.PersonenAsync(TestContext.Current.CancellationToken)).Single().Fraktion.ShouldBe("CDU-Fraktion");
        var m = (await ris.MitgliedschaftenAsync(TestContext.Current.CancellationToken)).Single(x => x.GremiumRisId == "o2");
        m.Stellvertretung.ShouldBeTrue();
        var s = (await ris.SitzungenAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), TestContext.Current.CancellationToken)).Single();
        (s.Beginn, s.Ort).ShouldBe((new DateTime(2026, 11, 10, 17, 0, 0), "Ratssaal"));
        s.Tagesordnung.ShouldBe("TOP 1 Haushalt 2026"); // nichtöffentlicher TOP fehlt
        var b = (await ris.BeschluesseAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), TestContext.Current.CancellationToken)).Single();
        (b.GremiumRisId, b.Titel, b.Ergebnis).ShouldBe(("o2", "Haushalt 2026", "mehrheitlich beschlossen"));
        RisAbgleich.Ergebnis(b.Ergebnis).ShouldBe(Beschlussergebnis.Angenommen);
        RisAbgleich.Ergebnis("mehrheitlich abgelehnt").ShouldBe(Beschlussergebnis.Abgelehnt);
    }

    private sealed class FakeHandler(Func<string, string> antwort) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(antwort(request.RequestUri!.ToString()), Encoding.UTF8, "application/json") });
    }
}
