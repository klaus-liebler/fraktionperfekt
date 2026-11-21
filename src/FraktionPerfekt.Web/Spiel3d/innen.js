// Joe & Chani 3D – Innenräume (frei erfunden): Büro der Zukunftsschmiede, Rathausflur, Rathausarchiv.
// Koordinaten: x nach rechts, z zur Kamera, y nach oben; Boden bei y = 0. Die Vorderwand fehlt (Puppenstuben-Schnitt).
import * as THREE from './three_min.js';
import { RoomEnvironment } from './roomenv.js';

let seed = 99;
const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;

function canvasTex(w, h, draw, wrap = true) {
    const c = document.createElement('canvas'); c.width = w; c.height = h;
    draw(c.getContext('2d'), w, h);
    const t = new THREE.CanvasTexture(c);
    if (wrap) { t.wrapS = t.wrapT = THREE.RepeatWrapping; }
    t.anisotropy = 8; t.colorSpace = THREE.SRGBColorSpace;
    return t;
}
const std = (o) => new THREE.MeshStandardMaterial(Object.assign({ roughness: .8 }, o));

/** Quader mit Texturen in echtem Maßstab (tile Meter je Kachel). */
function box(w, h, d, mat, tile = 1) {
    const g = new THREE.BoxGeometry(w, h, d);
    const uv = g.attributes.uv, dims = [[d, h], [d, h], [w, d], [w, d], [w, h], [w, h]];
    for (let f = 0; f < 6; f++) { for (let v = 0; v < 4; v++) { const i = f * 4 + v; uv.setXY(i, uv.getX(i) * dims[f][0] / tile, uv.getY(i) * dims[f][1] / tile); } }
    const m = new THREE.Mesh(g, mat); m.castShadow = m.receiveShadow = true; return m;
}
function plane(w, h, mat, tile) {
    const g = new THREE.PlaneGeometry(w, h);
    if (tile) { const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) { uv.setXY(i, uv.getX(i) * w / tile, uv.getY(i) * h / tile); } }
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true; return m;
}
const at = (o, x, y, z, parent) => { o.position.set(x, y, z); parent.add(o); return o; };

/** Grundgerüst eines Raums: Boden, Rück- und Seitenwände, Decke, Licht. */
function raum(renderer, o) {
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(o.hintergrund || 0x0d0a08);
    const pmrem = new THREE.PMREMGenerator(renderer);
    scene.environment = pmrem.fromScene(new RoomEnvironment(), .04).texture; scene.environmentIntensity = .35;
    const { x0, x1, z0, z1, hoehe } = o;
    const B = x1 - x0, T = z1 - z0;
    const boden = plane(B, T + 5.5, std({ map: o.boden, roughness: o.bodenRau || .7 }), o.bodenKachel || 1);
    boden.rotation.x = -Math.PI / 2; boden.position.set((x0 + x1) / 2, 0, (z0 + z1) / 2 + 2.75); boden.name = 'boden'; scene.add(boden);
    const wand = std({ map: o.wand, roughness: .92 });
    const rueck = plane(B, hoehe, wand, 1.5); rueck.position.set((x0 + x1) / 2, hoehe / 2, z0); scene.add(rueck);
    const links = plane(T + 5.5, hoehe, wand, 1.5); links.rotation.y = Math.PI / 2; links.position.set(x0, hoehe / 2, (z0 + z1) / 2 + 2.75); scene.add(links);
    const rechts = plane(T + 5.5, hoehe, wand, 1.5); rechts.rotation.y = -Math.PI / 2; rechts.position.set(x1, hoehe / 2, (z0 + z1) / 2 + 2.75); scene.add(rechts);
    const decke = plane(B, T + 5.5, std({ color: o.decke || 0xe8e4dc })); decke.rotation.x = Math.PI / 2; decke.position.set((x0 + x1) / 2, hoehe, (z0 + z1) / 2 + 2.75); scene.add(decke);
    // Fußleisten
    const leiste = std({ color: o.leiste || 0x6a5a4a });
    at(box(B, .08, .02, leiste), (x0 + x1) / 2, .04, z0 + .01, scene);
    for (const x of [x0 + .01, x1 - .01]) { at(box(.02, .08, T + 5.5, leiste), x, .04, (z0 + z1) / 2 + 2.75, scene); }
    scene.add(new THREE.HemisphereLight(o.himmel || 0xfff4e6, 0x5a4a3a, o.hell || 1.1));
    const key = new THREE.DirectionalLight(0xfff0dc, o.sonne || 1.2);
    key.position.set(x0 + B * .3, hoehe * 3, z1 + 6); key.target.position.set((x0 + x1) / 2, 0, (z0 + z1) / 2); scene.add(key, key.target);
    key.castShadow = true; key.shadow.mapSize.set(2048, 2048);
    Object.assign(key.shadow.camera, { left: -10, right: 10, top: 10, bottom: -10, near: .5, far: 40 }); key.shadow.bias = -.0005; key.shadow.normalBias = .02;
    return { scene, boden, wand };
}

/** Hindernisse als Rechtecke [x0, z0, x1, z1]; begehbar ist der Raum abzüglich Rand und Hindernisse. */
function freiImRaum(grenzen, hindernisse) {
    return (x, z) => {
        if (x < grenzen[0] || x > grenzen[2] || z < grenzen[1] || z > grenzen[3]) { return false; }
        for (const [a, b, c, d] of hindernisse) { if (x > a - .25 && x < c + .25 && z > b - .25 && z < d + .25) { return false; } }
        return true;
    };
}

// ---------- Texturen ----------
const dielen = () => canvasTex(512, 512, (g, w, h) => {
    for (let i = 0; i < 8; i++) {
        const y = i * 64; g.fillStyle = `rgb(${150 + rnd() * 30},${104 + rnd() * 20},${66 + rnd() * 14})`; g.fillRect(0, y, w, 64);
        g.fillStyle = 'rgba(60,35,20,.5)'; g.fillRect(0, y, w, 2); const s = Math.floor(rnd() * w); g.fillRect(s, y, 2, 64);
        for (let k = 0; k < 30; k++) { g.fillStyle = `rgba(80,50,30,${rnd() * .12})`; g.fillRect(rnd() * w, y + rnd() * 60, 40 + rnd() * 80, 1); }
    }
});
const putz = (farbe) => canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = farbe; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 1800; i++) { g.fillStyle = `rgba(90,80,60,${rnd() * .04})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
});
const linoleum = () => canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#8f8a7c'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 3000; i++) { g.fillStyle = `rgba(${rnd() < .5 ? '255,255,255' : '0,0,0'},${rnd() * .07})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
    g.fillStyle = 'rgba(0,0,0,.18)'; g.fillRect(0, 0, w, 2); g.fillRect(0, 0, 2, h);
});
const beton = () => canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#9c978c'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 2500; i++) { g.fillStyle = `rgba(0,0,0,${rnd() * .08})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
    g.fillStyle = 'rgba(0,0,0,.25)'; g.fillRect(0, 0, w, 3); g.fillRect(0, 0, 3, h);
});
function schrift(w, h, hg, farbe, text, font, zeilen) {
    return canvasTex(w, h, (g) => {
        g.fillStyle = hg; g.fillRect(0, 0, w, h); g.fillStyle = farbe; g.font = font; g.textAlign = 'center'; g.textBaseline = 'middle';
        const z = zeilen || [text]; z.forEach((t, i) => g.fillText(t, w / 2, h / 2 + (i - (z.length - 1) / 2) * parseInt(font.match(/(\d+)px/)[1], 10) * 1.2));
    }, false);
}

/** Bücher- bzw. Ordnerregal: Gestell plus bunte Rücken. */
function regal(breite, hoehe, tiefe, boeden, holz, ordner) {
    const g = new THREE.Group();
    for (const x of [-breite / 2, breite / 2]) { at(box(.04, hoehe, tiefe, holz), x, hoehe / 2, 0, g); }
    at(box(breite, hoehe, .02, holz), 0, hoehe / 2, -tiefe / 2, g);
    const fach = hoehe / boeden;
    for (let i = 0; i <= boeden; i++) { at(box(breite, .03, tiefe, holz), 0, i * fach, 0, g); }
    const farben = ordner ? [0x2f4f8a, 0x8a2f2f, 0x2f6a4a, 0x6a6a6a, 0xb08a3a] : [0x7a2e22, 0x2e4a7a, 0x3a6a3a, 0xc9a24a, 0x5a3a6a, 0xd8d0c0, 0x2a2a2a];
    for (let i = 0; i < boeden; i++) {
        let x = -breite / 2 + .05;
        while (x < breite / 2 - .1) {
            const bw = ordner ? .075 : .025 + rnd() * .04, bh = ordner ? fach * .82 : fach * (.55 + rnd() * .3);
            if (x + bw > breite / 2 - .05) { break; }
            const b = at(box(bw, bh, tiefe * .8, std({ color: farben[Math.floor(rnd() * farben.length)], roughness: .7 })), x + bw / 2, i * fach + .015 + bh / 2, .02, g);
            b.castShadow = false;
            if (!ordner && rnd() < .08) { b.rotation.z = .25; b.position.x += .03; }
            x += bw + (ordner ? .006 : .002);
        }
    }
    return g;
}
function lampe(scene, x, y, z, staerke = 6, farbe = 0xffd9a0) {
    const L = new THREE.PointLight(farbe, staerke, 9, 1.6); L.position.set(x, y, z); scene.add(L);
    return L;
}

// ======================================================================
// Büro der Zukunftsschmiede (Erdgeschoss Marktstraße 19)
// ======================================================================
export function buero(renderer) {
    seed = 1949;
    const x0 = -4.2, x1 = 4.2, z0 = -3, z1 = 3;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 3.2, boden: dielen(), bodenKachel: 2, wand: putz('#efebe2'), hell: .9, sonne: .9 });
    const holz = std({ color: 0x8a5a36, roughness: .6 }), weiss = std({ color: 0xf2f0ea, roughness: .5 }), dunkel = std({ color: 0x2a2c30, roughness: .6 }), metall = std({ color: 0x9a9d9e, roughness: .35, metalness: .7 });
    // blaues Band der Zukunftsschmiede an der Rückwand
    at(plane(8.4, .28, std({ color: 0x1597bb })), 0, 2.55, z0 + .01, scene);
    // Schaufenster und Glastür in der linken Wand (zur Marktstraße)
    const strasse = canvasTex(1024, 512, (g, w, h) => {
        const gr = g.createLinearGradient(0, 0, 0, h); gr.addColorStop(0, '#f0b27a'); gr.addColorStop(.5, '#c9a6a0'); gr.addColorStop(1, '#8a6a5a'); g.fillStyle = gr; g.fillRect(0, 0, w, h);
        // Häuser gegenüber (Abendlicht), Eiscafé mit Markise
        for (let i = 0; i < 6; i++) { const x = i * 180 - 20, hh = 200 + rnd() * 120; g.fillStyle = ['#b8836a', '#d9c3a8', '#a65a46', '#e2d6c2'][i % 4]; g.fillRect(x, h - hh - 80, 170, hh);
            g.fillStyle = '#ffd890'; for (let k = 0; k < 3; k++) { for (let j = 0; j < 3; j++) { if (rnd() < .55) { g.fillRect(x + 20 + k * 50, h - hh - 50 + j * 60, 26, 34); } } } }
        for (let x = 380; x < 620; x += 30) { g.fillStyle = (x / 30) % 2 ? '#f4f0e6' : '#b8322a'; g.fillRect(x, h - 190, 30, 40); }
        g.fillStyle = '#8a5a4a'; g.fillRect(0, h - 80, w, 80);
        g.fillStyle = '#3a2a22'; g.fillRect(150, h - 300, 10, 230); g.fillStyle = '#fff2c0'; g.beginPath(); g.arc(155, h - 305, 16, 0, 7); g.fill();
    }, false);
    const aussicht = plane(5.6, 2.9, new THREE.MeshBasicMaterial({ map: strasse })); aussicht.rotation.y = Math.PI / 2; aussicht.position.set(x0 - .6, 1.6, -.4); scene.add(aussicht);
    // Wandöffnung: Brüstung, Sturz, Pfosten, Glas
    const glas = std({ color: 0xbfd6e2, roughness: .05, metalness: .1, transparent: true, opacity: .18 });
    const wandStk = std({ map: putz('#efebe2') });
    // Die Wand bekommt ein echtes Fenster: Wandstück entfernen, indem wir die Seitenwand neu aufbauen
    scene.children.filter((c) => c.isMesh && Math.abs(c.position.x - x0) < .001 && c.geometry.type === 'PlaneGeometry').forEach((c) => scene.remove(c));
    const teile = [[z0, -2.6, 0, 3.2], [1.0, 1.25, 0, 3.2], [2.35, z1 + 5.5, 0, 3.2], [-2.6, 1.0, 0, .5], [-2.6, 1.0, 2.8, 3.2], [1.25, 2.35, 2.45, 3.2]];
    for (const [a, b, u, o] of teile) { const p = plane(b - a, o - u, wandStk, 1.5); p.rotation.y = Math.PI / 2; p.position.set(x0, (u + o) / 2, (a + b) / 2); scene.add(p); }
    const fensterGlas = plane(3.6, 2.3, glas); fensterGlas.rotation.y = Math.PI / 2; fensterGlas.position.set(x0 + .02, 1.65, -.8); scene.add(fensterGlas);
    fensterGlas.name = 'schaufenster';
    for (const z of [-2.6, -.8, 1.0]) { at(box(.08, 2.3, .08, weiss), x0 + .04, 1.65, z, scene); }
    at(box(.1, .06, 3.6, weiss), x0 + .05, .5, -.8, scene); at(box(.1, .06, 3.6, weiss), x0 + .05, 2.8, -.8, scene);
    // Glastür
    const tuer = new THREE.Group(); tuer.name = 'ladentuer';
    at(box(.06, 2.45, 1.1, weiss), 0, 1.225, 0, tuer);
    const tg = plane(.85, 2.0, std({ color: 0x8fb4c8, roughness: .05, metalness: .3, transparent: true, opacity: .55 })); tg.rotation.y = Math.PI / 2; at(tg, .04, 1.25, 0, tuer);
    at(box(.1, .45, .04, metall), .08, 1.05, .38, tuer);
    const oeffnungsz = schrift(256, 128, '#ffffff', '#1597bb', '', 'bold 30px Arial', ['Sprechstunde', 'Do 17–19 Uhr']);
    const sch = plane(.32, .16, std({ map: oeffnungsz })); sch.rotation.y = Math.PI / 2; at(sch, .045, 1.55, 0, tuer);
    tuer.position.set(x0 + .03, 0, 1.8); scene.add(tuer);
    // Tisch mit Umschlag, leerem Keksteller, Stühlen
    const tisch = new THREE.Group(); tisch.name = 'tisch';
    at(box(2.2, .05, 1.0, weiss), 0, .75, 0, tisch);
    for (const [x, z] of [[-1, -.4], [1, -.4], [-1, .4], [1, .4]]) { at(box(.05, .73, .05, metall), x, .365, z, tisch); }
    tisch.position.set(0, 0, .2); scene.add(tisch);
    const stuhl = (x, z, rot) => {
        const s = new THREE.Group();
        at(box(.44, .04, .42, holz), 0, .46, 0, s); at(box(.44, .42, .03, holz), 0, .7, -.2, s);
        for (const lx of [-.19, .19]) { for (const lz of [-.18, .18]) { at(box(.03, .46, .03, metall), lx, .23, lz, s); } }
        s.position.set(x, 0, z); s.rotation.y = rot; scene.add(s);
    };
    stuhl(-.6, -.55, 0); stuhl(.6, -.55, 0); stuhl(-.6, .95, Math.PI); stuhl(.6, .95, Math.PI + .3); stuhl(-1.45, .2, Math.PI / 2);
    const umschlag = new THREE.Group(); umschlag.name = 'umschlag';
    const papier = canvasTex(256, 160, (g, w, h) => { g.fillStyle = '#efe4c8'; g.fillRect(0, 0, w, h); g.strokeStyle = '#c9b48a'; g.lineWidth = 3; g.beginPath(); g.moveTo(0, 0); g.lineTo(w / 2, h * .55); g.lineTo(w, 0); g.stroke();
        g.fillStyle = '#b0261d'; g.beginPath(); g.arc(w / 2, h * .55, 16, 0, 7); g.fill(); g.fillStyle = '#4a3a2a'; g.font = 'italic 18px Georgia'; g.fillText('An die Fraktion,', 20, h - 40); g.fillText('die noch Briefe öffnet', 20, h - 18); }, false);
    const u = at(box(.32, .012, .2, std({ map: papier, roughness: .9 })), 0, .782, 0, umschlag); u.rotation.y = .25;
    umschlag.position.set(-.4, 0, .35); scene.add(umschlag);
    const teller = at(new THREE.Mesh(new THREE.CylinderGeometry(.14, .11, .02, 24), weiss), .55, .785, .25, scene); teller.name = 'teller';
    for (let i = 0; i < 4; i++) { at(box(.012, .004, .01, std({ color: 0xc8a060 })), .5 + rnd() * .1, .797, .2 + rnd() * .1, scene); }
    at(new THREE.Mesh(new THREE.CylinderGeometry(.04, .035, .1, 16), std({ color: 0x1597bb })), .2, .82, -.05, scene);
    // Bücherregal an der Rückwand
    const reg = regal(1.8, 2.2, .4, 5, holz, false); reg.position.set(-2.5, 0, z0 + .22); reg.name = 'regal'; scene.add(reg);
    // Tafel
    const tafelTex = canvasTex(512, 320, (g, w, h) => { g.fillStyle = '#1f2a24'; g.fillRect(0, 0, w, h); g.strokeStyle = '#8a6a40'; g.lineWidth = 14; g.strokeRect(0, 0, w, h);
        g.fillStyle = '#ece8dc'; g.font = '34px "Comic Sans MS", cursive'; g.fillText('Fraktionssitzung', 40, 60); g.font = '26px "Comic Sans MS", cursive';
        ['TOP 1: Kekse ✓', 'TOP 2: Haushalt', 'TOP 3: Verschiedenes', '    (Gral?)'].forEach((t, i) => g.fillText(t, 50, 115 + i * 44)); }, false);
    const tafel = plane(1.7, 1.06, std({ map: tafelTex, roughness: .9 })); tafel.position.set(.7, 1.6, z0 + .02); tafel.name = 'tafel'; scene.add(tafel);
    // Plakat an der rechten Wand
    const plakat = plane(1.0, 1.4, std({ map: schrift(256, 360, '#1597bb', '#ffffff', '', 'bold 40px Arial', ['Zukunfts-', 'schmiede', '', 'Ideen', 'für Greven']) }));
    plakat.rotation.y = -Math.PI / 2; plakat.position.set(x1 - .01, 1.7, -.2); scene.add(plakat);
    // Sideboard mit Faxgerät
    const sb = at(box(.5, .85, 1.5, holz), x1 - .27, .425, -1.6, scene); sb.name = 'sideboard';
    const fax = new THREE.Group(); fax.name = 'fax';
    at(box(.42, .14, .36, std({ color: 0xd8d0b8, roughness: .6 })), 0, .07, 0, fax);
    const schacht = at(box(.3, .02, .16, std({ color: 0xe6e0d0 })), 0, .17, -.12, fax); schacht.rotation.x = -.6;
    at(box(.24, .004, .2, std({ color: 0xffffff })), 0, .2, -.16, fax).rotation.x = -.6;
    at(box(.12, .04, .1, dunkel), .1, .15, .1, fax);
    const hoerer = at(new THREE.Mesh(new THREE.CapsuleGeometry(.025, .16, 4, 8), dunkel), -.15, .17, .05, fax); hoerer.rotation.z = Math.PI / 2;
    fax.position.set(x1 - .3, .85, -1.75); fax.rotation.y = -Math.PI / 2; scene.add(fax);
    const zettel = plane(.12, .09, std({ color: 0xfff27a })); zettel.rotation.x = -Math.PI / 2; zettel.position.set(x1 - .3, .852, -1.25); scene.add(zettel);
    // Garderobe: Ständer mit Joes Hut, Lederjacke, Taschenlampe
    const gard = new THREE.Group(); gard.name = 'garderobe';
    at(new THREE.Mesh(new THREE.CylinderGeometry(.025, .03, 1.85, 10), dunkel), 0, .925, 0, gard).castShadow = true;
    at(new THREE.Mesh(new THREE.CylinderGeometry(.25, .28, .03, 20), dunkel), 0, .015, 0, gard);
    for (let i = 0; i < 4; i++) { const a = i * Math.PI / 2; const h = at(box(.02, .02, .16, dunkel), Math.sin(a) * .08, 1.75, Math.cos(a) * .08, gard); h.rotation.y = a; h.rotation.x = -.5; }
    const sachen = new THREE.Group(); gard.add(sachen);
    const leder = std({ color: 0x5b3a22, roughness: .45 });
    const jacke = at(box(.46, .7, .16, leder), 0, 1.32, .14, sachen); jacke.rotation.x = .08;
    at(box(.1, .62, .1, leder), -.25, 1.3, .14, sachen); at(box(.1, .62, .1, leder), .25, 1.3, .14, sachen);
    const hut = new THREE.Group(); at(new THREE.Mesh(new THREE.CylinderGeometry(.2, .2, .012, 28), std({ color: 0x5e4630, roughness: .85 })), 0, 0, 0, hut);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.1, .117, .1, 24), std({ color: 0x5e4630, roughness: .85 })), 0, .05, 0, hut);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.118, .119, .026, 24), std({ color: 0x2a1d14 })), 0, .016, 0, hut);
    hut.position.set(0, 1.87, 0); hut.rotation.z = .12; sachen.add(hut);
    const lampeT = new THREE.Group(); at(new THREE.Mesh(new THREE.CylinderGeometry(.025, .025, .2, 12), std({ color: 0xe8b82a, roughness: .4 })), 0, 0, 0, lampeT);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.04, .025, .06, 12), metall), 0, .12, 0, lampeT); lampeT.position.set(.2, 1.45, -.06); sachen.add(lampeT);
    gard.position.set(x1 - .45, 0, 1.75); scene.add(gard);
    // Pflanze, Leuchten
    const topf = at(new THREE.Mesh(new THREE.CylinderGeometry(.2, .16, .4, 16), std({ color: 0xc8643a })), x0 + .45, .2, -2.6, scene);
    for (let i = 0; i < 9; i++) { const b = at(new THREE.Mesh(new THREE.SphereGeometry(.18 + rnd() * .1, 8, 6), std({ color: 0x3a7a3a })), x0 + .45 + (rnd() - .5) * .4, .6 + rnd() * .8, -2.6 + (rnd() - .5) * .4, scene); b.scale.y = 1.4; }
    topf.name = 'pflanze';
    for (const x of [-1.2, 1.2]) {
        at(new THREE.Mesh(new THREE.CylinderGeometry(.005, .005, .8, 4), dunkel), x, 2.8, .1, scene);
        const s = at(new THREE.Mesh(new THREE.ConeGeometry(.28, .22, 24, 1, true), std({ color: 0x1597bb, side: THREE.DoubleSide, emissive: 0x0a2a33 })), x, 2.32, .1, scene); s.castShadow = false;
        at(new THREE.Mesh(new THREE.SphereGeometry(.07, 12, 8), std({ color: 0xffffff, emissive: 0xffe2b0, emissiveIntensity: 3 })), x, 2.24, .1, scene);
        lampe(scene, x, 2.15, .1, 5);
    }
    const frei = freiImRaum([x0 + .35, z0 + .55, x1 - .4, z1 - .2], [[-1.15, -.35, 1.15, .75], [x1 - .55, -2.4, x1, -.8], [-3.45, z0, -1.55, z0 + .5], [x1 - .8, 1.4, x1, 2.1], [x0, -2.9, x0 + .75, -2.3]]);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => ({ p: new THREE.Vector3(.4 + p.x * .2, 2.9, 8.2), t: new THREE.Vector3(p.x * .15, 1.0, -.5) }),
        spawn: { start: [-1.9, 1.4, Math.PI / 2], tuer: [-3.2, 1.8, Math.PI / 2] },
        objekte: { umschlag, fax, regal: reg, garderobe: gard, garderobeSachen: sachen, tuer, tafel, teller, fensterGlas, pflanze: topf },
        tick(dt, t) { /* still */ }
    };
}

// ======================================================================
// Rathausflur (Erdgeschoss, erfunden): Eingang links, Treppe ins Untergeschoss rechts
// ======================================================================
export function flur(renderer) {
    seed = 1956;
    const x0 = -7.5, x1 = 7.5, z0 = -1.8, z1 = 1.8;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 3.0, boden: linoleum(), bodenKachel: .6, wand: putz('#e6e1d3'), hell: 1.0, sonne: .8, leiste: 0x4a4a48 });
    const dunkel = std({ color: 0x3a3c40, roughness: .6 }), holz = std({ color: 0x9a7048, roughness: .6 }), metall = std({ color: 0x9a9d9e, roughness: .35, metalness: .7 }), gruen = std({ color: 0x5a7a62 });
    // Sockelstreifen
    at(plane(15, .9, std({ color: 0xb8c2b0 })), 0, .45, z0 + .005, scene);
    const tuerBlatt = (x, schild) => {
        const g = new THREE.Group();
        at(box(1.1, 2.2, .06, holz), 0, 1.1, 0, g); at(box(1.24, .06, .08, dunkel), 0, 2.23, 0, g);
        at(box(.14, .03, .05, metall), .38, 1.05, .05, g);
        if (schild) { const s = plane(.6, .18, std({ map: schrift(256, 76, '#f4f2ea', '#2a2a2a', schild, 'bold 26px Arial') })); at(s, 0, 1.75, .04, g); }
        g.position.set(x, 0, z0 + .03); scene.add(g); return g;
    };
    tuerBlatt(-3.4, 'Bürgerbüro'); tuerBlatt(.2, 'Fraktionen'); tuerBlatt(3.4, 'Kämmerei');
    // Eingangstür links (Glas, Tageslicht dahinter)
    const ein = new THREE.Group(); ein.name = 'eingang';
    const licht = plane(2.6, 2.6, new THREE.MeshBasicMaterial({ color: 0xf2efe6 })); licht.rotation.y = Math.PI / 2; at(licht, .015, 1.3, 0, ein);
    for (const z of [-1.3, 0, 1.3]) { at(box(.08, 2.6, .08, dunkel), 0, 1.3, z, ein); }
    at(box(.08, .08, 2.6, dunkel), 0, 2.6, 0, ein);
    ein.position.set(x0 + .02, 0, .2); scene.add(ein);
    const ausgSchild = plane(.5, .2, std({ map: schrift(200, 80, '#1f8a4a', '#ffffff', 'Ausgang', 'bold 32px Arial'), emissive: 0x1f8a4a, emissiveIntensity: .4 }));
    ausgSchild.rotation.y = Math.PI / 2; ausgSchild.position.set(x0 + .03, 2.75, .2); scene.add(ausgSchild);
    // Treppe ins Untergeschoss rechts: Öffnung im Boden, Stufen, Geländer, Schild
    const treppe = new THREE.Group(); treppe.name = 'treppe';
    const loch = plane(1.6, 3.2, std({ color: 0x1a1816 })); loch.rotation.x = -Math.PI / 2; at(loch, 0, .005, 0, treppe);
    for (let i = 0; i < 7; i++) { at(box(1.5, .04, .4, std({ color: 0x8a8578 })), 0, -.02 - i * .18, -1.4 + i * .4, treppe).castShadow = false; }
    for (const x of [-.8, .8]) {
        at(box(.04, .04, 3.2, metall), x, .95, 0, treppe);
        for (let k = 0; k < 5; k++) { at(box(.025, .95, .025, metall), x, .475, -1.5 + k * .75, treppe); }
    }
    treppe.position.set(6.3, 0, -.1); scene.add(treppe);
    const archSchild = plane(1.3, .34, std({ map: schrift(400, 104, '#2a4a7a', '#ffffff', '', 'bold 30px Arial', ['Archiv ↓', 'Untergeschoss']) }));
    archSchild.position.set(6.3, 2.3, z0 + .02); scene.add(archSchild);
    // Schwarzes Brett, Ahnengalerie, Feuerlöscher, Bank, Pflanze
    const brett = new THREE.Group(); brett.name = 'brett';
    at(box(1.4, .9, .04, std({ color: 0x9a6a3a, roughness: .95 })), 0, 0, 0, brett);
    const zettelF = [0xfff27a, 0xffffff, 0xa9dfbf, 0xf5b7b1, 0xffffff];
    for (let i = 0; i < 9; i++) { const z = plane(.18 + rnd() * .1, .22 + rnd() * .08, std({ color: zettelF[i % 5] })); z.position.set(-.55 + (i % 5) * .26, .2 - Math.floor(i / 5) * .38, .025); z.rotation.z = (rnd() - .5) * .2; brett.add(z); }
    brett.position.set(-1.6, 1.5, z0 + .03); scene.add(brett);
    const galerie = new THREE.Group(); galerie.name = 'galerie';
    for (let i = 0; i < 3; i++) {
        const bild = canvasTex(128, 160, (g, w, h) => { g.fillStyle = '#4a3a2a'; g.fillRect(0, 0, w, h); g.fillStyle = '#8a7a6a'; g.fillRect(10, 10, w - 20, h - 20);
            g.fillStyle = '#d8b090'; g.beginPath(); g.ellipse(64, 70, 26, 32, 0, 0, 7); g.fill(); g.fillStyle = '#2a2a30'; g.fillRect(28, 110, 72, 40);
            g.fillStyle = ['#ccc', '#6a4a2a', '#222'][i]; g.fillRect(38, 34, 52, 12); }, false);
        const b = plane(.5, .62, std({ map: bild })); b.position.set(1.8 + i * .7, 1.6, z0 + .02); galerie.add(b);
        at(box(.56, .68, .02, std({ color: 0xb08a3a, metalness: .5, roughness: .4 })), 1.8 + i * .7, 1.6, z0 + .005, galerie);
    }
    scene.add(galerie);
    const loescher = new THREE.Group(); loescher.name = 'loescher';
    at(new THREE.Mesh(new THREE.CylinderGeometry(.09, .09, .5, 16), std({ color: 0xc0392b, roughness: .4 })), 0, .45, 0, loescher);
    at(box(.06, .08, .06, dunkel), 0, .74, 0, loescher);
    loescher.position.set(-5.3, 0, z0 + .15); scene.add(loescher);
    const bank = new THREE.Group(); at(box(1.6, .05, .42, holz), 0, .45, 0, bank); for (const x of [-.7, .7]) { at(box(.05, .45, .4, metall), x, .225, 0, bank); }
    bank.position.set(-3.4, 0, z0 + .45); scene.add(bank); bank.name = 'bank';
    const topf = at(new THREE.Mesh(new THREE.CylinderGeometry(.24, .2, .5, 16), std({ color: 0xe8e4dc })), 4.7, .25, z0 + .35, scene);
    for (let i = 0; i < 12; i++) { const b = at(new THREE.Mesh(new THREE.ConeGeometry(.08, .9, 5), gruen), 4.7 + (rnd() - .5) * .3, .9 + rnd() * .3, z0 + .35 + (rnd() - .5) * .3, scene); b.rotation.set((rnd() - .5) * .8, 0, (rnd() - .5) * .8); }
    // Deckenleuchten (Rasterleuchten)
    for (let x = -6; x <= 6; x += 3) {
        at(box(1.2, .05, .4, std({ color: 0xffffff, emissive: 0xfff6e0, emissiveIntensity: 1.6 })), x, 2.97, 0, scene).castShadow = false;
        lampe(scene, x, 2.6, .2, 4, 0xfff2dc);
    }
    const frei = freiImRaum([x0 + .4, z0 + .45, x1 - .35, z1 - .1], [[5.5, -1.7, 7.1, 1.5], [-4.25, z0, -2.55, z0 + .7], [4.4, z0, 5.0, z0 + .7]]);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => { const x = Math.max(-4.2, Math.min(4.2, p.x * .85)); return { p: new THREE.Vector3(x, 2.4, 6.0), t: new THREE.Vector3(x, 1.15, -.6) }; },
        spawn: { eingang: [-6.5, .4, Math.PI / 2], treppe: [5.0, .8, -Math.PI / 2] },
        objekte: { eingang: ein, treppe, brett, galerie, loescher, bank },
        tick() { }
    };
}

// ======================================================================
// Rathausarchiv (Untergeschoss, erfunden): Regalwand mit Ratsprotokollen, Gemälde „Der Rat von 1949“
// ======================================================================
export function archiv(renderer) {
    seed = 1946;
    const x0 = -5, x1 = 5, z0 = -3.5, z1 = 3.2;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 2.9, boden: beton(), bodenKachel: 1.2, wand: putz('#d8d2c0'), hell: .7, sonne: .55, himmel: 0xe6f0ff, leiste: 0x3a3a3a });
    const stahl = std({ color: 0x7a8288, roughness: .45, metalness: .6 }), dunkel = std({ color: 0x2a2c30 }), holz = std({ color: 0x7a5236, roughness: .6 });
    // raumfüllendes Regal an der Rückwand: Ratsprotokolle 1946 ff.
    const reg = new THREE.Group(); reg.name = 'protokolle';
    for (let i = 0; i < 4; i++) { const r = regal(2.3, 2.6, .45, 6, stahl, true); r.position.set(-3.5 + i * 2.35, 0, 0); reg.add(r); }
    const jahre = schrift(512, 64, '#e8e2d0', '#2a2a2a', 'Ratsprotokolle 1946 –', 'bold 34px Georgia');
    const etikett = plane(2.0, .25, std({ map: jahre })); etikett.position.set(-3.5, 2.72, .24); reg.add(etikett);
    reg.position.set(0, 0, z0 + .26); scene.add(reg);
    // Gemälde an der rechten Wand
    const bildTex = canvasTex(640, 448, (g, w, h) => {
        const gr = g.createLinearGradient(0, 0, 0, h); gr.addColorStop(0, '#3a2a1a'); gr.addColorStop(1, '#1e140c'); g.fillStyle = gr; g.fillRect(0, 0, w, h);
        // Stadtwappen oben
        g.fillStyle = '#c8a040'; g.beginPath(); g.moveTo(w / 2 - 34, 30); g.lineTo(w / 2 + 34, 30); g.lineTo(w / 2 + 34, 80); g.quadraticCurveTo(w / 2, 120, w / 2 - 34, 80); g.closePath(); g.fill();
        g.fillStyle = '#7a1e1a'; g.fillRect(w / 2 - 6, 40, 12, 56); g.fillRect(w / 2 - 24, 56, 48, 10);
        // Ratstisch und Ratsherren (Amateur-Pinselstrich)
        g.fillStyle = '#5a3a1e'; g.fillRect(40, 300, w - 80, 34);
        for (let i = 0; i < 9; i++) {
            const x = 70 + i * 62, y = 230 + (i % 2) * 6;
            g.fillStyle = ['#2a2a32', '#3a3226', '#262a22'][i % 3]; g.beginPath(); g.moveTo(x - 24, 300); g.lineTo(x - 18, y + 20); g.lineTo(x + 18, y + 20); g.lineTo(x + 24, 300); g.fill();
            g.fillStyle = '#d8b090'; g.beginPath(); g.arc(x, y, 15, 0, 7); g.fill();
            g.fillStyle = i % 3 ? '#4a3a2a' : '#bbb'; g.fillRect(x - 14, y - 16, 28, 8);
        }
        g.fillStyle = 'rgba(255,230,180,.08)'; g.fillRect(0, 0, w, h);
        g.fillStyle = '#e8d8a8'; g.font = 'italic 22px Georgia'; g.textAlign = 'center'; g.fillText('Der Rat von 1949', w / 2, 380);
        g.fillStyle = 'rgba(232,216,168,.35)'; g.font = 'italic 15px Georgia'; g.fillText('Was beschlossen wird, ruht in Protokoll 47.', w / 2, 416);
    }, false);
    const bild = new THREE.Group(); bild.name = 'gemaelde';
    at(box(.06, 1.42, 2.02, std({ color: 0xb08a3a, metalness: .55, roughness: .35 })), 0, 0, 0, bild);
    const leinwand = plane(1.84, 1.29, std({ map: bildTex, roughness: .6 })); leinwand.rotation.y = -Math.PI / 2; at(leinwand, -.035, 0, 0, bild);
    bild.position.set(x1 - .04, 1.7, -.6); scene.add(bild);
    const bildLicht = new THREE.SpotLight(0xffe2b0, 18, 6, .5, .6, 1.5); bildLicht.position.set(x1 - 1.4, 2.7, -.6); bildLicht.target = bild; scene.add(bildLicht);
    // Stahltür links (zum Flur)
    const tuer = new THREE.Group(); tuer.name = 'stahltuer';
    at(box(.08, 2.15, 1.05, std({ color: 0x5f6a70, roughness: .5, metalness: .5 })), 0, 1.075, 0, tuer);
    at(box(.1, 2.25, 1.2, dunkel), -.02, 1.12, 0, tuer).scale.set(.5, 1, 1);
    at(box(.06, .04, .18, stahl), .07, 1.05, .35, tuer);
    const ts = plane(.5, .2, std({ map: schrift(250, 100, '#f2d230', '#1a1a1a', '', 'bold 26px Arial', ['Zutritt nur', 'nach Beschluss']) })); ts.rotation.y = Math.PI / 2; at(ts, .05, 1.6, 0, tuer);
    tuer.position.set(x0 + .04, 0, 1.1); scene.add(tuer);
    // Lesetisch mit Lampe, Rollwagen, Kartons
    const tisch = new THREE.Group(); tisch.name = 'lesetisch';
    at(box(1.4, .05, .8, holz), 0, .74, 0, tisch); for (const [x, z] of [[-.65, -.35], [.65, -.35], [-.65, .35], [.65, .35]]) { at(box(.05, .72, .05, dunkel), x, .36, z, tisch); }
    at(new THREE.Mesh(new THREE.ConeGeometry(.14, .16, 16, 1, true), std({ color: 0x2f6a4a, side: THREE.DoubleSide })), .4, 1.14, -.2, tisch);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.012, .012, .4, 6), dunkel), .4, .94, -.2, tisch);
    at(box(.3, .04, .22, std({ color: 0xe8e0c8 })), -.2, .785, .05, tisch).rotation.y = .3;
    tisch.position.set(1.6, 0, .9); scene.add(tisch);
    const tischLicht = new THREE.PointLight(0xffd9a0, 2.5, 3, 1.5); tischLicht.position.set(2.0, 1.0, .7); scene.add(tischLicht);
    for (const [x, z, r] of [[-2.4, .3, .1], [-2.1, .9, -.2], [-2.5, 1.0, .4]]) { at(box(.5, .35, .4, std({ color: 0xb8925a, roughness: .9 })), x, .175 + (x === -2.5 ? .35 : 0), z, scene).rotation.y = r; }
    // Neonröhren (eine flackert)
    const roehren = [];
    for (const x of [-2.5, 0, 2.5]) {
        const r = at(box(1.3, .05, .1, std({ color: 0xffffff, emissive: 0xe8f4ff, emissiveIntensity: 2 })), x, 2.85, -.4, scene); r.castShadow = false;
        roehren.push({ r, L: lampe(scene, x, 2.5, -.2, 4.5, 0xe8f4ff) });
    }
    // Klebespuren: Dichtstoff-Raupen und Schuhsohlen (sichtbar je nach Spielstand)
    const kleber = std({ color: 0x18181a, roughness: .3 });
    const raupe = (x, z, ziel = scene) => { const m = new THREE.Mesh(new THREE.CapsuleGeometry(.035, .5, 4, 8), kleber); m.rotation.z = Math.PI / 2; m.position.set(x, .03, z); ziel.add(m); return m; };
    const raupen = [raupe(-3.5, .55), raupe(-3.5, 1.65)];
    const sohlen = new THREE.Group(); sohlen.name = 'sohlen';
    for (const [x, z] of [[2.7, -2.7], [3.6, -2.7]]) { for (const d of [-.1, .1]) { at(box(.11, .03, .3, std({ color: 0x1a1a1a, roughness: .5 })), x + d, .02, z, sohlen); } raupe(x, z + .1, sohlen); }
    scene.add(sohlen);
    const frei = freiImRaum([x0 + .35, z0 + .75, x1 - .35, z1 - .1], [[.8, .4, 2.4, 1.4], [-2.8, 0, -1.8, 1.3]]);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => ({ p: new THREE.Vector3(.3 + p.x * .25, 2.6, 8.6), t: new THREE.Vector3(p.x * .2, 1.05, -.7) }),
        spawn: { tuer: [-2.9, -.9, Math.PI / 2 + .4] },
        objekte: { protokolle: reg, gemaelde: bild, tuer, lesetisch: tisch, raupen, sohlen },
        tick(dt, t) {
            const f = Math.sin(t * 23) > .7 && Math.sin(t * 1.3) > .2;
            roehren[2].r.material.emissiveIntensity = f ? .2 : 2; roehren[2].L.intensity = f ? .5 : 4.5;
        }
    };
}

// ======================================================================
// Akt 4–5: Schulflur, Krypta, Tempel des Grundgesetzes (frei erfunden)
// ======================================================================
const backstein = () => canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#5a3a2a'; g.fillRect(0, 0, w, h);
    for (let r = 0; r < 16; r++) { for (let c = -1; c < 5; c++) {
        const x = c * 64 + (r % 2) * 32, y = r * 16;
        g.fillStyle = `rgb(${120 + rnd() * 40},${62 + rnd() * 20},${42 + rnd() * 14})`; g.fillRect(x + 2, y + 2, 60, 12);
    } }
});
const sandstein = () => canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#c9a874'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 2500; i++) { g.fillStyle = `rgba(${rnd() < .5 ? '90,60,30' : '255,240,210'},${rnd() * .08})`; g.fillRect(rnd() * w, rnd() * h, 3, 2); }
    g.strokeStyle = 'rgba(80,55,30,.35)'; g.lineWidth = 2;
    for (let y = 0; y <= h; y += 64) { g.beginPath(); g.moveTo(0, y); g.lineTo(w, y); g.stroke(); }
    for (let y = 0; y < h; y += 64) { for (let x = (y / 64) % 2 ? 64 : 0; x < w; x += 128) { g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + 64); g.stroke(); } }
});
function fackel(scene, x, y, z, roh) {
    const g = new THREE.Group();
    at(new THREE.Mesh(new THREE.CylinderGeometry(.05, .03, .5, 8), std({ color: 0x3a2a1a })), 0, 0, 0, g);
    const flamme = at(new THREE.Mesh(new THREE.ConeGeometry(.09, .3, 10), new THREE.MeshBasicMaterial({ color: 0xffb040 })), 0, .38, 0, g);
    g.position.set(x, y, z); g.rotation.z = roh || 0; scene.add(g);
    const L = new THREE.PointLight(0xff9a40, 5, 9, 1.6); L.position.set(x, y + .5, z + (z < 0 ? .3 : 0)); scene.add(L);
    return { flamme, L, seed: rnd() * 10 };
}
const flackern = (f, t) => { const k = .8 + .2 * Math.sin(t * 13 + f.seed) * Math.sin(t * 7.3 + f.seed * 2); f.L.intensity = 5 * k; f.flamme.scale.set(1, .8 + .4 * k, 1); };

export function schulflur(renderer) {
    seed = 1949 * 2;
    const x0 = -8, x1 = 8, z0 = -1.8, z1 = 1.8;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 3.0, boden: linoleum(), bodenKachel: .5, wand: putz('#f2ecd8'), hell: 1.0, sonne: .8, leiste: 0x3a6a9a });
    const holz = std({ color: 0xb07a40, roughness: .6 }), dunkel = std({ color: 0x3a3c40 }), metall = std({ color: 0x9a9d9e, roughness: .35, metalness: .7 });
    at(plane(16, 1.0, std({ color: 0x8fc0e8 })), 0, .5, z0 + .005, scene);
    const tuer = (x, schild, farbe) => {
        const g = new THREE.Group();
        at(box(1.0, 2.15, .06, std({ color: farbe, roughness: .6 })), 0, 1.075, 0, g); at(box(1.14, .06, .08, dunkel), 0, 2.18, 0, g);
        at(box(.14, .03, .05, metall), .34, 1.05, .05, g);
        at(plane(.56, .2, std({ map: schrift(256, 90, '#ffffff', '#2a2a2a', schild, 'bold 40px Arial') })), 0, 1.7, .04, g);
        g.position.set(x, 0, z0 + .03); scene.add(g); return g;
    };
    const t1a = tuer(-4.4, 'Klasse 1a', 0xe8c040), t2b = tuer(-.6, 'Klasse 2b', 0x40a0d8), wc = tuer(2.6, 'WC', 0x88aa66);
    // Kellertür mit Absperrband und Zettel
    const keller = new THREE.Group(); keller.name = 'kellertuer';
    at(box(1.1, 2.2, .08, std({ color: 0x6a7076, roughness: .45, metalness: .5 })), 0, 1.1, 0, keller);
    at(plane(.5, .16, std({ map: schrift(256, 80, '#f2d230', '#1a1a1a', 'KELLER', 'bold 46px Arial') })), 0, 1.85, .05, keller);
    const band = std({ map: canvasTex(128, 32, (g, w, h) => { for (let i = 0; i < 8; i++) { g.fillStyle = i % 2 ? '#ffffff' : '#d42020'; g.fillRect(i * 16, 0, 16, h); } }, false) });
    for (const [y, r] of [[1.4, .25], [.9, -.2]]) { const b = at(box(1.5, .07, .01, band), 0, y, .09, keller); b.rotation.z = r; }
    at(plane(.24, .3, std({ color: 0xffffff })), .28, 1.2, .1, keller);
    keller.position.set(6.6, 0, z0 + .04); scene.add(keller);
    // Kinderbilder, Garderobenleiste mit Turnbeuteln
    const farben = ['#e74c3c', '#f1c40f', '#2ecc71', '#3498db', '#9b59b6', '#e67e22'];
    const bilder = new THREE.Group(); bilder.name = 'bilder';
    for (let i = 0; i < 7; i++) {
        const tex = canvasTex(128, 96, (g, w, h) => { g.fillStyle = '#ffffff'; g.fillRect(0, 0, w, h); g.fillStyle = farben[i % 6]; g.beginPath(); g.arc(40 + rnd() * 40, 40, 20, 0, 7); g.fill();
            g.fillStyle = '#2a8a3a'; g.fillRect(0, h - 18, w, 18); g.fillStyle = '#5a3a2a'; g.fillRect(80, 40, 18, 40); g.fillStyle = '#e74c3c'; g.beginPath(); g.moveTo(74, 40); g.lineTo(89, 22); g.lineTo(104, 40); g.fill(); }, false);
        const b = plane(.5, .38, std({ map: tex })); b.position.set(-7.2 + i * .62 + (i > 2 ? 1.8 : 0) + (i > 4 ? 1.6 : 0), 1.85 + (i % 2) * .15, z0 + .02); b.rotation.z = (rnd() - .5) * .1; bilder.add(b);
    }
    scene.add(bilder);
    const haken = new THREE.Group(); haken.name = 'turnbeutel';
    at(box(3.2, .08, .06, holz), 0, 1.35, 0, haken);
    for (let i = 0; i < 7; i++) { const tb = at(box(.28, .38, .06, std({ color: new THREE.Color(farben[i % 6]) })), -1.4 + i * .46, 1.1, .05, haken); tb.rotation.z = (rnd() - .5) * .15; }
    haken.position.set(1.2, 0, z0 + .04); scene.add(haken);
    // Ausgang zum Hof (Glastür links), Deckenlicht
    const hof = new THREE.Group(); hof.name = 'hoftuer';
    const licht = plane(2.4, 2.6, new THREE.MeshBasicMaterial({ color: 0x9fb4d8 })); licht.rotation.y = Math.PI / 2; at(licht, .015, 1.3, 0, hof);
    for (const z of [-1.2, 0, 1.2]) { at(box(.08, 2.6, .08, dunkel), 0, 1.3, z, hof); }
    hof.position.set(x0 + .02, 0, .2); scene.add(hof);
    for (let x = -6; x <= 6; x += 3) { at(box(1.2, .05, .4, std({ color: 0xffffff, emissive: 0xfff6e0, emissiveIntensity: 1.6 })), x, 2.97, 0, scene).castShadow = false; lampe(scene, x, 2.6, .2, 4, 0xfff2dc); }
    const frei = freiImRaum([x0 + .4, z0 + .45, x1 - .35, z1 - .1], []);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => { const x = Math.max(-4.5, Math.min(4.5, p.x * .85)); return { p: new THREE.Vector3(x, 2.4, 6.0), t: new THREE.Vector3(x, 1.15, -.6) }; },
        spawn: { von_schule: [-7.0, .4, Math.PI / 2], von_krypta: [6.0, -.6, -Math.PI / 2] },
        objekte: { t1a, t2b, wc, keller, hof, bilder, haken },
        tick() { }
    };
}

export function krypta(renderer) {
    seed = 1517;
    const x0 = -5.5, x1 = 5.5, z0 = -3.5, z1 = 3.0;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 3.4, boden: beton(), bodenKachel: 1.2, wand: backstein(), hell: .25, sonne: .15, himmel: 0x8090c0, hintergrund: 0x050302, leiste: 0x2a1a10 });
    // Tonnengewölbe
    const gew = new THREE.Mesh(new THREE.CylinderGeometry(5.5, 5.5, 12, 32, 1, true, -Math.PI / 2, Math.PI), std({ map: backstein(), side: THREE.BackSide }));
    gew.material.map.repeat.set(6, 4); gew.rotation.z = Math.PI / 2; gew.scale.set(.25, 1, 1); gew.position.set(0, 3.4, 0); scene.add(gew);
    const stein = std({ color: 0x8a8070, roughness: .95 }), bronze = std({ color: 0x9a7a3a, metalness: .8, roughness: .35 });
    // Fresko (unten Urne, Mitte Buch, oben Waage)
    const freskoTex = canvasTex(256, 384, (g, w, h) => {
        g.fillStyle = '#d8c8a0'; g.fillRect(0, 0, w, h);
        for (let i = 0; i < 600; i++) { g.fillStyle = `rgba(120,90,60,${rnd() * .12})`; g.fillRect(rnd() * w, rnd() * h, 6, 3); }
        g.strokeStyle = '#7a4a2a'; g.fillStyle = '#7a4a2a'; g.lineWidth = 6;
        g.beginPath(); g.moveTo(128, 30); g.lineTo(128, 100); g.moveTo(70, 50); g.lineTo(186, 50); g.stroke();
        for (const x of [70, 186]) { g.beginPath(); g.arc(x, 80, 26, 0, Math.PI); g.stroke(); g.beginPath(); g.moveTo(x - 26, 80); g.lineTo(x, 50); g.lineTo(x + 26, 80); g.stroke(); }
        g.fillStyle = '#2a4a7a'; g.fillRect(70, 160, 116, 70); g.fillStyle = '#d8c8a0'; g.fillRect(126, 160, 4, 70);
        g.fillStyle = '#5a3a1e'; g.fillRect(84, 290, 88, 64); g.fillStyle = '#1a1a1a'; g.fillRect(108, 288, 40, 6);
        g.fillStyle = 'rgba(216,200,160,.5)'; for (let i = 0; i < 30; i++) { g.fillRect(rnd() * w, rnd() * h, 20 + rnd() * 30, 6 + rnd() * 10); }
        g.fillStyle = '#5a3a2a'; g.font = 'italic 17px Georgia'; g.textAlign = 'center'; g.fillText('von unten nach oben', w / 2, h - 8);
    }, false);
    const fresko = plane(1.5, 2.25, std({ map: freskoTex, roughness: 1 })); fresko.position.set(-3.6, 1.75, z0 + .02); fresko.name = 'fresko'; scene.add(fresko);
    // Symbolschloss: Sockel mit drei Drehscheiben
    const schloss = new THREE.Group(); schloss.name = 'schloss';
    at(box(1.0, 1.0, .6, stein), 0, .5, 0, schloss);
    const scheiben = [];
    const symbol = (art) => canvasTex(64, 64, (g, w) => { g.fillStyle = '#c8a050'; g.fillRect(0, 0, w, w); g.fillStyle = '#3a2a10'; g.strokeStyle = '#3a2a10'; g.lineWidth = 4;
        if (art === 0) { g.beginPath(); g.moveTo(32, 10); g.lineTo(32, 50); g.moveTo(12, 20); g.lineTo(52, 20); g.stroke(); g.beginPath(); g.arc(14, 34, 9, 0, Math.PI); g.arc(50, 34, 9, 0, Math.PI); g.fill(); }
        if (art === 1) { g.fillRect(14, 18, 36, 28); }
        if (art === 2) { g.fillRect(16, 24, 32, 28); g.fillStyle = '#c8a050'; g.fillRect(24, 22, 16, 4); } }, false);
    for (let i = 0; i < 3; i++) {
        const s = new THREE.Mesh(new THREE.CylinderGeometry(.16, .16, .06, 24), [bronze, std({ map: symbol(i), metalness: .6, roughness: .4 }), bronze]);
        s.rotation.x = Math.PI / 2; s.position.set(-.32 + i * .32, .75, .31); schloss.add(s); scheiben.push(s);
    }
    schloss.position.set(-.6, 0, z0 + .6); scene.add(schloss);
    // Tor mit drei Vertiefungen und Schlüsselloch
    const tor = new THREE.Group(); tor.name = 'tor';
    const rahmen = std({ color: 0x9a8a70, roughness: .9 });
    at(box(.5, 3.0, .5, rahmen), -1.3, 1.5, 0, tor); at(box(.5, 3.0, .5, rahmen), 1.3, 1.5, 0, tor); at(box(3.1, .5, .5, rahmen), 0, 3.0, 0, tor);
    const fluegel = new THREE.Group(); tor.add(fluegel);
    const blatt = at(box(2.1, 2.75, .2, std({ color: 0x6a5a48, roughness: .95 })), 0, 1.375, 0, fluegel);
    const siegelTeile = [];
    for (let i = 0; i < 3; i++) {
        at(new THREE.Mesh(new THREE.CylinderGeometry(.18, .18, .05, 24), std({ color: 0x2a2018 })), -.6 + i * .6, 2.1, .1, fluegel).rotation.x = Math.PI / 2;
        const s = at(new THREE.Mesh(new THREE.CylinderGeometry(.16, .16, .06, 24), std({ color: [0xd4af37, 0xc0c0c8, 0xb87333][i], metalness: .9, roughness: .25 })), -.6 + i * .6, 2.1, .12, fluegel);
        s.rotation.x = Math.PI / 2; siegelTeile.push(s);
    }
    at(box(.08, .18, .03, std({ color: 0x0a0a0a })), 0, 1.3, .11, fluegel);
    const dahinter = plane(2.1, 2.75, new THREE.MeshBasicMaterial({ color: 0xffd890 })); dahinter.position.set(0, 1.375, -.3); tor.add(dahinter);
    tor.position.set(3.0, 0, z0 + .3); scene.add(tor);
    // Treppe nach oben (links)
    const treppe = new THREE.Group(); treppe.name = 'treppe';
    for (let i = 0; i < 8; i++) { at(box(1.4, .2, .35, stein), 0, .1 + i * .2, -i * .35 + 1.2, treppe); }
    treppe.position.set(x0 + .9, 0, 1.2); scene.add(treppe);
    const fackeln = [fackel(scene, -1.6, 1.9, z0 + .15), fackel(scene, 1.4, 1.9, z0 + .15), fackel(scene, 5.2, 1.9, 0, -.4)];
    const frei = freiImRaum([x0 + .4, z0 + .7, x1 - .4, z1 - .1], [[-1.15, z0, -.05, z0 + 1.0], [x0, -.5, x0 + 1.7, z1]]);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => ({ p: new THREE.Vector3(.3 + p.x * .2, 2.4, 8.0), t: new THREE.Vector3(p.x * .15, 1.2, -.8) }),
        spawn: { von_schulflur: [x0 + 2.2, 1.6, Math.PI / 2], von_tempel: [3.0, -1.8, Math.PI] },
        objekte: { fresko, schloss, scheiben, tor, fluegel, siegelTeile, treppe },
        tick(dt, t) { fackeln.forEach((f) => flackern(f, t)); }
    };
}

/** Tempelhalle: Fallgitter mit zwei Plattformen, Tafel, Abgrund (mit Hängebrücke), Gang mit der Wahlkugel. */
export function tempelHalle(renderer) {
    seed = 1949 * 3;
    const x0 = -6.5, x1 = 6.5, z0 = -24, z1 = 4;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 7, boden: sandstein(), bodenKachel: 2, wand: sandstein(), hell: .35, sonne: .25, himmel: 0xffe0b0, hintergrund: 0x0a0604, leiste: 0x7a5a3a });
    const sst = std({ map: sandstein(), roughness: .9 }), dunkel = std({ color: 0x2a2018 }), eisen = std({ color: 0x3a3a3e, metalness: .7, roughness: .5 });
    // Säulen
    for (const z of [2, -3, -9, -20]) { for (const x of [-5.6, 5.6]) { at(new THREE.Mesh(new THREE.CylinderGeometry(.45, .5, 7, 16), sst), x, 3.5, z, scene); } }
    // Querwand mit Durchgang und Fallgitter bei z = -6.5
    at(box(4.5, 7, 1, sst), -4.25, 3.5, -6.5, scene); at(box(4.5, 7, 1, sst), 4.25, 3.5, -6.5, scene); at(box(4, 3, 1, sst), 0, 5.5, -6.5, scene);
    const gitter = new THREE.Group(); gitter.name = 'gitter';
    for (let x = -1.8; x <= 1.81; x += .3) { at(box(.06, 4, .06, eisen), x, 2, 0, gitter); }
    for (const y of [.8, 2, 3.2]) { at(box(3.8, .06, .06, eisen), 0, y, 0, gitter); }
    gitter.position.set(0, 0, -6.5); scene.add(gitter);
    const inschrift = plane(3.6, .5, std({ map: schrift(512, 72, '#c9a874', '#4a2a10', 'FRAKTIONSZWANG', 'bold 54px Georgia') }));
    inschrift.position.set(0, 4.4, -5.99); inschrift.name = 'inschrift'; scene.add(inschrift);
    // Plattformen auf Drehzapfen
    const platte = (x, schmal, gravur) => {
        const g = new THREE.Group();
        at(new THREE.Mesh(new THREE.CylinderGeometry(.18, .25, .5, 12), dunkel), 0, .25, 0, g);
        const p = at(box(schmal ? .9 : 1.6, schmal ? .14 : .35, schmal ? .9 : 1.6, sst), 0, .55, 0, g);
        const gr = plane(.4, .4, std({ map: canvasTex(64, 64, (c, w) => { c.clearRect(0, 0, w, w); c.fillStyle = '#4a2a10';
            if (gravur === 'hut') { c.fillRect(10, 40, 44, 6); c.fillRect(20, 20, 24, 22); } else { c.fillRect(12, 22, 40, 26); c.fillStyle = '#c9a874'; c.beginPath(); c.arc(32, 35, 8, 0, 7); c.fill(); } }, false), transparent: true }));
        gr.rotation.x = -Math.PI / 2; gr.position.y = (schmal ? .63 : .73); g.add(gr);
        g.position.set(x, 0, -4.2); scene.add(g); g.userData.oben = schmal ? .62 : .72; return g;
    };
    const links = platte(-3.2, true, 'hut'), rechts = platte(3.2, false, 'kamera');
    links.name = 'platte_links'; rechts.name = 'platte_rechts';
    // Tafel der Geschäftsordnung
    const tafelTex = canvasTex(256, 320, (g, w, h) => { g.fillStyle = '#8a7050'; g.fillRect(0, 0, w, h); g.fillStyle = '#3a2410'; g.font = 'bold 26px Georgia'; g.textAlign = 'center'; g.fillText('GESCHÄFTS-', w / 2, 44); g.fillText('ORDNUNG', w / 2, 76);
        g.font = '20px Georgia'; ['§ 1 …', '§ 2 …', '§ 3 …'].forEach((t, i) => g.fillText(t, w / 2, 140 + i * 50)); }, false);
    const tafel = new THREE.Group(); tafel.name = 'tafel';
    at(box(1.6, 2.0, .25, sst), 0, 1.6, 0, tafel); const tf = plane(1.4, 1.75, std({ map: tafelTex, emissive: 0x000000 })); tf.position.set(0, 1.6, .13); tafel.add(tf);
    tafel.rotation.y = Math.PI / 2; tafel.position.set(x0 + .2, 0, -9.2); scene.add(tafel);
    // Abgrund (z −12 … −17) mit Hängebrücke
    const abgrund = new THREE.Group(); abgrund.name = 'abgrund';
    const schwarz = at(box(13, .1, 5, new THREE.MeshBasicMaterial({ color: 0x000000 })), 0, .01, -14.5, abgrund);
    for (const z of [-12, -17]) { at(box(13, .3, .25, std({ color: 0x6a5030 })), 0, -.1, z, abgrund); }
    scene.add(abgrund);
    const pick = new THREE.Mesh(new THREE.BoxGeometry(12, 1, 4.6), new THREE.MeshBasicMaterial({ visible: false })); pick.position.set(0, -.3, -14.5); abgrund.add(pick);
    const bruecke = new THREE.Group(); bruecke.name = 'bruecke';
    for (let i = 0; i < 12; i++) { const z = -12.1 - i * .42, y = -.12 - Math.sin((i + .5) / 12 * Math.PI) * .35; at(box(1.8, .06, .36, std({ color: 0x8a6440 })), 0, y, z, bruecke); }
    for (const x of [-.95, .95]) { const kurve = new THREE.CatmullRomCurve3([new THREE.Vector3(x, 1, -12), new THREE.Vector3(x, .6, -14.5), new THREE.Vector3(x, 1, -17)]); at(new THREE.Mesh(new THREE.TubeGeometry(kurve, 20, .025, 6), std({ color: 0xc8b080 })), 0, 0, 0, bruecke); for (const z of [-12, -17]) { at(new THREE.Mesh(new THREE.CylinderGeometry(.06, .06, 1.1, 8), std({ color: 0x5a3a20 })), x, .55, z, bruecke); } }
    scene.add(bruecke);
    // Pult und Gang
    const pult = new THREE.Group(); at(box(.7, 1.1, .5, sst), 0, .55, 0, pult); const pd = at(box(.8, .06, .6, sst), 0, 1.15, 0, pult); pd.rotation.x = .3; pult.position.set(2.6, 0, -18.4); scene.add(pult);
    const gangTor = new THREE.Group(); gangTor.name = 'gang';
    at(box(1, 4.5, 1, sst), -2.5, 2.25, 0, gangTor); at(box(1, 4.5, 1, sst), 2.5, 2.25, 0, gangTor); at(box(6, 1, 1, sst), 0, 4.75, 0, gangTor);
    gangTor.position.set(0, 0, -20.5); scene.add(gangTor);
    // Querwand hinter dem Gangtor (der Gang führt durch sie hindurch)
    at(box(3.5, 7, 1, sst), -4.75, 3.5, -21, scene); at(box(3.5, 7, 1, sst), 4.75, 3.5, -21, scene); at(box(4, 2, 1, sst), 0, 6, -21, scene);
    // Gang: 60 m, abschüssig (wird für die Wahlkugel gebraucht)
    const GL = 60, GF = -.1;
    const gang = new THREE.Group();
    const gb = at(box(4, .3, GL, sst), 0, -.15, 0, gang); gb.name = 'gangboden';
    at(box(.4, 4.5, GL, sst), -2.2, 2.1, 0, gang); at(box(.4, 4.5, GL, sst), 2.2, 2.1, 0, gang); at(box(4.8, .4, GL, sst), 0, 4.5, 0, gang);
    const neigung = Math.asin(GF);
    gang.rotation.x = neigung; gang.position.set(0, GL / 2 * Math.sin(neigung), -21.5 - GL / 2 * Math.cos(neigung)); scene.add(gang);
    const gangY = (z) => (z < -21.5 ? GF * (-21.5 - z) : 0);
    const gangFackeln = [];
    for (let i = 0; i < 6; i++) { const z = -26 - i * 10; gangFackeln.push(fackel(scene, i % 2 ? 1.95 : -1.95, 2.0 + gangY(z), z)); }
    // Wahlkugel (oben im Gang, zunächst eingeklemmt)
    const kugelTex = canvasTex(256, 128, (g, w, h) => { g.fillStyle = '#a89070'; g.fillRect(0, 0, w, h); g.fillStyle = '#4a3420'; g.font = 'bold 40px Georgia'; g.textAlign = 'center'; g.fillText('☒ WAHL ☒', w / 2, h / 2 + 14); });
    const kugel = new THREE.Mesh(new THREE.SphereGeometry(1.8, 32, 24), std({ map: kugelTex, roughness: .9 })); kugel.castShadow = true; scene.add(kugel);
    const fackeln = [fackel(scene, -5.9, 2.4, 0, .3), fackel(scene, 5.9, 2.4, 0, -.3), fackel(scene, -5.9, 2.4, -10, .3), fackel(scene, 5.9, 2.4, -10, -.3), fackel(scene, -1.6, 3.2, -6.0), fackel(scene, 1.6, 3.2, -6.0)];
    // Position im Gang: s = 0 … 1 → Weltkoordinaten
    const gangPunkt = (s, x = 0, dy = 0) => { const z = -22 - s * (GL - 4); return new THREE.Vector3(x, gangY(z) + dy, z); };
    let zst = null;
    const frei = (x, z, aktuell) => {
        if (aktuell) { zst = aktuell; }
        if (x < x0 + .5 || x > x1 - .5 || z > z1 - .2) { return false; }
        if (z < -21.3) { return false; }
        if (Math.hypot(x - -3.2, z + 4.2) < .75 || Math.hypot(x - 3.2, z + 4.2) < 1.1) { return false; }
        if (z < -6.0 && z > -7.0 && (Math.abs(x) > 1.6 || !(zst && zst.flags.gitterOffen))) { return false; }
        if (z < -6.0 && !(zst && zst.flags.gitterOffen)) { return false; }
        if (z < -11.8 && z > -17.2 && !(zst && zst.flags.brueckeDa && Math.abs(x) < .7)) { return false; }
        if (Math.hypot(x - 2.6, z + 18.4) < .7) { return false; }
        if (z < -20 && Math.abs(x) > 1.6) { return false; }
        return true;
    };
    return {
        scene, frei, hoehe: (x, z) => (z < -11.9 && z > -17.1 ? -.12 - Math.sin(Math.max(0, Math.min(1, (-12.1 - z) / 5)) * Math.PI) * .35 : gangY(z)),
        kamera: (p) => ({ p: new THREE.Vector3(p.x * .4, p.y + 4.2, p.z + 8.2), t: new THREE.Vector3(p.x * .6, p.y + 1.2, p.z - 3) }),
        spawn: { von_krypta: [0, 2.5, Math.PI], gang: [0, -19.6, Math.PI] },
        objekte: { gitter, inschrift, links, rechts, tafel, tf, abgrund, bruecke, gangTor, kugel, schwarz },
        gangPunkt, zustandSetzen(z) { zst = z; },
        tick(dt, t) { fackeln.concat(gangFackeln).forEach((f) => flackern(f, t)); }
    };
}

/** Kammer des Grals: Pokal auf dem Podest, schlichte Wahlurne, Lichtschacht. */
export function tempelFinale(renderer) {
    seed = 23;
    const x0 = -6, x1 = 6, z0 = -5, z1 = 4;
    const { scene } = raum(renderer, { x0, x1, z0, z1, hoehe: 6, boden: sandstein(), bodenKachel: 2, wand: sandstein(), hell: .45, sonne: .3, himmel: 0xffe0b0, hintergrund: 0x0a0604, leiste: 0x7a5a3a });
    const sst = std({ map: sandstein(), roughness: .9 });
    const schacht = new THREE.SpotLight(0xfff0d0, 60, 14, .35, .5, 1.2); schacht.position.set(1.8, 6, -2); schacht.target.position.set(1.8, 0, -2); scene.add(schacht, schacht.target);
    const strahl = new THREE.Mesh(new THREE.CylinderGeometry(.6, 1.2, 6, 24, 1, true), new THREE.MeshBasicMaterial({ color: 0xfff0d0, transparent: true, opacity: .08, depthWrite: false })); strahl.position.set(1.8, 3, -2); scene.add(strahl);
    const gold = std({ color: 0xe6b422, metalness: 1, roughness: .2 });
    const pokal = new THREE.Group(); pokal.name = 'pokal';
    at(new THREE.Mesh(new THREE.CylinderGeometry(.5, .6, 1.1, 8), sst), 0, .55, 0, pokal);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.22, .26, .08, 20), gold), 0, 1.14, 0, pokal);
    at(new THREE.Mesh(new THREE.CylinderGeometry(.04, .05, .35, 12), gold), 0, 1.35, 0, pokal);
    at(new THREE.Mesh(new THREE.SphereGeometry(.24, 20, 12, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2), gold), 0, 1.75, 0, pokal).material.side = THREE.DoubleSide;
    for (const x of [-.27, .27]) { at(new THREE.Mesh(new THREE.TorusGeometry(.1, .025, 8, 16, Math.PI), gold), x, 1.7, 0, pokal).rotation.z = x > 0 ? -Math.PI / 2 : Math.PI / 2; }
    for (let i = 0; i < 5; i++) { at(new THREE.Mesh(new THREE.OctahedronGeometry(.035), std({ color: [0xc0392b, 0x2980b9, 0x27ae60][i % 3], metalness: .3, roughness: .1 })), Math.sin(i * 1.25) * .21, 1.65, Math.cos(i * 1.25) * .21, pokal); }
    pokal.position.set(-2, 0, -2); scene.add(pokal);
    const urne = new THREE.Group(); urne.name = 'urne';
    const tisch = std({ color: 0x8a6440, roughness: .7 });
    at(box(.9, .7, .6, tisch), 0, .35, 0, urne);
    const kiste = at(box(.6, .45, .45, std({ color: 0xc8a070, roughness: .8 })), 0, .93, 0, urne);
    at(box(.25, .02, .04, std({ color: 0x1a1a1a })), 0, 1.16, 0, urne);
    const stift = at(new THREE.Mesh(new THREE.CylinderGeometry(.012, .012, .16, 6), std({ color: 0xe8c040 })), .36, .75, .1, urne); stift.rotation.z = .3;
    at(new THREE.Mesh(new THREE.CylinderGeometry(.003, .003, .4, 4), std({ color: 0xeeeeee })), .33, .95, .1, urne);
    urne.position.set(1.8, 0, -2); scene.add(urne);
    const fackeln = [fackel(scene, -5.4, 2.4, -2, .3), fackel(scene, 5.4, 2.4, -2, -.3), fackel(scene, -3, 2.4, z0 + .15), fackel(scene, 3, 2.4, z0 + .15)];
    const frei = freiImRaum([x0 + .5, z0 + .8, x1 - .5, z1 - .1], [[-2.6, -2.6, -1.4, -1.4], [1.3, -2.4, 2.3, -1.6]]);
    return {
        scene, frei, hoehe: () => 0,
        kamera: (p) => ({ p: new THREE.Vector3(p.x * .2, 3.0, 8.0), t: new THREE.Vector3(0, 1.2, -1.5) }),
        spawn: { start: [0, 2.0, Math.PI], von_tempel: [0, 2.0, Math.PI] },
        objekte: { pokal, urne, kiste },
        tick(dt, t) { fackeln.forEach((f) => flackern(f, t)); }
    };
}
