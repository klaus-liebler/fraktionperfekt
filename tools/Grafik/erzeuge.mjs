// Erzeugt alle fehlenden Spielgrafiken nach assets/ (reproduzierbar, ohne Fremdpakete):
//   node tools/Grafik/erzeuge.mjs
// Klaus' Originale in assets/ werden nicht überschrieben; Neugenerierungen bekommen das Suffix _v2.
import { mkdirSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Bild, F, text, textBreite } from './pixel.mjs';
import * as HG from './hintergruende.mjs';
import { figurenSheets, posen, portraets, mitHut } from './figuren.mjs';
import { objekte, icons } from './objekte.mjs';
import { karteGreven } from './karte.mjs';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const ASSETS = join(wurzel, 'assets');
const SHOTS = join(wurzel, 'docs', 'spiel-screenshots');
mkdirSync(SHOTS, { recursive: true });

const erzeugt = [];
function speichere(name, bild, gruppe) {
    if (!name.includes('_v2') && existsSync(join(ASSETS, name)) && ORIGINALE.includes(name)) { throw new Error(`Original ${name} würde überschrieben.`); }
    bild.speichern(join(ASSETS, name));
    erzeugt.push({ name, bild, gruppe });
}
const ORIGINALE = ['buero_1990er_256x144.png', 'archiv_256x144.png', 'rathaus_flur_x5.png', 'joe_spritesheet_32x64.png', 'chani_spritesheet_32x64.png'];

// Schild „ZIMMER 012“ im Flur finden: Bereich der hellen Schildfläche um dunkle Schrift
function schildImFlur(flur) {
    let minX = 999, minY = 999, maxX = -1, maxY = -1;
    for (let y = 8; y < 26; y++) { for (let x = 80; x < 170; x++) { const p = flur.lies(x, y); if (p[0] < 70 && p[1] < 70 && p[2] < 70) { minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y); } } }
    if (maxX < 0) { return null; }
    const wand = flur.lies(minX - 3, minY);
    return [minX - 1, minY - 1, maxX - minX + 3, maxY - minY + 3, [wand[0], wand[1], wand[2]]];
}

// ---------- Hintergründe ----------
speichere('buero_zukunftsschmiede_v2_256x144.png', HG.zukunftsschmiede(), 'Hintergründe');
speichere('buero_zukunftsschmiede_v2_ohne_umschlag_256x144.png', HG.zukunftsschmiede(false), 'Hintergründe');
speichere('archiv_v2_256x144.png', HG.archivV2(Bild.laden(join(ASSETS, 'archiv_256x144.png'))), 'Hintergründe');
const flur = Bild.laden(join(ASSETS, 'rathaus_flur_x5.png'));
speichere('rathaus_flur_v2_256x144.png', HG.flurV2(flur, schildImFlur(flur)), 'Hintergründe');
speichere('ems_ufer_256x144.png', HG.emsUfer(), 'Hintergründe');
speichere('baustelle_bad_256x144.png', HG.baustelle(false), 'Hintergründe');
speichere('baustelle_bad_grube_trocken_256x144.png', HG.baustelle(true), 'Hintergründe');
speichere('schulhof_nacht_256x144.png', HG.schulhof(), 'Hintergründe');
speichere('krypta_256x144.png', HG.krypta(), 'Hintergründe');
speichere('tempel_eingang_256x144.png', HG.tempelEingang(false), 'Hintergründe');
speichere('tempel_eingang_offen_256x144.png', HG.tempelEingang(true), 'Hintergründe');
speichere('tempel_abgrund_256x144.png', HG.tempelAbgrund(false), 'Hintergründe');
speichere('tempel_abgrund_bruecke_256x144.png', HG.tempelAbgrund(true), 'Hintergründe');
speichere('tempel_finale_256x144.png', HG.tempelFinale(), 'Hintergründe');
speichere('karte_greven_256x144.png', karteGreven(join(wurzel, 'tools', 'Grafik', 'daten', 'greven-osm-kompakt.json')), 'Hintergründe');
speichere('minispiel_ems_512x144.png', HG.minispielEms(), 'Minispiele');
speichere('minispiel_gang_512x144.png', HG.minispielGang(), 'Minispiele');

// ---------- Figuren, Objekte, Icons ----------
const joe = Bild.laden(join(ASSETS, 'joe_spritesheet_32x64.png')), chani = Bild.laden(join(ASSETS, 'chani_spritesheet_32x64.png'));
const sheets = [['joe', joe], ['chani', chani]];
for (const [name, bild] of figurenSheets(joe, chani)) { speichere(name, bild, 'Figuren'); sheets.push([name.replace('_spritesheet_32x64.png', ''), bild]); }
const joeHut = mitHut(joe); speichere('joe_hut_spritesheet_32x64.png', joeHut, 'Figuren'); sheets.push(['joe_hut', joeHut]);
const GEGNER = ['hohlbein', 'droehn'];
for (const [name, bild] of sheets) {
    speichere(`${name}_posen_32x64.png`, posen(bild, name.startsWith('joe') ? 'kartusche' : name === 'chani' ? 'handy' : null), 'Posen');
    speichere(`${name}_portraet_64x64.png`, portraets(bild, GEGNER.includes(name) ? F.steindunkel : F.sand), 'Porträts');
}
for (const [name, bild] of objekte()) { speichere(name, bild, 'Objekte'); }
speichere('inventar_icons_16x16.png', icons(), 'Icons');

// ---------- Kontaktbogen und Einzelbilder ----------
function vergroessert(bild, s, hintergrund) {
    const v = new Bild(bild.b * s, bild.h * s, hintergrund);
    v.einsetzen(bild, 0, 0, { skala: s });
    return v;
}

const BREITE = 1600, RAND = 16;
const zeilen = [];
let x = RAND, y = 40, zeilenHoehe = 0;
const platz = [];
for (const e of erzeugt) {
    const s = e.bild.b <= 64 ? 4 : e.bild.b > 300 ? 1 : e.bild.b <= 192 ? 3 : 2;
    const bb = e.bild.b * s, bh = e.bild.h * s + 14;
    if (x + bb > BREITE - RAND) { x = RAND; y += zeilenHoehe + 12; zeilenHoehe = 0; }
    platz.push({ e, x, y, s }); x += bb + 16; zeilenHoehe = Math.max(zeilenHoehe, bh);
}
const bogen = new Bild(BREITE, y + zeilenHoehe + RAND, [60, 60, 72]);
text(bogen, 'JOE UND CHANI - NEUE GRAFIKEN ZUR FREIGABE', RAND, 12, F.gelb, 3);
for (const { e, x: px, y: py, s } of platz) {
    const kariert = new Bild(e.bild.b * s, e.bild.h * s);
    for (let j = 0; j < kariert.h; j += 8) { for (let i = 0; i < kariert.b; i += 8) { kariert.rechteck(i, j, 8, 8, ((i + j) / 8) % 2 ? [90, 90, 104] : [110, 110, 126]); } }
    bogen.einsetzen(kariert, px, py); bogen.einsetzen(e.bild, px, py, { skala: s });
    text(bogen, e.name.replace('.png', '').replace(/_/g, ' '), px, py + e.bild.h * s + 4, F.weiss);
}
bogen.speichern(join(SHOTS, 'grafik-uebersicht.png'));
for (const e of erzeugt.filter((x) => x.gruppe === 'Hintergründe' || x.gruppe === 'Minispiele')) {
    vergroessert(e.bild, e.bild.b > 300 ? 2 : 3).speichern(join(SHOTS, 'grafik-' + e.name));
}
console.log(`${erzeugt.length} Grafiken erzeugt.`);
void textBreite;
