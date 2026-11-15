using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115172355_Iteration2Sitzungen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Zustaendig",
            table: "Aufgaben",
            type: "TEXT",
            maxLength: 40,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 40);

        migrationBuilder.AddColumn<long>(
            name: "ErinnertAm",
            table: "Aufgaben",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Thema",
            table: "Aufgaben",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ZugewiesenPersonId",
            table: "Aufgaben",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Beschluesse",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Quelle = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Ergebnis = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Ja = table.Column<int>(type: "INTEGER", nullable: false),
                Nein = table.Column<int>(type: "INTEGER", nullable: false),
                Enthaltung = table.Column<int>(type: "INTEGER", nullable: false),
                SitzungId = table.Column<Guid>(type: "TEXT", nullable: true),
                TopId = table.Column<Guid>(type: "TEXT", nullable: true),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: true),
                RisId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Beschluesse", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Sitzungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Vorlage = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                TopAnmeldungBis = table.Column<long>(type: "INTEGER", nullable: true),
                LeitungPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                ProtokollPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                EingeladenAm = table.Column<long>(type: "INTEGER", nullable: true),
                BegonnenAm = table.Column<long>(type: "INTEGER", nullable: true),
                BeendetAm = table.Column<long>(type: "INTEGER", nullable: true),
                FreigegebenAm = table.Column<long>(type: "INTEGER", nullable: true),
                FreigegebenVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sitzungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Sitzungsanwesenheiten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SitzungId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sitzungsanwesenheiten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Themenpaten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Politikfeld = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                VertretungPersonId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Themenpaten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tops",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SitzungId = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Fest = table.Column<bool>(type: "INTEGER", nullable: false),
                AngemeldetVonPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                AntragId = table.Column<Guid>(type: "TEXT", nullable: true),
                VertagtAusTopId = table.Column<Guid>(type: "TEXT", nullable: true),
                UebernommenInSitzungId = table.Column<Guid>(type: "TEXT", nullable: true),
                ZusammengelegtMitTopId = table.Column<Guid>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Notiz = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tops", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Beschluesse_Datum",
            table: "Beschluesse",
            column: "Datum");

        migrationBuilder.CreateIndex(
            name: "IX_Sitzungen_TerminId",
            table: "Sitzungen",
            column: "TerminId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Sitzungsanwesenheiten_SitzungId_PersonId",
            table: "Sitzungsanwesenheiten",
            columns: new[] { "SitzungId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tops_SitzungId_Position",
            table: "Tops",
            columns: new[] { "SitzungId", "Position" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Beschluesse");

        migrationBuilder.DropTable(
            name: "Sitzungen");

        migrationBuilder.DropTable(
            name: "Sitzungsanwesenheiten");

        migrationBuilder.DropTable(
            name: "Themenpaten");

        migrationBuilder.DropTable(
            name: "Tops");

        migrationBuilder.DropColumn(
            name: "ErinnertAm",
            table: "Aufgaben");

        migrationBuilder.DropColumn(
            name: "Thema",
            table: "Aufgaben");

        migrationBuilder.DropColumn(
            name: "ZugewiesenPersonId",
            table: "Aufgaben");

        migrationBuilder.AlterColumn<string>(
            name: "Zustaendig",
            table: "Aufgaben",
            type: "TEXT",
            maxLength: 40,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 40,
            oldNullable: true);
    }
}
