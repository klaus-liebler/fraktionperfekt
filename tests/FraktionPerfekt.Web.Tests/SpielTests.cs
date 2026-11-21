using System.Diagnostics;

namespace FraktionPerfekt.Web.Tests;

/// <summary>Führt die Tests des Spiels (node --test) aus: Spiellogik und 3D-Fassung; ohne Node wird der Test übersprungen.</summary>
public sealed class SpielTests
{
    [Theory]
    [InlineData("logik.test.cjs")]
    [InlineData("spiel3d.test.cjs")]
    public async Task Spiellogik_Tests_mit_Node_sind_gruen(string testdatei)
    {
        var datei = Path.Combine(Projektordner(), "spiel", testdatei);
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
        // Ausgabe im Projektordner (bin/) oder in einem Artefaktordner im Repository (--artifacts-path)
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "FraktionPerfekt.Web.Tests.csproj")))
            {
                return d.FullName;
            }

            var imRepo = Path.Combine(d.FullName, "tests", "FraktionPerfekt.Web.Tests");
            if (File.Exists(Path.Combine(imRepo, "FraktionPerfekt.Web.Tests.csproj")))
            {
                return imRepo;
            }
        }

        throw new InvalidOperationException("Testprojekt nicht gefunden.");
    }
}
