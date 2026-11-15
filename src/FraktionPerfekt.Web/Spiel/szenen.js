// Szenengrafik in 320×200: statische Hintergründe (gedithert, gecacht), Hotspot-Objekte, Vordergrund mit Parallaxe,
// kleine Animationen je Szene, Reisekarte und Minispiele. Alles eigene Gestaltung.
(function (wurzel) {
    'use strict';
    const { Puffer, P, stift } = wurzel.Pixel;
    const B = 320, H = 200, RAND = 4;           // Hintergrund ist 8 px breiter für die Parallaxe
    const R = (h) => ({ x: Math.round(h.x * B / 100), y: Math.round(h.y * H / 100), b: Math.round(h.b * B / 100), h: Math.round(h.h * H / 100) });

    // ---------- Wiederkehrende Bausteine ----------
    function holzboden(p, y0, hell, dunkel) {
        for (let y = y0; y < H; y++) {
            const reihe = Math.floor((y - y0) / 6);
            p.rechteck(0, y, B + 2 * RAND, 1, (y - y0) % 6 === 0 ? dunkel : hell);
            for (let x = (reihe % 2) * 24; x < B + 2 * RAND; x += 48) { p.setze(x, y, dunkel); }
        }
        p.toenen(0, y0, B + 2 * RAND, 6, P.schwarz, 0.35);
        p.sprenkel(0, y0, B + 2 * RAND, H - y0, P.braun, 0.03, 11);
    }

    function ziegelwand(p, x0, y0, b, h, fuge, stein, stein2, saat) {
        p.rechteck(x0, y0, b, h, fuge);
        for (let y = 0; y < h; y += 6) {
            for (let x = ((y / 6) % 2) * 6 - 6; x < b; x += 12) {
                p.rechteck(x0 + x + 1, y0 + y + 1, 10, 4, ((x + y * 3 + (saat || 0)) % 5 === 0) ? stein2 : stein);
            }
        }
    }

    function sterne(p, b, h, saat) { p.sprenkel(0, 0, b, h, P.eis, 0.012, saat); }

    function fenster(p, x, y, b, h, himmel, silhouette) {
        p.rechteck(x - 3, y - 3, b + 6, h + 6, P.creme);
        p.verlauf(x, y, b, h, himmel);
        if (silhouette) { silhouette(p, x, y, b, h); }
        p.rechteck(x + Math.floor(b / 2) - 1, y, 2, h, P.creme); p.rechteck(x, y + Math.floor(h / 2) - 1, b, 2, P.creme);
        p.rechteck(x - 5, y + h + 3, b + 10, 3, P.steinhell);
    }

    function grevenSilhouette(p, x, y, b, h) {
        const f = P.tinte;
        // Dächer und der Kirchturm von St. Martinus (stilisiert)
        p.polygon([[x, y + h], [x, y + h - 10], [x + 8, y + h - 16], [x + 16, y + h - 10], [x + 16, y + h]], f);
        p.rechteck(x + 14, y + h - 12, 14, 12, f);
        p.rechteck(x + 30, y + h - 20, 8, 20, f); p.polygon([[x + 29, y + h - 20], [x + 34, y + h - 40], [x + 39, y + h - 20]], f);
        p.rechteck(x + 38, y + h - 9, 20, 9, f); p.polygon([[x + 38, y + h - 9], [x + 48, y + h - 15], [x + 58, y + h - 9]], f);
        for (let i = 0; i < 4; i++) { p.setze(x + 18 + i * 3, y + h - 7, P.gelb); }
        p.setze(x + 42, y + h - 5, P.gelb); p.setze(x + 50, y + h - 5, P.orange);
    }

    function fackel(p, x, y) { p.rechteck(x, y, 3, 10, P.dunkelbraun); p.rechteck(x - 1, y - 2, 5, 3, P.anthrazit); }

    function lichtkegel(p, cx, y0, y1, breiteOben, breiteUnten, c, staerke) {
        for (let y = y0; y < y1; y++) {
            const t = (y - y0) / (y1 - y0), w = breiteOben + (breiteUnten - breiteOben) * t;
            p.toenen(Math.round(cx - w / 2), y, Math.round(w), 1, c, staerke * (1 - t * 0.6));
        }
    }

    // ---------- Hotspot-Objekte ----------
    const OBJEKT = {
        brief(p, r) { p.rechteck(r.x + 4, r.y + 4, 22, 10, P.creme); p.linie(r.x + 4, r.y + 4, r.x + 15, r.y + 10, P.grau); p.linie(r.x + 25, r.y + 4, r.x + 15, r.y + 10, P.grau); p.rechteck(r.x + 5, r.y + 14, 22, 1, P.hautdunkel); p.ellipse(r.x + 15, r.y + 10, 1, 1, P.rot); },
        fax(p, r) {
            p.rechteck(r.x + 2, r.y + 10, 32, 14, [206, 196, 170]); p.rechteck(r.x + 2, r.y + 10, 32, 2, [226, 218, 196]); p.rechteck(r.x + 2, r.y + 23, 32, 1, P.dunkelgrau);
            p.polygon([[r.x + 8, r.y + 10], [r.x + 12, r.y], [r.x + 26, r.y], [r.x + 28, r.y + 10]], P.weiss); p.rechteck(r.x + 13, r.y + 2, 10, 1, P.grau); p.rechteck(r.x + 13, r.y + 5, 8, 1, P.grau);
            p.rechteck(r.x + 22, r.y + 13, 9, 4, [60, 90, 70]); for (let i = 0; i < 3; i++) { for (let j = 0; j < 3; j++) { p.rechteck(r.x + 5 + i * 4, r.y + 13 + j * 3, 3, 2, P.anthrazit); } }
            p.rechteck(r.x - 4, r.y + 24, 44, 3, P.braun); p.rechteck(r.x - 2, r.y + 27, 40, 30, P.holz); p.rechteck(r.x, r.y + 30, 17, 24, P.hellholz); p.rechteck(r.x + 19, r.y + 30, 17, 24, P.hellholz);
            p.setze(r.x + 15, r.y + 42, P.gold); p.setze(r.x + 21, r.y + 42, P.gold);
        },
        regal(p, r) {
            p.rechteck(r.x, r.y, r.b, r.h, P.dunkelbraun); p.rechteck(r.x + 2, r.y + 2, r.b - 4, r.h - 4, [70, 42, 28]);
            const farben = [P.rot, P.blau, P.dunkelgruen, P.ocker, P.violett, P.creme, P.dunkelrot, P.tuerkis];
            const faecher = 5, fh = Math.floor((r.h - 4) / faecher);
            for (let f = 0; f < faecher; f++) {
                const fy = r.y + 2 + f * fh;
                let x = r.x + 3, i = f * 3;
                while (x < r.x + r.b - 5) {
                    const w = 2 + (i % 3), hh = fh - 3 - (i % 4);
                    p.rechteck(x, fy + fh - 1 - hh, w, hh, farben[i % farben.length]); p.setze(x, fy + fh - hh, P.weiss);
                    x += w + (i % 5 === 0 ? 2 : 0); i++;
                }
                p.rechteck(r.x + 2, fy + fh - 1, r.b - 4, 2, P.holz);
            }
            p.rechteck(r.x, r.y, 2, r.h, P.holz); p.rechteck(r.x + r.b - 2, r.y, 2, r.h, [70, 42, 28]);
        },
        kasten(p, r) {
            p.rechteck(r.x, r.y, r.b, r.h, P.braun); p.rechteck(r.x + 1, r.y + 1, r.b - 2, r.h - 2, P.holz);
            for (let i = 0; i < 3; i++) { for (let j = 0; j < 2; j++) { const x = r.x + 2 + i * Math.floor((r.b - 4) / 3), y = r.y + 2 + j * Math.floor((r.h - 4) / 2); p.rechteck(x, y, Math.floor((r.b - 4) / 3) - 1, Math.floor((r.h - 4) / 2) - 1, P.dunkelbraun); p.rechteck(x + 1, y + 3, 5, 3, P.creme); } }
        },
        garderobe(p, r, z) {
            const mx = r.x + Math.floor(r.b / 2);
            p.rechteck(mx - 1, r.y, 3, r.h, P.dunkelbraun); p.rechteck(mx - 8, r.y + r.h - 2, 17, 2, P.dunkelbraun);
            p.rechteck(mx - 7, r.y + 2, 15, 2, P.dunkelbraun);
            p.polygon([[mx - 9, r.y + 8], [mx + 9, r.y + 8], [mx + 11, r.y + 40], [mx - 11, r.y + 40]], [92, 58, 34]); p.toenen(mx + 2, r.y + 8, 9, 32, [130, 86, 52], 0.5); p.rechteck(mx - 1, r.y + 8, 2, 30, [70, 42, 26]);
            if (!z.inventar.includes('taschenlampe')) { p.rechteck(mx - 9, r.y - 1, 19, 2, [96, 62, 36]); p.rechteck(mx - 5, r.y - 6, 11, 5, [118, 78, 46]); p.rechteck(mx - 5, r.y - 3, 11, 1, [58, 36, 22]); p.rechteck(mx + 6, r.y + 44, 3, 8, P.anthrazit); }
        },
        tuer(p, r) {
            p.rechteck(r.x - 3, r.y - 3, r.b + 6, r.h + 3, P.steinhell); p.rechteck(r.x, r.y, r.b, r.h, [110, 66, 36]);
            p.rechteck(r.x + 3, r.y + 4, r.b - 6, Math.floor(r.h / 2) - 6, [124, 78, 44]); p.rechteck(r.x + 3, r.y + Math.floor(r.h / 2) + 2, r.b - 6, Math.floor(r.h / 2) - 6, [124, 78, 44]);
            p.toenen(r.x, r.y, 3, r.h, P.schwarz, 0.4); p.rechteck(r.x + r.b - 6, r.y + Math.floor(r.h / 2), 3, 2, P.gold);
        },
        pfeil(p, r) { const cy = r.y + Math.floor(r.h / 2); p.polygon([[r.x + 2, cy - 4], [r.x + r.b - 8, cy - 4], [r.x + r.b - 8, cy - 9], [r.x + r.b - 1, cy], [r.x + r.b - 8, cy + 9], [r.x + r.b - 8, cy + 4], [r.x + 2, cy + 4]], P.creme); p.toenen(r.x + 2, cy, r.b - 4, 4, P.sand, 0.5); },
        bild(p, r) {
            p.rechteck(r.x - 3, r.y - 3, r.b + 6, r.h + 6, P.ocker); p.rechteck(r.x - 2, r.y - 2, r.b + 4, r.h + 4, P.gold);
            p.verlauf(r.x, r.y, r.b, r.h, [[90, 70, 60], [60, 50, 46]]);
            for (let i = 0; i < 6; i++) { const x = r.x + 4 + i * Math.floor((r.b - 8) / 6); p.rechteck(x, r.y + Math.floor(r.h * 0.55), 5, Math.floor(r.h * 0.45), [40, 36, 40]); p.ellipse(x + 2, r.y + Math.floor(r.h * 0.5), 2, 2, P.hautschatten); }
            p.ellipse(r.x + Math.floor(r.b / 2), r.y + 6, 4, 5, P.dunkelrot); p.rechteck(r.x + Math.floor(r.b / 2) - 3, r.y + 3, 7, 2, P.gold);
        },
        inschrift(p, r) { p.toenen(r.x, r.y, r.b, r.h, P.steindunkel, 0.5); for (let i = 0; i < r.b - 6; i += 4) { p.rechteck(r.x + 3 + i, r.y + 3 + (i % 3), 2, 3, P.steindunkel); } p.rechteck(r.x + 2, r.y + r.h - 4, r.b - 4, 1, P.steindunkel); },
        boot(p, r) { p.polygon([[r.x, r.y + 3], [r.x + r.b, r.y + 3], [r.x + r.b - 6, r.y + r.h], [r.x + 6, r.y + r.h]], [140, 74, 40]); p.rechteck(r.x + 1, r.y + 3, r.b - 2, 2, [180, 110, 60]); p.rechteck(r.x + 18, r.y + 7, 22, 4, P.creme); p.rechteck(r.x + 20, r.y + 8, 18, 1, P.rot); p.rechteck(r.x + 20, r.y + 10, 14, 1, P.rot); },
        pfaehle(p, r) { for (let i = 0; i < 7; i++) { const x = r.x + i * Math.floor(r.b / 7); p.rechteck(x, r.y + (i % 2) * 2, 4, r.h, i === 2 ? [126, 92, 60] : [96, 66, 40]); p.rechteck(x, r.y + (i % 2) * 2, 4, 1, [150, 116, 80]); p.toenen(x, r.y + r.h - 4, 4, 4, P.wasser, 0.5); } },
        rohr(p, r, z) { p.rechteck(r.x, r.y + 4, r.b, 6, P.grau); p.rechteck(r.x, r.y + 4, r.b, 1, P.hellgrau); p.rechteck(r.x + 12, r.y + 3, 4, 8, P.dunkelgrau); if (z.flags.grubeTrocken) { p.rechteck(r.x + 13, r.y + 10, 3, 2, [236, 236, 230]); } },
        grube(p, r, z) {
            p.rechteck(r.x, r.y, r.b, r.h, [110, 80, 50]); p.rechteck(r.x + 3, r.y + 4, r.b - 6, r.h - 4, [70, 50, 34]);
            if (z.flags.grubeTrocken) { for (let i = 0; i < 6; i++) { p.rechteck(r.x + 6 + i * 10, r.y + r.h - 8, 9, 5, i % 2 ? P.tuerkis : [120, 190, 190]); } if (!z.inventar.includes('siegel_judikative')) { p.ellipse(r.x + Math.floor(r.b / 2), r.y + r.h - 10, 3, 2, P.gold); } }
            else { p.verlauf(r.x + 3, r.y + 8, r.b - 6, r.h - 8, [P.wasser, P.tiefwasser]); p.ellipse(r.x + Math.floor(r.b / 2), r.y + r.h - 6, 2, 1, P.ocker); }
        },
        uhr(p, r, z) {
            const cx = r.x + Math.floor(r.b / 2), cy = r.y + Math.floor(r.h / 2), rad = Math.min(r.b, r.h) / 2 - 1;
            p.ellipse(cx, cy, rad + 1, rad + 1, P.anthrazit); p.ellipse(cx, cy, rad, rad, P.creme);
            p.linie(cx, cy, cx, cy - rad + 2, P.schwarz); p.linie(cx, cy, z.flags.abend ? cx + 4 : cx - 3, z.flags.abend ? cy + 3 : cy + 4, P.schwarz);
        },
        schloss(p, r) { p.rechteck(r.x, r.y, r.b, r.h, P.steindunkel); p.rechteck(r.x + 1, r.y + 1, r.b - 2, r.h - 2, P.stein); for (let i = 0; i < 3; i++) { const cx = r.x + 6 + i * Math.floor((r.b - 10) / 2); p.ellipse(cx, r.y + Math.floor(r.h / 2), 5, 5, P.ocker); p.ellipse(cx, r.y + Math.floor(r.h / 2), 3, 3, P.gold); } },
        tor(p, r, z) {
            p.rechteck(r.x - 4, r.y - 4, r.b + 8, r.h + 4, P.steinhell); p.polygon([[r.x - 6, r.y - 4], [r.x + r.b / 2, r.y - 14], [r.x + r.b + 6, r.y - 4]], P.stein);
            p.rechteck(r.x, r.y, r.b, r.h, z.flags.tempelOffen ? P.schwarz : [96, 80, 60]);
            if (!z.flags.tempelOffen) { p.rechteck(r.x + Math.floor(r.b / 2), r.y, 1, r.h, P.steindunkel); for (let i = 0; i < 3; i++) { p.ellipse(r.x + 8 + i * Math.floor((r.b - 16) / 2), r.y + 18, 4, 4, P.anthrazit); } p.ellipse(r.x + Math.floor(r.b / 2), r.y + Math.floor(r.h * 0.6), 2, 3, P.schwarz); }
        },
        platte(p, r) { p.rechteck(r.x, r.y, r.b, r.h, P.steindunkel); p.rechteck(r.x + 1, r.y + 1, r.b - 2, r.h - 2, [180, 150, 100]); p.rechteck(r.x + Math.floor(r.b / 2) - 3, r.y + 3, 6, 4, P.ocker); },
        gitter(p, r) { for (let i = 0; i <= 7; i++) { p.rechteck(r.x + i * Math.floor(r.b / 7), r.y, 3, r.h, P.anthrazit); p.rechteck(r.x + i * Math.floor(r.b / 7), r.y, 1, r.h, P.grau); p.polygon([[r.x + i * Math.floor(r.b / 7) - 1, r.y + r.h], [r.x + i * Math.floor(r.b / 7) + 1, r.y + r.h + 4], [r.x + i * Math.floor(r.b / 7) + 3, r.y + r.h]], P.anthrazit); } for (let j = 0; j < 4; j++) { p.rechteck(r.x, r.y + 6 + j * Math.floor(r.h / 4), r.b + 3, 3, P.dunkelgrau); } },
        tafel(p, r) { p.rechteck(r.x, r.y, r.b, r.h, P.steindunkel); p.rechteck(r.x + 2, r.y + 2, r.b - 4, r.h - 4, P.stein); p.rechteck(r.x + Math.floor(r.b / 2) - 3, r.y + 6, 6, 10, P.gold); p.rechteck(r.x + Math.floor(r.b / 2) - 2, r.y + 8, 4, 2, P.stein); p.rechteck(r.x + Math.floor(r.b / 2) - 2, r.y + 12, 4, 2, P.stein); for (let i = 0; i < 4; i++) { p.rechteck(r.x + 5, r.y + 22 + i * 5, r.b - 10, 1, P.steindunkel); } },
        abgrund(p, r, z) { p.verlauf(r.x, r.y, r.b, r.h, [P.tinte, P.schwarz]); p.rechteck(r.x, r.y, r.b, 2, P.steindunkel); if (z.flags.brueckeDa) { p.rechteck(r.x - 4, r.y + Math.floor(r.h / 2) - 3, r.b + 8, 6, P.sand); for (let i = 0; i < r.b; i += 8) { p.rechteck(r.x + i, r.y + Math.floor(r.h / 2) - 3, 1, 6, P.ocker); } } },
        pokal(p, r) { const cx = r.x + Math.floor(r.b / 2); p.rechteck(cx - 8, r.y + r.h - 6, 17, 6, P.steinhell); p.rechteck(cx - 2, r.y + r.h - 14, 5, 8, P.ocker); p.polygon([[cx - 10, r.y + 4], [cx + 10, r.y + 4], [cx + 6, r.y + r.h - 14], [cx - 6, r.y + r.h - 14]], P.gold); p.toenen(cx + 2, r.y + 4, 8, 14, P.gelb, 0.5); p.ellipse(cx, r.y + 10, 2, 2, P.rot); },
        urne(p, r) { p.rechteck(r.x, r.y + 4, r.b, r.h - 4, [176, 132, 84]); p.rechteck(r.x, r.y + 4, r.b, 2, [200, 156, 104]); p.rechteck(r.x + 8, r.y + 5, Math.floor(r.b / 2) - 4, 1, P.dunkelbraun); for (let i = 0; i < r.h - 6; i += 3) { p.rechteck(r.x + 1, r.y + 7 + i, r.b - 2, 1, [160, 118, 74]); } p.linie(r.x + r.b - 1, r.y + 8, r.x + r.b + 3, r.y + r.h - 4, P.grau); p.rechteck(r.x + r.b + 2, r.y + r.h - 6, 2, 6, P.gelb); },
        person() { /* Sprite */ }, gegner() { /* Sprite */ }
    };

    // ---------- Hintergründe ----------
    const HINTERGRUND = {
        buero(p) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 150, [[226, 206, 170], [206, 182, 142]]);
            for (let x = 0; x < BB; x += 10) { p.toenen(x, 6, 3, 104, [190, 164, 124], 0.5); }
            p.rechteck(0, 0, BB, 6, P.braun); p.rechteck(0, 6, BB, 1, P.hellholz);
            p.rechteck(0, 110, BB, 40, [120, 76, 44]); p.rechteck(0, 110, BB, 2, P.hellholz);
            for (let x = 4; x < BB; x += 34) { p.rechteck(x, 116, 28, 28, [136, 88, 52]); p.toenen(x, 116, 28, 2, P.dunkelbraun, 0.6); p.rechteck(x, 143, 28, 1, P.hellholz); }
            fenster(p, 100, 20, 56, 56, [[96, 66, 130], [200, 96, 110], [244, 150, 80], [250, 200, 120]], grevenSilhouette);
            p.rechteck(92, 14, 6, 72, [150, 40, 40]); p.rechteck(158, 14, 6, 72, [150, 40, 40]); p.toenen(92, 14, 3, 72, P.dunkelrot, 0.6); p.toenen(161, 14, 3, 72, P.dunkelrot, 0.6); p.rechteck(88, 12, 80, 3, P.ocker);
            // Gemälde: Ems-Landschaft
            p.rechteck(232, 16, 50, 30, P.ocker); p.verlauf(234, 18, 46, 26, [[150, 190, 220], [230, 210, 160]]); p.rechteck(234, 34, 46, 4, P.gruen); p.polygon([[234, 44], [252, 36], [280, 38], [280, 44]], P.wasser); p.ellipse(268, 30, 5, 6, P.dunkelgruen);
            // Deckenlampe
            p.rechteck(214, 0, 1, 14, P.anthrazit); p.polygon([[206, 18], [222, 18], [218, 13], [210, 13]], [60, 90, 70]); p.rechteck(209, 18, 10, 1, P.gelb);
            holzboden(p, 150, [150, 98, 56], [118, 74, 42]);
            lichtkegel(p, 214, 19, 150, 10, 60, P.creme, 0.035);
            // Tisch
            p.rechteck(98, 128, 82, 4, P.hellholz); p.rechteck(98, 132, 82, 2, P.braun); p.rechteck(102, 134, 4, 30, P.braun); p.rechteck(172, 134, 4, 30, P.braun); p.toenen(98, 164, 82, 6, P.schwarz, 0.35);
            p.rechteck(150, 120, 12, 8, P.weiss); p.rechteck(151, 118, 10, 2, P.hellgrau); p.rechteck(140, 124, 6, 4, [200, 60, 50]); p.rechteck(141, 122, 4, 2, P.creme);
            // Pflanze
            p.rechteck(298, 150, 14, 14, P.terrakotta); for (let i = 0; i < 9; i++) { p.ellipse(300 + (i % 4) * 4, 132 + i * 2 - (i % 3) * 6, 3, 5, i % 2 ? P.gruen : P.dunkelgruen); }
        },
        archiv(p) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 150, [[150, 160, 156], [118, 128, 126]]);
            for (let x = 0; x < BB; x += 64) { p.rechteck(x + 20, 4, 40, 3, P.eis); p.toenen(x + 14, 7, 52, 16, P.eis, 0.12); }
            for (let reihe = 0; reihe < 2; reihe++) {
                const yy = 18 + reihe * 48;
                for (let x = 0; x < BB; x += 3) { p.rechteck(x, yy, 2, 40, [[150, 40, 40], [40, 70, 140], [200, 180, 80], [60, 110, 70], [210, 210, 200]][(x * 7 + reihe * 3) % 5]); }
                p.rechteck(0, yy + 40, BB, 3, P.dunkelgrau); p.rechteck(0, yy - 2, BB, 2, P.grau);
            }
            p.toenen(0, 18, BB, 90, P.schwarz, 0.18);
            p.rechteck(0, 112, BB, 38, [110, 116, 116]);
            for (let y = 150; y < H; y++) { for (let x = 0; x < BB; x++) { p.setze(x, y, ((Math.floor(x / 10) + Math.floor((y - 150) / 6)) % 2) ? [168, 168, 160] : [146, 146, 140]); } }
            p.toenen(0, 150, BB, 5, P.schwarz, 0.3);
        },
        ems(p) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 70, [[120, 110, 180], [230, 150, 140], [250, 200, 140]]);
            p.ellipse(56, 52, 12, 12, [255, 230, 160]); p.ellipse(56, 52, 9, 9, [255, 244, 200]);
            for (let i = 0; i < 4; i++) { p.ellipse(120 + i * 60, 18 + (i % 2) * 8, 18, 4, [244, 200, 190]); p.toenen(104 + i * 60, 18 + (i % 2) * 8, 32, 4, [210, 150, 160], 0.5); }
            for (let x = 0; x < BB; x += 6) { p.ellipse(x, 68 - (x * 13 % 7), 6, 8, x % 12 ? P.dunkelgruen : [40, 90, 56]); }
            p.rechteck(0, 72, BB, 6, P.moos);
            // Alte Brücke
            p.rechteck(176, 40, 110, 10, P.stein); p.rechteck(176, 40, 110, 2, P.steinhell);
            for (let i = 0; i < 3; i++) { p.rechteck(184 + i * 38, 50, 10, 34, P.stein); p.toenen(184 + i * 38, 50, 3, 34, P.steindunkel, 0.6); }
            p.verlauf(0, 80, BB, 120, [[110, 120, 170], P.wasser, P.tiefwasser]);
            for (let i = 0; i < 3; i++) { p.toenen(184 + i * 38, 84, 10, 24, P.steindunkel, 0.3); }
            p.toenen(48, 84, 18, 60, [255, 220, 160], 0.25);
            // Ufer vorne links mit Steg
            p.polygon([[0, 130], [70, 136], [110, 150], [96, 200], [0, 200]], [86, 112, 56]); p.toenen(0, 150, 100, 50, P.dunkelgruen, 0.3);
            p.rechteck(70, 146, 70, 4, P.holz); p.rechteck(70, 150, 70, 1, P.braun); for (let i = 0; i < 4; i++) { p.rechteck(74 + i * 20, 150, 3, 22, P.braun); }
            for (let i = 0; i < 18; i++) { p.rechteck(4 + i * 5, 120 + (i % 3) * 3, 1, 14, P.moos); p.setze(4 + i * 5, 119 + (i % 3) * 3, P.braun); }
        },
        baustelle(p) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 110, [[60, 70, 120], [200, 120, 110], [246, 184, 120]]);
            // Kran
            p.rechteck(40, 10, 4, 100, P.gelb); p.rechteck(20, 10, 110, 3, P.gelb); for (let i = 0; i < 110; i += 6) { p.linie(20 + i, 10, 23 + i, 13, P.ocker); } p.rechteck(100, 13, 1, 30, P.anthrazit); p.rechteck(97, 43, 7, 4, P.anthrazit);
            p.rechteck(0, 104, BB, 46, [196, 170, 120]); p.sprenkel(0, 104, BB, 46, P.ocker, 0.08, 3);
            // Bauzaun
            for (let x = 0; x < BB; x += 30) { p.rechteck(x, 92, 2, 34, P.grau); } p.rechteck(0, 92, BB, 1, P.grau); p.rechteck(0, 124, BB, 1, P.grau);
            for (let x = 0; x < BB; x += 4) { p.linie(x, 92, x + 8, 124, [170, 170, 180]); }
            for (let x = 0; x < BB; x += 24) { p.rechteck(x, 120, 12, 6, P.rot); p.rechteck(x + 12, 120, 12, 6, P.weiss); }
            // Bauwagen rechts
            p.rechteck(246, 80, 74, 46, [226, 226, 216]); p.rechteck(246, 80, 74, 3, P.grau); p.rechteck(256, 90, 16, 12, P.himmel); p.rechteck(290, 92, 14, 34, [180, 180, 172]); p.rechteck(250, 126, 8, 6, P.anthrazit); p.rechteck(306, 126, 8, 6, P.anthrazit);
            // Sandhaufen, Boden
            p.polygon([[150, 150], [190, 124], [230, 150]], P.sand); p.toenen(190, 124, 40, 26, P.ocker, 0.4);
            p.rechteck(0, 150, BB, 50, [170, 140, 96]); p.sprenkel(0, 150, BB, 50, [140, 110, 70], 0.1, 9);
            p.rechteck(0, 150, BB, 1, [140, 110, 70]);
        },
        schule(p, z) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 70, z.flags.abend ? [[10, 10, 30], [30, 30, 70]] : [[40, 50, 100], [150, 110, 140]]);
            sterne(p, BB, 50, 5); p.ellipse(290, 20, 8, 8, P.eis); p.ellipse(293, 18, 7, 7, z.flags.abend ? [10, 10, 30] : [40, 50, 100]);
            ziegelwand(p, 30, 14, 230, 110, [120, 60, 50], [168, 74, 56], [146, 64, 48], 2);
            p.polygon([[24, 16], [145, 2], [266, 16]], [80, 40, 40]);
            for (let i = 0; i < 6; i++) { const x = 44 + i * 34; p.rechteck(x, 34, 20, 24, P.steinhell); p.rechteck(x + 2, 36, 16, 20, z.flags.abend ? [250, 220, 120] : [80, 100, 140]); p.rechteck(x + 9, 36, 2, 20, P.steinhell); p.rechteck(x, 70, 20, 22, P.steinhell); p.rechteck(x + 2, 72, 16, 18, i === 2 ? [250, 220, 120] : [60, 74, 110]); }
            p.rechteck(110, 100, 26, 4, P.steinhell); p.rechteck(98, 6, 94, 9, P.creme); for (let i = 0; i < 26; i++) { p.rechteck(102 + i * 3, 9, 2, 3, P.dunkelrot); }
            p.verlauf(0, 124, BB, 76, [[80, 84, 96], [64, 66, 78]]);
            // Himmel und Hüpfkästchen, Tischtennisplatte
            for (let i = 0; i < 4; i++) { p.rechteck(40 + i * 14, 168, 12, 10, [220, 220, 230]); p.rechteck(42 + i * 14, 170, 8, 6, [72, 76, 88]); }
            p.rechteck(120, 150, 54, 6, P.dunkelgruen); p.rechteck(146, 146, 2, 4, P.weiss); p.rechteck(124, 156, 3, 14, P.anthrazit); p.rechteck(167, 156, 3, 14, P.anthrazit);
            // Laterne
            p.rechteck(276, 80, 3, 90, P.anthrazit); p.rechteck(270, 76, 15, 5, P.anthrazit); lichtkegel(p, 277, 81, 175, 10, 60, P.gelb, z.flags.abend ? 0.25 : 0.12);
        },
        krypta(p) {
            const BB = B + 2 * RAND;
            ziegelwand(p, 0, 0, BB, 160, [40, 24, 20], [96, 52, 40], [80, 42, 32], 1);
            for (let i = 0; i < 3; i++) { p.ellipse(60 + i * 110, 10, 52, 28, [40, 24, 20]); p.ellipse(60 + i * 110, 12, 46, 24, [70, 40, 32]); }
            p.rechteck(0, 160, BB, 40, [70, 56, 48]); for (let x = 0; x < BB; x += 16) { p.rechteck(x, 160, 1, 40, [50, 40, 34]); } p.rechteck(0, 176, BB, 1, [50, 40, 34]);
            p.toenen(0, 0, BB, 200, P.schwarz, 0.35);
            fackel(p, 110, 60); fackel(p, 290, 60);
            // Treppe links
            for (let i = 0; i < 6; i++) { p.rechteck(0, 110 + i * 9, 30 - i * 4, 9, i % 2 ? P.steindunkel : P.stein); }
            // Spinnweben
            p.linie(BB - 1, 0, BB - 20, 14, P.hellgrau); p.linie(BB - 1, 8, BB - 18, 2, P.hellgrau); p.linie(BB - 10, 0, BB - 10, 10, P.hellgrau);
        },
        tempel(p) {
            const BB = B + 2 * RAND;
            p.verlauf(0, 0, BB, 140, [[150, 112, 70], [220, 180, 120]]);
            for (let x = 10; x < BB; x += 66) { p.rechteck(x, 8, 22, 132, P.steinhell); p.toenen(x + 15, 8, 7, 132, P.steindunkel, 0.6); p.rechteck(x - 3, 4, 28, 5, P.stein); p.rechteck(x - 3, 136, 28, 5, P.stein); for (let y = 16; y < 130; y += 12) { p.rechteck(x + 3, y, 1, 8, P.stein); } }
            // Reliefs: Waage, Buch, Urne
            p.linie(150, 30, 170, 30, P.steindunkel); p.linie(160, 26, 160, 40, P.steindunkel); p.rechteck(150, 31, 4, 1, P.steindunkel); p.rechteck(166, 31, 4, 1, P.steindunkel);
            for (let i = 0; i < 6; i++) { p.setze(BB / 2 + (i - 3) * 2, 1, P.gold); }
            // Boden in Perspektive
            p.rechteck(0, 140, BB, 60, [176, 140, 92]);
            for (let i = -8; i <= 8; i++) { p.linie(BB / 2 + i * 8, 140, BB / 2 + i * 50, 200, [150, 116, 74]); }
            for (let y = 146; y < 200; y += Math.max(3, Math.round((y - 136) / 4))) { p.rechteck(0, y, BB, 1, [150, 116, 74]); }
            lichtkegel(p, 120, 0, 140, 8, 40, P.gelb, 0.18); lichtkegel(p, 230, 0, 140, 8, 40, P.gelb, 0.14);
            fackel(p, 22, 76); fackel(p, 300, 76);
        }
    };

    // Vordergrund (bewegt sich bei der Parallaxe stärker)
    const VORDERGRUND = {
        ems(p) { for (let i = 0; i < 26; i++) { const x = 4 + i * 12, hh = 18 + (i % 4) * 6; p.rechteck(x, H - hh, 2, hh, P.dunkelgruen); p.rechteck(x + 1, H - hh - 4, 1, 6, P.braun); } },
        buero(p) { p.rechteck(0, 188, 26, 12, P.dunkelbraun); p.rechteck(2, 182, 22, 6, P.braun); },
        tempel(p) { p.rechteck(0, 0, 14, H, P.steindunkel); p.rechteck(B + 2 * RAND - 14, 0, 14, H, P.steindunkel); }
    };

    // ---------- Cache und Zeichnen ----------
    let cacheSchluessel = null, cacheBild = null, cacheVorne = null;
    function hintergrund(z, hotspots) {
        const sig = z.szene + '|' + hotspots.map((h) => h.id).join(',') + '|' + ['grubeTrocken', 'abend', 'tempelOffen', 'brueckeDa'].map((f) => (z.flags[f] ? 1 : 0)).join('') + '|' + z.inventar.length + '|' + (z.platten.links || '') + (z.platten.rechts || '');
        if (sig === cacheSchluessel) { return { bild: cacheBild, vorne: cacheVorne }; }
        const p = new Puffer(B + 2 * RAND, H);
        (HINTERGRUND[z.szene] || HINTERGRUND.buero)(p, z);
        for (const h of hotspots) {
            const f = OBJEKT[h.form];
            if (f) { const r = R(h); r.x += RAND; f(p, r, z, h); if (h.form === 'platte' && ((h.id === 'platte_links' && z.platten.links) || (h.id === 'platte_rechts' && z.platten.rechts))) { p.rechteck(r.x + 2, r.y + 1, r.b - 4, r.h - 2, P.gold); } }
        }
        cacheBild = p.nachCanvas();
        const v = new Puffer(B + 2 * RAND, H);
        if (VORDERGRUND[z.szene]) { VORDERGRUND[z.szene](v, z); }
        cacheVorne = v.nachCanvas();
        cacheSchluessel = sig;
        return { bild: cacheBild, vorne: cacheVorne };
    }

    /** Animationen über dem Hintergrund (je Bild neu). t: Zeit in Sekunden. */
    function animation(g, z, t, verschiebung) {
        const s = stift(g), o = verschiebung;
        const flamme = (x, y) => { const f = Math.floor(t * 12) % 3; s.r(x + o, y - 4 - f, 3, 4 + f, P.orange); s.r(x + 1 + o, y - 3 - f, 1, 3, P.gelb); g.globalAlpha = 0.12; g.fillStyle = 'rgb(255,200,100)'; g.beginPath(); g.arc(x + 1 + o, y - 2, 26 + f * 2, 0, Math.PI * 2); g.fill(); g.globalAlpha = 1; };
        if (z.szene === 'buero') {
            if (Math.floor(t * 2) % 2 === 0) { s.p(Math.round(224 * 1) + 26 + o, 92 + 14, P.hellgruen); }
            for (let i = 0; i < 8; i++) { const x = 190 + ((i * 37 + t * 4) % 50), y = 40 + ((i * 53 + t * 6) % 100); s.p(x + o, y, P.creme); }
        } else if (z.szene === 'archiv') {
            for (let i = 0; i < 14; i++) { const x = (i * 23 + t * 3) % B, y = (i * 41 + t * 5) % 150; s.p(x + o, y, P.eis); }
            if (Math.floor(t * 7) % 23 === 0) { g.globalAlpha = 0.2; g.fillStyle = '#000'; g.fillRect(0, 0, B, H); g.globalAlpha = 1; }
        } else if (z.szene === 'ems') {
            for (let i = 0; i < 30; i++) { const x = (i * 47 + t * 10 * (1 + (i % 3))) % (B + 20) - 10, y = 90 + (i * 29) % 100; s.r(x + o, y, 3 + (i % 3), 1, P.hellblau); }
            const ex = 150 + Math.sin(t / 3) * 30; for (let i = 0; i < 3; i++) { s.r(ex + i * 8 + o, 112, 4, 2, P.ocker); s.p(ex + i * 8 + 4 + o, 111, P.dunkelgruen); }
        } else if (z.szene === 'baustelle') {
            if (!z.flags.grubeTrocken) { for (let i = 0; i < 4; i++) { s.r(148 + Math.sin(t * 5 + i) * 2 + o, 116 + ((t * 40 + i * 9) % 22), 1, 2, P.hellblau); } }
            if (Math.floor(t * 2) % 2 === 0) { s.r(60 + o, 88, 3, 3, P.orange); }
        } else if (z.szene === 'schule') {
            if (z.flags.abend) { for (let i = 0; i < 7; i++) { const x = (i * 61 + t * 30) % B, y = 20 + Math.sin(t * 4 + i) * 10 + i * 4; s.r(x + o - 2, y, 2, 1, P.schwarz); s.r(x + o + 1, y, 2, 1, P.schwarz); s.p(x + o, y + (Math.floor(t * 8 + i) % 2), P.schwarz); } }
            if (Math.floor(t * 3) % 5 === 0) { s.p(60 + o, 10, P.weiss); }
        } else if (z.szene === 'krypta') { flamme(111, 60); flamme(291, 60); }
        else if (z.szene === 'tempel') { flamme(23, 76); flamme(301, 76); }
    }

    // ---------- Reisekarte ----------
    let karteBild = null;
    function karte() {
        if (karteBild) { return karteBild; }
        const p = new Puffer(B, H);
        p.verlauf(0, 0, B, H, [[236, 216, 168], [222, 196, 140]]);
        p.sprenkel(0, 0, B, H, [200, 170, 120], 0.08, 4);
        p.toenen(0, 0, B, 8, [170, 130, 80], 0.5); p.toenen(0, H - 8, B, 8, [170, 130, 80], 0.5); p.toenen(0, 0, 8, H, [170, 130, 80], 0.5); p.toenen(B - 8, 0, 8, H, [170, 130, 80], 0.5);
        // Ems
        for (let y = 0; y < H; y++) { const x = 70 + Math.sin(y / 30) * 18 + y * 0.25; p.rechteck(Math.round(x), y, 7, 1, [96, 150, 200]); p.setze(Math.round(x) + 1, y, [140, 190, 230]); }
        // Straßen
        p.linie(0, 92, B, 84, [190, 150, 110]); p.linie(160, 0, 170, H, [190, 150, 110]); p.linie(110, 30, 260, 150, [200, 164, 120]);
        // Häuserblocks der Innenstadt
        for (let i = 0; i < 14; i++) { p.rechteck(130 + (i % 5) * 12, 70 + Math.floor(i / 5) * 12, 8, 7, [200, 120, 90]); }
        p.polygon([[262, 22], [266, 34], [262, 30], [258, 34]], [120, 80, 50]); p.rechteck(261, 34, 2, 6, [120, 80, 50]);
        p.rechteck(262, 16, 1, 5, [120, 80, 50]);
        p.ellipse(290, 170, 12, 12, [200, 170, 120]); p.linie(290, 158, 290, 182, [120, 80, 50]); p.linie(278, 170, 302, 170, [120, 80, 50]);
        p.ellipse(40, 160, 10, 7, [210, 180, 130]);
        karteBild = p.nachCanvas();
        return karteBild;
    }

    // ---------- Minispiele ----------
    function boot(g, sim, t, sprites) {
        const s = stift(g);
        const p = new Puffer(1, 1); void p;
        g.fillStyle = 'rgb(44,98,150)'; g.fillRect(0, 0, B, H);
        g.fillStyle = 'rgb(86,112,56)'; g.fillRect(0, 0, B, 26); g.fillRect(0, H - 14, B, 14);
        for (let i = 0; i < 40; i++) { const x = ((i * 53 - t * 120) % (B + 40) + B + 40) % (B + 40) - 20; s.r(x, 26 + (i * 37) % 160, 6 + (i % 4) * 3, 1, P.hellblau); }
        for (let i = 0; i < 20; i++) { const x = ((i * 31 - t * 60) % (B + 20) + B + 20) % (B + 20) - 10; s.r(x, 6 + (i % 3) * 5, 6, 10, P.dunkelgruen); }
        const spurY = (sp) => 50 + sp * 50;
        const by = spurY(sim.spur);
        s.r(30, by + 8, 40, 6, [140, 74, 40]); s.r(34, by + 14, 32, 3, [110, 58, 30]); s.r(30, by + 8, 40, 1, [180, 110, 60]);
        g.drawImage(sprites.joe, 26, by - 52); g.drawImage(sprites.chani, 44, by - 52);
        for (const h of sim.hindernisse) {
            const x = h.x * B, y = spurY(h.spur);
            if (h.art.indexOf('Ente') === 0) { for (let i = 0; i < 3; i++) { s.r(x + i * 9, y + 6, 6, 4, P.ocker); s.r(x + i * 9 + 4, y + 3, 3, 3, P.dunkelgruen); s.p(x + i * 9 + 7, y + 4, P.orange); } }
            else if (h.art.indexOf('Treib') === 0) { s.r(x, y + 8, 40, 6, [96, 66, 40]); s.r(x + 6, y + 6, 4, 3, [96, 66, 40]); }
            else { s.r(x + 10, y - 6, 3, 18, P.braun); s.r(x, y - 16, 24, 12, P.creme); s.r(x + 3, y - 13, 18, 2, P.rot); s.r(x + 3, y - 9, 12, 2, P.dunkelgrau); }
        }
        const vy = spurY(1) + Math.sin(t * 1.5) * 40;
        s.r(B - 64, vy + 6, 56, 8, P.grau); s.r(B - 64, vy + 4, 56, 2, P.hellgrau); s.r(B - 10, vy + 4, 6, 10, P.anthrazit);
        g.drawImage(sprites.hohlbein, B - 62, vy - 54); g.drawImage(sprites.droehn, B - 44, vy - 56);
    }

    function kugel(g, sim, t, sprites) {
        const s = stift(g);
        g.fillStyle = 'rgb(150,112,70)'; g.fillRect(0, 0, B, H);
        for (let x = 0; x < B; x += 40) { s.r(x, 20, 18, 120, P.steinhell); s.r(x + 13, 20, 5, 120, P.stein); }
        g.fillStyle = 'rgb(120,90,56)'; g.fillRect(0, 150, B, 50);
        for (let x = 0; x < B; x += 20) { s.r(x, 150, 1, 50, [100, 74, 46]); }
        s.r(B - 24, 60, 24, 90, P.schwarz);
        const sx = 12 + sim.pos * (B - 40), kx = 12 + sim.kugel * (B - 40);
        const wackel = Math.sin(t * 40) * 1.5;
        if (sim.lastenrad) { s.r(sx - 18, 140, 44, 4, P.gruen); s.r(sx + 14, 130, 12, 10, P.gruen); for (const rx of [sx - 14, sx + 20]) { g.fillStyle = '#222'; g.beginPath(); g.arc(rx, 146, 6, 0, Math.PI * 2); g.fill(); } }
        g.drawImage(sprites.joe, sx - 20, (sim.lastenrad ? 138 : 150) - 70);
        g.drawImage(sprites.chani, sx - 6, (sim.lastenrad ? 138 : 150) - 70);
        g.fillStyle = 'rgb(130,126,120)'; g.beginPath(); g.arc(kx, 112 + wackel, 38, 0, Math.PI * 2); g.fill();
        g.fillStyle = 'rgb(160,156,150)'; g.beginPath(); g.arc(kx - 12, 98 + wackel, 12, 0, Math.PI * 2); g.fill();
        s.r(kx - 8, 108, 16, 3, P.dunkelgrau); s.r(kx - 1, 101, 3, 17, P.dunkelgrau);
        if (wackel > 0) { s.r(kx - 30, 152, 60, 2, P.steindunkel); }
    }

    wurzel.Szenen = { B, H, RAND, R, hintergrund, animation, karte, boot, kugel };
})(typeof globalThis !== 'undefined' ? globalThis : this);
