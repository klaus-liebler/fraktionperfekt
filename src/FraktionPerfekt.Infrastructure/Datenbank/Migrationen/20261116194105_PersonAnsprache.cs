using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116194105_PersonAnsprache : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            defaultValue: "Du");

        migrationBuilder.AddColumn<string>(
            name: "Begruessungsstil",
            table: "Personen",
            type: "TEXT",
            maxLength: 20,
            nullable: false,
            defaultValue: "Locker");

        migrationBuilder.AddColumn<bool>(
            name: "TitelInAnsprache",
            table: "Personen",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
            name: "TitelInAnsprache",
            table: "Personen");
    }
}
