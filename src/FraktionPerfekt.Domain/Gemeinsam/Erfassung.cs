namespace FraktionPerfekt.Domain.Gemeinsam;

/// <summary>Wie eine Version entstanden ist.</summary>
public enum Erfassungsart
{
    /// <summary>Erste Erfassung eines Werts.</summary>
    Ersterfassung,

    /// <summary>Neuer Wert ab einem Datum; die Vergangenheit bleibt (z. B. Heirat, Umzug).</summary>
    AenderungAbDatum,

    /// <summary>Der Wert war schon immer falsch erfasst und wird für seinen ganzen Zeitraum ersetzt.</summary>
    Korrektur,

    /// <summary>Der Wert gilt ab einem Datum nicht mehr (z. B. Nummer abgemeldet, Adresse aufgegeben).</summary>
    Beendet,

    /// <summary>Technische Abgrenzung: alter Wert mit verkürzter Gültigkeit nach einer Änderung ab Datum.</summary>
    Abgrenzung,
}

/// <summary>
/// Änderungsvorgang: wann, von wem, wie und warum Versionen erfasst wurden (Tabelle Aenderungsvorgaenge, Datenbank-Regel 3). Alle
/// Versionen eines Speichervorgangs verweisen auf denselben Vorgang; <see cref="ProtokollId"/> verbindet ihn mit dem Eintrag im
/// Audit-Protokoll desselben Speichervorgangs (eine Stelle für „wer hat wann was warum geändert“). Jede Abwandlung mit <c>with</c>
/// (z. B. Art = Korrektur) ist ein eigener Vorgang mit eigener Id.
/// </summary>
public sealed record Erfassung(DateTimeOffset Am, string Von, Erfassungsart Art, string Grund)
{
    public const string System = "System";

    /// <summary>Kennung des Vorgangs.</summary>
    public Guid Id { get; private init; } = Ids.Neu();

    /// <summary>Eintrag im Audit-Protokoll aus demselben Speichervorgang (falls protokolliert).</summary>
    public Guid? ProtokollId { get; private set; }

    /// <summary>Abwandlungen (<c>with</c>) sind neue Vorgänge: neue Id, ohne Protokollverweis.</summary>
    private Erfassung(Erfassung original)
    {
        ArgumentNullException.ThrowIfNull(original);
        (Am, Von, Art, Grund) = (original.Am, original.Von, original.Art, original.Grund);
        Id = Ids.Neu();
    }

    /// <summary>Verbindet den Vorgang mit dem Protokolleintrag desselben Speichervorgangs.</summary>
    public void ProtokollZuordnen(Guid protokollId) => ProtokollId ??= protokollId;

    public static Erfassung Erst(DateTimeOffset am, string? von = null, string grund = "Ersterfassung")
        => new(am, von ?? System, Erfassungsart.Ersterfassung, grund);

    public static Erfassung Aenderung(DateTimeOffset am, string grund, string? von = null)
        => new(am, von ?? System, Erfassungsart.AenderungAbDatum, grund);

    public static Erfassung Korrektur(DateTimeOffset am, string grund, string? von = null)
        => new(am, von ?? System, Erfassungsart.Korrektur, grund);

    public static Erfassung Ende(DateTimeOffset am, string grund, string? von = null)
        => new(am, von ?? System, Erfassungsart.Beendet, grund);

    public static string Anzeigename(Erfassungsart art) => art switch
    {
        Erfassungsart.Ersterfassung => "Ersterfassung",
        Erfassungsart.AenderungAbDatum => "Änderung ab Datum",
        Erfassungsart.Korrektur => "Korrektur",
        Erfassungsart.Beendet => "Beendet",
        Erfassungsart.Abgrenzung => "Abgrenzung",
        _ => art.ToString(),
    };
}
