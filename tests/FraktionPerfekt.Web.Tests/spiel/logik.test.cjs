// Tests der Spiellogik „Joe & Chani und der Gral der Demokratie“ (node --test).
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const S = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'logik.js'));

test('Komplettlösung führt bis zum Ende (das Spiel ist lösbar)', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) {
        S.schritt(z, s);
    }
    assert.equal(z.ende, true);
    assert.deepEqual(S.SIEGEL.filter((x) => z.inventar.includes(x)), S.SIEGEL);
    assert.ok(z.punkte >= 400, `Punkte: ${z.punkte}`);
});

test('Jeder Schritt der Komplettlösung bewirkt etwas Sichtbares (kein „Da ist nichts.“)', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) {
        const e = S.schritt(z, s);
        assert.notEqual(e.text[0], 'Da ist nichts.', `Schritt ${JSON.stringify(s)} in Szene ${z.szene}`);
    }
});

test('Fähigkeiten sind nötig: Chani kann das Fax nicht, Joe kann das Fresko nicht deuten', () => {
    const z = S.neuerZustand();
    S.figurWechseln(z);
    S.aktion(z, 'benutzen', 'fax');
    assert.equal(z.inventar.includes('stadtplan'), false);
    S.figurWechseln(z);
    S.aktion(z, 'benutzen', 'fax');
    assert.equal(z.inventar.includes('stadtplan'), true);
});

test('Rätsel: falsche Symbolfolge öffnet das Schloss nicht, richtige schon', () => {
    const z = S.neuerZustand();
    z.szene = 'krypta';
    S.aktion(z, 'benutzen', 'schloss');
    S.dialogWahl(z, 'schloss', 'waage');
    S.dialogWahl(z, 'schloss', 'buch');
    S.dialogWahl(z, 'schloss', 'urne');
    assert.equal(z.inventar.includes('tempelschluessel'), false);
    for (const s of S.RICHTIGE_KOMBINATION) { S.dialogWahl(z, 'schloss', s); }
    assert.equal(z.inventar.includes('tempelschluessel'), true);
});

test('Fallgitter öffnet sich nur, wenn beide Figuren auf verschiedenen Platten stehen', () => {
    const z = S.neuerZustand();
    z.szene = 'tempel';
    S.aktion(z, 'benutzen', 'platte_links');
    S.aktion(z, 'benutzen', 'platte_rechts'); // dieselbe Figur wechselt nur die Platte
    assert.equal(z.flags.gitterOffen, undefined);
    S.figurWechseln(z);
    S.aktion(z, 'benutzen', 'platte_links');
    assert.equal(z.flags.gitterOffen, true);
});

test('Geschäftsordnungs-Quiz: Fehler setzt zurück, mit Geschäftsordnung gibt es einen Tipp', () => {
    const z = S.neuerZustand();
    z.szene = 'tempel'; z.flags.gitterOffen = true; z.inventar.push('geschaeftsordnung');
    S.aktion(z, 'benutzen', 'tafel');
    S.dialogWahl(z, 'quiz', '0');
    const e = S.dialogWahl(z, 'quiz', '2');
    assert.equal(z.quiz, 0);
    assert.match(e.text.join(' '), /Geschäftsordnung/);
});

test('Speichern und Laden: Rundreise, fremde Daten werden verworfen', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG.slice(0, 14)) { S.schritt(z, s); }
    const geladen = S.laden(S.speichern(z));
    assert.equal(geladen.szene, z.szene);
    assert.deepEqual(geladen.inventar, z.inventar);
    assert.equal(geladen.punkte, z.punkte);
    assert.deepEqual(geladen.orte, z.orte);
    assert.equal(S.laden('kein json'), null);
    assert.equal(S.laden(JSON.stringify({ version: 99 })), null);
    const boese = S.laden(JSON.stringify({ version: 1, szene: '<script>', inventar: ['gral', 'paddel'], flags: { ende: 'ja', ok: true, '__proto__x': true } }));
    assert.equal(boese.szene, 'buero');
    assert.deepEqual(boese.inventar, ['paddel']);
    assert.equal(boese.flags.ende, undefined);
});

test('Speicherplätze: funktionieren mit Speicher, und das Spiel läuft auch, wenn der Speicher wirft', () => {
    const daten = {};
    const speicher = { getItem: (k) => daten[k] ?? null, setItem: (k, v) => { daten[k] = v; }, removeItem: (k) => { delete daten[k]; } };
    const p = S.speicherplaetze(speicher);
    const z = S.neuerZustand();
    S.aktion(z, 'benutzen', 'brief');
    assert.equal(p.schreiben('1', z), true);
    assert.equal(p.lesen('1').flags.briefGelesen, true);
    assert.equal(p.info('1').szene, 'buero');
    assert.equal(p.lesen('2'), null);
    const kaputt = S.speicherplaetze({ getItem() { throw new Error('gesperrt'); }, setItem() { throw new Error('voll'); }, removeItem() { throw new Error('x'); } });
    assert.equal(kaputt.schreiben('1', z), false);
    assert.equal(kaputt.lesen('1'), null);
    assert.equal(kaputt.info('auto'), null);
});

test('Bootsverfolgung: ohne Steuern verloren, mit Ausweichen gewonnen; Kugel: mit Tempo gewonnen', () => {
    const faul = S.bootNeu(42);
    while (faul.status === 'laeuft') { S.bootSchritt(faul, 1 / 30, null); }
    assert.equal(faul.status, 'verloren');
    const geschickt = S.bootNeu(42);
    while (geschickt.status === 'laeuft') { S.bootSchritt(geschickt, 1 / 30, S.bootAutopilot(geschickt)); }
    assert.equal(geschickt.status, 'gewonnen');

    const steh = S.kugelNeu(false);
    while (steh.status === 'laeuft') { S.kugelSchritt(steh, 1 / 30, false); }
    assert.equal(steh.status, 'verloren');
    for (const rad of [false, true]) {
        const k = S.kugelNeu(rad);
        let n = 0;
        while (k.status === 'laeuft') { S.kugelSchritt(k, 1 / 30, n++ % 5 === 0); }
        assert.equal(k.status, 'gewonnen', `Lastenrad: ${rad}`);
    }
});

test('Reisen nur zu bekannten Orten; nach dem Tempeltor führt die Schule direkt in den Tempel', () => {
    const z = S.neuerZustand();
    z.szene = 'karte';
    assert.match(S.reisen(z, 'ems').text[0], /kennt ihr noch nicht/);
    z.orte.push('schule'); z.flags.tempelOffen = true;
    S.reisen(z, 'schule');
    assert.equal(z.szene, 'tempel');
});

test('Grafiken: alle Bilder liegen in assets/, jede Szene und jeder Spielstand hat ein Bild, Hotspots liegen im Bild', () => {
    const fs = require('node:fs');
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const assets = path.join(__dirname, '..', '..', '..', 'assets');
    for (const n of G.NAMEN) { assert.ok(fs.existsSync(path.join(assets, n + '.png')), `fehlt: ${n}.png`); }
    for (const gid of Object.keys(S.GEGENSTAENDE)) { assert.ok(G.ICONS.includes(gid), `kein Icon für ${gid}`); }
    const z = S.neuerZustand();
    const pruefe = () => {
        assert.ok(G.NAMEN.includes(G.hintergrund(z).replace('.png', '')), `${z.szene}: ${G.hintergrund(z)}`);
        for (const o of G.ueberlagerungen(z, 1.3)) { assert.ok(G.NAMEN.includes(o.bild), o.bild); }
        for (const h of S.sichtbareHotspots(z)) {
            const r = G.R(h, z);
            assert.ok(r.x >= 0 && r.y >= 0 && r.x + r.b <= G.B + 1 && r.y + r.h <= G.H + 1 && r.b > 4 && r.h > 4, `${z.szene}/${h.id} außerhalb: ${JSON.stringify(r)}`);
        }
    };
    pruefe();
    for (const schritt of S.KOMPLETTLOESUNG) { S.schritt(z, schritt); pruefe(); }
    for (const n of ['joe', 'joe_hut', 'chani', 'hohlbein']) {
        for (const zustand of ['stehen', 'gehen', 'reden', 'benutzen', 'erschrocken']) { assert.ok(G.NAMEN.includes(G.rahmen(n, zustand, 7, true).bild)); }
        for (const a of ['neutral', 'lacht', 'erschrocken']) { assert.ok(G.NAMEN.includes(G.portraet(n, a).bild)); }
    }
});

test('Erkennungsmelodie: Fanfare, unverändertes Haydn-Zitat, Schluss; Stimmung stimmt', () => {
    const M = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'musik.js'));
    assert.deepEqual(M.STUECK.teile.map((t) => t.name), ['Fanfare', 'Haydn-Zitat', 'Schluss']);
    const haydn = M.STUECK.teile[1].melodie.map((n) => n[0]).join(' ');
    assert.equal(haydn, 'C5 D5 E5 D5 F5 E5 D5 B4 C5'); // erste Phrase, unverändert
    assert.equal(M.STUECK.teile[1].melodie.reduce((s, n) => s + n[1], 0), 8); // zwei 4/4-Takte
    assert.equal(Math.round(M.frequenz('A4')), 440);
    assert.equal(Math.round(M.frequenz('C5') * 100) / 100, 523.25);
    assert.equal(M.spielen(), 0); // ohne Browser: still, kein Fehler
});
