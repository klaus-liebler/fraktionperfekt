# Abnahme – Iteration 6: Politische Arbeit

Ablauf und Nachgespräch wie in [`abnahme-1.md`](abnahme-1.md). Dauer etwa 25 Minuten.

| Nr. | Aufgabe | Testkonto (Rolle) | Richtzeit |
|---|---|---|---|
| 1 | Anfrage schreiben und einreichen | Melanie Kock (Mitglied), Thomas Brinkmann (Vorstand) | 4 min |
| 2 | Überfällige Anfrage nachhaken | Katrin Albers (Administration) | 2 min |
| 3 | Beschlusslage recherchieren | Lena Hagedorn (sachkundige Bürgerin) | 3 min |
| 4 | Bürgeranliegen erfassen, zuweisen, beantworten | Jens Overmeyer, Katrin Albers, Dirk Schulze-Eling | 6 min |
| 5 | Pressemitteilung mit Freigabe | Markus Terhaar (Mitglied), Sabine Hölscher (Vorstand) | 5 min |
| 6 | Pressespiegel und Kontakte | Katrin Albers (Administration) | 3 min |

## 1. Anfrage schreiben und einreichen

- **Startpunkt:** **Anfragen an die Verwaltung**.
- **Aufgabe:** Öffnen Sie als Melanie Kock den Entwurf „Raumbedarf für den Ganztag“. Versuchen Sie einzureichen. Reichen
  Sie danach als Thomas Brinkmann ein.
- **Erwartung:** Für Melanie ist „Einreichen“ gesperrt und erklärt („im Namen der Fraktion … Vorstand oder
  Geschäftsführung“). Beim Einreichen ist die Frist vier Wochen vorbelegt.
- **Zeit:** ____ min. **Beobachtungen:**

## 2. Überfällige Anfrage nachhaken

- **Startpunkt:** **Aufgaben** (Korb Administration).
- **Erwartung:** Nach der stündlichen Fristenprüfung liegt „Bei der Verwaltung nachhaken: Finanzierung der
  Badsanierung …“ im Korb; die Anfrage ist rot als „überfällig“ markiert. Nach „Antwort erfassen“ ist die Aufgabe erledigt.
- **Zeit:** ____ min. **Beobachtungen:**

## 3. Beschlusslage recherchieren

- **Startpunkt:** **Beschlusslage**.
- **Aufgabe:** Was haben Fraktion, Schulausschuss und Rat zur Grundschullandschaft beschlossen?
- **Erwartung:** Suche „Grundschul“ zeigt Fraktionsbeschlüsse (mit Stimmen und Link zum Protokoll) und
  RIS-Beschlüsse (vertagt, zur Kenntnis genommen). Filter nach Gremium und Jahr wirken sofort.
- **Zeit:** ____ min. **Beobachtungen:**

## 4. Bürgeranliegen erfassen, zuweisen, beantworten

- **Aufgabe:** Erfassen Sie als Jens Overmeyer ein Anliegen zum Politikfeld „Stadtentwicklung und Verkehr“. Weisen Sie
  es als Katrin Albers zu und beantworten Sie es als Dirk Schulze-Eling.
- **Erwartung:** Vorgeschlagen ist der Themenpate Dirk Schulze-Eling. Er bekommt Aufgabe und Push. „Beantwortet –
  erledigt“ ist erst mit Antworttext möglich; danach sind alle zugehörigen Aufgaben erledigt.
- **Zeit:** ____ min. **Beobachtungen:**

## 5. Pressemitteilung mit Freigabe

- **Aufgabe:** Schreiben Sie als Markus Terhaar eine Mitteilung mit Termin im Redaktionskalender und geben Sie sie zur
  Freigabe. Geben Sie sie als Sabine Hölscher frei. Ändern Sie danach den Text.
- **Erwartung:** Der Vorstand hat eine Aufgabe „Pressemitteilung freigeben“. Thomas Brinkmann kann seine eigene
  Mitteilung nicht freigeben (Vier-Augen-Prinzip, erklärt). Nach der Textänderung steht die Mitteilung wieder auf
  „Entwurf“.
- **Zeit:** ____ min. **Beobachtungen:**

## 6. Pressespiegel und Kontakte

- **Aufgabe:** Erfassen Sie einen Artikel zur Elternbefragung und finden Sie in **Kontakte** die Redaktion der
  Lokalzeitung.
- **Erwartung:** Der Artikel zählt als Resonanz bei der Mitteilung. Links ohne `http(s)://` werden abgewiesen.
- **Zeit:** ____ min. **Beobachtungen:**

## Technisch: echtes RIS (OParl)

In `appsettings.json` oder als Umgebungsvariablen:

```bash
Ris__Art=OParl Ris__OParl__Endpunkt=https://<ris-host>/oparl/v1 dotnet run --project src/FraktionPerfekt.Web
```

Der Abgleich läuft nachts (2:30 Uhr) und unter **Betrieb** auf Knopfdruck.
