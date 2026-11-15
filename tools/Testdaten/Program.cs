// Testdaten-Generator für FraktionPerfekt: legt die fiktive Demofraktion (Personen, Gremien aus dem RIS-Mock, Termine,
// Abwesenheiten und Vertretungen) in einer Datenbankdatei an. Zufallswerte mit festem Startwert (Bogus).
// Aufruf:  dotnet run --project tools/Testdaten -- [Datenbankdatei]
// Standard: src/FraktionPerfekt.Web/data/fraktionperfekt.db. Die Anwendung erzeugt dieselben Daten in der Entwicklungsumgebung
// beim ersten Start auch selbst (Demodaten:Anlegen).

using FraktionPerfekt.Adapters.Mocks;
using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Infrastructure;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var datei = args.Length > 0 ? args[0] : Path.Combine("src", "FraktionPerfekt.Web", "data", "fraktionperfekt.db");

var konfiguration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FraktionPerfekt"] = $"Data Source={Path.GetFullPath(datei)}" })
    .Build();

var dienste = new ServiceCollection()
    .AddLogging(l => l.AddSimpleConsole().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning))
    .AddSingleton<IAktuellerBenutzer, Testdatenbenutzer>()
    .AddFraktionAnwendung(konfiguration)
    .AddFraktionInfrastruktur(konfiguration, Directory.GetCurrentDirectory(), strengeAbfragepruefung: true, hintergrundjobs: false)
    .AddFraktionMocks(konfiguration)
    .BuildServiceProvider();

await DatenbankInitialisierung.InitialisierenAsync(dienste);
await using (var bereich = dienste.CreateAsyncScope())
{
    var ergebnis = await bereich.ServiceProvider.GetRequiredService<Demodaten>().AnlegenAsync();
    Console.WriteLine(ergebnis is null
        ? $"{Path.GetFullPath(datei)} enthält bereits Personen; nichts angelegt."
        : $"Demofraktion in {Path.GetFullPath(datei)} angelegt: {ergebnis.Personen} Personen, {ergebnis.Gremien} Gremien, {ergebnis.Termine} Termine, "
          + $"{ergebnis.Vertretungsfaelle} Vertretungsfälle.");
}

internal sealed class Testdatenbenutzer : IAktuellerBenutzer
{
    public ValueTask<Benutzer> ErmittelnAsync(CancellationToken abbruch = default)
        => ValueTask.FromResult(new Benutzer("testdaten", "Testdaten-Generator", null));
}
