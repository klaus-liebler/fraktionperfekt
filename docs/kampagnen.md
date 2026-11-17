# Kampagnen (früher „Wahlkampf“)

Eine **Kampagne** (Wahlkampf, Haustüraktion, Themenkampagne) hat einen Namen, einen Zeitraum, eine Beschreibung (Editor), einen
Status (Planung, aktiv, abgeschlossen) und optional einen **Wahltag**. Anlegen und ändern dürfen Vorstand und Geschäftsführung
(„Neue Kampagne“ öffnet den großen Dialog). Die alte Adresse `/wahlkampf` leitet auf `/kampagnen` um.

Je Kampagne gibt es die Reiter:

- **Übersicht**: Kennzahlen, Beschreibung, nächste sinnvolle Schritte (eigene Tickets, freie Termine, offene Gebiete, Abhängen nach dem Wahltag).
- **Plakate** (Karte, Liste) und **Abhängliste**: Am Handy (`handy/plakat`) landen neue Plakate in der laufenden Kampagne (jüngste aktive).
  Status „beschädigt“ legt automatisch ein Ticket mit Plakatbezug an (Priorität hoch, fällig in 3 Tagen), wenn noch keins offen ist.
- **Gebiete**: zuteilen (Vorstand, Geschäftsführung, mit Push), abhaken (zugeteilte Person).
- **Termine**: Ein Kampagnentermin ist ein **normaler Kalendertermin** (Art „Kampagne“) mit Kampagnenbezug und optional begrenzten Plätzen.
  **Einbuchen = Zusage** im Kalender (dieselbe Teilnahme wie bei allen Terminen); ist der Termin voll, ist „Einbuchen“ gesperrt und erklärt.
  Eingebuchte Termine stehen im eigenen Kalender und im Kalender-Abo. Die früheren „Schichten“ gibt es nicht mehr.
  Die Besetzungsanfrage (N Plätze für Vertreter:innen der Fraktion) bleibt ein eigener Ablauf mit Einladung und Bericht.
- **ToDo-Liste je Termin**: Text, zuständig (optional), erledigt mit wer/wann. **ToDo-Vorlagen** (Name + Einträge, z. B. „Infostand“)
  werden auf der Kampagnenliste gepflegt; beim Anlegen eines Termins oder später am Termin: „ToDos aus Vorlage erzeugen“.
  Vorlagen sind **Kopiervorlagen**: Die erzeugten ToDos sind eigenständige Datensätze und ändern sich nicht, wenn die Vorlage geändert wird
  (keine Kopie im Sinne der Datenbank-Regeln, sondern bewusst neue Vorgänge).
- **Tickets**: fortlaufende Nummer je Kampagne, Titel, Beschreibung (Editor, Bilder per Strg+V), Status (neu, in Arbeit, wartet, erledigt),
  Priorität, zuständig (Benachrichtigung per Push und Mail über den Postausgang; Demo-Schutz gilt), Fälligkeit, optional Bezug auf
  Gebiet, Plakat oder Termin, Kommentare. Ansicht als Liste oder Kanban, Filter „meine“. Änderungen stehen im Protokoll.

Rollen: Die Rolle „Helferin“ heißt in der Oberfläche **Kampagnenteam** (technischer Name unverändert). Das Kampagnenteam sieht Kampagnen,
öffentliche Termine und Kampagnentermine, bucht sich ein, hakt ToDos ab, arbeitet an Tickets und darf dort (und in Kampagnenbeschreibungen)
Bilder einfügen – sonst gelten die bisherigen Sperren (keine Anträge, Sitzungen usw.).

Tabellen: `Kampagnen`, `Plakate.KampagneId`, `Gebiete.KampagneId`, `Termine.KampagneId`/`Plaetze`, `TerminTodos`, `TodoVorlagen`,
`TodoVorlagenPunkte`, `Tickets`, `TicketKommentare` (siehe `datenbank-schema.md`). Die Wahlkampfkasse in den Finanzen heißt weiter so
(Kasse der Partei, nicht an eine Kampagne gebunden).
