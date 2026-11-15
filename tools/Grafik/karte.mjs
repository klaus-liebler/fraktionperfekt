// Reisekarte von Greven aus OpenStreetMap-Daten (© OpenStreetMap-Mitwirkende, ODbL), gerendert im Spielstil:
// Pergament, flache Farben, nummerierte Stationen mit roter Reiselinie, Legende und Namensnennung.
import { readFileSync } from 'node:fs';
import { Bild, F, text, textBreite } from './pixel.mjs';

const B = 256, H = 144;
// Kartenausschnitt (aus der Abfrage): ungefähr quadratisch, ca. 2,1 × 2,1 km → 136 × 136 Pixel, mittig
const LAT0 = 52.0800, LAT1 = 52.0990, LON0 = 7.5960, LON1 = 7.6260;
const KX = 60, KY = 4, KB = 136, KH = 132;

export const STATIONEN = [
    { nr: '1', name: 'SCHMIEDE', lang: 'Zukunftsschmiede', lat: 52.0938, lon: 7.6120 },
    { nr: '2', name: 'RATHAUS', lang: 'Rathaus', lat: 52.09232, lon: 7.60944 },
    { nr: '3', name: 'EMS-UFER', lang: 'Ems-Ufer', lat: null, lon: null },
    { nr: '4', name: 'FREIBAD', lang: 'Freibad / Baustelle Badsanierung', lat: 52.08255, lon: 7.61905 },
    { nr: '5', name: 'SCHULE', lang: 'Martin-Luther-Grundschule', lat: 52.09457, lon: 7.60966 },
    { nr: '?', name: 'TEMPEL', lang: 'Krypta und Tempel (erfunden)', lat: 52.0957, lon: 7.6020 }
];

const px = (lon) => KX + ((lon - LON0) / (LON1 - LON0)) * KB;
const py = (lat) => KY + ((LAT1 - lat) / (LAT1 - LAT0)) * KH;

function zeichneLinienzug(b, g, farbe, dicke) {
    for (let i = 1; i < g.length; i++) {
        const [la0, lo0] = g[i - 1], [la1, lo1] = g[i];
        for (let d = 0; d < dicke; d++) { b.linie(px(lo0) + (d % 2), py(la0) + Math.floor(d / 2), px(lo1) + (d % 2), py(la1) + Math.floor(d / 2), farbe); }
    }
}

export function karteGreven(datei) {
    const daten = JSON.parse(readFileSync(datei, 'utf8'));
    const b = new Bild(B, H, F.sand);
    // Pergamentrand
    b.rechteck(0, 0, B, 2, F.sand2); b.rechteck(0, H - 2, B, 2, F.sand2); b.rechteck(0, 0, 2, H, F.sand2); b.rechteck(B - 2, 0, 2, H, F.sand2);
    // Kartenfläche
    const karte = new Bild(B, H);
    karte.rechteck(KX, KY, KB, KH, F.creme);
    const ordnung = ['gruen', 'wasser', 'weg', 'strasse', 'gebaeude', 'fluss'];
    for (const k of ordnung) {
        for (const e of daten.elemente.filter((x) => x.k === k)) {
            const punkte = e.g.map(([la, lo]) => [px(lo), py(la)]);
            if (k === 'gebaeude') { karte.polygon(punkte, F.sand2); }
            else if (k === 'gruen') { karte.polygon(punkte, [184, 216, 120]); }
            else if (k === 'wasser') { karte.polygon(punkte, F.hellblau); }
            else if (k === 'fluss') { zeichneLinienzug(karte, e.g, F.blau, 4); }
            else if (k === 'strasse') { zeichneLinienzug(karte, e.g, F.weiss, 2); }
            else { zeichneLinienzug(karte, e.g, F.beige, 1); }
        }
    }
    // nur innerhalb des Kartenrahmens übernehmen
    for (let y = KY; y < KY + KH; y++) { for (let x = KX; x < KX + KB; x++) { const p = karte.lies(x, y); if (p[3]) { b.setze(x, y, p); } } }
    b.rahmen(KX - 1, KY - 1, KB + 2, KH + 2, F.holz);

    // Ems-Station: nächster Flusspunkt zum Rathaus
    const ems = daten.elemente.filter((e) => e.k === 'fluss').flatMap((e) => e.g)
        .reduce((best, p) => { const d = (p[0] - 52.0922) ** 2 + ((p[1] - 7.6094) * 0.62) ** 2; return d < best.d ? { d, p } : best; }, { d: 1e9, p: null }).p;
    const stationen = STATIONEN.map((s) => (s.nr === '3' ? { ...s, lat: ems[0], lon: ems[1] } : s));

    // rote Reiselinie (gestrichelt) in Spielreihenfolge
    for (let i = 1; i < stationen.length; i++) {
        const a = stationen[i - 1], c = stationen[i];
        const x0 = px(a.lon), y0 = py(a.lat), x1 = px(c.lon), y1 = py(c.lat), n = Math.max(1, Math.round(Math.hypot(x1 - x0, y1 - y0)));
        for (let t = 0; t <= n; t++) { if (t % 4 < 3) { b.rechteck(Math.round(x0 + ((x1 - x0) * t) / n), Math.round(y0 + ((y1 - y0) * t) / n), 2, 2, F.rot); } }
    }
    for (const s of stationen) {
        const x = Math.round(px(s.lon)), y = Math.round(py(s.lat));
        b.ellipse(x, y, 4, 4, s.nr === '?' ? F.lila : F.dunkelrot); b.ellipse(x, y, 3, 3, s.nr === '?' ? F.flieder : F.rot);
        text(b, s.nr, x - 1, y - 2, F.weiss);
    }
    // Legende links
    text(b, 'GREVEN', 8, 8, F.holzdunkel, 2);
    stationen.forEach((s, i) => { b.ellipse(10, 34 + i * 12, 4, 4, s.nr === '?' ? F.flieder : F.rot); text(b, s.nr, 9, 32 + i * 12, F.weiss); text(b, s.name, 17, 32 + i * 12, F.holzdunkel); });
    // Kompass rechts
    const kx = 226, ky = 40;
    b.polygon([[kx, ky - 16], [kx + 4, ky], [kx, ky + 16], [kx - 4, ky]], F.holz); b.polygon([[kx - 16, ky], [kx, ky - 4], [kx + 16, ky], [kx, ky + 4]], F.holz);
    b.polygon([[kx, ky - 16], [kx + 4, ky], [kx - 4, ky]], F.rot); b.ellipse(kx, ky, 2, 2, F.gelb); text(b, 'N', kx - 1, ky - 24, F.holzdunkel);
    // Maßstab
    const m500 = (500 / (111320 * Math.cos((52.09 * Math.PI) / 180))) / (LON1 - LON0) * KB;
    b.rechteck(206, 76, Math.round(m500), 2, F.holzdunkel); text(b, '500 M', 206, 80, F.holzdunkel);
    // Kaffeefleck
    b.ellipse(232, 108, 9, 8, F.sand2); b.ellipse(232, 108, 7, 6, F.sand);
    // Namensnennung
    const credit = '(C) OPENSTREETMAP-MITWIRKENDE';
    b.rechteck(B - textBreite(credit) - 8, H - 10, textBreite(credit) + 6, 8, F.creme); text(b, credit, B - textBreite(credit) - 5, H - 9, F.holzdunkel);
    return b;
}
