using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116215140_KopienEntfernt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Von",
            table: "WikiVorschlaege",
            newName: "Quelle");

        migrationBuilder.AddColumn<Guid>(
            name: "AngelegtVonPersonId",
            table: "Termine",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ErledigtVonPersonId",
            table: "Aufgaben",
            type: "TEXT",
            nullable: true);

        // Namen als Text werden Personenverweise (Datenbank-Regel 1). Zuordnung über den heutigen Namen (mit Titel) bzw. „Name (Rolle)“;
        // nicht zuordenbare Personennamen bleiben im Vermerk bzw. in der Beschreibung erhalten.
        const string Namen = """
            WITH namen AS (
                SELECT n.PersonId AS PersonId,
                       trim(coalesce((SELECT m.Inhalt || ' ' FROM PersonMerkmale m WHERE m.PersonId = n.PersonId AND m.Art = 'Titel'
                                      AND m.ErsetztAm IS NULL AND m.GueltigBis IS NULL AND m.Inhalt <> ''), '')
                            || CASE WHEN n.Vorname <> '' THEN n.Vorname || ' ' ELSE '' END || coalesce(n.Namensvorsatz || ' ', '') || n.Nachname) AS Name
                FROM PersonNamen n WHERE n.ErsetztAm IS NULL AND n.GueltigBis IS NULL)
            """;
        migrationBuilder.Sql(Namen + """
            UPDATE Aufgaben SET ErledigtVonPersonId = (SELECT PersonId FROM namen WHERE Aufgaben.ErledigtVon = namen.Name OR Aufgaben.ErledigtVon LIKE namen.Name || ' (%' LIMIT 1)
            WHERE ErledigtVon IS NOT NULL;
            """);
        migrationBuilder.Sql("""
            UPDATE Aufgaben SET Vermerk = trim(coalesce(Vermerk || ' ', '') || '(erledigt von ' || ErledigtVon || ')')
            WHERE ErledigtVonPersonId IS NULL AND ErledigtVon IS NOT NULL AND ErledigtVon NOT IN ('Automatik', 'System');
            """);
        migrationBuilder.Sql(Namen + """
            UPDATE Termine SET AngelegtVonPersonId = (SELECT PersonId FROM namen WHERE Termine.AngelegtVon = namen.Name OR Termine.AngelegtVon LIKE namen.Name || ' (%' LIMIT 1);
            """);
        migrationBuilder.Sql("""
            UPDATE Termine SET Beschreibung = trim(coalesce(Beschreibung || char(10) || char(10), '') || '(angelegt von ' || AngelegtVon || ')')
            WHERE AngelegtVonPersonId IS NULL AND RisId IS NULL AND AngelegtVon LIKE '% (%)';
            """);

        migrationBuilder.DropColumn(
            name: "GeaendertAm",
            table: "WikiSeiten");

        migrationBuilder.DropColumn(
            name: "Version",
            table: "WikiSeiten");

        migrationBuilder.DropColumn(
            name: "AngelegtVon",
            table: "Termine");

        migrationBuilder.DropColumn(
            name: "GremiumId",
            table: "Sitzungen");

        migrationBuilder.DropColumn(
            name: "Titel",
            table: "Sitzungen");

        migrationBuilder.DropColumn(
            name: "ErledigtVon",
            table: "Aufgaben");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AngelegtVonPersonId",
            table: "Termine");

        migrationBuilder.DropColumn(
            name: "ErledigtVonPersonId",
            table: "Aufgaben");

        migrationBuilder.RenameColumn(
            name: "Quelle",
            table: "WikiVorschlaege",
            newName: "Von");

        migrationBuilder.AddColumn<long>(
            name: "GeaendertAm",
            table: "WikiSeiten",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<int>(
            name: "Version",
            table: "WikiSeiten",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "AngelegtVon",
            table: "Termine",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<Guid>(
            name: "GremiumId",
            table: "Sitzungen",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Titel",
            table: "Sitzungen",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ErledigtVon",
            table: "Aufgaben",
            type: "TEXT",
            maxLength: 200,
            nullable: true);
    }
}
