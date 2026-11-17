# Schnittstellen nach außen – geplant und Stand

Übersicht aller Verbindungen von FraktionPerfekt zu anderen Systemen, nach dem Muster von VibeVHS (`docs/schnittstellen.md`
dort). Grundlage: Konzept (Module 1 bis 20, Abschnitte „Bedienung“ und „Architektur“) und der Stand im Code am 17.11.2026.

**Stand-Begriffe:** **umgesetzt** (läuft mit dem echten Gegenüber bzw. Standardformat) · **Mock** (Port vorhanden, Simulation
statt Gegenstelle, Ergebnis im Postausgang bzw. Protokoll sichtbar) · **konzeptionell** (nur im Konzept beschrieben).

**Grundsätze:** Jede Schnittstelle ist ein Port in `FraktionPerfekt.Application` mit austauschbarem Adapter (Mocks in
`FraktionPerfekt.Adapters.Mocks`). Alles Ausgehende läuft über den **Postausgang** und wird protokolliert (Pseudonyme statt
Namen). Zugangsdaten nur als Umgebungsvariablen auf dem Zielsystem.

## Kommunikation

| Schnittstelle | Zweck | Richtung | Format / Technik | Stand |
|---|---|---|---|---|
| **E-Mail** | Einladungen, Erinnerungen, Vertretungsanfragen, Protokollversand | aus | SMTP über ein Mail-Relay | **Mock** (`IZustellKanal`, Kanal E-Mail; Demo-Schutz: mit Demodaten nie echte Zustellung) |
| **Web Push** | Vertretungsanfragen, Erinnerungen 24 h vor Sitzungen, Aufgaben mit Aktionsknopf | aus | Web Push mit VAPID (RFC 8291/8292), PWA | **umgesetzt** (Verschlüsselung und VAPID im Code); Zustellung in der Entwicklung simuliert |
| **Native Push** (App-Hülle) | wie Web Push, als native Mitteilung | aus | APNs (Apple), FCM (Google) über eine Capacitor-Hülle | **konzeptionell** |
| **Kalender-Abo** | Termine in der Kalender-App des Handys | aus | iCalendar (ICS, `webcal://`) mit widerrufbarem Token (nur als Hash gespeichert) | **umgesetzt** |
| **CalDAV** | sofort aktuelle Termine statt stündlichem Abo-Abgleich | aus / ein | CalDAV über Nextcloud | **konzeptionell** |

## Rat und Verwaltung

| Schnittstelle | Zweck | Richtung | Format / Technik | Stand |
|---|---|---|---|---|
| **Ratsinformationssystem (RIS)** | Gremien, Mitgliedschaften, Sitzungen, Tagesordnungen, Vorlagen, Beschlüsse | ein | OParl (SD.NET, Sternberg); nur öffentliche Inhalte, lesend | **Mock** (`IRisSchnittstelle`, Grevener Gremien); echte Anbindung offen: OParl in Greven freischalten, Bot-Schutz des RIS (403) für die Server-IP öffnen |
| **Mandatsträger-Zugang** | nichtöffentliche Sitzungsunterlagen | – | Link ins RIS statt Kopie | **umgesetzt** als Verweis |
| **Anfragen an die Verwaltung** | Anfrage stellen, Antwort ablegen, Frist verfolgen | aus / ein | E-Mail an die Verwaltung (Postausgang), Antwort von Hand erfasst | **Mock** (Versand simuliert) |

## Dateien, Wissen und KI

| Schnittstelle | Zweck | Richtung | Format / Technik | Stand |
|---|---|---|---|---|
| **Dateiablage** | Unterlagen je Gremium, Arbeitskreis, Projekt | aus / ein | heute eigene Ablage (`IDateiablage`); Konzept: Nextcloud per WebDAV mit Desktop-Client (Explorer/Finder) | **umgesetzt** (eigene Ablage), Nextcloud **konzeptionell** |
| **Office-Bearbeitung** | Dokumente gemeinsam im Browser bearbeiten | – | Collabora/OnlyOffice über Nextcloud | **konzeptionell** |
| **KI-Schnittstelle** | Wiki lesen, durchsuchen, Änderungen vorschlagen (nie direkt ändern) | ein / aus | REST `/api/wiki/…` und MCP-Server `/mcp` (JSON-RPC), eigener Schlüssel, ohne vertrauliche Bereiche | **umgesetzt** (Schnittstelle); Sprachmodell **Mock** (`IKiAssistent`); Anbieter mit AV-Vertrag oder selbst gehostet offen |
| **PDF** | Anträge, Protokolle, Quittungen | aus | PDFsharp/MigraDoc (`IPdfErzeugung`) | **umgesetzt** |

## Anmeldung und Plattform

| Schnittstelle | Zweck | Richtung | Format / Technik | Stand |
|---|---|---|---|---|
| **Anmeldung** | Login ohne Passwort | ein | Passkeys (WebAuthn), Anmeldelink per Mail als Rückfall; ggf. Keycloak | **Mock** (Testanmeldung; auf dem Testsystem abgeschaltet) |
| **Karten** | Plakatstandorte, Gebiete | ein | Leaflet mit Kacheln von tile.openstreetmap.org (Namensnennung) | **umgesetzt** |
| **App-Stores** | Installation wie eine native App | – | PWA (Manifest, Service Worker); Capacitor-Hülle für App Store / Google Play | PWA **umgesetzt**, Hülle **konzeptionell** |

## Öffentlichkeit

| Schnittstelle | Zweck | Richtung | Format / Technik | Stand |
|---|---|---|---|---|
| **Bürgeranliegen** | Eingang von Bürgerinnen und Bürgern | ein | Formular oder E-Mail-Eingang | **konzeptionell** (heute Erfassung durch die Fraktion) |
| **Presse und Social Media** | Pressemitteilungen, Beiträge nach Freigabe | aus | Versand an Presseverteiler, Social-Media-Konten | **konzeptionell** (heute Redaktion und Freigabe in der App) |
| **Impressum** | Anbieterkennzeichnung | aus | öffentliche Seite `/impressum` | **umgesetzt** |

## Offen und abzuklären

- RIS: OParl in Greven freischalten, Adresse, Freigabe der Server-IP am Bot-Schutz.
- Mail-Relay und Absenderadresse der Fraktion; Push-Schlüssel (VAPID) auf dem Zielsystem.
- Nextcloud ja/nein (Dateien, CalDAV, Office).
- KI-Anbieter mit Auftragsverarbeitung oder selbst gehostetes Modell.
- Passkeys/Keycloak für die echte Anmeldung.
