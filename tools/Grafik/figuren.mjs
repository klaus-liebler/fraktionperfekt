// Figuren-Sheets 192×64 (6 × 32×64, transparent) im Aufbau von Klaus' Joe- und Chani-Sheets: Bild 1 frontal stehend,
// Bilder 2–6 Laufzyklus nach rechts. Die neuen Figuren entstehen aus diesen Vorlagen (gleiche Proportionen und Bewegung),
// mit eigener Kleidung, Frisur und Merkmalen. Alle Personen sind erfunden.
import { Bild, F } from './pixel.mjs';

// Farben der Vorlagen
const JOE = { haut: [244, 192, 148], weiss: [252, 252, 252], haar: [196, 196, 196], brau: [140, 140, 140], brille: [64, 64, 64], glas: [200, 232, 252], schatten: [204, 140, 100], bart: [128, 104, 84], lippe: [160, 60, 50], jacke: [40, 56, 112], jackeD: [20, 32, 72], hemd: [200, 208, 220], anstecker: [248, 184, 0] };
const CHANI = { haar: [176, 124, 80], haarH: [216, 168, 116], hautS: [220, 168, 140], haut: [248, 212, 180], haarD: [128, 84, 52], mantelD: [40, 52, 68], mantel: [64, 80, 100], knopf: [28, 36, 48] };

const gleich = (p, c) => p[3] > 0 && p[0] === c[0] && p[1] === c[1] && p[2] === c[2];

function kopie(vorlage) { const b = new Bild(vorlage.b, vorlage.h); b.d.set(vorlage.d); return b; }

/** Jedes der 6 Bilder einzeln bearbeiten: fn(bild, x0, istFrontal) */
function jeBild(b, fn) { for (let i = 0; i < 6; i++) { fn(b, i * 32, i === 0); } }

function ersetze(b, karte, bereich) { for (const [von, nach] of karte) { b.farbeErsetzen(von, nach, bereich); } }

function inZeilen(b, x0, y0, y1, farben, nach, xBedingung) {
    for (let y = y0; y <= y1; y++) { for (let x = x0; x < x0 + 32; x++) { const p = b.lies(x, y); if (farben.some((c) => gleich(p, c)) && (!xBedingung || xBedingung(x - x0))) { if (nach) { b.setze(x, y, nach); } else { b.loesche(x, y); } } } }
}

function mittelSpalte(b, x0, y, farbe) {
    let min = Infinity, max = -1;
    for (let x = x0; x < x0 + 32; x++) { if (gleich(b.lies(x, y), farbe)) { min = Math.min(min, x); max = Math.max(max, x); } }
    return max < 0 ? null : Math.round((min + max) / 2);
}

function linkesteSpalte(b, x0, y) { for (let x = x0; x < x0 + 32; x++) { if (b.lies(x, y)[3] > 0) { return x; } } return null; }

/** Krawatte: frontal mittig im Hemd, seitlich am vorderen Hemdrand (rechts). */
function krawatte(b, x0, frontal, farbe) {
    for (let y = 23; y <= 42; y++) {
        let min = Infinity, max = -1;
        for (let x = x0 + 8; x < x0 + 26; x++) { if (gleich(b.lies(x, y), JOE.weiss)) { min = Math.min(min, x); max = Math.max(max, x); } }
        if (max < 0) { continue; }
        if (frontal) { const m = Math.round((min + max) / 2); b.setze(m, y, farbe); b.setze(m + 1, y, farbe); } else { b.setze(max, y, farbe); }
    }
}

// ---------- aus Joe ----------
function christian(joe) {
    const b = kopie(joe);
    ersetze(b, [[JOE.jacke, F.rot], [JOE.jackeD, F.dunkelrot], [JOE.anstecker, F.rot]]);
    jeBild(b, (b, x0, frontal) => {
        inZeilen(b, x0, 15, 20, [JOE.haut, JOE.schatten, JOE.bart], F.hellgrau);          // grauer Vollbart
        if (frontal) { for (let y = 27; y < 42; y += 4) { const m = mittelSpalte(b, x0, y, JOE.weiss); if (m !== null) { b.setze(m - 4, y, F.weiss); } } }
    });
    return b;
}

function pinsel(joe) {
    const b = kopie(joe);
    ersetze(b, [[JOE.jacke, F.grau], [JOE.jackeD, F.dunkelgrau], [JOE.anstecker, F.grau], [JOE.bart, JOE.haut], [JOE.brille, JOE.haut], [JOE.glas, F.weiss], [JOE.brau, F.ocker], [JOE.haar, F.ocker]]);
    jeBild(b, (b, x0, frontal) => {
        inZeilen(b, x0, 0, frontal ? 5 : 4, [JOE.haut], F.hellgelb);
        if (!frontal) { inZeilen(b, x0, 5, 9, [JOE.haut], F.hellgelb, (x) => x <= 17); }
        inZeilen(b, x0, 0, 3, [F.hellgelb], F.gelb, (x) => x % 3 === 0);                     // Seitenscheitel
        krawatte(b, x0, frontal, F.dunkelblau);
    });
    return b;
}

function ledeiw(joe) {
    const b = kopie(joe);
    ersetze(b, [[JOE.jacke, F.dunkelblau], [JOE.jackeD, [0, 24, 88]], [JOE.anstecker, F.dunkelblau], [JOE.bart, JOE.haut], [JOE.haar, F.dunkelgrau]]);
    jeBild(b, (b, x0, frontal) => {
        inZeilen(b, x0, 0, frontal ? 6 : 5, [JOE.haut], F.dunkelgrau);
        if (!frontal) { inZeilen(b, x0, 5, 10, [JOE.haut], F.dunkelgrau, (x) => x <= 16); }
        inZeilen(b, x0, 0, 2, [F.dunkelgrau], F.grau, (x) => x % 4 === 1);                    // Gel-Glanz
        krawatte(b, x0, frontal, F.rot);
    });
    return b;
}

// ---------- aus Chani ----------
function tasche(b, x0, farbe, riemen) {
    const links = linkesteSpalte(b, x0, 36);
    if (links === null) { return; }
    b.rechteck(links - 3, 34, 5, 9, farbe); b.rechteck(links - 3, 34, 5, 1, F.weiss);
    for (let y = 24; y < 34; y++) { b.setze(links + Math.round((y - 24) * 0.3), y, riemen); }
}

function joanna(chani) {
    const b = kopie(chani);
    ersetze(b, [[CHANI.mantel, F.lila], [CHANI.mantelD, F.dunkellila], [CHANI.knopf, F.dunkellila], [CHANI.haar, F.fastschwarz], [CHANI.haarH, F.dunkelgrau], [CHANI.haarD, F.schwarz]]);
    jeBild(b, (b, x0) => {
        inZeilen(b, x0, 16, 30, [F.fastschwarz, F.dunkelgrau, F.schwarz], null);             // kurze Haare
        tasche(b, x0, F.creme2, F.beige);
    });
    return b;
}

function lore(chani) {
    const b = kopie(chani);
    ersetze(b, [[CHANI.mantel, F.gruen], [CHANI.mantelD, F.dunkelgruen], [CHANI.knopf, F.dunkelgruen], [CHANI.haar, F.hellgelb], [CHANI.haarH, F.pfirsich], [CHANI.haarD, F.ocker]]);
    jeBild(b, (b, x0) => {
        inZeilen(b, x0, 0, 6, [F.hellgelb, F.pfirsich, F.ocker], F.hellgruen);               // Fahrradhelm
        inZeilen(b, x0, 7, 7, [F.hellgelb, F.pfirsich, F.ocker], F.dunkelgruen);
        tasche(b, x0, F.weiss, F.dunkelgruen);                                                // Gutachten unter dem Arm
    });
    return b;
}

function kemper(chani) {
    const b = kopie(chani);
    ersetze(b, [[CHANI.mantel, F.orange], [CHANI.mantelD, F.dunkelgrau], [CHANI.knopf, F.dunkelgrau], [CHANI.haar, F.braun], [CHANI.haarH, F.lehm], [CHANI.haarD, F.holz]]);
    jeBild(b, (b, x0) => {
        inZeilen(b, x0, 17, 30, [F.braun, F.lehm, F.holz], null);
        inZeilen(b, x0, 0, 6, [F.braun, F.lehm, F.holz], F.gelb);                              // Bauhelm
        for (let x = x0 + 1; x < x0 + 31; x++) { if (gleich(b.lies(x, 6), F.gelb)) { for (const dx of [-2, -1, 0, 1, 2]) { if (x + dx >= x0 && x + dx < x0 + 32) { b.setze(x + dx, 7, F.ocker); } } } }
        inZeilen(b, x0, 33, 33, [F.orange], F.hellgrau); inZeilen(b, x0, 38, 38, [F.orange], F.hellgrau);
    });
    return b;
}

export function figurenSheets(joe, chani) {
    return [
        ['christian_spritesheet_32x64.png', christian(joe)],
        ['pinsel_spritesheet_32x64.png', pinsel(joe)],
        ['ledeiw_spritesheet_32x64.png', ledeiw(joe)],
        ['joanna_spritesheet_32x64.png', joanna(chani)],
        ['lore_spritesheet_32x64.png', lore(chani)],
        ['kemper_spritesheet_32x64.png', kemper(chani)]
    ];
}

// ---------- Zusatzposen, Porträts, Joe mit Hut ----------
const LIPPEN = [[160, 60, 50], [204, 112, 112]];
const AUGEN = [[0, 0, 0], [80, 120, 168]];

function rahmen(sheet, i) { const b = new Bild(32, 64); b.einsetzen(sheet, 0, 0, { ausschnitt: [i * 32, 0, 32, 64] }); return b; }

function haeufigste(b, y0, y1) {
    const z = new Map();
    for (let y = y0; y <= y1; y++) { for (let x = 0; x < b.b; x++) { const p = b.lies(x, y); if (p[3]) { const k = p.slice(0, 3).join(','); z.set(k, (z.get(k) || 0) + 1); } } }
    const [k] = [...z.entries()].sort((a, c) => c[1] - a[1])[0];
    return k.split(',').map(Number);
}

function mundAuf(b, zaehne) {
    for (let y = 8; y < 24; y++) {
        const xs = []; for (let x = 0; x < 32; x++) { if (LIPPEN.some((c) => gleich(b.lies(x, y), c))) { xs.push(x); } }
        if (xs.length) { for (const x of xs) { b.setze(x, y, [72, 16, 16]); b.setze(x, y + 1, [72, 16, 16]); } if (zaehne) { for (const x of xs.slice(1, -1)) { b.setze(x, y, F.weiss); } } return; }
    }
}

function augenZu(b, haut) {
    const dunkel = [Math.round(haut[0] * 0.75), Math.round(haut[1] * 0.7), Math.round(haut[2] * 0.7)];
    for (let y = 7; y < 15; y++) { for (let x = 0; x < 32; x++) { const p = b.lies(x, y); if (AUGEN.some((c) => gleich(p, c)) || (gleich(p, [252, 252, 252]) && y >= 9 && y <= 12)) { b.setze(x, y, dunkel); } } }
}

function arm(b, stufe, ding) {
    const y = 27; let vorn = -1; for (let x = 31; x >= 0; x--) { if (b.lies(x, y)[3]) { vorn = x; break; } }
    if (vorn < 0) { return; }
    const jacke = b.lies(vorn - 1, 31).slice(0, 3), haut = haeufigste(b, 9, 14), lang = stufe === 1 ? 4 : 8;
    b.rechteck(vorn - 1, y - 1, lang + 1, 3, jacke); b.rechteck(vorn + lang, y - 1, 2, 3, haut);
    if (stufe === 2 && ding === 'kartusche') { b.rechteck(vorn + lang + 2, y - 1, 5, 2, F.weiss); b.setze(vorn + lang + 7, y - 1, F.orange); }
    if (stufe === 2 && ding === 'handy') { b.rechteck(vorn + lang, y - 6, 2, 5, F.fastschwarz); b.setze(vorn + lang, y - 5, F.himmel); }
}

function armeHoch(b) {
    const y = 32; let l = -1, r = -1; for (let x = 0; x < 32; x++) { if (b.lies(x, y)[3]) { if (l < 0) { l = x; } r = x; } }
    const jacke = b.lies(l + 1, 30).slice(0, 3), haut = haeufigste(b, 9, 14);
    for (let yy = 23; yy <= 44; yy++) { for (let x = l; x < l + 3; x++) { b.loesche(x, yy); } for (let x = r - 2; x <= r; x++) { b.loesche(x, yy); } }
    b.rechteck(l - 1, 12, 3, 13, jacke); b.rechteck(l - 1, 9, 3, 3, haut); b.rechteck(r - 1, 12, 3, 13, jacke); b.rechteck(r - 1, 9, 3, 3, haut);
}

/** Posen-Sheet 192×64: Reden (zu/offen), Benutzen (2 Stufen), Erschrocken, Blinzeln. */
export function posen(sheet, ding) {
    const vorne = () => rahmen(sheet, 0), seite = () => rahmen(sheet, 3);
    const haut = haeufigste(vorne(), 9, 14);
    const bilder = [vorne(), (() => { const b = vorne(); mundAuf(b); return b; })(),
        (() => { const b = seite(); arm(b, 1, ding); return b; })(), (() => { const b = seite(); arm(b, 2, ding); return b; })(),
        (() => { const b = vorne(); armeHoch(b); mundAuf(b); return b; })(), (() => { const b = vorne(); augenZu(b, haut); return b; })()];
    const s = new Bild(192, 64); bilder.forEach((b, i) => s.einsetzen(b, i * 32, 0)); return s;
}

/** Porträt-Sheet 192×64: drei 64×64-Köpfe (neutral, lacht, erschrocken), aus dem Frontalbild doppelt vergrößert. */
export function portraets(sheet, hintergrund) {
    const s = new Bild(192, 64);
    const haut = haeufigste(rahmen(sheet, 0), 9, 14);
    const varianten = [(b) => b, (b) => { augenZu(b, haut); mundAuf(b, true); return b; }, (b) => { mundAuf(b); return b; }];
    varianten.forEach((fn, i) => {
        s.rechteck(i * 64, 0, 64, 64, hintergrund || F.sand); s.rechteck(i * 64, 56, 64, 8, F.sand2);
        s.einsetzen(fn(rahmen(sheet, 0)), i * 64, 2, { skala: 2, ausschnitt: [0, 0, 32, 31] });
    });
    return s;
}

/** Joe mit Filzhut und Umhängetasche mit Kartuschen – für alle Bilder eines Sheets. */
export function mitHut(sheet) {
    const b = kopie(sheet);
    for (let i = 0; i < 6; i++) {
        const x0 = i * 32; let oben = -1;
        for (let y = 0; y < 20 && oben < 0; y++) { for (let x = x0; x < x0 + 32; x++) { if (b.lies(x, y)[3]) { oben = y; break; } } }
        let l = Infinity, r = -1; for (let x = x0; x < x0 + 32; x++) { if (b.lies(x, oben + 4)[3]) { l = Math.min(l, x); r = Math.max(r, x); } }
        if (r < 0) { continue; }
        b.rechteck(l + 1, 0, r - l - 1, oben + 4, F.braun); b.rechteck(l + 1, oben + 3, r - l - 1, 1, F.holzdunkel); b.rechteck(l - 2, oben + 4, r - l + 5, 1, F.holz); b.rechteck(l + 3, 0, 2, 2, [160, 104, 56]);
        const links = linkesteSpalte(b, x0, 36);
        if (links !== null) { b.rechteck(links - 3, 36, 5, 7, F.braun); b.rechteck(links - 3, 36, 5, 1, F.holzhell); b.rechteck(links - 2, 34, 2, 2, F.weiss); b.setze(links - 2, 33, F.orange); for (let y = 24; y < 36; y++) { b.setze(links + Math.round((y - 24) * 0.4), y, F.holzdunkel); } }
    }
    return b;
}

/**
 * Professor Relbeil (fiktive Figur, nur am Telefon): Porträt 3 × 64×64 wie die übrigen – neutral, lacht, erschrocken.
 * Freundlicher älterer Professor: Halbglatze mit weißem Haarkranz, runde Brille, Schnurrbart, Fliege, Tweedjackett, Telefonhörer.
 */
export function relbeilPortraet() {
    const s = new Bild(192, 64);
    for (let i = 0; i < 3; i++) {
        const x = i * 64;
        s.rechteck(x, 0, 64, 64, F.sand); s.rechteck(x, 56, 64, 8, F.sand2);
        // Jackett und Hemd mit Fliege
        s.polygon([[x + 10, 64], [x + 14, 48], [x + 50, 48], [x + 54, 64]], F.braun);
        s.polygon([[x + 26, 48], [x + 38, 48], [x + 32, 58]], F.weiss);
        s.rechteck(x + 28, 48, 8, 3, F.rot); s.rechteck(x + 31, 48, 2, 3, F.dunkelrot);
        // Kopf
        s.ellipse(x + 32, 28, 13, 16, F.hautH); s.rechteck(x + 28, 42, 8, 6, F.hautH);
        s.ellipse(x + 19, 30, 2, 4, F.hautS); s.ellipse(x + 45, 30, 2, 4, F.hautS);
        // weißer Haarkranz, Halbglatze
        s.rechteck(x + 18, 20, 4, 10, F.weiss); s.rechteck(x + 42, 20, 4, 10, F.weiss); s.rechteck(x + 21, 15, 3, 5, F.hellgrau); s.rechteck(x + 40, 15, 3, 5, F.hellgrau);
        // Augenbrauen (erschrocken: hoch)
        const by = i === 2 ? 21 : 23;
        s.rechteck(x + 23, by, 6, 1, F.hellgrau); s.rechteck(x + 35, by, 6, 1, F.hellgrau);
        // runde Brille
        s.rahmen(x + 22, 25, 8, 7, F.fastschwarz); s.rahmen(x + 34, 25, 8, 7, F.fastschwarz); s.rechteck(x + 30, 27, 4, 1, F.fastschwarz);
        if (i === 1) { s.rechteck(x + 24, 28, 4, 1, F.fastschwarz); s.rechteck(x + 36, 28, 4, 1, F.fastschwarz); }   // lacht: Augen zu
        else { s.rechteck(x + 25, 27, 2, 3, F.dunkelblau); s.rechteck(x + 37, 27, 2, 3, F.dunkelblau); }
        // Nase, Schnurrbart, Mund
        s.rechteck(x + 31, 30, 2, 5, F.hautS);
        s.rechteck(x + 26, 36, 12, 2, F.hellgrau);
        if (i === 0) { s.rechteck(x + 28, 39, 8, 1, F.dunkelrot); }
        else if (i === 1) { s.rechteck(x + 27, 39, 10, 3, F.dunkelrot); s.rechteck(x + 28, 39, 8, 1, F.weiss); }
        else { s.ellipse(x + 32, 40, 2, 2, F.dunkelrot); }
        // Telefonhörer am Ohr (rechts)
        s.rechteck(x + 46, 24, 5, 14, F.fastschwarz); s.rechteck(x + 44, 22, 6, 4, F.fastschwarz); s.rechteck(x + 44, 36, 6, 4, F.fastschwarz);
        s.rechteck(x + 48, 40, 6, 8, F.hautH);
    }

    return s;
}
