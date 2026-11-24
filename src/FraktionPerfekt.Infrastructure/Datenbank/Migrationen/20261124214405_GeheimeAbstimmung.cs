using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124214405_GeheimeAbstimmung : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Geheim",
            table: "Umfragen",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "Stimmberechtigungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                UmfrageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stimmberechtigungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Stimmvermerke",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                UmfrageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stimmvermerke", x => x.Id);
            });

        // Stimmzettel geheimer Abstimmungen: von Hand als WITHOUT ROWID angelegt – Schlüssel ist die zufällige Id (UUIDv4), damit weder eine
        // fortlaufende rowid noch die physische Reihenfolge in der Datei die Reihenfolge der Stimmabgabe verrät (Architekturtest prüft das).
        migrationBuilder.Sql("""
            CREATE TABLE "Stimmzettel" (
                "Id" BLOB NOT NULL CONSTRAINT "PK_Stimmzettel" PRIMARY KEY,
                "UmfrageId" BLOB NOT NULL,
                "OptionId" BLOB NOT NULL
            ) WITHOUT ROWID;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Stimmberechtigungen_UmfrageId_PersonId",
            table: "Stimmberechtigungen",
            columns: new[] { "UmfrageId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Stimmvermerke_UmfrageId_PersonId",
            table: "Stimmvermerke",
            columns: new[] { "UmfrageId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Stimmzettel_UmfrageId",
            table: "Stimmzettel",
            column: "UmfrageId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Stimmberechtigungen");

        migrationBuilder.DropTable(
            name: "Stimmvermerke");

        migrationBuilder.DropTable(
            name: "Stimmzettel");

        migrationBuilder.DropColumn(
            name: "Geheim",
            table: "Umfragen");
    }
}
