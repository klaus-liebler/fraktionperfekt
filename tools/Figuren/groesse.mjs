// Größenaufteilung der erzeugten Figuren (Texturen, Animationen, Mesh)
import fs from 'node:fs';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
for (const n of (process.argv.length > 2 ? process.argv.slice(2) : fs.readdirSync('../../src/FraktionPerfekt.Web/Spiel3d').filter((d) => /^figur_.*.glb$/.test(d)).map((d) => d.slice(6, -4)))) {
    const r = (await io.read(`../../src/FraktionPerfekt.Web/Spiel3d/figur_${n}.glb`)).getRoot();
    let t = 0; for (const x of r.listTextures()) { t += x.getImage().byteLength; console.log('  tex', x.getName(), (x.getImage().byteLength / 1024 | 0) + ' KB'); }
    for (const a of r.listAnimations()) { let s = 0; for (const x of a.listSamplers()) { s += x.getInput().getByteLength() + x.getOutput().getByteLength(); } console.log('  anim', a.getName(), (s / 1024 | 0) + ' KB', a.listSamplers()[0].getInput().getCount() + ' keys'); }
    let m = 0; for (const me of r.listMeshes()) { for (const p of me.listPrimitives()) { for (const a of p.listAttributes()) { m += a.getByteLength(); } m += p.getIndices()?.getByteLength() || 0; } }
    console.log(n, 'Texturen', t / 1024 | 0, 'KB, Mesh', m / 1024 | 0, 'KB');
}
