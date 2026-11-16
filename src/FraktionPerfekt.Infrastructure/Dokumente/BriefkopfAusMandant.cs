using System.Collections.Concurrent;
using FraktionPerfekt.Application.Dokumente;
using FraktionPerfekt.Application.Gemeinsam;

namespace FraktionPerfekt.Infrastructure.Dokumente;

/// <summary>Briefkopf aus den Angaben des Mandanten (statt eines Singletons mit fest eingetragener Fraktion).</summary>
internal static class BriefkopfAusMandant
{
    private static readonly ConcurrentDictionary<string, byte[]?> Logos = new(StringComparer.Ordinal);

    public static Briefkopf Erzeugen(MandantOptionen mandant, string basisVerzeichnis)
    {
        var logo = mandant.Logo is { Length: > 0 } l
            ? Logos.GetOrAdd(Path.GetFullPath(Path.Combine(basisVerzeichnis, "wwwroot", l)), p => File.Exists(p) ? File.ReadAllBytes(p) : null)
            : null;
        return new Briefkopf { Absender = mandant.Name, Zusatz = mandant.Zusatz ?? string.Empty, Logo = logo };
    }
}
