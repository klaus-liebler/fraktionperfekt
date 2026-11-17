# Abnahme – Iteration 8: Feinschliff

Ablauf und Nachgespräch wie in [`abnahme-1.md`](abnahme-1.md). Dauer etwa 20 Minuten.

| Nr. | Aufgabe | Testkonto (Rolle) | Richtzeit |
|---|---|---|---|
| 1 | Was steht heute an? | Felix Bäumer (Helfer), Dames Joud (Kassier) | 3 min |
| 2 | Alles finden | Dames Joud (Mitglied), Uta Ahrens (sachkundige Bürgerin) | 3 min |
| 3 | App installieren und offline öffnen | beliebig, Handy | 4 min |
| 4 | Push aktivieren (nur mit `Push:Art=WebPush`) | beliebig | 4 min |
| 5 | Termin in den eigenen Kalender | Dames Joud (Mitglied) | 2 min |
| 6 | Erinnerung vor der Sitzung | Katrin Albers (Administration) | 3 min |

## 1. Was steht heute an?

- **Startpunkt:** **Heute**.
- **Erwartung:** Felix sieht unter „Steht an“ seine Infostand-Schicht und das Gebiet „Greven-Innenstadt“; bei den
  Schnellaktionen sind „Plakat erfassen“ und „Auslage einreichen“ frei, „Anfrage schreiben“ ist gesperrt (Begründung beim
  Darüberfahren). Dames (Rolle Kassier) sieht „Zuwendungsbestätigungen offen“, im Korb „Auslage prüfen“.
- **Zeit:** ____ min. **Beobachtungen:**

## 2. Alles finden

- **Aufgabe:** Suchen Sie oben im Suchfeld „Grundschul“, danach „Stellungnahme Stadtwerke“.
- **Erwartung:** Treffer gruppiert nach Termine, Anträge, Beschlüsse, Wiki, Dateien …; die vertrauliche HFWA-Stellungnahme
  findet Dames (HFWA-Vertretung), Uta nicht.
- **Zeit:** ____ min. **Beobachtungen:**

## 3. App installieren und offline öffnen

- **Aufgabe:** Fügen Sie FraktionPerfekt am Handy zum Startbildschirm hinzu (HTTPS-Profil, siehe
  [`handy-test.md`](handy-test.md)). Langer Druck auf das Symbol zeigt die Kurzbefehle. Schalten Sie den Flugmodus ein und
  öffnen Sie die App.
- **Erwartung:** Symbol mit Fraktionslogo; Kurzbefehle Heute, Abwesenheit, Plakat, Auslage, Suche. Offline erscheint
  „Keine Verbindung“ im Fraktionsstil statt einer Browserfehlerseite.
- **Zeit:** ____ min. **Beobachtungen:**

## 4. Push aktivieren

- **Voraussetzung:** Start mit `Push__Art=WebPush` (Schlüssel werden beim ersten Start in `data/vapid.json` erzeugt), HTTPS
  oder localhost.
- **Startpunkt:** **Kalender-Abo und Push** → „Push auf diesem Gerät aktivieren“.
- **Erwartung:** Der Browser fragt nach Erlaubnis; das Gerät erscheint unter „Angemeldete Geräte“. Eine Vertretungsanfrage
  oder Aufgabe kommt als Mitteilung; Antippen öffnet die passende Seite. Im Postausgang steht „Web Push an 1 Gerät(e)“.
- **Zeit:** ____ min. **Beobachtungen:**

## 5. Termin in den eigenen Kalender

- **Startpunkt:** **Kalender** → beliebiger Termin → „In meinen Kalender“.
- **Erwartung:** Eine `.ics`-Datei öffnet sich im Kalender des Geräts, mit Link zurück zum Termin. Das Kalender-Abo
  enthält auch eingetragene Infostand-Schichten.
- **Zeit:** ____ min. **Beobachtungen:**

## 6. Erinnerung vor der Sitzung

- **Startpunkt:** **Postausgang** am Tag vor einer Ausschusssitzung (oder in der Demo die Testuhr; der Fristen-Job läuft
  stündlich).
- **Erwartung:** Je Mitglied genau eine Push-Mitteilung „Morgen 17:00: HFWA“; wer abgesagt hat oder abwesend ist, bekommt
  keine.
- **Zeit:** ____ min. **Beobachtungen:**
