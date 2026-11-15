using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Heute;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class VertretungAblaufTests
{
    /// <summary>Nächste HFA-Sitzung nach dem 15.11.2026 laut RIS-Mock (42-Tage-Rhythmus ab 06.01.2026): Dienstag, 08.12.2026, 17 Uhr.</summary>
    private static readonly DateTime NaechsterHfa = new(2026, 12, 8, 17, 0, 0);

    private static async Task<Termin> HfaAsync(TestUmgebung u)
        => (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TermineAsync(NaechsterHfa, NaechsterHfa.AddMinutes(1)))).Single();

    [Fact]
    public async Task Abwesenheit_findet_Sitzungen_fragt_in_Rangfolge_und_eskaliert_an_den_Vorstand()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var hfa = await HfaAsync(u);

        // Jens Overmeyer (ordentlich im HFA) meldet sich ab – Grund ist privat.
        await u.AnmeldenAsync("jovermeyer");
        var ergebnis = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().EintragenAsync(new DateOnly(2026, 12, 7), new DateOnly(2026, 12, 9), "krank"));
        ergebnis.Sitzungen.Select(s => s.Termin.Id).ShouldContain(hfa.Id);
        ergebnis.NaechsteSchritte[0].Hauptschritt.ShouldBeTrue();
        ergebnis.NaechsteSchritte[0].Titel.ShouldContain("Vertretung anfragen");

        var faelle = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().AnfragenAsync(ergebnis.Abwesenheit.Id));
        var fall = faelle.Single(f => f.TerminId == hfa.Id);

        // Rang 1 im HFA ist Anna Wewers: Push + Mail im Postausgang, Ein-Klick-Anfrage auf ihrer Startseite.
        var anna = await u.AnmeldenAsync("awewers");
        var heute = await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync());
        var anfrage = heute.Vertretungsanfragen.Single(a => a.FallId == fall.Id);
        var post = await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(50));
        post.Where(n => n.PersonId == anna.Id).Select(n => n.Kanal).ShouldBe([Kanal.Push, Kanal.EMail], ignoreOrder: true);
        post.ShouldAllBe(n => !n.Inhalt.Contains("krank", StringComparison.OrdinalIgnoreCase));

        // Anna kann nicht → Rang 2 (Petra Lammers) ist dran.
        (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().AntwortenAsync(anfrage.AnfrageId, false))).Status.ShouldBe(Vertretungsstatus.Angefragt);
        var petra = await u.AnmeldenAsync("plammers");
        (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().MeineAnfragenAsync())).Single().FallId.ShouldBe(fall.Id);

        // Petra antwortet nicht: nach Fristablauf geht es an Rang 3 (Dirk Schulze-Eling).
        u.Uhr.Advance(TimeSpan.FromHours(25));
        (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().FristenPruefenAsync())).ShouldBe(1);
        (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().MeineAnfragenAsync())).ShouldBeEmpty();
        await u.AnmeldenAsync("dschulze");
        var dirk = (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().MeineAnfragenAsync())).Single();

        // Dirk lehnt ab → alle gefragt → Aufgabe für den Vorstand.
        await u.InAsync(d => d.GetRequiredService<VertretungDienst>().AntwortenAsync(dirk.AnfrageId, false));
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var aufgabe = (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).Single();
        aufgabe.Titel.ShouldContain("HFA");
        aufgabe.Ziel.ShouldBe($"vertretungen/fall/{fall.Id}");
        var offen = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().OffeneVertretungenAsync());
        offen.Single(v => v.Termin.Id == hfa.Id).Fall!.Status.ShouldBe(Vertretungsstatus.Eskaliert);

        // Vorstand trägt nach Telefonat selbst ein → Aufgabe erledigt, Teilnahme „vertritt“.
        var melanie = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("mkock"));
        await u.InAsync(d => d.GetRequiredService<VertretungDienst>().ManuellBesetzenAsync(fall.Id, melanie!.Id));
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TeilnahmeAsync(hfa.Id, melanie!.Id)))!.Status.ShouldBe(Teilnahmestatus.Vertritt);

        // Protokoll: nur Pseudonyme, kein Klarname, kein Abwesenheitsgrund.
        var protokoll = await u.InAsync(d => d.GetRequiredService<IAuditProtokoll>().NeuesteAsync(500));
        protokoll.ShouldContain(e => e.Aktion == "Vertretung.Eskaliert");
        foreach (var e in protokoll)
        {
            foreach (var verboten in new[] { "Overmeyer", "Wewers", "Lammers", "Brinkmann", "krank", "jovermeyer" })
            {
                (e.Beschreibung + e.Benutzer + e.BenutzerKennung).ShouldNotContain(verboten);
            }
        }

        _ = petra;
    }

    [Fact]
    public async Task Zusage_traegt_ein_benachrichtigt_und_erscheint_im_Kalender_der_Vertretung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var hfa = await HfaAsync(u);
        var jens = await u.AnmeldenAsync("jovermeyer");
        var fall = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().FuerTerminAnfragenAsync(hfa.Id));

        var anna = await u.AnmeldenAsync("awewers");
        var anfrage = (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().MeineAnfragenAsync())).Single();
        var antwort = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().AntwortenAsync(anfrage.AnfrageId, true));

        antwort.Status.ShouldBe(Vertretungsstatus.Besetzt);
        antwort.NaechsteSchritte.ShouldContain(s => s.Ziel == $"termine/{hfa.Id}");
        var zeilen = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(new DateOnly(2026, 12, 8), new DateOnly(2026, 12, 8)));
        zeilen.Single(z => z.Termin.Id == hfa.Id).Beteiligung.ShouldBe(Beteiligung.Vertritt);
        var post = await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(50));
        post.ShouldContain(n => n.PersonId == jens.Id && n.Betreff.StartsWith("Vertretung steht", StringComparison.Ordinal));

        // Eine zweite Anfrage für dieselbe Sitzung ist gesperrt – mit Begründung.
        await u.AnmeldenAsync("jovermeyer");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<VertretungDienst>().FuerTerminAnfragenAsync(hfa.Id))))
            .Message.ShouldContain("läuft bereits");
        _ = (anna, fall);
    }

    [Fact]
    public async Task Abwesende_Stellvertretungen_werden_uebersprungen_und_Rueckzug_beendet_die_Anfrage()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var hfa = await HfaAsync(u);

        await u.AnmeldenAsync("awewers");
        await u.InAsync(d => d.GetRequiredService<VertretungDienst>().EintragenAsync(new DateOnly(2026, 12, 8), new DateOnly(2026, 12, 8), null));

        var jens = await u.AnmeldenAsync("jovermeyer");
        var abwesenheit = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().EintragenAsync(new DateOnly(2026, 12, 8), new DateOnly(2026, 12, 8), null));
        var fall = (await u.InAsync(d => d.GetRequiredService<VertretungDienst>().AnfragenAsync(abwesenheit.Abwesenheit.Id))).Single();

        var petra = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("plammers"));
        fall.OffeneAnfrage!.PersonId.ShouldBe(petra!.Id);

        await u.InAsync(d => d.GetRequiredService<VertretungDienst>().ZurueckziehenAsync(abwesenheit.Abwesenheit.Id));
        (await u.InAsync(d => d.GetRequiredService<IVertretungSpeicher>().FallAsync(fall.Id)))!.Status.ShouldBe(Vertretungsstatus.Entfallen);
        var post = await u.InAsync(d => d.GetRequiredService<INachrichtenSpeicher>().NeuesteAsync(50));
        post.ShouldContain(n => n.PersonId == petra.Id && n.Betreff == "Vertretung nicht mehr nötig");
        _ = (hfa, jens);
    }

    [Fact]
    public async Task Rangaenderung_gilt_ab_Datum_fuer_spaetere_Sitzungen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var hfa = await HfaAsync(u);
        await u.AnmeldenAsync("kalbers");
        var detail = await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().GremiumAsync(hfa.GremiumId!.Value));
        detail!.Stellvertretungen.Select(s => s.Name).ShouldBe(["Anna Wewers", "Petra Lammers", "Dirk Schulze-Eling"]);

        var dirk = detail.Stellvertretungen.Single(s => s.Name == "Dirk Schulze-Eling");
        await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().RangAendernAsync(dirk.MitgliedschaftId, 0 + 1, new DateOnly(2026, 12, 1), "Test"));
        var anna = detail.Stellvertretungen.Single(s => s.Name == "Anna Wewers");
        await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().RangAendernAsync(anna.MitgliedschaftId, 3, new DateOnly(2026, 12, 1), "Test"));

        var mitgliedschaften = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().MitgliedschaftenAsync());
        var personen = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync())).ToDictionary(p => p.Id, p => p.Nachname);
        Besetzung.StellvertretungenAm(mitgliedschaften, hfa.GremiumId!.Value, new DateOnly(2026, 11, 30), u.Uhr.GetUtcNow()).Select(id => personen[id])
            .ShouldBe(["Wewers", "Lammers", "Schulze-Eling"]);
        Besetzung.StellvertretungenAm(mitgliedschaften, hfa.GremiumId!.Value, new DateOnly(2026, 12, 8), u.Uhr.GetUtcNow()).Select(id => personen[id])
            .ShouldBe(["Schulze-Eling", "Lammers", "Wewers"]);
    }

    [Fact]
    public async Task Nur_die_Administration_pflegt_Rang_und_Rollen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        await u.AnmeldenAsync("awewers");
        var person = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("awewers"));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<StammdatenDienst>()
            .RollenAendernAsync(person!.Id, [Rolle.Vorstand], new DateOnly(2026, 12, 1), "Selbst befördert")))).Message.ShouldContain("Administration");
    }
}

public sealed class KalenderUndRisTests
{
    [Fact]
    public async Task Ris_Abgleich_spiegelt_Gremien_und_eigene_Mandate_idempotent()
    {
        await using var u = await TestUmgebung.ErzeugenAsync();
        var gremien = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync());
        gremien.Count.ShouldBe(7);
        gremien.ShouldContain(g => g.Name == "Ausschuss für Schule und Bildung" && g.Art == Gremienart.Ausschuss);

        // Andere Fraktion sitzt im Rat, wird aber nicht übernommen.
        var personen = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonenAsync());
        personen.ShouldNotContain(p => p.Nachname == "Musterhaus");
        var rat = await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().GremiumAsync(gremien.Single(g => g.Kurzname == "Rat").Id));
        rat!.Ordentliche.Count.ShouldBe(10);
        var asfs = await u.InAsync(d => d.GetRequiredService<StammdatenDienst>().GremiumAsync(gremien.Single(g => g.Kurzname == "ASFS").Id));
        asfs!.Stellvertretungen.Select(s => s.Name).ShouldBe(["Lena Hagedorn", "Ralf Niehues", "Jens Overmeyer"]);   // Reihenfolge laut RIS

        var zweiter = await u.InAsync(d => d.GetRequiredService<RisAbgleich>().AbgleichenAsync());
        (zweiter.GremienNeu + zweiter.GremienGeaendert + zweiter.MitgliedschaftenNeu + zweiter.MitgliedschaftenGeaendert + zweiter.SitzungenNeu + zweiter.SitzungenGeaendert)
            .ShouldBe(0);
    }

    [Fact]
    public async Task Kalender_Abo_liefert_nur_eigene_Termine_und_ist_widerrufbar()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("shoelscher");
        var link = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboAnlegenAsync());
        link.Webcal.Scheme.ShouldBe("webcal");
        var token = link.Https.Segments[^1].Replace(".ics", string.Empty, StringComparison.Ordinal);

        var ics = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboKalenderAsync(token)))?.Replace("\r\n ", string.Empty, StringComparison.Ordinal);
        ics.ShouldNotBeNull();
        ics.ShouldContain("BEGIN:VEVENT");
        ics.ShouldContain("Haupt- und Finanzausschuss");   // ordentliches Mitglied im HFA
        ics.ShouldNotContain("Rechnungsprüfungsausschuss"); // dort kein Sitz

        var abos = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().MeineAbosAsync());
        abos.Single().TokenHash.ShouldNotBe(token);
        await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboWiderrufenAsync(link.AboId));
        (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboKalenderAsync(token))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AboKalenderAsync("ausgedacht"))).ShouldBeNull();
    }

    [Fact]
    public async Task Helferinnen_sehen_nur_oeffentliche_Termine_Filter_nach_Gremium_und_Person()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var von = new DateOnly(2026, 11, 1);
        var bis = new DateOnly(2027, 2, 28);

        await u.AnmeldenAsync("fbaeumer");
        var helfer = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(von, bis));
        helfer.ShouldNotBeEmpty();
        helfer.ShouldAllBe(z => z.Termin.Art == Terminart.Oeffentlich || z.Termin.Art == Terminart.Wahlkampf);

        var anna = await u.AnmeldenAsync("awewers");
        var asb = (await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().GremienAsync())).Single(g => g.Kurzname == "ASB");
        var nurAsb = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(von, bis, new Kalenderfilter(GremiumId: asb.Id)));
        nurAsb.ShouldNotBeEmpty();
        nurAsb.ShouldAllBe(z => z.Termin.GremiumId == asb.Id);
        nurAsb.ShouldAllBe(z => z.Beteiligung == Beteiligung.Ordentlich);
        nurAsb[0].Termin.Beschreibung!.ShouldContain("Grundschul");

        var meine = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(von, bis, new Kalenderfilter(PersonId: anna.Id)));
        meine.ShouldNotContain(z => z.Termin.Art == Terminart.Ratssitzung && z.Beteiligung == Beteiligung.Keine);
        meine.ShouldContain(z => z.Termin.Art == Terminart.Fraktionssitzung);
    }

    [Fact]
    public async Task Zusage_und_Absage_mit_naechstem_Schritt_Vertretung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("awewers");
        var heute = await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync());
        heute.Vertretungsanfragen.ShouldHaveSingleItem();     // Demodaten: Anna ist für den HFA gefragt
        heute.OffeneZusagen.ShouldNotBeEmpty();               // Fraktionssitzungen ohne Antwort
        heute.Schnellaktionen.Single(s => s.Titel == "Raum buchen").Freigabe.Erlaubt.ShouldBeFalse();

        var asb = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(new DateOnly(2026, 11, 15), new DateOnly(2027, 1, 31))))
            .First(z => z.Termin.Art == Terminart.Ausschusssitzung && z.Beteiligung == Beteiligung.Ordentlich && !z.Abwesend);
        var ergebnis = await u.InAsync(d => d.GetRequiredService<KalenderDienst>().AntwortenAsync(asb.Termin.Id, false));
        ergebnis.NaechsteSchritte[0].Titel.ShouldBe("Vertretung anfragen");
        ergebnis.NaechsteSchritte[0].Ziel.ShouldBe($"vertretungen/termin/{asb.Termin.Id}");

        var vergangen = (await u.InAsync(d => d.GetRequiredService<KalenderDienst>().TermineAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 10)))).First();
        vergangen.Antworten.Erlaubt.ShouldBeFalse();
        vergangen.Antworten.Begruendung.ShouldBe("Der Termin liegt in der Vergangenheit.");
    }

    [Fact]
    public async Task Demodaten_haben_alle_Vertretungszustaende()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var offen = await u.InAsync(d => d.GetRequiredService<VertretungDienst>().OffeneVertretungenAsync());
        offen.ShouldContain(v => v.Fall == null);
        offen.ShouldContain(v => v.Fall != null && v.Fall.Status == Vertretungsstatus.Angefragt);
        offen.ShouldContain(v => v.Fall != null && v.Fall.Status == Vertretungsstatus.Eskaliert);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldHaveSingleItem();
    }
}
