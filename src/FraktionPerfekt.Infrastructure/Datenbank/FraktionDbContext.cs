using FraktionPerfekt.Application.Postausgang;
using FraktionPerfekt.Application.Protokoll;
using FraktionPerfekt.Domain.Aufgaben;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Gremien;
using FraktionPerfekt.Domain.Personen;
using FraktionPerfekt.Domain.Termine;
using FraktionPerfekt.Domain.Vertretung;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraktionPerfekt.Infrastructure.Datenbank;

public sealed partial class FraktionDbContext(DbContextOptions<FraktionDbContext> optionen) : DbContext(optionen)
{
    public DbSet<Person> Personen => Set<Person>();

    public DbSet<Gremium> Gremien => Set<Gremium>();

    public DbSet<Mitgliedschaft> Mitgliedschaften => Set<Mitgliedschaft>();

    public DbSet<Termin> Termine => Set<Termin>();

    public DbSet<Teilnahme> Teilnahmen => Set<Teilnahme>();

    public DbSet<KalenderAbo> KalenderAbos => Set<KalenderAbo>();

    public DbSet<Abwesenheit> Abwesenheiten => Set<Abwesenheit>();

    public DbSet<Vertretungsfall> Vertretungsfaelle => Set<Vertretungsfall>();

    public DbSet<Vertretungsanfrage> Vertretungsanfragen => Set<Vertretungsanfrage>();

    public DbSet<Aufgabe> Aufgaben => Set<Aufgabe>();

    public DbSet<AuditEintrag> Protokoll => Set<AuditEintrag>();

    public DbSet<AusgehendeNachricht> Postausgang => Set<AusgehendeNachricht>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite kennt keinen Zeitstempel mit Offset; als sortierbare Zahl (UTC-Ticks) speichern.
        // Datenbank-Regel 5: Zeitpunkte UTC in ms seit 1970, Kalendertage als JJJJMMTT, Ortszeiten (Termine) als UTC-ms
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<ZeitpunktZuMillisekunden>();
        configurationBuilder.Properties<DateOnly>().HaveConversion<TagZuZahl>();
        configurationBuilder.Properties<DateTime>().HaveConversion<OrtszeitZuMillisekunden>();
        // Datenbank-Regel 4: Ids binär (16 Byte, Big-Endian) statt als Text
        configurationBuilder.Properties<Guid>().HaveConversion<GuidZuBlob>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Erfassung>(b =>
        {
            b.ToTable("Aenderungsvorgaenge");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Von).HasMaxLength(200);
            b.Property(e => e.Art).HasConversion<string>().HasMaxLength(40);
            b.Property(e => e.Grund).HasMaxLength(500);
            b.HasIndex(e => e.Am);
            b.HasIndex(e => e.ProtokollId);
        });

        modelBuilder.Entity<Person>(b =>
        {
            b.ToTable("Personen");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            // Stand heute: aus den Versionen berechnet, nicht gespeichert (Datenbank-Regel 1)
            foreach (var n in new[] { nameof(Person.Vorname), nameof(Person.Nachname), nameof(Person.Vorsatz), nameof(Person.Zusatz), nameof(Person.Titel),
                         nameof(Person.Anrede), nameof(Person.EMail), nameof(Person.Mobil), nameof(Person.Aktiv), nameof(Person.Anredeform),
                         nameof(Person.Begruessungsstil), nameof(Person.TitelInAnsprache) })
            {
                b.Ignore(n);
            }

            b.Property(p => p.Benutzerkennung).HasMaxLength(100);
            b.Property(p => p.RisId).HasMaxLength(200);
            b.HasIndex(p => p.Benutzerkennung).IsUnique();
            b.HasIndex(p => p.RisId).IsUnique();
            b.Ignore(p => p.Anzeigename);
            b.Ignore(p => p.Sortiername);
            b.Ignore(p => p.Initialen);
            b.Ignore(p => p.NachnameMitVorsatz);
            b.Ignore(p => p.Sortierschluessel);
            b.HasMany(p => p.RollenVersionen).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.RollenVersionen).HasField("_rollen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.Namen).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.Namen).HasField("_namen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.Merkmale).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.Merkmale).HasField("_merkmale").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.Adressen).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.Adressen).HasField("_adressen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.EMails).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.EMails).HasField("_emails").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.Rufnummern).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.Rufnummern).HasField("_rufnummern").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.HasMany(p => p.Einwilligungen).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.Einwilligungen).HasField("_einwilligungen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.Ignore(p => p.AlleVersionen);
        });
        Version<PersonName>(modelBuilder, "PersonNamen", "PersonId", b =>
        {
            b.Property(v => v.Vorname).HasMaxLength(200);
            b.Property(v => v.Nachname).HasMaxLength(200);
            // Sortierung nach DIN 5007-2 in SQL: Collation an den Spalten, Index für die Personenliste (kein gespeicherter Sortierschlüssel)
            b.Property(v => v.Nachname).UseCollation(Din5007Kollation.Name);
            b.Property(v => v.Vorname).UseCollation(Din5007Kollation.Name);
            b.Property(v => v.Vorsatz).HasColumnName("Namensvorsatz").HasMaxLength(60).UseCollation(Din5007Kollation.Name);
            b.Property(v => v.Zusatz).HasColumnName("Namenszusatz").HasMaxLength(30);
            b.HasIndex(v => new { v.Nachname, v.Vorname, v.Vorsatz }).HasDatabaseName("IX_PersonNamen_DIN5007");
            b.Ignore(v => v.Anzeige);
        });
        Version<PersonMerkmal>(modelBuilder, "PersonMerkmale", "PersonId", b =>
        {
            b.Property(v => v.Art).HasConversion<string>().HasMaxLength(20);
            b.Property(v => v.Inhalt).HasMaxLength(320);
            b.Ignore(v => v.WertOderNull);
        });
        Version<Postadresse>(modelBuilder, "PersonAdressen", "PersonId", b =>
        {
            b.Property(v => v.Bereich).HasConversion<string>().HasMaxLength(20);
            b.Property(v => v.Zusatz).HasMaxLength(200);
            b.Property(v => v.Strasse).HasMaxLength(200);
            b.Property(v => v.Postleitzahl).HasMaxLength(10);
            b.Property(v => v.Ort).HasMaxLength(100);
            b.Ignore(v => v.Anzeige);
            b.HasIndex(v => v.AdressId);
        });
        Version<EMailAdresse>(modelBuilder, "PersonEMails", "PersonId", b =>
        {
            b.Property(v => v.Bereich).HasConversion<string>().HasMaxLength(20);
            b.Property(v => v.Adresse).HasMaxLength(320);
            b.HasIndex(v => v.AdressId);
        });
        Version<Rufnummer>(modelBuilder, "PersonRufnummern", "PersonId", b =>
        {
            b.Property(v => v.Art).HasConversion<string>().HasMaxLength(20);
            b.Property(v => v.Nummer).HasMaxLength(40);
            b.Ignore(v => v.DiensteAnzeige);
            b.HasIndex(v => v.NummerId);
        });
        Version<Einwilligung>(modelBuilder, "PersonEinwilligungen", "PersonId", b =>
        {
            b.Property(v => v.Zweck).HasConversion<string>().HasMaxLength(40);
        });
        Version<PersonRolle>(modelBuilder, "PersonRollen", "PersonId", b => b.Property(v => v.Rolle).HasConversion<string>().HasMaxLength(40));

        modelBuilder.Entity<Gremium>(b =>
        {
            b.ToTable("Gremien");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).ValueGeneratedNever();
            b.Ignore(g => g.Name);
            b.Ignore(g => g.Kurzname);
            b.Ignore(g => g.Aktiv);
            b.Property(g => g.RisId).HasMaxLength(200);
            b.HasIndex(g => g.RisId).IsUnique();
            b.HasMany(g => g.Versionen).WithOne().HasForeignKey("GremiumId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(g => g.Versionen).HasField("_versionen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });
        Version<GremiumVersion>(modelBuilder, "GremiumVersionen", "GremiumId", b =>
        {
            b.Property(v => v.Name).HasMaxLength(300);
            b.Property(v => v.Kurzname).HasMaxLength(60);
        });

        modelBuilder.Entity<Mitgliedschaft>(b =>
        {
            b.ToTable("Mitgliedschaften");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).ValueGeneratedNever();
            b.Property(m => m.RisId).HasMaxLength(200);
            b.HasIndex(m => m.RisId).IsUnique();
            b.HasIndex(m => m.GremiumId);
            b.HasIndex(m => m.PersonId);
            b.HasOne<Person>().WithMany().HasForeignKey(m => m.PersonId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Gremium>().WithMany().HasForeignKey(m => m.GremiumId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(m => m.Versionen).WithOne().HasForeignKey("MitgliedschaftId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(m => m.Versionen).HasField("_versionen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });
        Version<MitgliedschaftsVersion>(modelBuilder, "MitgliedschaftsVersionen", "MitgliedschaftId", b => b.Property(v => v.FunktionText).HasMaxLength(200));

        modelBuilder.Entity<Termin>(b =>
        {
            b.ToTable("Termine");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();
            b.Property(t => t.Titel).HasMaxLength(300);
            b.Property(t => t.Ort).HasMaxLength(300);
            b.Property(t => t.Beschreibung).HasMaxLength(4000);
            b.Property(t => t.RisId).HasMaxLength(200);
            b.HasIndex(t => t.Beginn);
            b.HasIndex(t => t.RisId).IsUnique();
            b.HasIndex(t => t.GremiumId);
            b.HasIndex(t => t.KampagneId);
            b.HasOne<Gremium>().WithMany().HasForeignKey(t => t.GremiumId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Domain.Kampagnen.Kampagne>().WithMany().HasForeignKey(t => t.KampagneId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(t => t.Tag);
        });

        modelBuilder.Entity<Teilnahme>(b =>
        {
            b.ToTable("Teilnahmen");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();
            b.HasIndex(t => new { t.TerminId, t.PersonId }).IsUnique();
            b.HasOne<Termin>().WithMany().HasForeignKey(t => t.TerminId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Person>().WithMany().HasForeignKey(t => t.PersonId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KalenderAbo>(b =>
        {
            b.ToTable("KalenderAbos");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.TokenHash).HasMaxLength(64);
            b.HasIndex(a => a.TokenHash).IsUnique();
            b.HasIndex(a => a.PersonId);
            b.HasOne<Person>().WithMany().HasForeignKey(a => a.PersonId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(a => a.Aktiv);
        });

        modelBuilder.Entity<Abwesenheit>(b =>
        {
            b.ToTable("Abwesenheiten");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.Grund).HasMaxLength(500);
            b.HasIndex(a => new { a.PersonId, a.Von });
            b.HasIndex(a => new { a.Von, a.Bis });
            b.HasOne<Person>().WithMany().HasForeignKey(a => a.PersonId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(a => a.Aktiv);
        });

        modelBuilder.Entity<Vertretungskandidat>(b =>
        {
            b.ToTable("Vertretungskandidaten");
            b.HasKey(k => k.Id);
            b.Property(k => k.Id).ValueGeneratedNever();
            b.HasIndex(k => new { k.VertretungsfallId, k.Position }).IsUnique();
            b.HasOne<Person>().WithMany().HasForeignKey(k => k.PersonId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Vertretungsfall>(b =>
        {
            b.ToTable("Vertretungsfaelle");
            b.HasKey(f => f.Id);
            b.Property(f => f.Id).ValueGeneratedNever();
            b.HasIndex(f => f.TerminId);
            b.HasIndex(f => f.Status);
            b.HasIndex(f => f.AbwesenheitId);
            b.HasOne<Termin>().WithMany().HasForeignKey(f => f.TerminId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(f => f.Anfragen).WithOne().HasForeignKey(a => a.VertretungsfallId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(f => f.Anfragen).HasField("_anfragen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.Ignore(f => f.Kandidaten);
            b.HasMany(f => f.KandidatenZeilen).WithOne().HasForeignKey(k => k.VertretungsfallId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(f => f.KandidatenZeilen).HasField("_kandidaten").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.Ignore(f => f.OffeneAnfrage);
            b.Ignore(f => f.IstOffen);
        });

        modelBuilder.Entity<Vertretungsanfrage>(b =>
        {
            b.ToTable("Vertretungsanfragen");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.HasIndex(a => new { a.PersonId, a.Status });
        });

        modelBuilder.Entity<Aufgabe>(b =>
        {
            b.ToTable("Aufgaben");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.Titel).HasMaxLength(300);
            b.Property(a => a.Beschreibung).HasMaxLength(2000);
            b.Property(a => a.Anlass).HasMaxLength(60);
            b.Property(a => a.Ziel).HasMaxLength(300);
            b.Property(a => a.Vermerk).HasMaxLength(2000);
            b.HasIndex(a => new { a.Status, a.Zustaendig });
            b.HasIndex(a => new { a.Anlass, a.AnlassId });
        });

        // Stufe 2: einfache Fachobjekte ohne Navigationen (Verweise über Ids).
        Einfach<Themenpate>(modelBuilder, "Themenpaten");
        Einfach<Domain.Sitzungen.Sitzung>(modelBuilder, "Sitzungen", b => b.HasIndex(s => s.TerminId).IsUnique());
        Einfach<Domain.Sitzungen.Top>(modelBuilder, "Tops", b => b.HasIndex(t => new { t.SitzungId, t.Position }));
        Einfach<Domain.Sitzungen.Sitzungsanwesenheit>(modelBuilder, "Sitzungsanwesenheiten", b => b.HasIndex(a => new { a.SitzungId, a.PersonId }).IsUnique());
        Einfach<Domain.Sitzungen.Beschluss>(modelBuilder, "Beschluesse", b => b.HasIndex(x => x.Datum));
        Stufe2(modelBuilder);

        modelBuilder.Entity<AuditEintrag>(b =>
        {
            b.ToTable("Protokoll");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.BenutzerKennung).HasMaxLength(100);
            b.Property(e => e.Benutzer).HasMaxLength(200);
            b.Property(e => e.Rolle).HasMaxLength(60);
            b.Property(e => e.Aktion).HasMaxLength(100);
            b.Property(e => e.ObjektTyp).HasMaxLength(100);
            b.Property(e => e.Beschreibung).HasMaxLength(2000);
            b.HasIndex(e => e.Zeitpunkt);
            b.HasIndex(e => new { e.ObjektTyp, e.ObjektId });
        });

        modelBuilder.Entity<AusgehendeNachricht>(b =>
        {
            b.ToTable("Postausgang");
            b.HasKey(n => n.Id);
            b.Property(n => n.Id).ValueGeneratedNever();
            b.Property(n => n.Empfaenger).HasMaxLength(320);
            b.Property(n => n.EmpfaengerAnzeige).HasMaxLength(200);
            b.Property(n => n.Betreff).HasMaxLength(300);
            b.Property(n => n.Link).HasMaxLength(500);
            b.Property(n => n.Aktion).HasMaxLength(100);
            b.Property(n => n.AusgeloestVon).HasMaxLength(200);
            b.Property(n => n.Rueckmeldung).HasMaxLength(500);
            b.Property(n => n.Fehler).HasMaxLength(2000);
            b.HasIndex(n => new { n.Status, n.ErstelltAm });
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NurAnfuegenPruefen();
        VorgaengeMitProtokollVerbinden();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NurAnfuegenPruefen();
        VorgaengeMitProtokollVerbinden();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Abbildung eines einfachen Fachobjekts: Tabelle, Schlüssel „Id“ (vom Objekt vergeben).</summary>
    internal static void Einfach<T>(ModelBuilder modelBuilder, string tabelle, Action<EntityTypeBuilder<T>>? weiter = null)
        where T : class
        => modelBuilder.Entity<T>(b =>
        {
            b.ToTable(tabelle);
            b.HasKey("Id");
            b.Property<Guid>("Id").ValueGeneratedNever();
            weiter?.Invoke(b);
        });

    /// <summary>Weitere Module (partielle Erweiterung in <c>FraktionDbContext.Module.cs</c>).</summary>
    static partial void Stufe2(ModelBuilder modelBuilder);

    /// <summary>Gemeinsame Abbildung aller bitemporalen Versionen.</summary>
    private static void Version<T>(ModelBuilder modelBuilder, string tabelle, string fremdschluessel, Action<EntityTypeBuilder<T>> weiter)
        where T : BitemporaleVersion<T>
    {
        modelBuilder.Entity<T>(b =>
        {
            b.ToTable(tabelle);
            b.HasKey(v => v.Id);
            b.Property(v => v.Id).ValueGeneratedNever();
            // Datenbank-Regel 3: wann/wer/wie/warum stehen im Änderungsvorgang, an der Zeile nur Gültigkeit und ErsetztAm
            b.HasOne(v => v.Vorgang).WithMany().HasForeignKey("VorgangId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(v => v.Vorgang).AutoInclude();
            b.Ignore(v => v.ErfasstAm);
            b.Ignore(v => v.ErfasstVon);
            b.Ignore(v => v.Erfassungsart);
            b.Ignore(v => v.Grund);
            b.Ignore(v => v.IstAktuellerWissensstand);
            b.Ignore(v => v.GueltigBis);
            b.Property<DateOnly>("GueltigBisSpalte").HasColumnName("GueltigBis");   // offenes Ende = 99991231
            b.Ignore(v => v.IstEndemarke);
            b.Ignore(v => v.Datenart);
            b.Ignore(v => v.Feld);
            b.Ignore(v => v.Gegenstand);
            b.Ignore(v => v.Wert);
            b.HasIndex(fremdschluessel, nameof(BitemporaleVersion<T>.GueltigVon));
            weiter(b);
        });
    }

    /// <summary>Neue Änderungsvorgänge verweisen auf den Protokolleintrag desselben Speichervorgangs (eine Stelle für wer/wann/was/warum).</summary>
    private void VorgaengeMitProtokollVerbinden()
    {
        var eintrag = ChangeTracker.Entries<AuditEintrag>().Where(e => e.State == EntityState.Added).Select(e => e.Entity).FirstOrDefault();
        if (eintrag is null)
        {
            return;
        }

        foreach (var v in ChangeTracker.Entries<Erfassung>().Where(e => e.State == EntityState.Added))
        {
            v.Entity.ProtokollZuordnen(eintrag.Id);
        }
    }

    /// <summary>Protokolleinträge, Änderungsvorgänge und bitemporale Versionen dürfen nur angefügt, nie gelöscht werden; Protokolleinträge und Vorgänge nie geändert.</summary>
    private void NurAnfuegenPruefen()
    {
        var verboten = ChangeTracker.Entries()
            .Where(e => (e.State == EntityState.Deleted && e.Entity is AuditEintrag or IHistorienwert or Erfassung)
                        || (e.State == EntityState.Modified && e.Entity is AuditEintrag or Erfassung))
            .Select(e => e.Entity.GetType().Name)
            .ToList();
        if (verboten.Count > 0)
        {
            throw new InvalidOperationException($"Unzulässige Änderung an nur anfügbaren Daten: {string.Join(", ", verboten.Distinct())}.");
        }
    }
}
