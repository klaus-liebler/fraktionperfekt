// Tests der 3D-Fassung (node --test): dieselbe Spiellogik mit den 3D-Textanpassungen, Abdeckung aller Hotspots von Akt 1–2
// durch 3D-Objekte und CSP-Tauglichkeit der Seite. Ohne Browser und ohne Ton; Browser-Tests starten stumm (/spiel3d/?ton=aus).
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');

const web = path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web');
const ordner = path.join(web, 'Spiel3d');
globalThis.window = globalThis;
const S = require(path.join(web, 'Spiel', 'logik.js'));
globalThis.GralSpiel = S;
require(path.join(ordner, 'anpassung3d.js'));

test('Komplettlösung führt auch mit den 3D-Texten bis zum Ende', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { S.schritt(z, s); }
    assert.equal(z.ende, true);
});

test('Chris steht in 3D vor dem Rathaus: Übergabe des Antrags schließt das Rathaus auf', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG.slice(0, 7)) { S.schritt(z, s); }
    assert.equal(z.szene, 'archiv');
    assert.match(S.einstieg(z, 'archiv'), /Vor dem Eingang/);
    const e = S.aktion(z, 'gegenstand', 'chris', 'spd_antrag');
    assert.ok(e.text.some((t) => t.includes('schließt das Rathaus auf')), e.text.join('\n'));
    assert.ok(e.text.some((t) => t.includes('Treppe zum Archiv')), e.text.join('\n'));
    assert.equal(z.flags.archivOffen, true);
    assert.deepEqual(e.erscheinen, ['gegner']);
});

test('Jeder Hotspot aller Szenen hat in 3D ein Gegenstück', () => {
    const quelle = ['bereiche.js', 'akt3.js', 'akt45.js'].map((d) => fs.readFileSync(path.join(ordner, d), 'utf8')).join('\n');
    const in3d = new Set([...quelle.matchAll(/(?:\bid|hotspot|sperreAktion): '([a-z0-9_]+)'/g)].map((m) => m[1]));
    for (const szene of Object.keys(S.SZENEN)) {
        for (const h of S.SZENEN[szene].hotspots) { assert.ok(in3d.has(h.id), `${szene}/${h.id} fehlt in bereiche.js`); }
    }
});

test('Die Seite kommt ohne Inline-Skripte aus (CSP: script-src self) und bringt three.js selbst mit', () => {
    const html = fs.readFileSync(path.join(ordner, 'index.html'), 'utf8');
    const skripte = [...html.matchAll(/<script([^>]*)>([\s\S]*?)<\/script>/g)];
    assert.ok(skripte.length >= 5);
    for (const [, attr, inhalt] of skripte) { assert.match(attr, /src="/); assert.equal(inhalt.trim(), ''); }
    for (const datei of ['three_min.js', 'buffergeometry.js', 'roomenv.js', 'welt_markt.json', 'welt_rathaus.json']) {
        assert.ok(fs.existsSync(path.join(ordner, datei)), datei);
    }
    for (const datei of fs.readdirSync(ordner).filter((d) => d.endsWith('.js'))) {
        const js = fs.readFileSync(path.join(ordner, datei), 'utf8');
        assert.doesNotMatch(js, /from\s+['"](three|https?:)/, `${datei}: nur lokale Importe`);
        assert.match(datei, /^[a-z0-9_]+\.js$/, 'Dateinamen passen zum Endpunkt /spiel3d/{datei}');
    }
});

test('Direkteinstiege (?start=…) führen zu den richtigen Szenen der Spiellogik', () => {
    const quelle = fs.readFileSync(path.join(ordner, 'darstellung3d.js'), 'utf8');
    const spruenge = [...quelle.matchAll(/^\s+(\w+): \{ schritte: (\d+)(?:, bereich: '(\w+)')?/gm)].map((m) => ({ name: m[1], n: Number(m[2]), bereich: m[3] }));
    assert.ok(spruenge.length >= 16);
    const spick = fs.readFileSync(path.join(ordner, 'cheatsheet.html'), 'utf8');
    for (const sp of spruenge) { assert.match(spick, new RegExp(`start=${sp.name}"`), `Spickzettel: ${sp.name}`); }
    const bereiche = fs.readFileSync(path.join(ordner, 'bereiche.js'), 'utf8');
    for (const sp of spruenge) {
        const z = S.neuerZustand();
        for (const s of S.KOMPLETTLOESUNG.slice(0, sp.n)) { S.schritt(z, s); }
        if (!sp.bereich) { assert.equal(z.szene, 'karte', sp.name); continue; }
        const szene = Object.entries({ buero: ['buero_innen', 'schmiede_aussen'], archiv: ['rathaus_aussen', 'rathaus_flur', 'archiv_innen'], ems: ['ems_deich'], baustelle: ['baustelle', 'radbruecke'], schule: ['schulhof'], schulflur: ['schulflur'], krypta: ['krypta'], tempel: ['tempel_halle', 'tempel_finale'] }).find(([, b]) => b.includes(sp.bereich))[0];
        assert.equal(z.szene, szene, sp.name);
        assert.match(bereiche, new RegExp(sp.bereich), sp.name);
    }
});

test('Chris steht in 3D vor dem Rathaus, weil er selbst Bürgermeister werden wollte', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG.slice(0, 7)) { S.schritt(z, s); }
    assert.match(S.aktion(z, 'reden', 'chris').text.join(' '), /Bürgermeister.*Dietrich Aden/);
});
