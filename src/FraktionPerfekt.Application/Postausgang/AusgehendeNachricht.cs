using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Application.Postausgang;

public enum Kanal
{
    EMail,

    /// <summary>Push-Mitteilung (Web Push, später APNs/FCM in der App-Hülle).</summary>
    Push,
}

public enum NachrichtenStatus
{
    Wartend,
    Zugestellt,
    Fehlgeschlagen,
}

/// <summary>
/// Outbox-Eintrag: Jede Mail und jede Push-Mitteilung wird zuerst hier abgelegt, gemeinsam mit der fachlichen Änderung
/// gespeichert und erst danach von einem Hintergrundjob zugestellt. So geht nichts verloren und alles ist im Postausgang sichtbar.
/// </summary>
public sealed class AusgehendeNachricht
{
    public const int MaxVersuche = 5;

    private AusgehendeNachricht()
    {
        Empfaenger = string.Empty;
        Betreff = string.Empty;
        Inhalt = string.Empty;
        AusgeloestVon = string.Empty;
    }

    public AusgehendeNachricht(Kanal kanal, string empfaenger, string? empfaengerAnzeige, string betreff, string inhalt,
        DateTimeOffset erstelltAm, string ausgeloestVon, Guid? personId = null, string? link = null, string? aktion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(empfaenger);
        ArgumentException.ThrowIfNullOrWhiteSpace(betreff);
        Id = Ids.Neu();
        Kanal = kanal;
        Empfaenger = empfaenger;
        EmpfaengerAnzeige = empfaengerAnzeige;
        Betreff = betreff;
        Inhalt = inhalt;
        ErstelltAm = erstelltAm;
        AusgeloestVon = ausgeloestVon;
        PersonId = personId;
        Link = link;
        Aktion = aktion;
        Status = NachrichtenStatus.Wartend;
    }

    public Guid Id { get; private set; }

    public Kanal Kanal { get; private set; }

    /// <summary>E-Mail-Adresse bzw. bei Push die Kennung des Empfangsgeräts ("person:{id}").</summary>
    public string Empfaenger { get; private set; }

    public string? EmpfaengerAnzeige { get; private set; }

    public string Betreff { get; private set; }

    public string Inhalt { get; private set; }

    /// <summary>Relative Adresse, die die Mitteilung öffnet.</summary>
    public string? Link { get; private set; }

    /// <summary>Beschriftung eines Aktionsknopfs direkt in der Mitteilung (z. B. „Ich vertrete“).</summary>
    public string? Aktion { get; private set; }

    public Guid? PersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public string AusgeloestVon { get; private set; }

    public NachrichtenStatus Status { get; private set; }

    public int Versuche { get; private set; }

    public DateTimeOffset? ZugestelltAm { get; private set; }

    public string? Rueckmeldung { get; private set; }

    public string? Fehler { get; private set; }

    public void AlsZugestelltMarkieren(DateTimeOffset zeitpunkt, string? rueckmeldung)
    {
        Versuche++;
        Status = NachrichtenStatus.Zugestellt;
        ZugestelltAm = zeitpunkt;
        Rueckmeldung = rueckmeldung;
        Fehler = null;
    }

    public void FehlversuchVermerken(string fehler)
    {
        Versuche++;
        Fehler = fehler;
        if (Versuche >= MaxVersuche)
        {
            Status = NachrichtenStatus.Fehlgeschlagen;
        }
    }

    /// <summary>Fehlgeschlagene Nachricht nach Prüfung erneut in die Warteschlange stellen.</summary>
    public void ErneutVersuchen()
    {
        if (Status != NachrichtenStatus.Fehlgeschlagen)
        {
            throw new InvalidOperationException("Nur fehlgeschlagene Nachrichten können erneut versendet werden.");
        }

        Status = NachrichtenStatus.Wartend;
        Versuche = 0;
    }
}
