using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115180403_Iteration6Politik : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Anfragen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                AutorPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                EingereichtAm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                FristBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Antwort = table.Column<string>(type: "TEXT", nullable: true),
                BeantwortetAm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErinnertAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Anfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Buergeranliegen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Betreff = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Kontaktweg = table.Column<string>(type: "TEXT", nullable: true),
                Kanal = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Eingang = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ZugewiesenPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                Antwort = table.Column<string>(type: "TEXT", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Anonymisiert = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Buergeranliegen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kontakte",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Organisation = table.Column<string>(type: "TEXT", nullable: true),
                Funktion = table.Column<string>(type: "TEXT", nullable: true),
                EMail = table.Column<string>(type: "TEXT", nullable: true),
                Telefon = table.Column<string>(type: "TEXT", nullable: true),
                Themen = table.Column<string>(type: "TEXT", nullable: true),
                Notiz = table.Column<string>(type: "TEXT", nullable: true),
                AngelegtVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kontakte", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Presseartikel",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Medium = table.Column<string>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Link = table.Column<string>(type: "TEXT", nullable: true),
                Zusammenfassung = table.Column<string>(type: "TEXT", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Tonlage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                PressemitteilungId = table.Column<Guid>(type: "TEXT", nullable: true),
                ErfasstVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Presseartikel", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Pressemitteilungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                GeplantAm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                AutorPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                FreigegebenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                FreigegebenAm = table.Column<long>(type: "INTEGER", nullable: true),
                VeroeffentlichtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pressemitteilungen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Anfragen_Status",
            table: "Anfragen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Buergeranliegen_Status",
            table: "Buergeranliegen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Kontakte_Name",
            table: "Kontakte",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Presseartikel_Datum",
            table: "Presseartikel",
            column: "Datum");

        migrationBuilder.CreateIndex(
            name: "IX_Pressemitteilungen_Status",
            table: "Pressemitteilungen",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Anfragen");

        migrationBuilder.DropTable(
            name: "Buergeranliegen");

        migrationBuilder.DropTable(
            name: "Kontakte");

        migrationBuilder.DropTable(
            name: "Presseartikel");

        migrationBuilder.DropTable(
            name: "Pressemitteilungen");
    }
}
