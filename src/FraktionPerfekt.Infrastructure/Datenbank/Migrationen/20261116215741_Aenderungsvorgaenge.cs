using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116215741_Aenderungsvorgaenge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonRollen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonNamen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "VorgangId",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "Aenderungsvorgaenge",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Von = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ProtokollId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aenderungsvorgaenge", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_VorgangId",
            table: "PersonRufnummern",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonRollen_VorgangId",
            table: "PersonRollen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonNamen_VorgangId",
            table: "PersonNamen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonMerkmale_VorgangId",
            table: "PersonMerkmale",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_VorgangId",
            table: "PersonEMails",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEinwilligungen_VorgangId",
            table: "PersonEinwilligungen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_VorgangId",
            table: "PersonAdressen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_MitgliedschaftsVersionen_VorgangId",
            table: "MitgliedschaftsVersionen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_GremiumVersionen_VorgangId",
            table: "GremiumVersionen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorgaenge_Am",
            table: "Aenderungsvorgaenge",
            column: "Am");

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorgaenge_ProtokollId",
            table: "Aenderungsvorgaenge",
            column: "ProtokollId");

        migrationBuilder.AddForeignKey(
            name: "FK_GremiumVersionen_Aenderungsvorgaenge_VorgangId",
            table: "GremiumVersionen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_MitgliedschaftsVersionen_Aenderungsvorgaenge_VorgangId",
            table: "MitgliedschaftsVersionen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonAdressen_Aenderungsvorgaenge_VorgangId",
            table: "PersonAdressen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonEinwilligungen_Aenderungsvorgaenge_VorgangId",
            table: "PersonEinwilligungen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonEMails_Aenderungsvorgaenge_VorgangId",
            table: "PersonEMails",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonMerkmale_Aenderungsvorgaenge_VorgangId",
            table: "PersonMerkmale",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonNamen_Aenderungsvorgaenge_VorgangId",
            table: "PersonNamen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonRollen_Aenderungsvorgaenge_VorgangId",
            table: "PersonRollen",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PersonRufnummern_Aenderungsvorgaenge_VorgangId",
            table: "PersonRufnummern",
            column: "VorgangId",
            principalTable: "Aenderungsvorgaenge",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // Datenbank-Regel 3: je unterschiedlichem (ErfasstAm, ErfasstVon, Erfassungsart, Grund) ein Änderungsvorgang; Versionen verweisen darauf
        migrationBuilder.Sql("""
            INSERT INTO Aenderungsvorgaenge (Id, Am, Von, Art, Grund, ProtokollId)
            SELECT upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))), ErfasstAm, ErfasstVon, Erfassungsart, Grund, NULL
            FROM (SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonNamen UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonMerkmale UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonAdressen UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonEMails UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonRufnummern UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonEinwilligungen UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM PersonRollen UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM MitgliedschaftsVersionen UNION SELECT ErfasstAm, ErfasstVon, Erfassungsart, Grund FROM GremiumVersionen);
            """);
        foreach (var tabelle in new[] { "PersonNamen", "PersonMerkmale", "PersonAdressen", "PersonEMails", "PersonRufnummern", "PersonEinwilligungen", "PersonRollen", "MitgliedschaftsVersionen", "GremiumVersionen" })
        {
            migrationBuilder.Sql($"""
                UPDATE {tabelle} SET VorgangId = (SELECT a.Id FROM Aenderungsvorgaenge a
                    WHERE a.Am = {tabelle}.ErfasstAm AND a.Von = {tabelle}.ErfasstVon AND a.Art = {tabelle}.Erfassungsart AND a.Grund = {tabelle}.Grund);
                """);
        }

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonRufnummern");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonRufnummern");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonRufnummern");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonRufnummern");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonRollen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonRollen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonRollen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonRollen");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonMerkmale");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonMerkmale");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonMerkmale");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonMerkmale");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonEMails");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonEMails");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonEMails");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonEMails");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonEinwilligungen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonEinwilligungen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonEinwilligungen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonEinwilligungen");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "PersonAdressen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "PersonAdressen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "PersonAdressen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "PersonAdressen");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "ErfasstAm",
            table: "GremiumVersionen");

        migrationBuilder.DropColumn(
            name: "ErfasstVon",
            table: "GremiumVersionen");

        migrationBuilder.DropColumn(
            name: "Erfassungsart",
            table: "GremiumVersionen");

        migrationBuilder.DropColumn(
            name: "Grund",
            table: "GremiumVersionen");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_GremiumVersionen_Aenderungsvorgaenge_VorgangId",
            table: "GremiumVersionen");

        migrationBuilder.DropForeignKey(
            name: "FK_MitgliedschaftsVersionen_Aenderungsvorgaenge_VorgangId",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonAdressen_Aenderungsvorgaenge_VorgangId",
            table: "PersonAdressen");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonEinwilligungen_Aenderungsvorgaenge_VorgangId",
            table: "PersonEinwilligungen");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonEMails_Aenderungsvorgaenge_VorgangId",
            table: "PersonEMails");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonMerkmale_Aenderungsvorgaenge_VorgangId",
            table: "PersonMerkmale");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonNamen_Aenderungsvorgaenge_VorgangId",
            table: "PersonNamen");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonRollen_Aenderungsvorgaenge_VorgangId",
            table: "PersonRollen");

        migrationBuilder.DropForeignKey(
            name: "FK_PersonRufnummern_Aenderungsvorgaenge_VorgangId",
            table: "PersonRufnummern");

        migrationBuilder.DropTable(
            name: "Aenderungsvorgaenge");

        migrationBuilder.DropIndex(
            name: "IX_PersonRufnummern_VorgangId",
            table: "PersonRufnummern");

        migrationBuilder.DropIndex(
            name: "IX_PersonRollen_VorgangId",
            table: "PersonRollen");

        migrationBuilder.DropIndex(
            name: "IX_PersonNamen_VorgangId",
            table: "PersonNamen");

        migrationBuilder.DropIndex(
            name: "IX_PersonMerkmale_VorgangId",
            table: "PersonMerkmale");

        migrationBuilder.DropIndex(
            name: "IX_PersonEMails_VorgangId",
            table: "PersonEMails");

        migrationBuilder.DropIndex(
            name: "IX_PersonEinwilligungen_VorgangId",
            table: "PersonEinwilligungen");

        migrationBuilder.DropIndex(
            name: "IX_PersonAdressen_VorgangId",
            table: "PersonAdressen");

        migrationBuilder.DropIndex(
            name: "IX_MitgliedschaftsVersionen_VorgangId",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropIndex(
            name: "IX_GremiumVersionen_VorgangId",
            table: "GremiumVersionen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonRufnummern");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonRollen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonNamen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonMerkmale");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonEMails");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonEinwilligungen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "PersonAdressen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "MitgliedschaftsVersionen");

        migrationBuilder.DropColumn(
            name: "VorgangId",
            table: "GremiumVersionen");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonRufnummern",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonRufnummern",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonRufnummern",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonRufnummern",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonRollen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonRollen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonRollen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonRollen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonNamen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonNamen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonMerkmale",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonMerkmale",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonMerkmale",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonMerkmale",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonEMails",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonEMails",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonEMails",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonEMails",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonEinwilligungen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonEinwilligungen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonEinwilligungen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonEinwilligungen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "PersonAdressen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "PersonAdressen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "PersonAdressen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "PersonAdressen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "MitgliedschaftsVersionen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "ErfasstAm",
            table: "GremiumVersionen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ErfasstVon",
            table: "GremiumVersionen",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Erfassungsart",
            table: "GremiumVersionen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Grund",
            table: "GremiumVersionen",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");
    }
}
