using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261116221149_ZeitformateDaten : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Datenbank-Regel 5, Bestandsdaten (nach dem Tabellenumbau in Zeitformate):
        // Kalendertage „JJJJ-MM-TT“ → JJJJMMTT; offenes Gültigkeitsende → 99991231.
        foreach (var (tabelle, spalte) in new[]
        {
            ("Abwesenheiten", "Bis"),
            ("Abwesenheiten", "Von"),
            ("Anfragen", "BeantwortetAm"),
            ("Anfragen", "EingereichtAm"),
            ("Anfragen", "FristBis"),
            ("Auslagen", "Datum"),
            ("Beschluesse", "Datum"),
            ("GremiumVersionen", "GueltigBis"),
            ("GremiumVersionen", "GueltigVon"),
            ("Kassenbuchungen", "Datum"),
            ("MitgliedschaftsVersionen", "GueltigBis"),
            ("MitgliedschaftsVersionen", "GueltigVon"),
            ("PersonAdressen", "GueltigBis"),
            ("PersonAdressen", "GueltigVon"),
            ("PersonEMails", "GueltigBis"),
            ("PersonEMails", "GueltigVon"),
            ("PersonEinwilligungen", "GueltigBis"),
            ("PersonEinwilligungen", "GueltigVon"),
            ("PersonMerkmale", "GueltigBis"),
            ("PersonMerkmale", "GueltigVon"),
            ("PersonNamen", "GueltigBis"),
            ("PersonNamen", "GueltigVon"),
            ("PersonRollen", "GueltigBis"),
            ("PersonRollen", "GueltigVon"),
            ("PersonRufnummern", "GueltigBis"),
            ("PersonRufnummern", "GueltigVon"),
            ("Presseartikel", "Datum"),
            ("Pressemitteilungen", "GeplantAm"),
            ("Spenden", "Datum"),
            ("Spenden", "QuittungAm"),
        })
        {
            migrationBuilder.Sql($"UPDATE \"{tabelle}\" SET \"{spalte}\" = CAST(replace(substr(\"{spalte}\", 1, 10), '-', '') AS INTEGER) WHERE typeof(\"{spalte}\") = 'text';");
            if (spalte == "GueltigBis")
            {
                migrationBuilder.Sql($"UPDATE \"{tabelle}\" SET \"GueltigBis\" = 99991231 WHERE \"GueltigBis\" IS NULL OR \"GueltigBis\" = 0;");
            }
        }

        // Termine mit Uhrzeit: Ortszeit Greven (Europe/Berlin) als Text → UTC-Millisekunden, je Wert mit dem an diesem Tag gültigen Versatz.
        foreach (var (tabelle, spalte) in new[]
        {
            ("Buchungen", "Bis"),
            ("Buchungen", "Von"),
            ("Schichten", "Bis"),
            ("Schichten", "Von"),
            ("Termine", "Beginn"),
            ("Termine", "Ende"),
            ("UmfrageOptionen", "Zeitpunkt"),
        })
        {
            var c = $"\"{spalte}\"";
            migrationBuilder.Sql($"UPDATE \"{tabelle}\" SET {c} = (CAST(strftime('%s', {c}) AS INTEGER) - CASE WHEN {c} >= date(substr({c}, 1, 4) || '-04-01', 'weekday 0', '-7 days') || ' 02:00:00'                  AND {c} < date(substr({c}, 1, 4) || '-11-01', 'weekday 0', '-7 days') || ' 03:00:00' THEN 7200 ELSE 3600 END) * 1000 WHERE typeof({c}) = 'text';");
        }

        // Zeitpunkte: Binärform von DateTimeOffsetToBinaryConverter → UTC-Millisekunden seit 1970.
        foreach (var (tabelle, spalte) in new[]
        {
            ("Abwesenheiten", "ErfasstAm"),
            ("Abwesenheiten", "ZurueckgezogenAm"),
            ("Aenderungsvorgaenge", "Am"),
            ("Aenderungsvorschlaege", "Am"),
            ("Anfragen", "ErinnertAm"),
            ("Anfragen", "ErstelltAm"),
            ("Antraege", "EingereichtAm"),
            ("Antraege", "ErstelltAm"),
            ("Antraege", "GeaendertAm"),
            ("AntragFassungen", "Am"),
            ("AntragKommentare", "Am"),
            ("Aufgaben", "ErinnertAm"),
            ("Aufgaben", "ErledigtAm"),
            ("Aufgaben", "ErstelltAm"),
            ("Aufgaben", "FaelligAm"),
            ("Auslagen", "AusgezahltAm"),
            ("Auslagen", "EingereichtAm"),
            ("Besetzungsanfragen", "BerichtAm"),
            ("Besetzungsanfragen", "ErstelltAm"),
            ("Besetzungsanfragen", "Frist"),
            ("Besetzungsmeldungen", "Am"),
            ("BildVerwendungen", "Am"),
            ("Bilder", "Am"),
            ("Bilder", "GeloeschtAm"),
            ("Buchungen", "ErstelltAm"),
            ("Buergeranliegen", "Eingang"),
            ("Buergeranliegen", "ErledigtAm"),
            ("Dateien", "Am"),
            ("Dateien", "GeloeschtAm"),
            ("Gebiete", "ErledigtAm"),
            ("GremiumVersionen", "ErsetztAm"),
            ("KalenderAbos", "ErstelltAm"),
            ("KalenderAbos", "WiderrufenAm"),
            ("KalenderAbos", "ZuletztAbgerufenAm"),
            ("Kassenbuchungen", "ErfasstAm"),
            ("Kontakte", "AngelegtAm"),
            ("Kontakte", "GeaendertAm"),
            ("MitgliedschaftsVersionen", "ErsetztAm"),
            ("PersonAdressen", "ErsetztAm"),
            ("PersonEMails", "ErsetztAm"),
            ("PersonEinwilligungen", "ErsetztAm"),
            ("PersonMerkmale", "ErsetztAm"),
            ("PersonNamen", "ErsetztAm"),
            ("PersonRollen", "ErsetztAm"),
            ("PersonRufnummern", "ErsetztAm"),
            ("Personen", "AngelegtAm"),
            ("Plakate", "ErfasstAm"),
            ("Plakate", "GeaendertAm"),
            ("Postausgang", "ErstelltAm"),
            ("Postausgang", "ZugestelltAm"),
            ("Pressemitteilungen", "ErstelltAm"),
            ("Pressemitteilungen", "FreigegebenAm"),
            ("Pressemitteilungen", "GeaendertAm"),
            ("Pressemitteilungen", "VeroeffentlichtAm"),
            ("Protokoll", "Zeitpunkt"),
            ("PushAbos", "ErstelltAm"),
            ("PushAbos", "ZuletztZugestelltAm"),
            ("Schichteintraege", "Am"),
            ("Sitzungen", "AngelegtAm"),
            ("Sitzungen", "BeendetAm"),
            ("Sitzungen", "BegonnenAm"),
            ("Sitzungen", "EingeladenAm"),
            ("Sitzungen", "FreigegebenAm"),
            ("Sitzungen", "TopAnmeldungBis"),
            ("Stimmen", "Am"),
            ("Teilnahmen", "GeantwortetAm"),
            ("TerminErinnerungen", "Am"),
            ("Termine", "AngelegtAm"),
            ("Tops", "AngelegtAm"),
            ("Umfragen", "AbgeschlossenAm"),
            ("Umfragen", "ErstelltAm"),
            ("Umfragen", "Frist"),
            ("Vertretungsanfragen", "BeantwortetAm"),
            ("Vertretungsanfragen", "FristBis"),
            ("Vertretungsanfragen", "GestelltAm"),
            ("Vertretungsfaelle", "AbgeschlossenAm"),
            ("Vertretungsfaelle", "ErstelltAm"),
            ("WikiSeiten", "ErstelltAm"),
            ("WikiVersionen", "Am"),
            ("WikiVorschlaege", "Am"),
            ("WikiVorschlaege", "EntschiedenAm"),
        })
        {
            var c = $"\"{spalte}\"";
            migrationBuilder.Sql($"UPDATE \"{tabelle}\" SET {c} = (((({c} >> 11) * 1000) - (CASE WHEN ({c} & 2047) >= 1024 THEN ({c} & 2047) - 2048 ELSE ({c} & 2047) END) * 600000000) - 621355968000000000) / 10000 WHERE {c} IS NOT NULL;");
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
