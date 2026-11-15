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
    let min = 99, max = -1;
    for (let x = x0; x < x0 + 32; x++) { if (gleich(b.lies(x, y), farbe)) { min = Math.min(min, x); max = Math.max(max, x); } }
    return max < 0 ? null : Math.round((min + max) / 2);
}

function linkesteSpalte(b, x0, y) { for (let x = x0; x < x0 + 32; x++) { if (b.lies(x, y)[3] > 0) { return x; } } return null; }

/** Krawatte: frontal mittig im Hemd, seitlich am vorderen Hemdrand (rechts). */
function krawatte(b, x0, frontal, farbe) {
    for (let y = 23; y <= 42; y++) {
        let min = 99, max = -1;
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

function hohlbein(joe) {
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

function droehn(joe) {
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
        ['hohlbein_spritesheet_32x64.png', hohlbein(joe)],
        ['droehn_spritesheet_32x64.png', droehn(joe)],
        ['joanna_spritesheet_32x64.png', joanna(chani)],
        ['lore_spritesheet_32x64.png', lore(chani)],
        ['kemper_spritesheet_32x64.png', kemper(chani)]
    ];
}
