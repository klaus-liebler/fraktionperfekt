using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115173602_Iteration3Antraege : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Aenderungsvorschlaege",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: false),
                Fassung = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Alt = table.Column<string>(type: "TEXT", nullable: false),
                Neu = table.Column<string>(type: "TEXT", nullable: false),
                Begruendung = table.Column<string>(type: "TEXT", nullable: true),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                EntschiedenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aenderungsvorschlaege", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Antraege",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Nummer = table.Column<string>(type: "TEXT", nullable: true),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                AutorPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ZielGremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                SitzungTerminId = table.Column<Guid>(type: "TEXT", nullable: true),
                AbstimmungId = table.Column<Guid>(type: "TEXT", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false),
                EingereichtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Antraege", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AntragFassungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: false),
                Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Notiz = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AntragFassungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AntragKommentare",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: false),
                Fassung = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Zitat = table.Column<string>(type: "TEXT", nullable: true),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Erledigt = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AntragKommentare", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Stimmen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UmfrageId = table.Column<Guid>(type: "TEXT", nullable: false),
                OptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stimmen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Umfragen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Frist = table.Column<long>(type: "INTEGER", nullable: false),
                QuorumProzent = table.Column<int>(type: "INTEGER", nullable: false),
                Mehrfachauswahl = table.Column<bool>(type: "INTEGER", nullable: false),
                Berechtigte = table.Column<int>(type: "INTEGER", nullable: false),
                ErstelltVonPersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                AbgeschlossenAm = table.Column<long>(type: "INTEGER", nullable: true),
                Ergebnis = table.Column<string>(type: "TEXT", nullable: true),
                GewinnerOptionId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Umfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UmfrageOptionen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UmfrageId = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Zeitpunkt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UmfrageOptionen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorschlaege_AntragId",
            table: "Aenderungsvorschlaege",
            column: "AntragId");

        migrationBuilder.CreateIndex(
            name: "IX_Antraege_Nummer",
            table: "Antraege",
            column: "Nummer");

        migrationBuilder.CreateIndex(
            name: "IX_Antraege_Status",
            table: "Antraege",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_AntragFassungen_AntragId_Nummer",
            table: "AntragFassungen",
            columns: new[] { "AntragId", "Nummer" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AntragKommentare_AntragId",
            table: "AntragKommentare",
            column: "AntragId");

        migrationBuilder.CreateIndex(
            name: "IX_Stimmen_UmfrageId_PersonId",
            table: "Stimmen",
            columns: new[] { "UmfrageId", "PersonId" });

        migrationBuilder.CreateIndex(
            name: "IX_Umfragen_Status",
            table: "Umfragen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_UmfrageOptionen_UmfrageId",
            table: "UmfrageOptionen",
            column: "UmfrageId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Aenderungsvorschlaege");

        migrationBuilder.DropTable(
            name: "Antraege");

        migrationBuilder.DropTable(
            name: "AntragFassungen");

        migrationBuilder.DropTable(
            name: "AntragKommentare");

        migrationBuilder.DropTable(
            name: "Stimmen");

        migrationBuilder.DropTable(
            name: "Umfragen");

        migrationBuilder.DropTable(
            name: "UmfrageOptionen");
    }
}
