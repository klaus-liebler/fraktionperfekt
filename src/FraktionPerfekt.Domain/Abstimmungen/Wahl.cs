using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Abstimmungen;

/// <summary>Amt in der Fraktion, das durch Wahl vergeben wird (aus SchoolCraft Modul 33 übertragen).</summary>
public enum Fraktionsamt
{
    Vorsitz,
    ErsteStellvertretung,
    ZweiteStellvertretung,
    Beisitz,
    Kassier,

    /// <summary>Sprecher:in der Fraktion in einem Ausschuss.</summary>
    Sprecher,

    /// <summary>Benennung für einen Sitz außerhalb der Fraktion (Ausschuss, Aufsichtsrat, Beirat) – Bezeichnung frei.</summary>
    Benennung,
}

public static class FraktionsamtExtensions
{
    public static string Anzeigename(this Fraktionsamt amt) => amt switch
    {
        Fraktionsamt.Vorsitz => "Fraktionsvorsitz",
        Fraktionsamt.ErsteStellvertretung => "1. stellv. Fraktionsvorsitz",
        Fraktionsamt.ZweiteStellvertretung => "2. stellv. Fraktionsvorsitz",
        Fraktionsamt.Beisitz => "Beisitz im Fraktionsvorstand",
        Fraktionsamt.Kassier => "Schatzmeister/Kassier",
        Fraktionsamt.Sprecher => "Sprecher:in",
        Fraktionsamt.Benennung => "Benennung",
        _ => amt.ToString(),
    };

    /// <summary>Rolle, die das Amt für die Amtszeit mit sich bringt (Rechte); <c>null</c> = keine eigene Rolle.</summary>
    public static Rolle? AmtsRolle(this Fraktionsamt amt) => amt switch
    {
        Fraktionsamt.Vorsitz or Fraktionsamt.ErsteStellvertretung or Fraktionsamt.ZweiteStellvertretung or Fraktionsamt.Beisitz => Rolle.Vorstand,
        Fraktionsamt.Kassier => Rolle.Kassier,
        _ => null,
    };

    /// <summary>Ämter, die je Fraktion genau eine Person hat (eine neue Wahl löst die bisherige Amtszeit ab).</summary>
    public static bool Einzeln(this Fraktionsamt amt) => amt is not (Fraktionsamt.Beisitz or Fraktionsamt.Benennung or Fraktionsamt.Sprecher);
}

public enum Wahlstatus
{
    /// <summary>Kandidaturen werden gesammelt.</summary>
    Vorbereitung,

    /// <summary>Ein Wahlgang läuft.</summary>
    Wahlgang,

    /// <summary>Niemand hat im ersten Wahlgang die Mehrheit: Stichwahl der Bestplatzierten steht an.</summary>
    Stichwahl,

    /// <summary>Gleichstand in der Stichwahl: Los entscheidet (Vorstand erfasst das Ergebnis).</summary>
    Los,

    /// <summary>Gewählt; die Person muss die Wahl noch annehmen.</summary>
    Gewaehlt,

    /// <summary>Wahl angenommen, Amtszeit eingetragen.</summary>
    Angenommen,

    /// <summary>Ohne Ergebnis (niemand gewählt, Wahl abgelehnt oder zu wenige Stimmen) – neue Wahl nötig.</summary>
    OhneErgebnis,
}

/// <summary>Wahl für ein Amt mit Amtszeit: Kandidaturen, Wahlgänge (offen oder geheim), Mehrheit, Stichwahl, Annahme, Wahlprotokoll.</summary>
public sealed class Wahl
{
    private Wahl()
    {
        Titel = string.Empty;
    }

    public Wahl(Fraktionsamt amt, string? bezeichnung, Guid? gremiumId, DateOnly amtAb, DateOnly? amtBis, bool geheim, Guid erstelltVonPersonId, DateTimeOffset jetzt)
    {
        if (amt is Fraktionsamt.Benennung && string.IsNullOrWhiteSpace(bezeichnung))
        {
            throw new ArgumentException("Bei einer Benennung bitte angeben, wofür (z. B. „Aufsichtsrat Stadtwerke“).", nameof(bezeichnung));
        }

        if (amt is Fraktionsamt.Sprecher && gremiumId is null)
        {
            throw new ArgumentException("Bei Sprecher:innen bitte den Ausschuss wählen.", nameof(gremiumId));
        }

        if (amtBis is { } bis && bis < amtAb)
        {
            throw new ArgumentException("Die Amtszeit endet vor ihrem Beginn.", nameof(amtBis));
        }

        Id = Ids.Neu();
        Amt = amt;
        Bezeichnung = string.IsNullOrWhiteSpace(bezeichnung) ? null : bezeichnung.Trim();
        GremiumId = gremiumId;
        Titel = Bezeichnung is null ? amt.Anzeigename() : $"{amt.Anzeigename()}: {Bezeichnung}";
        AmtAb = amtAb;
        AmtBis = amtBis;
        Geheim = geheim;
        ErstelltVonPersonId = erstelltVonPersonId;
        ErstelltAm = jetzt;
        Status = Wahlstatus.Vorbereitung;
    }

    public Guid Id { get; private set; }

    public Fraktionsamt Amt { get; private set; }

    public string? Bezeichnung { get; private set; }

    public Guid? GremiumId { get; private set; }

    public string Titel { get; private set; }

    /// <summary>Beginn der Amtszeit (ab diesem Tag gelten Rolle und Funktion).</summary>
    public DateOnly AmtAb { get; private set; }

    /// <summary>Ende der Amtszeit (letzter Tag), z. B. Ende der Wahlperiode.</summary>
    public DateOnly? AmtBis { get; private set; }

    /// <summary>Wahlgänge geheim (Standard bei Personenwahlen, ⚑ Geschäftsordnung).</summary>
    public bool Geheim { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public Wahlstatus Status { get; private set; }

    public Guid? GewaehltPersonId { get; private set; }

    /// <summary>Festgestelltes Ergebnis (Text für Liste und Wahlprotokoll, eingefroren).</summary>
    public string? Ergebnis { get; private set; }

    public DateTimeOffset? AngenommenAm { get; private set; }

    public void WahlgangGestartet()
    {
        if (Status is not (Wahlstatus.Vorbereitung or Wahlstatus.Stichwahl or Wahlstatus.OhneErgebnis))
        {
            throw new InvalidOperationException("Für diese Wahl kann kein Wahlgang mehr starten.");
        }

        Status = Wahlstatus.Wahlgang;
    }

    public void Festhalten(Wahlstatus status, Guid? gewaehlt, string ergebnis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ergebnis);
        Status = status;
        GewaehltPersonId = gewaehlt;
        Ergebnis = ergebnis;
    }

    public void Annehmen(DateTimeOffset jetzt)
    {
        if (Status != Wahlstatus.Gewaehlt)
        {
            throw new InvalidOperationException("Annehmen lässt sich nur eine abgeschlossene Wahl mit gewählter Person.");
        }

        Status = Wahlstatus.Angenommen;
        AngenommenAm = jetzt;
    }

    public void Ablehnen(string ergebnis)
    {
        if (Status != Wahlstatus.Gewaehlt)
        {
            throw new InvalidOperationException("Ablehnen lässt sich nur eine abgeschlossene Wahl mit gewählter Person.");
        }

        Status = Wahlstatus.OhneErgebnis;
        Ergebnis = ergebnis;
    }
}

/// <summary>Kandidatur für eine Wahl.</summary>
public sealed class Kandidatur
{
    private Kandidatur()
    {
    }

    public Kandidatur(Guid wahlId, Guid personId, Guid vorgeschlagenVonPersonId, DateTimeOffset am)
    {
        Id = Ids.Neu();
        WahlId = wahlId;
        PersonId = personId;
        VorgeschlagenVonPersonId = vorgeschlagenVonPersonId;
        Am = am;
    }

    public Guid Id { get; private set; }

    public Guid WahlId { get; private set; }

    public Guid PersonId { get; private set; }

    public Guid VorgeschlagenVonPersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public DateTimeOffset? ZurueckgezogenAm { get; private set; }

    public bool Aktiv => ZurueckgezogenAm is null;

    public void Zurueckziehen(DateTimeOffset jetzt) => ZurueckgezogenAm ??= jetzt;
}

/// <summary>Ein Amt mit Amtszeit, entstanden aus einer angenommenen Wahl (Beleg; Rolle und Funktion stehen bitemporal an der Person).</summary>
public sealed class Amtszeit
{
    private Amtszeit()
    {
    }

    public Amtszeit(Guid personId, Fraktionsamt amt, string? bezeichnung, Guid? gremiumId, DateOnly von, DateOnly? bis, Guid wahlId)
    {
        Id = Ids.Neu();
        PersonId = personId;
        Amt = amt;
        Bezeichnung = bezeichnung;
        GremiumId = gremiumId;
        Von = von;
        Bis = bis;
        WahlId = wahlId;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public Fraktionsamt Amt { get; private set; }

    public string? Bezeichnung { get; private set; }

    public Guid? GremiumId { get; private set; }

    public DateOnly Von { get; private set; }

    /// <summary>Letzter Tag der Amtszeit.</summary>
    public DateOnly? Bis { get; private set; }

    public Guid WahlId { get; private set; }

    /// <summary>Für die Fristenprüfung: Aufgabe „Neuwahl vorbereiten“ ist angelegt.</summary>
    public DateTimeOffset? NeuwahlErinnertAm { get; private set; }

    public bool GiltAm(DateOnly tag) => Von <= tag && (Bis is null || tag <= Bis);

    /// <summary>Endet vorzeitig, weil jemand anderes ab <paramref name="ab"/> gewählt ist (letzter Tag: Vortag).</summary>
    public void EndetVor(DateOnly ab)
    {
        if (Bis is null || Bis >= ab)
        {
            Bis = ab.AddDays(-1);
        }
    }

    public void NeuwahlErinnert(DateTimeOffset jetzt) => NeuwahlErinnertAm = jetzt;
}

/// <summary>Ergebnis eines Wahlgangs nach Mehrheitsregel.</summary>
public sealed record Wahlgangergebnis(Wahlstatus Status, Guid? Gewaehlt, IReadOnlyList<Guid> Stichwahl, IReadOnlyList<Guid> Gleichstand, string Text);

/// <summary>
/// Mehrheitsregeln (⚑ Geschäftsordnung, als Einstellung): Im ersten Wahlgang ist gewählt, wer mehr als die Hälfte der gültigen Stimmen hat
/// (Enthaltungen zählen nur, wenn die Einstellung es sagt). Bei nur einer Kandidatur entscheidet Ja gegen Nein. Erreicht niemand die Mehrheit,
/// folgt (Einstellung) eine Stichwahl der beiden Bestplatzierten; dort genügt die einfache Mehrheit, bei Gleichstand entscheidet das Los.
/// </summary>
public static class Mehrheit
{
    public const string Nein = "Nein";

    public const string Enthaltung = "Enthaltung";

    /// <param name="stimmen">Stimmen je Kandidat:in (Person), dazu Nein- und Enthaltungsstimmen.</param>
    public static Wahlgangergebnis Auswerten(IReadOnlyDictionary<Guid, int> stimmen, int nein, int enthaltung, bool stichwahl, bool enthaltungenZaehlen,
        bool stichwahlErlaubt, Func<Guid, string> name, bool zuWenigeStimmen = false)
    {
        ArgumentNullException.ThrowIfNull(stimmen);
        ArgumentNullException.ThrowIfNull(name);
        if (zuWenigeStimmen)
        {
            return new(Wahlstatus.OhneErgebnis, null, [], [], "Zu wenige Stimmen für eine geheime Auswertung – neuer Wahlgang nötig.");
        }

        var gueltig = stimmen.Values.Sum() + nein + (enthaltungenZaehlen ? enthaltung : 0);
        var rangfolge = stimmen.OrderByDescending(s => s.Value).ThenBy(s => name(s.Key), StringComparer.CurrentCulture).ToList();
        var verteilung = string.Join(", ", rangfolge.Select(s => $"{name(s.Key)} {s.Value}")) + (stimmen.Count == 1 ? $", Nein {nein}" : "") + $", Enthaltung {enthaltung}";
        if (rangfolge.Count == 0 || gueltig == 0)
        {
            return new(Wahlstatus.OhneErgebnis, null, [], [], $"Keine gültigen Stimmen ({verteilung}).");
        }

        var erster = rangfolge[0];
        if (stichwahl)
        {
            var gleich = rangfolge.Where(s => s.Value == erster.Value).Select(s => s.Key).ToList();
            return gleich.Count > 1
                ? new(Wahlstatus.Los, null, [], gleich, $"Stichwahl mit Gleichstand ({verteilung}): Das Los entscheidet.")
                : new(Wahlstatus.Gewaehlt, erster.Key, [], [], $"Gewählt in der Stichwahl: {name(erster.Key)} ({verteilung}).");
        }

        if (erster.Value * 2 > gueltig)
        {
            return new(Wahlstatus.Gewaehlt, erster.Key, [], [], $"Gewählt: {name(erster.Key)} mit {erster.Value} von {gueltig} gültigen Stimmen ({verteilung}).");
        }

        if (rangfolge.Count == 1)
        {
            return new(Wahlstatus.OhneErgebnis, null, [], [], $"Nicht gewählt: {name(erster.Key)} hat die Mehrheit verfehlt ({verteilung}).");
        }

        if (!stichwahlErlaubt)
        {
            return new(Wahlstatus.OhneErgebnis, null, [], [], $"Niemand hat die Mehrheit erreicht ({verteilung}); neuer Wahlgang nötig.");
        }

        // Stichwahl der beiden Bestplatzierten; bei Gleichstand um Platz 2 alle mit dieser Stimmenzahl
        var zweiterWert = rangfolge[1].Value;
        var stich = rangfolge.Where(s => s.Value >= zweiterWert).Select(s => s.Key).ToList();
        return new(Wahlstatus.Stichwahl, null, stich, [], $"Niemand hat die Mehrheit erreicht ({verteilung}): Stichwahl zwischen {string.Join(" und ", stich.Select(name))}.");
    }
}
