// Gebäude der Stadt rund um die Radbrücke beim Hallenbad aus OpenStreetMap (© OpenStreetMap-Mitwirkende, ODbL) für den
// Hintergrund der 3D-Szene „Radbrücke“: node tools/Welten/umgebung.mjs → src/FraktionPerfekt.Web/Spiel3d/umgebung_radbruecke.json
//
// Koordinaten: Ursprung in der Mitte der echten Radbrücke (OSM-Weg 26297520), e entlang der Brücke vom Ostende (Hallenbad,
// e < 0) zum Westende, n quer dazu (n < 0 = flussab, nach Norden). Die Spielbrücke ist kürzer als die echte (Deichkrone bis
// Deichkrone 98 m statt 206 m): Gebäude jenseits der Brückenenden rücken um die Differenz heran, damit z. B. das Hallenbad
// so weit hinter dem Brückenkopf steht wie in echt. Gebäude in der nachgebauten Flussaue (dort ist der Fluss im Spiel
// frei gestaltet) bleiben weg.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const osm = JSON.parse(readFileSync(join(wurzel, 'tools', 'Grafik', 'daten', 'greven-osm.json'), 'utf8'));
const brueckeOsm = osm.elements.find((e) => e.id === 26297520);
const [ost, west] = [brueckeOsm.geometry[0], brueckeOsm.geometry[brueckeOsm.geometry.length - 1]];
const lat0 = (ost.lat + west.lat) / 2, lon0 = (ost.lon + west.lon) / 2;
const MX = Math.cos(lat0 * Math.PI / 180) * 111320, MY = 110574;
const lokal = (p) => [(p.lon - lon0) * MX, (p.lat - lat0) * MY];
const w = lokal(west), halb = Math.hypot(w[0], w[1]), a = [w[0] / halb, w[1] / halb], senk = [-a[1], a[0]];
const SPIEL_HALB = 49;                          // Deichkrone bis Brückenmitte im Spiel (welt_ems.js, BR)
const RADIUS = 800;                             // so weit reicht der Hintergrund
const riverX = (n) => 10 * Math.sin(n / 55) + 4 * Math.sin(n / 23);   // Fluss im Spiel (welt_ems.js)

function spiel([x, y]) {
    const u = x * a[0] + y * a[1], v = x * senk[0] + y * senk[1];
    const e = Math.abs(u) > halb ? Math.sign(u) * (Math.abs(u) - halb + SPIEL_HALB) : u * SPIEL_HALB / halb;
    return [e, v];
}
function hoehe(t, flaeche) {
    const stock = Number(t['building:levels']);
    if (Number.isFinite(Number(t.height)) && Number(t.height) > 2) { return Number(t.height); }
    if (Number.isFinite(stock) && stock > 0) { return stock * 3.1 + 1.2; }
    const art = t.building;
    if (/garage|garages|shed|carport|roof|hut|kiosk/.test(art)) { return 3; }
    if (/apartments|commercial|office|retail/.test(art)) { return 12; }
    if (/school|public|civic|sports_hall|train_station|hospital/.test(art) || flaeche > 1500) { return 10; }
    if (flaeche > 400) { return 8.5; }
    return 7;
}

const gebaeude = [];
for (const el of osm.elements) {
    if (!el.tags || !el.tags.building || !el.geometry || el.geometry.length < 4) { continue; }
    const pts = el.geometry.map(lokal).map(spiel);
    if (pts.length > 2 && pts[0][0] === pts[pts.length - 1][0] && pts[0][1] === pts[pts.length - 1][1]) { pts.pop(); }
    const m = pts.reduce((s, p) => [s[0] + p[0] / pts.length, s[1] + p[1] / pts.length], [0, 0]);
    if (Math.hypot(m[0], m[1]) > RADIUS) { continue; }
    // nicht in die Flussaue des Spiels (Fluss, Wiesen, Deiche: 62 m zu beiden Seiten des Flusslaufs, auf 190 m Länge)
    if (pts.some(([e, n]) => Math.abs(n) < 190 && Math.abs(e - riverX(n)) < 62)) { continue; }
    let f = 0;
    for (let i = 0; i < pts.length; i++) { const p = pts[i], q = pts[(i + 1) % pts.length]; f += p[0] * q[1] - q[0] * p[1]; }
    const flaeche = Math.abs(f) / 2;
    if (flaeche < 12) { continue; }
    gebaeude.push({
        p: pts.flat().map((v) => Math.round(v * 10) / 10),
        h: Math.round(hoehe(el.tags, flaeche) * 10) / 10,
        ...(el.tags.name ? { name: el.tags.name } : {})
    });
}
const ziel = join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel3d', 'umgebung_radbruecke.json');
writeFileSync(ziel, JSON.stringify({ quelle: '© OpenStreetMap-Mitwirkende, ODbL 1.0 (Abruf 15.11.2026, tools/Grafik/daten/abfrage.overpassql)', gebaeude }));
const bad = gebaeude.find((g) => g.name === 'Hallenbad Greven');
console.log(`${gebaeude.length} Gebäude → ${ziel}`);
if (bad) { const p = bad.p; console.log('Hallenbad bei e/n', p[0], p[1], 'Höhe', bad.h); }
