# FraktionPerfekt – Konzept für ein Fraktionssystem

Nov 15, 2026 · @Klaus

## Ziel und Rollen

FraktionPerfekt bündelt die Organisation einer kommunalpolitischen Fraktion in einer Web-App: Termine, Räume, Anträge, Vertretungen, Wahlkampf, Protokolle und Dateien an einem Ort statt in Messenger-Gruppen, E-Mail-Verteilern und Tabellen. Es ist ein internes Werkzeug, kein öffentliches Portal, und muss auf dem Smartphone genauso gut funktionieren wie am Rechner.

| Rolle | Wer | Darf typischerweise |
| --- | --- | --- |
| Administration | Fraktionsgeschäftsführung | Mitglieder, Gremien, Ressourcen und Rechte verwalten |
| Vorstand | Fraktionsvorsitz, Stellvertretung | Anträge freigeben, Abstimmungen eröffnen, Terminbesetzung entscheiden |
| Kassier | Kassenverantwortliche Person der Fraktion | Finanzmodul verwalten: Budget, Spenden erfassen, Auslagen prüfen und zur Erstattung freigeben, Kassenbericht erstellen |
| Mitglied | Ratsmitglieder | Alles lesen, Anträge mitschreiben, abstimmen, buchen, sich abwesend melden |
| Sachkundige Bürger:innen | Mitglieder in Ausschüssen ohne Ratsmandat | Inhalte ihrer Ausschüsse, Kalender, Vertretung, Protokolle |
| Helfer:innen | Wahlkampfteam, Ortsverband | Nur Wahlkampf-Modul und ausgewählte Termine |

Rechte hängen an Rolle **und** Gremium: Wer im Bauausschuss sitzt, sieht dessen interne Protokolle, andere Mitglieder je nach Fraktionsregel ebenfalls.

## Funktionsmodule

Zwanzig Module teilen sich ein gemeinsames Fundament aus Mitgliedern, Gremien, Kalender und Benachrichtigungen. Fast jedes Modul erzeugt oder liest Termine, deshalb ist der Kalender das Rückgrat.

### 1. Gemeinsamer Kalender

- Termine der Fraktion, Rats- und Ausschusssitzungen, Arbeitskreise, öffentliche Termine und Wahlkampfaktionen in einer Ansicht, filterbar nach Gremium und Person.
- Zusagen/Absagen pro Termin, Erinnerungen per E-Mail oder Push.
- Abo-Link (iCal/CalDAV) für das eigene Handy, damit niemand einen zweiten Kalender pflegen muss.
- Optional: Import der Sitzungstermine aus dem Ratsinformationssystem (z. B. SessionNet, ALLRIS) per iCal oder OParl-Schnittstelle.

### 2. Ressourcen- und Raumbuchung

- Buchbare Ressourcen: Fraktionsräume, Beamer, Infostand, Lastenrad, Dienstwagen, Pavillon.
- Belegungsplan mit Konfliktprüfung; eine Buchung kann direkt an einen Kalendertermin gehängt werden.
- Optional Freigabe durch die Geschäftsführung für bestimmte Ressourcen.

### 3. Anträge gemeinsam entwickeln

- Ein Antrag durchläuft Status: Idee, Entwurf, in Abstimmung, beschlossen (Fraktion), eingereicht, im Gremium beraten, erledigt.
- Gemeinsames Bearbeiten im Browser mit Versionsverlauf und Kommentaren am Text; Änderungsvorschläge lassen sich annehmen oder ablehnen.
- Abstimmung in der Fraktion: Ja/Nein/Enthaltung, mit Frist und Quorum; Ergebnis wird am Antrag festgehalten.
- Export als PDF/DOCX im Fraktions-Layout zum Einreichen beim Rat.
- Verknüpfung mit Gremium und Sitzung, in der der Antrag beraten wird.

### 4. Abwesenheiten und Vertretung in Ausschusssitzungen

- Mitglieder tragen Abwesenheiten ein (Urlaub, Krankheit, beruflich).
- Das System erkennt Sitzungen, in denen ein ordentliches Mitglied fehlt, und benachrichtigt die hinterlegten Stellvertreter:innen in ihrer Reihenfolge.
- Wer zusagt, ist eingetragen; bleibt die Stelle offen, eskaliert die Anfrage an Vorstand oder Geschäftsführung.
- Übersicht: welche Sitzung ist voll besetzt, wo fehlt noch jemand.

### 5. Terminbesetzung für öffentliche Termine

- Eine Einladung (z. B. Vereinsjubiläum, Bürgerversammlung) wird mit der Zahl N der gewünschten Vertreter:innen angelegt.
- Mitglieder melden Interesse; der Vorstand bestätigt oder es gilt „wer zuerst kommt“.
- Nach der Bestätigung landet der Termin im Kalender der Teilnehmenden; offene Plätze werden sichtbar markiert.
- Optional: kurzer Bericht nach dem Termin.

### 6. Straßenwahlkampf

- Karte (OpenStreetMap) mit Plakatstandorten: wer hat wann wo welches Plakat gehängt, Status hängt/beschädigt/abgehängt, Foto vom Handy.
- Straßen oder Gebiete für Haustür- und Flyer-Aktionen zuteilen und als erledigt markieren; die Karte färbt sich mit dem Fortschritt.
- Infostand-Schichten mit Eintragung.
- Nach der Wahl: Liste aller Plakate zum Abhängen, damit die Frist der Kommune eingehalten wird.

### 7. Eigene Protokolle von Ausschusssitzungen

- Vorlage pro Gremium mit Tagesordnung (aus dem Ratsinformationssystem übernehmbar), Teilnehmenden, Notizen pro TOP, Abstimmungsverhalten.
- Aufgaben aus dem Protokoll („Klären bis nächste Sitzung“) werden Personen zugewiesen und verfolgt.
- Volltextsuche über alle Protokolle; Verknüpfung mit Anträgen.

### 8. Filesharing

- Ordner pro Gremium, Arbeitskreis und Projekt mit Rechten aus den Gremien.
- Dateien an Anträge, Protokolle und Termine anhängen.
- Empfehlung: nicht selbst bauen, sondern Nextcloud anbinden (siehe Architektur).

### 9. Fraktionssitzungen: Einladung, Durchführung, Protokoll

- Einladung mit Tagesordnung aus einer Vorlage (feste TOPs wie „Bericht Vorstand“, „Vorbereitung Ratssitzung“, „Verschiedenes“); Mitglieder können bis zu einer Frist TOPs anmelden.
- Versand per E-Mail und im Kalender, mit Zu-/Absage; Unterlagen und anstehende Anträge hängen automatisch an.
- Sitzungsmodus für die Protokollführung: Anwesenheit abhaken, TOP für TOP mitschreiben, Beschlüsse und Abstimmungsergebnisse als eigene Einträge erfassen, Aufgaben direkt Personen zuweisen.
- Vorgegeben, aber flexibel: Die Vorlage gibt Struktur und Pflichtfelder vor, während der Sitzung lassen sich TOPs umsortieren, einfügen, vertagen oder zusammenlegen; Vertagtes wandert in die nächste Einladung.
- Nach der Sitzung: Protokoll als Entwurf, Freigabe durch Sitzungsleitung, Versand und Ablage; Beschlussliste und offene Aufgaben sind fraktionsweit durchsuchbar.
- Dieselbe Protokoll-Engine dient auch den eigenen Ausschussprotokollen (Modul 7), nur mit anderer Vorlage.

### 10 bis 19. Zusatzmodule

| Nr. | Modul | Kernfunktionen | Stufe |
| --- | --- | --- | --- |
| 10 | RIS-Anbindung (OParl) | Gremien, Sitzungen, Tagesordnungen, Vorlagen und Beschlüsse aus dem Ratsinformationssystem regelmäßig abgleichen; Vorlagen direkt an Protokoll und Antrag verknüpfen | 1 |
| 11 | Mandatsübersicht | Wer sitzt in welchem Gremium, ordentlich oder Stellvertretung; **wird aus dem RIS synchronisiert** und nur um fraktionsinterne Angaben ergänzt (z. B. Rang der Vertretung, Sprecherrolle) | 1 |
| 12 | Aufgaben und Zuständigkeiten | Aufgaben aus Protokollen, Anträgen und Anliegen; Zuständige, Frist, Erinnerung; Themenpaten pro Politikfeld | 1 |
| 13 | Abstimmungen, Umfragen, Terminfindung | Schnelle Meinungsbilder, Umfragen und Terminfindung (wie Doodle) innerhalb der Fraktion | 2 |
| 14 | Anfragen an die Verwaltung | Gestellte Anfragen mit Frist verfolgen, Antwort ablegen, Erinnerung bei Überschreitung | 2 |
| 15 | Beschlusslage-Archiv | Was hat Rat oder Ausschuss wann beschlossen, was hat die Fraktion beschlossen; durchsuchbar, aus RIS und eigenen Protokollen gespeist | 2 |
| 16 | Kontakte | Vereine, Verwaltung, Presse, Initiativen; Ansprechpartner pro Thema, letzte Kontakte | 2 |
| 17 | Bürgeranliegen | Eingang per Formular oder E-Mail, Zuweisung an zuständiges Mitglied, Status bis zur Antwort | 3 |
| 18 | Pressearbeit | Redaktionskalender, Entwürfe von Pressemitteilungen und Social-Media-Beiträgen mit Freigabe, Pressespiegel | 3 |
| 19 | Finanzen | Budget der Fraktion, Spenden, Auslagen mit Belegfoto vom Handy und Freigabe zur Erstattung durch den Kassier; Kassenbericht | 3 |

**RIS in Greven (Sternberg SD.NET):** SD.NET kann eine OParl-Schnittstelle bereitstellen. Ob sie in Greven freigeschaltet ist, ist noch zu prüfen. OParl liefert nur öffentliche Inhalte (Gremien, Personen, Mitgliedschaften, Sitzungen, Tagesordnungen, Vorlagen, Dateien), und zwar nur lesend. Nichtöffentliche Unterlagen gibt es nur über den persönlichen Mandatsträger-Zugang; die App verlinkt dorthin, statt sie zu kopieren. Ist OParl nicht aktiv, sind ein iCal-Export der Sitzungstermine oder eine Anfrage der Fraktion an die Verwaltung, die Schnittstelle freizuschalten, die Wege. Bis dahin werden Mandate einmalig importiert und von Hand gepflegt. Das RIS (greven.ratsinfomanagement.net) steht hinter einem Bot-Schutz, der automatisierte Zugriffe blockt (Test am 15.11.2026: 403); für den Abgleich muss die IP des Servers dort freigegeben werden.

Für Kontakte und Bürgeranliegen gelten eigene Datenschutzhinweise und Löschfristen, da hier Daten von Personen außerhalb der Fraktion liegen.

### 20. Wissensbereich (Wiki) mit KI-Schnittstelle

- Wiki im Stil von Confluence für die ganze Fraktion, nicht nur für Neumitglieder: Bereiche für Abläufe, Themen und Politikfelder, Gremien, Positionen und Hintergründe.
- Seiten mit Versionsverlauf, Vorlagen, Verlinkungen untereinander und zu Anträgen, Protokollen und Beschlüssen; Rechte nach Gremium; Volltextsuche. Derselbe Editor wie für Anträge.
- **KI-Schnittstelle**: eine API (zusätzlich als MCP-Server), über die ein Sprachmodell Seiten lesen, durchsuchen und Änderungen vorschlagen kann. Auf Abruf oder in festen Abständen prüft die KI die Wissensfragmente:
  - veraltete Angaben und Widersprüche finden, etwa zu neueren Beschlüssen
  - Dubletten zusammenführen, verwaiste Seiten einordnen, Struktur und Verlinkung vorschlagen
  - Lücken aus Protokollen und Beschlüssen ergänzen
  - Fragen beantworten, mit Verweis auf die Quellseiten
- **Die KI ändert nichts direkt**: Jede Umstrukturierung kommt als Änderungsvorschlag mit Vorher-nachher-Ansicht, den eine verantwortliche Person annimmt oder ablehnt. Alles wird protokolliert.
- Die KI arbeitet mit einem eigenen Konto und sieht nur freigegebene Bereiche; vertrauliche Bereiche (z. B. nichtöffentliche Sitzungen) bleiben ausgeschlossen. Anbieter mit Auftragsverarbeitungsvertrag und ohne Training auf den Daten, alternativ ein selbst gehostetes Modell.
- Stufe 2, die KI-Schnittstelle als eigener Schritt danach.

## Bedienung und Integration ins Handy und den Rechner

Die App soll sich wie eine native App anfühlen und ohne Schulung nutzbar sein. Dazu gehören fünf feste Vorgaben.

| Anforderung | Lösung | Hinweis |
| --- | --- | --- |
| Anmeldung wie am Handy gewohnt | Passkeys (WebAuthn): Anmeldung per Face ID, Touch ID oder Fingerabdruck, synchronisiert über iCloud-Schlüsselbund bzw. Google-Passwortmanager; danach lange Sitzung mit biometrischer Entsperrung | Kein Passwort nötig; Keycloak unterstützt Passkeys |
| Wirkt wie eine native App | Dieselbe Web-App in einer dünnen nativen Hülle (z. B. Capacitor) für App Store und Google Play; zusätzlich als PWA im Browser nutzbar | Eine Codebasis; App-Icon, Vollbild, Teilen-Menü, Kamera für Plakatfotos |
| Kalender in der nativen Kalender-App | Persönlicher Abo-Link (webcal://) mit einem Tipp auf „Zum Kalender hinzufügen“; für sofortige Änderungen zusätzlich CalDAV | Google Kalender aktualisiert Abo-Links nur alle paar Stunden; CalDAV ist sofort aktuell (iOS eingebaut, Android über DAVx⁵) |
| Benachrichtigungen als native Mitteilung | Native Push über Apple (APNs) und Google (FCM), mit Aktionsknopf direkt in der Mitteilung („Ich vertrete“) | Inhalte knapp halten, keine vertraulichen Details auf dem Sperrbildschirm |
| Dateien im Explorer und Finder | Nextcloud-Desktop-Client mit virtuellen Dateien: erscheint unter Windows im Explorer, unter macOS im Finder; am Handy in der Dateien-App | Spricht klar für Nextcloud statt eigener Dateiablage |

### Einfachheit und vorgeschlagene Folgeschritte

- **Startseite „Heute“** zeigt nur, was jetzt zu tun ist: nächste Termine, offene Anfragen und Abstimmungen, jeweils mit einer Ein-Tipp-Antwort.
- **Schnellaktionen** für die Standardprozesse, erreichbar über einen Plus-Knopf und per langem Druck auf das App-Icon: Abwesenheit melden, Raum buchen, Antrag starten, Fraktionssitzung einladen, Plakat erfassen.
- **Folgeschritte schlägt das System nach jedem Abschluss vor**, als Karte mit einem Knopf:

| Wenn … | … schlägt das System vor |
| --- | --- |
| eine Abwesenheit eingetragen ist | „In 2 Sitzungen fehlt dann jemand: Vertretung anfragen?“ |
| eine Einladung zu einem öffentlichen Termin angelegt ist | Besetzungsanfrage mit N Plätzen an alle senden |
| ein Antrag in der Fraktion beschlossen ist | PDF erzeugen, einreichen, der passenden Ratssitzung zuordnen |
| eine Fraktionssitzung terminiert ist | Raum buchen, Einladung mit Vorlage-TOPs versenden |
| ein Protokoll freigegeben ist | Aufgaben verteilen, vertagte TOPs in die nächste Einladung übernehmen |
| ein öffentlicher Termin vorbei ist | Kurzbericht anlegen |

Die Vorschläge sind feste Regeln, keine KI; das macht sie vorhersehbar. Eine KI-Unterstützung (z. B. Antragsentwurf aus Stichpunkten) kann später dazukommen.

## Datenmodell

Rund ein Dutzend Kernentitäten tragen alle Module; „Gremium“ und „Termin“ sind die Knoten, an denen fast alles hängt.

| Entität | Wichtige Felder | Beziehungen |
| --- | --- | --- |
| Person | Name, Kontakt, Rolle, aktiv | ist Mitglied in Gremien, hat Abwesenheiten |
| Gremium | Name, Art (Rat, Ausschuss, Arbeitskreis) | hat Mitgliedschaften, Sitzungen, Ordner |
| Mitgliedschaft | Funktion (ordentlich, Stellvertretung), Rang der Vertretung | verbindet Person und Gremium |
| Termin | Art, Beginn, Ende, Ort, Sichtbarkeit | gehört optional zu Gremium; hat Teilnahmen, Buchungen |
| Teilnahme | Status (eingeladen, zugesagt, abgesagt, vertritt) | verbindet Person und Termin |
| Besetzungsanfrage | benötigte Anzahl N, Frist, Modus | gehört zu Termin; hat Meldungen |
| Abwesenheit | Von, Bis, Grund (optional, privat) | gehört zu Person; löst Vertretungsanfragen aus |
| Ressource | Name, Typ, Freigabe nötig | hat Buchungen |
| Buchung | Von, Bis, Zweck | verbindet Ressource, Person, optional Termin |
| Antrag | Titel, Status, aktuelle Fassung | hat Fassungen, Kommentare, Abstimmungen; Ziel-Gremium |
| Abstimmung | Frage, Frist, Quorum, Ergebnis | hat Stimmen; gehört zu Antrag oder frei |
| Protokoll | Sitzung, Vorlage, TOPs, Anwesenheit, Notizen | gehört zu Termin; hat Beschlüsse und Aufgaben |
| Plakat | Position, Motiv, Status, Foto, gehängt von | gehört zu Wahlkampagne |
| Gebiet / Straße | Geometrie, Status, zugeteilt an | gehört zu Wahlkampagne |
| Datei | Pfad in Nextcloud, Freigabe | an Antrag, Protokoll, Termin oder Ordner |

Die Zusatzmodule bringen weitere Entitäten mit: Aufgabe, Anfrage, Anliegen, Kontakt, Beschluss, Pressebeitrag, Beleg und Wikiseite (mit Versionen und KI-Änderungsvorschlägen). Gremien, Sitzungen, Vorlagen und Mandate kommen aus dem RIS und werden dort gepflegt, im System nur gespiegelt und ergänzt.

Abwesenheitsgründe sind Gesundheitsdaten, wenn „krank“ drinsteht: Das Feld bleibt optional und ist nur für die Person selbst und die Geschäftsführung sichtbar.

## Architektur und Technik

Empfehlung: eine eigene Web-App für die fraktionsspezifische Logik, Dateien und Dokumentbearbeitung aber über Nextcloud und einen Office-Server. So bauen wir nur, was es nicht schon gut gibt.

**Grundlage ist die Architektur von VibeVHS** (.NET, Blazor, SQLite), damit Bausteine, Muster und Erfahrungen direkt übernommen werden können. Übernommene Leitlinien:

- **Gesperrt heißt erklärt:** Nicht klickbare Knöpfe zeigen per Tooltip den Grund (z. B. „Abstimmung erst nach Ende der Kommentarfrist“).
- **Nächste sinnvolle Schritte:** Baustein `NaechsteSchritte` nach jeder Aktion; trägt die Folgeschritt-Regeln aus dem Abschnitt Bedienung.
- **Nichts geht unbemerkt raus:** Jede Mail, jeder Push und jeder Schnittstellenaufruf läuft über den Postausgang (Outbox), gespeichert mit der fachlichen Änderung, per Hintergrundjob zugestellt, protokolliert.
- **Nichts wird überschrieben:** Mandate, Mitgliedschaften und Rollen sind bitemporal (gültig von/bis, erfasst/ersetzt); Anträge und Protokolle haben Fassungen.
- **Protokoll mit Pseudonymen,** Aufgabenkörbe je Rolle mit Übernehmen/Zuweisen/Eskalation, Abwesenheit mit Vertretung (fachlich erweitert um Gremiensitzungen).
- **Kalender-Abo mit widerrufbarem Token** (nur als Hash gespeichert), Anmeldelink per Mail, mobile Seiten statisch gerendert.
- **Betrieb:** tägliche SQLite-Online-Sicherung mit Prüfsumme und automatischem Wiederherstellungstest, OWASP-Checkliste, Rate-Limits, Sicherheitsheader.

| Baustein | Vorschlag (wie VibeVHS, wo möglich) | Begründung |
| --- | --- | --- |
| Aufbau | .NET 11, Projekte Domain / Application / Infrastructure / Adapters.Mocks / Web, Tests mit xUnit v3 + Shouldly | Gleiche Struktur wie VibeVHS, Fachkern ohne Abhängigkeiten |
| Oberfläche | Blazor Web App mit Radzen; Verwaltungsseiten interaktiv (Server), Handy-Seiten statisch gerendert mit wenig JavaScript | Schnell auch bei schlechtem Netz |
| App-Gefühl | PWA (Manifest, Service Worker, App-Shortcuts); für App Store und Google Play eine dünne Capacitor-Hülle um dieselbe Web-App | Eine Codebasis; native Mitteilungen und Kamera |
| Datenbank | EF Core + SQLite (WAL); Plakatstandorte als Koordinaten, Straßen als GeoJSON | Für eine Fraktion mehr als ausreichend; später Litestream |
| Anmeldung | ASP.NET Core Identity mit **Passkeys** (Face ID, Fingerabdruck), Anmeldelink per Mail als Rückfall; in der Entwicklung Testanmeldung | Passkeys sind seit .NET 10 eingebaut |
| Hintergrundjobs | Quartz.NET: Postausgang, RIS-Abgleich, Erinnerungen, Fristen, Eskalation | Wie VibeVHS |
| Benachrichtigung | Web Push (VAPID) für die PWA, APNs/FCM für die App-Hülle, E-Mail als Rückfall – alles über den Postausgang | Vertretungsanfragen müssen schnell ankommen |
| Kalender | ICS-Abo pro Person (webcal, widerrufbares Token); CalDAV später über Nextcloud | Sofort in iOS- und Android-Kalendern |
| Dateien | Nextcloud per WebDAV; Ordner pro Gremium; Desktop-Client für Explorer/Finder | Nicht selbst bauen |
| Antragstexte | Editor in der App mit Fassungen und Kommentaren | Status und Abstimmung direkt am Antrag |
| Karten | Leaflet oder MapLibre mit OpenStreetMap | Kostenlos, ohne Tracking |
| RIS | OParl-Adapter hinter einem Port, vorerst Mock mit Grevener Gremien | Echte Anbindung, sobald OParl geklärt ist |
| Betrieb | Zunächst nativ unter Windows, später Linux-Server in Deutschland (systemd, nginx) | Wie VibeVHS |

Alternative ohne Eigenentwicklung: Nextcloud mit Kalender, Deck, Forms und Collabora deckt Kalender, Dateien und Texte ab, aber nicht Vertretungslogik, Terminbesetzung, Abstimmungen mit Quorum oder die Plakatkarte. Diese vier Punkte sind der eigentliche Mehrwert einer eigenen App.

## Datenschutz, Sicherheit, Hosting

Die Fraktion ist Verantwortliche im Sinne der DSGVO; das System verarbeitet personenbezogene Daten und teils politische Meinungen (Art. 9 DSGVO). Daraus folgen wenige, aber feste Vorgaben.

- **Hosting in Deutschland oder der EU** bei einem Anbieter mit Auftragsverarbeitungsvertrag (z. B. Hetzner, IONOS, netcup) oder auf Infrastruktur der Partei bzw. Kommune.
- **Verzeichnis der Verarbeitungstätigkeiten** und Datenschutzhinweis für Mitglieder und Helfer:innen; Helfer:innen sehen nur Wahlkampfdaten.
- **Zwei-Faktor-Anmeldung** für alle mit Zugriff auf interne Protokolle und Antragsentwürfe; Rechte nach Gremium.
- **Verschlüsselung** im Transport (TLS) und für Backups; tägliche Backups an einem zweiten Ort.
- **Löschkonzept**: Abwesenheiten nach z. B. 12 Monaten löschen, Plakat- und Gebietsdaten nach Abschluss des Wahlkampfs, ausgeschiedene Mitglieder deaktivieren und nach Frist entfernen.
- **Fotos von Plakaten** ohne erkennbare Personen; Standortdaten nur des Plakats, nicht der hängenden Person.
- **Nichtöffentliche Sitzungsunterlagen** der Kommune unterliegen der Verschwiegenheit: Ablage nur in Ordnern mit entsprechend engem Zugriff.

Der Betrieb (Updates, Backups, Monitoring) braucht eine verantwortliche Person oder einen Dienstleister; für einen Server mit App und Nextcloud sind etwa 20 bis 40 Euro im Monat realistisch (Schätzung, ohne Arbeitszeit).

## MVP und Ausbaustufen

Das MVP deckt die Abläufe ab, die jede Woche anfallen: Sitzungen, Termine und Vertretungen. Wer das System wöchentlich nutzt, nutzt es später auch für Anträge und Wahlkampf.

[embedded content: Ausbaustufen · 3 Stufen, je 5 Bausteine]

Die Reihenfolge von Stufe 2 und 3 hängt vom nächsten Wahltermin ab: Steht eine Wahl im kommenden Jahr an, ziehen wir den Wahlkampf vor. Die Zusatzmodule 10 bis 20 sind in ihrer Tabelle einer Stufe zugeordnet; RIS-Anbindung, Mandatsübersicht und Aufgaben gehören ins MVP.

## Offene Fragen

Diese Punkte entscheiden über Technik und Reihenfolge; ohne Antwort gelten die Annahmen in Klammern.

1. Wie groß ist die Fraktion, mit sachkundigen Bürger:innen und Helfer:innen? (Annahme: 10 bis 40 Personen)
2. Welche Werkzeuge laufen heute schon, etwa Nextcloud, Messenger-Gruppen, ein Partei-Tool? (Annahme: keine Nextcloud vorhanden)
3. Ist die OParl-Schnittstelle des Grevener SD.NET-RIS freigeschaltet, und wie lautet die Adresse des RIS? (offen)
4. Wann ist die nächste Kommunalwahl? Das bestimmt, ob der Wahlkampf vorgezogen wird. (offen)
5. Wo soll gehostet werden: eigener Server, Partei-Infrastruktur oder ein Dienstleister? Wer kümmert sich um den Betrieb? (Annahme: gemieteter Server in Deutschland)
6. Wo finde ich VibeVHS (GitHub-Repository), dessen Architektur übernommen werden soll? (offen)
7. Soll das System später auch anderen Fraktionen angeboten werden (mandantenfähig, Open Source)? (Annahme: zunächst nur eine Fraktion, aber sauber trennbar)
8. Gibt es ein GitHub-Repository für den Code, oder lege ich die Struktur vor? (offen)
