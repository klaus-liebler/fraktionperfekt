// Joe & Chani 3D – Porträts für Sprechblasen und Figurenknöpfe: Kopfbilder der 3D-Figuren.
// Ein kleiner eigener Renderer fotografiert den Kopf im Studiolicht; jedes Bild (Figur, Ausdruck, Hut) nur einmal.
import * as THREE from './three_min.js';
import * as FIG from './figuren3d.js';

const PX = 192;
// Sprecher-Kennung (darstellung3d.js) → Baufunktion der Nebenfigur
const NEBEN = { christian: FIG.chris, pinsel: FIG.pinsel, ledeiw: FIG.ledeiw, joanna: FIG.johanna, lore: FIG.lorette, kemper: FIG.kemper, relbeil: FIG.relbeil };

export function portraetMaler(figuren) {
    let renderer = null, scene = null, kamera = null;
    const cache = new Map(), neben = {};
    function buehne() {
        if (renderer) { return; }
        const c = document.createElement('canvas'); c.width = c.height = PX;
        renderer = new THREE.WebGLRenderer({ canvas: c, antialias: true, preserveDrawingBuffer: true });
        renderer.setSize(PX, PX, false); renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.outputColorSpace = THREE.SRGBColorSpace;
        scene = new THREE.Scene();
        const hg = document.createElement('canvas'); hg.width = 4; hg.height = 128;
        const g = hg.getContext('2d'), gr = g.createLinearGradient(0, 0, 0, 128); gr.addColorStop(0, '#5a3c26'); gr.addColorStop(1, '#1c120b');
        g.fillStyle = gr; g.fillRect(0, 0, 4, 128);
        scene.background = new THREE.CanvasTexture(hg); scene.background.colorSpace = THREE.SRGBColorSpace;
        scene.add(new THREE.HemisphereLight(0xfff0dc, 0x3a2a1c, 1.4));
        const licht = new THREE.DirectionalLight(0xfff2e0, 2.4); licht.position.set(1.2, 2.2, 2.2); scene.add(licht);
        const rand = new THREE.DirectionalLight(0xffc890, 1.6); rand.position.set(-1.5, 2, -1.5); scene.add(rand);
        kamera = new THREE.PerspectiveCamera(20, 1, .05, 10);
    }
    /** 3D-Figur zur Sprecher-Kennung; Nebenfiguren entstehen beim ersten Bedarf (ihr Modell lädt dann im Hintergrund). */
    function figur(id) {
        if (figuren[id]) { return figuren[id]; }
        if (!NEBEN[id]) { return null; }
        return neben[id] || (neben[id] = NEBEN[id]());
    }
    function malen(id, ausdruck, hut) {
        buehne();
        const f = figur(id);
        if (!f) { return null; }
        const u = f.userData;
        // Figur kurz in die Porträtbühne holen und danach zurückstellen
        const eltern = f.parent, pos = f.position.clone(), rot = f.rotation.clone();
        const hutVorher = u.hut ? u.hut.visible : null, helmVorher = u.helm ? u.helm.visible : null;
        scene.add(f); f.position.set(0, 0, 0); f.rotation.set(0, 0, 0);
        if (u.hut) { u.hut.visible = !!hut; }
        if (u.helm) { u.helm.visible = false; }
        // Joe und Chani behalten ihre Haltung aus dem Spiel; Porträt-Nebenfiguren stehen ruhig (sonst T-Pose des Modells)
        if (!u.rb || neben[id] === f) { FIG.pose(f, 1.3, 'stehen'); }
        const zurueck = FIG.gesicht(f, ausdruck);
        f.updateMatrixWorld(true);
        const kopf = u.head.getWorldPosition(new THREE.Vector3());
        const rb = !!u.rb, mitHut = !!(u.hut && u.hut.visible);
        const ziel = kopf.clone().add(new THREE.Vector3(0, rb ? (mitHut ? .12 : .085) : .17, rb ? .03 : .05));
        const abstand = rb ? (mitHut ? 1.25 : .95) : 1.45;
        kamera.position.set(ziel.x + abstand * .3, ziel.y + .03, ziel.z + abstand); kamera.lookAt(ziel);
        renderer.render(scene, kamera);
        const bild = document.createElement('canvas'); bild.width = bild.height = PX;
        bild.getContext('2d').drawImage(renderer.domElement, 0, 0);
        zurueck();
        if (u.hut) { u.hut.visible = hutVorher; }
        if (u.helm) { u.helm.visible = helmVorher; }
        scene.remove(f);
        if (eltern) { eltern.add(f); }
        f.position.copy(pos); f.rotation.copy(rot);
        return bild;
    }
    return {
        /** Alle Nebenfiguren schon anlegen: Ihre Modelle laden im Hintergrund, bevor sie das erste Mal sprechen. */
        vorbereiten() { for (const id of Object.keys(NEBEN)) { figur(id); } },
        /** Porträt als neues Canvas (darf in den DOM); null, wenn es für id keine 3D-Figur gibt. */
        bild(id, ausdruck = 'neutral', hut = false) {
            // Solange das Rocketbox-Modell noch lädt, gilt das Bild der einfachen Figur nur vorläufig
            const f = figur(id), rb = f && f.userData.rb ? 1 : 0;
            const schluessel = `${id}|${ausdruck}|${id === 'joe' && hut ? 1 : 0}|${rb}`;
            if (!cache.has(schluessel)) { cache.set(schluessel, malen(id, ausdruck, id === 'joe' && hut)); }
            const m = cache.get(schluessel);
            if (!m) { return null; }
            const c = document.createElement('canvas'); c.width = c.height = PX; c.getContext('2d').drawImage(m, 0, 0);
            return c;
        }
    };
}
