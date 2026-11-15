// Joe & Chani und der Gral der Demokratie – Darstellung im Stil der Point-and-Click-Adventures der frühen 90er:
// 320×200 Pixel-Art (ganzzahlig hochskaliert), Verbleiste, Satzzeile, Inventar mit Icons, laufende Figuren, Porträts mit
// Mimik, Dialogauswahl, Irisblende. Tippen = Standardaktion, langes Drücken = Verbmenü.
(function () {
    'use strict';
    const S = window.GralSpiel, F = window.Figuren, SZ = window.Szenen;
    const B = SZ.B, H = SZ.H, SKALA = 3;
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

    let z = null, warteschlange = [], danach = null, mini = null, pausiert = false, hover = null, auswahl = -1, reise = null, letzterOrt = 'buero';
    let verb = 'gehe', objekt = null;          // gewähltes Verb und Gegenstand („Benutze X mit …“)
    let blende = null;                          // { t, ziel } Irisblende
    let zeit = 0;
    const held = { joe: { x: 40, ziel: 40, blick: 1, schritt: 0 }, chani: { x: 58, ziel: 58, blick: 1, schritt: 0 } };
    let laufAuftrag = null;                     // wird nach Ankunft ausgeführt
    let sprecher = null;                        // für Sprechanimation
    /** Kopie eines (gecachten) Canvas für das DOM – ein Canvas kann nur an einer Stelle hängen. */
    function kopie(c) { const k = document.createElement('canvas'); k.width = c.width; k.height = c.height; k.getContext('2d').drawImage(c, 0, 0); return k; }
    let ton = true;
    try { ton = window.localStorage.getItem('fp-spiel-ton') !== 'aus'; } catch (_) { ton = true; }

    // ---------- Ton ----------
    let audio = null;
    function klang(art) {
        if (!ton) { return; }
        try {
            audio = audio || new (window.AudioContext || window.webkitAudioContext)();
            const t = audio.currentTime;
            const folge = { klick: [[660, 0.04]], schritt: [[110, 0.02]], erfolg: [[523, 0.09], [659, 0.09], [784, 0.16]], fehler: [[220, 0.12], [165, 0.18]], kartusche: [[80, 0.2], [60, 0.15]], ende: [[523, 0.14], [659, 0.14], [784, 0.14], [1046, 0.4]], blende: [[300, 0.06], [200, 0.08]] }[art] || [[440, 0.05]];
            let start = t;
            for (const [f, d] of folge) {
                const o = audio.createOscillator(), v = audio.createGain();
                o.type = art === 'kartusche' ? 'sawtooth' : 'square'; o.frequency.value = f;
                v.gain.setValueAtTime(art === 'schritt' ? 0.015 : 0.05, start); v.gain.exponentialRampToValueAtTime(0.001, start + d);
                o.connect(v).connect(audio.destination); o.start(start); o.stop(start + d); start += d * 0.9;
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
    const BODEN = 196;
    function heldenZiel(h) { const r = SZ.R(h); return Math.max(18, Math.min(B - 18, r.x + r.b / 2)); }

    function npcName(h) {
        if (h.form === 'gegner') { return 'gegner'; }
        return { rolf: 'rolf', tanja: 'tanja', mia: 'mia', bauleiter: 'kemper' }[h.id] || null;
    }

    // ---------- Zeichnen ----------
    function zeichneSzene(dt) {
        const hs = S.sichtbareHotspots(z);
        const aktiv = held[z.figur];
        const parallaxe = Math.round(((aktiv.x - B / 2) / (B / 2)) * SZ.RAND * -0.5);
        const { bild, vorne } = SZ.hintergrund(z, hs);
        t2.drawImage(bild, -SZ.RAND + parallaxe, 0);
        SZ.animation(t2, z, zeit, parallaxe);
        // Personen (NPC) als Sprites
        const figuren = [];
        for (const h of hs) {
            const n = npcName(h);
            if (!n) { continue; }
            const r = SZ.R(h);
            const fy = Math.min(BODEN - 4, r.y + r.h + 2);
            const redet = sprecher === h.id && !textbox.hidden;
            if (n === 'gegner') {
                figuren.push({ y: fy, name: 'hohlbein', x: r.x + r.b * 0.3, zustand: redet ? 'reden' : 'stehen', blick: -1 });
                figuren.push({ y: fy - 1, name: 'droehn', x: r.x + r.b * 0.75, zustand: redet ? 'reden' : 'stehen', blick: -1 });
            } else {
                figuren.push({ y: fy, name: n, x: r.x + r.b / 2, zustand: redet ? 'reden' : 'stehen', blick: r.x + r.b / 2 > held.joe.x ? -1 : 1 });
            }
        }
        if (z.szene === 'tempel' && z.flags.kugelGeschafft) {
            const wackel = Math.floor(zeit * 3) % 2;
            figuren.push({ y: 186, name: 'hohlbein', x: 112, zustand: z.ende ? 'benutzen' : 'reden', blick: 1 });
            figuren.push({ y: 184 - wackel, name: 'droehn', x: z.ende ? 178 : 148, zustand: z.ende ? 'benutzen' : 'stehen', blick: 1 });
        }
        for (const n of ['joe', 'chani']) {
            const hh = held[n];
            const geht = Math.abs(hh.ziel - hh.x) > 0.5;
            const istSprecher = sprecher === n && !textbox.hidden;
            const zustand = geht ? 'gehen' : (istSprecher ? 'reden' : (benutzenBis > zeit && n === z.figur ? 'benutzen' : 'stehen'));
            figuren.push({ y: BODEN, name: n, x: hh.x, zustand, blick: hh.blick, held: true });
        }
        figuren.sort((a, b) => a.y - b.y);
        for (const f of figuren) {
            const bildNr = f.zustand === 'gehen' ? Math.floor(zeit * 10) % 6 : f.zustand === 'reden' ? Math.floor(zeit * 8) % 2 : Math.floor(zeit * 1.3) % 2;
            const blinzeln = (Math.floor(zeit * 10) + f.name.length * 7) % 37 === 0;
            const spr = F.sprite(f.name, f.zustand, bildNr, { hut: f.name === 'joe' && z.inventar.includes('taschenlampe'), blinzeln });
            const x = Math.round(f.x - F.FUSS_X), y = Math.round(f.y - F.FUSS_Y);
            // Schatten
            t2.globalAlpha = 0.3; t2.fillStyle = '#000'; t2.beginPath(); t2.ellipse(Math.round(f.x), f.y - 1, 9, 2, 0, 0, Math.PI * 2); t2.fill(); t2.globalAlpha = 1;
            if (f.blick < 0) { t2.save(); t2.translate(Math.round(f.x) * 2, 0); t2.scale(-1, 1); t2.drawImage(spr, x, y); t2.restore(); } else { t2.drawImage(spr, x, y); }
            if (f.held && f.name === z.figur && !mini) { t2.fillStyle = 'rgb(255,214,79)'; t2.fillRect(Math.round(f.x) - 1, y - 2, 3, 2); t2.fillRect(Math.round(f.x), y, 1, 1); }
        }
        t2.drawImage(vorne, -SZ.RAND + parallaxe * 2, 0);
        // Hover-Rahmen (dezent)
        const ziel = hs.find((h) => h.id === hover);
        if (ziel && textbox.hidden) { const r = SZ.R(ziel); t2.strokeStyle = 'rgba(255,214,79,0.6)'; t2.setLineDash([2, 2]); t2.strokeRect(r.x + 0.5, r.y + 0.5, r.b - 1, r.h - 1); t2.setLineDash([]); }
        void dt;
    }

    function zeichneKarte() {
        t2.drawImage(SZ.karte(), 0, 0);
        const s = window.Pixel.stift(t2);
        for (const [id, o] of Object.entries(S.ORTE)) {
            const frei = z.orte.includes(id), x = Math.round(o.x * B / 100), y = Math.round(o.y * H / 100);
            if (!frei) { s.r(x - 1, y - 1, 3, 3, [150, 120, 90]); continue; }
            s.r(x - 3, y - 3, 7, 7, [60, 30, 20]); s.r(x - 2, y - 2, 5, 5, hover === id ? [250, 80, 60] : [190, 40, 34]);
        }
        if (reise) {
            const a = S.ORTE[reise.von], b = S.ORTE[reise.nach], t = Math.min(1, reise.t);
            const ax = a.x * B / 100, ay = a.y * H / 100, bx = b.x * B / 100, by = b.y * H / 100;
            const n = Math.floor(t * 40);
            for (let i = 0; i < n; i++) { if (i % 3 === 2) { continue; } const u = i / 40; s.r(ax + (bx - ax) * u, ay + (by - ay) * u - Math.sin(u * Math.PI) * 12, 2, 2, [200, 30, 30]); }
            const u = t; s.r(ax + (bx - ax) * u - 3, ay + (by - ay) * u - Math.sin(u * Math.PI) * 12 - 6, 7, 3, [118, 78, 46]); s.r(ax + (bx - ax) * u - 2, ay + (by - ay) * u - Math.sin(u * Math.PI) * 12 - 9, 5, 3, [118, 78, 46]);
        }
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
            if (mini.art === 'boot') { textMitSchatten(`Noch ${Math.max(0, Math.ceil(mini.dauer - mini.zeit))} s   Leben ${'♥'.repeat(Math.max(0, mini.leben))}`, 16, 34, 26, '#ffe08a'); textMitSchatten('↑/↓ oder oben/unten tippen', B * SKALA - 16, 34, 20, '#fff', 'right'); }
            else { textMitSchatten('Tippen / Leertaste / → für Tempo!', B * SKALA - 16, 34, 22, '#fff', 'right'); g.fillStyle = 'rgba(0,0,0,0.4)'; g.fillRect(16, 16, 300, 18); g.fillStyle = '#f4d03f'; g.fillRect(16, 16, 300 * Math.min(1, mini.pos), 18); }
            return;
        }
        if (z.szene === 'karte') {
            textMitSchatten('GREVEN', B * SKALA / 2, 54, 40, '#6b3e1a', 'center');
            for (const [id, o] of Object.entries(S.ORTE)) { if (z.orte.includes(id)) { textMitSchatten(o.name, o.x * B / 100 * SKALA + 16, o.y * H / 100 * SKALA + 8, 20, hover === id ? '#c0392b' : '#3b2410'); } }
            textMitSchatten('Ort antippen, um hinzureisen', B * SKALA / 2, H * SKALA - 20, 18, '#3b2410', 'center');
        } else {
            textMitSchatten(S.SZENEN[z.szene].titel, 14, 32, 22, '#fff');
            if (hover && textbox.hidden) {
                const hs = S.sichtbareHotspots(z).find((h) => h.id === hover);
                if (hs) { const r = SZ.R(hs); textMitSchatten(hs.name, (r.x + r.b / 2) * SKALA, Math.max(28, r.y * SKALA - 8), 20, '#ffe08a', 'center'); }
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
                if (mini.status !== 'laeuft') { const art = mini.art, ok = mini.status === 'gewonnen'; mini = null; klang(ok ? 'erfolg' : 'fehler'); ergebnis(S.minispielEnde(z, art, ok)); if (!ok) { danach = { minispiel: art }; } }
            }
            if (reise) { reise.t += dt * 0.9; if (reise.t >= 1.15) { const ziel = reise.nach; reise = null; szenenwechsel(() => { const v = z.szene; ergebnis(S.reisen(z, ziel), v); }); } }
            laufen(dt);
        }
        if (z) {
            if (mini) {
                const sp = { joe: F.sprite('joe', 'stehen', 0, { hut: true }), chani: F.sprite('chani', mini.art === 'kugel' ? 'gehen' : 'stehen', Math.floor(zeit * 10) % 6), hohlbein: F.sprite('hohlbein', 'reden', Math.floor(zeit * 6) % 2), droehn: F.sprite('droehn', 'reden', Math.floor(zeit * 6 + 1) % 2) };
                if (mini.art === 'kugel') { sp.joe = F.sprite('joe', 'gehen', Math.floor(zeit * 10) % 6, { hut: true }); }
                (mini.art === 'boot' ? SZ.boot : SZ.kugel)(t2, mini, zeit, sp);
            } else if (z.szene === 'karte') { zeichneKarte(); } else { zeichneSzene(dt); }
            hochskalieren(); overlay();
            satzEl.textContent = z.szene === 'karte' || mini ? '' : satz();
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

    function gehe(x, danachTun) {
        const a = held[z.figur], b = held[z.figur === 'joe' ? 'chani' : 'joe'];
        a.ziel = Math.max(14, Math.min(B - 14, x));
        const seite = a.ziel > b.x ? -1 : 1;
        b.ziel = Math.max(14, Math.min(B - 14, a.ziel + seite * 24));
        laufAuftrag = danachTun || null;
    }

    // ---------- Text, Porträts, Dialoge ----------
    const SPRECHER = [['Joe', 'joe'], ['Chani', 'chani'], ['Rolf', 'rolf'], ['Tanja', 'tanja'], ['Mia', 'mia'], ['Kemper', 'kemper'], ['Frau Kemper', 'kemper'], ['Hohlbein', 'hohlbein'], ['Dröhn', 'droehn']];
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
            const d = danach; danach = null;
            if (d && d.dialog) { dialogZeigen(d.dialog); }
            if (d && d.gespraech) { gespraechZeigen(d.gespraech); }
            if (d && d.minispiel) { mini = d.minispiel === 'boot' ? S.bootNeu(Date.now() % 1000) : S.kugelNeu(z.flags.lastenrad); }
            leisteAktualisieren();
            return;
        }
        const zeile = warteschlange.shift();
        const wer = sprecherAus(zeile);
        sprecher = wer === 'hohlbein' || wer === 'droehn' ? 'gegner' : (['rolf', 'tanja', 'mia'].includes(wer) ? wer : wer === 'kemper' ? 'bauleiter' : wer);
        portraitEl.replaceChildren();
        if (wer) { const bild = kopie(F.portrait(wer, ausdruck(zeile), { hut: wer === 'joe' && z.inventar.includes('taschenlampe') })); bild.className = 'portraitbild'; portraitEl.appendChild(bild); portraitEl.hidden = false; } else { portraitEl.hidden = true; }
        textEl.textContent = zeile;
        textbox.hidden = false;
    }

    function ergebnis(e, vorherSzene) {
        const vorher = vorherSzene === undefined ? z.szene : vorherSzene;
        warteschlange = warteschlange.concat(e.text || []);
        if (e.dialog || e.minispiel) { danach = { dialog: e.dialog, minispiel: e.minispiel }; }
        if (z.szene !== vorher) {
            if (z.szene !== 'karte' && S.SZENEN[z.szene] && !(e.text || []).includes(S.SZENEN[z.szene].einstieg)) { warteschlange.push(S.SZENEN[z.szene].einstieg); }
            if (S.ORTE[vorher]) { letzterOrt = vorher; }
            held.joe.x = held.joe.ziel = 30; held.chani.x = held.chani.ziel = 48; held.joe.blick = held.chani.blick = 1;
            plaetze.schreiben('auto', z);
        }
        if (e.ende) { klang('ende'); plaetze.schreiben('auto', z); }
        hover = null; auswahl = -1;
        zeigeText();
    }

    function szenenwechsel(fn) { klang('blende'); blende = { t: 0, gewechselt: false, wechsel: fn }; }

    function dialogZeigen(art) {
        const d = S.dialogInhalt(z, art);
        if (!d) { return; }
        auswahlListe(d.frage, d.optionen.map((o) => ({ text: o.text, tun: () => { klang('klick'); ergebnis(S.dialogWahl(z, art, o.id)); } })));
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
        const vorher = z.punkte, szene = z.szene;
        const e = S.aktion(z, art, h.id, gegenstand);
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
        gehe(heldenZiel(h), () => {
            held[z.figur].blick = (SZ.R(h).x + SZ.R(h).b / 2) >= held[z.figur].x ? 1 : -1;
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
        [['ansehen', 'Schau an'], ['benutzen', 'Nimm / Benutze'], ['reden', 'Rede mit'], ['f1', f[0]], ['f2', f[1]]].forEach(([art, t], i) => knopf(`${i + 1}. ${t}`, () => gehe(heldenZiel(h), () => (art === 'reden' && npcName(h) ? gespraechZeigen(h) : handlung(art, h)))));
        for (const gid of z.inventar) { knopf(`➜ ${S.GEGENSTAENDE[gid].name}`, () => gehe(heldenZiel(h), () => handlung('gegenstand', h, gid))); }
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
            k.addEventListener('click', () => { verb = v; objekt = null; klang('klick'); leisteAktualisieren(); });
            verbenEl.appendChild(k);
        }
        figurenEl.replaceChildren();
        for (const id of ['joe', 'chani']) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'figur' + (z.figur === id ? ' aktiv' : ''); k.title = S.FIGUREN[id].beschreibung; k.setAttribute('aria-pressed', String(z.figur === id));
            const bild = kopie(F.portrait(id, 'neutral', { hut: id === 'joe' && z.inventar.includes('taschenlampe') })); bild.className = 'mini';
            k.append(bild, document.createTextNode(S.FIGUREN[id].name));
            k.addEventListener('click', () => { if (z.figur !== id && textbox.hidden) { klang('klick'); ergebnis(S.figurWechseln(z)); } });
            figurenEl.appendChild(k);
        }
        inventarEl.replaceChildren();
        for (const gid of z.inventar) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'ding' + (objekt === gid ? ' aktiv' : ''); k.title = S.GEGENSTAENDE[gid].name; k.setAttribute('aria-label', S.GEGENSTAENDE[gid].name);
            const bild = kopie(F.icon(gid)); bild.className = 'icon'; k.appendChild(bild);
            k.addEventListener('click', () => {
                if (!textbox.hidden) { return; }
                if (verb === 'benutze' || verb === 'gib') { objekt = gid; leisteAktualisieren(); return; }
                ergebnis({ text: [`${S.GEGENSTAENDE[gid].name}: ${S.GEGENSTAENDE[gid].text}`] });
            });
            inventarEl.appendChild(k);
        }
        punkteEl.textContent = `${z.punkte} Punkte`;
    }

    // ---------- Eingabe ----------
    function punktAus(ev) { const r = leinwand.getBoundingClientRect(); return { x: (ev.clientX - r.left) * B / r.width, y: (ev.clientY - r.top) * H / r.height }; }
    function treffer(p) {
        if (z.szene === 'karte') { const o = Object.entries(S.ORTE).find(([id, o]) => z.orte.includes(id) && Math.abs(p.x - o.x * B / 100) < 10 && Math.abs(p.y - o.y * H / 100) < 8); return o ? { karte: o[0] } : null; }
        const h = S.sichtbareHotspots(z).slice().reverse().find((h) => { const r = SZ.R(h); return p.x >= r.x && p.x <= r.x + r.b && p.y >= r.y && p.y <= r.y + r.h; });
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
        if (!textbox.hidden) { zeigeText(); return; }
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
        if (ev.key === 'Escape' || ev.key === 'p' || ev.key === 'P') { ev.preventDefault(); pauseUmschalten(); return; }
        if (pausiert) { return; }
        if (!textbox.hidden && (ev.key === 'Enter' || ev.key === ' ')) { ev.preventDefault(); zeigeText(); return; }
        if (mini) {
            if (mini.art === 'boot' && (ev.key === 'ArrowUp' || ev.key === 'w')) { eingabeBoot = 'hoch'; ev.preventDefault(); }
            if (mini.art === 'boot' && (ev.key === 'ArrowDown' || ev.key === 's')) { eingabeBoot = 'runter'; ev.preventDefault(); }
            if (mini.art === 'kugel' && (ev.key === ' ' || ev.key === 'ArrowRight' || ev.key === 'd')) { eingabeKugel = true; ev.preventDefault(); }
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
        knopf(ton ? 'Ton aus' : 'Ton an', () => { ton = !ton; try { window.localStorage.setItem('fp-spiel-ton', ton ? 'an' : 'aus'); } catch (_) { /* egal */ } menueZeigen(); });
        knopf('Neues Spiel', () => starten(S.neuerZustand(), true));
        const zurueck = document.createElement('a'); zurueck.href = '../'; zurueck.textContent = 'Zurück zu FraktionPerfekt'; menueEl.appendChild(zurueck);
        menueEl.hidden = false;
        function knopf(t, fn) { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', fn); menueEl.appendChild(k); }
    }
    function hinweis(t) { const p = document.createElement('p'); p.className = 'hinweis'; p.textContent = t; menueEl.appendChild(p); }

    function starten(zustand, neu) {
        z = zustand; mini = null; reise = null; blende = null; pausiert = false; laufAuftrag = null;
        menueEl.hidden = true; titelEl.hidden = true; dialogEl.hidden = true; kontextEl.hidden = true;
        warteschlange = []; danach = null; verb = 'gehe'; objekt = null;
        held.joe.x = held.joe.ziel = 30; held.chani.x = held.chani.ziel = 48;
        letzterOrt = S.ORTE[z.szene] ? z.szene : 'buero';
        $('leiste').hidden = false;
        leisteAktualisieren();
        if (neu) {
            ergebnis({ text: [
                'Greven, Montagabend. Die Fraktionssitzung ist vorbei, die Kekse sind alle.',
                'Joe: „Chani, hast du den Umschlag auf dem Tisch gesehen? Ohne Absender.“',
                'Chani: „Und draußen steigen zwei Herren in zu engen Anzügen aus einem Wagen … Das gefällt mir nicht.“',
                '(Antippen = gehen und die naheliegende Aktion. Verben unten wählen, Gegenstände mit „Benutze“ oder „Gib“ kombinieren. Langes Drücken öffnet alle Aktionen. Esc: Pause.)'
            ] });
        } else {
            ergebnis({ text: [z.szene === 'karte' ? 'Weiter geht’s – auf der Karte.' : `Weiter geht’s: ${S.SZENEN[z.szene].titel}.`] });
        }
    }

    $('neu').addEventListener('click', () => { klang('klick'); starten(S.neuerZustand(), true); });
    const weiterKnopf = $('weiter');
    if (plaetze.info('auto')) { weiterKnopf.hidden = false; weiterKnopf.addEventListener('click', () => { const s = plaetze.lesen('auto'); if (s) { starten(s); } }); }
    $('pause').addEventListener('click', () => { if (z && titelEl.hidden) { pauseUmschalten(); } });

    // Titelbild: beide Porträts
    $('titelbilder').append(kopie(F.portrait('joe', 'lacht', { hut: true })), kopie(F.portrait('chani', 'lacht')));

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
    requestAnimationFrame(schleife);
})();
