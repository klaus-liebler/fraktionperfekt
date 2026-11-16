using FraktionPerfekt.Domain.Personen;

namespace FraktionPerfekt.Domain.Tests;

public sealed class AnspracheTests
{
    private static Person P(string vorname, string nachname, string? anrede, string? titel = null)
        => new(vorname, nachname, null, null, DateTimeOffset.UnixEpoch, titel: titel, anrede: anrede);

    [Theory]
    // Du
    [InlineData("Herr", "Dr.", Anredeform.Du, Begruessungsstil.Locker, true, "Hallo Klaus")]
    [InlineData("Herr", "Dr.", Anredeform.Du, Begruessungsstil.Neutral, true, "Guten Tag, Klaus")]
    [InlineData("Herr", "Dr.", Anredeform.Du, Begruessungsstil.Foermlich, true, "Lieber Klaus")]
    [InlineData("Frau", null, Anredeform.Du, Begruessungsstil.Foermlich, true, "Liebe Klaus")]
    [InlineData(null, null, Anredeform.Du, Begruessungsstil.Foermlich, true, "Guten Tag, Klaus")]
    // Sie mit Titel
    [InlineData("Herr", "Dr.", Anredeform.Sie, Begruessungsstil.Locker, true, "Hallo Herr Dr. Liebler")]
    [InlineData("Herr", "Dr.", Anredeform.Sie, Begruessungsstil.Neutral, true, "Guten Tag, Herr Dr. Liebler")]
    [InlineData("Herr", "Dr.", Anredeform.Sie, Begruessungsstil.Foermlich, true, "Sehr geehrter Herr Dr. Liebler")]
    // Sie ohne Titel (abgewählt oder keiner)
    [InlineData("Herr", "Dr.", Anredeform.Sie, Begruessungsstil.Foermlich, false, "Sehr geehrter Herr Liebler")]
    [InlineData("Frau", null, Anredeform.Sie, Begruessungsstil.Foermlich, true, "Sehr geehrte Frau Liebler")]
    [InlineData("Frau", null, Anredeform.Sie, Begruessungsstil.Locker, true, "Hallo Frau Liebler")]
    // Sie ohne Anrede: neutrale Form mit vollem Namen
    [InlineData(null, null, Anredeform.Sie, Begruessungsstil.Foermlich, true, "Guten Tag Klaus Liebler")]
    [InlineData(null, "Dr.", Anredeform.Sie, Begruessungsstil.Locker, true, "Guten Tag Dr. Klaus Liebler")]
    public void Begruessung_nach_eigener_Einstellung(string? anrede, string? titel, Anredeform form, Begruessungsstil stil, bool mitTitel, string erwartet)
    {
        var p = P("Klaus", "Liebler", anrede, titel);
        p.AnspracheEinstellen(anrede, form, stil, mitTitel);
        Ansprache.Begruessung(p).ShouldBe(erwartet);
    }

    [Fact]
    public void Standard_ist_Du_und_locker_Titel_steht_nie_in_der_Du_Form()
    {
        var p = P("Tim", "Schubert", "Herr", "Dr.");
        Ansprache.Begruessung(p).ShouldBe("Hallo Tim");
        Ansprache.DuSie(p, "Kannst du?", "Können Sie?").ShouldBe("Kannst du?");
        p.AnspracheEinstellen("Herr", Anredeform.Sie, Begruessungsstil.Neutral, true);
        Ansprache.DuSie(p, "Kannst du?", "Können Sie?").ShouldBe("Können Sie?");
        Should.Throw<ArgumentException>(() => p.AnspracheEinstellen("Divers", Anredeform.Sie, Begruessungsstil.Neutral, true));
    }
}
