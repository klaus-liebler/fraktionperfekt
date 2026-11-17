// Szenenbilder für docs/spiel-screenshots/: zeichnet Spielstände mit derselben Logik (logik.js) und denselben Platzregeln (grafik.js)
// wie das Spiel – Hintergrund, Überlagerungen, Nebenfiguren (auch verkleinert im Hintergrund, mit Klebemasse) und die Helden an ihren
// Startplätzen. Ohne Browser und ohne Ton:  node tools/Grafik/szenenbilder.mjs
import { createRequire } from 'node:module';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Bild } from './pixel.mjs';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const require = createRequire(import.meta.url);
const S = require(join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel', 'logik.js'));
const G = require(join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
const ASSETS = join(wurzel, 'assets'), SHOTS = join(wurzel, 'docs', 'spiel-screenshots');
const cache = {};
const bild = (n) => (cache[n] ??= Bild.laden(join(ASSETS, n + '.png')));

/** Ausschnitt mit beliebigem Maßstab (nächster Nachbar) einsetzen; dunkel < 1 dunkelt ab. */
function zeichne(ziel, quelle, sx, sy, b, h, x, y, s = 1, dunkel = 1) {
    const zb = Math.round(b * s), zh = Math.round(h * s);
    for (let j = 0; j < zh; j++) {
        for (let i = 0; i < zb; i++) {
            const p = quelle.lies(sx + Math.floor(i / s), sy + Math.floor(j / s));
            if (p[3] < 128) { continue; }
            ziel.setze(Math.round(x) + i, Math.round(y) + j, [p[0] * dunkel, p[1] * dunkel, p[2] * dunkel].map(Math.round));
        }
    }
}

function szene(z) {
    const b = new Bild(G.B, G.H);
    const bg = bild(G.hintergrund(z));
    zeichne(b, bg, 0, 0, G.B, G.H, 0, 0);
    for (const o of G.ueberlagerungen(z, 0)) { zeichne(b, bild(o.bild), o.sx, o.sy || 0, o.b, o.h, o.x, o.y); }
    const hs = S.sichtbareHotspots(z), start = G.startplaetze(z, hs), hut = z.inventar.includes('taschenlampe');
    const figuren = G.npcPlaetze(z, hs).concat([
        { name: hut ? 'joe_hut' : 'joe', x: start.joe, y: G.BODEN }, { name: 'chani', x: start.chani, y: G.BODEN }
    ]).sort((a, c) => a.y - c.y);
    for (const f of figuren) {
        const s = f.skala || 1, r = G.rahmen(f.name, 'stehen', 0, false);
        if (f.klebt) { zeichne(b, bild('klebemasse_32x6'), 0, 0, 32, 6, f.x - 16 * s, f.y - 4 * s, s); }
        zeichne(b, bild(r.bild), r.sx, 0, 32, 64, f.x - G.FUSS_X * s, f.y - G.FUSS_Y * s, s, f.hinten ? 0.72 : 1);
    }
    const gross = new Bild(G.B * 3, G.H * 3);
    gross.einsetzen(b, 0, 0, { skala: 3 });
    return gross;
}

/** Komplettlösung bis zur Bedingung spielen (ohne den Schritt, der sie aufhebt). */
function bis(bedingung, nachher) {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { if (bedingung(z)) { break; } S.schritt(z, s); }
    if (nachher) { nachher(z); }
    return z;
}

const bilder = [
    ['20-zukunftsschmiede', S.neuerZustand()],
    ['15-archiv-festgeklebt', bis((z) => z.flags.gegnerArchivFest)],
    ['16-karte-hallenbad', bis((z) => z.orte.includes('baustelle') && z.szene === 'karte')],
    ['17-baustelle-hallenbad', bis((z) => z.szene === 'baustelle')],
    ['18-schulhof-eingang', bis((z) => z.szene === 'schule')],
    ['19-schulflur', bis((z) => z.szene === 'schulflur')]
];
for (const [name, z] of bilder) { szene(z).speichern(join(SHOTS, name + '.png')); console.log(name, z.szene); }
