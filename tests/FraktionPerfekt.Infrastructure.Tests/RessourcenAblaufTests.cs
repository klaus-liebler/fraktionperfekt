using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Aufgaben;
using FraktionPerfekt.Application.Heute;
using FraktionPerfekt.Application.Ressourcen;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Ressourcen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Infrastructure.Tests;

public sealed class RessourcenAblaufTests
{
    [Fact]
    public async Task Raum_buchen_mit_Konflikt_und_Freigabe_durch_die_Geschaeftsfuehrung()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        RessourcenDienst R(IServiceProvider d) => d.GetRequiredService<RessourcenDienst>();
        var ressourcen = await u.InAsync(d => R(d).RessourcenAsync());
        var buero = ressourcen.Single(r => r.Name.StartsWith("Fraktionsbüro", StringComparison.Ordinal));
        var saal = ressourcen.Single(r => r.FreigabeNoetig && r.Art == Ressourcenart.Raum);

        // „Raum buchen“ aus der Fraktionssitzung: Vorschlag mit Zeit, Konflikt mit der Demo-Buchung.
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var sitzung = (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TermineAsync(new DateTime(2026, 11, 15), new DateTime(2027, 1, 15))))
            .First(t => t.Art == Terminart.Fraktionssitzung);
        var vorschlag = (await u.InAsync(d => R(d).VorschlagAusTerminAsync(sitzung.Id)))!;
        vorschlag.Von.ShouldBe(sitzung.Beginn.AddMinutes(-30));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).BuchenAsync(buero.Id, vorschlag.Von, vorschlag.Bis, "Doppelt", sitzung.Id))))
            .Message.ShouldContain("bereits gebucht");

        // Raum mit Freigabe: Anfrage blockiert, Aufgabe im Korb Administration, Freigabe benachrichtigt.
        var e = await u.InAsync(d => R(d).BuchenAsync(saal.Id, new DateTime(2026, 12, 2, 18, 0, 0), new DateTime(2026, 12, 2, 21, 0, 0), "Klausur Haushalt 2026", null));
        e.Buchung.Status.ShouldBe(Buchungsstatus.Angefragt);
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).BuchenAsync(saal.Id, new DateTime(2026, 12, 2, 20, 0, 0),
            new DateTime(2026, 12, 2, 22, 0, 0), "überlappt", null)))).Message.ShouldContain("angefragt");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).EntscheidenAsync(e.Buchung.Id, true, null)))).Message.ShouldContain("Administration");

        await u.AnmeldenAsync("kalbers", Rolle.Administration);
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldContain(a => a.AnlassId == e.Buchung.Id);
        (await u.InAsync(d => R(d).OffeneFreigabenAsync())).ShouldContain(z => z.Buchung.Id == e.Buchung.Id);
        await u.InAsync(d => R(d).EntscheidenAsync(e.Buchung.Id, true, "ok"));
        (await u.InAsync(d => d.GetRequiredService<AufgabenDienst>().KorbAsync())).ShouldNotContain(a => a.AnlassId == e.Buchung.Id);
        var belegung = await u.InAsync(d => R(d).BelegungAsync(new DateTime(2026, 12, 2), new DateTime(2026, 12, 3), saal.Id));
        belegung.Single().Buchung.Status.ShouldBe(Buchungsstatus.Bestaetigt);

        // Stornieren nur durch Buchende oder Geschäftsführung
        await u.AnmeldenAsync("awewers");
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => R(d).StornierenAsync(e.Buchung.Id)))).Message.ShouldContain("gebucht hat");
    }

    [Fact]
    public async Task Terminbesetzung_Vorstand_bestaetigt_und_wer_zuerst_kommt_mit_Warteliste_und_Bericht()
    {
        await using var u = await TestUmgebung.ErzeugenAsync(mitDemodaten: true);
        BesetzungDienst B(IServiceProvider d) => d.GetRequiredService<BesetzungDienst>();
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var termin = (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TermineAsync(new DateTime(2026, 11, 16), new DateTime(2027, 2, 1))))
            .First(t => t.Art == Terminart.Oeffentlich && t.Titel.StartsWith("Bürgerinformation", StringComparison.Ordinal));
        var anfrage = await u.InAsync(d => B(d).AnlegenAsync(termin.Id, 1, u.Uhr.GetUtcNow().AddDays(1), Besetzungsmodus.WerZuerstKommt, null));
        (await Should.ThrowAsync<InvalidOperationException>(() => u.InAsync(d => B(d).AnlegenAsync(termin.Id, 1, u.Uhr.GetUtcNow().AddDays(1), Besetzungsmodus.WerZuerstKommt, null))))
            .Message.ShouldContain("bereits");

        await u.AnmeldenAsync("awewers");
        (await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync())).OffeneBesetzungen.ShouldContain(z => z.Anfrage.Id == anfrage.Id);
        (await u.InAsync(d => B(d).InteresseMeldenAsync(anfrage.Id))).ShouldContain("dabei");
        var anna = await u.InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync("awewers"));
        (await u.InAsync(d => d.GetRequiredService<ITerminSpeicher>().TeilnahmeAsync(termin.Id, anna!.Id)))!.Status.ShouldBe(Teilnahmestatus.Zugesagt);

        await u.AnmeldenAsync("mkock");
        (await u.InAsync(d => B(d).InteresseMeldenAsync(anfrage.Id))).ShouldContain("Warteliste");

        // Anna zieht zurück → Melanie rückt nach und bekommt eine Push-Mitteilung.
        await u.AnmeldenAsync("awewers");
        await u.InAsync(d => B(d).ZurueckziehenAsync(anfrage.Id));
        var detail = (await u.InAsync(d => B(d).DetailAsync(anfrage.Id)))!;
        detail.Meldungen.Single(m => m.Person == "Melanie Kock").Meldung.Status.ShouldBe(Meldungsstatus.Bestaetigt);
        detail.Meldungen.Single(m => m.Person == "Anna Wewers").Meldung.Status.ShouldBe(Meldungsstatus.Zurueckgezogen);
        detail.BerichtSchreiben.Erlaubt.ShouldBeFalse();

        // Vorstandsbestätigung (Demo-Infoabend): bestätigen bis die Plätze voll sind.
        await u.AnmeldenAsync("tbrinkmann", Rolle.Vorstand);
        var infoabend = (await u.InAsync(d => B(d).ListeAsync())).Single(z => z.Termin.Titel.StartsWith("Infoabend", StringComparison.Ordinal));
        var d1 = (await u.InAsync(d => B(d).DetailAsync(infoabend.Anfrage.Id)))!;
        foreach (var m in d1.Meldungen)
        {
            await u.InAsync(d => B(d).BestaetigenAsync(m.Meldung.Id, true));
        }

        (await u.InAsync(d => B(d).DetailAsync(infoabend.Anfrage.Id)))!.Frei.ShouldBe(0);

        // Kurzbericht: der Demo-Termin von vor drei Tagen erscheint bei Anna als offener Folgeschritt.
        await u.AnmeldenAsync("awewers");
        var offen = (await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync())).BerichtOffen.Single();
        await u.InAsync(d => B(d).BerichtAsync(offen.Anfrage.Id, "Gut besucht; viele Fragen zu Schulwegen."));
        (await u.InAsync(d => d.GetRequiredService<HeuteDienst>().LadenAsync())).BerichtOffen.ShouldBeEmpty();
    }
}
