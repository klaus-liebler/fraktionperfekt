# FraktionPerfekt

Webbasiertes System für eine kommunalpolitische Fraktion (Stadt Greven, NRW): Termine, Sitzungen, Vertretungen und
später Anträge, Protokolle und Wahlkampf an einem Ort statt in Messenger-Gruppen, Verteilern und Tabellen. Internes
Werkzeug, auf dem Handy genauso nutzbar wie am Rechner.

Architektur, Leitlinien und Bausteine folgen **VibeVHS** (`C:\repos\vibevhs`).

| Dokument | Link |
|---|---|
| Konzept | https://claude.ai/artifact/F6Ln4qK4YndXtGJrB8F7Mb |
| Abnahme (Klick-Drehbücher) | [`docs/abnahme-1.md`](docs/abnahme-1.md), [`docs/abnahme-2.md`](docs/abnahme-2.md), [`docs/abnahme-3.md`](docs/abnahme-3.md) |
| Betrieb, Sicherung, Wiederherstellung | [`docs/betrieb.md`](docs/betrieb.md) |
| Sicherheit (OWASP-Checkliste) | [`docs/sicherheit.md`](docs/sicherheit.md) |
| Handy-Test im LAN (HTTPS) | [`docs/handy-test.md`](docs/handy-test.md) |

## Voraussetzungen

- .NET 11 SDK (`winget install --id Microsoft.DotNet.SDK.11 -e`)
- Git

## Starten

```powershell
cd C:\repos\fraktionperfekt
dotnet run --project src\FraktionPerfekt.Web --launch-profile http
```

Dann <http://localhost:5290> öffnen und bei der **Testanmeldung** ein Konto wählen. Wer mehrere Rollen hat, wechselt die
aktive Rolle oben rechts. Die schlanke Handy-Ansicht liegt unter <http://localhost:5290/handy>.

Beim ersten Start wird `src/FraktionPerfekt.Web/data/fraktionperfekt.db` angelegt (SQLite, WAL, Migrationen automatisch).
In der Entwicklungsumgebung kommen die **Demodaten** dazu (`Demodaten:Anlegen` in `appsettings.Development.json`).
Zurücksetzen: Anwendung beenden und den Ordner `src/FraktionPerfekt.Web/data` löschen. Dieselben Daten für eine beliebige
Datei:

```powershell
dotnet run --project tools\Testdaten -- C:\temp\demo.db
```

### Testkonten (alle Personen erfunden)

| Konto | Rollen | Gut zum Ausprobieren |
|---|---|---|
| Katrin Albers | Administration | Rollen und Vertretungsrang pflegen, RIS-Abgleich, Postausgang, Protokoll |
| Thomas Brinkmann | Vorstand, Mitglied | Aufgabe „Vertretung offen“ (eskaliert), Vertretung selbst eintragen |
| Sabine Hölscher | Vorstand, Mitglied | Abwesenheit melden → Vertretung anfragen |
| Markus Terhaar | Kassier, Mitglied | Rollenwechsel |
| Anna Wewers | Mitglied | ist als Vertretung für den HFA gefragt (Startseite und `/handy`) |
| Jens Overmeyer, Petra Lammers, Dirk Schulze-Eling, Melanie Kock, Stefan Grüter, Ralf Niehues | Mitglied | |
| Lena Hagedorn, Yusuf Demir, Claudia Rensing | Sachkundige Bürger:innen | |
| Felix Bäumer, Nora Feldhaus | Helfer:innen | sehen nur öffentliche und Wahlkampftermine |

Demodaten: sieben fiktive Grevener Gremien aus dem RIS-Mock (Rat, Haupt- und Finanzausschuss, Ausschuss für Schule und
Bildung, Ausschuss für Stadtentwicklung und Verkehr, Soziales, Klima, Rechnungsprüfung) mit ordentlichen Mitgliedern und
Stellvertretungen in Rangfolge, Sitzungen im festen Rhythmus, die Fraktion selbst und ein Arbeitskreis, 14-tägliche
Fraktionssitzungen und öffentliche Termine. Themen echt (Stand November 2026: Grundschulplanung inkl.
Martin-Luther-Grundschule, Ganztag ab 2026, Haushalt 2026, Prioritätenliste Bauinvestitionen, Badsanierung durch die
Stadtwerke, Radfahrstreifen Rathausstraße), alle Inhalte und Personen erfunden. Vertretungen in allen Zuständen:
angefragt (HFA), abwesend aber noch nicht angefragt (ASV), beim Vorstand eskaliert (ASFS), besetzt (RPA).

## Bauen und testen

```powershell
dotnet build FraktionPerfekt.slnx
dotnet test --solution FraktionPerfekt.slnx
```

Tests laufen mit xUnit v3 auf Microsoft.Testing.Platform (`global.json`). Die Infrastrukturtests arbeiten gegen echte
SQLite-Dateien mit Migrationen und einer Testuhr, die bei jedem Abruf weiterläuft. Der Webtest startet die gebaute
Anwendung als eigenen Prozess und prüft Anmeldung, alle Seiten und die Ein-Tipp-Antwort auf dem Handy per HTTP.

Abfragen mit `Skip`/`Take` oder `First` ohne Filter müssen eindeutig sortiert sein; in Entwicklung und Tests bricht eine
solche Abfrage ab (`Abfragewarnungen.Konfigurieren`), im Betrieb wird sie protokolliert.

## Aufbau

| Projekt | Inhalt |
|---|---|
| `src/FraktionPerfekt.Domain` | Fachkern ohne Abhängigkeiten: bitemporale Versionen (`BitemporaleVersion`, `ZeitlicheHistorie`), `Person` mit versionierten Rollen, `Gremium`, `Mitgliedschaft` (Funktion + Rang bitemporal), `Termin`, `Teilnahme`, `KalenderAbo`, `Abwesenheit`, `Vertretungsfall` (Ablauf in Rangfolge), `Aufgabe` |
| `src/FraktionPerfekt.Application` | Anwendungsfälle und Ports: `Freigabe`/`Rechte`, `NaechsterSchritt`, Postausgang (Mail + Push), Protokoll, `StammdatenDienst`, `RisAbgleich` + Port `IRisSchnittstelle` (OParl), `KalenderDienst` (inkl. ICS-Abo), `VertretungDienst`, `AufgabenDienst`, `HeuteDienst` |
| `src/FraktionPerfekt.Infrastructure` | EF Core + SQLite (WAL), Migrationen, Speicher, Quartz-Jobs (Postausgang alle 15 s, Vertretungsfristen alle 5 min, RIS-Abgleich nachts) |
| `src/FraktionPerfekt.Adapters.Mocks` | Testanmeldung, simulierte Mail-/Push-Kanäle, RIS-Mock mit Grevener Gremien, Demodaten |
| `src/FraktionPerfekt.Web` | Blazor Web App (Interactive Server, Radzen), deutsche Oberfläche; Handy-Seiten statisch gerendert; PWA-Manifest |
| `tests/*` | xUnit v3 + Shouldly |
| `tools/Testdaten` | Demodaten-Generator (Bogus, fester Startwert) |

## Leitlinien

- **Gesperrt heißt erklärt:** Nicht klickbare Schaltflächen (`BegruendeterButton`) und Navigationspunkte zeigen per Tooltip
  den Grund (Rolle fehlt, Termin vorbei, „folgt in Stufe 2“ …).
- **Nächste sinnvolle Schritte:** Nach jeder Aktion bietet `NaechsteSchritte` die Folgeschritte an, z. B. nach einer
  Abwesenheit „In 2 Sitzungen fehlt dann jemand: Vertretung anfragen“, nach einer Absage als ordentliches Mitglied
  „Vertretung anfragen“. Die Regeln sind fest, keine KI.
- **Nichts geht unbemerkt raus:** Jede Mail und jede Push-Mitteilung läuft über den Postausgang, wird mit der fachlichen
  Änderung gespeichert, per Quartz-Job zugestellt (in der Entwicklung simuliert) und protokolliert.
- **Nichts wird überschrieben:** Rollen und Mitgliedschaften (Funktion, Vertretungsrang) sind bitemporal; Änderungen gelten
  ab einem Datum, der Verlauf ist über das Uhr-Symbol sichtbar.
- **Protokoll nur mit Pseudonymen:** „Person #ABCD1234“ statt Namen; Abwesenheitsgründe erscheinen nie in Protokoll oder
  Nachrichten und sind nur für die Person selbst und die Administration sichtbar.
- **Eindeutige Sortierung** bei begrenzten Abfragen (siehe oben).

## Aus VibeVHS übernommen (Betrieb und Sicherheit)

- **Sicherheitsheader** mit strenger CSP (Skripte nur eigene Dateien plus Hash der Import-Map), `no-store` für Seiten mit
  Personendaten, **Rate-Limits** auf Anmeldung und Kalender-Abo (`docs/sicherheit.md`).
- **Tägliche Sicherung** (SQLite-Online-Backup) mit SHA-256 und automatischem **Wiederherstellungstest**, Seite
  `/betrieb` (Administration), Notfallablauf in `docs/betrieb.md`.
- **LAN-Profil** mit selbst erzeugtem Zertifikat für den Handy-Test über HTTPS (`docs/handy-test.md`).
- **Abnahme-Drehbuch** mit Think-aloud, Richtzeiten und Nachgespräch (`docs/abnahme-1.md`).
- Bewusst nicht übernommen, weil für eine Fraktion mit 10 bis 40 Personen unnötig: virtuelle Listen mit Keyset-Nachladen
  und FTS5-Suche, Webshop-API mit Proof-of-Work. Das Muster „Anmeldelink per Mail“ (15 Minuten, einmalig, Bestätigung
  per POST) ist für die echte Anmeldung vorgemerkt.

## Iteration 1: was es gibt

- **Personen, Gremien, Mandate** (`/personen`, `/gremien`): Gremien und Mitgliedschaften kommen aus dem RIS (Port
  `IRisSchnittstelle`, derzeit `MockRis`); Funktion folgt dem RIS, der Vertretungsrang ist fraktionsintern und ab Datum
  änderbar. Rollen je Person ab Datum. RIS-Abgleich nachts oder auf Knopfdruck, idempotent.
- **Kalender** (`/kalender`): Monats- und Listenansicht, Filter nach Gremium und Person („Nur meine“), Zusage/Absage,
  Termine anlegen (Rats-/Ausschusssitzungen nur aus dem RIS). Termindetail mit Besetzung und Rückmeldungen.
- **Kalender-Abo** (`/kalender-abo`): persönlicher webcal-Link, Token nur als SHA-256 gespeichert, widerrufbar; ein neuer
  Link widerruft den alten. Abruf ohne Anmeldung unter `/kalender/abo/{token}.ics`.
- **Startseite „Heute“** (`/`, Handy: `/handy`): Vertretungsanfragen und offene Zusagen mit Ein-Klick-Antwort, nächste
  Termine, für den Vorstand die Aufgaben; Schnellaktionen „Abwesenheit melden“ und „Termin anlegen“.
- **Abwesenheit und Vertretung** (`/abwesenheit`, `/vertretungen`): Abwesenheit eintragen (Grund optional, privat) →
  betroffene Sitzungen als ordentliches Mitglied → „Vertretung anfragen“ → Stellvertretungen in Rangfolge
  (Push + Mail; abwesende werden übersprungen) → Zusage trägt ein und benachrichtigt; Ablehnung oder Fristablauf (24 h,
  spätestens 2 h vor Beginn) geht zur nächsten Person → ohne Zusage Aufgabe an den Vorstand, der selbst eintragen kann.
  „Ich bin doch da“ zieht alles zurück. Übersicht „Sitzungen mit offener Vertretung“ (auch: abwesend, noch nicht angefragt).

## Iteration 2: Sitzungen, Protokolle, Aufgaben

- **Sitzungen** (`/sitzungen`): eine Protokoll-Engine für Fraktionssitzungen, Arbeitskreise und eigene Ausschuss- und
  Ratsprotokolle.
  - Vorbereiten aus einer Vorlage mit festen TOPs; bei Gremienprotokollen kommen die TOPs aus der RIS-Tagesordnung.
  - Vertagte TOPs früherer Sitzungen desselben Gremiums werden übernommen.
  - Mitglieder melden TOPs bis zur Frist an (drei Tage vorher).
  - Tagesordnung: umsortieren, einfügen, vertagen, zusammenlegen.
  - Einladung mit Tagesordnung und Zu-/Absage-Link über den Postausgang (Mail und Push).
  - Sitzungsmodus: Anwesenheit aus den Rückmeldungen vorbelegt, Notizen je TOP, Beschlüsse mit Stimmen, Aufgaben an
    Personen mit Frist.
  - Ablauf: Entwurf → Freigabe durch Sitzungsleitung oder Vorstand → Versand per Mail; danach unveränderlich.
  - PDF im Fraktionslayout mit Logo (`/sitzungen/{id}/protokoll.pdf`, PDFsharp/MigraDoc).
  - Volltextsuche über freigegebene Protokolle.
- **Aufgaben** (`/aufgaben`, Modul 12, wie in VibeVHS):
  - Listen „Meine“, „Im Korb“ (aktive Rolle), „Bei anderen“, „Kürzlich erledigt“.
  - Übernehmen, Zuweisen (mit Push), Zurück in den Korb, Erledigen mit Vermerk; gesperrte Knöpfe erklären den Grund.
  - Fristen mit einmaliger Erinnerung 24 Stunden vorher (stündlicher Fristen-Job).
  - **Themenpaten** je Politikfeld bekommen passende Aufgaben direkt.
- Startseite: „Meine Aufgaben“. Termin- und Kalenderseiten führen zu Tagesordnung und Protokoll.
- Technisch:
  - Allgemeiner Datenport `IDaten` (Abfragen als `IQueryable`, ausgeführt in der Infrastruktur).
  - Fristen-Job mit `IFristenpruefung` je Modul.
  - Logo aus `CDU-Gesamtlogo_transparent_RGB.png` (nur skaliert) in Kopfzeile, Favicon, PWA-Icons (192/512) und
    PDF-Briefkopf (`wwwroot/marke`).

## Iteration 3: Anträge, Abstimmungen, Umfragen, Terminfindung

- **Anträge** (`/antraege`, Modul 3):
  - Status-Ablauf mit erlaubten Übergängen: Idee → Entwurf → in Abstimmung → beschlossen/abgelehnt → eingereicht →
    im Gremium beraten → erledigt, dazu zurückziehen.
  - Der Text ist Markdown in **Fassungen** (nie überschrieben), mit wortweisem **Vergleich**.
  - **Kommentare** mit Zitat. **Änderungsvorschläge**: Die Stelle muss eindeutig vorkommen; Annehmen erzeugt eine neue
    Fassung; entscheiden dürfen Autor:in oder Vorstand.
  - **Fraktionsabstimmung** mit Frist und Quorum: stimmberechtigt sind die Ratsmitglieder. Das Ergebnis setzt den Status
    und legt einen Fraktionsbeschluss an.
  - Einreichen mit Nummer `A-JJJJ-NNN`, Zuordnung zu Gremium und Sitzung, **PDF im Fraktionslayout**
    (`/antraege/{id}/antrag.pdf`).
  - Anstehende Anträge hängen automatisch als TOP an der nächsten Fraktionssitzung.
- **Abstimmungen und Umfragen** (`/abstimmungen`, Modul 13):
  - Arten: Meinungsbild (Ja/Nein/Enthaltung), Umfrage (eigene Antworten, optional Mehrfachauswahl), Terminfindung
    (Zeitpunkte).
  - Stimmen bis zur Frist änderbar; das Protokoll vermerkt nur, *dass* abgestimmt wurde.
  - Abschluss durch die Erstellerin bzw. den Ersteller oder den Vorstand, sonst automatisch bei Fristablauf (Fristen-Job).
  - Nach einer Terminfindung schlagen die nächsten Schritte „Termin anlegen“ mit vorausgefülltem Kalender vor.
- Startseite: „Bitte abstimmen“ und Schnellaktionen „Antrag starten“, „Fraktionssitzung einladen“.

## Gestaltung

Farben und Schrift stehen zentral als CSS-Variablen in `src/FraktionPerfekt.Web/wwwroot/app.css` (`:root`, oben), das
Radzen-Theme liest sie mit. Angelehnt an das CDU-Corporate-Design: „Cadenabbia“ (Türkis, `--fp-cadenabbia`) als Fläche,
„Rhöndorf“ (Dunkelblau, `--fp-rhoendorf`) für Text und Schaltflächen, Schwarz-Rot-Gold nur als dünner Streifen
(`.fp-streifen`). **Die Hex-Werte sind vorläufig** und mit dem offiziellen Markenhandbuch abzugleichen. Kontraste nach
WCAG 2.2 AA: Text auf Türkis immer dunkel. Die Hausschrift ist lizenzpflichtig und nicht eingebunden; Ersatz über
`--fp-font` (Tahoma, Helvetica, Arial).

Logo: Die Fraktion hat `CDU-Gesamtlogo_transparent_RGB.png` (dunkle Schrift, für hellen Grund) und zwei EPS-Dateien
abgelegt. Verwendet wird nur das PNG, unverändert und nur skaliert:

| Datei in `src/FraktionPerfekt.Web/wwwroot/marke` | Verwendung |
|---|---|
| `logo.png` (600 px breit) | Kopfzeile (`Fraktion:Logo`) |
| `favicon.png` (64 px) | Favicon |
| `icon-192.png`, `icon-512.png` | PWA-Symbol, Apple-Touch-Icon (Logo zentriert auf weißer Fläche) |
| `logo-druck.png` (1200 px breit) | Briefkopf der PDFs (Protokolle, Anträge) |

Die EPS-Dateien kann der Browser nicht darstellen; sie bleiben als Quelle im Projektordner.

## Bibliotheken

Erlaubt: Radzen.Blazor, EF Core + SQLite, Serilog, Quartz.NET, xUnit, Shouldly, Bogus, Verify.
Nicht verwenden: QuestPDF, Nager.Date, MediatR, AutoMapper, FluentAssertions. Versionen zentral in `Directory.Packages.props`.

## Offen / nächste Schritte

- Echte Anmeldung (Passkeys, Anmeldelink per Mail), echte Zustellung (SMTP, Web Push/VAPID), OParl-Adapter für das
  Grevener RIS (Freischaltung und Server-IP beim Bot-Schutz klären).
- Service Worker und App-Hülle (Capacitor); das Manifest ist vorbereitet.
- Personen und Gremien anlegen/bearbeiten in der Oberfläche (bisher Demodaten und RIS-Abgleich; Rang und Rollen sind pflegbar).
- Löschkonzept für Abwesenheiten (z. B. nach 12 Monaten), Erinnerungen vor Sitzungen.
- Stufe 2 laut Konzept: Raumbuchung, Anträge, Protokolle, Terminbesetzung öffentlicher Termine.
