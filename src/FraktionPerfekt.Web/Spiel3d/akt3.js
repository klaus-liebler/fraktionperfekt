// Joe & Chani 3D – Akt 3: Emsdeich am Kanuverleih (mit der Radbrücke), Bootsflucht auf der Ems, Baustelle am Hallenbad.
// Gelände und Brücke aus den 3D-Prototypen; Kanuverleih, Steg, Skulpturen, Baustelle frei erfunden.
import * as THREE from './three_min.js';
import { eingangHallenbad } from './hallenbad_eingang.js';

const laden = (name) => fetch(name).then((r) => { if (!r.ok) { throw new Error('Daten fehlen: ' + name); } return r.json(); });
const std = (o) => new THREE.MeshStandardMaterial(Object.assign({ roughness: .8 }, o));
function mesh(geo, mat, parent, x = 0, y = 0, z = 0) { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.castShadow = m.receiveShadow = true; parent.add(m); return m; }
function greifbar(obj, w, h, d, y = h / 2) { const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({ visible: false })); m.position.y = y; obj.add(m); return obj; }
function texte(canvasTex, w, h, hg, farbe, zeilen, font) {
    return canvasTex(w, h, (g, cw, ch) => {
        g.fillStyle = hg; g.fillRect(0, 0, cw, ch); g.fillStyle = farbe; g.font = font; g.textAlign = 'center'; g.textBaseline = 'middle';
        const fs = parseInt(font.match(/(\d+)px/)[1], 10);
        zeilen.forEach((t, i) => g.fillText(t, cw / 2, ch / 2 + (i - (zeilen.length - 1) / 2) * fs * 1.15));
    });
}
function wegweiser(w, ziele) {
    const g = new THREE.Group(); g.name = 'wegweiser';
    mesh(new THREE.CylinderGeometry(.05, .06, 3, 10), std({ color: 0x3a3d40, metalness: .5, roughness: .4 }), g, 0, 1.5, 0);
    ziele.forEach(([t, y, r]) => {
        const tex = texte(w.canvasTex, 256, 56, '#f4f2ea', '#2a2a2a', [t + '  →'], 'bold 30px Arial');
        const mm = std({ map: tex }), mt = std({ color: 0x9a9d9e, metalness: .6, roughness: .4 });
        const arm = new THREE.Group(); mesh(new THREE.BoxGeometry(1.0, .24, .03), [mt, mt, mt, mt, mm, mm], arm, .45, y, 0); arm.rotation.y = r; g.add(arm);
    });
    return greifbar(g, 1.4, 3.0, .8);
}
const imKreis = (liste, e, n) => liste.some(([be, bn, r]) => Math.hypot(e - be, n - bn) < r);

// ======================================================================
// Emsdeich am Kanuverleih (Westufer, ein Stück südlich der Radbrücke)
// ======================================================================
export async function emsDeich(renderer) {
    const { baue } = await import('./welt_ems.js');
    const w = baue(renderer, await laden('welt_ems.json'));
    const { scene, H, W3, riverX } = w;
    const U = (off, n) => [riverX(n) + off, n];             // Ufer-Koordinate: Abstand zur Flussmitte (West negativ), Nord
    const P = ([e, n]) => [e, -n];
    const at = (o, [e, n], dy = 0) => { o.position.copy(W3(e, n, dy)); scene.add(o); return o; };
    const holz = std({ color: 0x8a6440, roughness: .85 }), dunkelholz = std({ color: 0x5a3e28, roughness: .9 });
    const eigene = [];
    // Die Radbrücke mit den gelben Laternen steht beim Hallenbad, nicht am Kanuverleih: hier ausblenden
    w.bridge.visible = false;
    scene.children.forEach((c) => { if ((c.isGroup && Math.abs(c.position.z) < 3.5 && c.children.some((k) => k.isPointLight)) || c.name === 'zsign') { c.visible = false; } });
    // Bäume zwischen Wasser und Deich im Spielbereich entfernen (sie stünden vor der Kamera)
    const weg_ = [];
    scene.traverse((o) => {
        if (!o.isMesh || !o.geometry || !(o.material === w.M.bark || (o.material.flatShading && o.material.color && o.material.color.getHex() === 0x4f7a32))) { return; }
        o.geometry.computeBoundingBox(); const c = o.geometry.boundingBox.getCenter(new THREE.Vector3()), n = -c.z;
        if (n > -58 && n < 4 && c.x - riverX(n) < -10 && c.x - riverX(n) > -40) { weg_.push(o); }
    });
    weg_.forEach((o) => o.parent.remove(o));
    for (let i = w.blockers.length - 1; i >= 0; i--) { const [e, n] = w.blockers[i]; if (n > -58 && n < 4 && e - riverX(n) < -10 && e - riverX(n) > -40) { w.blockers.splice(i, 1); } }
    // ---------- Kanuverleih: Holzhütte mit Satteldach, Schild, Preistafel
    const huette = new THREE.Group(); huette.name = 'verleih';
    mesh(new THREE.BoxGeometry(3.4, 2.4, 2.8), holz, huette, 0, 1.2, 0);
    for (let i = 0; i < 9; i++) { mesh(new THREE.BoxGeometry(3.42, .03, 2.82), dunkelholz, huette, 0, .2 + i * .26, 0).castShadow = false; }
    const dach = new THREE.Shape([new THREE.Vector2(-1.9, 0), new THREE.Vector2(1.9, 0), new THREE.Vector2(0, 1.1)]);
    const dg = new THREE.ExtrudeGeometry(dach, { depth: 3.3, bevelEnabled: false }); dg.translate(0, 0, -1.65);
    mesh(dg, std({ color: 0x3a3f3a, roughness: .7 }), huette, 0, 2.4, 0).rotation.y = Math.PI / 2;
    mesh(new THREE.BoxGeometry(.06, 1.9, .9), dunkelholz, huette, 1.71, .95, .5);
    const schild = mesh(new THREE.PlaneGeometry(2.2, .42), std({ map: texte(w.canvasTex, 512, 96, '#f2e6c8', '#2a4a7a', ['KANUVERLEIH'], 'bold 60px Georgia') }), huette, 1.73, 2.15, 0); schild.rotation.y = Math.PI / 2;
    const tafelTex = w.canvasTex(256, 320, (g, cw, ch) => {
        g.fillStyle = '#1e2a22'; g.fillRect(0, 0, cw, ch); g.strokeStyle = '#8a6440'; g.lineWidth = 12; g.strokeRect(0, 0, cw, ch);
        g.fillStyle = '#ece8dc'; g.font = 'bold 34px "Comic Sans MS", cursive'; g.textAlign = 'center'; g.fillText('Preise', cw / 2, 52);
        g.strokeStyle = '#ece8dc'; g.lineWidth = 4; g.beginPath(); g.moveTo(70, 44); g.lineTo(186, 44); g.stroke();
        g.fillStyle = '#f5b7b1'; g.font = 'bold 30px "Comic Sans MS", cursive'; g.fillText('Solidarität', cw / 2, 100);
        g.fillStyle = '#ece8dc'; g.font = '22px "Comic Sans MS", cursive'; ['Kanu: nach Bedarf', 'Boot: nach Können', 'Paddel: Umverteilung'].forEach((t, i) => g.fillText(t, cw / 2, 160 + i * 40));
    });
    const tafel = new THREE.Group();
    mesh(new THREE.PlaneGeometry(.9, 1.12), std({ map: tafelTex }), tafel, 0, 1.45, 0);
    for (const x of [-.4, .4]) { mesh(new THREE.BoxGeometry(.06, 1.9, .06), dunkelholz, tafel, x, .95, -.04); }
    const plan = mesh(new THREE.PlaneGeometry(.34, .26), std({ map: w.canvasTex(128, 96, (g, cw, ch) => { g.fillStyle = '#f2ecd8'; g.fillRect(0, 0, cw, ch); g.strokeStyle = '#3a6ab0'; g.lineWidth = 6; g.beginPath(); g.moveTo(20, 0); g.bezierCurveTo(60, 40, 30, 60, 80, ch); g.stroke(); g.strokeStyle = '#c0392b'; g.lineWidth = 3; g.beginPath(); g.arc(50, 48, 18, 0, 7); g.stroke(); g.fillStyle = '#555'; g.fillRect(70, 10, 40, 4); g.fillRect(70, 20, 30, 4); }) }), tafel, .25, 1.75, .01);
    plan.rotation.z = .08;
    tafel.position.set(1.0, 0, 1.9); tafel.rotation.y = .5; huette.add(tafel);
    huette.rotation.y = 0; at(huette, U(-29, -37)); eigene.push([...U(-29, -37), 2.6]);
    // ---------- Steg mit Ruderboot „Volkseigentum“
    const stegN = -21.5, wasser = -2.35;
    const steg = new THREE.Group();
    for (let off = -19; off <= -8; off += .5) {
        const [e] = U(off, stegN), y = Math.max(H(e, stegN) + .15, wasser + .55);
        mesh(new THREE.BoxGeometry(.46, .06, 1.6), holz, steg, e, y, -stegN);
        if (Math.round(off * 2) % 4 === 0) { for (const dz of [-.75, .75]) { mesh(new THREE.CylinderGeometry(.07, .07, 2.2, 8), dunkelholz, steg, e, y - 1.0, -stegN + dz); } }
    }
    scene.add(steg);
    const boot = new THREE.Group(); boot.name = 'boot';
    const rumpfForm = new THREE.Shape(); rumpfForm.moveTo(-1.9, 0); rumpfForm.quadraticCurveTo(-1.6, -.62, 0, -.66); rumpfForm.quadraticCurveTo(1.6, -.62, 2.1, 0); rumpfForm.lineTo(-1.9, 0);
    const rumpf = new THREE.ExtrudeGeometry(rumpfForm, { depth: 1.0, bevelEnabled: true, bevelSize: .12, bevelThickness: .12, bevelSegments: 3, steps: 1 }); rumpf.translate(0, 0, -.5);
    const rumpfM = mesh(rumpf, std({ color: 0xf2f0e6, roughness: .5 }), boot, 0, .42, 0); rumpfM.scale.set(1, .7, 1);
    mesh(new THREE.BoxGeometry(3.6, .05, .9), std({ color: 0x9a6a40 }), boot, 0, .3, 0);
    for (const x of [-.6, .7]) { mesh(new THREE.BoxGeometry(.25, .05, 1.0), std({ color: 0x9a6a40 }), boot, x, .5, 0); }
    const name = mesh(new THREE.PlaneGeometry(1.8, .26), new THREE.MeshBasicMaterial({ map: texte(w.canvasTex, 512, 72, 'rgba(0,0,0,0)', '#c0392b', ['Volkseigentum'], 'bold 50px Georgia'), transparent: true }), boot, -.1, .32, .63);
    name.castShadow = false;
    const bootPos = U(-11.5, stegN - 1.3);
    boot.position.set(bootPos[0], wasser + .05, -bootPos[1]); boot.rotation.y = 0; scene.add(boot); greifbar(boot, 4, 1.2, 1.4, .4);
    // ---------- Skulpturen: Sandstein mit Bronzetafel; Stahlwelle; Bronzering
    const sand = std({ color: 0xcdb88c, roughness: .95 }), bronze = std({ color: 0x8a6a3a, roughness: .35, metalness: .8 }), rost = std({ color: 0x8a4a2a, roughness: .8, metalness: .3 });
    const stein = new THREE.Group(); stein.name = 'skulptur';
    mesh(new THREE.BoxGeometry(1.0, .5, 1.0), sand, stein, 0, .25, 0);
    const s1 = mesh(new THREE.DodecahedronGeometry(.7, 0), sand, stein, 0, 1.05, 0); s1.scale.set(.8, 1.1, .7);
    const s2 = mesh(new THREE.DodecahedronGeometry(.45, 0), sand, stein, .1, 1.95, 0); s2.rotation.set(.4, .7, .2);
    const tafelB = mesh(new THREE.BoxGeometry(.5, .3, .03), bronze, stein, 0, .32, -.52);
    mesh(new THREE.PlaneGeometry(.3, .18), std({ color: 0x4a6a3a, roughness: 1 }), stein, -.06, .38, -.537).rotation.y = Math.PI;   // Moos
    stein.rotation.y = -.3; at(stein, U(-22, -14)); eigene.push([...U(-22, -14), 1.0]);
    const andere = new THREE.Group(); andere.name = 'skulpturen';
    const kurve = new THREE.CatmullRomCurve3([new THREE.Vector3(-1.2, .1, 0), new THREE.Vector3(-.5, 1.6, .2), new THREE.Vector3(.3, .5, -.2), new THREE.Vector3(1.1, 2.0, 0)]);
    mesh(new THREE.TubeGeometry(kurve, 40, .12, 8, false), rost, andere, 0, 0, 0).scale.set(1, 1, 3);
    const ring = mesh(new THREE.TorusGeometry(.8, .12, 12, 40), bronze, andere, 2.6, 1.0, .6); ring.rotation.y = .6;
    mesh(new THREE.BoxGeometry(.5, .2, .5), sand, andere, 2.6, .1, .6);
    at(andere, U(-29, -8)); eigene.push([...U(-29, -8), 1.6], [...U(-29 + 2.6, -8 - .6), .9]);
    // ---------- Wegweiser zur Straße (Reisekarte)
    const weg = wegweiser(w, [['Innenstadt', 2.6, .3], ['Hallenbad', 2.25, -.6], ['Rathaus', 1.9, 2.4]]);
    at(weg, U(-33, -27)); eigene.push([...U(-33, -27), .45]);
    const fluss = (n) => riverX(n);
    const frei = (x, z) => {
        const e = x, n = -z;
        if (n < -46 || n > -3) { return false; }
        const off = e - fluss(n);
        if (off > -14 || off < -36) { return false; }
        return !imKreis(w.blockers, e, n) && !imKreis(eigene, e, n);
    };
    const boden = []; scene.traverse((o) => { if (o.name === 'ground') { boden.push(o); } });
    return {
        scene, frei, boden, hoehe: (x, z) => H(x, -z),
        kamera: (p) => {
            // vom Wasser aus auf das Ufer: Kanuverleih, Skulpturen, Deich; im Hintergrund die Radbrücke
            const [ce, cn] = U(-23, -23), n = -p.z;
            const te = ce + (p.x - ce) * .45, tn = cn + (n - cn) * .45;
            const t = new THREE.Vector3(te, H(te, tn) + 1.0, -tn);
            return { p: new THREE.Vector3(riverX(tn - 8) + 1, 4.6, -(tn - 8)), t };
        },
        spawn: { karte: [...P(U(-31, -24)), Math.PI / 2], steg: [...P(U(-16, -24)), Math.PI / 2] },
        hotspots: [
            { obj: huette, stand: P(U(-25, -34.5)), eintraege: [{ id: 'verleih' }] },
            { obj: boot, stand: P(U(-15.5, -23.5)), eintraege: [{ id: 'boot' }] },
            { obj: stein, stand: P(U(-21, -16.2)), eintraege: [{ id: 'skulptur' }] },
            { obj: andere, stand: P(U(-26, -10.5)), eintraege: [{ id: 'skulpturen' }] },
            { obj: weg, stand: P(U(-32, -26)), eintraege: [{ id: 'zurkarte' }] }
        ],
        npcs: [{ hotspot: 'joanna', figur: 'johanna', pos: P(U(-19.5, -27)), rot: 2.2, pose: 'stehen', stand: P(U(-21.5, -25.6)) }],
        // Johanna heftet den Stadtplan an die Preistafel
        dinge: [{ obj: plan, sichtbar: (b) => !!b.flags.stadtplanWeg }],
        bootFlucht: bootFlucht(scene, w, boot),
        tick() { }
    };
}

// ---------- Bootsflucht: Ruderboot fährt flussab (nach Süden), Treibgut von vorn, Verfolger dahinter ----------
function bootFlucht(scene, w, bootVorlage) {
    const { riverX } = w, wasser = -2.35, LAENGE = 62, START_N = -40, TEMPO = 1.4;
    const gruppe = new THREE.Group(); gruppe.visible = false; scene.add(gruppe);
    const ruder = bootVorlage.clone(); ruder.children = ruder.children.filter((c) => !(c.material && c.material.visible === false)); gruppe.add(ruder);
    // Motorboot der Verfolger
    const motor = new THREE.Group();
    const mr = new THREE.Shape(); mr.moveTo(-2.2, 0); mr.lineTo(2.0, 0); mr.lineTo(2.8, .55); mr.lineTo(-2.2, .55);
    const mg = new THREE.ExtrudeGeometry(mr, { depth: 1.5, bevelEnabled: false }); mg.translate(0, -.15, -.75);
    mesh(mg, std({ color: 0x1d4f9a, roughness: .4, metalness: .2 }), motor);
    mesh(new THREE.BoxGeometry(.6, .7, .5), std({ color: 0x222222, metalness: .5 }), motor, -2.4, .3, 0);
    mesh(new THREE.BoxGeometry(.05, .5, 1.3), std({ color: 0xbfd6e2, transparent: true, opacity: .5 }), motor, .9, .65, 0);
    gruppe.add(motor);
    // Treibgut je Art
    const holz = std({ color: 0x6a4a2a, roughness: .9 });
    const mach = {
        Treibholz: () => { const g = new THREE.Group(); mesh(new THREE.CylinderGeometry(.22, .26, 3.2, 10), holz, g).rotation.z = Math.PI / 2; mesh(new THREE.CylinderGeometry(.08, .1, 1.2, 6), holz, g, .6, .3, .3).rotation.set(.5, 0, .9); return g; },
        Entenfamilie: () => { const g = new THREE.Group(); const ente = (x, z, s, c) => { mesh(new THREE.SphereGeometry(.28 * s, 12, 10), std({ color: c }), g, x, .15 * s, z).scale.set(1.4, .8, 1); mesh(new THREE.SphereGeometry(.14 * s, 10, 8), std({ color: c === 0xf2d23a ? c : 0x2e6a3a }), g, x + .3 * s, .38 * s, z); mesh(new THREE.ConeGeometry(.05 * s, .14 * s, 6), std({ color: 0xe67e22 }), g, x + .46 * s, .36 * s, z).rotation.z = -Math.PI / 2; };
            ente(0, 0, 1.4, 0x8a6a4a); for (let i = 0; i < 4; i++) { ente(-.8 - i * .5, (i % 2 - .5) * .5, .6, 0xf2d23a); } return g; },
        Schild: () => { const g = new THREE.Group(); mesh(new THREE.BoxGeometry(1.8, .05, .5), holz, g); const s = mesh(new THREE.PlaneGeometry(1.8, .7), std({ map: texte(w.canvasTex, 384, 150, '#f2d230', '#1a1a1a', ['Alternative', 'Route →'], 'bold 52px Arial'), side: THREE.DoubleSide }), g, 0, .6, 0); s.rotation.y = Math.PI / 2; return g; },
        Plakat: () => { const g = new THREE.Group(); const s = mesh(new THREE.PlaneGeometry(1.8, 1.2), std({ map: texte(w.canvasTex, 384, 256, '#2b3f86', '#ffffff', ['Grenzen', 'dicht!'], 'bold 72px Arial'), side: THREE.DoubleSide }), g, 0, .1, 0); s.rotation.set(-1.2, Math.PI / 2, 0); return g; }
    };
    const blase = {};
    function sprechblase(text) {
        if (!blase[text]) {
            const t = w.canvasTex(512, 128, (g, cw, ch) => { g.fillStyle = '#ffffff'; g.strokeStyle = '#000'; g.lineWidth = 8; g.beginPath(); g.roundRect(6, 6, cw - 12, ch - 34, 20); g.fill(); g.stroke();
                g.beginPath(); g.moveTo(60, ch - 30); g.lineTo(40, ch - 4); g.lineTo(100, ch - 30); g.fill(); g.fillStyle = '#c0392b'; g.font = 'bold 40px Arial'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(text, cw / 2, (ch - 28) / 2 + 4); });
            t.wrapS = t.wrapT = THREE.ClampToEdgeWrapping;
            blase[text] = new THREE.SpriteMaterial({ map: t, depthTest: false });
        }
        const s = new THREE.Sprite(blase[text]); s.scale.set(5.2, 1.3, 1); s.renderOrder = 10; return s;
    }
    const objekte = new Map();
    let figurPlaetze = null;
    const nPos = (sim, x) => START_N - TEMPO * sim.zeit - (x - .55) * LAENGE;
    const spurOff = (s) => (s - 1) * 5.5;
    const welt = (sim, x, spur, y) => { const n = nPos(sim, x); return new THREE.Vector3(riverX(n) + spurOff(spur), y, -n); };
    return {
        start(figuren, npcs) {
            gruppe.visible = true; bootVorlage.visible = false;
            for (const [, o] of objekte) { gruppe.remove(o); } objekte.clear();
            figurPlaetze = { figuren, npcs };
        },
        ende() { gruppe.visible = false; bootVorlage.visible = true; for (const [, o] of objekte) { gruppe.remove(o); } objekte.clear(); },
        bild(sim, t) {
            const nB = nPos(sim, .55), wackel = Math.sin(t * 3) * .04;
            const pB = welt(sim, .55, sim.spur, wasser + .05 + wackel);
            // Spurwechsel weich: Position des Boots nachziehen
            ruder.userData.x = ruder.userData.x === undefined ? pB.x : ruder.userData.x + (pB.x - ruder.userData.x) * Math.min(1, .2);
            ruder.position.set(ruder.userData.x, pB.y, pB.z); ruder.rotation.set(0, -Math.PI / 2, Math.sin(t * 2) * .03);
            // Joe und Chani sitzen im Boot (Oberkörper ragen heraus)
            const [joe, chani] = figurPlaetze.figuren;
            joe.position.set(ruder.position.x, ruder.position.y - .38, ruder.position.z - .6); joe.rotation.y = Math.PI;
            chani.position.set(ruder.position.x, ruder.position.y - .38, ruder.position.z + .75); chani.rotation.y = 0;
            // Verfolger: zielen auf eure Spur, sinken am Ende
            const sinkt = sim.sinken === null ? 0 : sim.sinken;
            const pM = welt(sim, .12, sim.gegnerSpur, wasser + .15 - sinkt * .8 + Math.sin(t * 6) * .05);
            motor.position.copy(pM); motor.rotation.set(sinkt * .3, -Math.PI / 2, sinkt * .25);
            const [pinsel, ledeiw] = figurPlaetze.npcs;
            pinsel.visible = ledeiw.visible = sinkt < 2.2;
            pinsel.position.set(pM.x - .35, pM.y - .25, pM.z + .5); ledeiw.position.set(pM.x + .35, pM.y - .25, pM.z - .6);
            pinsel.rotation.y = ledeiw.rotation.y = 0;
            // Treibgut und Geschosse
            const lebend = new Set();
            for (const h of sim.hindernisse) {
                let o = objekte.get(h);
                if (!o) { const art = Object.keys(mach).find((k) => h.art.indexOf(k) === 0) || 'Treibholz'; o = mach[art](); gruppe.add(o); objekte.set(h, o); }
                o.position.copy(welt(sim, h.x, h.spur, wasser + .12 + Math.sin(t * 2 + h.x * 9) * .05)); o.rotation.y = Math.sin(h.x * 7) * .3;
                lebend.add(h);
            }
            for (const f of sim.schuesse) {
                let o = objekte.get(f);
                if (!o) { o = sprechblase(f.text); gruppe.add(o); objekte.set(f, o); }
                o.position.copy(welt(sim, f.x, f.spur, wasser + 1.8));
                lebend.add(f);
            }
            for (const [k, o] of objekte) { if (!lebend.has(k)) { gruppe.remove(o); objekte.delete(k); } }
            return nB;
        },
        kamera(sim) {
            const nB = nPos(sim, .5), e = riverX(nB);
            return { p: new THREE.Vector3(e + 24, 15, -nB + 4), t: new THREE.Vector3(e - 1, wasser, -nB - 2) };
        }
    };
}

// ======================================================================
// Baustelle am Hallenbad (Rasen südöstlich der Halle)
// ======================================================================
export async function baustelle(renderer) {
    const { baue } = await import('./welt_bad.js');
    const daten = await laden('welt_bad.json');
    const w = baue(renderer, daten);
    const { scene, H, MW1, MW2, MWN } = w;
    // Das LoD2-Hallenbad (Wände, Dächer über seinem Grundriss, die Rutsche) weicht dem nachgebauten Gebäude (hallenbad_eingang.js)
    {
        const bad = []; daten.lod2.forEach((b) => { if (b.s) { bad.push(...b.g); } });
        const imBad = (x, z) => bad.some(([a, b, c]) => {
            const d1 = (x - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (z - b[1]), d2 = (x - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (z - c[1]), d3 = (x - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (z - a[1]);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        });
        const raus = [];
        scene.traverse((o) => {
            if (!o.isMesh || o.parent !== scene) { return; }
            if (o.name === 'pool' || o.name === 'slide') { raus.push(o); return; }
            const g = o.geometry, p = g.attributes.position;
            if (o.material && o.material.color && o.material.color.getHex() === 0xf2c12e) {   // Stützen der Rutsche
                g.computeBoundingBox(); const c = g.boundingBox.getCenter(new THREE.Vector3()); if (imBad(c.x, c.z) || Math.hypot(c.x - 5, c.z + 26) < 25) { raus.push(o); } return;
            }
            if (o.material && o.material.vertexColors && o.material.flatShading && !g.index && p) {   // Dächer der LoD2-Gebäude: nur die über dem Bad
                const behalten = [];
                for (let i = 0; i < p.count; i += 3) { const x = (p.getX(i) + p.getX(i + 1) + p.getX(i + 2)) / 3, z = (p.getZ(i) + p.getZ(i + 1) + p.getZ(i + 2)) / 3; if (!imBad(x, z)) { behalten.push(i); } }
                for (const name of Object.keys(g.attributes)) {
                    const at = g.attributes[name], n = at.itemSize, neu = new at.array.constructor(behalten.length * 3 * n);
                    behalten.forEach((i, k) => { for (let j = 0; j < 3 * n; j++) { neu[k * 3 * n + j] = at.array[i * n + j]; } });
                    g.setAttribute(name, new THREE.BufferAttribute(neu, n, at.normalized));
                }
            }
        });
        raus.forEach((o) => scene.remove(o));
    }
    // Ortsrahmen des Eingangs (hallenbad_eingang.js): x entlang der Eingangsfront, z zum Platz hin. Die Seitenwand des
    // vorspringenden Umkleidetrakts (lokal x = 4,3) liegt auf der LoD2-Wand an der Ecke (E −18,5 / N −8,1).
    const L = Math.hypot(MW2[0] - MW1[0], MW2[1] - MW1[1]), d = [(MW2[0] - MW1[0]) / L, (MW2[1] - MW1[1]) / L];
    const nullE = -18.5 - 4.3 * d[0], nullN = -8.1 - 4.3 * d[1], ROT = Math.atan2(d[1], d[0]);
    const y0 = H(nullE + 1.4 * d[0] + 3 * MWN[0], nullN + 1.4 * d[1] + 3 * MWN[1]);
    const ort = new THREE.Group(); ort.position.set(nullE, y0, -nullN); ort.rotation.y = ROT; scene.add(ort); ort.updateMatrixWorld(true);
    const welt = (x, z) => { const v = ort.localToWorld(new THREE.Vector3(x, 0, z)); return [v.x, v.z]; };
    const lokal = (x, z) => { const v = ort.worldToLocal(new THREE.Vector3(x, y0, z)); return [v.x, v.z]; };
    const blick = (r) => r + ROT;   // Drehung im Ortsrahmen → Welt
    // Was die Welt schon an dieser Stelle hatte (grober Pavillon, Wandbild, Tür, Vordach, Leuchte, Bügel, Pflaster), weicht
    const VX = [-16.5, 22.5], VZ = [-1.2, 18.5];
    const imVorplatz = (x, z, rand = 0) => { const [a, b] = lokal(x, z); return a > VX[0] - rand && a < VX[1] + rand && b > VZ[0] - rand && b < VZ[1] + rand; };
    const weg = [];
    scene.traverse((o) => {
        if (o === w.ground || o === ort || !o.parent || o.parent !== scene) { return; }
        if (['mural', 'door', 'globelamp', 'bench'].includes(o.name) || o.getObjectByName('esign')) { weg.push(o); return; }
        if (o.isMesh && (o.name === 'ground' || o.geometry.type === 'TorusGeometry')) {
            o.geometry.computeBoundingBox(); const c = o.geometry.boundingBox.getCenter(new THREE.Vector3()); o.localToWorld(c);
            if (imVorplatz(c.x, c.z)) { weg.push(o); }
        }
    });
    // Bäume am Platz (sie stünden mitten im Bild) und die Laufbahn, soweit sie über den Platz reicht
    scene.traverse((o) => {
        if (!o.isMesh || o.parent !== scene || weg.includes(o)) { return; }
        if (o.material && o.material.color && o.material.color.getHex() === 0xf2c12e && o.geometry.type !== 'TubeGeometry') {   // gelbe Geländer der alten Welt
            o.geometry.computeBoundingBox(); const c = o.geometry.boundingBox.getCenter(new THREE.Vector3()); if (imVorplatz(c.x, c.z, 6)) { weg.push(o); }
        }
        if (o.material === w.M.bark) { o.geometry.computeBoundingBox(); const c = o.geometry.boundingBox.getCenter(new THREE.Vector3()); if (imVorplatz(c.x, c.z, 4)) { weg.push(o); } }
        if (o.geometry.type === 'RingGeometry') {
            o.updateMatrixWorld(true); const p = o.geometry.attributes.position, v = new THREE.Vector3(), inv = o.matrixWorld.clone().invert();
            for (let i = 0; i < p.count; i++) { v.fromBufferAttribute(p, i).applyMatrix4(o.matrixWorld); if (imVorplatz(v.x, v.z, 2)) { v.y = y0 - .6; v.applyMatrix4(inv); p.setXYZ(i, v.x, v.y, v.z); } }
            p.needsUpdate = true;
        }
    });
    weg.forEach((o) => scene.remove(o));
    // Gelände unter dem Vorplatz eben (knapp unter dem Pflaster)
    {
        const p = w.ground.geometry.attributes.position;
        for (let i = 0; i < p.count; i++) {
            if (!imVorplatz(p.getX(i), p.getZ(i), 1.5)) { continue; }
            const [a, b] = lokal(p.getX(i), p.getZ(i)), inGrube = a > -4.8 && a < 4.6 && b > 1.6 && b < 9.6;   // Grube (hallenbad_eingang.js) frei lassen
            p.setY(i, Math.min(p.getY(i), y0 - (inGrube ? 1.8 : .06)));
        }
        p.needsUpdate = true; w.ground.geometry.computeVertexNormals();
    }
    const e = eingangHallenbad(ort, { baustelle: true, welt: true });
    // Bauwagen links auf dem Platz (Tür zum Platz hin)
    const schwarz = std({ color: 0x222222, roughness: .7 });
    const wagen = new THREE.Group(); wagen.name = 'bauwagen';
    mesh(new THREE.BoxGeometry(5, 2.3, 2.3), std({ color: 0x3a7a4a, roughness: .7 }), wagen, 0, 1.6, 0);
    mesh(new THREE.CylinderGeometry(1.25, 1.25, 5.04, 20, 1, false, 0, Math.PI), std({ color: 0x5a5a5a }), wagen, 0, 2.75, 0).rotation.set(0, 0, Math.PI / 2);
    for (const x of [-1.6, 1.6]) { for (const z of [-1.0, 1.0]) { mesh(new THREE.CylinderGeometry(.38, .38, .2, 16), schwarz, wagen, x, .38, z).rotation.x = Math.PI / 2; } }
    mesh(new THREE.BoxGeometry(.9, 1.8, .05), std({ color: 0x2a4a3a }), wagen, 1.4, 1.4, 1.17);
    for (const x of [-1.2, 0]) { mesh(new THREE.BoxGeometry(.8, .6, .04), std({ color: 0xbfd6e2, roughness: .1, metalness: .3 }), wagen, x, 1.9, 1.17); }
    wagen.position.set(-12.6, 0, 9.0); ort.add(wagen);
    // Bauzaun um den Platz, Tor vorn links, davor der Weg zur Karte
    const zaunTex = w.canvasTex(128, 128, (g, cw, ch) => { g.clearRect(0, 0, cw, ch); g.strokeStyle = '#c8ccd0'; g.lineWidth = 3; for (let i = 0; i <= 8; i++) { g.beginPath(); g.moveTo(i * 16, 0); g.lineTo(i * 16, ch); g.moveTo(0, i * 16); g.lineTo(cw, i * 16); g.stroke(); } });
    const gitter = new THREE.MeshStandardMaterial({ map: zaunTex, transparent: true, alphaTest: .3, side: THREE.DoubleSide, metalness: .6, roughness: .4 });
    const fuss = std({ color: 0xc8483a, roughness: .9 }), rohr = std({ color: 0xb0b4b8, metalness: .7, roughness: .4 });
    const zaun = (x0, z0, x1, z1) => {
        const l = Math.hypot(x1 - x0, z1 - z0), k = Math.max(1, Math.round(l / 3.5));
        for (let i = 0; i < k; i++) {
            const a = i / k, b = (i + 1) / k, xa = x0 + (x1 - x0) * a, za = z0 + (z1 - z0) * a, xb = x0 + (x1 - x0) * b, zb = z0 + (z1 - z0) * b;
            const p = new THREE.Group(), len = Math.hypot(xb - xa, zb - za);
            const f = mesh(new THREE.PlaneGeometry(len, 1.9), gitter, p, 0, 1.15, 0); f.castShadow = false; f.userData.durchlaessig = true;
            for (const x of [-len / 2, len / 2]) { mesh(new THREE.CylinderGeometry(.03, .03, 2.0, 6), rohr, p, x, 1.1, 0); mesh(new THREE.BoxGeometry(.6, .15, .25), fuss, p, x, .08, 0); }
            mesh(new THREE.BoxGeometry(len, .04, .04), rohr, p, 0, 2.08, 0);
            p.position.set((xa + xb) / 2, 0, (za + zb) / 2); p.rotation.y = -Math.atan2(zb - za, xb - xa); ort.add(p);
        }
    };
    zaun(-16.2, 18, -10.6, 18); zaun(-7.4, 18, 22.2, 18); zaun(-16.2, 4.6, -16.2, 18); zaun(22.2, 11.5, 22.2, 18);
    const tor = new THREE.Group(); tor.name = 'tor';
    const torFluegel = mesh(new THREE.PlaneGeometry(2.6, 1.9), gitter, tor, -1.0, 1.15, .9); torFluegel.rotation.y = -.9; torFluegel.userData.durchlaessig = true;
    mesh(new THREE.PlaneGeometry(1.2, .5), std({ map: texte(w.canvasTex, 256, 104, '#ffffff', '#c0392b', ['Baustelle', 'Betreten verboten'], 'bold 28px Arial'), side: THREE.DoubleSide }), tor, 2.4, 1.7, .02);
    tor.position.set(-9, 0, 18); ort.add(tor); greifbar(tor, 3.4, 2.4, 1.2, 1.2);
    // Wegweiser zur Radbrücke rechts
    const zurBruecke = new THREE.Group(); zurBruecke.name = 'zurbruecke';
    mesh(new THREE.CylinderGeometry(.05, .05, 2.4, 8), rohr, zurBruecke, 0, 1.2, 0);
    const brTex = texte(w.canvasTex, 256, 60, '#f4f2ea', '#1a3a6a', ['Radbrücke / Ems →'], 'bold 28px Arial');
    mesh(new THREE.BoxGeometry(1.3, .3, .03), [rohr, rohr, rohr, rohr, std({ map: brTex }), std({ map: brTex })], zurBruecke, .5, 2.1, 0);
    greifbar(zurBruecke, 1.4, 2.4, .6); zurBruecke.position.set(20.6, 0, 14.6); zurBruecke.rotation.y = .4; ort.add(zurBruecke);

    // ---------- Begehbar: der Vorplatz bis zum Zaun, nicht in Grube, Gebäude und Möbel (alles im Ortsrahmen)
    const kreise = [[-.2, .9, .45], [-10.4, 3.4, .7], [-11.6, 8.5, .7], [-9.7, 7.2, .5], [-6.4, 5.2, .9], [4.6, 9.2, .5], [6.6, 8.6, .3], [8.2, 8.6, .3],
        [2.9, 6.4, .35], [4.7, 7.4, .45], [13.6, 9.0, .35], [20.6, 14.6, .4]];
    const strecken = [[-8.6, 10.2, -4.9, 10.2], [-3.4, 10.8, 1.6, 10.8], [3.8, 10.2, 8.6, 10.2], [12.6, 7.4, 12.6, 10.0], [12.6, 10.0, 18, 10.4]];
    const nahStrecke = (x, z, [a, b, c, dd]) => { const vx = c - a, vz = dd - b, t = Math.max(0, Math.min(1, ((x - a) * vx + (z - b) * vz) / (vx * vx + vz * vz))); return Math.hypot(x - a - vx * t, z - b - vz * t) < .35; };
    const freiLokal = (x, z) => {
        if (x < -15.6 || x > 21.6 || z < .45 || z > 17.4) { return false; }
        if (x > 3.6 && z < 7.4) { return false; }                     // Umkleidetrakt mit Hecke
        if (x > -4.7 && x < 4.5 && z > 2.0 && z < 9.3) { return false; } // Grube mit Bauzaun
        if (x < -8.2 && z < 3.4) { return false; }                     // Hecke und Baum links
        if (x > -15.4 && x < -9.8 && z > 7.6 && z < 10.4) { return false; }   // Bauwagen
        if (kreise.some(([a, b, r]) => Math.hypot(x - a, z - b) < r)) { return false; }
        return !strecken.some((s) => nahStrecke(x, z, s));
    };
    const frei = (x, z) => { const [a, b] = lokal(x, z); return freiLokal(a, b); };
    const boden = []; scene.traverse((o) => { if (o.name === 'ground') { boden.push(o); } });
    const W = (x, z) => welt(x, z);
    const kv = (x, y, z) => ort.localToWorld(new THREE.Vector3(x, y, z));
    return {
        scene, frei, boden, hoehe: (x, z) => (imVorplatz(x, z) ? y0 : H(x, -z)),
        kamera: (p) => {
            const [lx, lz] = lokal(p.x, p.z), x = Math.max(-9, Math.min(14, lx));
            return { p: kv(x * .55 + 1.5, 7.6, Math.min(lz, 15) + 11.5), t: kv(x * .75 + .4, 1.5, Math.min(lz, 15) - 3.5) };
        },
        spawn: { karte: [...W(-9, 15.8), blick(Math.PI)], von_bruecke: [...W(18.6, 13.6), blick(-Math.PI / 2)] },
        hotspots: [
            { obj: e.leitung, stand: W(-5.2, 5.0), eintraege: [{ id: 'leitung' }] },
            { obj: e.grube, stand: W(2.6, 9.8), eintraege: [{ id: 'grube' }] },
            { obj: e.wandbild, stand: W(-6.4, 1.6), eintraege: [{ id: 'wandbild' }] },
            { obj: e.schild, stand: W(2.2, 1.3), eintraege: [{ id: 'schild' }] },
            { obj: e.buchstaben, stand: W(-8.4, 14.4), eintraege: [{ id: 'buchstaben' }], klickbar: (b) => !!b.flags.gegnerBaustelleWeg && !b.flags.schildHeil },
            { obj: tor, stand: W(-9, 16.6), eintraege: [{ id: 'zurkarte' }] },
            { obj: zurBruecke, stand: W(19.6, 13.8), eintraege: [{ tor: 'radbruecke', spawn: 'von_baustelle', name: 'Weg zur Radbrücke', ansehen: 'Ein Schild: „Radbrücke / Ems“. Dahinter die gelben Laternen.' }] },
            { obj: wagen, stand: W(-10.6, 11.4), eintraege: [{ deko: 'Bauwagen', text: 'Der Bauwagen. Drinnen: Kaffeemaschine, Bauplan, Gummistiefel.' }] },
            { obj: e.bauschild, stand: W(7.4, 9.9), eintraege: [{ deko: 'Bauschild', text: '„Stadt Greven – Sanierung Hallenbad. Bauleitung: Frau Kemper. Wiedereröffnung: bald.“ Das Datum ist überklebt.' }] }
        ],
        npcs: [
            { hotspot: 'bauleiter', figur: 'kemper', pos: W(-6.8, 7.6), rot: blick(.6), pose: 'stehen', stand: W(-7.2, 8.9) },
            // Aus der Ems durch das Bauzaun-Tor herein zur Leiter (Pinsel steigt hinauf, Ledeiw hält sie); später rückwärts in den Bauwagen
            { hotspot: 'gegner', figur: 'pinsel', tempo: 2.3, pos: W(-.2, .55), rot: blick(Math.PI), pose: 'benutzen', hoch: 1.05, stand: W(-2.8, 1.4),
                von: [W(-9, 20), W(-9, 15), W(-4.75, 11.5), W(-4.75, 1.5)], nach: [W(-4.75, 1.5), W(-4.75, 11.5), W(-10.4, 11.8), W(-11.2, 10.3)], rueckwaerts: true },
            { hotspot: 'gegner', figur: 'ledeiw', tempo: 2.3, pos: W(.9, 1.6), rot: blick(-2.4), pose: 'stehen', stand: W(-2.8, 1.4),
                von: [W(-9.8, 20), W(-9.8, 15.2), W(-4.75, 11.8), W(-4.75, 1.9)], nach: [W(-4.75, 1.9), W(-4.75, 11.8), W(-10.8, 12.1), W(-11.0, 10.3)], rueckwaerts: true }
        ],
        dinge: [
            { obj: e.siegel, sichtbar: (b) => !!b.flags.grubeTrocken && !b.inventar.includes('siegel_judikative'), art: 'nehmen' },
            // Leiter und abgeschraubte Buchstaben gibt es erst, wenn Pinsel und Ledeiw da sind; Joe schraubt sie wieder an
            { obj: e.leiter, sichtbar: (b) => !!b.flags.gegnerBaustelle && !b.flags.schildHeil, art: 'schwinden' },
            { obj: e.buchstaben, sichtbar: (b) => !!b.flags.gegnerBaustelle && !b.flags.schildHeil, art: 'schwinden' }
        ],
        aktualisieren(z) {
            const f = z.flags;
            e.strahl.visible = !f.leckDicht && !f.grubeTrocken;   // das Leck ist dicht …
            e.wasser.visible = !f.grubeTrocken;                    // … dann pumpt die Grube leer
            e.schildMat.map = f.gegnerBaustelle && !f.schildHeil ? e.schildKaputt : e.schildHeil;
        },
        helme: (z) => !!z.flags.helme,
        tick(dt, t) {
            e.strahl.children.forEach((s, i) => { const k = ((t * 1.6 + i / 9) % 1); s.position.set(k * .5, -k * .9, k * .4); s.scale.setScalar(1 - k * .4); });
        }
    };
}

// ======================================================================
// Radbrücke beim Hallenbad (gelbe Doppellaternen; in der dritten ruht das Siegel der Ausführung)
// ======================================================================
/**
 * Stadt im Hintergrund der Radbrücke: Gebäude aus OpenStreetMap (umgebung_radbruecke.json, erzeugt mit
 * tools/Welten/umgebung.mjs), lagerichtig zur echten Brücke, als schlichte Klötze mit Flachdach in einem einzigen Mesh.
 */
function stadtImHintergrund(scene, H, daten) {
    const pos = [], farbe = [], idx = [];
    const wandFarben = [0xc9b8a0, 0xb5684f, 0xd8d0c2, 0xa65a45, 0xe2dccf, 0x9c8a74], dachFarbe = new THREE.Color(0x6b5a52);
    let k = 0;
    for (const g of daten.gebaeude) {
        const p = []; for (let i = 0; i < g.p.length; i += 2) { p.push([g.p[i], g.p[i + 1]]); }
        let boden = Infinity; for (const [e, n] of p) { boden = Math.min(boden, H(e, n)); }
        const unten = boden - .5, oben = boden + g.h, wand = new THREE.Color(wandFarben[(k++ * 7) % wandFarben.length]);
        if (g.name === 'Hallenbad Greven') { wand.set(0xe8eef0); }
        // Wände
        for (let i = 0; i < p.length; i++) {
            const [e0, n0] = p[i], [e1, n1] = p[(i + 1) % p.length], b = pos.length / 3;
            pos.push(e0, unten, -n0, e1, unten, -n1, e1, oben, -n1, e0, oben, -n0);
            for (let j = 0; j < 4; j++) { farbe.push(wand.r, wand.g, wand.b); }
            idx.push(b, b + 1, b + 2, b, b + 2, b + 3);
        }
        // Dach (Polygon trianguliert)
        const b = pos.length / 3;
        for (const [e, n] of p) { pos.push(e, oben, -n); farbe.push(dachFarbe.r, dachFarbe.g, dachFarbe.b); }
        for (const t of THREE.ShapeUtils.triangulateShape(p.map(([e, n]) => new THREE.Vector2(e, n)), [])) { idx.push(b + t[0], b + t[2], b + t[1]); }
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); geo.setAttribute('color', new THREE.Float32BufferAttribute(farbe, 3));
    geo.setIndex(idx); geo.computeVertexNormals();
    const m = new THREE.Mesh(geo, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .85, side: THREE.DoubleSide }));
    m.castShadow = false; m.receiveShadow = true; m.name = 'stadt'; scene.add(m);
    return m;
}

export async function radbruecke(renderer) {
    const { baue } = await import('./welt_ems.js');
    const [w, umgebung] = await Promise.all([laden('welt_ems.json').then((d) => baue(renderer, d)), laden('umgebung_radbruecke.json')]);
    const { scene, H, W3, riverX, walkH, onBridge } = w;
    stadtImHintergrund(scene, H, umgebung);
    scene.fog = new THREE.Fog(0xdfe2e1, 160, 820);   // weiter Blick: die Stadt mit dem Hallenbad hinter dem Deich
    const U = (off, n) => [riverX(n) + off, n];
    const at = (o, [e, n], dy = 0) => { o.position.copy(W3(e, n, dy)); scene.add(o); return o; };
    // Laternen auf der Brücke (vom Hallenbad, also von Westen, aus gezählt): die dritte steht bei e = −10, Nordseite
    let dritte = null;
    scene.children.forEach((c) => { if (c.isGroup && c.children.some((k) => k.isPointLight) && Math.abs(c.position.x + 10) < .5 && c.position.z < 0) { dritte = c; } });
    const klappe = new THREE.Group();
    const kl = mesh(new THREE.BoxGeometry(.04, .3, .22), std({ color: 0xd4a017, roughness: .5 }), klappe, 0, .15, 0);
    klappe.position.set(-.12, .2, 0); if (dritte) { dritte.add(klappe); greifbar(dritte, 1.2, 5.5, 1.2); }
    // Ruderboot liegt am Ufer (nach der Flucht)
    const boot = new THREE.Group();
    const rumpfForm = new THREE.Shape(); rumpfForm.moveTo(-1.9, 0); rumpfForm.quadraticCurveTo(-1.6, -.62, 0, -.66); rumpfForm.quadraticCurveTo(1.6, -.62, 2.1, 0); rumpfForm.lineTo(-1.9, 0);
    const rumpf = new THREE.ExtrudeGeometry(rumpfForm, { depth: 1.0, bevelEnabled: true, bevelSize: .12, bevelThickness: .12, bevelSegments: 3, steps: 1 }); rumpf.translate(0, 0, -.5);
    mesh(rumpf, std({ color: 0xf2f0e6, roughness: .5 }), boot, 0, .42, 0).scale.set(1, .7, 1);
    boot.rotation.set(.12, .5, .05); at(boot, U(-14.2, -7), -.1);
    // Weg zur Baustelle
    // Wegweiser oben auf dem Deich, nahe dem Brückenkopf
    const weg = wegweiser(w, [['Hallenbad / Baustelle', 2.6, -2.2], ['Kanuverleih', 2.25, .9]]);
    at(weg, U(-47, -7));
    const eigene = [[...U(-47, -7), .45], [...U(-14.2, -7), 2.2]];
    const frei = (x, z) => {
        const e = x, n = -z;
        if (onBridge(e, n)) { return Math.abs(n) < 1.7 && e < riverX(0) + 2; }
        if (n < -16 || n > 12) { return false; }
        const off = e - riverX(n);
        // Ufer und Deich bis knapp hinter die Deichkrone: Dort (e = −49) endet die Brücke auf Kronenhöhe, und es geht ganz
        // normal auf das Deck; seitlich (über das Geländer) nicht
        if (off > -13.6 || off < -55) { return false; }
        return !imKreis(w.blockers, e, n) && !imKreis(eigene, e, n);
    };
    const boden = []; scene.traverse((o) => { if (o.name === 'ground') { boden.push(o); } });
    const mitte = W3(...U(-16, 0));
    return {
        scene, frei, boden, hoehe: (x, z) => walkH(x, -z),
        kamera: (p) => { const t = p.clone().lerp(mitte, .35); t.y = p.y + 1.4; return { p: new THREE.Vector3(t.x + 9, t.y + 4.2, t.z + 13), t }; },
        spawn: { boot: [...U(-17, -8)].map((v, i) => (i ? -v : v)).concat([0]), von_baustelle: [...U(-48.5, -8.5)].map((v, i) => (i ? -v : v)).concat([Math.PI / 2]), karte: [...U(-48.5, -8.5)].map((v, i) => (i ? -v : v)).concat([Math.PI / 2]) },
        hotspots: [
            ...(dritte ? [{ obj: dritte, stand: [-10.9, -1.1], eintraege: [{ id: 'laterne' }, { deko: 'Dritte Laterne', text: 'Die Klappe am Sockel steht offen. Leer – das Siegel habt ihr schon.' }] }] : []),
            { obj: weg, stand: (() => { const [e, n] = U(-47, -8.2); return [e, -n]; })(), eintraege: [{ tor: 'baustelle', spawn: 'von_bruecke', name: 'Weg zur Baustelle', ansehen: 'Ein Wegweiser: Hallenbad / Baustelle.' }] },
            { obj: boot, stand: (() => { const [e, n] = U(-17.5, -9.5); return [e, -n]; })(), eintraege: [{ deko: 'Ruderboot „Volkseigentum“', text: 'Das Ruderboot liegt am Ufer. Johanna holt es später ab – vergesellschaftet ist vergesellschaftet.' }] }
        ],
        npcs: [],
        aktualisieren(z) { klappe.rotation.y = z.flags.klappeAuf || z.inventar.includes('siegel_exekutive') ? -1.6 : 0; },
        tick() { }
    };
}
