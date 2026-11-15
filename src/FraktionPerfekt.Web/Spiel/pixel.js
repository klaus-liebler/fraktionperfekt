// Pixel-Grundlage: Palette, Pixelpuffer (ImageData), geordnetes Dithering, Formen. Alle Grafiken des Spiels entstehen hier im Code.
(function (wurzel) {
    'use strict';

    // Palette (VGA-Stil, eigene Farben). Name → [r, g, b]
    const P = {
        schwarz: [12, 8, 10], nacht: [22, 20, 40], tinte: [34, 30, 58], dunkelblau: [36, 48, 92], blau: [58, 84, 150], himmel: [96, 140, 206], hellblau: [150, 190, 232], eis: [206, 226, 244],
        violett: [92, 58, 120], lila: [140, 86, 150], rosa: [220, 130, 150], abendrot: [236, 120, 80], orange: [242, 160, 70], gold: [248, 204, 90], gelb: [252, 232, 140], creme: [248, 238, 210],
        weiss: [252, 252, 248], hellgrau: [196, 196, 200], grau: [140, 140, 150], dunkelgrau: [84, 84, 96], anthrazit: [52, 52, 62],
        dunkelbraun: [58, 34, 24], braun: [104, 64, 38], holz: [146, 94, 52], hellholz: [190, 134, 76], sand: [214, 180, 120], ocker: [184, 136, 64], terrakotta: [170, 84, 52], ziegel: [140, 58, 40], dunkelrot: [100, 28, 30], rot: [196, 48, 44],
        dunkelgruen: [28, 70, 44], gruen: [58, 120, 60], hellgruen: [120, 176, 82], moos: [92, 110, 52], tuerkis: [46, 140, 140], wasser: [44, 98, 150], tiefwasser: [26, 60, 104],
        haut: [236, 190, 156], hautschatten: [204, 150, 120], hauthell: [248, 214, 186], hautdunkel: [172, 120, 92],
        stein: [176, 160, 136], steinhell: [214, 200, 170], steindunkel: [120, 104, 86]
    };
    const hex = (c) => `rgb(${c[0]},${c[1]},${c[2]})`;
    const farbe = (c) => (typeof c === 'string' ? (P[c] || [255, 0, 255]) : c);

    // Bayer-4×4-Matrix für geordnetes Dithering
    const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5].map((v) => (v + 0.5) / 16);

    class Puffer {
        constructor(b, h) {
            this.b = b; this.h = h;
            this.daten = new Uint8ClampedArray(b * h * 4);
        }

        setze(x, y, c) {
            x |= 0; y |= 0;
            if (x < 0 || y < 0 || x >= this.b || y >= this.h) { return; }
            const i = (y * this.b + x) * 4, f = farbe(c);
            this.daten[i] = f[0]; this.daten[i + 1] = f[1]; this.daten[i + 2] = f[2]; this.daten[i + 3] = 255;
        }

        lies(x, y) { const i = (y * this.b + x) * 4; return [this.daten[i], this.daten[i + 1], this.daten[i + 2]]; }

        rechteck(x, y, b, h, c) { for (let j = 0; j < h; j++) { for (let i = 0; i < b; i++) { this.setze(x + i, y + j, c); } } }

        /** Fläche mit zwei Farben gemischt: anteil 0 → nur a, 1 → nur b (geordnetes Dithering). */
        mische(x, y, b, h, a, c, anteil) {
            for (let j = 0; j < h; j++) {
                for (let i = 0; i < b; i++) {
                    const px = x + i, py = y + j;
                    this.setze(px, py, BAYER[(py & 3) * 4 + (px & 3)] < anteil ? c : a);
                }
            }
        }

        /** Senkrechter Verlauf über eine Farbliste (gedithert). */
        verlauf(x, y, b, h, farben) {
            const n = farben.length - 1;
            for (let j = 0; j < h; j++) {
                const t = (j / Math.max(1, h - 1)) * n, k = Math.min(n - 1, Math.floor(t)), f = t - k;
                for (let i = 0; i < b; i++) {
                    const px = x + i, py = y + j;
                    this.setze(px, py, BAYER[(py & 3) * 4 + (px & 3)] < f ? farben[k + 1] : farben[k]);
                }
            }
        }

        /** Waagerechter Verlauf. */
        verlaufH(x, y, b, h, farben) {
            const n = farben.length - 1;
            for (let i = 0; i < b; i++) {
                const t = (i / Math.max(1, b - 1)) * n, k = Math.min(n - 1, Math.floor(t)), f = t - k;
                for (let j = 0; j < h; j++) {
                    const px = x + i, py = y + j;
                    this.setze(px, py, BAYER[(py & 3) * 4 + (px & 3)] < f ? farben[k + 1] : farben[k]);
                }
            }
        }

        ellipse(cx, cy, rx, ry, c) {
            for (let y = -ry; y <= ry; y++) { for (let x = -rx; x <= rx; x++) { if ((x * x) / (rx * rx) + (y * y) / (ry * ry) <= 1) { this.setze(cx + x, cy + y, c); } } }
        }

        linie(x0, y0, x1, y1, c) {
            x0 |= 0; y0 |= 0; x1 |= 0; y1 |= 0;
            const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            let e = dx + dy;
            for (;;) {
                this.setze(x0, y0, c);
                if (x0 === x1 && y0 === y1) { break; }
                const e2 = 2 * e;
                if (e2 >= dy) { e += dy; x0 += sx; }
                if (e2 <= dx) { e += dx; y0 += sy; }
            }
        }

        /** Polygon füllen (Scanline). punkte: [[x,y],…] */
        polygon(punkte, c) {
            const ys = punkte.map((p) => p[1]);
            for (let y = Math.floor(Math.min(...ys)); y <= Math.ceil(Math.max(...ys)); y++) {
                const xs = [];
                for (let i = 0; i < punkte.length; i++) {
                    const [ax, ay] = punkte[i], [bx, by] = punkte[(i + 1) % punkte.length];
                    if ((ay <= y && by > y) || (by <= y && ay > y)) { xs.push(ax + ((y - ay) * (bx - ax)) / (by - ay)); }
                }
                xs.sort((a, b) => a - b);
                for (let k = 0; k + 1 < xs.length; k += 2) { for (let x = Math.ceil(xs[k]); x <= Math.floor(xs[k + 1]); x++) { this.setze(x, y, c); } }
            }
        }

        /** Rauschen/Textur: einzelne Pixel einer Farbe mit Wahrscheinlichkeit p (deterministisch). */
        sprenkel(x, y, b, h, c, p, saat) {
            let s = saat || 1;
            for (let j = 0; j < h; j++) { for (let i = 0; i < b; i++) { s = (s * 1103515245 + 12345) & 0x7fffffff; if ((s % 1000) / 1000 < p) { this.setze(x + i, y + j, c); } } }
        }

        /** Abdunkeln/aufhellen über Mischung mit einer Farbe (gedithert), z. B. für Schatten und Lichtkegel. */
        toenen(x, y, b, h, c, anteil) {
            for (let j = 0; j < h; j++) {
                for (let i = 0; i < b; i++) {
                    const px = x + i, py = y + j;
                    if (BAYER[(py & 3) * 4 + (px & 3)] < anteil) { this.setze(px, py, c); }
                }
            }
        }

        nachCanvas() {
            const c = document.createElement('canvas'); c.width = this.b; c.height = this.h;
            c.getContext('2d').putImageData(new ImageData(this.daten, this.b, this.h), 0, 0);
            return c;
        }
    }

    /** Kleines Zeichen-Hilfsobjekt direkt auf einen 2D-Kontext (für Sprites und Animationen). */
    function stift(g) {
        return {
            r(x, y, b, h, c) { g.fillStyle = hex(farbe(c)); g.fillRect(Math.round(x), Math.round(y), b, h); },
            p(x, y, c) { g.fillStyle = hex(farbe(c)); g.fillRect(Math.round(x), Math.round(y), 1, 1); }
        };
    }

    wurzel.Pixel = { P, hex, farbe, Puffer, stift, BAYER };
})(typeof globalThis !== 'undefined' ? globalThis : this);
