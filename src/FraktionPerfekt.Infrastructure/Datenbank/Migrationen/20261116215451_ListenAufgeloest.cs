using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116215451_ListenAufgeloest : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Rolle",
            table: "PersonRollen",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "Vertretungskandidaten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                VertretungsfallId = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vertretungskandidaten", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vertretungskandidaten_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Vertretungskandidaten_Vertretungsfaelle_VertretungsfallId",
                    column: x => x.VertretungsfallId,
                    principalTable: "Vertretungsfaelle",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungskandidaten_PersonId",
            table: "Vertretungskandidaten",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungskandidaten_VertretungsfallId_Position",
            table: "Vertretungskandidaten",
            columns: new[] { "VertretungsfallId", "Position" },
            unique: true);

        // Rollenliste als Text → eine Zeile je Rolle mit derselben Gültigkeit und Erfassung; danach die alten Listenzeilen entfernen
        foreach (var rolle in new[] { "Administration", "Vorstand", "Kassier", "Mitglied", "SachkundigeBuergerin", "Helferin" })
        {
            migrationBuilder.Sql($"""
                INSERT INTO PersonRollen (Id, Rolle, RollenText, PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund)
                SELECT upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))), '{rolle}', '', PersonId, GueltigVon, GueltigBis, ErfasstAm, ErfasstVon, Erfassungsart, ErsetztAm, Grund
                FROM PersonRollen WHERE Rolle = '' AND ',' || RollenText || ',' LIKE '%,{rolle},%';
                """);
        }

        migrationBuilder.Sql("DELETE FROM PersonRollen WHERE Rolle = '';");

        // Kandidatenliste (Ids im Format N, kommagetrennt) → eine Zeile je Person mit Position
        migrationBuilder.Sql("""
            WITH RECURSIVE teile(FallId, Position, Rest, Teil) AS (
                SELECT Id, 0, KandidatenText || ',', NULL FROM Vertretungsfaelle WHERE KandidatenText <> ''
                UNION ALL
                SELECT FallId, Position + 1, substr(Rest, instr(Rest, ',') + 1), substr(Rest, 1, instr(Rest, ',') - 1)
                FROM teile WHERE Rest <> '')
            INSERT INTO Vertretungskandidaten (Id, VertretungsfallId, Position, PersonId)
            SELECT upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))), FallId, Position,
                   upper(substr(Teil, 1, 8) || '-' || substr(Teil, 9, 4) || '-' || substr(Teil, 13, 4) || '-' || substr(Teil, 17, 4) || '-' || substr(Teil, 21, 12))
            FROM teile WHERE Teil IS NOT NULL AND Teil <> '';
            """);

        migrationBuilder.DropColumn(
            name: "KandidatenText",
            table: "Vertretungsfaelle");

        migrationBuilder.DropColumn(
            name: "RollenText",
            table: "PersonRollen");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Vertretungskandidaten");

        migrationBuilder.DropColumn(
            name: "Rolle",
            table: "PersonRollen");

        migrationBuilder.AddColumn<string>(
            name: "KandidatenText",
            table: "Vertretungsfaelle",
            type: "TEXT",
            maxLength: 4000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "RollenText",
            table: "PersonRollen",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            defaultValue: "");
    }
}
