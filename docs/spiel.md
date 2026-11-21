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
- Screenshots aller Szenen: [`docs/spiel-screenshots/`](spiel-screenshots/); Szenenbilder geänderter Szenen (15–19) erzeugt
  `node tools/Grafik/szenenbilder.mjs` aus denselben Regeln wie das Spiel.
- Zu Spielbeginn stellen sich Joe, Chani und Professor Relbeil mit ihren Fähigkeiten vor (Esc überspringt; im Pausemenü „Figuren vorstellen“).
- Joes Handy (immer im Inventar): Antippen oder „Benutze“ ruft Professor Relbeil an (fiktive Figur). „Guten Tag!“ = Plauderei,
  „Ich brauche deine Hilfe!“ = der nächste Schritt, abgeleitet aus dem Spielstand und der Komplettlösung (kostet keine Punkte).
- Wer während eines offenen Textes Verben, Figuren oder Gegenstände anklickt, bekommt kurz den Hinweis „Erst zu Ende lesen“.
- Figuren verschwinden oder wechseln den Platz erst, wenn der zugehörige Text gelesen ist. Pinsel und Ledeiw bleiben nach dem
  Festkleben im Archiv verkleinert und abgedunkelt im Hintergrund stehen (mit schwarzer Klebemasse unter den Schuhen), der Ausgang ist frei.
- Stadtplan: Station 4 ist das Hallenbad Greven (OSM-Gebäude „Hallenbad Greven“, Am Hallenbad 3); das Freibad steht ohne Nummer auf der Karte.
  Station 3 ist der Emsdeich mit Skulpturen und Kanuverleih, ein Stück flussauf (südlich) vom Hallenbad.

## Bedienung

- **Verbleiste** unten: Gehe zu, Schau an, Nimm, Benutze, Rede mit, Gib und die zwei Fähigkeiten der aktiven Figur; die
  Satzzeile zeigt, was passiert („Gib SPD-Antrag „Tag der offenen Tür“ an Genosse Chris“). Gegenstände: Verb „Benutze“ oder „Gib“
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
- **Minispiele:** Bootsflucht die Ems hinab (↑/↓ oder oben/unten tippen; Treibgut von vorn, alternative Fakten von hinten, drei Treffer
  erlaubt; sind Pinsel und Ledeiw die Fakten ausgegangen, geht ihr Motorboot unter) und die
  rollende Wahlkugel (tippen/Leertaste/→ für Tempo; mit Lorettes Lastenrad leichter). Verloren = einfach noch einmal.

## Komplettlösung

Ausführlich mit allen Texten und Regieanweisungen: [`spiel-loesungsweg.md`](spiel-loesungsweg.md) (erzeugt mit `node tools/Grafik/loesungsweg.mjs`).

1. **Zukunftsschmiede (Parteibüro, Marktstraße):** Umschlag nehmen. Als **Joe** das Faxgerät benutzen (Stadtplan 1949). Regal benutzen
   (Geschäftsordnung), Briefkasten benutzen (**SPD-Antrag** „Tag-der-offenen-Tür-Ordnung für öffentliche Gebäude“, vorab mit der CDU-Fraktion abgestimmt),
   Garderobe benutzen (Taschenlampe). Tür → Karte.
2. **Rathausarchiv:** Genosse **Chris** (SPD) erinnert sich an keine Absprache und blockiert die Archivtür. Ihm seinen eigenen **SPD-Antrag** geben →
   er erinnert sich, schließt auf und geht. Gleich danach poltern Pinsel und Ledeiw herein und versperren den Ausgang. **Chani:** Gemälde „Der Rat
   von 1949“ deuten → „Protokoll 47“. Regal mit Ratsprotokollen benutzen → **Siegel der Beschlüsse**. **Joe**, Kartusche auf die beiden → festgeklebt. Ausgang.
3. **Emsdeich am Kanuverleih** (flussauf vom Hallenbad, mit Skulpturen): Johanna (sie hat den Kanuverleih „vergesellschaftet“) den **Stadtplan** geben →
   Paddel. **Chani:** Inschrift an der Steinskulptur deuten → „in der dritten Laterne, flussab, wo das Wasser warm ist“. Ruderboot benutzen →
   **Bootsflucht**: Pinsel und Ledeiw haben sich aus dem Dichtstoff befreit (auf Socken) und schießen von hinten alternative Fakten; als sie keine mehr haben,
   geht ihr Boot unter. Ihr treibt bis unter die Radbrücke beim Hallenbad: dritte Laterne benutzen → **Siegel der Ausführung**. Es geht direkt an der Baustelle weiter.
4. **Baustelle Hallenbad** (Sanierung; das Freibad ist nur beschriftet und nicht im Umbau): **Joe:** Kartusche auf die leckende Leitung (Grube läuft leer).
   **Joe:** Verkaufsgespräch mit Frau Kemper → Helme. Baugrube benutzen → **Siegel des Rechts**. **Chani:** Live-Story bei Pinsel und Ledeiw → sie
   stolpern in den Bauwagen. Bauzaun-Tor.
5. **Martin-Luther-Grundschule:** Schulhofuhr benutzen (bis 20:30 warten, Fledermäuse fliegen aus). Mit Lorette reden → Lastenrad (zusammengefaltet, in Chanis Tasche; im Gang wieder ausgeklappt). Durch den Haupteingang
   in den Schulflur (Klassenzimmer 1a, 2b und WC mit Absagen; die Schule ist zweizügig), dort die Kellertür benutzen (nach 20:30 Uhr, Taschenlampe nötig).
6. **Krypta:** **Chani:** Wandfresko deuten → „von unten nach oben“. Symbolschloss: **Urne, Buch, Waage** → Tempelschlüssel. Tor benutzen (drei Siegel + Schlüssel).
7. **Tempel:** Zwei Balance-Plattformen auf Drehzapfen: **Chani** auf die dicke rechte (Kamera), **Joe** auf die schmale linke (Hut). Jede Figur steht
   allein und verkleinert im Hintergrund auf ihrer Plattform; die andere bewegt sich unabhängig → Fallgitter offen. Tafel der Geschäftsordnung:
   **Sofort, vor der Sache selbst** – **Die Bürgermeisterin oder der Bürgermeister** – **Die Mehrheit der abgegebenen Stimmen**. Bis zur Brücke bleiben
   Joe und Chani links des Abgrunds. Abgrund: **Antrag zur Geschäftsordnung** → Hängebrücke. Gang benutzen → **Wahlkugel** entkommen.
8. **Finale:** Den goldenen Pokal ruhig ansehen (Kegelpokal von 1987). Die **schlichte Holzkiste** benutzen – der Gral der Demokratie ist eine
   Wahlurne mit einem Bleistift an der Schnur. Sie lässt sich nur gemeinsam tragen.

## Technik und Tests

**Tests geben nie Ton aus.** Automatisierte oder ferngesteuerte Browser starten stumm (Chromium/Edge mit `--mute-audio`,
Playwright mit `args: ['--mute-audio']`) und öffnen das Spiel mit `?ton=aus` (`/spiel/?ton=aus`): Dann bleiben Melodie und
Effekte aus, ohne die gespeicherte Ton-Einstellung zu ändern. Test-Tabs mit dem Spiel nach dem Lauf schließen.

- Reines HTML/CSS/JavaScript ohne Bibliotheken: `logik.js` (Zustand, Szenen, Rätsel, Minispiele, Speichern – ohne
  Browser testbar), `darstellung.js` (Canvas-Grafik im Code, Eingabe, WebAudio-Töne, Speicherplätze), `spiel.css`.
  CSP-konform (keine Inline-Skripte).
- Tests: `tests/FraktionPerfekt.Web.Tests/spiel/logik.test.cjs` (node --test; ebenso `spiel3d.test.cjs`), eingebunden in `dotnet test` über
  `SpielTests` (ohne Node übersprungen). Geprüft u. a.: Die Komplettlösung führt bis zum Ende, Rätsel (Schloss, Gitter,
  Quiz), Speichern/Laden mit Abwehr fremder Daten, Speicher, der Ausnahmen wirft, und beide Minispiele.

## 3D-Fassung (`/spiel3d/`, alle Akte)

Zweite Darstellung desselben Spiels mit three.js (0.170, lokal mitgeliefert, CSP-konform ohne Import-Map). Spiellogik,
Speicherformat, Komplettlösung und Professor Relbeil sind identisch: `/spiel3d/` lädt `logik.js`, `grafik.js` (Porträts,
Inventar-Icons, Reisekarte), `musik.js` und `spiel.css` aus `/spiel/`. Speicherplätze heißen `fp-spiel3d-…`.

- Schauplätze: Eine Szene der Spiellogik besteht in 3D aus einem oder mehreren begehbaren Bereichen
  (`Spiel3d/bereiche.js`, `akt3.js`, `akt45.js`, Innenräume in `innen.js`):
  `buero` = Vor der Zukunftsschmiede + Büro; `archiv` = Vor dem Rathaus + Rathausflur + Rathausarchiv;
  `ems` = Emsdeich am Kanuverleih; `baustelle` = Radbrücke beim Hallenbad (gelbe Doppellaternen, in der dritten das
  Siegel) + Baustelle; `schule` = Schulhof (abends mit Fledermäusen); `schulflur`; `krypta`; `tempel` = Tempelhalle
  (Fallgitter, Plattformen, Tafel, Abgrund mit Hängebrücke, Gang) + Kammer des Grals.
  Gebäude aus 3D-Gebäudemodellen LoD2 (© Geobasis NRW, dl-de/zero-2-0) mit Gelände; Innenräume frei erfunden.
- Minispiele in 3D mit derselben Simulation wie die Pixel-Fassung: Bootsflucht auf der Ems (bewusst gutmütig: weniger
  Treibgut, langsame und seltene alternative Fakten) und Wahlkugel im Tempelgang – mit Lorettes Lastenrad, das Chani
  zusammengefaltet in der Tasche trägt.
- Texte, die nur in 3D anders sind (`Spiel3d/anpassung3d.js`): Chris steht vor dem Rathaus – er wollte selbst
  Bürgermeister werden, gewählt wurde Dietrich Aden (CDU), jetzt lässt er niemanden hinein; „Rathaustür“, Wegweiser.
- Direkteinstieg: `/cheatsheet` (Spickzettel) bzw. `/spiel3d/?start=…` spielt die Komplettlösung bis dorthin nach.
- Tests: `tests/FraktionPerfekt.Web.Tests/spiel/spiel3d.test.cjs` (Komplettlösung mit den 3D-Texten, jeder Hotspot jeder
  Szene hat ein 3D-Gegenstück, Direkteinstiege und Spickzettel, keine Inline-Skripte, nur lokale Importe). Browsertests
  stumm mit `/spiel3d/?ton=aus`; mit Software-Rendering zusätzlich `&test` (große Zeitschritte, Minispiele mit Autopilot).

## Grafiken im Stil der Vorlagen (Freigabe ausstehend)

`node tools/Grafik/erzeuge.mjs` erzeugt alle fehlenden Grafiken nach `assets/` (256×144, Figuren 6 × 32×64), passend zu
Klaus' Vorlagen. Übersicht: `docs/spiel-screenshots/grafik-uebersicht.png`, Liste: `docs/spiel-grafikliste.md`.

Die Reisekarte basiert auf Daten von **OpenStreetMap** (© OpenStreetMap-Mitwirkende, Lizenz ODbL 1.0). Die Daten wurden
einmalig per Overpass-API abgerufen (`tools/Grafik/daten/abfrage.overpassql`) und als `greven-osm-kompakt.json`
gespeichert; die Namensnennung steht auf der Karte und im Abspann.

## Erkennungsmelodie

Eigene Abenteuer-Fanfare (Blechbläser, punktierter Marsch, C-Dur), darin als würdiges Zitat die erste Phrase von Joseph
Haydns Melodie von 1797 (gemeinfrei; Melodie der Nationalhymne), unverändert und ohne Text, dann Fanfaren-Schluss mit
Pauke. Erzeugt per WebAudio (`Spiel/musik.js`). Sie läuft auf dem Titelbild und kurz im Finale und lässt sich dort und im
Pausemenü abschalten und leiser stellen. Filmmusik wird nicht zitiert.
