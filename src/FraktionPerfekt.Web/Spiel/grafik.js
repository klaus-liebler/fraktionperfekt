// Joe & Chani und der Gral der Demokratie – Grafiken aus assets/ (erzeugt mit tools/Grafik/erzeuge.mjs, ausgeliefert unter grafik/).
// Interne Auflösung 256×144, ganzzahlig hochskaliert. Hier steht nur, WELCHES Bild WO gezeichnet wird (ohne DOM testbar);
// das Laden der Bilder passiert im Browser über laden().
(function (wurzel) {
    'use strict';
    const B = 256, H = 144;
    const BODEN = 136;                      // Fußlinie der Helden
    const FUSS_X = 16, FUSS_Y = 62;         // Fußpunkt im 32×64-Bild

    const PERSONEN = ['joe', 'joe_hut', 'chani', 'christian', 'joanna', 'lore', 'kemper', 'hohlbein', 'droehn'];
    const HINTERGRUENDE = ['buero_zukunftsschmiede_v2', 'buero_zukunftsschmiede_v2_ohne_umschlag', 'rathaus_flur_v2', 'archiv_v2', 'ems_ufer',
        'baustelle_bad', 'baustelle_bad_grube_trocken', 'schulhof_nacht', 'schulflur', 'krypta', 'tempel_eingang', 'tempel_eingang_offen', 'tempel_abgrund',
        'tempel_abgrund_bruecke', 'tempel_finale', 'karte_greven'].map((n) => n + '_256x144');
    const OBJEKTE = ['ruderboot_46x14', 'motorboot_64x24', 'hindernisse_ems_96x24', 'wahlkugel_64x64_x4', 'lastenrad_48x32', 'fledermaeuse_8x6_x4',
        'leck_26x12_x3', 'uhr_zeiger_26x23_x2', 'tor_siegel_46x72', 'bodenplatte_leuchtend_36x9', 'klebemasse_32x6', 'minispiel_ems_512x144', 'minispiel_gang_512x144', 'inventar_icons_16x16'];
    const NAMEN = HINTERGRUENDE.concat(OBJEKTE,
        PERSONEN.map((p) => p + '_spritesheet_32x64'), PERSONEN.map((p) => p + '_posen_32x64'), PERSONEN.map((p) => p + '_portraet_64x64'));

    const ICONS = ['brief', 'stadtplan', 'tagesordnung', 'geschaeftsordnung', 'taschenlampe', 'paddel', 'siegel_legislative', 'siegel_exekutive', 'siegel_judikative', 'tempelschluessel'];
    const AUSDRUCK = { neutral: 0, lacht: 1, erschrocken: 2 };
    const FACKELN = { krypta: [[104, 36], [158, 36], [238, 36]], tempel_eingang: [[40, 46], [214, 46]], tempel_eingang_offen: [[40, 46], [214, 46]] };

    /** Hotspot in Pixeln; ein Hotspot darf je Spielstand eine andere Lage haben (lage(z)). */
    function R(h, z) {
        const l = (z && h.lage && h.lage(z)) || h;
        return { x: Math.round(l.x * B / 100), y: Math.round(l.y * H / 100), b: Math.round(l.b * B / 100), h: Math.round(l.h * H / 100) };
    }

    /** Name des Hintergrundbilds für den Spielstand. */
    function hintergrund(z) {
        const f = z.flags;
        const n = {
            buero: () => (z.inventar.includes('brief') || f.briefGelesen ? 'buero_zukunftsschmiede_v2_ohne_umschlag' : 'buero_zukunftsschmiede_v2'),
            archiv: () => (f.archivOffen ? 'archiv_v2' : 'rathaus_flur_v2'),
            ems: () => 'ems_ufer',
            baustelle: () => (f.grubeTrocken ? 'baustelle_bad_grube_trocken' : 'baustelle_bad'),
            schule: () => 'schulhof_nacht',
            schulflur: () => 'schulflur',
            krypta: () => 'krypta',
            tempel: () => (f.kugelGeschafft ? 'tempel_finale' : f.gitterOffen ? (f.brueckeDa ? 'tempel_abgrund_bruecke' : 'tempel_abgrund') : 'tempel_eingang'),
            karte: () => 'karte_greven'
        }[z.szene];
        return (n ? n() : 'buero_zukunftsschmiede_v2') + '_256x144';
    }

    /** Bildausschnitte über dem Hintergrund (Sprites und Animationen), t: Zeit in Sekunden. */
    function ueberlagerungen(z, t) {
        const f = z.flags, o = [];
        const teil = (bild, i, b, h, x, y) => o.push({ bild, sx: i * b, sy: 0, b, h, x, y });
        if (z.szene === 'ems' && !f.bootGewonnen) { teil('ruderboot_46x14', 0, 46, 14, 144, 93 + (Math.floor(t * 1.5) % 2)); }
        if (z.szene === 'baustelle' && !f.grubeTrocken) { teil('leck_26x12_x3', Math.floor(t * 6) % 3, 26, 12, 118, 76); }
        if (z.szene === 'schule') {
            teil('uhr_zeiger_26x23_x2', f.abend ? 1 : 0, 26, 23, 154, 20);
            if (f.abend) { for (let i = 0; i < 4; i++) { teil('fledermaeuse_8x6_x4', Math.floor(t * 8 + i) % 4, 8, 6, Math.round((i * 67 + t * 26) % (B + 16)) - 8, Math.round(24 + i * 9 + Math.sin(t * 3 + i) * 5)); } }
        }
        if (z.szene === 'krypta' && f.tempelOffen) { teil('tor_siegel_46x72', 0, 46, 72, 179, 26); }
        if (z.szene === 'tempel' && !f.gitterOffen) {
            if (z.platten && z.platten.links) { teil('bodenplatte_leuchtend_36x9', 0, 36, 9, 56, 104); }
            if (z.platten && z.platten.rechts) { teil('bodenplatte_leuchtend_36x9', 0, 36, 9, 159, 104); }
        }
        return o;
    }

    function fackeln(z) { return FACKELN[hintergrund(z).replace('_256x144', '')] || []; }

    /** Bild einer Figur: zustand stehen | gehen | reden | benutzen | erschrocken; seitlich = darf gespiegelt werden. */
    function rahmen(name, zustand, bildNr, blinzeln) {
        const n = PERSONEN.includes(name) ? name : 'joe';
        if (zustand === 'gehen') { return { bild: n + '_spritesheet_32x64', sx: 32 * (1 + (bildNr % 5)), seitlich: true }; }
        if (zustand === 'reden') { return { bild: n + '_posen_32x64', sx: 32 * (bildNr % 2), seitlich: false }; }
        if (zustand === 'benutzen') { return { bild: n + '_posen_32x64', sx: 32 * (2 + (bildNr % 2)), seitlich: true }; }
        if (zustand === 'erschrocken') { return { bild: n + '_posen_32x64', sx: 128, seitlich: false }; }
        return blinzeln ? { bild: n + '_posen_32x64', sx: 160, seitlich: false } : { bild: n + '_spritesheet_32x64', sx: 0, seitlich: false };
    }

    function portraet(name, ausdruck) {
        const n = PERSONEN.includes(name) ? name : 'joe';
        return { bild: n + '_portraet_64x64', sx: 64 * (AUSDRUCK[ausdruck] || 0) };
    }

    function icon(gid) { const i = ICONS.indexOf(gid); return { bild: 'inventar_icons_16x16', sx: 16 * Math.max(0, i) }; }

    // ---------- Stehplätze: keine Figur steht vor einer anderen ----------
    const ABSTAND = 24;                      // Mindestabstand zweier Fußpunkte (Körperbreite ≈ 22 px)
    const RAND_X = 14;
    const HINTEN = 0.65;                     // Maßstab von Figuren im Hintergrund
    const NPC = { christian: 'christian', joanna: 'joanna', lore: 'lore', bauleiter: 'kemper' };
    function npcName(h) { return h.form === 'gegner' ? 'gegner' : NPC[h.id] || null; }

    /** Plätze der Nebenfiguren (Fußpunkt x/y) für den Spielstand; dieselben Plätze zeichnet die Darstellung. */
    function npcPlaetze(z, hotspots) {
        const p = [];
        for (const h of hotspots) {
            const n = npcName(h);
            if (!n) { continue; }
            const r = R(h, z), mitte = r.x + r.b / 2, fuss = Math.min(BODEN - 2, r.y + r.h);
            if (n === 'gegner' && h.hinten && h.hinten(z)) {
                // perspektivisch weiter hinten: verkleinert, dunkler, Fußpunkt höher; Abstand wie im Vordergrund (keine Figur vor einer anderen)
                p.push({ id: h.id, name: 'hohlbein', x: mitte - ABSTAND / 2, y: fuss, skala: HINTEN, hinten: true, klebt: true },
                    { id: h.id, name: 'droehn', x: mitte + ABSTAND / 2, y: fuss - 1, skala: HINTEN, hinten: true, klebt: true });
            }
            else if (n === 'gegner') { p.push({ id: h.id, name: 'hohlbein', x: mitte - ABSTAND / 2, y: fuss }, { id: h.id, name: 'droehn', x: mitte + ABSTAND / 2, y: fuss - 1 }); }
            else { p.push({ id: h.id, name: n, x: mitte, y: fuss }); }
        }
        if (z.szene === 'tempel' && z.flags.kugelGeschafft) {
            p.push({ id: 'finale', name: 'hohlbein', x: z.ende ? 208 : 196, y: BODEN - 4 }, { id: 'finale', name: 'droehn', x: z.ende ? 236 : 222, y: BODEN - 5 });
        }
        return p;
    }

    /** Nächster freier Platz zu wunsch (Abstand ≥ ABSTAND zu allen belegten Plätzen, innerhalb des Bildes). */
    function freiesZiel(wunsch, belegt) {
        const frei = (x) => x >= RAND_X && x <= B - RAND_X && belegt.every((b) => Math.abs(b - x) >= ABSTAND);
        const w = Math.max(RAND_X, Math.min(B - RAND_X, Math.round(wunsch)));
        for (let d = 0; d <= B; d++) { if (frei(w - d)) { return w - d; } if (frei(w + d)) { return w + d; } }
        return w;
    }

    /** Stehplätze für aktive Figur und Begleitung beim Gang zu einem Hotspot (oder zu einem x ohne Hotspot). */
    function standplaetze(z, hotspots, ziel, aktivX, begleitX) {
        const npc = npcPlaetze(z, hotspots).map((p) => p.x);
        let wunsch;
        if (typeof ziel === 'number') { wunsch = ziel; }
        else {
            const r = R(ziel, z), mitte = r.x + r.b / 2;
            const eigene = npcPlaetze(z, [ziel]).map((p) => p.x);
            // Personen spricht man von der Seite an, von der man kommt: daneben stehen, nicht davor
            wunsch = eigene.length ? (aktivX <= mitte ? Math.min(...eigene) - ABSTAND - 2 : Math.max(...eigene) + ABSTAND + 2) : mitte;
        }
        const aktiv = freiesZiel(wunsch, npc);
        const seite = aktiv > begleitX ? -1 : 1;
        const begleit = freiesZiel(aktiv + seite * (ABSTAND + 2), npc.concat([aktiv]));
        return { aktiv, begleit };
    }

    /** Startplätze beim Betreten einer Szene. */
    function startplaetze(z, hotspots) {
        const wunsch = z.szene === 'buero' ? 64 : z.szene === 'tempel' && z.flags.kugelGeschafft ? 116 : 92;
        const npc = npcPlaetze(z, hotspots).map((p) => p.x);
        const joe = freiesZiel(wunsch, npc);
        return { joe, chani: freiesZiel(joe + ABSTAND + 2, npc.concat([joe])) };
    }

    /**
     * Klick in die Szene, während noch ein Text steht: Mit gewähltem Verb oder Gegenstand auf einen Hotspot wird der Text
     * abgeschlossen und die Aktion sofort ausgeführt (kein überflüssiger Zwischenklick); sonst blättert der Klick weiter.
     */
    function klickBeiText(verb, objekt, aufHotspot) { return aufHotspot && (!!objekt || verb !== 'gehe') ? 'ausfuehren' : 'weiter'; }

    // ---------- nur im Browser ----------
    const bilder = {};
    function laden(basis) {
        return Promise.all(NAMEN.map((n) => new Promise((ok, fehler) => {
            const i = new Image();
            i.onload = () => { bilder[n] = i; ok(); };
            i.onerror = () => fehler(new Error('Grafik fehlt: ' + n));
            i.src = (basis || 'grafik/') + n + '.png';
        })));
    }
    function bild(n) { return bilder[n]; }
    /** Ausschnitt als eigenes Canvas (für Porträts und Icons im DOM). */
    function ausschnitt(teil, b, h) {
        const c = document.createElement('canvas'); c.width = b; c.height = h;
        const g = c.getContext('2d'); g.imageSmoothingEnabled = false;
        if (bilder[teil.bild]) { g.drawImage(bilder[teil.bild], teil.sx, 0, b, h, 0, 0, b, h); }
        return c;
    }

    const api = { B, H, BODEN, FUSS_X, FUSS_Y, NAMEN, ICONS, ABSTAND, HINTEN, R, npcName, npcPlaetze, freiesZiel, standplaetze, startplaetze, klickBeiText, hintergrund, ueberlagerungen, fackeln, rahmen, portraet, icon, laden, bild, ausschnitt };
    wurzel.Grafik = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof globalThis !== 'undefined' ? globalThis : this);
