# Easteregg: „Joe & Chani und Grevens Gral der Demokratie“

Ein kleines Abenteuer im Stil der Pulp-Abenteuer der 30er-Jahre (Hut, Lederjacke, Karte mit roter Reiselinie, Tempel,
Fallen, rollende Kugel), mit eigenen Figuren und Rätseln, in 3D an echten Greven-Schauplätzen. Alle Personen sind erfunden;
Parteien erscheinen nur als Karikatur-Typen. Spielzeit etwa 20–40 Minuten.

## Einstieg

- **Direkt:** <http://localhost:5290/spiel3d/> nach der Anmeldung. Die frühere Adresse `/spiel/` (Pixel-Fassung, entfernt)
  leitet dorthin um.
- **Versteckt:** auf einer beliebigen Seite (angemeldet) den Konami-Code tippen: ↑ ↑ ↓ ↓ ← → ← → B A – oder siebenmal
  schnell auf das Logo oben links tippen (auch in der Handy-Ansicht). Im Impressum steht ein Link.
- Ohne Anmeldung leitet `/spiel3d/` zur Anmeldung um; die Dateien liegen nicht in `wwwroot`, sondern in
  `src/FraktionPerfekt.Web/Spiel3d/` und werden nur über den geschützten Endpunkt ausgeliefert.
- Das Spiel braucht einen Browser mit WebGL.
- Nach dem Aktualisieren des Codes die laufende Anwendung **neu starten**.

## Spielablauf

- Zu Spielbeginn stellen sich Joe, Chani und Professor Relbeil mit ihren Fähigkeiten vor (Esc überspringt; im Pausemenü „Figuren vorstellen“).
- Joes Handy (immer im Inventar): Antippen oder „Benutze“ ruft Professor Relbeil an (fiktive Figur). „Guten Tag!“ = Plauderei,
  „Ich brauche deine Hilfe!“ = der nächste Schritt, abgeleitet aus dem Spielstand und der Komplettlösung (kostet keine Punkte).
- Wer während eines offenen Textes Verben, Figuren oder Gegenstände anklickt, bekommt kurz den Hinweis „Erst zu Ende lesen“.
- **Regie: erst der Grund, dann die Änderung.** Figuren und Dinge ändern sich erst mit der Textzeile, die den Grund nennt –
  nicht vorher und nicht erst am Ende des Textes. Figuren kommen und gehen dabei zu Fuß (Chris geht zum Ortsverein,
  Pinsel und Ledeiw drängen sich ins Rathaus, laufen rückwärts in den Bauwagen, Helferinnen und Helfer kommen ins Finale),
  festgeklebte Figuren rutschen an die Wand, Genommenes wandert zur Figur, Neues wächst ins Bild, und das Inventar
  füllt sich mit derselben Zeile. Siehe „Technik“.
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
- **Pause-Menü:** Speichern auf Platz 1–3, Laden (auch Autospeicher bei jedem Szenenwechsel), Musik, Ton an/aus, neues Spiel.
  Spielstände liegen im `localStorage` des Browsers (`fp-spiel3d-…`); ohne Speicher läuft das Spiel trotzdem.
- **Minispiele:** Bootsflucht die Ems hinab (↑/↓ oder oben/unten tippen; Treibgut von vorn, alternative Fakten von hinten, jeweils
  gut drei bzw. zwei Sekunden Zeit zum Ausweichen). Das Boot hat drei Herzen: Jeder Treffer meldet sich rot mit dem, was getroffen
  hat, und wie viele Treffer noch gehen; beim dritten kentert es und es geht mit drei Herzen von vorn los. Sind Pinsel und Ledeiw
  die Fakten ausgegangen (Anzeige oben), geht ihr Motorboot unter – gewonnen. Dazu die
  rollende Wahlkugel (tippen/Leertaste/→ für Tempo; mit Lorettes Lastenrad leichter). Verloren = einfach noch einmal.

## Komplettlösung

Ausführlich mit allen Texten und der Regie je Textzeile: [`spiel-loesungsweg.md`](spiel-loesungsweg.md) (erzeugt mit `node tools/Grafik/loesungsweg.mjs`).

1. **Zukunftsschmiede (Parteibüro, Marktstraße):** Umschlag nehmen. Als **Joe** das Faxgerät benutzen (Stadtplan 1949). Regal benutzen
   (Geschäftsordnung), Briefkasten vor der Tür benutzen (**SPD-Antrag** „Tag-der-offenen-Tür-Ordnung für öffentliche Gebäude“, vorab mit der
   CDU-Fraktion abgestimmt), Garderobe benutzen (Taschenlampe, Hut, Lederjacke). Wegweiser → Karte.
2. **Rathaus:** Genosse **Chris** (SPD) steht vor dem Eingang – er wollte selbst Bürgermeister werden, gewählt wurde Dietrich Aden (CDU).
   Ihm seinen eigenen **SPD-Antrag** geben → er schließt auf und geht zum Ortsverein. Gleich danach drängen sich Pinsel und Ledeiw vorbei ins
   Rathaus und versperren im Archiv die Stahltür. **Chani:** Gemälde „Der Rat von 1949“ deuten → „Protokoll 47“. Regal mit Ratsprotokollen
   benutzen → **Siegel der Beschlüsse**. **Joe**, Kartusche auf die beiden → festgeklebt an der Rückwand. Stahltür.
3. **Emsdeich am Kanuverleih** (flussauf vom Hallenbad, mit Skulpturen): Johanna (sie hat den Kanuverleih „vergesellschaftet“) den **Stadtplan** geben →
   Paddel. **Chani:** Inschrift an der Steinskulptur deuten → „in der dritten Laterne, flussab, wo das Wasser warm ist“. Ruderboot benutzen →
   **Bootsflucht**: Pinsel und Ledeiw haben sich aus dem Dichtstoff befreit (auf Socken) und schießen von hinten alternative Fakten; als sie keine mehr haben,
   geht ihr Boot unter. Ihr treibt bis unter die Radbrücke beim Hallenbad. Den Deich hinauf und an der Deichkrone auf die Brücke: dritte Laterne
   benutzen → **Siegel der Ausführung**. Es geht direkt an der Baustelle weiter.
4. **Baustelle Hallenbad** (Sanierung; das Freibad ist nur beschriftet und nicht im Umbau): **Joe:** Kartusche auf die leckende Leitung (Leck dicht,
   die Grube läuft leer). **Joe:** Verkaufsgespräch mit Frau Kemper → Helme. Baugrube benutzen → **Siegel des Rechts**. **Chani:** Live-Story bei
   Pinsel und Ledeiw → sie laufen rückwärts in den Bauwagen. Bauzaun-Tor.
5. **Martin-Luther-Grundschule:** Schulhofuhr benutzen (bis 20:30 warten, Fledermäuse fliegen aus). Mit Lorette reden → Lastenrad (zusammengefaltet, in
   Chanis Tasche; im Gang wieder ausgeklappt). Durch den Haupteingang in den Schulflur (Klassenzimmer 1a – abends leer –, 2b mit Elternabend und
   WC), dort die Kellertür benutzen (nach 20:30 Uhr, Taschenlampe nötig).
6. **Krypta:** **Chani:** Wandfresko deuten → „von unten nach oben“. Symbolschloss: **Urne, Buch, Waage** → Tempelschlüssel. Tor benutzen (drei Siegel + Schlüssel).
7. **Tempel:** Zwei Balance-Plattformen auf Drehzapfen: **Chani** auf die dicke rechte (Kamera), **Joe** auf die schmale linke (Hut); jede Figur steht
   allein auf ihrer Plattform → Fallgitter offen. Tafel der Geschäftsordnung: **Sofort, vor der Sache selbst** – **Die Bürgermeisterin oder der
   Bürgermeister** – **Die Mehrheit der abgegebenen Stimmen**. Abgrund: **Antrag zur Geschäftsordnung** → Hängebrücke. Gang benutzen →
   **Wahlkugel** entkommen.
8. **Finale:** Den goldenen Pokal ruhig ansehen (Kegelpokal von 1987). Die **schlichte Holzkiste** benutzen – der Gral der Demokratie ist eine
   Wahlurne mit einem Bleistift an der Schnur. Sie lässt sich nur gemeinsam tragen; wer unterwegs geholfen hat, kommt dazu.

## Technik

three.js (0.170, lokal mitgeliefert, CSP-konform ohne Import-Map und ohne Inline-Skripte). Alle Dateien liegen in
`src/FraktionPerfekt.Web/Spiel3d/`:

- `logik.js`: Zustand, Szenen, Texte, Rätsel, Minispiele, Speichern, Komplettlösung, Professor Relbeil – ohne Browser testbar.
- `darstellung3d.js`: Darstellung, Eingabe, Wegsuche, Text und Regie, Karte, Minispiele, Menü, WebAudio-Töne;
  `musik.js` (Erkennungsmelodie), `spiel.css` und `spiel3d.css`.
- Schauplätze: Eine Szene der Spiellogik besteht aus einem oder mehreren begehbaren Bereichen
  (`bereiche.js`, `akt3.js`, `akt45.js`, Innenräume in `innen.js`):
  `buero` = Vor der Zukunftsschmiede + Büro; `archiv` = Vor dem Rathaus + Rathausflur + Rathausarchiv;
  `ems` = Emsdeich am Kanuverleih; `baustelle` = Radbrücke beim Hallenbad (gelbe Doppellaternen oben auf der Brücke, in der
  dritten das Siegel; die Brücke endet wie in echt auf den Deichkronen, dort geht es aufs Deck; im Hintergrund die Stadt mit
  dem Hallenbad aus OpenStreetMap, lagerichtig zur echten Brücke: `node tools/Welten/umgebung.mjs` → `umgebung_radbruecke.json`;
  die Spielbrücke ist kürzer als die echte, Gebäude jenseits der Brückenenden rücken um die Differenz heran) + Baustelle; `schule` = Schulhof (abends mit Fledermäusen); `schulflur`; `krypta`; `tempel` = Tempelhalle
  (Fallgitter, Plattformen, Tafel, Abgrund mit Hängebrücke, Gang) + Kammer des Grals.
  Gebäude aus 3D-Gebäudemodellen LoD2 (© Geobasis NRW, dl-de/zero-2-0) mit Gelände; Innenräume frei erfunden.
  Begehbar ist nur, wo die Kamera die Figuren sieht: Vor dem Rathaus etwa endet der Platz an der Fassadenlinie und nicht
  rechts am Gebäude entlang (sonst verdecken Stützen und Glaswände Joe und Chani).

### Regie

Die Spiellogik ändert ihren Zustand sofort; jede Antwort sagt aber, **welche Textzeile welche Änderung begründet**: Eine Zeile
darf als `[text, schlüssel …]` stehen, Schlüssel sind Flags, Gegenstände oder `ende` (`antwort()` in `logik.js` macht daraus
`{ text, regie: { zeile: [schlüssel] } }`). Wo eine Änderung mehrere Schritte hat, gibt es eigene Flags (`leckDicht` vor
`grubeTrocken`, `dichtstoffArchiv` vor `gegnerArchivFest`, `gegnerBaustelle`, `gegnerFinale`, `pokalGenommen`, `urneGetragen`,
`chrisHilft`, `loretteHilft`, `johannaHilft`, `gegnerAbgang`, `klappeAuf`, `schlossOffen`); ältere Spielstände bekommen sie
beim Laden nachgetragen.

Die Darstellung zeigt einen **angezeigten Spielstand** (`bild`): Nach einer Aktion übernimmt er alles außer den
Regie-Schlüsseln; jeder Schlüssel kommt erst mit seiner Zeile, am Ende des Textes ist `bild` wieder gleich der Logik.
Figuren, Dinge, Helme, Hut, Inventar und der Abspann richten sich nur nach `bild`. Übergänge:

- Nebenfiguren (`npcs` der Bereiche): `von` = Auftrittsort (kommt zu Fuß herein), `nach` = Abgangsort oder Wegpunkte
  (geht hinaus, `rueckwaerts` möglich), `lage(bild)` = anderer Platz im selben Bereich (z. B. festgeklebt, gleitet dorthin),
  `durchzug(bild)` + `weg` = läuft einmal vorbei (Pinsel und Ledeiw ins Rathaus). Beim Betreten eines Bereichs steht alles
  ohne Übergang an seinem Platz.
- Dinge: Hotspots mit `sichtbarObj(bild)` und `dinge` der Bereiche – Genommenes wandert zur aktiven Figur und schrumpft
  (`nehmen`) oder schwindet an Ort und Stelle (`schwinden`), Neues wächst ins Bild.

Der Test „Regie“ in `logik.test.cjs` probiert an jeder Stelle der Komplettlösung jede Aktion an jedem Hotspot mit beiden
Figuren (samt Dialogen und Minispiel-Enden) und verlangt, dass jede Änderung an Flags, Inventar und Ende einer Textzeile
zugeordnet ist.

### Figuren

Alle Figuren sind realistische Modelle aus **Microsoft Rocketbox** (MIT, Lizenztext in `tools/Figuren/LICENSE-Rocketbox.md`)
mit Rocketbox-Animationen je Pose (stehen, gehen, rennen, reden, benutzen, erschrocken, sitzen, arme):

| Figur | Rocketbox | Anpassung |
|---|---|---|
| Joe | `Male_Adult_03` | Hut, randlose Brille, Schnauzer; „Lederjacke an“ färbt die Sakkotextur zur Laufzeit braun |
| Chani | `Female_Adult_07` | – |
| Chris | `Gardener_Male_01` | Arbeitsjacke rot umgefärbt (Strickjacke), Brille, Thermoskanne |
| Pinsel | `Business_Male_03` | – |
| Ledeiw | `Business_Male_01` | kleine schwarze Brille |
| Johanna | `Female_Adult_12` | Kapuzenjacke violett umgefärbt, Klemmbrett |
| Lorette | `Female_Adult_01` | Bluse grün umgefärbt (Regenjacke), Fahrradhelm, Gutachten |
| Frau Kemper | `Construction_Female_01` | Warnweste orange umgefärbt, Klemmbrett (Bauhelm am Modell) |
| Professor Relbeil | `Male_Adult_14` | Brille (nur Porträt) |

`cd tools/Figuren && npm install && node baue.mjs [namen…]` lädt die Quellen (FBX, TGA) in `tools/Figuren/.cache/` und
schreibt `Spiel3d/figur_<name>.glb` (Joe und Chani gut 2 MB mit Texturen 1024 px; Nebenfiguren knapp 2 MB mit 512 px am
Körper und ohne Rennen; Animationen ausgedünnt, Rotationen als 16 Bit). Umfärben geschieht beim Bauen nur am Oberkörper:
Die Maske entsteht aus den UV-Inseln, deren Skin-Gewichte an Rumpf und Armen hängen (Hosen bleiben).
`figuren3d.js` setzt die Modelle in die vorhandenen Figuren ein (`realistisch()`); Nebenfiguren erscheinen sofort als
einfache Figur und tauschen nach dem Laden selbst (`nachladen()`, glb je Figur nur einmal geladen). Brillen, Hut und Helme
hängen an einem Kopfanker (Maße aus den Gesichtsknochen: `node masse.mjs`; alle Männer- bzw. Frauenköpfe sind gleich
vermessen), Requisiten an Handankern, die in der Stehhaltung wie die Figur ausgerichtet sind. Schlägt das Laden fehl,
bleiben die einfachen Figuren.

### Bilder, Karte, Minispiele

- Fototexturen (Poly Haven, CC0): `cd tools/Welten && npm install && node texturen.mjs` lädt Farb- und Normal-Maps,
  verkleinert sie (`Spiel3d/tex_<name>_farbe.jpg`, `_normal.jpg`) und schreibt die Kachelmaße nach `texturen3d_daten.js`;
  `texturen3d.js` liefert die Materialien (`fotoMaterial`, `meterMaterial`). Alle Welten: Backstein (Wände der LoD2-Gebäude),
  Fischgrätpflaster, Kopfstein, Putz, Rasen (grün getönt), Kies, Asphalt, Beton; innen Dielen, Altziegel (Krypta),
  Sandstein (Tempel). Eigene Muster (Rippenbeton am Rathaus, Hallenbadfassade, Linoleum, Fliesen, Schilder) bleiben gemalt.
- Porträts in Sprechblasen und Figurenknöpfen: Kopfbilder der 3D-Figuren (`portraets3d.js`, eigener kleiner Renderer,
  je Figur, Ausdruck und Hut einmal erzeugt); Ausdrücke über die Gesichtsknochen (`gesicht()` in `figuren3d.js`).
  Professor Relbeil hat dafür eine eigene Figur. Die Nebenfiguren für Porträts werden kurz nach dem Start angelegt, damit
  ihre Modelle schon geladen sind, wenn sie zum ersten Mal sprechen. Inventar-Icons: kleine 3D-Modelle (`gegenstaende3d.js`).
- Startseite und Reisekarte: Greven als Stadtmodell 1:1000 auf einem Kartentisch (`karte3d.js`, Daten `karte3d.json`
  aus den OSM-Daten, erzeugt mit `node tools/Grafik/karte3d.mjs`): Karte auf Pergament in 2560 px, über 5000 Gebäude als
  Klötze, Stecknadeln je Station, die Reise als rote Punktspur mit Joes Hut. Auf der Startseite stehen Joe und Chani am Tisch.
  Die Kartendaten stammen von **OpenStreetMap** (© OpenStreetMap-Mitwirkende, Lizenz ODbL 1.0), einmalig per Overpass-API
  abgerufen (`tools/Grafik/daten/abfrage.overpassql`, gespeichert als `greven-osm-kompakt.json`); die Namensnennung steht
  auf dem Titel und im Abspann.
- Minispiele: Bootsflucht auf der Ems (bewusst gutmütig: weniger Treibgut, langsame und seltene alternative Fakten) und
  Wahlkugel im Tempelgang – mit Lorettes Lastenrad, das Chani zusammengefaltet in der Tasche trägt.
- Direkteinstieg: `/cheatsheet` (Spickzettel) bzw. `/spiel3d/?start=…` spielt die Komplettlösung bis dorthin nach
  (Schrittzahlen in `GralSpiel.SPRUENGE`, logik.js). Der Service Worker lässt `/spiel3d/` und `/cheatsheet` aus dem Cache
  (sonst passen nach einem Update alte und neue Module nicht zusammen).

## Tests

**Tests geben nie Ton aus.** Automatisierte oder ferngesteuerte Browser starten stumm (Chromium/Edge mit `--mute-audio`,
Playwright mit `args: ['--mute-audio']`) und öffnen das Spiel mit `?ton=aus` (`/spiel3d/?ton=aus`): Dann bleiben Melodie
und Effekte aus, ohne die gespeicherte Ton-Einstellung zu ändern. Test-Tabs mit dem Spiel nach dem Lauf schließen. Mit
Software-Rendering zusätzlich `&test` (große Zeitschritte, Minispiele mit Autopilot). Browsertests lesen den Stand über
`window.GralSpielStand()`, die Ziele über `window.GralSpiel3dZiele()` und lösen Aktionen mit `window.GralSpiel3dHandlung()` aus.

- `tests/FraktionPerfekt.Web.Tests/spiel/logik.test.cjs` und `spiel3d.test.cjs` (node --test), eingebunden in
  `dotnet test` über `SpielTests` (ohne Node übersprungen). Geprüft u. a.: Die Komplettlösung führt bis zum Ende, Rätsel
  (Schloss, Gitter, Quiz), Speichern/Laden mit Abwehr fremder Daten, Speicher, der Ausnahmen wirft, beide Minispiele, die
  Regie (siehe oben), jeder Hotspot jeder Szene hat ein 3D-Gegenstück, Auftritts- und Abgangsorte, Direkteinstiege und
  Spickzettel, keine Inline-Skripte, nur lokale Importe, jede Figur hat ihr Modell.

## Erkennungsmelodie

Eigene Abenteuer-Fanfare (Blechbläser, punktierter Marsch, C-Dur), darin als würdiges Zitat die erste Phrase von Joseph
Haydns Melodie von 1797 (gemeinfrei; Melodie der Nationalhymne), unverändert und ohne Text, dann Fanfaren-Schluss mit
Pauke. Erzeugt per WebAudio (`Spiel3d/musik.js`). Sie läuft auf dem Titelbild und kurz im Finale und lässt sich dort und im
Pausemenü abschalten und leiser stellen. Filmmusik wird nicht zitiert.
