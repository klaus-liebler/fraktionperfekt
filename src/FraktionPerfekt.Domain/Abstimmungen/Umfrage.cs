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

    /// <summary>Wahlgang einer Wahl (Kandidat:innen, ggf. Nein, Enthaltung); stimmberechtigt wie bei Antragsabstimmungen.</summary>
    Wahl,
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
        Guid erstelltVonPersonId, DateTimeOffset jetzt, Guid? antragId = null, bool geheim = false, Guid? wahlId = null, Guid? topId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        if (geheim && art == Umfrageart.Terminfindung)
        {
            throw new ArgumentException("Eine Terminfindung ist nie geheim (wer wann kann, muss sichtbar sein).", nameof(geheim));
        }

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
        Geheim = geheim;
        WahlId = wahlId;
        TopId = topId;
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

    /// <summary>
    /// Geheime Abstimmung: Wer abgestimmt hat (<see cref="Stimmvermerk"/>) und wie (<see cref="Stimmzettel"/>) liegen getrennt, ohne Verbindung;
    /// kein Zwischenstand vor dem Ende, die Stimme ist endgültig.
    /// </summary>
    public bool Geheim { get; private set; }

    /// <summary>Wahlgang dieser Wahl (Modul Wahlen und Ämter).</summary>
    public Guid? WahlId { get; private set; }

    /// <summary>Abstimmung im Sitzungsmodus zu diesem TOP (nur die Anwesenden); das Ergebnis wird Beschluss im Protokoll.</summary>
    public Guid? TopId { get; private set; }

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

    public UmfrageOption(Guid umfrageId, int position, string text, DateTime? zeitpunkt = null, Guid? kandidatPersonId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = Ids.Neu();
        UmfrageId = umfrageId;
        Position = position;
        Text = text.Trim();
        Zeitpunkt = zeitpunkt;
        KandidatPersonId = kandidatPersonId;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public int Position { get; private set; }

    public string Text { get; private set; }

    /// <summary>Bei der Terminfindung: vorgeschlagener Beginn (Wanduhrzeit).</summary>
    public DateTime? Zeitpunkt { get; private set; }

    /// <summary>Bei einem Wahlgang: die Person, für die diese Antwort steht.</summary>
    public Guid? KandidatPersonId { get; private set; }
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

/// <summary>Stimmberechtigte Person einer Abstimmung, beim Start als feste Liste gespeichert (Wählerverzeichnis).</summary>
public sealed class Stimmberechtigung
{
    private Stimmberechtigung()
    {
    }

    public Stimmberechtigung(Guid umfrageId, Guid personId)
    {
        Id = Ids.Neu();
        UmfrageId = umfrageId;
        PersonId = personId;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public Guid PersonId { get; private set; }
}

/// <summary>
/// Geheime Abstimmung: Vermerk, <em>dass</em> eine Person abgestimmt hat (genau einer je Person und Abstimmung). Enthält bewusst keine Option
/// und keinen Verweis auf den Stimmzettel.
/// </summary>
public sealed class Stimmvermerk
{
    private Stimmvermerk()
    {
    }

    public Stimmvermerk(Guid umfrageId, Guid personId, DateTimeOffset am)
    {
        Id = Ids.Neu();
        UmfrageId = umfrageId;
        PersonId = personId;
        Am = am;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }
}

/// <summary>
/// Geheime Abstimmung: der Stimmzettel – <em>wie</em> abgestimmt wurde, ohne Person und ohne Zeitpunkt. Die Id ist zufällig (UUIDv4, nicht
/// <see cref="Ids.Neu"/>), weil eine zeitlich sortierbare Id über den Zeitpunkt des Stimmvermerks die Person verraten würde; die Tabelle ist
/// in SQLite <c>WITHOUT ROWID</c> mit dieser Id als Schlüssel, damit auch die Reihenfolge in der Datei nichts verrät.
/// </summary>
public sealed class Stimmzettel
{
    private Stimmzettel()
    {
    }

    public Stimmzettel(Guid umfrageId, Guid optionId)
    {
        Id = Ids.Zufaellig();
        UmfrageId = umfrageId;
        OptionId = optionId;
    }

    public Guid Id { get; private set; }

    public Guid UmfrageId { get; private set; }

    public Guid OptionId { get; private set; }
}

public sealed record Optionsergebnis(Guid OptionId, string Text, int Stimmen, DateTime? Zeitpunkt);

/// <param name="ZuWenigeStimmen">Geheime Abstimmung mit weniger Stimmen als die Mindestzahl: Verteilung wird nicht gezeigt.</param>
public sealed record Umfrageergebnis(int Teilnehmende, int Berechtigte, bool QuorumErreicht, IReadOnlyList<Optionsergebnis> Optionen, Optionsergebnis? Gewinner,
    bool? Angenommen, string Text, bool ZuWenigeStimmen = false);

public static class Auswertung
{
    /// <summary>Standard für die Mindestzahl an Stimmen, ab der eine geheime Abstimmung ausgewertet wird (⚑ Geschäftsordnung).</summary>
    public const int GeheimMindeststimmenStandard = 3;

    /// <summary>Namentliche Abstimmung: wertet die Stimmen (mit Person) aus.</summary>
    public static Umfrageergebnis Auswerten(Umfrage umfrage, IReadOnlyList<UmfrageOption> optionen, IReadOnlyList<Stimme> stimmen)
    {
        ArgumentNullException.ThrowIfNull(stimmen);
        return Auswerten(umfrage, optionen, stimmen.Select(s => s.PersonId).Distinct().Count(),
            stimmen.GroupBy(s => s.OptionId).ToDictionary(g => g.Key, g => g.Count()));
    }

    /// <summary>
    /// Wertet eine Umfrage aus Teilnahmezahl und Stimmen je Option aus (für geheime Abstimmungen: Zahl der Stimmvermerke und Stimmzettel je
    /// Option). Abstimmungen (Ja/Nein/Enthaltung): angenommen, wenn das Quorum erreicht ist und mehr Ja- als Nein-Stimmen abgegeben wurden.
    /// Sonst gewinnt die Option mit den meisten Stimmen (bei Gleichstand die frühere). Geheim mit weniger als
    /// <paramref name="geheimMindeststimmen"/> Stimmen: keine Verteilung, nicht angenommen.
    /// </summary>
    public static Umfrageergebnis Auswerten(Umfrage umfrage, IReadOnlyList<UmfrageOption> optionen, int teilnehmende, IReadOnlyDictionary<Guid, int> stimmenJeOption,
        int geheimMindeststimmen = GeheimMindeststimmenStandard)
    {
        ArgumentNullException.ThrowIfNull(umfrage);
        ArgumentNullException.ThrowIfNull(optionen);
        ArgumentNullException.ThrowIfNull(stimmenJeOption);
        var quorum = umfrage.QuorumProzent == 0 || (umfrage.Berechtigte > 0 && teilnehmende * 100 >= umfrage.QuorumProzent * umfrage.Berechtigte);
        var beteiligung = umfrage.Berechtigte > 0 ? $"{teilnehmende} von {umfrage.Berechtigte} haben abgestimmt" : $"{teilnehmende} haben abgestimmt";
        if (umfrage.Geheim && teilnehmende < geheimMindeststimmen)
        {
            var leer = optionen.OrderBy(o => o.Position).Select(o => new Optionsergebnis(o.Id, o.Text, 0, o.Zeitpunkt)).ToList();
            var art = umfrage.Art is Umfrageart.Antragsabstimmung or Umfrageart.Meinungsbild ? false : (bool?)null;
            return new Umfrageergebnis(teilnehmende, umfrage.Berechtigte, quorum, leer, null, art,
                $"Zu wenige Stimmen für eine geheime Auswertung ({beteiligung}, mindestens {geheimMindeststimmen} nötig).", ZuWenigeStimmen: true);
        }

        var ergebnisse = optionen.OrderBy(o => o.Position)
            .Select(o => new Optionsergebnis(o.Id, o.Text, stimmenJeOption.GetValueOrDefault(o.Id), o.Zeitpunkt)).ToList();
        var geheim = umfrage.Geheim ? "Geheim abgestimmt. " : "";
        if (umfrage.Art is Umfrageart.Antragsabstimmung or Umfrageart.Meinungsbild)
        {
            int Zahl(string text) => ergebnisse.FirstOrDefault(e => e.Text == text)?.Stimmen ?? 0;
            var (ja, nein, enth) = (Zahl("Ja"), Zahl("Nein"), Zahl("Enthaltung"));
            var angenommen = quorum && ja > nein;
            var text = !quorum
                ? $"{geheim}Quorum von {umfrage.QuorumProzent} % nicht erreicht ({beteiligung})."
                : $"{geheim}{(angenommen ? "Angenommen" : "Abgelehnt")}: {ja} ja, {nein} nein, {enth} Enthaltung ({beteiligung}).";
            return new Umfrageergebnis(teilnehmende, umfrage.Berechtigte, quorum, ergebnisse, null, angenommen, text);
        }

        var gewinner = ergebnisse.Where(e => e.Stimmen > 0).OrderByDescending(e => e.Stimmen).FirstOrDefault();
        var t = gewinner is null ? $"{geheim}Keine Stimmen ({beteiligung})." : $"{geheim}Meiste Stimmen: {gewinner.Text} ({gewinner.Stimmen}); {beteiligung}.";
        return new Umfrageergebnis(teilnehmende, umfrage.Berechtigte, quorum, ergebnisse, quorum ? gewinner : null, null,
            quorum ? t : $"{geheim}Quorum von {umfrage.QuorumProzent} % nicht erreicht ({beteiligung}).");
    }
}
