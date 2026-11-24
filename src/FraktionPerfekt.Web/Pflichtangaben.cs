namespace FraktionPerfekt.Web;

/// <summary>Was in einem Anlegen-Dialog noch fehlt (für „OK nennt, was fehlt“, aus SchoolCraft FA-3229).</summary>
public static class Pflichtangaben
{
    /// <summary>Bezeichnungen der Angaben, die nicht erfüllt sind, in der übergebenen Reihenfolge.</summary>
    public static IReadOnlyList<string> Fehlend(params (string Bezeichnung, bool Erfuellt)[] angaben)
        => angaben.Where(a => !a.Erfuellt).Select(a => a.Bezeichnung).ToList();

    public static bool Text(string? wert) => !string.IsNullOrWhiteSpace(wert);
}
