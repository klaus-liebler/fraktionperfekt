using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Personen;

/// <summary>Welche Art Kontakt (Adresse, E-Mail oder Rufnummer).</summary>
public enum Kontaktart
{
    Adresse,
    EMail,
    Rufnummer,
}

/// <summary>Privat oder dienstlich (Adresse, E-Mail).</summary>
public enum Kontaktbereich
{
    Privat,
    Dienstlich,
}

/// <summary>
/// Eine Postadresse einer Person (nach VibeVHS). Mehrere Adressen sind möglich; jede hat eine feste <see cref="AdressId"/>, ihre
/// Versionen bilden eine Reihe (Änderung ab Datum, Korrektur, Aufgabe nach Umzug). „Bevorzugt“ = Adresse für Briefe.
/// </summary>
public sealed class Postadresse : BitemporaleVersion<Postadresse>
{
    private Postadresse()
    {
        Strasse = string.Empty;
        Postleitzahl = string.Empty;
        Ort = string.Empty;
    }

    public Postadresse(Guid adressId, Kontaktbereich bereich, string? zusatz, string strasse, string postleitzahl, string ort, bool bevorzugt,
        DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strasse);
        ArgumentException.ThrowIfNullOrWhiteSpace(postleitzahl);
        ArgumentException.ThrowIfNullOrWhiteSpace(ort);
        AdressId = adressId;
        Bereich = bereich;
        Zusatz = string.IsNullOrWhiteSpace(zusatz) ? null : zusatz.Trim();
        Strasse = strasse.Trim();
        Postleitzahl = postleitzahl.Trim();
        Ort = ort.Trim();
        Bevorzugt = bevorzugt;
    }

    public Guid AdressId { get; private set; }

    public Kontaktbereich Bereich { get; private set; }

    /// <summary>Adresszusatz, z. B. „c/o Rathaus, Zimmer 12“.</summary>
    public string? Zusatz { get; private set; }

    public string Strasse { get; private set; }

    public string Postleitzahl { get; private set; }

    public string Ort { get; private set; }

    public bool Bevorzugt { get; private set; }

    public string Anzeige => string.Join(", ", new[] { Zusatz, Strasse, $"{Postleitzahl} {Ort}" }.Where(t => !string.IsNullOrWhiteSpace(t)));

    public override string Datenart => "Adressen";

    public override string Feld => Bereich == Kontaktbereich.Privat ? "Postadresse (privat)" : "Postadresse (dienstlich)";

    public override string Gegenstand => AdressId.ToString("N");

    public override string Wert => IstEndemarke ? "(aufgegeben)" : Anzeige + (Bevorzugt ? " · bevorzugt" : string.Empty);

    protected internal override Postadresse KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(AdressId, Bereich, Zusatz, Strasse, Postleitzahl, Ort, Bevorzugt, gueltigVon, gueltigBis, erfassung);
}

public enum RufnummerArt
{
    Mobil,
    Festnetz,
    Dienstlich,
}

/// <summary>Über welche Dienste eine Nummer erreichbar ist.</summary>
[Flags]
public enum Messagingdienst
{
    Keiner = 0,
    Anruf = 1,
    Sms = 2,
    Signal = 4,
    WhatsApp = 8,
}

/// <summary>Telefon- bzw. Messaging-Nummer mit Einwilligung (Messaging) und Kennzeichen „bevorzugt“ (nach VibeVHS).</summary>
public sealed class Rufnummer : BitemporaleVersion<Rufnummer>
{
    private Rufnummer() => Nummer = string.Empty;

    public Rufnummer(Guid nummerId, RufnummerArt art, string nummer, Messagingdienst dienste, bool einwilligung, bool bevorzugt,
        DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nummer);
        NummerId = nummerId;
        Art = art;
        Nummer = nummer.Trim();
        Dienste = dienste;
        Einwilligung = einwilligung;
        Bevorzugt = bevorzugt;
    }

    public Guid NummerId { get; private set; }

    public RufnummerArt Art { get; private set; }

    public string Nummer { get; private set; }

    public Messagingdienst Dienste { get; private set; }

    /// <summary>Einwilligung zur Kontaktaufnahme über Messaging-Dienste (SMS, Signal, WhatsApp) liegt vor.</summary>
    public bool Einwilligung { get; private set; }

    public bool Bevorzugt { get; private set; }

    public string DiensteAnzeige => Dienste == Messagingdienst.Keiner
        ? "–"
        : string.Join(", ", Enum.GetValues<Messagingdienst>()
            .Where(d => d != Messagingdienst.Keiner && Dienste.HasFlag(d))
            .Select(d => d switch { Messagingdienst.Sms => "SMS", _ => d.ToString() }));

    public override string Datenart => "Telefon";

    public override string Feld => Art switch
    {
        RufnummerArt.Mobil => "Rufnummer (mobil)",
        RufnummerArt.Festnetz => "Rufnummer (Festnetz)",
        _ => "Rufnummer (dienstlich)",
    };

    public override string Gegenstand => NummerId.ToString("N");

    public override string Wert => IstEndemarke
        ? "(abgemeldet)"
        : $"{Nummer} · {DiensteAnzeige}{(Einwilligung ? " · Einwilligung Messaging" : string.Empty)}{(Bevorzugt ? " · bevorzugt" : string.Empty)}";

    protected internal override Rufnummer KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(NummerId, Art, Nummer, Dienste, Einwilligung, Bevorzugt, gueltigVon, gueltigBis, erfassung);
}

/// <summary>E-Mail-Adresse mit Bereich und Kennzeichen „bevorzugt“ (an die bevorzugte gehen Mails der Fraktion).</summary>
public sealed class EMailAdresse : BitemporaleVersion<EMailAdresse>
{
    private EMailAdresse() => Adresse = string.Empty;

    public EMailAdresse(Guid adressId, Kontaktbereich bereich, string adresse, bool bevorzugt, DateOnly gueltigVon, DateOnly? gueltigBis,
        Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adresse);
        if (!adresse.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException($"„{adresse}“ ist keine E-Mail-Adresse.", nameof(adresse));
        }

        AdressId = adressId;
        Bereich = bereich;
        Adresse = adresse.Trim();
        Bevorzugt = bevorzugt;
    }

    public Guid AdressId { get; private set; }

    public Kontaktbereich Bereich { get; private set; }

    public string Adresse { get; private set; }

    public bool Bevorzugt { get; private set; }

    public override string Datenart => "E-Mail";

    public override string Feld => Bereich == Kontaktbereich.Privat ? "E-Mail (privat)" : "E-Mail (dienstlich)";

    public override string Gegenstand => AdressId.ToString("N");

    public override string Wert => IstEndemarke ? "(aufgegeben)" : Adresse + (Bevorzugt ? " · bevorzugt" : string.Empty);

    protected internal override EMailAdresse KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(AdressId, Bereich, Adresse, Bevorzugt, gueltigVon, gueltigBis, erfassung);
}

/// <summary>Wofür eine Einwilligung gilt. VibeVHS kennt Kursorganisation, Werbung und Fotos; für eine Fraktion passen diese drei.</summary>
public enum Einwilligungszweck
{
    /// <summary>Kontaktdaten (Adressen, Rufnummern, E-Mails) sind für alle in der Fraktion sichtbar, nicht nur für Geschäftsführung und die Person selbst.</summary>
    WeitergabeInDerFraktion,

    /// <summary>Newsletter und Informationen der Partei/Fraktion an private Kontaktdaten.</summary>
    Informationen,

    /// <summary>Fotos veröffentlichen (Website, Presse, soziale Medien).</summary>
    Fotos,
}

/// <summary>Einwilligung je Zweck (DSGVO), zeitlich versioniert: erteilt oder nicht erteilt/widerrufen (nach VibeVHS).</summary>
public sealed class Einwilligung : BitemporaleVersion<Einwilligung>
{
    private Einwilligung()
    {
    }

    public Einwilligung(Einwilligungszweck zweck, bool erteilt, DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        : base(gueltigVon, gueltigBis, erfassung)
    {
        Zweck = zweck;
        Erteilt = erteilt;
    }

    public Einwilligungszweck Zweck { get; private set; }

    public bool Erteilt { get; private set; }

    public static string ZweckAnzeige(Einwilligungszweck z) => z switch
    {
        Einwilligungszweck.WeitergabeInDerFraktion => "Kontaktdaten in der Fraktion weitergeben",
        Einwilligungszweck.Informationen => "Newsletter und Informationen",
        _ => "Fotos veröffentlichen",
    };

    public override string Datenart => "Einwilligungen";

    public override string Feld => ZweckAnzeige(Zweck);

    public override string Gegenstand => Zweck.ToString();

    public override string Wert => IstEndemarke ? "(keine Angabe)" : Erteilt ? "erteilt" : "nicht erteilt";

    protected internal override Einwilligung KopieMit(DateOnly gueltigVon, DateOnly? gueltigBis, Erfassung erfassung)
        => new(Zweck, Erteilt, gueltigVon, gueltigBis, erfassung);
}
