using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using System.Linq;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116220246_IdsBinaer : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<byte[]>(
            name: "SeiteId",
            table: "WikiVorschlaege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "EntschiedenVonPersonId",
            table: "WikiVorschlaege",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "WikiVorschlaege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorschlagId",
            table: "WikiVersionen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "SeiteId",
            table: "WikiVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "WikiVersionen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "WikiVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "WikiSeiten",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "WikiSeiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertretungsfallId",
            table: "Vertretungskandidaten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Vertretungskandidaten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Vertretungskandidaten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertreterPersonId",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertretenePersonId",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AbwesenheitId",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Vertretungsfaelle",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertretungsfallId",
            table: "Vertretungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Vertretungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Vertretungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "UmfrageId",
            table: "UmfrageOptionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "UmfrageOptionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GewinnerOptionId",
            table: "Umfragen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErstelltVonPersonId",
            table: "Umfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "Umfragen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Umfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ZusammengelegtMitTopId",
            table: "Tops",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertagtAusTopId",
            table: "Tops",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "UebernommenInSitzungId",
            table: "Tops",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "SitzungId",
            table: "Tops",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "Tops",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AngemeldetVonPersonId",
            table: "Tops",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Tops",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertretungPersonId",
            table: "Themenpaten",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Themenpaten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Themenpaten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "TerminErinnerungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "TerminErinnerungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "TerminErinnerungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Termine",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AngelegtVonPersonId",
            table: "Termine",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Termine",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VertrittPersonId",
            table: "Teilnahmen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Teilnahmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Teilnahmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Teilnahmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "UmfrageId",
            table: "Stimmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Stimmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "OptionId",
            table: "Stimmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Stimmen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErfasstVonPersonId",
            table: "Spenden",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Spenden",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "SitzungId",
            table: "Sitzungsanwesenheiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Sitzungsanwesenheiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Sitzungsanwesenheiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Sitzungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ProtokollPersonId",
            table: "Sitzungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "LeitungPersonId",
            table: "Sitzungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "FreigegebenVonPersonId",
            table: "Sitzungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Sitzungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Schichten",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Schichten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "SchichtId",
            table: "Schichteintraege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Schichteintraege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Schichteintraege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Ressourcen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PushAbos",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PushAbos",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ObjektId",
            table: "Protokoll",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Protokoll",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "FreigegebenVonPersonId",
            table: "Pressemitteilungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AutorPersonId",
            table: "Pressemitteilungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Pressemitteilungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PressemitteilungId",
            table: "Presseartikel",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErfasstVonPersonId",
            table: "Presseartikel",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Presseartikel",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Postausgang",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Postausgang",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GeaendertVonPersonId",
            table: "Plakate",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErfasstVonPersonId",
            table: "Plakate",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Plakate",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonRufnummern",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonRufnummern",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "NummerId",
            table: "PersonRufnummern",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonRufnummern",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonRollen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonRollen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonRollen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonNamen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonNamen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonNamen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonMerkmale",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonMerkmale",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonMerkmale",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Personen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonEMails",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonEMails",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AdressId",
            table: "PersonEMails",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonEMails",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonEinwilligungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonEinwilligungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonEinwilligungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "PersonAdressen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "PersonAdressen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AdressId",
            table: "PersonAdressen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "PersonAdressen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "MitgliedschaftsVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "MitgliedschaftId",
            table: "MitgliedschaftsVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "MitgliedschaftsVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Mitgliedschaften",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Mitgliedschaften",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Mitgliedschaften",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AngelegtVonPersonId",
            table: "Kontakte",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Kontakte",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "QuelleId",
            table: "Kassenbuchungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "PostenId",
            table: "Kassenbuchungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErfasstVonPersonId",
            table: "Kassenbuchungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Kassenbuchungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "KalenderAbos",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "KalenderAbos",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Haushaltsposten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "VorgangId",
            table: "GremiumVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "GremiumVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "GremiumVersionen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Gremien",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ZugeteiltPersonId",
            table: "Gebiete",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Gebiete",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "HochgeladenVonPersonId",
            table: "Dateien",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Dateien",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "BezugId",
            table: "Dateien",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Dateien",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ZugewiesenPersonId",
            table: "Buergeranliegen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Buergeranliegen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Buchungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "RessourceId",
            table: "Buchungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Buchungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "EntschiedenVonPersonId",
            table: "Buchungen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Buchungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "BildId",
            table: "BildVerwendungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "BezugId",
            table: "BildVerwendungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "BildVerwendungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "HochgeladenVonPersonId",
            table: "Bilder",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Bilder",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Besetzungsmeldungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AnfrageId",
            table: "Besetzungsmeldungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Besetzungsmeldungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TerminId",
            table: "Besetzungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErstelltVonPersonId",
            table: "Besetzungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "BerichtVonPersonId",
            table: "Besetzungsanfragen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Besetzungsanfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "TopId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "SitzungId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "Beschluesse",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Beschluesse",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PostenId",
            table: "Auslagen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Auslagen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "EntschiedenVonPersonId",
            table: "Auslagen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Auslagen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ZugewiesenPersonId",
            table: "Aufgaben",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "ErledigtVonPersonId",
            table: "Aufgaben",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AnlassId",
            table: "Aufgaben",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Aufgaben",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "AntragKommentare",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "AntragKommentare",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "AntragKommentare",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "AntragFassungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "AntragFassungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "AntragFassungen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ZielGremiumId",
            table: "Antraege",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "SitzungTerminId",
            table: "Antraege",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AutorPersonId",
            table: "Antraege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "AbstimmungId",
            table: "Antraege",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Antraege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "GremiumId",
            table: "Anfragen",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AutorPersonId",
            table: "Anfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Anfragen",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Aenderungsvorschlaege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "EntschiedenVonPersonId",
            table: "Aenderungsvorschlaege",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "AntragId",
            table: "Aenderungsvorschlaege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Aenderungsvorschlaege",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "ProtokollId",
            table: "Aenderungsvorgaenge",
            type: "BLOB",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Aenderungsvorgaenge",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "PersonId",
            table: "Abwesenheiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "Abwesenheiten",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "SeiteId",
            table: "WikiVorschlaege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "EntschiedenVonPersonId",
            table: "WikiVorschlaege",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "WikiVorschlaege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorschlagId",
            table: "WikiVersionen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "SeiteId",
            table: "WikiVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "WikiVersionen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "WikiVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "WikiSeiten",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "WikiSeiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VertretungsfallId",
            table: "Vertretungskandidaten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Vertretungskandidaten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Vertretungskandidaten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VertreterPersonId",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "VertretenePersonId",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AbwesenheitId",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Vertretungsfaelle",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VertretungsfallId",
            table: "Vertretungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Vertretungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Vertretungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "UmfrageId",
            table: "UmfrageOptionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "UmfrageOptionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GewinnerOptionId",
            table: "Umfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "ErstelltVonPersonId",
            table: "Umfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "Umfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Umfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ZusammengelegtMitTopId",
            table: "Tops",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "VertagtAusTopId",
            table: "Tops",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "UebernommenInSitzungId",
            table: "Tops",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "SitzungId",
            table: "Tops",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "Tops",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AngemeldetVonPersonId",
            table: "Tops",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Tops",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VertretungPersonId",
            table: "Themenpaten",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Themenpaten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Themenpaten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "TerminErinnerungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "TerminErinnerungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "TerminErinnerungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Termine",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AngelegtVonPersonId",
            table: "Termine",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Termine",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VertrittPersonId",
            table: "Teilnahmen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Teilnahmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Teilnahmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Teilnahmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "UmfrageId",
            table: "Stimmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Stimmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "OptionId",
            table: "Stimmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Stimmen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ErfasstVonPersonId",
            table: "Spenden",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Spenden",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "SitzungId",
            table: "Sitzungsanwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Sitzungsanwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Sitzungsanwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Sitzungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ProtokollPersonId",
            table: "Sitzungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "LeitungPersonId",
            table: "Sitzungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "FreigegebenVonPersonId",
            table: "Sitzungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Sitzungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Schichten",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Schichten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "SchichtId",
            table: "Schichteintraege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Schichteintraege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Schichteintraege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Ressourcen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PushAbos",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PushAbos",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ObjektId",
            table: "Protokoll",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Protokoll",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "FreigegebenVonPersonId",
            table: "Pressemitteilungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AutorPersonId",
            table: "Pressemitteilungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Pressemitteilungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PressemitteilungId",
            table: "Presseartikel",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "ErfasstVonPersonId",
            table: "Presseartikel",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Presseartikel",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Postausgang",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Postausgang",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GeaendertVonPersonId",
            table: "Plakate",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ErfasstVonPersonId",
            table: "Plakate",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Plakate",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "NummerId",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonRollen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonRollen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonRollen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonNamen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonNamen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonNamen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Personen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AdressId",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AdressId",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "MitgliedschaftId",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Mitgliedschaften",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Mitgliedschaften",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Mitgliedschaften",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AngelegtVonPersonId",
            table: "Kontakte",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Kontakte",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "QuelleId",
            table: "Kassenbuchungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "PostenId",
            table: "Kassenbuchungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "ErfasstVonPersonId",
            table: "Kassenbuchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Kassenbuchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "KalenderAbos",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "KalenderAbos",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Haushaltsposten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "VorgangId",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Gremien",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ZugeteiltPersonId",
            table: "Gebiete",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Gebiete",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "HochgeladenVonPersonId",
            table: "Dateien",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Dateien",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "BezugId",
            table: "Dateien",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Dateien",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ZugewiesenPersonId",
            table: "Buergeranliegen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Buergeranliegen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Buchungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "RessourceId",
            table: "Buchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Buchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "EntschiedenVonPersonId",
            table: "Buchungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Buchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "BildId",
            table: "BildVerwendungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "BezugId",
            table: "BildVerwendungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "BildVerwendungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "HochgeladenVonPersonId",
            table: "Bilder",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Bilder",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Besetzungsmeldungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AnfrageId",
            table: "Besetzungsmeldungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Besetzungsmeldungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TerminId",
            table: "Besetzungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ErstelltVonPersonId",
            table: "Besetzungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "BerichtVonPersonId",
            table: "Besetzungsanfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Besetzungsanfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "TopId",
            table: "Beschluesse",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "SitzungId",
            table: "Beschluesse",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Beschluesse",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "Beschluesse",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Beschluesse",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PostenId",
            table: "Auslagen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Auslagen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "EntschiedenVonPersonId",
            table: "Auslagen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Auslagen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ZugewiesenPersonId",
            table: "Aufgaben",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "ErledigtVonPersonId",
            table: "Aufgaben",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AnlassId",
            table: "Aufgaben",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Aufgaben",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "AntragKommentare",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "AntragKommentare",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "AntragKommentare",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "AntragFassungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "AntragFassungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "AntragFassungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ZielGremiumId",
            table: "Antraege",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "SitzungTerminId",
            table: "Antraege",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AutorPersonId",
            table: "Antraege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "AbstimmungId",
            table: "Antraege",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Antraege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "GremiumId",
            table: "Anfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AutorPersonId",
            table: "Anfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Anfragen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Aenderungsvorschlaege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "EntschiedenVonPersonId",
            table: "Aenderungsvorschlaege",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "AntragId",
            table: "Aenderungsvorschlaege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Aenderungsvorschlaege",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "ProtokollId",
            table: "Aenderungsvorgaenge",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(byte[]),
            oldType: "BLOB",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Aenderungsvorgaenge",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "PersonId",
            table: "Abwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Abwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");
    }
}
