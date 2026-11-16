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

### Keine Kopien

Die Datenbank ist in 3. Normalform: Es gibt keine Spalten, die Werte aus anderen Tabellen oder Versionen kopieren. Der aktuelle Stand
wird zur Abfragezeit aus den Versionen berechnet. Ausnahmen sind ausdrücklich als **Beleg** eingestuft (eingefrorene Werte wie
Empfänger einer versandten Mail oder Spender auf der Quittung).
