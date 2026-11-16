# Laufzeitmessungen

Messung mit `MessungTests` (Infrastruktur-Tests, nur auf Anforderung: `FP_MESSUNG=1` bzw. `=10`). SQLite-Datei wie im Betrieb (WAL),
Demodaten; bei Faktor 10 werden Personen (mit Rollen und Mandaten) und Termine neunmal zusätzlich angelegt. Je Messpunkt 8 Läufe, der
erste zum Aufwärmen; angegeben ist der **Median** der übrigen 7. Jeder Lauf in eigenem Bereich (eigener DbContext) wie eine Anfrage, mit
der strengen Abfrageprüfung der Tests. Entwicklungsrechner (Windows 11).

## Baseline vor dem Umbau (16.11.2026, Stand e0cc086)

| Messpunkt | realistisch: 53 Personen, 170 Termine, 204 Mandate | 10×: 530 Personen, 1 700 Termine, 2 040 Mandate |
|---|---:|---:|
| Liste: Personen (alle, mit Rollen und Mandatszahl) | 16,8 ms | 144,3 ms |
| Liste: Kalender, aktueller Monat | 25,4 ms | 147,9 ms |
| Folgeseite: Kalender, Folgemonat | 35,2 ms | 145,4 ms |
| Filter: Kalender nach Gremium (3 Monate) | 30,6 ms | 79,7 ms |
| Suche „kühlert“ (alle Module) | 105,7 ms | 286,4 ms |
| Detail: Person (Stand heute, Mandate, Verlauf) | 11,3 ms | 29,4 ms |
| Detail: Termin | 30,7 ms | 74,7 ms |
| Ermittlung: Mandatsübersicht (alle Gremien mit Besetzung) | 9,9 ms | 28,7 ms |
| Ermittlung: Startseite „Heute“ | 74,1 ms | 132,6 ms |
| Ermittlung: Offene Vertretungen | 27,0 ms | 76,8 ms |
| Ermittlung: Kalender-Abo (ICS) einer Person | 57,0 ms | 138,8 ms |
| Dateigröße (Datenbank + WAL) | 1 152 KB | 3 412 KB |

Die Personenliste hat keine Seitenaufteilung (eine Fraktion hat unter 100 Personen); als „Folgeseite“ dient der Folgemonat im Kalender.
Auffällig: Liste, Kalender und Suche wachsen etwa linear mit der Datenmenge (alle Personen bzw. Mitgliedschaften werden geladen).

## Personenliste mit 30 000 Personen (Collation DIN5007_2, 16.11.2026)

`FP_MESSUNG_PERSONEN=30000` (`MessungTests.Personenliste_mit_vielen_Personen`), Median aus 5 Läufen nach einem Aufwärmlauf:

| Messpunkt | 30 000 Personen |
|---|---:|
| Erste Seite (50 Namen, SQL `ORDER BY … LIMIT 50` über den Index) | 2,4 ms |
| Namensliste vollständig sortiert (SQL, Collation, Index) | 90,1 ms |
| Alle Personen laden (alle Versionen im Paket, Stand heute, sortiert) | 1 297 ms |
| Dateigröße | 21,0 MB |

`EXPLAIN QUERY PLAN` der Namensliste: Scan über `IX_PersonNamen_DIN5007` ohne zusätzliche Sortierung (Test `Din5007Tests`).
Eine Fraktion hat unter 100 Personen; die 30 000 zeigen, dass die Sortierung in SQL mit Collation trägt.

## Nach dem Umbau

(folgt)
