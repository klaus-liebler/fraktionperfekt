# Datenbank – Tabellen und Spalten

Automatisch erzeugt aus dem EF-Modell (`DatenbankArchitekturTests`), nicht von Hand ändern; die Einleitung steht in
`docs/datenbank-einleitung.md`. Einstufung siehe `Spalteneinstufung.cs`: S = Stammdatum, V = Version, G = Vorgang, B = Beleg (eingefroren),
A = Anonymisierungsrest; **K** = Kopie und **J** = JSON/Liste sind verboten und nur während des Umbaus vorhanden.

## Bitemporale Datenhaltung

Alle veränderlichen Angaben zu Personen (Name mit Titel, Anrede, Geschlecht, Funktion, Rollen, Adressen, E-Mails, Rufnummern,
Einwilligungen) und zu Gremienmitgliedschaften werden **bitemporal** gespeichert. Es gibt zwei Zeitachsen:

| Achse | Spalten | Bedeutung |
|---|---|---|
| **Gültigkeitszeit** | `GueltigVon`, `GueltigBis` | Wann galt die Angabe in der Welt? Halboffen: `GueltigVon` einschließlich, `GueltigBis` ausschließlich (leer = offen). |
| **Erfassungszeit** | `Aenderungsvorgaenge.Am`, `ErsetztAm` | Wann wusste das System das? Ebenfalls halboffen: ab dem Zeitpunkt des Änderungsvorgangs bis `ErsetztAm` (leer = aktueller Wissensstand). |

**Änderungsvorgänge:** Wann, von wem, wie (Ersterfassung, Änderung ab Datum, Korrektur, Beendet, Abgrenzung) und warum stehen nicht an jeder
Versionszeile, sondern einmal in `Aenderungsvorgaenge`; jede Version verweist per `VorgangId` darauf. Ein Speichervorgang ist ein
Vorgang (technische Abgrenzungen bekommen einen eigenen Vorgang mit gleicher Zeit, Person und Grund). `ProtokollId` verbindet den Vorgang mit
dem Eintrag im Audit-Protokoll desselben Speichervorgangs.

**Versionen werden nie geändert oder gelöscht.** Jede Änderung – auch das Beenden einer Gültigkeit (Umzug, Rolle endet) – setzt
`ErsetztAm` an der alten Zeile und legt neue Zeilen an. So bleibt beantwortbar, was das System zu einem früheren Zeitpunkt wusste.

**Stabile Identität und Version:** `Id` ist die Zeilen-Id einer *Version*. Gegenstände, von denen eine Person mehrere haben kann, tragen
eine stabile Identität über alle Versionen: `AdressId` (eine Adresse), `NummerId` (eine Rufnummer), `AdressId` in `PersonEMails`.

**Änderung ab Datum und Korrektur:** Eine *Änderung ab Datum* (Heirat, Umzug, neues Amt) lässt die Vergangenheit stehen. Eine
*Korrektur* (Tippfehler, falsches Umzugsdatum) ersetzt die betroffenen Versionen für ihren ganzen Zeitraum; die alten bleiben mit
früherem Wissensstand abrufbar.

### Beispiel: Umzug mit nachträglicher Korrektur

Frau Muster wohnt seit 2020 in der Lindenstraße. Am 15.03.2026 meldet sie einen Umzug zum 01.08. in den Bergweg. Am 10.07. geht ein
Brief raus. Am 20.09. kommt er zurück, und sie sagt, sie sei schon zum 01.07. umgezogen.

| Zeile | Adresse | gültig von | gültig bis (einschl.) | erfasst | ersetzt | Vorgang |
|---|---|---|---|---|---|---|
| A | Lindenstr. 5 | 2020-01-01 | offen | 2020-01-10 | 2026-03-15 | Ersterfassung |
| B | Lindenstr. 5 | 2020-01-01 | 2026-07-31 | 2026-03-15 | 2026-09-20 | Ende wegen gemeldetem Umzug |
| C | Bergweg 2 | 2026-08-01 | offen | 2026-03-15 | 2026-09-20 | Umzug ab 01.08. |
| D | Lindenstr. 5 | 2020-01-01 | 2026-06-30 | 2026-09-20 | – | Korrektur |
| E | Bergweg 2 | 2026-07-01 | offen | 2026-09-20 | – | Korrektur |

- **„Wo wohnte sie am 10.07.?“** (heutiger Wissensstand) → nur nicht ersetzte Zeilen D, E → **Bergweg**.
- **„Wohin hätten wir am 10.07. geschrieben?“** (Wissensstand 10.07.) → B, C → **Lindenstraße**.

Beide Fragen beantwortet dieselbe Abfrage mit Stichtag `:tag` und Wissensstand `:bekannt`:

```sql
SELECT a.* FROM PersonAdressen a JOIN Aenderungsvorgaenge v ON v.Id = a.VorgangId
WHERE a.PersonId = :person
  AND a.GueltigVon <= :tag AND (a.GueltigBis IS NULL OR a.GueltigBis > :tag)
  AND v.Am <= :bekannt AND (a.ErsetztAm IS NULL OR a.ErsetztAm > :bekannt);
```

Im Code übernimmt das `ZeitlicheHistorie.AlsStichtag(gueltigAm, bekanntAm)`; `Person.StandAm(tag, bekannt)` liefert den ganzen Stand.

### Sortierung nach DIN 5007-2 (Collation „DIN5007_2“)

Namenslisten sortieren nach DIN 5007-2 (ä = ae, ö = oe, ü = ue, ß = ss, ohne Groß-/Kleinschreibung; Nachname ohne Vorsatz, dann Vorname,
dann Namensvorsatz – „von Berg“ steht unter B; mehrteilige Nachnamen wie „Schulze Höping Pellengahr“ sind kein Vorsatz). Es gibt **keinen
gespeicherten Sortierschlüssel**: `PersonNamen.Nachname`, `Vorname` und `Namensvorsatz` tragen die eigene SQLite-Collation `DIN5007_2`,
der Index `IX_PersonNamen_DIN5007` (Nachname, Vorname, Namensvorsatz) bedient `ORDER BY … LIMIT` direkt (geprüft per `EXPLAIN QUERY PLAN`).
Die Collation wird bei jedem Öffnen einer Verbindung registriert (`Din5007Kollation`, zentral für alle DbContexte, Tests, Migrationen und
den Wiederherstellungstest der Sicherung).

**Haken für externe Werkzeuge** (DB Browser for SQLite, `sqlite3`), die die Collation nicht kennen (getestet in `Din5007Tests`):
Lesen (`SELECT *`, Zählen) und Ändern anderer Spalten geht; **Sortieren oder Vergleichen** über die Namensspalten und **jedes Einfügen**
in `PersonNamen` sowie das Ändern der Namensspalten scheitert mit „no such collation sequence: DIN5007_2“. Für Auswertungen von außen die
lesbaren Views verwenden bzw. nur lesen; Änderungen nur über die Anwendung. Auch `PRAGMA integrity_check` braucht die Collation – mit einer
falsch nachgebauten Vergleichsregel meldet es fehlende Indexeinträge, obwohl die Datei in Ordnung ist.

### Keine Kopien

Die Datenbank ist in 3. Normalform: Es gibt keine Spalten, die Werte aus anderen Tabellen oder Versionen kopieren. Der aktuelle Stand
wird zur Abfragezeit aus den Versionen berechnet. Ausnahmen sind ausdrücklich als **Beleg** eingestuft (eingefrorene Werte wie
Empfänger einer versandten Mail oder Spender auf der Quittung).

## Übersicht

- [Abwesenheiten](#abwesenheiten) – 7 Spalten
- [Aenderungsvorgaenge](#aenderungsvorgaenge) – 6 Spalten
- [Aenderungsvorschlaege](#aenderungsvorschlaege) – 10 Spalten
- [Anfragen](#anfragen) – 13 Spalten
- [Antraege](#antraege) – 13 Spalten
- [AntragFassungen](#antragfassungen) – 7 Spalten
- [AntragKommentare](#antragkommentare) – 8 Spalten
- [Aufgaben](#aufgaben) – 16 Spalten
- [Auslagen](#auslagen) – 15 Spalten
- [Beschluesse](#beschluesse) – 14 Spalten
- [Besetzungsanfragen](#besetzungsanfragen) – 11 Spalten
- [Besetzungsmeldungen](#besetzungsmeldungen) – 5 Spalten
- [BildVerwendungen](#bildverwendungen) – 5 Spalten
- [Bilder](#bilder) – 11 Spalten
- [Buchungen](#buchungen) – 11 Spalten
- [Buergeranliegen](#buergeranliegen) – 13 Spalten
- [Dateien](#dateien) – 14 Spalten
- [Gebiete](#gebiete) – 10 Spalten
- [Gremien](#gremien) – 4 Spalten
- [GremiumVersionen](#gremiumversionen) – 9 Spalten
- [Haushaltsposten](#haushaltsposten) – 5 Spalten
- [KalenderAbos](#kalenderabos) – 6 Spalten
- [Kassenbuchungen](#kassenbuchungen) – 11 Spalten
- [Kontakte](#kontakte) – 11 Spalten
- [Mitgliedschaften](#mitgliedschaften) – 4 Spalten
- [MitgliedschaftsVersionen](#mitgliedschaftsversionen) – 10 Spalten
- [PersonAdressen](#personadressen) – 13 Spalten
- [PersonEMails](#personemails) – 10 Spalten
- [PersonEinwilligungen](#personeinwilligungen) – 8 Spalten
- [PersonMerkmale](#personmerkmale) – 8 Spalten
- [PersonNamen](#personnamen) – 11 Spalten
- [PersonRollen](#personrollen) – 7 Spalten
- [PersonRufnummern](#personrufnummern) – 12 Spalten
- [Personen](#personen) – 4 Spalten
- [Plakate](#plakate) – 13 Spalten
- [Postausgang](#postausgang) – 16 Spalten
- [Presseartikel](#presseartikel) – 10 Spalten
- [Pressemitteilungen](#pressemitteilungen) – 12 Spalten
- [Protokoll](#protokoll) – 10 Spalten
- [PushAbos](#pushabos) – 8 Spalten
- [Ressourcen](#ressourcen) – 6 Spalten
- [Schichteintraege](#schichteintraege) – 4 Spalten
- [Schichten](#schichten) – 6 Spalten
- [Sitzungen](#sitzungen) – 14 Spalten
- [Sitzungsanwesenheiten](#sitzungsanwesenheiten) – 4 Spalten
- [Spenden](#spenden) – 9 Spalten
- [Stimmen](#stimmen) – 5 Spalten
- [Teilnahmen](#teilnahmen) – 6 Spalten
- [TerminErinnerungen](#terminerinnerungen) – 4 Spalten
- [Termine](#termine) – 12 Spalten
- [Themenpaten](#themenpaten) – 4 Spalten
- [Tops](#tops) – 14 Spalten
- [UmfrageOptionen](#umfrageoptionen) – 5 Spalten
- [Umfragen](#umfragen) – 15 Spalten
- [Vertretungsanfragen](#vertretungsanfragen) – 8 Spalten
- [Vertretungsfaelle](#vertretungsfaelle) – 9 Spalten
- [Vertretungskandidaten](#vertretungskandidaten) – 4 Spalten
- [WikiSeiten](#wikiseiten) – 7 Spalten
- [WikiVersionen](#wikiversionen) – 8 Spalten
- [WikiVorschlaege](#wikivorschlaege) – 12 Spalten

## Abwesenheiten

Klasse: `FraktionPerfekt.Domain.Vertretung.Abwesenheit`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Bis | TEXT | ja | Index | G |  |
| ErfasstAm | INTEGER | ja |  | G |  |
| Grund | TEXT(500) |  |  | G |  |
| PersonId | TEXT | ja | → Personen, Index | G |  |
| Von | TEXT | ja | Index | G |  |
| ZurueckgezogenAm | INTEGER |  |  | G |  |

Indizes: (PersonId, Von); (Von, Bis)

## Aenderungsvorgaenge

Klasse: `FraktionPerfekt.Domain.Gemeinsam.Erfassung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel |
| Am | INTEGER | ja | Index | G | Wissenszeit: erfasst am (für alle Versionen des Vorgangs) |
| Art | TEXT(40) | ja |  | G | Ersterfassung, Änderung ab Datum, Korrektur, Beendet, Abgrenzung |
| Grund | TEXT(500) | ja |  | G | Grund bzw. Nachweis |
| ProtokollId | TEXT |  | Index | G | Eintrag im Audit-Protokoll desselben Speichervorgangs |
| Von | TEXT(200) | ja |  | B | wer erfasst hat (Name und Rolle bzw. Automatik), eingefroren wie im Audit-Protokoll |

Indizes: (Am); (ProtokollId)

## Aenderungsvorschlaege

Klasse: `FraktionPerfekt.Domain.Antraege.Aenderungsvorschlag`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Alt | TEXT | ja |  | G |  |
| Am | INTEGER | ja |  | G |  |
| AntragId | TEXT | ja | Index | G |  |
| Begruendung | TEXT |  |  | G |  |
| EntschiedenVonPersonId | TEXT |  |  | G |  |
| Fassung | INTEGER | ja |  | G |  |
| Neu | TEXT | ja |  | G |  |
| PersonId | TEXT | ja |  | G |  |
| Status | TEXT(40) | ja |  | G |  |

Indizes: (AntragId)

## Anfragen

Klasse: `FraktionPerfekt.Domain.Politik.Anfrage`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Antwort | TEXT |  |  | G |  |
| AutorPersonId | TEXT | ja |  | G |  |
| BeantwortetAm | TEXT |  |  | G |  |
| EingereichtAm | TEXT |  |  | G |  |
| ErinnertAm | INTEGER |  |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| FristBis | TEXT |  |  | G |  |
| GremiumId | TEXT |  |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| Text | TEXT | ja |  | G |  |
| Thema | TEXT |  |  | G |  |
| Titel | TEXT | ja |  | G |  |

Indizes: (Status)

## Antraege

Klasse: `FraktionPerfekt.Domain.Antraege.Antrag`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AbstimmungId | TEXT |  |  | G |  |
| AutorPersonId | TEXT | ja |  | G |  |
| EingereichtAm | INTEGER |  |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| GeaendertAm | INTEGER | ja |  | G |  |
| Nummer | TEXT |  | Index | G |  |
| SitzungTerminId | TEXT |  |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| Thema | TEXT |  |  | G |  |
| Titel | TEXT | ja |  | G |  |
| Vermerk | TEXT |  |  | G |  |
| ZielGremiumId | TEXT |  |  | G |  |

Indizes: (Nummer); (Status)

## AntragFassungen

Klasse: `FraktionPerfekt.Domain.Antraege.AntragFassung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| AntragId | TEXT | ja | eindeutig | G |  |
| Notiz | TEXT |  |  | G |  |
| Nummer | INTEGER | ja | eindeutig | G |  |
| PersonId | TEXT | ja |  | G |  |
| Text | TEXT | ja |  | G |  |

Indizes: (AntragId, Nummer) eindeutig

## AntragKommentare

Klasse: `FraktionPerfekt.Domain.Antraege.AntragKommentar`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| AntragId | TEXT | ja | Index | G |  |
| Erledigt | INTEGER | ja |  | G |  |
| Fassung | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja |  | G |  |
| Text | TEXT | ja |  | G |  |
| Zitat | TEXT |  |  | G |  |

Indizes: (AntragId)

## Aufgaben

Klasse: `FraktionPerfekt.Domain.Aufgaben.Aufgabe`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Anlass | TEXT(60) | ja | Index | G |  |
| AnlassId | TEXT | ja | Index | G |  |
| Beschreibung | TEXT(2000) |  |  | G |  |
| ErinnertAm | INTEGER |  |  | G |  |
| ErledigtAm | INTEGER |  |  | G |  |
| ErledigtVonPersonId | TEXT |  |  | G | wer erledigt hat; leer = Automatik |
| ErstelltAm | INTEGER | ja |  | G |  |
| FaelligAm | INTEGER |  |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| Thema | TEXT |  |  | G |  |
| Titel | TEXT(300) | ja |  | G |  |
| Vermerk | TEXT(2000) |  |  | G |  |
| Ziel | TEXT(300) | ja |  | G |  |
| ZugewiesenPersonId | TEXT |  |  | G |  |
| Zustaendig | TEXT(40) |  | Index | G |  |

Indizes: (Anlass, AnlassId); (Status, Zustaendig)

## Auslagen

Klasse: `FraktionPerfekt.Domain.Finanzen.Auslage` – Auslagenbeleg mit Datei (eingefroren nach Auszahlung)

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | B | Schlüssel (Zeilen-Id) |
| AusgezahltAm | INTEGER |  |  | B |  |
| BelegName | TEXT |  |  | B | Dateiname des Belegs (kein Personenname) |
| BelegSchluessel | TEXT |  |  | B |  |
| BelegTyp | TEXT |  |  | B |  |
| Betrag | TEXT | ja |  | B |  |
| Datum | TEXT | ja |  | B |  |
| EingereichtAm | INTEGER | ja |  | B |  |
| EntschiedenVonPersonId | TEXT |  |  | B |  |
| Kasse | TEXT(40) | ja |  | B |  |
| PersonId | TEXT | ja |  | B |  |
| PostenId | TEXT |  |  | B |  |
| Status | TEXT(40) | ja | Index | B |  |
| Vermerk | TEXT |  |  | B |  |
| Zweck | TEXT | ja |  | B |  |

Indizes: (Status)

## Beschluesse

Klasse: `FraktionPerfekt.Domain.Sitzungen.Beschluss`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AntragId | TEXT |  |  | G |  |
| Datum | TEXT | ja | Index | G |  |
| Enthaltung | INTEGER | ja |  | G |  |
| Ergebnis | TEXT(40) | ja |  | G |  |
| GremiumId | TEXT |  |  | G |  |
| Ja | INTEGER | ja |  | G |  |
| Nein | INTEGER | ja |  | G |  |
| Quelle | TEXT(40) | ja |  | G |  |
| RisId | TEXT |  |  | G |  |
| SitzungId | TEXT |  |  | G |  |
| Text | TEXT | ja |  | G |  |
| Titel | TEXT | ja |  | G |  |
| TopId | TEXT |  |  | G |  |

Indizes: (Datum)

## Besetzungsanfragen

Klasse: `FraktionPerfekt.Domain.Termine.Besetzungsanfrage`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Bericht | TEXT |  |  | G |  |
| BerichtAm | INTEGER |  |  | G |  |
| BerichtVonPersonId | TEXT |  |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| ErstelltVonPersonId | TEXT | ja |  | G |  |
| Frist | INTEGER | ja |  | G |  |
| Hinweis | TEXT |  |  | G |  |
| Modus | TEXT(40) | ja |  | G |  |
| Plaetze | INTEGER | ja |  | G |  |
| TerminId | TEXT | ja | eindeutig | G |  |

Indizes: (TerminId) eindeutig

## Besetzungsmeldungen

Klasse: `FraktionPerfekt.Domain.Termine.Besetzungsmeldung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| AnfrageId | TEXT | ja | eindeutig | G |  |
| PersonId | TEXT | ja | eindeutig | G |  |
| Status | TEXT(40) | ja |  | G |  |

Indizes: (AnfrageId, PersonId) eindeutig

## BildVerwendungen

Klasse: `FraktionPerfekt.Domain.Wissen.BildVerwendung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| BezugArt | TEXT | ja | eindeutig | G |  |
| BezugId | TEXT | ja | eindeutig | G |  |
| BildId | TEXT | ja | eindeutig | G |  |

Indizes: (BezugArt, BezugId); (BildId, BezugArt, BezugId) eindeutig

## Bilder

Klasse: `FraktionPerfekt.Domain.Wissen.Bild`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| Breite | INTEGER | ja |  | G |  |
| GeloeschtAm | INTEGER |  |  | G |  |
| Groesse | INTEGER | ja |  | G |  |
| HochgeladenVonPersonId | TEXT | ja |  | G |  |
| Hoehe | INTEGER | ja |  | G |  |
| OriginalName | TEXT | ja |  | G | ursprünglicher Dateiname (kein Personenname) |
| Schluessel | TEXT | ja |  | G |  |
| Sha256 | TEXT | ja | Index | G |  |
| Typ | TEXT | ja |  | G |  |

Indizes: (Sha256)

## Buchungen

Klasse: `FraktionPerfekt.Domain.Ressourcen.Buchung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Bis | TEXT | ja |  | G |  |
| EntschiedenVonPersonId | TEXT |  |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja |  | G |  |
| RessourceId | TEXT | ja | Index | G |  |
| Status | TEXT(40) | ja |  | G |  |
| TerminId | TEXT |  |  | G |  |
| Vermerk | TEXT |  |  | G |  |
| Von | TEXT | ja | Index | G |  |
| Zweck | TEXT | ja |  | G |  |

Indizes: (RessourceId, Von)

## Buergeranliegen

Klasse: `FraktionPerfekt.Domain.Politik.Buergeranliegen`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Anonymisiert | INTEGER | ja |  | G | Kennzeichen: personenbezogene Angaben nach Löschfrist entfernt |
| Antwort | TEXT |  |  | G |  |
| Betreff | TEXT | ja |  | G |  |
| Eingang | INTEGER | ja |  | G |  |
| ErledigtAm | INTEGER |  |  | G |  |
| Kanal | TEXT(40) | ja |  | G |  |
| Kontaktweg | TEXT |  |  | G |  |
| Name | TEXT | ja |  | G | Name der Bürgerin bzw. des Bürgers (eigenes Datum des Anliegens, nach Frist anonymisiert) |
| Status | TEXT(40) | ja | Index | G |  |
| Text | TEXT | ja |  | G |  |
| Thema | TEXT |  |  | G |  |
| ZugewiesenPersonId | TEXT |  |  | G |  |

Indizes: (Status)

## Dateien

Klasse: `FraktionPerfekt.Domain.Wissen.Datei`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| BezugArt | TEXT |  | Index | G |  |
| BezugId | TEXT |  | Index | G |  |
| GeloeschtAm | INTEGER |  |  | G |  |
| GremiumId | TEXT |  |  | G |  |
| Groesse | INTEGER | ja |  | G |  |
| HochgeladenVonPersonId | TEXT | ja |  | G |  |
| Name | TEXT | ja |  | G | Dateiname (kein Personenname) |
| Ordner | TEXT | ja | Index | G |  |
| Schluessel | TEXT | ja |  | G |  |
| Sha256 | TEXT | ja |  | G |  |
| Typ | TEXT | ja |  | G |  |
| Vertraulich | INTEGER | ja |  | G |  |

Indizes: (Ordner); (BezugArt, BezugId)

## Gebiete

Klasse: `FraktionPerfekt.Domain.Wahlkampf.Gebiet`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| Art | TEXT(40) | ja |  | S |  |
| Beschreibung | TEXT |  |  | S |  |
| Breite | REAL |  |  | S |  |
| ErledigtAm | INTEGER |  |  | S |  |
| Laenge | REAL |  |  | S |  |
| Name | TEXT | ja |  | S | Name des Plakatier-/Verteilgebiets |
| Status | TEXT(40) | ja |  | S |  |
| Vermerk | TEXT |  |  | S |  |
| ZugeteiltPersonId | TEXT |  |  | S |  |

## Gremien

Klasse: `FraktionPerfekt.Domain.Gremien.Gremium`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| Art | TEXT(40) | ja |  | S |  |
| RisId | TEXT(200) |  | eindeutig | S |  |
| Sortierung | INTEGER | ja |  | S |  |

Indizes: (RisId) eindeutig

## GremiumVersionen

Klasse: `FraktionPerfekt.Domain.Gremien.GremiumVersion`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| Aktiv | INTEGER | ja |  | V | Gremium aktiv |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GremiumId | TEXT | ja | → Gremien, Index | V |  |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| Kurzname | TEXT(60) | ja |  | V | Kurzname laut RIS |
| Name | TEXT(300) | ja |  | V | Name laut RIS |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (VorgangId); (GremiumId, GueltigVon)

## Haushaltsposten

Klasse: `FraktionPerfekt.Domain.Finanzen.Haushaltsposten`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| Ansatz | TEXT | ja |  | S |  |
| Bezeichnung | TEXT | ja |  | S |  |
| Jahr | INTEGER | ja | Index | S |  |
| Kasse | TEXT(40) | ja | Index | S |  |

Indizes: (Kasse, Jahr)

## KalenderAbos

Klasse: `FraktionPerfekt.Domain.Termine.KalenderAbo`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| ErstelltAm | INTEGER | ja |  | S |  |
| PersonId | TEXT | ja | → Personen, Index | S |  |
| TokenHash | TEXT(64) | ja | eindeutig | S |  |
| WiderrufenAm | INTEGER |  |  | S |  |
| ZuletztAbgerufenAm | INTEGER |  |  | S |  |

Indizes: (PersonId); (TokenHash) eindeutig

## Kassenbuchungen

Klasse: `FraktionPerfekt.Domain.Finanzen.Kassenbuchung` – Kassenbuch: Buchungen (eingefroren, GoBD)

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | B | Schlüssel (Zeilen-Id) |
| Betrag | TEXT | ja |  | B |  |
| Datum | TEXT | ja | Index | B |  |
| Einnahme | INTEGER | ja |  | B |  |
| ErfasstAm | INTEGER | ja |  | B |  |
| ErfasstVonPersonId | TEXT | ja |  | B |  |
| Kasse | TEXT(40) | ja | Index | B |  |
| PostenId | TEXT |  |  | B |  |
| Quelle | TEXT(40) | ja |  | B |  |
| QuelleId | TEXT |  |  | B |  |
| Text | TEXT | ja |  | B |  |

Indizes: (Kasse, Datum)

## Kontakte

Klasse: `FraktionPerfekt.Domain.Politik.Kontakt`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| AngelegtAm | INTEGER | ja |  | S |  |
| AngelegtVonPersonId | TEXT | ja |  | S |  |
| EMail | TEXT |  |  | S |  |
| Funktion | TEXT |  |  | S |  |
| GeaendertAm | INTEGER | ja |  | S |  |
| Name | TEXT | ja | Index | S | Name des externen Kontakts (eigenes Datum) |
| Notiz | TEXT |  |  | S |  |
| Organisation | TEXT |  |  | S |  |
| Telefon | TEXT |  |  | S |  |
| Themen | TEXT |  |  | S |  |

Indizes: (Name)

## Mitgliedschaften

Klasse: `FraktionPerfekt.Domain.Gremien.Mitgliedschaft`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| GremiumId | TEXT | ja | → Gremien, Index | S |  |
| PersonId | TEXT | ja | → Personen, Index | S |  |
| RisId | TEXT(200) |  | eindeutig | S |  |

Indizes: (GremiumId); (PersonId); (RisId) eindeutig

## MitgliedschaftsVersionen

Klasse: `FraktionPerfekt.Domain.Gremien.MitgliedschaftsVersion`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| Amt | TEXT(40) | ja |  | V | Vorsitz oder Stellvertretung des Vorsitzes (RIS) |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| Funktion | TEXT(40) | ja |  | V |  |
| FunktionText | TEXT(200) |  |  | V | Funktionsbezeichnung wie im RIS |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| MitgliedschaftId | TEXT | ja | → Mitgliedschaften, Index | V |  |
| Rang | INTEGER |  |  | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (VorgangId); (MitgliedschaftId, GueltigVon)

## PersonAdressen

Klasse: `FraktionPerfekt.Domain.Personen.Postadresse`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| AdressId | TEXT | ja | Index | V |  |
| Bereich | TEXT(20) | ja |  | V |  |
| Bevorzugt | INTEGER | ja |  | V |  |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| Ort | TEXT(100) | ja |  | V |  |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| Postleitzahl | TEXT(10) | ja |  | V |  |
| Strasse | TEXT(200) | ja |  | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |
| Zusatz | TEXT(200) |  |  | V |  |

Indizes: (AdressId); (VorgangId); (PersonId, GueltigVon)

## PersonEMails

Klasse: `FraktionPerfekt.Domain.Personen.EMailAdresse`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| AdressId | TEXT | ja | Index | V |  |
| Adresse | TEXT(320) | ja |  | V |  |
| Bereich | TEXT(20) | ja |  | V |  |
| Bevorzugt | INTEGER | ja |  | V |  |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (AdressId); (VorgangId); (PersonId, GueltigVon)

## PersonEinwilligungen

Klasse: `FraktionPerfekt.Domain.Personen.Einwilligung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| Erteilt | INTEGER | ja |  | V |  |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |
| Zweck | TEXT(40) | ja |  | V |  |

Indizes: (VorgangId); (PersonId, GueltigVon)

## PersonMerkmale

Klasse: `FraktionPerfekt.Domain.Personen.PersonMerkmal`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| Art | TEXT(20) | ja |  | V |  |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| Inhalt | TEXT(320) | ja |  | V |  |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (VorgangId); (PersonId, GueltigVon)

## PersonNamen

Klasse: `FraktionPerfekt.Domain.Personen.PersonName`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| Nachname | TEXT(200) | ja | Index | V |  |
| Namensvorsatz | TEXT(60) |  | Index | V | Namensvorsatz („von“, „van der“), Collation DIN5007_2; mehrteilige Nachnamen bleiben im Nachnamen |
| Namenszusatz | TEXT(30) |  |  | V | Namenszusatz („jun.“) |
| NurEinName | INTEGER | ja |  | V | laut Ausweis nur ein Name: Vorname bewusst leer |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |
| Vorname | TEXT(200) | ja | Index | V |  |

Indizes: (VorgangId); (PersonId, GueltigVon); (Nachname, Vorname, Namensvorsatz)

## PersonRollen

Klasse: `FraktionPerfekt.Domain.Personen.PersonRolle`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| Rolle | TEXT(40) | ja |  | V | eine Rolle je Zeilenreihe (Gegenstand) |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (VorgangId); (PersonId, GueltigVon)

## PersonRufnummern

Klasse: `FraktionPerfekt.Domain.Personen.Rufnummer`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | V | Zeilen-Id = Version |
| Art | TEXT(20) | ja |  | V |  |
| Bevorzugt | INTEGER | ja |  | V |  |
| Dienste | TEXT(40) | ja |  | V |  |
| Einwilligung | INTEGER | ja |  | V |  |
| ErsetztAm | INTEGER |  |  | V | Wissenszeit: ersetzt am, leer = aktuell |
| GueltigBis | TEXT |  |  | V | Gültigkeit bis (ausschließlich), leer = offen |
| GueltigVon | TEXT | ja | Index | V | Gültigkeit von (einschließlich) |
| Nummer | TEXT(40) | ja |  | V |  |
| NummerId | TEXT | ja | Index | V |  |
| PersonId | TEXT | ja | → Personen, Index | V |  |
| VorgangId | TEXT | ja | → Aenderungsvorgaenge, Index | V | Änderungsvorgang (wann, wer, wie, warum), Tabelle Aenderungsvorgaenge |

Indizes: (NummerId); (VorgangId); (PersonId, GueltigVon)

## Personen

Klasse: `FraktionPerfekt.Domain.Personen.Person`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| AngelegtAm | INTEGER | ja |  | S | Zeitpunkt der Anlage der Identität |
| Benutzerkennung | TEXT(100) |  | eindeutig | S | Anmeldename (Konto) |
| RisId | TEXT(200) |  | eindeutig | S | Kennung im Ratsinformationssystem |

Indizes: (Benutzerkennung) eindeutig; (RisId) eindeutig

## Plakate

Klasse: `FraktionPerfekt.Domain.Wahlkampf.Plakat`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Art | TEXT(40) | ja |  | G |  |
| Breite | REAL | ja |  | G |  |
| ErfasstAm | INTEGER | ja |  | G |  |
| ErfasstVonPersonId | TEXT | ja |  | G |  |
| FotoSchluessel | TEXT |  |  | G |  |
| FotoTyp | TEXT |  |  | G |  |
| GeaendertAm | INTEGER | ja |  | G |  |
| GeaendertVonPersonId | TEXT | ja |  | G |  |
| Laenge | REAL | ja |  | G |  |
| Motiv | TEXT |  |  | G |  |
| Standort | TEXT | ja |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |

Indizes: (Status)

## Postausgang

Klasse: `FraktionPerfekt.Application.Postausgang.AusgehendeNachricht` – Versandbeleg: Empfänger, Text und Auslöser so, wie die Nachricht hinausging (eingefroren)

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | B | Schlüssel (Zeilen-Id) |
| Aktion | TEXT(100) |  |  | B |  |
| AusgeloestVon | TEXT(200) | ja |  | B | wer den Versand ausgelöst hat, so wie beim Versand (eingefroren) |
| Betreff | TEXT(300) | ja |  | B |  |
| Empfaenger | TEXT(320) | ja |  | B | Adresse wie beim Versand (eingefroren) |
| EmpfaengerAnzeige | TEXT(200) |  |  | B | Empfängername wie beim Versand (eingefroren) |
| ErstelltAm | INTEGER | ja | Index | B |  |
| Fehler | TEXT(2000) |  |  | B |  |
| Inhalt | TEXT | ja |  | B |  |
| Kanal | TEXT(40) | ja |  | B |  |
| Link | TEXT(500) |  |  | B |  |
| PersonId | TEXT |  |  | B |  |
| Rueckmeldung | TEXT(500) |  |  | B |  |
| Status | TEXT(40) | ja | Index | B |  |
| Versuche | INTEGER | ja |  | B |  |
| ZugestelltAm | INTEGER |  |  | B |  |

Indizes: (Status, ErstelltAm)

## Presseartikel

Klasse: `FraktionPerfekt.Domain.Politik.Presseartikel`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Datum | TEXT | ja | Index | G |  |
| ErfasstVonPersonId | TEXT | ja |  | G |  |
| Link | TEXT |  |  | G |  |
| Medium | TEXT | ja |  | G |  |
| PressemitteilungId | TEXT |  |  | G |  |
| Thema | TEXT |  |  | G |  |
| Titel | TEXT | ja |  | G |  |
| Tonlage | TEXT(40) | ja |  | G |  |
| Zusammenfassung | TEXT |  |  | G |  |

Indizes: (Datum)

## Pressemitteilungen

Klasse: `FraktionPerfekt.Domain.Politik.Pressemitteilung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AutorPersonId | TEXT | ja |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| FreigegebenAm | INTEGER |  |  | G |  |
| FreigegebenVonPersonId | TEXT |  |  | G |  |
| GeaendertAm | INTEGER | ja |  | G |  |
| GeplantAm | TEXT |  |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| Text | TEXT | ja |  | G |  |
| Thema | TEXT |  |  | G |  |
| Titel | TEXT | ja |  | G |  |
| VeroeffentlichtAm | INTEGER |  |  | G |  |

Indizes: (Status)

## Protokoll

Klasse: `FraktionPerfekt.Application.Protokoll.AuditEintrag` – Audit-Protokoll: wer hat wann was getan (eingefroren, nur anfügen)

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | B | Schlüssel (Zeilen-Id) |
| Aktion | TEXT(100) | ja |  | B |  |
| Benutzer | TEXT(200) | ja |  | B | Name und Rolle zum Zeitpunkt der Handlung (eingefroren) |
| BenutzerKennung | TEXT(100) | ja |  | B | Anmeldename zum Zeitpunkt der Handlung |
| Beschreibung | TEXT(2000) | ja |  | B |  |
| Erfolgreich | INTEGER | ja |  | B |  |
| ObjektId | TEXT |  | Index | B |  |
| ObjektTyp | TEXT(100) |  | Index | B |  |
| Rolle | TEXT(60) |  |  | B |  |
| Zeitpunkt | INTEGER | ja | Index | B |  |

Indizes: (Zeitpunkt); (ObjektTyp, ObjektId)

## PushAbos

Klasse: `FraktionPerfekt.Domain.Postausgang.PushAbo`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| Auth | TEXT | ja |  | S |  |
| Endpunkt | TEXT | ja | eindeutig | S |  |
| ErstelltAm | INTEGER | ja |  | S |  |
| Geraet | TEXT |  |  | S |  |
| P256dh | TEXT | ja |  | S |  |
| PersonId | TEXT | ja | Index | S |  |
| ZuletztZugestelltAm | INTEGER |  |  | S |  |

Indizes: (Endpunkt) eindeutig; (PersonId)

## Ressourcen

Klasse: `FraktionPerfekt.Domain.Ressourcen.Ressource`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| Aktiv | INTEGER | ja |  | S |  |
| Art | TEXT(40) | ja |  | S |  |
| Beschreibung | TEXT |  |  | S |  |
| FreigabeNoetig | INTEGER | ja |  | S |  |
| Name | TEXT | ja |  | S | Name der Ressource |

## Schichteintraege

Klasse: `FraktionPerfekt.Domain.Wahlkampf.Schichteintrag`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja | eindeutig | G |  |
| SchichtId | TEXT | ja | eindeutig | G |  |

Indizes: (SchichtId, PersonId) eindeutig

## Schichten

Klasse: `FraktionPerfekt.Domain.Wahlkampf.Schicht`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Bis | TEXT | ja |  | G |  |
| Ort | TEXT | ja |  | G |  |
| Plaetze | INTEGER | ja |  | G |  |
| TerminId | TEXT |  |  | G |  |
| Von | TEXT | ja | Index | G |  |

Indizes: (Von)

## Sitzungen

Klasse: `FraktionPerfekt.Domain.Sitzungen.Sitzung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AngelegtAm | INTEGER | ja |  | G |  |
| Art | TEXT(40) | ja |  | G |  |
| BeendetAm | INTEGER |  |  | G |  |
| BegonnenAm | INTEGER |  |  | G |  |
| EingeladenAm | INTEGER |  |  | G |  |
| FreigegebenAm | INTEGER |  |  | G |  |
| FreigegebenVonPersonId | TEXT |  |  | G |  |
| LeitungPersonId | TEXT |  |  | G |  |
| ProtokollPersonId | TEXT |  |  | G |  |
| Status | TEXT(40) | ja |  | G |  |
| TerminId | TEXT | ja | eindeutig | G |  |
| TopAnmeldungBis | INTEGER |  |  | G |  |
| Vorlage | TEXT | ja |  | G |  |

Indizes: (TerminId) eindeutig

## Sitzungsanwesenheiten

Klasse: `FraktionPerfekt.Domain.Sitzungen.Sitzungsanwesenheit`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| PersonId | TEXT | ja | eindeutig | G |  |
| SitzungId | TEXT | ja | eindeutig | G |  |
| Status | TEXT(40) | ja |  | G |  |

Indizes: (SitzungId, PersonId) eindeutig

## Spenden

Klasse: `FraktionPerfekt.Domain.Finanzen.Spende` – Spendenbeleg: Spender und Anschrift wie auf der Quittung (eingefroren, GoBD)

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | B | Schlüssel (Zeilen-Id) |
| Anschrift | TEXT |  |  | B | Anschrift wie auf der Quittung (eingefroren, GoBD) |
| Betrag | TEXT | ja |  | B |  |
| Datum | TEXT | ja |  | B |  |
| ErfasstVonPersonId | TEXT | ja |  | B |  |
| Kasse | TEXT(40) | ja |  | B |  |
| QuittungAm | TEXT |  |  | B |  |
| Spender | TEXT |  |  | B | Spender wie auf der Quittung (eingefroren, GoBD) |
| Zweck | TEXT | ja |  | B |  |

## Stimmen

Klasse: `FraktionPerfekt.Domain.Abstimmungen.Stimme`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| OptionId | TEXT | ja |  | G |  |
| PersonId | TEXT | ja | Index | G |  |
| UmfrageId | TEXT | ja | Index | G |  |

Indizes: (UmfrageId, PersonId)

## Teilnahmen

Klasse: `FraktionPerfekt.Domain.Termine.Teilnahme`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| GeantwortetAm | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja | → Personen, eindeutig | G |  |
| Status | TEXT(40) | ja |  | G |  |
| TerminId | TEXT | ja | → Termine, eindeutig | G |  |
| VertrittPersonId | TEXT |  |  | G |  |

Indizes: (PersonId); (TerminId, PersonId) eindeutig

## TerminErinnerungen

Klasse: `FraktionPerfekt.Domain.Termine.TerminErinnerung`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja | eindeutig | G |  |
| TerminId | TEXT | ja | eindeutig | G |  |

Indizes: (TerminId, PersonId) eindeutig

## Termine

Klasse: `FraktionPerfekt.Domain.Termine.Termin`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Abgesagt | INTEGER | ja |  | G |  |
| AngelegtAm | INTEGER | ja |  | G |  |
| AngelegtVonPersonId | TEXT |  |  | G | wer angelegt hat; leer = RIS-Abgleich bzw. Automatik |
| Art | TEXT(40) | ja |  | G |  |
| Beginn | TEXT | ja | Index | G |  |
| Beschreibung | TEXT(4000) |  |  | G |  |
| Ende | TEXT | ja |  | G |  |
| GremiumId | TEXT |  | → Gremien, Index | G |  |
| Ort | TEXT(300) |  |  | G |  |
| RisId | TEXT(200) |  | eindeutig | G |  |
| Titel | TEXT(300) | ja |  | G |  |

Indizes: (Beginn); (GremiumId); (RisId) eindeutig

## Themenpaten

Klasse: `FraktionPerfekt.Domain.Aufgaben.Themenpate`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | S | Schlüssel (Zeilen-Id) |
| PersonId | TEXT | ja |  | S |  |
| Politikfeld | TEXT | ja |  | S |  |
| VertretungPersonId | TEXT |  |  | S |  |

## Tops

Klasse: `FraktionPerfekt.Domain.Sitzungen.Top`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AngelegtAm | INTEGER | ja |  | G |  |
| AngemeldetVonPersonId | TEXT |  |  | G |  |
| AntragId | TEXT |  |  | G |  |
| Beschreibung | TEXT |  |  | G |  |
| Fest | INTEGER | ja |  | G |  |
| Notiz | TEXT |  |  | G |  |
| Position | INTEGER | ja | Index | G |  |
| SitzungId | TEXT | ja | Index | G |  |
| Status | TEXT(40) | ja |  | G |  |
| Titel | TEXT | ja |  | G |  |
| UebernommenInSitzungId | TEXT |  |  | G |  |
| VertagtAusTopId | TEXT |  |  | G |  |
| ZusammengelegtMitTopId | TEXT |  |  | G |  |

Indizes: (SitzungId, Position)

## UmfrageOptionen

Klasse: `FraktionPerfekt.Domain.Abstimmungen.UmfrageOption`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Position | INTEGER | ja |  | G |  |
| Text | TEXT | ja |  | G |  |
| UmfrageId | TEXT | ja | Index | G |  |
| Zeitpunkt | TEXT |  |  | G |  |

Indizes: (UmfrageId)

## Umfragen

Klasse: `FraktionPerfekt.Domain.Abstimmungen.Umfrage`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AbgeschlossenAm | INTEGER |  |  | G |  |
| AntragId | TEXT |  |  | G |  |
| Art | TEXT(40) | ja |  | G |  |
| Berechtigte | INTEGER | ja |  | B | Zahl der Stimmberechtigten beim Start, eingefroren als Grundlage des Quorums |
| Beschreibung | TEXT |  |  | G |  |
| Ergebnis | TEXT |  |  | B | festgestelltes Ergebnis beim Abschluss (eingefroren) |
| ErstelltAm | INTEGER | ja |  | G |  |
| ErstelltVonPersonId | TEXT | ja |  | G |  |
| Frist | INTEGER | ja |  | G |  |
| GewinnerOptionId | TEXT |  |  | G |  |
| Mehrfachauswahl | INTEGER | ja |  | G |  |
| QuorumProzent | INTEGER | ja |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| Titel | TEXT | ja |  | G |  |

Indizes: (Status)

## Vertretungsanfragen

Klasse: `FraktionPerfekt.Domain.Vertretung.Vertretungsanfrage`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| BeantwortetAm | INTEGER |  |  | G |  |
| FristBis | INTEGER | ja |  | G |  |
| GestelltAm | INTEGER | ja |  | G |  |
| PersonId | TEXT | ja | Index | G |  |
| Reihenfolge | INTEGER | ja |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| VertretungsfallId | TEXT | ja | → Vertretungsfaelle, Index | G |  |

Indizes: (VertretungsfallId); (PersonId, Status)

## Vertretungsfaelle

Klasse: `FraktionPerfekt.Domain.Vertretung.Vertretungsfall`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| AbgeschlossenAm | INTEGER |  |  | G |  |
| AbwesenheitId | TEXT |  | Index | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| GremiumId | TEXT | ja |  | G |  |
| Status | TEXT(40) | ja | Index | G |  |
| TerminId | TEXT | ja | → Termine, Index | G |  |
| VertretenePersonId | TEXT | ja |  | G |  |
| VertreterPersonId | TEXT |  |  | G |  |

Indizes: (AbwesenheitId); (Status); (TerminId)

## Vertretungskandidaten

Klasse: `FraktionPerfekt.Domain.Vertretung.Vertretungskandidat`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel |
| PersonId | TEXT | ja | → Personen, Index | G |  |
| Position | INTEGER | ja | eindeutig | G | Rangfolge beim Start (1 = zuerst gefragt) |
| VertretungsfallId | TEXT | ja | → Vertretungsfaelle, eindeutig | G |  |

Indizes: (PersonId); (VertretungsfallId, Position) eindeutig

## WikiSeiten

Klasse: `FraktionPerfekt.Domain.Wissen.WikiSeite`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Archiviert | INTEGER | ja |  | G |  |
| Bereich | TEXT | ja |  | G |  |
| ErstelltAm | INTEGER | ja |  | G |  |
| GremiumId | TEXT |  |  | G |  |
| Titel | TEXT | ja | eindeutig | G |  |
| Vertraulich | INTEGER | ja |  | G |  |

Indizes: (Titel) eindeutig

## WikiVersionen

Klasse: `FraktionPerfekt.Domain.Wissen.WikiVersion`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| Kommentar | TEXT |  |  | G |  |
| Nummer | INTEGER | ja | eindeutig | G |  |
| PersonId | TEXT |  |  | G |  |
| SeiteId | TEXT | ja | eindeutig | G |  |
| Text | TEXT | ja |  | G |  |
| VorschlagId | TEXT |  |  | G |  |

Indizes: (SeiteId, Nummer) eindeutig

## WikiVorschlaege

Klasse: `FraktionPerfekt.Domain.Wissen.WikiVorschlag`

| Spalte | Typ | Pflicht | Schlüssel/Verweis | Einstufung | Bedeutung |
|---|---|---|---|---|---|
| Id | TEXT | ja | PK | G | Schlüssel (Zeilen-Id) |
| Am | INTEGER | ja |  | G |  |
| Art | TEXT(20) | ja |  | G |  |
| BasisVersion | INTEGER | ja |  | G |  |
| Begruendung | TEXT | ja |  | G |  |
| EntschiedenAm | INTEGER |  |  | G |  |
| EntschiedenVonPersonId | TEXT |  |  | G |  |
| Nachher | TEXT |  |  | G |  |
| Quelle | TEXT | ja |  | G | Name des KI-Systems (keine Person) |
| SeiteId | TEXT | ja | Index | G |  |
| Status | TEXT(20) | ja | Index | G |  |
| Vorher | TEXT | ja |  | G |  |

Indizes: (SeiteId, Status)
