# Übertragung aus SchoolCraft nach FraktionPerfekt: priorisierte Kandidaten

Stand: 24.11.2026 · Vergleich SchoolCraft `main` (3e8d226) mit FraktionPerfekt `main` (9d08300, lokal) · **Klaus hat am 24.11.2026 die Punkte 1 bis 12 freigegeben** (13 und 14 nicht); Umsetzungsstand je Punkt unten unter „Umgesetzt“.

Gleicher Aufbau wie der Rückport nach VibeVHS (`rueckport-schoolcraft.md`). Für FraktionPerfekt gilt anders: kleine Nutzerzahl
(18 Ratsmitglieder, 32 sachkundige Bürger:innen, Kampagnenteam), überwiegend **Handy**, Handy-Seiten statisch gerendert,
**EF-Migrationen** (nur zusätzliche Migrationen, Testsystem-Datenbank bleibt erhalten), Mandanten mit je eigener SQLite-Datei schon vorbereitet.

**Aufwand:** S = eine Arbeitssitzung, M = ein bis zwei Sitzungen, L = mehrere Sitzungen mit eigenem Fachkonzept.
**Priorität:** P1 = sofort lohnend, P2 = lohnend, P3 = nur bei konkretem Bedarf.

## Kurzantwort: Gibt es schon eine geheime Abstimmung?

**Nein.** Modul 13 (Abstimmungen, Umfragen, Terminfindung) ist eine **nicht öffentliche, aber namentliche** Abstimmung:

- Jede `Stimme` speichert `PersonId` + `OptionId` (`Domain/Abstimmungen/Umfrage.cs`). Wer Zugriff auf die Datenbank oder eine Sicherung hat, sieht, wer wie gestimmt hat.
- Die Oberfläche zeigt nur, **dass** jemand abgestimmt hat („Abgestimmt haben: …“), und das Protokoll nennt nur Pseudonyme – der Kommentar „Geheim“ im `UmfrageDienst` meint nur das Protokoll.
- Der **Zwischenstand ist während der Abstimmung für alle sichtbar**, und Stimmen sind bis zur Frist änderbar – beides verträgt sich nicht mit einer geheimen Wahl.
- Personenwahlen (Kandidaturen, Wahlgänge, Stichwahl) und eine Prüfung der Beschlussfähigkeit gibt es nicht; Beschlüsse im Sitzungsmodus werden als Ja/Nein/Enthaltung-Zahlen ohne Plausibilitätsprüfung erfasst.

SchoolCraft hat die digitale geheime Wahl selbst nur spezifiziert (FA-3303/FA-3304, Stand „offen“); umgesetzt ist dort die **Präsenzwahl mit
Erfassung** (Plausibilität, Mehrheit, Stichwahl, Wahlprotokoll). Die geheime Abstimmung ist deshalb **Punkt 1** dieser Liste und eine Neuentwicklung,
die anschließend nach SchoolCraft zurückfließen kann.

## Übersicht

| Nr. | Konzept | Nutzen für die Fraktion | Aufwand | Priorität |
| --- | --- | --- | --- | --- |
| 1 | **Geheime Abstimmung aller Stimmberechtigten** (neu, Grundlage SchoolCraft FA-3304) | sehr hoch (Personalfragen, Listenplätze, strittige Positionen) | M | **P1** |
| 2 | Löschen und Absagen nur nach Rückfrage (FA-3230) | hoch | S | **P1** |
| 3 | Alles-Suche mit Vorschlägen, Funktionen, <kbd>Strg</kbd>+<kbd>K</kbd> (FA-3231) | mittel–hoch | S–M | **P1** |
| 4 | Fehlertolerante Personen- und Kontaktsuche (FA-3227/3228) | hoch (Kontakte, Bürgeranliegen, Vertretungen) | S | **P1** |
| 5 | Tastenkürzel als `<kbd>` und Kürzelliste (FA-3236) | mittel (nur Rechner) | S | **P1** (mit 3) |
| 6 | Wahlen und Ämter mit Amtszeit (Modul 33: Kandidaturen, Stichwahl, Wahlprotokoll) | hoch | M | **P2** |
| 7 | Abstimmen im Sitzungsmodus: Beschlussfähigkeit, Plausibilität, Stimmzettel erfassen | hoch | S–M | **P2** |
| 8 | Beamer-/Monitoransicht der Sitzung mit Token (FA-412 ff.) | mittel | S–M | **P2** |
| 9 | Übungsinstanzen über die Mandanten statt Touren mit echten Aktionen (FK-14) | hoch vor dem Echtbetrieb | M (ohne Spielzeit) | **P2** |
| 10 | 30-Minuten-Korrekturregel (FA-3235) | mittel | S–M | P3 |
| 11 | Einheitliches Anlegen per Dialog (FA-3229) | mittel | M | P3 |
| 12 | Rundschreiben mit Gestaltungen, Anreden, Archiv als Vorlage (FA-820 ff.) | mittel | M | P3 |
| 13 | Podium für Meinungsbilder bei Bürgerdialogen und Klausuren (FK-15) | mittel | S (Link) bis M | P3 |
| 14 | Telefonnotiz → Bürgeranliegen (Modul 31, ohne Sprachagent) | gering–mittel | S | P3 |

**Empfohlenes erstes Paket:** 1 + 2 (eine bis zwei Sitzungen). Danach 3 + 4 + 5 als zweites Paket, 6 + 7 bauen auf 1 auf.

---

## P1 – sofort lohnend

### 1 Geheime Abstimmung aller Stimmberechtigten (neu)

**Ziel:** Eine Abstimmung, bei der niemand – auch nicht Vorstand, Administration oder wer die Datenbank liest – erkennen kann, wie eine Person gestimmt hat,
während jede stimmberechtigte Person genau einmal stimmen kann und das Ergebnis nachprüfbar bleibt.

**Fachlich (⚑ Geschäftsordnung der Fraktion prüfen):**

- Neue Eigenschaft `Geheim` bei Antragsabstimmung, Meinungsbild und Umfrage (nicht bei der Terminfindung). Typische Fälle: Wahl und Abwahl im Vorstand,
  Benennungen für Ausschüsse, Listenplätze, strittige Positionen; viele Fraktions-GOs schreiben geheime Wahl vor, wenn ein Mitglied es verlangt.
- **Stimmberechtigte** werden beim Start als feste Liste gespeichert (heute nur die Zahl): Antragsabstimmung = Rolle Mitglied, sonst wie bisher.
  Ob sachkundige Bürger:innen mitstimmen, legt die GO fest ⚑ – als Einstellung je Abstimmungsart, nicht fest im Code.
- **Kein Zwischenstand**, bis die Abstimmung beendet ist; sichtbar ist nur „12 von 18 haben abgestimmt“ und (Wählerverzeichnis) wer schon abgestimmt hat.
- **Stimme endgültig:** Nach der Abgabe nicht mehr änderbar (sonst bräuchte es eine Verknüpfung Person–Stimme). Vor dem Absenden Rückfrage mit der gewählten Antwort.
- Beenden: zur Frist, vorzeitig durch den Vorstand oder automatisch, sobald alle abgestimmt haben. Ergebnis mit Beteiligung und Quorum wie heute.
- **Kleine Gruppen:** Bei Einstimmigkeit ist das Ergebnis zwangsläufig offen; bei weniger als 3 Stimmen wird nur „zu wenige Stimmen für eine geheime Auswertung“ gezeigt (Schwelle einstellbar ⚑).

**Technisch (FA-3304 „getrennt gespeichert, kein Rückschluss“):**

- Zwei Tabellen statt `Stimme`: `Stimmvermerk` (UmfrageId, PersonId, Am) und `Stimmzettel` (UmfrageId, OptionId) – **ohne PersonId, ohne Zeitpunkt**.
- Beide in **einer** Transaktion schreiben (genau eine Stimme je Vermerk; eindeutiger Index auf UmfrageId + PersonId im Vermerk).
- **Keine zeitlich sortierbaren Ids** für Stimmzettel: `Ids.Neu()` (UUIDv7) verrät die Reihenfolge und damit über den Vermerk-Zeitpunkt die Person.
  Stimmzettel bekommen zufällige Ids (UUIDv4) und werden als `WITHOUT ROWID` mit dieser Id als Schlüssel gespeichert, damit auch die physische
  Reihenfolge in der SQLite-Datei nichts verrät. Ein Architekturtest sichert das ab.
- Protokoll, Serilog, Postausgang und Push nennen nie die gewählte Option; die Seite schickt die Wahl per POST ohne Option in der Adresse.
- **Ehrliche Grenze** (in Hilfe und Konzept): Gegen den Betreiber des Servers, der Anfragen mitschneidet, schützt das nicht vollständig. Für
  Fraktionszwecke ist das angemessen; eine kryptografische Lösung (blinde Signaturen, Ende-zu-Ende) wäre L und braucht eine eigene Entscheidung.
- Variante für die Sitzung: siehe Nr. 7 (Präsenz, alle stimmen gleichzeitig am Handy, Ergebnis auf dem Beamer, Nr. 8).

**Migration:** zusätzliche Migration (`GeheimeAbstimmung`); bestehende offene Abstimmungen bleiben namentlich.

**Umgesetzt (24.11.2026) ✅:** wie beschrieben. Abweichungen: Die feste Liste der Stimmberechtigten gilt für **alle** neuen Abstimmungen (ältere
ohne Liste prüfen weiter die Rolle). Mehrfachauswahl bei geheimen Umfragen ist erlaubt (je gewählte Antwort ein Stimmzettel). Geheim abstimmen
lässt sich beim Start wählen (Kästchen); dass ein Mitglied es verlangen kann, ist nur als ⚑ in Konzept und Einstellungen vermerkt. Geheime
Antragsabstimmung mit weniger als 3 Stimmen gilt als nicht angenommen (Antrag „abgelehnt“, Beschluss ohne Zahlen). Tests:
`GeheimeAbstimmungTests`, Architekturtest `Geheime_Stimmzettel_ohne_Person_ohne_Zeit_ohne_rowid`.
**Tests:** keine Spalte mit PersonId neben OptionId, doppelte Stimme abgewiesen, kein Zwischenstand vor Ende, Rechte (Kampagnenteam, Nicht-Mitglied), Protokoll ohne Option.
**Konzept:** Modul 13 in `docs/konzept.md` ergänzen, Hilfe und Abnahme-Drehbuch nachziehen.
**Aufwand:** M. **Rückfluss:** dasselbe Verfahren deckt SchoolCraft FA-3304 ab.

### 2 Löschen und Absagen nur nach Rückfrage (FA-3230)

- **SchoolCraft:** `Rueckfragen.cs` (Radzen `DialogService`): Frage nennt Objekt und Folge, Schaltfläche heißt wie die Folge, Klick daneben schließt nicht.
- **FraktionPerfekt heute:** keine Rückfrage, z. B. „Löschen“ bei Dateien, Kontakten und in den Kampagnen; ebenso folgenreiche Klicks wie Abstimmung abschließen, Antrag zurückziehen, Vertretung absagen.
- **Anpassung:** Auf den **statisch gerenderten Handy-Seiten** gibt es keinen Dialog – dort ein zweiter Schritt über `verhalten.js` (data-Attribut, CSP ohne Inline-Skript) oder eine kurze Bestätigungsseite. Als Leitlinie in die README und ins Konzept.
- **Aufwand:** S.
- **Umgesetzt (24.11.2026) ✅:** `Rueckfragen.cs` (wie SchoolCraft, dazu `WennBestaetigtAsync`) an Anhängen, Dateien, Kontakten, ToDo-Vorlagen,
  ToDos, Buchungen (stornieren), Kalender-Abo (widerrufen), Abwesenheit (zurückziehen), Terminbesetzung („Doch nicht“), Vertretung absagen
  („Kann nicht“), Sitzung beenden, Protokoll freigeben, Antrag zurückziehen, Abstimmung abschließen. Handy: `data-rueckfrage` (Browser-Rückfrage
  `confirm`, keine eigene Bestätigungsseite) bei „Kann nicht“ und „abgehängt“. Bewusst ohne Rückfrage: Zusage/Absage zu Terminen (änderbar,
  Ein-Tipp-Antwort). Leitlinie in README und Konzept, Test `RueckfrageTests` prüft alle Schaltflächen.

### 3 Alles-Suche verbessern (FA-3231)

- **FraktionPerfekt heute:** `SucheDienst` sucht schon über alle Module **mit den Rechten des jeweiligen Moduls** (das Wichtigste ist also da); die Kopfzeile schickt aber nur ein GET-Formular an `/suche`, ohne Vorschläge beim Tippen, ohne Pfeiltasten, ohne Kürzel und ohne Funktionen.
- **Übernehmen:** Vorschläge beim Tippen, <kbd>↑</kbd> <kbd>↓</kbd> <kbd>Enter</kbd>, <kbd>Strg</kbd>+<kbd>K</kbd>, Treffer auf **Funktionen** („Abwesenheit melden“, „Abstimmung starten“, „Auslage einreichen“) – Funktionen nur, wenn die Rolle sie darf.
- **Anpassung:** Auf dem Handy bleibt die Suchseite; die Vorschläge nur am Rechner. Grenze acht Treffer je Bereich bleibt.
- **Aufwand:** S–M.
- **Umgesetzt (24.11.2026) ✅:** `suche.js` + `GET /suche/vorschlaege` (JSON, angemeldet, `no-store`): Vorschläge ab zwei Zeichen nach 150 ms,
  je Bereich drei, insgesamt vierzehn, Funktionen zuerst; <kbd>↑</kbd> <kbd>↓</kbd> <kbd>Enter</kbd> <kbd>Esc</kbd>, <kbd>Strg</kbd>+<kbd>K</kbd> von
  überall. `Funktionskatalog` (17 Funktionen mit Stichwörtern, Recht wie Menü/Schnellaktionen) auch auf der Suchseite. Nur am Rechner
  (`min-width: 768px` und feiner Zeiger); auf dem Handy bleibt `/suche`. Tests: `AllesSucheTests`, Webtest auf `/suche/vorschlaege`.

### 4 Fehlertolerante Personen- und Kontaktsuche (FA-3227/3228)

- **SchoolCraft:** `Fachregeln/Unscharf.cs` (rein, deterministisch: ä = ae, ß/ss, Akzente, ein Tippfehler, ab 8 Buchstaben zwei, Bewertung 0–100) und Baustein `Personenauswahl.razor`.
- **FraktionPerfekt heute:** Suche per `Contains` – „Nienteidt“ oder „Kuehlert“ finden nichts. Betroffen: Personen, Kontakte, Bürgeranliegen, Zuweisung von Aufgaben und Themenpaten.
- **Anpassung:** einfacher als in VibeVHS: Personen und Kontakte liegen ohnehin im Speicher, also `Unscharf` direkt übernehmen, kein SQL-Umbau.
- **Aufwand:** S.
- **Umgesetzt (24.11.2026) ✅:** `Unscharf.cs` unverändert nach `Domain/Gemeinsam` übernommen. Globale Suche (Personen sortiert nach Bewertung,
  Bürgeranliegen auch über den Namen), Kontaktsuche (ganze Zeile unscharf, beste zuerst) und ein Baustein `Personenwahl` (Radzen-Auswahl mit
  unscharfem Filter) für Aufgabe zuweisen, Themenpate/Vertretung und Bürgeranliegen zuweisen. Statt des SchoolCraft-Bausteins mit eigener
  Vorschlagsliste die vorhandene Radzen-Auswahl mit `LoadData`. Tests: `UnscharfTests`, `UnscharfeSucheTests`.

### 5 Tastenkürzel als `<kbd>` (FA-3236)

- Jeder Tastenhinweis in Oberfläche, Hilfe und Touren als `<kbd>`, Kürzelliste auf `/hilfe`. Klein und nur am Rechner wirksam; gehört zu Nr. 3.
- **Aufwand:** S.
- **Umgesetzt (24.11.2026) ✅:** Kürzelliste „Tastenkürzel“ in `docs/hilfe.md` (auf `/hilfe`), Tastenhinweise in Oberfläche, Hilfe, Wiki-Doku und
  Suchvorschlägen als `<kbd>`. Die Markdown-Darstellung lässt genau `<kbd>` durch (alles andere HTML bleibt Text). Touren enthalten keine
  Tastenhinweise. Test `TastenkuerzelTests`.

---

## P2 – lohnend

### 6 Wahlen und Ämter mit Amtszeit (Modul 33)

- **SchoolCraft:** Ämter als zeitlich gültige Rollen, Wahlberechtigung abgeleitet statt gepflegt, Wahlgang offen oder geheim, Mehrheit, Gleichstand → Stichwahl, Annahme der Wahl, Wahlprotokoll; Gewählte bekommen die Rolle für die Amtszeit.
- **Für die Fraktion:** Wahl des Vorstands (Vorsitz, Stellvertretung, Kassier), Sprecher:innen je Ausschuss, Benennungen für Ausschüsse und Aufsichtsräte. Heute werden Vorstand und Funktionen von Hand bitemporal gepflegt.
- **Anpassung:** baut auf Nr. 1 auf (geheimer Wahlgang). Ergebnis setzt Rolle bzw. Funktion **ab Datum** (bitemporal, mit Wahl als Grund). Mehrheitsregeln und Stichwahl aus der Fraktions-GO ⚑ als Einstellung. Ablauf der Wahlperiode (Kommunalwahl 2030) → Neuwahlen als Aufgabe.
- **Aufwand:** M.
- **Umgesetzt (24.11.2026) ✅:** `/wahlen` und `/wahlen/{id}`; Wahl, Kandidatur, Amtszeit (Zusatzmigration `WahlenUndAemter`), Wahlgang als
  Abstimmung der neuen Art `Wahl` (Optionen tragen die Person), Mehrheitsregeln in `Mehrheit` (Domain), Annahme setzt Rolle/Funktion bitemporal
  ab Datum und beendet die Vorgänger-Amtszeit, Wahlprotokoll, Fristenprüfung „Neuwahl vorbereiten“. Abweichend von SchoolCraft: ein Sitz je Wahl
  (Benennungen mit mehreren Sitzen = mehrere Wahlen); Los wird nicht gezogen, sondern vom Vorstand erfasst; Sprecher:in/Benennung ändern keine
  Rolle. Ämter, die vor der Einführung von Hand gepflegt wurden (Demodaten), bleiben unberührt, bis eine Wahl sie ablöst. Tests: `MehrheitTests`,
  `WahlAblaufTests`.

### 7 Abstimmen im Sitzungsmodus

- **SchoolCraft:** Präsenzwahl mit Erfassung: Plausibilität (nicht mehr Stimmen als Anwesende), Beschlussfähigkeit (FA-3307).
- **FraktionPerfekt heute:** Beschlüsse im Sitzungsmodus mit frei eingetippten Zahlen; Anwesenheit ist erfasst, wird aber nicht gegen die Stimmen geprüft.
- **Übernehmen:** Beschlussfähigkeit aus Anwesenheit und GO-Quote ⚑ anzeigen; Ja + Nein + Enthaltung ≤ anwesende Stimmberechtigte; Schaltfläche „Geheim abstimmen“ startet eine Abstimmung nach Nr. 1 nur für die Anwesenden mit kurzer Frist (alle stimmen am Handy), Ergebnis landet als Beschluss im Protokoll; Rückfall „Stimmzettel auszählen und erfassen“.
- **Aufwand:** S–M (mit Nr. 1).
- **Umgesetzt (24.11.2026) ✅:** Beschlussfähigkeit (Fraktionssitzung und Arbeitskreis; stimmberechtigt wie bei Antragsabstimmungen), Plausibilität beim
  Beschluss, „Geheim abstimmen“ je TOP (Meinungsbild, geheim, nur Anwesende, Frist aus der GO-Einstellung), Übernahme des Ergebnisses als Beschluss
  (beim Öffnen der Sitzung und per Fristenprüfung, genau einmal; neue Spalten `Umfragen.TopId`, `Beschluesse.UmfrageId`, Zusatzmigration
  `Sitzungsabstimmung`), Rückfall „Stimmzettel“ als Kästchen beim Beschluss. Nicht: Beschluss trotz fehlender Beschlussfähigkeit sperren (nur Anzeige,
  da die GO meist eine Feststellung auf Antrag verlangt ⚑); Wahlgänge in der Sitzung laufen über Wahlen und Ämter (Dienst kann auf Anwesende
  beschränken, eigene Schaltfläche in der Sitzung fehlt noch). Tests: `SitzungAbstimmungTests`, erweiterter `SitzungAblaufTests`.

### 8 Beamer- und Monitoransicht der Sitzung

- **SchoolCraft:** Info-Monitore mit geheimer Adresse (nur Prüfwert gespeichert), sperrbar, Vorschau mit 30-Minuten-Token, Echtzeit-Aktualisierung (`MonitorDienst`, `monitor.js`).
- **Für die Fraktion:** Beamer in der Fraktionssitzung zeigt Tagesordnung, laufenden TOP und das Ergebnis einer Abstimmung, sobald sie beendet ist – ohne dass jemand am Beamer angemeldet sein muss. Ein Monitor im Fraktionsbüro ist eher nicht nötig.
- **Anpassung:** nur Inhalte der laufenden Sitzung, keine nichtöffentlichen Unterlagen, keine Abwesenheitsgründe.
- **Aufwand:** S–M.
- **Umgesetzt (24.11.2026) ✅:** `BeamerZugang` (Prüfwert, gültig bis, gesperrt; Zusatzmigration `BeamerZugaenge`), `BeamerDienst`, Endpunkte
  `/beamer/{token}` (ohne Anmeldung, Rate-Limit, `no-store`, `noindex`) und `/beamer/vorschau/{sitzungId}`. Abweichend von SchoolCraft: statt
  Echtzeit über SignalR/`monitor.js` lädt die schlichte Seite alle zehn Sekunden neu (kein Skript nötig); statt 30-Minuten-Vorschau-Token eine
  Vorschau für Angemeldete; ein Link je Sitzung statt dauerhafter Monitore. Tests: `BeamerTests`, Webtest auf ungültigen Token.

### 9 Übungsinstanzen über die Mandanten (FK-14)

- **SchoolCraft:** Schulungsinstanzen mit eigener Datenbank als Kopie einer Vorlage, Spielzeit (`TimeProvider` mit Zeitraffer), Drehbuch, Werkzeug `instanzen.ps1`.
- **FraktionPerfekt heute:** Die Touren wechseln per Testanmeldung das Konto und lösen echte Aktionen aus; im Konzept steht, dass sie vor dem Echtbetrieb ausgeschaltet oder überarbeitet werden müssen.
- **Vorteil:** Mandanten mit eigener SQLite-Datei sind schon vorbereitet (`Gemeinsam/Mandant.cs`). Eine Übungsinstanz ist ein Mandant mit Demodaten-Kopie unter eigenem Host (z. B. `uebung.…`) – dort dürfen Testanmeldung und Touren aktiv bleiben, die echte Instanz läuft mit Passkeys und ohne Touren. Zurücksetzen = Datei neu kopieren.
- **Spielzeit** (Zeitraffer bis zur Sitzung, Fristablauf der Vertretung) nur bei Bedarf: `TimeProvider` ist schon eingespritzt, Quartz müsste folgen → dann L.
- **Aufwand:** M (ohne Spielzeit).
- **Umgesetzt (24.11.2026) ✅:** `MandantOptionen.Uebung`, `Betriebsart` (Touren, Testanmeldung, Demo-Schutz je Mandant), `Uebungsinstanz.NeuAufbauenAsync`
  (frische Demodaten in einer Temp-Datei, dann SQLite-Online-Sicherung über die Übungsdatenbank – statt einer festen Vorlagedatei, damit Termine
  relativ zu heute stimmen), beim Start für eine leere Übungsdatenbank, „Übung zurücksetzen“ auf `/hilfe` (POST, Rückfrage, danach neu anmelden).
  Dazu: Hintergrundaufträge laufen je Mandant; ein ausdrücklich festgelegter Mandant hat Vorrang vor dem Host der Anfrage. Ohne Spielzeit; ohne eigenes
  `instanzen.ps1` (Einrichtung über appsettings und nginx, siehe `docs/deploy.md`). Tests: `BetriebsartTests`, `UebungsinstanzTests`.

---

## P3 – bei konkretem Bedarf

### 10 30-Minuten-Korrekturregel (FA-3235)
Eigene Einträge 30 Minuten ohne Grund berichtigen, danach mit Grund. Passt zu Protokollnotizen im Sitzungsmodus, Kommentaren, Auslagen vor Freigabe. **Nicht** für Kassenbuchungen nach Freigabe. S–M.

**Umgesetzt (24.11.2026) ✅:** `Korrekturregel` (Application/Gemeinsam) wie in SchoolCraft, als Zurückziehen (Storno, Eintrag bleibt) für Beschlüsse im
Sitzungsmodus (neu: wer/wann erfasst, zurückgezogen mit Grund; aus Beschlusslage, Protokolltext, Beamer und KI-Prüfung ausgeblendet), Kommentare an
Anträgen und Auslagen vor der Entscheidung (Status „zurückgezogen“, Prüfaufgabe schließt sich). Baustein `GrundDialog` fragt nur nach einer Begründung,
wenn sie nötig ist. Protokollnotizen haben schon Fassungen über den Editor und bleiben ohne Regel; Kassenbuchungen bewusst ausgenommen. Zusatzmigration
`Korrekturregel`. Tests: `KorrekturregelTests`.

### 11 Einheitliches Anlegen per Dialog (FA-3229)
FraktionPerfekt hat mit `GrosserDialog` schon große Editor-Dialoge; einheitlich wäre „+ … hinzufügen“ mit OK/Abbrechen und „OK nennt, was fehlt“ für kleine Objekte (Kontakt, Aufgabe, Ressource). Auf Handy-Seiten nicht. M.

**Umgesetzt (24.11.2026) ✅:** Bausteine `Hinzufuegen` (wie SchoolCraft, mit `BegruendeterButton`) und `AnlegenDialog` (auf `GrosserDialog` im kleinen Modus,
OK gesperrt mit Liste der fehlenden Angaben, Abbrechen mit Nachfrage bei Eingaben), `Pflichtangaben`. Umgestellt: Kontakt, Abstimmung, Wahl, Gebiet.
Abweichend: Aufgaben entstehen in FraktionPerfekt aus Protokollen und Vorgängen (kein freies Anlegen), Ressourcen kommen aus den Stammdaten – dort gibt
es keinen Anlegen-Dialog; die Raumbuchung bleibt als Formular auf ihrer Seite. Test `AnlegenDialogTests`.

### 12 Rundschreiben mit Gestaltungen und Archiv als Vorlage (FA-820 ff.)
Einladungen, Fraktionsinfo an Mitglieder und Kampagnenteam mit Anredestilen und Archiv als Vorlage; läuft über den vorhandenen Postausgang. M.

**Umgesetzt (24.11.2026) ✅:** `Rundschreiben` (Domain/Postausgang; Zusatzmigration `Rundschreiben`), `RundschreibenDienst` (Vorstand, Geschäftsführung),
Seite `/rundschreiben` mit großem Dialog, Vorschau, Rückfrage vor dem Senden und Archiv („Als Vorlage“). Gestaltungen Schlicht/Fraktionsinfo/Einladung
als Textrahmen (Mails sind Text), Anredestile persönlich (vorhandene Du/Sie-Einstellung je Person), förmlich, Sammelanrede; je Person eine eigene Mail,
dazu ein knapper Push ohne Inhalt. Nicht übernommen: HTML-Briefpapier und Serienbrief-Platzhalter. Tests: `RundschreibenTests`.

### 13 Podium für Meinungsbilder (FK-15)
Für Klausurtagungen und öffentliche Bürgerdialoge der Fraktion: Podium-Runde mit Spitznamen ohne Konto, Ergebnisse auf den Beamer. Fraktionsinterne Abstimmungen bleiben in FraktionPerfekt (Nr. 1). Stufe 1 nur Link (S).

### 14 Telefonnotiz → Bürgeranliegen
Anruf im Fraktionsbüro als Bürgeranliegen mit Rückrufaufgabe anlegen, fehlertolerante Kontaktauswahl (Nr. 4). Anruferkennung braucht eine Telefonanlage. S.

---

## Bewusst nicht vorgeschlagen

Schulspezifisches (Stundenplan, Klassenbuch, Zeugnisse, Familienmodell, Aufnahmeverfahren), Geräteverwaltung, Schnellauswahl <kbd>F8</kbd> (die Startseite
„Heute“ hat Ein-Tipp-Aktionen, Arbeit läuft überwiegend am Handy), Schlüsselverwaltung, Massenaktionen mit Vier-Augen-Freigabe (Pressefreigabe hat schon
vier Augen), Signatur ausgehender Dokumente (Anträge gehen über das RIS bzw. die Verwaltung), SmartStatistics.

## Rückfluss nach SchoolCraft (Vorschlag, dort noch nichts geändert)

- **FA-3304 geheime Wahl digital:** Verfahren aus Punkt 1 übernehmen – Stimmvermerk (wer) und Stimmzettel (wie) getrennt in einem Speichervorgang,
  Stimmzettel mit UUIDv4 als Schlüssel in einer Tabelle `WITHOUT ROWID` (in SchoolCraft prüfen, ob die Datenbank das gleich abbildet), feste
  Liste der Wahlberechtigten beim Start, kein Zwischenstand, Stimme endgültig mit Rückfrage, Ende automatisch, wenn alle abgestimmt haben,
  Mindestzahl für die Auswertung, Architekturtest „keine Tabelle verbindet Option und Person“, ehrliche Grenze in Hilfe und Spezifikation.
- **Wahlgang als Abstimmungsart** mit Optionen, die die Person tragen, und Mehrheitsregeln als reine Funktion (`Mehrheit`) – passt zur Präsenzwahl
  mit Erfassung in SchoolCraft (Modul 33) als zweiter Weg neben „Stimmzettel auszählen und erfassen“.
- **Sitzungsabstimmung nur für Anwesende** (Punkt 7) für Konferenzen und Schulkonferenz; **Beamer-Link je Sitzung** als leichte Variante der
  Info-Monitore.
- Kleinigkeiten: `Rueckfragen.WennBestaetigtAsync`, `GrundDialog` (Begründung nur, wenn die Korrekturregel sie verlangt), `Personenwahl` als
  Radzen-Auswahl mit `Unscharf` statt eigener Vorschlagsliste, Testmuster „jede folgenreiche Schaltfläche fragt nach“.

## Nächster Schritt

Punkte 1 bis 12 sind umgesetzt (siehe „Umgesetzt“ je Punkt, Drehbuch `docs/abnahme-9.md`). Offen: 13 und 14 (nicht freigegeben). Je Punkt galt: Konzept (`docs/konzept.md`) und Hilfe im selben Commit, Tests (Rechte, Grenzfälle), Browserprüfung inkl. 390 px,
Schemaänderungen nur als zusätzliche Migration. Lokal auf `main` committen, kein Push ohne Freigabe. Was dabei neu entsteht (geheime Abstimmung),
wird anschließend nach SchoolCraft (FA-3304) zurückgespielt.
