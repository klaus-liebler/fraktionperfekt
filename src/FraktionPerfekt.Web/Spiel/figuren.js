// Figuren als Pixel-Art im Code: Sprites (ca. 58 px, Laufen 6 Bilder, Stehen, Reden, Benutzen), Porträts 96×96 mit Mimik,
// Inventar-Icons. Joe und Chani sind nach Fotos stilisiert (freundlich, wiedererkennbar, ohne Übertreibung).
(function (wurzel) {
    'use strict';
    const { Puffer, stift } = wurzel.Pixel;

    const STIL = {
        joe: {
            haut: [234, 186, 152], hautS: [204, 150, 118], haar: [128, 108, 92], haarD: [96, 80, 68], form: 'licht', brille: true, bart: 'schnurr', bartF: [112, 92, 76],
            ober: [40, 34, 32], oberH: [62, 54, 50], kragen: 'steh', hemd: [122, 142, 164], hose: [50, 52, 64], schuh: [28, 24, 24], tasche: [120, 76, 40], kartusche: true
        },
        chani: {
            haut: [246, 208, 182], hautS: [222, 172, 146], haar: [184, 104, 62], haarD: [140, 72, 42], haarH: [214, 138, 86], form: 'zopf', ohrring: true,
            ober: [44, 52, 78], oberH: [66, 76, 108], kragen: 'bluse', hose: [40, 42, 56], schuh: [36, 28, 30], tasche: [96, 70, 56], augen: [80, 110, 130], lippen: [196, 110, 110]
        },
        rolf: { haut: [230, 180, 150], hautS: [196, 142, 112], haar: [188, 188, 192], haarD: [150, 150, 156], form: 'kurz', bart: 'voll', bartF: [176, 176, 180], ober: [170, 44, 44], oberH: [204, 70, 64], kragen: 'strick', hemd: [220, 220, 210], hose: [70, 70, 80], schuh: [40, 30, 26], brille: true },
        tanja: { haut: [232, 190, 160], hautS: [200, 150, 124], haar: [36, 30, 34], haarD: [20, 16, 20], form: 'kurzfrau', ober: [118, 64, 140], oberH: [146, 88, 168], kragen: 'kapuze', hose: [44, 44, 60], schuh: [30, 30, 30], tasche: [210, 196, 160], ohrring: true },
        mia: { haut: [240, 200, 170], hautS: [210, 160, 132], haar: [214, 176, 96], haarD: [176, 136, 70], form: 'zopf', helm: [80, 170, 80], ober: [60, 130, 70], oberH: [90, 164, 90], kragen: 'jacke', hose: [56, 60, 72], schuh: [70, 50, 40] },
        kemper: { haut: [226, 176, 140], hautS: [194, 140, 108], haar: [90, 60, 40], haarD: [60, 40, 28], form: 'kurzfrau', helm: [246, 204, 60], ober: [70, 80, 100], oberH: [96, 106, 126], weste: [246, 140, 40], kragen: 'jacke', hose: [60, 64, 80], schuh: [60, 44, 30] },
        hohlbein: { haut: [238, 178, 150], hautS: [208, 140, 114], haar: [214, 190, 120], haarD: [176, 150, 90], form: 'scheitel', ober: [110, 112, 120], oberH: [136, 138, 146], kragen: 'anzug', hemd: [236, 236, 240], krawatte: [70, 80, 110], hose: [100, 102, 110], schuh: [24, 22, 22], dick: true },
        droehn: { haut: [230, 184, 156], hautS: [198, 146, 120], haar: [52, 44, 40], haarD: [30, 26, 24], form: 'gegelt', brille: true, ober: [44, 54, 84], oberH: [64, 76, 110], kragen: 'anzug', hemd: [226, 226, 232], krawatte: [140, 40, 40], hose: [44, 54, 84], schuh: [20, 18, 18], gross: true }
    };

    // ---------- Sprites ----------
    const BREITE = 32, HOEHE = 72, FUSS_X = 16, FUSS_Y = 70;
    const cache = new Map();

    /**
     * Sprite als Canvas (32×72, Füße bei 16/70). zustand: stehen | gehen | reden | benutzen; bild: Animationsbild; hut: Joe mit Hut.
     * Blickrichtung rechts; links wird beim Zeichnen gespiegelt.
     */
    function sprite(name, zustand, bild, extras) {
        const e = extras || {};
        const schluessel = `${name}|${zustand}|${bild}|${e.hut ? 1 : 0}|${e.blinzeln ? 1 : 0}`;
        if (cache.has(schluessel)) { return cache.get(schluessel); }
        const c = document.createElement('canvas'); c.width = BREITE; c.height = HOEHE;
        zeichneFigur(stift(c.getContext('2d')), STIL[name], zustand, bild, e);
        cache.set(schluessel, c);
        return c;
    }

    function zeichneFigur(s, st, zustand, bild, e) {
        const X = FUSS_X, Y = FUSS_Y + (st.gross ? 0 : 0);
        const g = st.gross ? 3 : 0;            // größer
        const d = st.dick ? 2 : 0;             // breiter
        const schritt = zustand === 'gehen' ? Math.round(3 * Math.sin((bild % 6) / 6 * Math.PI * 2)) : 0;
        const hub = zustand === 'gehen' && (bild % 3 === 1) ? -1 : 0;
        const atmen = zustand === 'stehen' && (bild % 2 === 1) ? 1 : 0;
        const y0 = Y + hub;

        // Beine (hinteres dunkler)
        const beinOben = y0 - 24 - g;
        s.r(X - 4 - schritt, beinOben, 4, 22 + g, dunkler(st.hose, 0.8));
        s.r(X - 5 - schritt, y0 - 2, 6, 2, st.schuh);
        s.r(X + schritt, beinOben, 4, 22 + g, st.hose);
        s.r(X + schritt, y0 - 2, 6, 2, st.schuh);
        s.r(X + schritt + 1, beinOben + 2, 1, 16, heller(st.hose, 1.15));

        // Rumpf
        const rumpfOben = y0 - 43 - g + atmen;
        const rb = 13 + d;
        s.r(X - 7 - d / 2, rumpfOben, rb, 20 - atmen, st.ober);
        s.r(X + 3 + d / 2, rumpfOben + 1, 2, 18 - atmen, st.oberH);               // Licht von rechts
        s.r(X - 7 - d / 2, rumpfOben + 1, 1, 18, dunkler(st.ober, 0.75));
        if (st.weste) { s.r(X - 6, rumpfOben + 2, 11, 14, st.weste); s.r(X - 6, rumpfOben + 9, 11, 1, [240, 240, 220]); s.r(X - 1, rumpfOben + 2, 1, 14, st.ober); }
        // Kragen
        if (st.kragen === 'steh') { s.r(X - 3, rumpfOben - 2, 6, 3, st.ober); s.r(X - 1, rumpfOben, 3, 4, st.hemd); s.r(X - 2, rumpfOben, 1, 2, heller(st.hemd, 1.2)); s.r(X + 2, rumpfOben, 1, 2, heller(st.hemd, 1.2)); s.r(X, rumpfOben + 1, 1, 14, dunkler(st.ober, 0.7)); }
        if (st.kragen === 'bluse') { s.r(X - 2, rumpfOben, 5, 2, st.oberH); s.p(X, rumpfOben + 2, st.haut); s.r(X, rumpfOben + 4, 1, 1, st.oberH); s.r(X, rumpfOben + 8, 1, 1, st.oberH); s.r(X, rumpfOben + 12, 1, 1, st.oberH); }
        if (st.kragen === 'anzug') { s.r(X - 1, rumpfOben, 3, 8, st.hemd); s.r(X, rumpfOben + 1, 1, 8, st.krawatte); s.p(X - 2, rumpfOben + 1, st.oberH); s.p(X + 2, rumpfOben + 1, st.oberH); }
        if (st.kragen === 'strick') { s.r(X - 1, rumpfOben, 3, 3, st.hemd); for (let i = 0; i < 4; i++) { s.p(X - 4, rumpfOben + 4 + i * 4, st.oberH); } }
        if (st.kragen === 'kapuze') { s.r(X - 5, rumpfOben - 1, 10, 3, st.oberH); s.r(X - 1, rumpfOben + 2, 1, 6, [230, 230, 230]); }
        if (st.kragen === 'jacke') { s.r(X, rumpfOben, 1, 18, dunkler(st.ober, 0.7)); }

        // Taschenriemen und Tasche
        if (st.tasche) {
            for (let i = 0; i < 16; i++) { s.p(X - 5 + Math.round(i * 0.75), rumpfOben + i, dunkler(st.tasche, 0.8)); }
            s.r(X - 9, y0 - 28 - g, 6, 6, st.tasche); s.r(X - 9, y0 - 28 - g, 6, 1, heller(st.tasche, 1.2));
            if (st.kartusche) { s.r(X - 8, y0 - 31 - g, 2, 3, [236, 236, 230]); s.p(X - 8, y0 - 32 - g, [240, 140, 40]); s.r(X - 5, y0 - 30 - g, 2, 2, [236, 236, 230]); }
        }

        // Arme: hinterer (pendelt gegen), vorderer (pendelt mit / streckt beim Benutzen)
        const armOben = rumpfOben + 1;
        s.r(X - 6 - d / 2 + Math.round(schritt / 2), armOben, 3, 14, dunkler(st.ober, 0.7));
        s.r(X - 6 - d / 2 + Math.round(schritt / 2), armOben + 14, 3, 2, st.hautS);
        if (zustand === 'benutzen') {
            s.r(X + 3, armOben + 2, 3, 4, st.ober); s.r(X + 5, armOben + 3, 8, 3, st.ober); s.r(X + 13, armOben + 3, 3, 3, st.haut);
            if (st.kartusche) { s.r(X + 15, armOben + 3, 7, 2, [236, 236, 230]); s.p(X + 22, armOben + 3, [240, 140, 40]); }
            else if (e.handy !== false) { s.r(X + 15, armOben, 3, 5, [24, 24, 30]); s.p(X + 16, armOben + 1, [120, 200, 240]); }
        } else {
            const ax = X + 2 + d / 2 - Math.round(schritt / 2);
            s.r(ax, armOben, 3, 14, st.ober); s.r(ax + 2, armOben + 1, 1, 12, st.oberH); s.r(ax, armOben + 14, 3, 2, st.haut);
        }

        // Hals und Kopf (3/4-Ansicht nach rechts)
        const kopfOben = rumpfOben - 13;
        s.r(X - 1, rumpfOben - 2, 4, 3, st.hautS);
        s.r(X - 5, kopfOben + 1, 10, 10, st.haut);
        s.r(X - 4, kopfOben, 8, 1, st.haut); s.r(X - 4, kopfOben + 11, 8, 1, st.haut);
        s.r(X - 5, kopfOben + 6, 2, 5, st.hautS);                     // Schatten Hinterkopf/Wange
        s.p(X + 5, kopfOben + 6, st.haut); s.p(X + 5, kopfOben + 7, st.hautS); // Nase
        s.r(X - 4, kopfOben + 5, 1, 3, st.hautS);                     // Ohr

        // Haare
        const h = st.haar, hd = st.haarD;
        if (st.form === 'licht') {
            s.r(X - 5, kopfOben + 2, 2, 5, h); s.r(X - 4, kopfOben + 1, 3, 2, hd); s.p(X - 3, kopfOben + 3, h); s.r(X - 2, kopfOben, 3, 1, h); s.p(X - 5, kopfOben + 7, hd);
        } else if (st.form === 'zopf') {
            s.r(X - 5, kopfOben - 1, 10, 3, h); s.r(X - 6, kopfOben, 3, 8, h); s.r(X - 1, kopfOben - 1, 1, 2, st.haarD);
            s.r(X + 2, kopfOben, 3, 2, st.haarH || h); s.r(X + 3, kopfOben + 2, 2, 2, h);
            s.r(X - 9, kopfOben + 3, 3, 3, hd); s.r(X - 10, kopfOben + 6, 3, 8, h); s.r(X - 10, kopfOben + 13, 2, 2, hd);
            if (st.haarH) { s.p(X, kopfOben - 1, st.haarH); s.p(X - 4, kopfOben, st.haarH); s.p(X - 9, kopfOben + 8, st.haarH); }
        } else if (st.form === 'kurz') {
            s.r(X - 5, kopfOben - 1, 10, 3, h); s.r(X - 6, kopfOben, 2, 7, h); s.r(X - 3, kopfOben - 1, 4, 1, hd);
        } else if (st.form === 'kurzfrau') {
            s.r(X - 5, kopfOben - 1, 11, 4, h); s.r(X - 6, kopfOben, 3, 9, h); s.r(X + 3, kopfOben + 2, 2, 2, h);
        } else if (st.form === 'scheitel') {
            s.r(X - 5, kopfOben - 1, 10, 3, h); s.r(X - 6, kopfOben, 2, 5, h); s.r(X + 1, kopfOben - 2, 4, 2, h); s.p(X - 2, kopfOben - 1, hd);
        } else if (st.form === 'gegelt') {
            s.r(X - 5, kopfOben - 2, 10, 4, h); s.r(X - 6, kopfOben, 2, 6, h); s.r(X - 3, kopfOben - 2, 7, 1, [96, 90, 86]);
        }

        // Augen, Brille, Bart, Mund
        const ay = kopfOben + 5;
        const zu = e.blinzeln && zustand !== 'reden';
        if (zu) { s.r(X, ay, 2, 1, st.hautS); s.r(X + 3, ay, 2, 1, st.hautS); }
        else { s.p(X + 1, ay, st.augen || [40, 34, 40]); s.p(X + 4, ay, st.augen || [40, 34, 40]); s.p(X + 1, ay - 2, hd); s.p(X + 4, ay - 2, hd); }
        if (st.brille) { s.p(X, ay, [214, 220, 228]); s.p(X + 2, ay, [214, 220, 228]); s.p(X + 3, ay, [214, 220, 228]); s.p(X + 5, ay, [214, 220, 228]); s.r(X, ay - 1, 6, 1, [190, 196, 206]); }
        if (st.ohrring) { s.p(X - 4, kopfOben + 8, [250, 250, 240]); }
        const mundY = kopfOben + 9;
        if (st.bart === 'schnurr') { s.r(X + 1, mundY - 1, 4, 1, st.bartF); s.p(X + 5, mundY - 1, dunkler(st.bartF, 0.8)); }
        if (st.bart === 'voll') { s.r(X - 3, kopfOben + 7, 8, 5, st.bartF); s.r(X - 2, kopfOben + 12, 6, 1, st.bartF); }
        const offen = zustand === 'reden' && bild % 2 === 0;
        if (offen) { s.r(X + 2, mundY, 3, 2, [96, 30, 30]); } else { s.r(X + 2, mundY, 3, 1, st.lippen || [150, 80, 70]); s.p(X + 5, mundY - 1, st.lippen || [150, 80, 70]); }

        // Helm, Hut
        if (st.helm) { s.r(X - 6, kopfOben - 3, 12, 4, st.helm); s.r(X - 5, kopfOben - 4, 10, 1, st.helm); s.r(X - 7, kopfOben, 14, 1, dunkler(st.helm, 0.8)); s.p(X + 1, kopfOben - 3, heller(st.helm, 1.3)); }
        if (e.hut) {
            s.r(X - 8, kopfOben - 1, 17, 2, [96, 62, 36]); s.r(X - 5, kopfOben - 6, 10, 5, [118, 78, 46]); s.r(X - 5, kopfOben - 3, 10, 1, [58, 36, 22]);
            s.r(X - 4, kopfOben - 7, 8, 1, [118, 78, 46]); s.p(X - 1, kopfOben - 7, [96, 62, 36]); s.r(X + 2, kopfOben - 6, 2, 3, [140, 96, 60]);
        }
    }

    function dunkler(c, f) { return [Math.round(c[0] * f), Math.round(c[1] * f), Math.round(c[2] * f)]; }
    function heller(c, f) { return [Math.min(255, Math.round(c[0] * f)), Math.min(255, Math.round(c[1] * f)), Math.min(255, Math.round(c[2] * f))]; }

    // ---------- Porträts 96×96 (Mimik: neutral, lacht, erschrocken) ----------
    const portraitCache = new Map();

    function portrait(name, ausdruck, extras) {
        const e = extras || {};
        const schluessel = `${name}|${ausdruck}|${e.hut ? 1 : 0}`;
        if (portraitCache.has(schluessel)) { return portraitCache.get(schluessel); }
        const st = STIL[name];
        const p = new Puffer(96, 96);
        p.verlauf(0, 0, 96, 96, name === 'hohlbein' || name === 'droehn' ? [[70, 60, 70], [40, 34, 44]] : [[120, 92, 64], [70, 50, 40]]);
        // Schultern und Kleidung
        p.ellipse(48, 104, 44 + (st.dick ? 4 : 0), 26, st.ober);
        p.ellipse(60, 100, 18, 14, st.oberH);
        if (st.weste) { p.ellipse(48, 106, 30, 22, st.weste); p.rechteck(20, 92, 56, 2, [240, 240, 220]); }
        if (st.kragen === 'steh') { p.polygon([[34, 76], [62, 76], [60, 86], [36, 86]], st.ober); p.polygon([[40, 80], [56, 80], [48, 92]], st.hemd); p.polygon([[40, 80], [46, 80], [44, 88]], heller(st.hemd, 1.15)); p.polygon([[50, 80], [56, 80], [52, 88]], heller(st.hemd, 1.15)); }
        if (st.kragen === 'bluse') { p.polygon([[36, 80], [48, 84], [46, 90], [34, 86]], st.oberH); p.polygon([[60, 80], [48, 84], [50, 90], [62, 86]], st.oberH); p.rechteck(47, 86, 2, 10, dunkler(st.ober, 0.7)); }
        if (st.kragen === 'anzug') { p.polygon([[40, 78], [56, 78], [48, 96]], st.hemd); p.polygon([[46, 82], [50, 82], [51, 96], [45, 96]], st.krawatte); }
        if (st.kragen === 'strick' || st.kragen === 'kapuze' || st.kragen === 'jacke') { p.ellipse(48, 80, 14, 5, st.oberH); }
        if (st.tasche) { p.linie(22, 82, 70, 96, dunkler(st.tasche, 0.8)); p.linie(23, 82, 71, 96, st.tasche); }
        // Hals
        p.rechteck(40, 62, 16, 16, st.hautS); p.rechteck(42, 62, 12, 12, st.haut);
        // Kopf (Zopf-Frisur: Haar hinter dem Kopf zuerst)
        if (st.form === 'zopf') { p.ellipse(48, 36, 25, 26, st.haarD); p.ellipse(48, 33, 24, 22, st.haar); }
        p.ellipse(48, 42, 21, 27, st.haut);
        p.toenen(27, 20, 9, 48, st.hautS, 0.5);
        p.ellipse(27, 46, 4, 7, st.hautS); p.ellipse(69, 46, 4, 7, st.hautS);
        // Haare
        const h = st.haar, hd = st.haarD;
        if (st.form === 'licht') {
            p.ellipse(28, 34, 5, 12, h); p.ellipse(68, 34, 5, 12, h); p.ellipse(48, 18, 15, 5, st.haut);
            p.sprenkel(30, 14, 36, 8, h, 0.18, 7); p.toenen(26, 24, 6, 18, hd, 0.4); p.toenen(66, 24, 6, 18, hd, 0.4);
        } else if (st.form === 'zopf') {
            // Mittelscheitel, seitlich nach hinten genommen, Stirn frei
            p.polygon([[48, 14], [38, 14], [29, 20], [25, 32], [25, 50], [29, 42], [32, 30], [38, 22], [47, 19]], h);
            p.polygon([[48, 14], [58, 14], [67, 20], [71, 32], [71, 50], [67, 42], [64, 30], [58, 22], [49, 19]], h);
            p.ellipse(48, 15, 12, 3, h);
            p.linie(48, 13, 48, 19, hd);
            p.toenen(36, 14, 10, 8, st.haarH || h, 0.5); p.toenen(56, 15, 8, 6, st.haarH || h, 0.35);
            p.linie(30, 22, 26, 44, hd); p.linie(66, 22, 70, 44, hd);
        } else if (st.form === 'kurz' || st.form === 'scheitel' || st.form === 'gegelt' || st.form === 'kurzfrau') {
            p.ellipse(48, 22, 22, 12, h); p.ellipse(28, 32, 5, 10, h); p.ellipse(68, 32, 5, 10, h);
            if (st.form === 'scheitel') { p.linie(36, 12, 40, 24, hd); p.ellipse(58, 18, 12, 6, heller(h, 1.1)); }
            if (st.form === 'gegelt') { for (let i = 0; i < 6; i++) { p.linie(32 + i * 6, 12, 36 + i * 6, 24, hd); } }
            if (st.form === 'kurzfrau') { p.ellipse(30, 40, 5, 14, h); p.ellipse(66, 40, 5, 14, h); }
            p.ellipse(48, 30, 17, 5, st.haut);
        }
        if (st.helm) { p.ellipse(48, 20, 25, 14, st.helm); p.rechteck(22, 26, 52, 3, dunkler(st.helm, 0.8)); p.ellipse(40, 14, 6, 3, heller(st.helm, 1.25)); }
        if (e.hut) {
            p.ellipse(48, 22, 36, 6, [96, 62, 36]); p.rechteck(30, 4, 36, 16, [118, 78, 46]); p.ellipse(48, 5, 18, 3, [100, 66, 40]);
            p.rechteck(30, 14, 36, 4, [58, 36, 22]); p.toenen(54, 4, 12, 10, [140, 96, 60], 0.5);
        }
        // Augenbrauen und Augen je Ausdruck
        const brauenHoch = ausdruck === 'erschrocken' ? -4 : ausdruck === 'lacht' ? -1 : 0;
        const braue = st.lippen ? 1 : 2;
        p.rechteck(34, 33 + brauenHoch, 10, braue, hd); p.rechteck(52, 33 + brauenHoch, 10, braue, hd);
        if (ausdruck === 'lacht') {
            p.linie(35, 41, 39, 39, [40, 30, 30]); p.linie(39, 39, 43, 41, [40, 30, 30]);
            p.linie(53, 41, 57, 39, [40, 30, 30]); p.linie(57, 39, 61, 41, [40, 30, 30]);
        } else {
            const gross = ausdruck === 'erschrocken' ? 1 : 0;
            for (const ax of [39, 57]) {
                p.rechteck(ax - 4, 38 - gross, 9, 5 + gross * 2, [248, 246, 240]); p.rechteck(ax - 3, 37 - gross, 7, 1, [248, 246, 240]); p.rechteck(ax - 3, 43 + gross, 7, 1, [248, 246, 240]);
                p.rechteck(ax - 1, 38 - gross, 3, 5 + gross * 2, st.augen || [96, 78, 60]);
                p.rechteck(ax, 39, 1, 3, [24, 18, 18]); p.setze(ax - 1, 38 - gross, [255, 255, 255]);
                p.rechteck(ax - 4, 37 - gross, 9, 1, st.lippen ? dunkler(st.haarD, 0.7) : st.hautS);
            }
        }
        if (st.brille) {
            const gf = [206, 212, 222];
            p.linie(31, 36, 46, 36, gf); p.linie(31, 44, 46, 44, gf); p.linie(31, 36, 31, 44, gf); p.linie(46, 36, 46, 44, gf);
            p.linie(50, 36, 65, 36, gf); p.linie(50, 44, 65, 44, gf); p.linie(50, 36, 50, 44, gf); p.linie(65, 36, 65, 44, gf);
            p.linie(46, 38, 50, 38, gf); p.setze(33, 37, [255, 255, 255]); p.setze(52, 37, [255, 255, 255]);
        }
        // Nase
        p.linie(48, 42, 46, 52, st.hautS); p.linie(46, 52, 50, 53, st.hautS);
        // Bart und Mund
        const mundY = 60;
        if (st.bart === 'voll') { p.ellipse(48, 60, 18, 12, st.bartF); p.ellipse(48, 54, 10, 3, st.bartF); }
        if (ausdruck === 'lacht') { p.ellipse(48, mundY, 7, 4, [110, 30, 34]); p.rechteck(42, mundY - 3, 13, 2, [250, 250, 245]); }
        else if (ausdruck === 'erschrocken') { p.ellipse(48, mundY + 1, 3, 4, [90, 24, 28]); }
        else { p.linie(42, mundY, 54, mundY, st.lippen || [150, 76, 70]); p.setze(41, mundY - 1, st.lippen || [150, 76, 70]); p.setze(55, mundY - 1, st.lippen || [150, 76, 70]); }
        if (st.bart === 'schnurr') { p.ellipse(48, 56, 9, 2, st.bartF); p.toenen(40, 55, 16, 3, dunkler(st.bartF, 0.8), 0.4); }
        if (st.ohrring) { p.ellipse(27, 54, 1, 1, [252, 250, 244]); p.ellipse(69, 54, 1, 1, [252, 250, 244]); }
        if (st.lippen && ausdruck !== 'erschrocken') { p.toenen(36, 50, 6, 4, [236, 160, 150], 0.35); p.toenen(56, 50, 6, 4, [236, 160, 150], 0.35); }
        const c = p.nachCanvas();
        portraitCache.set(schluessel, c);
        return c;
    }

    // ---------- Inventar-Icons 16×16 ----------
    const iconCache = new Map();
    function icon(id) {
        if (iconCache.has(id)) { return iconCache.get(id); }
        const p = new Puffer(16, 16);
        const zeichne = {
            brief() { p.rechteck(1, 4, 14, 9, [244, 240, 228]); p.linie(1, 4, 8, 9, [160, 150, 140]); p.linie(14, 4, 8, 9, [160, 150, 140]); p.ellipse(8, 9, 1, 1, [180, 40, 40]); },
            stadtplan() { p.rechteck(1, 2, 14, 12, [236, 226, 196]); p.linie(3, 3, 6, 13, [80, 130, 200]); p.linie(7, 6, 14, 9, [150, 120, 90]); p.ellipse(10, 7, 2, 2, [200, 40, 40]); p.toenen(1, 2, 14, 12, [200, 190, 160], 0.3); },
            tagesordnung() { p.rechteck(3, 1, 10, 14, [250, 250, 244]); for (let i = 0; i < 5; i++) { p.rechteck(5, 3 + i * 2, 6, 1, [120, 120, 130]); } },
            geschaeftsordnung() { p.rechteck(3, 2, 10, 12, [40, 70, 130]); p.rechteck(3, 2, 2, 12, [24, 44, 90]); p.rechteck(7, 5, 4, 1, [240, 210, 100]); p.rechteck(12, 3, 1, 10, [240, 236, 220]); },
            taschenlampe() { p.rechteck(2, 6, 9, 4, [60, 60, 70]); p.rechteck(11, 5, 3, 6, [180, 180, 190]); p.toenen(14, 4, 2, 8, [255, 240, 150], 0.7); },
            paddel() { p.linie(2, 14, 11, 5, [150, 100, 60]); p.ellipse(12, 4, 3, 2, [130, 86, 50]); },
            siegel_legislative() { siegel([60, 90, 160]); p.rechteck(5, 6, 6, 4, [250, 246, 230]); p.linie(8, 6, 8, 9, [120, 110, 100]); },
            siegel_exekutive() { siegel([60, 130, 80]); p.ellipse(7, 8, 2, 2, [250, 230, 140]); p.rechteck(9, 8, 3, 1, [250, 230, 140]); },
            siegel_judikative() { siegel([150, 60, 60]); p.linie(4, 6, 12, 6, [250, 230, 140]); p.linie(8, 5, 8, 11, [250, 230, 140]); p.rechteck(4, 7, 2, 1, [250, 230, 140]); p.rechteck(10, 7, 2, 1, [250, 230, 140]); },
            tempelschluessel() { p.ellipse(5, 8, 3, 3, [214, 170, 60]); p.ellipse(5, 8, 1, 1, [40, 30, 20]); p.rechteck(8, 7, 7, 2, [214, 170, 60]); p.rechteck(12, 9, 1, 2, [214, 170, 60]); p.rechteck(14, 9, 1, 3, [214, 170, 60]); }
        };
        function siegel(f) { p.ellipse(8, 8, 7, 7, [200, 160, 60]); p.ellipse(8, 8, 6, 6, f); }
        (zeichne[id] || (() => p.rechteck(2, 2, 12, 12, [200, 0, 200])))();
        const c = p.nachCanvas();
        iconCache.set(id, c);
        return c;
    }

    wurzel.Figuren = { STIL, sprite, portrait, icon, BREITE, HOEHE, FUSS_X, FUSS_Y };
})(typeof globalThis !== 'undefined' ? globalThis : this);
