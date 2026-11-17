# Abnahme – Iteration 5: Wissen und Dateien

Ablauf und Nachgespräch wie in [`abnahme-1.md`](abnahme-1.md). Dauer etwa 25 Minuten.

| Nr. | Aufgabe | Testkonto (Rolle) | Richtzeit |
|---|---|---|---|
| 1 | Datei im Ausschussordner finden und ablegen | Chantal Sluka (Mitglied) | 3 min |
| 2 | Vertrauliches bleibt vertraulich | Uta Ahrens (sachkundige Bürgerin), Felix Bäumer (Helfer) | 3 min |
| 3 | Anhang an einem Antrag | Dames Joud (Mitglied) | 2 min |
| 4 | Wiki-Seite aus Vorlage anlegen und verlinken | Dames Joud (Mitglied) | 5 min |
| 5 | Suchen und die KI fragen | Steve Sterthues (Mitglied) | 3 min |
| 6 | KI-Vorschläge prüfen und entscheiden | Johannes Wilp (Vorstand) | 6 min |
| 7 | KI-Schnittstelle (technisch) | – | 3 min |

## 1. Datei im Ausschussordner finden und ablegen

- **Startpunkt:** **Dateien** → Ordner „Haupt- und Finanzausschuss“.
- **Aufgabe:** Öffnen Sie die Haushalts-Eckdaten und legen Sie eine eigene PDF in denselben Ordner.
- **Erwartung:** Der Download startet im neuen Tab. Nach dem Upload steht die Datei oben in der Liste mit Ihrem Namen;
  eine `.exe` wird mit Begründung abgewiesen.
- **Zeit:** ____ min. **Beobachtungen:**

## 2. Vertrauliches bleibt vertraulich

- **Aufgabe:** Melden Sie sich als Uta Ahrens an und öffnen Sie den HFWA-Ordner. Danach als Felix Bäumer **Dateien**.
- **Erwartung:** Uta sieht die Eckdaten, aber nicht die vertrauliche Stellungnahme zur Badsanierung. Felix sieht nur
  den Ordner „Wahlkampf“.
- **Zeit:** ____ min. **Beobachtungen:**

## 3. Anhang an einem Antrag

- **Startpunkt:** **Anträge** → „Elternbefragung vor Schließung von Grundschulstandorten“.
- **Aufgabe:** Hängen Sie eine Datei an.
- **Erwartung:** Der Anhang erscheint unter „Anhänge“ und im Ordner „Anträge“ (mit Bezug „Antrag“).
- **Zeit:** ____ min. **Beobachtungen:**

## 4. Wiki-Seite aus Vorlage anlegen und verlinken

- **Startpunkt:** **Wiki** → „Neue Seite“.
- **Aufgabe:** Legen Sie „Badsanierung“ im Bereich Politikfelder mit der Vorlage „Politikfeld“ an. Schreiben Sie einen
  Satz und verweisen Sie mit `[[Haushaltsberatungen]]` und `[[Bäderkonzept]]`.
- **Erwartung:** Die Seite öffnet sich im Bearbeiten-Modus mit der Vorlage. Nach dem Speichern: Version 2, unter
  „Fehlende Seiten“ steht „Bäderkonzept anlegen“, auf „Haushaltsberatungen“ erscheint „Badsanierung“ als Rückverweis.
  Eine Seite „bad-sanierung“ lässt sich nicht noch einmal anlegen.
- **Zeit:** ____ min. **Beobachtungen:**

## 5. Suchen und die KI fragen

- **Startpunkt:** **Wiki**.
- **Aufgabe:** Suchen Sie nach „Grundschule“. Fragen Sie dann „Welche Fristen gelten für Anträge?“ mit „KI fragen“.
- **Erwartung:** Treffer mit markiertem Ausschnitt; die Antwort nennt „Antrag stellen“ als Quelle. Die vertrauliche
  Seite „Personalfragen der Fraktion“ taucht bei Mitgliedern nirgends auf.
- **Zeit:** ____ min. **Beobachtungen:**

## 6. KI-Vorschläge prüfen und entscheiden

- **Startpunkt:** **Wiki** → „KI-Vorschläge“ → „KI jetzt prüfen lassen“.
- **Aufgabe:** Nehmen Sie den Vorschlag zu „Haushaltsberatungen“ (veraltet) an und lehnen Sie einen anderen ab.
- **Erwartung:**
  - Vorschläge: Dublette „Presse-Arbeit“/„Pressearbeit“, veraltete Angaben (Stand 2024 bzw. „ab 2023“), Lücke
    „Grundschullandschaft“ (neuere Fraktionsbeschlüsse), verwaiste Seiten.
  - Vorher-nachher ist farbig markiert. Nach dem Annehmen hat die Seite eine neue Version „KI-Vorschlag“.
  - Wird eine Seite nach dem Vorschlag von Hand geändert, ist „Annehmen“ gesperrt und erklärt.
  - Als Mitglied ist „Annehmen“ gesperrt mit Begründung.
- **Zeit:** ____ min. **Beobachtungen:**

## 7. KI-Schnittstelle (technisch)

```bash
curl -s -H "Authorization: Bearer demo-ki-schluessel" http://localhost:5290/api/wiki/suche?q=Haushalt
```

```bash
curl -s -H "Authorization: Bearer demo-ki-schluessel" -H "Content-Type: application/json" -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}" http://localhost:5290/mcp
```

- **Erwartung:** Ohne Schlüssel 401. Die Seitenliste enthält keine vertraulichen Seiten. Ein Vorschlag über
  `wiki_vorschlagen` erscheint unter „KI-Vorschläge“ mit Absender „KI-Schnittstelle (MCP)“.
- Ein MCP-Client (z. B. Claude Desktop) kann `http://localhost:5290/mcp` mit dem Bearer-Schlüssel als HTTP-Server
  einbinden.
