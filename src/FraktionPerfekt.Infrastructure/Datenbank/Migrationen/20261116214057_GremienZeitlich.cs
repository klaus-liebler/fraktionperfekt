using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116214057_GremienZeitlich : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Amt",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "FunktionText",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "GremiumVersionen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Kurzname = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: false),
                GueltigVon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                GueltigBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ErfasstVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Erfassungsart = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GremiumVersionen", x => x.Id);
                table.ForeignKey(
                    name: "FK_GremiumVersionen_Gremien_GremiumId",
                    column: x => x.GremiumId,
                    principalTable: "Gremien",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GremiumVersionen_GremiumId_GueltigVon",
            table: "GremiumVersionen",
            columns: new[] { "GremiumId", "GueltigVon" });

        // Daten übernehmen, bevor die alten Spalten entfallen (Datenbank-Regeln, Etappe 1b)
        migrationBuilder.Sql("""
            UPDATE MitgliedschaftsVersionen SET
                Amt = (SELECT m.Amt FROM Mitgliedschaften m WHERE m.Id = MitgliedschaftsVersionen.MitgliedschaftId),
                FunktionText = (SELECT m.FunktionText FROM Mitgliedschaften m WHERE m.Id = MitgliedschaftsVersionen.MitgliedschaftId);
            """);
        var jetzt = (long)new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter().ConvertToProvider(DateTimeOffset.UtcNow)!;
        migrationBuilder.Sql($"""
            INSERT INTO GremiumVersionen (Id, Name, Kurzname, Aktiv, GremiumId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
            SELECT upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))),
                   Name, Kurzname, Aktiv, Id, '0001-01-01', NULL, {jetzt}, 'Migration', 'Ersterfassung', NULL, 'Übernahme aus Gremien'
            FROM Gremien;
            """);

        migrationBuilder.DropColumn(
            name: "Amt",
            table: "Mitgliedschaften");

        migrationBuilder.DropColumn(
            name: "FunktionText",
            table: "Mitgliedschaften");

        migrationBuilder.DropColumn(
            name: "Aktiv",
            table: "Gremien");

        migrationBuilder.DropColumn(
            name: "Kurzname",
            table: "Gremien");

        migrationBuilder.DropColumn(
            name: "Name",
            table: "Gremien");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GremiumVersionen");

        migrationBuilder.DropColumn(
            name: "Amt",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "FunktionText",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.AddColumn<string>(
            name: "Amt",
            table: "Mitgliedschaften",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "FunktionText",
            table: "Mitgliedschaften",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "Aktiv",
            table: "Gremien",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Kurzname",
            table: "Gremien",
            type: "TEXT",
            maxLength: 60,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "Gremien",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            defaultValue: "");
    }
}
