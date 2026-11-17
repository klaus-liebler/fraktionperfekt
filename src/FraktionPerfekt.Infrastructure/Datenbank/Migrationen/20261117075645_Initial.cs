using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraktionPerfekt.Infrastructure.Datenbank.Migrationen;

/// <inheritdoc />
public partial class _20261117075645_Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Aenderungsvorgaenge",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Von = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Grund = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ProtokollId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aenderungsvorgaenge", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Aenderungsvorschlaege",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Fassung = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Alt = table.Column<string>(type: "TEXT", nullable: false),
                Neu = table.Column<string>(type: "TEXT", nullable: false),
                Begruendung = table.Column<string>(type: "TEXT", nullable: true),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                EntschiedenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aenderungsvorschlaege", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Anfragen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                AutorPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                EingereichtAm = table.Column<int>(type: "INTEGER", nullable: true),
                FristBis = table.Column<int>(type: "INTEGER", nullable: true),
                Antwort = table.Column<string>(type: "TEXT", nullable: true),
                BeantwortetAm = table.Column<int>(type: "INTEGER", nullable: true),
                ErinnertAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Anfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Antraege",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Nummer = table.Column<string>(type: "TEXT", nullable: true),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                AutorPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ZielGremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                SitzungTerminId = table.Column<byte[]>(type: "BLOB", nullable: true),
                AbstimmungId = table.Column<byte[]>(type: "BLOB", nullable: true),
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Fassung = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
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
            name: "Aufgaben",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Titel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Zustaendig = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                ZugewiesenPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Anlass = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                AnlassId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Ziel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                FaelligAm = table.Column<long>(type: "INTEGER", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                ErledigtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                ErinnertAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Aufgaben", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Auslagen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<int>(type: "INTEGER", nullable: false),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                PostenId = table.Column<byte[]>(type: "BLOB", nullable: true),
                EingereichtAm = table.Column<long>(type: "INTEGER", nullable: false),
                BelegSchluessel = table.Column<string>(type: "TEXT", nullable: true),
                BelegName = table.Column<string>(type: "TEXT", nullable: true),
                BelegTyp = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                EntschiedenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true),
                AusgezahltAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Auslagen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Beschluesse",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Quelle = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<int>(type: "INTEGER", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Ergebnis = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Ja = table.Column<int>(type: "INTEGER", nullable: false),
                Nein = table.Column<int>(type: "INTEGER", nullable: false),
                Enthaltung = table.Column<int>(type: "INTEGER", nullable: false),
                SitzungId = table.Column<byte[]>(type: "BLOB", nullable: true),
                TopId = table.Column<byte[]>(type: "BLOB", nullable: true),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: true),
                RisId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Beschluesse", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Besetzungsanfragen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Plaetze = table.Column<int>(type: "INTEGER", nullable: false),
                Frist = table.Column<long>(type: "INTEGER", nullable: false),
                Modus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                Hinweis = table.Column<string>(type: "TEXT", nullable: true),
                Bericht = table.Column<string>(type: "TEXT", nullable: true),
                BerichtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                BerichtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Besetzungsanfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Besetzungsmeldungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AnfrageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Besetzungsmeldungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Bilder",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Schluessel = table.Column<string>(type: "TEXT", nullable: false),
                OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                Typ = table.Column<string>(type: "TEXT", nullable: false),
                Groesse = table.Column<long>(type: "INTEGER", nullable: false),
                Breite = table.Column<int>(type: "INTEGER", nullable: false),
                Hoehe = table.Column<int>(type: "INTEGER", nullable: false),
                Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                HochgeladenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                GeloeschtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bilder", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BildVerwendungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                BildId = table.Column<byte[]>(type: "BLOB", nullable: false),
                BezugArt = table.Column<string>(type: "TEXT", nullable: false),
                BezugId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BildVerwendungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Buchungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                RessourceId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Von = table.Column<long>(type: "INTEGER", nullable: false),
                Bis = table.Column<long>(type: "INTEGER", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                EntschiedenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Buchungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Buergeranliegen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Betreff = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Kontaktweg = table.Column<string>(type: "TEXT", nullable: true),
                Kanal = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Eingang = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ZugewiesenPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Antwort = table.Column<string>(type: "TEXT", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Anonymisiert = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Buergeranliegen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Dateien",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Ordner = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Typ = table.Column<string>(type: "TEXT", nullable: false),
                Groesse = table.Column<long>(type: "INTEGER", nullable: false),
                Schluessel = table.Column<string>(type: "TEXT", nullable: false),
                Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                HochgeladenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Vertraulich = table.Column<bool>(type: "INTEGER", nullable: false),
                BezugArt = table.Column<string>(type: "TEXT", nullable: true),
                BezugId = table.Column<byte[]>(type: "BLOB", nullable: true),
                GeloeschtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dateien", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Gremien",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Sortierung = table.Column<int>(type: "INTEGER", nullable: false),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gremien", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Haushaltsposten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Jahr = table.Column<int>(type: "INTEGER", nullable: false),
                Bezeichnung = table.Column<string>(type: "TEXT", nullable: false),
                Ansatz = table.Column<decimal>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Haushaltsposten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kampagnen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Beginn = table.Column<int>(type: "INTEGER", nullable: true),
                Ende = table.Column<int>(type: "INTEGER", nullable: true),
                Wahltag = table.Column<int>(type: "INTEGER", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                AngelegtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kampagnen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kassenbuchungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<int>(type: "INTEGER", nullable: false),
                Einnahme = table.Column<bool>(type: "INTEGER", nullable: false),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                PostenId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                ErfasstVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                Quelle = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                QuelleId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kassenbuchungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Kontakte",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Organisation = table.Column<string>(type: "TEXT", nullable: true),
                Funktion = table.Column<string>(type: "TEXT", nullable: true),
                EMail = table.Column<string>(type: "TEXT", nullable: true),
                Telefon = table.Column<string>(type: "TEXT", nullable: true),
                Themen = table.Column<string>(type: "TEXT", nullable: true),
                Notiz = table.Column<string>(type: "TEXT", nullable: true),
                AngelegtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kontakte", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Personen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Benutzerkennung = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Kanal = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Empfaenger = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                EmpfaengerAnzeige = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Betreff = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Inhalt = table.Column<string>(type: "TEXT", nullable: false),
                Link = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Aktion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
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
            name: "Presseartikel",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Medium = table.Column<string>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Datum = table.Column<int>(type: "INTEGER", nullable: false),
                Link = table.Column<string>(type: "TEXT", nullable: true),
                Zusammenfassung = table.Column<string>(type: "TEXT", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                Tonlage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                PressemitteilungId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErfasstVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Presseartikel", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Pressemitteilungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                GeplantAm = table.Column<int>(type: "INTEGER", nullable: true),
                Thema = table.Column<string>(type: "TEXT", nullable: true),
                AutorPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                FreigegebenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                FreigegebenAm = table.Column<long>(type: "INTEGER", nullable: true),
                VeroeffentlichtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pressemitteilungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Protokoll",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Zeitpunkt = table.Column<long>(type: "INTEGER", nullable: false),
                BenutzerKennung = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Benutzer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Rolle = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                Aktion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ObjektTyp = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ObjektId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Erfolgreich = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Protokoll", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PushAbos",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
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
            name: "Ressourcen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                FreigabeNoetig = table.Column<bool>(type: "INTEGER", nullable: false),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ressourcen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Sitzungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Vorlage = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                TopAnmeldungBis = table.Column<long>(type: "INTEGER", nullable: true),
                LeitungPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ProtokollPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                EingeladenAm = table.Column<long>(type: "INTEGER", nullable: true),
                BegonnenAm = table.Column<long>(type: "INTEGER", nullable: true),
                BeendetAm = table.Column<long>(type: "INTEGER", nullable: true),
                FreigegebenAm = table.Column<long>(type: "INTEGER", nullable: true),
                FreigegebenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sitzungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Sitzungsanwesenheiten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                SitzungId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sitzungsanwesenheiten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Spenden",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Kasse = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Datum = table.Column<int>(type: "INTEGER", nullable: false),
                Spender = table.Column<string>(type: "TEXT", nullable: true),
                Anschrift = table.Column<string>(type: "TEXT", nullable: true),
                Betrag = table.Column<decimal>(type: "TEXT", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", nullable: false),
                ErfasstVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                QuittungAm = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Spenden", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Stimmen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                UmfrageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                OptionId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stimmen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TerminErinnerungen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TerminErinnerungen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Themenpaten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Politikfeld = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                VertretungPersonId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Themenpaten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TodoVorlagen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoVorlagen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tops",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                SitzungId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Fest = table.Column<bool>(type: "INTEGER", nullable: false),
                AngemeldetVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                AngelegtAm = table.Column<long>(type: "INTEGER", nullable: false),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: true),
                VertagtAusTopId = table.Column<byte[]>(type: "BLOB", nullable: true),
                UebernommenInSitzungId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ZusammengelegtMitTopId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Notiz = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tops", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Umfragen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Frist = table.Column<long>(type: "INTEGER", nullable: false),
                QuorumProzent = table.Column<int>(type: "INTEGER", nullable: false),
                Mehrfachauswahl = table.Column<bool>(type: "INTEGER", nullable: false),
                Berechtigte = table.Column<int>(type: "INTEGER", nullable: false),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                AntragId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                AbgeschlossenAm = table.Column<long>(type: "INTEGER", nullable: true),
                Ergebnis = table.Column<string>(type: "TEXT", nullable: true),
                GewinnerOptionId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Umfragen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UmfrageOptionen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                UmfrageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Zeitpunkt = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UmfrageOptionen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiSeiten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Vertraulich = table.Column<bool>(type: "INTEGER", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                Archiviert = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiSeiten", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiVersionen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                SeiteId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                VorschlagId = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiVersionen", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WikiVorschlaege",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                SeiteId = table.Column<byte[]>(type: "BLOB", nullable: false),
                BasisVersion = table.Column<int>(type: "INTEGER", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Begruendung = table.Column<string>(type: "TEXT", nullable: false),
                Vorher = table.Column<string>(type: "TEXT", nullable: false),
                Nachher = table.Column<string>(type: "TEXT", nullable: true),
                Quelle = table.Column<string>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                EntschiedenVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                EntschiedenAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WikiVorschlaege", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "GremiumVersionen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Kurzname = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GremiumVersionen", x => x.Id);
                table.ForeignKey(
                    name: "FK_GremiumVersionen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_GremiumVersionen_Gremien_GremiumId",
                    column: x => x.GremiumId,
                    principalTable: "Gremien",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Gebiete",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                KampagneId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Breite = table.Column<double>(type: "REAL", nullable: true),
                Laenge = table.Column<double>(type: "REAL", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ZugeteiltPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true),
                Vermerk = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gebiete", x => x.Id);
                table.ForeignKey(
                    name: "FK_Gebiete_Kampagnen_KampagneId",
                    column: x => x.KampagneId,
                    principalTable: "Kampagnen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Plakate",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                KampagneId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Breite = table.Column<double>(type: "REAL", nullable: false),
                Laenge = table.Column<double>(type: "REAL", nullable: false),
                Standort = table.Column<string>(type: "TEXT", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Motiv = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                FotoSchluessel = table.Column<string>(type: "TEXT", nullable: true),
                FotoTyp = table.Column<string>(type: "TEXT", nullable: true),
                ErfasstVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErfasstAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Plakate", x => x.Id);
                table.ForeignKey(
                    name: "FK_Plakate_Kampagnen_KampagneId",
                    column: x => x.KampagneId,
                    principalTable: "Kampagnen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Termine",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Titel = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Beginn = table.Column<long>(type: "INTEGER", nullable: false),
                Ende = table.Column<long>(type: "INTEGER", nullable: false),
                Ort = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Beschreibung = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: true),
                RisId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                KampagneId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Plaetze = table.Column<int>(type: "INTEGER", nullable: true),
                Abgesagt = table.Column<bool>(type: "INTEGER", nullable: false),
                AngelegtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
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
                table.ForeignKey(
                    name: "FK_Termine_Kampagnen_KampagneId",
                    column: x => x.KampagneId,
                    principalTable: "Kampagnen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Abwesenheiten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Von = table.Column<int>(type: "INTEGER", nullable: false),
                Bis = table.Column<int>(type: "INTEGER", nullable: false),
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: false),
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
            name: "PersonAdressen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AdressId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Zusatz = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Strasse = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Postleitzahl = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Ort = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonAdressen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonAdressen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Zweck = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Erteilt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonEinwilligungen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonEinwilligungen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AdressId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Bereich = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Adresse = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonEMails", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonEMails_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PersonEMails_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonMerkmale",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Inhalt = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonMerkmale", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonMerkmale_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PersonMerkmale_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonNamen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Vorname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "DIN5007_2"),
                Nachname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "DIN5007_2"),
                Namensvorsatz = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true, collation: "DIN5007_2"),
                Namenszusatz = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                NurEinName = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonNamen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonNamen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PersonNamen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonRollen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Rolle = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonRollen", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonRollen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PersonRollen_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PersonRufnummern",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                NummerId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Art = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Nummer = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Dienste = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Einwilligung = table.Column<bool>(type: "INTEGER", nullable: false),
                Bevorzugt = table.Column<bool>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonRufnummern", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonRufnummern_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PersonRufnummern_Personen_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Personen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TodoVorlagenPunkte",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                VorlageId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoVorlagenPunkte", x => x.Id);
                table.ForeignKey(
                    name: "FK_TodoVorlagenPunkte_TodoVorlagen_VorlageId",
                    column: x => x.VorlageId,
                    principalTable: "TodoVorlagen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Teilnahmen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                VertrittPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
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
            name: "TerminTodos",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                ZustaendigPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErledigtVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErledigtAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TerminTodos", x => x.Id);
                table.ForeignKey(
                    name: "FK_TerminTodos_Termine_TerminId",
                    column: x => x.TerminId,
                    principalTable: "Termine",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tickets",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                KampagneId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                Titel = table.Column<string>(type: "TEXT", nullable: false),
                Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Prioritaet = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ZustaendigPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Faellig = table.Column<int>(type: "INTEGER", nullable: true),
                GebietId = table.Column<byte[]>(type: "BLOB", nullable: true),
                PlakatId = table.Column<byte[]>(type: "BLOB", nullable: true),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErstelltVonPersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                GeaendertAm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tickets", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tickets_Gebiete_GebietId",
                    column: x => x.GebietId,
                    principalTable: "Gebiete",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tickets_Kampagnen_KampagneId",
                    column: x => x.KampagneId,
                    principalTable: "Kampagnen",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tickets_Plakate_PlakatId",
                    column: x => x.PlakatId,
                    principalTable: "Plakate",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tickets_Termine_TerminId",
                    column: x => x.TerminId,
                    principalTable: "Termine",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Vertretungsfaelle",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TerminId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GremiumId = table.Column<byte[]>(type: "BLOB", nullable: false),
                VertretenePersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                AbwesenheitId = table.Column<byte[]>(type: "BLOB", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                VertreterPersonId = table.Column<byte[]>(type: "BLOB", nullable: true),
                ErstelltAm = table.Column<long>(type: "INTEGER", nullable: false),
                AbgeschlossenAm = table.Column<long>(type: "INTEGER", nullable: true)
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
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                Amt = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                FunktionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Funktion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Rang = table.Column<int>(type: "INTEGER", nullable: true),
                MitgliedschaftId = table.Column<byte[]>(type: "BLOB", nullable: false),
                GueltigVon = table.Column<int>(type: "INTEGER", nullable: false),
                GueltigBis = table.Column<int>(type: "INTEGER", nullable: false),
                VorgangId = table.Column<byte[]>(type: "BLOB", nullable: false),
                ErsetztAm = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MitgliedschaftsVersionen", x => x.Id);
                table.ForeignKey(
                    name: "FK_MitgliedschaftsVersionen_Aenderungsvorgaenge_VorgangId",
                    column: x => x.VorgangId,
                    principalTable: "Aenderungsvorgaenge",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MitgliedschaftsVersionen_Mitgliedschaften_MitgliedschaftId",
                    column: x => x.MitgliedschaftId,
                    principalTable: "Mitgliedschaften",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TicketKommentare",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                TicketId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                Am = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketKommentare", x => x.Id);
                table.ForeignKey(
                    name: "FK_TicketKommentare_Tickets_TicketId",
                    column: x => x.TicketId,
                    principalTable: "Tickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Vertretungsanfragen",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                VertretungsfallId = table.Column<byte[]>(type: "BLOB", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false),
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

        migrationBuilder.CreateTable(
            name: "Vertretungskandidaten",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                VertretungsfallId = table.Column<byte[]>(type: "BLOB", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                PersonId = table.Column<byte[]>(type: "BLOB", nullable: false)
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
            name: "IX_Abwesenheiten_PersonId_Von",
            table: "Abwesenheiten",
            columns: new[] { "PersonId", "Von" });

        migrationBuilder.CreateIndex(
            name: "IX_Abwesenheiten_Von_Bis",
            table: "Abwesenheiten",
            columns: new[] { "Von", "Bis" });

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorgaenge_Am",
            table: "Aenderungsvorgaenge",
            column: "Am");

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorgaenge_ProtokollId",
            table: "Aenderungsvorgaenge",
            column: "ProtokollId");

        migrationBuilder.CreateIndex(
            name: "IX_Aenderungsvorschlaege_AntragId",
            table: "Aenderungsvorschlaege",
            column: "AntragId");

        migrationBuilder.CreateIndex(
            name: "IX_Anfragen_Status",
            table: "Anfragen",
            column: "Status");

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
            name: "IX_Aufgaben_Anlass_AnlassId",
            table: "Aufgaben",
            columns: new[] { "Anlass", "AnlassId" });

        migrationBuilder.CreateIndex(
            name: "IX_Aufgaben_Status_Zustaendig",
            table: "Aufgaben",
            columns: new[] { "Status", "Zustaendig" });

        migrationBuilder.CreateIndex(
            name: "IX_Auslagen_Status",
            table: "Auslagen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Beschluesse_Datum",
            table: "Beschluesse",
            column: "Datum");

        migrationBuilder.CreateIndex(
            name: "IX_Besetzungsanfragen_TerminId",
            table: "Besetzungsanfragen",
            column: "TerminId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Besetzungsmeldungen_AnfrageId_PersonId",
            table: "Besetzungsmeldungen",
            columns: new[] { "AnfrageId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bilder_Sha256",
            table: "Bilder",
            column: "Sha256");

        migrationBuilder.CreateIndex(
            name: "IX_BildVerwendungen_BezugArt_BezugId",
            table: "BildVerwendungen",
            columns: new[] { "BezugArt", "BezugId" });

        migrationBuilder.CreateIndex(
            name: "IX_BildVerwendungen_BildId_BezugArt_BezugId",
            table: "BildVerwendungen",
            columns: new[] { "BildId", "BezugArt", "BezugId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Buchungen_RessourceId_Von",
            table: "Buchungen",
            columns: new[] { "RessourceId", "Von" });

        migrationBuilder.CreateIndex(
            name: "IX_Buergeranliegen_Status",
            table: "Buergeranliegen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Dateien_BezugArt_BezugId",
            table: "Dateien",
            columns: new[] { "BezugArt", "BezugId" });

        migrationBuilder.CreateIndex(
            name: "IX_Dateien_Ordner",
            table: "Dateien",
            column: "Ordner");

        migrationBuilder.CreateIndex(
            name: "IX_Gebiete_KampagneId",
            table: "Gebiete",
            column: "KampagneId");

        migrationBuilder.CreateIndex(
            name: "IX_Gremien_RisId",
            table: "Gremien",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GremiumVersionen_GremiumId_GueltigVon",
            table: "GremiumVersionen",
            columns: new[] { "GremiumId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_GremiumVersionen_VorgangId",
            table: "GremiumVersionen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_Haushaltsposten_Kasse_Jahr",
            table: "Haushaltsposten",
            columns: new[] { "Kasse", "Jahr" });

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
            name: "IX_Kampagnen_Status",
            table: "Kampagnen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Kassenbuchungen_Kasse_Datum",
            table: "Kassenbuchungen",
            columns: new[] { "Kasse", "Datum" });

        migrationBuilder.CreateIndex(
            name: "IX_Kontakte_Name",
            table: "Kontakte",
            column: "Name");

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
            name: "IX_MitgliedschaftsVersionen_VorgangId",
            table: "MitgliedschaftsVersionen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_AdressId",
            table: "PersonAdressen",
            column: "AdressId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_PersonId_GueltigVon",
            table: "PersonAdressen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonAdressen_VorgangId",
            table: "PersonAdressen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEinwilligungen_PersonId_GueltigVon",
            table: "PersonEinwilligungen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonEinwilligungen_VorgangId",
            table: "PersonEinwilligungen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_AdressId",
            table: "PersonEMails",
            column: "AdressId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_PersonId_GueltigVon",
            table: "PersonEMails",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonEMails_VorgangId",
            table: "PersonEMails",
            column: "VorgangId");

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
            name: "IX_PersonMerkmale_PersonId_GueltigVon",
            table: "PersonMerkmale",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonMerkmale_VorgangId",
            table: "PersonMerkmale",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonNamen_DIN5007",
            table: "PersonNamen",
            columns: new[] { "Nachname", "Vorname", "Namensvorsatz" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonNamen_PersonId_GueltigVon",
            table: "PersonNamen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonNamen_VorgangId",
            table: "PersonNamen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonRollen_PersonId_GueltigVon",
            table: "PersonRollen",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonRollen_VorgangId",
            table: "PersonRollen",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_NummerId",
            table: "PersonRufnummern",
            column: "NummerId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_PersonId_GueltigVon",
            table: "PersonRufnummern",
            columns: new[] { "PersonId", "GueltigVon" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonRufnummern_VorgangId",
            table: "PersonRufnummern",
            column: "VorgangId");

        migrationBuilder.CreateIndex(
            name: "IX_Plakate_KampagneId_Status",
            table: "Plakate",
            columns: new[] { "KampagneId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Postausgang_Status_ErstelltAm",
            table: "Postausgang",
            columns: new[] { "Status", "ErstelltAm" });

        migrationBuilder.CreateIndex(
            name: "IX_Presseartikel_Datum",
            table: "Presseartikel",
            column: "Datum");

        migrationBuilder.CreateIndex(
            name: "IX_Pressemitteilungen_Status",
            table: "Pressemitteilungen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Protokoll_ObjektTyp_ObjektId",
            table: "Protokoll",
            columns: new[] { "ObjektTyp", "ObjektId" });

        migrationBuilder.CreateIndex(
            name: "IX_Protokoll_Zeitpunkt",
            table: "Protokoll",
            column: "Zeitpunkt");

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
            name: "IX_Stimmen_UmfrageId_PersonId",
            table: "Stimmen",
            columns: new[] { "UmfrageId", "PersonId" });

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
            name: "IX_Termine_KampagneId",
            table: "Termine",
            column: "KampagneId");

        migrationBuilder.CreateIndex(
            name: "IX_Termine_RisId",
            table: "Termine",
            column: "RisId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TerminErinnerungen_TerminId_PersonId",
            table: "TerminErinnerungen",
            columns: new[] { "TerminId", "PersonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TerminTodos_TerminId_Position",
            table: "TerminTodos",
            columns: new[] { "TerminId", "Position" });

        migrationBuilder.CreateIndex(
            name: "IX_TicketKommentare_TicketId",
            table: "TicketKommentare",
            column: "TicketId");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_GebietId",
            table: "Tickets",
            column: "GebietId");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_KampagneId_Nummer",
            table: "Tickets",
            columns: new[] { "KampagneId", "Nummer" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_PlakatId",
            table: "Tickets",
            column: "PlakatId");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_TerminId",
            table: "Tickets",
            column: "TerminId");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_ZustaendigPersonId",
            table: "Tickets",
            column: "ZustaendigPersonId");

        migrationBuilder.CreateIndex(
            name: "IX_TodoVorlagenPunkte_VorlageId_Position",
            table: "TodoVorlagenPunkte",
            columns: new[] { "VorlageId", "Position" });

        migrationBuilder.CreateIndex(
            name: "IX_Tops_SitzungId_Position",
            table: "Tops",
            columns: new[] { "SitzungId", "Position" });

        migrationBuilder.CreateIndex(
            name: "IX_Umfragen_Status",
            table: "Umfragen",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_UmfrageOptionen_UmfrageId",
            table: "UmfrageOptionen",
            column: "UmfrageId");

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

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungskandidaten_PersonId",
            table: "Vertretungskandidaten",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Vertretungskandidaten_VertretungsfallId_Position",
            table: "Vertretungskandidaten",
            columns: new[] { "VertretungsfallId", "Position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WikiSeiten_Titel",
            table: "WikiSeiten",
            column: "Titel",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WikiVersionen_SeiteId_Nummer",
            table: "WikiVersionen",
            columns: new[] { "SeiteId", "Nummer" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WikiVorschlaege_SeiteId_Status",
            table: "WikiVorschlaege",
            columns: new[] { "SeiteId", "Status" });

        // Volltextsuche im Wiki (FTS5, aus den Wiki-Versionen aufgebaut)
        migrationBuilder.Sql(Wissen.WikiSuche.Anlegen);

        // Datenbank-Regel 5, Prüfbarkeit: lesbare Views für externe Werkzeuge – Ids als GUID-Text, Tage als JJJJ-MM-TT („offen“ = 99991231),
        // Zeitpunkte als Text in UTC. Nur lesen; Namensspalten mit COLLATE BINARY, damit die Views auch ohne die Collation DIN5007_2 lesbar sind.
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Aenderungsvorgaenge; CREATE VIEW v_Aenderungsvorgaenge AS SELECT CASE WHEN Id IS NULL THEN NULL ELSE lower(substr(hex(Id),1,8)||'-'||substr(hex(Id),9,4)||'-'||substr(hex(Id),13,4)||'-'||substr(hex(Id),17,4)||'-'||substr(hex(Id),21)) END AS Id, CASE WHEN Am IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', Am / 1000, 'unixepoch') || ' UTC' END AS Am, Von, Art, Grund, CASE WHEN ProtokollId IS NULL THEN NULL ELSE lower(substr(hex(ProtokollId),1,8)||'-'||substr(hex(ProtokollId),9,4)||'-'||substr(hex(ProtokollId),13,4)||'-'||substr(hex(ProtokollId),17,4)||'-'||substr(hex(ProtokollId),21)) END AS ProtokollId FROM Aenderungsvorgaenge;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonNamen; CREATE VIEW v_PersonNamen AS SELECT CASE WHEN n.PersonId IS NULL THEN NULL ELSE lower(substr(hex(n.PersonId),1,8)||'-'||substr(hex(n.PersonId),9,4)||'-'||substr(hex(n.PersonId),13,4)||'-'||substr(hex(n.PersonId),17,4)||'-'||substr(hex(n.PersonId),21)) END AS PersonId, n.Vorname COLLATE BINARY AS Vorname, n.Namensvorsatz COLLATE BINARY AS Namensvorsatz, n.Nachname COLLATE BINARY AS Nachname, n.Namenszusatz, n.NurEinName, CASE WHEN n.GueltigVon IS NULL THEN NULL WHEN n.GueltigVon = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', n.GueltigVon / 10000, n.GueltigVon / 100 % 100, n.GueltigVon % 100) END AS GueltigVon, CASE WHEN n.GueltigBis IS NULL THEN NULL WHEN n.GueltigBis = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', n.GueltigBis / 10000, n.GueltigBis / 100 % 100, n.GueltigBis % 100) END AS GueltigBis, CASE WHEN v.Am IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', v.Am / 1000, 'unixepoch') || ' UTC' END AS ErfasstAm, CASE WHEN n.ErsetztAm IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', n.ErsetztAm / 1000, 'unixepoch') || ' UTC' END AS ErsetztAm, v.Art, v.Von, v.Grund FROM PersonNamen n JOIN Aenderungsvorgaenge v ON v.Id = n.VorgangId;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonMerkmale; CREATE VIEW v_PersonMerkmale AS SELECT CASE WHEN m.PersonId IS NULL THEN NULL ELSE lower(substr(hex(m.PersonId),1,8)||'-'||substr(hex(m.PersonId),9,4)||'-'||substr(hex(m.PersonId),13,4)||'-'||substr(hex(m.PersonId),17,4)||'-'||substr(hex(m.PersonId),21)) END AS PersonId, m.Art AS Merkmal, m.Inhalt, CASE WHEN m.GueltigVon IS NULL THEN NULL WHEN m.GueltigVon = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', m.GueltigVon / 10000, m.GueltigVon / 100 % 100, m.GueltigVon % 100) END AS GueltigVon, CASE WHEN m.GueltigBis IS NULL THEN NULL WHEN m.GueltigBis = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', m.GueltigBis / 10000, m.GueltigBis / 100 % 100, m.GueltigBis % 100) END AS GueltigBis, CASE WHEN v.Am IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', v.Am / 1000, 'unixepoch') || ' UTC' END AS ErfasstAm, CASE WHEN m.ErsetztAm IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', m.ErsetztAm / 1000, 'unixepoch') || ' UTC' END AS ErsetztAm, v.Art, v.Von, v.Grund FROM PersonMerkmale m JOIN Aenderungsvorgaenge v ON v.Id = m.VorgangId;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonRollen; CREATE VIEW v_PersonRollen AS SELECT CASE WHEN r.PersonId IS NULL THEN NULL ELSE lower(substr(hex(r.PersonId),1,8)||'-'||substr(hex(r.PersonId),9,4)||'-'||substr(hex(r.PersonId),13,4)||'-'||substr(hex(r.PersonId),17,4)||'-'||substr(hex(r.PersonId),21)) END AS PersonId, r.Rolle, CASE WHEN r.GueltigVon IS NULL THEN NULL WHEN r.GueltigVon = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', r.GueltigVon / 10000, r.GueltigVon / 100 % 100, r.GueltigVon % 100) END AS GueltigVon, CASE WHEN r.GueltigBis IS NULL THEN NULL WHEN r.GueltigBis = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', r.GueltigBis / 10000, r.GueltigBis / 100 % 100, r.GueltigBis % 100) END AS GueltigBis, CASE WHEN v.Am IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', v.Am / 1000, 'unixepoch') || ' UTC' END AS ErfasstAm, CASE WHEN r.ErsetztAm IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', r.ErsetztAm / 1000, 'unixepoch') || ' UTC' END AS ErsetztAm, v.Art, v.Von, v.Grund FROM PersonRollen r JOIN Aenderungsvorgaenge v ON v.Id = r.VorgangId;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Mitgliedschaften; CREATE VIEW v_Mitgliedschaften AS SELECT CASE WHEN m.Id IS NULL THEN NULL ELSE lower(substr(hex(m.Id),1,8)||'-'||substr(hex(m.Id),9,4)||'-'||substr(hex(m.Id),13,4)||'-'||substr(hex(m.Id),17,4)||'-'||substr(hex(m.Id),21)) END AS MitgliedschaftId, CASE WHEN m.PersonId IS NULL THEN NULL ELSE lower(substr(hex(m.PersonId),1,8)||'-'||substr(hex(m.PersonId),9,4)||'-'||substr(hex(m.PersonId),13,4)||'-'||substr(hex(m.PersonId),17,4)||'-'||substr(hex(m.PersonId),21)) END AS PersonId, CASE WHEN m.GremiumId IS NULL THEN NULL ELSE lower(substr(hex(m.GremiumId),1,8)||'-'||substr(hex(m.GremiumId),9,4)||'-'||substr(hex(m.GremiumId),13,4)||'-'||substr(hex(m.GremiumId),17,4)||'-'||substr(hex(m.GremiumId),21)) END AS GremiumId, mv.Funktion, mv.Rang, mv.Amt, mv.FunktionText, CASE WHEN mv.GueltigVon IS NULL THEN NULL WHEN mv.GueltigVon = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', mv.GueltigVon / 10000, mv.GueltigVon / 100 % 100, mv.GueltigVon % 100) END AS GueltigVon, CASE WHEN mv.GueltigBis IS NULL THEN NULL WHEN mv.GueltigBis = 99991231 THEN 'offen' ELSE printf('%04d-%02d-%02d', mv.GueltigBis / 10000, mv.GueltigBis / 100 % 100, mv.GueltigBis % 100) END AS GueltigBis, CASE WHEN v.Am IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', v.Am / 1000, 'unixepoch') || ' UTC' END AS ErfasstAm, CASE WHEN mv.ErsetztAm IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', mv.ErsetztAm / 1000, 'unixepoch') || ' UTC' END AS ErsetztAm, v.Art FROM Mitgliedschaften m JOIN MitgliedschaftsVersionen mv ON mv.MitgliedschaftId = m.Id JOIN Aenderungsvorgaenge v ON v.Id = mv.VorgangId;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Termine; CREATE VIEW v_Termine AS SELECT CASE WHEN Id IS NULL THEN NULL ELSE lower(substr(hex(Id),1,8)||'-'||substr(hex(Id),9,4)||'-'||substr(hex(Id),13,4)||'-'||substr(hex(Id),17,4)||'-'||substr(hex(Id),21)) END AS Id, Titel, Art, CASE WHEN Beginn IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', Beginn / 1000, 'unixepoch') || ' UTC' END AS Beginn, CASE WHEN Ende IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', Ende / 1000, 'unixepoch') || ' UTC' END AS Ende, Ort, CASE WHEN GremiumId IS NULL THEN NULL ELSE lower(substr(hex(GremiumId),1,8)||'-'||substr(hex(GremiumId),9,4)||'-'||substr(hex(GremiumId),13,4)||'-'||substr(hex(GremiumId),17,4)||'-'||substr(hex(GremiumId),21)) END AS GremiumId, Abgesagt, RisId, CASE WHEN KampagneId IS NULL THEN NULL ELSE lower(substr(hex(KampagneId),1,8)||'-'||substr(hex(KampagneId),9,4)||'-'||substr(hex(KampagneId),13,4)||'-'||substr(hex(KampagneId),17,4)||'-'||substr(hex(KampagneId),21)) END AS KampagneId, Plaetze FROM Termine;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Protokoll; CREATE VIEW v_Protokoll AS SELECT CASE WHEN Id IS NULL THEN NULL ELSE lower(substr(hex(Id),1,8)||'-'||substr(hex(Id),9,4)||'-'||substr(hex(Id),13,4)||'-'||substr(hex(Id),17,4)||'-'||substr(hex(Id),21)) END AS Id, CASE WHEN Zeitpunkt IS NULL THEN NULL ELSE strftime('%Y-%m-%d %H:%M:%S', Zeitpunkt / 1000, 'unixepoch') || ' UTC' END AS Zeitpunkt, Benutzer, Rolle, Aktion, Beschreibung, ObjektTyp, CASE WHEN ObjektId IS NULL THEN NULL ELSE lower(substr(hex(ObjektId),1,8)||'-'||substr(hex(ObjektId),9,4)||'-'||substr(hex(ObjektId),13,4)||'-'||substr(hex(ObjektId),17,4)||'-'||substr(hex(ObjektId),21)) END AS ObjektId, Erfolgreich FROM Protokoll;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Aenderungsvorgaenge;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonNamen;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonMerkmale;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_PersonRollen;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Mitgliedschaften;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Termine;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_Protokoll;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS WikiSuche");

        migrationBuilder.DropTable(
            name: "Abwesenheiten");

        migrationBuilder.DropTable(
            name: "Aenderungsvorschlaege");

        migrationBuilder.DropTable(
            name: "Anfragen");

        migrationBuilder.DropTable(
            name: "Antraege");

        migrationBuilder.DropTable(
            name: "AntragFassungen");

        migrationBuilder.DropTable(
            name: "AntragKommentare");

        migrationBuilder.DropTable(
            name: "Aufgaben");

        migrationBuilder.DropTable(
            name: "Auslagen");

        migrationBuilder.DropTable(
            name: "Beschluesse");

        migrationBuilder.DropTable(
            name: "Besetzungsanfragen");

        migrationBuilder.DropTable(
            name: "Besetzungsmeldungen");

        migrationBuilder.DropTable(
            name: "Bilder");

        migrationBuilder.DropTable(
            name: "BildVerwendungen");

        migrationBuilder.DropTable(
            name: "Buchungen");

        migrationBuilder.DropTable(
            name: "Buergeranliegen");

        migrationBuilder.DropTable(
            name: "Dateien");

        migrationBuilder.DropTable(
            name: "GremiumVersionen");

        migrationBuilder.DropTable(
            name: "Haushaltsposten");

        migrationBuilder.DropTable(
            name: "KalenderAbos");

        migrationBuilder.DropTable(
            name: "Kassenbuchungen");

        migrationBuilder.DropTable(
            name: "Kontakte");

        migrationBuilder.DropTable(
            name: "MitgliedschaftsVersionen");

        migrationBuilder.DropTable(
            name: "PersonAdressen");

        migrationBuilder.DropTable(
            name: "PersonEinwilligungen");

        migrationBuilder.DropTable(
            name: "PersonEMails");

        migrationBuilder.DropTable(
            name: "PersonMerkmale");

        migrationBuilder.DropTable(
            name: "PersonNamen");

        migrationBuilder.DropTable(
            name: "PersonRollen");

        migrationBuilder.DropTable(
            name: "PersonRufnummern");

        migrationBuilder.DropTable(
            name: "Postausgang");

        migrationBuilder.DropTable(
            name: "Presseartikel");

        migrationBuilder.DropTable(
            name: "Pressemitteilungen");

        migrationBuilder.DropTable(
            name: "Protokoll");

        migrationBuilder.DropTable(
            name: "PushAbos");

        migrationBuilder.DropTable(
            name: "Ressourcen");

        migrationBuilder.DropTable(
            name: "Sitzungen");

        migrationBuilder.DropTable(
            name: "Sitzungsanwesenheiten");

        migrationBuilder.DropTable(
            name: "Spenden");

        migrationBuilder.DropTable(
            name: "Stimmen");

        migrationBuilder.DropTable(
            name: "Teilnahmen");

        migrationBuilder.DropTable(
            name: "TerminErinnerungen");

        migrationBuilder.DropTable(
            name: "TerminTodos");

        migrationBuilder.DropTable(
            name: "Themenpaten");

        migrationBuilder.DropTable(
            name: "TicketKommentare");

        migrationBuilder.DropTable(
            name: "TodoVorlagenPunkte");

        migrationBuilder.DropTable(
            name: "Tops");

        migrationBuilder.DropTable(
            name: "Umfragen");

        migrationBuilder.DropTable(
            name: "UmfrageOptionen");

        migrationBuilder.DropTable(
            name: "Vertretungsanfragen");

        migrationBuilder.DropTable(
            name: "Vertretungskandidaten");

        migrationBuilder.DropTable(
            name: "WikiSeiten");

        migrationBuilder.DropTable(
            name: "WikiVersionen");

        migrationBuilder.DropTable(
            name: "WikiVorschlaege");

        migrationBuilder.DropTable(
            name: "Mitgliedschaften");

        migrationBuilder.DropTable(
            name: "Aenderungsvorgaenge");

        migrationBuilder.DropTable(
            name: "Tickets");

        migrationBuilder.DropTable(
            name: "TodoVorlagen");

        migrationBuilder.DropTable(
            name: "Vertretungsfaelle");

        migrationBuilder.DropTable(
            name: "Personen");

        migrationBuilder.DropTable(
            name: "Gebiete");

        migrationBuilder.DropTable(
            name: "Plakate");

        migrationBuilder.DropTable(
            name: "Termine");

        migrationBuilder.DropTable(
            name: "Gremien");

        migrationBuilder.DropTable(
            name: "Kampagnen");
    }
}
