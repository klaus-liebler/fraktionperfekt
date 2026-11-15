using FraktionPerfekt.Application.Oberflaeche;

namespace FraktionPerfekt.Application.Tests;

public sealed class BalkenTests
{
    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(50, 100, 50)]
    [InlineData(10_000, 10_000, 100)]
    [InlineData(1_500_000, 1_234_567.89, 100)]  // über dem Maximum: begrenzt
    [InlineData(-250, 100, 0)]                  // negativ: kein Balken
    [InlineData(5, 0, 0)]                       // ohne Maximum: kein Balken
    public void Prozent_ist_immer_zwischen_0_und_100(double wert, double max, double erwartet)
        => Balkenrechnung.Prozent(wert, max).ShouldBe(erwartet, 0.0001);

    [Fact]
    public void Css_nutzt_Punkt_und_Warnung_bei_Ueberziehung_oder_negativ()
    {
        Balkenrechnung.Css(Balkenrechnung.Prozent(1_234_567.89m, 2_469_135.78m)).ShouldBe("50%");
        Balkenrechnung.Css(33.333).ShouldBe("33.3%");
        Balkenrechnung.Ausserhalb(1_500_000, 1_234_567.89).ShouldBeTrue();
        Balkenrechnung.Ausserhalb(-1, 100).ShouldBeTrue();
        Balkenrechnung.Ausserhalb(99, 100).ShouldBeFalse();
    }
}
