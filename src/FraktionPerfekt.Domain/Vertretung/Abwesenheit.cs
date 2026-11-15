using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Vertretung;

/// <summary>
/// Abwesenheit einer Person (Urlaub, beruflich …). Der Grund ist optional und privat: Er kann Gesundheitsdaten enthalten
/// („krank“) und ist nur für die Person selbst und die Administration sichtbar; er erscheint nie im Protokoll oder in Nachrichten.
/// </summary>
public sealed class Abwesenheit
{
    private Abwesenheit()
    {
    }

    public Abwesenheit(Guid personId, DateOnly von, DateOnly bis, string? grund, DateTimeOffset erfasstAm)
    {
        if (bis < von)
        {
            throw new ArgumentException("„Bis“ darf nicht vor „von“ liegen.", nameof(bis));
        }

        Id = Ids.Neu();
        PersonId = personId;
        Von = von;
        Bis = bis;
        Grund = string.IsNullOrWhiteSpace(grund) ? null : grund.Trim();
        ErfasstAm = erfasstAm;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    /// <summary>Erster Tag der Abwesenheit.</summary>
    public DateOnly Von { get; private set; }

    /// <summary>Letzter Tag der Abwesenheit (einschließlich).</summary>
    public DateOnly Bis { get; private set; }

    /// <summary>Privat: nur für die Person selbst und die Administration.</summary>
    public string? Grund { get; private set; }

    public DateTimeOffset ErfasstAm { get; private set; }

    public DateTimeOffset? ZurueckgezogenAm { get; private set; }

    public bool Aktiv => ZurueckgezogenAm is null;

    public bool Umfasst(DateOnly tag) => Aktiv && Von <= tag && tag <= Bis;

    /// <summary>„Ich bin doch da“: Die Abwesenheit gilt nicht mehr; offene Vertretungsanfragen werden zurückgezogen.</summary>
    public void Zurueckziehen(DateTimeOffset jetzt) => ZurueckgezogenAm ??= jetzt;
}
