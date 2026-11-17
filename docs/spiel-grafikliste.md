# Spiel-Grafikliste: was da ist, was fehlt

> **Stand 15.11.2026, abends:** Alle Einträge sind erzeugt, und zwar per Skript `node tools/Grafik/erzeuge.mjs`
> (reproduzierbar, ohne Fremdpakete; Ausgabe nach `assets/`). Neugenerierungen tragen das Suffix `_v2`; Klaus'
> Originale bleiben unverändert. Übersicht zur Freigabe: [`docs/spiel-screenshots/grafik-uebersicht.png`](spiel-screenshots/grafik-uebersicht.png),
> Einzelbilder `docs/spiel-screenshots/grafik-*.png`. Noch nicht im Spiel eingebaut (wartet auf Freigabe).
> Das Parteibüro ist die **Zukunftsschmiede** in der Marktstraße (`buero_zukunftsschmiede_v2_256x144.png`, Ladenlokal mit
> Schaufenster, ohne Parteilogo). Die Reisekarte entsteht aus **OpenStreetMap**-Daten (© OpenStreetMap-Mitwirkende,
> ODbL; einmaliger Abruf, `tools/Grafik/daten/`). Noch offen: Zusatzposen (Reden, Benutzen, Erschrocken) und Porträts
> im 256er-Stil.

Stand: 15.11.2026. Grundlage: Klaus' Grafiken in `assets/` und alle Orte, Figuren, Gegenstände und Minispiele des
Spiels (`src/FraktionPerfekt.Web/Spiel/logik.js`, `docs/spiel.md`). Das Spiel ist noch nicht umgebaut; diese Liste
beschreibt nur, welche Grafiken für den Umbau gebraucht werden.

## Vorhandene Grafiken (`assets/`)

| Datei | Maße | Art | Inhalt | Technik |
|---|---|---|---|---|
| `buero_1990er_256x144.png` | 256×144 | Hintergrund | Büro: Bücherregal links, Kalender 1995, Wanduhr, Schreibtisch mit Röhrenmonitor und Bürostuhl, Topfpflanze, Drucker/Fax auf Rollcontainer, Tür mit EXIT-Schild rechts | deckend, 27 Farben |
| `archiv_256x144.png` | 256×144 | Hintergrund | Archivraum: Regalwand voller Ordner und Bücher, Rollleiter, Kartons, Holzboden | deckend, 23 Farben |
| `rathaus_flur_x5.png` | 256×144 | Hintergrund | Rathausflur: Bogenfenster, Aushangbrett, Tür „Zimmer 012“, Doppeltür „ARCHIV“, Bank, Pflanze, Feuerlöscher, Heizkörper, Schachbrettboden | deckend, 22 Farben |
| `joe_spritesheet_32x64.png` | 192×64 (6 × 32×64) | Sprite-Sheet | Joe: Glatze mit grauem Haarkranz, Brille, Schnurrbart, blaues Sakko, hellblaues Hemd, dunkle Hose. Bild 1 stehend frontal, Bilder 2–6 Laufzyklus nach rechts | transparent, 17 Farben, Figur 62 px hoch |
| `chani_spritesheet_32x64.png` | 192×64 (6 × 32×64) | Sprite-Sheet | Chani: lange glatte kupferbraune Haare (offen), dunkelpetrolfarbener Mantel/Blazer, dunkle Hose. Aufbau wie bei Joe | transparent, 15 Farben, Figur 59 px hoch |

**Stil:** Pixel-Art in 256×144 (16:9) ohne Dithering und ohne schwarze Umrisslinien, flache Farbflächen mit je einer
dunkleren Schattenstufe, 20–30 Farben je Hintergrund, 15–17 je Figur. Räume sind frontal gezeichnet (gerade Rückwand,
Boden als Streifen im unteren Fünftel), helles, freundliches Tageslicht, klare, gut lesbare Formen im Look der
Adventures der frühen 90er. Figuren sind ca. 60 px hoch und leicht gestreckt (Kopf ca. 1/6), Laufzyklus seitlich nach
rechts. Für die Gegenrichtung spiegelt das Spiel die Bilder.

**Abgleich mit dem Spiel:**
- Das **Büro** passt fast. Es fehlen drei Objekte, die im Spiel gebraucht werden: Briefkasten/Postfach, Garderobe mit Hut und Lederjacke, Umschlag auf dem Tisch. Siehe „Neugenerierung“; als Ausweichlösung gehen auch einzelne Sprites.
- Das **Archiv** wird zu zwei Szenen:
  - **Rathausflur** (`rathaus_flur_x5.png`): Hier sitzt Chris vor der Archivtür, später versperren Pinsel und Ledeiw den Ausgang.
  - **Archivraum** (`archiv_256x144.png`): mit dem Regal der Ratsprotokolle. Es fehlt das Gemälde „Der Rat von 1949“ als Sprite.
- **Joe** trägt im Spiel nach der Garderobe Hut und Umhängetasche mit Kartuschen. Dafür ist ein zweites Sheet sinnvoll, alternativ bleibt er ohne Hut.
- **Chani** hat im Spiel Umhängetasche und Handy. Das ist optional; das vorhandene Sheet reicht.
- Für Reden und Benutzen fehlen bei beiden Figuren Posen (siehe Figuren).

## Neugenerierung vorhandener Hintergründe

Bei allen drei vorhandenen Hintergründen fehlen Objekte, die das Spiel als Klickflächen braucht. Statt sie nachträglich
als Sprites einzusetzen, sollen die Bilder mit dem angepassten Prompt neu erzeugt werden. Die vorhandene Anordnung
bleibt erhalten; die Positionen der neuen Objekte sind auf die bestehenden Bilder abgestimmt. Die Klickflächen
des Spiels passe ich beim Einbau an die fertigen Bilder an; die Positionen hier sind Zielwerte.

| Datei (neu) | Was fehlt / was sich ändert | Angepasster Prompt (zusätzlich zur Stilvorgabe) |
|---|---|---|
| `buero_1990er_256x144.png` (Neugenerierung) | **Briefkasten/Postfach** der Fraktion: Holz-Wandfach mit sechs Fächern, Papiere darin, an der Wand zwischen Kalender und Wanduhr (ca. x 100–124, y 26–44). **Garderobe:** Garderobenständer mit brauner Lederjacke und braunem Filzhut, statt der Topfpflanze links neben dem Schreibtisch (ca. x 62–82, y 36–102); die Pflanze wird klein und steht auf dem Aktenschrank. **Umschlag:** weißer Briefumschlag mit rotem Siegel gut sichtbar vorn auf dem Schreibtisch (ca. x 140–160, y 78–86). **Fax:** Das Gerät auf dem Rollcontainer klar als Faxgerät erkennbar machen (Papiereinzug, Hörer, Tastenfeld; x 180–208, y 66–84). Bleibt: Bücherregal links, Kalender 1995, Wanduhr, Schreibtisch mit Röhrenmonitor, Tür mit EXIT-Schild rechts | „1990s office of a town council party group, bookshelf on the left, wall calendar 1995, wall clock, wooden wall-mounted mail pigeonhole box with six compartments and papers between calendar and clock, coat stand with a brown leather jacket and a brown fedora left of the desk, desk with an old CRT monitor and a white envelope with a red seal lying on it, fax machine with handset and paper tray on a rolling cabinet, small plant, door with EXIT sign on the right“ |
| `archiv_256x144.png` (Neugenerierung) | **Gemälde „Der Rat von 1949“** im Goldrahmen an einem freien Wandstück zwischen zwei Regalen (ca. x 160–208, y 24–56): Gruppe von Ratsherren der Nachkriegszeit vor einem Fantasiewappen. **Regal mit Ratsprotokollen** als eigenes, deutlich erkennbares Regal mit gleichförmigen grauen, nummerierten Ordnern und Schild „RATSPROTOKOLLE“ (ca. x 110–150, y 40–100). **Tür zurück in den Flur** am Rand (links, ca. x 4–28, y 36–104). Rollleiter und Kartons können bleiben | „archive room full of shelves with binders and books, one distinct shelf of identical grey numbered binders labeled RATSPROTOKOLLE in the middle, a gold-framed oil painting of a group of post-war councilmen in front of a fantasy coat of arms on a free wall section, rolling library ladder, boxes, door on the left edge, wooden floor“ |
| `rathaus_flur_x5.png` (Neugenerierung, optional) | Passt bis auf die Ausgänge: **Doppeltür „ARCHIV“** bleibt (Archivtür, Klickfläche). Die Tür „Zimmer 012“ wird zur Tür **„FRAKTION“** (Rückweg ins Büro). Am linken Rand ein **Treppenhaus/Ausgang** mit grünem Notausgangsschild (ca. x 0–24, y 36–104), dort versperren später Pinsel und Ledeiw den Weg. Die Bank bleibt (Chris sitzt davor oder darauf; die Figur kommt als Sprite) | „town hall corridor, arched window, notice board, wooden door labeled FRAKTION, double door labeled ARCHIV, wooden bench, plant, fire extinguisher, radiator, checkered floor, stairwell exit with green emergency exit sign at the left edge“ |

## Gemeinsame Stilvorgabe für alle Prompts

> Pixel art, early-1990s point-and-click adventure game style, flat colors with one darker shade, no dithering, no black
> outlines, limited palette (about 25 colors), clean readable shapes, frontal view, warm friendly lighting, consistent
> with the existing scenes. No text except short signs, no logos, no real persons.

Für Hintergründe: **„… 256×144 pixels, background only, no characters, floor strip in the lower fifth where characters
walk.“** Für Sprite-Sheets: **„… sprite sheet 192×64 pixels, 6 frames of 32×64 on transparent background, character
about 60 px tall, frame 1 standing front view, frames 2–6 walk cycle side view facing right, same proportions as the
existing Joe sprite sheet.“**

Koordinaten unten beziehen sich auf 256×144 (x, y = linke obere Ecke; B×H). Das Spiel legt seine Klickflächen dorthin.
Kleine Abweichungen passe ich beim Einbau an.

## Fehlende Hintergründe

| Datei (Vorschlag) | Inhalt, Licht, Objekte mit Position (256×144) | Prompt (zusätzlich zur Stilvorgabe) |
|---|---|---|
| `ems_ufer_256x144.png` | Ems bei Sonnenuntergang. Links Uferwiese mit Holzsteg (x 60–140, y 100–115); rechts Fluss, dahinter Wiese und Bäume. Alte Steinbrücke mit drei Bögen hinten (x 150–230, y 25–60), daran eine **Steintafel mit Inschrift** (x 165–210, y 33–45). **Ruderboot** am Steg (x 102–148, y 92–106; besser als eigenes Sprite). **Sieben Holzpfähle** im Wasser (x 184–235, y 81–101). Pfad nach rechts als Ausgang (x 230–250, y 43–86). Laufbereich: Wiese links und Steg, y 110–144 | „river bank at sunset in a small German town, old three-arched stone bridge in the background with a small stone plaque, wooden jetty on the left, seven old wooden posts in the water on the right, reeds, ducks, footpath leaving to the right“ |
| `baustelle_bad_256x144.png` | Baustelle des Freibads am Abend (orange Himmel). Bauzaun mit rot-weißen Planken, Baukran links hinten, Bauwagen rechts (x 200–256, y 55–95). **Baugrube** Mitte (x 102–158, y 89–109) als Variante a) mit Wasser gefüllt, b) trocken mit alten türkisen Beckenfliesen (zweite Datei `baustelle_bad_grube_trocken_256x144.png` oder Grube als Sprite). **Leckende Leitung** über der Grube (x 118–144, y 75–87). Tor im Bauzaun rechts (x 225–248, y 43–92). Sandhaufen, Absperrbaken. Laufbereich: sandiger Boden y 108–144 | „construction site of an old open-air swimming pool at dusk, construction fence with red and white boards, tower crane, site trailer on the right, open excavation pit in the middle with an old broken pipe above it, sand piles, traffic beacons“ |
| `schulhof_nacht_256x144.png` | Hof einer Grundschule aus rotem Backstein, Abend/Nacht. Fassade mit Fensterreihen (einige erleuchtet), Schild „GRUNDSCHULE“ (kein echter Name nötig), **große Wanduhr** an der Fassade (x 154–180, y 20–43; Zeiger als Sprite oder zwei Varianten 18:10/20:35). **Kellerabgang** mit Treppe nach unten und rot-weißem Absperrband (x 179–210, y 52–101). Tischtennisplatte, Hüpfkästchen, Laterne. Schultor links als Ausgang (x 10–30, y 43–92). Laufbereich y 110–144 | „schoolyard of a red brick primary school at night, lit windows, large wall clock on the facade, cellar stairs going down with red and white barrier tape, ping-pong table, hopscotch, street lamp, moon and stars, school gate on the left“ |
| `krypta_256x144.png` | Gewölbekeller aus Backstein mit Rundbögen, Fackellicht. Links Treppe nach oben (x 5–25, y 81–110). **Wandfresko** links (x 26–93, y 20–63): drei verblasste Bilder übereinander – oben Waage, Mitte offenes Buch, unten Urne. **Steintafel mit drei drehbaren Symbolscheiben** Mitte (x 113–144, y 58–78). **Steinernes Tor** rechts (x 179–225, y 26–98) mit drei runden Vertiefungen für Siegel und Schlüsselloch. Fackeln zwischen den Objekten. Laufbereich y 110–144 | „underground brick vault with round arches lit by torches, faded fresco of three stacked symbols (scales, open book, urn) on the left wall, stone panel with three rotating symbol discs in the middle, carved stone gate with three round recesses and a keyhole on the right, stairs leading up on the left“ |
| `tempel_eingang_256x144.png` | Säulenhalle aus Sandstein, Licht von oben. Mitte **Fallgitter** (x 102–148, y 26–101), darüber **Inschrift** „FRAKTIONSZWANG“ (x 87–164, y 12–24). Links und rechts davor je eine **Bodenplatte** (links x 56–92, rechts x 159–195, y 104–113). Fackeln an den Seiten. Variante: Gitter hochgezogen (`tempel_eingang_offen_256x144.png` oder Gitter als Sprite) | „grand sandstone temple hall with columns, light falling from above, iron portcullis in the middle with a carved inscription above, two stone pressure plates on the floor left and right, torches on the walls“ |
| `tempel_abgrund_256x144.png` | Hinter dem Gitter: links **Steintafel mit Paragrafenzeichen** (x 51–87, y 35–75), Mitte **Abgrund** quer durch die Halle (x 118–185, y 72–98), rechts dahinter Eingang in einen **langen abschüssigen Gang**, in dem eine riesige Steinkugel liegt (x 195–231, y 43–101). Variante mit **Steinbrücke** über dem Abgrund (`tempel_abgrund_bruecke_256x144.png` oder Brücke als Sprite) | „sandstone temple room with a carved stone tablet showing a paragraph sign on the left, a deep chasm across the middle, a dark sloping corridor on the right with a giant stone ball at its top“ |
| `tempel_finale_256x144.png` | Allerheiligstes: links **goldener Pokal** auf Steinpodest (x 77–103, y 52–78), rechts **schlichte Holzkiste mit Schlitz** (Wahlurne) mit Bleistift an einer Schnur auf Steinsockel (x 154–180, y 63–83). Feierliches Licht. Platz für vier Figuren auf dem Boden | „innermost temple chamber, a shiny golden trophy cup on a stone pedestal on the left, a plain wooden ballot box with a slot and a pencil on a string on a simple stone plinth on the right, solemn light from above“ |
| `karte_greven_256x144.png` | Gezeichnete Landkarte auf Pergament: Ems als Fluss von oben nach unten, Straßen, Innenstadt mit Kirchturm, Felder; Kompassrose unten rechts, Kaffeefleck. Orte als rote Markierungen ungefähr bei: Fraktionsbüro (133, 66), Rathausarchiv (123, 58), Ems-Ufer (97, 81), Baustelle Badsanierung (164, 92), Grundschule (184, 49). Ortsnamen schreibt das Spiel selbst | „hand-drawn treasure map on parchment of a small German town, a river running from top to bottom, roads, old town with a church tower, fields, compass rose, coffee stain, no labels“ |
| `minispiel_ems_512x144.png` | Fluss in Seitenansicht für die Bootsverfolgung, waagerecht nahtlos kachelbar (rechter Rand passt an linken): oben Ufer mit Büschen, unten Ufer, dazwischen drei Wasserbahnen | „side view of a river for a boat chase, seamlessly tileable horizontally, bushes on the upper bank, grass on the lower bank, open water in between“ |
| `minispiel_gang_512x144.png` | Langer Tempelgang in Seitenansicht für die Flucht vor der Kugel, kachelbar, rechts das helle Ende (Halle) | „side view of a long sloping sandstone temple corridor with columns, seamlessly tileable, bright exit at the far right“ |

## Fehlende Figuren (Sprite-Sheets 192×64, Aufbau wie bei Joe)

| Datei (Vorschlag) | Figur, Aussehen | Prompt (zusätzlich zur Sprite-Vorgabe) |
|---|---|---|
| `rolf_spritesheet_32x64.png` | **Chris (SPD-Typ)**, älterer Herr, grauer Vollbart, Brille, rote Strickjacke über hellem Hemd, graue Hose, Thermoskanne in der Hand. Steht meist | „older man with grey full beard and glasses, red knitted cardigan, light shirt, grey trousers, holding a thermos flask“ |
| `tanja_spritesheet_32x64.png` | **Johanna (Linke-Typ)**, Frau mittleren Alters, kurze schwarze Haare, violetter Kapuzenpullover, Jutebeutel, Klemmbrett | „woman with short black hair, purple hoodie, cloth tote bag over the shoulder, clipboard“ |
| `mia_spritesheet_32x64.png` | **Lorette (Grünen-Typ)**, junge Frau, blonder Zopf, grüner Fahrradhelm, grüne Regenjacke, dickes Gutachten unter dem Arm | „young woman with blonde ponytail, green bicycle helmet, green rain jacket, thick report under her arm“ |
| `kemper_spritesheet_32x64.png` | **Frau Kemper**, Bauleiterin, gelber Bauhelm, orange Warnweste über grauer Jacke, Klemmbrett | „female construction site manager, yellow hard hat, orange high-visibility vest over grey jacket, clipboard“ |
| `hohlbein_spritesheet_32x64.png` | **Pinsel** (Gegner, Karikatur), kräftig, kurzes blondes Haar mit Seitenscheitel, zu enger grauer Anzug, Krawatte, selbstgefälliges Grinsen. Kein Abzeichen | „stocky man with short blond side-parted hair, too tight grey suit, tie, smug grin, no badges or symbols“ |
| `droehn_spritesheet_32x64.png` | **Ledeiw** (Gegner, Karikatur), groß und dünn, gegelte dunkle Haare, kleine Brille, dunkelblauer Anzug, rote Krawatte. Kein Abzeichen | „tall thin man with slicked-back dark hair, small glasses, dark blue suit, red tie, pompous posture, no badges or symbols“ |

**Zusatzposen für alle Figuren** (je ein Sheet `<name>_posen_32x64.png`, 192×64, transparent):

| Bild | Pose |
|---|---|
| 1, 2 | Reden (Mund zu/offen, eine Hand gestikuliert), Seitenansicht nach rechts |
| 3, 4 | Benutzen/Greifen (Arm nach vorn ausgestreckt, zwei Stufen), Seitenansicht nach rechts |
| 5 | Erschrocken (Arme hoch), frontal |
| 6 | Blinzeln (wie Bild 1 des Laufsheets, Augen zu) |

Sonderposen: **Joe** mit Hut (brauner Filzhut) und Umhängetasche mit Kartuschen (`joe_hut_spritesheet_32x64.png`);
Joe mit Dichtstoff-Kartusche im Anschlag (in `joe_posen`, Bild 3/4). **Chani** mit Handy hochgehalten
(Live-Story, in `chani_posen`, Bild 3/4).

## Porträts für die Textbox (optional, empfohlen)

| Datei (Vorschlag) | Inhalt | Prompt |
|---|---|---|
| `<name>_portraet_64x64.png` (je Figur: joe, chani, christian, joanna, lore, kemper, hohlbein, droehn) | Sheet 192×64 aus 3 Bildern à 64×64: neutral, lacht, erschrocken. Kopf und Schultern, frontal, schlichter Hintergrund | „portrait sheet 192×64 pixels, three 64×64 head-and-shoulders portraits of the same character: neutral, laughing, startled; plain background“ + Figurbeschreibung |

## Objekte, die sich im Spiel ändern (Sprites, transparent)

| Datei (Vorschlag) | Maße | Inhalt |
|---|---|---|
| `umschlag_26x10.png` | 26×10 | weißer Briefumschlag mit rotem Siegel, liegt auf dem Schreibtisch im Büro |
| `postfach_20x14.png` | 20×14 | Holz-Postfach mit sechs Fächern, Papiere darin (Wand im Büro) |
| `garderobe_20x49.png` + `garderobe_ohne_hut_20x49.png` | 20×49 | Garderobenständer mit Lederjacke und braunem Filzhut / ohne Hut |
| `gemaelde_rat1949_46x32.png` | 46×32 | Ölgemälde im Goldrahmen: Gruppe von Ratsherren der Nachkriegszeit vor einem Wappen (Fantasiewappen) |
| `ruderboot_46x14.png` | 46×14 | Holzruderboot mit selbstgemaltem Schild „Volkseigentum“ |
| `motorboot_64x24.png` | 64×24 | graues Motorboot (Seitenansicht) für das Minispiel |
| `hindernisse_ems_96x24.png` | 4 × 24×24 | Treibholz, Entenfamilie, Schild „Alternative Route“, treibendes Plakat (ohne Parteilogos) |
| `leck_26x12_x3.png` | 78×12 (3 Bilder) | Rohr mit spritzendem Wasser, Animation |
| `fledermaeuse_8x6_x4.png` | 32×6 (4 Bilder) | kleine Fledermaus, Flügelschlag |
| `uhr_zeiger_26x23_x2.png` | 52×23 (2 Bilder) | Zifferblatt 18:10 und 20:35 |
| `symbolscheiben_31x20.png` | 31×20 | drei goldene Scheiben (Waage, Buch, Urne) auf Stein |
| `tor_siegel_46x72.png` | 46×72 | Tor mit eingesetzten drei Siegeln, offen (dunkle Öffnung) |
| `bodenplatte_leuchtend_36x9.png` | 36×9 | Bodenplatte, golden leuchtend (betreten) |
| `bruecke_67x10.png` | 67×10 | Steinbrücke über den Abgrund |
| `wahlkugel_64x64_x4.png` | 256×64 (4 Bilder) | riesige Steinkugel mit eingemeißeltem Wahlkreuz, rollend |
| `lastenrad_48x32.png` | 48×32 | grünes Lastenrad mit Transportkiste |
| `pokal_26x26.png`, `wahlurne_26x20.png` | – | falls nicht im Finale-Hintergrund enthalten |

## Inventar-Icons (16×16, transparent, ein Sheet `inventar_icons_16x16.png`, 160×16)

Reihenfolge: Anonymer Brief · Stadtplan von 1949 (Fax) · Mustertagesordnung · Geschäftsordnung (Taschenbuch) ·
Taschenlampe · Paddel · Siegel der Beschlüsse (Buch) · Siegel der Ausführung (Schlüsselbund) · Siegel des Rechts (Waage) ·
Tempelschlüssel („Art. 20“).

Prompt: „pixel art inventory icon sheet 160×16, ten 16×16 icons on transparent background: envelope, faxed town map,
sheet of paper with agenda lines, small blue rule book, flashlight, wooden paddle, round seal with book, round seal with
key ring, round seal with scales, ornate golden key“.

## Zuordnung im Spiel (für den späteren Umbau)

| Spielort | Grafik |
|---|---|
| Zukunftsschmiede (Parteibüro, Marktstraße) | `buero_1990er_256x144.png` (neu generiert mit Briefkasten, Garderobe, Umschlag) |
| Rathausflur (Chris, Archivtür, Gegner, Ausgang) | `rathaus_flur_x5.png` |
| Archivraum (Gemälde, Ratsprotokolle) | `archiv_256x144.png` (neu generiert mit Gemälde, Protokollregal, Tür) |
| Ems-Ufer, Bootsverfolgung | `ems_ufer`, `minispiel_ems` + Boote, Hindernisse |
| Baustelle Badsanierung | `baustelle_bad` (+ trocken), Leck |
| Schulhof | `schulhof_nacht` + Uhr, Fledermäuse |
| Krypta | `krypta` + Symbolscheiben, Tor mit Siegeln |
| Tempel: Fallgitter, Tafel/Abgrund, Flucht, Finale | `tempel_eingang`, `tempel_abgrund`, `minispiel_gang`, `tempel_finale` + Brücke, Kugel, Lastenrad |
| Reisekarte | `karte_greven` |
