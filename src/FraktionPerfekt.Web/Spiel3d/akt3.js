// Joe & Chani 3D – Akt 3: Emsdeich am Kanuverleih (mit der Radbrücke), Bootsflucht auf der Ems, Baustelle am Hallenbad.
// Gelände und Brücke aus den 3D-Prototypen; Kanuverleih, Steg, Skulpturen, Baustelle frei erfunden.
import * as THREE from './three_min.js';

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
    const w = baue(renderer, await laden('welt_bad.json'));
    const { scene, H, W3 } = w;
    const E0 = 4, E1 = 34, N0 = -58, N1 = -32;               // Bauzaun
    const GRUBE = [12, -46, 19, -40], BECKEN = [23, -55, 31, -46];
    const P = ([e, n]) => [e, -n];
    const at = (o, e, n, dy = 0) => { o.position.copy(W3(e, n, dy)); scene.add(o); return o; };
    const eigene = [];
    const boden0 = (e, n) => H(e, n);
    // Gelände in Grube und Becken absenken
    const senken = [[...GRUBE, 2.3], [...BECKEN, 2.0]];
    {
        const p = w.ground.geometry.attributes.position;
        for (let i = 0; i < p.count; i++) {
            const e = p.getX(i), n = -p.getZ(i);
            for (const [a, b, c, d, tief] of senken) { if (e > a - 1.2 && e < c + 1.2 && n > b - 1.2 && n < d + 1.2) { p.setY(i, p.getY(i) - tief); } }
        }
        p.needsUpdate = true; w.ground.geometry.computeVertexNormals();
    }
    const kasten = (rect, tief, wandMat, bodenMat, randMat) => {
        const [a, b, c, d] = rect, g = new THREE.Group();
        const y0 = Math.min(H(a, b), H(c, d), H(a, d), H(c, b));
        const bw = c - a, bt = d - b;
        mesh(new THREE.BoxGeometry(bw, .1, bt), bodenMat, g, 0, -tief, 0);
        for (const [x, z, sw, st] of [[0, -bt / 2, bw, .2], [0, bt / 2, bw, .2], [-bw / 2, 0, .2, bt], [bw / 2, 0, .2, bt]]) { mesh(new THREE.BoxGeometry(sw, tief + .1, st), wandMat, g, x, -tief / 2 + .02, -z); }
        // Kante auf Geländehöhe (Rand aus Beton bzw. Holzbohlen)
        for (const [x, z, sw, st] of [[0, -bt / 2 - .25, bw + .9, .5], [0, bt / 2 + .25, bw + .9, .5], [-bw / 2 - .25, 0, .5, bt], [bw / 2 + .25, 0, .5, bt]]) { mesh(new THREE.BoxGeometry(sw, .08, st), randMat, g, x, .02, -z); }
        g.position.set((a + c) / 2, y0, -(b + d) / 2); scene.add(g); return { g, y0 };
    };
    const erde = std({ color: 0x6a5038, roughness: 1 }), schlamm = std({ color: 0x4a3a2a, roughness: .5 });
    const fliese = std({ map: w.canvasTex(128, 128, (g, cw, ch) => { g.fillStyle = '#9fd3e6'; g.fillRect(0, 0, cw, ch); g.strokeStyle = '#e8f4f8'; g.lineWidth = 3; for (let i = 0; i <= 4; i++) { g.beginPath(); g.moveTo(i * 32, 0); g.lineTo(i * 32, ch); g.moveTo(0, i * 32); g.lineTo(cw, i * 32); g.stroke(); } }), roughness: .3 });
    fliese.map.repeat.set(4, 2);
    const grube = kasten(GRUBE, 2.3, erde, schlamm, std({ color: 0x7a5a3a, roughness: .95 })); grube.g.name = 'grube';
    const wasserG = mesh(new THREE.BoxGeometry(GRUBE[2] - GRUBE[0] - .3, .05, GRUBE[3] - GRUBE[1] - .3), std({ color: 0x5a6a50, roughness: .1, metalness: .3, transparent: true, opacity: .85 }), grube.g, 0, -1.1, 0);
    const siegel = mesh(new THREE.CylinderGeometry(.2, .2, .05, 24), std({ color: 0xd4af37, metalness: .9, roughness: .2, emissive: 0x3a2a00 }), grube.g, .6, -2.2, .4);
    const fliesenrest = mesh(new THREE.BoxGeometry(.4, .04, .4), fliese, grube.g, .2, -2.22, .4); fliesenrest.rotation.y = .4;
    greifbar(grube.g, GRUBE[2] - GRUBE[0] - .4, 1.0, GRUBE[3] - GRUBE[1] - 1.0, -1.7);   // unterhalb der Leitung, damit beide klickbar bleiben
    // leckende Leitung an der Nordkante der Grube
    const leitung = new THREE.Group(); leitung.name = 'leitung';
    mesh(new THREE.CylinderGeometry(.14, .14, 8.5, 14), std({ color: 0x5a5a5a, metalness: .6, roughness: .5 }), leitung).rotation.z = Math.PI / 2;
    const strahl = new THREE.Group(); leitung.add(strahl);
    const wasserM = std({ color: 0xbfe0f0, transparent: true, opacity: .7, roughness: .1 });
    for (let i = 0; i < 7; i++) { mesh(new THREE.SphereGeometry(.07, 8, 6), wasserM, strahl, 0, 0, 0).castShadow = false; }
    leitung.position.set((GRUBE[0] + GRUBE[2]) / 2, grube.y0 - .7, -(GRUBE[3] - .15)); scene.add(leitung);
    greifbar(leitung, 8.5, .8, .8, 0);
    const flick = mesh(new THREE.CylinderGeometry(.18, .18, .3, 14), std({ color: 0x1a1a1a, roughness: .3 }), leitung, -.4, 0, 0); flick.rotation.z = Math.PI / 2;
    // leeres Becken (in Sanierung)
    const becken = kasten(BECKEN, 2.0, fliese, fliese, std({ color: 0xd8d4cc, roughness: .8 })); becken.g.name = 'becken';
    for (const x of [-2, 2]) { const l = new THREE.Group(); for (const dz of [-.25, .25]) { mesh(new THREE.TorusGeometry(.25, .03, 6, 12, Math.PI), std({ color: 0xc0c4c8, metalness: .8 }), l, 0, .3, dz); mesh(new THREE.CylinderGeometry(.03, .03, 2.2, 6), std({ color: 0xc0c4c8, metalness: .8 }), l, .25, -.8, dz); } l.position.set(x, 0, (BECKEN[3] - BECKEN[1]) / 2 - .2); l.rotation.y = Math.PI / 2; becken.g.add(l); }
    greifbar(becken.g, BECKEN[2] - BECKEN[0], 1.2, BECKEN[3] - BECKEN[1], -.5);
    // Bagger
    const gelb = std({ color: 0xf2b01e, roughness: .5, metalness: .2 }), schwarz = std({ color: 0x222222, roughness: .7 });
    const bagger = new THREE.Group(); bagger.name = 'bagger';
    for (const z of [-1.1, 1.1]) { mesh(new THREE.BoxGeometry(3.6, .8, .7), schwarz, bagger, 0, .4, z); }
    const oben = new THREE.Group(); oben.position.y = .9; bagger.add(oben);
    mesh(new THREE.BoxGeometry(3.0, 1.0, 2.4), gelb, oben, -.3, .5, 0);
    mesh(new THREE.BoxGeometry(1.2, 1.5, 1.1), std({ color: 0x2a3a4a, roughness: .1, metalness: .4 }), oben, .6, 1.75, .55);
    const arm1 = mesh(new THREE.BoxGeometry(3.4, .35, .35), gelb, oben, 2.0, 2.1, -.4); arm1.rotation.z = .45;
    const arm2 = mesh(new THREE.BoxGeometry(2.6, .3, .3), gelb, oben, 4.1, 2.0, -.4); arm2.rotation.z = -1.0;
    mesh(new THREE.BoxGeometry(.8, .7, .9), schwarz, oben, 4.9, .7, -.4);
    bagger.rotation.y = Math.PI * .8; at(bagger, 25, -38); eigene.push([25, -38, 2.6], [22.5, -39.5, 1.2]);
    // Bauwagen
    const wagen = new THREE.Group(); wagen.name = 'bauwagen';
    mesh(new THREE.BoxGeometry(5, 2.3, 2.3), std({ color: 0x3a7a4a, roughness: .7 }), wagen, 0, 1.6, 0);
    mesh(new THREE.CylinderGeometry(1.25, 1.25, 5.04, 20, 1, false, 0, Math.PI), std({ color: 0x5a5a5a }), wagen, 0, 2.75, 0).rotation.set(0, 0, Math.PI / 2);
    for (const x of [-1.6, 1.6]) { for (const z of [-1.0, 1.0]) { mesh(new THREE.CylinderGeometry(.38, .38, .2, 16), schwarz, wagen, x, .38, z).rotation.x = Math.PI / 2; } }
    mesh(new THREE.BoxGeometry(.9, 1.8, .05), std({ color: 0x2a4a3a }), wagen, 1.4, 1.4, 1.17);
    for (const x of [-1.2, 0]) { mesh(new THREE.BoxGeometry(.8, .6, .04), std({ color: 0xbfd6e2, roughness: .1, metalness: .3 }), wagen, x, 1.9, 1.17); }
    wagen.rotation.y = Math.PI; at(wagen, 9.5, -35.2); eigene.push([9.5, -35.2, 2.9], [7.5, -35.2, 1.5], [11.5, -35.2, 1.5]);
    // Bauzaun mit Tor im Westen
    const zaunTex = w.canvasTex(128, 128, (g, cw, ch) => { g.clearRect(0, 0, cw, ch); g.strokeStyle = '#c8ccd0'; g.lineWidth = 3; for (let i = 0; i <= 8; i++) { g.beginPath(); g.moveTo(i * 16, 0); g.lineTo(i * 16, ch); g.moveTo(0, i * 16); g.lineTo(cw, i * 16); g.stroke(); } });
    const gitter = new THREE.MeshStandardMaterial({ map: zaunTex, transparent: true, alphaTest: .3, side: THREE.DoubleSide, metalness: .6, roughness: .4 });
    const fuss = std({ color: 0xc8483a, roughness: .9 }), rohr = std({ color: 0xb0b4b8, metalness: .7, roughness: .4 });
    const zaun = (e0, n0, e1, n1) => {
        const l = Math.hypot(e1 - e0, n1 - n0), k = Math.max(1, Math.round(l / 3.5));
        for (let i = 0; i < k; i++) {
            const a = i / k, b = (i + 1) / k, ea = e0 + (e1 - e0) * a, na = n0 + (n1 - n0) * a, eb = e0 + (e1 - e0) * b, nb = n0 + (n1 - n0) * b;
            const em = (ea + eb) / 2, nm = (na + nb) / 2, len = Math.hypot(eb - ea, nb - na), rot = Math.atan2(-(nb - na), eb - ea);
            const p = new THREE.Group();
            const f = mesh(new THREE.PlaneGeometry(len, 1.9), gitter, p, 0, 1.15, 0); f.castShadow = false; f.userData.durchlaessig = true;
            for (const x of [-len / 2, len / 2]) { mesh(new THREE.CylinderGeometry(.03, .03, 2.0, 6), rohr, p, x, 1.1, 0); mesh(new THREE.BoxGeometry(.6, .15, .25), fuss, p, x, .08, 0); }
            mesh(new THREE.BoxGeometry(len, .04, .04), rohr, p, 0, 2.08, 0);
            p.position.copy(W3(em, nm)); p.rotation.y = rot; scene.add(p);
        }
    };
    zaun(E0, N1, E1, N1); zaun(E1, N1, E1, N0); zaun(E1, N0, E0, N0); zaun(E0, N0, E0, -45); zaun(E0, -40, E0, N1);
    const tor = new THREE.Group(); tor.name = 'tor';
    const torFluegel = mesh(new THREE.PlaneGeometry(2.6, 1.9), gitter, tor, 0, 1.15, 1.3); torFluegel.rotation.y = Math.PI / 2 + .9; torFluegel.userData.durchlaessig = true; torFluegel.position.set(-.9, 1.15, 1.6);
    const torSchild = mesh(new THREE.PlaneGeometry(1.2, .5), std({ map: texte(w.canvasTex, 256, 104, '#ffffff', '#c0392b', ['Baustelle', 'Betreten verboten'], 'bold 28px Arial'), side: THREE.DoubleSide }), tor, 0, 1.7, -2.4);
    torSchild.rotation.y = Math.PI / 2;
    at(tor, E0, -42.5); greifbar(tor, 1.2, 2.4, 5, 1.2);
    // Stapel abgeschraubter Wegweiser vor dem Tor
    const stapel = new THREE.Group(); stapel.name = 'stapel';
    ['Münster', 'Nordwalde', 'Emsdetten', 'Saerbeck', 'Ladbergen', 'Reckenbeck'].forEach((t, i) => {
        const tex = texte(w.canvasTex, 256, 56, '#f2d230', '#1a1a1a', [t], 'bold 32px Arial');
        const s = mesh(new THREE.BoxGeometry(1.3, .03, .3), [std({ color: 0xaaaaaa }), std({ color: 0xaaaaaa }), std({ map: tex }), std({ color: 0xaaaaaa }), std({ color: 0xaaaaaa }), std({ color: 0xaaaaaa })], stapel, 0, .03 + i * .045, 0);
        s.rotation.y = (i % 3 - 1) * .25;
    });
    at(stapel, E0 - 1.0, -42.5);
    // Schild „Hallenbad wird saniert“
    const info = new THREE.Group(); info.name = 'infotafel';
    mesh(new THREE.PlaneGeometry(3.2, 1.6), std({ map: texte(w.canvasTex, 512, 256, '#1f5fa8', '#ffffff', ['Hier wird saniert:', 'Ihr Hallenbad', 'Wiedereröffnung: bald'], 'bold 44px Arial') }), info, 0, 2.2, 0);
    for (const x of [-1.4, 1.4]) { mesh(new THREE.CylinderGeometry(.05, .05, 3, 8), rohr, info, x, 1.5, -.05); }
    info.rotation.y = -Math.PI / 2 + .2; at(info, 33, -36); eigene.push([33, -36, .6]);
    const zurBruecke = new THREE.Group(); zurBruecke.name = 'zurbruecke';
    mesh(new THREE.CylinderGeometry(.05, .05, 2.4, 8), rohr, zurBruecke, 0, 1.2, 0);
    mesh(new THREE.BoxGeometry(1.3, .3, .03), [rohr, rohr, rohr, rohr, std({ map: texte(w.canvasTex, 256, 60, '#f4f2ea', '#1a3a6a', ['Radbrücke / Ems →'], 'bold 28px Arial') }), std({ map: texte(w.canvasTex, 256, 60, '#f4f2ea', '#1a3a6a', ['Radbrücke / Ems →'], 'bold 28px Arial') })], zurBruecke, .5, 2.1, 0);
    greifbar(zurBruecke, 1.4, 2.4, .6); zurBruecke.rotation.y = -Math.PI / 2; at(zurBruecke, E1 - 1.2, -34); eigene.push([E1 - 1.2, -34, .4]);
    const frei = (x, z) => {
        const e = x, n = -z;
        if (e < E0 + .5 || e > E1 - .5 || n < N0 + .5 || n > N1 - .5) { return false; }
        return !imKreis(eigene, e, n) && !(e > GRUBE[0] - .9 && e < GRUBE[2] + .9 && n > GRUBE[1] - .9 && n < GRUBE[3] + .9) && !(e > BECKEN[0] - .9 && e < BECKEN[2] + .9 && n > BECKEN[1] - .9 && n < BECKEN[3] + .9);
    };
    const boden = []; scene.traverse((o) => { if (o.name === 'ground') { boden.push(o); } });
    const mitte = W3(19, -45);
    return {
        scene, frei, boden, hoehe: (x, z) => boden0(x, -z),
        kamera: (p) => { const t = p.clone().lerp(mitte, .3); t.y = p.y + 1.0; return { p: new THREE.Vector3(p.x * .7 + mitte.x * .3 + 4, p.y + 6.5, p.z + 12), t }; },
        spawn: { karte: [8.5, 42.6, Math.PI / 2], von_bruecke: [E1 - 2.6, 34.6, -Math.PI / 2] },
        hotspots: [
            { obj: leitung, stand: [16.5, 38.6], eintraege: [{ id: 'leitung' }] },
            { obj: grube.g, stand: [11, 43], eintraege: [{ id: 'grube' }] },
            { obj: tor, stand: [5.4, 42.5], eintraege: [{ id: 'zurkarte' }] },
            { obj: zurBruecke, stand: [E1 - 2.2, 34.6], eintraege: [{ tor: 'radbruecke', spawn: 'von_baustelle', name: 'Weg zur Radbrücke', ansehen: 'Ein Schild: „Radbrücke / Ems“. Dahinter die gelben Laternen.' }] },
            { obj: becken.g, stand: [27, 44.6], eintraege: [{ deko: 'Leeres Becken', text: 'Das Außenbecken, leer und frisch verfugt. Joe: „Ordentliche Arbeit. Fast so gut wie meine.“' }] },
            { obj: bagger, stand: [21.5, 36.5], eintraege: [{ deko: 'Bagger', text: 'Ein Bagger. Der Schlüssel steckt nicht – Frau Kemper hat ihn um den Hals.' }] },
            { obj: wagen, stand: [13.4, 37.2], eintraege: [{ deko: 'Bauwagen', text: 'Der Bauwagen. Drinnen: Kaffeemaschine, Bauplan, Gummistiefel.' }] },
            { obj: info, stand: [31.6, 37.2], eintraege: [{ deko: 'Bauschild', text: '„Hier wird saniert: Ihr Hallenbad. Wiedereröffnung: bald.“ Das Datum ist überklebt.' }] }
        ],
        npcs: [
            { hotspot: 'bauleiter', figur: 'kemper', pos: [10.2, 47.2], rot: .6, pose: 'stehen', stand: [10.6, 45.6] },
            // Aus der Ems durch das Bauzaun-Tor herein; später rückwärts in den Bauwagen
            { hotspot: 'gegner', figur: 'pinsel', pos: [5.6, 40.6], rot: -Math.PI / 2, pose: 'benutzen', stand: [7.4, 41.4], von: [1.4, 41.6], nach: [[9.0, 37.4], [9.3, 35.6]], rueckwaerts: true },
            { hotspot: 'gegner', figur: 'ledeiw', pos: [5.6, 44.4], rot: -Math.PI / 2, pose: 'stehen', stand: [7.4, 41.4], von: [1.2, 43.6], nach: [[10.0, 37.6], [9.8, 35.6]], rueckwaerts: true }
        ],
        dinge: [
            { obj: siegel, sichtbar: (b) => !!b.flags.grubeTrocken && !b.inventar.includes('siegel_judikative'), art: 'nehmen' },
            // Die abgeschraubten Wegweiser stapeln Pinsel und Ledeiw erst, wenn sie da sind
            { obj: stapel, sichtbar: (b) => !!b.flags.gegnerBaustelle && !b.flags.gegnerBaustelleWeg, art: 'schwinden' }
        ],
        aktualisieren(z) {
            const f = z.flags;
            strahl.visible = !f.leckDicht && !f.grubeTrocken;   // das Leck ist dicht …
            wasserG.visible = !f.grubeTrocken;                    // … dann pumpt die Grube leer
            flick.visible = !!f.grubeTrocken;
        },
        helme: (z) => !!z.flags.helme,
        tick(dt, t) {
            strahl.children.forEach((s, i) => { const k = ((t * 1.6 + i / 7) % 1); s.position.set(.6, -k * 1.2, k * .9); s.scale.setScalar(1 - k * .4); });
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
