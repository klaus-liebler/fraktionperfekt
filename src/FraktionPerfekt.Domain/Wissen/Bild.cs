using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Wissen;

/// <summary>
/// Bild im Wiki (eingefügt per Zwischenablage, Ziehen oder Hochladen). Der Inhalt liegt im Dateispeicher unter
/// „Bilder/{Guid}.{Endung}“ – nie unter dem Originalnamen; der Originalname ist nur Metadatum (Alt-Text, Mediathek).
/// Gleiche Inhalte (SHA-256) werden nur einmal gespeichert; wo ein Bild benutzt wird, steht in <see cref="BildVerwendung"/>.
/// </summary>
public sealed class Bild
{
    private Bild()
    {
        Schluessel = string.Empty;
        OriginalName = string.Empty;
        Typ = string.Empty;
        Sha256 = string.Empty;
    }

    public Bild(string originalName, string typ, string endung, long groesse, int breite, int hoehe, string sha256, Guid hochgeladenVon, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(typ);
        Id = Ids.Neu();
        Schluessel = $"Bilder/{Id:N}{endung}";
        OriginalName = originalName;
        Typ = typ;
        Groesse = groesse;
        Breite = breite;
        Hoehe = hoehe;
        Sha256 = sha256;
        HochgeladenVonPersonId = hochgeladenVon;
        Am = am;
    }

    /// <summary>GUID v7 – zugleich Dateiname im Speicher und Teil der Adresse <c>dateien/bild/{Id}</c>.</summary>
    public Guid Id { get; private set; }

    public string Schluessel { get; private set; }

    /// <summary>Name der Originaldatei; bei eingefügten Bildern ohne Namen „Eingefügt JJJJ-MM-TT HH-MM.png“.</summary>
    public string OriginalName { get; private set; }

    public string Typ { get; private set; }

    public long Groesse { get; private set; }

    public int Breite { get; private set; }

    public int Hoehe { get; private set; }

    public string Sha256 { get; private set; }

    public Guid HochgeladenVonPersonId { get; private set; }

    public DateTimeOffset Am { get; private set; }

    public DateTimeOffset? GeloeschtAm { get; private set; }

    public void Loeschen(DateTimeOffset jetzt) => GeloeschtAm ??= jetzt;

    /// <summary>Adresse im Markdown (relativ zur Basisadresse der Anwendung).</summary>
    public string Adresse => $"dateien/bild/{Id}";
}

/// <summary>Ein Bild wird auf einer Seite (oder einem anderen Bezug) benutzt; daraus folgen die Leserechte.</summary>
public sealed class BildVerwendung
{
    private BildVerwendung() => BezugArt = string.Empty;

    public BildVerwendung(Guid bildId, string bezugArt, Guid bezugId, DateTimeOffset am)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bezugArt);
        Id = Ids.Neu();
        BildId = bildId;
        BezugArt = bezugArt;
        BezugId = bezugId;
        Am = am;
    }

    public Guid Id { get; private set; }

    public Guid BildId { get; private set; }

    /// <summary>„Wiki“ (weitere Bezüge später, z. B. Anträge).</summary>
    public string BezugArt { get; private set; }

    public Guid BezugId { get; private set; }

    public DateTimeOffset Am { get; private set; }
}
