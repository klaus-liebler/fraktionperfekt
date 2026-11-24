using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124223551_Rundschreiben : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Rundschreiben",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Betreff = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                Gestaltung = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Anredestil = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Sammelanrede = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Kreis = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                VorlageId = table.Column<byte[]>(type: "BLOB", nullable: true),
                VersandtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Empfaenger = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rundschreiben", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Rundschreiben_ErstelltAm",
            table: "Rundschreiben",
            column: "ErstelltAm");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Rundschreiben");
    }
}
