using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116195641_PersonKontakte : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PersonAdressen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AdressId = table.Column<Guid>(type: "TEXT", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Zusatz = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Strasse = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Postleitzahl = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Ort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GueltigVon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                GueltigBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ErfasstVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Erfassungsart = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonAdressen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonAdressen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonEinwilligungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Erteilt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GueltigVon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                GueltigBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ErfasstVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Erfassungsart = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonEinwilligungen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonEinwilligungen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonEMails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AdressId = table.Column<Guid>(type: "TEXT", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Adresse = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GueltigVon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                GueltigBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ErfasstVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Erfassungsart = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonEMails", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonEMails_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonRufnummern",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                NummerId = table.Column<Guid>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Nummer = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Dienste = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Einwilligung = table.Column<bool>(type: "INTEGER", nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GueltigVon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                GueltigBis = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ErfasstVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Erfassungsart = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonRufnummern", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonRufnummern_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_AdressId",
            table: "PersonAdressen",
            column: "AdressId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_PersonId_GueltigVon",
            table: "PersonAdressen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonEinwilligungen_PersonId_GueltigVon",
            table: "PersonEinwilligungen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_AdressId",
            table: "PersonEMails",
            column: "AdressId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_PersonId_GueltigVon",
            table: "PersonEMails",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_NummerId",
            table: "PersonRufnummern",
            column: "NummerId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_PersonId_GueltigVon",
            table: "PersonRufnummern",
            columns: new[] { "PersonId", "GueltigVon" });

        // Bisherige E-Mail- und Mobil-Merkmale werden zu (bevorzugten) Kontakten; leere Angaben werden zu Endemarken.
        migrationBuilder.Sql("""
            INSERT INTO PersonEMails (Id, AdressId, Bereich, Adresse, Bevorzugt, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
            SELECT Id, PersonId, 'Privat', CASE WHEN Inhalt = '' THEN '–' ELSE Inhalt END, 1, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon,
                   CASE WHEN Inhalt = '' THEN 'Beendet' ELSE Erfassungsart END, ErsetztAm, Grund
            FROM PersonMerkmale WHERE Art = 'EMail';
            """);
        migrationBuilder.Sql("""
            INSERT INTO PersonRufnummern (Id, NummerId, Art, Nummer, Dienste, Einwilligung, Bevorzugt, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
            SELECT Id, PersonId, 'Mobil', CASE WHEN Inhalt = '' THEN '–' ELSE Inhalt END, 'Anruf, Sms', 0, 1, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon,
                   CASE WHEN Inhalt = '' THEN 'Beendet' ELSE Erfassungsart END, ErsetztAm, Grund
            FROM PersonMerkmale WHERE Art = 'Mobil';
            """);
        migrationBuilder.Sql("DELETE FROM PersonMerkmale WHERE Art IN ('EMail', 'Mobil');");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PersonAdressen");

        migrationBuilder.DropTable(
            name: "PersonEinwilligungen");

        migrationBuilder.DropTable(
            name: "PersonEMails");

        migrationBuilder.DropTable(
            name: "PersonRufnummern");
    }
}
