using FraktionPerfekt.Application.Abstimmungen;
using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Application.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Was das Menü für Helfer:innen sperrt, liefern auch die Dienste nicht (keine Lücke über die direkte Adresse).</summary>
public sealed class HelferinRechteTests
{
    [Fact]
    public async Task Helferinnen_bekommen_gesperrte_Module_auch_ueber_die_Dienste_nicht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);

        // Ids als Mitglied ermitteln
        await u.AnmeldenAsync("lkuehlert");
        var antrag = (await u.InAsync(d => d.GetRequiredService<AntragDienst>().ListeAsync()))[0].Antrag.Id;
        var sitzung = (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().SitzungenAsync()))[0].Sitzung.Id;
        var umfrage = (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().ListeAsync()))[0].Umfrage.Id;

        await u.AnmeldenAsync("fbaeumer");
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<AntragDienst>().DetailAsync(antrag))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().SitzungenAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<SitzungDienst>().DetailAsync(sitzung))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<AnfrageDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<AnliegenDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<PresseDienst>().MitteilungenAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<KontaktDienst>().ListeAsync(null))).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<BeschlussArchiv>().SuchenAsync(null, null, null, null))).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().ListeAsync())).ShouldBeEmpty();
        (await u.InAsync(d => d.GetRequiredService<UmfrageDienst>().DetailAsync(umfrage))).ShouldBeNull();
        (await u.InAsync(d => d.GetRequiredService<BesetzungDienst>().ListeAsync())).ShouldBeEmpty();
    }
}

/// <summary>Du-Ansprache immer mit dem Vornamen-Feld, nie mit dem ersten Wort des Anzeigenamens (Titel).</summary>
public sealed class AnspracheTests
{
    [Fact]
    public async Task Startseite_begruesst_Personen_mit_Titel_beim_Vornamen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        foreach (var (kennung, vorname, anzeige) in new[] { ("kliebler", "Klaus", "Dr. Klaus Liebler"), ("tschubert", "Tim", "Dr. Tim Schubert"), ("djoud", "Dames", "Dames Joud") })
        {
            var person = await u.AnmeldenAsync(kennung);
            person.Anzeigename.ShouldBe(anzeige);
            var heute = await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Heute.HeuteDienst>().LadenAsync());
            heute.Vorname.ShouldBe(vorname);
        }
    }

    [Fact]
    public async Task Eigene_Einstellung_Sie_foermlich_wirkt_auf_Startseite_und_Mails()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        var ich = await u.AnmeldenAsync("djoud");
        await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>()
            .AnspracheSpeichernAsync("Herr", FraktionPerfekt.Domain.Personen.Anredeform.Sie, FraktionPerfekt.Domain.Personen.Begruessungsstil.Foermlich, true));
        (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Heute.HeuteDienst>().LadenAsync())).Begruessung.ShouldBe("Sehr geehrter Herr Joud");

        // Eine neue Vertretungsanfrage an ihn ist in der Sie-Form
        await u.AnmeldenAsync("thorstmann");
        var hfwa = (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.IStammdatenSpeicher>().GremienAsync())).Single(g => g.Kurzname == "HFWA");
        var termin = (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Termine.KalenderDienst>().TermineAsync(new DateOnly(2027, 1, 1), new DateOnly(2027, 3, 31))))
            .First(z => z.Termin.GremiumId == hfwa.Id).Termin;
        await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Vertretung.VertretungDienst>().FuerTerminAnfragenAsync(termin.Id));
        var post = await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Postausgang.INachrichtenSpeicher>().NeuesteAsync(20));
        var mail = post.First(n => n.PersonId == ich.Id && n.Kanal == FraktionPerfekt.Application.Postausgang.Kanal.EMail);
        mail.Inhalt.ShouldStartWith("Sehr geehrter Herr Joud,");
        mail.Inhalt.ShouldContain("Sie sind als Stellvertretung an der Reihe");
        post.First(n => n.PersonId == ich.Id && n.Kanal == FraktionPerfekt.Application.Postausgang.Kanal.Push).Inhalt.ShouldStartWith("Können Sie");
    }
}

public sealed class KalenderKlartextTests
{
    [Fact]
    public void Beschreibung_im_Kalender_Abo_ist_Klartext_ohne_Markdown()
    {
        var text = FraktionPerfekt.Application.Termine.Kalenderdatei.Klartext("## Ablauf\n\n- **Begrüßung** durch [Johannes](https://example.org)\n- ![Lageplan](dateien/bild/0196a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b)");
        text.ShouldContain("Ablauf");
        text.ShouldContain("Begrüßung durch Johannes");
        text.ShouldNotContain("**");
        text.ShouldNotContain("##");
        text.ShouldNotContain("dateien/bild");
    }
}


/// <summary>Personendaten zeitlich: Änderung über den Dienst, Name im alten Antrag zum damaligen Stand.</summary>
public sealed class PersonZeitlichAblaufTests
{
    [Fact]
    public async Task Namensaenderung_ab_heute_alter_Antrag_zeigt_den_damaligen_Namen()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        await u.AnmeldenAsync("lkuehlert");
        var antrag = (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Antraege.AntragDienst>().ListeAsync()))
            .First(z => z.Autor == "Liz Kühlert").Antrag;
        var liz = (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.IStammdatenSpeicher>().PersonNachKennungAsync("lkuehlert")))!;

        await u.AnmeldenAsync("kalbers", FraktionPerfekt.Domain.Personen.Rolle.Administration);
        var heute = FraktionPerfekt.Application.Gemeinsam.Ortszeit.Heute(u.Uhr);
        await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>()
            .NameAendernAsync(liz.Id, "Liz", "Kühlert-Sommer", null, heute, false, "Heirat"));

        // Personenseite: heute neuer Name, gestern der alte; der alte Antrag zeigt den Namen zum Tag der Antragstellung
        var detail = (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>().PersonAsync(liz.Id)))!;
        detail.Heute!.Anzeige.ShouldBe("Liz Kühlert-Sommer");
        (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>().StandAmAsync(liz.Id, heute.AddDays(-1))))!
            .Anzeige.ShouldBe("Liz Kühlert");
        (await u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Antraege.AntragDienst>().DetailAsync(antrag.Id)))!.Autor.ShouldBe("Liz Kühlert");

        // Nur die Geschäftsführung pflegt Personendaten
        await u.AnmeldenAsync("thorstmann");
        await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => d.GetRequiredService<FraktionPerfekt.Application.Stammdaten.StammdatenDienst>()
            .NameAendernAsync(liz.Id, "X", "Y", null, heute, false, "Test")));
    }
}
