// Hintergründe 256×144 im Stil von Klaus' Vorlagen: flache Farben, eine Schattenstufe, keine Umrisse, kein Dithering,
// Muster nur als 1-Pixel-Streifen oder Fugen. Objekte stehen an den Klickflächen des Spiels (Koordinaten in 256×144).
import { Bild, F, text, textBreite } from './pixel.mjs';

const B = 256, H = 144;

// ---------- Bausteine ----------
function tapete(b, y0, y1, a, c) { for (let y = y0; y < y1; y++) { b.rechteck(0, y, B, 1, (y - y0) % 2 ? c : a); } }
function holzboden(b, y0, hell, dunkel, fuge) {
    b.rechteck(0, y0, B, H - y0, hell);
    for (let y = y0; y < H; y += 5) { b.rechteck(0, y, B, 1, fuge); }
    for (let y = y0, r = 0; y < H; y += 5, r++) { for (let x = (r % 2) * 17; x < B; x += 34) { b.rechteck(x, y + 1, 1, 4, fuge); } b.rechteck(0, y + 1, B, 1, dunkel); }
}
function sockelleiste(b, y, c, d) { b.rechteck(0, y - 3, B, 3, c); b.rechteck(0, y - 1, B, 1, d); }
function tuer(b, x, y, w, h, holz, holzD, griff, glas) {
    b.rechteck(x - 2, y - 2, w + 4, h + 2, F.beige);
    b.rechteck(x, y, w, h, holz);
    if (glas) { b.rechteck(x + 3, y + 3, w - 6, Math.round(h * 0.55), glas); }
    else { b.rechteck(x + 3, y + 4, w - 6, Math.round(h / 2) - 6, holzD); b.rechteck(x + 3, y + Math.round(h / 2) + 2, w - 6, Math.round(h / 2) - 6, holzD); }
    b.rechteck(x + w - 5, y + Math.round(h * 0.58), 3, 2, griff);
}
function regalBuecher(b, x, y, w, h, faecher, saat) {
    b.rechteck(x, y, w, h, F.holzdunkel); b.rechteck(x + 2, y + 2, w - 4, h - 4, F.holzschwarz);
    const farben = [F.rot, F.blau, F.gruen, F.gelb, F.lila, F.weiss, F.orange, F.petrol, F.dunkelblau];
    const fh = Math.floor((h - 4) / faecher);
    for (let f = 0; f < faecher; f++) {
        const fy = y + 2 + f * fh;
        let xx = x + 3, i = f * 3 + saat;
        while (xx < x + w - 5) {
            const bw = 2 + (i % 3), bh = fh - 3 - (i % 4);
            b.rechteck(xx, fy + fh - 1 - bh, bw, bh, farben[i % farben.length]);
            b.rechteck(xx, fy + fh - 1 - bh + 2, bw, 1, F.weiss);
            xx += bw + (i % 5 === 0 ? 2 : 0); i++;
        }
        b.rechteck(x + 2, fy + fh - 1, w - 4, 2, F.holz);
    }
    b.rechteck(x, y, 2, h, F.holz);
}
function fackel(b, x, y) { b.rechteck(x, y, 3, 9, F.holzdunkel); b.rechteck(x - 1, y - 1, 5, 2, F.dunkelgrau); b.rechteck(x - 1, y - 6, 5, 5, F.orange); b.rechteck(x, y - 8, 3, 4, F.gelb); b.rechteck(x + 1, y - 9, 1, 2, F.hellgelb); }
function sterne(b, n, saat, maxY) { let s = saat; for (let i = 0; i < n; i++) { s = (s * 1103515245 + 12345) & 0x7fffffff; const x = s % B; s = (s * 1103515245 + 12345) & 0x7fffffff; b.setze(x, s % maxY, i % 5 === 0 ? F.gelb : F.weiss); } }
function bodenplatte(b, x, y, w, h, aktiv) { b.rechteck(x, y, w, h, F.steindunkel); b.rechteck(x + 1, y + 1, w - 2, h - 2, aktiv ? F.gelb : F.stein); b.rechteck(x + Math.round(w / 2) - 3, y + 3, 6, 3, aktiv ? F.hellgelb : F.sand2); }

// ---------- Zukunftsschmiede (Parteibüro, Ladenlokal Marktstraße) ----------
export function zukunftsschmiede(mitUmschlag = true) {
    const b = new Bild(B, H);
    b.rechteck(0, 0, B, 6, F.hellgrau); b.rechteck(0, 6, B, 1, F.grau);
    tapete(b, 7, 104, F.sand, F.sand2);
    // Schild über dem Schaufenster
    b.rechteck(92, 8, 124, 11, F.holzdunkel); b.rechteck(93, 9, 122, 9, F.holz);
    const t = 'ZUKUNFTSSCHMIEDE'; text(b, t, 154 - Math.floor(textBreite(t) / 2), 11, F.gelb);
    // Schaufenster zur Marktstraße (Blick auf Giebelhäuser gegenüber)
    b.rechteck(90, 22, 42, 66, F.creme);
    b.rechteck(92, 24, 38, 62, F.hellblau);
    const giebel = [[92, F.rot], [104, F.gelb], [116, F.hellorange]];
    for (const [gx, c] of giebel) { b.polygon([[gx, 44], [gx + 6, 34], [gx + 12, 44]], c); b.rechteck(gx, 44, 12, 30, c); b.rechteck(gx + 3, 48, 3, 4, F.weiss); b.rechteck(gx + 7, 48, 3, 4, F.weiss); b.rechteck(gx + 3, 56, 3, 4, F.weiss); b.rechteck(gx + 7, 56, 3, 4, F.weiss); }
    b.rechteck(92, 74, 38, 12, F.hellgrau); for (let x = 92; x < 130; x += 4) { b.rechteck(x, 74 + ((x / 4) % 2) * 3, 3, 1, F.grau); b.rechteck(x + 2, 80, 3, 1, F.grau); }
    b.rechteck(125, 46, 2, 28, F.dunkelgrau); b.rechteck(122, 43, 8, 4, F.dunkelgrau); b.rechteck(123, 44, 6, 2, F.hellgelb);
    b.rechteck(110, 24, 2, 62, F.creme); b.rechteck(92, 52, 38, 2, F.creme);
    b.rechteck(88, 88, 46, 3, F.beige);
    // Ladentür mit Glas zur Straße (Klickfläche x 133–159, y 37–95)
    tuer(b, 133, 37, 26, 67, F.holzhell, F.holz, F.gelb, F.hellblau);
    b.rechteck(139, 40, 14, 6, F.weiss); text(b, 'AUF', 140, 41, F.rot);
    // Bücherregal (x 31–62, y 43–104)
    regalBuecher(b, 31, 40, 31, 64, 5, 1);
    // Briefkasten/Postfach an der Wand (x 72–92, y 60–74)
    b.rechteck(70, 58, 24, 18, F.holzdunkel); b.rechteck(71, 59, 22, 16, F.holz);
    for (let i = 0; i < 3; i++) { for (let j = 0; j < 2; j++) { b.rechteck(72 + i * 7, 60 + j * 7, 6, 6, F.holzschwarz); b.rechteck(73 + i * 7, 62 + j * 7, 4, 3, (i + j) % 2 ? F.weiss : F.creme); } }
    b.rechteck(76, 54, 12, 4, F.weiss); text(b, 'POST', 76, 53, F.dunkelblau);
    // Plakat ohne Logo
    b.rechteck(70, 20, 16, 24, F.weiss); b.rechteck(71, 21, 14, 10, F.himmel); b.rechteck(71, 27, 14, 4, F.gruen); b.rechteck(73, 34, 10, 1, F.dunkelgrau); b.rechteck(73, 37, 8, 1, F.dunkelgrau); b.rechteck(73, 40, 10, 1, F.dunkelgrau);
    // Schreibtisch mit Umschlag (Umschlag x 102–128, y 84–94)
    b.rechteck(86, 94, 46, 4, F.holzhell); b.rechteck(86, 98, 46, 2, F.holz); b.rechteck(88, 100, 4, 16, F.holz); b.rechteck(126, 100, 4, 16, F.holz);
    if (mitUmschlag) { b.rechteck(106, 88, 18, 6, F.weiss); b.linie(106, 88, 115, 92, F.hellgrau); b.linie(123, 88, 115, 92, F.hellgrau); b.rechteck(114, 91, 3, 2, F.rot); }
    b.rechteck(90, 86, 10, 8, F.dunkelgrau); b.rechteck(91, 87, 8, 5, F.dunkelblau); b.rechteck(93, 94, 4, 1, F.grau);
    // Rollcontainer mit Faxgerät (Fax x 179–210, y 66–83)
    b.rechteck(176, 84, 38, 22, F.hellgrau); b.rechteck(176, 84, 38, 2, F.weiss); b.rechteck(178, 88, 34, 7, F.grau); b.rechteck(178, 97, 34, 7, F.grau); b.rechteck(193, 91, 4, 1, F.dunkelgrau); b.rechteck(193, 100, 4, 1, F.dunkelgrau);
    b.rechteck(180, 72, 30, 12, F.beige); b.rechteck(180, 72, 30, 2, F.creme); b.rechteck(186, 64, 16, 8, F.weiss); b.rechteck(188, 66, 12, 1, F.grau); b.rechteck(188, 68, 9, 1, F.grau);
    b.rechteck(181, 75, 9, 4, F.dunkelgrau); b.rechteck(182, 76, 7, 2, F.gruen); for (let i = 0; i < 3; i++) { for (let j = 0; j < 2; j++) { b.rechteck(193 + i * 4, 75 + j * 3, 3, 2, F.dunkelgrau); } }
    b.rechteck(205, 74, 4, 8, F.dunkelgrau); b.rechteck(204, 73, 6, 2, F.fastschwarz);
    // Garderobe mit Lederjacke und Filzhut (x 220–240, y 43–104)
    b.rechteck(229, 46, 3, 58, F.holzdunkel); b.rechteck(222, 102, 17, 2, F.holzdunkel); b.rechteck(223, 47, 15, 2, F.holzdunkel);
    b.polygon([[222, 52], [239, 52], [241, 84], [220, 84]], F.braun); b.rechteck(230, 52, 2, 30, F.holz); b.rechteck(221, 70, 6, 14, F.holz); b.rechteck(235, 56, 5, 26, F.holz);
    b.rechteck(220, 43, 21, 3, F.holz); b.rechteck(224, 37, 13, 6, F.braun); b.rechteck(224, 41, 13, 2, F.holzdunkel);
    // Wanduhr, Kalender
    b.ellipse(176, 30, 7, 7, F.dunkelgrau); b.ellipse(176, 30, 6, 6, F.weiss); b.rechteck(176, 25, 1, 6, F.schwarz); b.rechteck(176, 30, 4, 1, F.schwarz);
    b.rechteck(196, 22, 18, 22, F.weiss); b.rechteck(196, 22, 18, 5, F.rot); text(b, '1995', 197, 23, F.weiss); for (let i = 0; i < 4; i++) { for (let j = 0; j < 3; j++) { b.rechteck(198 + i * 4, 30 + j * 4, 2, 2, F.grau); } }
    sockelleiste(b, 104, F.holz, F.holzdunkel);
    holzboden(b, 104, F.holzhell, F.holz, F.holzdunkel);
    // Pflanze auf dem Boden rechts
    b.rechteck(244, 92, 10, 12, F.orange); b.rechteck(244, 92, 10, 2, F.hellorange); b.ellipse(249, 84, 5, 7, F.gruen); b.ellipse(246, 80, 2, 5, F.dunkelgruen); b.ellipse(252, 79, 2, 5, F.dunkelgruen);
    return b;
}

// ---------- Archiv (Neugenerierung auf Klaus' Vorlage) ----------
export function archivV2(vorlage) {
    const b = vorlage;
    // Gemälde „Der Rat von 1949“ (x 169–215, y 26–58): freies Wandstück schaffen
    b.rechteck(163, 20, 58, 44, F.holzdunkel); b.rechteck(165, 22, 54, 40, F.holz);
    b.rechteck(167, 24, 50, 36, F.gelb); b.rechteck(169, 26, 46, 32, F.ocker);
    b.rechteck(170, 27, 44, 30, F.dunkelgrau);
    b.rechteck(170, 27, 44, 10, F.grau);
    b.rechteck(188, 28, 8, 9, F.rot); b.rechteck(189, 29, 6, 3, F.weiss); b.rechteck(190, 33, 4, 3, F.gelb);
    for (let i = 0; i < 6; i++) { const x = 172 + i * 7; b.rechteck(x + 1, 40, 4, 4, F.hautH); b.rechteck(x + 1, 40, 4, 1, i % 2 ? F.grau : F.holzdunkel); b.rechteck(x, 44, 6, 13, F.fastschwarz); b.rechteck(x + 2, 44, 2, 3, F.weiss); }
    // Regal mit Ratsprotokollen (x 118–154, y 43–98)
    b.rechteck(116, 36, 40, 66, F.holzdunkel); b.rechteck(118, 38, 36, 62, F.holzschwarz);
    for (let f = 0; f < 4; f++) {
        const fy = 40 + f * 15;
        for (let x = 120; x < 152; x += 4) { b.rechteck(x, fy, 3, 12, F.hellgrau); b.rechteck(x, fy + 2, 3, 3, F.weiss); b.rechteck(x + 1, fy + 8, 1, 1, F.dunkelgrau); }
        b.rechteck(118, fy + 12, 36, 2, F.holz);
    }
    b.rechteck(116, 28, 40, 8, F.weiss); text(b, 'PROTOKOLLE', 117, 30, F.dunkelblau);
    // Tür zurück in den Flur (links)
    tuer(b, 4, 38, 22, 66, F.holzhell, F.holz, F.gelb);
    return b;
}

// ---------- Rathausflur (Neugenerierung auf Klaus' Vorlage) ----------
export function flurV2(vorlage, schildBereich) {
    const b = vorlage;
    if (schildBereich) {
        const [x, y, w, h, wand] = schildBereich;
        b.rechteck(x, y, w, h, wand);
        const t = 'FRAKTION'; text(b, t, x + Math.floor((w - textBreite(t)) / 2), y + Math.floor((h - 5) / 2), F.fastschwarz);
    }
    // Ausgang nach links (Bildrand): hängendes Notausgangsschild mit Pfeil
    b.rechteck(62, 6, 1, 6, F.grau); b.rechteck(78, 6, 1, 6, F.grau);
    b.rechteck(56, 12, 28, 9, F.gruen); b.rechteck(57, 13, 26, 7, F.gruen);
    b.rechteck(59, 16, 8, 1, F.weiss); b.rechteck(60, 15, 1, 3, F.weiss); b.rechteck(59, 16, 1, 1, F.weiss);
    b.rechteck(70, 14, 2, 2, F.weiss); b.rechteck(70, 16, 2, 3, F.weiss); b.rechteck(72, 16, 3, 1, F.weiss); b.rechteck(69, 19, 1, 1, F.weiss); b.rechteck(72, 19, 1, 1, F.weiss);
    return b;
}

// ---------- Ems-Ufer bei Sonnenuntergang ----------
export function emsUfer() {
    const b = new Bild(B, H);
    b.baender(0, 0, B, 40, [F.lila, F.rosa, F.lachs, F.hellorange, F.hellgelb]);
    b.ellipse(40, 40, 12, 12, F.gelb); b.ellipse(40, 40, 9, 9, F.hellgelb);
    for (const [x, y, w] of [[90, 8, 26], [150, 14, 34], [210, 6, 22]]) { b.rechteck(x, y, w, 3, F.pfirsich); b.rechteck(x + 4, y - 2, w - 10, 2, F.pfirsich); b.rechteck(x + 2, y + 3, w - 4, 1, F.rosa); }
    // ferne Bäume und Wiese
    b.rechteck(0, 40, B, 14, F.dunkelgruen);
    for (let x = 0; x < B; x += 9) { b.ellipse(x + 4, 41 - (x * 7) % 5, 6, 6, (x / 9) % 2 ? F.dunkelgruen : F.petroldunkel); }
    b.rechteck(0, 54, B, 6, F.gruen);
    // Ems
    b.rechteck(0, 60, B, 52, F.blau);
    for (let y = 63; y < 110; y += 6) { for (let x = (y * 7) % 23; x < B; x += 23) { b.rechteck(x, y, 8, 1, F.hellblau); } }
    b.rechteck(30, 62, 20, 1, F.hellgelb); b.rechteck(34, 66, 14, 1, F.hellgelb); b.rechteck(36, 70, 10, 1, F.hellgelb);
    // alte Steinbrücke mit drei Bögen (x 150–230, y 25–60), Inschrift (x 165–210, y 33–45)
    b.rechteck(146, 24, 88, 14, F.stein); b.rechteck(146, 24, 88, 2, F.steinhell); b.rechteck(146, 36, 88, 2, F.steindunkel);
    for (let i = 0; i < 4; i++) { b.rechteck(148 + i * 27, 38, 10, 26, F.stein); b.rechteck(155 + i * 27, 38, 3, 26, F.steindunkel); }
    for (let i = 0; i < 3; i++) { b.ellipse(165 + i * 27, 38, 8, 6, F.blau); b.rechteck(157 + i * 27, 38, 17, 1, F.steindunkel); }
    b.rechteck(166, 26, 44, 10, F.steinhell); b.rechteck(166, 35, 44, 1, F.steindunkel);
    for (let i = 0; i < 9; i++) { b.rechteck(168 + i * 4 + (i % 2), 28, 3, 2, F.steindunkel); b.rechteck(169 + i * 4, 32, 2, 2, F.steindunkel); }
    for (let i = 0; i < 4; i++) { b.rechteck(148 + i * 27, 64, 10, 3, F.dunkelblau); }
    // sieben Pfähle (x 184–235, y 81–101)
    for (let i = 0; i < 7; i++) { const x = 186 + i * 7; b.rechteck(x, 81 + (i % 2) * 2, 4, 20 - (i % 2) * 2, F.holz); b.rechteck(x, 81 + (i % 2) * 2, 4, 1, F.holzhell); b.rechteck(x + 3, 83, 1, 17, F.holzdunkel); b.rechteck(x - 1, 100, 6, 1, F.hellblau); }
    // Ufer vorn mit Steg
    b.rechteck(0, 110, B, 34, F.gras); b.rechteck(0, 110, B, 2, F.gruen); for (let x = 0; x < B; x += 5) { b.rechteck(x, 113 + (x % 3), 1, 2, F.dunkelgruen); b.rechteck(x + 2, 125 + (x % 4), 1, 2, F.dunkelgruen); }
    b.rechteck(60, 102, 82, 5, F.holzhell); b.rechteck(60, 107, 82, 1, F.holz); for (let i = 0; i < 5; i++) { b.rechteck(62 + i * 19, 108, 3, 6, F.holz); }
    for (let x = 60; x < 142; x += 8) { b.rechteck(x, 102, 1, 5, F.holz); }
    // Schilf und Uferpfad nach rechts (Ausgang x 230–250, y 43–86 → Pfad rechts am Ufer)
    for (let i = 0; i < 14; i++) { const x = 4 + i * 4; b.rechteck(x, 96 + (i % 3) * 2, 1, 16, F.dunkelgruen); b.rechteck(x, 94 + (i % 3) * 2, 1, 3, F.braun); }
    b.polygon([[226, 112], [256, 104], [256, 144], [236, 144]], F.sand2); b.polygon([[232, 112], [256, 106], [256, 112]], F.sand);
    b.rechteck(244, 88, 2, 22, F.holzdunkel); b.rechteck(236, 86, 18, 6, F.weiss); text(b, 'STADT', 237, 87, F.gruen);
    return b;
}

// ---------- Baustelle Badsanierung ----------
export function baustelle(trocken) {
    const b = new Bild(B, H);
    b.baender(0, 0, B, 64, [F.dunkellila, F.lila, F.lachs, F.hellorange, F.pfirsich]);
    // Kran
    b.rechteck(30, 8, 3, 70, F.gelb); for (let y = 10; y < 78; y += 4) { b.rechteck(30, y, 3, 1, F.ocker); }
    b.rechteck(14, 8, 100, 3, F.gelb); for (let x = 14; x < 114; x += 5) { b.rechteck(x, 9, 1, 1, F.ocker); } b.rechteck(88, 11, 1, 22, F.dunkelgrau); b.rechteck(85, 33, 7, 4, F.dunkelgrau);
    b.rechteck(14, 11, 10, 5, F.dunkelgrau);
    // Hintergrund-Häuser
    for (let i = 0; i < 6; i++) { b.rechteck(120 + i * 22, 46 - (i % 3) * 5, 18, 24 + (i % 3) * 5, (i % 2) ? F.steindunkel : F.stein); b.rechteck(124 + i * 22, 52 - (i % 3) * 5, 4, 4, F.hellgelb); }
    // Bauzaun
    b.rechteck(0, 64, B, 40, F.sand2);
    for (let x = 0; x < B; x += 32) { b.rechteck(x, 62, 2, 34, F.grau); }
    b.rechteck(0, 62, B, 1, F.grau); b.rechteck(0, 92, B, 1, F.grau);
    for (let x = 0; x < B; x += 4) { b.rechteck(x, 63, 1, 29, F.hellgrau); } for (let y = 66; y < 92; y += 4) { b.rechteck(0, y, B, 1, F.hellgrau); }
    for (let x = 0; x < B; x += 16) { b.rechteck(x, 92, 8, 5, F.rot); b.rechteck(x + 8, 92, 8, 5, F.weiss); }
    // Bauwagen (hinter den Gegnern, x 176–222)
    b.rechteck(172, 60, 52, 36, F.creme); b.rechteck(172, 60, 52, 3, F.grau); b.rechteck(178, 68, 14, 10, F.hellblau); b.rechteck(178, 68, 14, 2, F.weiss);
    b.rechteck(200, 66, 14, 30, F.beige); b.rechteck(210, 80, 2, 2, F.dunkelgrau); b.rechteck(176, 96, 8, 6, F.fastschwarz); b.rechteck(212, 96, 8, 6, F.fastschwarz);
    // Tor im Bauzaun rechts (x 225–248, y 43–92)
    b.rechteck(226, 58, 22, 46, F.dunkelgrau); b.rechteck(228, 60, 18, 44, F.sand2); for (let x = 228; x < 246; x += 3) { b.rechteck(x, 60, 1, 44, F.hellgrau); }
    b.rechteck(229, 62, 16, 8, F.weiss); text(b, 'TOR', 231, 63, F.rot);
    // Boden
    b.rechteck(0, 104, B, 40, F.sand); for (let x = 0; x < B; x += 7) { b.rechteck(x, 108 + (x % 5) * 6, 2, 1, F.sand2); }
    b.polygon([[20, 104], [44, 88], [70, 104]], F.sand2); b.polygon([[44, 88], [56, 96], [70, 104], [44, 104]], F.lehm);
    // Leitung über der Grube (x 118–144, y 75–87)
    b.rechteck(112, 79, 38, 5, F.grau); b.rechteck(112, 79, 38, 1, F.hellgrau); b.rechteck(130, 77, 5, 9, F.dunkelgrau);
    if (!trocken) { b.rechteck(131, 86, 2, 4, F.hellblau); b.rechteck(130, 90, 1, 2, F.hellblau); b.rechteck(133, 89, 1, 3, F.hellblau); }
    else { b.rechteck(130, 86, 5, 2, F.weiss); }
    // Baugrube (x 102–158, y 89–109)
    b.rechteck(100, 88, 60, 22, F.holzdunkel); b.rechteck(102, 90, 56, 20, F.holz);
    if (trocken) {
        b.rechteck(104, 94, 52, 16, F.lehm);
        for (let i = 0; i < 7; i++) { b.rechteck(105 + i * 7, 102, 6, 5, i % 2 ? F.tuerkis : F.himmel); b.rechteck(105 + i * 7, 102, 6, 1, F.eisblau); }
        b.ellipse(130, 98, 3, 2, F.gelb); b.rechteck(129, 97, 1, 1, F.hellgelb);
    } else {
        b.rechteck(104, 94, 52, 16, F.blau); for (let x = 106; x < 154; x += 9) { b.rechteck(x, 97, 5, 1, F.hellblau); b.rechteck(x + 3, 103, 5, 1, F.hellblau); }
    }
    // Absperrbaken
    for (const x of [90, 166]) { b.rechteck(x, 96, 6, 16, F.weiss); b.rechteck(x, 99, 6, 3, F.rot); b.rechteck(x, 105, 6, 3, F.rot); b.rechteck(x - 1, 111, 8, 2, F.dunkelgrau); }
    return b;
}

// ---------- Schulhof bei Nacht ----------
export function schulhof() {
    const b = new Bild(B, H, F.nacht);
    b.baender(0, 0, B, 40, [F.nacht, F.nacht2]); sterne(b, 40, 7, 30);
    b.ellipse(232, 14, 7, 7, F.creme); b.ellipse(235, 12, 6, 6, F.nacht);
    // Backsteinfassade
    b.rechteck(20, 18, 216, 86, F.dunkelrot);
    for (let y = 18; y < 104; y += 4) { for (let x = 20 + ((y / 4) % 2) * 4; x < 236; x += 8) { b.rechteck(x, y, 7, 3, F.rot); } }
    b.polygon([[14, 20], [128, 4], [242, 20]], F.holzdunkel); b.rechteck(14, 18, 228, 3, F.holzschwarz);
    b.rechteck(84, 22, 70, 9, F.creme); text(b, 'GRUNDSCHULE', 119 - Math.floor(textBreite('GRUNDSCHULE') / 2), 24, F.dunkelrot);
    for (let i = 0; i < 7; i++) {
        const x = 28 + i * 30; if (x > 166 && x < 214) { continue; }
        b.rechteck(x, 36, 16, 18, F.beige); b.rechteck(x + 2, 38, 12, 14, i % 3 === 0 ? F.hellgelb : F.navy); b.rechteck(x + 7, 38, 2, 14, F.beige);
        b.rechteck(x, 62, 16, 18, F.beige); b.rechteck(x + 2, 64, 12, 14, i === 2 ? F.hellgelb : F.navy); b.rechteck(x + 7, 64, 2, 14, F.beige);
    }
    // Wanduhr (x 154–180, y 20–43)
    b.ellipse(167, 33, 11, 11, F.dunkelgrau); b.ellipse(167, 33, 10, 10, F.weiss); for (let i = 0; i < 12; i++) { b.setze(167 + Math.round(Math.sin(i * Math.PI / 6) * 8), 33 - Math.round(Math.cos(i * Math.PI / 6) * 8), F.dunkelgrau); }
    b.linie(167, 33, 167, 25, F.schwarz); b.linie(167, 33, 171, 35, F.schwarz);
    // Haupteingang mit Vordach und Glastüren (x 176–216, y 52–104): Elternabend, drinnen brennt Licht
    b.rechteck(172, 46, 48, 4, F.stein); b.rechteck(172, 50, 48, 1, F.steindunkel);
    b.rechteck(176, 52, 40, 52, F.beige);
    for (const x of [179, 197]) { b.rechteck(x, 56, 16, 48, F.dunkelgrau); b.rechteck(x + 2, 58, 12, 30, F.hellgelb); b.rechteck(x + 2, 74, 12, 1, F.dunkelgrau); }
    b.rechteck(193, 76, 2, 4, F.hellgrau); b.rechteck(197, 76, 2, 4, F.hellgrau);
    b.rechteck(182, 40, 28, 6, F.creme); text(b, 'EINGANG', 196 - Math.floor(textBreite('EINGANG') / 2), 41, F.dunkelrot);
    b.ellipse(196, 112, 22, 3, F.hellgrau);
    // Schultor links (x 10–30, y 43–92)
    b.rechteck(4, 60, 3, 44, F.dunkelgrau); b.rechteck(28, 60, 3, 44, F.dunkelgrau); for (let x = 9; x < 28; x += 3) { b.rechteck(x, 66, 1, 36, F.grau); } b.rechteck(7, 66, 21, 2, F.grau); b.rechteck(7, 96, 21, 2, F.grau);
    // Hof
    b.rechteck(0, 104, B, 40, F.grau); b.rechteck(0, 104, B, 2, F.dunkelgrau);
    for (let i = 0; i < 4; i++) { b.rahmen(40 + i * 10, 124, 9, 8, F.hellgrau); } b.rahmen(80, 120, 9, 8, F.hellgrau); b.rahmen(80, 128, 9, 8, F.hellgrau);
    b.rechteck(108, 108, 40, 4, F.dunkelgruen); b.rechteck(108, 108, 40, 1, F.gruen); b.rechteck(127, 104, 1, 4, F.weiss); b.rechteck(110, 112, 2, 10, F.dunkelgrau); b.rechteck(144, 112, 2, 10, F.dunkelgrau);
    // Laterne mit Lichtkreis
    b.rechteck(236, 60, 2, 50, F.dunkelgrau); b.rechteck(232, 56, 10, 4, F.dunkelgrau); b.rechteck(233, 60, 8, 2, F.hellgelb);
    b.ellipse(237, 116, 16, 3, F.hellgrau);
    return b;
}

// ---------- Flur der Grundschule ----------
// Klassenzimmertüren 1a (x 36–60), 2b (x 88–112), 4c (x 140–164), Kellertür mit Absperrband (x 196–222), Ausgang zum Hof links.
export function schulflur() {
    const b = new Bild(B, H, F.creme);
    // Wand: oben hell, unten abwaschbare Fliesen
    b.rechteck(0, 0, B, 14, F.creme2); b.rechteck(0, 14, B, 1, F.beige);
    for (let i = 0; i < 5; i++) { const x = 18 + i * 52; b.rechteck(x, 4, 20, 3, F.weiss); b.rechteck(x, 7, 20, 1, F.hellgrau); }
    b.rechteck(0, 70, B, 34, F.eisblau);
    for (let y = 70; y < 104; y += 6) { b.rechteck(0, y, B, 1, F.hellblau); }
    for (let y = 70, r = 0; y < 104; y += 6, r++) { for (let x = (r % 2) * 6; x < B; x += 12) { b.rechteck(x, y + 1, 1, 5, F.hellblau); } }
    b.rechteck(0, 69, B, 1, F.blau);
    // Linoleum
    b.rechteck(0, 104, B, 40, F.lehm); b.rechteck(0, 104, B, 2, F.braun);
    for (let y = 110; y < H; y += 8) { b.rechteck(0, y, B, 1, F.sand2); }
    b.ellipse(128, 124, 70, 6, F.sand2);
    // Ausgang zum Hof (links): Glastür mit Pfeil
    b.rechteck(0, 40, 22, 64, F.dunkelgrau); b.rechteck(2, 44, 18, 36, F.nacht2); sterne(b, 0, 1, 1);
    b.rechteck(4, 30, 16, 8, F.gruen); b.rechteck(7, 33, 8, 1, F.weiss); b.rechteck(8, 32, 1, 3, F.weiss); b.rechteck(7, 33, 1, 1, F.weiss);
    // Klassenzimmer
    const klassen = [['1A', 36, F.rot], ['2B', 88, F.blau], ['4C', 140, F.gruen]];
    for (const [name, x, farbe] of klassen) {
        tuer(b, x, 44, 24, 60, farbe, F.dunkelgrau, F.hellgrau, F.eisblau);
        b.rechteck(x + 4, 34, 16, 7, F.weiss); text(b, name, x + 12 - Math.floor(textBreite(name) / 2), 35, F.fastschwarz);
    }
    // Kinderbilder und Garderobe zwischen den Türen
    const bilder = [[66, F.gelb, F.rot], [118, F.hellgruen, F.blau], [170, F.rosa, F.lila]];
    for (const [x, a, c] of bilder) {
        b.rechteck(x, 26, 16, 12, F.weiss); b.ellipse(x + 5, 31, 3, 3, a); b.rechteck(x + 9, 30, 5, 6, c); b.rechteck(x + 2, 35, 12, 1, F.gruen);
        b.rechteck(x - 2, 52, 20, 2, F.holz);
        for (let i = 0; i < 3; i++) { b.rechteck(x + i * 6, 54, 2, 3, F.grau); b.rechteck(x - 1 + i * 6, 57, 5, 9, i === 1 ? c : a); }
    }
    // Kellertür (Stahl) mit Absperrband und Zettel
    b.rechteck(194, 40, 30, 64, F.steindunkel); b.rechteck(196, 42, 26, 62, F.grau); b.rechteck(198, 44, 22, 58, F.hellgrau);
    b.rechteck(215, 72, 3, 2, F.dunkelgrau);
    b.rechteck(198, 30, 22, 8, F.gelb); text(b, 'KELLER', 209 - Math.floor(textBreite('KELLER') / 2), 31, F.fastschwarz);
    for (let i = 0; i < 9; i++) { b.rechteck(190 + i * 4, 60 + Math.round(i * 0.6), 2, 2, F.rot); b.rechteck(192 + i * 4, 60 + Math.round(i * 0.6), 2, 2, F.weiss); }
    b.rechteck(201, 78, 12, 9, F.weiss); b.rechteck(203, 80, 8, 1, F.grau); b.rechteck(203, 82, 6, 1, F.grau); b.rechteck(203, 84, 7, 1, F.grau);
    // Turnbeutel und Feuerlöscher
    b.rechteck(232, 70, 6, 14, F.rot); b.rechteck(233, 66, 4, 4, F.dunkelgrau); b.rechteck(231, 84, 8, 2, F.dunkelrot);
    return b;
}

// ---------- Krypta ----------
export function krypta() {
    const b = new Bild(B, H, F.fastschwarz);
    // Gewölbe aus Backstein
    for (let y = 0; y < 104; y += 4) { for (let x = ((y / 4) % 2) * 4; x < B; x += 8) { b.rechteck(x, y, 7, 3, F.holzdunkel); } }
    for (let i = 0; i < 3; i++) { const cx = 42 + i * 86; b.ellipse(cx, 8, 40, 18, F.holzschwarz); b.ellipse(cx, 6, 36, 14, F.fastschwarz); }
    for (let i = 0; i < 4; i++) { b.rechteck(i * 86 - 3, 0, 6, 104, F.holz); b.rechteck(i * 86 - 3, 0, 2, 104, F.holzhell); }
    // Fresko (x 26–93, y 20–63)
    b.rechteck(24, 18, 72, 48, F.steindunkel); b.rechteck(26, 20, 68, 44, F.beige);
    const cx = 60;
    b.rechteck(cx - 14, 25, 29, 1, F.braun); b.rechteck(cx, 22, 1, 10, F.braun); b.linie(cx - 14, 25, cx - 17, 30, F.braun); b.linie(cx - 14, 25, cx - 11, 30, F.braun); b.rechteck(cx - 18, 30, 8, 2, F.ocker); b.linie(cx + 14, 25, cx + 11, 30, F.braun); b.linie(cx + 14, 25, cx + 17, 30, F.braun); b.rechteck(cx + 10, 30, 8, 2, F.ocker);
    b.polygon([[cx - 12, 36], [cx, 38], [cx, 48], [cx - 12, 46]], F.creme); b.polygon([[cx + 12, 36], [cx, 38], [cx, 48], [cx + 12, 46]], F.weiss); b.rechteck(cx, 38, 1, 10, F.braun);
    b.rechteck(cx - 8, 51, 16, 11, F.holzhell); b.rechteck(cx - 8, 51, 16, 2, F.lehm); b.rechteck(cx - 3, 52, 6, 1, F.holzschwarz);
    // Symbolschloss (x 113–144, y 58–78)
    b.rechteck(112, 56, 34, 24, F.steindunkel); b.rechteck(114, 58, 30, 20, F.stein);
    for (let i = 0; i < 3; i++) { b.ellipse(120 + i * 9, 68, 4, 4, F.ocker); b.ellipse(120 + i * 9, 68, 3, 3, F.gelb); }
    b.rechteck(119, 67, 3, 1, F.holzdunkel); b.rechteck(127, 66, 3, 4, F.holzdunkel); b.rechteck(137, 66, 4, 4, F.holzdunkel);
    // Tor mit drei Vertiefungen (x 179–225, y 26–98)
    b.rechteck(175, 22, 54, 82, F.stein); b.polygon([[173, 22], [202, 12], [231, 22]], F.steinhell); b.rechteck(175, 22, 54, 2, F.steinhell);
    b.rechteck(180, 28, 44, 76, F.steindunkel); b.rechteck(201, 28, 2, 76, F.holzschwarz);
    for (let i = 0; i < 3; i++) { b.ellipse(188 + i * 14, 40, 4, 4, F.fastschwarz); }
    b.rechteck(201, 64, 2, 5, F.schwarz); b.ellipse(202, 63, 1, 1, F.schwarz);
    // Fackeln, Treppe, Boden
    fackel(b, 104, 36); fackel(b, 158, 36); fackel(b, 238, 36);
    b.rechteck(0, 104, B, 40, F.dunkelgrau); for (let x = 0; x < B; x += 16) { b.rechteck(x, 104, 1, 40, F.fastschwarz); } b.rechteck(0, 118, B, 1, F.fastschwarz); b.rechteck(0, 132, B, 1, F.fastschwarz);
    for (let i = 0; i < 5; i++) { b.rechteck(0, 80 + i * 6, 26 - i * 4, 6, i % 2 ? F.grau : F.hellgrau); b.rechteck(0, 85 + i * 6, 26 - i * 4, 1, F.dunkelgrau); }
    return b;
}

// ---------- Tempel ----------
function tempelHalle(b) {
    b.rechteck(0, 0, B, 104, F.sand);
    for (let y = 8; y < 104; y += 8) { b.rechteck(0, y, B, 1, F.sand2); for (let x = ((y / 8) % 2) * 12; x < B; x += 24) { b.rechteck(x, y, 1, 8, F.sand2); } }
    for (const x of [8, 64, 178, 234]) { b.rechteck(x, 4, 14, 100, F.steinhell); b.rechteck(x + 10, 4, 4, 100, F.stein); b.rechteck(x - 2, 2, 18, 4, F.stein); b.rechteck(x - 2, 100, 18, 4, F.stein); for (let y = 10; y < 98; y += 8) { b.rechteck(x + 4, y, 1, 5, F.stein); } }
    b.rechteck(0, 104, B, 40, F.lehm);
    for (let y = 104; y < H; y += 8) { b.rechteck(0, y, B, 1, F.sand2); }
    for (let i = -6; i <= 6; i++) { b.linie(128 + i * 12, 104, 128 + i * 30, 144, F.sand2); }
    fackel(b, 40, 46); fackel(b, 214, 46);
}

export function tempelEingang(offen) {
    const b = new Bild(B, H); tempelHalle(b);
    b.rechteck(85, 10, 82, 16, F.steindunkel); b.rechteck(87, 12, 78, 12, F.steinhell); text(b, 'FRAKTIONSZWANG', 126 - Math.floor(textBreite('FRAKTIONSZWANG') / 2), 15, F.holzdunkel);
    b.rechteck(98, 26, 56, 78, F.fastschwarz); b.rechteck(96, 26, 2, 78, F.steindunkel); b.rechteck(154, 26, 2, 78, F.steindunkel);
    const unten = offen ? 36 : 101;
    for (let i = 0; i < 7; i++) { const x = 102 + i * 7; b.rechteck(x, 26, 3, unten - 26, F.dunkelgrau); b.rechteck(x, 26, 1, unten - 26, F.grau); b.polygon([[x - 1, unten], [x + 1, unten + 4], [x + 4, unten]], F.dunkelgrau); }
    for (let y = 30; y < unten - 4; y += 14) { b.rechteck(100, y, 52, 2, F.grau); }
    bodenplatte(b, 56, 104, 36, 9, false); bodenplatte(b, 159, 104, 36, 9, false);
    return b;
}

export function tempelAbgrund(bruecke) {
    const b = new Bild(B, H); tempelHalle(b);
    // Tafel mit Paragraf (x 51–87, y 35–75)
    b.rechteck(49, 33, 40, 44, F.steindunkel); b.rechteck(51, 35, 36, 40, F.steinhell);
    const par = ['.###.', '#....', '.###.', '#...#', '.###.', '....#', '.###.'];
    par.forEach((z, j) => { for (let i = 0; i < 5; i++) { if (z[i] === '#') { b.rechteck(62 + i * 3, 38 + j * 2, 3, 2, F.ocker); } } });
    for (let i = 0; i < 4; i++) { b.rechteck(55, 58 + i * 4, 28, 1, F.stein); }
    // Abgrund quer durch den Boden (Klickfläche x 118–185, y 72–98 liegt darüber)
    b.polygon([[116, 104], [188, 104], [196, 144], [108, 144]], F.fastschwarz);
    b.polygon([[120, 108], [184, 108], [190, 144], [114, 144]], F.schwarz);
    b.rechteck(116, 104, 72, 2, F.steindunkel); b.rechteck(114, 104, 2, 40, F.sand2); b.rechteck(188, 104, 2, 40, F.sand2);
    if (bruecke) { b.rechteck(104, 116, 96, 10, F.steinhell); b.rechteck(104, 116, 96, 2, F.weiss); b.rechteck(104, 124, 96, 2, F.steindunkel); for (let x = 108; x < 200; x += 12) { b.rechteck(x, 118, 1, 6, F.stein); } }
    // Gang mit Kugel (x 195–231, y 43–101)
    b.rechteck(193, 40, 40, 64, F.steinhell); b.polygon([[191, 40], [213, 30], [235, 40]], F.stein);
    b.rechteck(197, 46, 32, 58, F.holzschwarz); b.rechteck(201, 50, 24, 54, F.fastschwarz);
    b.ellipse(213, 60, 9, 9, F.grau); b.ellipse(210, 57, 3, 3, F.hellgrau);
    return b;
}

export function tempelFinale() {
    const b = new Bild(B, H); tempelHalle(b);
    // Oberlicht mit Lichtfleck
    b.ellipse(128, 2, 18, 8, F.hellgelb); b.ellipse(128, 1, 14, 5, F.weiss);
    b.polygon([[96, 104], [160, 104], [176, 132], [80, 132]], F.sand);
    // Pokal auf Podest (x 77–103)
    b.rechteck(78, 78, 24, 26, F.stein); b.rechteck(78, 78, 24, 2, F.steinhell); b.rechteck(98, 78, 4, 26, F.steindunkel); b.rechteck(75, 76, 30, 3, F.steinhell);
    b.polygon([[80, 54], [100, 54], [94, 68], [86, 68]], F.gelb); b.rechteck(88, 68, 4, 5, F.ocker); b.rechteck(84, 73, 12, 3, F.ocker); b.rechteck(93, 55, 4, 8, F.hellgelb); b.rechteck(89, 59, 2, 2, F.rot);
    // Wahlurne auf Sockel (x 154–180)
    b.rechteck(152, 84, 30, 20, F.stein); b.rechteck(152, 84, 30, 2, F.steinhell); b.rechteck(178, 84, 4, 20, F.steindunkel); b.rechteck(150, 82, 34, 3, F.steinhell);
    b.rechteck(155, 66, 24, 16, F.holzhell); b.rechteck(155, 66, 24, 2, F.lehm); b.rechteck(161, 67, 12, 1, F.holzschwarz); for (let y = 70; y < 82; y += 3) { b.rechteck(156, y, 22, 1, F.braun); }
    b.linie(179, 69, 183, 78, F.grau); b.rechteck(182, 78, 2, 6, F.gelb); b.rechteck(182, 83, 2, 1, F.hautS);
    return b;
}

// ---------- Minispiel-Streifen (512×144, waagerecht kachelbar) ----------
export function minispielEms() {
    const b = new Bild(512, H);
    b.rechteck(0, 0, 512, 22, F.gras); for (let x = 0; x < 512; x += 16) { b.ellipse(x + 8, 10, 8, 7, (x / 16) % 2 ? F.dunkelgruen : F.petroldunkel); }
    b.rechteck(0, 22, 512, 4, F.dunkelgruen);
    b.rechteck(0, 26, 512, 100, F.blau);
    for (let y = 30; y < 124; y += 7) { for (let x = (y * 11) % 32; x < 512; x += 32) { b.rechteck(x, y, 10, 1, F.hellblau); } }
    b.rechteck(0, 126, 512, 18, F.gras); b.rechteck(0, 126, 512, 2, F.gruen); for (let x = 0; x < 512; x += 8) { b.rechteck(x + 3, 120, 1, 8, F.dunkelgruen); }
    return b;
}

export function minispielGang() {
    const b = new Bild(512, H);
    b.rechteck(0, 0, 512, 110, F.sand); for (let y = 8; y < 110; y += 8) { b.rechteck(0, y, 512, 1, F.sand2); }
    for (let x = 0; x < 512; x += 64) { b.rechteck(x + 20, 6, 16, 104, F.steinhell); b.rechteck(x + 32, 6, 4, 104, F.stein); b.rechteck(x + 18, 4, 20, 3, F.stein); }
    b.rechteck(0, 110, 512, 34, F.lehm); for (let x = 0; x < 512; x += 16) { b.rechteck(x, 110, 1, 34, F.sand2); }
    return b;
}
