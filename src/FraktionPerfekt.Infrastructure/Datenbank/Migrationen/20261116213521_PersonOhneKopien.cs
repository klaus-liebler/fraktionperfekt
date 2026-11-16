using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116213521_PersonOhneKopien : Migration
{
    private const string Guid = "upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6)))";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Datenbank-Regeln, Etappe 1: Der Titel wird ein eigenes Merkmal (aus jeder Namensversion, nur bei Personen mit Titel),
        // Aktiv und die persönliche Ansprache werden Merkmale (nur abweichend vom Standard). Danach entfallen die Kopien an Personen.
        migrationBuilder.Sql("""
            INSERT INTO PersonMerkmale (Id, Art, Inhalt, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
            SELECT GUID, 'Titel', COALESCE(Titel, ''), PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund
            FROM PersonNamen WHERE PersonId IN (SELECT PersonId FROM PersonNamen WHERE Titel IS NOT NULL AND Titel <> '');
            """.Replace("GUID", Guid));
        foreach (var (spalte, art, wert, standard) in new[]
                 {
                     ("Aktiv", "Aktiv", "CASE Aktiv WHEN 0 THEN 'inaktiv' ELSE 'aktiv' END", "Aktiv = 0"),
                     ("Anredeform", "Anredeform", "Anredeform", "Anredeform <> 'Du'"),
                     ("Begruessungsstil", "Begruessungsstil", "Begruessungsstil", "Begruessungsstil <> 'Locker'"),
                     ("TitelInAnsprache", "TitelInAnsprache", "CASE TitelInAnsprache WHEN 0 THEN 'nein' ELSE 'ja' END", "TitelInAnsprache = 0"),
                 })
        {
            migrationBuilder.Sql($"""
                INSERT INTO PersonMerkmale (Id, Art, Inhalt, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
                SELECT {Guid}, '{art}', {wert}, Id, '0001-01-01', NULL, AngelegtAm, 'Migration', 'Ersterfassung', NULL, 'Übernahme der Spalte {spalte}'
                FROM Personen WHERE {standard};
                """);
        }

        migrationBuilder.DropColumn(
            name: "Aktiv",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Anrede",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Anredeform",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Begruessungsstil",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "EMail",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Mobil",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Nachname",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Titel",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "TitelInAnsprache",
            table: "Personen");

        migrationBuilder.DropColumn(
            name: "Vorname",
            table: "Personen");

        migrationBuilder.RenameColumn(
            name: "Titel",
            table: "PersonNamen",
            newName: "Vorsatz");

        migrationBuilder.Sql("UPDATE PersonNamen SET Vorsatz = NULL;");

        migrationBuilder.AddColumn<bool>(
            name: "NurEinName",
            table: "PersonNamen",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Zusatz",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 30,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NurEinName",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "Zusatz",
            table: "PersonNamen");

        migrationBuilder.RenameColumn(
            name: "Vorsatz",
            table: "PersonNamen",
            newName: "Titel");

        migrationBuilder.AddColumn<bool>(
            name: "Aktiv",
            table: "Personen",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Anrede",
            table: "Personen",
            type: "TEXT",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Anredeform",
            table: "Personen",
            type: "TEXT",
            maxLength: 10,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Begruessungsstil",
            table: "Personen",
            type: "TEXT",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "EMail",
            table: "Personen",
            type: "TEXT",
            maxLength: 320,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Mobil",
            table: "Personen",
            type: "TEXT",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Nachname",
            table: "Personen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Titel",
            table: "Personen",
            type: "TEXT",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "TitelInAnsprache",
            table: "Personen",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Vorname",
            table: "Personen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");
    }
}
