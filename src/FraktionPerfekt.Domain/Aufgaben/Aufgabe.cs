using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Aufgaben;

public enum Aufgabenstatus
{
    Offen,
    Erledigt,
}

/// <summary>
/// Aufgabe im Korb einer Rolle (z. B. Vorstand: „Vertretung für Bauausschuss am 03.12. offen“). Der Anlass verweist auf den
/// fachlichen Vorgang; ein Anlass erzeugt höchstens eine offene Aufgabe.
/// </summary>
public sealed class Aufgabe
{
    private Aufgabe()
    {
        Titel = string.Empty;
        Anlass = string.Empty;
        Ziel = string.Empty;
    }

    public Aufgabe(string titel, string? beschreibung, Rolle zustaendig, string anlass, Guid anlassId, string ziel, DateTimeOffset erstelltAm,
        DateTimeOffset? faelligAm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        ArgumentException.ThrowIfNullOrWhiteSpace(anlass);
        Id = Ids.Neu();
        Titel = titel;
        Beschreibung = beschreibung;
        Zustaendig = zustaendig;
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

    /// <summary>Korb: Rolle, die sich kümmert.</summary>
    public Rolle Zustaendig { get; private set; }

    /// <summary>Art des Anlasses, z. B. "Vertretung".</summary>
    public string Anlass { get; private set; }

    public Guid AnlassId { get; private set; }

    /// <summary>Relative Adresse, die zum Vorgang führt.</summary>
    public string Ziel { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset? FaelligAm { get; private set; }

    public Aufgabenstatus Status { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public string? ErledigtVon { get; private set; }

    public string? Vermerk { get; private set; }

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
}
