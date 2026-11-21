// Joe & Chani 3D – Figuren aus Microsoft Rocketbox (MIT, https://github.com/microsoft/Microsoft-Rocketbox) als glb.
// Aufruf: cd tools/Figuren && npm install && node baue.mjs
// Lädt die Quelldateien (FBX, TGA) in .cache/, wandelt mit three.js (FBXLoader, GLTFExporter) und gltf-transform um
// und schreibt src/FraktionPerfekt.Web/Spiel3d/figur_<name>.glb (Mesh, Skelett, Texturen, Animationen je Pose).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import * as THREE from 'three';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';
import { TGALoader } from 'three/examples/jsm/loaders/TGALoader.js';
import { GLTFExporter } from 'three/examples/jsm/exporters/GLTFExporter.js';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { dedup, prune, quantize, resample } from '@gltf-transform/functions';
import jpeg from 'jpeg-js';
import { PNG } from 'pngjs';

const HIER = path.dirname(fileURLToPath(import.meta.url));
const CACHE = path.join(HIER, '.cache');
const ZIEL = path.resolve(HIER, '../../src/FraktionPerfekt.Web/Spiel3d');
const QUELLE = 'https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/master/Assets';

/** Figuren: Rocketbox-Avatar, Texturkürzel, Geschlecht (Präfix der Animationen). */
const FIGUREN = {
    joe: { avatar: 'Adults/Male_Adult_03', tex: 'm004', g: 'm' },
    // Chani: walk_fast_02 wirkt steif und staksig; walk_fast_01 (1,5 m/s) hat einen natürlichen Armschwung
    chani: { avatar: 'Adults/Female_Adult_07', tex: 'f007', g: 'f', clips: { gehen: ['xy', 'walk_fast_01'] } }
};
/** Pose im Spiel → Rocketbox-Animation (static = auf der Stelle, xy = Bewegung herausgerechnet). */
const CLIPS = {
    stehen: ['static', 'idle_neutral_01'],
    gehen: ['xy', 'walk_fast_02'],      // 2,0 m/s (m) bzw. 1,9 m/s (f); das Spiel läuft 2,3 m/s
    rennen: ['xy', 'run_neutral_01'],
    reden: ['static', 'gestic_talk_neutral_01'],
    benutzen: ['static', 'work_mid'],
    erschrocken: ['static', 'idle_nervous_01'],
    sitzen: ['static', 'sit_chair_idle_neutral_01'],
    arme: ['static', 'idle_waiting_01']
};
const ORDNER = { static: 'all_animations_max_motextr_static', xy: 'all_animations_max_motextr_xy' };
// Nur Körperknochen animieren (Gesicht bleibt in Ruhe; den Kiefer bewegt das Spiel beim Reden selbst)
const KOERPER = /^Bip01(_Pelvis|_Spine\d?|_Neck|_Head|_[LR]_(Thigh|Calf|Foot|Toe0|Clavicle|UpperArm|Forearm|Hand|Finger\d+))?$/;

// ---------- Hilfen
async function holen(rel, datei) {
    const p = path.join(CACHE, datei);
    if (!fs.existsSync(p)) {
        const r = await fetch(`${QUELLE}/${rel}`);
        if (!r.ok) { throw new Error(`${rel}: HTTP ${r.status}`); }
        fs.mkdirSync(CACHE, { recursive: true });
        fs.writeFileSync(p, Buffer.from(await r.arrayBuffer()));
        console.log('geladen', rel);
    }
    const b = fs.readFileSync(p);
    return b.buffer.slice(b.byteOffset, b.byteOffset + b.byteLength);
}
// Texturen kommen getrennt dazu; FBXLoader soll nur die Namen kennen
THREE.TextureLoader.prototype.load = function (url) { const t = new THREE.Texture(); t.name = url; return t; };
const fbx = (buf) => { const warn = console.warn; console.warn = () => { }; try { return new FBXLoader().parse(buf, ''); } finally { console.warn = warn; } };

/** TGA → RGBA, halbe Kantenlänge, vertikal gespiegelt (glTF: Ursprung oben links, FBX-UVs unverändert). */
function tga(buf, halbieren = 1) {
    let { width: w, height: h, data } = new TGALoader().parse(buf);
    for (let k = 1; k < halbieren; k++) { ({ width: w, height: h, data } = verkleinern(w, h, data)); }
    return verkleinern(w, h, data, true);
}
function verkleinern(w, h, data, spiegeln = false) {
    const W = w / 2, Hh = h / 2, out = new Uint8Array(W * Hh * 4);
    for (let y = 0; y < Hh; y++) {
        for (let x = 0; x < W; x++) {
            for (let c = 0; c < 4; c++) {
                const s = data[((2 * y) * w + 2 * x) * 4 + c] + data[((2 * y) * w + 2 * x + 1) * 4 + c] + data[((2 * y + 1) * w + 2 * x) * 4 + c] + data[((2 * y + 1) * w + 2 * x + 1) * 4 + c];
                out[((spiegeln ? Hh - 1 - y : y) * W + x) * 4 + c] = s / 4;
            }
        }
    }
    return { width: W, height: Hh, data: out };
}
const alsJpeg = (img, q) => Buffer.from(jpeg.encode({ width: img.width, height: img.height, data: img.data }, q).data);
function alsPng(img) { const p = new PNG({ width: img.width, height: img.height }); p.data = Buffer.from(img.data); return PNG.sync.write(p); }

// GLTFExporter braucht FileReader (gibt es in Node nicht)
globalThis.FileReader = class {
    readAsArrayBuffer(b) { b.arrayBuffer().then((r) => { this.result = r; this.onloadend?.(); }); }
    readAsDataURL(b) { b.arrayBuffer().then((r) => { this.result = 'data:application/octet-stream;base64,' + Buffer.from(r).toString('base64'); this.onloadend?.(); }); }
};

/** Animation säubern: nur Körperknochen, nur Rotationen, Wurzel ohne Vorwärtsbewegung. */
function clip(roh, name, knochen, ord) {
    const tracks = [];
    for (const t of roh.tracks) {
        const [bone, prop] = t.name.split('.');
        if (!knochen.has(bone) || !KOERPER.test(bone)) { continue; }
        if (prop === 'quaternion') { tracks.push(t); }
        else if (prop === 'position' && bone === 'Bip01') {
            // Stand-Animationen: auf den Startpunkt beziehen (Wiegen bleibt); Gehen/Rennen: waagerecht ganz auf der Stelle
            const v = t.values.slice(), x0 = v[0], z0 = v[2];
            for (let i = 0; i < v.length; i += 3) { v[i] = ord === 'xy' ? 0 : v[i] - x0; v[i + 2] = ord === 'xy' ? 0 : v[i + 2] - z0; }
            tracks.push(new THREE.VectorKeyframeTrack(t.name, t.times, v));
        }
    }
    const c = new THREE.AnimationClip(name, roh.duration, tracks);
    c.optimize();
    return c;
}

async function figur(name, f) {
    console.log('==', name);
    const avatar = fbx(await holen(`Avatars/${f.avatar}/Export/${path.basename(f.avatar)}.fbx`, `${path.basename(f.avatar)}.fbx`));
    const knochen = new Set();
    let haut = null;
    avatar.traverse((o) => { if (o.isBone) { knochen.add(o.name); } if (o.isSkinnedMesh) { haut = o; } });
    // Materialien: PBR statt Phong, Texturen folgen in gltf-transform (nach Namen)
    haut.material = [].concat(haut.material).map((m) => {
        const art = m.name.replace(/^.*_/, '');   // body | head | opacity
        const n = new THREE.MeshStandardMaterial({ name: art, roughness: art === 'opacity' ? .9 : .78, metalness: 0 });
        if (art === 'opacity') { n.alphaTest = .4; n.side = THREE.DoubleSide; }
        return n;
    });
    const animationen = [];
    for (const [pose, standard] of Object.entries(CLIPS)) {
        const [ord, anim] = (f.clips && f.clips[pose]) || standard;
        const roh = fbx(await holen(`Animations/${ORDNER[ord]}/${f.g}_${anim}.max.fbx`, `${f.g}_${anim}_${ord}.fbx`)).animations[0];
        animationen.push(clip(roh, pose, knochen, ord));
    }
    const glb = await new GLTFExporter().parseAsync(avatar, { binary: true, animations: animationen, onlyVisible: false });

    // ---------- gltf-transform: Texturen, Alpha, Kompression
    const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
    const doc = await io.readBinary(new Uint8Array(glb));
    const tex = async (datei, art, q, halbieren = 1) => {
        const img = tga(await holen(`Avatars/${f.avatar}/Textures/${datei}`, datei), halbieren);
        const t = doc.createTexture(datei.replace('.tga', ''));
        if (art === 'png') { t.setImage(alsPng(img)).setMimeType('image/png'); } else { t.setImage(alsJpeg(img, q)).setMimeType('image/jpeg'); }
        return t;
    };
    for (const m of doc.getRoot().listMaterials()) {
        const art = m.getName();
        if (art === 'opacity') {
            m.setBaseColorTexture(await tex(`${f.tex}_opacity_color.tga`, 'png', 0, 2)).setAlphaMode('MASK').setAlphaCutoff(.4).setDoubleSided(true);
        } else {
            m.setBaseColorTexture(await tex(`${f.tex}_${art}_color.tga`, 'jpg', 84));
            m.setNormalTexture(await tex(`${f.tex}_${art}_normal.tga`, 'jpg', 80));
        }
    }
    await doc.transform(resample({ tolerance: 1.5e-3 }), prune(), dedup(), quantize({ quantizeNormal: 10, quantizeTexcoord: 12, quantizePosition: 14 }));
    // Rotationen der Animationen als normierte 16-Bit-Werte (glTF 2.0 erlaubt SHORT normalized für rotation)
    for (const a of doc.getRoot().listAnimations()) {
        for (const ch of a.listChannels()) {
            if (ch.getTargetPath() !== 'rotation') { continue; }
            const acc = ch.getSampler().getOutput(), v = acc.getArray();
            if (!(v instanceof Float32Array)) { continue; }
            const q = new Int16Array(v.length);
            for (let i = 0; i < v.length; i++) { q[i] = Math.round(Math.max(-1, Math.min(1, v[i])) * 32767); }
            acc.setArray(q).setNormalized(true);
        }
    }
    await doc.transform(dedup(), prune());
    const datei = path.join(ZIEL, `figur_${name}.glb`);
    await io.write(datei, doc);
    const anim = doc.getRoot().listAnimations().map((a) => `${a.getName()} ${a.listChannels().length}`).join(', ');
    console.log(`${datei}: ${(fs.statSync(datei).size / 1048576).toFixed(2)} MB; Animationen: ${anim}`);
}

for (const [name, f] of Object.entries(FIGUREN)) { await figur(name, f); }
