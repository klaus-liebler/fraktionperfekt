## Bitemporale Datenhaltung

Alle veränderlichen Angaben zu Personen (Name mit Titel, Anrede, Geschlecht, Funktion, Rollen, Adressen, E-Mails, Rufnummern,
Einwilligungen) und zu Gremienmitgliedschaften werden **bitemporal** gespeichert. Es gibt zwei Zeitachsen:

| Achse | Spalten | Bedeutung |
|---|---|---|
| **Gültigkeitszeit** | `GueltigVon`, `GueltigBis` | Wann galt die Angabe in der Welt? Halboffen: `GueltigVon` einschließlich, `GueltigBis` ausschließlich (leer = offen). |
| **Erfassungszeit** | `ErfasstAm`, `ErsetztAm` | Wann wusste das System das? Ebenfalls halboffen: ab `ErfasstAm` bis `ErsetztAm` (leer = aktueller Wissensstand). |

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
SELECT a.* FROM PersonAdressen a
WHERE a.PersonId = :person
  AND a.GueltigVon <= :tag AND (a.GueltigBis IS NULL OR a.GueltigBis > :tag)
  AND a.ErfasstAm <= :bekannt AND (a.ErsetztAm IS NULL OR a.ErsetztAm > :bekannt);
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
