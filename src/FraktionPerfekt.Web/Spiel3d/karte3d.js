// Joe & Chani 3D – Reisekarte und Titelbild: Greven als Stadtmodell (1:1000) auf einem Kartentisch.
// Daten: karte3d.json (OpenStreetMap-Auszug, © OpenStreetMap-Mitwirkende, ODbL; erzeugt von tools/Grafik/karte3d.mjs).
import * as THREE from './three_min.js';
import { mergeGeometries } from './buffergeometry.js';

const M = 1 / 1000;          // Maßstab: 1 m in Greven = 1 mm auf dem Tisch
const HOCH = 2;              // Gebäude überhöht, damit das Modell plastisch wirkt
const TISCH = .78;           // Höhe der Tischplatte
const RAND = .2;             // Pergamentrand um die Karte (Szeneneinheiten)

function canvasTex(w, h, malen) {
    const c = document.createElement('canvas'); c.width = w; c.height = h;
    malen(c.getContext('2d'), w, h);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8;
    return t;
}
let saat = 4711;
const zufall = () => (saat = (saat * 16807) % 2147483647) / 2147483647;

/** Baut die Szene. Liefert Szene, Kamera und Funktionen für Bild, Treffer und Bildschirmposition. */
export async function baueKarte(renderer) {
    const d = await (await fetch(new URL('karte3d.json', import.meta.url).href)).json();
    const [hx, hy] = d.halb, W = 2 * hx * M, D = 2 * hy * M;
    const ort = (x, y) => new THREE.Vector3(x * M, TISCH, -y * M);

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x140d08);
    scene.fog = new THREE.Fog(0x140d08, 6, 14);
    scene.add(new THREE.HemisphereLight(0xffe2b8, 0x2a1a10, .9));
    const lampe = new THREE.DirectionalLight(0xffe6c0, 2.4);
    lampe.position.set(-1.2, TISCH + 3.2, 1.4); lampe.target.position.set(0, TISCH, 0);
    lampe.castShadow = true; lampe.shadow.mapSize.set(2048, 2048); lampe.shadow.bias = -.0003; lampe.shadow.normalBias = .002;
    Object.assign(lampe.shadow.camera, { left: -1.8, right: 1.8, top: 1.8, bottom: -1.8, near: .5, far: 8 });
    scene.add(lampe, lampe.target);
    const licht = new THREE.PointLight(0xffc27a, 3, 4, 1.6); licht.position.set(.6, TISCH + 1.3, -.4); scene.add(licht);

    // ---------- Raum und Tisch
    const holz = canvasTex(512, 512, (g, w, h) => {
        g.fillStyle = '#5a3a22'; g.fillRect(0, 0, w, h);
        for (let i = 0; i < 160; i++) { g.strokeStyle = `rgba(${30 + zufall() * 40},${15 + zufall() * 20},5,${.15 + zufall() * .25})`; g.lineWidth = 1 + zufall() * 3; const y = zufall() * h; g.beginPath(); g.moveTo(0, y); g.bezierCurveTo(w * .3, y + zufall() * 20 - 10, w * .6, y + zufall() * 20 - 10, w, y + zufall() * 10 - 5); g.stroke(); }
    });
    holz.wrapS = holz.wrapT = THREE.RepeatWrapping; holz.repeat.set(2, 2);
    const holzMat = new THREE.MeshStandardMaterial({ map: holz, roughness: .55 });
    const platte = new THREE.Mesh(new THREE.BoxGeometry(W + 2 * RAND + .5, .06, D + 2 * RAND + .5), holzMat);
    platte.position.y = TISCH - .03; platte.receiveShadow = true; platte.castShadow = true; scene.add(platte);
    for (const sx of [-1, 1]) {
        for (const sz of [-1, 1]) {
            const bein = new THREE.Mesh(new THREE.BoxGeometry(.09, TISCH - .06, .09), holzMat);
            bein.position.set(sx * (W / 2 + RAND + .1), (TISCH - .06) / 2, sz * (D / 2 + RAND + .1)); bein.castShadow = true; scene.add(bein);
        }
    }
    const boden = new THREE.Mesh(new THREE.PlaneGeometry(30, 30), new THREE.MeshStandardMaterial({ color: 0x2a1c12, roughness: .9 }));
    boden.rotation.x = -Math.PI / 2; boden.receiveShadow = true; scene.add(boden);

    // ---------- Kartenblatt (Pergament mit Rand, Titel, Kompass, Maßstab, Namensnennung)
    const KB = 2560, kh = Math.round(KB * (D + 2 * RAND) / (W + 2 * RAND));
    const proM = (KB * W / (W + 2 * RAND)) / (2 * hx), rx = KB * RAND / (W + 2 * RAND);
    const px = (x) => rx + (x + hx) * proM, py = (y) => rx + (hy - y) * proM;
    const blatt = canvasTex(KB, kh, (g, w, h) => {
        // Pergament
        const gr = g.createRadialGradient(w / 2, h / 2, w * .2, w / 2, h / 2, w * .75); gr.addColorStop(0, '#efdfb8'); gr.addColorStop(1, '#c9a874');
        g.fillStyle = gr; g.fillRect(0, 0, w, h);
        for (let i = 0; i < 900; i++) { g.fillStyle = `rgba(120,80,30,${zufall() * .05})`; g.beginPath(); g.arc(zufall() * w, zufall() * h, 2 + zufall() * 30, 0, 7); g.fill(); }
        // Kartenfläche
        g.save(); g.beginPath(); g.rect(rx, rx, 2 * hx * proM, 2 * hy * proM); g.clip();
        g.fillStyle = 'rgba(246,238,214,.92)'; g.fillRect(rx, rx, 2 * hx * proM, 2 * hy * proM);
        const flaeche = (p, farbe) => { g.fillStyle = farbe; g.beginPath(); for (let i = 0; i < p.length; i += 2) { g.lineTo(px(p[i]), py(p[i + 1])); } g.closePath(); g.fill(); };
        const linie = (p, farbe, breite) => { g.strokeStyle = farbe; g.lineWidth = breite; g.lineJoin = g.lineCap = 'round'; g.beginPath(); for (let i = 0; i < p.length; i += 2) { g.lineTo(px(p[i]), py(p[i + 1])); } g.stroke(); };
        for (const p of d.gruen) { flaeche(p, '#bcd18e'); }
        for (const p of d.wasser) { flaeche(p, '#8db7d4'); }
        for (const p of d.fluss) { linie(p, '#7aa6c9', 44 * proM); linie(p, '#97c0dd', 30 * proM); }
        for (const p of d.weg) { linie(p, '#cdb48a', 2.2 * proM); }
        for (const p of d.strasse) { linie(p, '#a88d66', 13 * proM); }
        for (const p of d.strasse) { linie(p, '#fffaf0', 9 * proM); }
        for (const p of d.gebaeude) { flaeche(p, '#b59a72'); }
        g.restore();
        g.strokeStyle = '#6b4a2a'; g.lineWidth = 6; g.strokeRect(rx - 3, rx - 3, 2 * hx * proM + 6, 2 * hy * proM + 6);
        // Beschriftung auf dem Rand
        g.fillStyle = '#4a2e18'; g.textBaseline = 'middle';
        g.font = 'bold 120px Georgia, "Times New Roman", serif'; g.fillText('GREVEN', rx, rx / 2);
        g.font = 'italic 46px Georgia, serif'; g.textAlign = 'right'; g.fillText('an der Ems', w - rx, rx / 2 + 6);
        g.font = '30px Georgia, serif'; g.textAlign = 'left';
        g.fillText('Kartendaten © OpenStreetMap-Mitwirkende (ODbL)', rx, h - rx / 2);
        // Maßstab 500 m
        const m500 = 500 * proM, mx = w - rx - m500, my = h - rx / 2;
        g.fillRect(mx, my - 6, m500, 12); g.fillStyle = '#efdfb8'; g.fillRect(mx + m500 / 2, my - 4, m500 / 2 - 2, 8);
        g.fillStyle = '#4a2e18'; g.textAlign = 'right'; g.fillText('500 m', mx - 16, my);
        // Kompass oben rechts in der Karte
        const kx = w - rx - 120, ky = rx + 140;
        g.save(); g.translate(kx, ky); g.globalAlpha = .85;
        for (let i = 0; i < 4; i++) { g.rotate(Math.PI / 2); g.fillStyle = i === 3 ? '#a3281c' : '#6b4a2a'; g.beginPath(); g.moveTo(0, -80); g.lineTo(16, 0); g.lineTo(-16, 0); g.fill(); }
        g.fillStyle = '#4a2e18'; g.textAlign = 'center'; g.font = 'bold 44px Georgia, serif'; g.fillText('N', 0, -112); g.restore();
    });
    const blattMesh = new THREE.Mesh(new THREE.PlaneGeometry(W + 2 * RAND, D + 2 * RAND), new THREE.MeshStandardMaterial({ map: blatt, roughness: .85 }));
    blattMesh.rotation.x = -Math.PI / 2; blattMesh.position.y = TISCH + .0008; blattMesh.receiveShadow = true; scene.add(blattMesh);

    // ---------- Gebäude als Klötze (Höhe geschätzt aus der Grundfläche, überhöht)
    {
        const teile = [], dach = new THREE.Color(), wand = new THREE.Color();
        for (const p of d.gebaeude) {
            const pts = []; for (let i = 0; i < p.length; i += 2) { pts.push(new THREE.Vector2(p[i] * M, p[i + 1] * M)); }
            if (pts.length > 3 && pts[0].distanceTo(pts[pts.length - 1]) < 1e-6) { pts.pop(); }
            let flaeche = 0; for (let i = 0; i < p.length - 2; i += 2) { flaeche += p[i] * p[i + 3] - p[i + 2] * p[i + 1]; }
            flaeche = Math.abs(flaeche) / 2;
            const hm = flaeche < 50 ? 3 : flaeche > 1500 ? 11 + zufall() * 4 : 6 + zufall() * 6;
            const geo = new THREE.ExtrudeGeometry(new THREE.Shape(pts), { depth: hm * HOCH * M, bevelEnabled: false });
            geo.rotateX(-Math.PI / 2); geo.deleteAttribute('uv');
            const n = geo.attributes.normal, farben = new Float32Array(n.count * 3), ton = .85 + zufall() * .25;
            dach.setRGB(.55 * ton, .26 * ton, .18 * ton); wand.setRGB(.86 * ton, .78 * ton, .64 * ton);
            for (let i = 0; i < n.count; i++) { const c = n.getY(i) > .5 ? dach : wand; farben[i * 3] = c.r; farben[i * 3 + 1] = c.g; farben[i * 3 + 2] = c.b; }
            geo.setAttribute('color', new THREE.BufferAttribute(farben, 3));
            teile.push(geo);
        }
        const haeuser = new THREE.Mesh(mergeGeometries(teile), new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .8 }));
        haeuser.position.y = TISCH + .001; haeuser.castShadow = haeuser.receiveShadow = true; scene.add(haeuser);
    }

    // ---------- Stationen: Stecknadeln mit Nummer und Namen
    const etikett = (text, gross) => {
        const c = document.createElement('canvas'), g = c.getContext('2d'), fs = gross ? 64 : 40;
        g.font = `bold ${fs}px Georgia, serif`; const tw = g.measureText(text).width;
        c.width = Math.ceil(tw + 40); c.height = fs + 30;
        g.fillStyle = 'rgba(30,18,10,.82)'; g.beginPath(); g.roundRect(0, 0, c.width, c.height, 14); g.fill();
        g.strokeStyle = '#c9a227'; g.lineWidth = 4; g.stroke();
        g.fillStyle = '#ffe08a'; g.font = `bold ${fs}px Georgia, serif`; g.textBaseline = 'middle'; g.fillText(text, 20, c.height / 2 + 2);
        const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
        const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: t, depthTest: false, transparent: true }));
        const hoehe = gross ? .06 : .045; s.scale.set(hoehe * c.width / c.height, hoehe, 1); s.renderOrder = 10;
        return s;
    };
    const nadeln = {}, treffbar = [];
    const nadelMetall = new THREE.MeshStandardMaterial({ color: 0xc8c8c8, metalness: .9, roughness: .25 });
    for (const st of d.stationen) {
        const g = new THREE.Group(); g.position.copy(ort(st.p[0], st.p[1])); scene.add(g);
        const stab = new THREE.Mesh(new THREE.CylinderGeometry(.0025, .0012, .1, 8), nadelMetall); stab.position.y = .05; stab.castShadow = true; g.add(stab);
        const farbe = 0xc62828;
        const kopfMat = new THREE.MeshStandardMaterial({ color: farbe, roughness: .3, metalness: .1, transparent: true });
        const kopf = new THREE.Mesh(new THREE.SphereGeometry(.018, 24, 16), kopfMat); kopf.position.y = .105; kopf.castShadow = true; g.add(kopf);
        const nummer = etikett(st.nr, false); nummer.position.y = .16; g.add(nummer);
        const name = etikett(st.name, true); name.position.y = .22; name.visible = false; g.add(name);
        const ziel = new THREE.Mesh(new THREE.SphereGeometry(.05, 8, 6), new THREE.MeshBasicMaterial({ visible: false })); ziel.position.y = .1; ziel.userData.id = st.id; g.add(ziel);
        treffbar.push(ziel);
        nadeln[st.id] = { g, kopf, kopfMat, farbe, nummer, name, ziel };
    }

    // ---------- Reise: rote Punktspur im Bogen und Joes Hut
    const PUNKTE = 48;
    const spur = new THREE.InstancedMesh(new THREE.SphereGeometry(.0055, 8, 6), new THREE.MeshStandardMaterial({ color: 0xbc0000, roughness: .4 }), PUNKTE);
    spur.count = 0; scene.add(spur);
    const hut = new THREE.Group();
    {
        const filz = new THREE.MeshStandardMaterial({ color: 0x5e4630, roughness: .85 });
        hut.add(new THREE.Mesh(new THREE.CylinderGeometry(.05, .05, .004, 32), filz));
        const krone = new THREE.Mesh(new THREE.CylinderGeometry(.026, .03, .035, 24), filz); krone.position.y = .018; hut.add(krone);
        const band = new THREE.Mesh(new THREE.CylinderGeometry(.0305, .0305, .007, 24), new THREE.MeshStandardMaterial({ color: 0x2a1d14 })); band.position.y = .005; hut.add(band);
        hut.traverse((o) => { if (o.isMesh) { o.castShadow = true; } });
    }
    hut.visible = false; scene.add(hut);
    const bogen = (a, b, u, out) => out.lerpVectors(a, b, u).setY(TISCH + .03 + Math.sin(u * Math.PI) * (.12 + a.distanceTo(b) * .15));
    const tmp = new THREE.Object3D(), va = new THREE.Vector3(), vb = new THREE.Vector3(), vp = new THREE.Vector3();

    // ---------- Kameras
    const kamera = new THREE.PerspectiveCamera(40, 16 / 9, .05, 40);
    // Kartenansicht auf die Stationen ausrichten (Mitte und Ausdehnung der Nadeln)
    const box = new THREE.Box3(); for (const n of Object.values(nadeln)) { box.expandByPoint(n.g.position); }
    const mitte = box.getCenter(new THREE.Vector3()), spanne = Math.max(box.max.x - box.min.x, box.max.z - box.min.z) + .35;
    const ray = new THREE.Raycaster(), ndc = new THREE.Vector2();
    let ansicht = 'karte';
    function kameraStellen(t, aspekt) {
        if (ansicht === 'titel') {
            const w = Math.sin(t * .07) * .32, dist = aspekt < 1.2 ? 4.6 : 3.4;
            kamera.position.set(Math.sin(w) * dist, TISCH + .95, Math.cos(w) * dist);
            kamera.lookAt(0, TISCH + .45, -.55);
        } else {
            const weit = spanne * (aspekt < 1.2 ? 1.9 : 1.15);
            kamera.position.set(mitte.x + Math.sin(t * .11) * .03, TISCH + 1.25 * weit, mitte.z + .95 * weit);
            kamera.lookAt(mitte.x, TISCH, mitte.z - .16);   // etwas nördlich: oben liegt oft das Textfeld
        }
    }

    return {
        scene, kamera, TISCH, tiefe: D,
        /** Kamera-Seitenverhältnis (bei Größenänderung). */
        groesse(aspekt) { kamera.aspect = aspekt; kamera.updateProjectionMatrix(); },
        /** Ein Bild der Reisekarte: orte = erreichbare Orte, hover = Ort unter dem Zeiger, reise = { von, nach, t }. */
        bild(t, { orte, hover, reise }) {
            ansicht = 'karte'; kameraStellen(t, kamera.aspect);
            for (const [id, n] of Object.entries(nadeln)) {
                const da = orte.includes(id), h = hover === id;
                n.kopfMat.color.set(h ? 0xffd54f : da ? n.farbe : 0xb8a888); n.kopfMat.opacity = da ? 1 : .55;
                const s = h ? 1.35 : 1; n.kopf.scale.setScalar(s);
                n.kopf.position.y = .105 + (da ? Math.sin(t * 2.4 + n.g.position.x * 9) * .004 : 0);
                n.nummer.visible = da; n.name.visible = h || (da && !hover);
                n.name.material.opacity = h ? 1 : .8;
            }
            if (reise && nadeln[reise.von] && nadeln[reise.nach]) {
                va.copy(nadeln[reise.von].g.position); vb.copy(nadeln[reise.nach].g.position);
                const u = Math.min(1, reise.t), bis = Math.floor(u * PUNKTE);
                for (let i = 0; i < bis; i++) { bogen(va, vb, i / PUNKTE, tmp.position); tmp.updateMatrix(); spur.setMatrixAt(i, tmp.matrix); }
                spur.count = bis; spur.instanceMatrix.needsUpdate = true;
                bogen(va, vb, u, vp); hut.position.copy(vp); hut.rotation.y = t * 3; hut.visible = true;
            } else { spur.count = 0; hut.visible = false; }
        },
        /** Ein Bild des Titels: langsame Kamerafahrt am Tisch entlang. */
        titelBild(t) { ansicht = 'titel'; kameraStellen(t, kamera.aspect); for (const n of Object.values(nadeln)) { n.name.visible = false; n.nummer.visible = true; n.kopfMat.opacity = 1; n.kopfMat.color.set(n.farbe); n.kopf.scale.setScalar(1); } spur.count = 0; hut.visible = false; },
        /** Ort unter einem Bildschirmpunkt (NDC −1…1), nur erreichbare Orte. */
        treffer(x, y, orte) {
            ndc.set(x, y); ray.setFromCamera(ndc, kamera);
            const h = ray.intersectObjects(treffbar.filter((o) => orte.includes(o.userData.id)), false)[0];
            return h ? h.object.userData.id : null;
        },
        /** Bildschirmposition (relativ 0…1) einer Nadel. */
        bildschirm(id) {
            const n = nadeln[id]; if (!n) { return null; }
            const p = n.kopf.getWorldPosition(new THREE.Vector3()).project(kamera);
            return { x: (p.x + 1) / 2, y: (1 - p.y) / 2 };
        },
        vorbereiten(cam) { renderer.compile(scene, cam || kamera); }
    };
}
