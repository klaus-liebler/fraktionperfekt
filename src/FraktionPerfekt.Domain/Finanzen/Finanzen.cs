using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Finanzen;

/// <summary>
/// Zwei getrennte Kassen: die Fraktionskasse (Zuwendungen der Stadt, zweckgebunden) und die Wahlkampfkasse
/// (Ortsverband/Partei). Spenden nimmt nur die Wahlkampfkasse an.
/// </summary>
public enum Kasse
{
    Fraktion,
    Wahlkampf,
}

/// <summary>Haushaltsposten mit Ansatz (Budget) je Kasse und Jahr.</summary>
public sealed class Haushaltsposten
{
    private Haushaltsposten()
    {
        Bezeichnung = string.Empty;
    }

    public Haushaltsposten(Kasse kasse, int jahr, string bezeichnung, decimal ansatz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bezeichnung);
        if (ansatz < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ansatz), "Der Ansatz darf nicht negativ sein.");
        }

        Id = Ids.Neu();
        (Kasse, Jahr, Bezeichnung, Ansatz) = (kasse, jahr, bezeichnung.Trim(), ansatz);
    }

    public Guid Id { get; private set; }

    public Kasse Kasse { get; private set; }

    public int Jahr { get; private set; }

    public string Bezeichnung { get; private set; }

    public decimal Ansatz { get; private set; }

    public void AnsatzAendern(decimal ansatz) => Ansatz = ansatz >= 0 ? ansatz : throw new ArgumentOutOfRangeException(nameof(ansatz));
}

public enum Buchungsquelle
{
    Manuell,
    Spende,
    Auslage,
}

/// <summary>Kassenbuchung (Einnahme oder Ausgabe). Buchungen werden nicht geändert, sondern storniert (Gegenbuchung).</summary>
public sealed class Kassenbuchung
{
    private Kassenbuchung()
    {
        Text = string.Empty;
    }

    public Kassenbuchung(Kasse kasse, DateOnly datum, bool einnahme, decimal betrag, Guid? postenId, string text, Guid erfasstVon, DateTimeOffset am,
        Buchungsquelle quelle = Buchungsquelle.Manuell, Guid? quelleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (betrag <= 0 || decimal.Round(betrag, 2) != betrag)
        {
            throw new ArgumentOutOfRangeException(nameof(betrag), "Der Betrag muss positiv sein (höchstens zwei Nachkommastellen).");
        }

        Id = Ids.Neu();
        (Kasse, Datum, Einnahme, Betrag, PostenId, Text, ErfasstVonPersonId, ErfasstAm, Quelle, QuelleId) =
            (kasse, datum, einnahme, betrag, postenId, text.Trim(), erfasstVon, am, quelle, quelleId);
    }

    public Guid Id { get; private set; }

    public Kasse Kasse { get; private set; }

    public DateOnly Datum { get; private set; }

    public bool Einnahme { get; private set; }

    public decimal Betrag { get; private set; }

    public Guid? PostenId { get; private set; }

    public string Text { get; private set; }

    public Guid ErfasstVonPersonId { get; private set; }

    public DateTimeOffset ErfasstAm { get; private set; }

    public Buchungsquelle Quelle { get; private set; }

    public Guid? QuelleId { get; private set; }

    public decimal Wirkung => Einnahme ? Betrag : -Betrag;
}

/// <summary>
/// Spende an die Wahlkampfkasse. Anonyme Spenden sind nur bis 500 Euro zulässig (§ 25 PartG); ab 300 Euro wird eine
/// Zuwendungsbestätigung erwartet.
/// </summary>
public sealed class Spende
{
    public const decimal AnonymGrenze = 500m;

    public const decimal QuittungAb = 300m;

    private Spende()
    {
        Zweck = string.Empty;
    }

    public Spende(Kasse kasse, DateOnly datum, string? spender, string? anschrift, decimal betrag, string zweck, Guid erfasstVon)
    {
        if (kasse != Kasse.Wahlkampf)
        {
            throw new InvalidOperationException("Spenden nimmt nur die Wahlkampfkasse an; die Fraktion finanziert sich aus den Zuwendungen der Stadt.");
        }

        if (betrag <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(betrag), "Der Betrag muss positiv sein.");
        }

        if (string.IsNullOrWhiteSpace(spender) && betrag > AnonymGrenze)
        {
            throw new InvalidOperationException($"Anonyme Spenden über {AnonymGrenze:N0} Euro dürfen nicht angenommen werden (§ 25 PartG).");
        }

        Id = Ids.Neu();
        Kasse = kasse;
        Datum = datum;
        Spender = string.IsNullOrWhiteSpace(spender) ? null : spender.Trim();
        Anschrift = string.IsNullOrWhiteSpace(anschrift) ? null : anschrift.Trim();
        Betrag = betrag;
        Zweck = string.IsNullOrWhiteSpace(zweck) ? "Wahlkampf" : zweck.Trim();
        ErfasstVonPersonId = erfasstVon;
    }

    public Guid Id { get; private set; }

    public Kasse Kasse { get; private set; }

    public DateOnly Datum { get; private set; }

    /// <summary><c>null</c> = anonym.</summary>
    public string? Spender { get; private set; }

    public string? Anschrift { get; private set; }

    public decimal Betrag { get; private set; }

    public string Zweck { get; private set; }

    public Guid ErfasstVonPersonId { get; private set; }

    public DateOnly? QuittungAm { get; private set; }

    public bool QuittungFaellig => Spender is not null && Betrag >= QuittungAb && QuittungAm is null;

    public void QuittungVermerken(DateOnly am) => QuittungAm = Spender is null ? throw new InvalidOperationException("Anonyme Spenden bekommen keine Quittung.") : am;
}

public enum Auslagenstatus
{
    Eingereicht,
    Freigegeben,
    Abgelehnt,
    Ausgezahlt,

    /// <summary>Von der einreichenden Person bzw. dem Kassier vor der Entscheidung zurückgezogen (Korrekturregel).</summary>
    Zurueckgezogen,
}

/// <summary>Auslage (Erstattung) mit Belegfoto: einreichen → Freigabe (nie die eigene) → Auszahlung durch den Kassier.</summary>
public sealed class Auslage
{
    private Auslage()
    {
        Zweck = string.Empty;
    }

    public Auslage(Guid personId, Kasse kasse, DateOnly datum, decimal betrag, string zweck, Guid? postenId, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zweck);
        if (betrag <= 0 || decimal.Round(betrag, 2) != betrag)
        {
            throw new ArgumentOutOfRangeException(nameof(betrag), "Der Betrag muss positiv sein (höchstens zwei Nachkommastellen).");
        }

        Id = Ids.Neu();
        (PersonId, Kasse, Datum, Betrag, Zweck, PostenId, EingereichtAm, Status) = (personId, kasse, datum, betrag, zweck.Trim(), postenId, am, Auslagenstatus.Eingereicht);
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public Kasse Kasse { get; private set; }

    public DateOnly Datum { get; private set; }

    public decimal Betrag { get; private set; }

    public string Zweck { get; private set; }

    public Guid? PostenId { get; private set; }

    public DateTimeOffset EingereichtAm { get; private set; }

    public string? BelegSchluessel { get; private set; }

    public string? BelegName { get; private set; }

    public string? BelegTyp { get; private set; }

    public Auslagenstatus Status { get; private set; }

    public Guid? EntschiedenVonPersonId { get; private set; }

    public string? Vermerk { get; private set; }

    public DateTimeOffset? AusgezahltAm { get; private set; }

    public void BelegSetzen(string schluessel, string name, string typ) => (BelegSchluessel, BelegName, BelegTyp) = (schluessel, name, typ);

    public void Entscheiden(bool freigeben, Guid personId, string? vermerk)
    {
        if (Status != Auslagenstatus.Eingereicht)
        {
            throw new InvalidOperationException("Über die Auslage ist bereits entschieden.");
        }

        if (personId == PersonId)
        {
            throw new InvalidOperationException("Vier-Augen-Prinzip: Die eigene Auslage gibt jemand anderes frei.");
        }

        if (freigeben && BelegSchluessel is null)
        {
            throw new InvalidOperationException("Ohne Beleg keine Erstattung.");
        }

        (Status, EntschiedenVonPersonId, Vermerk) = (freigeben ? Auslagenstatus.Freigegeben : Auslagenstatus.Abgelehnt, personId,
            string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim());
    }

    public void Zurueckziehen(string grund)
    {
        if (Status != Auslagenstatus.Eingereicht)
        {
            throw new InvalidOperationException("Zurückziehen lässt sich eine Auslage nur, solange nicht über sie entschieden ist.");
        }

        (Status, Vermerk) = (Auslagenstatus.Zurueckgezogen, grund);
    }

    public void Auszahlen(DateTimeOffset jetzt)
    {
        if (Status != Auslagenstatus.Freigegeben)
        {
            throw new InvalidOperationException("Nur freigegebene Auslagen werden ausgezahlt.");
        }

        (Status, AusgezahltAm) = (Auslagenstatus.Ausgezahlt, jetzt);
    }
}
