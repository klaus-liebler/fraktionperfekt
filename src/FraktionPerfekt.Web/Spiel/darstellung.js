// Joe & Chani und der Gral der Demokratie – Darstellung im Stil der Point-and-Click-Adventures der frühen 90er:
// 256×144 Pixel-Art aus assets/ (ganzzahlig hochskaliert, Pixel scharf), Verbleiste, Satzzeile, Inventar mit Icons, laufende Figuren, Porträts mit
// Mimik, Dialogauswahl, Irisblende. Tippen = Standardaktion, langes Drücken = Verbmenü.
(function () {
    'use strict';
    const S = window.GralSpiel, G = window.Grafik;
    const B = G.B, H = G.H, SKALA = 4;
    const $ = (id) => document.getElementById(id);
    const leinwand = $('leinwand');
    leinwand.width = B * SKALA; leinwand.height = H * SKALA;
    const g = leinwand.getContext('2d');
    const tief = document.createElement('canvas'); tief.width = B; tief.height = H;
    const t2 = tief.getContext('2d');
    const textbox = $('textbox'), textEl = $('text'), portraitEl = $('portrait'), dialogEl = $('dialog'), menueEl = $('menue'), kontextEl = $('kontext'), titelEl = $('titel');
    const inventarEl = $('inventar'), figurenEl = $('figuren'), verbenEl = $('verben'), satzEl = $('satz'), punkteEl = $('punkte');

    let speicherObjekt = null;
    try { speicherObjekt = window.localStorage; } catch (_) { speicherObjekt = null; }
    const plaetze = S.speicherplaetze(speicherObjekt || { getItem() { throw new Error('kein Speicher'); }, setItem() { throw new Error('kein Speicher'); }, removeItem() { } });

    // gehalten: Plätze der Nebenfiguren vor der letzten Aktion – Figuren verschwinden oder wechseln den Platz erst, wenn der Text gelesen ist
    let gehalten = null;
    let z = null, warteschlange = [], danach = null, mini = null, pausiert = false, hover = null, auswahl = -1, reise = null, letzterOrt = 'buero';
    let verb = 'gehe', objekt = null;          // gewähltes Verb und Gegenstand („Benutze X mit …“)
    let blende = null;                          // { t, ziel } Irisblende
    let zeit = 0, geladen = false;
    // Startplatz: rechts neben den Personen am linken Bildrand; im Büro links vom Schreibtisch, damit der Umschlag sichtbar bleibt
    // Startplätze kommen aus Grafik.startplaetze: nie vor oder hinter einer anderen Person
    function aufStart() { const p = z && z.szene !== 'karte' ? G.startplaetze(z, S.sichtbareHotspots(z)) : { joe: 64, chani: 90 }; const j = p.joe, c = p.chani; held.joe.x = held.joe.ziel = j; held.chani.x = held.chani.ziel = c; held.joe.blick = held.chani.blick = 1; }
    const held = { joe: { x: 64, ziel: 64, blick: 1, schritt: 0 }, chani: { x: 84, ziel: 84, blick: 1, schritt: 0 } };
    let laufAuftrag = null;                     // wird nach Ankunft ausgeführt
    let sprecher = null, sprecherSchreck = false; // für Sprechanimation
    const R = (h) => G.R(h, z);
    const hutAuf = () => !!z && z.inventar.includes('taschenlampe');
    const figurName = (n) => (n === 'joe' && hutAuf() ? 'joe_hut' : n);
    function portraetBild(n, ausdruck) { return G.ausschnitt(G.portraet(n === 'joe' && hutAuf() ? 'joe_hut' : n, ausdruck), 64, 64); }
    let ton = true;
    try { ton = window.localStorage.getItem('fp-spiel-ton') !== 'aus'; } catch (_) { ton = true; }
    const STUMM = /[?&]ton=aus(&|$)/.test(window.location.search);   // Tests: nie Ton ausgeben

    // ---------- Ton ----------
    // Effekte: jede Note mit weicher Hüllkurve (Attack/Release ≥ 5 ms, Ende bei 0), Tiefpass, gemeinsamer Bus mit Headroom
    // und Kompressor; Knoten werden nach dem Ende getrennt. So knackt es auch im Kopfhörer nicht.
    let audio = null, effektBus = null;
    function bus() {
        audio = audio || new (window.AudioContext || window.webkitAudioContext)();
        if (!effektBus) {
            const k = audio.createDynamicsCompressor();
            k.threshold.value = -20; k.knee.value = 12; k.ratio.value = 8; k.attack.value = 0.003; k.release.value = 0.15;
            effektBus = audio.createGain(); effektBus.gain.value = 0.5;
            effektBus.connect(k).connect(audio.destination);
        }
        if (audio.state === 'suspended') { audio.resume(); }
        return effektBus;
    }
    function klang(art) {
        if (!ton || STUMM) { return; }
        try {
            const ziel = bus(), t = audio.currentTime + 0.01;
            const folge = { klick: [[660, 0.05]], schritt: [[140, 0.05]], erfolg: [[523, 0.1], [659, 0.1], [784, 0.18]], fehler: [[220, 0.14], [165, 0.2]], kartusche: [[90, 0.22], [65, 0.18]], ende: [[523, 0.15], [659, 0.15], [784, 0.15], [1046, 0.4]], blende: [[300, 0.07], [200, 0.09]] }[art] || [[440, 0.06]];
            const spitze = art === 'schritt' ? 0.012 : art === 'klick' ? 0.03 : 0.05;
            let start = t;
            for (const [f, d] of folge) {
                const o = audio.createOscillator(), filter = audio.createBiquadFilter(), v = audio.createGain();
                o.type = art === 'kartusche' ? 'sawtooth' : art === 'schritt' ? 'triangle' : 'square'; o.frequency.value = f;
                filter.type = 'lowpass'; filter.frequency.value = art === 'schritt' ? 500 : 2200;
                v.gain.setValueAtTime(0, start);
                v.gain.linearRampToValueAtTime(spitze, start + 0.006);
                v.gain.setValueAtTime(spitze, start + d * 0.5);
                v.gain.linearRampToValueAtTime(0, start + d);
                o.connect(filter).connect(v).connect(ziel);
                o.start(start); o.stop(start + d + 0.02);
                o.onended = () => { o.disconnect(); filter.disconnect(); v.disconnect(); };
                start += d * 0.9;
            }
        } catch (_) { /* ohne Ton */ }
    }

    // ---------- Verben ----------
    const VERBEN = [['gehe', 'Gehe zu'], ['ansehen', 'Schau an'], ['nimm', 'Nimm'], ['benutze', 'Benutze'], ['reden', 'Rede mit'], ['gib', 'Gib'], ['f1', ''], ['f2', '']];
    function verbName(v) { if (v === 'f1' || v === 'f2') { return S.FIGUREN[z.figur].faehigkeiten[v === 'f1' ? 0 : 1]; } return (VERBEN.find((x) => x[0] === v) || ['', ''])[1]; }

    function satz() {
        const ziel = hover ? zielName(hover) : '';
        if (objekt) { return `${verb === 'gib' ? 'Gib' : 'Benutze'} ${S.GEGENSTAENDE[objekt].name} ${verb === 'gib' ? 'an' : 'mit'} ${ziel}`.trim(); }
        if (verb === 'f1' || verb === 'f2') { return `${verbName(verb)}: ${ziel}`.trim(); }
        return `${verbName(verb)} ${ziel}`.trim();
    }
    function zielName(id) {
        if (z.szene === 'karte') { return S.ORTE[id] ? S.ORTE[id].name : ''; }
        const h = S.sichtbareHotspots(z).find((x) => x.id === id);
        return h ? h.name : '';
    }

    // ---------- Figurenlage ----------
    const BODEN = G.BODEN;
    const npcName = G.npcName;

    // ---------- Zeichnen ----------
    function teil(t) { const b = G.bild(t.bild); if (b) { t2.drawImage(b, t.sx, t.sy || 0, t.b, t.h, Math.round(t.x), Math.round(t.y), t.b, t.h); } }

    /** Figur mit Fußpunkt (x, fussY) zeichnen; seitliche Bilder schauen nach rechts und werden für blick < 0 gespiegelt. */
    function zeichneFigur(name, zustand, bildNr, x, fussY, blick, blinzeln, schatten, skala, hinten, klebt) {
        const r = G.rahmen(name, zustand, bildNr, blinzeln), b = G.bild(r.bild);
        if (!b) { return; }
        const s = skala || 1, bb = Math.round(32 * s), bh = Math.round(64 * s);
        const px = Math.round(x - G.FUSS_X * s), py = Math.round(fussY - G.FUSS_Y * s);
        if (schatten !== false) { t2.globalAlpha = 0.3; t2.fillStyle = '#000'; t2.beginPath(); t2.ellipse(Math.round(x), fussY, Math.round(9 * s), 2, 0, 0, Math.PI * 2); t2.fill(); t2.globalAlpha = 1; }
        // Festgeklebt: schwarze Dichtstoff-Raupe unter den Schuhen
        const kleber = klebt && G.bild('klebemasse_32x6');
        if (kleber) { t2.drawImage(kleber, 0, 0, 32, 6, Math.round(x - 16 * s), Math.round(fussY - 4 * s), bb, Math.max(3, Math.round(6 * s))); }
        if (hinten) { t2.save(); t2.filter = 'brightness(0.72)'; }
        if (r.seitlich && blick < 0) { t2.save(); t2.translate(Math.round(x) * 2, 0); t2.scale(-1, 1); t2.drawImage(b, r.sx, 0, 32, 64, px, py, bb, bh); t2.restore(); }
        else { t2.drawImage(b, r.sx, 0, 32, 64, px, py, bb, bh); }
        if (hinten) { t2.restore(); }
        return py;
    }

    // Balance-Plattformen: Drehzapfen, Platte (links schmal, rechts dick), Gravur; belegt: senkt sich und leuchtet
    const FARBE = { stein: 'rgb(188,172,140)', steinhell: 'rgb(216,204,172)', steindunkel: 'rgb(140,124,96)', fastschwarz: 'rgb(36,36,44)', gelb: 'rgb(248,184,0)', hellgelb: 'rgb(248,216,120)', holzdunkel: 'rgb(80,40,8)' };
    function zeichnePlattformen(liste) {
        for (const p of liste) {
            const m = Math.round(p.x + p.b / 2), unten = p.oben + p.dicke;
            t2.fillStyle = FARBE.fastschwarz; t2.globalAlpha = 0.3; t2.beginPath(); t2.ellipse(m, p.boden, 14, 2, 0, 0, Math.PI * 2); t2.fill(); t2.globalAlpha = 1;
            t2.fillStyle = FARBE.steindunkel; t2.fillRect(m - 9, p.boden - 2, 18, 2); t2.fillRect(m - 4, unten, 8, p.boden - unten);
            t2.fillStyle = FARBE.stein; t2.fillRect(m - 3, unten, 3, p.boden - unten - 2);
            t2.fillStyle = FARBE.steindunkel; t2.fillRect(p.x, p.oben, p.b, p.dicke);
            t2.fillStyle = p.belegt ? FARBE.gelb : FARBE.stein; t2.fillRect(p.x + 1, p.oben, p.b - 2, p.dicke - 1);
            t2.fillStyle = p.belegt ? FARBE.hellgelb : FARBE.steinhell; t2.fillRect(p.x + 1, p.oben, p.b - 2, 1);
            // Gravur am Sockel: links ein Hut (Joe), rechts eine Kamera (Chani)
            t2.fillStyle = FARBE.holzdunkel;
            if (p.seite === 'links') { t2.fillRect(m - 4, p.boden - 6, 9, 1); t2.fillRect(m - 2, p.boden - 9, 5, 3); }
            else { t2.fillRect(m - 4, p.boden - 9, 9, 5); t2.fillRect(m - 2, p.boden - 10, 3, 1); t2.fillStyle = FARBE.stein; t2.fillRect(m, p.boden - 8, 2, 2); }
        }
    }

    /** Anzeige-Spielstand: Öffnet sich das Fallgitter, bleibt der Raum mit Plattformen sichtbar, bis der Text gelesen ist. */
    let gitterGehalten = false;
    function anzeige() { return gitterGehalten ? Object.assign({}, z, { flags: Object.assign({}, z.flags, { gitterOffen: false }) }) : z; }

    function zeichneSzene() {
        const hs = S.sichtbareHotspots(z), za = anzeige();
        const bg = G.bild(G.hintergrund(za));
        if (bg) { t2.drawImage(bg, 0, 0); }
        for (const o of G.ueberlagerungen(za, zeit)) { teil(o); }
        zeichnePlattformen(G.plattformen(za));
        // Fackeln flackern
        for (const [fx, fy] of G.fackeln(za)) {
            const f = Math.floor(zeit * 10 + fx) % 3;
            t2.fillStyle = f === 0 ? 'rgb(252,152,56)' : f === 1 ? 'rgb(252,216,120)' : 'rgb(252,252,188)';
            t2.fillRect(fx + (f === 2 ? 0 : 1), fy - 9 - f, 1 + (f === 1 ? 1 : 0), 2);
            t2.globalAlpha = 0.05 + f * 0.02; t2.fillStyle = 'rgb(255,200,100)'; t2.beginPath(); t2.arc(fx + 1, fy - 4, 16 + f, 0, Math.PI * 2); t2.fill(); t2.globalAlpha = 1;
        }
        // Wasser glitzert
        if (z.szene === 'ems') { for (let i = 0; i < 10; i++) { const x = Math.round((i * 47 + zeit * 8 * (1 + (i % 3))) % B), y = 96 + (i * 13) % 40; t2.fillStyle = 'rgb(188,228,252)'; t2.fillRect(x, y, 3 + (i % 2), 1); } }
        // Personen (NPC)
        const figuren = [];
        for (const p of (gehalten || G.npcPlaetze(z, hs))) {
            const redet = (sprecher === p.id || (sprecher === 'gegner' && p.id === 'finale')) && !textbox.hidden;
            let zustand = redet ? (sprecherSchreck ? 'erschrocken' : 'reden') : 'stehen';
            if (p.id === 'finale') { zustand = z.ende ? 'erschrocken' : p.name === 'pinsel' ? 'reden' : 'stehen'; }
            const wackel = p.id === 'finale' && p.name === 'ledeiw' ? Math.floor(zeit * 3) % 2 : 0;
            figuren.push({ y: p.y - wackel, name: p.name, x: p.x, zustand, blick: p.x > held[z.figur].x ? -1 : 1, skala: p.skala, hinten: p.hinten, klebt: p.klebt });
        }
        for (const n of ['joe', 'chani']) {
            const hh = held[n];
            const istSprecher = sprecher === n && !textbox.hidden;
            // Auf der eigenen Plattform: verkleinert im Hintergrund, unabhängig von der anderen Figur
            const pl = G.aufPlattform(za, n);
            if (pl) { figuren.push({ y: pl.y, name: figurName(n), x: pl.x, zustand: istSprecher ? (sprecherSchreck ? 'erschrocken' : 'reden') : 'stehen', blick: 1, skala: pl.skala, hinten: true, held: n }); continue; }
            const geht = Math.abs(hh.ziel - hh.x) > 0.5;
            const zustand = geht ? 'gehen' : (istSprecher ? (sprecherSchreck ? 'erschrocken' : 'reden') : (benutzenBis > zeit && n === z.figur ? 'benutzen' : 'stehen'));
            figuren.push({ y: BODEN, name: figurName(n), x: hh.x, zustand, blick: hh.blick, held: n });
        }
        figuren.sort((a, b) => a.y - b.y);
        for (const f of figuren) {
            const bildNr = f.zustand === 'gehen' ? Math.floor(zeit * 10) : f.zustand === 'reden' ? Math.floor(zeit * 7) : f.zustand === 'benutzen' ? (benutzenBis - zeit > 0.4 ? 0 : 1) : 0;
            const blinzeln = f.zustand === 'stehen' && (Math.floor(zeit * 8) + f.name.length * 11) % 41 === 0;
            const py = zeichneFigur(f.name, f.zustand, bildNr, f.x, f.y, f.blick, blinzeln, !(f.held && f.skala), f.skala, f.hinten, f.klebt);
            if (f.held && f.held === z.figur && !mini && py !== undefined) { t2.fillStyle = 'rgb(255,214,79)'; t2.fillRect(Math.round(f.x) - 1, py - 4, 3, 2); t2.fillRect(Math.round(f.x), py - 2, 1, 1); }
        }
        // Hover-Rahmen (dezent)
        const ziel = hs.find((h) => h.id === hover);
        if (ziel && textbox.hidden) { const r = R(ziel); t2.strokeStyle = 'rgba(255,214,79,0.7)'; t2.setLineDash([2, 2]); t2.strokeRect(r.x + 0.5, r.y + 0.5, r.b - 1, r.h - 1); t2.setLineDash([]); }
    }

    const ortPunkt = (id) => ({ x: S.ORTE[id].x * B / 100, y: S.ORTE[id].y * H / 100 });
    function zeichneKarte() {
        const k = G.bild('karte_greven_256x144');
        if (k) { t2.drawImage(k, 0, 0); }
        for (const id of Object.keys(S.ORTE)) {
            const { x, y } = ortPunkt(id);
            if (!z.orte.includes(id)) { t2.globalAlpha = 0.55; t2.fillStyle = 'rgb(228,196,148)'; t2.beginPath(); t2.arc(x, y, 5, 0, Math.PI * 2); t2.fill(); t2.globalAlpha = 1; continue; }
            const puls = hover === id ? 7 : 6 + (Math.floor(zeit * 3) % 2);
            t2.strokeStyle = hover === id ? 'rgb(252,216,0)' : 'rgba(255,255,255,0.8)'; t2.lineWidth = 1;
            t2.beginPath(); t2.arc(Math.round(x) + 0.5, Math.round(y) + 0.5, puls, 0, Math.PI * 2); t2.stroke();
        }
        if (reise) {
            // rote Reiselinie wächst von Ort zu Ort, dahinter ein laufender Wegpunkt
            const a = ortPunkt(reise.von), b = ortPunkt(reise.nach), t = Math.min(1, reise.t);
            const n = Math.max(8, Math.round(Math.hypot(b.x - a.x, b.y - a.y) / 2)), bis = Math.floor(t * n);
            const bogen = (u) => ({ x: a.x + (b.x - a.x) * u, y: a.y + (b.y - a.y) * u - Math.sin(u * Math.PI) * 10 });
            t2.fillStyle = 'rgb(188,0,0)';
            for (let i = 0; i <= bis; i++) { const p = bogen(i / n); if (i % 3 === 2 || Math.hypot(p.x - a.x, p.y - a.y) < 6 || Math.hypot(p.x - b.x, p.y - b.y) < 6) { continue; } t2.fillRect(Math.round(p.x) - 1, Math.round(p.y) - 1, 2, 2); }
            const p = bogen(t);
            t2.fillStyle = 'rgb(60,30,20)'; t2.fillRect(Math.round(p.x) - 3, Math.round(p.y) - 9, 7, 3); t2.fillRect(Math.round(p.x) - 2, Math.round(p.y) - 12, 5, 3);
        }
    }

    // ---------- Minispiele ----------
    function streifen(name, versatz) {
        const b = G.bild(name); if (!b) { return; }
        const v = ((Math.round(versatz) % 512) + 512) % 512;
        t2.drawImage(b, -v, 0); t2.drawImage(b, 512 - v, 0);
    }
    /** Oberkörper einer Figur (für Bootsfahrten). */
    function oberkoerper(name, x, y) { const r = G.rahmen(name, 'stehen', 0, false), b = G.bild(r.bild); if (b) { t2.drawImage(b, r.sx, 0, 32, 40, Math.round(x), Math.round(y), 32, 40); } }

    const BOOT_ART = { Treibholz: 0, Entenfamilie: 1, Schild: 2, Plakat: 3 };
    const spurY = (sp) => 50 + sp * 30;
    /** Flucht flussab: Ruderboot in der Mitte, Treibgut von vorn (rechts), hinten (links) Pinsel und Ledeiw im Motorboot. */
    function zeichneBoot(sim) {
        streifen('minispiel_ems_512x144', zeit * 70);
        const mx = Math.round(S.BOOT_X * B);
        const by = spurY(sim.spur) + (Math.floor(zeit * 4) % 2);
        oberkoerper('joe_hut', mx - 22, by - 30); oberkoerper('chani', mx - 2, by - 30);
        teil({ bild: 'ruderboot_46x14', sx: 0, b: 46, h: 14, x: mx - 23, y: by + 4 });
        t2.fillStyle = 'rgb(188,228,252)'; for (let i = 0; i < 4; i++) { t2.fillRect(mx - 28 - i * 5, by + 14 + (i % 2), 4, 1); }
        for (const h of sim.hindernisse) {
            const i = BOOT_ART[Object.keys(BOOT_ART).find((k) => h.art.indexOf(k) === 0)] || 0;
            teil({ bild: 'hindernisse_ems_96x24', sx: i * 24, b: 24, h: 24, x: h.x * B - 12, y: spurY(h.spur) - 4 });
        }
        // Verfolger hinten: zielen auf eure Spur; ohne Fakten sinkt das Boot (Blasen steigen auf)
        const sinkt = sim.sinken !== null ? sim.sinken : 0;
        const vy = spurY(sim.gegnerSpur) + sinkt * 12 + (sim.sinken === null ? Math.sin(zeit * 6) : 0);
        if (sinkt < 2.2) {
            t2.save(); t2.globalAlpha = Math.max(0, 1 - sinkt / 2.2);
            oberkoerper('pinsel', 10, vy - 30); oberkoerper('ledeiw', 28, vy - 32);
            teil({ bild: 'motorboot_64x24', sx: 0, b: 64, h: 24, x: 2, y: vy - 2 });
            t2.restore();
        }
        if (sim.sinken !== null) { t2.fillStyle = 'rgb(188,228,252)'; for (let i = 0; i < 6; i++) { const t = (zeit * 1.5 + i * 0.37) % 1; t2.fillRect(14 + i * 8, Math.round(spurY(sim.gegnerSpur) + 16 - t * 18), 2, 2); } }
        else { t2.fillStyle = 'rgb(188,228,252)'; for (let i = 0; i < 5; i++) { t2.fillRect(i * 2, vy + 14 + (Math.floor(zeit * 20 + i) % 3), 2, 1); } }
        // Geschosse: kleine rot-weiße Sprechblasen (der Text steht im Overlay)
        for (const f of sim.schuesse) { const x = Math.round(f.x * B), y = spurY(f.spur) + 2; t2.fillStyle = 'rgb(216,40,0)'; t2.fillRect(x - 6, y, 12, 7); t2.fillStyle = 'rgb(252,252,252)'; t2.fillRect(x - 5, y + 1, 10, 5); t2.fillStyle = 'rgb(216,40,0)'; t2.fillRect(x - 1, y + 2, 2, 2); t2.fillRect(x - 9, y + 3, 3, 1); }
    }

    function zeichneKugel(sim) {
        const weg = (p) => 30 + p * (B - 90);
        streifen('minispiel_gang_512x144', sim.pos * 400);
        const sx = weg(sim.pos), kx = weg(sim.kugel) - 44, boden = 132;
        const lauf = Math.floor(zeit * 12);
        if (sim.lastenrad) {
            teil({ bild: 'lastenrad_48x32', sx: 0, b: 48, h: 32, x: sx - 22, y: boden - 30 });
            zeichneFigur('joe_hut', 'stehen', 0, sx - 12, boden - 16, 1, false, false); zeichneFigur('chani', 'stehen', 0, sx + 10, boden - 18, 1, false, false);
        } else {
            zeichneFigur('joe_hut', 'gehen', lauf, sx - 8, boden, 1, false); zeichneFigur('chani', 'gehen', lauf + 2, sx + 12, boden, 1, false);
        }
        const wackel = Math.floor(zeit * 20) % 2;
        teil({ bild: 'wahlkugel_64x64_x4', sx: 64 * (Math.floor(zeit * 8) % 4), b: 64, h: 64, x: kx - 32, y: boden - 64 - wackel });
    }

    function hochskalieren() {
        g.imageSmoothingEnabled = false;
        g.drawImage(tief, 0, 0, B * SKALA, H * SKALA);
    }

    function textMitSchatten(t, x, y, groesse, farbe, ausrichtung) {
        g.font = `bold ${groesse}px "Courier New", ui-monospace, monospace`; g.textAlign = ausrichtung || 'left';
        g.fillStyle = '#000'; g.fillText(t, x + 2, y + 2); g.fillStyle = farbe; g.fillText(t, x, y);
    }

    function overlay() {
        if (mini) {
            if (mini.art === 'boot') {
                textMitSchatten(mini.sinken !== null ? `Blubb!   Leben ${'♥'.repeat(Math.max(0, mini.leben))}` : `Alternative Fakten: ${mini.munition + mini.schuesse.length}   Leben ${'♥'.repeat(Math.max(0, mini.leben))}`, 16, 34, 26, '#ffe08a');
                textMitSchatten('↑/↓ oder oben/unten tippen', B * SKALA - 16, 34, 20, '#fff', 'right');
                // Die alternativen Fakten als Sprechblasen über den Geschossen
                g.font = 'bold 15px "Courier New", ui-monospace, monospace'; g.textAlign = 'center';
                for (const f of mini.schuesse) {
                    const x = f.x * B * SKALA, y = (spurY(f.spur) - 6) * SKALA, w = g.measureText(f.text).width + 12;
                    g.fillStyle = '#fff'; g.fillRect(x - w / 2, y - 18, w, 24); g.strokeStyle = '#000'; g.lineWidth = 2; g.strokeRect(x - w / 2, y - 18, w, 24);
                    g.fillStyle = '#c0392b'; g.fillText(f.text, x, y);
                }
            }
            else { textMitSchatten('Tippen / Leertaste / → für Tempo!', B * SKALA - 16, 34, 22, '#fff', 'right'); g.fillStyle = 'rgba(0,0,0,0.4)'; g.fillRect(16, 16, 300, 18); g.fillStyle = '#f4d03f'; g.fillRect(16, 16, 300 * Math.min(1, mini.pos), 18); }
            return;
        }
        if (z.szene === 'karte') {
            if (hover && S.ORTE[hover]) { const p = ortPunkt(hover); textMitSchatten(S.ORTE[hover].name, p.x * SKALA, p.y * SKALA - 34, 22, '#ffe08a', 'center'); }
        } else {
            textMitSchatten(S.SZENEN[z.szene].titel, 14, 32, 22, '#fff');
            if (hover && textbox.hidden) {
                const hs = S.sichtbareHotspots(z).find((h) => h.id === hover);
                if (hs) { const r = R(hs); textMitSchatten(hs.name, (r.x + r.b / 2) * SKALA, Math.max(28, r.y * SKALA - 8), 20, '#ffe08a', 'center'); }
            }
        }
        if (z.ende && textbox.hidden) { g.fillStyle = 'rgba(0,0,0,0.5)'; g.fillRect(0, 0, B * SKALA, H * SKALA); textMitSchatten('ENDE', B * SKALA / 2, H * SKALA / 2, 96, '#ffd54f', 'center'); textMitSchatten(`${z.punkte} Punkte · ${Math.round(z.sekunden / 60)} Minuten`, B * SKALA / 2, H * SKALA / 2 + 54, 30, '#fff', 'center'); }
        if (blende) {
            const p = blende.t < 0.5 ? 1 - blende.t * 2 : (blende.t - 0.5) * 2;
            const r = Math.max(0, p) * Math.hypot(B, H) * SKALA / 2;
            const fx = held[z.figur].x * SKALA, fy = (BODEN - 30) * SKALA;
            g.save(); g.fillStyle = '#000'; g.beginPath(); g.rect(0, 0, B * SKALA, H * SKALA); g.arc(fx, fy, r, 0, Math.PI * 2, true); g.fill('evenodd'); g.restore();
        }
    }

    let benutzenBis = 0;
    let letzteZeit = performance.now();
    function schleife(jetzt) {
        const dt = Math.min(0.05, (jetzt - letzteZeit) / 1000); letzteZeit = jetzt;
        if (z && !pausiert) {
            zeit += dt;
            if (!z.ende) { z.sekunden += dt; }
            if (blende) { blende.t += dt * 1.6; if (blende.t >= 0.5 && !blende.gewechselt) { blende.gewechselt = true; blende.wechsel(); } if (blende.t >= 1) { blende = null; } }
            if (mini && textbox.hidden && dialogEl.hidden) {
                if (mini.art === 'boot') { S.bootSchritt(mini, dt, eingabeBoot); eingabeBoot = null; } else { S.kugelSchritt(mini, dt, eingabeKugel); eingabeKugel = false; }
                if (mini.status !== 'laeuft') {
                    const art = mini.art, ok = mini.status === 'gewonnen', v = z.szene; mini = null; klang(ok ? 'erfolg' : 'fehler');
                    const e = S.minispielEnde(z, art, ok);
                    if (z.szene !== v) { const neu = z.szene; z.szene = v; szenenwechsel(() => { z.szene = neu; ergebnis(e, v); }); }
                    else { ergebnis(e); if (ok && art === 'kugel') { aufStart(); } if (!ok) { danach = { minispiel: art }; } }
                }
            }
            if (reise) { reise.t += dt * 0.9; if (reise.t >= 1.15) { const ziel = reise.nach; reise = null; szenenwechsel(() => { const v = z.szene; ergebnis(S.reisen(z, ziel), v); }); } }
            laufen(dt);
        }
        if (z) {
            if (mini) { if (mini.art === 'boot') { zeichneBoot(mini); } else { zeichneKugel(mini); } }
            else if (z.szene === 'karte') { zeichneKarte(); } else { zeichneSzene(); }
            hochskalieren(); overlay();
            satzEl.textContent = mini ? '' : z.szene === 'karte' ? `Reise nach ${hover && S.ORTE[hover] ? S.ORTE[hover].name : '…'}` : satz();
        } else {
            t2.fillStyle = '#120c08'; t2.fillRect(0, 0, B, H); hochskalieren();
        }
        requestAnimationFrame(schleife);
    }
    let eingabeBoot = null, eingabeKugel = false;

    function laufen(dt) {
        for (const n of ['joe', 'chani']) {
            const h = held[n], d = h.ziel - h.x;
            if (Math.abs(d) > 0.5) { const v = 55 * dt; h.x += Math.sign(d) * Math.min(Math.abs(d), v); h.blick = Math.sign(d); h.schritt += dt; if (h.schritt > 0.3) { h.schritt = 0; klang('schritt'); } }
        }
        const a = held[z.figur];
        if (laufAuftrag && Math.abs(a.ziel - a.x) <= 0.5) { const f = laufAuftrag; laufAuftrag = null; f(); }
    }

    /** Taucht eine Person dort auf, wo jemand steht (z. B. die Gegner im Archiv), treten die Helden zur Seite. */
    function ausweichen() {
        if (!z || z.szene === 'karte' || laufAuftrag) { return; }
        const npc = G.npcPlaetze(z, S.sichtbareHotspots(z)).map((p) => p.x), grenzen = G.bereich(z);
        const a = held[z.figur], b = held[z.figur === 'joe' ? 'chani' : 'joe'];
        a.ziel = G.freiesZiel(a.ziel, npc, grenzen);
        b.ziel = G.freiesZiel(b.ziel, npc.concat([a.ziel]), grenzen);
    }

    /** Gehen zu einem Hotspot oder einer x-Position; Stehplätze überlappen nie mit anderen Personen. */
    function gehe(ziel, danachTun) {
        const andere = z.figur === 'joe' ? 'chani' : 'joe';
        const a = held[z.figur], b = held[andere];
        // Balance-Plattformen: Wer auf seiner Plattform steht, bleibt dort, bis er selbst woandershin geht
        const aufA = G.aufPlattform(z, z.figur);
        const zielPlatte = ziel && typeof ziel === 'object' ? ({ platte_links: 'links', platte_rechts: 'rechts' })[ziel.id] : null;
        if (aufA) {
            a.x = a.ziel = aufA.x;
            if (aufA.seite === zielPlatte) { laufAuftrag = danachTun || null; return; }
            S.platteVerlassen(z, z.figur);
        }
        const p = G.standplaetze(z, S.sichtbareHotspots(z), ziel, a.x, b.x);
        a.ziel = p.aktiv;
        if (!G.aufPlattform(z, andere)) { b.ziel = p.begleit; }
        laufAuftrag = danachTun || null;
    }

    // ---------- Text, Porträts, Dialoge ----------
    const SPRECHER = [['Joe', 'joe'], ['Chani', 'chani'], ['Chris', 'christian'], ['Johanna', 'joanna'], ['Lorette', 'lore'], ['Kemper', 'kemper'], ['Frau Kemper', 'kemper'], ['Pinsel', 'pinsel'], ['Ledeiw', 'ledeiw'], ['Relbeil', 'relbeil']];
    function sprecherAus(zeile) {
        for (const [n, id] of SPRECHER) { if (zeile.startsWith(n + ':') || zeile.startsWith(n + ' ')) { return id; } }
        return null;
    }
    function ausdruck(zeile) {
        if (/(Kugel|rollt|heult|Knirschen|Hilfe|poltert|stürzen|klebt)/.test(zeile)) { return 'erschrocken'; }
        if (/(!|Geschafft|Danke|richtig|Solidarität|leicht)/.test(zeile)) { return 'lacht'; }
        return 'neutral';
    }

    function zeigeText() {
        if (warteschlange.length === 0) {
            textbox.hidden = true; sprecher = null;
            if (gehalten) { gehalten = null; ausweichen(); }
            // Fallgitter offen und Text gelesen: Joe und Chani springen von den Plattformen und gehen hindurch (Blende)
            if (gitterGehalten) { gitterGehalten = false; szenenwechsel(() => aufStart()); }
            const d = danach; danach = null;
            if (d && d.dialog) { dialogZeigen(d.dialog); }
            if (d && d.gespraech) { gespraechZeigen(d.gespraech); }
            if (d && d.minispiel) { mini = d.minispiel === 'boot' ? S.bootNeu(Date.now() % 1000) : S.kugelNeu(z.flags.lastenrad); }
            leisteAktualisieren();
            return;
        }
        const zeile = warteschlange.shift();
        const wer = sprecherAus(zeile);
        sprecherSchreck = ausdruck(zeile) === 'erschrocken';
        // Sprecher → Hotspot der Figur (für die Sprechanimation); Chris' Bilder heißen weiter „christian“
        sprecher = wer === 'pinsel' || wer === 'ledeiw' ? 'gegner' : ({ christian: 'chris', kemper: 'bauleiter' })[wer] || wer;
        portraitEl.replaceChildren();
        if (wer) { const bild = portraetBild(wer, ausdruck(zeile)); bild.className = 'portraitbild'; portraitEl.appendChild(bild); portraitEl.hidden = false; } else { portraitEl.hidden = true; }
        textEl.textContent = zeile;
        textbox.hidden = false;
    }

    function ergebnis(e, vorherSzene) {
        const vorher = vorherSzene === undefined ? z.szene : vorherSzene;
        warteschlange = warteschlange.concat(e.text || []);
        if (e.dialog || e.minispiel) { danach = { dialog: e.dialog, minispiel: e.minispiel }; }
        if (z.szene !== vorher) {
            if (z.szene !== 'karte' && S.SZENEN[z.szene] && !e.ohneEinstieg && !(e.text || []).includes(S.einstieg(z, z.szene))) { warteschlange.push(S.einstieg(z, z.szene)); }
            if (S.ORTE[vorher]) { letzterOrt = vorher; }
            aufStart();
            plaetze.schreiben('auto', z);
        }
        // Figuren, die erst nach dem Text auftauchen (z. B. Pinsel und Ledeiw nach dem Bootsunglück am Hallenbad oder nach der Wahlkugel)
        if (e.erscheinen) { gehalten = (gehalten || G.npcPlaetze(z, S.sichtbareHotspots(z))).filter((p) => !e.erscheinen.includes(p.id)); }
        if (e.ende) { plaetze.schreiben('auto', z); if (window.Musik && window.Musik.an) { window.Musik.spielen(true); } else { klang('ende'); } }
        hover = null; auswahl = -1;
        ausweichen();
        zeigeText();
    }

    function szenenwechsel(fn) { klang('blende'); blende = { t: 0, gewechselt: false, wechsel: fn }; }

    function dialogZeigen(art) {
        const d = S.dialogInhalt(z, art);
        if (!d) { return; }
        auswahlListe(d.frage, d.optionen.map((o) => ({ text: o.text, tun: () => { klang('klick'); ergebnis(halten(() => S.dialogWahl(z, art, o.id))); } })));
    }

    /** Aktion ausführen; verschwinden dabei Figuren oder wechseln sie den Platz, bleiben sie sichtbar, bis der Text gelesen ist. */
    function halten(tun) {
        const szene = z.szene, vorher = szene === 'karte' ? null : G.npcPlaetze(z, S.sichtbareHotspots(z));
        const e = tun();
        if (vorher && z.szene === szene && (e.text || []).length > 0 && JSON.stringify(vorher) !== JSON.stringify(G.npcPlaetze(z, S.sichtbareHotspots(z)))) { gehalten = gehalten || vorher; }
        return e;
    }

    /** Joe ruft Professor Relbeil an (Chani lässt Joe telefonieren). */
    function anrufen() {
        if (z.figur !== 'joe') { ergebnis({ text: ['Chani: „Das ist Joes Handy. Joe, ruf du an – mit Tasten kann ich nicht.“'] }); return; }
        klang('klick');
        auswahlListe('Joe ruft Professor Relbeil an:', [
            { text: '„Guten Tag!“', tun: () => ergebnis(S.anrufen(z, 'hallo')) },
            { text: '„Ich brauche deine Hilfe!“', tun: () => ergebnis(S.anrufen(z, 'hilfe')) },
            { text: 'Auflegen', tun: () => { leisteAktualisieren(); } }
        ]);
    }

    /** Kurzer Hinweis, wenn während eines offenen Textes etwas anderes angeklickt wird (blendet sich selbst aus). */
    let hinweisTimer = null;
    function lesehinweis() {
        let el = document.getElementById('lesehinweis');
        if (!el) { el = document.createElement('div'); el.id = 'lesehinweis'; el.setAttribute('role', 'status'); document.getElementById('buehne').appendChild(el); }
        el.textContent = 'Erst zu Ende lesen – ein Klick auf den Text blättert weiter.';
        el.classList.add('sichtbar');
        clearTimeout(hinweisTimer);
        hinweisTimer = setTimeout(() => el.classList.remove('sichtbar'), 2200);
    }

    /** Gesprächsauswahl mit Personen (Dialogbaum: Wer sind Sie? / Anliegen / Tschüss). */
    function gespraechZeigen(h) {
        const anrede = z.figur === 'joe' ? 'Joe' : 'Chani';
        auswahlListe(`${anrede} spricht mit ${h.name}:`, [
            { text: '„Wer sind Sie eigentlich?“', tun: () => ergebnis(S.aktion(z, 'ansehen', h.id)) },
            { text: h.form === 'gegner' ? '„Was wollen Sie hier?“' : '„Wir brauchen Ihre Hilfe.“', tun: () => handlung('reden', h) },
            { text: '„Schönen Abend noch.“', tun: () => ergebnis({ text: [`${anrede}: „Schönen Abend noch.“`] }) }
        ]);
    }

    function auswahlListe(frage, optionen) {
        dialogEl.replaceChildren();
        const f = document.createElement('p'); f.textContent = frage; dialogEl.appendChild(f);
        optionen.forEach((o, i) => { const k = document.createElement('button'); k.type = 'button'; k.textContent = `${i + 1}. ${o.text}`; k.addEventListener('click', () => { dialogEl.hidden = true; o.tun(); }); dialogEl.appendChild(k); });
        dialogEl.hidden = false; dialogEl.querySelector('button').focus();
    }

    // ---------- Aktionen ----------
    function handlung(art, h, gegenstand) {
        const vorher = z.punkte, szene = z.szene, gitterVorher = !!z.flags.gitterOffen;
        const e = halten(() => S.aktion(z, art, h.id, gegenstand));
        if (!gitterVorher && z.flags.gitterOffen && z.szene === 'tempel') { gitterGehalten = true; }
        if (art === 'f1' && z.figur === 'joe') { klang('kartusche'); } else { klang(z.punkte > vorher ? 'erfolg' : 'klick'); }
        if (art !== 'ansehen' && art !== 'reden') { benutzenBis = zeit + 0.8; }
        if (z.szene !== szene) {
            const neu = z.szene; z.szene = szene;
            szenenwechsel(() => { z.szene = neu; ergebnis(e, szene); });
            return;
        }
        ergebnis(e);
    }

    /** Standardaktion beim Tippen (Gehe zu): Personen ansprechen, Ausgänge benutzen, sonst ansehen. */
    function standard(h) {
        if (npcName(h)) { return 'reden'; }
        if (h.form === 'pfeil' || h.form === 'tuer' || h.form === 'tor') { return 'benutzen'; }
        return 'ansehen';
    }

    function hotspotAusloesen(h, langDruck, px, py) {
        if (langDruck) { kontextZeigen(h, px, py); return; }
        const v = verb, o = objekt;
        verb = 'gehe'; objekt = null;
        gehe(h, () => {
            held[z.figur].blick = (R(h).x + R(h).b / 2) >= held[z.figur].x ? 1 : -1;
            if (o) { handlung('gegenstand', h, o); return; }
            const art = v === 'gehe' ? standard(h) : v === 'nimm' || v === 'benutze' ? 'benutzen' : v === 'gib' ? null : v;
            if (art === null) { ergebnis({ text: [`${S.FIGUREN[z.figur].name}: „Was soll ich geben? Erst einen Gegenstand im Inventar wählen.“`] }); return; }
            if (art === 'reden' && npcName(h)) { gespraechZeigen(h); return; }
            handlung(art, h);
        });
        leisteAktualisieren();
    }

    function kontextZeigen(h, x, y) {
        kontextEl.replaceChildren();
        const kopf = document.createElement('div'); kopf.className = 'kopf'; kopf.textContent = h.name; kontextEl.appendChild(kopf);
        const f = S.FIGUREN[z.figur].faehigkeiten;
        [['ansehen', 'Schau an'], ['benutzen', 'Nimm / Benutze'], ['reden', 'Rede mit'], ['f1', f[0]], ['f2', f[1]]].forEach(([art, t], i) => knopf(`${i + 1}. ${t}`, () => gehe(h, () => (art === 'reden' && npcName(h) ? gespraechZeigen(h) : handlung(art, h)))));
        for (const gid of z.inventar) { knopf(`➜ ${S.GEGENSTAENDE[gid].name}`, () => gehe(h, () => handlung('gegenstand', h, gid))); }
        knopf('Abbrechen', () => { });
        const r = leinwand.getBoundingClientRect(), bu = $('buehne').getBoundingClientRect();
        kontextEl.hidden = false;
        kontextEl.style.left = `${Math.min(Math.max(r.left - bu.left + x * r.width / B, 4), bu.width - kontextEl.offsetWidth - 4)}px`;
        kontextEl.style.top = `${Math.min(Math.max(r.top - bu.top + y * r.height / H, 4), bu.height - kontextEl.offsetHeight - 4)}px`;
        kontextEl.querySelector('button').focus();
        function knopf(t, fn) { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', () => { kontextEl.hidden = true; fn(); }); kontextEl.appendChild(k); }
    }

    // ---------- Leiste: Verben, Figuren, Inventar ----------
    function leisteAktualisieren() {
        if (!z) { return; }
        verbenEl.replaceChildren();
        for (const [v] of VERBEN) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'verb' + (verb === v ? ' aktiv' : '') + (v === 'f1' || v === 'f2' ? ' faehigkeit' : '');
            k.textContent = verbName(v);
            k.addEventListener('click', () => { if (!textbox.hidden) { lesehinweis(); } verb = v; objekt = null; klang('klick'); leisteAktualisieren(); });
            verbenEl.appendChild(k);
        }
        figurenEl.replaceChildren();
        for (const id of ['joe', 'chani']) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'figur' + (z.figur === id ? ' aktiv' : ''); k.title = S.FIGUREN[id].beschreibung; k.setAttribute('aria-pressed', String(z.figur === id));
            const bild = portraetBild(id, 'neutral'); bild.className = 'mini';
            k.append(bild, document.createTextNode(S.FIGUREN[id].name));
            k.addEventListener('click', () => { if (!textbox.hidden) { lesehinweis(); return; } if (z.figur !== id) { klang('klick'); ergebnis(S.figurWechseln(z)); } });
            figurenEl.appendChild(k);
        }
        inventarEl.replaceChildren();
        for (const gid of z.inventar) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'ding' + (objekt === gid ? ' aktiv' : ''); k.title = S.GEGENSTAENDE[gid].name; k.setAttribute('aria-label', S.GEGENSTAENDE[gid].name);
            const bild = G.ausschnitt(G.icon(gid), 16, 16); bild.className = 'icon'; k.appendChild(bild);
            k.addEventListener('click', () => {
                // Joes Handy: Benutzen (oder einfach antippen) ruft Professor Relbeil an
                if (gid === 'handy' && (verb === 'benutze' || verb === 'gehe')) {
                    if (!textbox.hidden) { lesehinweis(); return; }
                    anrufen(); return;
                }
                // Ein Klick wählt den Gegenstand sofort – auch wenn noch ein Text offen ist (keine Doppelklick-Falle)
                if (verb === 'benutze' || verb === 'gib') { objekt = objekt === gid ? null : gid; klang('klick'); leisteAktualisieren(); return; }
                if (!textbox.hidden) { zeigeText(); return; }
                ergebnis({ text: [`${S.GEGENSTAENDE[gid].name}: ${S.GEGENSTAENDE[gid].text}`] });
            });
            inventarEl.appendChild(k);
        }
        punkteEl.textContent = `${z.punkte} Punkte`;
    }

    // ---------- Eingabe ----------
    function punktAus(ev) { const r = leinwand.getBoundingClientRect(); return { x: (ev.clientX - r.left) * B / r.width, y: (ev.clientY - r.top) * H / r.height }; }
    function treffer(p) {
        if (z.szene === 'karte') { const o = Object.entries(S.ORTE).find(([id, o]) => z.orte.includes(id) && Math.abs(p.x - o.x * B / 100) < 8 && Math.abs(p.y - o.y * H / 100) < 8); return o ? { karte: o[0] } : null; }
        const h = S.sichtbareHotspots(z).slice().reverse().find((h) => { const r = R(h); return p.x >= r.x && p.x <= r.x + r.b && p.y >= r.y && p.y <= r.y + r.h; });
        return h ? { hotspot: h } : null;
    }
    function blockiert() { return !z || pausiert || !textbox.hidden || !dialogEl.hidden || !titelEl.hidden || reise || blende; }

    leinwand.addEventListener('pointermove', (ev) => {
        if (blockiert() || mini) { return; }
        const t = treffer(punktAus(ev));
        hover = t ? (t.karte || t.hotspot.id) : null;
        leinwand.style.cursor = hover ? 'pointer' : 'crosshair';
    });

    let druck = null;
    leinwand.addEventListener('pointerdown', (ev) => {
        if (!z || pausiert || !titelEl.hidden) { return; }
        ev.preventDefault();
        if (!textbox.hidden) {
            const t = mini ? null : treffer(punktAus(ev));
            if (G.klickBeiText(verb, objekt, !!(t && t.hotspot)) === 'weiter') { if (t && t.hotspot && verb === 'gehe' && warteschlange.length > 0) { lesehinweis(); } zeigeText(); return; }
            while (!textbox.hidden) { zeigeText(); }    // Text abschließen, dann gilt der Klick der Aktion
            if (!dialogEl.hidden || mini) { return; }
        }
        if (mini) { const p = punktAus(ev); if (mini.art === 'boot') { eingabeBoot = p.y < H / 2 ? 'hoch' : 'runter'; } else { eingabeKugel = true; } return; }
        if (blockiert()) { return; }
        kontextEl.hidden = true;
        const p = punktAus(ev);
        druck = { p, zeit: performance.now(), lang: false, timer: setTimeout(() => { druck.lang = true; const t = treffer(p); if (t && t.hotspot) { hotspotAusloesen(t.hotspot, true, p.x, p.y); } }, 480) };
    });
    leinwand.addEventListener('pointerup', () => {
        if (!druck) { return; }
        clearTimeout(druck.timer);
        const d = druck; druck = null;
        if (d.lang || blockiert()) { return; }
        const t = treffer(d.p);
        if (!t) { if (z.szene !== 'karte') { verb = 'gehe'; objekt = null; gehe(d.p.x); leisteAktualisieren(); } return; }
        if (t.karte) { reiseStarten(t.karte); return; }
        hotspotAusloesen(t.hotspot, false);
    });
    leinwand.addEventListener('contextmenu', (ev) => ev.preventDefault());
    textbox.addEventListener('click', () => zeigeText());

    function reiseStarten(ziel) {
        if (ziel === letzterOrt && ziel !== 'buero') { szenenwechsel(() => { const v = z.szene; ergebnis(S.reisen(z, ziel), v); }); return; }
        reise = { von: S.ORTE[letzterOrt] ? letzterOrt : 'buero', nach: ziel, t: 0 };
        klang('klick');
    }

    document.addEventListener('keydown', (ev) => {
        if (!z || !titelEl.hidden) { return; }
        // Esc während der Figurenvorstellung: Vorstellung überspringen (statt Pause)
        if (ev.key === 'Escape' && !textbox.hidden && (warteschlange.some((l) => S.VORSTELLUNG.includes(l)) || S.VORSTELLUNG.includes(textEl.textContent))) {
            ev.preventDefault(); warteschlange = warteschlange.filter((l) => !S.VORSTELLUNG.includes(l)); zeigeText(); return;
        }
        if (ev.key === 'Escape' || ev.key === 'p' || ev.key === 'P') { ev.preventDefault(); pauseUmschalten(); return; }
        if (pausiert) { return; }
        if (!textbox.hidden && (ev.key === 'Enter' || ev.key === ' ')) { ev.preventDefault(); zeigeText(); return; }
        if (mini) {
            if (mini.art === 'boot' && (ev.key === 'ArrowUp' || ev.key === 'w')) { eingabeBoot = 'hoch'; ev.preventDefault(); }
            if (mini.art === 'boot' && (ev.key === 'ArrowDown' || ev.key === 's')) { eingabeBoot = 'runter'; ev.preventDefault(); }
            if (mini.art === 'kugel' && (ev.key === ' ' || ev.key === 'ArrowRight' || ev.key === 'd')) { if (!ev.repeat) { eingabeKugel = true; } ev.preventDefault(); }   // Festhalten zählt nicht
            return;
        }
        const n = Number(ev.key);
        if (!dialogEl.hidden) { const k = dialogEl.querySelectorAll('button'); if (n >= 1 && n <= k.length) { k[n - 1].click(); } return; }
        if (!kontextEl.hidden) { const k = kontextEl.querySelectorAll('button'); if (n >= 1 && n <= 5) { k[n - 1].click(); } return; }
        if (n >= 1 && n <= 8) { verb = VERBEN[n - 1][0]; objekt = null; leisteAktualisieren(); return; }
        if (ev.key === 'Tab' || ev.key === 'f') { ev.preventDefault(); ergebnis(S.figurWechseln(z)); return; }
        const ziele = z.szene === 'karte' ? Object.keys(S.ORTE).filter((o) => z.orte.includes(o)).map((id) => ({ id })) : S.sichtbareHotspots(z);
        if (ev.key === 'ArrowRight' || ev.key === 'ArrowLeft') { ev.preventDefault(); auswahl = (auswahl + (ev.key === 'ArrowRight' ? 1 : -1) + ziele.length) % ziele.length; hover = ziele[auswahl] ? ziele[auswahl].id : null; }
        if (ev.key === 'Enter' && auswahl >= 0 && ziele[auswahl]) { ev.preventDefault(); if (z.szene === 'karte') { reiseStarten(ziele[auswahl].id); } else { hotspotAusloesen(ziele[auswahl], false); } }
    });

    // ---------- Menü ----------
    function pauseUmschalten() { pausiert = !pausiert; if (pausiert) { menueZeigen(); } else { menueEl.hidden = true; } }
    function menueZeigen() {
        menueEl.replaceChildren();
        const h = document.createElement('h2'); h.textContent = 'Pause'; menueEl.appendChild(h);
        knopf('Weiterspielen', () => pauseUmschalten());
        for (const p of ['1', '2', '3']) { const info = plaetze.info(p); knopf(`Speichern auf Platz ${p}${info ? ` (belegt: ${info.punkte} Punkte)` : ''}`, () => { const ok = plaetze.schreiben(p, z); hinweis(ok ? `Gespeichert auf Platz ${p}.` : 'Speichern nicht möglich (Browser-Speicher gesperrt).'); }); }
        for (const p of ['auto', '1', '2', '3']) { const info = plaetze.info(p); if (info) { knopf(`Laden: ${p === 'auto' ? 'Autospeicher' : 'Platz ' + p} (${info.punkte} Punkte)`, () => { const s = plaetze.lesen(p); if (s) { starten(s); } }); } }
        if (window.Musik) {
            knopf(window.Musik.an ? 'Musik aus' : 'Musik an', () => { window.Musik.setzeAn(!window.Musik.an); menueZeigen(); });
            knopf('Erkennungsmelodie abspielen', () => { window.Musik.setzeAn(true); window.Musik.spielen(); });
            const regler = document.createElement('input'); regler.type = 'range'; regler.min = '0'; regler.max = '1'; regler.step = '0.05'; regler.value = String(window.Musik.lautstaerke); regler.setAttribute('aria-label', 'Lautstärke der Musik');
            regler.addEventListener('input', () => window.Musik.setzeLautstaerke(regler.value)); menueEl.appendChild(regler);
        }
        knopf(ton ? 'Ton aus' : 'Ton an', () => { ton = !ton; try { window.localStorage.setItem('fp-spiel-ton', ton ? 'an' : 'aus'); } catch (_) { /* egal */ } menueZeigen(); });
        knopf('Figuren vorstellen', () => { pauseUmschalten(); ergebnis({ text: S.VORSTELLUNG }); });
        knopf('Neues Spiel', () => starten(S.neuerZustand(), true));
        const zurueck = document.createElement('a'); zurueck.href = '../'; zurueck.textContent = 'Zurück zu FraktionPerfekt'; menueEl.appendChild(zurueck);
        menueEl.hidden = false;
        function knopf(t, fn) { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', fn); menueEl.appendChild(k); }
    }
    function hinweis(t) { const p = document.createElement('p'); p.className = 'hinweis'; p.textContent = t; menueEl.appendChild(p); }

    function starten(zustand, neu, hinweis) {
        z = zustand; mini = null; reise = null; blende = null; pausiert = false; laufAuftrag = null;
        menueEl.hidden = true; titelEl.hidden = true; dialogEl.hidden = true; kontextEl.hidden = true;
        warteschlange = []; danach = null; verb = 'gehe'; objekt = null; gehalten = null; gitterGehalten = false;
        aufStart();
        letzterOrt = S.ORTE[z.szene] ? z.szene : 'buero';
        $('leiste').hidden = false;
        leisteAktualisieren();
        if (neu) {
            ergebnis({ text: [...S.VORSTELLUNG, ...S.EINLEITUNG] });
        } else if (hinweis) {
            ergebnis({ text: [hinweis] });
        } else {
            ergebnis({ text: [z.szene === 'karte' ? 'Weiter geht’s – auf der Karte.' : `Weiter geht’s: ${S.SZENEN[z.szene].titel}.`] });
        }
    }

    $('neu').addEventListener('click', () => { if (geladen) { klang('klick'); starten(S.neuerZustand(), true); } });

    // Erkennungsmelodie: startet mit der ersten Bedienung auf dem Titelbild (Browser erlauben Ton erst nach einer Eingabe)
    const M = window.Musik;
    let melodieGespielt = false;
    function musikKnopf() { $('musik').textContent = M && M.an ? '♪ Musik an' : '♪ Musik aus'; }
    if (M) {
        $('lautstaerke').value = String(M.lautstaerke);
        musikKnopf();
        $('musik').addEventListener('click', () => { M.setzeAn(!M.an); musikKnopf(); if (M.an) { melodieGespielt = true; M.spielen(); } });
        $('lautstaerke').addEventListener('input', (e) => M.setzeLautstaerke(e.target.value));
        const ersteEingabe = () => { if (!melodieGespielt && !titelEl.hidden) { melodieGespielt = true; M.spielen(); } };
        document.addEventListener('pointerdown', ersteEingabe, { once: true });
        document.addEventListener('keydown', ersteEingabe, { once: true });
    } else { $('musik').hidden = true; }
    const weiterKnopf = $('weiter');
    if (plaetze.info('auto')) { weiterKnopf.hidden = false; weiterKnopf.addEventListener('click', () => { const s = plaetze.lesen('auto'); if (s && geladen) { starten(s); } }); }
    $('pause').addEventListener('click', () => { if (z && titelEl.hidden) { pauseUmschalten(); } });

    // Grafiken laden, dann Titelbild mit beiden Porträts
    $('neu').disabled = true; weiterKnopf.disabled = true;
    G.laden('grafik/').then(() => {
        geladen = true; $('neu').disabled = false; weiterKnopf.disabled = false;
        $('titelbilder').append(G.ausschnitt(G.portraet('joe_hut', 'lacht'), 64, 64), G.ausschnitt(G.portraet('chani', 'lacht'), 64, 64));
        // Direkteinstieg (Spickzettel /cheatsheet), z. B. /spiel/?start=archiv: Komplettlösung bis dorthin nachspielen
        const sprung = S.sprungZustand((new URLSearchParams(window.location.search).get('start') || '').toLowerCase());
        if (sprung) {
            const ort = sprung.szene === 'karte' ? 'Reisekarte' : S.SZENEN[sprung.szene].titel;
            starten(sprung, false, `Direkteinstieg: ${ort}. Im Gepäck: ${sprung.inventar.map((g) => S.GEGENSTAENDE[g].name).join(', ')}.`);
        }
    }).catch((e) => { $('titelbilder').textContent = e.message; });

    // Ganzzahlige Skalierung, wenn möglich
    function groesse() {
        const leiste = $('leiste').hidden ? 0 : $('leiste').offsetHeight;
        const vb = window.innerWidth, vh = window.innerHeight - leiste;
        let f = Math.min(vb / B, vh / H);
        if (f >= 2) { f = Math.floor(f); }
        $('buehne').style.width = `${Math.floor(B * f)}px`; $('buehne').style.height = `${Math.floor(H * f)}px`;
    }
    window.addEventListener('resize', groesse);
    new ResizeObserver(groesse).observe($('leiste'));
    groesse();
    // Für Browsertests der Komplettlösung: aktueller Spielstand und laufendes Minispiel (nur lesen)
    window.GralSpielStand = () => ({ z, mini, textOffen: !textbox.hidden, dialogOffen: !dialogEl.hidden, beschaeftigt: !!(reise || blende || laufAuftrag) });
    requestAnimationFrame(schleife);
})();
