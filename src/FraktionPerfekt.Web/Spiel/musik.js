// Erkennungsmelodie „Joe & Chani und der Gral der Demokratie“ – per WebAudio erzeugt (Chiptune-Blechbläser, Pauke).
// Aufbau: eigene Abenteuer-Fanfare (punktierter Marsch, C-Dur) → als würdiges Zitat die erste Phrase von Joseph Haydns
// Melodie „Gott erhalte Franz den Kaiser“ (1797, gemeinfrei; Melodie der deutschen Nationalhymne) getragen in den Hörnern,
// unverändert und ohne Text → Fanfaren-Schluss. Die Fanfare ist eine eigene Komposition und zitiert keine Filmmusik.
(function (wurzel) {
    'use strict';

    // Notenname → Frequenz (A4 = 440 Hz)
    const HALBTON = { C: -9, D: -7, E: -5, F: -4, G: -2, A: 0, B: 2 };
    function frequenz(name) {
        const m = /^([A-G])(#|b)?(\d)$/.exec(name);
        if (!m) { return 0; }
        const n = HALBTON[m[1]] + (m[2] === '#' ? 1 : m[2] === 'b' ? -1 : 0) + (Number(m[3]) - 4) * 12;
        return 440 * Math.pow(2, n / 12);
    }

    // Dauer in Vierteln. P = Pause.
    const FANFARE_A = [['G4', 0.75], ['G4', 0.25], ['C5', 1], ['E5', 0.75], ['D5', 0.25], ['C5', 1],
        ['D5', 0.75], ['E5', 0.25], ['F5', 1], ['E5', 2]];
    const FANFARE_B = [['A4', 0.75], ['A4', 0.25], ['D5', 1], ['F5', 0.75], ['E5', 0.25], ['D5', 1],
        ['C5', 1], ['G4', 1], ['C5', 2]];
    // Haydn, erste Phrase (in C-Dur): C. D E D | F E D–H C
    const HAYDN = [['C5', 1.5], ['D5', 0.5], ['E5', 1], ['D5', 1], ['F5', 1], ['E5', 1], ['D5', 0.5], ['B4', 0.5], ['C5', 1]];
    const HAYDN_AKKORDE = [[['C3', 'G3', 'E4'], 2], [['G2', 'D3', 'B3'], 2], [['F2', 'C3', 'A3'], 2], [['G2', 'D3', 'B3'], 1], [['C3', 'G3', 'E4'], 1]];
    const SCHLUSS = [['G4', 0.75], ['C5', 0.25], ['E5', 1], ['G5', 1], ['P', 0.25], ['C6', 2.75]];

    /** Das ganze Stück als Liste von Stimmen (für Wiedergabe und Tests). */
    const STUECK = {
        tempo: 104,
        teile: [
            { name: 'Fanfare', tempo: 112, melodie: FANFARE_A.concat(FANFARE_B), klang: 'blech', bass: true },
            { name: 'Haydn-Zitat', tempo: 76, melodie: HAYDN, klang: 'horn', akkorde: HAYDN_AKKORDE },
            { name: 'Schluss', tempo: 112, melodie: SCHLUSS, klang: 'blech', pauke: true }
        ]
    };
    const KURZ = { tempo: 100, teile: [STUECK.teile[1], STUECK.teile[2]] };

    let ctx = null, haupt = null, laeuft = [];
    const PEGEL = 0.55;   // Headroom: viele Stimmen summieren sich, der Kompressor fängt Spitzen ab
    let lautstaerke = 0.5, an = true;
    try { an = wurzel.localStorage ? wurzel.localStorage.getItem('fp-spiel-musik') !== 'aus' : true; lautstaerke = Number(wurzel.localStorage.getItem('fp-spiel-lautstaerke') || 0.5); } catch (_) { /* Standard */ }

    function speichern() {
        try { wurzel.localStorage.setItem('fp-spiel-musik', an ? 'an' : 'aus'); wurzel.localStorage.setItem('fp-spiel-lautstaerke', String(lautstaerke)); } catch (_) { /* egal */ }
    }

    function ton(zeit, f, dauer, art, laut) {
        const o = ctx.createOscillator(), v = ctx.createGain(), filter = ctx.createBiquadFilter();
        o.type = art === 'horn' ? 'triangle' : art === 'bass' ? 'square' : 'sawtooth';
        o.frequency.setValueAtTime(f, zeit);
        if (art === 'blech') { o.frequency.setValueAtTime(f * 0.985, zeit); o.frequency.linearRampToValueAtTime(f, zeit + 0.04); }
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(art === 'horn' ? 1400 : 600, zeit);
        filter.frequency.linearRampToValueAtTime(art === 'horn' ? 1800 : 2600, zeit + 0.05);
        filter.frequency.linearRampToValueAtTime(art === 'horn' ? 1200 : 1500, zeit + Math.max(0.06, dauer));
        const spitze = laut * (art === 'bass' ? 0.12 : art === 'horn' ? 0.22 : 0.16);
        v.gain.setValueAtTime(0.0001, zeit);
        v.gain.linearRampToValueAtTime(spitze, zeit + (art === 'horn' ? 0.06 : 0.02));
        v.gain.setValueAtTime(spitze * 0.85, zeit + Math.max(0.03, dauer * 0.7));
        v.gain.exponentialRampToValueAtTime(0.0001, zeit + dauer + 0.08);
        o.connect(filter).connect(v).connect(haupt);
        o.start(zeit); o.stop(zeit + dauer + 0.1);
        o.onended = () => { o.disconnect(); filter.disconnect(); v.disconnect(); };
        laeuft.push(o);
    }

    function pauke(zeit, laut) {
        const o = ctx.createOscillator(), v = ctx.createGain();
        o.type = 'sine'; o.frequency.setValueAtTime(110, zeit); o.frequency.exponentialRampToValueAtTime(55, zeit + 0.3);
        v.gain.setValueAtTime(0.0001, zeit); v.gain.linearRampToValueAtTime(0.35 * laut, zeit + 0.005); v.gain.exponentialRampToValueAtTime(0.0001, zeit + 0.5);
        o.connect(v).connect(haupt); o.start(zeit); o.stop(zeit + 0.55);
        o.onended = () => { o.disconnect(); v.disconnect(); };
        laeuft.push(o);
    }

    /** Spielt das Stück (oder die Kurzfassung fürs Finale). Gibt die Gesamtdauer in Sekunden zurück. */
    function spielen(kurz) {
        if (!an) { return 0; }
        try {
            ctx = ctx || new (wurzel.AudioContext || wurzel.webkitAudioContext)();
            if (ctx.state === 'suspended') { ctx.resume(); }
            stoppen();
            // Jede Wiedergabe bekommt eine eigene Summe → Kompressor → Ausgang; die alte wird ausgeblendet (kein Knacken).
            const kompressor = ctx.createDynamicsCompressor();
            kompressor.threshold.value = -16; kompressor.knee.value = 10; kompressor.ratio.value = 6; kompressor.attack.value = 0.004; kompressor.release.value = 0.2;
            haupt = ctx.createGain(); haupt.gain.value = lautstaerke * PEGEL; haupt.connect(kompressor).connect(ctx.destination);
            let t = ctx.currentTime + 0.12;
            for (const teil of (kurz ? KURZ : STUECK).teile) {
                const viertel = 60 / teil.tempo;
                let z = t;
                for (const [n, d] of teil.melodie) {
                    if (n !== 'P') {
                        ton(z, frequenz(n), d * viertel * 0.92, teil.klang, 1);
                        if (teil.klang === 'blech') { ton(z, frequenz(n) / 2, d * viertel * 0.9, 'blech', 0.5); }
                    }
                    if (teil.bass && d >= 1) { ton(z, frequenz(n) / 4, viertel * 0.5, 'bass', 1); }
                    z += d * viertel;
                }
                if (teil.akkorde) {
                    let a = t;
                    for (const [noten, d] of teil.akkorde) { for (const n of noten) { ton(a, frequenz(n), d * viertel * 0.97, 'horn', 0.45); } a += d * viertel; }
                }
                if (teil.pauke) { for (let i = 0; i < 4; i++) { pauke(z - 2.75 * viertel + i * viertel * 0.25, 0.7); } pauke(z - 2.75 * viertel + viertel, 1); }
                t = z + 0.15;
            }
            return t - ctx.currentTime;
        } catch (_) { return 0; }
    }

    /** Weich ausblenden (40 ms) statt hart abschneiden, dann die Oszillatoren anhalten und die alte Summe trennen. */
    function stoppen() {
        if (!ctx || !haupt) { laeuft = []; return; }
        const jetzt = ctx.currentTime, alt = haupt, alteStimmen = laeuft;
        alt.gain.cancelScheduledValues(jetzt); alt.gain.setValueAtTime(alt.gain.value, jetzt); alt.gain.linearRampToValueAtTime(0, jetzt + 0.04);
        for (const o of alteStimmen) { try { o.stop(jetzt + 0.05); } catch (_) { /* schon aus */ } }
        setTimeout(() => { try { alt.disconnect(); } catch (_) { /* egal */ } }, 200);
        laeuft = []; haupt = null;
    }

    function setzeAn(wert) { an = !!wert; if (!an) { stoppen(); } speichern(); }
    function setzeLautstaerke(wert) { lautstaerke = Math.min(1, Math.max(0, Number(wert) || 0)); if (haupt && ctx) { haupt.gain.setTargetAtTime(lautstaerke * PEGEL, ctx.currentTime, 0.02); } speichern(); }

    const api = { STUECK, KURZ, frequenz, spielen, stoppen, setzeAn, setzeLautstaerke, get an() { return an; }, get lautstaerke() { return lautstaerke; } };
    wurzel.Musik = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof globalThis !== 'undefined' ? globalThis : this);
