// Joe & Chani und der Gral der Demokratie – Darstellung, Bedienung (Maus, Touch, Tastatur), Ton und Speicherplätze.
// Alle Grafik wird hier im Code gezeichnet; keine externen Dateien.
(function () {
    'use strict';
    const S = window.GralSpiel;
    const B = 960, H = 540;
    const leinwand = document.getElementById('leinwand');
    const g = leinwand.getContext('2d');
    const $ = (id) => document.getElementById(id);
    const textbox = $('textbox'), dialogEl = $('dialog'), menueEl = $('menue'), kontextEl = $('kontext'), titelEl = $('titel');
    const inventarEl = $('inventar'), figurenEl = $('figuren'), statusEl = $('status');

    let speicherObjekt = null;
    try { speicherObjekt = window.localStorage; } catch (_) { speicherObjekt = null; }
    const plaetze = S.speicherplaetze(speicherObjekt || { getItem() { throw new Error('kein Speicher'); }, setItem() { throw new Error('kein Speicher'); }, removeItem() { } });

    let z = null;                 // Spielzustand
    let warteschlange = [];       // anzuzeigende Textzeilen
    let danach = null;            // was nach dem Text passiert (Dialog, Minispiel)
    let mini = null;              // laufendes Minispiel
    let pausiert = false;
    let hover = null, auswahl = -1;
    let reise = null;             // Reiseanimation auf der Karte
    let letzterOrt = 'buero';
    let ton = true;
    try { ton = window.localStorage.getItem('fp-spiel-ton') !== 'aus'; } catch (_) { ton = true; }
    let tick = 0;

    // ---------- Ton (WebAudio, erzeugt) ----------
    let audio = null;
    function klang(art) {
        if (!ton) { return; }
        try {
            audio = audio || new (window.AudioContext || window.webkitAudioContext)();
            const t = audio.currentTime;
            const tonfolge = { klick: [[660, 0.05]], erfolg: [[523, 0.1], [659, 0.1], [784, 0.18]], fehler: [[200, 0.15], [150, 0.2]], kartusche: [[90, 0.25]], ende: [[523, 0.15], [659, 0.15], [784, 0.15], [1046, 0.4]] }[art] || [[440, 0.05]];
            let start = t;
            for (const [f, d] of tonfolge) {
                const o = audio.createOscillator(), v = audio.createGain();
                o.type = art === 'kartusche' ? 'sawtooth' : 'square';
                o.frequency.value = f;
                v.gain.setValueAtTime(0.06, start);
                v.gain.exponentialRampToValueAtTime(0.001, start + d);
                o.connect(v).connect(audio.destination);
                o.start(start); o.stop(start + d);
                start += d * 0.9;
            }
        } catch (_) { /* ohne Ton weiter */ }
    }

    // ---------- Zeichnen: Grundformen ----------
    const px = (p) => p * B / 100, py = (p) => p * H / 100;
    function rechteck(x, y, b, h, f) { g.fillStyle = f; g.fillRect(x, y, b, h); }
    function kreis(x, y, r, f) { g.fillStyle = f; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill(); }
    function text(t, x, y, groesse, farbe, ausrichtung) {
        g.fillStyle = farbe || '#fff'; g.font = `${groesse || 16}px "Trebuchet MS", sans-serif`; g.textAlign = ausrichtung || 'left'; g.fillText(t, x, y);
    }
    function verlauf(oben, unten) { const v = g.createLinearGradient(0, 0, 0, H); v.addColorStop(0, oben); v.addColorStop(1, unten); g.fillStyle = v; g.fillRect(0, 0, B, H); }

    function person(x, y, s, farbe, opts) {
        const o = opts || {};
        // Beine, Körper, Kopf
        rechteck(x - 7 * s, y - 22 * s, 5 * s, 22 * s, o.hose || '#2c3e50');
        rechteck(x + 2 * s, y - 22 * s, 5 * s, 22 * s, o.hose || '#2c3e50');
        rechteck(x - 11 * s, y - 52 * s, 22 * s, 32 * s, farbe);
        kreis(x, y - 62 * s, 10 * s, o.haut || '#f1c27d');
        if (o.hut) { rechteck(x - 15 * s, y - 70 * s, 30 * s, 4 * s, '#5d4037'); rechteck(x - 9 * s, y - 80 * s, 18 * s, 11 * s, '#6d4c41'); rechteck(x - 9 * s, y - 72 * s, 18 * s, 2 * s, '#3e2723'); }
        if (o.haare) { rechteck(x - 11 * s, y - 74 * s, 22 * s, 8 * s, o.haare); rechteck(x - 12 * s, y - 70 * s, 5 * s, 22 * s, o.haare); rechteck(x + 7 * s, y - 70 * s, 5 * s, 22 * s, o.haare); }
        if (o.schal) { rechteck(x - 11 * s, y - 52 * s, 22 * s, 5 * s, o.schal); }
        if (o.kartusche) { rechteck(x + 11 * s, y - 44 * s, 14 * s, 5 * s, '#ecf0f1'); rechteck(x + 25 * s, y - 43 * s, 6 * s, 3 * s, '#e67e22'); }
        if (o.handy) { rechteck(x + 11 * s, y - 50 * s, 6 * s, 10 * s, '#111'); rechteck(x + 12 * s, y - 49 * s, 4 * s, 7 * s, '#5dade2'); }
        if (o.krawatte) { rechteck(x - 2 * s, y - 51 * s, 4 * s, 18 * s, o.krawatte); }
        if (o.helm) { g.fillStyle = '#f1c40f'; g.beginPath(); g.arc(x, y - 66 * s, 11 * s, Math.PI, 0); g.fill(); }
        // Augen
        kreis(x - 3.5 * s, y - 63 * s, 1.4 * s, '#222'); kreis(x + 3.5 * s, y - 63 * s, 1.4 * s, '#222');
        if (o.mund !== false) { rechteck(x - 3 * s, y - 58 * s, 6 * s, 1.5 * s, '#7b3f00'); }
    }

    function heldenZeichnen() {
        const joeAktiv = z.figur === 'joe';
        person(px(5), py(97), 1.1, '#8d6e63', { hut: true, kartusche: true, hose: '#34495e' });
        person(px(10.5), py(97), 1.05, '#1abc9c', { haare: '#4a2c2a', schal: '#e74c3c', handy: true, hose: '#2c3e50' });
        const x = joeAktiv ? px(5) : px(10.5);
        g.fillStyle = '#ffd54f'; g.beginPath(); g.moveTo(x - 7, py(97) - 100); g.lineTo(x + 7, py(97) - 100); g.lineTo(x, py(97) - 90); g.fill();
    }

    // ---------- Szenenhintergründe ----------
    const HINTERGRUND = {
        buero() {
            verlauf('#e8dcc4', '#c9b48f');
            rechteck(0, py(70), B, H, '#8b5a2b');
            for (let i = 0; i < B; i += 60) { rechteck(i, py(70), 2, H, '#7a4d24'); }
            rechteck(px(30), py(10), px(18), py(18), '#87ceeb'); rechteck(px(30), py(18.5), px(18), 3, '#fff'); rechteck(px(38.5), py(10), 3, py(18), '#fff');
            rechteck(px(34), py(60), px(32), py(4), '#5d4037'); rechteck(px(36), py(64), 8, py(10), '#4e342e'); rechteck(px(63), py(64), 8, py(10), '#4e342e');
            text('Fraktionsbüro', px(55), py(8), 14, '#5d4037');
        },
        archiv() {
            verlauf('#d5d8dc', '#aab7b8');
            rechteck(0, py(70), B, H, '#7f8c8d');
            for (let x = 30; x < B; x += 140) { rechteck(x, py(5), 6, py(65), '#95a5a6'); }
            g.fillStyle = 'rgba(255,255,255,0.4)'; for (let x = 100; x < B; x += 200) { g.fillRect(x, 0, 80, 6); }
        },
        ems() {
            verlauf('#aed6f1', '#d6eaf8');
            rechteck(0, py(40), B, py(10), '#7dcea0');
            rechteck(0, py(50), B, py(50), '#2e86c1');
            for (let i = 0; i < 12; i++) { rechteck(((i * 97 + tick * 0.6) % B), py(56 + (i % 4) * 9), 40, 2, '#85c1e9'); }
            rechteck(px(55), py(30), px(32), py(6), '#a04000'); rechteck(px(60), py(36), 14, py(16), '#873600'); rechteck(px(82), py(36), 14, py(16), '#873600');
            kreis(px(12), py(12), 26, '#f9e79f');
        },
        baustelle() {
            verlauf('#fdebd0', '#f5cba7');
            rechteck(0, py(66), B, H, '#d4ac0d');
            for (let x = 0; x < B; x += 40) { rechteck(x, py(62), 6, py(8), '#e74c3c'); rechteck(x, py(64), 40, 3, '#ecf0f1'); }
            rechteck(px(78), py(40), px(20), py(26), '#ecf0f1'); text('Bauwagen', px(80), py(46), 13, '#555');
            rechteck(px(20), py(18), px(14), py(8), '#f39c12'); rechteck(px(26), py(8), 8, py(12), '#f39c12');
        },
        schule() {
            verlauf('#5d6d7e', '#212f3c');
            rechteck(0, py(70), B, H, '#566573');
            rechteck(px(30), py(10), px(36), py(30), '#a93226'); for (let x = 32; x < 66; x += 7) { rechteck(px(x), py(16), px(4), py(8), '#f7dc6f'); }
            text('Martin-Luther-Grundschule', px(48), py(9), 14, '#f2f3f4', 'center');
            for (let i = 0; i < 8; i++) { kreis(px(10 + i * 11), py(5 + (i % 3) * 3), 1.5, '#fff'); }
        },
        krypta() {
            verlauf('#1b1b1b', '#3b2f2f');
            for (let y = 0; y < H; y += 26) { for (let x = (y / 26) % 2 * 25; x < B; x += 50) { rechteck(x, y, 48, 24, 'rgba(120,60,40,0.35)'); } }
            const v = g.createRadialGradient(px(40), py(50), 30, px(40), py(50), 420); v.addColorStop(0, 'rgba(255,230,150,0.25)'); v.addColorStop(1, 'rgba(0,0,0,0.6)');
            g.fillStyle = v; g.fillRect(0, 0, B, H);
        },
        tempel() {
            verlauf('#f0d9a0', '#b9935a');
            rechteck(0, py(70), B, H, '#a67c52');
            for (let x = 40; x < B; x += 180) { rechteck(x, py(4), 34, py(66), '#e6c98f'); rechteck(x - 6, py(4), 46, 10, '#d4b06a'); }
            for (const x of [px(8), px(92)]) { rechteck(x, py(30), 6, 30, '#5d4037'); kreis(x + 3, py(28) + Math.sin(tick / 6) * 2, 9, '#f39c12'); kreis(x + 3, py(27), 5, '#f9e79f'); }
            text('GRUNDGESETZ', B / 2, py(3.5) + 10, 18, '#7b5a2a', 'center');
        }
    };

    // ---------- Hotspot-Formen ----------
    function hotspotZeichnen(h, aktiv) {
        const x = px(h.x), y = py(h.y), b = px(h.b), hh = py(h.h);
        switch (h.form) {
            case 'brief': rechteck(x, y, b, hh, '#fdfefe'); g.strokeStyle = '#999'; g.beginPath(); g.moveTo(x, y); g.lineTo(x + b / 2, y + hh / 2); g.lineTo(x + b, y); g.stroke(); break;
            case 'fax': rechteck(x, y + hh * 0.3, b, hh * 0.7, '#bdc3c7'); rechteck(x + b * 0.15, y, b * 0.7, hh * 0.35, '#ecf0f1'); rechteck(x + b * 0.6, y + hh * 0.45, b * 0.3, hh * 0.2, '#2ecc71'); break;
            case 'regal': rechteck(x, y, b, hh, '#6e2c00'); for (let i = 1; i < 5; i++) { rechteck(x, y + hh * i / 5, b, 3, '#4e2000'); for (let j = 0; j < 6; j++) { rechteck(x + 4 + j * (b - 8) / 6, y + hh * (i - 1) / 5 + 6, (b - 8) / 7, hh / 5 - 8, ['#c0392b', '#2980b9', '#27ae60', '#f1c40f', '#8e44ad', '#ecf0f1'][(i + j) % 6]); } } break;
            case 'kasten': rechteck(x, y, b, hh, '#7f8c8d'); rechteck(x + 4, y + 4, b - 8, hh / 3, '#2c3e50'); break;
            case 'garderobe': rechteck(x + b / 2 - 3, y, 6, hh, '#5d4037'); rechteck(x + b * 0.1, y + 10, b * 0.8, hh * 0.45, '#8d6e63'); rechteck(x + b * 0.2, y - 6, b * 0.6, 8, '#6d4c41'); break;
            case 'tuer': rechteck(x, y, b, hh, '#784212'); rechteck(x + 4, y + 4, b - 8, hh - 4, '#935116'); kreis(x + b - 10, y + hh / 2, 4, '#f7dc6f'); break;
            case 'pfeil': g.fillStyle = 'rgba(255,255,255,0.75)'; g.beginPath(); g.moveTo(x, y + hh * 0.3); g.lineTo(x + b * 0.6, y + hh * 0.3); g.lineTo(x + b * 0.6, y + hh * 0.1); g.lineTo(x + b, y + hh * 0.5); g.lineTo(x + b * 0.6, y + hh * 0.9); g.lineTo(x + b * 0.6, y + hh * 0.7); g.lineTo(x, y + hh * 0.7); g.fill(); break;
            case 'person': person(x + b / 2, y + hh, 1, h.farbe || '#999', { helm: h.id === 'bauleiter' }); break;
            case 'gegner': {
                const wackel = Math.sin(tick / 8) * 2;
                person(x + b * 0.3, y + hh + wackel, 1, '#566573', { krawatte: '#2e4053', mund: true });
                person(x + b * 0.75, y + hh - wackel, 1.08, '#4d5656', { krawatte: '#1b2631' });
                text('!!', x + b * 0.5, y - 4, 18, '#c0392b', 'center');
                break;
            }
            case 'bild': rechteck(x - 6, y - 6, b + 12, hh + 12, '#b7950b'); rechteck(x, y, b, hh, '#5d6d7e'); for (let i = 0; i < 5; i++) { kreis(x + b * (0.15 + i * 0.17), y + hh * 0.7, 6, '#f5cba7'); } rechteck(x + b * 0.4, y + 6, b * 0.2, hh * 0.3, '#c0392b'); break;
            case 'inschrift': rechteck(x, y, b, hh, 'rgba(0,0,0,0.15)'); for (let i = 0; i < 4; i++) { rechteck(x + 8, y + 6 + i * (hh - 10) / 4, b - 16, 2, 'rgba(60,40,20,0.6)'); } break;
            case 'boot': g.fillStyle = '#a04000'; g.beginPath(); g.moveTo(x, y); g.lineTo(x + b, y); g.lineTo(x + b * 0.85, y + hh); g.lineTo(x + b * 0.15, y + hh); g.fill(); text('Volkseigentum', x + b / 2, y + hh * 0.7, 11, '#fff', 'center'); break;
            case 'pfaehle': for (let i = 0; i < 7; i++) { rechteck(x + i * b / 7, y, b / 14, hh, i === 2 ? '#7b5b3a' : '#6e4b2a'); } break;
            case 'rohr': rechteck(x, y, b, hh, '#7f8c8d'); for (let i = 0; i < 5; i++) { kreis(x + b * 0.5 + Math.sin(tick / 3 + i) * 6, y + hh + 6 + ((tick * 2 + i * 9) % 30), 3, '#5dade2'); } break;
            case 'grube': rechteck(x, y, b, hh, '#6e2c00'); rechteck(x + 6, y + 6, b - 12, hh - 6, z.flags.grubeTrocken ? '#4e342e' : '#2e86c1'); if (z.flags.grubeTrocken) { kreis(x + b / 2, y + hh * 0.7, 6, '#f1c40f'); } break;
            case 'uhr': kreis(x + b / 2, y + hh / 2, Math.min(b, hh) / 2, '#ecf0f1'); g.strokeStyle = '#111'; g.lineWidth = 3; g.beginPath(); g.moveTo(x + b / 2, y + hh / 2); g.lineTo(x + b / 2, y + hh * 0.15); g.moveTo(x + b / 2, y + hh / 2); g.lineTo(x + b * (z.flags.abend ? 0.7 : 0.3), y + hh / 2); g.stroke(); g.lineWidth = 1; break;
            case 'schloss': rechteck(x, y, b, hh, '#7f8c8d'); for (let i = 0; i < 3; i++) { kreis(x + b * (0.2 + i * 0.3), y + hh / 2, hh * 0.3, '#b7950b'); } break;
            case 'tor': rechteck(x, y, b, hh, '#5d4e37'); for (let i = 0; i < 3; i++) { kreis(x + b * (0.25 + i * 0.25), y + hh * 0.3, 10, '#2c2c2c'); } kreis(x + b / 2, y + hh * 0.6, 5, '#000'); break;
            case 'platte': rechteck(x, y, b, hh, '#8d6e63'); rechteck(x + 3, y + 2, b - 6, hh - 4, (h.id === 'platte_links' ? z.platten.links : z.platten.rechts) ? '#f4d03f' : '#a1887f'); break;
            case 'gitter': for (let i = 0; i <= 6; i++) { rechteck(x + i * b / 6, y, 5, hh, '#424949'); } for (let j = 0; j < 4; j++) { rechteck(x, y + j * hh / 4, b + 5, 5, '#424949'); } break;
            case 'tafel': rechteck(x, y, b, hh, '#7b7d7d'); text('§', x + b / 2, y + hh / 2 + 8, 28, '#f7dc6f', 'center'); break;
            case 'abgrund': rechteck(x, y, b, hh, '#000'); if (z.flags.brueckeDa) { rechteck(x, y + hh / 2 - 6, b, 12, '#a67c52'); } break;
            case 'pokal': rechteck(x + b * 0.3, y + hh * 0.75, b * 0.4, hh * 0.25, '#b7950b'); g.fillStyle = '#f4d03f'; g.beginPath(); g.moveTo(x, y); g.lineTo(x + b, y); g.lineTo(x + b * 0.6, y + hh * 0.75); g.lineTo(x + b * 0.4, y + hh * 0.75); g.fill(); kreis(x + b / 2, y + hh * 0.3, 4, '#e74c3c'); break;
            case 'urne': rechteck(x, y, b, hh, '#c69c6d'); rechteck(x + b * 0.3, y + 3, b * 0.4, 3, '#3e2723'); rechteck(x + b * 0.9, y + hh * 0.3, 2, hh * 0.5, '#555'); rechteck(x + b * 0.88, y + hh * 0.75, 3, 10, '#f1c40f'); break;
            default: rechteck(x, y, b, hh, 'rgba(255,255,255,0.2)');
        }
        if (aktiv) {
            g.strokeStyle = '#ffd54f'; g.lineWidth = 2; g.setLineDash([6, 4]); g.strokeRect(x - 3, y - 3, b + 6, hh + 6); g.setLineDash([]); g.lineWidth = 1;
            const t = h.name; g.font = '15px "Trebuchet MS", sans-serif';
            const w = g.measureText(t).width + 14, lx = Math.min(Math.max(x + b / 2 - w / 2, 4), B - w - 4), ly = Math.max(y - 30, 4);
            rechteck(lx, ly, w, 24, 'rgba(0,0,0,0.75)'); text(t, lx + 7, ly + 17, 15, '#ffd54f');
        }
    }

    // ---------- Karte ----------
    function karteZeichnen() {
        verlauf('#f3e5ab', '#e0c98a');
        g.strokeStyle = 'rgba(120,90,40,0.25)'; for (let i = 0; i < B; i += 48) { g.beginPath(); g.moveTo(i, 0); g.lineTo(i, H); g.stroke(); } for (let i = 0; i < H; i += 48) { g.beginPath(); g.moveTo(0, i); g.lineTo(B, i); g.stroke(); }
        g.strokeStyle = '#5dade2'; g.lineWidth = 14; g.beginPath(); g.moveTo(px(20), 0); g.bezierCurveTo(px(30), py(40), px(45), py(60), px(30), H); g.stroke(); g.lineWidth = 1;
        text('Ems', px(27), py(30), 16, '#2e86c1');
        text('GREVEN', B / 2, py(8), 30, '#7b5a2a', 'center'); text('Karte der Schatzsuche', B / 2, py(13), 14, '#7b5a2a', 'center');
        for (const [id, o] of Object.entries(S.ORTE)) {
            const frei = z.orte.indexOf(id) >= 0;
            kreis(px(o.x), py(o.y), frei ? 10 : 6, frei ? (hover === id ? '#c0392b' : '#7b241c') : 'rgba(0,0,0,0.25)');
            if (frei) { text(o.name, px(o.x) + 14, py(o.y) + 5, 15, '#4a2c0a'); }
        }
        if (reise) {
            const a = S.ORTE[reise.von], b = S.ORTE[reise.nach], t = Math.min(1, reise.t);
            g.strokeStyle = '#c0392b'; g.lineWidth = 4; g.setLineDash([10, 6]); g.beginPath(); g.moveTo(px(a.x), py(a.y));
            g.lineTo(px(a.x + (b.x - a.x) * t), py(a.y + (b.y - a.y) * t)); g.stroke(); g.setLineDash([]); g.lineWidth = 1;
            const fx = px(a.x + (b.x - a.x) * t), fy = py(a.y + (b.y - a.y) * t);
            rechteck(fx - 10, fy - 14, 20, 6, '#5d4037');
        }
        text('Ort antippen, um hinzureisen', B / 2, H - 16, 14, '#7b5a2a', 'center');
    }

    function orteAufKarte() { return Object.entries(S.ORTE).filter(([id]) => z.orte.indexOf(id) >= 0).map(([id, o]) => ({ id, x: px(o.x) - 16, y: py(o.y) - 16, b: 220, h: 32 })); }

    // ---------- Minispiele ----------
    function bootZeichnen() {
        verlauf('#aed6f1', '#2e86c1');
        for (let i = 0; i < 3; i++) { rechteck(0, py(25 + i * 25), B, 2, 'rgba(255,255,255,0.35)'); }
        for (let i = 0; i < 20; i++) { rechteck(((i * 131 - tick * 6) % B + B) % B, py(20 + (i % 9) * 8), 30, 2, '#d6eaf8'); }
        const spurY = (s) => py(25 + s * 25) + py(12.5);
        // eigenes Boot
        const by = spurY(mini.spur);
        g.fillStyle = '#a04000'; g.beginPath(); g.moveTo(px(8), by - 12); g.lineTo(px(19), by - 12); g.lineTo(px(17), by + 10); g.lineTo(px(10), by + 10); g.fill();
        kreis(px(12), by - 22, 7, '#f1c27d'); rechteck(px(11) - 2, by - 33, 18, 5, '#6d4c41'); kreis(px(15.5), by - 22, 7, '#f1c27d'); rechteck(px(15.5) - 7, by - 30, 14, 5, '#4a2c2a');
        for (const h of mini.hindernisse) {
            const x = px(h.x * 100), y = spurY(h.spur);
            if (h.art.indexOf('Ente') === 0) { for (let i = 0; i < 3; i++) { kreis(x + i * 14, y, 6, '#f4d03f'); } }
            else if (h.art.indexOf('Schild') === 0 || h.art.indexOf('Plakat') === 0) { rechteck(x, y - 18, 46, 26, '#ecf0f1'); rechteck(x + 20, y + 8, 4, 10, '#6e4b2a'); text('?!', x + 23, y, 13, '#c0392b', 'center'); }
            else { rechteck(x, y - 4, 60, 10, '#6e4b2a'); }
        }
        // Verfolger
        const vy = spurY(1) + Math.sin(tick / 10) * py(20);
        rechteck(px(88), vy - 10, px(10), 20, '#566573'); text('Hohlbein & Dröhn', px(93), vy - 16, 12, '#fff', 'center');
        text(`Noch ${Math.max(0, Math.ceil(mini.dauer - mini.zeit))} s · Leben ${'♥'.repeat(Math.max(0, mini.leben))}`, 16, 28, 18, '#fff');
        text('↑/↓ oder oben/unten tippen', B - 16, 28, 14, '#fff', 'right');
    }

    function kugelZeichnen() {
        verlauf('#c8a165', '#7b5a2a');
        rechteck(0, py(70), B, py(30), '#5d4037');
        g.fillStyle = 'rgba(0,0,0,0.2)'; for (let x = 0; x < B; x += 80) { g.fillRect(x, py(70), 2, py(30)); }
        rechteck(px(92), py(30), px(8), py(40), '#2c2c2c'); text('Halle', px(96), py(28), 13, '#fff', 'center');
        const sx = px(4 + mini.pos * 86), kx = px(4 + mini.kugel * 86);
        if (mini.lastenrad) { kreis(sx - 14, py(69), 10, '#222'); kreis(sx + 18, py(69), 10, '#222'); rechteck(sx - 18, py(62), 40, 6, '#27ae60'); }
        person(sx - 6, py(70) - (mini.lastenrad ? 10 : 0), 0.9, '#8d6e63', { hut: true });
        person(sx + 10, py(70) - (mini.lastenrad ? 10 : 0), 0.85, '#1abc9c', { haare: '#4a2c2a' });
        kreis(kx, py(70) - 60, 60, '#8e8e8e'); kreis(kx - 18, py(70) - 78, 12, '#a6a6a6');
        text('JA · NEIN', kx, py(70) - 54, 14, '#444', 'center');
        rechteck(16, 16, 300, 14, 'rgba(0,0,0,0.3)'); rechteck(16, 16, 300 * Math.min(1, mini.pos), 14, '#f4d03f');
        text('Tippen / Leertaste / → für Tempo', B - 16, 28, 14, '#fff', 'right');
    }

    // ---------- Zeichenschleife ----------
    function zeichnen() {
        g.clearRect(0, 0, B, H);
        if (!z) { verlauf('#2c1810', '#000'); return; }
        if (mini) { (mini.art === 'boot' ? bootZeichnen : kugelZeichnen)(); return; }
        if (z.szene === 'karte') { karteZeichnen(); return; }
        const s = S.SZENEN[z.szene];
        (HINTERGRUND[s.stimmung] || HINTERGRUND.buero)();
        const hs = S.sichtbareHotspots(z);
        hs.forEach((h, i) => hotspotZeichnen(h, hover === h.id || auswahl === i));
        heldenZeichnen();
        text(s.titel, 16, 26, 18, '#fff');
        if (z.ende) { rechteck(0, 0, B, H, 'rgba(0,0,0,0.45)'); text('ENDE', B / 2, H / 2, 64, '#ffd54f', 'center'); text(`${z.punkte} Punkte · ${Math.round(z.sekunden / 60)} Minuten`, B / 2, H / 2 + 44, 22, '#fff', 'center'); }
    }

    let letzteZeit = performance.now();
    function schleife(jetzt) {
        const dt = Math.min(0.05, (jetzt - letzteZeit) / 1000);
        letzteZeit = jetzt;
        tick++;
        if (z && !pausiert) {
            if (!textbox.hidden || !dialogEl.hidden) { /* Spiel wartet */ } else if (mini) {
                if (mini.art === 'boot') { S.bootSchritt(mini, dt, eingabeBoot); eingabeBoot = null; } else { S.kugelSchritt(mini, dt, eingabeKugel); eingabeKugel = false; }
                if (mini.status !== 'laeuft') { const art = mini.art, ok = mini.status === 'gewonnen'; mini = null; ergebnis(S.minispielEnde(z, art, ok)); klang(ok ? 'erfolg' : 'fehler'); if (!ok) { danach = { minispiel: art }; } }
            }
            if (reise) { reise.t += dt * 1.4; if (reise.t >= 1.2) { const ziel = reise.nach; reise = null; ergebnis(S.reisen(z, ziel)); } }
            if (!z.ende) { z.sekunden += dt; }
        }
        zeichnen();
        requestAnimationFrame(schleife);
    }
    let eingabeBoot = null, eingabeKugel = false;

    // ---------- Text, Dialoge, Ergebnisse ----------
    function zeigeText() {
        if (warteschlange.length === 0) {
            textbox.hidden = true;
            const d = danach; danach = null;
            if (d && d.dialog) { dialogZeigen(d.dialog); }
            if (d && d.minispiel) { mini = d.minispiel === 'boot' ? S.bootNeu(Date.now() % 1000) : S.kugelNeu(z.flags.lastenrad); }
            leisteAktualisieren();
            return;
        }
        textbox.hidden = false;
        textbox.textContent = warteschlange.shift();
        const weiter = document.createElement('span'); weiter.className = 'weiter'; weiter.textContent = ' ▶';
        textbox.appendChild(weiter);
    }

    function ergebnis(e) {
        const vorher = z.szene;
        warteschlange = warteschlange.concat(e.text || []);
        if (e.dialog || e.minispiel) { danach = { dialog: e.dialog, minispiel: e.minispiel }; }
        if (e.szene && e.szene !== vorher) { klang('klick'); }
        if (z.szene !== vorher || e.szene) {
            if (z.szene !== 'karte' && S.SZENEN[z.szene] && e.szene && e.szene !== 'karte' && e.text.indexOf(S.SZENEN[z.szene].einstieg) < 0) { warteschlange.push(S.SZENEN[z.szene].einstieg); }
            if (S.ORTE[vorher]) { letzterOrt = vorher; }
            plaetze.schreiben('auto', z);
        }
        if (e.ende) { klang('ende'); plaetze.schreiben('auto', z); }
        hover = null; auswahl = -1;
        zeigeText();
    }

    function dialogZeigen(art) {
        const d = S.dialogInhalt(z, art);
        if (!d) { return; }
        dialogEl.replaceChildren();
        const f = document.createElement('p'); f.textContent = d.frage; dialogEl.appendChild(f);
        d.optionen.forEach((o, i) => {
            const k = document.createElement('button'); k.type = 'button'; k.textContent = `${i + 1}. ${o.text}`;
            k.addEventListener('click', () => { dialogEl.hidden = true; klang('klick'); ergebnis(S.dialogWahl(z, art, o.id)); });
            dialogEl.appendChild(k);
        });
        dialogEl.hidden = false;
        dialogEl.querySelector('button').focus();
    }

    // ---------- Kontextmenü am Hotspot ----------
    function kontextZeigen(h, x, y) {
        kontextEl.replaceChildren();
        const kopf = document.createElement('div'); kopf.className = 'kopf'; kopf.textContent = h.name; kontextEl.appendChild(kopf);
        const f = S.FIGUREN[z.figur].faehigkeiten;
        const verben = [['ansehen', 'Ansehen'], ['benutzen', 'Benutzen / Nehmen'], ['reden', 'Reden'], ['f1', f[0]], ['f2', f[1]]];
        verben.forEach(([art, t], i) => knopf(`${i + 1}. ${t}`, () => ausfuehren(art, h.id)));
        for (const gid of z.inventar) { knopf(`➜ ${S.GEGENSTAENDE[gid].name}`, () => ausfuehren('gegenstand', h.id, gid)); }
        knopf('Abbrechen', () => { kontextEl.hidden = true; });
        const r = leinwand.getBoundingClientRect(), buehne = $('buehne').getBoundingClientRect();
        kontextEl.hidden = false;
        const kx = Math.min(Math.max(r.left - buehne.left + x * r.width / B, 4), buehne.width - kontextEl.offsetWidth - 4);
        const ky = Math.min(Math.max(r.top - buehne.top + y * r.height / H, 4), buehne.height - kontextEl.offsetHeight - 4);
        kontextEl.style.left = `${kx}px`; kontextEl.style.top = `${ky}px`;
        kontextEl.querySelector('button').focus();
        function knopf(t, fn) { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', () => { kontextEl.hidden = true; fn(); }); kontextEl.appendChild(k); }
    }

    function ausfuehren(art, ziel, gegenstand) {
        const vorherPunkte = z.punkte;
        const e = S.aktion(z, art, ziel, gegenstand);
        if (art === 'f1' && z.figur === 'joe') { klang('kartusche'); } else { klang(z.punkte > vorherPunkte ? 'erfolg' : 'klick'); }
        ergebnis(e);
    }

    // ---------- Leiste: Figuren, Inventar, Status ----------
    function leisteAktualisieren() {
        if (!z) { return; }
        figurenEl.replaceChildren();
        for (const id of ['joe', 'chani']) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'figur' + (z.figur === id ? ' aktiv' : '');
            k.textContent = S.FIGUREN[id].name; k.title = S.FIGUREN[id].beschreibung;
            k.setAttribute('aria-pressed', String(z.figur === id));
            k.addEventListener('click', () => { if (z.figur !== id) { klang('klick'); ergebnis(S.figurWechseln(z)); } });
            figurenEl.appendChild(k);
        }
        inventarEl.replaceChildren();
        for (const gid of z.inventar) {
            const k = document.createElement('button'); k.type = 'button'; k.className = 'ding ' + gid; k.textContent = S.GEGENSTAENDE[gid].name;
            k.addEventListener('click', () => ergebnis({ text: [S.GEGENSTAENDE[gid].text] }));
            inventarEl.appendChild(k);
        }
        statusEl.textContent = `${z.punkte} Punkte · ${S.FIGUREN[z.figur].faehigkeiten.join(' / ')}`;
    }

    // ---------- Eingabe ----------
    function punktAusEreignis(ev) {
        const r = leinwand.getBoundingClientRect();
        return { x: (ev.clientX - r.left) * B / r.width, y: (ev.clientY - r.top) * H / r.height };
    }

    function treffer(p) {
        if (z.szene === 'karte') { const o = orteAufKarte().find((o) => p.x >= o.x && p.x <= o.x + o.b && p.y >= o.y && p.y <= o.y + o.h); return o ? { karte: o.id } : null; }
        const hs = S.sichtbareHotspots(z).slice().reverse();
        const h = hs.find((h) => p.x >= px(h.x) && p.x <= px(h.x + h.b) && p.y >= py(h.y) && p.y <= py(h.y + h.h));
        return h ? { hotspot: h } : null;
    }

    function blockiert() { return !z || pausiert || !textbox.hidden || !dialogEl.hidden || !titelEl.hidden || reise; }

    leinwand.addEventListener('pointermove', (ev) => {
        if (blockiert() || mini) { return; }
        const t = treffer(punktAusEreignis(ev));
        hover = t ? (t.karte || t.hotspot.id) : null;
        leinwand.style.cursor = hover ? 'pointer' : 'default';
    });

    leinwand.addEventListener('pointerdown', (ev) => {
        if (!z || pausiert || !titelEl.hidden) { return; }
        ev.preventDefault();
        if (!textbox.hidden) { zeigeText(); return; }
        if (mini) {
            const p = punktAusEreignis(ev);
            if (mini.art === 'boot') { eingabeBoot = p.y < H / 2 ? 'hoch' : 'runter'; } else { eingabeKugel = true; }
            return;
        }
        if (blockiert()) { return; }
        kontextEl.hidden = true;
        const p = punktAusEreignis(ev), t = treffer(p);
        if (!t) { return; }
        if (t.karte) { reiseStarten(t.karte); return; }
        kontextZeigen(t.hotspot, p.x, p.y);
    });

    textbox.addEventListener('click', () => zeigeText());

    function reiseStarten(ziel) {
        if (ziel === letzterOrt && ziel !== 'buero') { ergebnis(S.reisen(z, ziel)); return; }
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
        if (!dialogEl.hidden) {
            const n = Number(ev.key); const k = dialogEl.querySelectorAll('button');
            if (n >= 1 && n <= k.length) { k[n - 1].click(); }
            return;
        }
        if (!kontextEl.hidden) {
            const n = Number(ev.key); const k = kontextEl.querySelectorAll('button');
            if (n >= 1 && n <= 5) { k[n - 1].click(); }
            return;
        }
        if (ev.key === 'Tab' || ev.key === 'f') { ev.preventDefault(); ergebnis(S.figurWechseln(z)); return; }
        const ziele = z.szene === 'karte' ? orteAufKarte() : S.sichtbareHotspots(z);
        if (ev.key === 'ArrowRight' || ev.key === 'ArrowLeft') {
            ev.preventDefault();
            auswahl = (auswahl + (ev.key === 'ArrowRight' ? 1 : -1) + ziele.length) % ziele.length;
            hover = ziele[auswahl] ? ziele[auswahl].id : null;
        }
        if (ev.key === 'Enter' && auswahl >= 0 && ziele[auswahl]) {
            ev.preventDefault();
            if (z.szene === 'karte') { reiseStarten(ziele[auswahl].id); } else { const h = ziele[auswahl]; kontextZeigen(h, px(h.x + h.b / 2), py(h.y + h.h / 2)); }
        }
    });

    // ---------- Menü, Pause, Speicherplätze ----------
    function pauseUmschalten() {
        pausiert = !pausiert;
        if (pausiert) { menueZeigen(); } else { menueEl.hidden = true; }
    }

    function menueZeigen() {
        menueEl.replaceChildren();
        const h = document.createElement('h2'); h.textContent = 'Pause'; menueEl.appendChild(h);
        knopf('Weiterspielen', () => pauseUmschalten());
        for (const p of ['1', '2', '3']) {
            const info = plaetze.info(p);
            knopf(`Speichern auf Platz ${p}${info ? ` (belegt: ${info.punkte} Punkte)` : ''}`, () => { const ok = plaetze.schreiben(p, z); hinweis(ok ? `Gespeichert auf Platz ${p}.` : 'Speichern nicht möglich (Browser-Speicher gesperrt).'); });
        }
        for (const p of ['auto', '1', '2', '3']) {
            const info = plaetze.info(p);
            if (info) { knopf(`Laden: ${p === 'auto' ? 'Autospeicher' : 'Platz ' + p} (${info.punkte} Punkte)`, () => { const s = plaetze.lesen(p); if (s) { starten(s); } }); }
        }
        knopf(ton ? 'Ton aus' : 'Ton an', () => { ton = !ton; try { window.localStorage.setItem('fp-spiel-ton', ton ? 'an' : 'aus'); } catch (_) { /* egal */ } menueZeigen(); });
        knopf('Neues Spiel', () => starten(S.neuerZustand(), true));
        const zurueck = document.createElement('a'); zurueck.href = '../'; zurueck.textContent = 'Zurück zu FraktionPerfekt'; menueEl.appendChild(zurueck);
        menueEl.hidden = false;
        function knopf(t, fn) { const k = document.createElement('button'); k.type = 'button'; k.textContent = t; k.addEventListener('click', fn); menueEl.appendChild(k); }
    }

    function hinweis(t) { const p = document.createElement('p'); p.className = 'hinweis'; p.textContent = t; menueEl.appendChild(p); }

    function starten(zustand, neu) {
        z = zustand; mini = null; reise = null; pausiert = false; menueEl.hidden = true; titelEl.hidden = true; dialogEl.hidden = true; kontextEl.hidden = true;
        warteschlange = []; danach = null;
        letzterOrt = S.ORTE[z.szene] ? z.szene : 'buero';
        leisteAktualisieren();
        if (neu) {
            ergebnis({ text: [
                'Greven, Montagabend. Die Fraktionssitzung ist vorbei, die Kekse sind alle.',
                'Joe – Fraktionschef, im Hauptberuf Vertreter für Baudichtstoffe – und Chani – Social-Media-Referentin und Kunsthistorikerin – räumen noch auf.',
                'Da liegt ein Umschlag auf dem Tisch. Und draußen fährt ein Wagen vor, aus dem zwei Herren in zu engen Anzügen steigen …',
                '(Tippe auf Dinge im Raum. Unten wechselst du zwischen Joe und Chani – jede Figur hat eigene Fähigkeiten. Esc oder P: Pause.)'
            ] });
        } else {
            ergebnis({ text: [z.szene === 'karte' ? 'Weiter geht’s – auf der Karte.' : `Weiter geht’s: ${S.SZENEN[z.szene].titel}.`] });
        }
    }

    // Titelbildschirm
    $('neu').addEventListener('click', () => { klang('klick'); starten(S.neuerZustand(), true); });
    const weiterKnopf = $('weiter');
    if (plaetze.info('auto')) {
        weiterKnopf.hidden = false;
        weiterKnopf.addEventListener('click', () => { const s = plaetze.lesen('auto'); if (s) { starten(s); } });
    }
    $('pause').addEventListener('click', () => { if (z && titelEl.hidden) { pauseUmschalten(); } });

    // Skalierung: Seitenverhältnis 16:9 halten
    function groesse() {
        const leiste = $('leiste').offsetHeight;
        const verf = { b: window.innerWidth, h: window.innerHeight - leiste };
        const f = Math.min(verf.b / B, verf.h / H);
        $('buehne').style.width = `${Math.floor(B * f)}px`; $('buehne').style.height = `${Math.floor(H * f)}px`;
    }
    window.addEventListener('resize', groesse);
    groesse();
    requestAnimationFrame(schleife);
})();
