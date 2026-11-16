# Datenbank-Regeln: Bestandsaufnahme (16.11.2026)

Prüfung des Datenmodells von FraktionPerfekt gegen die Datenbank-Regeln aus VibeVHS (Übergabe vom 16.11.2026). Stand des Modells:
57 Tabellen, 568 Spalten (vollständig in `docs/datenbank-schema.md`, eingestuft in `Spalteneinstufung.cs`). Messwerte vor dem
Umbau: `docs/messungen.md`.

Je Regel: **Ist** → **Abweichung** → **Etappe**. Etappen nach Abschnitt 8: 1 Kopien entfernen (dazu fehlende Versionierung nach Regel 2
und Personenfelder nach Regel 7), 2 JSON auflösen, 3 Änderungsvorgänge, 4 Ids und Zeitformate (dazu Mandanten-Vorbereitung),
5 erneute Messung, 6 Doku des Endstands. Je Etappe ein Commit mit grünen Tests; jede Migration wird auf einer Kopie einer bestehenden
Datenbank (`VACUUM INTO`) geprüft.

## 1. Keine Kopien, volle Normalisierung

| Ist | Abweichung | Etappe |
|---|---|---|
| `Personen.Vorname/Nachname/Titel/Anrede/EMail/Mobil` sind ein Zwischenstand aus den Versionen, fortgeschrieben nach jeder Änderung und nachts (Fristenprüfung „Personen-Zwischenstand“). | 6 Kopien + 1 Fortschreib-Job | 1: Spalten und Job entfallen; Listen, Suche, Anmeldung und Postausgang fragen `StandAm` im Paket ab (eine Abfrage je Versionstabelle, nicht N+1). |
| `Sitzungen.GremiumId`, `Sitzungen.Titel` werden beim Anlegen aus dem Termin kopiert. | 2 Kopien | 1: aus dem Termin lesen. |
| `WikiSeiten.Version`, `WikiSeiten.GeaendertAm` wiederholen Nummer und Zeit der letzten `WikiVersionen`-Zeile. | 2 Kopien | 1: aus den Versionen berechnen. |
| `Aufgaben.ErledigtVon`, `Termine.AngelegtVon`, `WikiVorschlaege.Von` speichern Personennamen als Text. | 3 Namenskopien | 1: Personenverweis (bzw. Kennzeichen „KI“ beim Wiki-Vorschlag). |
| Versionstabellen speichern `ErfasstVon` als Namen. | Namenskopie in 9 Tabellen | 3: Verweis auf den Änderungsvorgang. |
| Eingefrorene Werte: Postausgang (Empfänger, Auslöser), Protokoll (Benutzer, Rolle), Spenden (Spender, Anschrift), Kassenbuch, Auslagen, Umfragen (Berechtigte beim Start, festgestelltes Ergebnis). | keine – als **Beleg** eingestuft und begründet | – |
| Abgeleitete Werte: keine gespeicherten Altersgruppen o. ä. | – | – |
| Architekturtest: neu (`DatenbankArchitekturTests`): jede Spalte eingestuft, Kopien/JSON nur in der Übergangsliste, „…Name“/„…Von“ neben Personenverweis nur begründet, keine „Statistik…“-Spalten. | – | Übergangsliste wird je Etappe kürzer und ist am Ende leer. |
| Indizes `(EntitaetId, GueltigVon)` an allen Versionstabellen vorhanden. | – | – |

## 2. Bitemporale Datenhaltung

| Ist | Abweichung | Etappe |
|---|---|---|
| Person: Name (mit Titel), Anrede, Geschlecht, Funktion, Rollen, Adressen, E-Mails, Rufnummern, Einwilligungen sind bitemporal (`BitemporaleVersion`, `ZeitlicheHistorie`), Änderung ab Datum und Korrektur, stabile Identität (`AdressId`, `NummerId`). Versionen werden nie gelöscht (Prüfung in `SaveChanges`). | – | – |
| Beenden einer Gültigkeit: `ErsetztAm` an der alten Zeile, neue Zeile mit Ende (Abgrenzung). | entspricht der Regel | – |
| Korrektur von Gültigkeitsbeginn und -ende („Umzugsdatum korrigieren“) | fehlt: Korrektur ersetzt nur den Wert, nicht den Zeitraum | 1: `ZeitlicheHistorie.ZeitraumKorrigieren` + Test mit dem Beispiel Muster (Zeilen A–E). |
| `Personen.Aktiv` | veränderlich, nicht versioniert | 1: Merkmal „aktiv“ mit Gültigkeit. |
| `Mitgliedschaften.Amt`, `Mitgliedschaften.FunktionText` | veränderlich, nicht versioniert (Funktion und Rang sind es) | 1: in `MitgliedschaftsVersionen`. |
| Gremien (Name, Kurzname, aktiv) ohne Versionen | Regel 2 | 1: bitemporal (Entscheidung Klaus, 16.11.2026: alte Protokolle zeigen den damaligen Namen). |
| Pressekontakte, Ressourcen, Gebiete, Haushaltsposten, Themenpaten | – | Entscheidung Klaus (16.11.2026): bleiben Arbeitsdaten mit Änderungsprotokoll, keine Versionen. |

## 3. Änderungsvorgänge

| Ist | Abweichung | Etappe |
|---|---|---|
| `ErfasstAm`, `ErfasstVon`, `Erfassungsart`, `Grund` stehen in jeder der 9 Versionstabellen. | wiederholte Metadaten | 3: Tabelle `Aenderungsvorgaenge`; Versionen verweisen per `VorgangId`; `GueltigVon/GueltigBis/ErsetztAm` bleiben an der Zeile. Abgrenzungszeilen erhalten einen eigenen Vorgang (Art „Abgrenzung“) mit gleicher Zeit, Person und Grund. |
| Audit-Protokoll (`Protokoll`) getrennt von den Versionen | zwei Stellen für „wer hat was geändert“ | 3: Änderungsvorgang verweist auf den Protokolleintrag desselben Speichervorgangs (eine Stelle für wer/wann/warum; das Protokoll behält auch Nicht-Stammdaten-Aktionen). |

## 4. Ids

| Ist | Abweichung | Etappe |
|---|---|---|
| `Ids.Neu()` = `Guid.CreateVersion7()` zentral. | nicht deterministisch in Tests | 4: Id-Erzeuger austauschbar (Tests: Zähler mit fester Zeit). |
| 123 Guid-Spalten als TEXT (36 Zeichen). | Speicherform | 4: globaler Konverter Guid ↔ 16-Byte-BLOB (`ToByteArray(bigEndian: true)`), Migration wandelt Bestandsdaten (`unhex`). |
| Ids als Text: URLs (`personen/{id}`), `Pseudonym` im Protokoll, `KandidatenText`, Roh-SQL in der Migration `PersonKontakte`, ICS-UIDs, Bildpfade `dateien/bild/{guid}`. | prüfen | 4: URLs, Pfade und ICS bleiben Text (Anzeigeform), Roh-SQL und Abfragen werden geprüft; `KandidatenText` entfällt in Etappe 2. |

## 5. Zeitangaben

| Ist | Abweichung | Etappe |
|---|---|---|
| 53 Zeitpunkt-Spalten (`DateTimeOffset`) mit `DateTimeOffsetToBinaryConverter` (Ticks mit Versatz). | nicht ms seit 1970 | 4: int64 Millisekunden UTC, Migration rechnet um. |
| 15 Kalendertag-Spalten (`DateOnly`) als Text `JJJJ-MM-TT`; offenes Ende = `NULL`. | nicht int JJJJMMTT, offenes Ende nicht 99991231 | 4: int JJJJMMTT; offenes Ende 99991231 in der Datenbank. |
| 6 Spalten mit **Ortszeit** (`DateTime`): `Termine.Beginn/Ende`, `Buchungen.Von/Bis`, `Schichten.Von/Bis`. | lokale Zeit gespeichert | 4: UTC-Zeitpunkte; Umrechnung je Termin über den zentralen Zeitdienst. |
| Zentrale Umrechnung `Ortszeit` (fest Europe/Berlin, statisch). | nicht einstellbar | 4: Zeitzone aus der Mandanten-Konfiguration, ersatzweise Systemzeitzone. |
| Serientermine: Fraktionssitzungen 14-täglich (Demodaten), RIS-Sitzungen in festen Abständen (Mock). | kein Test über die Zeitumstellung | 4: Test für beide Umstellungstage (gleiche Ortszeit). |
| Lesbare SQL-Views | fehlen | 4: Views `v_…` mit Zeiten und Daten als Text. |
| Hash-Ketten | keine im Protokoll (nur Datei-Hashes der Sicherungen, unabhängig von der Speicherform) | – (nach Etappe 4 bestätigt). |

## 6. Mandantenfähigkeit

| Ist | Abweichung | Etappe |
|---|---|---|
| Eine Datenbank, Verbindung fest aus der Konfiguration beim Start. Keine MandantId-Spalten. | Verbindung fest | 4: Mandant aus Konfiguration (Name, Bundesland, Logo, Absender, Zeitzone), Auflösung über einen Dienst je Anfrage (vorerst ein Mandant), Verbindungszeichenfolge vom Mandanten. |
| Mandantenbezogene Singletons: `Ortszeit.Zeitzone` (statisch), Fraktionsname/Logo in Layout und PDFs. | prüfen | 4: über den Mandanten-Dienst. |

## 7. Namen und Personen

| Ist | Abweichung | Etappe |
|---|---|---|
| Titel ist eigenes Feld der Namensversion (zeitlich, nie Teil von Vor-/Nachname). | in VibeVHS eigenes Merkmal | 1: eigenes versioniertes Merkmal „Titel“; Hinweis „Als Titel übernehmen?“, wenn im Vornamen ein Grad steht. |
| Namensvorsatz/-zusatz („von“, „jun.“) | fehlen | 1: erledigt – Spalten `Namensvorsatz`, `Namenszusatz`; Sortierung in SQL mit Collation `DIN5007_2` und Index, kein gespeicherter Sortierschlüssel (Festlegung Klaus 16.11.2026). Haken: externe Werkzeuge ohne Collation können nicht nach Namen sortieren und nicht in `PersonNamen` schreiben (siehe `docs/datenbank-einleitung.md`). |
| Nur ein Name laut Ausweis | Vorname ist Pflicht | 1: leerer Vorname mit Kennzeichen. |
| Geschlecht und Anrede versioniert | – | – |
| Persönliche Ansprache (Du/Sie, Stil, Titel nennen) | Benutzereinstellung ohne Historie (Spalten an `Personen`) | 1: Merkmal mit Gültigkeit. |

## 8. Vorgehen

| Ist | Abweichung | Etappe |
|---|---|---|
| Schema-Doku | neu: `docs/datenbank-schema.md`, erzeugt aus dem Modell, Einleitung `docs/datenbank-einleitung.md`, Test bei veralteter Doku. | – |
| Baseline-Messung | neu: `MessungTests` (`FP_MESSUNG=1` bzw. `=10`), Ergebnisse in `docs/messungen.md`. | 5: Vergleich. |
| Demodaten relativ zum Tagesdatum | Termine relativ zum Montag der aktuellen Woche (ganze Wochen); Ratsbeginn und Wahlperiode sind echte feste Daten; RIS-Mock-Sitzungen laufen in festen Abständen ab einem festen Startdatum (auch in zwei Wochen gleichmäßig, aber nicht wochengenau verschoben). | 4: RIS-Mock ab einem Start relativ zum Tagesdatum in ganzen Wochen. |

## Entscheidung (16.11.2026)

Klaus: Außer Personen und Mitgliedschaften werden nur **Gremien** (Name, Kurzname, aktiv) bitemporal. Pressekontakte, Ressourcen,
Gebiete und Haushaltsposten bleiben Arbeitsdaten mit Änderungsprotokoll.
