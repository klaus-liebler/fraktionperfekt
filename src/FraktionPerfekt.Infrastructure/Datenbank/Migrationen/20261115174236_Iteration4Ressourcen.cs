using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115174236_Iteration4Ressourcen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Besetzungsanfragen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: false),
                Plaetze = table.Column<int>(type: "INTEGER", nullable: false),
                Frist = table.Column<long>(type: "INTEGER", nullable: false),
                Modus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                Hinweis = table.Column<string>(type: "TEXT", nullable: true),
                Bericht = table.Column<string>(type: "TEXT", nullable: true),
                BerichtVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                BerichtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Besetzungsanfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Besetzungsmeldungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AnfrageId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Besetzungsmeldungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Buchungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                RessourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Von = table.Column<DateTime>(type: "TEXT", nullable: false),
                Bis = table.Column<DateTime>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                EntschiedenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Buchungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Ressourcen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                FreigabeNoetig = table.Column<bool>(type: "INTEGER", nullable: false),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ressourcen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Besetzungsanfragen_TerminId",
            table: "Besetzungsanfragen",
            column: "TerminId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Besetzungsmeldungen_AnfrageId_PersonId",
            table: "Besetzungsmeldungen",
            columns: new[] { "AnfrageId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Buchungen_RessourceId_Von",
            table: "Buchungen",
            columns: new[] { "RessourceId", "Von" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Besetzungsanfragen");

        migrationBuilder.DropTable(
            name: "Besetzungsmeldungen");

        migrationBuilder.DropTable(
            name: "Buchungen");

        migrationBuilder.DropTable(
            name: "Ressourcen");
    }
}
