using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Antraege;

public enum Antragsstatus
{
    Idee,
    Entwurf,
    InAbstimmung,
    Beschlossen,
    Abgelehnt,
    Eingereicht,
    ImGremium,
    Erledigt,
    Zurueckgezogen,
}

public static class AntragsstatusExtensions
{
    public static string Anzeigename(this Antragsstatus s) => s switch
    {
        Antragsstatus.Idee => "Idee",
        Antragsstatus.Entwurf => "Entwurf",
        Antragsstatus.InAbstimmung => "in Abstimmung",
        Antragsstatus.Beschlossen => "beschlossen (Fraktion)",
        Antragsstatus.Abgelehnt => "abgelehnt (Fraktion)",
        Antragsstatus.Eingereicht => "eingereicht",
        Antragsstatus.ImGremium => "im Gremium beraten",
        Antragsstatus.Erledigt => "erledigt",
        Antragsstatus.Zurueckgezogen => "zurückgezogen",
        _ => s.ToString(),
    };

    /// <summary>Text kann nur in frühen Phasen bearbeitet werden; danach gilt die beschlossene Fassung.</summary>
    public static bool TextAenderbar(this Antragsstatus s) => s is Antragsstatus.Idee or Antragsstatus.Entwurf;
}

/// <summary>
/// Antrag der Fraktion: Idee → Entwurf → in Abstimmung → beschlossen → eingereicht → im Gremium beraten → erledigt.
/// Der Text liegt in Fassungen vor (nie überschrieben); Kommentare und Änderungsvorschläge beziehen sich auf eine Fassung.
/// </summary>
public sealed class Antrag
{
    private static readonly Dictionary<Antragsstatus, Antragsstatus[]> Uebergaenge = new()
    {
        [Antragsstatus.Idee] = [Antragsstatus.Entwurf, Antragsstatus.Zurueckgezogen],
        [Antragsstatus.Entwurf] = [Antragsstatus.Idee, Antragsstatus.InAbstimmung, Antragsstatus.Zurueckgezogen],
        [Antragsstatus.InAbstimmung] = [Antragsstatus.Beschlossen, Antragsstatus.Abgelehnt, Antragsstatus.Entwurf],
        [Antragsstatus.Beschlossen] = [Antragsstatus.Eingereicht, Antragsstatus.Zurueckgezogen],
        [Antragsstatus.Abgelehnt] = [Antragsstatus.Entwurf],
        [Antragsstatus.Eingereicht] = [Antragsstatus.ImGremium, Antragsstatus.Zurueckgezogen],
        [Antragsstatus.ImGremium] = [Antragsstatus.Erledigt],
        [Antragsstatus.Erledigt] = [],
        [Antragsstatus.Zurueckgezogen] = [],
    };

    private Antrag()
    {
        Titel = string.Empty;
    }

    public Antrag(string titel, Guid autorPersonId, Guid? zielGremiumId, string? thema, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        Titel = titel.Trim();
        AutorPersonId = autorPersonId;
        ZielGremiumId = zielGremiumId;
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        ErstelltAm = jetzt;
        GeaendertAm = jetzt;
        Status = Antragsstatus.Idee;
    }

    public Guid Id { get; private set; }

    /// <summary>Fraktionsinterne Nummer, vergeben beim Einreichen (z. B. „A-2026-007“).</summary>
    public string? Nummer { get; private set; }

    public string Titel { get; private set; }

    public string? Thema { get; private set; }

    public Antragsstatus Status { get; private set; }

    public Guid AutorPersonId { get; private set; }

    public Guid? ZielGremiumId { get; private set; }

    /// <summary>Sitzung (Termin), in der der Antrag beraten wird.</summary>
    public Guid? SitzungTerminId { get; private set; }

    public Guid? AbstimmungId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public DateTimeOffset? EingereichtAm { get; private set; }

    public string? Vermerk { get; private set; }

    public static IReadOnlyList<Antragsstatus> MoeglicheUebergaenge(Antragsstatus von) => Uebergaenge[von];

    public bool KannWechselnZu(Antragsstatus ziel) => Uebergaenge[Status].Contains(ziel);

    public void Umbenennen(string titel, string? thema, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Titel = titel.Trim();
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        GeaendertAm = jetzt;
    }

    public void Wechseln(Antragsstatus ziel, DateTimeOffset jetzt, string? vermerk = null)
    {
        if (!KannWechselnZu(ziel))
        {
            throw new InvalidOperationException($"Von „{Status.Anzeigename()}“ geht es nicht zu „{ziel.Anzeigename()}“.");
        }

        Status = ziel;
        GeaendertAm = jetzt;
        if (!string.IsNullOrWhiteSpace(vermerk))
        {
            Vermerk = vermerk.Trim();
        }
    }

    public void AbstimmungZuordnen(Guid abstimmungId, DateTimeOffset jetzt)
    {
        Wechseln(Antragsstatus.InAbstimmung, jetzt);
        AbstimmungId = abstimmungId;
    }

    public void Einreichen(string nummer, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nummer);
        Wechseln(Antragsstatus.Eingereicht, jetzt);
        Nummer = nummer;
        EingereichtAm = jetzt;
    }

    public void Zuordnen(Guid? zielGremiumId, Guid? sitzungTerminId, DateTimeOffset jetzt)
    {
        ZielGremiumId = zielGremiumId ?? ZielGremiumId;
        SitzungTerminId = sitzungTerminId;
        GeaendertAm = jetzt;
    }
}

/// <summary>Eine Fassung des Antragstexts (Markdown). Fassungen werden nie geändert.</summary>
public sealed class AntragFassung
{
    private AntragFassung()
    {
        Text = string.Empty;
    }

    public AntragFassung(Guid antragId, int nummer, string text, Guid personId, DateTimeOffset am, string? notiz)
    {
        Id = Ids.Neu();
        AntragId = antragId;
        Nummer = nummer;
        Text = text ?? string.Empty;
        PersonId = personId;
        Am = am;
        Notiz = string.IsNullOrWhiteSpace(notiz) ? null : notiz.Trim();
    }

    public Guid Id { get; private set; }

    public Guid AntragId { get; private set; }

    public int Nummer { get; private set; }

    public string Text { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public string? Notiz { get; private set; }
}

/// <summary>Kommentar am Text (optional mit zitierter Stelle).</summary>
public sealed class AntragKommentar
{
    private AntragKommentar()
    {
        Text = string.Empty;
    }

    public AntragKommentar(Guid antragId, int fassung, Guid personId, string? zitat, string text, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        AntragId = antragId;
        Fassung = fassung;
        PersonId = personId;
        Zitat = string.IsNullOrWhiteSpace(zitat) ? null : zitat.Trim();
        Text = text.Trim();
        Am = am;
    }

    public Guid Id { get; private set; }

    public Guid AntragId { get; private set; }

    public int Fassung { get; private set; }

    public Guid PersonId { get; private set; }

    public string? Zitat { get; private set; }

    public string Text { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public bool Erledigt { get; private set; }

    /// <summary>Zurückgezogen (Korrekturregel): Der Text bleibt erhalten, wird aber nicht mehr gezeigt.</summary>
    public DateTimeOffset? ZurueckgezogenAm { get; private set; }

    public string? Korrekturgrund { get; private set; }

    public void AlsErledigtMarkieren() => Erledigt = true;

    public void Zurueckziehen(string grund, DateTimeOffset jetzt)
    {
        if (ZurueckgezogenAm is not null)
        {
            throw new InvalidOperationException("Der Kommentar ist bereits zurückgezogen.");
        }

        (ZurueckgezogenAm, Korrekturgrund) = (jetzt, grund);
    }
}

public enum Vorschlagsstatus
{
    Offen,
    Angenommen,
    Abgelehnt,
}

/// <summary>Änderungsvorschlag: Textstelle <see cref="Alt"/> durch <see cref="Neu"/> ersetzen.</summary>
public sealed class Aenderungsvorschlag
{
    private Aenderungsvorschlag()
    {
        Alt = string.Empty;
        Neu = string.Empty;
    }

    public Aenderungsvorschlag(Guid antragId, int fassung, Guid personId, string alt, string neu, string? begruendung, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alt);
        Id = Ids.Neu();
        AntragId = antragId;
        Fassung = fassung;
        PersonId = personId;
        Alt = alt;
        Neu = neu ?? string.Empty;
        Begruendung = string.IsNullOrWhiteSpace(begruendung) ? null : begruendung.Trim();
        Am = am;
        Status = Vorschlagsstatus.Offen;
    }

    public Guid Id { get; private set; }

    public Guid AntragId { get; private set; }

    public int Fassung { get; private set; }

    public Guid PersonId { get; private set; }

    public string Alt { get; private set; }

    public string Neu { get; private set; }

    public string? Begruendung { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public Vorschlagsstatus Status { get; private set; }

    public Guid? EntschiedenVonPersonId { get; private set; }

    /// <summary>Wendet den Vorschlag auf den aktuellen Text an; die Stelle muss genau einmal vorkommen.</summary>
    public string Anwenden(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var erste = text.IndexOf(Alt, StringComparison.Ordinal);
        if (erste < 0)
        {
            throw new InvalidOperationException("Die Textstelle kommt in der aktuellen Fassung nicht mehr vor.");
        }

        if (text.IndexOf(Alt, erste + Alt.Length, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException("Die Textstelle kommt mehrfach vor; bitte im Vorschlag genauer zitieren.");
        }

        return string.Concat(text.AsSpan(0, erste), Neu, text.AsSpan(erste + Alt.Length));
    }

    public void Entscheiden(bool annehmen, Guid personId)
    {
        if (Status != Vorschlagsstatus.Offen)
        {
            throw new InvalidOperationException("Über den Vorschlag ist bereits entschieden.");
        }

        Status = annehmen ? Vorschlagsstatus.Angenommen : Vorschlagsstatus.Abgelehnt;
        EntschiedenVonPersonId = personId;
    }
}

public enum Vergleichsart
{
    Gleich,
    Entfernt,
    Eingefuegt,
}

public sealed record Vergleichsstueck(Vergleichsart Art, string Text);

/// <summary>Wortweiser Vergleich zweier Fassungen (längste gemeinsame Teilfolge).</summary>
public static class Textvergleich
{
    public static IReadOnlyList<Vergleichsstueck> Vergleichen(string alt, string neu)
    {
        var a = Woerter(alt);
        var b = Woerter(neu);
        var l = new int[a.Count + 1, b.Count + 1];
        for (var i = a.Count - 1; i >= 0; i--)
        {
            for (var j = b.Count - 1; j >= 0; j--)
            {
                l[i, j] = a[i] == b[j] ? l[i + 1, j + 1] + 1 : Math.Max(l[i + 1, j], l[i, j + 1]);
            }
        }

        var stuecke = new List<Vergleichsstueck>();
        void Anhaengen(Vergleichsart art, string wort)
        {
            if (stuecke.Count > 0 && stuecke[^1].Art == art)
            {
                stuecke[^1] = stuecke[^1] with { Text = stuecke[^1].Text + wort };
            }
            else
            {
                stuecke.Add(new Vergleichsstueck(art, wort));
            }
        }

        int x = 0, y = 0;
        while (x < a.Count && y < b.Count)
        {
            if (a[x] == b[y])
            {
                Anhaengen(Vergleichsart.Gleich, a[x]);
                x++;
                y++;
            }
            else if (l[x + 1, y] >= l[x, y + 1])
            {
                Anhaengen(Vergleichsart.Entfernt, a[x++]);
            }
            else
            {
                Anhaengen(Vergleichsart.Eingefuegt, b[y++]);
            }
        }

        while (x < a.Count)
        {
            Anhaengen(Vergleichsart.Entfernt, a[x++]);
        }

        while (y < b.Count)
        {
            Anhaengen(Vergleichsart.Eingefuegt, b[y++]);
        }

        return stuecke;
    }

    /// <summary>Wörter samt folgendem Leerraum, damit der Text beim Zusammensetzen gleich bleibt.</summary>
    private static List<string> Woerter(string text)
        => System.Text.RegularExpressions.Regex.Matches(text ?? string.Empty, @"\S+\s*|\s+").Select(m => m.Value).ToList();
}
