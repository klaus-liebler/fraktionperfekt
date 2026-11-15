using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115175005_Iteration5Wissen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(Wissen.WikiSuche.Anlegen);

        migrationBuilder.CreateTable(
            name: "Dateien",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Ordner = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Typ = table.Column<string>(type: "TEXT", nullable: false),
                Groesse = table.Column<long>(type: "INTEGER", nullable: false),
                Schluessel = table.Column<string>(type: "TEXT", nullable: false),
                Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                HochgeladenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                Vertraulich = table.Column<bool>(type: "INTEGER", nullable: false),
                BezugArt = table.Column<string>(type: "TEXT", nullable: true),
                BezugId = table.Column<Guid>(type: "TEXT", nullable: true),
                GeloeschtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dateien", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiSeiten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                Vertraulich = table.Column<bool>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false),
                Archiviert = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiSeiten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiVersionen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SeiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                VorschlagId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiVersionen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiVorschlaege",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SeiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                BasisVersion = table.Column<int>(type: "INTEGER", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Begruendung = table.Column<string>(type: "TEXT", nullable: false),
                Vorher = table.Column<string>(type: "TEXT", nullable: false),
                Nachher = table.Column<string>(type: "TEXT", nullable: true),
                Von = table.Column<string>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                EntschiedenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                EntschiedenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiVorschlaege", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Dateien_BezugArt_BezugId",
            table: "Dateien",
            columns: new[] { "BezugArt", "BezugId" });

        migrationBuilder.CreateIndex(
            name: "IX_Dateien_Ordner",
            table: "Dateien",
            column: "Ordner");

        migrationBuilder.CreateIndex(
            name: "IX_WikiSeiten_Titel",
            table: "WikiSeiten",
            column: "Titel",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WikiVersionen_SeiteId_Nummer",
            table: "WikiVersionen",
            columns: new[] { "SeiteId", "Nummer" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WikiVorschlaege_SeiteId_Status",
            table: "WikiVorschlaege",
            columns: new[] { "SeiteId", "Status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS WikiSuche");

        migrationBuilder.DropTable(
            name: "Dateien");

        migrationBuilder.DropTable(
            name: "WikiSeiten");

        migrationBuilder.DropTable(
            name: "WikiVersionen");

        migrationBuilder.DropTable(
            name: "WikiVorschlaege");
    }
}
