using System.Diagnostics;

namespace FraktionPerfekt.Web.Tests;

/// <summary>Führt die Tests der Spiellogik (node --test) aus; ohne Node wird der Test übersprungen.</summary>
public sealed class SpielTests
{
    [Fact]
    public async Task Spiellogik_Tests_mit_Node_sind_gruen()
    {
        var datei = Path.Combine(Projektordner(), "spiel", "logik.test.cjs");
        File.Exists(datei).ShouldBeTrue(datei);
        ProcessStartInfo start = new("node", $"--test \"{datei}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        Process? node;
        try
        {
            node = Process.Start(start);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Assert.Skip("Node.js ist nicht installiert.");
            return;
        }

        ArgumentNullException.ThrowIfNull(node);
        var ausgabe = await node.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await node.WaitForExitAsync(TestContext.Current.CancellationToken);
        node.ExitCode.ShouldBe(0, ausgabe);
        ausgabe.ShouldContain("fail 0");
    }

    private static string Projektordner()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "FraktionPerfekt.Web.Tests.csproj")))
        {
            d = d.Parent;
        }

        return d?.FullName ?? throw new InvalidOperationException("Testprojekt nicht gefunden.");
    }
}
