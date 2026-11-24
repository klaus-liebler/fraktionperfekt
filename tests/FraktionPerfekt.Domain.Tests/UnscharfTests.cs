using FraktionPerfekt.Domain.Gemeinsam;

namespace FraktionPerfekt.Domain.Tests;

/// <summary>Fehlertolerante Suche (aus SchoolCraft übernommen): Umlaute, ß, Akzente, Präfixe, Tippfehler, Reihenfolge der Treffer.</summary>
public sealed class UnscharfTests
{
    [Theory]
    [InlineData("Nienteidt", "Philipp Nientiedt")]      // vertauschte Buchstaben
    [InlineData("Kuehlert", "Liz Kühlert")]             // ü = ue
    [InlineData("Kuhlert", "Liz Kühlert")]              // ü ohne e
    [InlineData("Schulte-Jochmaring", "Alfons Schulte Jochmaring")]
    [InlineData("Waschko", "Christa Waschkowitz-Biggeleben")] // Präfix
    [InlineData("Stoecker", "Anika Stöcker")]
    [InlineData("liz kuh", "Liz Kühlert")]              // mehrere Wörter, Präfix
    [InlineData("Brinkhuas", "Eike Brinkhaus")]         // Tippfehler
    [InlineData("Strasse", "Rathausstraße")]            // ß = ss, Teilwort
    [InlineData("Jose", "José Müller")]                 // Akzent
    public void Findet_wie_Menschen_tippen(string eingabe, string name) => Unscharf.Passt(eingabe, name).ShouldBeTrue();

    [Theory]
    [InlineData("Gries", "Eike Brinkhaus")]
    [InlineData("xy", "Liz Kühlert")]
    [InlineData("", "Liz Kühlert")]
    [InlineData("Liz Meier", "Liz Kühlert")] // jedes Wort muss passen
    public void Findet_nichts_Falsches(string eingabe, string name) => Unscharf.Passt(eingabe, name).ShouldBeFalse();

    [Fact]
    public void Genaue_Treffer_vor_unscharfen()
    {
        var namen = new[] { "Klaus Schwenken", "Klaus Liebler", "Klara Klause" };
        Unscharf.Ordnen("Klaus", namen, n => n).ShouldBe(["Klaus Liebler", "Klaus Schwenken", "Klara Klause"]);
        Unscharf.Bewertung("Liebler", "Klaus Liebler").ShouldBe(100);
        Unscharf.Abstand("ab", "ba").ShouldBe(1);
    }
}
