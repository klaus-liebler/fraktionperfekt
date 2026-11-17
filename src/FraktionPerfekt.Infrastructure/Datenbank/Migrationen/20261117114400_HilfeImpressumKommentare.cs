using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261117114400_HilfeImpressumKommentare : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Impressum",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Impressum", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kommentare",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Dokument = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Abschnitt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AbschnittTitel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                VerfasstVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                AntwortAuf = table.Column<byte[]>(type: "BLOB", nullable: true),
                Erledigt = table.Column<bool>(type: "INTEGER", nullable: false),
                ErledigtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kommentare", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Kommentare_AntwortAuf",
            table: "Kommentare",
            column: "AntwortAuf");

        migrationBuilder.CreateIndex(
            name: "IX_Kommentare_Dokument_Abschnitt",
            table: "Kommentare",
            columns: new[] { "Dokument", "Abschnitt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Impressum");

        migrationBuilder.DropTable(
            name: "Kommentare");
    }
}
