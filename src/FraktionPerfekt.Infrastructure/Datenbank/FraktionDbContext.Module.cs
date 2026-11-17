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
        Einfach<Domain.Wissen.Bild>(modelBuilder, "Bilder", b =>
        {
            b.HasIndex(x => x.Sha256);
            b.Ignore(x => x.Adresse);
        });
        Einfach<Domain.Wissen.BildVerwendung>(modelBuilder, "BildVerwendungen", b =>
        {
            b.HasIndex(x => new { x.BildId, x.BezugArt, x.BezugId }).IsUnique();
            b.HasIndex(x => new { x.BezugArt, x.BezugId });
        });
        Einfach<Domain.Wissen.WikiSeite>(modelBuilder, "WikiSeiten", b =>
        {
            b.HasIndex(x => x.Titel).IsUnique();
            b.Ignore(x => x.Version);       // aus WikiVersionen (keine Kopie)
            b.Ignore(x => x.GeaendertAm);
        });
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
        Einfach<Domain.Kampagnen.Kampagne>(modelBuilder, "Kampagnen", b =>
        {
            b.Property(x => x.Name).HasMaxLength(200);
            b.HasIndex(x => x.Status);
        });
        Einfach<Domain.Kampagnen.Plakat>(modelBuilder, "Plakate", b =>
        {
            b.HasIndex(x => new { x.KampagneId, x.Status });
            b.HasOne<Domain.Kampagnen.Kampagne>().WithMany().HasForeignKey(x => x.KampagneId).OnDelete(DeleteBehavior.Restrict);
        });
        Einfach<Domain.Kampagnen.Gebiet>(modelBuilder, "Gebiete", b =>
        {
            b.HasIndex(x => x.KampagneId);
            b.HasOne<Domain.Kampagnen.Kampagne>().WithMany().HasForeignKey(x => x.KampagneId).OnDelete(DeleteBehavior.Restrict);
        });
        Einfach<Domain.Kampagnen.TodoVorlage>(modelBuilder, "TodoVorlagen", b => b.Property(x => x.Name).HasMaxLength(200));
        Einfach<Domain.Kampagnen.TodoVorlagenPunkt>(modelBuilder, "TodoVorlagenPunkte", b =>
        {
            b.HasIndex(x => new { x.VorlageId, x.Position });
            b.HasOne<Domain.Kampagnen.TodoVorlage>().WithMany().HasForeignKey(x => x.VorlageId).OnDelete(DeleteBehavior.Cascade);
        });
        Einfach<Domain.Kampagnen.TerminTodo>(modelBuilder, "TerminTodos", b =>
        {
            b.HasIndex(x => new { x.TerminId, x.Position });
            b.HasOne<Domain.Termine.Termin>().WithMany().HasForeignKey(x => x.TerminId).OnDelete(DeleteBehavior.Cascade);
            b.Ignore(x => x.Erledigt);
        });
        Einfach<Domain.Kampagnen.Ticket>(modelBuilder, "Tickets", b =>
        {
            b.HasIndex(x => new { x.KampagneId, x.Nummer }).IsUnique();
            b.HasIndex(x => x.ZustaendigPersonId);
            b.HasOne<Domain.Kampagnen.Kampagne>().WithMany().HasForeignKey(x => x.KampagneId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Kampagnen.Gebiet>().WithMany().HasForeignKey(x => x.GebietId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Kampagnen.Plakat>().WithMany().HasForeignKey(x => x.PlakatId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Termine.Termin>().WithMany().HasForeignKey(x => x.TerminId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(x => x.Offen);
        });
        Einfach<Domain.Kampagnen.TicketKommentar>(modelBuilder, "TicketKommentare", b =>
        {
            b.HasIndex(x => x.TicketId);
            b.HasOne<Domain.Kampagnen.Ticket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });
        Einfach<Domain.Finanzen.Haushaltsposten>(modelBuilder, "Haushaltsposten", b => b.HasIndex(x => new { x.Kasse, x.Jahr }));
        Einfach<Domain.Finanzen.Kassenbuchung>(modelBuilder, "Kassenbuchungen", b =>
        {
            b.HasIndex(x => new { x.Kasse, x.Datum });
            b.Ignore(x => x.Wirkung);
        });
        Einfach<Domain.Finanzen.Spende>(modelBuilder, "Spenden", b => b.Ignore(x => x.QuittungFaellig));
        Einfach<Domain.Finanzen.Auslage>(modelBuilder, "Auslagen", b => b.HasIndex(x => x.Status));
        Iteration8(modelBuilder);
    }

    private static void Iteration8(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Termine.TerminErinnerung>(modelBuilder, "TerminErinnerungen", b => b.HasIndex(x => new { x.TerminId, x.PersonId }).IsUnique());
        Einfach<Domain.Postausgang.PushAbo>(modelBuilder, "PushAbos", b =>
        {
            b.HasIndex(x => x.Endpunkt).IsUnique();
            b.HasIndex(x => x.PersonId);
        });
        Hilfe(modelBuilder);
    }

    /// <summary>Hilfe, Doku und Impressum (aus VibeVHS übernommen): Kommentare je Abschnitt, Impressum der Fraktion.</summary>
    private static void Hilfe(ModelBuilder modelBuilder)
    {
        Einfach<Domain.Dokumentation.Kommentar>(modelBuilder, "Kommentare", b =>
        {
            b.Property(k => k.Dokument).HasMaxLength(200);
            b.Property(k => k.Abschnitt).HasMaxLength(200);
            b.Property(k => k.AbschnittTitel).HasMaxLength(300);
            b.Property(k => k.Text).HasMaxLength(Domain.Dokumentation.Kommentar.TextMaxZeichen);
            b.HasIndex(k => new { k.Dokument, k.Abschnitt });
            b.HasIndex(k => k.AntwortAuf);
        });
        Einfach<Domain.Dokumentation.Impressum>(modelBuilder, "Impressum", b => b.Property(i => i.Text).HasMaxLength(Domain.Dokumentation.Impressum.MaxZeichen));
    }
}
