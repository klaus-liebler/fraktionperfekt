using FraktionPerfekt.Adapters.Mocks;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Application.Termine;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using Microsoft.Extensions.Time.Testing;

namespace FraktionPerfekt.Application.Tests;

public sealed class RechteTests
{
    [Fact]
    public void Helferinnen_sehen_nur_oeffentliche_Termine_und_melden_keine_Abwesenheit_mit_Begruendung()
    {
        Rechte.SiehtTermin(Rolle.Helferin, Terminart.Kampagne).ShouldBeTrue();
        Rechte.SiehtTermin(Rolle.Helferin, Terminart.Fraktionssitzung).ShouldBeFalse();
        Rechte.SiehtTermin(Rolle.Mitglied, Terminart.Fraktionssitzung).ShouldBeTrue();
        var freigabe = Rechte.AbwesenheitMelden(Rolle.Helferin);
        freigabe.Erlaubt.ShouldBeFalse();
        freigabe.Begruendung.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Gesperrt_heisst_erklaert_die_Begruendung_nennt_Rollen()
    {
        var f = Rechte.StammdatenPflegen(Rolle.Mitglied);
        f.Erlaubt.ShouldBeFalse();
        f.Begruendung!.ShouldContain("„Administration“");
        f.Begruendung!.ShouldContain("Mitglied");
    }

    [Fact]
    public void Abwesenheitsgrund_nur_fuer_die_Person_selbst_und_die_Administration()
    {
        var person = Guid.NewGuid();
        Rechte.SiehtAbwesenheitsgrund(new Benutzer("a", "A", Rolle.Mitglied, PersonId: person), person).ShouldBeTrue();
        Rechte.SiehtAbwesenheitsgrund(new Benutzer("b", "B", Rolle.Vorstand, PersonId: Guid.NewGuid()), person).ShouldBeFalse();
        Rechte.SiehtAbwesenheitsgrund(new Benutzer("c", "C", Rolle.Administration, PersonId: Guid.NewGuid()), person).ShouldBeTrue();
    }

    [Fact]
    public void Rollenwechsel_nur_zu_eigenen_Rollen()
    {
        var person = Guid.NewGuid();
        var principal = MockAnmeldung.Anmelden(new Benutzer("tb", "T B", Rolle.Mitglied, [Rolle.Mitglied, Rolle.Vorstand], person), "Test");

        var neu = MockAnmeldung.RolleWechseln(principal, Rolle.Vorstand, "Test");
        MockAnmeldung.AlsBenutzer(neu)!.Rolle.ShouldBe(Rolle.Vorstand);
        MockAnmeldung.AlsBenutzer(neu)!.PersonId.ShouldBe(person);
        MockAnmeldung.RolleWechseln(principal, Rolle.Administration, "Test").ShouldBeNull();
    }
}

public sealed class KalenderdateiTests
{
    [Fact]
    public void Erzeugt_UTC_Zeiten_nach_Ortszeit_und_markiert_Absagen()
    {
        var winter = new Kalendereintrag(Guid.NewGuid(), "Haupt- und Finanzausschuss", new DateTime(2026, 12, 8, 17, 0, 0), new DateTime(2026, 12, 8, 19, 30, 0),
            "Rathaus Greven, Ratssaal", "TOP 1 Haushalt 2026; Defizit", false);
        var sommer = new Kalendereintrag(Guid.NewGuid(), "Rat", new DateTime(2026, 7, 2, 17, 0, 0), new DateTime(2026, 7, 2, 20, 0, 0), null, null, true);

        var ics = Kalenderdatei.Erzeugen("Test", [winter, sommer], new DateTimeOffset(2026, 11, 15, 8, 0, 0, TimeSpan.Zero));

        ics.ShouldStartWith("BEGIN:VCALENDAR\r\n");
        ics.ShouldContain("DTSTART:20261208T160000Z");
        ics.ShouldContain("DTSTART:20260702T150000Z");
        ics.ShouldContain("STATUS:CANCELLED");
        ics.ShouldContain("Haushalt 2026\\; Defizit");
        ics.Split("\r\n").ShouldAllBe(z => z.Length <= 75);
    }

    [Fact]
    public void Ortszeit_rechnet_unabhaengig_von_der_Serverzone()
    {
        var uhr = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 23, 30, 0, TimeSpan.Zero));
        Ortszeit.Heute(uhr).ShouldBe(new DateOnly(2026, 11, 16));
        Ortszeit.Zeitpunkt(new DateTime(2026, 11, 16, 0, 30, 0)).ShouldBe(uhr.GetUtcNow());
    }
}

public sealed class PostausgangTests
{
    private sealed class FesterBenutzer : IAktuellerBenutzer
    {
        public ValueTask<Benutzer> ErmittelnAsync(CancellationToken abbruch = default)
            => ValueTask.FromResult(new Benutzer("tb", "Johannes Wilp", Rolle.Vorstand, PersonId: Guid.NewGuid()));
    }

    private sealed class SpeicherProtokoll : IAuditProtokoll
    {
        public List<AuditEintrag> Eintraege { get; } = [];

        public Task ProtokollierenAsync(string aktion, string beschreibung, string? objektTyp = null, Guid? objektId = null, bool erfolgreich = true,
            CancellationToken abbruch = default)
        {
            Eintraege.Add(new AuditEintrag(DateTimeOffset.UnixEpoch, "x", "x", null, aktion, objektTyp, objektId, beschreibung, erfolgreich));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEintrag>> NeuesteAsync(int anzahl, CancellationToken abbruch = default)
            => Task.FromResult<IReadOnlyList<AuditEintrag>>(Eintraege);
    }

    private sealed class SpeicherPostausgang : INachrichtenSpeicher
    {
        public List<AusgehendeNachricht> Nachrichten { get; } = [];

        public void Hinzufuegen(AusgehendeNachricht nachricht) => Nachrichten.Add(nachricht);

        public Task<AusgehendeNachricht?> LadenAsync(Guid id, CancellationToken abbruch = default) => Task.FromResult(Nachrichten.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<AusgehendeNachricht>> WartendeAsync(int maximal, CancellationToken abbruch = default)
            => Task.FromResult<IReadOnlyList<AusgehendeNachricht>>(Nachrichten.Where(n => n.Status == NachrichtenStatus.Wartend).Take(maximal).ToList());

        public Task<IReadOnlyList<AusgehendeNachricht>> NeuesteAsync(int anzahl, CancellationToken abbruch = default)
            => Task.FromResult<IReadOnlyList<AusgehendeNachricht>>(Nachrichten);

        public Task<int> AnzahlAsync(NachrichtenStatus status, CancellationToken abbruch = default) => Task.FromResult(Nachrichten.Count(n => n.Status == status));

        public Task SpeichernAsync(CancellationToken abbruch = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task Benachrichtigung_geht_als_Push_und_Mail_in_den_Postausgang_Protokoll_ohne_Klarnamen()
    {
        var uhr = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 8, 0, 0, TimeSpan.Zero));
        var speicher = new SpeicherPostausgang();
        var protokoll = new SpeicherProtokoll();
        var dienst = new PostausgangDienst(speicher, protokoll, new FesterBenutzer(), uhr);
        var anna = new Person("Anna", "Wewers", "awewers@demo.invalid", "awewers", uhr.GetUtcNow());

        await dienst.BenachrichtigenAsync(anna, "Vertretung gesucht", "Kannst du vertreten?", "Langtext", "handy/anfrage/1", "Ich vertrete", TestContext.Current.CancellationToken);

        speicher.Nachrichten.Select(n => n.Kanal).ShouldBe([Kanal.Push, Kanal.EMail]);
        speicher.Nachrichten[0].Aktion.ShouldBe("Ich vertrete");
        protokoll.Eintraege.ShouldAllBe(e => !e.Beschreibung.Contains("Wewers", StringComparison.Ordinal));

        var zustellung = new Zustellung(speicher, [new MockMailKanal(), new MockPushKanal()], protokoll, uhr, new Demobetrieb(false));
        var ergebnis = await zustellung.WartendeZustellenAsync(abbruch: TestContext.Current.CancellationToken);
        ergebnis.Zugestellt.ShouldBe(2);
        speicher.Nachrichten.ShouldAllBe(n => n.Status == NachrichtenStatus.Zugestellt);
    }

    [Fact]
    public async Task Ohne_Kanal_wird_nach_fuenf_Versuchen_aufgegeben()
    {
        var uhr = new FakeTimeProvider();
        var speicher = new SpeicherPostausgang();
        var protokoll = new SpeicherProtokoll();
        var anna = new Person("Anna", "Wewers", null, "awewers", uhr.GetUtcNow());
        await new PostausgangDienst(speicher, protokoll, new FesterBenutzer(), uhr).PushEinreihenAsync(anna, "Titel", "Text", null, abbruch: TestContext.Current.CancellationToken);
        var zustellung = new Zustellung(speicher, [], protokoll, uhr, new Demobetrieb(false));

        for (var i = 0; i < AusgehendeNachricht.MaxVersuche; i++)
        {
            await zustellung.WartendeZustellenAsync(abbruch: TestContext.Current.CancellationToken);
        }

        speicher.Nachrichten.Single().Status.ShouldBe(NachrichtenStatus.Fehlgeschlagen);
    }

    /// <summary>Echter Mailkanal im Demobetrieb: Mail wird nicht versendet (Demo-Schutz), Push-Mocks laufen weiter.</summary>
    [Fact]
    public async Task Im_Demobetrieb_gehen_keine_echten_Mails_raus()
    {
        var uhr = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 8, 0, 0, TimeSpan.Zero));
        var speicher = new SpeicherPostausgang();
        var protokoll = new SpeicherProtokoll();
        var person = new Person("Johannes", "Wilp", "johannes.wilp@cdu-greven.de", "jwilp", uhr.GetUtcNow());
        await new PostausgangDienst(speicher, protokoll, new FesterBenutzer(), uhr).BenachrichtigenAsync(person, "Titel", "Kurz", "Lang", null, null,
            TestContext.Current.CancellationToken);
        var echt = new EchterMailKanal();
        var zustellung = new Zustellung(speicher, [echt, new MockPushKanal()], protokoll, uhr, new Demobetrieb(true));

        var ergebnis = await zustellung.WartendeZustellenAsync(abbruch: TestContext.Current.CancellationToken);

        echt.Versendet.ShouldBe(0);
        ergebnis.Zugestellt.ShouldBe(1); // nur der Push-Mock
        speicher.Nachrichten.Single(n => n.Kanal == Kanal.EMail).Status.ShouldNotBe(NachrichtenStatus.Zugestellt);
        protokoll.Eintraege.ShouldContain(e => e.Beschreibung.Contains("Demo-Schutz", StringComparison.Ordinal));
    }

    private sealed class EchterMailKanal : IZustellKanal
    {
        public int Versendet { get; private set; }

        public Kanal Kanal => Kanal.EMail;

        public Task<string?> ZustellenAsync(AusgehendeNachricht nachricht, CancellationToken abbruch = default)
        {
            Versendet++;
            return Task.FromResult<string?>("versendet");
        }
    }
}
