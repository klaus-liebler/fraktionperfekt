namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Einheitliche Korrekturregel (aus SchoolCraft FA-3235): Wer etwas eingetragen hat, darf es 30 Minuten lang ohne Begründung zurückziehen; danach – und
/// für die Zuständigen (Vorstand, Sitzungsleitung, Kassier) jederzeit – nur mit Begründung. Der ursprüngliche Eintrag bleibt erhalten (Storno statt
/// Löschen). Gilt für Beschlüsse im Sitzungsmodus, Kommentare an Anträgen und Auslagen vor der Entscheidung – <b>nicht</b> für Kassenbuchungen.
/// </summary>
public static class Korrekturregel
{
    public static readonly TimeSpan Fenster = TimeSpan.FromMinutes(30);

    /// <summary>Wirft eine verständliche Ausnahme, wenn das Zurückziehen nicht zulässig ist.</summary>
    public static void Pruefen(bool eigener, DateTimeOffset? eingetragen, DateTimeOffset jetzt, bool zustaendig, string? grund, string wer)
    {
        if (!eigener && !zustaendig)
        {
            throw new InvalidOperationException($"Zurückziehen dürfen {wer}.");
        }

        if (!OhneGrund(eigener, eingetragen, jetzt) && string.IsNullOrWhiteSpace(grund))
        {
            throw new InvalidOperationException(
                $"Nach {Fenster.TotalMinutes:0} Minuten (oder für fremde Einträge) braucht das Zurückziehen eine Begründung; der ursprüngliche Eintrag bleibt erhalten.");
        }
    }

    /// <summary>Ob die Person noch ohne Begründung zurückziehen darf (für die Anzeige).</summary>
    public static bool OhneGrund(bool eigener, DateTimeOffset? eingetragen, DateTimeOffset jetzt) => eigener && eingetragen is { } am && jetzt <= am + Fenster;

    public static string Grund(string? grund) => string.IsNullOrWhiteSpace(grund) ? "Fehleingabe, von der eintragenden Person zurückgezogen" : grund.Trim();
}
