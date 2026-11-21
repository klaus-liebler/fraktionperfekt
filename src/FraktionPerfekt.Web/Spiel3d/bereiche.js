// Joe & Chani 3D – Schauplätze. Eine Szene der Spiellogik (logik.js) besteht in 3D aus mehreren begehbaren Bereichen:
//   buero  → „Vor der Zukunftsschmiede“ (Marktstraße, Außen) + „Zukunftsschmiede“ (Büro, innen)
//   archiv → „Vor dem Rathaus“ (Außen, Chris vor der Tür) + „Rathausflur“ + „Rathausarchiv“ (Untergeschoss)
// Jeder Bereich liefert: scene, frei(x, z), hoehe(x, z), kamera(p), spawn, hotspots und Nebenfiguren (npcs).
// Hotspot-Einträge: { id } = Hotspot der Spiellogik; { tor, spawn, name } = Wechsel in einen anderen Bereich; { deko, name, text } = nur Ansehen.
import * as THREE from './three_min.js';
import * as innen from './innen.js';
import { emsDeich, baustelle, radbruecke } from './akt3.js';
import { schulhof, schulflurBereich, kryptaBereich, tempelHalleBereich, tempelFinaleBereich } from './akt45.js';

export const SZENE_ZU_BEREICHEN = {
    buero: ['buero_innen', 'schmiede_aussen'], archiv: ['rathaus_aussen', 'rathaus_flur', 'archiv_innen'], ems: ['ems_deich'], baustelle: ['baustelle', 'radbruecke'],
    schule: ['schulhof'], schulflur: ['schulflur'], krypta: ['krypta'], tempel: ['tempel_halle', 'tempel_finale']
};
/** Bereich, in dem eine Szene der Spiellogik beginnt (abhängig vom Spielstand). */
export function startBereich(z, szene) {
    if (szene === 'baustelle') { return z.inventar.includes('siegel_exekutive') ? 'baustelle' : 'radbruecke'; }
    if (szene === 'tempel') { return z.flags.kugelGeschafft ? 'tempel_finale' : 'tempel_halle'; }
    return SZENE_ZU_BEREICHEN[szene] ? SZENE_ZU_BEREICHEN[szene][0] : null;
}
export const TITEL = { buero_innen: 'Zukunftsschmiede', schmiede_aussen: 'Vor der Zukunftsschmiede', rathaus_aussen: 'Vor dem Rathaus', rathaus_flur: 'Rathausflur', archiv_innen: 'Rathausarchiv', ems_deich: 'Emsdeich am Kanuverleih', baustelle: 'Baustelle am Hallenbad', radbruecke: 'Radbrücke beim Hallenbad', schulhof: 'Hof der Martin-Luther-Grundschule', schulflur: 'Schulflur', krypta: 'Krypta unter der Schule', tempel_halle: 'Tempel des Grundgesetzes', tempel_finale: 'Kammer des Grals' };

const laden = (name) => fetch(name).then((r) => { if (!r.ok) { throw new Error('Daten fehlen: ' + name); } return r.json(); });
const zuXZ = ([e, n]) => [e, -n];

/** Unsichtbarer Klickkörper als Kind eines dünnen Objekts (Wegweiser, Ständer), damit es leicht zu treffen ist. */
function greifbar(obj, w, h, d, y = h / 2) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({ visible: false }));
    m.position.y = y; obj.add(m); return obj;
}

/** Unsichtbarer Klickkörper. */
function klickKoerper(scene, w, h, d, x, y, z, rot = 0) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({ visible: false }));
    m.position.set(x, y, z); m.rotation.y = rot; scene.add(m); return m;
}

function imDreieck(footprints, e, n) {
    const z = -n;
    for (const [a, b, c] of footprints) {
        const d1 = (e - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (z - b[1]);
        const d2 = (e - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (z - c[1]);
        const d3 = (e - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (z - a[1]);
        if (!((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0))) { return true; }
    }
    return false;
}
function aussenFrei(w, zentrum, radius, extra) {
    return (x, z) => {
        const e = x, n = -z;
        if (Math.hypot(e - zentrum[0], n - zentrum[1]) > radius) { return false; }
        for (const [be, bn, r] of w.blockers) { if (Math.hypot(e - be, n - bn) < r) { return false; } }
        for (const [be, bn, r] of extra) { if (Math.hypot(e - be, n - bn) < r) { return false; } }
        for (const [de, dn] of [[0, 0], [.3, 0], [-.3, 0], [0, .3], [0, -.3]]) { if (imDreieck(w.footprints, e + de, n + dn)) { return false; } }
        return true;
    };
}

// ======================================================================
async function schmiedeAussen(renderer) {
    const { baue } = await import('./welt_markt.js');
    const w = baue(renderer, await laden('welt_markt.json'));
    const { scene, F1, F2, FN, ST, E1, E2, EN, M, box } = w;
    const fw = (t, d) => [F1[0] + (F2[0] - F1[0]) * t + FN[0] * d, F1[1] + (F2[1] - F1[1]) * t + FN[1] * d];
    const FL = Math.hypot(F2[0] - F1[0], F2[1] - F1[1]);
    // Briefkasten der Fraktion am rechten Pfeiler neben der Tür
    const tuer = scene.getObjectByName('door');
    const front = tuer.parent;
    const kasten = new THREE.Group(); kasten.name = 'briefkasten';
    kasten.add(box(.38, .48, .16, new THREE.MeshStandardMaterial({ color: 0x2b4a7a, roughness: .45, metalness: .3 })));
    const schlitz = box(.26, .025, .02, M.dark); schlitz.position.set(0, .13, .085); kasten.add(schlitz);
    const schild = new THREE.Mesh(new THREE.PlaneGeometry(.26, .08), new THREE.MeshStandardMaterial({ map: w.canvasTex(128, 40, (g, cw, ch) => { g.fillStyle = '#fff'; g.fillRect(0, 0, cw, ch); g.fillStyle = '#1a2a4a'; g.font = 'bold 22px Arial'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('Fraktion', cw / 2, ch / 2); }) }));
    schild.position.set(0, -.05, .085); kasten.add(schild);
    kasten.position.set(FL / 2 - .25, 1.15, .22); front.add(kasten);
    // Wegweiser zu den Orten (führt zur Reisekarte)
    const weg = new THREE.Group(); weg.name = 'wegweiser';
    weg.add(new THREE.Mesh(new THREE.CylinderGeometry(.05, .06, 3, 10), new THREE.MeshStandardMaterial({ color: 0x3a3d40, metalness: .5, roughness: .4 })));
    weg.children[0].position.y = 1.5;
    [['Rathaus', 2.6, .3], ['Ems', 2.25, -.5], ['Hallenbad', 1.9, 2.4]].forEach(([t, y, r]) => {
        const tex = w.canvasTex(256, 56, (g, cw, ch) => { g.fillStyle = '#f4f2ea'; g.fillRect(0, 0, cw, ch); g.strokeStyle = '#2a2a2a'; g.lineWidth = 4; g.strokeRect(2, 2, cw - 4, ch - 4); g.fillStyle = '#2a2a2a'; g.font = 'bold 30px Arial'; g.textBaseline = 'middle'; g.fillText(t + '  →', 16, ch / 2 + 2); });
        const p = new THREE.Mesh(new THREE.BoxGeometry(1.0, .24, .03), [M.metal, M.metal, M.metal, M.metal, new THREE.MeshStandardMaterial({ map: tex }), new THREE.MeshStandardMaterial({ map: tex })]);
        p.position.set(.45, y, 0); const arm = new THREE.Group(); arm.add(p); arm.rotation.y = r; weg.add(arm);
    });
    const [we, wn] = ST(5.2, -3.4); weg.position.copy(w.W3(we, wn)); weg.traverse((o) => { if (o.isMesh) { o.castShadow = true; } }); scene.add(weg); greifbar(weg, 1.4, 3.0, .8);
    const schaufenster = scene.getObjectByName('window');
    const eis = scene.getObjectByName('eis'), aufsteller = scene.getObjectByName('aboard'), rad = scene.getObjectByName('bike'), bank = scene.getObjectByName('bench');
    const P = (en) => zuXZ(en);
    return {
        scene, hoehe: (x, z) => w.H(x, -z), frei: aussenFrei(w, [12.5, 5], 26, [[we, wn, .5]]),
        boden: (() => { const b = []; scene.traverse((o) => { if (o.name === 'ground') { b.push(o); } }); return b; })(),
        kamera: (p) => {
            // Blick von der Eiscafé-Seite quer über die Marktstraße auf die Front; schwenkt mit, wenn die Figur die Straße entlanggeht
            const e = p.x, n = -p.z, a = Math.max(-12, Math.min(12, (e - 12.5) * -.377 + (n - 5) * .926));
            const [ae, an] = ST(-3.2 + a * .55, 5), [te, tn] = ST(-.6 + a * .6, -5.5);
            return { p: new THREE.Vector3(ae, w.H(ae, an) + 3.0, -an), t: new THREE.Vector3(te, w.H(te, tn) + 1.5, -tn) };
        },
        spawn: { tuer: [...P(fw(.11, 1.5)), Math.atan2(FN[0], -FN[1])], karte: [...P(ST(-3, -.4)), Math.atan2(FN[0], -FN[1]) + Math.PI] },
        hotspots: [
            { obj: kasten, stand: P(fw(.05, 1.2)), eintraege: [{ id: 'postfach' }] },
            { obj: tuer, stand: P(fw(.11, 1.3)), eintraege: [{ tor: 'buero_innen', spawn: 'tuer', name: 'Ladentür der Zukunftsschmiede', ansehen: 'Die Ladentür. Drinnen brennt noch Licht.' }] },
            { obj: weg, stand: P(ST(4.4, -2.6)), eintraege: [{ id: 'tuer' }] },
            { obj: schaufenster, stand: P(fw(.42, 1.6)), eintraege: [{ deko: 'Schaufenster', text: 'Drinnen die Tafel mit der Tagesordnung. TOP 1: Kekse – erledigt.' }] },
            { obj: eis, stand: [15.3, -8.8], eintraege: [{ deko: 'Eiscafé', text: 'Das Eiscafé gegenüber. Von hier hat man die Zukunftsschmiede genau im Blick …' }] },
            { obj: aufsteller, stand: [11.6, -4.6], eintraege: [{ deko: 'Aufsteller', text: 'Ein blauer Aufsteller: „Komm rein – wir haben Kaffee!“ Die Kekse sind aber alle.' }] },
            { obj: rad, stand: P(fw(.3, 1.8)), eintraege: [{ deko: 'Fahrrad', text: 'Ein altes Hollandrad. Nicht abgeschlossen – mutig.' }] },
            { obj: bank, stand: P(ST(-10, -2)), eintraege: [{ deko: 'Bank', text: 'Eine Bank mitten in der Fußgängerzone. Hier wurden schon Koalitionen geschmiedet.' }] }
        ].filter((h) => h.obj),
        npcs: [],
        tick(dt, t) { }
    };
}

// ======================================================================
async function rathausAussen(renderer) {
    const { baue } = await import('./welt_rathaus.js');
    const w = baue(renderer, await laden('welt_rathaus.json'));
    const { scene, GW1, GW2, GWN, SD, flags, W3 } = w;
    const gm = [(GW1[0] + GW2[0]) / 2, (GW1[1] + GW2[1]) / 2];
    const vor = (d, s = 0) => { const a = [GW2[0] - GW1[0], GW2[1] - GW1[1]], l = Math.hypot(a[0], a[1]); return [gm[0] + GWN[0] * d + a[0] / l * s, gm[1] + GWN[1] * d + a[1] / l * s]; };
    const P = (en) => zuXZ(en);
    const blickRaus = Math.atan2(GWN[0], -GWN[1]);
    const tuer = scene.getObjectByName('door'), schild = scene.getObjectByName('rsign'), kasten = scene.getObjectByName('board'), bank = scene.getObjectByName('bench'), weg = scene.getObjectByName('stsign');
    // Wegweiser zur Reisekarte nahe dem Ankunftspunkt (das Straßenschild „Rathaus“ bleibt Dekoration)
    const zurKarte = new THREE.Group(); zurKarte.name = 'wegweiser';
    zurKarte.add(new THREE.Mesh(new THREE.CylinderGeometry(.05, .06, 3, 10), new THREE.MeshStandardMaterial({ color: 0x3a3d40, metalness: .5, roughness: .4 })));
    zurKarte.children[0].position.y = 1.5;
    [['Marktstraße', 2.6, .4], ['Ems', 2.25, -.6], ['Hallenbad', 1.9, 2.6]].forEach(([t, y, r]) => {
        const tex = w.canvasTex(256, 56, (g, cw, ch) => { g.fillStyle = '#f4f2ea'; g.fillRect(0, 0, cw, ch); g.strokeStyle = '#2a2a2a'; g.lineWidth = 4; g.strokeRect(2, 2, cw - 4, ch - 4); g.fillStyle = '#2a2a2a'; g.font = 'bold 30px Arial'; g.textBaseline = 'middle'; g.fillText(t + '  →', 16, ch / 2 + 2); });
        const mm = new THREE.MeshStandardMaterial({ map: tex }), mt = new THREE.MeshStandardMaterial({ color: 0x9a9d9e, metalness: .6, roughness: .4 });
        const pl = new THREE.Mesh(new THREE.BoxGeometry(1.0, .24, .03), [mt, mt, mt, mt, mm, mm]); pl.position.set(.45, y, 0);
        const arm = new THREE.Group(); arm.add(pl); arm.rotation.y = r; zurKarte.add(arm);
    });
    zurKarte.position.copy(W3(7.6, -27.5)); zurKarte.traverse((o) => { if (o.isMesh) { o.castShadow = true; } }); scene.add(zurKarte); greifbar(zurKarte, 1.4, 3.0, .8);
    if (weg) { greifbar(weg, 1.4, 3.2, .8); }
    const sal = klickKoerper(scene, 14, 7, 20, 31, w.H(31, -13) + 8.5, 13, .34);
    const turm = klickKoerper(scene, 20, 20, 20, 0, w.H(0, 21) + 12, -21, .22);
    const tuerZiel = W3(...gm);
    return {
        scene, hoehe: (x, z) => w.H(x, -z), frei: aussenFrei(w, [14, -20], 30, []),
        boden: (() => { const b = []; scene.traverse((o) => { if (o.name === 'ground') { b.push(o); } }); return b; })(),
        kamera: (p) => {
            // hinter der Figur, leicht zum Eingang gedreht
            // nahe am Eingang (unter dem Sitzungstrakt mit seinen Stützen) rückt die Kamera dichter heran
            const nah = Math.max(0, Math.min(1, (14 - p.distanceTo(tuerZiel)) / 7));
            const t = p.clone().lerp(tuerZiel, .22 * (1 - nah)); t.y = w.H(t.x, -t.z) + 1.5;
            const pos = new THREE.Vector3(p.x + 1.5 - nah * 1.2, 0, p.z + 10.5 - nah * 5.5); pos.y = Math.max(w.H(pos.x, -pos.z), w.H(p.x, -p.z)) + 3.6 - nah * 1.4;
            return { p: pos, t };
        },
        spawn: { karte: [10, 26, 2.36], eingang: [...P(vor(2.2, -1)), blickRaus] },
        hotspots: [
            { obj: tuer, stand: P(vor(1.5, -.6)), eintraege: [{ id: 'archivtuer' }, { tor: 'rathaus_flur', spawn: 'eingang', name: 'Haupteingang', ansehen: 'Die Glastür steht offen. Ein Pfeil: „Archiv – Untergeschoss“.', sichtbar: (z) => z.flags.archivOffen }] },
            { obj: zurKarte, stand: [8.4, 26.6], eintraege: [{ id: 'flurausgang' }, { id: 'ausgang' }] },
            { obj: weg, stand: P(SD(-1, -21.6)), eintraege: [{ deko: 'Straßenschild', text: 'RATHAUS. Ihr seid richtig.' }] },
            { obj: kasten, stand: [26.6, 10.4], eintraege: [{ deko: 'Schaukasten', text: 'Amtliche Bekanntmachungen. Ganz unten: „Archiv nur nach Beschluss geöffnet. Gez. Chris, Archivpfleger (ehrenamtlich)“.' }] },
            { obj: schild, stand: P(vor(2.4, .6)), eintraege: [{ deko: 'Schild „Rathaus“', text: 'RATHAUS – daneben das Stadtwappen von Greven.' }] },
            { obj: sal, stand: [26, 21], eintraege: [{ deko: 'Sitzungstrakt', text: 'Über euch der Ratssaal hinter den Lamellen. Heute ist keine Sitzung – nur Chris ist da.' }] },
            { obj: turm, stand: [2, 20], eintraege: [{ deko: 'Verwaltungsturm', text: 'Sieben Etagen Verwaltung. Ganz oben brennt noch Licht.' }] },
            { obj: bank, stand: [11.2, 21.2], eintraege: [{ deko: 'Bank', text: 'Eine Bank vor dem Rathaus. Ideal zum Warten auf Beschlüsse.' }] }
        ].filter((h) => h.obj),
        npcs: [
            { hotspot: 'chris', figur: 'chris', pos: P(vor(1.1, .2)), rot: blickRaus, pose: 'arme', stand: P(vor(2.5, -.9)) }
        ],
        tick(dt, t) {
            flags.forEach((f, k) => { const p = f.geometry.attributes.position; for (let i = 0; i < p.count; i++) { const y = p.getY(i); p.setZ(i, Math.sin(t * 2.2 + y * 1.6 + k) * .12 * (1.6 - y) / 3.2); } p.needsUpdate = true; });
        }
    };
}

// ======================================================================
function bueroInnen(renderer) {
    const r = innen.buero(renderer), o = r.objekte;
    greifbar(o.garderobe, .7, 2.0, .7); greifbar(o.umschlag, .5, .2, .4, .8);
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.umschlag, stand: [-.5, 1.15], eintraege: [{ id: 'brief' }], sichtbarObj: (z) => !z.inventar.includes('brief') },
            { obj: o.fax, stand: [3.2, -1.75], eintraege: [{ id: 'fax' }] },
            { obj: o.regal, stand: [-2.5, -1.95], eintraege: [{ id: 'regal' }] },
            { obj: o.garderobe, stand: [3.1, 1.6], eintraege: [{ id: 'garderobe' }] },
            { obj: o.tuer, stand: [-3.35, 1.8], eintraege: [{ tor: 'schmiede_aussen', spawn: 'tuer', name: 'Ladentür zur Marktstraße', ansehen: 'Die Ladentür. Draußen die Marktstraße – und gegenüber das Eiscafé.' }] },
            { obj: o.tafel, stand: [.7, -2.0], eintraege: [{ deko: 'Tafel', text: 'TOP 1: Kekse ✓ · TOP 2: Haushalt · TOP 3: Verschiedenes. Darunter, in Chanis Schrift: „(Gral?)“' }] },
            { obj: o.teller, stand: [.6, 1.15], eintraege: [{ deko: 'Keksteller', text: 'Leer. Nur noch Krümel. Die Fraktionssitzung war gründlich.' }] },
            { obj: o.fensterGlas, stand: [-3.3, -.8], eintraege: [{ deko: 'Schaufenster', text: 'Draußen die Marktstraße im Abendlicht. Gegenüber das Eiscafé.' }] }
        ],
        npcs: [],
        aktualisieren(z) { o.garderobeSachen.visible = !z.inventar.includes('taschenlampe'); }
    });
}

function rathausFlur(renderer) {
    const r = innen.flur(renderer), o = r.objekte;
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.eingang, stand: [-6.4, .3], eintraege: [{ tor: 'rathaus_aussen', spawn: 'eingang', name: 'Ausgang zur Rathausstraße', ansehen: 'Die Glastür nach draußen.' }] },
            { obj: o.treppe, stand: [4.9, .6], eintraege: [{ tor: 'archiv_innen', spawn: 'tuer', name: 'Treppe ins Untergeschoss (Archiv)', ansehen: 'Eine Treppe nach unten. Schild: „Archiv – Untergeschoss“.' }] },
            { obj: o.brett, stand: [-1.6, -.9], eintraege: [{ deko: 'Schwarzes Brett', text: 'Zettel: „Tag der offenen Tür – Antrag der SPD liegt vor“, „Kantine: Montag Grünkohl“, „Wer hat meinen Locher?“' }] },
            { obj: o.galerie, stand: [2.5, -.9], eintraege: [{ deko: 'Ahnengalerie', text: 'Ehemalige Bürgermeister in Öl. Einer schaut, als hätte er den Gral persönlich versteckt.' }] },
            { obj: o.loescher, stand: [-5.3, -.9], eintraege: [{ deko: 'Feuerlöscher', text: 'Geprüft 1998. Joe: „Die Dichtung am Ventil ist durch.“' }] },
            { obj: o.bank, stand: [-3.4, -.6], eintraege: [{ deko: 'Wartebank', text: 'Die Wartebank vor dem Bürgerbüro. Wartenummer 4711 wird gleich aufgerufen. Seit 2019.' }] }
        ],
        npcs: []
    });
}

function archivInnen(renderer) {
    const r = innen.archiv(renderer), o = r.objekte;
    greifbar(o.sohlen, 1.6, .3, .6, .1); o.sohlen.children[o.sohlen.children.length - 1].position.x = 3.15; o.sohlen.children[o.sohlen.children.length - 1].position.z = -2.7;
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.protokolle, stand: [-.5, -2.2], eintraege: [{ id: 'protokolle' }] },
            { obj: o.gemaelde, stand: [3.9, -.5], eintraege: [{ id: 'wappen' }] },
            { obj: o.sohlen, stand: [3.1, -1.9], eintraege: [{ id: 'sohlen' }] },
            {
                obj: o.tuer, stand: [-4.1, 1.1], eintraege: [{
                    tor: 'rathaus_flur', spawn: 'treppe', name: 'Stahltür (zum Flur)', ansehen: 'Die Stahltür nach oben in den Flur.',
                    // Stehen Pinsel und Ledeiw im Weg, antwortet die Spiellogik (Hotspot „ausgang“)
                    sperre: (z) => z.flags.gegnerArchiv && !z.flags.gegnerArchivFest, sperreAktion: 'ausgang'
                }]
            },
            { obj: o.lesetisch, stand: [1.6, 1.9], eintraege: [{ deko: 'Lesetisch', text: 'Ein Lesetisch mit grüner Bankerlampe. Darauf ein Zettel: „Protokoll 47 – NICHT ausleihen!“' }] }
        ],
        npcs: [
            { hotspot: 'gegner', figur: 'pinsel', pos: [-4.2, .45], rot: Math.PI / 2 - .3, fest: [2.7, -2.75], festRot: .1, pose: 'arme', stand: [-2.7, .9] },
            { hotspot: 'gegner', figur: 'ledeiw', pos: [-4.2, 1.8], rot: Math.PI / 2 + .3, fest: [3.6, -2.75], festRot: -.1, pose: 'arme', stand: [-2.7, .9] }
        ],
        aktualisieren(z) {
            const f = z.flags;
            o.raupen.forEach((m) => { m.visible = !!f.gegnerArchivFest; });
            o.sohlen.visible = !!f.gegnerArchivFest;
            o.sohlen.children.forEach((c, i) => { if (c.geometry && c.geometry.type === 'BoxGeometry') { c.visible = !!f.gegnerBefreit; } });
        }
    });
}

const BAUER = { schmiede_aussen: schmiedeAussen, rathaus_aussen: rathausAussen, buero_innen: bueroInnen, rathaus_flur: rathausFlur, archiv_innen: archivInnen, ems_deich: emsDeich, baustelle, radbruecke, schulhof, schulflur: schulflurBereich, krypta: kryptaBereich, tempel_halle: tempelHalleBereich, tempel_finale: tempelFinaleBereich };
const fertig = {};
/** Bereich (einmal) aufbauen; Außenbereiche laden dabei ihre LoD2-Daten. */
export function bereich(id, renderer) {
    if (!fertig[id]) { fertig[id] = Promise.resolve(BAUER[id](renderer)).then((b) => Object.assign(b, { id, titel: TITEL[id] })); }
    return fertig[id];
}
