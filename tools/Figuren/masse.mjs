// Gesichtsmaße relativ zum Kopfknochen (Meter, Achsen der Figur: y oben, z vorn) für das Zubehör in figuren3d.js
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { getBounds } from '@gltf-transform/functions';
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const mul = (m, v) => [m[0] * v[0] + m[4] * v[1] + m[8] * v[2] + m[12], m[1] * v[0] + m[5] * v[1] + m[9] * v[2] + m[13], m[2] * v[0] + m[6] * v[1] + m[10] * v[2] + m[14]];
for (const n of ['joe', 'chani']) {
    const doc = await io.read(`../../src/FraktionPerfekt.Web/Spiel3d/figur_${n}.glb`), r = doc.getRoot();
    const pos = (name) => { const nd = r.listNodes().find((x) => x.getName() === name); return mul(nd.getWorldMatrix(), [0, 0, 0]).map((v) => v / 100); };
    const k = pos('Bip01_Head'), rel = (p) => p.map((v, i) => +(v - k[i]).toFixed(3));
    console.log(`== ${n}  Kopfknochen (m): ${k.map((v) => v.toFixed(3))}`);
    for (const b of ['Bip01_LEye', 'Bip01_REye', 'Bip01_MNose', 'Bip01_MUpperLip', 'Bip01_MMiddleEyebrow', 'Bip01_LOuterEyebrow', 'Bip01_MJaw', 'Bip01_LCheek']) { console.log(b.padEnd(22), rel(pos(b))); }
    // Kopfgeometrie: Vertices mit Material head (Ruhehaltung), Ausdehnung relativ zum Kopfknochen
    const skin = r.listNodes().find((x) => x.getSkin()), m = skin.getWorldMatrix();
    for (const prim of skin.getMesh().listPrimitives()) {
        if (prim.getMaterial().getName() !== 'head') { continue; }
        const a = prim.getAttribute('POSITION'), lo = [1e9, 1e9, 1e9], hi = [-1e9, -1e9, -1e9], el = [];
        let breiteAugen = 0;
        for (let i = 0; i < a.getCount(); i++) {
            const p = rel(mul(m, a.getElement(i, el)).map((v) => v / 100));
            for (let j = 0; j < 3; j++) { lo[j] = Math.min(lo[j], p[j]); hi[j] = Math.max(hi[j], p[j]); }
        }
        console.log('Kopf min', lo, 'max', hi);
        // halbe Kopfbreite in Augenhöhe (Schläfen) und Hinterkopf
        const ey = rel(pos('Bip01_LEye'))[1]; let sx = 0, hz = 0, fz = -1;
        for (let i = 0; i < a.getCount(); i++) { const p = rel(mul(m, a.getElement(i, el)).map((v) => v / 100)); if (Math.abs(p[1] - ey) < .01) { sx = Math.max(sx, Math.abs(p[0])); fz = Math.max(fz, p[2]); } }
        console.log('Augenhöhe: halbe Breite', sx.toFixed(3), 'vorderste z', fz.toFixed(3));
    }
}
