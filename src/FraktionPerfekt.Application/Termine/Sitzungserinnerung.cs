using FraktionPerfekt.Application.Gemeinsam;
using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;

namespace FraktionPerfekt.Application.Termine;

/// <summary>
/// Erinnerungen und Löschfristen rund um Termine (stündliche Fristenprüfung):
/// <list type="bullet">
/// <item>Push etwa 24 Stunden vor jeder Sitzung (Fraktion, Rat, Ausschuss, Arbeitskreis) an alle, die dabei sind
/// (ordentlich, vertretend oder zugesagt) – genau einmal je Termin und Person; nicht bei Absage oder Abwesenheit.</item>
/// <item>Löschfrist Abwesenheiten: Grund drei Monate nach Ende entfernen; Abwesenheiten ohne Vertretungsfall zwölf Monate
/// nach Ende löschen (mit Vertretungsfall bleibt der Zeitraum als Nachweis, ohne Grund).</item>
/// </list>
/// </summary>
public sealed class Sitzungserinnerung(
    KalenderDienst kalender,
    IDaten daten,
    IStammdatenSpeicher stammdaten,
    PostausgangDienst postausgang,
    TimeProvider uhr)
{
    public const int VorlaufStunden = 24;

    public const int GrundLoeschenMonate = 3;

    public const int AbwesenheitLoeschenMonate = 12;

    public async Task<int> ErinnernAsync(CancellationToken abbruch = default)
    {
        var jetzt = Ortszeit.Jetzt(uhr);
        var grenze = jetzt.AddHours(VorlaufStunden);
        var heute = DateOnly.FromDateTime(jetzt);
        var schon = (await daten.ListeAsync(daten.Menge<TerminErinnerung>().Where(e => e.Am > uhr.GetUtcNow().AddDays(-3)).Select(e => new { e.TerminId, e.PersonId }), abbruch))
            .Select(e => (e.TerminId, e.PersonId)).ToHashSet();
        var n = 0;
        foreach (var person in (await stammdaten.PersonenAsync(abbruch)).Where(p => p.Aktiv))
        {
            var zeilen = await kalender.ZeilenAsync(person.Id, null, heute, DateOnly.FromDateTime(grenze), new Kalenderfilter(PersonId: person.Id), abbruch);
            foreach (var z in zeilen.Where(z => z.Termin.Art is Terminart.Fraktionssitzung or Terminart.Ratssitzung or Terminart.Ausschusssitzung or Terminart.Arbeitskreis
                                                && z.Termin.Beginn > jetzt && z.Termin.Beginn <= grenze && !z.Termin.Abgesagt && !z.Abwesend
                                                && z.MeinStatus != Teilnahmestatus.Abgesagt && !schon.Contains((z.Termin.Id, person.Id))))
            {
                var wann = DateOnly.FromDateTime(z.Termin.Beginn) == heute ? "Heute" : "Morgen";
                await postausgang.PushEinreihenAsync(person, $"{wann} {z.Termin.Beginn:HH:mm}: {z.GremiumKurz ?? z.Termin.Titel}",
                    $"{z.Termin.Titel}{(z.Termin.Ort is null ? "" : $" · {z.Termin.Ort}")}{(z.Beteiligung == Beteiligung.Vertritt ? " · Sie vertreten" : "")}",
                    $"termine/{z.Termin.Id}", abbruch: abbruch);
                daten.Hinzufuegen(new TerminErinnerung(z.Termin.Id, person.Id, uhr.GetUtcNow()));
                n++;
            }
        }

        if (n > 0)
        {
            await daten.SpeichernAsync(abbruch);
        }

        return n;
    }

    public async Task<int> AbwesenheitenLoeschfristAsync(CancellationToken abbruch = default)
    {
        var heute = Ortszeit.Heute(uhr);
        var grundGrenze = heute.AddMonths(-GrundLoeschenMonate);
        var loeschGrenze = heute.AddMonths(-AbwesenheitLoeschenMonate);
        var alle = await daten.ListeAsync(daten.Menge<Abwesenheit>().Where(a => a.Bis < grundGrenze).OrderBy(a => a.Id), abbruch);
        var mitFall = (await daten.ListeAsync(daten.Menge<Vertretungsfall>().Where(f => f.AbwesenheitId != null).Select(f => f.AbwesenheitId!.Value), abbruch)).ToHashSet();
        var n = 0;
        foreach (var a in alle)
        {
            if (a.Bis < loeschGrenze && !mitFall.Contains(a.Id))
            {
                daten.Entfernen(a);
                n++;
            }
            else if (a.Grund is not null)
            {
                a.GrundEntfernen();
                n++;
            }
        }

        if (n > 0)
        {
            await daten.SpeichernAsync(abbruch);
        }

        return n;
    }
}
