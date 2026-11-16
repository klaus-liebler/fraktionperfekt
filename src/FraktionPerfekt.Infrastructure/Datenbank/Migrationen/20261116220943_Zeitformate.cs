using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116220943_Zeitformate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<long>(
            name: "Zeitpunkt",
            table: "UmfrageOptionen",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<long>(
            name: "Ende",
            table: "Termine",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<long>(
            name: "Beginn",
            table: "Termine",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "QuittungAm",
            table: "Spenden",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Datum",
            table: "Spenden",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<long>(
            name: "Von",
            table: "Schichten",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<long>(
            name: "Bis",
            table: "Schichten",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GeplantAm",
            table: "Pressemitteilungen",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Datum",
            table: "Presseartikel",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonRufnummern",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonRufnummern",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonRollen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonRollen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonNamen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonNamen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonMerkmale",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonMerkmale",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonEMails",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonEMails",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonEinwilligungen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonEinwilligungen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "PersonAdressen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "PersonAdressen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "MitgliedschaftsVersionen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "MitgliedschaftsVersionen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Datum",
            table: "Kassenbuchungen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigVon",
            table: "GremiumVersionen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "GueltigBis",
            table: "GremiumVersionen",
            type: "INTEGER",
            nullable: false,
            defaultValue: 99991231,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<long>(
            name: "Von",
            table: "Buchungen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<long>(
            name: "Bis",
            table: "Buchungen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "Datum",
            table: "Beschluesse",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "Datum",
            table: "Auslagen",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "FristBis",
            table: "Anfragen",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "EingereichtAm",
            table: "Anfragen",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "BeantwortetAm",
            table: "Anfragen",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Von",
            table: "Abwesenheiten",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<int>(
            name: "Bis",
            table: "Abwesenheiten",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "TEXT");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "Zeitpunkt",
            table: "UmfrageOptionen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "Ende",
            table: "Termine",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Beginn",
            table: "Termine",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "QuittungAm",
            table: "Spenden",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Datum",
            table: "Spenden",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Von",
            table: "Schichten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Bis",
            table: "Schichten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GeplantAm",
            table: "Pressemitteilungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Datum",
            table: "Presseartikel",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonRufnummern",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonRollen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonRollen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonNamen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonNamen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonMerkmale",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonEMails",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonEMails",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonEinwilligungen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "PersonAdressen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "MitgliedschaftsVersionen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Datum",
            table: "Kassenbuchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigVon",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "GueltigBis",
            table: "GremiumVersionen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Von",
            table: "Buchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Bis",
            table: "Buchungen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Datum",
            table: "Beschluesse",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Datum",
            table: "Auslagen",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "FristBis",
            table: "Anfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "EingereichtAm",
            table: "Anfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "BeantwortetAm",
            table: "Anfragen",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Von",
            table: "Abwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "Bis",
            table: "Abwesenheiten",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");
    }
}
