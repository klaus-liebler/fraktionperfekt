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
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Person>(b =>
        {
            b.ToTable("Personen");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.Vorname).HasMaxLength(200);
            b.Property(p => p.Nachname).HasMaxLength(200);
            b.Property(p => p.EMail).HasMaxLength(320);
            b.Property(p => p.Mobil).HasMaxLength(40);
            b.Property(p => p.Benutzerkennung).HasMaxLength(100);
            b.Property(p => p.RisId).HasMaxLength(200);
            b.HasIndex(p => p.Benutzerkennung).IsUnique();
            b.HasIndex(p => p.RisId).IsUnique();
            b.Ignore(p => p.Anzeigename);
            b.Ignore(p => p.Sortiername);
            b.HasMany(p => p.RollenVersionen).WithOne().HasForeignKey("PersonId").IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(p => p.RollenVersionen).HasField("_rollen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });
        Version<PersonRollen>(modelBuilder, "PersonRollen", "PersonId", b =>
        {
            b.Property(v => v.RollenText).HasMaxLength(300);
            b.Ignore(v => v.Rollen);
        });

        modelBuilder.Entity<Gremium>(b =>
        {
            b.ToTable("Gremien");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).ValueGeneratedNever();
            b.Property(g => g.Name).HasMaxLength(300);
            b.Property(g => g.Kurzname).HasMaxLength(60);
            b.Property(g => g.RisId).HasMaxLength(200);
            b.HasIndex(g => g.RisId).IsUnique();
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
        Version<MitgliedschaftsVersion>(modelBuilder, "MitgliedschaftsVersionen", "MitgliedschaftId", _ => { });

        modelBuilder.Entity<Termin>(b =>
        {
            b.ToTable("Termine");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();
            b.Property(t => t.Titel).HasMaxLength(300);
            b.Property(t => t.Ort).HasMaxLength(300);
            b.Property(t => t.Beschreibung).HasMaxLength(4000);
            b.Property(t => t.RisId).HasMaxLength(200);
            b.Property(t => t.AngelegtVon).HasMaxLength(200);
            b.HasIndex(t => t.Beginn);
            b.HasIndex(t => t.RisId).IsUnique();
            b.HasIndex(t => t.GremiumId);
            b.HasOne<Gremium>().WithMany().HasForeignKey(t => t.GremiumId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Vertretungsfall>(b =>
        {
            b.ToTable("Vertretungsfaelle");
            b.HasKey(f => f.Id);
            b.Property(f => f.Id).ValueGeneratedNever();
            b.Property(f => f.KandidatenText).HasMaxLength(4000);
            b.HasIndex(f => f.TerminId);
            b.HasIndex(f => f.Status);
            b.HasIndex(f => f.AbwesenheitId);
            b.HasOne<Termin>().WithMany().HasForeignKey(f => f.TerminId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(f => f.Anfragen).WithOne().HasForeignKey(a => a.VertretungsfallId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            b.Navigation(f => f.Anfragen).HasField("_anfragen").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            b.Ignore(f => f.Kandidaten);
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
            b.Property(a => a.ErledigtVon).HasMaxLength(200);
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
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NurAnfuegenPruefen();
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
            b.Property(v => v.Grund).HasMaxLength(500);
            b.Property(v => v.ErfasstVon).HasMaxLength(200);
            b.Ignore(v => v.IstAktuellerWissensstand);
            b.Ignore(v => v.IstEndemarke);
            b.Ignore(v => v.Datenart);
            b.Ignore(v => v.Feld);
            b.Ignore(v => v.Gegenstand);
            b.Ignore(v => v.Wert);
            b.HasIndex(fremdschluessel, nameof(BitemporaleVersion<T>.GueltigVon));
            weiter(b);
        });
    }

    /// <summary>Protokolleinträge und bitemporale Versionen dürfen nur angefügt, nie gelöscht werden; Protokolleinträge nie geändert.</summary>
    private void NurAnfuegenPruefen()
    {
        var verboten = ChangeTracker.Entries()
            .Where(e => (e.State == EntityState.Deleted && e.Entity is AuditEintrag or IHistorienwert)
                        || (e.State == EntityState.Modified && e.Entity is AuditEintrag))
            .Select(e => e.Entity.GetType().Name)
            .ToList();
        if (verboten.Count > 0)
        {
            throw new InvalidOperationException($"Unzulässige Änderung an nur anfügbaren Daten: {string.Join(", ", verboten.Distinct())}.");
        }
    }
}
