namespace FraktionPerfekt.Domain.Personen;

/// <summary>Du oder Sie – wie die Person in Oberfläche, Push und Mails angesprochen werden möchte.</summary>
public enum Anredeform
{
    Du,
    Sie,
}

/// <summary>Stil der Begrüßung: locker („Hallo …“), neutral („Guten Tag, …“), förmlich („Liebe/r …“ bzw. „Sehr geehrte/r …“).</summary>
public enum Begruessungsstil
{
    Locker,
    Neutral,
    Foermlich,
}

/// <summary>
/// Formatiert die persönliche Ansprache einer Person nach ihren eigenen Einstellungen (Du/Sie, Stil, Titel). Zentral für
/// Startseite, Push und Mails an Nutzer:innen; Mails an Dritte (Verwaltung, Bürger:innen) sind davon nicht betroffen.
/// Ohne Anrede (keine Angabe/divers) gibt es in der Sie-Form eine neutrale Fassung mit vollem Namen.
/// </summary>
public static class Ansprache
{
    /// <summary>Begrüßung ohne Satzzeichen, z. B. „Hallo Klaus“, „Guten Tag, Herr Dr. Liebler“, „Sehr geehrte Frau Kühlert“.</summary>
    public static string Begruessung(Person p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var vorname = string.IsNullOrEmpty(p.Vorname) ? p.NachnameMitVorsatz : p.Vorname;   // nur ein Name laut Ausweis
        if (p.Anredeform == Anredeform.Du)
        {
            return p.Begruessungsstil switch
            {
                Begruessungsstil.Neutral => $"Guten Tag, {vorname}",
                Begruessungsstil.Foermlich => p.Anrede switch
                {
                    "Frau" => $"Liebe {vorname}",
                    "Herr" => $"Lieber {vorname}",
                    _ => $"Guten Tag, {vorname}",
                },
                _ => $"Hallo {vorname}",
            };
        }

        var titel = p.TitelInAnsprache && p.Titel is { } t ? $"{t} " : string.Empty;
        if (p.Anrede is not ("Herr" or "Frau"))
        {
            // neutrale Form ohne Anrede: voller Name
            return $"Guten Tag {titel}{PersonName.Zusammensetzen(null, p.Vorname, p.Vorsatz, p.Nachname, null)}";
        }

        var name = $"{p.Anrede} {titel}{p.NachnameMitVorsatz}";
        return p.Begruessungsstil switch
        {
            Begruessungsstil.Neutral => $"Guten Tag, {name}",
            Begruessungsstil.Foermlich => $"{(p.Anrede == "Frau" ? "Sehr geehrte" : "Sehr geehrter")} {name}",
            _ => $"Hallo {name}",
        };
    }

    /// <summary>Text in Du- oder Sie-Form je nach Einstellung der Person.</summary>
    public static string DuSie(Person p, string du, string sie)
    {
        ArgumentNullException.ThrowIfNull(p);
        return p.Anredeform == Anredeform.Sie ? sie : du;
    }
}
