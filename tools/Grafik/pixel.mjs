// Grundlagen für den Grafik-Generator: PNG lesen/schreiben (ohne Fremdpakete), Bild mit Zeichenfunktionen, Palette
// (abgeleitet aus Klaus' Vorlagen in assets/: NES-artige, kräftige Farben, flach, eine Schattenstufe, keine Umrisse).
import { readFileSync, writeFileSync } from 'node:fs';
import { deflateSync, inflateSync } from 'node:zlib';

export const F = {
    schwarz: [0, 0, 0], weiss: [252, 252, 252], hellgrau: [188, 188, 188], grau: [124, 124, 124], dunkelgrau: [64, 64, 64], fastschwarz: [36, 36, 44],
    creme: [232, 224, 196], creme2: [220, 212, 180], beige: [196, 184, 152], sand: [228, 196, 148], sand2: [200, 168, 120], hautH: [244, 192, 148], hautS: [204, 140, 100],
    holzhell: [172, 100, 44], holz: [124, 68, 24], holzdunkel: [80, 40, 8], holzschwarz: [44, 20, 4], braun: [136, 80, 40], lehm: [188, 140, 88],
    rot: [216, 40, 0], dunkelrot: [136, 20, 0], orange: [228, 92, 16], hellorange: [252, 160, 68], gelb: [248, 184, 0], hellgelb: [248, 216, 120], ocker: [200, 148, 0], senf: [172, 124, 0],
    gruen: [0, 168, 0], dunkelgruen: [0, 100, 0], hellgruen: [88, 216, 84], petrol: [0, 120, 88], petroldunkel: [0, 80, 60], gras: [0, 184, 0], moos: [0, 136, 136],
    blau: [0, 88, 248], dunkelblau: [0, 40, 140], navy: [0, 0, 168], hellblau: [104, 184, 252], eisblau: [164, 228, 252], tuerkis: [88, 248, 248], himmel: [60, 188, 252],
    lila: [148, 60, 168], dunkellila: [88, 24, 108], flieder: [152, 120, 248], rosa: [248, 120, 248], lachs: [248, 120, 88], pfirsich: [252, 224, 168],
    teppich: [92, 100, 128], teppich2: [72, 80, 108], nacht: [24, 20, 64], nacht2: [36, 32, 96], stein: [188, 172, 140], steinhell: [216, 204, 172], steindunkel: [140, 124, 96]
};

export class Bild {
    constructor(b, h, hintergrund) {
        this.b = b; this.h = h; this.d = new Uint8Array(b * h * 4);
        if (hintergrund) { this.rechteck(0, 0, b, h, hintergrund); }
    }

    setze(x, y, c, alpha = 255) {
        x = Math.round(x); y = Math.round(y);
        if (x < 0 || y < 0 || x >= this.b || y >= this.h || !c) { return; }
        const i = (y * this.b + x) * 4;
        this.d[i] = c[0]; this.d[i + 1] = c[1]; this.d[i + 2] = c[2]; this.d[i + 3] = alpha;
    }

    lies(x, y) { const i = (y * this.b + x) * 4; return [this.d[i], this.d[i + 1], this.d[i + 2], this.d[i + 3]]; }

    loesche(x, y) { const i = (Math.round(y) * this.b + Math.round(x)) * 4; if (x >= 0 && y >= 0 && x < this.b && y < this.h) { this.d[i + 3] = 0; } }

    rechteck(x, y, b, h, c) { for (let j = 0; j < h; j++) { for (let i = 0; i < b; i++) { this.setze(x + i, y + j, c); } } }

    rahmen(x, y, b, h, c) { this.rechteck(x, y, b, 1, c); this.rechteck(x, y + h - 1, b, 1, c); this.rechteck(x, y, 1, h, c); this.rechteck(x + b - 1, y, 1, h, c); }

    ellipse(cx, cy, rx, ry, c) {
        for (let y = -ry; y <= ry; y++) { for (let x = -rx; x <= rx; x++) { if ((x * x) / ((rx + 0.5) * (rx + 0.5)) + (y * y) / ((ry + 0.5) * (ry + 0.5)) <= 1) { this.setze(cx + x, cy + y, c); } } }
    }

    linie(x0, y0, x1, y1, c) {
        x0 = Math.round(x0); y0 = Math.round(y0); x1 = Math.round(x1); y1 = Math.round(y1);
        const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        let e = dx + dy;
        for (;;) { this.setze(x0, y0, c); if (x0 === x1 && y0 === y1) { break; } const e2 = 2 * e; if (e2 >= dy) { e += dy; x0 += sx; } if (e2 <= dx) { e += dx; y0 += sy; } }
    }

    polygon(p, c) {
        const ys = p.map((q) => q[1]);
        for (let y = Math.floor(Math.min(...ys)); y <= Math.ceil(Math.max(...ys)); y++) {
            const xs = [];
            for (let i = 0; i < p.length; i++) { const [ax, ay] = p[i], [bx, by] = p[(i + 1) % p.length]; if ((ay <= y + 0.5 && by > y + 0.5) || (by <= y + 0.5 && ay > y + 0.5)) { xs.push(ax + ((y + 0.5 - ay) * (bx - ax)) / (by - ay)); } }
            xs.sort((a, b) => a - b);
            for (let k = 0; k + 1 < xs.length; k += 2) { for (let x = Math.round(xs[k]); x < Math.round(xs[k + 1]); x++) { this.setze(x, y, c); } }
        }
    }

    /** Waagerechte Streifen in festen Farbstufen (statt Verlauf): farben von oben nach unten, gleich hohe Bänder. */
    baender(x, y, b, h, farben) { const n = farben.length; for (let i = 0; i < n; i++) { const y0 = y + Math.round((i * h) / n), y1 = y + Math.round(((i + 1) * h) / n); this.rechteck(x, y0, b, y1 - y0, farben[i]); } }

    /** Anderes Bild hineinkopieren (Transparenz beachten), optional gespiegelt und ganzzahlig skaliert. */
    einsetzen(quelle, x, y, opt = {}) {
        const s = opt.skala || 1, sx = opt.ausschnitt ? opt.ausschnitt[0] : 0, sy = opt.ausschnitt ? opt.ausschnitt[1] : 0;
        const qb = opt.ausschnitt ? opt.ausschnitt[2] : quelle.b, qh = opt.ausschnitt ? opt.ausschnitt[3] : quelle.h;
        for (let j = 0; j < qh; j++) {
            for (let i = 0; i < qb; i++) {
                const p = quelle.lies(sx + (opt.spiegeln ? qb - 1 - i : i), sy + j);
                if (p[3] < 128) { continue; }
                for (let a = 0; a < s; a++) { for (let c = 0; c < s; c++) { this.setze(x + i * s + c, y + j * s + a, p); } }
            }
        }
    }

    farbeErsetzen(von, nach, bereich) {
        const [x0, y0, b, h] = bereich || [0, 0, this.b, this.h];
        for (let y = y0; y < y0 + h; y++) { for (let x = x0; x < x0 + b; x++) { const p = this.lies(x, y); if (p[3] && p[0] === von[0] && p[1] === von[1] && p[2] === von[2]) { this.setze(x, y, nach, p[3]); } } }
    }

    speichern(datei) { writeFileSync(datei, pngSchreiben(this)); }

    static laden(datei) { return pngLesen(readFileSync(datei)); }
}

// ---------- PNG ----------
const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) { c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; } return c; });
function crc32(buf) { let c = -1; for (const b of buf) { c = CRC[(c ^ b) & 255] ^ (c >>> 8); } return (c ^ -1) >>> 0; }
function block(typ, daten) {
    const laenge = Buffer.alloc(4); laenge.writeUInt32BE(daten.length);
    const td = Buffer.concat([Buffer.from(typ, 'ascii'), daten]);
    const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
    return Buffer.concat([laenge, td, crc]);
}

export function pngSchreiben(bild) {
    const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(bild.b, 0); ihdr.writeUInt32BE(bild.h, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
    const roh = Buffer.alloc((bild.b * 4 + 1) * bild.h);
    for (let y = 0; y < bild.h; y++) { roh[y * (bild.b * 4 + 1)] = 0; Buffer.from(bild.d.buffer, y * bild.b * 4, bild.b * 4).copy(roh, y * (bild.b * 4 + 1) + 1); }
    return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), block('IHDR', ihdr), block('IDAT', deflateSync(roh, { level: 9 })), block('IEND', Buffer.alloc(0))]);
}

export function pngLesen(buf) {
    let i = 8, b = 0, h = 0, ct = 0; const idat = [];
    while (i < buf.length) {
        const n = buf.readUInt32BE(i), typ = buf.toString('ascii', i + 4, i + 8), d = buf.subarray(i + 8, i + 8 + n);
        if (typ === 'IHDR') { b = d.readUInt32BE(0); h = d.readUInt32BE(4); ct = d[9]; }
        if (typ === 'IDAT') { idat.push(d); }
        i += 12 + n;
    }
    const bpp = ct === 6 ? 4 : 3, roh = inflateSync(Buffer.concat(idat)), stride = b * bpp, bild = new Bild(b, h);
    let prev = Buffer.alloc(stride), p = 0;
    for (let y = 0; y < h; y++) {
        const ft = roh[p], z = Buffer.from(roh.subarray(p + 1, p + 1 + stride)); p += 1 + stride;
        for (let x = 0; x < stride; x++) {
            const a = x >= bpp ? z[x - bpp] : 0, o = prev[x], c = x >= bpp ? prev[x - bpp] : 0;
            if (ft === 1) { z[x] = (z[x] + a) & 255; } else if (ft === 2) { z[x] = (z[x] + o) & 255; } else if (ft === 3) { z[x] = (z[x] + ((a + o) >> 1)) & 255; } else if (ft === 4) { const pa = Math.abs(o - c), pb = Math.abs(a - c), pc = Math.abs(a + o - 2 * c); z[x] = (z[x] + (pa <= pb && pa <= pc ? a : pb <= pc ? o : c)) & 255; }
        }
        for (let x = 0; x < b; x++) { bild.setze(x, y, [z[x * bpp], z[x * bpp + 1], z[x * bpp + 2]], bpp === 4 ? z[x * bpp + 3] : 255); }
        prev = z;
    }
    return bild;
}

// ---------- Pixel-Schrift 3×5 für Schilder ----------
const SCHRIFT = {
    A: '111101111101101', B: '110101110101110', C: '111100100100111', D: '110101101101110', E: '111100110100111', F: '111100110100100', G: '111100101101111', H: '101101111101101',
    I: '111010010010111', J: '001001001101111', K: '101101110101101', L: '100100100100111', M: '101111111101101', N: '110101101101101', O: '111101101101111', P: '111101111100100',
    Q: '111101101111001', R: '111101110101101', S: '111100111001111', T: '111010010010010', U: '101101101101111', V: '101101101101010', W: '101101111111101', X: '101101010101101',
    Y: '101101010010010', Z: '111001010100111', Ä: '101010111101101', Ö: '101111101101111', Ü: '101000101101111', '0': '111101101101111', '1': '010110010010111', '2': '111001111100111',
    '3': '111001111001111', '4': '101101111001001', '5': '111100111001111', '6': '111100111101111', '7': '111001001001001', '8': '111101111101111', '9': '111101111001111',
    ' ': '000000000000000', '.': '000000000000010', '-': '000000111000000', '?': '111001011000010', '!': '010010010000010', '§': '011100111001110', ':': '000010000010000', '/': '001001010100100', '©': '111100100100111', '(': '010100100100010', ')': '010001001001010'
};

export function text(bild, s, x, y, c, skala = 1) {
    let cx = x;
    for (const z of s.toUpperCase()) {
        const m = SCHRIFT[z] || SCHRIFT['?'];
        for (let j = 0; j < 5; j++) { for (let i = 0; i < 3; i++) { if (m[j * 3 + i] === '1') { bild.rechteck(cx + i * skala, y + j * skala, skala, skala, c); } } }
        cx += 4 * skala;
    }
    return cx - x;
}

export const textBreite = (s, skala = 1) => s.length * 4 * skala - skala;
