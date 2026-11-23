// Daten für die 3D-Reisekarte (Spiel3d/karte3d.js): OpenStreetMap-Auszug (© OpenStreetMap-Mitwirkende, ODbL) in Meter
// um die Kartenmitte (x nach Osten, y nach Norden), ganzzahlig gerundet, auf den Kartenausschnitt beschnitten.
// Aufruf: node tools/Grafik/karte3d.mjs → src/FraktionPerfekt.Web/Spiel3d/karte3d.json
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

/** Stationen der Reise (Ort, Koordinaten aus OpenStreetMap). */
const STATIONEN = [
    { nr: '1', name: 'SCHMIEDE', lang: 'Zukunftsschmiede', lat: 52.0938, lon: 7.6120 },
    { nr: '2', name: 'RATHAUS', lang: 'Rathaus', lat: 52.09232, lon: 7.60944 },
    // Emsdeich mit Skulpturen und Kanuverleih: ein Stück flussauf vom Hallenbad (Flusspunkt aus den OSM-Daten)
    { nr: '3', name: 'EMSDEICH', lang: 'Emsdeich (Skulpturen, Kanuverleih)', lat: 52.08747, lon: 7.60266 },
    // Hallenbad Greven, Am Hallenbad 3 (OSM way 45506851, Mittelpunkt des Gebäudes): Baustelle der Sanierung
    { nr: '4', name: 'HALLENBAD', lang: 'Hallenbad / Baustelle Sanierung', lat: 52.09067, lon: 7.60537 },
    { nr: '5', name: 'SCHULE', lang: 'Martin-Luther-Grundschule', lat: 52.09457, lon: 7.60966 }
];

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const daten = JSON.parse(readFileSync(join(wurzel, 'tools', 'Grafik', 'daten', 'greven-osm-kompakt.json'), 'utf8'));

// Kartenausschnitt (Breite/Länge der OSM-Abfrage), Mitte als Ursprung
const LAT0 = 52.0800, LAT1 = 52.0990, LON0 = 7.5960, LON1 = 7.6260;
const LATM = (LAT0 + LAT1) / 2, LONM = (LON0 + LON1) / 2;
const MX = 111320 * Math.cos((LATM * Math.PI) / 180), MY = 110574;
const xy = ([la, lo]) => [Math.round((lo - LONM) * MX), Math.round((la - LATM) * MY)];
const BX = Math.round((LON1 - LON0) * MX / 2), BY = Math.round((LAT1 - LAT0) * MY / 2);
const drin = ([x, y], rand = 0) => Math.abs(x) <= BX + rand && Math.abs(y) <= BY + rand;

/** Punkte ausdünnen (Abstand < 2 m entfällt), flach als [x0, y0, x1, y1, …]. */
function flach(g) {
    const out = [];
    let lx = null, ly = null;
    for (const p of g) {
        const [x, y] = xy(p);
        if (lx !== null && Math.hypot(x - lx, y - ly) < 2) { continue; }
        out.push(x, y); lx = x; ly = y;
    }
    return out;
}

const ebenen = { gebaeude: [], gruen: [], wasser: [], fluss: [], strasse: [], weg: [] };
for (const e of daten.elemente) {
    const p = flach(e.g);
    if (p.length < 4) { continue; }
    // Flächen: Schwerpunkt im Ausschnitt; Linien: mindestens ein Punkt im (erweiterten) Ausschnitt
    if (e.k === 'gebaeude' || e.k === 'gruen' || e.k === 'wasser') {
        let sx = 0, sy = 0; for (let i = 0; i < p.length; i += 2) { sx += p[i]; sy += p[i + 1]; }
        if (!drin([sx / (p.length / 2), sy / (p.length / 2)], e.k === 'gebaeude' ? 0 : 300)) { continue; }
        if (e.k === 'gebaeude' && p.length < 6) { continue; }
    } else {
        let ok = false; for (let i = 0; i < p.length && !ok; i += 2) { ok = drin([p[i], p[i + 1]], 200); }
        if (!ok) { continue; }
    }
    ebenen[e.k].push(p);
}

// Stationen der Reisekarte (Schlüssel wie ORTE in Spiel/logik.js); der Tempel ist nur ein Fragezeichen
const ID = { SCHMIEDE: 'buero', RATHAUS: 'archiv', EMSDEICH: 'ems', HALLENBAD: 'baustelle', SCHULE: 'schule' };
const stationen = STATIONEN.map((s) => ({ id: ID[s.name], nr: s.nr, name: s.lang, p: xy([s.lat, s.lon]) }));

const aus = { quelle: daten.quelle, halb: [BX, BY], stationen, ...ebenen };
const ziel = join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel3d', 'karte3d.json');
writeFileSync(ziel, JSON.stringify(aus));
console.log(ziel, (JSON.stringify(aus).length / 1024).toFixed(0) + ' KB', Object.fromEntries(Object.entries(ebenen).map(([k, v]) => [k, v.length])), 'Ausschnitt ±', BX, BY, 'm');
