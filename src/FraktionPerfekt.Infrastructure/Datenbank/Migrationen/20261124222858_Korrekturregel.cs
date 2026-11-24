using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261124222858_Korrekturregel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "Beschluesse",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "ErfasstVonPersonId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Korrekturgrund",
            table: "Beschluesse",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ZurueckgezogenAm",
            table: "Beschluesse",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Korrekturgrund",
            table: "AntragKommentare",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ZurueckgezogenAm",
            table: "AntragKommentare",
            type: "INTEGER",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "Beschluesse");

        migrationBuilder.DropColumn(
            name: "ErfasstVonPersonId",
            table: "Beschluesse");

        migrationBuilder.DropColumn(
            name: "Korrekturgrund",
            table: "Beschluesse");

        migrationBuilder.DropColumn(
            name: "ZurueckgezogenAm",
            table: "Beschluesse");

        migrationBuilder.DropColumn(
            name: "Korrekturgrund",
            table: "AntragKommentare");

        migrationBuilder.DropColumn(
            name: "ZurueckgezogenAm",
            table: "AntragKommentare");
    }
}
