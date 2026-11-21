// Fototexturen für die 3D-Welten von Poly Haven (CC0, https://polyhaven.com/license) – Farb- und Normal-Map je Material,
// verkleinert und neu komprimiert. Aufruf: cd tools/Welten && npm install && node texturen.mjs
// Schreibt src/FraktionPerfekt.Web/Spiel3d/tex_<name>_farbe.jpg, tex_<name>_normal.jpg und texturen3d_daten.js (Kachelmaße).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import jpeg from 'jpeg-js';

const HIER = path.dirname(fileURLToPath(import.meta.url));
const CACHE = path.join(HIER, '.cache');
const ZIEL = path.resolve(HIER, '../../src/FraktionPerfekt.Web/Spiel3d');

/** name im Spiel → Poly-Haven-Kennung; farbe/normal: Kantenlänge in Pixeln. */
const TEXTUREN = {
    backstein: { id: 'red_brick', farbe: 1024, normal: 512 },
    pflaster: { id: 'herringbone_pavement', farbe: 1024, normal: 512 },
    kopfstein: { id: 'cobblestone_square', farbe: 1024, normal: 512 },
    putz: { id: 'white_stucco', farbe: 512, normal: 512 },
    rasen: { id: 'sparse_grass', farbe: 512, normal: 512 },
    kies: { id: 'gravel_floor', farbe: 512, normal: 512 },
    asphalt: { id: 'asphalt_02', farbe: 512, normal: 512 },
    beton: { id: 'concrete_floor_02', farbe: 512, normal: 512 },
    dielen: { id: 'old_wood_floor', farbe: 1024, normal: 512 },
    altziegel: { id: 'medieval_red_brick', farbe: 1024, normal: 512 },
    sandstein: { id: 'sandstone_blocks_08', farbe: 1024, normal: 512 }
};

async function holen(url, datei) {
    const p = path.join(CACHE, datei);
    if (!fs.existsSync(p)) {
        const r = await fetch(url, { headers: { 'User-Agent': 'fraktionperfekt-tools' } });
        if (!r.ok) { throw new Error(`${url}: HTTP ${r.status}`); }
        fs.mkdirSync(CACHE, { recursive: true });
        fs.writeFileSync(p, Buffer.from(await r.arrayBuffer()));
    }
    return fs.readFileSync(p);
}
const json = async (url) => (await fetch(url, { headers: { 'User-Agent': 'fraktionperfekt-tools' } })).json();

function verkleinern(img, kante) {
    let { width: w, height: h, data } = img;
    while (w > kante) {
        const W = w / 2, H = h / 2, out = Buffer.alloc(W * H * 4);
        for (let y = 0; y < H; y++) {
            for (let x = 0; x < W; x++) {
                for (let c = 0; c < 4; c++) {
                    out[(y * W + x) * 4 + c] = (data[((2 * y) * w + 2 * x) * 4 + c] + data[((2 * y) * w + 2 * x + 1) * 4 + c] + data[((2 * y + 1) * w + 2 * x) * 4 + c] + data[((2 * y + 1) * w + 2 * x + 1) * 4 + c]) / 4;
                }
            }
        }
        w = W; h = H; data = out;
    }
    return { width: w, height: h, data };
}

const daten = {};
for (const [name, t] of Object.entries(TEXTUREN)) {
    const info = await json(`https://api.polyhaven.com/info/${t.id}`);
    const files = await json(`https://api.polyhaven.com/files/${t.id}`);
    for (const [karte, schluessel, q] of [['farbe', 'Diffuse', 80], ['normal', 'nor_gl', 86]]) {
        const url = files[schluessel]['1k'].jpg.url;
        const roh = jpeg.decode(await holen(url, path.basename(url)), { useTArray: true, maxMemoryUsageInMB: 1024 });
        const img = verkleinern(roh, t[karte]);
        const datei = `tex_${name}_${karte}.jpg`;
        fs.writeFileSync(path.join(ZIEL, datei), jpeg.encode(img, q).data);
        console.log(datei, img.width + ' px', (fs.statSync(path.join(ZIEL, datei)).size / 1024).toFixed(0) + ' KB');
    }
    daten[name] = { meter: +(info.dimensions[0] / 1000).toFixed(2), quelle: `Poly Haven: ${t.id} (${Object.keys(info.authors).join(', ')}), CC0` };
}
fs.writeFileSync(path.join(ZIEL, 'texturen3d_daten.js'),
    '// Erzeugt von tools/Welten/texturen.mjs – Kantenlänge einer Kachel in Metern und Herkunft (Poly Haven, CC0).\n'
    + `export const TEXTUREN = ${JSON.stringify(daten, null, 4)};\n`);
console.log(daten);
