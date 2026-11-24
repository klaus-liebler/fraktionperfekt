using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124221123_Sitzungsabstimmung : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "TopId",
            table: "Umfragen",
            type: "BLOB",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "UmfrageId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TopId",
            table: "Umfragen");

        migrationBuilder.DropColumn(
            name: "UmfrageId",
            table: "Beschluesse");
    }
}
