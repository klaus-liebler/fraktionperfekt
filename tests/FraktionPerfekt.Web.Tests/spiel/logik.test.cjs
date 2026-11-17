// Tests der Spiellogik „Joe & Chani und der Gral der Demokratie“ (node --test).
// Diese Tests laufen ohne Browser und geben nie Ton aus; Browser-Tests des Spiels starten stumm (--mute-audio, /spiel/?ton=aus).
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

test('Fallgitter: Joe auf die schmale linke, Chani auf die dicke rechte Plattform – jede Figur steht allein auf ihrer', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const z = S.neuerZustand();
    z.szene = 'tempel';
    S.aktion(z, 'benutzen', 'platte_rechts');          // Joe gehört nicht auf die rechte
    assert.equal(z.platten.rechts, null);
    S.aktion(z, 'benutzen', 'platte_links');
    assert.equal(z.platten.links, 'joe');
    assert.equal(z.flags.gitterOffen, undefined);
    S.figurWechseln(z);
    S.aktion(z, 'benutzen', 'platte_links');           // Chani gehört nicht auf die linke
    assert.equal(z.platten.links, 'joe');
    // Joe steht für sich auf seiner Plattform (verkleinert im Hintergrund), Chani bewegt sich unabhängig
    const joe = G.aufPlattform(z, 'joe');
    assert.ok(joe && joe.skala < 1 && joe.y < G.BODEN && joe.seite === 'links');
    assert.equal(G.aufPlattform(z, 'chani'), null);
    const [links, rechts] = G.plattformen(z);
    assert.ok(links.dicke < rechts.dicke, 'links schmaler als rechts');
    assert.ok(links.oben > G.PLATTFORMEN.links.oben, 'belegte Plattform senkt sich');
    assert.equal(rechts.oben, G.PLATTFORMEN.rechts.oben, 'die andere Plattform bewegt sich nicht mit');
    S.aktion(z, 'benutzen', 'platte_rechts');
    assert.equal(z.flags.gitterOffen, true);
});

test('Plattform verlassen: Wer woandershin geht, steigt herunter; die andere Figur bleibt stehen', () => {
    const z = S.neuerZustand();
    z.szene = 'tempel';
    S.aktion(z, 'benutzen', 'platte_links');
    S.figurWechseln(z);
    S.platteVerlassen(z, 'chani');
    assert.equal(z.platten.links, 'joe');
    S.figurWechseln(z);
    S.platteVerlassen(z, 'joe');
    assert.equal(z.platten.links, null);
});

test('Tempel: Bis zur Brücke bleiben Joe und Chani links des Abgrunds, danach erreichen sie den Gang', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { if (s[0] === 'dialog' && s[1] === 'bruecke') { break; } S.schritt(z, s); }
    assert.equal(z.szene, 'tempel'); assert.equal(z.flags.brueckeDa, undefined);
    const hs = S.sichtbareHotspots(z);
    const st = G.startplaetze(z, hs);
    assert.ok(st.joe <= G.ABGRUND_LINKS && st.chani <= G.ABGRUND_LINKS);
    for (const ziel of [hs.find((h) => h.id === 'gang'), hs.find((h) => h.id === 'abgrund'), 250]) {
        const p = G.standplaetze(z, hs, ziel, 60, 40);
        assert.ok(p.aktiv <= G.ABGRUND_LINKS && p.begleit <= G.ABGRUND_LINKS, JSON.stringify(p));
    }
    assert.match(S.aktion(z, 'benutzen', 'gang').text.join(' '), /Brücke/);
    S.dialogWahl(z, 'bruecke', '1');
    const p = G.standplaetze(z, S.sichtbareHotspots(z), S.sichtbareHotspots(z).find((h) => h.id === 'gang'), 60, 40);
    assert.ok(p.aktiv > 196, `über die Brücke: ${p.aktiv}`);
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
    assert.deepEqual(boese.inventar, ['handy', 'paddel']);   // Joes Handy wird bei alten Ständen ergänzt
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
    for (const n of ['joe', 'joe_hut', 'chani', 'pinsel']) {
        for (const zustand of ['stehen', 'gehen', 'reden', 'benutzen', 'erschrocken']) { assert.ok(G.NAMEN.includes(G.rahmen(n, zustand, 7, true).bild)); }
        for (const a of ['neutral', 'lacht', 'erschrocken']) { assert.ok(G.NAMEN.includes(G.portraet(n, a).bild)); }
    }
});

test('Keine Figur steht vor einer anderen: Start- und Stehplätze überlappen nie (alle Szenen, alle Hotspots)', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const pruefeAbstand = (xs, wo) => {
        for (let i = 0; i < xs.length; i++) { for (let j = i + 1; j < xs.length; j++) { assert.ok(Math.abs(xs[i] - xs[j]) >= G.ABSTAND, `${wo}: ${xs[i]} und ${xs[j]} überlappen`); } }
        for (const x of xs) { assert.ok(x >= 0 && x <= G.B, `${wo}: ${x} außerhalb`); }
    };
    const z = S.neuerZustand();
    const pruefe = () => {
        if (z.szene === 'karte') { return; }
        const hs = S.sichtbareHotspots(z), npc = G.npcPlaetze(z, hs).map((p) => p.x);
        pruefeAbstand(npc, `${z.szene}: Nebenfiguren`);
        const st = G.startplaetze(z, hs);
        pruefeAbstand(npc.concat([st.joe, st.chani]), `${z.szene}: Start`);
        for (const h of hs) {
            for (const [von, begleit] of [[10, 40], [G.B - 10, G.B - 40], [G.B / 2, G.B / 2 + 30]]) {
                const p = G.standplaetze(z, hs, h, von, begleit);
                pruefeAbstand(npc.concat([p.aktiv, p.begleit]), `${z.szene}/${h.id} von ${von}`);
            }
        }
        for (const x of [0, 60, 128, 200, G.B]) { const p = G.standplaetze(z, hs, x, 128, 100); pruefeAbstand(npc.concat([p.aktiv, p.begleit]), `${z.szene}/x=${x}`); }
    };
    pruefe();
    for (const schritt of S.KOMPLETTLOESUNG) { S.schritt(z, schritt); pruefe(); }
});

test('Archiv: Chris blockiert die Tür, bis er den SPD-Antrag aus dem Briefkasten bekommt', () => {
    const z = S.neuerZustand();
    for (const schritt of S.KOMPLETTLOESUNG.slice(0, 6)) { S.schritt(z, schritt); }
    assert.ok(z.inventar.includes('spd_antrag'));
    S.reisen(z, 'archiv');
    assert.match(S.aktion(z, 'reden', 'chris').text.join(' '), /erinnern/);
    assert.equal(z.flags.archivOffen, undefined);
    assert.match(S.aktion(z, 'benutzen', 'archivtuer').text.join(' '), /Chris/);
    const e = S.aktion(z, 'gegenstand', 'chris', 'spd_antrag');
    assert.match(e.text.join(' '), /Tag-der-offenen-Tür/);
    assert.equal(z.flags.archivOffen, true);
    assert.equal(z.inventar.includes('spd_antrag'), false);
});

test('Ältere Spielstände: Mustertagesordnung wird zum SPD-Antrag, der gegründete Arbeitskreis zum übergebenen Antrag', () => {
    const alt = S.laden(JSON.stringify({ version: 1, szene: 'archiv', inventar: ['handy', 'tagesordnung'], flags: { agGegruendet: true } }));
    assert.deepEqual(alt.inventar, ['handy', 'spd_antrag']);
    assert.equal(alt.flags.antragUebergeben, true);
});

test('Emsdeich: Inschrift an der Skulptur, Boot vom Kanuverleih, Flucht endet am Hallenbad', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { if (s[0] === 'aktion' && s[2] === 'skulptur') { break; } S.schritt(z, s); }
    assert.equal(z.szene, 'ems');
    assert.equal(S.sichtbareHotspots(z).some((h) => /Brücke/.test(h.name)), false);
    assert.ok(S.sichtbareHotspots(z).some((h) => h.id === 'verleih'));
    assert.match(S.aktion(z, 'benutzen', 'boot').text.join(' '), /Tafel an der Skulptur/);   // ohne Inschrift kein Ziel
    S.aktion(z, 'f1', 'skulptur');
    const start = S.aktion(z, 'benutzen', 'boot');
    assert.equal(start.minispiel, 'boot');
    assert.match(start.text.join(' '), /alternativen Fakten/);
    const ende = S.minispielEnde(z, 'boot', true);
    assert.equal(z.szene, 'baustelle');
    assert.equal(ende.szene, 'baustelle');
    assert.deepEqual(ende.erscheinen, ['gegner']);
    assert.match(ende.text.join(' '), /geht unter/);
    assert.ok(z.inventar.includes('siegel_exekutive'));
    assert.ok(z.orte.includes('baustelle'));
    // Im Archiv kleben jetzt nur noch die Schuhsohlen
    z.szene = 'karte'; S.reisen(z, 'archiv');
    const ids = S.sichtbareHotspots(z).map((h) => h.id);
    assert.ok(ids.includes('sohlen') && !ids.includes('gegner'));
});

test('Bootsflucht: Die Verfolger schießen alternative Fakten von hinten; sind sie verschossen, sinkt ihr Boot', () => {
    const b = S.bootNeu(5);
    let schuesse = 0, sinken = false;
    while (b.status === 'laeuft') {
        const vorher = b.munition;
        S.bootSchritt(b, 1 / 30, S.bootAutopilot(b));
        if (b.munition < vorher) { schuesse++; assert.ok(b.schuesse[b.schuesse.length - 1].x < S.BOOT_X, 'Schuss kommt von hinten'); }
        if (b.sinken !== null) { sinken = true; assert.equal(b.munition, 0); }
    }
    assert.equal(b.status, 'gewonnen');
    assert.equal(schuesse, S.ALTERNATIVE_FAKTEN.length);
    assert.ok(sinken);
});

test('Bedienung: Verb → Gegenstand → Ziel ohne Zwischenklick, auch wenn noch ein Text steht', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    assert.equal(G.klickBeiText('gib', 'spd_antrag', true), 'ausfuehren');
    assert.equal(G.klickBeiText('benutze', 'stadtplan', true), 'ausfuehren');
    assert.equal(G.klickBeiText('benutze', null, true), 'ausfuehren');
    assert.equal(G.klickBeiText('gehe', null, true), 'weiter');
    assert.equal(G.klickBeiText('gib', 'spd_antrag', false), 'weiter');
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

test('Archiv: Pinsel und Ledeiw kleben nach dem Rätsel verkleinert im Hintergrund, der Ausgang ist frei', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { S.schritt(z, s); if (z.flags.gegnerArchivFest) { break; } }
    assert.equal(z.szene, 'archiv');
    const gegner = S.sichtbareHotspots(z).find((h) => h.id === 'gegner');
    assert.ok(gegner, 'die beiden bleiben sichtbar');
    const plaetze = G.npcPlaetze(z, S.sichtbareHotspots(z)).filter((p) => p.id === 'gegner');
    assert.equal(plaetze.length, 2);
    for (const p of plaetze) { assert.ok(p.hinten && p.klebt && p.skala < 1 && p.y < G.BODEN - 10, JSON.stringify(p)); }
    assert.ok(G.NAMEN.includes('klebemasse_32x6'));
    assert.deepEqual(S.aktion(z, 'benutzen', 'ausgang').szene, 'karte');
});

test('Schule: erst der Flur mit Klassenzimmern, der Keller nur über die Kellertür', () => {
    const z = S.neuerZustand();
    z.szene = 'karte'; z.orte.push('schule'); S.reisen(z, 'schule');
    assert.equal(S.sichtbareHotspots(z).some((h) => h.id === 'kellergang'), false);
    S.aktion(z, 'benutzen', 'eingang');
    assert.equal(z.szene, 'schulflur');
    for (const k of ['klasse1a', 'klasse2b', 'wc']) { const e = S.aktion(z, 'benutzen', k); assert.equal(z.szene, 'schulflur'); assert.ok(e.text.length > 0); }
    assert.match(S.aktion(z, 'benutzen', 'kellertuer').text.join(' '), /20:30/);
    z.flags.abend = true; z.inventar.push('taschenlampe');
    S.aktion(z, 'benutzen', 'kellertuer');
    assert.equal(z.szene, 'krypta');
    S.aktion(z, 'benutzen', 'zurueck');
    assert.equal(z.szene, 'schulflur');
});

test('Karte: Die Baustelle ist am Hallenbad (das Freibad ist nicht im Umbau)', () => {
    assert.match(S.ORTE.baustelle.name, /Hallenbad/);
    assert.match(S.einstieg(S.neuerZustand(), 'baustelle'), /Hallenbad/);
    assert.ok(S.ORTE.ems.y > S.ORTE.baustelle.y, 'der Emsdeich liegt flussauf (südlich) vom Hallenbad');
});

test('Archiv: Nach Festkleben, Speichern und Neuladen stehen beide verkleinert im Hintergrund', () => {
    const G = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));
    const speicher = new Map();
    const plaetze = S.speicherplaetze({ getItem: (k) => speicher.get(k) ?? null, setItem: (k, v) => speicher.set(k, v), removeItem: (k) => speicher.delete(k) });
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { S.schritt(z, s); if (z.szene === 'ems') { break; } }
    plaetze.schreiben('auto', z);
    const geladen = plaetze.lesen('auto');
    geladen.szene = 'karte'; S.reisen(geladen, 'archiv');
    const gegner = G.npcPlaetze(geladen, S.sichtbareHotspots(geladen)).filter((p) => p.id === 'gegner');
    assert.equal(gegner.length, 2);
    assert.ok(gegner.every((p) => p.hinten && p.klebt));
});

test('Service Worker hält das Spiel nie im Cache (sonst laufen nach Änderungen alte Skripte)', () => {
    const fs = require('node:fs');
    const sw = fs.readFileSync(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'wwwroot', 'sw.js'), 'utf8');
    assert.match(sw, /startsWith\('\/spiel\/'\)/);
    assert.doesNotMatch(sw, /fp-huelle-v1'/);
});

test('Joes Handy: Professor Relbeil nennt an jeder Stelle der Komplettlösung den nächsten Schritt, ohne Punkte zu kosten', () => {
    const z = S.neuerZustand();
    assert.ok(z.inventar.includes('handy'));
    for (const s of S.KOMPLETTLOESUNG) {
        const punkte = z.punkte;
        const hilfe = S.anrufen(z, 'hilfe').text.join(' ');
        assert.match(hilfe, /Relbeil: „Natürlich\. .+“/, `Schritt ${JSON.stringify(s)}`);
        assert.equal(z.punkte, punkte);
        S.schritt(z, s);
    }
    assert.match(S.anrufen(z, 'hilfe').text.join(' '), /geschafft/);
    assert.ok(z.punkte >= 400);
    assert.match(S.anrufen(z, 'hallo').text[1], /^Relbeil:/);
    // Konkret: am Anfang den Brief, im Archiv nach dem Rätsel die Kartusche bei Pinsel und Ledeiw
    const neu = S.neuerZustand();
    assert.match(S.naechsterSchritt(neu).join(' '), /Umschlag/);
    for (const s of S.KOMPLETTLOESUNG) { if (s[0] === 'aktion' && s[1] === 'f1' && s[2] === 'gegner') { break; } S.schritt(neu, s); }
    assert.match(S.naechsterSchritt(neu).join(' '), /Kartusche.*Pinsel/);
});

test('Vorstellung nennt nur Fähigkeiten, die es im Spiel gibt', () => {
    const text = S.VORSTELLUNG.join(' ');
    for (const f of [...S.FIGUREN.joe.faehigkeiten, ...S.FIGUREN.chani.faehigkeiten]) { assert.ok(text.includes(f), f); }
    assert.ok(text.includes('Relbeil'));
});
