using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Dokumentation;

/// <summary>
/// Anmerkung von Administration oder Vorstand zu einem Abschnitt der Hilfe, des Konzepts oder der Doku (oder Antwort darauf).
/// Verfasser als Personenverweis (Name zur Laufzeit); der Abschnittstitel ist bewusst eingefroren, weil sich Überschriften
/// in einer späteren Fassung des Dokuments ändern können.
/// </summary>
public sealed class Kommentar
{
    public const int TextMaxZeichen = 4000;

    private Kommentar()
    {
        Dokument = Abschnitt = AbschnittTitel = Text = string.Empty;
    }

    public Kommentar(string dokument, string abschnitt, string abschnittTitel, string text, Guid? verfasstVonPersonId, DateTimeOffset am, Guid? antwortAuf = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dokument);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Bitte einen Text eingeben.", nameof(text));
        }

        if (text.Trim().Length > TextMaxZeichen)
        {
            throw new ArgumentException($"Der Kommentar ist zu lang (höchstens {TextMaxZeichen} Zeichen).", nameof(text));
        }

        Id = Ids.Neu();
        Dokument = dokument.Trim();
        Abschnitt = (abschnitt ?? string.Empty).Trim();
        AbschnittTitel = (abschnittTitel ?? string.Empty).Trim();
        Text = text.Trim().ReplaceLineEndings("\n");
        VerfasstVonPersonId = verfasstVonPersonId;
        Am = am;
        AntwortAuf = antwortAuf;
    }

    public Guid Id { get; private set; }

    /// <summary>Schlüssel des Dokuments, z. B. „hilfe“ oder „doku/betrieb“.</summary>
    public string Dokument { get; private set; }

    /// <summary>Anker der Überschrift; leer = Dokument als Ganzes.</summary>
    public string Abschnitt { get; private set; }

    public string AbschnittTitel { get; private set; }

    public string Text { get; private set; }

    public Guid? VerfasstVonPersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    /// <summary>Antwort auf diesen Kommentar (eine Ebene: Antworten hängen am ersten Kommentar des Fadens).</summary>
    public Guid? AntwortAuf { get; private set; }

    public bool Erledigt { get; private set; }

    public Guid? ErledigtVonPersonId { get; private set; }

    public DateTimeOffset? ErledigtAm { get; private set; }

    public void ErledigtSetzen(bool erledigt, Guid? personId, DateTimeOffset am)
    {
        if (AntwortAuf is not null)
        {
            throw new InvalidOperationException("Nur der erste Kommentar eines Fadens wird als erledigt markiert.");
        }

        Erledigt = erledigt;
        ErledigtVonPersonId = erledigt ? personId : null;
        ErledigtAm = erledigt ? am : null;
    }
}

/// <summary>
/// Impressum der Fraktion (Anbieterkennzeichnung, § 5 DDG) als Markdown; öffentlich unter /impressum.
/// Genau ein Eintrag (<see cref="EinzigeId"/>); leer = die Seite zeigt einen deutlich markierten Platzhalter.
/// </summary>
public sealed class Impressum
{
    public static readonly Guid EinzigeId = new("00000000-0000-7000-8000-000000000001");

    public const int MaxZeichen = 8000;

    private Impressum()
    {
    }

    public Impressum(DateTimeOffset am) => (Id, GeaendertAm) = (EinzigeId, am);

    public Guid Id { get; private set; }

    public string? Text { get; private set; }

    public DateTimeOffset GeaendertAm { get; private set; }

    public Guid? GeaendertVonPersonId { get; private set; }

    public void Setzen(string? text, Guid? personId, DateTimeOffset am)
    {
        if (text is { Length: > MaxZeichen })
        {
            throw new ArgumentException($"Das Impressum ist zu lang (höchstens {MaxZeichen} Zeichen).", nameof(text));
        }

        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim().ReplaceLineEndings("\n");
        GeaendertVonPersonId = personId;
        GeaendertAm = am;
    }
}
