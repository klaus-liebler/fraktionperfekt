// Tests der Spiellogik „Joe & Chani und der Gral der Demokratie“ (node --test).
// Diese Tests laufen ohne Browser und geben nie Ton aus; Browser-Tests des Spiels starten stumm (--mute-audio, /spiel3d/?ton=aus).
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const S = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel3d', 'logik.js'));

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
    assert.equal(z.platten.rechts, null);
    const e = S.aktion(z, 'benutzen', 'platte_rechts');
    assert.match(e.text[e.regie && Object.keys(e.regie)[0]], /Fallgitter hebt sich/);
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

test('Tempel: Ohne Brücke ist der Gang unerreichbar, der Antrag zur Geschäftsordnung baut sie', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { if (s[0] === 'dialog' && s[1] === 'bruecke') { break; } S.schritt(z, s); }
    assert.equal(z.szene, 'tempel'); assert.equal(z.flags.brueckeDa, undefined);
    assert.match(S.aktion(z, 'benutzen', 'gang').text.join(' '), /Brücke/);
    S.dialogWahl(z, 'bruecke', '0');
    assert.equal(z.flags.brueckeDa, undefined);
    S.dialogWahl(z, 'bruecke', '1');
    assert.equal(z.flags.brueckeDa, true);
    assert.equal(S.aktion(z, 'benutzen', 'gang').minispiel, 'kugel');
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
    // Chris geht mit der Zeile, in der er aufschließt; Pinsel und Ledeiw kommen eine Zeile später
    const zeile = (k) => Number(Object.keys(e.regie).find((i) => e.regie[i].includes(k)));
    assert.match(e.text[zeile('archivOffen')], /schließt das Rathaus auf.*geht er/);
    assert.match(e.text[zeile('gegnerArchiv')], /zwei Gestalten/);
    assert.ok(zeile('gegnerArchiv') > zeile('archivOffen'));
    const sichtbar = S.sichtbareHotspots(z).map((h) => h.id);
    assert.ok(!sichtbar.includes('chris'));
    assert.ok(sichtbar.includes('gegner'));
    assert.match(S.aktion(z, 'benutzen', 'ausgang').text.join(' '), /Pinsel und Ledeiw/);
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
    const zeile = Number(Object.keys(ende.regie).find((i) => ende.regie[i].includes('gegnerBaustelle')));
    assert.equal(zeile, ende.text.length - 1, 'Pinsel und Ledeiw kommen erst mit der letzten Zeile');
    assert.match(ende.text.join(' '), /geht unter/);
    assert.ok(!z.inventar.includes('siegel_exekutive'));   // das Siegel steckt in der dritten Laterne der Radbrücke
    assert.match(S.aktion(z, 'benutzen', 'laterne').text.join(' '), /zweite Siegel/);
    assert.ok(z.inventar.includes('siegel_exekutive'));
    assert.equal(S.sichtbareHotspots(z).some((h) => h.id === 'laterne'), false);
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

test('Erkennungsmelodie: Fanfare, unverändertes Haydn-Zitat, Schluss; Stimmung stimmt', () => {
    const M = require(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'Spiel3d', 'musik.js'));
    assert.deepEqual(M.STUECK.teile.map((t) => t.name), ['Fanfare', 'Haydn-Zitat', 'Schluss']);
    const haydn = M.STUECK.teile[1].melodie.map((n) => n[0]).join(' ');
    assert.equal(haydn, 'C5 D5 E5 D5 F5 E5 D5 B4 C5'); // erste Phrase, unverändert
    assert.equal(M.STUECK.teile[1].melodie.reduce((s, n) => s + n[1], 0), 8); // zwei 4/4-Takte
    assert.equal(Math.round(M.frequenz('A4')), 440);
    assert.equal(Math.round(M.frequenz('C5') * 100) / 100, 523.25);
    assert.equal(M.spielen(), 0); // ohne Browser: still, kein Fehler
});

test('Archiv: Pinsel und Ledeiw kleben nach dem Festkleben an der Rückwand, der Ausgang ist frei', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { S.schritt(z, s); if (z.flags.gegnerArchivFest) { break; } }
    assert.equal(z.szene, 'archiv');
    assert.ok(S.sichtbareHotspots(z).find((h) => h.id === 'gegner'), 'die beiden bleiben sichtbar');
    assert.match(S.aktion(z, 'ansehen', 'gegner').text[0], /Rückwand/);
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
});

test('Archiv: Nach Festkleben, Speichern und Neuladen kleben beide noch', () => {
    const speicher = new Map();
    const plaetze = S.speicherplaetze({ getItem: (k) => speicher.get(k) ?? null, setItem: (k, v) => speicher.set(k, v), removeItem: (k) => speicher.delete(k) });
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { S.schritt(z, s); if (z.szene === 'ems') { break; } }
    plaetze.schreiben('auto', z);
    const geladen = plaetze.lesen('auto');
    geladen.szene = 'karte'; S.reisen(geladen, 'archiv');
    assert.ok(S.sichtbareHotspots(geladen).some((h) => h.id === 'gegner'));
    assert.equal(geladen.flags.gegnerArchivFest, true);
    assert.equal(geladen.flags.dichtstoffArchiv, true);
});

test('Service Worker hält das Spiel nie im Cache (sonst laufen nach Änderungen alte Skripte)', () => {
    const fs = require('node:fs');
    const sw = fs.readFileSync(path.join(__dirname, '..', '..', '..', 'src', 'FraktionPerfekt.Web', 'wwwroot', 'sw.js'), 'utf8');
    assert.match(sw, /startsWith\(BASIS \+ 'spiel\/'\)/); // Pfad-Basis (z. B. /fraktionperfekt/) aus dem Geltungsbereich
    assert.match(sw, /startsWith\(BASIS \+ 'spiel3d\/'\)/); // 3D-Fassung ebenfalls nie aus dem Cache
    assert.match(sw, /startsWith\(BASIS \+ 'cheatsheet'\)/);
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

// ---------- Regie: Figuren und Dinge ändern sich erst, nachdem der Grund im Text steht ----------
/** Sichtbarer Teil des Spielstands: Flags (ohne Buchhaltung), Inventar, Ende. */
function sichtbarerStand(z) {
    const flags = {};
    for (const [k, v] of Object.entries(z.flags)) { if (!/^(p|hatte|besucht)_/.test(k) && k !== 'ende') { flags[k] = v; } }
    return { flags, inventar: [...z.inventar], ende: !!z.ende };
}
/** Schlüssel, die sich zwischen zwei Ständen unterscheiden (Flag-Name, Gegenstand oder 'ende'). */
function geaendert(a, b) {
    const k = new Set();
    for (const f of new Set([...Object.keys(a.flags), ...Object.keys(b.flags)])) { if (a.flags[f] !== b.flags[f]) { k.add(f); } }
    for (const g of new Set([...a.inventar, ...b.inventar])) { if (a.inventar.includes(g) !== b.inventar.includes(g)) { k.add(g); } }
    if (a.ende !== b.ende) { k.add('ende'); }
    return k;
}
function regiePruefen(z, tun, wo) {
    const vor = sichtbarerStand(z), e = tun(), nach = sichtbarerStand(z);
    const anders = geaendert(vor, nach), regie = e.regie || {};
    const genannt = new Set(Object.values(regie).flat());
    for (const k of anders) { assert.ok(genannt.has(k), `${wo}: „${k}“ ändert sich, aber keine Textzeile nennt den Grund`); }
    // Umgekehrt darf eine Zeile einen Schlüssel nennen, der schon gilt (Aktion wiederholt) – dann ändert sich eben nichts
    const bekannt = new Set([...Object.keys(S.GEGENSTAENDE), 'ende']);
    for (const k of genannt) { assert.ok(bekannt.has(k) || /^[a-z][A-Za-z0-9]*$/.test(k), `${wo}: unbekannter Regie-Schlüssel „${k}“`); }
    for (const i of Object.keys(regie)) { assert.ok(Number(i) < e.text.length && typeof e.text[i] === 'string', `${wo}: Regie-Zeile ${i} fehlt`); }
    return e;
}
const kopie = (z) => S.laden(S.speichern(z));

test('Regie: Komplettlösung – jede sichtbare Änderung gehört zu der Textzeile, die sie begründet', () => {
    const z = S.neuerZustand();
    S.KOMPLETTLOESUNG.forEach((s, i) => regiePruefen(z, () => S.schritt(z, s), `Schritt ${i + 1} ${JSON.stringify(s)}`));
    assert.equal(z.ende, true);
});

test('Regie: alle Aktionen an allen Hotspots, an jeder Stelle der Komplettlösung, mit beiden Figuren', () => {
    const z = S.neuerZustand();
    let geprueft = 0;
    const allesProbieren = (wo) => {
        if (z.szene === 'karte' || z.ende) { return; }
        for (const figur of ['joe', 'chani']) {
            for (const h of S.sichtbareHotspots(z)) {
                const arten = ['ansehen', 'benutzen', 'reden', 'f1', 'f2'].map((a) => [a]).concat(z.inventar.map((g) => ['gegenstand', g]));
                for (const [art, g] of arten) {
                    const k = kopie(z); k.figur = figur;
                    const e = regiePruefen(k, () => S.aktion(k, art, h.id, g), `${wo}: ${figur} ${art} ${h.id}${g ? ' + ' + g : ''}`);
                    geprueft++;
                    // Dialoge bis zum Ende durchspielen (Schloss, Quiz, Brücke) – jede Wahl einzeln
                    if (e.dialog) {
                        for (const o of S.dialogInhalt(k, e.dialog).optionen) {
                            const d = kopie(k);
                            regiePruefen(d, () => S.dialogWahl(d, e.dialog, o.id), `${wo}: Dialog ${e.dialog} → ${o.id}`);
                        }
                    }
                    if (e.minispiel) {
                        for (const ok of [true, false]) { const m = kopie(k); regiePruefen(m, () => S.minispielEnde(m, e.minispiel, ok), `${wo}: Minispiel ${e.minispiel} ${ok}`); }
                    }
                }
            }
        }
    };
    allesProbieren('Start');
    S.KOMPLETTLOESUNG.forEach((s, i) => { S.schritt(z, s); allesProbieren(`nach Schritt ${i + 1}`); });
    assert.ok(geprueft > 1000, `${geprueft} Aktionen geprüft`);
});

test('Regie: Im Finale kommt jede Helferin und jeder Helfer mit der eigenen Zeile – und nur, wer geholfen hat', () => {
    const z = S.neuerZustand();
    for (const s of S.KOMPLETTLOESUNG) { if (s[0] === 'aktion' && s[2] === 'urne') { break; } S.schritt(z, s); }
    const ohne = kopie(z);
    for (const f of ['antragUebergeben', 'lastenrad', 'stadtplanWeg']) { delete ohne.flags[f]; }
    const e1 = regiePruefen(ohne, () => S.aktion(ohne, 'benutzen', 'urne'), 'Finale ohne Hilfe');
    assert.ok(!e1.text.some((t) => /Chris|Lorette kommt|Johanna/.test(t)));
    const e2 = regiePruefen(z, () => S.aktion(z, 'benutzen', 'urne'), 'Finale mit Hilfe');
    const zeile = (k) => Number(Object.keys(e2.regie).find((i) => e2.regie[i].includes(k)));
    assert.match(e2.text[zeile('chrisHilft')], /Chris kommt herein/);
    assert.match(e2.text[zeile('loretteHilft')], /Lorette kommt herein/);
    assert.match(e2.text[zeile('johannaHilft')], /Johanna kommt/);
    assert.ok(zeile('pokalGenommen') < zeile('gegnerAbgang') && zeile('gegnerAbgang') < zeile('ende'));
});

test('Regie: Alte Spielstände bekommen die neuen Teilschritte nachgetragen (niemand bleibt unsichtbar stecken)', () => {
    const alt = S.laden(JSON.stringify({ version: 1, szene: 'baustelle', inventar: ['handy', 'siegel_exekutive'], flags: { bootGewonnen: true, grubeTrocken: true, gegnerArchivFest: true, kugelGeschafft: true } }));
    for (const f of ['gegnerBaustelle', 'leckDicht', 'dichtstoffArchiv', 'gegnerFinale', 'klappeAuf']) { assert.equal(alt.flags[f], true, f); }
    assert.ok(S.sichtbareHotspots(alt).some((h) => h.id === 'gegner'), 'Pinsel und Ledeiw stehen weiter auf der Baustelle');
});

test('Bootsflucht: genug Zeit zum Ausweichen, und ein Treffer nennt, was getroffen hat', () => {
    const b = S.bootNeu(3);
    // Treibgut taucht bei x = 1,05 auf und erreicht das Boot (BOOT_X) frühestens nach gut drei Sekunden
    let t = 0; while (!b.hindernisse.length) { S.bootSchritt(b, .05, null); t += .05; }
    const h = b.hindernisse[0], start = b.zeit;
    while (h.x > S.BOOT_X + .045 && b.status === 'laeuft') { S.bootSchritt(b, .05, h.spur === b.spur ? (b.spur > 0 ? 'hoch' : 'runter') : null); }
    assert.ok(b.zeit - start > 2.5, `Treibgut braucht ${(b.zeit - start).toFixed(2)} s`);
    // Ohne Ausweichen: Treffer mit Namen, beim dritten ist Schluss
    const c = S.bootNeu(3);
    while (c.status === 'laeuft') { S.bootSchritt(c, .05, null); if (c.treffer === 1 && c.leben === 2) { assert.ok(c.letzterTreffer, 'Treffer hat einen Namen'); } }
    assert.equal(c.status, 'verloren');
    const e = S.minispielEnde(S.neuerZustand(), 'boot', false);
    assert.match(e.text.join(' '), /dritte Treffer.*drei Herzen/);
});
