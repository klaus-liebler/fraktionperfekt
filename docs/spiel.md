# Easteregg: „Joe & Chani und der Gral der Demokratie“

Ein kleines Abenteuer im Stil der Pulp-Abenteuer der 30er-Jahre (Hut, Lederjacke, Karte mit roter Reiselinie, Tempel,
Fallen, rollende Kugel), mit eigenen Figuren, Rätseln und Grafiken. Alle Personen sind erfunden; Parteien erscheinen nur
als Karikatur-Typen. Spielzeit etwa 20–40 Minuten.

## Einstieg

- **Direkt:** <http://localhost:5290/spiel/> (nach der Anmeldung).
- **Versteckt:** auf einer beliebigen Seite (angemeldet) den Konami-Code tippen: ↑ ↑ ↓ ↓ ← → ← → B A – oder siebenmal
  schnell auf das Logo oben links tippen (auch in der Handy-Ansicht).
- Ohne Anmeldung leitet `/spiel/` zur Anmeldung um; die Dateien liegen nicht in `wwwroot`, sondern in
  `src/FraktionPerfekt.Web/Spiel/` und werden nur über den geschützten Endpunkt ausgeliefert.
- Nach dem Aktualisieren des Codes die laufende Anwendung **neu starten**.

## Grafik (ab Iteration 10)

Look der Point-and-Click-Adventures der frühen 90er, aber komplett eigene Gestaltung: interne Auflösung 320×200,
ganzzahlig hochskaliert, feste Palette, Verläufe und Licht per geordnetem Dithering, alles im Code erzeugt
(`pixel.js` Palette und Pixelpuffer, `figuren.js` Figuren, Porträts und Icons, `szenen.js` Hintergründe, Animationen,
Karte, Minispiele). Joe und Chani sind nach Fotos stilisiert; die Fotos liegen nicht im Repository.

- Figuren ca. 58 px mit Laufanimation (6 Bilder, links/rechts), Stehen mit Blinzeln, Reden, Benutzen.
- Porträts 96×96 im Textfeld mit Mimik (neutral, lacht, erschrocken).
- Szenen mit Animationen: blinkendes Fax, Staub im Archiv, Wasser und Enten auf der Ems, tropfendes Leck, Fledermäuse,
  flackernde Fackeln; leichte Parallaxe; Irisblende beim Szenenwechsel; Reisekarte mit roter Reiselinie.
- Screenshots aller Szenen: [`docs/spiel-screenshots/`](spiel-screenshots/).

## Bedienung

- **Verbleiste** unten: Gehe zu, Schau an, Nimm, Benutze, Rede mit, Gib und die zwei Fähigkeiten der aktiven Figur; die
  Satzzeile zeigt, was passiert („Benutze Mustertagesordnung mit Genosse Christian“). Gegenstände: Verb „Benutze“ oder „Gib“
  wählen, dann Icon im Inventar, dann das Ziel.
- **Tippen/Klicken** in der Szene: Die Figur läuft hin und führt das gewählte Verb aus; mit „Gehe zu“ die naheliegende
  Aktion (Personen ansprechen, Ausgänge benutzen, sonst ansehen). **Langes Drücken** öffnet das Verbmenü am Objekt.
  Gespräche mit Personen bieten Antworten zur Auswahl.
- **Tastatur:** 1–8 Verb, ←/→ Dinge auswählen, Enter ausführen, Tab oder F Figur wechseln, Enter/Leertaste Text weiter,
  Esc oder P Pause.
- **Figuren** (unten umschalten): **Joe** – Kartusche (abdichten, festkleben), Verkaufsgespräch. **Chani** – Deuten
  (Inschriften, Gemälde, Wappen), Live-Story (lenkt ab).
- **Pause-Menü:** Speichern auf Platz 1–3, Laden (auch Autospeicher bei jedem Szenenwechsel), Ton an/aus, neues Spiel.
  Spielstände liegen im `localStorage` des Browsers; ohne Speicher läuft das Spiel trotzdem.
- **Minispiele:** Bootsverfolgung auf der Ems (↑/↓ oder oben/unten tippen, 35 Sekunden, drei Treffer erlaubt) und die
  rollende Wahlkugel (tippen/Leertaste/→ für Tempo; mit Lores Lastenrad leichter). Verloren = einfach noch einmal.

## Komplettlösung

1. **Zukunftsschmiede (Parteibüro, Marktstraße):** Umschlag nehmen. Als **Joe** das Faxgerät benutzen (Stadtplan 1949). Regal benutzen
   (Geschäftsordnung), Postfach benutzen (Mustertagesordnung), Garderobe benutzen (Taschenlampe). Tür → Karte.
2. **Rathausarchiv:** Christian die **Mustertagesordnung** geben (Arbeitskreis „Archivöffnung“ wird gegründet, Tür offen).
   **Chani:** Gemälde „Der Rat von 1949“ deuten → „Protokoll 47“. Regal mit Ratsprotokollen benutzen → **Siegel der
   Beschlüsse**. Hohlbein und Dröhn versperren den Ausgang: **Joe**, Kartusche auf die beiden → festgeklebt. Ausgang.
3. **Ems-Ufer:** Joanna den **Stadtplan** geben (Umverteilung) → Paddel. **Chani:** Inschrift an der Brücke deuten →
   „dritter von sieben Pfählen“. Boot benutzen → **Bootsverfolgung** schaffen. Sieben Pfähle benutzen → **Siegel der
   Ausführung**. Weg zur Straße.
4. **Baustelle Badsanierung:** **Joe:** Kartusche auf die leckende Leitung (Grube läuft leer). **Joe:**
   Verkaufsgespräch mit Frau Kemper → Helme. Baugrube benutzen → **Siegel des Rechts**. **Chani:** Live-Story bei
   Hohlbein und Dröhn → sie stolpern in den Bauwagen. Bauzaun-Tor.
5. **Martin-Luther-Grundschule:** Schulhofuhr benutzen (bis 20:30 warten, Fledermäuse fliegen aus). Mit Lore reden →
   Lastenrad. Kellergang benutzen (Taschenlampe nötig).
6. **Krypta:** **Chani:** Wandfresko deuten → „von unten nach oben“. Symbolschloss: **Urne, Buch, Waage** →
   Tempelschlüssel. Tor benutzen (drei Siegel + Schlüssel).
7. **Tempel:** Mit einer Figur auf die linke Bodenplatte, Figur wechseln, mit der anderen auf die rechte → Fallgitter
   offen. Tafel der Geschäftsordnung: **Sofort, vor der Sache selbst** – **Die Bürgermeisterin oder der Bürgermeister** –
   **Die Mehrheit der abgegebenen Stimmen**. Abgrund: **Antrag zur Geschäftsordnung** → Brücke. Gang benutzen →
   **Wahlkugel** entkommen.
8. **Finale:** Den goldenen Pokal ruhig ansehen (Kegelpokal von 1987). Die **schlichte Holzkiste** benutzen – der Gral
   der Demokratie ist eine Wahlurne mit einem Bleistift an der Schnur. Sie lässt sich nur gemeinsam tragen.

## Technik und Tests

- Reines HTML/CSS/JavaScript ohne Bibliotheken: `logik.js` (Zustand, Szenen, Rätsel, Minispiele, Speichern – ohne
  Browser testbar), `darstellung.js` (Canvas-Grafik im Code, Eingabe, WebAudio-Töne, Speicherplätze), `spiel.css`.
  CSP-konform (keine Inline-Skripte).
- Tests: `tests/FraktionPerfekt.Web.Tests/spiel/logik.test.cjs` (node --test), eingebunden in `dotnet test` über
  `SpielTests` (ohne Node übersprungen). Geprüft u. a.: Die Komplettlösung führt bis zum Ende, Rätsel (Schloss, Gitter,
  Quiz), Speichern/Laden mit Abwehr fremder Daten, Speicher, der Ausnahmen wirft, und beide Minispiele.
