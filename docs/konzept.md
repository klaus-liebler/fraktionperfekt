# FraktionPerfekt – Konzept für ein Fraktionssystem

> **Führende Version.** Dieses Dokument im Repository ist maßgeblich; das frühere Claude-Dokument (Stand 15.11.2026) ist nur noch
> Archiv. Der ursprüngliche Export steht unverändert in der Git-Historie (Commit „Konzept ins Repository übernommen“).
>
> **Stand: 17.11.2026** · ursprünglich 15.11.2026 · @Klaus
>
> Kennzeichnung: ✅ umgesetzt · ◐ teilweise bzw. als Mock/Vorbereitung · ⏳ noch nicht umgesetzt

## Ziel und Rollen

FraktionPerfekt bündelt die Organisation einer kommunalpolitischen Fraktion in einer Web-App: Termine, Räume, Anträge, Vertretungen, Kampagnen, Protokolle und Dateien an einem Ort statt in Messenger-Gruppen, E-Mail-Verteilern und Tabellen. Es ist ein internes Werkzeug, kein öffentliches Portal, und muss auf dem Smartphone genauso gut funktionieren wie am Rechner.

| Rolle | Wer | Darf typischerweise |
| --- | --- | --- |
| Administration | Fraktionsgeschäftsführung | Mitglieder, Gremien, Ressourcen und Rechte verwalten |
| Vorstand | Fraktionsvorsitz, Stellvertretung | Anträge freigeben, Abstimmungen eröffnen, Terminbesetzung entscheiden, Kampagnen steuern |
| Schatzmeister/Kassier | Kassenverantwortliche Person der Fraktion | Finanzmodul verwalten: Budget, Spenden erfassen, Auslagen prüfen und zur Erstattung freigeben, Kassenbericht erstellen |
| Mitglied | Ratsmitglieder | Alles lesen, Anträge mitschreiben, abstimmen, buchen, sich abwesend melden |
| Sachkundige Bürger:innen | Mitglieder in Ausschüssen ohne Ratsmandat | Inhalte ihrer Ausschüsse, Kalender, Vertretung, Protokolle |
| Kampagnenteam | Ortsverband, Unterstützer:innen (technischer Rollenname `Helferin`) | Kampagnen (Plakate, Gebiete, Termine, ToDos, Tickets), öffentliche Termine und Kampagnentermine; keine Anträge, Sitzungen, Abstimmungen |

Rechte hängen an Rolle **und** Gremium: Wer im Bauausschuss sitzt, sieht dessen interne Protokolle, andere Mitglieder je nach Fraktionsregel ebenfalls. Rollen sind bitemporal (gültig von/bis); wer mehrere Rollen hat, wählt nach der Anmeldung die aktive Rolle.

## Funktionsmodule

Zwanzig Module teilen sich ein gemeinsames Fundament aus Mitgliedern, Gremien, Kalender und Benachrichtigungen. Fast jedes Modul erzeugt oder liest Termine, deshalb ist der Kalender das Rückgrat.

### 1. Gemeinsamer Kalender ✅

- Termine der Fraktion, Rats- und Ausschusssitzungen, Arbeitskreise, öffentliche Termine und Kampagnentermine in einer Ansicht, filterbar nach Gremium und Person.
- Zusagen/Absagen pro Termin, Erinnerungen per E-Mail oder Push (über den Postausgang).
- Abo-Link (webcal/ICS) für das eigene Handy mit widerrufbarem Token, einzelne Termine als ICS. ⏳ CalDAV (über Nextcloud).
- ◐ Import der Sitzungstermine aus dem Ratsinformationssystem: OParl-Adapter vorhanden, im Betrieb noch Mock (siehe Modul 10).

### 2. Ressourcen- und Raumbuchung ✅

- Buchbare Ressourcen: Fraktionsräume, Beamer, Infostand, Lastenrad, Dienstwagen, Pavillon.
- Belegungsplan mit Konfliktprüfung; eine Buchung kann direkt an einen Kalendertermin gehängt werden.
- Freigabe durch die Geschäftsführung für bestimmte Ressourcen.

### 3. Anträge gemeinsam entwickeln ✅

- Ein Antrag durchläuft Status: Idee, Entwurf, in Abstimmung, beschlossen (Fraktion), eingereicht, im Gremium beraten, erledigt.
- Bearbeiten im Browser mit Fassungen (neue Fassung im großen Dialog), Vergleich, Kommentaren und Änderungsvorschlägen zum Annehmen oder Ablehnen. ⏳ Gleichzeitiges Bearbeiten in Echtzeit.
- Abstimmung in der Fraktion: Ja/Nein/Enthaltung, mit Frist und Quorum; Ergebnis wird am Antrag festgehalten.
- Export als PDF im Fraktions-Layout (Briefkopf je Mandant). ⏳ DOCX.
- Verknüpfung mit Gremium und Sitzung, in der der Antrag beraten wird.

### 4. Abwesenheiten und Vertretung in Ausschusssitzungen ✅

- Mitglieder tragen Abwesenheiten ein (Urlaub, Krankheit, beruflich).
- Das System erkennt Sitzungen, in denen ein ordentliches Mitglied fehlt, und benachrichtigt die hinterlegten Stellvertreter:innen in ihrer Reihenfolge.
- Wer zusagt, ist eingetragen; bleibt die Stelle offen, eskaliert die Anfrage an Vorstand oder Geschäftsführung.
- Übersicht: welche Sitzung ist voll besetzt, wo fehlt noch jemand.

### 5. Terminbesetzung für öffentliche Termine ✅

- Eine Einladung (z. B. Vereinsjubiläum, Bürgerversammlung) wird mit der Zahl N der gewünschten Vertreter:innen angelegt.
- Mitglieder melden Interesse; der Vorstand bestätigt oder es gilt „wer zuerst kommt“, mit Warteliste.
- Nach der Bestätigung landet der Termin im Kalender der Teilnehmenden; offene Plätze werden sichtbar markiert.
- Kurzbericht nach dem Termin.

### 6. Kampagnen ✅ (früher „Straßenwahlkampf“)

Statt eines festen Wahlkampf-Moduls gibt es **Kampagnen** (Wahlkampf, Haustüraktion, Themenkampagne), beliebig viele, jeweils mit
Zeitraum, Beschreibung, Status (Planung, aktiv, abgeschlossen) und optional einem Wahltag. Je Kampagne:

- **Plakate:** Karte (OpenStreetMap) mit Plakatstandorten, Erfassung am Handy mit GPS und Foto, Status geplant/hängt/beschädigt/abgehängt. „Beschädigt“ legt automatisch ein Ticket an.
- **Abhängliste** nach dem Wahltag der Kampagne, am Handy nach Entfernung sortiert; Aufgabe „Plakate abhängen“ für den Vorstand.
- **Gebiete** für Plakatieren, Haustür- und Flyer-Aktionen zuteilen (mit Push) und als erledigt abhaken. ⏳ Gebiete als Geometrie auf der Karte einfärben.
- **Termine:** Ein Kampagnentermin ist ein normaler Kalendertermin mit Kampagnenbezug und optional begrenzten Plätzen (vereinheitlicht, die früheren Infostand-Schichten entfallen). **Einbuchen = Zusage**; der Termin steht dann im eigenen Kalender und im Abo; ist er voll, ist „Einbuchen“ gesperrt und erklärt.
- **ToDo-Liste je Termin** (zuständig, erledigt mit wer/wann). **ToDo-Vorlagen** (z. B. „Infostand“) als Kopiervorlagen: beim Anlegen eines Termins „ToDos aus dieser Vorlage erstellen“ oder später am Termin.
- **Tickets** je Kampagne: Nummer, Beschreibung mit Bildern, Status (neu, in Arbeit, wartet, erledigt), Priorität, Zuständigkeit mit Benachrichtigung, Fälligkeit, Bezug auf Gebiet, Plakat oder Termin, Kommentare; Liste und Kanban, Filter „meine“.
- Übersicht je Kampagne mit Kennzahlen und nächsten sinnvollen Schritten. Details: `docs/kampagnen.md`.

### 7. Eigene Protokolle von Ausschusssitzungen ✅

- Vorlage pro Gremium mit Tagesordnung, Teilnehmenden, Notizen pro TOP, Abstimmungsverhalten. ◐ Tagesordnung aus dem RIS (Adapter vorhanden, Betrieb noch Mock).
- Aufgaben aus dem Protokoll („Klären bis nächste Sitzung“) werden Personen zugewiesen und verfolgt.
- Volltextsuche über alle Protokolle; Verknüpfung mit Anträgen.

### 8. Filesharing ◐

- Ordner pro Gremium, Arbeitskreis, Anträge, Protokolle und Kampagnen mit Rechten aus den Gremien; Dateien an Anträge, Protokolle und Termine anhängen.
- Ablage lokal oder per WebDAV (Adapter für Nextcloud vorhanden). ⏳ Echte Nextcloud-Anbindung im Betrieb (es gibt noch keine Nextcloud).

### 9. Fraktionssitzungen: Einladung, Durchführung, Protokoll ✅

- Einladung mit Tagesordnung aus einer Vorlage (feste TOPs wie „Bericht Vorstand“, „Vorbereitung Ratssitzung“, „Verschiedenes“); Mitglieder können bis zu einer Frist TOPs anmelden.
- Versand per E-Mail und im Kalender, mit Zu-/Absage; Unterlagen und anstehende Anträge hängen automatisch an.
- Sitzungsmodus für die Protokollführung: Anwesenheit abhaken, TOP für TOP mitschreiben, Beschlüsse und Abstimmungsergebnisse als eigene Einträge erfassen, Aufgaben direkt Personen zuweisen.
- Vorgegeben, aber flexibel: TOPs umsortieren, einfügen, vertagen oder zusammenlegen; Vertagtes wandert in die nächste Einladung.
- Nach der Sitzung: Protokoll als Entwurf, Freigabe durch Sitzungsleitung, Versand und Ablage; Beschlussliste und offene Aufgaben sind fraktionsweit durchsuchbar.
- Dieselbe Protokoll-Engine dient auch den eigenen Ausschussprotokollen (Modul 7), nur mit anderer Vorlage.

### 10 bis 19. Zusatzmodule

| Nr. | Modul | Kernfunktionen | Stufe | Stand |
| --- | --- | --- | --- | --- |
| 10 | RIS-Anbindung (OParl) | Gremien, Sitzungen, Tagesordnungen, Vorlagen und Beschlüsse aus dem Ratsinformationssystem regelmäßig abgleichen; Vorlagen direkt an Protokoll und Antrag verknüpfen | 1 | ◐ OParl-Adapter und nächtlicher Abgleich vorhanden, im Betrieb Mock mit Grevener Gremien |
| 11 | Mandatsübersicht | Wer sitzt in welchem Gremium, ordentlich oder Stellvertretung; **wird aus dem RIS synchronisiert** und nur um fraktionsinterne Angaben ergänzt (z. B. Rang der Vertretung, Sprecherrolle) | 1 | ✅ bitemporal; Abgleich wie Modul 10 |
| 12 | Aufgaben und Zuständigkeiten | Aufgaben aus Protokollen, Anträgen und Anliegen; Zuständige, Frist, Erinnerung; Themenpaten pro Politikfeld | 1 | ✅ |
| 13 | Abstimmungen, Umfragen, Terminfindung | Schnelle Meinungsbilder, Umfragen und Terminfindung (wie Doodle) innerhalb der Fraktion | 2 | ✅ |
| 14 | Anfragen an die Verwaltung | Gestellte Anfragen mit Frist verfolgen, Antwort ablegen, Erinnerung bei Überschreitung | 2 | ✅ |
| 15 | Beschlusslage-Archiv | Was hat Rat oder Ausschuss wann beschlossen, was hat die Fraktion beschlossen; durchsuchbar, aus RIS und eigenen Protokollen gespeist | 2 | ✅ (RIS-Teil wie Modul 10) |
| 16 | Kontakte | Vereine, Verwaltung, Presse, Initiativen; Ansprechpartner pro Thema, letzte Kontakte | 2 | ✅ |
| 17 | Bürgeranliegen | Eingang per Formular oder E-Mail, Zuweisung an zuständiges Mitglied, Status bis zur Antwort | 3 | ✅ (Löschfrist für Name/Kontakt) |
| 18 | Pressearbeit | Redaktionskalender, Entwürfe von Pressemitteilungen und Social-Media-Beiträgen mit Freigabe, Pressespiegel | 3 | ✅ (vier Augen) |
| 19 | Finanzen | Budget der Fraktion, Spenden, Auslagen mit Belegfoto vom Handy und Freigabe zur Erstattung durch den Schatzmeister/Kassier; Kassenbericht | 3 | ✅ Fraktions- und Wahlkampfkasse getrennt |

**RIS in Greven (Sternberg SD.NET):** Adresse `greven.ratsinfomanagement.net`. SD.NET kann eine OParl-Schnittstelle bereitstellen; ob sie in Greven freigeschaltet ist, ist weiter ungeklärt. OParl liefert nur öffentliche Inhalte (Gremien, Personen, Mitgliedschaften, Sitzungen, Tagesordnungen, Vorlagen, Dateien), und zwar nur lesend. Nichtöffentliche Unterlagen gibt es nur über den persönlichen Mandatsträger-Zugang; die App verlinkt dorthin, statt sie zu kopieren. Ist OParl nicht aktiv, sind ein iCal-Export der Sitzungstermine oder eine Anfrage der Fraktion an die Verwaltung, die Schnittstelle freizuschalten, die Wege. Bis dahin werden Mandate einmalig importiert und von Hand gepflegt. Das RIS steht hinter einem Bot-Schutz, der automatisierte Zugriffe blockt (Test am 15.11.2026: 403); für den Abgleich muss die IP des Servers dort freigegeben werden.

Für Kontakte und Bürgeranliegen gelten eigene Datenschutzhinweise und Löschfristen, da hier Daten von Personen außerhalb der Fraktion liegen.

### 20. Wissensbereich (Wiki) mit KI-Schnittstelle ◐

- ✅ Wiki für die ganze Fraktion: Bereiche für Abläufe, Themen und Politikfelder, Gremien, Positionen und Hintergründe; Seiten mit Versionsverlauf, Vorlagen, `[[Seitenlinks]]`, Bildern, Rechten (vertraulich) und Volltextsuche (SQLite FTS5). Derselbe Editor wie für Anträge.
- ◐ **KI-Schnittstelle**: API und MCP-Server vorhanden, über die ein Sprachmodell Seiten lesen, durchsuchen und Änderungen vorschlagen kann; im Betrieb noch ein Mock (⏳ echtes Modell mit Auftragsverarbeitungsvertrag oder selbst gehostet). Aufgaben der KI:
  - veraltete Angaben und Widersprüche finden, etwa zu neueren Beschlüssen
  - Dubletten zusammenführen, verwaiste Seiten einordnen, Struktur und Verlinkung vorschlagen
  - Lücken aus Protokollen und Beschlüssen ergänzen
  - Fragen beantworten, mit Verweis auf die Quellseiten
- **Die KI ändert nichts direkt**: Jede Umstrukturierung kommt als Änderungsvorschlag mit Vorher-nachher-Ansicht, den eine verantwortliche Person annimmt oder ablehnt. Alles wird protokolliert.
- Die KI arbeitet mit einem eigenen Konto und sieht nur freigegebene Bereiche; vertrauliche Seiten bleiben ausgeschlossen.

### Außerhalb des Plans: Easteregg-Spiel ✅

Ein kleines Point-and-Click-Spiel in 3D („Joe & Chani und Grevens Gral der Demokratie“) mit realen Greven-Schauplätzen (LoD2) unter `/spiel3d` (die frühere Pixel-Fassung unter `/spiel` ist entfernt und leitet dorthin um), ohne Bezug zu Fachdaten; dazu ein Spickzettel mit Direkteinstiegen unter `/cheatsheet`. Details: `docs/spiel.md`.

## Bedienung und Integration ins Handy und den Rechner

Die App soll sich wie eine native App anfühlen und ohne Schulung nutzbar sein. Dazu gehören fünf feste Vorgaben.

| Anforderung | Lösung | Stand |
| --- | --- | --- |
| Anmeldung wie am Handy gewohnt | Passkeys (WebAuthn): Anmeldung per Face ID, Touch ID oder Fingerabdruck, Anmeldelink per Mail als Rückfall; danach lange Sitzung | ⏳ Bisher nur **Testanmeldung** (Konto wählen, ohne Passwort) – auf dem Server deshalb nur hinter dem Zugangspasswort von nginx |
| Wirkt wie eine native App | PWA (Manifest, Service Worker, App-Shortcuts) mit schlanken Handy-Seiten; zusätzlich dünne Capacitor-Hülle für App Store und Google Play | ✅ PWA · ⏳ Capacitor-Hülle |
| Kalender in der nativen Kalender-App | Persönlicher Abo-Link (webcal://) mit widerrufbarem Token; für sofortige Änderungen zusätzlich CalDAV | ✅ Abo · ⏳ CalDAV |
| Benachrichtigungen als native Mitteilung | Web Push (VAPID) mit Aktionsknopf („Ich vertrete“); APNs/FCM für die App-Hülle; Inhalte knapp, nichts Vertrauliches auf dem Sperrbildschirm | ◐ Web Push eingebaut, ohne produktive VAPID-Schlüssel · ⏳ APNs/FCM |
| Dateien im Explorer und Finder | Nextcloud-Desktop-Client mit virtuellen Dateien | ⏳ (WebDAV-Adapter vorhanden, keine Nextcloud) |

### Einfachheit und vorgeschlagene Folgeschritte ✅

- **Startseite „Heute“** zeigt nur, was jetzt zu tun ist: nächste Termine, offene Anfragen und Abstimmungen, Hinweise (eigene Kampagnentermine, Gebiete, Tickets, Fristen), jeweils mit einer Ein-Tipp-Antwort.
- **Schnellaktionen** für die Standardprozesse: Abwesenheit melden, Raum buchen, Antrag starten, Fraktionssitzung einladen, Plakat erfassen, Auslage einreichen.
- **Neu-Formulare mit langem Text** (Anfrage, Antrag, Fassung, Pressemitteilung, Bürgeranliegen, Kampagne, Termin, Ticket) öffnen einen großen Dialog mit Editor (WYSIWYG, Symbolleiste; Markdown nur über einen unauffälligen Knopf).
- **Folgeschritte schlägt das System nach jedem Abschluss vor**, als Karte mit einem Knopf:

| Wenn … | … schlägt das System vor |
| --- | --- |
| eine Abwesenheit eingetragen ist | „In 2 Sitzungen fehlt dann jemand: Vertretung anfragen?“ |
| eine Einladung zu einem öffentlichen Termin angelegt ist | Besetzungsanfrage mit N Plätzen an alle senden |
| ein Antrag in der Fraktion beschlossen ist | PDF erzeugen, einreichen, der passenden Ratssitzung zuordnen |
| eine Fraktionssitzung terminiert ist | Raum buchen, Einladung mit Vorlage-TOPs versenden |
| ein Protokoll freigegeben ist | Aufgaben verteilen, vertagte TOPs in die nächste Einladung übernehmen |
| ein öffentlicher Termin vorbei ist | Kurzbericht anlegen |
| eine Kampagne angelegt ist | Termin anlegen, Gebiete anlegen, Kampagne auf „aktiv“ setzen |
| der Wahltag einer Kampagne vorbei ist | Plakate abhängen (Abhängliste am Handy) |

Die Vorschläge sind feste Regeln, keine KI; das macht sie vorhersehbar. Eine KI-Unterstützung (z. B. Antragsentwurf aus Stichpunkten) kann später dazukommen.

## Datenmodell

Rund ein Dutzend Kernentitäten tragen alle Module; „Gremium“ und „Termin“ sind die Knoten, an denen fast alles hängt. Das vollständige, aus dem Code erzeugte Schema steht in `docs/datenbank-schema.md`; die Regeln in `docs/datenbank-einleitung.md`, der zeitliche Personenaufbau in `docs/personen-zeitlich.md`.

| Entität | Wichtige Felder | Beziehungen |
| --- | --- | --- |
| Person | Id; Namensbestandteile, Kontakte, Anschriften, Geschlecht, Rollen jeweils mit zeitlicher Gültigkeit | ist Mitglied in Gremien, hat Abwesenheiten |
| Gremium | Name, Art (Rat, Ausschuss, Arbeitskreis), zeitlich gültig | hat Mitgliedschaften, Sitzungen, Ordner |
| Mitgliedschaft | Funktion (ordentlich, Stellvertretung), Rang der Vertretung, Amt – bitemporal | verbindet Person und Gremium |
| Termin | Art, Beginn, Ende, Ort; optional Kampagne und Plätze | gehört optional zu Gremium oder Kampagne; hat Teilnahmen, Buchungen, ToDos |
| Teilnahme | Status (zugesagt, abgesagt, vertritt) | verbindet Person und Termin; Einbuchen in Kampagnentermine |
| Besetzungsanfrage | benötigte Anzahl N, Frist, Modus | gehört zu Termin; hat Meldungen |
| Abwesenheit | Von, Bis, Grund (optional, privat) | gehört zu Person; löst Vertretungsanfragen aus |
| Ressource | Name, Typ, Freigabe nötig | hat Buchungen |
| Buchung | Von, Bis, Zweck | verbindet Ressource, Person, optional Termin |
| Antrag | Titel, Status, aktuelle Fassung | hat Fassungen, Kommentare, Abstimmungen; Ziel-Gremium |
| Abstimmung | Frage, Frist, Quorum, Ergebnis | hat Stimmen; gehört zu Antrag oder frei |
| Protokoll | Sitzung, Vorlage, TOPs, Anwesenheit, Notizen | gehört zu Termin; hat Beschlüsse und Aufgaben |
| Kampagne | Name, Zeitraum, Status, Wahltag | hat Plakate, Gebiete, Termine, Tickets |
| Plakat | Koordinaten, Motiv, Status, Foto, erfasst von | gehört zu Kampagne |
| Gebiet | Name, Art, Status, zugeteilt an | gehört zu Kampagne |
| ToDo / ToDo-Vorlage | Text, zuständig, erledigt wer/wann / Name und Einträge | ToDo gehört zu Termin; Vorlage ist Kopiervorlage |
| Ticket | Nummer, Titel, Status, Priorität, Fälligkeit, Bezug | gehört zu Kampagne; hat Kommentare |
| Datei / Bild | Ablageschlüssel (GUID), Freigabe | an Antrag, Protokoll, Termin, Wiki-Seite, Ticket oder Ordner |

Die Zusatzmodule bringen weitere Entitäten mit: Aufgabe, Anfrage, Anliegen, Kontakt, Beschluss, Pressebeitrag, Beleg und Wikiseite (mit Versionen und KI-Änderungsvorschlägen). Gremien, Sitzungen, Vorlagen und Mandate kommen aus dem RIS und werden dort gepflegt, im System nur gespiegelt und ergänzt.

Abwesenheitsgründe sind Gesundheitsdaten, wenn „krank“ drinsteht: Das Feld bleibt optional und ist nur für die Person selbst und die Geschäftsführung sichtbar.

## Architektur und Technik

Eine eigene Web-App für die fraktionsspezifische Logik; Dateien perspektivisch über Nextcloud. So bauen wir nur, was es nicht schon gut gibt.

**Grundlage ist die Architektur von VibeVHS** (.NET, Blazor, SQLite; lokal unter `C:\repos\vibevhs`), damit Bausteine, Muster und Erfahrungen direkt übernommen werden. Übernommene Leitlinien (alle ✅):

- **Gesperrt heißt erklärt:** Nicht klickbare Knöpfe zeigen per Tooltip den Grund (z. B. „Abstimmung erst nach Ende der Kommentarfrist“).
- **Nächste sinnvolle Schritte:** Baustein `NaechsteSchritte` nach jeder Aktion; trägt die Folgeschritt-Regeln aus dem Abschnitt Bedienung.
- **Nichts geht unbemerkt raus:** Jede Mail, jeder Push und jeder Schnittstellenaufruf läuft über den Postausgang (Outbox), gespeichert mit der fachlichen Änderung, per Hintergrundjob zugestellt, protokolliert. Im Demobetrieb verlässt nichts das System (Demo-Schutz).
- **Nichts wird überschrieben:** Personenangaben, Mandate, Mitgliedschaften und Rollen sind bitemporal (gültig von/bis, erfasst/ersetzt, je Änderung ein Änderungsvorgang); Anträge, Wiki-Seiten und Protokolle haben Fassungen.
- **Protokoll mit Pseudonymen,** Aufgabenkörbe je Rolle mit Übernehmen/Zuweisen/Eskalation, Abwesenheit mit Vertretung.
- **Kalender-Abo mit widerrufbarem Token** (nur als Hash gespeichert), mobile Seiten statisch gerendert.

| Baustein | Umsetzung | Stand |
| --- | --- | --- |
| Aufbau | .NET 11, Projekte Domain / Application / Infrastructure / Adapters.Mocks / Web, Tests mit xUnit v3 + Shouldly, Architekturtests | ✅ |
| Oberfläche | Blazor Web App mit Radzen; Verwaltungsseiten interaktiv (Server), Handy-Seiten statisch gerendert; große Dialoge für Neu-Formulare | ✅ |
| Texte und Editor | Toast UI Editor (WYSIWYG, Markdown im Hintergrund), Darstellung mit Markdig und HTML-Bereinigung; Bilder per Strg+V, im Browser verkleinert, abgelegt unter GUID, Rechte vom „Besitzer“ (Antrag, Wiki-Seite, Ticket …) | ✅ |
| Datenbank | EF Core + SQLite (WAL) nach den **Datenbank-Regeln aus VibeVHS**: 3NF ohne Kopien und ohne JSON-Spalten, bitemporale Versionen mit Tabelle `Aenderungsvorgaenge`, Ids als UUIDv7-BLOB, Zeitpunkte als UTC-Millisekunden, Kalendertage als JJJJMMTT, Collation `DIN5007_2` für Namen, lesbare Sichten `v_…`; jede Spalte eingestuft und per Architekturtest geprüft | ✅ siehe `docs/datenbank-*.md` |
| Mandanten | Konfiguration je Mandant (eigene Datenbank, Briefkopf, Zeitzone), Auswahl über den Hostnamen | ◐ vorbereitet, im Betrieb ein Mandant |
| PDF | PDFsharp/MigraDoc aus Markdown, Briefkopf je Mandant | ✅ |
| Anmeldung | ASP.NET Core Identity mit Passkeys, Anmeldelink per Mail als Rückfall | ⏳ bisher Testanmeldung; außerhalb der Entwicklung startet die App nur mit ausdrücklicher Freigabe |
| Hintergrundjobs | Quartz.NET: Postausgang, RIS-Abgleich, Erinnerungen, Fristen und Löschfristen, Eskalation, KI-Prüfung | ✅ |
| Benachrichtigung | Web Push (VAPID) für die PWA, E-Mail – alles über den Postausgang | ◐ Mailkanal ist Mock (⏳ SMTP), Web Push ohne produktive Schlüssel |
| Kalender | ICS-Abo pro Person (webcal, widerrufbares Token) | ✅ · ⏳ CalDAV |
| Dateien | lokal oder WebDAV (Nextcloud) | ◐ lokal im Betrieb |
| Karten | Leaflet mit OpenStreetMap | ✅ |
| RIS | OParl-Adapter hinter einem Port, Mock mit Grevener Gremien | ◐ |
| KI | API + MCP-Server, Mock-Modell | ◐ |
| Betrieb | Linux-Server `liebler.iui.hs-osnabrueck.de/fraktionperfekt` (Klaus' Server, derselbe wie VibeVHS): framework-abhängiger Build (gemeinsame .NET-Runtime aus VibeVHS-Schritt 09), systemd, nginx mit Zugangspasswort (Basic Auth) davor, Daten in `/var/lib/fraktionperfekt`, Sicherung vor jedem Update und täglich, automatischer Rückweg; Ausrollen per `deploy/server/deploy.ps1` | ✅ seit 17.11.2026, siehe `docs/deploy.md` |
| Schema-Änderungen | Migrationen beim Start; die Migration „Initial“ ist seit dem ersten Ausrollen eingefroren, Änderungen nur als **zusätzliche** Migrationen | Regel |

Alternative ohne Eigenentwicklung: Nextcloud mit Kalender, Deck, Forms und Collabora deckt Kalender, Dateien und Texte ab, aber nicht Vertretungslogik, Terminbesetzung, Abstimmungen mit Quorum oder die Plakatkarte. Diese vier Punkte sind der eigentliche Mehrwert einer eigenen App.

## Datenschutz, Sicherheit, Hosting

Die Fraktion ist Verantwortliche im Sinne der DSGVO; das System verarbeitet personenbezogene Daten und teils politische Meinungen (Art. 9 DSGVO). Daraus folgen wenige, aber feste Vorgaben.

- **Hosting:** eigener Server von Klaus (Hochschulnetz Osnabrück, Testsystem). Für den Echtbetrieb Deutschland/EU mit Auftragsverarbeitungsvertrag oder Infrastruktur der Partei bzw. Kommune – noch zu entscheiden.
- **Verzeichnis der Verarbeitungstätigkeiten** und Datenschutzhinweis für Mitglieder und Kampagnenteam; das Kampagnenteam sieht nur Kampagnen und öffentliche Termine. ⏳ Dokumente erstellen.
- **Anmeldung:** ⏳ Passkeys bzw. Zwei-Faktor für alle mit Zugriff auf interne Protokolle und Antragsentwürfe. Bis dahin ist das Testsystem nur hinter dem Zugangspasswort erreichbar, und wer es kennt, kann jedes Konto wählen – nur an Testpersonen weitergeben.
- **Demodaten** enthalten echte Namen der Fraktion und die echten Mailadressen des Vorstands, alle Vorgänge sind erfunden; mit Demodaten verschickt der Postausgang nichts.
- **Verschlüsselung** im Transport (TLS, Hochschulzertifikat); Sicherungen vor jedem Update und täglich auf dem Server. ⏳ Sicherung an einem zweiten Ort, Verschlüsselung der Sicherungen.
- **Löschkonzept** ✅ als Fristenprüfung: u. a. abgehängte Plakate und Zusagen vergangener Kampagnentermine nach einem halben Jahr, Name und Kontakt bei erledigten Bürgeranliegen nach Frist.
- **Fotos von Plakaten** ohne erkennbare Personen; Standortdaten nur des Plakats, nicht der hängenden Person.
- **Nichtöffentliche Sitzungsunterlagen** der Kommune unterliegen der Verschwiegenheit: Ablage nur in Ordnern mit entsprechend engem Zugriff.

Der Betrieb (Updates, Sicherungen, Monitoring) liegt derzeit bei Klaus.

## MVP und Ausbaustufen

Das MVP deckt die Abläufe ab, die jede Woche anfallen: Sitzungen, Termine und Vertretungen. Wer das System wöchentlich nutzt, nutzt es später auch für Anträge und Kampagnen.

| Stufe | Bausteine | Iterationen | Stand |
| --- | --- | --- | --- |
| 1 – MVP | Fundament, Personen/Gremien/Mandate (bitemporal), Kalender mit ICS-Abo, Startseite „Heute“, Abwesenheit und Vertretung, Fraktionssitzungen und Protokolle, Aufgaben mit Körben und Themenpaten, RIS-Adapter (Mock) | I0–I2 | ✅ (RIS echt ⏳) |
| 2 – Zusammenarbeit | Anträge mit Fassungen und Abstimmung, Umfragen und Terminfindung, Terminbesetzung, Räume und Ressourcen, Dateien und Wiki mit Volltextsuche und KI-Schnittstelle | I3–I5 | ✅ (KI echt, Nextcloud ⏳) |
| 3 – Außenwirkung | Anfragen, Beschlusslage, Bürgeranliegen, Pressearbeit, Kontakte, Kampagnen (früher Wahlkampf) und Finanzen | I6–I7 | ✅ (Kampagnen am 17.11.2026 ausgebaut) |
| Feinschliff | Heute mit Hinweisen und Schnellaktionen, globale Suche, PWA, Web Push, Erinnerungen, Löschfristen; danach Datenbank-Regeln aus VibeVHS, große Editor-Dialoge, Betrieb auf dem Server | I8 und folgende | ✅ |
| Easteregg | Spiel „Joe & Chani und Grevens Gral der Demokratie“ | I9 | ✅ |
| Vor dem Echtbetrieb | Passkeys/echte Anmeldung, SMTP, produktive Web-Push-Schlüssel, echte KI, echte OParl- und Nextcloud-Anbindung, Capacitor-Hülle, Mandanten-Rest, Datenschutzdokumente, geführte Touren ausschalten (`Abnahmemodus__Aktiv=false`, Standard ist an) oder überarbeiten: ohne Kontowechsel und ohne Demodaten-Annahmen (heute wechseln sie per Testanmeldung das Konto und lösen echte Aktionen aus) | – | ⏳ |

Die Reihenfolge von Stufe 2 und 3 sollte vom nächsten Wahltermin abhängen; umgesetzt sind inzwischen alle drei Stufen. Die Zusatzmodule 10 bis 20 sind in ihrer Tabelle einer Stufe zugeordnet.

## Offene Fragen

Diese Punkte entscheiden über Technik und Reihenfolge; ohne Antwort gelten die Annahmen in Klammern. Beantwortete Fragen bleiben stehen.

1. ~~Wie groß ist die Fraktion, mit sachkundigen Bürger:innen und Helfer:innen?~~ **Beantwortet:** 18 Ratsmitglieder und 32 sachkundige Bürger:innen, dazu Geschäftsführung und Kampagnenteam.
2. Welche Werkzeuge laufen heute schon, etwa Nextcloud, Messenger-Gruppen, ein Partei-Tool? (Annahme: keine Nextcloud vorhanden)
3. Ist die OParl-Schnittstelle des Grevener SD.NET-RIS freigeschaltet? **Teilweise beantwortet:** Adresse des RIS ist `greven.ratsinfomanagement.net`. Offen: ob OParl aktiv ist und ob die Server-IP am Bot-Schutz freigegeben wird.
4. ~~Wann ist die nächste Kommunalwahl?~~ **Beantwortet:** Die letzte Kommunalwahl in NRW war am 14.09.2025, die nächste ist 2030 (Herbst, genauer Termin noch nicht festgelegt). Der Wahltag ist je Kampagne einstellbar; die Demo-Kampagne „Kommunalwahl 2030“ nimmt den 15.09.2030 an. Die aktive Demo-Kampagne ist eine Themenkampagne zu den Haushaltsberatungen.
5. ~~Wo soll gehostet werden?~~ **Beantwortet (Testsystem):** eigener Server von Klaus (`liebler.iui.hs-osnabrueck.de`), Betrieb durch Klaus. Offen: Hosting für den Echtbetrieb.
6. ~~Wo finde ich VibeVHS?~~ **Beantwortet:** lokal unter `C:\repos\vibevhs` (nur lesend genutzt).
7. ~~Soll das System später auch anderen Fraktionen angeboten werden?~~ **Beantwortet:** Mandantenfähigkeit ist vorbereitet (je Mandant eigene Datenbank, Auswahl über den Hostnamen); Open Source offen.
8. ~~Gibt es ein GitHub-Repository für den Code?~~ **Beantwortet:** Der Code liegt lokal (`C:\repos\fraktionperfekt`), ohne GitHub-Remote; Commits nur lokal.
