using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116191341_GremienAemter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Amt",
            table: "Mitgliedschaften");

        migrationBuilder.DropColumn(
            name: "FunktionText",
            table: "Mitgliedschaften");
    }
}
