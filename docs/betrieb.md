# Betrieb: Sicherung und Wiederherstellung

Übernommen aus VibeVHS, angepasst auf FraktionPerfekt.

## Datensicherung (nativ unter Windows, jetzt)

- Täglich um **01:30 Uhr** (`Sicherung:Zeitplan`, Quartz-Cron) sichert FraktionPerfekt die SQLite-Datenbank mit der
  **Online-Backup-API** von SQLite. Die Sicherung ist konsistent, auch während jemand arbeitet.
- Ziel ist `Sicherung:Ordner` (Standard: Ordner `sicherung` neben der Datenbank). Je Sicherung entstehen eine Datei
  `fraktionperfekt-JJJJMMTT-HHMMSS.db` und eine `.json` mit **SHA-256**, Größe, Migrationsstand und Testergebnis.
- Direkt danach läuft der **Wiederherstellungstest**:
  - Prüfsumme vergleichen.
  - Kopie in einen temporären Ordner „wiederherstellen“ und öffnen.
  - `PRAGMA integrity_check` ausführen.
  - Migrationsstand mit der laufenden Datenbank vergleichen.
  - Zeilen der Kerntabellen zählen (Personen, Gremien, Mitgliedschaften, Termine, Abwesenheiten, Vertretungsfälle, Protokoll).

  Das Ergebnis steht im Protokoll und auf der Seite **Betrieb und Sicherung** (`/betrieb`, nur Administration). Dort
  gibt es auch „Jetzt sichern“ und „Erneut prüfen“.
- Es bleiben die letzten `Sicherung:Behalten` Sicherungen (Standard 14). `Sicherung:Aktiv=false` schaltet den Job ab.
- **Wichtig:** Der Sicherungsordner liegt standardmäßig auf demselben Datenträger. Für echten Schutz den Ordner auf ein
  anderes Laufwerk legen oder von einer Dateisicherung erfassen lassen. Das Konzept verlangt tägliche, verschlüsselte
  Backups an einem zweiten Ort.

## Wiederherstellung im Notfall

1. Anwendung beenden.
2. Auf der Seite **Betrieb und Sicherung** die jüngste Sicherung mit „ok“ wählen (oder „Erneut prüfen“).
3. Prüfsumme vergleichen: `Get-FileHash fraktionperfekt-….db -Algorithm SHA256` muss dem Wert in der `.json` entsprechen.
4. Die laufende `fraktionperfekt.db` (und gegebenenfalls `-wal` und `-shm`) beiseitelegen, die Sicherung als
   `fraktionperfekt.db` einsetzen.
5. Anwendung starten. Ist die Sicherung älter, laufen die Migrationen beim Start automatisch nach.
6. Im Protokoll vermerken, was verloren ist (Zeitraum seit der Sicherung). Den Postausgang prüfen: Vertretungsanfragen
   aus diesem Zeitraum gegebenenfalls neu anstoßen.

## Später unter Linux: Litestream

Litestream repliziert das WAL fortlaufend (Sekunden statt einmal täglich). Voraussetzung ist der WAL-Modus, den
FraktionPerfekt beim Start setzt. Die tägliche Online-Sicherung bleibt als zweite, unabhängige Linie bestehen. Die
Wiederherstellung regelmäßig mit `litestream restore` und `PRAGMA integrity_check` proben. Vorlage: siehe VibeVHS
`deploy/linux/litestream.yml`.

## Hintergrundjobs (Quartz)

| Job | Takt |
|---|---|
| Postausgang zustellen | alle 15 s (`Postausgang:TaktSekunden`) |
| Vertretungsfristen prüfen (weitergeben, eskalieren, abschließen) | alle 5 min |
| RIS-Abgleich | täglich 02:30 |
| Sicherung mit Wiederherstellungstest | täglich 01:30 (`Sicherung:Zeitplan`) |

`Hintergrundjobs:Aktiv=false` schaltet alle Jobs ab (für Tests).
