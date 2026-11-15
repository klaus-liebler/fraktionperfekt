// Verkleinert den einmaligen Overpass-Abruf (greven-osm.json) auf das, was die Karte braucht. Daten: © OpenStreetMap-Mitwirkende, ODbL.
import { readFileSync, writeFileSync } from 'node:fs';
const roh = JSON.parse(readFileSync(new URL('./greven-osm.json', import.meta.url), 'utf8'));
const r = (v) => Math.round(v * 1e5) / 1e5;
const aus = [];
function art(t) {
    if (t.waterway === 'river' || t.waterway === 'canal') { return 'fluss'; }
    if (t.natural === 'water' || t.leisure === 'swimming_pool') { return 'wasser'; }
    if (t.building) { return 'gebaeude'; }
    if (t.highway) { return ['primary', 'secondary', 'tertiary', 'residential', 'pedestrian', 'living_street', 'unclassified'].includes(t.highway) ? 'strasse' : ['footway', 'path', 'cycleway', 'service'].includes(t.highway) ? 'weg' : null; }
    if (t.landuse || t.leisure) { return 'gruen'; }
    return null;
}
for (const e of roh.elements) {
    const t = e.tags || {}, k = art(t);
    if (!k) { continue; }
    if (e.type === 'way' && e.geometry) { aus.push({ k, g: e.geometry.map((p) => [r(p.lat), r(p.lon)]) }); }
    if (e.type === 'relation' && e.members) { for (const m of e.members) { if (m.geometry && m.role === 'outer') { aus.push({ k, g: m.geometry.map((p) => [r(p.lat), r(p.lon)]) }); } } }
}
writeFileSync(new URL('./greven-osm-kompakt.json', import.meta.url), JSON.stringify({ quelle: '© OpenStreetMap-Mitwirkende, ODbL 1.0, Abruf 15.11.2026 per Overpass-API (abfrage.overpassql)', bbox: [52.08, 7.596, 52.099, 7.626], elemente: aus }));
console.log(aus.length, 'Elemente');
