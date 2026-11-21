// Joe & Chani und der Gral der Demokratie – 3D-Darstellung (three.js). Gleiche Spiellogik (logik.js), gleiche Bedienung wie die
// Pixel-Fassung: Verbleiste, Satzzeile, Inventar, Porträts, Dialoge, Pause-Menü, Speicherplätze, Joes Handy.
// Schauplätze aus LoD2-Daten (Marktstraße, Rathaus) und frei erfundene Innenräume; begehbar per Klick (Wegsuche auf einem Raster).
import * as THREE from './three_min.js';
import { bereich as bereichLaden, SZENE_ZU_BEREICHEN, startBereich } from './bereiche.js';
import * as FIG from './figuren3d.js';

const S = window.GralSpiel, G = window.Grafik;
const $ = (id) => document.getElementById(id);
const leinwand = $('leinwand'), buehne = $('buehne');
const textbox = $('textbox'), textEl = $('text'), portraitEl = $('portrait'), dialogEl = $('dialog'), menueEl = $('menue'), kontextEl = $('kontext'), titelEl = $('titel');
const inventarEl = $('inventar'), figurenEl = $('figuren'), verbenEl = $('verben'), satzEl = $('satz'), punkteEl = $('punkte');
const ortTitelEl = $('orttitel'), hoverEl = $('hoverlabel'), blendeEl = $('blende'), ladenEl = $('laden'), karteEl = $('karte');
const STUMM = /[?&]ton=aus(&|$)/.test(window.location.search);
const ZEITRAFFER = /[?&]test(&|$)/.test(window.location.search);   // Browsertests mit Software-Rendering: große Zeitschritte erlauben

let speicherObjekt = null;
try { speicherObjekt = window.localStorage; } catch (_) { speicherObjekt = null; }
const ersatzSpeicher = { getItem() { throw new Error('kein Speicher'); }, setItem() { throw new Error('kein Speicher'); }, removeItem() { } };
const plaetze = S.speicherplaetze(speicherObjekt || ersatzSpeicher, 'fp-spiel3d-');

// ---------- Renderer ----------
const renderer = new THREE.WebGLRenderer({ canvas: leinwand, antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.05; renderer.outputColorSpace = THREE.SRGBColorSpace;
const kamera = new THREE.PerspectiveCamera(48, 16 / 9, .1, 400);
const kamZiel = new THREE.Vector3();

// ---------- Zustand ----------
let z = null, warteschlange = [], danach = null, pausiert = false, hover = null, geladen = false;
let verb = 'gehe', objekt = null, sprecher = null, sprecherSchreck = false, benutzenBis = 0, zeit = 0;
let akt = null;                 // aktueller Bereich (3D)
let gehalten = null;            // sichtbare Logik-Hotspots vor der letzten Aktion (Figuren verschwinden erst nach dem Text)
let blende = false, reise = null, letzterOrt = 'buero';
let mini = null, eingabeBoot = null, tippen = false;   // laufendes Minispiel (Bootsflucht oder Wahlkugel)
const endeEl = $('ende');
const hudEl = $('minihud');
const figuren = { joe: FIG.joe(), chani: FIG.chani() };
const held = { joe: { ziel: null, weg: [], auftrag: null }, chani: { ziel: null, weg: [] } };
let spur = [];                  // Fußspur der aktiven Figur (die andere folgt ihr)
const npcCache = {};

// ---------- Ton (wie in der Pixel-Fassung) ----------
let ton = true;
try { ton = window.localStorage.getItem('fp-spiel-ton') !== 'aus'; } catch (_) { ton = true; }
let audio = null, effektBus = null;
function bus() {
    audio = audio || new (window.AudioContext || window.webkitAudioContext)();
    if (!effektBus) {
        const k = audio.createDynamicsCompressor(); k.threshold.value = -20; k.knee.value = 12; k.ratio.value = 8; k.attack.value = .003; k.release.value = .15;
        effektBus = audio.createGain(); effektBus.gain.value = .5; effektBus.connect(k).connect(audio.destination);
    }
    if (audio.state === 'suspended') { audio.resume(); }
    return effektBus;
}
function klang(art) {
    if (!ton || STUMM) { return; }
    try {
        const ziel = bus(), t = audio.currentTime + .01;
        const folge = { klick: [[660, .05]], schritt: [[140, .05]], erfolg: [[523, .1], [659, .1], [784, .18]], fehler: [[220, .14], [165, .2]], kartusche: [[90, .22], [65, .18]], blende: [[300, .07], [200, .09]] }[art] || [[440, .06]];
        const spitze = art === 'schritt' ? .01 : art === 'klick' ? .03 : .05;
        let start = t;
        for (const [f, d] of folge) {
            const o = audio.createOscillator(), filter = audio.createBiquadFilter(), v = audio.createGain();
            o.type = art === 'kartusche' ? 'sawtooth' : art === 'schritt' ? 'triangle' : 'square'; o.frequency.value = f;
            filter.type = 'lowpass'; filter.frequency.value = art === 'schritt' ? 500 : 2200;
            v.gain.setValueAtTime(0, start); v.gain.linearRampToValueAtTime(spitze, start + .006); v.gain.setValueAtTime(spitze, start + d * .5); v.gain.linearRampToValueAtTime(0, start + d);
            o.connect(filter).connect(v).connect(ziel); o.start(start); o.stop(start + d + .02);
            o.onended = () => { o.disconnect(); filter.disconnect(); v.disconnect(); };
            start += d * .9;
        }
    } catch (_) { /* ohne Ton */ }
}

// ---------- Verben ----------
const VERBEN = [['gehe', 'Gehe zu'], ['ansehen', 'Schau an'], ['nimm', 'Nimm'], ['benutze', 'Benutze'], ['reden', 'Rede mit'], ['gib', 'Gib'], ['f1', ''], ['f2', '']];
const verbName = (v) => ((v === 'f1' || v === 'f2') ? S.FIGUREN[z.figur].faehigkeiten[v === 'f1' ? 0 : 1] : (VERBEN.find((x) => x[0] === v) || ['', ''])[1]);
function satz() {
    const ziel = hover ? hover.name : '';
    if (objekt) { return `${verb === 'gib' ? 'Gib' : 'Benutze'} ${S.GEGENSTAENDE[objekt].name} ${verb === 'gib' ? 'an' : 'mit'} ${ziel}`.trim(); }
    if (verb === 'f1' || verb === 'f2') { return `${verbName(verb)}: ${ziel}`.trim(); }
    return `${verbName(verb)} ${ziel}`.trim();
}

// ---------- Hotspots des Bereichs ----------
const logikHotspot = (id, liste) => (liste || S.sichtbareHotspots(z)).find((x) => x.id === id);
/** Aktiver Eintrag eines 3D-Hotspots im aktuellen Spielstand (oder null). */
function eintrag(h, sichtbare) {
    for (const e of h.eintraege) {
        if (e.id) { const lh = logikHotspot(e.id, sichtbare); if (lh) { return { art: 'logik', h: lh, e }; } continue; }
        if (e.tor) { if (!e.sichtbar || e.sichtbar(z)) { return { art: 'tor', e, name: e.name }; } continue; }
        if (e.deko) { return { art: 'deko', e, name: e.deko }; }
    }
    return null;
}
function ziele() {
    if (!akt || !z) { return []; }
    const sichtbare = S.sichtbareHotspots(z), aus = [];
    for (const h of akt.hotspots) {
        if (h.sichtbarObj && !h.sichtbarObj(z)) { continue; }
        const e = eintrag(h, sichtbare);
        if (e) { aus.push({ h, obj: h.obj, stand: h.stand, ...e, name: e.art === 'logik' ? e.h.name : e.name }); }
    }
    for (const n of npcs()) {
        if (!n.sichtbar) { continue; }
        const lh = logikHotspot(n.def.hotspot, sichtbare);
        if (lh && n.def.stand) { aus.push({ obj: n.rig, stand: n.def.stand, art: 'logik', h: lh, e: { id: lh.id }, name: lh.name, npc: n }); }
    }
    return aus;
}

// ---------- Nebenfiguren ----------
function npcs() {
    if (!akt) { return []; }
    const sichtbarIds = gehalten || new Set(S.sichtbareHotspots(z).map((h) => h.id));
    return akt.npcs.map((def) => {
        const key = akt.id + ':' + def.figur;
        if (!npcCache[key]) { const rig = FIG[def.figur](); rig.traverse((o) => { if (o.isMesh) { o.castShadow = true; } }); akt.scene.add(rig); npcCache[key] = { def, rig }; }
        const n = npcCache[key];
        n.sichtbar = def.sichtbar ? !!def.sichtbar(z) : sichtbarIds.has(def.hotspot);
        return n;
    });
}
function npcsAktualisieren() {
    for (const n of npcs()) {
        const { def, rig } = n;
        rig.visible = n.sichtbar;
        if (!n.sichtbar) { continue; }
        const fest = def.fest && z.flags.gegnerArchivFest && !gehalten;
        const amEnde = def.ende && z.ende && textbox.hidden;
        const [x, zz] = fest ? def.fest : amEnde ? def.ende : def.pos;
        rig.position.set(x, akt.hoehe(x, zz), zz);
        rig.rotation.y = fest ? def.festRot : amEnde ? def.endeRot : def.rot;
        // Wer spricht, schaut zur aktiven Figur
        const redet = !textbox.hidden && (sprecher === def.hotspot || sprecher === def.figur || (sprecher === 'gegner' && (def.hotspot === 'gegner' || def.figur === 'pinsel' || def.figur === 'ledeiw')));
        const p = figuren[z.figur].position;
        if (redet && !fest) { rig.rotation.y = Math.atan2(p.x - x, p.z - zz); }
        let art = redet ? (sprecherSchreck ? 'erschrocken' : 'reden') : (def.pose || 'stehen');
        if (fest && !redet) { art = 'kleben'; }
        FIG.pose(rig, zeit, art);
    }
}

// ---------- Wegsuche (Raster, A*, Sichtlinien-Glättung) ----------
const RASTER = .25;
let npcHindernisse = [];
/** Begehbar: Bereich frei und keine sichtbare Nebenfigur im Weg. */
function frei(x, zz) {
    if (!akt.frei(x, zz, z)) { return false; }
    for (const [nx, nz] of npcHindernisse) { if (Math.hypot(x - nx, zz - nz) < .55) { return false; } }
    return true;
}
function sichtlinie(a, b) {
    const d = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.max(1, Math.ceil(d / .1));
    for (let i = 1; i <= n; i++) { const t = i / n; if (!frei(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)) { return false; } }
    return true;
}
function wegSuchen(von, nach) {
    if (sichtlinie(von, nach)) { return [nach]; }
    const minX = Math.min(von[0], nach[0]) - 14, minZ = Math.min(von[1], nach[1]) - 14;
    const nx = Math.ceil((Math.abs(nach[0] - von[0]) + 28) / RASTER), nz = Math.ceil((Math.abs(nach[1] - von[1]) + 28) / RASTER);
    const zelle = (p) => [Math.round((p[0] - minX) / RASTER), Math.round((p[1] - minZ) / RASTER)];
    const pos = (i, k) => [minX + i * RASTER, minZ + k * RASTER];
    const ok = new Uint8Array(nx * nz), g = new Float32Array(nx * nz).fill(Infinity), vor = new Int32Array(nx * nz).fill(-1), zu = new Uint8Array(nx * nz);
    for (let k = 0; k < nz; k++) { for (let i = 0; i < nx; i++) { const p = pos(i, k); ok[k * nx + i] = frei(p[0], p[1]) ? 1 : 2; } }
    const [si, sk] = zelle(von), [zi, zk] = zelle(nach);
    const start = sk * nx + si, ziel = zk * nx + zi;
    if (si < 0 || sk < 0 || si >= nx || sk >= nz) { return [nach]; }
    ok[start] = 1;
    const offen = [[0, start]]; g[start] = 0;
    const hoch = (a) => { let i = offen.length - 1; offen.push(a); while (i >= 0 && offen[i][0] > a[0]) { offen[i + 1] = offen[i]; i--; } offen[i + 1] = a; };
    const h = (i, k) => Math.hypot(i - zi, k - zk);
    let bester = start, besterH = h(si, sk), schritte = 0;
    while (offen.length && schritte++ < 60000) {
        const [, c] = offen.pop();
        if (zu[c]) { continue; } zu[c] = 1;
        const ci = c % nx, ck = (c - ci) / nx;
        const hh = h(ci, ck); if (hh < besterH) { besterH = hh; bester = c; }
        if (c === ziel) { break; }
        for (let di = -1; di <= 1; di++) {
            for (let dk = -1; dk <= 1; dk++) {
                if (!di && !dk) { continue; }
                const i = ci + di, k = ck + dk; if (i < 0 || k < 0 || i >= nx || k >= nz) { continue; }
                const n = k * nx + i; if (ok[n] !== 1 || zu[n]) { continue; }
                if (di && dk && (ok[ck * nx + i] !== 1 || ok[k * nx + ci] !== 1)) { continue; }
                const ng = g[c] + (di && dk ? 1.414 : 1);
                if (ng < g[n]) { g[n] = ng; vor[n] = c; hoch([-(ng + h(i, k)), n]); }
            }
        }
    }
    const ende = zu[ziel] ? ziel : bester;
    const punkte = [];
    for (let c = ende; c !== -1 && c !== start; c = vor[c]) { const ci = c % nx; punkte.unshift(pos(ci, (c - ci) / nx)); }
    if (ende === ziel && frei(nach[0], nach[1])) { punkte.push(nach); }
    // glätten: nur Eckpunkte behalten, zwischen denen keine Sichtlinie besteht
    const glatt = []; let a = von;
    for (let i = 0; i < punkte.length; i++) {
        if (i === punkte.length - 1 || !sichtlinie(a, punkte[i + 1])) { glatt.push(punkte[i]); a = punkte[i]; }
    }
    return glatt;
}
const xz = (o) => [o.position.x, o.position.z];

/** Aktive Figur zu einem Ort schicken; danach ggf. eine Aktion ausführen. */
function gehe(ziel, auftrag, blickAuf) {
    const n = z.figur, f = figuren[n];
    if (f.userData.aufPlatte) { S.platteVerlassen(z, n); f.userData.aufPlatte = null; }
    npcHindernisse = npcs().filter((m) => m.sichtbar).map((m) => [m.rig.position.x, m.rig.position.z]);
    held[n].weg = wegSuchen(xz(f), ziel);
    held[n].auftrag = auftrag || null; held[n].blickAuf = blickAuf || null;
    if (!held[n].weg.length) { ankunft(n); }
}
function ankunft(n) {
    const h = held[n];
    if (h.blickAuf) { const p = new THREE.Vector3(); h.blickAuf.getWorldPosition(p); const f = figuren[n]; f.userData.blickZiel = Math.atan2(p.x - f.position.x, p.z - f.position.z); }
    const a = h.auftrag; h.auftrag = null; h.blickAuf = null;
    if (a) { a(); }
}
function laufen(dt) {
    const aktiv = z.figur, andere = aktiv === 'joe' ? 'chani' : 'joe';
    const fa = figuren[aktiv], fb = figuren[andere];
    const schritt = (f, ziel, tempo) => {
        const dx = ziel[0] - f.position.x, dz = ziel[1] - f.position.z, d = Math.hypot(dx, dz);
        const s = Math.min(d, tempo * dt);
        if (d > 1e-4) { f.position.x += dx / d * s; f.position.z += dz / d * s; f.userData.blickZiel = Math.atan2(dx, dz); }
        return d - s;
    };
    const ha = held[aktiv];
    fa.userData.geht = false;
    if (ha.weg.length) {
        fa.userData.geht = true;
        const rest = schritt(fa, ha.weg[0], 2.3);
        if (rest < .02) { ha.weg.shift(); if (!ha.weg.length) { ankunft(aktiv); } }
        const l = spur[spur.length - 1];
        if (!l || Math.hypot(l[0] - fa.position.x, l[1] - fa.position.z) > .1) { spur.push(xz(fa)); if (spur.length > 400) { spur.shift(); } }
        fa.userData.schritt = (fa.userData.schritt || 0) + dt; if (fa.userData.schritt > .38) { fa.userData.schritt = 0; klang('schritt'); }
    }
    // Begleitung folgt der Spur im Abstand von gut einem Meter
    fb.userData.geht = false;
    let ziel = null, laenge = 0;
    for (let i = spur.length - 1; i > 0; i--) { laenge += Math.hypot(spur[i][0] - spur[i - 1][0], spur[i][1] - spur[i - 1][1]); if (laenge > 1.15) { ziel = spur[i - 1]; break; } }
    if (fb.userData.aufPlatte) { ziel = null; }
    if (ziel && Math.hypot(fb.position.x - fa.position.x, fb.position.z - fa.position.z) > 1.0) {
        const rest = schritt(fb, ziel, 2.6); fb.userData.geht = rest > .02;
    } else if (!fa.userData.geht && Math.hypot(fb.position.x - fa.position.x, fb.position.z - fa.position.z) < 2) {
        // stehen beide, schaut die Begleitung zur aktiven Figur
        fb.userData.blickZiel = Math.atan2(fa.position.x - fb.position.x, fa.position.z - fb.position.z);
    }
    for (const n of ['joe', 'chani']) {
        const f = figuren[n];
        if (!f.userData.aufPlatte) { f.position.y = akt.hoehe(f.position.x, f.position.z); }
        if (f.userData.blickZiel !== undefined) { let d = f.userData.blickZiel - f.rotation.y; d = Math.atan2(Math.sin(d), Math.cos(d)); f.rotation.y += d * Math.min(1, dt * 9); }
    }
}
function aufstellen(spawnName) {
    const s = akt.spawn[spawnName] || akt.spawn.karte || Object.values(akt.spawn)[0];
    const fa = figuren[z.figur], fb = figuren[z.figur === 'joe' ? 'chani' : 'joe'];
    fa.position.set(s[0], akt.hoehe(s[0], s[1]), s[1]); fa.rotation.y = s[2] || 0; fa.userData.blickZiel = fa.rotation.y;
    // Begleitung einen Schritt dahinter (oder daneben, wenn dort kein Platz ist)
    const r = fa.rotation.y;
    const kandidaten = [[-Math.sin(r) * 1.0, -Math.cos(r) * 1.0], [Math.cos(r) * .9, -Math.sin(r) * .9], [-Math.cos(r) * .9, Math.sin(r) * .9], [Math.sin(r) * .9, Math.cos(r) * .9]];
    const k = kandidaten.find(([dx, dz]) => frei(s[0] + dx, s[1] + dz)) || kandidaten[0];
    fb.position.set(s[0] + k[0], akt.hoehe(s[0] + k[0], s[1] + k[1]), s[1] + k[1]); fb.rotation.y = r; fb.userData.blickZiel = r;
    spur = [xz(fb), xz(fa)];
    held.joe.weg = []; held.chani.weg = []; held.joe.auftrag = held.chani.auftrag = null;
    const kz = akt.kamera(fa.position); kamera.position.copy(kz.p); kamZiel.copy(kz.t);
}

/** In einen Bereich wechseln (aufbauen, falls nötig). */
async function betreten(id, spawnName) {
    ladenEl.hidden = false;
    let b;
    try { b = await bereichLaden(id, renderer); } finally { ladenEl.hidden = true; }
    akt = b; hover = null; hoverEl.hidden = true; auswahl = -1;
    b.scene.add(figuren.joe, figuren.chani);
    aufstellen(spawnName);
    if (b.aktualisieren) { b.aktualisieren(z); }
    ortTitelEl.textContent = b.titel;
    karteEl.hidden = true;
    renderer.compile(b.scene, kamera);
}

// ---------- Blende ----------
function blenden(fn) {
    klang('blende'); blende = true; blendeEl.classList.add('zu');
    setTimeout(async () => { try { await fn(); } finally { blendeEl.classList.remove('zu'); setTimeout(() => { blende = false; }, 350); } }, 380);
}

// ---------- Text, Porträts, Dialoge ----------
const SPRECHER = [['Joe', 'joe'], ['Chani', 'chani'], ['Chris', 'christian'], ['Pinsel', 'pinsel'], ['Ledeiw', 'ledeiw'], ['Relbeil', 'relbeil'], ['Johanna', 'joanna'], ['Lorette', 'lore'], ['Kemper', 'kemper'], ['Frau Kemper', 'kemper']];
const SPRECHER_HOTSPOT = { christian: 'chris', joanna: 'joanna', lore: 'lore', kemper: 'bauleiter' };
const sprecherAus = (zeile) => { for (const [n, id] of SPRECHER) { if (zeile.startsWith(n + ':') || zeile.startsWith(n + ' ')) { return id; } } return null; };
const ausdruck = (zeile) => (/(Kugel|rollt|heult|Knirschen|Hilfe|poltert|stürzen|klebt)/.test(zeile) ? 'erschrocken' : /(!|Geschafft|Danke|richtig|Solidarität|leicht)/.test(zeile) ? 'lacht' : 'neutral');
const hutAuf = () => !!z && z.inventar.includes('taschenlampe');
function portraetBild(n, a) { return G.ausschnitt(G.portraet(n === 'joe' && hutAuf() ? 'joe_hut' : n, a), 64, 64); }

function zeigeText() {
    if (warteschlange.length === 0) {
        textbox.hidden = true; sprecher = null; gehalten = null;
        if (akt && akt.aktualisieren) { akt.aktualisieren(z); }
        const d = danach; danach = null;
        if (d && d.dialog) { dialogZeigen(d.dialog); }
        if (d && d.gespraech) { gespraechZeigen(d.gespraech); }
        if (d && d.minispiel === 'boot') { bootStarten(); }
        if (d && d.minispiel === 'kugel') { kugelStarten(); }
        leisteAktualisieren();
        return;
    }
    const zeile = warteschlange.shift(), wer = sprecherAus(zeile);
    sprecherSchreck = ausdruck(zeile) === 'erschrocken';
    sprecher = wer === 'pinsel' || wer === 'ledeiw' ? 'gegner' : SPRECHER_HOTSPOT[wer] || wer;
    portraitEl.replaceChildren();
    if (wer) { const bild = portraetBild(wer, ausdruck(zeile)); bild.className = 'portraitbild'; portraitEl.appendChild(bild); portraitEl.hidden = false; } else { portraitEl.hidden = true; }
    textEl.textContent = zeile; textbox.hidden = false;
}

/** Ergebnis einer Aktion anzeigen; bei Szenenwechsel Blende und neuer Bereich. */
function ergebnis(e, vorherSzene) {
    const vorher = vorherSzene === undefined ? z.szene : vorherSzene;
    warteschlange = warteschlange.concat(e.text || []);
    if (e.dialog) { danach = { dialog: e.dialog }; }
    if (e.minispiel === 'boot') { danach = { minispiel: 'boot' }; }
    if (e.minispiel === 'kugel') { danach = { minispiel: 'kugel' }; }
    // Figuren, die erst nach dem Text auftauchen (Pinsel und Ledeiw nach dem Bootsunglück)
    if (e.erscheinen) { gehalten = new Set(S.sichtbareHotspots(z).map((h) => h.id).filter((id) => !e.erscheinen.includes(id))); }
    if (z.szene !== vorher) {
        if (z.szene !== 'karte' && S.SZENEN[z.szene] && !e.ohneEinstieg && !(e.text || []).includes(S.einstieg(z, z.szene))) { warteschlange.push(S.einstieg(z, z.szene)); }
        if (S.ORTE[vorher]) { letzterOrt = vorher; }
        plaetze.schreiben('auto', z);
    }
    hover = null; hoverEl.hidden = true;
    zeigeText();
}

/** Szene der Spiellogik darstellen (nach Reise, Laden, Ausgang). */
async function szeneZeigen(spawn) {
    if (z.szene === 'karte') { karteZeigen(); return; }
    const liste = SZENE_ZU_BEREICHEN[z.szene];
    if (!liste) { nochNichtIn3d(); return; }
    let id = startBereich(z, z.szene);
    if (z.szene === 'buero' && spawn === 'start') { id = 'buero_innen'; }
    if (z.szene === 'buero' && spawn !== 'start') { id = 'schmiede_aussen'; }
    await betreten(id, spawn || 'karte');
}

function dialogZeigen(art) {
    const d = S.dialogInhalt(z, art);
    if (!d) { return; }
    auswahlListe(d.frage, d.optionen.map((o) => ({ text: o.text, tun: () => { klang('klick'); handlungsErgebnis(() => S.dialogWahl(z, art, o.id)); } })));
}
function anrufen() {
    if (z.figur !== 'joe') { ergebnis({ text: ['Chani: „Das ist Joes Handy. Joe, ruf du an – mit Tasten kann ich nicht.“'] }); return; }
    klang('klick');
    auswahlListe('Joe ruft Professor Relbeil an:', [
        { text: '„Guten Tag!“', tun: () => ergebnis(S.anrufen(z, 'hallo')) },
        { text: '„Ich brauche deine Hilfe!“', tun: () => ergebnis(S.anrufen(z, 'hilfe')) },
        { text: 'Auflegen', tun: () => leisteAktualisieren() }
    ]);
}
function gespraechZeigen(h) {
    const anrede = S.FIGUREN[z.figur].name;
    auswahlListe(`${anrede} spricht mit ${h.name}:`, [
        { text: '„Wer sind Sie eigentlich?“', tun: () => handlung('ansehen', h) },
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
let hinweisTimer = null;
function lesehinweis() {
    let el = $('lesehinweis');
    if (!el) { el = document.createElement('div'); el.id = 'lesehinweis'; el.setAttribute('role', 'status'); buehne.appendChild(el); }
    el.textContent = 'Erst zu Ende lesen – ein Klick auf den Text blättert weiter.'; el.classList.add('sichtbar');
    clearTimeout(hinweisTimer); hinweisTimer = setTimeout(() => el.classList.remove('sichtbar'), 2200);
}

// ---------- Aktionen ----------
/** Aktion der Spiellogik ausführen; Szenenwechsel mit Blende, Figuren verschwinden erst nach dem Text. */
function handlungsErgebnis(tun, opt) {
    const szene = z.szene, vorherSichtbar = new Set(S.sichtbareHotspots(z).map((h) => h.id)), punkteVorher = z.punkte;
    const e = tun();
    klang(opt && opt.kartusche ? 'kartusche' : z.punkte > punkteVorher ? 'erfolg' : 'klick');
    if (z.szene !== szene) {
        const neu = z.szene; z.szene = szene;
        blenden(async () => { z.szene = neu; await szeneZeigen('von_' + szene); ergebnis(e, szene); });
        return;
    }
    const jetzt = new Set(S.sichtbareHotspots(z).map((h) => h.id));
    if ((e.text || []).length && [...vorherSichtbar].some((id) => !jetzt.has(id))) { gehalten = vorherSichtbar; }
    if (akt && akt.aktualisieren && !gehalten) { akt.aktualisieren(z); }
    ergebnis(e);
}
function handlung(art, h, gegenstand) {
    if (art !== 'ansehen' && art !== 'reden') { benutzenBis = zeit + .9; }
    handlungsErgebnis(() => S.aktion(z, art, h.id, gegenstand), { kartusche: art === 'f1' && z.figur === 'joe' });
}
const istPerson = (lh) => lh.form === 'person' || lh.form === 'gegner';
function standard(lh) { if (istPerson(lh)) { return 'reden'; } if (['pfeil', 'tuer', 'tor'].includes(lh.form)) { return 'benutzen'; } return 'ansehen'; }

/** Tor in einen anderen Bereich benutzen. */
function torBenutzen(e) {
    if (e.sperre && e.sperre(z)) { handlung('benutzen', logikHotspot(e.sperreAktion)); return; }
    klang('klick');
    blenden(() => betreten(e.tor, e.spawn));
}

function zielAusloesen(zi, langDruck, px, py) {
    if (langDruck) { kontextZeigen(zi, px, py); return; }
    const v = verb, o = objekt;
    verb = 'gehe'; objekt = null; auswahl = -1;
    gehe(zi.stand, () => {
        if (zi.art === 'logik') {
            const lh = logikHotspot(zi.h.id);
            if (!lh) { return; }
            if (o) { handlung('gegenstand', lh, o); return; }
            const art = v === 'gehe' ? standard(lh) : v === 'nimm' || v === 'benutze' ? 'benutzen' : v === 'gib' ? null : v;
            if (art === null) { ergebnis({ text: [`${S.FIGUREN[z.figur].name}: „Was soll ich geben? Erst einen Gegenstand im Inventar wählen.“`] }); return; }
            if (art === 'reden' && istPerson(lh)) { gespraechZeigen(lh); return; }
            handlung(art, lh);
            return;
        }
        const pseudo = { name: zi.name };
        if (o) { ergebnis(S.STANDARD.gegenstand(z, pseudo, o)); return; }
        if (zi.art === 'tor') {
            if (v === 'ansehen') { ergebnis({ text: [zi.e.ansehen || zi.name] }); return; }
            if (v === 'gehe' || v === 'benutze' || v === 'nimm') { torBenutzen(zi.e); return; }
        }
        if (zi.art === 'deko' && (v === 'gehe' || v === 'ansehen')) { ergebnis({ text: [zi.e.text] }); return; }
        const art = v === 'nimm' || v === 'benutze' ? 'benutzen' : v === 'gib' ? 'benutzen' : v;
        ergebnis(S.STANDARD[art] ? S.STANDARD[art](z, pseudo) : { text: ['Hm.'] });
    }, zi.obj);
    leisteAktualisieren();
}

function kontextZeigen(zi, x, y) {
    kontextEl.replaceChildren();
    const kopf = document.createElement('div'); kopf.className = 'kopf'; kopf.textContent = zi.name; kontextEl.appendChild(kopf);
    const f = S.FIGUREN[z.figur].faehigkeiten;
    const knopf = (t, fn) => { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', () => { kontextEl.hidden = true; fn(); }); kontextEl.appendChild(k); };
    [['ansehen', 'Schau an'], ['benutze', 'Nimm / Benutze'], ['reden', 'Rede mit'], ['f1', f[0]], ['f2', f[1]]].forEach(([v, t], i) => knopf(`${i + 1}. ${t}`, () => { verb = v; objekt = null; zielAusloesen(zi, false); }));
    for (const gid of z.inventar) { knopf(`➜ ${S.GEGENSTAENDE[gid].name}`, () => { verb = 'benutze'; objekt = gid; zielAusloesen(zi, false); }); }
    knopf('Abbrechen', () => { });
    const bu = buehne.getBoundingClientRect();
    kontextEl.hidden = false;
    kontextEl.style.left = `${Math.min(Math.max(x - bu.left, 4), bu.width - kontextEl.offsetWidth - 4)}px`;
    kontextEl.style.top = `${Math.min(Math.max(y - bu.top, 4), bu.height - kontextEl.offsetHeight - 4)}px`;
    kontextEl.querySelector('button').focus();
}

// ---------- Leiste ----------
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
        k.addEventListener('click', () => { if (!textbox.hidden) { lesehinweis(); return; } if (z.figur !== id) { figurWechseln(); } });
        figurenEl.appendChild(k);
    }
    inventarEl.replaceChildren();
    for (const gid of z.inventar) {
        const k = document.createElement('button'); k.type = 'button'; k.className = 'ding' + (objekt === gid ? ' aktiv' : ''); k.title = S.GEGENSTAENDE[gid].name; k.setAttribute('aria-label', S.GEGENSTAENDE[gid].name);
        const bild = G.ausschnitt(G.icon(gid), 16, 16); bild.className = 'icon'; k.appendChild(bild);
        k.addEventListener('click', () => {
            if (gid === 'handy' && (verb === 'benutze' || verb === 'gehe')) { if (!textbox.hidden) { lesehinweis(); return; } anrufen(); return; }
            if (verb === 'benutze' || verb === 'gib') { objekt = objekt === gid ? null : gid; klang('klick'); leisteAktualisieren(); return; }
            if (!textbox.hidden) { zeigeText(); return; }
            ergebnis({ text: [`${S.GEGENSTAENDE[gid].name}: ${S.GEGENSTAENDE[gid].text}`] });
        });
        inventarEl.appendChild(k);
    }
    punkteEl.textContent = `${z.punkte} Punkte`;
}
function figurWechseln() {
    klang('klick');
    const e = S.figurWechseln(z);
    held.joe.weg = []; held.chani.weg = [];
    spur = [xz(figuren[z.figur === 'joe' ? 'chani' : 'joe']), xz(figuren[z.figur])];
    ergebnis(e); leisteAktualisieren();
}

// ---------- Eingabe: Raycast auf Hotspots und Boden ----------
const strahl = new THREE.Raycaster(), ndc = new THREE.Vector2();
function sichtbarImBaum(o) { for (let x = o; x; x = x.parent) { if (!x.visible) { return false; } } return true; }
function treffer(ev) {
    if (!akt) { return null; }
    const r = leinwand.getBoundingClientRect();
    ndc.set((ev.clientX - r.left) / r.width * 2 - 1, -((ev.clientY - r.top) / r.height) * 2 + 1);
    strahl.setFromCamera(ndc, kamera);
    const liste = ziele(), figurenMeshes = new Set();
    figuren.joe.traverse((o) => figurenMeshes.add(o)); figuren.chani.traverse((o) => figurenMeshes.add(o));
    const hits = strahl.intersectObject(akt.scene, true);
    for (const hit of hits) {
        if (!hit.object.isMesh || figurenMeshes.has(hit.object) || hit.object.userData.durchlaessig || !sichtbarImBaum(hit.object)) { continue; }   // Gitterzäune lassen Klicks durch
        let zi = null;
        for (let o = hit.object; o && !zi; o = o.parent) { zi = liste.find((l) => l.obj === o) || null; }
        if (zi) { return { ziel: zi }; }
        if (hit.object.material && hit.object.material.visible === false) { continue; }   // unsichtbare Klickkörper inaktiver Ziele
        if (akt.boden.includes(hit.object)) { return { boden: [hit.point.x, hit.point.z] }; }
        return null;
    }
    return null;
}
function blockiert() { return !z || pausiert || !!mini || !textbox.hidden || !dialogEl.hidden || !titelEl.hidden || blende || reise || !karteEl.hidden; }

let letzteBewegung = 0;
leinwand.addEventListener('pointermove', (ev) => {
    if (blockiert()) { return; }
    const jetzt = performance.now(); if (jetzt - letzteBewegung < 40) { return; } letzteBewegung = jetzt;
    const t = treffer(ev);
    hover = t && t.ziel ? t.ziel : null;
    leinwand.style.cursor = hover ? 'pointer' : 'crosshair';
    if (hover) { const bu = buehne.getBoundingClientRect(); hoverEl.textContent = hover.name; hoverEl.style.left = `${ev.clientX - bu.left}px`; hoverEl.style.top = `${ev.clientY - bu.top - 34}px`; hoverEl.hidden = false; } else { hoverEl.hidden = true; }
});
let druck = null;
leinwand.addEventListener('pointerdown', (ev) => {
    if (!z || pausiert || !titelEl.hidden) { return; }
    ev.preventDefault();
    if (!textbox.hidden) { zeigeText(); return; }
    if (mini) { if (mini.art === 'kugel') { tippen = true; return; } const r = leinwand.getBoundingClientRect(); eingabeBoot = ev.clientY - r.top < r.height / 2 ? 'hoch' : 'runter'; return; }
    if (blockiert()) { return; }
    kontextEl.hidden = true;
    const p = { clientX: ev.clientX, clientY: ev.clientY };
    druck = { p, lang: false, timer: setTimeout(() => { druck.lang = true; const t = treffer(p); if (t && t.ziel) { zielAusloesen(t.ziel, true, p.clientX, p.clientY); } }, 480) };
});
leinwand.addEventListener('pointerup', () => {
    if (!druck) { return; }
    clearTimeout(druck.timer);
    const d = druck; druck = null;
    if (d.lang || blockiert()) { return; }
    const t = treffer(d.p);
    if (!t) { return; }
    if (t.boden) { verb = 'gehe'; objekt = null; gehe(t.boden); leisteAktualisieren(); return; }
    zielAusloesen(t.ziel, false);
});
leinwand.addEventListener('contextmenu', (ev) => ev.preventDefault());
textbox.addEventListener('click', () => zeigeText());

let auswahl = -1;
document.addEventListener('keydown', (ev) => {
    if (!z || !titelEl.hidden) { return; }
    if (ev.key === 'Escape' && !textbox.hidden && (warteschlange.some((l) => S.VORSTELLUNG.includes(l)) || S.VORSTELLUNG.includes(textEl.textContent))) {
        ev.preventDefault(); warteschlange = warteschlange.filter((l) => !S.VORSTELLUNG.includes(l)); zeigeText(); return;
    }
    if (ev.key === 'Escape' || ev.key === 'p' || ev.key === 'P') { ev.preventDefault(); pauseUmschalten(); return; }
    if (pausiert) { return; }
    if (!textbox.hidden && (ev.key === 'Enter' || ev.key === ' ')) { ev.preventDefault(); zeigeText(); return; }
    if (mini) {
        if (mini.art === 'kugel') { if (ev.key === ' ' || ev.key === 'ArrowRight' || ev.key === 'Enter') { tippen = true; ev.preventDefault(); } return; }
        if (ev.key === 'ArrowUp' || ev.key === 'w') { eingabeBoot = 'hoch'; ev.preventDefault(); }
        if (ev.key === 'ArrowDown' || ev.key === 's') { eingabeBoot = 'runter'; ev.preventDefault(); }
        return;
    }
    const n = Number(ev.key);
    if (!dialogEl.hidden) { const k = dialogEl.querySelectorAll('button'); if (n >= 1 && n <= k.length) { k[n - 1].click(); } return; }
    if (!kontextEl.hidden) { const k = kontextEl.querySelectorAll('button'); if (n >= 1 && n <= 5) { k[n - 1].click(); } return; }
    if (!karteEl.hidden) { return; }
    if (n >= 1 && n <= 8) { verb = VERBEN[n - 1][0]; objekt = null; leisteAktualisieren(); return; }
    if (ev.key === 'Tab' || ev.key === 'f') { ev.preventDefault(); if (textbox.hidden) { figurWechseln(); } return; }
    const liste = ziele();
    if (ev.key === 'ArrowRight' || ev.key === 'ArrowLeft') { ev.preventDefault(); auswahl = (auswahl + (ev.key === 'ArrowRight' ? 1 : -1) + liste.length) % liste.length; hover = liste[auswahl] || null; hoverEl.hidden = !hover; if (hover) { const p = bildschirm(hover.obj); hoverEl.textContent = hover.name; hoverEl.style.left = `${p.x}px`; hoverEl.style.top = `${p.y - 34}px`; } }
    if (ev.key === 'Enter' && hover && textbox.hidden) { ev.preventDefault(); zielAusloesen(hover, false); }
});

/** Bildschirmposition (relativ zur Bühne) eines Objekts. */
function bildschirm(obj) {
    const b = new THREE.Box3().setFromObject(obj);
    let p = b.isEmpty() ? obj.getWorldPosition(new THREE.Vector3()) : b.getCenter(new THREE.Vector3());
    if (obj.userData.hips) { p = obj.getWorldPosition(new THREE.Vector3()); p.y += 1.15; }   // Figuren: Brusthöhe
    p.project(kamera);
    const r = leinwand.getBoundingClientRect(), bu = buehne.getBoundingClientRect();
    return { x: (p.x + 1) / 2 * r.width + r.left - bu.left, y: (1 - p.y) / 2 * r.height + r.top - bu.top, sichtbar: p.z < 1 };
}

// ---------- Reisekarte (Pixel-Karte als Überlagerung) ----------
const karteCtx = karteEl.getContext('2d');
const KB = 256, KH = 144;
const ortPunkt = (id) => ({ x: S.ORTE[id].x * KB / 100, y: S.ORTE[id].y * KH / 100 });
let karteHover = null;
function karteZeigen() { karteEl.hidden = false; ortTitelEl.textContent = 'Greven'; akt = null; }
function zeichneKarte() {
    karteCtx.imageSmoothingEnabled = false;
    const k = G.bild('karte_greven_256x144');
    if (k) { karteCtx.drawImage(k, 0, 0); }
    for (const id of Object.keys(S.ORTE)) {
        const { x, y } = ortPunkt(id);
        if (!z.orte.includes(id)) { karteCtx.globalAlpha = .55; karteCtx.fillStyle = 'rgb(228,196,148)'; karteCtx.beginPath(); karteCtx.arc(x, y, 5, 0, Math.PI * 2); karteCtx.fill(); karteCtx.globalAlpha = 1; continue; }
        const puls = karteHover === id ? 7 : 6 + (Math.floor(zeit * 3) % 2);
        karteCtx.strokeStyle = karteHover === id ? 'rgb(252,216,0)' : 'rgba(255,255,255,0.8)'; karteCtx.lineWidth = 1;
        karteCtx.beginPath(); karteCtx.arc(Math.round(x) + .5, Math.round(y) + .5, puls, 0, Math.PI * 2); karteCtx.stroke();
        if (SZENE_ZU_BEREICHEN[id]) { karteCtx.fillStyle = 'rgb(252,216,0)'; karteCtx.fillRect(Math.round(x) + 6, Math.round(y) - 8, 3, 3); }
    }
    if (reise) {
        const a = ortPunkt(reise.von), b = ortPunkt(reise.nach), t = Math.min(1, reise.t);
        const n = Math.max(8, Math.round(Math.hypot(b.x - a.x, b.y - a.y) / 2)), bis = Math.floor(t * n);
        const bogen = (u) => ({ x: a.x + (b.x - a.x) * u, y: a.y + (b.y - a.y) * u - Math.sin(u * Math.PI) * 10 });
        karteCtx.fillStyle = 'rgb(188,0,0)';
        for (let i = 0; i <= bis; i++) { const p = bogen(i / n); if (i % 3 === 2) { continue; } karteCtx.fillRect(Math.round(p.x) - 1, Math.round(p.y) - 1, 2, 2); }
        const p = bogen(t); karteCtx.fillStyle = 'rgb(60,30,20)'; karteCtx.fillRect(Math.round(p.x) - 3, Math.round(p.y) - 9, 7, 3); karteCtx.fillRect(Math.round(p.x) - 2, Math.round(p.y) - 12, 5, 3);
    }
}
function kartePunkt(ev) {
    const r = karteEl.getBoundingClientRect(), x = (ev.clientX - r.left) * KB / r.width, y = (ev.clientY - r.top) * KH / r.height;
    const o = Object.entries(S.ORTE).find(([id, ort]) => z.orte.includes(id) && Math.abs(x - ort.x * KB / 100) < 8 && Math.abs(y - ort.y * KH / 100) < 8);
    return o ? o[0] : null;
}
karteEl.addEventListener('pointermove', (ev) => { if (!z || reise || !textbox.hidden) { return; } karteHover = kartePunkt(ev); karteEl.style.cursor = karteHover ? 'pointer' : 'default'; satzEl.textContent = `Reise nach ${karteHover ? S.ORTE[karteHover].name : '…'}`; });
karteEl.addEventListener('pointerdown', (ev) => {
    if (!z || pausiert) { return; }
    if (!textbox.hidden) { zeigeText(); return; }
    if (reise || !dialogEl.hidden || blende) { return; }
    const ort = kartePunkt(ev); if (ort) { reiseStarten(ort); }
});
function reiseStarten(ziel) {
    if (!SZENE_ZU_BEREICHEN[ziel]) { nochNichtIn3d(ziel); return; }
    klang('klick');
    reise = { von: S.ORTE[letzterOrt] ? letzterOrt : 'buero', nach: ziel, t: ziel === letzterOrt ? 1 : 0 };
}
function reiseAnkunft(ziel) {
    blenden(async () => { const v = z.szene; const e = S.reisen(z, ziel); await szeneZeigen('karte'); ergebnis(e, v); });
}

/** Schauplätze ab Akt 3 gibt es (noch) nur in der Pixel-Fassung: Spielstand dorthin mitnehmen. */
function nochNichtIn3d(ort) {
    const name = ort ? S.ORTE[ort].name : (S.SZENEN[z.szene] ? S.SZENEN[z.szene].titel : z.szene);
    auswahlListe(`„${name}“ gibt es in der 3D-Fassung noch nicht. In der klassischen Pixel-Fassung könnt ihr mit diesem Spielstand weiterspielen.`, [
        { text: 'In der klassischen Fassung weiterspielen', tun: () => {
            if (ort && z.szene === 'karte') { S.reisen(z, ort); }
            try { window.localStorage.setItem('fp-spiel-auto', S.speichern(z)); } catch (_) { /* ohne Speicher */ }
            plaetze.schreiben('auto', z);
            window.location.href = '../spiel/';
        } },
        { text: 'Hierbleiben', tun: () => { if (z.szene !== 'karte' && !SZENE_ZU_BEREICHEN[z.szene]) { z.szene = 'karte'; karteZeigen(); } } }
    ]);
}

// ---------- Minispiel: Bootsflucht auf der Ems ----------
function bootRigs() {
    const key = 'boot:verfolger';
    if (!npcCache[key]) { const r = [FIG.pinsel(), FIG.ledeiw()]; r.forEach((x) => akt.scene.add(x)); npcCache[key] = { rigs: r }; }
    return npcCache[key].rigs;
}
function bootStarten() {
    if (!akt || !akt.bootFlucht) { return; }
    mini = S.bootNeu(Date.now() % 1000); eingabeBoot = null;
    akt.bootFlucht.start([figuren.joe, figuren.chani], bootRigs());
    hudEl.hidden = false;
}
function bootSchritt(dt) {
    // Feste Teilschritte, damit das Minispiel bei jeder Bildrate gleich schwer ist
    let rest = Math.min(dt, ZEITRAFFER ? 1 : .25);
    while (rest > 1e-6 && mini.status === 'laeuft') {
        const d = Math.min(.05, rest); rest -= d;
        const ein = ZEITRAFFER ? S.bootAutopilot(mini) : eingabeBoot; eingabeBoot = null;
        S.bootSchritt(mini, d, ein);
    }
    hudEl.textContent = mini.sinken !== null ? `Blubb!   Leben ${'♥'.repeat(Math.max(0, mini.leben))}` : `Alternative Fakten: ${mini.munition + mini.schuesse.length}   Leben ${'♥'.repeat(Math.max(0, mini.leben))}   ·   ↑/↓ oder oben/unten tippen`;
    if (mini.status === 'laeuft') { return; }
    const ok = mini.status === 'gewonnen', v = z.szene; mini = null; hudEl.hidden = true;
    akt.bootFlucht.ende(); bootRigs().forEach((r) => { r.visible = false; });
    klang(ok ? 'erfolg' : 'fehler');
    const e = S.minispielEnde(z, 'boot', ok);
    if (z.szene !== v) {
        const neu = z.szene; z.szene = v;
        blenden(async () => { z.szene = neu; await szeneZeigen(); ergebnis(e, v); });
    } else {
        aufstellen('steg'); ergebnis(e); danach = { minispiel: 'boot' };
    }
}

// ---------- Minispiel: Wahlkugel im Tempelgang ----------
function kugelStarten() {
    if (!akt || !akt.kugelFlucht) { return; }
    mini = S.kugelNeu(z.flags.lastenrad); tippen = false;
    akt.kugelFlucht.start([figuren.joe, figuren.chani], !!z.flags.lastenrad);
    hudEl.hidden = false;
}
function kugelSchritt(dt) {
    let rest = Math.min(dt, ZEITRAFFER ? 1 : .25);
    while (rest > 1e-6 && mini.status === 'laeuft') {
        const d = Math.min(.05, rest); rest -= d;
        S.kugelSchritt(mini, d, ZEITRAFFER ? true : tippen); tippen = false;
    }
    const abstand = Math.max(0, mini.pos - mini.kugel);
    hudEl.textContent = `${mini.lastenrad ? 'Lastenrad' : 'Rennen'}!  Ziel ${Math.round(Math.min(1, mini.pos) * 100)} %  ·  Vorsprung ${'▮'.repeat(Math.min(10, Math.round(abstand * 40)))}  ·  Tippen / Leertaste / →`;
    if (mini.status === 'laeuft') { return; }
    const ok = mini.status === 'gewonnen'; mini = null; hudEl.hidden = true;
    akt.kugelFlucht.ende();
    klang(ok ? 'erfolg' : 'fehler');
    const e = S.minispielEnde(z, 'kugel', ok);
    if (ok) { blenden(async () => { await betreten('tempel_finale', 'start'); ergebnis(e); }); }
    else { aufstellen('gang'); ergebnis(e); danach = { minispiel: 'kugel' }; }
}

// ---------- Plattformen im Tempel: Wer auf seiner Plattform steht, bleibt dort ----------
function plattenAbgleich() {
    if (!akt || !akt.plattformen) { return; }
    for (const n of ['joe', 'chani']) {
        const f = figuren[n], seite = z.platten.links === n ? 'links' : z.platten.rechts === n ? 'rechts' : null;
        if (z.flags.gitterOffen && f.userData.aufPlatte) {
            // Gitter offen: beide springen herunter und gehen hindurch
            f.userData.aufPlatte = null; f.position.set(n === 'joe' ? -.6 : .6, 0, -8.2); held[n].weg = [];
            if (n === z.figur) { spur = [[0, -7.2], xz(f)]; }
            continue;
        }
        if (seite && !held[n].weg.length && !z.flags.gitterOffen) {
            const pl = akt.plattformen[seite];
            f.userData.aufPlatte = seite; f.position.set(pl.x, pl.obj.userData.oben - .12, pl.z);
        } else if (!seite) { f.userData.aufPlatte = null; }
    }
}

// ---------- Menü ----------
function pauseUmschalten() { pausiert = !pausiert; if (pausiert) { menueZeigen(); } else { menueEl.hidden = true; } }
function menueZeigen() {
    menueEl.replaceChildren();
    const h = document.createElement('h2'); h.textContent = 'Pause'; menueEl.appendChild(h);
    const knopf = (t, fn) => { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', fn); menueEl.appendChild(k); };
    const hinweis = (t) => { const p = document.createElement('p'); p.className = 'hinweis'; p.textContent = t; menueEl.appendChild(p); };
    knopf('Weiterspielen', () => pauseUmschalten());
    for (const p of ['1', '2', '3']) { const info = plaetze.info(p); knopf(`Speichern auf Platz ${p}${info ? ` (belegt: ${info.punkte} Punkte)` : ''}`, () => { hinweis(plaetze.schreiben(p, z) ? `Gespeichert auf Platz ${p}.` : 'Speichern nicht möglich (Browser-Speicher gesperrt).'); }); }
    for (const p of ['auto', '1', '2', '3']) { const info = plaetze.info(p); if (info) { knopf(`Laden: ${p === 'auto' ? 'Autospeicher' : 'Platz ' + p} (${info.punkte} Punkte)`, () => { const s = plaetze.lesen(p); if (s) { starten(s); } }); } }
    if (window.Musik) {
        knopf(window.Musik.an ? 'Musik aus' : 'Musik an', () => { window.Musik.setzeAn(!window.Musik.an); menueZeigen(); });
        const regler = document.createElement('input'); regler.type = 'range'; regler.min = '0'; regler.max = '1'; regler.step = '0.05'; regler.value = String(window.Musik.lautstaerke); regler.setAttribute('aria-label', 'Lautstärke der Musik');
        regler.addEventListener('input', () => window.Musik.setzeLautstaerke(regler.value)); menueEl.appendChild(regler);
    }
    knopf(ton ? 'Ton aus' : 'Ton an', () => { ton = !ton; try { window.localStorage.setItem('fp-spiel-ton', ton ? 'an' : 'aus'); } catch (_) { /* egal */ } menueZeigen(); });
    knopf('Figuren vorstellen', () => { pauseUmschalten(); ergebnis({ text: S.VORSTELLUNG }); });
    knopf('Neues Spiel', () => starten(S.neuerZustand(), true));
    knopf('Klassische Pixel-Fassung', () => { try { window.localStorage.setItem('fp-spiel-auto', S.speichern(z)); } catch (_) { /* egal */ } window.location.href = '../spiel/'; });
    const zurueck = document.createElement('a'); zurueck.href = '../'; zurueck.textContent = 'Zurück zu FraktionPerfekt'; menueEl.appendChild(zurueck);
    menueEl.hidden = false;
}

async function starten(zustand, neu, ort) {
    z = zustand; pausiert = false; reise = null; gehalten = null;
    figuren.joe.userData.traegt = false; figuren.joe.userData.aufPlatte = figuren.chani.userData.aufPlatte = null;
    if (mini && akt && akt.bootFlucht) { akt.bootFlucht.ende(); } if (mini && akt && akt.kugelFlucht) { akt.kugelFlucht.ende(); } mini = null; hudEl.hidden = true; endeEl.hidden = true;
    menueEl.hidden = true; titelEl.hidden = true; dialogEl.hidden = true; kontextEl.hidden = true;
    warteschlange = []; danach = null; verb = 'gehe'; objekt = null;
    letzterOrt = S.ORTE[z.szene] ? z.szene : 'buero';
    $('leiste').hidden = false; groesse();
    leisteAktualisieren();
    if (ort && ort.bereich) { await betreten(ort.bereich, ort.spawn); } else { await szeneZeigen(neu || (z.szene === 'buero' && !z.flags.briefGelesen) ? 'start' : 'karte'); }
    if (ort) { ergebnis({ text: [`Direkteinstieg: ${ort.titel}. Im Gepäck: ${z.inventar.map((g) => S.GEGENSTAENDE[g].name).join(', ')}.`] }); }
    else if (neu) { ergebnis({ text: [...S.VORSTELLUNG, ...S.EINLEITUNG] }); }
    else { ergebnis({ text: [z.szene === 'karte' ? 'Weiter geht’s – auf der Karte.' : `Weiter geht’s: ${akt ? akt.titel : S.SZENEN[z.szene].titel}.`] }); }
}

// ---------- Schleife ----------
const uhr = new THREE.Clock();
function schleife() {
    const dt = Math.min(ZEITRAFFER ? .6 : .05, uhr.getDelta());
    if (z && !pausiert) {
        zeit += dt;
        if (!z.ende) { z.sekunden += dt; }
        if (reise) { reise.t += dt * .9; if (reise.t >= 1.15) { const ziel = reise.nach; reise = null; reiseAnkunft(ziel); } }
        if (mini && textbox.hidden && dialogEl.hidden) { if (mini.art === 'kugel') { kugelSchritt(dt); } else { bootSchritt(dt); } }
        else if (akt && !mini) { laufen(dt); plattenAbgleich(); }
    }
    if (z && akt) {
        figuren.joe.userData.hut.visible = hutAuf();
        if (figuren.joe.userData.jackeAn !== hutAuf()) { figuren.joe.userData.jackeAn = hutAuf(); figuren.joe.userData.jacke(hutAuf()); }
        const helme = !!(akt.helme && akt.helme(z));
        FIG.helm(figuren.joe, helme); FIG.helm(figuren.chani, helme);
        if (helme) { figuren.joe.userData.hut.visible = false; }
        for (const n of ['joe', 'chani']) {
            const f = figuren[n], ud = f.userData;
            if (mini) { FIG.pose(f, zeit, mini.art === 'kugel' ? (mini.lastenrad ? (n === 'joe' ? 'radeln' : 'sitzen') : 'rennen') : n === 'joe' ? 'rudern' : 'sitzen'); continue; }
            const redet = sprecher === n && !textbox.hidden;
            const art = ud.geht ? 'gehen' : redet ? (sprecherSchreck ? 'erschrocken' : 'reden') : (benutzenBis > zeit && n === z.figur ? 'benutzen' : 'stehen');
            FIG.pose(f, zeit, art);
        }
        for (const h of akt.hotspots) { if (h.sichtbarObj) { h.obj.visible = h.sichtbarObj(z); } }
        npcsAktualisieren();
        if (akt.tick) { akt.tick(dt, zeit); }
        if (mini && mini.art === 'kugel') { akt.kugelFlucht.bild(mini, zeit); }
        else if (mini) { akt.bootFlucht.bild(mini, zeit); bootRigs().forEach((r, i) => FIG.pose(r, zeit, i === 0 ? 'zielen' : 'sitzen')); npcs().forEach((m) => { m.rig.visible = false; }); }
        const k = mini ? (mini.art === 'kugel' ? akt.kugelFlucht : akt.bootFlucht).kamera(mini) : akt.kamera(figuren[z.figur].position), a = 1 - Math.pow(.04, dt);
        kamera.position.lerp(k.p, a); kamZiel.lerp(k.t, a); kamera.lookAt(kamZiel);
        renderer.render(akt.scene, kamera);
        satzEl.textContent = satz();
        endeEl.hidden = !(z.ende && textbox.hidden);
        if (z.ende && !endeEl.hidden) { $('endepunkte').textContent = `${z.punkte} Punkte · ${Math.round(z.sekunden / 60)} Minuten`; }
        if (z.ende && akt.id === 'tempel_finale' && !figuren.joe.userData.traegt) {
            // Joe und Chani tragen die Urne gemeinsam
            figuren.joe.userData.traegt = true; held.joe.weg = []; held.chani.weg = [];
            figuren.joe.position.set(1.1, 0, -1.9); figuren.joe.userData.blickZiel = Math.PI / 2;
            figuren.chani.position.set(2.5, 0, -1.9); figuren.chani.userData.blickZiel = -Math.PI / 2;
        }
    }
    if (z && !karteEl.hidden) { zeichneKarte(); if (!karteHover) { satzEl.textContent = 'Reise nach …'; } }
    requestAnimationFrame(schleife);
}

// ---------- Größe ----------
function groesse() {
    const leiste = $('leiste').hidden ? 0 : $('leiste').offsetHeight;
    const b = window.innerWidth, h = Math.max(200, window.innerHeight - leiste);
    buehne.style.width = `${b}px`; buehne.style.height = `${h}px`;
    renderer.setSize(b, h, false);
    const f = Math.min(b / 256, h / 144);
    Object.assign(karteEl.style, { width: `${256 * f}px`, height: `${144 * f}px`, left: `${(b - 256 * f) / 2}px`, top: `${(h - 144 * f) / 2}px` });
    kamera.aspect = b / h; kamera.fov = b / h < 1.2 ? 62 : 48; kamera.updateProjectionMatrix();
}
window.addEventListener('resize', groesse);
new ResizeObserver(groesse).observe($('leiste'));
groesse();

// ---------- Titel ----------
$('neu').addEventListener('click', () => { if (geladen) { klang('klick'); starten(S.neuerZustand(), true); } });
const M = window.Musik;
let melodieGespielt = false;
const musikKnopf = () => { $('musik').textContent = M && M.an ? '♪ Musik an' : '♪ Musik aus'; };
if (M && !STUMM) {
    $('lautstaerke').value = String(M.lautstaerke); musikKnopf();
    $('musik').addEventListener('click', () => { M.setzeAn(!M.an); musikKnopf(); if (M.an) { melodieGespielt = true; M.spielen(); } });
    $('lautstaerke').addEventListener('input', (e) => M.setzeLautstaerke(e.target.value));
    const ersteEingabe = () => { if (!melodieGespielt && !titelEl.hidden) { melodieGespielt = true; M.spielen(); } };
    document.addEventListener('pointerdown', ersteEingabe, { once: true });
    document.addEventListener('keydown', ersteEingabe, { once: true });
} else { $('musik').hidden = true; $('lautstaerke').hidden = true; }
const weiterKnopf = $('weiter');
if (plaetze.info('auto')) { weiterKnopf.hidden = false; weiterKnopf.addEventListener('click', () => { const s = plaetze.lesen('auto'); if (s && geladen) { starten(s); } }); }
$('pause').addEventListener('click', () => { if (z && titelEl.hidden) { pauseUmschalten(); } });
$('nochmal').addEventListener('click', () => { if (geladen) { starten(S.neuerZustand(), true); } });
$('neu').disabled = true; weiterKnopf.disabled = true;

/**
 * Direkteinstieg per Adresse, z. B. /spiel3d/?start=rathaus: Die Komplettlösung wird bis zu diesem Ort nachgespielt,
 * damit alle nötigen Gegenstände und Merkmale da sind (zum Ausprobieren und Testen; Spielstände bleiben unberührt,
 * bis automatisch gespeichert wird).
 */
const SPRUENGE = {
    buero: { schritte: 0, bereich: 'buero_innen', spawn: 'start', titel: 'Zukunftsschmiede (Anfang)' },
    marktstrasse: { schritte: 5, bereich: 'schmiede_aussen', spawn: 'tuer', titel: 'Vor der Zukunftsschmiede, alles aus dem Büro dabei' },
    karte: { schritte: 6, titel: 'Reisekarte nach Akt 1' },
    rathaus: { schritte: 7, bereich: 'rathaus_aussen', spawn: 'karte', titel: 'Vor dem Rathaus, Chris an der Tür' },
    rathausflur: { schritte: 8, bereich: 'rathaus_flur', spawn: 'eingang', titel: 'Rathausflur, Rathaus offen' },
    archiv: { schritte: 8, bereich: 'archiv_innen', spawn: 'tuer', titel: 'Rathausarchiv, Pinsel und Ledeiw an der Tür' },
    ems: { schritte: 15, bereich: 'ems_deich', spawn: 'karte', titel: 'Emsdeich am Kanuverleih' },
    boot: { schritte: 18, bereich: 'ems_deich', spawn: 'steg', titel: 'Am Steg, Paddel und Hinweis da – bereit zur Bootsflucht' },
    radbruecke: { schritte: 20, bereich: 'radbruecke', spawn: 'boot', titel: 'Radbrücke beim Hallenbad, das Siegel steckt noch in der dritten Laterne' },
    baustelle: { schritte: 21, bereich: 'baustelle', spawn: 'karte', titel: 'Baustelle am Hallenbad' },
    schule: { schritte: 29, bereich: 'schulhof', spawn: 'karte', titel: 'Hof der Martin-Luther-Grundschule' },
    schulflur: { schritte: 32, bereich: 'schulflur', spawn: 'von_schule', titel: 'Schulflur, Fledermäuse ausgeflogen, Lastenrad in Chanis Tasche' },
    krypta: { schritte: 33, bereich: 'krypta', spawn: 'von_schulflur', titel: 'Krypta unter der Schule' },
    tempel: { schritte: 39, bereich: 'tempel_halle', spawn: 'von_krypta', titel: 'Tempel des Grundgesetzes, vor dem Fallgitter' },
    gang: { schritte: 48, bereich: 'tempel_halle', spawn: 'gang', titel: 'Vor dem Gang mit der Wahlkugel, Brücke steht' },
    finale: { schritte: 50, bereich: 'tempel_finale', spawn: 'start', titel: 'Kammer des Grals' }
};
function sprungZustand(name) {
    const sp = SPRUENGE[name];
    if (!sp) { return null; }
    const zz = S.neuerZustand();
    for (const schritt of S.KOMPLETTLOESUNG.slice(0, sp.schritte)) { S.schritt(zz, schritt); }
    if (['baustelle', 'ems_deich', 'radbruecke'].includes(sp.bereich)) { zz.figur = 'joe'; }
    return { zz, sp };
}
const sprungName = (new URLSearchParams(window.location.search).get('start') || '').toLowerCase();

G.laden('../spiel/grafik/').then(() => {
    geladen = true; $('neu').disabled = false; weiterKnopf.disabled = false;
    const sprung = sprungZustand(sprungName);
    if (sprung) { starten(sprung.zz, false, sprung.sp.bereich ? sprung.sp : { titel: sprung.sp.titel }); }
    $('titelbilder').append(G.ausschnitt(G.portraet('joe_hut', 'lacht'), 64, 64), G.ausschnitt(G.portraet('chani', 'lacht'), 64, 64));
}).catch((e) => { $('titelbilder').textContent = e.message; });

// Für Browsertests: Spielstand lesen und klickbare Ziele mit Bildschirmposition (nur lesen)
window.GralSpielStand = () => ({ z, mini: mini ? { leben: mini.leben, status: mini.status } : null, bereich: akt ? akt.id : null, pos: xz(figuren[z ? z.figur : 'joe']), weg: z ? held[z.figur].weg : [], textOffen: !textbox.hidden, dialogOffen: !dialogEl.hidden, karte: !karteEl.hidden,
    beschaeftigt: !!(blende || reise || (mini && mini.status === 'laeuft') || held.joe.weg.length || held.chani.weg.length || held.joe.auftrag || held.chani.auftrag) || !ladenEl.hidden });
window.GralSpiel3dZiele = () => ziele().map((zi) => ({ name: zi.name, id: zi.art === 'logik' ? zi.h.id : null, art: zi.art, ...bildschirm(zi.obj) }));
window.GralSpiel3dTreffer = (x, y) => { const t = treffer({ clientX: x, clientY: y }); if (!t) { const r = leinwand.getBoundingClientRect(); ndc.set((x - r.left) / r.width * 2 - 1, -((y - r.top) / r.height) * 2 + 1); strahl.setFromCamera(ndc, kamera); const h = strahl.intersectObject(akt.scene, true).filter((q) => q.object.isMesh)[0]; return h ? 'blockiert: ' + (h.object.name || h.object.parent.name || h.object.geometry.type) : null; } return t.ziel ? t.ziel.name : 'boden'; };
window.GralSpiel3dOrte = () => Object.keys(S.ORTE).filter((id) => z && z.orte.includes(id)).map((id) => { const r = karteEl.getBoundingClientRect(); return { id, x: r.left + S.ORTE[id].x / 100 * r.width, y: r.top + S.ORTE[id].y / 100 * r.height }; });
requestAnimationFrame(schleife);
