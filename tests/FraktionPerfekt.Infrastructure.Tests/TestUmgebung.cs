using FraktionPerfekt.Adapters.Mocks;
using FraktionPerfekt.Adapters.Mocks.Testdaten;
using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Ris;
using FraktionPerfekt.Application.Sicherheit;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Infrastructure.Datenbank;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Infrastructure.Tests;

/// <summary>Benutzer, den der Test vor jedem Schritt setzt (wer klickt gerade?).</summary>
internal sealed class TestBenutzer : IAktuellerBenutzer
{
    public Benutzer Aktuell { get; set; } = Benutzer.Automatik;

    public ValueTask<Benutzer> ErmittelnAsync(CancellationToken abbruch = default) => ValueTask.FromResult(Aktuell);
}

/// <summary>
/// Echte Dienste mit SQLite-Datei (je Test frisch, strenge Abfrageprüfung), steuerbarer Uhr, Mock-RIS und Mock-Kanälen.
/// Hintergrundjobs laufen nicht; Tests rufen die Routinen direkt auf.
/// </summary>
internal sealed class TestUmgebung : IAsyncDisposable
{
    private readonly string _ordner;

    private TestUmgebung(ServiceProvider dienste, string ordner)
    {
        Dienste = dienste;
        _ordner = ordner;
    }

    public ServiceProvider Dienste { get; }

    /// <summary>Datei der Testdatenbank (für Messungen der Dateigröße).</summary>
    public string Datenbankdatei => Path.Combine(_ordner, "test.db");

    public FakeTimeProvider Uhr => (FakeTimeProvider)Dienste.GetRequiredService<TimeProvider>();

    public TestBenutzer Benutzer => (TestBenutzer)Dienste.GetRequiredService<IAktuellerBenutzer>();

    /// <param name="mitDemodaten">Vollständige Demodaten (inkl. Vertretungsfällen) statt nur Personen + RIS-Abgleich.</param>
    public static async Task<TestUmgebung> ErzeugenAsync(bool mitDemodaten = false)
    {
        var ordner = Path.Combine(Path.GetTempPath(), "fraktionperfekt-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ordner);
        var konfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FraktionPerfekt"] = $"Data Source={Path.Combine(ordner, "test.db")};Pooling=False" })
            .Build();
        var dienste = new ServiceCollection()
            .AddLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IConfiguration>(konfiguration)
            // Die Uhr läuft bei jedem Abruf ein wenig weiter – wie im Betrieb (deckt veraltete Stichtage auf).
            .AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 8, 0, 0, TimeSpan.Zero)) { AutoAdvanceAmount = TimeSpan.FromMilliseconds(5) })
            .AddSingleton<IAktuellerBenutzer, TestBenutzer>()
            .AddFraktionAnwendung(konfiguration)
            .AddFraktionInfrastruktur(konfiguration, ordner, strengeAbfragepruefung: true, hintergrundjobs: false)
            .AddFraktionMocks(konfiguration)
            .BuildServiceProvider();
        var umgebung = new TestUmgebung(dienste, ordner);
        await DatenbankInitialisierung.InitialisierenAsync(dienste);
        if (mitDemodaten)
        {
            await umgebung.InAsync(d => d.GetRequiredService<Demodaten>().AnlegenAsync());
        }
        else
        {
            await umgebung.InAsync(async d =>
            {
                var speicher = d.GetRequiredService<IStammdatenSpeicher>();
                var jetzt = umgebung.Uhr.GetUtcNow();
                foreach (var p in Demofraktion.Personen)
                {
                    var person = new Person(p.Vorname, p.Nachname, p.Adresse, p.Kennung, jetzt, p.RisId, titel: p.Titel, anrede: p.Anrede);
                    person.RollenFestlegenAb(new DateOnly(2025, 11, 1), p.Rollen, Erfassung.Erst(jetzt, "Test"));
                    speicher.Hinzufuegen(person);
                }

                await speicher.SpeichernAsync();
                return await d.GetRequiredService<RisAbgleich>().AbgleichenAsync();
            });
        }

        return umgebung;
    }

    /// <summary>Ein Arbeitsschritt in eigenem Bereich (eigener DbContext), wie eine Anfrage in der Anwendung.</summary>
    public async Task<T> InAsync<T>(Func<IServiceProvider, Task<T>> arbeit)
    {
        await using var bereich = Dienste.CreateAsyncScope();
        return await arbeit(bereich.ServiceProvider);
    }

    public Task InAsync(Func<IServiceProvider, Task> arbeit) => InAsync<bool>(async d =>
    {
        await arbeit(d);
        return true;
    });

    /// <summary>Als Person mit Kennung angemeldet (erste Rolle aktiv, oder die angegebene).</summary>
    public async Task<Person> AnmeldenAsync(string kennung, Rolle? rolle = null)
    {
        var person = await InAsync(d => d.GetRequiredService<IStammdatenSpeicher>().PersonNachKennungAsync(kennung))
            ?? throw new InvalidOperationException($"Person {kennung} fehlt.");
        var rollen = person.RollenAm(new DateOnly(2026, 11, 15), Uhr.GetUtcNow());
        Benutzer.Aktuell = new Benutzer(kennung, person.Anzeigename, rolle ?? rollen.Min(), rollen, person.Id);
        return person;
    }

    public async ValueTask DisposeAsync()
    {
        await Dienste.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_ordner, recursive: true);
        }
        catch (IOException)
        {
            // Temporärer Ordner; Aufräumen ist nicht kritisch.
        }
    }
}
