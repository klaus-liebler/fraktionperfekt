# Abnahme – Klick-Drehbuch für die Fraktion (Stand nach Iteration 1)

**Teilnehmende:** 2–4 Personen der Fraktion, z. B. Geschäftsführung, ein Vorstandsmitglied, ein Ratsmitglied, eine
sachkundige Bürgerin oder ein sachkundiger Bürger.
**Dauer:** 45–60 Minuten. **Adresse:** die lokale Testinstanz (`http://localhost:5290`, Handy: siehe
`docs/handy-test.md`). **Daten:** ausschließlich fiktive Demodaten.

## Ablauf

1. **Begrüßung (5 min):** Wir testen die Software, nicht Sie. Fehler sind wertvolle Hinweise. Alles ist Testbetrieb:
   Mails und Push-Mitteilungen landen nur im **Postausgang**, das Ratsinformationssystem ist simuliert.
2. **Lautes Denken:** Bitte sagen Sie beim Klicken laut, was Sie suchen, erwarten und worüber Sie sich wundern. Die
   Moderation hilft nur, wenn jemand länger als zwei Minuten festsitzt, und notiert, wo das war.
3. **Aufgaben (30–40 min):** Reihenfolge wie unten. Das Konto wechseln Sie oben rechts mit **Konto wechseln** (kein
   Passwort). Ausgegraute Schaltflächen erklären beim Darüberfahren (oder mit der Tab-Taste), warum sie gesperrt sind.
   Bitte sagen Sie, ob die Erklärung verständlich ist.
4. **Nachgespräch (10 min)** mit den fünf Fragen am Ende.

Moderation: je Aufgabe die Zeit (Start bis „fertig“ oder Abbruch) und Beobachtungen eintragen. Abbruch nach 6 Minuten.

| Nr. | Aufgabe | Testkonto (Rolle) | Richtzeit |
|---|---|---|---|
| 1 | Termine finden und zusagen | Dames Joud (Mitglied) | 4 min |
| 2 | Vertretungsanfrage am Handy beantworten | Dames Joud (Mitglied) | 2 min |
| 3 | Abwesenheit melden und Vertretung anfragen | Eike Brinkhaus (Vorstand, Mitglied) | 4 min |
| 4 | Kalender aufs Handy holen | Eike Brinkhaus | 3 min |
| 5 | Offene Vertretung als Vorstand lösen | Johannes Wilp (Vorstand) | 4 min |
| 6 | Fraktionssitzung anlegen | Johannes Wilp | 3 min |
| 7 | Vertretungsrang ändern | Katrin Albers (Administration) | 4 min |
| 8 | Nachvollziehen: Postausgang und Protokoll | Katrin Albers | 3 min |
| 9 | Was sehen Helfer:innen? | Felix Bäumer (Helfer:innen) | 2 min |

---

## 1. Termine finden und zusagen

- **Testkonto:** Dames Joud. **Startpunkt:** Startseite „Heute“.
- **Aufgabe:** Sagen Sie der nächsten Fraktionssitzung zu. Finden Sie im Kalender alle Sitzungen des Ausschusses für
  Schule und Bildung in den nächsten zwei Monaten. Welche Tagesordnungspunkte sind angekündigt?
- **Erwartung:**
  - „Zusagen“ wirkt mit einem Klick; die Sitzung verschwindet aus „Bitte zu- oder absagen“.
  - Der Filter „Gremium“ zeigt nur ASB-Sitzungen. In der Termindetailseite steht ein Auszug der Tagesordnung
    (z. B. Grundschulplanung, Ganztag).
- **Zeit:** ____ min. **Beobachtungen:**

## 2. Vertretungsanfrage am Handy beantworten

- **Testkonto:** Dames Joud. **Startpunkt:** `/handy` (oder Handy-Ansicht oben links).
- **Aufgabe:** Sie werden gefragt, im HFWA zu vertreten. Sagen Sie zu.
- **Erwartung:**
  - Ein Tipp auf „Ich vertrete“ genügt; die Bestätigung nennt Datum und Uhrzeit.
  - Im Kalender steht der HFWA-Termin mit „Sie vertreten“.
- **Zeit:** ____ min. **Beobachtungen:**

## 3. Abwesenheit melden und Vertretung anfragen

- **Testkonto:** Eike Brinkhaus. **Startpunkt:** Startseite → Schnellaktion **Abwesenheit melden**.
- **Aufgabe:** Sie sind vom 07.12. bis 09.12. verreist. Tragen Sie das ein und sorgen Sie dafür, dass Sie vertreten werden.
- **Erwartung:**
  - Nach dem Eintragen zeigt das System die betroffenen Sitzungen und schlägt als nächsten Schritt
    „In 2 Sitzungen fehlt dann jemand: Vertretung anfragen“ vor.
  - Danach steht bei beiden Sitzungen „angefragt“. Im Verlauf ist sichtbar, wer gerade gefragt ist und bis wann.
  - Der Grund (optional) erscheint nirgends außer in „Meine Abwesenheiten“.
- **Zeit:** ____ min. **Beobachtungen:**

## 4. Kalender aufs Handy holen

- **Testkonto:** Eike Brinkhaus. **Startpunkt:** **Kalender-Abo**.
- **Aufgabe:** Abonnieren Sie Ihre Termine in Ihrem Handykalender. Danach ist der Link „versehentlich weitergegeben“
  worden: Sorgen Sie dafür, dass er nicht mehr funktioniert.
- **Erwartung:**
  - Der Link wird nur einmal angezeigt; „Zum Kalender hinzufügen“ öffnet die Kalender-App.
  - „Widerrufen“ oder ein neuer Link macht den alten unbrauchbar.
- **Zeit:** ____ min. **Beobachtungen:**

## 5. Offene Vertretung als Vorstand lösen

- **Testkonto:** Johannes Wilp, Rolle **Vorstand** (oben rechts). **Startpunkt:** Startseite, Bereich „Für den Vorstand“.
- **Aufgabe:** Für den Sozialausschuss hat niemand zugesagt. Sie haben telefonisch jemanden gefunden: Tragen Sie
  Liz Kühlert als Vertretung ein.
- **Erwartung:**
  - Der Verlauf zeigt alle Anfragen mit „kann nicht“.
  - „Als Vertretung eintragen“ ist gesperrt, bis eine Person gewählt ist (Tooltip).
  - Danach ist die Aufgabe erledigt; Alexandra Cleve bekommt eine Mitteilung (Postausgang).
- **Zeit:** ____ min. **Beobachtungen:**

## 6. Fraktionssitzung anlegen

- **Testkonto:** Johannes Wilp. **Startpunkt:** **Kalender** → **Termin anlegen**.
- **Aufgabe:** Legen Sie eine Sondersitzung der Fraktion zum Haushalt 2026 an (nächster Donnerstag, 19 Uhr, 2 Stunden).
- **Erwartung:**
  - Rats- und Ausschusssitzungen sind nicht wählbar, weil sie aus dem RIS kommen.
  - Die nächsten Schritte bieten „Raum buchen“ und „Einladung versenden“ an. Beide sind gesperrt mit der Begründung
    „folgt in Iteration 2“.
- **Zeit:** ____ min. **Beobachtungen:**

## 7. Vertretungsrang ändern

- **Testkonto:** Katrin Albers. **Startpunkt:** **Gremien und Mandate** → Haupt- und Finanzausschuss.
- **Aufgabe:** Ab dem 1. des nächsten Monats soll Michael Gries als Erster vertreten. Ändern Sie die Rangfolge.
- **Erwartung:**
  - Die Änderung gilt ab Datum; frühere Sitzungen behalten die alte Reihenfolge (Uhr-Symbol: Verlauf).
  - Mitglieder in städtischen Gremien lassen sich hier nicht hinzufügen (Hinweis: kommt aus dem RIS).
- **Zeit:** ____ min. **Beobachtungen:**

## 8. Nachvollziehen: Postausgang und Protokoll

- **Testkonto:** Katrin Albers. **Startpunkt:** **Postausgang**, dann **Protokoll**.
- **Aufgabe:** Finden Sie die Push-Mitteilung, die Liz Kühlert (Aufgabe 5) oder Dames Joud bekommen hat. Was steht
  im Protokoll zu Aufgabe 3?
- **Erwartung:**
  - Push-Texte sind kurz und ohne vertrauliche Details; die Mails sind ausführlicher.
  - Das Protokoll nennt nur Pseudonyme („Person #…“), keine Namen und keine Abwesenheitsgründe.
- **Zeit:** ____ min. **Beobachtungen:**

## 9. Was sehen Helfer:innen?

- **Testkonto:** Felix Bäumer. **Startpunkt:** **Kalender**.
- **Aufgabe:** Welche Termine sehen Sie? Versuchen Sie, eine Abwesenheit zu melden.
- **Erwartung:** Nur öffentliche und Wahlkampftermine. „Abwesenheit melden“ ist gesperrt und erklärt warum.
- **Zeit:** ____ min. **Beobachtungen:**

---

## Nachgespräch (5 Fragen)

1. Welche Aufgabe ging am leichtesten, welche war am schwersten, und warum?
2. Wo haben Sie etwas anders erwartet (Begriffe, Reihenfolge, Ort einer Schaltfläche)?
3. Waren die Erklärungen an gesperrten Schaltflächen und die vorgeschlagenen nächsten Schritte hilfreich?
4. Was fehlt Ihnen im Vergleich zur heutigen Arbeitsweise (Messenger-Gruppe, Mail-Verteiler), damit Sie umsteigen würden?
5. Wenn Sie eine Sache sofort ändern dürften: welche?

**Notizen der Moderation:**
