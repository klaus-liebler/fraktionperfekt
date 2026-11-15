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

    private static void Iteration5(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Wissen.Datei>(modelBuilder, "Dateien", b =>
        {
            b.HasIndex(x => x.Ordner);
            b.HasIndex(x => new { x.BezugArt, x.BezugId });
        });
        Einfach<Domain.Wissen.WikiSeite>(modelBuilder, "WikiSeiten", b => b.HasIndex(x => x.Titel).IsUnique());
        Einfach<Domain.Wissen.WikiVersion>(modelBuilder, "WikiVersionen", b => b.HasIndex(x => new { x.SeiteId, x.Nummer }).IsUnique());
        Einfach<Domain.Wissen.WikiVorschlag>(modelBuilder, "WikiVorschlaege", b =>
        {
            b.HasIndex(x => new { x.SeiteId, x.Status });
            b.Property(x => x.Art).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        Iteration6(modelBuilder);
    }

    private static void Iteration6(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Politik.Anfrage>(modelBuilder, "Anfragen", b => b.HasIndex(x => x.Status));
        Einfach<Domain.Politik.Kontakt>(modelBuilder, "Kontakte", b => b.HasIndex(x => x.Name));
        Einfach<Domain.Politik.Buergeranliegen>(modelBuilder, "Buergeranliegen", b => b.HasIndex(x => x.Status));
        Einfach<Domain.Politik.Pressemitteilung>(modelBuilder, "Pressemitteilungen", b => b.HasIndex(x => x.Status));
        Einfach<Domain.Politik.Presseartikel>(modelBuilder, "Presseartikel", b => b.HasIndex(x => x.Datum));
        Iteration7(modelBuilder);
    }

    private static void Iteration7(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Wahlkampf.Plakat>(modelBuilder, "Plakate", b => b.HasIndex(x => x.Status));
        Einfach<Domain.Wahlkampf.Gebiet>(modelBuilder, "Gebiete");
        Einfach<Domain.Wahlkampf.Schicht>(modelBuilder, "Schichten", b => b.HasIndex(x => x.Von));
        Einfach<Domain.Wahlkampf.Schichteintrag>(modelBuilder, "Schichteintraege", b => b.HasIndex(x => new { x.SchichtId, x.PersonId }).IsUnique());
        Einfach<Domain.Finanzen.Haushaltsposten>(modelBuilder, "Haushaltsposten", b => b.HasIndex(x => new { x.Kasse, x.Jahr }));
        Einfach<Domain.Finanzen.Kassenbuchung>(modelBuilder, "Kassenbuchungen", b =>
        {
            b.HasIndex(x => new { x.Kasse, x.Datum });
            b.Ignore(x => x.Wirkung);
        });
        Einfach<Domain.Finanzen.Spende>(modelBuilder, "Spenden", b => b.Ignore(x => x.QuittungFaellig));
        Einfach<Domain.Finanzen.Auslage>(modelBuilder, "Auslagen", b => b.HasIndex(x => x.Status));
    }
}
