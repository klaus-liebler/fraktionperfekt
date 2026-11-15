using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115181330_Iteration7WahlkampfFinanzen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Auslagen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                PostenId = table.Column<Guid>(type: "TEXT", nullable: true),
                EingereichtAm = table.Column<long>(type: "INTEGER", nullable: false),
                BelegSchluessel = table.Column<string>(type: "TEXT", nullable: true),
                BelegName = table.Column<string>(type: "TEXT", nullable: true),
                BelegTyp = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                EntschiedenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true),
                AusgezahltAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auslagen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Gebiete",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Breite = table.Column<double>(type: "REAL", nullable: true),
                Laenge = table.Column<double>(type: "REAL", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ZugeteiltPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gebiete", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Haushaltsposten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Jahr = table.Column<int>(type: "INTEGER", nullable: false),
                Bezeichnung = table.Column<string>(type: "TEXT", nullable: false),
                Ansatz = table.Column<decimal>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Haushaltsposten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kassenbuchungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Einnahme = table.Column<bool>(type: "INTEGER", nullable: false),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                PostenId = table.Column<Guid>(type: "TEXT", nullable: true),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                ErfasstVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                Quelle = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                QuelleId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kassenbuchungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Plakate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Breite = table.Column<double>(type: "REAL", nullable: false),
                Laenge = table.Column<double>(type: "REAL", nullable: false),
                Standort = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Motiv = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                FotoSchluessel = table.Column<string>(type: "TEXT", nullable: true),
                FotoTyp = table.Column<string>(type: "TEXT", nullable: true),
                ErfasstVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Plakate", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Schichteintraege",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SchichtId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Schichteintraege", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Schichten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Ort = table.Column<string>(type: "TEXT", nullable: false),
                Von = table.Column<DateTime>(type: "TEXT", nullable: false),
                Bis = table.Column<DateTime>(type: "TEXT", nullable: false),
                Plaetze = table.Column<int>(type: "INTEGER", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Schichten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Spenden",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Spender = table.Column<string>(type: "TEXT", nullable: true),
                Anschrift = table.Column<string>(type: "TEXT", nullable: true),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                ErfasstVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                QuittungAm = table.Column<DateOnly>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Spenden", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Auslagen_Status",
            table: "Auslagen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Haushaltsposten_Kasse_Jahr",
            table: "Haushaltsposten",
            columns: new[] { "Kasse", "Jahr" });

        migrationBuilder.CreateIndex(
            name: "IX_Kassenbuchungen_Kasse_Datum",
            table: "Kassenbuchungen",
            columns: new[] { "Kasse", "Datum" });

        migrationBuilder.CreateIndex(
            name: "IX_Plakate_Status",
            table: "Plakate",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Schichteintraege_SchichtId_PersonId",
            table: "Schichteintraege",
            columns: new[] { "SchichtId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Schichten_Von",
            table: "Schichten",
            column: "Von");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Auslagen");

        migrationBuilder.DropTable(
            name: "Gebiete");

        migrationBuilder.DropTable(
            name: "Haushaltsposten");

        migrationBuilder.DropTable(
            name: "Kassenbuchungen");

        migrationBuilder.DropTable(
            name: "Plakate");

        migrationBuilder.DropTable(
            name: "Schichteintraege");

        migrationBuilder.DropTable(
            name: "Schichten");

        migrationBuilder.DropTable(
            name: "Spenden");
    }
}
