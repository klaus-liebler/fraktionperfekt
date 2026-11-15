using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261115164106_Iteration1 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Aufgaben",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Zustaendig = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Anlass = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                AnlassId = table.Column<Guid>(type: "TEXT", nullable: false),
                Ziel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                FaelligAm = table.Column<long>(type: "INTEGER", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                ErledigtVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aufgaben", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Gremien",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Kurzname = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Sortierung = table.Column<int>(type: "INTEGER", nullable: false),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gremien", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Personen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Vorname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Nachname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                EMail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                Mobil = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                Benutzerkennung = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Personen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Postausgang",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Kanal = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Empfaenger = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                EmpfaengerAnzeige = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Betreff = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Inhalt = table.Column<string>(type: "TEXT", nullable: false),
                Link = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Aktion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                AusgeloestVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Versuche = table.Column<int>(type: "INTEGER", nullable: false),
                ZugestelltAm = table.Column<long>(type: "INTEGER", nullable: true),
                Rueckmeldung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Fehler = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Postausgang", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Protokoll",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Zeitpunkt = table.Column<long>(type: "INTEGER", nullable: false),
                BenutzerKennung = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Benutzer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Rolle = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                Aktion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ObjektTyp = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ObjektId = table.Column<Guid>(type: "TEXT", nullable: true),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Erfolgreich = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Protokoll", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Termine",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beginn = table.Column<DateTime>(type: "TEXT", nullable: false),
                Ende = table.Column<DateTime>(type: "TEXT", nullable: false),
                Ort = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Abgesagt = table.Column<bool>(type: "INTEGER", nullable: false),
                AngelegtVon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Termine", x => x.Id);
                table.ForeignKey(
                    name: "FK_Termine_Gremien_GremiumId",
                    column: x => x.GremiumId,
                    principalTable: "Gremien",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Abwesenheiten",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Von = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Bis = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                ZurueckgezogenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Abwesenheiten", x => x.Id);
                table.ForeignKey(
                    name: "FK_Abwesenheiten_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "KalenderAbos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                WiderrufenAm = table.Column<long>(type: "INTEGER", nullable: true),
                ZuletztAbgerufenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KalenderAbos", x => x.Id);
                table.ForeignKey(
                    name: "FK_KalenderAbos_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Mitgliedschaften",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: false),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Mitgliedschaften", x => x.Id);
                table.ForeignKey(
                    name: "FK_Mitgliedschaften_Gremien_GremiumId",
                    column: x => x.GremiumId,
                    principalTable: "Gremien",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Mitgliedschaften_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonRollen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                RollenText = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
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
                table.PrimaryKey("PK_PersonRollen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonRollen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Teilnahmen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                VertrittPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                GeantwortetAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teilnahmen", x => x.Id);
                table.ForeignKey(
                    name: "FK_Teilnahmen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Teilnahmen_Termine_TerminId",
                    column: x => x.TerminId,
                    principalTable: "Termine",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Vertretungsfaelle",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TerminId = table.Column<Guid>(type: "TEXT", nullable: false),
                GremiumId = table.Column<Guid>(type: "TEXT", nullable: false),
                VertretenePersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                AbwesenheitId = table.Column<Guid>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                VertreterPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                AbgeschlossenAm = table.Column<long>(type: "INTEGER", nullable: true),
                KandidatenText = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vertretungsfaelle", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vertretungsfaelle_Termine_TerminId",
                    column: x => x.TerminId,
                    principalTable: "Termine",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MitgliedschaftsVersionen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Funktion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Rang = table.Column<int>(type: "INTEGER", nullable: true),
                MitgliedschaftId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                table.PrimaryKey("PK_MitgliedschaftsVersionen", x => x.Id);
                table.ForeignKey(
                    name: "FK_MitgliedschaftsVersionen_Mitgliedschaften_MitgliedschaftId",
                    column: x => x.MitgliedschaftId,
                    principalTable: "Mitgliedschaften",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Vertretungsanfragen",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                VertretungsfallId = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                Reihenfolge = table.Column<int>(type: "INTEGER", nullable: false),
                GestelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                FristBis = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                BeantwortetAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vertretungsanfragen", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vertretungsanfragen_Vertretungsfaelle_VertretungsfallId",
                    column: x => x.VertretungsfallId,
                    principalTable: "Vertretungsfaelle",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Abwesenheiten_PersonId_Von",
            table: "Abwesenheiten",
            columns: new[] { "PersonId", "Von" });

        migrationBuilder.CreateIndex(
            name: "IX_Abwesenheiten_Von_Bis",
            table: "Abwesenheiten",
            columns: new[] { "Von", "Bis" });

        migrationBuilder.CreateIndex(
            name: "IX_Aufgaben_Anlass_AnlassId",
            table: "Aufgaben",
            columns: new[] { "Anlass", "AnlassId" });

        migrationBuilder.CreateIndex(
            name: "IX_Aufgaben_Status_Zustaendig",
            table: "Aufgaben",
            columns: new[] { "Status", "Zustaendig" });

        migrationBuilder.CreateIndex(
            name: "IX_Gremien_RisId",
            table: "Gremien",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_KalenderAbos_PersonId",
            table: "KalenderAbos",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_KalenderAbos_TokenHash",
            table: "KalenderAbos",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Mitgliedschaften_GremiumId",
            table: "Mitgliedschaften",
            column: "GremiumId");

        migrationBuilder.CreateIndex(
            name: "IX_Mitgliedschaften_PersonId",
            table: "Mitgliedschaften",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Mitgliedschaften_RisId",
            table: "Mitgliedschaften",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MitgliedschaftsVersionen_MitgliedschaftId_GueltigVon",
            table: "MitgliedschaftsVersionen",
            columns: new[] { "MitgliedschaftId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_Personen_Benutzerkennung",
            table: "Personen",
            column: "Benutzerkennung",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Personen_RisId",
            table: "Personen",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PersonRollen_PersonId_GueltigVon",
            table: "PersonRollen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_Postausgang_Status_ErstelltAm",
            table: "Postausgang",
            columns: new[] { "Status", "ErstelltAm" });

        migrationBuilder.CreateIndex(
            name: "IX_Protokoll_ObjektTyp_ObjektId",
            table: "Protokoll",
            columns: new[] { "ObjektTyp", "ObjektId" });

        migrationBuilder.CreateIndex(
            name: "IX_Protokoll_Zeitpunkt",
            table: "Protokoll",
            column: "Zeitpunkt");

        migrationBuilder.CreateIndex(
            name: "IX_Teilnahmen_PersonId",
            table: "Teilnahmen",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Teilnahmen_TerminId_PersonId",
            table: "Teilnahmen",
            columns: new[] { "TerminId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Termine_Beginn",
            table: "Termine",
            column: "Beginn");

        migrationBuilder.CreateIndex(
            name: "IX_Termine_GremiumId",
            table: "Termine",
            column: "GremiumId");

        migrationBuilder.CreateIndex(
            name: "IX_Termine_RisId",
            table: "Termine",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungsanfragen_PersonId_Status",
            table: "Vertretungsanfragen",
            columns: new[] { "PersonId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungsanfragen_VertretungsfallId",
            table: "Vertretungsanfragen",
            column: "VertretungsfallId");

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungsfaelle_AbwesenheitId",
            table: "Vertretungsfaelle",
            column: "AbwesenheitId");

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungsfaelle_Status",
            table: "Vertretungsfaelle",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungsfaelle_TerminId",
            table: "Vertretungsfaelle",
            column: "TerminId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Abwesenheiten");

        migrationBuilder.DropTable(
            name: "Aufgaben");

        migrationBuilder.DropTable(
            name: "KalenderAbos");

        migrationBuilder.DropTable(
            name: "MitgliedschaftsVersionen");

        migrationBuilder.DropTable(
            name: "PersonRollen");

        migrationBuilder.DropTable(
            name: "Postausgang");

        migrationBuilder.DropTable(
            name: "Protokoll");

        migrationBuilder.DropTable(
            name: "Teilnahmen");

        migrationBuilder.DropTable(
            name: "Vertretungsanfragen");

        migrationBuilder.DropTable(
            name: "Mitgliedschaften");

        migrationBuilder.DropTable(
            name: "Vertretungsfaelle");

        migrationBuilder.DropTable(
            name: "Personen");

        migrationBuilder.DropTable(
            name: "Termine");

        migrationBuilder.DropTable(
            name: "Gremien");
    }
}
