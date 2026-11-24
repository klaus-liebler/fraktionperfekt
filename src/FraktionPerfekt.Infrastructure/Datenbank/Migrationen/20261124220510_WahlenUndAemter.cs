using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124220510_WahlenUndAemter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "KandidatPersonId",
            table: "UmfrageOptionen",
            type: "BLOB",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "WahlId",
            table: "Umfragen",
            type: "BLOB",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Amtszeiten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Amt = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Bezeichnung = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Von = table.Column<int>(type: "INTEGER", nullable: false),
                Bis = table.Column<int>(type: "INTEGER", nullable: true),
                WahlId = table.Column<byte[]>(type: "BLOB", nullable: false),
                NeuwahlErinnertAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Amtszeiten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kandidaturen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                WahlId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                VorgeschlagenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                ZurueckgezogenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kandidaturen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Wahlen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Amt = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Bezeichnung = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Titel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                AmtAb = table.Column<int>(type: "INTEGER", nullable: false),
                AmtBis = table.Column<int>(type: "INTEGER", nullable: true),
                Geheim = table.Column<bool>(type: "INTEGER", nullable: false),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                GewaehltPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Ergebnis = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                AngenommenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Wahlen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Umfragen_WahlId",
            table: "Umfragen",
            column: "WahlId");

        migrationBuilder.CreateIndex(
            name: "IX_Amtszeiten_PersonId",
            table: "Amtszeiten",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Amtszeiten_WahlId",
            table: "Amtszeiten",
            column: "WahlId");

        migrationBuilder.CreateIndex(
            name: "IX_Kandidaturen_WahlId_PersonId",
            table: "Kandidaturen",
            columns: new[] { "WahlId", "PersonId" });

        migrationBuilder.CreateIndex(
            name: "IX_Wahlen_Status",
            table: "Wahlen",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Amtszeiten");

        migrationBuilder.DropTable(
            name: "Kandidaturen");

        migrationBuilder.DropTable(
            name: "Wahlen");

        migrationBuilder.DropIndex(
            name: "IX_Umfragen_WahlId",
            table: "Umfragen");

        migrationBuilder.DropColumn(
            name: "KandidatPersonId",
            table: "UmfrageOptionen");

        migrationBuilder.DropColumn(
            name: "WahlId",
            table: "Umfragen");
    }
}
