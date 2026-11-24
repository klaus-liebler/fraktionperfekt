namespace FraktionPerfekt.Web.Tests;

/// <summary>Einheitliches Anlegen per Dialog (aus SchoolCraft FA-3229): „+ … hinzufügen“, OK nennt, was fehlt; nicht auf Handy-Seiten.</summary>
public sealed class AnlegenDialogTests
{
    [Fact]
    public void Pflichtangaben_nennen_was_fehlt_in_Reihenfolge()
    {
        Pflichtangaben.Fehlend(("Name", Pflichtangaben.Text(" ")), ("Ort", true), ("Frage", Pflichtangaben.Text(null))).ShouldBe(["Name", "Frage"]);
        Pflichtangaben.Fehlend(("Name", Pflichtangaben.Text("Stadtwerke"))).ShouldBeEmpty();
    }

    [Fact]
    public void Kleine_Objekte_werden_einheitlich_per_Dialog_angelegt()
    {
        var seiten = Path.Combine(AnspracheTexteTests.RepositoryFuerTests(), "src", "FraktionPerfekt.Web", "Components", "Pages");
        foreach (var (datei, objekt) in new[] { ("Kontakte.razor", "Kontakt"), ("Abstimmungen.razor", "Abstimmung"), ("Wahlen.razor", "Wahl"), ("KampagneSeite.razor", "Gebiet") })
        {
            var text = File.ReadAllText(Path.Combine(seiten, datei));
            text.ShouldContain($"<Hinzufuegen Objekt=\"{objekt}\"", customMessage: datei);
            text.ShouldContain("<AnlegenDialog", customMessage: datei);
        }

        Directory.EnumerateFiles(Path.Combine(seiten, "Handy"), "*.razor").ShouldAllBe(f => !File.ReadAllText(f).Contains("AnlegenDialog", StringComparison.Ordinal));
    }
}
