// Tests der Darstellung (node --test): Abdeckung aller Hotspots durch 3D-Objekte, Auftritte und Abgänge der Figuren,
// CSP-Tauglichkeit der Seite. Ohne Browser und ohne Ton; Browser-Tests starten stumm (/spiel3d/?ton=aus).
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');

const web = path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web');
const ordner = path.join(web, 'Spiel3d');
globalThis.window = globalThis;
const S = require(path.join(ordner, 'logik.js'));

test('Komplettlösung führt bis zum Ende', () => {
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
    assert.ok(Object.values(e.regie).some((k) => k.includes('gegnerArchiv')), 'Pinsel und Ledeiw kommen mit ihrer Zeile');
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
    assert.ok(skripte.length >= 3);
    for (const [, attr, inhalt] of skripte) { assert.match(attr, /src="/); assert.equal(inhalt.trim(), ''); }
    for (const datei of ['logik.js', 'musik.js', 'spiel.css', 'three_min.js', 'buffergeometry.js', 'roomenv.js', 'welt_markt.json', 'welt_rathaus.json']) {
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
    const spruenge = [...quelle.matchAll(/^\s+(\w+): \{ (?:bereich: '(\w+)')?/gm)].map((m) => ({ name: m[1], n: S.SPRUENGE[m[1]], bereich: m[2] }));
    assert.ok(spruenge.length >= 16);
    assert.deepEqual(spruenge.map((sp) => sp.name).sort(), Object.keys(S.SPRUENGE).sort(), '3D-Tabelle und GralSpiel.SPRUENGE passen zusammen');
    assert.equal(S.sprungZustand('gibtsnicht'), null);
    assert.equal(S.sprungZustand('constructor'), null);
    const spick = fs.readFileSync(path.join(ordner, 'cheatsheet.html'), 'utf8');
    for (const sp of spruenge) {
        assert.match(spick, new RegExp(`"index.html\\?start=${sp.name}"`), `Spickzettel: ${sp.name}`);
    }
    assert.doesNotMatch(spick, /\.\.\/spiel\//, 'keine Links mehr auf die frühere Pixel-Fassung');
    const bereiche = fs.readFileSync(path.join(ordner, 'bereiche.js'), 'utf8');
    for (const sp of spruenge) {
        const z = S.sprungZustand(sp.name);
        assert.ok(S.SZENEN[z.szene] || z.szene === 'karte', sp.name);
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

test('Jede realistische Figur hat ihr glb, und jede Nebenfigur lädt eins nach', () => {
    const quelle = fs.readFileSync(path.join(ordner, 'figuren3d.js'), 'utf8');
    const rb = quelle.match(/const RB = \{([\s\S]*?)\n\};/);
    assert.ok(rb, 'RB-Tabelle in figuren3d.js');
    const namen = [...rb[1].matchAll(/^\s+(\w+): \{/gm)].map((m) => m[1]);
    assert.ok(namen.length >= 9, namen.join(', '));
    for (const n of namen) { assert.ok(fs.existsSync(path.join(ordner, `figur_${n}.glb`)), `figur_${n}.glb fehlt`); }
    for (const n of namen.filter((x) => x !== 'joe' && x !== 'chani')) {
        assert.match(quelle, new RegExp(`export function ${n}\\(\\) \\{\\s+return nachladen\\('${n}'`), `${n}() lädt das Modell nach`);
    }
});

test('Wer mitten in einer Szene kommt oder geht, hat einen Auftritts- bzw. Abgangsort', () => {
    const quelle = (d) => fs.readFileSync(path.join(ordner, d), 'utf8');
    const def = (d, anfang) => { const zeile = quelle(d).split('\n').find((l) => l.includes(anfang)); assert.ok(zeile, `${d}: ${anfang}`); return zeile; };
    // Chris geht nach dem Aufschließen; Pinsel und Ledeiw drängen sich vorbei ins Rathaus
    assert.match(def('bereiche.js', "hotspot: 'chris'"), /nach: /);
    assert.match(def('bereiche.js', "figur: 'pinsel', durchzug"), /weg: /);
    assert.match(def('bereiche.js', "figur: 'ledeiw', durchzug"), /weg: /);
    // Festgeklebt rutschen sie an die Rückwand, statt dorthin zu springen
    assert.match(def('bereiche.js', "hotspot: 'gegner', figur: 'pinsel'"), /lage: /);
    // Baustelle: aus der Ems herein, rückwärts in den Bauwagen hinaus
    for (const f of ['pinsel', 'ledeiw']) { assert.match(quelle('akt3.js'), new RegExp(`hotspot: 'gegner', figur: '${f}'[^}]*?von: [\\s\\S]*?nach: [\\s\\S]*?rueckwaerts: true`)); }
    // Finale: Pinsel und Ledeiw kommen und gehen, die Helfer kommen herein
    for (const f of ['pinsel', 'ledeiw']) { assert.match(def('akt45.js', `gegnerAbgang, figur: '${f}'`), /von: .*nach: /); }
    for (const f of ['chris', 'lorette', 'johanna']) { assert.match(def('akt45.js', `Hilft, figur: '${f}'`), /von: /); }
    // Die Darstellung kennt kein sofortiges Verschwinden nach dem Text mehr, sondern nur noch die Regie-Zeilen
    const d = quelle('darstellung3d.js');
    assert.doesNotMatch(d, /gehalten|erscheinen/);
    assert.match(d, /function regieVorbereiten/);
});

test('Radbrücke: über den Deich und den Brückenkopf aufs Deck (nicht seitlich); am Rathaus nicht hinter die Fassade', () => {
    const akt3 = fs.readFileSync(path.join(ordner, 'akt3.js'), 'utf8');
    assert.match(akt3, /off < -55/, 'Ufer und Deich reichen bis knapp hinter die Deichkrone (Brückenkopf bei e = −49)');
    assert.match(fs.readFileSync(path.join(ordner, 'welt_ems.js'), 'utf8'), /const BL = 38, BR = 49;/, 'die Brücke endet auf den Deichkronen');
    assert.match(akt3, /if \(onBridge\(e, n\)\) \{ return Math\.abs\(n\) < 1\.7/, 'seitlich zwischen Deck und Wiese bleibt eine Lücke');
    assert.doesNotMatch(akt3, /Treppe/);
    const d = fs.readFileSync(path.join(ordner, 'darstellung3d.js'), 'utf8');
    assert.match(d, /return wegSuchen\(von, nach, 60\)/, 'weite Umwege findet die Wegsuche mit großem Fenster');
    const bereiche = fs.readFileSync(path.join(ordner, 'bereiche.js'), 'utf8');
    assert.match(bereiche, /z >= 7 && !\(x > 34\.5 && z < 14\)/);
});

test('Radbrücke: Die Stadt aus OpenStreetMap steht lagerichtig im Hintergrund, das Hallenbad hinter dem Brückenkopf', () => {
    const u = JSON.parse(fs.readFileSync(path.join(ordner, 'umgebung_radbruecke.json'), 'utf8'));
    assert.match(u.quelle, /OpenStreetMap/);
    assert.ok(u.gebaeude.length > 500, `${u.gebaeude.length} Gebäude`);
    const bad = u.gebaeude.find((g) => g.name === 'Hallenbad Greven');
    assert.ok(bad, 'Hallenbad dabei');
    const e = bad.p.filter((_, i) => i % 2 === 0), mitte = e.reduce((a, b) => a + b, 0) / e.length;
    assert.ok(mitte < -60 && mitte > -160, `Hallenbad bei e = ${mitte.toFixed(0)}: auf der Seite der Laternenzählung, hinter dem Deich`);
    // keines steht in der nachgebauten Flussaue
    const riverX = (n) => 10 * Math.sin(n / 55) + 4 * Math.sin(n / 23);
    for (const g of u.gebaeude) { for (let i = 0; i < g.p.length; i += 2) { const [x, n] = [g.p[i], g.p[i + 1]]; assert.ok(Math.abs(n) >= 190 || Math.abs(x - riverX(n)) >= 62); } }
});
