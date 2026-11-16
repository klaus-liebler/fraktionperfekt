using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116184935_WikiBilder : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Bilder",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Schluessel = table.Column<string>(type: "TEXT", nullable: false),
                OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                Typ = table.Column<string>(type: "TEXT", nullable: false),
                Groesse = table.Column<long>(type: "INTEGER", nullable: false),
                Breite = table.Column<int>(type: "INTEGER", nullable: false),
                Hoehe = table.Column<int>(type: "INTEGER", nullable: false),
                Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                HochgeladenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                GeloeschtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bilder", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BildVerwendungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                BildId = table.Column<Guid>(type: "TEXT", nullable: false),
                BezugArt = table.Column<string>(type: "TEXT", nullable: false),
                BezugId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BildVerwendungen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bilder_Sha256",
            table: "Bilder",
            column: "Sha256");

        migrationBuilder.CreateIndex(
            name: "IX_BildVerwendungen_BezugArt_BezugId",
            table: "BildVerwendungen",
            columns: new[] { "BezugArt", "BezugId" });

        migrationBuilder.CreateIndex(
            name: "IX_BildVerwendungen_BildId_BezugArt_BezugId",
            table: "BildVerwendungen",
            columns: new[] { "BildId", "BezugArt", "BezugId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Bilder");

        migrationBuilder.DropTable(
            name: "BildVerwendungen");
    }
}
