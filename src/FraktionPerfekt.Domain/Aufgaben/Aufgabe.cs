using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Aufgaben;

public enum Aufgabenstatus
{
    Offen,
    Erledigt,
}

/// <summary>
/// Aufgabe aus Protokoll, Antrag, Anliegen, Anfrage oder Automatik (z. B. Eskalation einer Vertretung). Sie liegt im Korb
/// einer Rolle (<see cref="Zustaendig"/>), bis jemand sie übernimmt oder zugewiesen bekommt (<see cref="ZugewiesenPersonId"/>),
/// oder sie ist von Anfang an einer Person zugewiesen. Ein Anlass erzeugt höchstens eine offene Aufgabe.
/// </summary>
public sealed class Aufgabe
{
    private Aufgabe()
    {
        Titel = string.Empty;
        Anlass = string.Empty;
        Ziel = string.Empty;
    }

    public Aufgabe(string titel, string? beschreibung, Rolle? zustaendig, string anlass, Guid anlassId, string ziel, DateTimeOffset erstelltAm,
        DateTimeOffset? faelligAm, Guid? zugewiesenPersonId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        ArgumentException.ThrowIfNullOrWhiteSpace(anlass);
        if (zustaendig is null && zugewiesenPersonId is null)
        {
            throw new ArgumentException("Eine Aufgabe braucht einen Korb (Rolle) oder eine zuständige Person.", nameof(zustaendig));
        }

        Id = Ids.Neu();
        Titel = titel.Trim();
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        Zustaendig = zustaendig;
        ZugewiesenPersonId = zugewiesenPersonId;
        Anlass = anlass;
        AnlassId = anlassId;
        Ziel = ziel;
        ErstelltAm = erstelltAm;
        FaelligAm = faelligAm;
        Status = Aufgabenstatus.Offen;
    }

    public Guid Id { get; private set; }

    public string Titel { get; private set; }

    public string? Beschreibung { get; private set; }

    /// <summary>Korb: Rolle, die sich kümmert (<c>null</c> = nur persönlich).</summary>
    public Rolle? Zustaendig { get; private set; }

    /// <summary>Wer die Aufgabe bearbeitet (übernommen oder zugewiesen).</summary>
    public Guid? ZugewiesenPersonId { get; private set; }

    /// <summary>Art des Anlasses, z. B. "Vertretung", "Protokoll", "Antrag", "Anliegen", "Anfrage".</summary>
    public string Anlass { get; private set; }

    public Guid AnlassId { get; private set; }

    /// <summary>Relative Adresse, die zum Vorgang führt.</summary>
    public string Ziel { get; private set; }

    /// <summary>Politikfeld für Themenpaten (optional).</summary>
    public string? Thema { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset? FaelligAm { get; private set; }

    public Aufgabenstatus Status { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public string? ErledigtVon { get; private set; }

    public string? Vermerk { get; private set; }

    /// <summary>Wann an die Frist erinnert wurde (einmalig).</summary>
    public DateTimeOffset? ErinnertAm { get; private set; }

    public bool IstOffen => Status == Aufgabenstatus.Offen;

    public bool Ueberfaellig(DateTimeOffset jetzt) => IstOffen && FaelligAm is { } f && f < jetzt;

    public Aufgabe MitThema(string? thema)
    {
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        return this;
    }

    /// <summary>Aus dem Korb in die eigene Liste.</summary>
    public void Uebernehmen(Guid personId)
    {
        OffenPruefen();
        if (ZugewiesenPersonId is { } p && p != personId)
        {
            throw new InvalidOperationException("Die Aufgabe bearbeitet bereits jemand anderes.");
        }

        ZugewiesenPersonId = personId;
    }

    public void Zuweisen(Guid personId)
    {
        OffenPruefen();
        ZugewiesenPersonId = personId;
        ErinnertAm = null;
    }

    /// <summary>Zurück in den Korb der Rolle (nur möglich, wenn es einen Korb gibt).</summary>
    public void ZurueckInKorb()
    {
        OffenPruefen();
        if (Zustaendig is null)
        {
            throw new InvalidOperationException("Diese Aufgabe gehört keinem Korb; bitte einer Person zuweisen.");
        }

        ZugewiesenPersonId = null;
    }

    public void FristSetzen(DateTimeOffset? frist)
    {
        FaelligAm = frist;
        ErinnertAm = null;
    }

    public void ErinnerungVermerken(DateTimeOffset jetzt) => ErinnertAm = jetzt;

    public void Erledigen(DateTimeOffset jetzt, string von, string? vermerk)
    {
        if (Status == Aufgabenstatus.Erledigt)
        {
            return;
        }

        Status = Aufgabenstatus.Erledigt;
        ErledigtAm = jetzt;
        ErledigtVon = von;
        Vermerk = string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim();
    }

    private void OffenPruefen()
    {
        if (!IstOffen)
        {
            throw new InvalidOperationException("Die Aufgabe ist bereits erledigt.");
        }
    }
}

/// <summary>Themenpate: zuständig für ein Politikfeld (z. B. „Schule und Bildung“); bekommt passende Aufgaben und Anliegen.</summary>
public sealed class Themenpate
{
    private Themenpate()
    {
        Politikfeld = string.Empty;
    }

    public Themenpate(string politikfeld, Guid personId, Guid? vertretungPersonId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(politikfeld);
        Id = Ids.Neu();
        Politikfeld = politikfeld.Trim();
        PersonId = personId;
        VertretungPersonId = vertretungPersonId;
    }

    public Guid Id { get; private set; }

    public string Politikfeld { get; private set; }

    public Guid PersonId { get; private set; }

    public Guid? VertretungPersonId { get; private set; }

    public void Aendern(Guid personId, Guid? vertretungPersonId)
    {
        PersonId = personId;
        VertretungPersonId = vertretungPersonId;
    }
}
