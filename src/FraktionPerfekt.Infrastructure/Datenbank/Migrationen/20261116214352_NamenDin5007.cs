using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116214352_NamenDin5007 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Zusatz",
            table: "PersonNamen",
            newName: "Namenszusatz");

        migrationBuilder.RenameColumn(
            name: "Vorsatz",
            table: "PersonNamen",
            newName: "Namensvorsatz");

        migrationBuilder.AlterColumn<string>(
            name: "Vorname",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            collation: "DIN5007_2",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "Nachname",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            collation: "DIN5007_2",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "Namensvorsatz",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 60,
            nullable: true,
            collation: "DIN5007_2",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 60,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PersonNamen_DIN5007",
            table: "PersonNamen",
            columns: new[] { "Nachname", "Vorname", "Namensvorsatz" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PersonNamen_DIN5007",
            table: "PersonNamen");

        migrationBuilder.RenameColumn(
            name: "Namenszusatz",
            table: "PersonNamen",
            newName: "Zusatz");

        migrationBuilder.RenameColumn(
            name: "Namensvorsatz",
            table: "PersonNamen",
            newName: "Vorsatz");

        migrationBuilder.AlterColumn<string>(
            name: "Vorname",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 200,
            oldCollation: "DIN5007_2");

        migrationBuilder.AlterColumn<string>(
            name: "Nachname",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 200,
            oldCollation: "DIN5007_2");

        migrationBuilder.AlterColumn<string>(
            name: "Vorsatz",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 60,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 60,
            oldNullable: true,
            oldCollation: "DIN5007_2");
    }
}
