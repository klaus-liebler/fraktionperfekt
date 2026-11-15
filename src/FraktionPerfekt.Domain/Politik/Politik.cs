using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Politik;

public enum Anfragestatus
{
    Entwurf,
    Eingereicht,
    Beantwortet,
    Zurueckgezogen,
}

/// <summary>
/// Anfrage der Fraktion an die Verwaltung (Modul 13) mit Antwortfrist. Nach Ablauf der Frist erinnert das System einmal
/// und legt eine Aufgabe „nachhaken“ an.
/// </summary>
public sealed class Anfrage
{
    private Anfrage()
    {
        Titel = string.Empty;
        Text = string.Empty;
    }

    public Anfrage(string titel, string text, Guid autorPersonId, Guid? gremiumId, string? thema, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        Titel = titel.Trim();
        Text = text?.Trim() ?? string.Empty;
        AutorPersonId = autorPersonId;
        GremiumId = gremiumId;
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        Status = Anfragestatus.Entwurf;
        ErstelltAm = jetzt;
    }

    public Guid Id { get; private set; }

    public string Titel { get; private set; }

    public string Text { get; private set; }

    public Guid AutorPersonId { get; private set; }

    public Guid? GremiumId { get; private set; }

    public string? Thema { get; private set; }

    public Anfragestatus Status { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateOnly? EingereichtAm { get; private set; }

    /// <summary>Antwortfrist (z. B. vier Wochen nach Eingang laut Geschäftsordnung des Rates).</summary>
    public DateOnly? FristBis { get; private set; }

    public string? Antwort { get; private set; }

    public DateOnly? BeantwortetAm { get; private set; }

    public DateTimeOffset? ErinnertAm { get; private set; }

    public bool Ueberfaellig(DateOnly heute) => Status == Anfragestatus.Eingereicht && FristBis is { } f && f < heute;

    public void Bearbeiten(string titel, string text)
    {
        if (Status != Anfragestatus.Entwurf)
        {
            throw new InvalidOperationException("Eingereichte Anfragen werden nicht mehr geändert.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Titel = titel.Trim();
        Text = text?.Trim() ?? string.Empty;
    }

    public void Einreichen(DateOnly am, DateOnly frist)
    {
        if (Status != Anfragestatus.Entwurf)
        {
            throw new InvalidOperationException("Die Anfrage ist bereits eingereicht.");
        }

        if (frist < am)
        {
            throw new ArgumentException("Die Frist liegt vor dem Einreichen.", nameof(frist));
        }

        (Status, EingereichtAm, FristBis) = (Anfragestatus.Eingereicht, am, frist);
    }

    public void Beantworten(string antwort, DateOnly am)
    {
        if (Status != Anfragestatus.Eingereicht)
        {
            throw new InvalidOperationException("Nur eingereichte Anfragen können beantwortet werden.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(antwort);
        (Status, Antwort, BeantwortetAm) = (Anfragestatus.Beantwortet, antwort.Trim(), am);
    }

    public void Zurueckziehen() => Status = Status == Anfragestatus.Beantwortet
        ? throw new InvalidOperationException("Beantwortete Anfragen bleiben im Archiv.")
        : Anfragestatus.Zurueckgezogen;

    public void ErinnerungVermerken(DateTimeOffset jetzt) => ErinnertAm = jetzt;
}

/// <summary>Kontakt (Modul 14): Verwaltung, Vereine, Presse, Verbände. Nur dienstliche Angaben; Löschen jederzeit.</summary>
public sealed class Kontakt
{
    private Kontakt()
    {
        Name = string.Empty;
    }

    public Kontakt(string name, string? organisation, string? funktion, string? eMail, string? telefon, string? themen, string? notiz, Guid angelegtVon,
        DateTimeOffset jetzt)
    {
        Id = Ids.Neu();
        Name = name;
        AngelegtVonPersonId = angelegtVon;
        AngelegtAm = jetzt;
        Aendern(name, organisation, funktion, eMail, telefon, themen, notiz, jetzt);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Organisation { get; private set; }

    public string? Funktion { get; private set; }

    public string? EMail { get; private set; }

    public string? Telefon { get; private set; }

    /// <summary>Kommagetrennte Themen, z. B. „Schule, Ganztag“.</summary>
    public string? Themen { get; private set; }

    public string? Notiz { get; private set; }

    public Guid AngelegtVonPersonId { get; private set; }

    public DateTimeOffset AngelegtAm { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public void Aendern(string name, string? organisation, string? funktion, string? eMail, string? telefon, string? themen, string? notiz, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Organisation = Leer(organisation);
        Funktion = Leer(funktion);
        EMail = Leer(eMail);
        Telefon = Leer(telefon);
        Themen = Leer(themen);
        Notiz = Leer(notiz);
        GeaendertAm = jetzt;
    }

    private static string? Leer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public enum Anliegenstatus
{
    Neu,
    Zugewiesen,
    InBearbeitung,
    Erledigt,
}

public enum Eingangskanal
{
    Mail,
    Telefon,
    Sprechstunde,
    Brief,
    Persoenlich,
}

/// <summary>
/// Bürgeranliegen (Modul 15): Eingang → Zuweisung an ein Mitglied (gern den Themenpaten) → Bearbeitung → Antwort.
/// Personenbezogene Angaben der Bürger:innen werden nach Erledigung und Löschfrist entfernt.
/// </summary>
public sealed class Buergeranliegen
{
    private Buergeranliegen()
    {
        Betreff = string.Empty;
        Text = string.Empty;
        Name = string.Empty;
    }

    public Buergeranliegen(string betreff, string text, string name, string? kontakt, Eingangskanal kanal, string? thema, DateTimeOffset eingang)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(betreff);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Ids.Neu();
        Betreff = betreff.Trim();
        Text = text?.Trim() ?? string.Empty;
        Name = name.Trim();
        Kontaktweg = string.IsNullOrWhiteSpace(kontakt) ? null : kontakt.Trim();
        Kanal = kanal;
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        Eingang = eingang;
        Status = Anliegenstatus.Neu;
    }

    public Guid Id { get; private set; }

    public string Betreff { get; private set; }

    public string Text { get; private set; }

    public string Name { get; private set; }

    public string? Kontaktweg { get; private set; }

    public Eingangskanal Kanal { get; private set; }

    public string? Thema { get; private set; }

    public DateTimeOffset Eingang { get; private set; }

    public Anliegenstatus Status { get; private set; }

    public Guid? ZugewiesenPersonId { get; private set; }

    public string? Antwort { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public bool Anonymisiert { get; private set; }

    public void Zuweisen(Guid personId)
    {
        if (Status == Anliegenstatus.Erledigt)
        {
            throw new InvalidOperationException("Das Anliegen ist bereits erledigt.");
        }

        (ZugewiesenPersonId, Status) = (personId, Anliegenstatus.Zugewiesen);
    }

    public void InBearbeitung()
    {
        if (Status is Anliegenstatus.Neu or Anliegenstatus.Erledigt)
        {
            throw new InvalidOperationException("Erst zuweisen.");
        }

        Status = Anliegenstatus.InBearbeitung;
    }

    public void Erledigen(string antwort, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(antwort);
        if (Status == Anliegenstatus.Erledigt)
        {
            throw new InvalidOperationException("Das Anliegen ist bereits erledigt.");
        }

        (Antwort, Status, ErledigtAm) = (antwort.Trim(), Anliegenstatus.Erledigt, jetzt);
    }

    /// <summary>Löschfrist: Name und Kontaktweg entfernen, Inhalt bleibt für die politische Auswertung.</summary>
    public void Anonymisieren()
    {
        (Name, Kontaktweg, Anonymisiert) = ("(gelöscht)", null, true);
    }
}

public enum Pressestatus
{
    Entwurf,
    ZurFreigabe,
    Freigegeben,
    Veroeffentlicht,
}

/// <summary>Pressemitteilung (Modul 16) im Redaktionskalender; Freigabe durch den Vorstand nach dem Vier-Augen-Prinzip.</summary>
public sealed class Pressemitteilung
{
    private Pressemitteilung()
    {
        Titel = string.Empty;
        Text = string.Empty;
    }

    public Pressemitteilung(string titel, string text, DateOnly? geplantAm, string? thema, Guid autorPersonId, DateTimeOffset jetzt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        Id = Ids.Neu();
        Titel = titel.Trim();
        Text = text?.Trim() ?? string.Empty;
        GeplantAm = geplantAm;
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        AutorPersonId = autorPersonId;
        ErstelltAm = jetzt;
        GeaendertAm = jetzt;
        Status = Pressestatus.Entwurf;
    }

    public Guid Id { get; private set; }

    public string Titel { get; private set; }

    public string Text { get; private set; }

    public DateOnly? GeplantAm { get; private set; }

    public string? Thema { get; private set; }

    public Guid AutorPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public Pressestatus Status { get; private set; }

    public Guid? FreigegebenVonPersonId { get; private set; }

    public DateTimeOffset? FreigegebenAm { get; private set; }

    public DateTimeOffset? VeroeffentlichtAm { get; private set; }

    /// <summary>Jede Änderung nach der Freigabe setzt auf Entwurf zurück (die Freigabe gilt für genau diesen Text).</summary>
    public void Aendern(string titel, string text, DateOnly? geplantAm, DateTimeOffset jetzt)
    {
        if (Status == Pressestatus.Veroeffentlicht)
        {
            throw new InvalidOperationException("Veröffentlichte Mitteilungen werden nicht mehr geändert.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        var textGeaendert = titel.Trim() != Titel || (text?.Trim() ?? string.Empty) != Text;
        (Titel, Text, GeplantAm, GeaendertAm) = (titel.Trim(), text?.Trim() ?? string.Empty, geplantAm, jetzt);
        if (textGeaendert && Status is Pressestatus.Freigegeben or Pressestatus.ZurFreigabe)
        {
            (Status, FreigegebenVonPersonId, FreigegebenAm) = (Pressestatus.Entwurf, null, null);
        }
    }

    public void ZurFreigabe()
    {
        if (Status != Pressestatus.Entwurf)
        {
            throw new InvalidOperationException("Nur Entwürfe gehen zur Freigabe.");
        }

        Status = Pressestatus.ZurFreigabe;
    }

    public void Freigeben(Guid personId, DateTimeOffset jetzt)
    {
        if (Status != Pressestatus.ZurFreigabe)
        {
            throw new InvalidOperationException("Die Mitteilung liegt nicht zur Freigabe vor.");
        }

        if (personId == AutorPersonId)
        {
            throw new InvalidOperationException("Vier-Augen-Prinzip: Die eigene Mitteilung gibt jemand anderes frei.");
        }

        (Status, FreigegebenVonPersonId, FreigegebenAm) = (Pressestatus.Freigegeben, personId, jetzt);
    }

    public void Zurueckweisen() => Status = Status == Pressestatus.ZurFreigabe ? Pressestatus.Entwurf : throw new InvalidOperationException("Nicht zur Freigabe.");

    public void Veroeffentlichen(DateTimeOffset jetzt)
    {
        if (Status != Pressestatus.Freigegeben)
        {
            throw new InvalidOperationException("Erst nach der Freigabe veröffentlichen.");
        }

        (Status, VeroeffentlichtAm) = (Pressestatus.Veroeffentlicht, jetzt);
    }
}

public enum Tonlage
{
    Positiv,
    Neutral,
    Kritisch,
}

/// <summary>Eintrag im Pressespiegel: Artikel über die Fraktion oder ihre Themen.</summary>
public sealed class Presseartikel
{
    private Presseartikel()
    {
        Medium = string.Empty;
        Titel = string.Empty;
    }

    public Presseartikel(string medium, string titel, DateOnly datum, string? link, string? zusammenfassung, string? thema, Tonlage tonlage, Guid? pressemitteilungId,
        Guid erfasstVon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medium);
        ArgumentException.ThrowIfNullOrWhiteSpace(titel);
        if (link is { Length: > 0 } l && !(l.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || l.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Der Link muss mit http:// oder https:// beginnen.", nameof(link));
        }

        Id = Ids.Neu();
        Medium = medium.Trim();
        Titel = titel.Trim();
        Datum = datum;
        Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        Zusammenfassung = string.IsNullOrWhiteSpace(zusammenfassung) ? null : zusammenfassung.Trim();
        Thema = string.IsNullOrWhiteSpace(thema) ? null : thema.Trim();
        Tonlage = tonlage;
        PressemitteilungId = pressemitteilungId;
        ErfasstVonPersonId = erfasstVon;
    }

    public Guid Id { get; private set; }

    public string Medium { get; private set; }

    public string Titel { get; private set; }

    public DateOnly Datum { get; private set; }

    public string? Link { get; private set; }

    public string? Zusammenfassung { get; private set; }

    public string? Thema { get; private set; }

    public Tonlage Tonlage { get; private set; }

    /// <summary>Auf welche eigene Mitteilung der Artikel zurückgeht (Resonanz).</summary>
    public Guid? PressemitteilungId { get; private set; }

    public Guid ErfasstVonPersonId { get; private set; }
}
