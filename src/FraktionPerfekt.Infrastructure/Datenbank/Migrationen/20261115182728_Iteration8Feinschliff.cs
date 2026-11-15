using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115182728_Iteration8Feinschliff : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PushAbos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Endpunkt = table.Column<string>(type: "TEXT", nullable: false),
                P256dh = table.Column<string>(type: "TEXT", nullable: false),
                Auth = table.Column<string>(type: "TEXT", nullable: false),
                Geraet = table.Column<string>(type: "TEXT", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                ZuletztZugestelltAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PushAbos", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TerminErinnerungen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TerminErinnerungen", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PushAbos_Endpunkt",
            table: "PushAbos",
            column: "Endpunkt",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PushAbos_PersonId",
            table: "PushAbos",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_TerminErinnerungen_TerminId_PersonId",
            table: "TerminErinnerungen",
            columns: new[] { "TerminId", "PersonId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PushAbos");

        migrationBuilder.DropTable(
            name: "TerminErinnerungen");
    }
}
