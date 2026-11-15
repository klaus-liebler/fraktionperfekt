using Bogus;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application.Stammdaten;
using FraktionPerfekt.Application.Vertretung;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Was die Demodaten angelegt haben (für Anzeige und Tests).</summary>
public sealed record Demodatensatz(int Personen, int Gremien, int Termine, RisAbgleichErgebnis Ris, int Vertretungsfaelle);

/// <summary>
/// Legt die fiktive Demofraktion an (idempotent: nur in eine Datenbank ohne Personen). Ablauf: Personen mit Rollen und
/// Testkonten → RIS-Abgleich (Gremien, Mandate, Sitzungen aus dem Mock) → fraktionsinterne Gremien und Termine →
/// Abwesenheiten in verschiedenen Vertretungszuständen. Zufallswerte mit festem Startwert (Bogus).
/// </summary>
public sealed class Demodaten(
    IStammdatenSpeicher stammdaten,
    ITerminSpeicher termine,
    IVertretungSpeicher vertretung,
    RisAbgleich risAbgleich,
    PostausgangDienst postausgang,
    IAuditProtokoll protokoll,
    TimeProvider uhr)
{
    public const int Startwert = 20261115;

    private const string Von = "Demodaten";

    public async Task<Demodatensatz?> AnlegenAsync(CancellationToken abbruch = default)
    {
        if ((await stammdaten.PersonenAsync(abbruch)).Count > 0)
        {
            return null;
        }

        var jetzt = uhr.GetUtcNow();
        var heute = Ortszeit.Heute(uhr);
        var seit = new DateOnly(2025, 11, 1);

        // 1. Personen und Rollen
        var personen = new Dictionary<string, Person>(StringComparer.Ordinal);
        foreach (var d in Demofraktion.Personen)
        {
            var p = new Person(d.Vorname, d.Nachname, $"{d.Kennung}@demo.example", d.Kennung, jetzt, d.RisId);
            p.RollenFestlegenAb(seit, d.Rollen, Erfassung.Erst(jetzt, Von, $"Demodaten: {d.Funktion}"));
            stammdaten.Hinzufuegen(p);
            personen[d.Kennung] = p;
        }

        await stammdaten.SpeichernAsync(abbruch);

        // 2. Gremien, Mandate und Sitzungen aus dem RIS (Mock)
        var ris = await risAbgleich.AbgleichenAsync(abbruch);

        // 3. Fraktionsinterne Gremien: die Fraktion selbst und ein Arbeitskreis
        var fraktion = new Gremium(Demofraktion.Anzeigename, "Fraktion", Gremienart.Fraktion, 1);
        var arbeitskreis = new Gremium("Arbeitskreis Schule und Ganztag", "AK Schule", Gremienart.Arbeitskreis, 200);
        stammdaten.Hinzufuegen(fraktion);
        stammdaten.Hinzufuegen(arbeitskreis);
        var erst = Erfassung.Erst(jetzt, Von, "Demodaten");
        foreach (var d in Demofraktion.Personen.Where(d => d.Rollen.Contains(Rolle.Mitglied) || d.Rollen.Contains(Rolle.SachkundigeBuergerin)))
        {
            stammdaten.Hinzufuegen(new Mitgliedschaft(personen[d.Kennung].Id, fraktion.Id, Funktion.Ordentlich, null, seit, erst));
        }

        foreach (var k in new[] { "awewers", "ydemir", "mkock", "crensing" })
        {
            stammdaten.Hinzufuegen(new Mitgliedschaft(personen[k].Id, arbeitskreis.Id, Funktion.Ordentlich, null, seit, erst));
        }

        // 4. Fraktionssitzungen (14-täglich montags 19 Uhr), Arbeitskreis, öffentliche Termine und Wahlkampf
        var anzahlTermine = 0;
        var montag = heute.AddDays(-(((int)heute.DayOfWeek + 6) % 7) - 56);
        var nr = 0;
        for (var tag = montag; tag <= heute.AddDays(120); tag = tag.AddDays(14), nr++)
        {
            termine.Hinzufuegen(new Termin("Fraktionssitzung", Terminart.Fraktionssitzung, tag.ToDateTime(new TimeOnly(19, 0)),
                tag.ToDateTime(new TimeOnly(21, 30)), "Fraktionsbüro, Rathaus Greven", fraktion.Id, Von, jetzt,
                "TOP 1 Bericht des Vorstands\nTOP 2 " + GrevenerThemen.Fraktionstops[nr % GrevenerThemen.Fraktionstops.Length]
                + "\nTOP 3 Vorbereitung der Ausschuss- und Ratssitzungen\nTOP 4 Verschiedenes"));
            anzahlTermine++;
        }

        nr = 0;
        for (var tag = montag.AddDays(9); tag <= heute.AddDays(120); tag = tag.AddDays(28), nr++)
        {
            termine.Hinzufuegen(new Termin("Arbeitskreis Schule und Ganztag", Terminart.Arbeitskreis, tag.ToDateTime(new TimeOnly(18, 30)),
                tag.ToDateTime(new TimeOnly(20, 0)), "Fraktionsbüro", arbeitskreis.Id, Von, jetzt,
                GrevenerThemen.Arbeitskreistops[nr % GrevenerThemen.Arbeitskreistops.Length]));
            anzahlTermine++;
        }

        Randomizer.Seed = new Random(Startwert);
        var f = new Faker("de");
        var orte = new[] { "Marktplatz Greven", "Bürgerhaus Reckenfeld", "Ballenlager", "Rathausstraße (Treffpunkt Rathaus)", "Emsaue" };
        var oeffentliche = new List<Termin>();
        foreach (var (titel, art, ort, beschreibung) in GrevenerThemen.OeffentlicheTermine)
        {
            var tag = heute.AddDays(f.Random.Int(2, 80));
            var beginn = tag.ToDateTime(new TimeOnly(f.Random.Int(10, 19), f.Random.Bool() ? 0 : 30));
            var t = new Termin(titel, art, beginn, beginn.AddHours(f.Random.Int(1, 3)), ort ?? f.PickRandom(orte), null, Von, jetzt, beschreibung);
            termine.Hinzufuegen(t);
            oeffentliche.Add(t);
            anzahlTermine++;
        }

        await stammdaten.SpeichernAsync(abbruch);

        // Ein paar Zusagen zu öffentlichen Terminen
        foreach (var t in oeffentliche.Take(4))
        {
            foreach (var k in f.PickRandom(new[] { "tbrinkmann", "awewers", "fbaeumer", "nfeldhaus", "plammers", "mkock" }, 2))
            {
                termine.Hinzufuegen(new Teilnahme(t.Id, personen[k].Id, Teilnahmestatus.Zugesagt, jetzt));
            }
        }

        // 5. Vertretungen in verschiedenen Zuständen
        // Aktueller Zeitpunkt: Die Mandate aus dem RIS-Abgleich sind erst seit eben bekannt (bitemporaler Wissensstand).
        var faelle = await VertretungenAsync(personen, heute, uhr.GetUtcNow(), abbruch);

        await protokoll.ProtokollierenAsync("Demodaten.Angelegt",
            $"Demofraktion angelegt: {personen.Count} Personen, {anzahlTermine} eigene Termine, {ris.Zusammenfassung}, {faelle} Vertretungsfälle", abbruch: abbruch);
        await stammdaten.SpeichernAsync(abbruch);
        return new Demodatensatz(personen.Count, ris.GremienNeu + 2, anzahlTermine + ris.SitzungenNeu, ris, faelle);
    }

    private async Task<int> VertretungenAsync(IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt, CancellationToken abbruch)
    {
        var gremien = (await stammdaten.GremienAsync(abbruch)).Where(g => g.RisId is not null).ToDictionary(g => g.RisId!);
        var mitgliedschaften = await stammdaten.MitgliedschaftenAsync(abbruch);
        var sitzungen = (await termine.TermineAsync(heute.AddDays(2).ToDateTime(TimeOnly.MinValue), heute.AddDays(60).ToDateTime(TimeOnly.MinValue), abbruch))
            .Where(t => t.Art.IstGremiensitzung()).ToList();
        Termin? Naechste(string gremiumRisId) => sitzungen.FirstOrDefault(t => t.GremiumId == gremien[gremiumRisId].Id);
        IReadOnlyList<Guid> Kandidaten(Termin t) => Besetzung.StellvertretungenAm(mitgliedschaften, t.GremiumId!.Value, t.Tag, jetzt);
        DateTimeOffset Frist(Termin t) => Vertretungsfall.FristBerechnen(jetzt, Ortszeit.Zeitpunkt(t.Beginn), 24);
        var anzahl = 0;

        // a) Jens Overmeyer fehlt im HFA: Anna Wewers (Rang 1) ist gefragt und hat noch nicht geantwortet.
        if (Naechste("ris-g02") is { } hfa)
        {
            var a = Abwesenheit(personen["jovermeyer"], hfa.Tag, hfa.Tag.AddDays(4), "Urlaub", jetzt);
            var (fall, schritt) = Vertretungsfall.Starten(hfa.Id, hfa.GremiumId!.Value, a.PersonId, a.Id, Kandidaten(hfa), jetzt, Frist(hfa));
            vertretung.Hinzufuegen(fall);
            if (schritt.NeueAnfrage is { } anfrage && personen.Values.FirstOrDefault(p => p.Id == anfrage.PersonId) is { } gefragt)
            {
                await postausgang.BenachrichtigenAsync(gefragt, $"Vertretung gesucht: HFA am {hfa.Beginn:dd.MM.} um {hfa.Beginn:HH:mm} Uhr",
                    $"Kannst du am {hfa.Beginn:dd.MM.} im HFA vertreten?", "Demodaten: Vertretungsanfrage (siehe Startseite).",
                    $"handy/anfrage/{anfrage.Id}", "Ich vertrete", abbruch);
            }

            anzahl++;
        }

        // b) Dirk Schulze-Eling fehlt im ASBU: Abwesenheit eingetragen, Vertretung noch nicht angefragt.
        if (Naechste("ris-g03") is { } asbu)
        {
            Abwesenheit(personen["dschulze"], asbu.Tag.AddDays(-1), asbu.Tag.AddDays(1), null, jetzt);
        }

        // c) Claudia Rensing fehlt im ASFS: alle Stellvertretungen haben abgesagt → Aufgabe für den Vorstand.
        if (Naechste("ris-g05") is { } asfs)
        {
            var a = Abwesenheit(personen["crensing"], asfs.Tag, asfs.Tag, "beruflich verhindert", jetzt);
            var (fall, _) = Vertretungsfall.Starten(asfs.Id, asfs.GremiumId!.Value, a.PersonId, a.Id, Kandidaten(asfs), jetzt.AddDays(-2), jetzt.AddDays(-1));
            while (fall.OffeneAnfrage is { } offen)
            {
                fall.Antworten(offen.PersonId, false, jetzt.AddHours(-30), jetzt.AddHours(-6));
            }

            vertretung.Hinzufuegen(fall);
            vertretung.Hinzufuegen(new Aufgabe($"Vertretung offen: ASFS am {asfs.Beginn:dd.MM.yyyy}",
                $"Alle {fall.Kandidaten.Count} Stellvertretungen für Claudia Rensing haben abgesagt oder nicht rechtzeitig geantwortet.",
                Rolle.Vorstand, VertretungDienst.AufgabenAnlass, fall.Id, $"vertretungen/fall/{fall.Id}", jetzt, Ortszeit.Zeitpunkt(asfs.Beginn).AddHours(-2)));
            anzahl++;
        }

        // d) Stefan Grüter fehlt im RPA: Dirk Schulze-Eling (Rang 1) hat zugesagt.
        if (Naechste("ris-g07") is { } rpa)
        {
            var a = Abwesenheit(personen["sgrueter"], rpa.Tag, rpa.Tag, null, jetzt);
            var (fall, schritt) = Vertretungsfall.Starten(rpa.Id, rpa.GremiumId!.Value, a.PersonId, a.Id, Kandidaten(rpa), jetzt.AddDays(-1), Frist(rpa));
            if (schritt.NeueAnfrage is { } anfrage)
            {
                fall.Antworten(anfrage.PersonId, true, jetzt.AddHours(-20), Frist(rpa));
                termine.Hinzufuegen(new Teilnahme(rpa.Id, anfrage.PersonId, Teilnahmestatus.Vertritt, jetzt.AddHours(-20), a.PersonId));
            }

            vertretung.Hinzufuegen(fall);
            anzahl++;
        }

        return anzahl;
    }

    private Abwesenheit Abwesenheit(Person person, DateOnly von, DateOnly bis, string? grund, DateTimeOffset jetzt)
    {
        var a = new Abwesenheit(person.Id, von, bis, grund, jetzt);
        vertretung.Hinzufuegen(a);
        return a;
    }
}
