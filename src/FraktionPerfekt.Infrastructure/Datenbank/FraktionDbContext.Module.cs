using FraktionPerfekt.Domain.Abstimmungen;
using FraktionPerfekt.Domain.Antraege;
using Microsoft.EntityFrameworkCore;

namespace FraktionPerfekt.Infrastructure.Datenbank;

/// <summary>Abbildung der Module ab Iteration 3 (einfache Fachobjekte, Verweise über Ids).</summary>
public sealed partial class FraktionDbContext
{
    static partial void Stufe2(ModelBuilder modelBuilder)
    {
        // Iteration 3: Anträge und Abstimmungen
        Einfach<Antrag>(modelBuilder, "Antraege", b =>
        {
            b.HasIndex(a => a.Status);
            b.HasIndex(a => a.Nummer);
        });
        Einfach<AntragFassung>(modelBuilder, "AntragFassungen", b => b.HasIndex(f => new { f.AntragId, f.Nummer }).IsUnique());
        Einfach<AntragKommentar>(modelBuilder, "AntragKommentare", b => b.HasIndex(k => k.AntragId));
        Einfach<Aenderungsvorschlag>(modelBuilder, "Aenderungsvorschlaege", b => b.HasIndex(v => v.AntragId));
        Einfach<Umfrage>(modelBuilder, "Umfragen", b => b.HasIndex(u => u.Status));
        Einfach<UmfrageOption>(modelBuilder, "UmfrageOptionen", b => b.HasIndex(o => o.UmfrageId));
        Einfach<Stimme>(modelBuilder, "Stimmen", b => b.HasIndex(s => new { s.UmfrageId, s.PersonId }));
        Iteration4(modelBuilder);
    }

    private static void Iteration4(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Ressourcen.Ressource>(modelBuilder, "Ressourcen");
        Einfach<Domain.Ressourcen.Buchung>(modelBuilder, "Buchungen", b => b.HasIndex(x => new { x.RessourceId, x.Von }));
        Einfach<Domain.Termine.Besetzungsanfrage>(modelBuilder, "Besetzungsanfragen", b => b.HasIndex(x => x.TerminId).IsUnique());
        Einfach<Domain.Termine.Besetzungsmeldung>(modelBuilder, "Besetzungsmeldungen", b => b.HasIndex(x => new { x.AnfrageId, x.PersonId }).IsUnique());
        Iteration5(modelBuilder);
    }

    static partial void Iteration5(ModelBuilder modelBuilder);
}
