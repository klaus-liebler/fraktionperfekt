// Erzeugt aus dem 3D-Prototyp (rathaus.html) mit gen_welt.py – Außenschauplatz (Gebäude: LoD2, Geobasis NRW, dl-de/zero-2-0).
import * as THREE from './three_min.js';
import { mergeGeometries } from './buffergeometry.js';
import { RoomEnvironment } from './roomenv.js';

export function baue(renderer, DATEN) {
  let __bl = [];
  // ---------- deterministic random
  let seed = 1234;
  const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const rr = (a, b) => a + (b - a) * rnd();
  
  // ---------- renderer / scene
  
  const scene = new THREE.Scene();
  // ---------- terrain (heights interpolated from the LoD2 terrain intersections, metres relative to the main building)
  const HF = DATEN.hf;
  function H(e, n) {
    let gx = (e - HF.x0) / HF.s, gy = (n - HF.y0) / HF.s;
    gx = Math.max(0, Math.min(HF.nx - 1.001, gx)); gy = Math.max(0, Math.min(HF.ny - 1.001, gy));
    const i = Math.floor(gx), j = Math.floor(gy), fx = gx - i, fy = gy - j, d = HF.d, nx = HF.nx;
    const a = d[j * nx + i], b = d[j * nx + i + 1], c = d[(j + 1) * nx + i], e2 = d[(j + 1) * nx + i + 1];
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + e2 * fx) * fy;
  }
  function terrainMesh(mat, tile) {
    const W = (HF.nx - 1) * HF.s, D = (HF.ny - 1) * HF.s, seg = Math.min(HF.nx - 1, 200);
    const g = new THREE.PlaneGeometry(W, D, seg, seg); g.rotateX(-Math.PI / 2);
    const p = g.attributes.position, uv = g.attributes.uv, cx = HF.x0 + W / 2, cn = HF.y0 + D / 2;
    for (let i = 0; i < p.count; i++) {
      const e = p.getX(i) + cx, n = -p.getZ(i) + cn;
      p.setXYZ(i, e, H(e, n), -n); uv.setXY(i, e / tile, n / tile);
    }
    g.computeVertexNormals();
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true; m.name = 'ground'; scene.add(m);
    // wide apron beyond the measured area
    let lo = Infinity; for (const v of HF.d) lo = Math.min(lo, v);
    const ap = new THREE.Mesh(new THREE.PlaneGeometry(1200, 1200), mat); ap.rotation.x = -Math.PI / 2; ap.position.y = lo - .4; ap.receiveShadow = true; scene.add(ap);
    return m;
  }
  // lay a flat (subdivided) mesh onto the terrain
  function drape(m, off = .03) {
    m.updateMatrixWorld(true);
    const g = m.geometry.clone().applyMatrix4(m.matrixWorld), p = g.attributes.position;
    for (let i = 0; i < p.count; i++) p.setY(i, H(p.getX(i), -p.getZ(i)) + off);
    g.computeVertexNormals(); m.geometry = g; m.position.set(0, 0, 0); m.rotation.set(0, 0, 0); m.scale.set(1, 1, 1);
    return m;
  }
  const camVec = a => new THREE.Vector3(a[0], a[1] + H(a[0], -a[2]), a[2]);
  
  const pmrem = new THREE.PMREMGenerator(renderer);
  scene.environment = pmrem.fromScene(new RoomEnvironment(), .04).texture;
  scene.environmentIntensity = .45;
  
  // ---------- canvas texture helpers
  function canvasTex(w, h, draw, srgb = true) {
    const c = document.createElement('canvas'); c.width = w; c.height = h;
    draw(c.getContext('2d'), w, h);
    const t = new THREE.CanvasTexture(c);
    t.wrapS = t.wrapT = THREE.RepeatWrapping;
    t.anisotropy = 8;
    if (srgb) t.colorSpace = THREE.SRGBColorSpace;
    return t;
  }
  const jitter = (base, v) => {
    const k = 1 + (rnd() - .5) * v;
    return `rgb(${base.map(x => Math.max(0, Math.min(255, Math.round(x * k)))).join(',')})`;
  };
  
  // Brick wall (tile = 1.0 m wide x 0.6 m high)
  const brickTex = canvasTex(512, 312, (g, w, h) => {
    g.fillStyle = '#c99c86'; g.fillRect(0, 0, w, h);
    const bw = 512 / 4, bh = 312 / 8.5;
    for (let r = 0; r < 9; r++) for (let c = -1; c < 5; c++) {
      const x = c * bw + (r % 2 ? bw / 2 : 0), y = r * bh;
      g.fillStyle = jitter([168, 64, 44], .28);
      g.fillRect(x + 3, y + 3, bw - 6, bh - 6);
      g.fillStyle = 'rgba(0,0,0,.08)'; g.fillRect(x + 3, y + bh - 8, bw - 6, 5);
    }
  });
  // Paving: parquet pattern of clinker (tile 1.6 m)
  const paveTex = canvasTex(512, 512, (g, w, h) => {
    g.fillStyle = '#86544a'; g.fillRect(0, 0, w, h);
    const b = 64;
    for (let i = 0; i < 8; i++) for (let j = 0; j < 8; j++) {
      const x = i * b, y = j * b, hor = (i + j) % 2 === 0;
      for (let k = 0; k < 2; k++) {
        g.fillStyle = jitter([168, 94, 76], .2);
        if (hor) g.fillRect(x + 2, y + k * b / 2 + 2, b - 4, b / 2 - 4);
        else g.fillRect(x + k * b / 2 + 2, y + 2, b / 2 - 4, b - 4);
      }
    }
  });
  // Cobble stripe (tile 0.8 m)
  const cobTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#55524f'; g.fillRect(0, 0, w, h);
    for (let r = 0; r < 8; r++) for (let c = -1; c < 9; c++) {
      const x = c * 32 + (r % 2 ? 16 : 0), y = r * 32;
      g.fillStyle = jitter([132, 126, 120], .3);
      g.beginPath(); g.roundRect(x + 2, y + 2, 28, 28, 7); g.fill();
    }
  });
  // Green metal cladding (tile 0.5 m wide)
  const cladTex = canvasTex(128, 256, (g, w, h) => {
    g.fillStyle = '#9fc0a8'; g.fillRect(0, 0, w, h);
    g.fillStyle = '#86aa92'; g.fillRect(0, 0, 6, h);
    g.fillStyle = '#b4d2bb'; g.fillRect(6, 0, 3, h);
  });
  // Concrete
  const concTex = canvasTex(128, 128, (g, w, h) => {
    g.fillStyle = '#b5b0a6'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 600; i++) { g.fillStyle = `rgba(0,0,0,${rnd() * .08})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
  });
  
  // Sign texture
  const signTex = canvasTex(2048, 128, (g, w, h) => {
    g.fillStyle = '#f6f3ea'; g.fillRect(0, 0, w, h);
    const txt = 'MARTIN-LUTHER-GRUNDSCHULE';
    g.font = 'bold 92px "Arial Black", Arial, sans-serif';
    g.textBaseline = 'middle';
    const tw = g.measureText(txt).width;
    let x = (w - tw) / 2;
    for (const ch of txt) {
      const cw = g.measureText(ch).width;
      // mosaic letters: draw char, then mosaic overlay
      g.fillStyle = '#5a4a3a'; g.fillText(ch, x + 3, h / 2 + 5);
      g.fillStyle = ['#c84a32', '#d99a2a', '#7d6a50', '#4a7a9a', '#a35a3a'][Math.floor(rnd() * 5)];
      g.fillText(ch, x, h / 2 + 2);
      x += cw;
    }
    // mosaic grid
    g.globalCompositeOperation = 'source-atop';
    for (let yy = 0; yy < h; yy += 9) for (let xx = 0; xx < w; xx += 9) {
      g.fillStyle = `rgba(255,255,255,${rnd() * .25})`; g.fillRect(xx, yy, 8, 8);
    }
    g.globalCompositeOperation = 'source-over';
    g.strokeStyle = '#8b8b84'; g.lineWidth = 6; g.strokeRect(3, 3, w - 6, h - 6);
  });
  
  // Decorated school window (snowmen, stars)
  function decoTex(i) {
    return canvasTex(256, 256, (g, w, h) => {
      g.fillStyle = '#2e3236'; g.fillRect(0, 0, w, h);
      g.fillStyle = '#e9ecef'; g.fillRect(18, 18, w - 36, h - 36);
      // curtain folds
      for (let x = 22; x < w - 20; x += 16) { g.fillStyle = 'rgba(120,130,150,.12)'; g.fillRect(x, 20, 6, h - 40); }
      const drawSnowman = (x, y, s) => {
        g.fillStyle = '#fff'; g.strokeStyle = '#555'; g.lineWidth = 2;
        for (const [dy, r] of [[0, 20], [-30, 14], [-52, 10]]) { g.beginPath(); g.arc(x, y + dy * s, r * s, 0, 7); g.fill(); g.stroke(); }
        g.fillStyle = '#222'; g.fillRect(x - 9 * s, y - 70 * s, 18 * s, 10 * s); g.fillRect(x - 12 * s, y - 62 * s, 24 * s, 3 * s);
        g.fillStyle = '#e46a1e'; g.fillRect(x, y - 54 * s, 9 * s, 3 * s);
      };
      if (i === 3) { // the yellow chick in the photo
        g.fillStyle = '#f2c534'; g.beginPath(); g.arc(128, 160, 42, 0, 7); g.fill(); g.beginPath(); g.arc(128, 104, 28, 0, 7); g.fill();
        g.fillStyle = '#e07a1c'; g.beginPath(); g.moveTo(150, 104); g.lineTo(168, 110); g.lineTo(150, 114); g.fill();
        g.fillStyle = '#222'; g.beginPath(); g.arc(136, 98, 4, 0, 7); g.fill();
        g.fillStyle = '#4a90d9'; g.fillRect(60, 120, 30, 40);
      } else {
        const n = i === 2 ? 2 : 2 + (i % 2);
        for (let k = 0; k < n; k++) drawSnowman(70 + k * (120 / Math.max(1, n - 1)), 196, .9);
      }
      for (let k = 0; k < 7; k++) {
        g.fillStyle = k % 2 ? '#f2d34a' : '#9cc8f0';
        const x = 40 + rnd() * 176, y = 40 + rnd() * 50;
        g.beginPath();
        for (let a = 0; a < 10; a++) { const r = a % 2 ? 5 : 12, an = a * Math.PI / 5 - Math.PI / 2; g.lineTo(x + Math.cos(an) * r, y + Math.sin(an) * r); }
        g.fill();
      }
    });
  }
  
  // ---------- materials
  const M = {
    brick: new THREE.MeshStandardMaterial({ map: brickTex, roughness: .9 }),
    clad: new THREE.MeshStandardMaterial({ map: cladTex, roughness: .55, metalness: .25 }),
    cladTrim: new THREE.MeshStandardMaterial({ color: 0xb6d3bd, roughness: .5, metalness: .3 }),
    frame: new THREE.MeshStandardMaterial({ color: 0x9fc4ab, roughness: .5, metalness: .2 }),
    glass: new THREE.MeshStandardMaterial({ color: 0x4a5d68, roughness: .08, metalness: .6 }),
    dark: new THREE.MeshStandardMaterial({ color: 0x2b2d30, roughness: .7 }),
    pave: new THREE.MeshStandardMaterial({ map: paveTex, roughness: .95 }),
    cob: new THREE.MeshStandardMaterial({ map: cobTex, roughness: .95 }),
    conc: new THREE.MeshStandardMaterial({ map: concTex, roughness: .95 }),
    metal: new THREE.MeshStandardMaterial({ color: 0x9a9d9e, roughness: .4, metalness: .7 }),
    wood: new THREE.MeshStandardMaterial({ color: 0x7a5236, roughness: .8 }),
    bark: new THREE.MeshStandardMaterial({ color: 0x4f463f, roughness: 1 }),
    roofEave: new THREE.MeshStandardMaterial({ color: 0x5d6563, roughness: .7 }),
    snow: new THREE.MeshStandardMaterial({ color: 0xf4f6f8, roughness: .9 }),
    hall: new THREE.MeshStandardMaterial({ color: 0x46514f, emissive: 0xffc77a, emissiveIntensity: .18, roughness: .15, metalness: .5 }),
  };
  
  // world-size UVs for boxes so textures keep real scale
  function box(w, h, d, mat, tile = 1) {
    const g = new THREE.BoxGeometry(w, h, d);
    const uv = g.attributes.uv, dims = [[d, h], [d, h], [w, d], [w, d], [w, h], [w, h]];
    for (let f = 0; f < 6; f++) for (let v = 0; v < 4; v++) {
      const i = f * 4 + v;
      uv.setXY(i, uv.getX(i) * dims[f][0] / tile, uv.getY(i) * dims[f][1] / (tile * .6));
    }
    const m = new THREE.Mesh(g, mat); m.castShadow = m.receiveShadow = true; return m;
  }
  function plane(w, h, mat, tileU, tileV) {
    const g = new THREE.PlaneGeometry(w, h);
    if (tileU) { const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * w / tileU, uv.getY(i) * h / tileV); }
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true; return m;
  }
  const add = (o, x, y, z, parent = scene) => { o.position.set(x, y + (parent === scene ? H(x, -z) : 0), z); parent.add(o); return o; };
  
  // ---------- sky
  const skyTex = canvasTex(4, 256, (g, w, h) => {
    const gr = g.createLinearGradient(0, 0, 0, h);
    gr.addColorStop(0, '#c9d2d8'); gr.addColorStop(.55, '#e4e7e6'); gr.addColorStop(1, '#efe9df');
    g.fillStyle = gr; g.fillRect(0, 0, w, h);
  });
  scene.background = skyTex;
  scene.fog = new THREE.Fog(0xdfe2e1, 60, 180);
  
  // ---------- lights
  const hemi = new THREE.HemisphereLight(0xe8eef2, 0x8a6a58, 1.6);
  scene.add(hemi);
  const sun = new THREE.DirectionalLight(0xfff2de, 1.6);
  sun.position.set(30, 45, 40);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -70, right: 70, top: 70, bottom: -70, near: 1, far: 220 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = .03;
  scene.add(sun);
  
  // ---------- real-world helpers (E = east, N = north, metres from the Rathaus centre)
  const W3 = (e, n, y = 0) => new THREE.Vector3(e, y + H(e, n), -n);
  // local frame of the council-hall wing along Rathausstraße
  const HO = [-18.3, -16.8], HA = [.977, .212], HN = [-.212, .977];
  const SD = (s, d) => [HO[0] + HA[0] * s + HN[0] * d, HO[1] + HA[1] * s + HN[1] * d];
  const SD3 = (s, d, y = 0) => { const [e, n] = SD(s, d); return W3(e, n, y); };
  const toSD = (e, n) => { const x = e - HO[0], y = n - HO[1]; return [x * HA[0] + y * HA[1], x * HN[0] + y * HN[1]]; };
  const HALL_ROT = Math.atan2(HA[1], HA[0]);   // rotation.y for objects aligned with the hall axis
  
  // ---------- extra textures
  const ribTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#d3cdc1'; g.fillRect(0, 0, w, h);
    for (let x = 0; x < w; x += 16) { g.fillStyle = '#b9b2a5'; g.fillRect(x, 0, 4, h); g.fillStyle = '#e2ddd3'; g.fillRect(x + 4, 0, 2, h); }
    for (let i = 0; i < 900; i++) { g.fillStyle = `rgba(60,55,45,${rnd() * .07})`; g.fillRect(rnd() * w, rnd() * h, 2, 3); }
  });
  const asphaltTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#4d4e50'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 4000; i++) { const v = 60 + rnd() * 40; g.fillStyle = `rgb(${v},${v},${v + 2})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
  });
  const grassTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#6b7a3c'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 5000; i++) { g.fillStyle = rnd() < .5 ? '#7d8a45' : (rnd() < .5 ? '#5b6a33' : '#8a7a4a'); g.fillRect(rnd() * w, rnd() * h, 1, 3); }
  });
  // glass wall with mullions (tile 1.3 m x 3 m)
  const glassTex = canvasTex(128, 256, (g, w, h) => {
    const gr = g.createLinearGradient(0, 0, 0, h); gr.addColorStop(0, '#5d6b74'); gr.addColorStop(.5, '#2b343b'); gr.addColorStop(1, '#3d474e');
    g.fillStyle = gr; g.fillRect(0, 0, w, h);
    g.fillStyle = '#26282a'; g.fillRect(0, 0, 6, h); g.fillRect(0, 150, w, 6);
  });
  const rathausSignTex = canvasTex(512, 96, (g, w, h) => {
    g.fillStyle = '#e9e6de'; g.fillRect(0, 0, w, h);
    g.fillStyle = '#2a2a2a'; g.font = 'bold 64px Arial, sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle';
    g.fillText('RATHAUS', w / 2, h / 2 + 3);
  });
  const RM = {
    rib: new THREE.MeshStandardMaterial({ map: ribTex, roughness: .9 }),
    ribV: new THREE.MeshStandardMaterial({ color: 0xd8d2c6, roughness: .85 }),
    glass: new THREE.MeshStandardMaterial({ map: glassTex, roughness: .1, metalness: .55 }),
    roof: new THREE.MeshStandardMaterial({ color: 0x55585b, roughness: .8, side: THREE.DoubleSide }),
    soffit: new THREE.MeshStandardMaterial({ color: 0xc9c4b8, roughness: .9, side: THREE.DoubleSide }),
    asphalt: new THREE.MeshStandardMaterial({ map: asphaltTex, roughness: .95 }),
    grass: new THREE.MeshStandardMaterial({ map: grassTex, roughness: 1 }),
    white: new THREE.MeshStandardMaterial({ color: 0xf2f2f2, roughness: .6 }),
  };
  
  // ---------- ground: brick paving, street, sidewalk, lawn
  const ground = terrainMesh(M.pave, 1.6);
  function strip(s0, s1, d0, d1, mat, y, tu = 1, tv = 1) {
    const L = s1 - s0, D = d1 - d0;
    const g = new THREE.PlaneGeometry(L, D, Math.ceil(L / 3), Math.max(1, Math.ceil(D / 3))); g.rotateX(-Math.PI / 2);
    const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * L / tu, uv.getY(i) * D / tv);
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true;
    m.position.copy(SD3((s0 + s1) / 2, (d0 + d1) / 2, 0)); m.rotation.y = HALL_ROT; m.name = 'ground'; scene.add(m); drape(m, y + .02); return m;
  }
  strip(-140, 200, -34, -26.5, RM.asphalt, .01, 3, 3);              // Rathausstraße
  for (let s = -138; s < 198; s += 6) strip(s, s + 3, -30.35, -30.15, RM.white, .02);   // centre line
  strip(-140, 200, -26.5, -26.3, M.conc, .03, 1, 1);                  // kerb
  strip(-140, 200, -34.2, -34, M.conc, .03, 1, 1);
  strip(-34, -2, -22, -11, RM.grass, .015, 2, 2);                  // lawn west of the Ratssaal block
  const cobLine = strip(-40, 80, -25.6, -24.8, M.cob, .02, .8, .8);    // paving seam along the street
  
  // ---------- neighbouring buildings from LoD2 (Geobasis NRW)
  const LOD2 = DATEN.lod2;
  const wallUniforms = { uNight: { value: 0 } };
  function wallMaterial(kind) {   // kind 0 = houses (brick), 1 = Rathaus (ribbed concrete bands)
    const m = new THREE.MeshStandardMaterial({ map: kind ? ribTex : brickTex, roughness: .88, side: THREE.DoubleSide });
    m.onBeforeCompile = sh => {
      sh.uniforms.uKind = { value: kind };
      sh.uniforms.uNight = wallUniforms.uNight;
      sh.vertexShader = sh.vertexShader
        .replace('#include <common>', '#include <common>\nattribute vec3 wattr;\nattribute float bofs;\nvarying vec3 vW;\nvarying float vH;')
        .replace('#include <begin_vertex>', '#include <begin_vertex>\nvW = wattr; vH = position.y - bofs;');
      sh.fragmentShader = sh.fragmentShader
        .replace('#include <common>', `#include <common>
  uniform float uKind; uniform float uNight; varying vec3 vW; varying float vH;`)
        .replace('#include <map_fragment>', `
  vec4 col; float topH = vH + vW.y; bool glow = false; bool isGlass = false;
  if (uKind < .5) {
    col = texture2D(map, vec2(vW.x, vH / .6));
    if (vW.z > 3.5 && topH > 2.6) {
      float per = 2.7, sh = mod(vW.z, per) * .5, lx = mod(vW.x - sh, per) / per, ly = mod(vH - .9, 2.8), wh = 1.45;
      float idx = floor((vW.x - sh) / per) + 31.0 * floor((vH - .9) / 2.8);
      bool fits = vH > .9 && vW.x > sh && vW.x < vW.z - sh && vW.y > (wh - ly) + .25;
      if (fits && lx > .2 && lx < .8 && ly < wh) {
        bool fr = lx < .24 || lx > .76 || ly < .07 || ly > wh - .07 || abs(lx - .5) < .015;
        col = vec4(fr ? vec3(.85) : mix(vec3(.03,.05,.07), vec3(.25,.3,.34), ly / wh), 1.);
        glow = !fr && fract(idx * .618 + floor(vW.z * 10.) * .137) > .45;
      }
    }
  } else {
    // Rathaus: recessed ground floor on pillars, ribbed spandrel bands, window strips, glazed top floor
    col = texture2D(map, vec2(vW.x / 1.6, vH / 1.6));
    float per = 1.25, lx = mod(vW.x, per) / per;
    if (topH < 3.0) {
      // low structures: plain concrete
    } else if (vH < 3.4) {
      bool pillar = mod(vW.x, 6.0) < .6;
      col = pillar ? vec4(.8, .78, .73, 1.) : vec4(mix(vec3(.04,.05,.06), vec3(.18,.2,.22), vH / 3.4), 1.);
      if (!pillar && lx < .04) col.rgb = vec3(.1);
      isGlass = !pillar;
    } else if (vW.y < .9) {
      // parapet: ribbed concrete
    } else if (vW.y < 3.6 && topH > 20.0) {
      bool post = mod(vW.x, 3.6) < .35;
      col = post ? vec4(.78, .76, .72, 1.) : vec4(mix(vec3(.05,.06,.08), vec3(.3,.34,.37), (vW.y - .9) / 2.7), 1.);
      if (!post && lx < .04) col.rgb = vec3(.12);
      isGlass = !post;
    } else {
      float ly = mod(vH - 3.4, 3.2);
      if (ly > 1.25 && ly < 3.05) {
        bool fr = lx < .05 || ly < 1.33 || ly > 2.98;
        col = vec4(fr ? vec3(.16, .17, .18) : mix(vec3(.05,.07,.09), vec3(.32,.36,.4), (ly - 1.25) / 1.8), 1.);
        isGlass = !fr;
      }
    }
    float idx = floor(vW.x / per) + 37.0 * floor((vH - 3.4) / 3.2) + floor(vW.z * 7.);
    glow = isGlass && fract(idx * .618) > .4;
  }
  if (glow) totalEmissiveRadiance += vec3(1.0, .74, .42) * uNight * 1.3;
  diffuseColor *= col;`);
    };
    return m;
  }
  const houseWallMat = wallMaterial(0), rathausWallMat = wallMaterial(1);
  const roofMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .85, side: THREE.DoubleSide, flatShading: true });
  const footprints = [];
  {
    const walls = [], wallsR = [], roofs = [], unders = [], pillarPts = [];
    const mk = (pos, attr, o = 0) => {
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
      if (attr) { g.setAttribute('wattr', new THREE.Float32BufferAttribute(attr, 3)); g.setAttribute('bofs', new THREE.Float32BufferAttribute(new Float32Array(pos.length / 3).fill(o), 1)); }
      g.computeVertexNormals(); return g;
    };
    LOD2.forEach((b, bi) => {
      if (b.w.length) (b.s === 2 ? wallsR : walls).push(mk(b.w, b.a, b.o));
      if (b.r.length) {
        const g = mk(b.r), p = g.attributes.position, cols = [];
        seed = 99 + bi;
        const tile = b.s === 3 ? [.3, .52, .45] : b.s === 2 ? [.35, .35, .35] : (rnd() < .6 ? [.32, .14, .1] : [.16, .16, .18]);
        const v = new THREE.Vector3(), a = new THREE.Vector3(), c = new THREE.Vector3();
        for (let i = 0; i < p.count; i += 3) {
          v.fromBufferAttribute(p, i); a.fromBufferAttribute(p, i + 1); c.fromBufferAttribute(p, i + 2);
          const n = a.clone().sub(v).cross(c.clone().sub(v)).normalize();
          const flat = Math.abs(n.y) > .97, k = .9 + rnd() * .2;
          const col = flat ? [.36 * k, .36 * k, .35 * k] : tile.map(x => x * k);
          for (let j = 0; j < 3; j++) cols.push(...col);
        }
        g.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3)); roofs.push(g);
      }
      for (const t of b.g) footprints.push(t);
      if (b.u && b.u.length) unders.push(mk(b.u));
      if (b.pl) for (let i = 0; i < b.pl.length; i += 3) pillarPts.push([b.pl[i], b.pl[i + 1], b.pl[i + 2]]);
    });
    if (unders.length) { const um = new THREE.Mesh(mergeGeometries(unders), RM.soffit); um.receiveShadow = true; scene.add(um); }
    if (pillarPts.length) {
      const pg = pillarPts.map(([x, z, top]) => { const gr = H(x, -z) - .3, h = Math.max(.5, top - gr); const g = new THREE.BoxGeometry(.6, h, .6); g.translate(x, gr + h / 2, z); return g; });
      const pm = new THREE.Mesh(mergeGeometries(pg), RM.ribV); pm.castShadow = true; scene.add(pm);
    }
    const m = new THREE.Mesh(mergeGeometries(walls), houseWallMat); m.castShadow = m.receiveShadow = true; m.name = 'lod2'; scene.add(m);
    const mr = new THREE.Mesh(mergeGeometries(wallsR), rathausWallMat); mr.castShadow = mr.receiveShadow = true; mr.name = 'lod2'; scene.add(mr);
    const rm = new THREE.Mesh(mergeGeometries(roofs), roofMat); rm.castShadow = rm.receiveShadow = true; scene.add(rm);
  }
  
  // ---------- Rathaus office blocks: extruded from the LoD2 footprints (Rathausstraße 6)
  function extrude(poly, h, mat, y0 = 0) {   // poly: [[E,N],...] counter-clockwise or not
    const pos = [], attr = [];
    let u = 0;
    for (let i = 0; i < poly.length; i++) {
      const a = poly[i], b = poly[(i + 1) % poly.length];
      const L = Math.hypot(b[0] - a[0], b[1] - a[1]); if (L < .05) continue;
      const A0 = [a[0], y0, -a[1]], B0 = [b[0], y0, -b[1]], A1 = [a[0], h, -a[1]], B1 = [b[0], h, -b[1]];
      pos.push(...A0, ...B0, ...B1, ...A0, ...B1, ...A1);
      const at = (uu, y) => [uu, h - y, L];
      attr.push(...at(0, y0), ...at(L, y0), ...at(L, h), ...at(0, y0), ...at(L, h), ...at(0, h));
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
    g.setAttribute('wattr', new THREE.Float32BufferAttribute(attr, 3));
    g.setAttribute('bofs', new THREE.Float32BufferAttribute(new Float32Array(pos.length / 3), 1));
    g.computeVertexNormals();
    const m = new THREE.Mesh(g, mat); m.castShadow = m.receiveShadow = true; m.name = 'lod2'; scene.add(m);
    // flat roof
    const shape = new THREE.Shape(poly.map(p => new THREE.Vector2(p[0], p[1])));
    const rg = new THREE.ShapeGeometry(shape); rg.rotateX(-Math.PI / 2); rg.translate(0, h, 0);
    const r = new THREE.Mesh(rg, RM.roof); r.receiveShadow = true; r.castShadow = true; scene.add(r);
    // footprint for walking
    const tris = THREE.ShapeUtils.triangulateShape(poly.map(p => new THREE.Vector2(p[0], p[1])), []);
    for (const t of tris) footprints.push(t.map(i => [poly[i][0], -poly[i][1]]));
    return m;
  }
  const TOWER = [[-6.6, 10.3], [-1.0, 11.5], [11.2, 14.2], [10.2, 19.3], [7.2, 32.6], [2.2, 31.4], [2.1, 32.2], [-3.7, 31.0], [-10.8, 29.4]];
  const MID = [[1.0, 2.5], [2.7, -5.0], [21.7, -.7], [17.0, 20.8], [11.7, 19.6], [10.2, 19.3], [11.2, 14.2], [-1.0, 11.5]];
  const CORE = [[8.1, 13.5], [-1.1, 11.5], [-2.8, 19.1], [6.4, 21.2]];
  const RAMP = [[-11.1, 30.5], [-3.9, 32.1], [-8.8, 54.2], [-16.0, 52.6]];
  extrude(TOWER, 23.1, rathausWallMat);
  extrude(MID, 17.8, rathausWallMat);
  extrude(CORE, 25.0, new THREE.MeshStandardMaterial({ map: ribTex, roughness: .9 }), 22.0);
  extrude(RAMP, 1.8, new THREE.MeshStandardMaterial({ color: 0xa9a59c, roughness: .9 }));
  // horizontal concrete ledges on the tower (the strong bands in the photos)
  {
    const geos = [];
    for (let y = 3.4; y < 21; y += 3.2) {
      for (let i = 0; i < TOWER.length; i++) {
        const a = TOWER[i], b = TOWER[(i + 1) % TOWER.length], L = Math.hypot(b[0] - a[0], b[1] - a[1]);
        if (L < 1.5) continue;
        const g = new THREE.BoxGeometry(L + .5, .18, .5);
        const ang = Math.atan2(b[1] - a[1], b[0] - a[0]);
        const nx = (b[1] - a[1]) / L, nn = -(b[0] - a[0]) / L;   // right-hand normal
        const cx = (a[0] + b[0]) / 2, cn = (a[1] + b[1]) / 2;
        // pick the outward side (away from tower centre)
        const out = ((cx + nx) - 1) ** 2 + ((cn + nn) - 21) ** 2 > (cx - 1) ** 2 + (cn - 21) ** 2 ? 1 : -1;
        g.rotateY(ang); g.translate(cx + nx * .2 * out, y, -(cn + nn * .2 * out)); geos.push(g);
      }
    }
    const m = new THREE.Mesh(mergeGeometries(geos), RM.ribV); m.castShadow = true; scene.add(m);
  }
  
  // south-west office wing (LoD2 part, 12.6 m above the plaza)
  const SWWING = [[1.0, 2.5], [2.7, -5.0], [21.7, -.7], [23.3, -7.8], [16.0, -9.4], [13.6, -9.9], [-7.1, -14.5], [-9.0, -14.9], [-18.3, -16.8], [-21.4, -2.5]];
  extrude(SWWING, 14.7, rathausWallMat);
  // ---------- Sitzungstrakt (council wing): raised on pillars, fins, roof rising towards the south end
  // footprint from the LoD2 overhang structures east of the office block
  const HALLPOLY = [[16, -9.4], [24.5, -7.5], [23.8, -3.9], [27.3, -3.1], [34.1, -1.6], [45.4, -23.8], [28.4, -27.6], [25.3, -27.0], [20.2, -28.2], [18.5, -28.5], [19.3, -26.3], [19.5, -25.4], [16.3, -11]];
  const HAX = [.33, -.944];                       // north → south axis of the wing
  const proj = (e, n) => e * HAX[0] + n * HAX[1];
  const P0 = Math.min(...HALLPOLY.map(p => proj(...p))), P1 = Math.max(...HALLPOLY.map(p => proj(...p)));
  const tOf = (e, n) => (proj(e, n) - P0) / (P1 - P0);
  const SOFFIT = 4.0;
  // roof profile (from the photo): low and level over the dense fins, then rising steadily to the tall glazed street end
  const TK = .45;
  const hRoof = t => t < TK ? 9.0 : 9.0 + (t - TK) / (1 - TK) * 5.2;
  const zone = t => t < TK ? 'fins' : 'tall';
  const BAND = 1.0;   // white fascia band above the pillars
  const hallGroup = new THREE.Group(); hallGroup.position.y = Math.max(...HALLPOLY.map(p => H(...p))) + .1; scene.add(hallGroup);
  function clipPoly(poly, a, b) {   // keep part with a <= t <= b
    const clip = (pts, keep) => {
      const out = [];
      for (let i = 0; i < pts.length; i++) {
        const p = pts[i], q = pts[(i + 1) % pts.length], kp = keep(p), kq = keep(q);
        if (kp) out.push(p);
        if (kp !== kq) {
          const tp = tOf(...p), tq = tOf(...q), lim = keep === inA ? a : b, f = (lim - tp) / (tq - tp);
          out.push([p[0] + (q[0] - p[0]) * f, p[1] + (q[1] - p[1]) * f]);
        }
      }
      return out;
    };
    const inA = p => tOf(...p) >= a, inB = p => tOf(...p) <= b;
    let r = clip(poly, inA); if (r.length < 3) return [];
    r = clip(r, inB); return r.length < 3 ? [] : r;
  }
  {
    // roof and soffit, built in thin slices so the folded profile is exact
    const roofPos = [], sofPos = [];
    const cuts = []; for (let t = 0; t <= 1.0001; t += .02) cuts.push(t); cuts.push(TK); cuts.sort((x, y) => x - y);
    for (let i = 0; i < cuts.length - 1; i++) {
      const sl = clipPoly(HALLPOLY, cuts[i], cuts[i + 1]); if (!sl.length) continue;
      const tri = THREE.ShapeUtils.triangulateShape(sl.map(p => new THREE.Vector2(p[0], p[1])), []);
      for (const t of tri) for (const k of t) {
        const [e, n] = sl[k];
        roofPos.push(e, hRoof(Math.min(1, Math.max(0, tOf(e, n)))), -n);
        sofPos.push(e, SOFFIT, -n);
      }
    }
    const geo = pos => { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.computeVertexNormals(); return g; };
    const r = new THREE.Mesh(geo(roofPos), RM.roof); r.castShadow = true; hallGroup.add(r);
    const so = new THREE.Mesh(geo(sofPos), RM.soffit); so.receiveShadow = true; hallGroup.add(so);
    // walls: glass all round, white fascia band above the pillars, continuous roof-edge band, vertical fins / mullions
    const glassPos = [], glassUv = [], solidPos = [], solidUv = [], finGeos = [], bandPos = [], botGeos = [];
    let u = 0;
    for (let i = 0; i < HALLPOLY.length; i++) {
      const a = HALLPOLY[i], b = HALLPOLY[(i + 1) % HALLPOLY.length];
      const L = Math.hypot(b[0] - a[0], b[1] - a[1]), N = Math.max(1, Math.ceil(L / .5));
      const nrm = [-(b[1] - a[1]) / L, (b[0] - a[0]) / L];
      const ang = Math.atan2(b[1] - a[1], b[0] - a[0]);
      // the street end of the wing is a closed wall with vertical cladding (no glass, no fins)
      const endWall = Math.abs(((b[0] - a[0]) * HAX[0] + (b[1] - a[1]) * HAX[1]) / L) < .5 && Math.min(tOf(...a), tOf(...b)) > .75;
      // bottom fascia: one straight box per edge (horizontal, so no steps)
      { const g = new THREE.BoxGeometry(L + .3, BAND, .35); g.rotateY(ang); g.translate((a[0] + b[0]) / 2 + nrm[0] * .1, SOFFIT + BAND / 2, -((a[1] + b[1]) / 2 + nrm[1] * .1)); botGeos.push(g); }
      for (let k = 0; k < N; k++) {
        const f0 = k / N, f1 = (k + 1) / N;
        const p = [a[0] + (b[0] - a[0]) * f0, a[1] + (b[1] - a[1]) * f0], q = [a[0] + (b[0] - a[0]) * f1, a[1] + (b[1] - a[1]) * f1];
        const hp = hRoof(tOf(...p)), hq = hRoof(tOf(...q));
        const quad = [p[0], SOFFIT + BAND, -p[1], q[0], SOFFIT + BAND, -q[1], q[0], hq, -q[1], p[0], SOFFIT + BAND, -p[1], q[0], hq, -q[1], p[0], hp, -p[1]];
        const s0 = (u + f0 * L) / 1.3, s1 = (u + f1 * L) / 1.3;
        const uvq = [s0, 0, s1, 0, s1, (hq - SOFFIT) / 3, s0, 0, s1, (hq - SOFFIT) / 3, s0, (hp - SOFFIT) / 3];
        if (endWall) { solidPos.push(...quad); solidUv.push(...uvq.map((v, j) => j % 2 ? v * 3 / 3.2 : v * 1.3 / 1.6)); } else { glassPos.push(...quad); glassUv.push(...uvq); }
        // roof-edge band as a continuous ribbon (outer face + top), following the slope exactly
        const o = .18, pe = [p[0] + nrm[0] * o, p[1] + nrm[1] * o], qe = [q[0] + nrm[0] * o, q[1] + nrm[1] * o];
        bandPos.push(pe[0], hp - .55, -pe[1], qe[0], hq - .55, -qe[1], qe[0], hq + .12, -qe[1], pe[0], hp - .55, -pe[1], qe[0], hq + .12, -qe[1], pe[0], hp + .12, -pe[1]);
        bandPos.push(pe[0], hp + .12, -pe[1], qe[0], hq + .12, -qe[1], q[0], hq + .12, -q[1], pe[0], hp + .12, -pe[1], q[0], hq + .12, -q[1], p[0], hp + .12, -p[1]);
        bandPos.push(pe[0], hp - .55, -pe[1], qe[0], hq - .55, -qe[1], q[0], hq - .55, -q[1], pe[0], hp - .55, -pe[1], q[0], hq - .55, -q[1], p[0], hp - .55, -p[1]);
      }
      // fins: dense and slim over the low part, sparse and strong mullions at the tall end
      if (!endWall) for (let s = .5; s < L - .3;) {
        const e = a[0] + (b[0] - a[0]) * s / L, n = a[1] + (b[1] - a[1]) * s / L, t = tOf(e, n), dense = zone(t) === 'fins';
        const h = hRoof(t) - .55 - (SOFFIT + BAND), w = dense ? .17 : .32, d = dense ? .55 : .45;
        const g = new THREE.BoxGeometry(w, h, d); g.rotateY(ang);
        g.translate(e + nrm[0] * (d / 2 + .02), SOFFIT + BAND + h / 2, -(n + nrm[1] * (d / 2 + .02))); finGeos.push(g);
        if (!dense) { const tr = new THREE.BoxGeometry(1.75, .08, .12); tr.rotateY(ang); tr.translate(e + (b[0] - a[0]) / L * .85 + nrm[0] * .05, SOFFIT + BAND + 2.4, -(n + (b[1] - a[1]) / L * .85 + nrm[1] * .05)); finGeos.push(tr); }
        s += dense ? .62 : 1.75;
      }
      u += L;
    }
    const mk = (pos, uv) => { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); if (uv) g.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); g.computeVertexNormals(); return g; };
    const gw = new THREE.Mesh(mk(glassPos, glassUv), new THREE.MeshStandardMaterial({ map: glassTex, roughness: .1, metalness: .55, side: THREE.DoubleSide }));
    gw.name = 'lod2'; gw.castShadow = gw.receiveShadow = true; hallGroup.add(gw);
    const cladTex2 = canvasTex(128, 256, (g, w, h) => { g.fillStyle = '#d9d4c8'; g.fillRect(0, 0, w, h); g.fillStyle = '#c3bdb0'; g.fillRect(0, 0, 5, h); g.fillStyle = '#b8b2a5'; g.fillRect(0, h - 3, w, 3); for (let i = 0; i < 300; i++) { g.fillStyle = `rgba(80,70,60,${rnd() * .06})`; g.fillRect(rnd() * w, rnd() * h, 2, 4); } });
    const sw = new THREE.Mesh(mk(solidPos, solidUv), new THREE.MeshStandardMaterial({ map: cladTex2, roughness: .85, side: THREE.DoubleSide }));
    sw.name = 'lod2'; sw.castShadow = sw.receiveShadow = true; hallGroup.add(sw);
    const white = new THREE.MeshStandardMaterial({ color: 0xdcd7cc, roughness: .85, side: THREE.DoubleSide });
    const band = new THREE.Mesh(mk(bandPos), white); band.castShadow = true; hallGroup.add(band);
    const bot = new THREE.Mesh(mergeGeometries(botGeos), RM.ribV); bot.castShadow = bot.receiveShadow = true; hallGroup.add(bot);
    const fins = new THREE.Mesh(mergeGeometries(finGeos), RM.ribV); fins.castShadow = true; hallGroup.add(fins);
  }
  // pillars under the wing
  const blockers = [];
  {
    const geos = [];
    const inside = (e, n) => { let c = false; for (let i = 0, j = HALLPOLY.length - 1; i < HALLPOLY.length; j = i++) { const [xi, yi] = HALLPOLY[i], [xj, yj] = HALLPOLY[j]; if ((yi > n) !== (yj > n) && e < (xj - xi) * (n - yi) / (yj - yi) + xi) c = !c; } return c; };
    for (let a = -40; a < 40; a += 6) for (let c = -40; c < 40; c += 6) {
      const e = 30 + a * .944 + c * .33, n = -15 + a * .33 - c * .944;
      if (!inside(e, n)) continue;
      // keep pillars at least 1.2 m inside the outline
      if (![[1.2, 0], [-1.2, 0], [0, 1.2], [0, -1.2]].every(([de, dn]) => inside(e + de, n + dn))) continue;
      const lowg = H(e, n) - hallGroup.position.y - .3; const g = new THREE.BoxGeometry(.7, SOFFIT - lowg, .7); g.translate(e, (SOFFIT + lowg) / 2, -n); geos.push(g); blockers.push([e, n, .6]);
    }
    const m = new THREE.Mesh(mergeGeometries(geos), RM.ribV); m.castShadow = m.receiveShadow = true; hallGroup.add(m);
  }
  // entrance: the glazed front of the office wing continues under the council wing (no separate glass box)
  const GW1 = [23.3, -7.8], GW2 = [35.85, -5.04], GWN = [.215, -.976];
  const lobbyMat = new THREE.MeshStandardMaterial({ map: glassTex, roughness: .12, metalness: .5, emissive: 0xffd9a0, emissiveIntensity: 0, side: THREE.DoubleSide });
  function onWall(obj, p1, p2, t, y, nrm, off = .06) {
    const e = p1[0] + (p2[0] - p1[0]) * t, n = p1[1] + (p2[1] - p1[1]) * t;
    obj.position.set(e + nrm[0] * off, y + H(e, n), -(n + nrm[1] * off)); obj.rotation.y = Math.atan2(nrm[0], -nrm[1]); return obj;
  }
  {
    const L = Math.hypot(GW2[0] - GW1[0], GW2[1] - GW1[1]), N = 24, pos = [], uv = [];
    const top = hallGroup.position.y + SOFFIT;
    for (let k = 0; k < N; k++) {
      const f0 = k / N, f1 = (k + 1) / N;
      const p = [GW1[0] + (GW2[0] - GW1[0]) * f0, GW1[1] + (GW2[1] - GW1[1]) * f0], q = [GW1[0] + (GW2[0] - GW1[0]) * f1, GW1[1] + (GW2[1] - GW1[1]) * f1];
      const gp = H(...p) - .2, gq = H(...q) - .2;
      pos.push(p[0], gp, -p[1], q[0], gq, -q[1], q[0], top, -q[1], p[0], gp, -p[1], q[0], top, -q[1], p[0], top, -p[1]);
      uv.push(f0 * L / 1.3, 0, f1 * L / 1.3, 0, f1 * L / 1.3, 1.4, f0 * L / 1.3, 0, f1 * L / 1.3, 1.4, f0 * L / 1.3, 1.4);
    }
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); g.computeVertexNormals();
    const m = new THREE.Mesh(g, lobbyMat); m.receiveShadow = true; m.name = 'lod2'; scene.add(m);
    // everything behind this front is indoors
    const poly = [GW1, GW2, [34.1, -1.6], [27.3, -3.1], [23.8, -3.9], [24.5, -7.5]];
    const tris = THREE.ShapeUtils.triangulateShape(poly.map(p => new THREE.Vector2(p[0], p[1])), []);
    for (const t of tris) footprints.push(t.map(i => [poly[i][0], -poly[i][1]]));
  }
  const rathausSign = plane(2.6, .5, new THREE.MeshStandardMaterial({ map: rathausSignTex, roughness: .7 })); rathausSign.name = 'rsign';
  scene.add(onWall(rathausSign, GW1, GW2, .5, 3.0, GWN, .08));
  const crest = new THREE.Mesh(new THREE.CircleGeometry(.28, 24), new THREE.MeshStandardMaterial({ color: 0xc8b070, roughness: .5, metalness: .4 }));
  scene.add(onWall(crest, GW1, GW2, .62, 3.05, GWN, .08));
  const doorGroup = new THREE.Group(); doorGroup.name = 'door';
  add(box(2.4, 2.5, .1, M.dark), 0, 1.25, 0, doorGroup);
  for (const x of [-.58, .58]) add(plane(1.05, 2.3, M.glass), x, 1.17, .06, doorGroup);
  scene.add(onWall(doorGroup, GW1, GW2, .5, 0, GWN, .05));
  {
    const g = new THREE.Group(); g.name = 'board';
    add(box(1.8, 1.2, .2, M.dark), 0, 1.4, 0, g);
    add(plane(1.6, 1.0, new THREE.MeshStandardMaterial({ color: 0xe9e3d3, roughness: .8 })), 0, 1.4, .11, g);
    for (const x of [-.7, .7]) add(box(.08, .8, .08, M.metal), x, .4, 0, g);
    scene.add(onWall(g, GW1, GW2, .2, 0, GWN, 1.4)); blockers.push([26.1, -8.6, .9]);
  }
  const canLamp = new THREE.Mesh(new THREE.CylinderGeometry(.18, .18, .05, 20), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffe2a8, emissiveIntensity: .4 }));
  canLamp.position.set(29.6 + .215 * 1.8, hallGroup.position.y + SOFFIT - .03, -(-6.42 - .976 * 1.8)); scene.add(canLamp);
  const porchLight = new THREE.PointLight(0xffc888, 0, 14, 1.4); porchLight.position.set(29.6 + .215 * 2, hallGroup.position.y + SOFFIT - .4, -(-6.42 - .976 * 2)); scene.add(porchLight);
  const GB = new THREE.Group(); scene.add(GB);   // kept for the evening toggle
  // ---------- vegetation
  function noisySphere(r, detail = 2, amp = .18) {
    const g = new THREE.IcosahedronGeometry(r, detail);
    const p = g.attributes.position;
    for (let i = 0; i < p.count; i++) {
      const v = new THREE.Vector3().fromBufferAttribute(p, i);
      v.multiplyScalar(1 + (rnd() - .5) * amp); p.setXYZ(i, v.x, v.y, v.z);
    }
    g.computeVertexNormals(); return g;
  }
  const hedgeMat = new THREE.MeshStandardMaterial({ color: 0x3d5a2c, roughness: 1, flatShading: true });
  function boxHedge(s0, s1, d, h = .8, w = .9) {
    const L = s1 - s0, g = new THREE.BoxGeometry(L, h, w, Math.ceil(L), 2, 2);
    const p = g.attributes.position;
    for (let i = 0; i < p.count; i++) p.setXYZ(i, p.getX(i) + (rnd() - .5) * .08, p.getY(i) + (rnd() - .5) * .08, p.getZ(i) + (rnd() - .5) * .08);
    g.computeVertexNormals();
    const m = new THREE.Mesh(g, hedgeMat); m.position.copy(SD3((s0 + s1) / 2, d, h / 2)); m.rotation.y = HALL_ROT;
    m.castShadow = m.receiveShadow = true; scene.add(m);
    for (let s = s0; s <= s1; s += .6) blockers.push([...SD(s, d), .55]);
  }
  boxHedge(-33, -3, -11.6);
  boxHedge(-33, -6, -21.4);
  function tree(e, n, h, spread, seedOff) {
    seed = 777 + seedOff;
    const geos = [], up = new THREE.Vector3(0, 1, 0);
    function br(p, dir, len, rad, depth) {
      const g = new THREE.CylinderGeometry(rad * .7, rad, len, 5, 1, true);
      g.translate(0, len / 2, 0);
      g.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(up, dir));
      g.translate(p.x, p.y, p.z); geos.push(g);
      const end = p.clone().addScaledVector(dir, len);
      if (depth === 0) return;
      const k = depth > 3 ? 2 : (depth > 1 ? 3 : 4);
      for (let i = 0; i < k; i++) {
        const d = dir.clone().add(new THREE.Vector3(rr(-spread, spread), rr(.1, .5), rr(-spread, spread))).normalize();
        br(end, d, len * rr(.62, .78), rad * .62, depth - 1);
      }
    }
    br(W3(e, n), new THREE.Vector3(rr(-.05, .05), 1, rr(-.05, .05)).normalize(), h * .35, h * .03, 5);
    const m = new THREE.Mesh(mergeGeometries(geos), M.bark); m.castShadow = true; scene.add(m);
    blockers.push([e, n, .6]);
  }
  [[-26, -16, 17], [-12, -17, 19], [62, -23.5, 15], [70, -23.5, 14], [-26, 8, 16], [30, 26, 15], [14, -23.5, 13], [36, -23.5, 12]]
    .forEach(([s, d, h], i) => { const [e, n] = SD(s, d); tree(e, n, h, .75, i + 10); });
  seed = 4242;
  
  // ---------- street furniture: flags, lamps, street sign
  const lampLights = [];
  function lamp(s, d, h, blue = false) {
    const g = new THREE.Group();
    const mat = blue ? new THREE.MeshStandardMaterial({ color: 0x2f5fb0, roughness: .5, metalness: .4 }) : M.metal;
    add(new THREE.Mesh(new THREE.CylinderGeometry(.07, .1, h, 12), mat), 0, h / 2, 0, g).castShadow = true;
    const bulb = add(new THREE.Mesh(new THREE.SphereGeometry(.22, 16, 12), new THREE.MeshStandardMaterial({ color: 0xf4f4f4, emissive: 0xffd9a0, emissiveIntensity: 0 })), 0, h + .15, 0, g);
    const L = new THREE.PointLight(0xffc888, 0, 14, 1.6); L.position.set(0, h, 0); g.add(L);
    lampLights.push({ L, bulb });
    g.position.copy(SD3(s, d)); scene.add(g); blockers.push([...SD(s, d), .3]);
  }
  lamp(14.5, -24.5, 3.6, true); lamp(13.8, -15, 3.6, true); lamp(-14, -25.6, 6); lamp(20, -25.6, 6); lamp(54, -25.6, 6);
  const flagMats = [new THREE.MeshStandardMaterial({ color: 0x2c5fb4, side: THREE.DoubleSide, roughness: .8 }), new THREE.MeshStandardMaterial({ color: 0xf2f2f2, side: THREE.DoubleSide, roughness: .8 })];
  const flags = [];
  for (const [s, k] of [[-6, 0], [-4.6, 1]]) {
    const g = new THREE.Group();
    add(new THREE.Mesh(new THREE.CylinderGeometry(.05, .07, 9, 10), M.metal), 0, 4.5, 0, g).castShadow = true;
    const fg = new THREE.PlaneGeometry(.9, 3.2, 1, 8);
    const f = add(new THREE.Mesh(fg, flagMats[k]), .5, 7.2, 0, g); f.castShadow = true; flags.push(f);
    g.position.copy(SD3(s, -10)); g.rotation.y = HALL_ROT + .4; scene.add(g); blockers.push([...SD(s, -10), .3]);
  }
  {
    const g = new THREE.Group();
    for (const x of [-.7, .7]) add(new THREE.Mesh(new THREE.CylinderGeometry(.04, .04, 1.6, 8), M.metal), x, .8, 0, g);
    add(plane(1.6, .42, new THREE.MeshStandardMaterial({ map: rathausSignTex, roughness: .6, side: THREE.DoubleSide })), 0, 1.45, 0, g);
    g.position.copy(SD3(-2, -24)); g.rotation.y = HALL_ROT; g.name = 'stsign'; scene.add(g);
    blockers.push([...SD(-2, -24), .9]);
  }
  // bench under the hall
  {
    const g = new THREE.Group();
    add(box(1.8, .08, .45, M.wood), 0, .45, 0, g);
    for (const x of [-.75, .75]) add(box(.08, .45, .4, M.dark), x, .22, 0, g);
    g.position.copy(W3(12.5, -21)); g.rotation.y = 1.4; g.name = 'bench'; scene.add(g);
    blockers.push([12.5, -21, 1.0]);
  }
  // parked bikes stand
  {
    const geos = [];
    for (let i = 0; i < 5; i++) { const t = new THREE.TorusGeometry(.4, .03, 6, 16, Math.PI); t.translate(i * .8, .1, 0); geos.push(t); }
    const m = new THREE.Mesh(mergeGeometries(geos), M.metal); m.position.copy(W3(14.5, -12)); m.rotation.y = 1.4; scene.add(m);
  }
  
  return { scene, H, W3, camVec, blockers, footprints, M, box, plane, add, canvasTex, onWall, GW1, GW2, GWN, SD, flags, sun };
}
