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

/** Nebenfiguren kommen und gehen zu Fuß, rennen aber nie (sitzen: Pinsel und Ledeiw im Motorboot). */
const NEBEN_POSEN = ['stehen', 'gehen', 'reden', 'benutzen', 'erschrocken', 'sitzen', 'arme'];
/**
 * Figuren: Rocketbox-Avatar, Texturkürzel, Geschlecht (Präfix der Animationen).
 * posen: nur diese Animationen (Nebenfiguren: ohne Rennen); halb: Körpertextur zusätzlich so oft halbieren (Nebenfiguren: kleinere Dateien);
 * umfaerben: Kleidung am Oberkörper (Rumpf, Arme; Maske aus den Skin-Gewichten) neu einfärben – wahl(r, g, b, lum, sat, hue)
 * liefert 0…1, ziel ist die Farbe bei Helligkeit ref (Falten und Muster bleiben über die Helligkeit erhalten).
 */
const FIGUREN = {
    joe: { avatar: 'Adults/Male_Adult_03', tex: 'm004', g: 'm' },
    // Chani: walk_fast_02 wirkt steif und staksig; walk_fast_01 (1,5 m/s) hat einen natürlichen Armschwung
    chani: { avatar: 'Adults/Female_Adult_07', tex: 'f007', g: 'f', clips: { gehen: ['xy', 'walk_fast_01'] } },
    // Chris: älterer Herr mit grauem Vollbart; die braune Arbeitsjacke wird zur roten Strickjacke (Brille im Spiel)
    chris: {
        avatar: 'Professions/Gardener_Male_01', tex: 'm106', g: 'm', posen: NEBEN_POSEN, halb: 2,
        umfaerben: [{ wahl: (r, g, b, l, s, h) => band(h, 10, 50, 8) * band(s, .15, .8, .06) * band(l, 15, 140, 10), ziel: [168, 42, 36], ref: 62 }]
    },
    // Pinsel: Anzug mit Krawatte; Ledeiw: schwarzer Anzug, rote Krawatte, gegeltes dunkles Haar (kleine Brille im Spiel)
    pinsel: { avatar: 'Professions/Business_Male_03', tex: 'm020', g: 'm', posen: NEBEN_POSEN, halb: 2 },
    ledeiw: { avatar: 'Professions/Business_Male_01', tex: 'm005', g: 'm', posen: NEBEN_POSEN, halb: 2 },
    // Johanna: dunkles Haar, die schwarze Kapuzenjacke wird violett
    johanna: {
        avatar: 'Adults/Female_Adult_12', tex: 'f012', g: 'f', posen: NEBEN_POSEN, halb: 2,
        umfaerben: [{ wahl: (r, g, b, l, s) => band(l, 0, 95, 15) * band(s, 0, .35, .1), ziel: [112, 52, 140], ref: 40 }]
    },
    // Lorette: blonder Zopf (Female_Adult_17 hat einen Dutt, der durch den Helm sticht); die rosa Bluse wird zur grünen
    // Regenjacke (Fahrradhelm und Gutachten im Spiel)
    lorette: {
        avatar: 'Adults/Female_Adult_01', tex: 'f001', g: 'f', posen: NEBEN_POSEN, halb: 2,
        umfaerben: [{ wahl: (r, g, b, l, s, h) => (h >= 290 || h <= 20 ? 1 : 0) * band(s, .12, 1, .08), ziel: [46, 160, 84], ref: 150 }]
    },
    // Frau Kemper: Bauhelm am Modell; die gelbe Warnweste wird orange
    kemper: {
        avatar: 'Professions/Construction_Female_01', tex: 'f101', g: 'f', posen: NEBEN_POSEN, halb: 2,
        umfaerben: [{ wahl: (r, g, b, l, s, h) => band(h, 40, 70, 6) * band(s, .35, 1, .1), ziel: [240, 120, 24], ref: 190 }]
    },
    // Professor Relbeil (nur fürs Porträt am Telefon): älterer Herr mit Halbglatze (Brille im Spiel); Male_Adult_13 hätte an
    // der rechten Schulter abstehende Dreiecke
    relbeil: { avatar: 'Adults/Male_Adult_14', tex: 'm012', g: 'm', posen: ['stehen'], halb: 2 }
};
/** Weiche Bandauswahl: 1 zwischen a und b, linear auf 0 im Abstand weich. */
function band(x, a, b, weich) { return Math.max(0, Math.min(1, (x - a + weich) / weich, (b + weich - x) / weich)); }
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
    const maske = f.umfaerben ? oberkoerper(haut) : null;
    const animationen = [];
    for (const [pose, standard] of Object.entries(CLIPS)) {
        if (f.posen && !f.posen.includes(pose)) { continue; }
        const [ord, anim] = (f.clips && f.clips[pose]) || standard;
        const roh = fbx(await holen(`Animations/${ORDNER[ord]}/${f.g}_${anim}.max.fbx`, `${f.g}_${anim}_${ord}.fbx`)).animations[0];
        animationen.push(clip(roh, pose, knochen, ord));
    }
    const glb = await new GLTFExporter().parseAsync(avatar, { binary: true, animations: animationen, onlyVisible: false });

    // ---------- gltf-transform: Texturen, Alpha, Kompression
    const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
    const doc = await io.readBinary(new Uint8Array(glb));
    const tex = async (datei, art, q, halbieren = 1, faerben = null) => {
        const img = tga(await holen(`Avatars/${f.avatar}/Textures/${datei}`, datei), halbieren);
        if (faerben) { faerben(img); }
        const t = doc.createTexture(datei.replace('.tga', ''));
        if (art === 'png') { t.setImage(alsPng(img)).setMimeType('image/png'); } else { t.setImage(alsJpeg(img, q)).setMimeType('image/jpeg'); }
        return t;
    };
    for (const m of doc.getRoot().listMaterials()) {
        const art = m.getName();
        if (art === 'opacity') {
            m.setBaseColorTexture(await tex(`${f.tex}_opacity_color.tga`, 'png', 0, 2)).setAlphaMode('MASK').setAlphaCutoff(.4).setDoubleSided(true);
        } else {
            const halb = art === 'body' ? (f.halb || 1) : 1;
            const faerben = art === 'body' && maske ? (img) => umfaerben(img, maske, f.umfaerben) : null;
            m.setBaseColorTexture(await tex(`${f.tex}_${art}_color.tga`, 'jpg', 84, halb, faerben));
            m.setNormalTexture(await tex(`${f.tex}_${art}_normal.tga`, 'jpg', 80, halb));
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

/**
 * Maske des Oberkörpers auf der Körpertextur (Ursprung oben links wie im glb): UV-Inseln des Materials „body“, deren Fläche
 * überwiegend an Rumpf, Hals oder Armen hängt (nicht an Becken, Beinen oder Händen). Ganze Inseln, damit der Saum einer
 * Jacke mitkommt, obwohl er am Becken hängt; dazu ein Rand, den Mipmaps an den Inselkanten mitlesen. (u, v) → 0/1.
 */
function oberkoerper(haut) {
    const N = 512, RAND = 10, geo = haut.geometry;
    const uv = geo.attributes.uv, si = geo.attributes.skinIndex, sw = geo.attributes.skinWeight, idx = geo.index;
    const knochen = haut.skeleton.bones.map((b) => /_(Spine\d?|Neck|[LR]_(Clavicle|UpperArm|Forearm))$/.test(b.name));
    const oben = (v) => { let s = 0; for (let k = 0; k < 4; k++) { if (knochen[si.getComponent(v, k)]) { s += sw.getComponent(v, k); } } return s; };
    const mats = [].concat(haut.material);
    const gruppen = geo.groups.length ? geo.groups : [{ start: 0, count: idx ? idx.count : uv.count, materialIndex: 0 }];
    // Dreiecke sammeln und über gemeinsame UV-Punkte zu Inseln verbinden
    const tris = [], eltern = new Map();
    const finde = (k) => { while (eltern.get(k) !== k) { eltern.set(k, eltern.get(eltern.get(k))); k = eltern.get(k); } return k; };
    const schluessel = (i) => `${Math.round(uv.getX(i) * 8192)},${Math.round(uv.getY(i) * 8192)}`;
    for (const gr of gruppen) {
        if (mats[gr.materialIndex].name !== 'body') { continue; }
        for (let t = gr.start; t < gr.start + gr.count; t += 3) {
            const v = [0, 1, 2].map((i) => (idx ? idx.getX(t + i) : t + i));
            const k = v.map(schluessel);
            for (const x of k) { if (!eltern.has(x)) { eltern.set(x, x); } }
            eltern.set(finde(k[1]), finde(k[0])); eltern.set(finde(k[2]), finde(k[0]));
            const p = v.map((i) => [(uv.getX(i) % 1 + 1) % 1 * N, (uv.getY(i) % 1 + 1) % 1 * N]);
            const flaeche = Math.abs((p[1][0] - p[0][0]) * (p[2][1] - p[0][1]) - (p[1][1] - p[0][1]) * (p[2][0] - p[0][0])) / 2;
            tris.push({ k: k[0], p, flaeche, oben: (oben(v[0]) + oben(v[1]) + oben(v[2])) / 3 });
        }
    }
    const insel = new Map();
    for (const t of tris) { const w = finde(t.k), s = insel.get(w) || { a: 0, o: 0 }; s.a += t.flaeche; s.o += t.flaeche * t.oben; insel.set(w, s); }
    const m = new Uint8Array(N * N);
    for (const t of tris) { const s = insel.get(finde(t.k)); if (s.o / (s.a || 1) >= .5) { dreieck(m, N, t.p); } }
    // Rand: Maske um RAND Pixel erweitern (Polsterung der Insel, die beim Filtern mit hineinspielt)
    let d = m;
    for (let r = 0; r < RAND; r++) {
        const n = d.slice();
        for (let y = 1; y < N - 1; y++) { for (let x = 1; x < N - 1; x++) { if (!d[y * N + x] && (d[y * N + x - 1] || d[y * N + x + 1] || d[(y - 1) * N + x] || d[(y + 1) * N + x])) { n[y * N + x] = 1; } } }
        d = n;
    }
    return (u, w) => d[Math.min(N - 1, w * N | 0) * N + Math.min(N - 1, u * N | 0)];
}
function dreieck(m, N, [a, b, c]) {
    const x0 = Math.max(0, Math.floor(Math.min(a[0], b[0], c[0])) - 1), x1 = Math.min(N - 1, Math.ceil(Math.max(a[0], b[0], c[0])) + 1);
    const y0 = Math.max(0, Math.floor(Math.min(a[1], b[1], c[1])) - 1), y1 = Math.min(N - 1, Math.ceil(Math.max(a[1], b[1], c[1])) + 1);
    const fl = Math.sign((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]));
    if (!fl) { return; }
    // Vorzeichenbehafteter Abstand zur Kante (Pixel, innen positiv); 1 Pixel Zugabe, damit an Nähten keine Ränder bleiben
    const abstand = (p, q, x, y) => fl * ((q[0] - p[0]) * (y - p[1]) - (q[1] - p[1]) * (x - p[0])) / (Math.hypot(q[0] - p[0], q[1] - p[1]) || 1);
    for (let y = y0; y <= y1; y++) {
        for (let x = x0; x <= x1; x++) {
            const px = x + .5, py = y + .5;
            if (abstand(a, b, px, py) >= -1 && abstand(b, c, px, py) >= -1 && abstand(c, a, px, py) >= -1) { m[y * N + x] = 1; }
        }
    }
}
/** Kleidung umfärben: Helligkeit bleibt (Falten, Strickmuster), Farbton und Sättigung kommen aus dem Ziel. */
function umfaerben(img, maske, regeln) {
    const { width: W, height: H, data: p } = img;
    for (let y = 0; y < H; y++) {
        for (let x = 0; x < W; x++) {
            if (!maske((x + .5) / W, (y + .5) / H)) { continue; }
            const i = (y * W + x) * 4, r = p[i], g = p[i + 1], b = p[i + 2];
            const mx = Math.max(r, g, b), mn = Math.min(r, g, b), l = .3 * r + .59 * g + .11 * b, s = mx ? (mx - mn) / mx : 0;
            let h = 0;
            if (mx > mn) { h = mx === r ? ((g - b) / (mx - mn)) % 6 : mx === g ? (b - r) / (mx - mn) + 2 : (r - g) / (mx - mn) + 4; h = (h * 60 + 360) % 360; }
            for (const regel of regeln) {
                const w = regel.wahl(r, g, b, l, s, h);
                if (w <= 0) { continue; }
                const k = l / regel.ref;
                for (let c = 0; c < 3; c++) { p[i + c] = Math.round(p[i + c] + (Math.min(255, regel.ziel[c] * k) - p[i + c]) * w); }
                break;
            }
        }
    }
}

// Aufruf mit Namen baut nur diese Figuren (node baue.mjs chris pinsel), sonst alle
const auswahl = process.argv.slice(2);
for (const [name, f] of Object.entries(FIGUREN)) { if (!auswahl.length || auswahl.includes(name)) { await figur(name, f); } }
