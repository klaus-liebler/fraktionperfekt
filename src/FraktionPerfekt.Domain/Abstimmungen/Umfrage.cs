using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Abstimmungen;

public enum Umfrageart
{
    /// <summary>Fraktionsabstimmung über einen Antrag (Ja/Nein/Enthaltung) mit Quorum.</summary>
    Antragsabstimmung,

    /// <summary>Schnelles Meinungsbild (Ja/Nein/Enthaltung) ohne Antrag.</summary>
    Meinungsbild,

    /// <summary>Umfrage mit frei wählbaren Antworten.</summary>
    Umfrage,

    /// <summary>Terminfindung: mehrere Zeitpunkte, Mehrfachauswahl.</summary>
    Terminfindung,
}

public enum Umfragestatus
{
    Offen,
    Abgeschlossen,
}

/// <summary>Abstimmung, Umfrage oder Terminfindung (Modul 13) mit Frist und optionalem Quorum.</summary>
public sealed class Umfrage
{
    public static readonly string[] JaNeinEnthaltung = ["Ja", "Nein", "Enthaltung"];

    private Umfrage()
    {
        Titel = string.Empty;
    }

    public Umfrage(string titel, string? beschreibung, Umfrageart art, DateTimeOffset frist, int quorumProzent, bool mehrfachauswahl, int berechtigte,
        Guid erstelltVonPersonId, DateTimeOffset jetzt, Guid? antragId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        if (frist <= jetzt)
        {
            throw new ArgumentException("Die Frist muss in der Zukunft liegen.", nameof(frist));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(quorumProzent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quorumProzent, 100);
        Id = Ids.Neu();
        Titel = titel.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        Art = art;
        Frist = frist;
        QuorumProzent = quorumProzent;
        Mehrfachauswahl = art == Umfrageart.Terminfindung || mehrfachauswahl;
        Berechtigte = berechtigte;
        ErstelltVonPersonId = erstelltVonPersonId;
        ErstelltAm = jetzt;
        AntragId = antragId;
        Status = Umfragestatus.Offen;
    }

    public Guid Id { get; private set; }

    public string Titel { get; private set; }

    public string? Beschreibung { get; private set; }

    public Umfrageart Art { get; private set; }

    public DateTimeOffset Frist { get; private set; }

    /// <summary>Mindestbeteiligung in Prozent der Berechtigten (0 = ohne Quorum).</summary>
    public int QuorumProzent { get; private set; }

    public bool Mehrfachauswahl { get; private set; }

    /// <summary>Zahl der Stimmberechtigten beim Start (Grundlage für das Quorum).</summary>
    public int Berechtigte { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public Guid? AntragId { get; private set; }

    public Umfragestatus Status { get; private set; }

    public DateTimeOffset? AbgeschlossenAm { get; private set; }

    public string? Ergebnis { get; private set; }

    public Guid? GewinnerOptionId { get; private set; }

    public bool Offen(DateTimeOffset jetzt) => Status == Umfragestatus.Offen && jetzt < Frist;

    public void Abschliessen(Umfrageergebnis ergebnis, DateTimeOffset jetzt)
    {
        ArgumentNullException.ThrowIfNull(ergebnis);
        if (Status == Umfragestatus.Abgeschlossen)
        {
            throw new InvalidOperationException("Die Abstimmung ist bereits abgeschlossen.");
        }

        Status = Umfragestatus.Abgeschlossen;
        AbgeschlossenAm = jetzt;
        Ergebnis = ergebnis.Text;
        GewinnerOptionId = ergebnis.Gewinner?.OptionId;
    }
}

public sealed class UmfrageOption
{
    private UmfrageOption()
    {
        Text = string.Empty;
    }

    public UmfrageOption(Guid umfrageId, int position, string text, DateTime? zeitpunkt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        UmfrageId = umfrageId;
        Position = position;
        Text = text.Trim();
        Zeitpunkt = zeitpunkt;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public int Position { get; private set; }

    public string Text { get; private set; }

    /// <summary>Bei der Terminfindung: vorgeschlagener Beginn (Wanduhrzeit).</summary>
    public DateTime? Zeitpunkt { get; private set; }
}

public sealed class Stimme
{
    private Stimme()
    {
    }

    public Stimme(Guid umfrageId, Guid optionId, Guid personId, DateTimeOffset am)
    {
        Id = Ids.Neu();
        UmfrageId = umfrageId;
        OptionId = optionId;
        PersonId = personId;
        Am = am;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public Guid OptionId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }
}

public sealed record Optionsergebnis(Guid OptionId, string Text, int Stimmen, DateTime? Zeitpunkt);

public sealed record Umfrageergebnis(int Teilnehmende, int Berechtigte, bool QuorumErreicht, IReadOnlyList<Optionsergebnis> Optionen, Optionsergebnis? Gewinner,
    bool? Angenommen, string Text);

public static class Auswertung
{
    /// <summary>
    /// Wertet eine Umfrage aus. Abstimmungen (Ja/Nein/Enthaltung): angenommen, wenn das Quorum erreicht ist und mehr Ja- als
    /// Nein-Stimmen abgegeben wurden. Sonst gewinnt die Option mit den meisten Stimmen (bei Gleichstand die frühere).
    /// </summary>
    public static Umfrageergebnis Auswerten(Umfrage umfrage, IReadOnlyList<UmfrageOption> optionen, IReadOnlyList<Stimme> stimmen)
    {
        ArgumentNullException.ThrowIfNull(umfrage);
        ArgumentNullException.ThrowIfNull(optionen);
        ArgumentNullException.ThrowIfNull(stimmen);
        var teilnehmende = stimmen.Select(s => s.PersonId).Distinct().Count();
        var quorum = umfrage.QuorumProzent == 0 || (umfrage.Berechtigte > 0 && teilnehmende * 100 >= umfrage.QuorumProzent * umfrage.Berechtigte);
        var ergebnisse = optionen.OrderBy(o => o.Position)
            .Select(o => new Optionsergebnis(o.Id, o.Text, stimmen.Count(s => s.OptionId == o.Id), o.Zeitpunkt)).ToList();
        var beteiligung = umfrage.Berechtigte > 0 ? $"{teilnehmende} von {umfrage.Berechtigte} haben abgestimmt" : $"{teilnehmende} haben abgestimmt";
        if (umfrage.Art is Umfrageart.Antragsabstimmung or Umfrageart.Meinungsbild)
        {
            int Zahl(string text) => ergebnisse.FirstOrDefault(e => e.Text == text)?.Stimmen ?? 0;
            var (ja, nein, enth) = (Zahl("Ja"), Zahl("Nein"), Zahl("Enthaltung"));
            var angenommen = quorum && ja > nein;
            var text = !quorum
                ? $"Quorum von {umfrage.QuorumProzent} % nicht erreicht ({beteiligung})."
                : $"{(angenommen ? "Angenommen" : "Abgelehnt")}: {ja} ja, {nein} nein, {enth} Enthaltung ({beteiligung}).";
            return new Umfrageergebnis(teilnehmende, umfrage.Berechtigte, quorum, ergebnisse, null, angenommen, text);
        }

        var gewinner = ergebnisse.Where(e => e.Stimmen > 0).OrderByDescending(e => e.Stimmen).FirstOrDefault();
        var t = gewinner is null ? $"Keine Stimmen ({beteiligung})." : $"Meiste Stimmen: {gewinner.Text} ({gewinner.Stimmen}); {beteiligung}.";
        return new Umfrageergebnis(teilnehmende, umfrage.Berechtigte, quorum, ergebnisse, quorum ? gewinner : null, null,
            quorum ? t : $"Quorum von {umfrage.QuorumProzent} % nicht erreicht ({beteiligung}).");
    }
}
