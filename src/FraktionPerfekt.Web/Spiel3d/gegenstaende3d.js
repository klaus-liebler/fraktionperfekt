// Joe & Chani 3D – Inventar-Icons: kleine 3D-Modelle der Gegenstände, einmal im Studiolicht fotografiert (statt der 16×16-Pixel-Icons).
// Ein eigener kleiner Renderer mit transparentem Hintergrund; jedes Icon wird nur einmal gerendert.
import * as THREE from './three_min.js';

const PX = 128;

const mat = (color, roughness = .7, metalness = 0, extra = {}) => new THREE.MeshStandardMaterial(Object.assign({ color, roughness, metalness }, extra));
function tex(w, h, draw) {
    const c = document.createElement('canvas'); c.width = w; c.height = h;
    draw(c.getContext('2d'), w, h);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4;
    return t;
}
function teil(geo, m, parent, x = 0, y = 0, z = 0) { const o = new THREE.Mesh(geo, m); o.position.set(x, y, z); parent.add(o); return o; }
/** Quader, dessen Oberseite (+y) eine eigene Textur trägt. */
function blatt(w, h, d, oben, seite) {
    const s = mat(seite, .85), o = mat(0xffffff, .85, 0, { map: oben });
    return new THREE.Mesh(new THREE.BoxGeometry(w, h, d), [s, s, o, s, s, s]);
}
const zeilen = (g, x0, y0, x1, n, abstand, farbe = 'rgba(40,40,40,.55)') => {
    g.fillStyle = farbe;
    for (let i = 0; i < n; i++) { const l = (x1 - x0) * (.6 + ((i * 37) % 10) / 25); g.fillRect(x0, y0 + i * abstand, Math.min(x1 - x0, l), 3); }
};

// ---------- Modelle (Größen in Metern, Ansicht von schräg oben) ----------
const MODELLE = {
    brief() {
        const g = new THREE.Group();
        const papier = tex(256, 160, (c, w, h) => {
            c.fillStyle = '#efe6d2'; c.fillRect(0, 0, w, h);
            c.strokeStyle = 'rgba(120,100,70,.5)'; c.lineWidth = 3; c.beginPath(); c.moveTo(0, 0); c.lineTo(w / 2, h * .62); c.lineTo(w, 0); c.stroke();
            c.beginPath(); c.moveTo(0, h); c.lineTo(w * .42, h * .5); c.moveTo(w, h); c.lineTo(w * .58, h * .5); c.stroke();
        });
        teil(new THREE.BoxGeometry(.22, .006, .14), [mat(0xe6dcc6), mat(0xe6dcc6), mat(0xffffff, .9, 0, { map: papier }), mat(0xe6dcc6), mat(0xe6dcc6), mat(0xe6dcc6)], g);
        teil(new THREE.CylinderGeometry(.018, .02, .008, 20), mat(0x9a1c1c, .45), g, 0, .006, .015);
        g.rotation.y = -.25; g.rotation.x = .6;   // flach liegend, zur Kamera gekippt
        return g;
    },
    stadtplan() {
        const g = new THREE.Group();
        const plan = tex(256, 256, (c, w, h) => {
            c.fillStyle = '#e8e4da'; c.fillRect(0, 0, w, h);
            c.strokeStyle = '#555'; c.lineWidth = 4;
            for (let i = 0; i < 7; i++) { c.beginPath(); c.moveTo(0, 20 + i * 36 + (i % 2) * 12); c.lineTo(w, 30 + i * 34); c.stroke(); }
            for (let i = 0; i < 6; i++) { c.beginPath(); c.moveTo(18 + i * 44, 0); c.lineTo(30 + i * 42, h); c.stroke(); }
            c.strokeStyle = '#3a6ea8'; c.lineWidth = 12; c.beginPath(); c.moveTo(0, 200); c.bezierCurveTo(80, 150, 150, 240, 256, 170); c.stroke();
            c.fillStyle = 'rgba(0,0,0,.18)'; for (let i = 0; i < 400; i++) { c.fillRect((i * 97) % w, (i * 61) % h, 2, 2); }
            c.fillStyle = '#222'; c.font = 'bold 22px sans-serif'; c.fillText('GREVEN 1949', 60, 120);
        });
        // Zwei leicht geknickte Hälften eines gefalteten Plans
        for (const s of [-1, 1]) {
            const t = plan.clone(); t.needsUpdate = true; t.repeat.set(.5, 1); t.offset.set(s < 0 ? 0 : .5, 0);
            const h = teil(new THREE.BoxGeometry(.11, .003, .2), [mat(0xddd8cc), mat(0xddd8cc), mat(0xffffff, .9, 0, { map: t }), mat(0xddd8cc), mat(0xddd8cc), mat(0xddd8cc)], g, s * .054, 0, 0);
            h.rotation.z = -s * .12;
        }
        g.rotation.y = .3; g.rotation.x = .6;   // flach liegend, zur Kamera gekippt
        return g;
    },
    spd_antrag() {
        const g = new THREE.Group();
        const seite = tex(200, 280, (c, w, h) => {
            c.fillStyle = '#f7f5ef'; c.fillRect(0, 0, w, h);
            c.fillStyle = '#e3000f'; c.fillRect(0, 0, w, 46);
            c.fillStyle = '#fff'; c.font = 'bold 30px sans-serif'; c.textAlign = 'center'; c.fillText('SPD', w / 2, 34);
            c.fillStyle = '#222'; c.font = 'bold 17px sans-serif'; c.fillText('ANTRAG', w / 2, 76);
            zeilen(c, 20, 96, w - 20, 9, 16);
            c.strokeStyle = '#1a3a8a'; c.lineWidth = 3; c.beginPath(); c.moveTo(110, 258); c.bezierCurveTo(130, 240, 140, 270, 175, 250); c.stroke();
        });
        const b = blatt(.15, .003, .21, seite, 0xeeeae0); g.add(b);
        teil(new THREE.BoxGeometry(.15, .002, .21), mat(0xe9e5da), g, .01, -.004, .008).rotation.y = .08;
        g.rotation.y = -.2; g.rotation.x = .6;   // flach liegend, zur Kamera gekippt
        return g;
    },
    geschaeftsordnung() {
        const g = new THREE.Group();
        const deckel = tex(180, 260, (c, w, h) => {
            c.fillStyle = '#1f4e8c'; c.fillRect(0, 0, w, h);
            c.fillStyle = '#f2c230'; c.fillRect(0, 70, w, 8);
            c.fillStyle = '#fff'; c.font = 'bold 26px Georgia'; c.textAlign = 'center'; c.fillText('Geschäfts-', w / 2, 120); c.fillText('ordnung', w / 2, 152);
            c.font = '16px Georgia'; c.fillText('des Rates', w / 2, 186);
            c.fillStyle = 'rgba(255,255,255,.7)'; c.beginPath(); c.moveTo(w - 30, 0); c.lineTo(w, 0); c.lineTo(w, 30); c.fill();   // Eselsohr
        });
        const blau = mat(0x1f4e8c, .6), seiten = mat(0xf2eddf, .95);
        teil(new THREE.BoxGeometry(.12, .03, .18), [seiten, blau, mat(0xffffff, .6, 0, { map: deckel }), blau, seiten, seiten], g);
        g.rotation.y = .35; g.rotation.x = .6;   // flach liegend, zur Kamera gekippt
        return g;
    },
    taschenlampe() {
        const g = new THREE.Group();
        const gelb = mat(0xf2c230, .45), schwarz = mat(0x1e1e1e, .6);
        const k = new THREE.Group(); g.add(k);
        teil(new THREE.CylinderGeometry(.022, .022, .16, 24), gelb, k);
        teil(new THREE.CylinderGeometry(.034, .024, .05, 24), gelb, k, 0, .105, 0);
        teil(new THREE.CylinderGeometry(.034, .034, .006, 24), mat(0xfff6c8, .1, 0, { emissive: 0xffe9a0, emissiveIntensity: 1.2 }), k, 0, .133, 0);
        teil(new THREE.CylinderGeometry(.023, .023, .012, 24), schwarz, k, 0, -.083, 0);
        teil(new THREE.BoxGeometry(.012, .02, .008), schwarz, k, 0, .03, .022);
        k.rotation.z = -Math.PI / 2.4; k.rotation.y = .3;
        return g;
    },
    paddel() {
        const g = new THREE.Group();
        const holz = mat(0xb5824a, .6);
        // Kurzer Schaft mit Griff, breites Blatt (sonst wirkt das Icon wie ein Stock)
        teil(new THREE.CylinderGeometry(.014, .014, .42, 12), holz, g, 0, .19, 0);
        teil(new THREE.CylinderGeometry(.02, .02, .07, 12), mat(0x2a2a2a, .7), g, 0, .4, 0);
        const blattForm = new THREE.Shape(); blattForm.moveTo(-.025, 0); blattForm.quadraticCurveTo(-.12, -.1, -.1, -.32); blattForm.quadraticCurveTo(0, -.37, .1, -.32); blattForm.quadraticCurveTo(.12, -.1, .025, 0);
        teil(new THREE.ExtrudeGeometry(blattForm, { depth: .012, bevelEnabled: true, bevelSize: .005, bevelThickness: .005, bevelSegments: 2 }), mat(0xc0392b, .5), g, 0, .0, -.006);
        g.rotation.z = -.7; g.rotation.x = -.35;
        return g;
    },
    siegel_legislative() { return siegel('buch'); },
    siegel_exekutive() { return siegel('schluessel'); },
    siegel_judikative() { return siegel('waage'); },
    tempelschluessel() {
        const g = new THREE.Group();
        const eisen = mat(0x9a7a3a, .35, .9);
        teil(new THREE.TorusGeometry(.04, .011, 12, 32), eisen, g, -.1, 0, 0);
        teil(new THREE.CylinderGeometry(.009, .009, .17, 12), eisen, g, -.005, 0, 0).rotation.z = Math.PI / 2;
        teil(new THREE.BoxGeometry(.012, .04, .008), eisen, g, .06, -.022, 0);
        teil(new THREE.BoxGeometry(.012, .028, .008), eisen, g, .035, -.016, 0);
        teil(new THREE.BoxGeometry(.008, .046, .008), eisen, g, .078, -.025, 0);
        g.rotation.x = -.6; g.rotation.y = -.2;
        return g;
    },
    handy() {
        const g = new THREE.Group();
        const silber = mat(0x8c9096, .35, .7), dunkel = mat(0x2a2c30, .5, .3);
        const tasten = tex(96, 128, (c, w, h) => {
            c.fillStyle = '#2a2c30'; c.fillRect(0, 0, w, h);
            c.fillStyle = '#c8ccd2';
            for (let r = 0; r < 4; r++) { for (let s = 0; s < 3; s++) { c.fillRect(12 + s * 26, 34 + r * 22, 20, 14); } }
            c.fillRect(30, 8, 36, 16);
        });
        const display = tex(96, 128, (c, w, h) => {
            c.fillStyle = '#2a2c30'; c.fillRect(0, 0, w, h);
            c.fillStyle = '#8fd0a0'; c.fillRect(10, 22, w - 20, 70);
            c.fillStyle = '#1e3a26'; c.font = 'bold 13px sans-serif'; c.fillText('Relbeil', 18, 52); c.fillRect(18, 62, 40, 3);
        });
        // Aufgeklappt: Tastenteil liegt, Displayteil steht leicht nach hinten geneigt am Scharnier (z = 0)
        teil(new THREE.BoxGeometry(.05, .012, .09), [silber, silber, mat(0xffffff, .5, 0, { map: tasten }), silber, silber, silber], g, 0, 0, .045);
        const oben = new THREE.Group(); g.add(oben); oben.rotation.x = -.35;
        teil(new THREE.BoxGeometry(.05, .09, .01), [silber, silber, silber, silber, mat(0xffffff, .3, 0, { map: display, emissive: 0x335533, emissiveIntensity: .4 }), dunkel], oben, 0, .045, 0);
        g.rotation.y = .5;
        return g;
    }
};

/** Bronzesiegel mit erhabenem Bild (buch | schluessel | waage). */
function siegel(motiv) {
    const g = new THREE.Group();
    const bild = (hell) => tex(256, 256, (c, w) => {
        c.fillStyle = hell ? '#000' : '#a8742c'; c.fillRect(0, 0, w, w);
        c.fillStyle = hell ? '#fff' : '#5a3a12'; c.strokeStyle = c.fillStyle; c.lineWidth = 12; c.lineCap = 'round';
        c.beginPath(); c.arc(128, 128, 112, 0, 7); c.stroke();
        c.translate(128, 128); c.rotate(-Math.PI / 2); c.translate(-128, -128);   // Deckel-UV der Zylinderscheibe liegt um 90° gedreht
        if (motiv === 'buch') {
            // Aufgeschlagenes Buch: zwei Seiten mit Falz und Zeilen
            for (const r of [-1, 1]) {
                c.beginPath(); c.moveTo(128 + r * 6, 92); c.quadraticCurveTo(128 + r * 40, 74, 128 + r * 78, 90); c.lineTo(128 + r * 78, 176); c.quadraticCurveTo(128 + r * 40, 160, 128 + r * 6, 178); c.closePath(); c.fill();
            }
            c.fillStyle = hell ? '#000' : '#a8742c';
            for (let i = 0; i < 5; i++) { for (const r of [-1, 1]) { c.fillRect(r < 0 ? 66 : 146, 106 + i * 13, 44, 4); } }
        } else if (motiv === 'schluessel') {
            for (const [x, y, r] of [[96, 100, -.5], [150, 110, .4]]) { c.save(); c.translate(x, y); c.rotate(r); c.beginPath(); c.arc(0, -20, 18, 0, 7); c.stroke(); c.fillRect(-6, -4, 12, 80); c.fillRect(6, 52, 18, 10); c.fillRect(6, 66, 14, 10); c.restore(); }
        } else {
            c.fillRect(122, 62, 12, 130); c.fillRect(70, 186, 116, 12); c.fillRect(56, 76, 144, 10);
            for (const x of [70, 186]) { c.beginPath(); c.moveTo(x, 86); c.lineTo(x - 26, 136); c.moveTo(x, 86); c.lineTo(x + 26, 136); c.stroke(); c.beginPath(); c.arc(x, 136, 28, 0, Math.PI); c.fill(); }
        }
    });
    const bronze = mat(0xb07a30, .4, .85);
    const vorn = mat(0xffffff, .4, .85, { map: bild(false), bumpMap: bild(true), bumpScale: 3 });
    teil(new THREE.CylinderGeometry(.07, .072, .018, 48), [bronze, vorn, bronze], g);
    g.rotation.x = .55;
    return g;
}

export function gegenstandMaler() {
    let renderer = null, scene = null, kamera = null;
    const cache = new Map();
    function buehne() {
        if (renderer) { return; }
        const c = document.createElement('canvas'); c.width = c.height = PX;
        renderer = new THREE.WebGLRenderer({ canvas: c, antialias: true, alpha: true, preserveDrawingBuffer: true });
        renderer.setSize(PX, PX, false); renderer.setClearColor(0x000000, 0);
        renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.outputColorSpace = THREE.SRGBColorSpace;
        scene = new THREE.Scene();
        scene.add(new THREE.HemisphereLight(0xfff4e6, 0x3a2a1c, 1.6));
        const licht = new THREE.DirectionalLight(0xfff2e0, 2.6); licht.position.set(1.5, 3, 2); scene.add(licht);
        const rand = new THREE.DirectionalLight(0xffc890, 1.4); rand.position.set(-2, 1.5, -1.5); scene.add(rand);
        kamera = new THREE.PerspectiveCamera(25, 1, .01, 10);
    }
    function malen(gid) {
        const bau = MODELLE[gid];
        if (!bau) { return null; }
        buehne();
        const m = bau(); scene.add(m); m.updateMatrixWorld(true);
        // Bildfüllend einpassen: Kamera von vorn oben auf die Mitte der Hülle
        const kugel = new THREE.Box3().setFromObject(m).getBoundingSphere(new THREE.Sphere());
        const d = kugel.radius / Math.sin(THREE.MathUtils.degToRad(kamera.fov / 2)) * .92;
        const richtung = new THREE.Vector3(0, .75, 1).normalize();
        kamera.position.copy(kugel.center).addScaledVector(richtung, d); kamera.lookAt(kugel.center);
        renderer.render(scene, kamera);
        const bild = document.createElement('canvas'); bild.width = bild.height = PX;
        bild.getContext('2d').drawImage(renderer.domElement, 0, 0);
        scene.remove(m);
        m.traverse((o) => { if (o.isMesh) { o.geometry.dispose(); (Array.isArray(o.material) ? o.material : [o.material]).forEach((x) => { if (x.map) { x.map.dispose(); } if (x.bumpMap) { x.bumpMap.dispose(); } x.dispose(); }); } });
        return bild;
    }
    return {
        /** Icon als neues Canvas (darf in den DOM); null, wenn es kein Modell gibt. */
        bild(gid) {
            if (!cache.has(gid)) { cache.set(gid, malen(gid)); }
            const m = cache.get(gid);
            if (!m) { return null; }
            const c = document.createElement('canvas'); c.width = c.height = PX; c.getContext('2d').drawImage(m, 0, 0);
            return c;
        }
    };
}
