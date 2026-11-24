using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124221654_BeamerZugaenge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BeamerZugaenge",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                SitzungId = table.Column<byte[]>(type: "BLOB", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<long>(type: "INTEGER", nullable: false),
                GesperrtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BeamerZugaenge", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BeamerZugaenge_SitzungId",
            table: "BeamerZugaenge",
            column: "SitzungId");

        migrationBuilder.CreateIndex(
            name: "IX_BeamerZugaenge_TokenHash",
            table: "BeamerZugaenge",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BeamerZugaenge");
    }
}
