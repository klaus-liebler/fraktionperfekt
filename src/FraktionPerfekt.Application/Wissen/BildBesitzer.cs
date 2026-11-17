using FraktionPerfekt.Application.Antraege;
using FraktionPerfekt.Application.Politik;
using FraktionPerfekt.Application.Sitzungen;
using FraktionPerfekt.Domain.Antraege;
using FraktionPerfekt.Domain.Politik;
using FraktionPerfekt.Domain.Sitzungen;
using FraktionPerfekt.Domain.Wissen;
using Microsoft.Extensions.DependencyInjection;

namespace FraktionPerfekt.Application.Wissen;

/// <summary>
/// Ein „Besitzer“ von Bildern (Wiki-Seite, Antrag, Sitzung …): Wer ihn sehen darf, darf auch seine Bilder sehen.
/// Die Prüfung nutzt die Rechte des jeweiligen Moduls (vertrauliche Seiten, Gremien, Datenschutz bei Bürgeranliegen).
/// <paramref name="Texte"/> liefert alle Texte, die Bilder enthalten können – für das Aufräumen (ohne Rechteprüfung).
/// Die Dienste werden erst beim Aufruf aufgelöst (keine Abhängigkeitsschleife, der Dienst darf selbst Bilder nutzen).
/// </summary>
public sealed record BildBesitzer(
    string Art,
    Func<IServiceProvider, Guid, CancellationToken, Task<bool>> Sichtbar,
    Func<IDaten, CancellationToken, Task<IEnumerable<string>>> Texte)
{
    /// <summary>Bilder in Formularen, deren Gegenstand es noch nicht gibt (neuer Antrag, neue Anfrage): nur für die hochladende Person,
    /// bis der Gegenstand gespeichert ist und die Verwendung übernommen wird.</summary>
    public const string Entwurf = "Entwurf";

    public static IEnumerable<BildBesitzer> Standard() =>
    [
        new(BildDienst.Wiki,
            async (sp, id, t) => await sp.GetRequiredService<WikiDienst>().SeiteAsync(id.ToString(), t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<WikiVersion>().OrderBy(v => v.SeiteId).ThenBy(v => v.Nummer).ThenBy(v => v.Id), t))
                .GroupBy(v => v.SeiteId).Select(g => g.Last().Text)),

        // Anträge: Bilder aller Fassungen bleiben, solange die Fassung existiert.
        new("Antrag",
            async (sp, id, t) => (await sp.GetRequiredService<Sicherheit.IAktuellerBenutzer>().ErmittelnAsync(t)).Rolle is not Domain.Personen.Rolle.Helferin
                                 && await sp.GetRequiredService<AntragDienst>().DetailAsync(id, t) is not null,
            async (d, t) => await d.ListeAsync(d.Menge<AntragFassung>().Select(f => f.Text), t)),

        new("Sitzung",
            async (sp, id, t) => await sp.GetRequiredService<SitzungDienst>().DetailAsync(id, t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<Top>().Select(x => x.Notiz), t)).OfType<string>()),

        new("Anfrage",
            async (sp, id, t) => (await sp.GetRequiredService<AnfrageDienst>().ListeAsync(t)).Any(z => z.Anfrage.Id == id),
            async (d, t) => (await d.ListeAsync(d.Menge<Anfrage>().Select(a => a.Text + "\n" + a.Antwort), t))),

        new("Presse",
            async (sp, id, t) => (await sp.GetRequiredService<PresseDienst>().MitteilungenAsync(t)).Any(z => z.Mitteilung.Id == id),
            async (d, t) => await d.ListeAsync(d.Menge<Pressemitteilung>().Select(m => m.Text), t)),

        // Bürgeranliegen: nur wer das Anliegen sehen darf (Zuständige, Leitung) – Datenschutz der Bürger:innen.
        new("Anliegen",
            async (sp, id, t) => (await sp.GetRequiredService<AnliegenDienst>().ListeAsync(t)).Any(z => z.Anliegen.Id == id),
            async (d, t) => await d.ListeAsync(d.Menge<Buergeranliegen>().Select(a => a.Text + "\n" + a.Antwort), t)),

        new("Besetzung",
            async (sp, id, t) => await sp.GetRequiredService<Termine.BesetzungDienst>().DetailAsync(id, t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<Domain.Termine.Besetzungsanfrage>().Select(a => a.Bericht), t)).OfType<string>()),

        new("Termin",
            async (sp, id, t) => await sp.GetRequiredService<Termine.KalenderDienst>().TerminAsync(id, t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<Domain.Termine.Termin>().Select(x => x.Beschreibung), t)).OfType<string>()),

        new("Kampagne",
            async (sp, id, t) => await sp.GetRequiredService<Kampagnen.KampagnenDienst>().KampagneAsync(id, t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<Domain.Kampagnen.Kampagne>().Select(x => x.Beschreibung), t)).OfType<string>()),

        new("Ticket",
            async (sp, id, t) => await sp.GetRequiredService<Kampagnen.KampagnenDienst>().TicketAsync(id, t) is not null,
            async (d, t) => (await d.ListeAsync(d.Menge<Domain.Kampagnen.Ticket>().Select(x => x.Beschreibung), t)).OfType<string>()),

        new(Entwurf, (_, _, _) => Task.FromResult(false), (_, _) => Task.FromResult(Enumerable.Empty<string>())),
    ];
}
