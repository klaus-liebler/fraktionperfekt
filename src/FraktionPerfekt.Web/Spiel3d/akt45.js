// Joe & Chani 3D – Akt 4–5: Hof der Martin-Luther-Grundschule (LoD2 mit Gelände), Schulflur, Krypta, Tempel des Grundgesetzes,
// Wahlkugel-Flucht durch den Gang (mit Lorettes faltbarem Lastenrad) und die Kammer des Grals.
import * as THREE from './three_min.js';
import * as innen from './innen.js';
import * as FIG from './figuren3d.js';

const laden = (name) => fetch(name).then((r) => { if (!r.ok) { throw new Error('Daten fehlen: ' + name); } return r.json(); });
const std = (o) => new THREE.MeshStandardMaterial(Object.assign({ roughness: .8 }, o));
function mesh(geo, mat, parent, x = 0, y = 0, z = 0) { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.castShadow = m.receiveShadow = true; parent.add(m); return m; }
function greifbar(obj, w, h, d, y = h / 2) { const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({ visible: false })); m.position.y = y; obj.add(m); return obj; }
const imKreis = (liste, e, n) => liste.some(([be, bn, r]) => Math.hypot(e - be, n - bn) < r);
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

// ======================================================================
// Schulhof (abends: Elternabend, Fledermäuse fliegen aus)
// ======================================================================
export async function schulhof(renderer) {
    const { baue } = await import('./welt_schule.js');
    const w = baue(renderer, await laden('welt_schule.json'));
    const { scene, H, W3, EW1, EW2, EWN, M, skyTex, hemi, sun, wallUniforms, lampLights, canLamp, porchLight, GB } = w;
    const at = (o, e, n, dy = 0) => { o.position.copy(W3(e, n, dy)); scene.add(o); return o; };
    const eigene = [];
    const tuerMitte = [(EW1[0] + EW2[0]) / 2, (EW1[1] + EW2[1]) / 2];
    // Haupteingang: Klickkörper vor der Tür
    const tuer = new THREE.Mesh(new THREE.BoxGeometry(2.6, 2.8, .6), new THREE.MeshBasicMaterial({ visible: false }));
    tuer.position.copy(W3(tuerMitte[0] + EWN[0] * .2, tuerMitte[1] + EWN[1] * .2, 1.4)); tuer.rotation.y = Math.atan2(-(EW2[1] - EW1[1]), EW2[0] - EW1[0]); scene.add(tuer);
    // Schulhofuhr auf einem Mast
    const uhr = new THREE.Group(); uhr.name = 'uhr';
    mesh(new THREE.CylinderGeometry(.06, .08, 3.2, 10), M.metal, uhr, 0, 1.6, 0);
    const blatt = new THREE.Group(); blatt.position.y = 3.3; uhr.add(blatt);
    mesh(new THREE.CylinderGeometry(.42, .42, .1, 32), std({ color: 0x2a2a2a }), blatt).rotation.x = Math.PI / 2;
    const ziffer = mesh(new THREE.CircleGeometry(.37, 32), std({ map: w.canvasTex(128, 128, (g, cw) => { g.fillStyle = '#f8f6f0'; g.fillRect(0, 0, cw, cw); g.fillStyle = '#1a1a1a'; for (let i = 0; i < 12; i++) { const a = i / 12 * Math.PI * 2; g.fillRect(64 + Math.sin(a) * 52 - 2, 64 - Math.cos(a) * 52 - 5, 4, 10); } }) }), blatt, 0, 0, .052);
    ziffer.castShadow = false;
    const zeiger = (l, b) => { const z = new THREE.Group(); mesh(new THREE.BoxGeometry(b, l, .01), std({ color: 0x111111 }), z, 0, l / 2, 0); z.position.z = .06; blatt.add(z); return z; };
    const std_ = zeiger(.2, .03), min_ = zeiger(.3, .02);
    uhr.rotation.y = 2.4; at(uhr, 12.5, 22); greifbar(uhr, .9, 3.8, .9); eigene.push([12.5, 22, .4]);
    // Tischtennisplatte
    const platte = new THREE.Group(); platte.name = 'tischtennis';
    mesh(new THREE.BoxGeometry(2.74, .1, 1.52), std({ color: 0x2a6a4a, roughness: .6 }), platte, 0, .76, 0);
    mesh(new THREE.BoxGeometry(.04, .15, 1.6), std({ color: 0xdddddd }), platte, 0, .88, 0);
    for (const x of [-1, 1]) { mesh(new THREE.BoxGeometry(.3, .72, 1.2), std({ color: 0x5a5a5a }), platte, x, .36, 0); }
    platte.rotation.y = .5; at(platte, 19.5, 18.5); eigene.push([19.5, 18.5, 1.7]);
    // Schultor: Gittertor mit Wegweiser
    const tor = new THREE.Group(); tor.name = 'schultor';
    for (const x of [-1.6, 1.6]) { mesh(new THREE.BoxGeometry(.15, 1.8, .15), M.metal, tor, x, .9, 0); }
    for (let x = -1.4; x <= 1.41; x += .2) { mesh(new THREE.BoxGeometry(.03, 1.4, .03), std({ color: 0x2a5a8a, metalness: .5 }), tor, x, .8, .35).rotation.y = 0; }
    const torSchild = mesh(new THREE.PlaneGeometry(1.4, .36), std({ map: w.canvasTex(256, 64, (g, cw, ch) => { g.fillStyle = '#ffffff'; g.fillRect(0, 0, cw, ch); g.fillStyle = '#1a3a6a'; g.font = 'bold 28px Arial'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('Ausgang Schulhof', cw / 2, ch / 2); }), side: THREE.DoubleSide }), tor, 0, 1.6, .05);
    torSchild.castShadow = false;
    tor.rotation.y = -1.25; at(tor, 30, 14); greifbar(tor, 3.4, 2, 1); eigene.push([30, 14, 1.7]);
    // Kellerabgang mit Gitter (Fledermausquartier) neben dem Eingang
    const kellerG = new THREE.Group();
    mesh(new THREE.BoxGeometry(1.6, .1, 2.4), std({ color: 0x3a3a3a }), kellerG, 0, -.05, 0);
    for (const x of [-.85, .85]) { mesh(new THREE.BoxGeometry(.05, .9, 2.4), M.metal, kellerG, x, .45, 0); }
    kellerG.rotation.y = Math.atan2(EWN[0], -EWN[1]) + Math.PI / 2; at(kellerG, tuerMitte[0] + EWN[0] * 3.2 + 1.6, tuerMitte[1] + EWN[1] * 3.2 - 1.2, .02);
    // Lorette mit Lastenrad
    const rad = FIG.lastenrad(); rad.rotation.y = -.6; at(rad, 8.8, 13.6); eigene.push([8.8, 13.6, 1.2]);
    // Fledermäuse (fliegen beim Warten aus dem Kellergang)
    const fledermaeuse = new THREE.Group(); scene.add(fledermaeuse);
    const flugM = new THREE.MeshBasicMaterial({ color: 0x1a1414, side: THREE.DoubleSide });
    for (let i = 0; i < 40; i++) {
        const f = new THREE.Group();
        for (const s of [-1, 1]) { const fl = new THREE.Mesh(new THREE.PlaneGeometry(.18, .08), flugM); fl.position.x = s * .09; f.add(fl); }
        f.userData = { a: Math.random() * 6.28, r: 2 + Math.random() * 6, v: .5 + Math.random(), h: Math.random() * 3 };
        fledermaeuse.add(f);
    }
    fledermaeuse.visible = false;
    const kellerPos = kellerG.position.clone();
    let abend = null, flugZeit = -1, faltZeit = -1;
    function abendSetzen(an) {
        if (abend === an) { return; }
        abend = an;
        const sky = an ? ['#1e2a48', '#59587a', '#c9836a'] : ['#c9d2d8', '#e4e7e6', '#efe9df'];
        const g = skyTex.image.getContext('2d'), gr = g.createLinearGradient(0, 0, 0, 256);
        gr.addColorStop(0, sky[0]); gr.addColorStop(.55, sky[1]); gr.addColorStop(1, sky[2]); g.fillStyle = gr; g.fillRect(0, 0, 4, 256); skyTex.needsUpdate = true;
        scene.fog.color.set(an ? 0x5c5a72 : 0xdfe2e1);
        hemi.intensity = an ? .35 : 1.6; hemi.color.set(an ? 0x8090c0 : 0xe8eef2);
        sun.intensity = an ? .25 : 1.6; sun.color.set(an ? 0xff9a6a : 0xfff2de);
        scene.environmentIntensity = an ? .08 : .45;
        M.hall.emissiveIntensity = an ? 1.6 : .18; canLamp.material.emissiveIntensity = an ? 3 : .6; porchLight.intensity = an ? 12 : 0;
        wallUniforms.uNight.value = an ? 1 : 0;
        lampLights.forEach(({ L, bulb }) => { L.intensity = an ? 40 : 0; bulb.material.emissiveIntensity = an ? 4 : 0; });
        GB.children.forEach((c) => { if (c.name === 'deco') { c.material.emissive.set(an ? 0xffd8a0 : 0x000000); c.material.emissiveMap = an ? c.material.map : null; c.material.emissiveIntensity = an ? .8 : 0; c.material.needsUpdate = true; } });
        std_.rotation.z = -(an ? 20.58 : 18.17) / 12 * Math.PI * 2; min_.rotation.z = -(an ? 35 : 10) / 60 * Math.PI * 2;
    }
    const frei = (x, z) => {
        const e = x, n = -z;
        if (Math.hypot(e - 14, n - 20) > 24) { return false; }
        if (imKreis(w.blockers, e, n) || imKreis(eigene, e, n)) { return false; }
        for (const [de, dn] of [[0, 0], [.35, 0], [-.35, 0], [0, .35], [0, -.35]]) { if (imDreieck(w.footprints, e + de, n + dn)) { return false; } }
        return true;
    };
    const boden = []; scene.traverse((o) => { if (o.name === 'ground') { boden.push(o); } });
    const stand = (d, s = 0) => { const a = [EW2[0] - EW1[0], EW2[1] - EW1[1]], l = Math.hypot(a[0], a[1]); return [tuerMitte[0] + EWN[0] * d + a[0] / l * s, -(tuerMitte[1] + EWN[1] * d + a[1] / l * s)]; };
    const raus = Math.atan2(EWN[0], -EWN[1]);
    return {
        scene, frei, boden, hoehe: (x, z) => H(x, -z),
        kamera: (p) => { const t = p.clone(); t.y += 1.3; return { p: new THREE.Vector3(p.x + 9.5, p.y + 4.6, p.z - 6.4), t }; },
        spawn: { karte: [27.4, -15.4, -1.88], von_schulflur: [...stand(2.2), raus] },
        hotspots: [
            { obj: uhr, stand: [13.4, -20.8], eintraege: [{ id: 'uhr' }] },
            { obj: tuer, stand: stand(1.6), eintraege: [{ id: 'eingang' }] },
            { obj: tor, stand: [28.0, -14.6], eintraege: [{ id: 'zurkarte' }] },
            { obj: platte, stand: [18.4, -16.6], eintraege: [{ deko: 'Tischtennisplatte', text: 'Eine Tischtennisplatte aus Beton. Joe: „Unverwüstlich. Wie das Ehrenamt.“' }] },
            { obj: rad, stand: [9.6, -11.9], eintraege: [{ deko: 'Lorettes Lastenrad', text: 'Ein grünes Lastenrad mit Holzkiste. Ein Aufkleber: „Faltbar nach Gutachten, Seite 213“.' }], sichtbarObj: (z) => !z.flags.lastenrad || faltZeit >= 0 },
            { obj: kellerG, stand: [...stand(3.2, -.4)], eintraege: [{ deko: 'Kellerabgang', text: 'Ein vergitterter Kellerabgang. Darüber ein Schild: „Fledermausquartier – bitte leise“.' }] }
        ],
        npcs: [{ hotspot: 'lore', figur: 'lorette', pos: [10.3, -15.2], rot: 2.2, pose: 'stehen', stand: [11.8, -13.6] }],
        aktualisieren(z) {
            if (abend === false && z.flags.abend && flugZeit < 0) { flugZeit = 0; }
            abendSetzen(!!z.flags.abend);
            if (z.flags.lastenrad && rad.visible && faltZeit < 0) { faltZeit = 0; }
            if (!z.flags.lastenrad) { rad.visible = true; rad.scale.setScalar(1); faltZeit = -1; }
        },
        tick(dt, t) {
            // Fledermäuse: 8 Sekunden lang aus dem Kellergang in die Nacht
            if (flugZeit >= 0) {
                flugZeit += dt; fledermaeuse.visible = flugZeit < 8;
                fledermaeuse.children.forEach((f, i) => {
                    const u = f.userData, k = Math.min(1, flugZeit / 6);
                    f.position.set(kellerPos.x + Math.cos(u.a + t * u.v) * u.r * k, kellerPos.y + .5 + u.h * k + flugZeit * 1.6 * k, kellerPos.z + Math.sin(u.a + t * u.v) * u.r * k);
                    f.children[0].rotation.y = Math.sin(t * 30 + i) * .8; f.children[1].rotation.y = -f.children[0].rotation.y;
                });
            }
            // Lastenrad zusammenfalten: schrumpft auf Brotdosengröße, dann ab in Chanis Tasche
            if (faltZeit >= 0 && rad.visible) {
                faltZeit += dt; const k = Math.max(0, 1 - faltZeit / 1.6);
                rad.scale.set(.15 + .85 * k, .15 + .85 * Math.max(0, 1 - faltZeit / 1.2), .15 + .85 * k); rad.rotation.x = (1 - k) * .4;
                if (faltZeit > 2.0) { rad.visible = false; }
            }
        }
    };
}

// ---------- Innenräume: Hotspots der Spiellogik ----------
export function schulflurBereich(renderer) {
    const r = innen.schulflur(renderer), o = r.objekte;
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.t1a, stand: [-4.4, -.9], eintraege: [{ id: 'klasse1a' }] },
            { obj: o.t2b, stand: [-.6, -.9], eintraege: [{ id: 'klasse2b' }] },
            { obj: o.wc, stand: [2.6, -.9], eintraege: [{ id: 'wc' }] },
            { obj: o.keller, stand: [6.6, -.9], eintraege: [{ id: 'kellertuer' }] },
            { obj: o.hof, stand: [-7.1, .3], eintraege: [{ id: 'zumhof' }] },
            { obj: o.bilder, stand: [-5.6, -.9], eintraege: [{ deko: 'Kinderbilder', text: 'Häuser, Sonnen, eine Bürgermeisterin mit drei Armen. Eins heißt „Mein Rathaus“ – mit Rutsche.' }] },
            { obj: o.haken, stand: [1.2, -.9], eintraege: [{ deko: 'Turnbeutel', text: 'Turnbeutel in allen Farben. Über die richtige Farbe streitet gerade der Elternabend.' }] }
        ],
        npcs: []
    });
}

export function kryptaBereich(renderer) {
    const r = innen.krypta(renderer), o = r.objekte;
    greifbar(o.schloss, 1.2, 1.2, 1.0);
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.fresko, stand: [-3.6, -2.0], eintraege: [{ id: 'fresko' }] },
            { obj: o.schloss, stand: [-.6, -1.6], eintraege: [{ id: 'schloss' }] },
            { obj: o.tor, stand: [3.0, -1.9], eintraege: [{ id: 'tor' }] },
            { obj: o.treppe, stand: [-3.3, 1.6], eintraege: [{ id: 'zurueck' }] }
        ],
        npcs: [],
        aktualisieren(z) {
            const hat = (g) => z.inventar.includes(g);
            o.siegelTeile.forEach((s) => { s.visible = !!z.flags.tempelOffen; });
            o.fluegel.position.x = z.flags.tempelOffen ? 1.9 : 0;
            o.scheiben.forEach((s, i) => { s.rotation.y = hat('tempelschluessel') || z.flags.tempelOffen ? [2, 1, 0][i] * 2.1 : i * .7; });
        }
    });
}

export function tempelHalleBereich(renderer) {
    const r = innen.tempelHalle(renderer), o = r.objekte;
    greifbar(o.links, 1.0, 1.0, 1.0); greifbar(o.rechts, 1.7, 1.0, 1.7); greifbar(o.gangTor, 5, 4.5, 1.5);
    let gitterY = 0;
    const zustandSetzen = r.zustandSetzen, tick0 = r.tick;
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden'), ...o.bruecke.children],
        hotspots: [
            { obj: o.inschrift, stand: [0, -5.0], eintraege: [{ id: 'inschrift' }] },
            { obj: o.links, stand: [-2.2, -3.0], eintraege: [{ id: 'platte_links' }] },
            { obj: o.rechts, stand: [2.0, -3.0], eintraege: [{ id: 'platte_rechts' }] },
            { obj: o.gitter, stand: [0, -5.3], eintraege: [{ id: 'gitter' }] },
            { obj: o.tafel, stand: [-5.0, -9.2], eintraege: [{ id: 'tafel' }] },
            { obj: o.abgrund, stand: [0, -11.3], eintraege: [{ id: 'abgrund' }] },
            { obj: o.gangTor, stand: [0, -19.4], eintraege: [{ id: 'gang' }] }
        ],
        npcs: [],
        /** Plattformen: Wer auf seiner Plattform steht, bleibt dort, bis er woanders hingeht. */
        plattformen: { links: { obj: o.links, x: -3.2, z: -4.2 }, rechts: { obj: o.rechts, x: 3.2, z: -4.2 } },
        aktualisieren(z) {
            zustandSetzen(z);
            o.gitter.userData.ziel = z.flags.gitterOffen ? 4.2 : 0;
            o.bruecke.visible = !!z.flags.brueckeDa;
            o.tf.material.emissive.set(z.flags.goGeloest ? 0x000000 : z.flags.gitterOffen ? 0x2a1a00 : 0x000000);
            for (const s of ['links', 'rechts']) { o[s].children[1].position.y = .55 - (z.platten[s] ? .12 : 0); }
            o.kugel.position.copy(r.gangPunkt(0, 0, 1.8)); o.kugel.position.z -= 2.5;
            o.kugel.visible = !z.flags.kugelGeschafft;
        },
        tick(dt, t) {
            const ziel = o.gitter.userData.ziel || 0; gitterY += (ziel - gitterY) * Math.min(1, dt * 1.5); o.gitter.position.y = gitterY;
            tick0(dt, t);
        },
        kugelFlucht: kugelFlucht(r)
    });
}

/** Wahlkugel-Flucht: Kamera vor den Figuren, Blick zurück auf die heranrollende Kugel. */
function kugelFlucht(r) {
    const kugel = r.objekte.kugel;
    let rad = null, figs = null;
    return {
        start(figuren, lastenrad) {
            figs = figuren;
            if (lastenrad) { if (!rad) { rad = FIG.lastenrad(); r.scene.add(rad); } rad.visible = true; rad.rotation.y = Math.PI / 2; }
            kugel.visible = true;
        },
        ende() { if (rad) { rad.visible = false; } },
        bild(sim, t) {
            const p = r.gangPunkt(Math.min(1, sim.pos)), k = r.gangPunkt(Math.max(0, sim.kugel));
            kugel.position.set(0, k.y + 1.8, k.z + 1.4); kugel.rotation.x -= .08 * (1 + sim.zeit * .05);
            const [joe, chani] = figs;
            if (sim.lastenrad && rad) {
                rad.position.set(0, p.y, p.z); rad.rotation.set(Math.asin(-.1) * -1, Math.PI / 2, 0);
                rad.userData.raeder.forEach((w) => { w.rotation.x += sim.tempo * .6; });
                joe.position.set(.0, p.y + .45, p.z + .4); joe.rotation.y = Math.PI;
                chani.position.set(0, p.y + .45, p.z - .9); chani.rotation.y = Math.PI;
            } else {
                joe.position.set(-.5, p.y, p.z); chani.position.set(.5, p.y, p.z - .3); joe.rotation.y = chani.rotation.y = Math.PI;
            }
        },
        kamera(sim) {
            const p = r.gangPunkt(Math.min(1, sim.pos));
            return { p: new THREE.Vector3(1.3, p.y + 2.6, p.z - 9.5), t: new THREE.Vector3(0, p.y + .9, p.z + 4) };
        }
    };
}

export function tempelFinaleBereich(renderer) {
    const r = innen.tempelFinale(renderer), o = r.objekte;
    greifbar(o.pokal, 1.2, 2.0, 1.2); greifbar(o.urne, 1.0, 1.3, .8);
    return Object.assign(r, {
        boden: [r.scene.getObjectByName('boden')],
        hotspots: [
            { obj: o.pokal, stand: [-2.0, -.6], eintraege: [{ id: 'pokal' }] },
            { obj: o.urne, stand: [1.8, -.7], eintraege: [{ id: 'urne' }] }
        ],
        npcs: [
            { sichtbar: (z) => z.flags.kugelGeschafft, figur: 'pinsel', pos: [-3.6, -1.4], rot: 1.6, pose: 'stehen', ende: [-4.6, 2.6], endeRot: 2.6 },
            { sichtbar: (z) => z.flags.kugelGeschafft, figur: 'ledeiw', pos: [-3.4, -.1], rot: 1.9, pose: 'stehen', ende: [-3.8, 3.0], endeRot: 2.6 },
            { sichtbar: (z) => z.ende && z.flags.antragUebergeben, figur: 'chris', pos: [4.2, 1.6], rot: -2.0, pose: 'arme' },
            { sichtbar: (z) => z.ende && z.flags.lastenrad, figur: 'lorette', pos: [3.0, 2.6], rot: -2.6, pose: 'stehen' },
            { sichtbar: (z) => z.ende && z.flags.stadtplanWeg, figur: 'johanna', pos: [4.6, .2], rot: -1.6, pose: 'stehen' }
        ],
        aktualisieren(z) {
            // Im Finale tragen Joe und Chani die Urne gemeinsam
            o.urne.userData.getragen = !!z.ende;
        }
    });
}
