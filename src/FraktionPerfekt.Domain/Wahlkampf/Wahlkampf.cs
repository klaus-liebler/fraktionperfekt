using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Wahlkampf;

public enum Plakatart
{
    Hohlkammer,
    Dreieck,
    Grossflaeche,
}

public enum Plakatstatus
{
    Geplant,
    Haengt,
    Beschaedigt,
    Abgehaengt,
}

/// <summary>
/// Plakatstandort (Modul 17) mit Koordinaten, Foto und Status. Am Handy erfasst (Standort per GPS, Foto mit der Kamera);
/// nach der Wahl entsteht daraus die Abhängliste.
/// </summary>
public sealed class Plakat
{
    private Plakat()
    {
        Standort = string.Empty;
    }

    public Plakat(double breite, double laenge, string standort, Plakatart art, Plakatstatus status, Guid erfasstVon, DateTimeOffset jetzt, string? motiv = null)
    {
        if (breite is < -90 or > 90 || laenge is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(breite), "Ungültige Koordinaten.");
        }

        Id = Ids.Neu();
        Breite = breite;
        Laenge = laenge;
        Standort = string.IsNullOrWhiteSpace(standort) ? $"{breite:F5}, {laenge:F5}" : standort.Trim();
        Art = art;
        Motiv = string.IsNullOrWhiteSpace(motiv) ? null : motiv.Trim();
        Status = status;
        ErfasstVonPersonId = erfasstVon;
        ErfasstAm = jetzt;
        GeaendertAm = jetzt;
        GeaendertVonPersonId = erfasstVon;
    }

    public Guid Id { get; private set; }

    public double Breite { get; private set; }

    public double Laenge { get; private set; }

    public string Standort { get; private set; }

    public Plakatart Art { get; private set; }

    public string? Motiv { get; private set; }

    public Plakatstatus Status { get; private set; }

    public string? FotoSchluessel { get; private set; }

    public string? FotoTyp { get; private set; }

    public Guid ErfasstVonPersonId { get; private set; }

    public DateTimeOffset ErfasstAm { get; private set; }

    public Guid GeaendertVonPersonId { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public bool HaengtNoch => Status is Plakatstatus.Haengt or Plakatstatus.Beschaedigt;

    public void StatusSetzen(Plakatstatus status, Guid personId, DateTimeOffset jetzt)
        => (Status, GeaendertVonPersonId, GeaendertAm) = (status, personId, jetzt);

    public void FotoSetzen(string schluessel, string typ) => (FotoSchluessel, FotoTyp) = (schluessel, typ);

    public void FotoEntfernen() => (FotoSchluessel, FotoTyp) = (null, null);

    /// <summary>Entfernung in Metern (Haversine), für „nächste Plakate“ und die Abhängliste.</summary>
    public double EntfernungMeter(double breite, double laenge)
    {
        const double r = 6_371_000;
        var dB = (breite - Breite) * Math.PI / 180;
        var dL = (laenge - Laenge) * Math.PI / 180;
        var a = Math.Sin(dB / 2) * Math.Sin(dB / 2) + Math.Cos(Breite * Math.PI / 180) * Math.Cos(breite * Math.PI / 180) * Math.Sin(dL / 2) * Math.Sin(dL / 2);
        return 2 * r * Math.Asin(Math.Sqrt(a));
    }
}

public enum Gebietsart
{
    Plakatieren,
    Haustuer,
    Flyer,
}

public enum Gebietsstatus
{
    Offen,
    Zugeteilt,
    Erledigt,
}

/// <summary>Gebiet (Straßenzüge, Ortsteil) zum Plakatieren, für Haustürbesuche oder Flyer; wird Personen zugeteilt.</summary>
public sealed class Gebiet
{
    private Gebiet()
    {
        Name = string.Empty;
    }

    public Gebiet(string name, Gebietsart art, string? beschreibung, double? breite, double? laenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        Name = name.Trim();
        Art = art;
        Beschreibung = string.IsNullOrWhiteSpace(beschreibung) ? null : beschreibung.Trim();
        (Breite, Laenge) = (breite, laenge);
        Status = Gebietsstatus.Offen;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Gebietsart Art { get; private set; }

    public string? Beschreibung { get; private set; }

    public double? Breite { get; private set; }

    public double? Laenge { get; private set; }

    public Gebietsstatus Status { get; private set; }

    public Guid? ZugeteiltPersonId { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public string? Vermerk { get; private set; }

    public void Zuteilen(Guid? personId)
    {
        if (Status == Gebietsstatus.Erledigt)
        {
            throw new InvalidOperationException("Das Gebiet ist bereits erledigt.");
        }

        (ZugeteiltPersonId, Status) = (personId, personId is null ? Gebietsstatus.Offen : Gebietsstatus.Zugeteilt);
    }

    public void Erledigen(DateTimeOffset jetzt, string? vermerk)
    {
        if (ZugeteiltPersonId is null)
        {
            throw new InvalidOperationException("Erst zuteilen.");
        }

        (Status, ErledigtAm, Vermerk) = (Gebietsstatus.Erledigt, jetzt, string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim());
    }

    public void Wiedereroeffnen() => (Status, ErledigtAm) = (ZugeteiltPersonId is null ? Gebietsstatus.Offen : Gebietsstatus.Zugeteilt, null);
}

/// <summary>Schicht am Infostand: Zeitraum, Ort, Plätze. Helfer:innen tragen sich selbst ein.</summary>
public sealed class Schicht
{
    private Schicht()
    {
        Ort = string.Empty;
    }

    public Schicht(string ort, DateTime von, DateTime bis, int plaetze, Guid? terminId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ort);
        if (bis <= von)
        {
            throw new ArgumentException("Das Ende liegt vor dem Beginn.", nameof(bis));
        }

        if (plaetze is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(plaetze), "1 bis 20 Plätze.");
        }

        Id = Ids.Neu();
        Ort = ort.Trim();
        (Von, Bis, Plaetze, TerminId) = (von, bis, plaetze, terminId);
    }

    public Guid Id { get; private set; }

    public string Ort { get; private set; }

    /// <summary>Wanduhrzeit Greven.</summary>
    public DateTime Von { get; private set; }

    public DateTime Bis { get; private set; }

    public int Plaetze { get; private set; }

    public Guid? TerminId { get; private set; }
}

public sealed class Schichteintrag
{
    private Schichteintrag()
    {
    }

    public Schichteintrag(Guid schichtId, Guid personId, DateTimeOffset am)
    {
        Id = Ids.Neu();
        (SchichtId, PersonId, Am) = (schichtId, personId, am);
    }

    public Guid Id { get; private set; }

    public Guid SchichtId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }
}
