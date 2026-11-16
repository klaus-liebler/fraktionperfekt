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

### Speicherformen (Datenbank-Regeln 4 und 5)

| Art | Speicherform | Beispiel |
|---|---|---|
| Ids | 16-Byte-BLOB, Big-Endian, UUIDv7 (zeitlich sortiert), zentral über `Ids.Neu()` | `X'01A1E6302C7B7544…'` |
| Zeitpunkte (Erfassung, Protokoll, Fristen) | INTEGER, UTC in Millisekunden seit 1970 | `1794859314079` |
| Kalendertage (Gültigkeit, Beschlussdatum) | INTEGER `JJJJMMTT`, offenes Ende `99991231` | `20251101` |
| Termine mit Uhrzeit | INTEGER, UTC-Millisekunden; je Termin mit dem an diesem Tag gültigen Versatz aus der Ortszeit umgerechnet | dienstags 19:00 → Winter 18:00 UTC, Sommer 17:00 UTC |

Umrechnungen zwischen Ortszeit und UTC laufen zentral über `Ortszeit` (Zeitzone aus der Mandanten-Konfiguration, ersatzweise die
Systemzeitzone). **Lesbare Views** für Prüfungen und externe Werkzeuge: `v_PersonNamen`, `v_PersonMerkmale`, `v_PersonRollen`,
`v_Mitgliedschaften`, `v_Termine`, `v_Aenderungsvorgaenge`, `v_Protokoll` zeigen Ids als GUID-Text, Tage als `JJJJ-MM-TT` („offen“) und
Zeitpunkte als UTC-Text; die Namensspalten tragen dort `COLLATE BINARY`, damit die Views auch ohne die Collation `DIN5007_2` lesbar sind
(eine View, die eine Spalte mit unbekannter Collation durchreicht, scheitert sonst schon beim einfachen Lesen).

### Keine Kopien

Die Datenbank ist in 3. Normalform: Es gibt keine Spalten, die Werte aus anderen Tabellen oder Versionen kopieren. Der aktuelle Stand
wird zur Abfragezeit aus den Versionen berechnet. Ausnahmen sind ausdrücklich als **Beleg** eingestuft (eingefrorene Werte wie
Empfänger einer versandten Mail oder Spender auf der Quittung).
