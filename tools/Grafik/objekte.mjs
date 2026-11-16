// Spielobjekte als einzelne Sprites (transparent) und das Inventar-Icon-Sheet 160×16.
import { Bild, F, text } from './pixel.mjs';

function ruderboot() {
    const b = new Bild(46, 14);
    b.polygon([[0, 2], [46, 2], [40, 14], [6, 14]], F.holz); b.rechteck(1, 2, 44, 2, F.holzhell); b.rechteck(7, 12, 32, 2, F.holzdunkel);
    b.rechteck(12, 5, 22, 6, F.weiss); text(b, 'VOLK', 15, 5, F.rot);
    return b;
}
function motorboot() {
    const b = new Bild(64, 24);
    b.polygon([[0, 10], [56, 10], [64, 4], [64, 14], [54, 24], [6, 24]], F.hellgrau); b.rechteck(2, 10, 54, 2, F.weiss); b.rechteck(6, 20, 48, 4, F.grau);
    b.rechteck(20, 2, 18, 8, F.grau); b.rechteck(22, 3, 14, 4, F.hellblau); b.rechteck(0, 12, 4, 10, F.dunkelgrau);
    return b;
}
function hindernisse() {
    const b = new Bild(96, 24);
    b.rechteck(2, 14, 20, 5, F.holz); b.rechteck(2, 14, 20, 1, F.holzhell); b.rechteck(6, 11, 3, 4, F.holz); b.rechteck(16, 19, 2, 2, F.holzdunkel);
    for (let i = 0; i < 3; i++) { const x = 26 + i * 7; b.rechteck(x, 16 - (i === 0 ? 2 : 0), 6, 4, i === 0 ? F.braun : F.ocker); b.rechteck(x + 4, 13 - (i === 0 ? 2 : 0), 3, 3, F.dunkelgruen); b.setze(x + 7, 14 - (i === 0 ? 2 : 0), F.orange); }
    b.rechteck(59, 10, 2, 13, F.holzdunkel); b.rechteck(50, 2, 20, 10, F.gelb); b.rechteck(51, 3, 18, 8, F.weiss); text(b, 'ALT', 54, 3, F.rot); b.rechteck(53, 9, 14, 1, F.dunkelgrau);
    b.rechteck(74, 6, 20, 14, F.weiss); b.rechteck(75, 7, 18, 4, F.grau); b.rechteck(76, 13, 14, 1, F.dunkelgrau); b.rechteck(76, 16, 10, 1, F.dunkelgrau); b.rechteck(74, 19, 20, 2, F.hellblau);
    return b;
}
function wahlkugel() {
    const b = new Bild(256, 64);
    for (let f = 0; f < 4; f++) {
        const cx = f * 64 + 32, cy = 32;
        b.ellipse(cx, cy, 30, 30, F.grau); b.ellipse(cx - 3, cy - 3, 26, 26, F.hellgrau); b.ellipse(cx - 12, cy - 12, 6, 5, F.weiss);
        const w = (f * Math.PI) / 8;
        for (let i = -10; i <= 10; i++) {
            for (const r of [w, w + Math.PI / 2]) { const x = Math.round(cx + Math.cos(r + Math.PI / 4) * i), y = Math.round(cy + Math.sin(r + Math.PI / 4) * i); b.rechteck(x - 1, y - 1, 3, 3, F.dunkelgrau); }
        }
    }
    return b;
}
function lastenrad() {
    const b = new Bild(48, 32);
    for (const x of [8, 40]) { b.ellipse(x, 25, 6, 6, F.fastschwarz); b.ellipse(x, 25, 4, 4, F.hellgrau); b.setze(x, 25, F.dunkelgrau); }
    b.rechteck(2, 10, 20, 12, F.gruen); b.rechteck(2, 10, 20, 2, F.hellgruen); b.rechteck(4, 14, 16, 1, F.dunkelgruen);
    b.linie(20, 22, 40, 25, F.dunkelgruen); b.linie(26, 22, 34, 10, F.dunkelgruen); b.rechteck(31, 8, 7, 2, F.fastschwarz); b.linie(40, 25, 40, 8, F.dunkelgruen); b.rechteck(38, 6, 6, 2, F.fastschwarz);
    return b;
}
function fledermaeuse() {
    const b = new Bild(32, 6);
    const fl = [[0, 0, 1, 1], [1, 1, 2, 2], [2, 2, 2, 1], [1, 0, 0, 0]];
    for (let f = 0; f < 4; f++) {
        const x = f * 8; b.rechteck(x + 3, 2, 2, 3, F.schwarz);
        const [a, c, d] = fl[f]; b.rechteck(x, 1 + a, 3, 1, F.schwarz); b.rechteck(x + 5, 1 + a, 3, 1, F.schwarz); b.setze(x, 2 + c - a, F.schwarz); b.setze(x + 7, 2 + c - a, F.schwarz); void d;
    }
    return b;
}
function leck() {
    const b = new Bild(78, 12);
    for (let f = 0; f < 3; f++) {
        const x = f * 26; b.rechteck(x, 2, 26, 5, F.grau); b.rechteck(x, 2, 26, 1, F.hellgrau); b.rechteck(x + 12, 1, 5, 7, F.dunkelgrau);
        for (let i = 0; i < 4; i++) { b.setze(x + 13 + ((i + f) % 3) - 1, 8 + i, F.hellblau); b.setze(x + 15 + ((i + f + 1) % 2), 8 + ((i + f) % 4), F.himmel); }
    }
    return b;
}
function uhrZeiger() {
    const b = new Bild(52, 23);
    for (let f = 0; f < 2; f++) {
        const cx = f * 26 + 13, cy = 11; b.ellipse(cx, cy, 10, 10, F.dunkelgrau); b.ellipse(cx, cy, 9, 9, F.weiss);
        if (f === 0) { b.linie(cx, cy, cx + 1, cy - 7, F.schwarz); b.linie(cx, cy, cx + 5, cy + 2, F.schwarz); } else { b.linie(cx, cy, cx + 6, cy + 4, F.schwarz); b.linie(cx, cy, cx - 2, cy + 5, F.schwarz); }
    }
    return b;
}
function symbolscheiben() {
    const b = new Bild(31, 20);
    b.rechteck(0, 0, 31, 20, F.steindunkel); b.rechteck(1, 1, 29, 18, F.stein);
    for (let i = 0; i < 3; i++) { b.ellipse(6 + i * 9, 10, 4, 4, F.ocker); b.ellipse(6 + i * 9, 10, 3, 3, F.gelb); }
    b.rechteck(4, 9, 5, 1, F.holzdunkel); b.rechteck(13, 8, 4, 4, F.holzdunkel); b.rechteck(22, 8, 4, 5, F.holzdunkel);
    return b;
}
function torSiegel() {
    const b = new Bild(46, 72);
    b.rechteck(0, 0, 46, 72, F.stein); b.rechteck(3, 4, 40, 68, F.fastschwarz);
    const farben = [F.blau, F.gruen, F.rot];
    for (let i = 0; i < 3; i++) { b.ellipse(9 + i * 14, 14, 5, 5, F.ocker); b.ellipse(9 + i * 14, 14, 4, 4, farben[i]); }
    return b;
}
function bruecke() { const b = new Bild(67, 10); b.rechteck(0, 0, 67, 10, F.lehm); b.rechteck(0, 0, 67, 2, F.sand); for (let x = 2; x < 67; x += 8) { b.rechteck(x, 0, 1, 10, F.sand2); } return b; }
/** Schwarze Klebemasse (Dichtstoff-Raupe) unter festgeklebten Schuhen: zwei Pfützen mit Glanz und ein paar Fäden. */
function klebemasse() {
    const b = new Bild(32, 6);
    b.ellipse(9, 3, 8, 2, F.fastschwarz); b.ellipse(23, 3, 8, 2, F.fastschwarz); b.rechteck(9, 3, 14, 2, F.fastschwarz);
    b.rechteck(5, 2, 3, 1, F.dunkelgrau); b.rechteck(20, 2, 2, 1, F.dunkelgrau); b.setze(13, 1, F.grau); b.setze(27, 2, F.grau);
    b.setze(1, 4, F.fastschwarz); b.setze(31, 3, F.fastschwarz); b.rechteck(15, 5, 3, 1, F.fastschwarz);
    return b;
}
function bodenplatteAktiv() { const b = new Bild(36, 9); b.rechteck(0, 0, 36, 9, F.steindunkel); b.rechteck(1, 1, 34, 7, F.gelb); b.rechteck(15, 3, 6, 3, F.hellgelb); return b; }

export function objekte() {
    return [
        ['ruderboot_46x14.png', ruderboot()], ['motorboot_64x24.png', motorboot()], ['hindernisse_ems_96x24.png', hindernisse()],
        ['wahlkugel_64x64_x4.png', wahlkugel()], ['lastenrad_48x32.png', lastenrad()], ['fledermaeuse_8x6_x4.png', fledermaeuse()],
        ['leck_26x12_x3.png', leck()], ['uhr_zeiger_26x23_x2.png', uhrZeiger()], ['symbolscheiben_31x20.png', symbolscheiben()],
        ['tor_siegel_46x72.png', torSiegel()], ['bruecke_67x10.png', bruecke()], ['bodenplatte_leuchtend_36x9.png', bodenplatteAktiv()],
        ['klebemasse_32x6.png', klebemasse()]
    ];
}

export function icons() {
    const b = new Bild(160, 16);
    const z = [
        (x) => { b.rechteck(x + 1, 4, 14, 9, F.weiss); b.linie(x + 1, 4, x + 8, 9, F.hellgrau); b.linie(x + 14, 4, x + 8, 9, F.hellgrau); b.rechteck(x + 7, 8, 3, 2, F.rot); },
        (x) => { b.rechteck(x + 1, 2, 14, 12, F.creme); b.linie(x + 4, 2, x + 6, 13, F.blau); b.linie(x + 7, 6, x + 14, 9, F.lehm); b.rechteck(x + 10, 6, 3, 3, F.rot); },
        (x) => { b.rechteck(x + 3, 1, 10, 14, F.weiss); for (let i = 0; i < 5; i++) { b.rechteck(x + 5, 3 + i * 2, 6, 1, F.grau); } },
        (x) => { b.rechteck(x + 3, 2, 10, 12, F.dunkelblau); b.rechteck(x + 3, 2, 2, 12, F.navy); b.rechteck(x + 7, 5, 4, 1, F.gelb); b.rechteck(x + 12, 3, 1, 10, F.weiss); },
        (x) => { b.rechteck(x + 2, 6, 9, 4, F.dunkelgrau); b.rechteck(x + 11, 5, 3, 6, F.hellgrau); b.rechteck(x + 14, 6, 1, 4, F.hellgelb); },
        (x) => { b.linie(x + 2, 14, x + 10, 6, F.holz); b.linie(x + 3, 14, x + 11, 6, F.holzhell); b.ellipse(x + 12, 4, 3, 2, F.holz); },
        (x) => { b.ellipse(x + 8, 8, 7, 7, F.ocker); b.ellipse(x + 8, 8, 6, 6, F.blau); b.rechteck(x + 5, 6, 7, 4, F.weiss); b.rechteck(x + 8, 6, 1, 4, F.grau); },
        (x) => { b.ellipse(x + 8, 8, 7, 7, F.ocker); b.ellipse(x + 8, 8, 6, 6, F.gruen); b.ellipse(x + 6, 8, 2, 2, F.gelb); b.rechteck(x + 8, 8, 4, 1, F.gelb); b.rechteck(x + 11, 9, 1, 2, F.gelb); },
        (x) => { b.ellipse(x + 8, 8, 7, 7, F.ocker); b.ellipse(x + 8, 8, 6, 6, F.rot); b.rechteck(x + 4, 6, 9, 1, F.gelb); b.rechteck(x + 8, 4, 1, 7, F.gelb); b.rechteck(x + 4, 7, 2, 2, F.gelb); b.rechteck(x + 11, 7, 2, 2, F.gelb); },
        (x) => { b.ellipse(x + 4, 8, 3, 3, F.ocker); b.setze(x + 4, 8, F.holzdunkel); b.rechteck(x + 7, 7, 8, 2, F.ocker); b.rechteck(x + 11, 9, 1, 2, F.ocker); b.rechteck(x + 13, 9, 1, 3, F.ocker); }
    ];
    z.forEach((f, i) => f(i * 16));
    return b;
}
