using FraktionPerfekt.Domain.Abstimmungen;

namespace FraktionPerfekt.Domain.Tests;

/// <summary>Mehrheitsregeln für Wahlen (⚑ Geschäftsordnung als Einstellung): absolute Mehrheit, Ja/Nein bei einer Kandidatur, Stichwahl, Los.</summary>
public sealed class MehrheitTests
{
    private static readonly Guid A = Guid.CreateVersion7(), B = Guid.CreateVersion7(), C = Guid.CreateVersion7();

    private static string Name(Guid g) => g == A ? "Anna" : g == B ? "Bert" : "Cleo";

    [Fact]
    public void Absolute_Mehrheit_ohne_Enthaltungen()
    {
        var e = Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 5, [B] = 4 }, 0, 3, false, false, true, Name);
        e.Status.ShouldBe(Wahlstatus.Gewaehlt);
        e.Gewaehlt.ShouldBe(A);
        e.Text.ShouldStartWith("Gewählt: Anna mit 5 von 9");
        // Zählen Enthaltungen mit, reichen 5 von 12 nicht → Stichwahl
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 5, [B] = 4 }, 0, 3, false, true, true, Name).Status.ShouldBe(Wahlstatus.Stichwahl);
    }

    [Fact]
    public void Eine_Kandidatur_braucht_mehr_Ja_als_Nein()
    {
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 4 }, 4, 1, false, false, true, Name).Status.ShouldBe(Wahlstatus.OhneErgebnis);
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 5 }, 4, 1, false, false, true, Name).Gewaehlt.ShouldBe(A);
    }

    [Fact]
    public void Stichwahl_der_Bestplatzierten_und_Los_bei_Gleichstand()
    {
        var e = Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 4, [B] = 3, [C] = 3 }, 0, 0, false, false, true, Name);
        e.Status.ShouldBe(Wahlstatus.Stichwahl);
        e.Stichwahl.ShouldBe([A, B, C], ignoreOrder: true); // Gleichstand um Platz 2: beide in die Stichwahl
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 4, [B] = 3, [C] = 1 }, 0, 0, false, false, true, Name).Stichwahl.ShouldBe([A, B]);
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 4, [B] = 3, [C] = 1 }, 0, 0, false, false, false, Name).Status.ShouldBe(Wahlstatus.OhneErgebnis);

        var stich = Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 4, [B] = 4 }, 0, 1, true, false, true, Name);
        stich.Status.ShouldBe(Wahlstatus.Los);
        stich.Gleichstand.ShouldBe([A, B], ignoreOrder: true);
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 3, [B] = 4 }, 0, 0, true, false, true, Name).Gewaehlt.ShouldBe(B);
        Mehrheit.Auswerten(new Dictionary<Guid, int> { [A] = 3 }, 0, 0, false, false, true, Name, zuWenigeStimmen: true).Status.ShouldBe(Wahlstatus.OhneErgebnis);
    }

    [Fact]
    public void Amtszeit_endet_vorzeitig_und_Wahl_prueft_Angaben()
    {
        var a = new Amtszeit(A, Fraktionsamt.Vorsitz, null, null, new DateOnly(2026, 1, 1), new DateOnly(2030, 10, 31), Guid.CreateVersion7());
        a.EndetVor(new DateOnly(2027, 3, 1));
        a.Bis.ShouldBe(new DateOnly(2027, 2, 28));
        a.GiltAm(new DateOnly(2027, 3, 1)).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => new Wahl(Fraktionsamt.Benennung, null, null, new DateOnly(2027, 1, 1), null, true, A, DateTimeOffset.UnixEpoch));
        Should.Throw<ArgumentException>(() => new Wahl(Fraktionsamt.Sprecher, null, null, new DateOnly(2027, 1, 1), null, true, A, DateTimeOffset.UnixEpoch));
        Fraktionsamt.Vorsitz.AmtsRolle().ShouldBe(Personen.Rolle.Vorstand);
        Fraktionsamt.Benennung.AmtsRolle().ShouldBeNull();
    }
}
