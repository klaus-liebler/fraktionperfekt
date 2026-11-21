// Erzeugt aus dem 3D-Prototyp (index.html) mit gen_welt.py – Außenschauplatz (Gebäude: LoD2, Geobasis NRW, dl-de/zero-2-0).
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
  sun.position.set(35, 40, 25);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -60, right: 60, top: 60, bottom: -60, near: 1, far: 200 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = .03;
  scene.add(sun);
  
  // ---------- helpers for real-world coordinates (E = east, N = north, metres from school centre)
  const W3 = (e, n, y = 0) => new THREE.Vector3(e, y + H(e, n), -n);
  // place a flat object on a wall segment (endpoints in E/N), t = 0..1 along, outward normal (E/N)
  function onWall(obj, p1, p2, t, y, nrm, off = .06) {
    const e = p1[0] + (p2[0] - p1[0]) * t, n = p1[1] + (p2[1] - p1[1]) * t;
    obj.position.set(e + nrm[0] * off, y + H(e, n), -(n + nrm[1] * off));
    obj.rotation.y = Math.atan2(nrm[0], -nrm[1]);
    return obj;
  }
  
  // ---------- ground
  const ground = terrainMesh(M.pave, 1.6);
  const stripeA = [8, 15.5], stripeB = [23, 31];
  {
    const len = Math.hypot(stripeB[0] - stripeA[0], stripeB[1] - stripeA[1]);
    const sg = new THREE.PlaneGeometry(1.5, len, 1, Math.ceil(len / 1.5)); sg.rotateX(-Math.PI / 2);
    { const uv = sg.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * 1.5 / .8, uv.getY(i) * len / .8); }
    const st = new THREE.Mesh(sg, M.cob); st.receiveShadow = true;
    st.rotation.y = Math.atan2(-(stripeB[0] - stripeA[0]), stripeB[1] - stripeA[1]);
    st.position.set((stripeA[0] + stripeB[0]) / 2, 0, -(stripeA[1] + stripeB[1]) / 2); st.name = 'ground'; scene.add(st); drape(st, .03);
  }
  for (const [e, n, r] of [[15, 19, .45], [9.5, 13.8, .3]]) {
    const mh = new THREE.Mesh(new THREE.CircleGeometry(r, 28), new THREE.MeshStandardMaterial({ color: 0x4b3c35, roughness: .6, metalness: .4 }));
    mh.rotation.x = -Math.PI / 2; mh.position.set(e, 0, -n); mh.receiveShadow = true; scene.add(mh); drape(mh, .04);
  }
  
  // ---------- LoD2 buildings (Geobasis NRW, CityGML LoD2) with procedural facades
  const LOD2 = DATEN.lod2;
  const wallUniforms = { uNight: { value: 0 } };
  function wallMaterial(school) {
    const m = new THREE.MeshStandardMaterial({ map: brickTex, roughness: .88, side: THREE.DoubleSide });
    m.onBeforeCompile = sh => {
      sh.uniforms.cladMap = { value: cladTex };
      sh.uniforms.uSchool = { value: school ? 1 : 0 };
      sh.uniforms.uNight = wallUniforms.uNight;
      sh.vertexShader = sh.vertexShader
        .replace('#include <common>', '#include <common>\nattribute vec3 wattr;\nattribute float bofs;\nvarying vec3 vW;\nvarying float vH;')
        .replace('#include <begin_vertex>', '#include <begin_vertex>\nvW = wattr; vH = position.y - bofs;');
      sh.fragmentShader = sh.fragmentShader
        .replace('#include <common>', `#include <common>
  uniform sampler2D cladMap; uniform float uSchool; uniform float uNight;
  varying vec3 vW; varying float vH;
  float hsh(vec2 p){ return fract(sin(dot(p, vec2(12.9898,78.233))) * 43758.5453); }`)
        .replace('#include <map_fragment>', `
  vec4 col = texture2D(map, vec2(vW.x / 1.0, vH / 0.6));
  float topH = vH + vW.y;
  float band = uSchool > .5 ? (topH > 8.0 ? 2.2 : 1.3) : 0.0;
  if (vW.y < band) {
    col = texture2D(cladMap, vec2(vW.x / .5, vH));
  } else if (vW.z > 3.5 && topH > 2.6) {
    float per = uSchool > .5 ? 2.9 : 2.7;
    float sh = mod(vW.z, per) * .5;
    float lx = mod(vW.x - sh, per) / per;
    float sto = uSchool > .5 ? 3.5 : 2.8;
    float ly = mod(vH - .9, sto);
    float wh = uSchool > .5 ? 2.3 : 1.45;
    float idx = floor((vW.x - sh) / per) + 31.0 * floor((vH - .9) / sto);
    bool fits = vH > .9 && vW.x > sh && vW.x < vW.z - sh && (vW.y - band) > (wh - ly) + .25;
    if (fits && lx > .2 && lx < .8 && ly < wh) {
      bool frame = lx < .24 || lx > .76 || ly < .07 || ly > wh - .07 || abs(lx - .5) < .015 || abs(ly - wh * .72) < .035;
      vec3 fr = uSchool > .5 ? vec3(.36, .55, .42) : vec3(.85);
      vec3 gl = mix(vec3(.03, .05, .07), vec3(.25, .3, .34), smoothstep(0., wh, ly));
      col = vec4(frame ? fr : gl, 1.0);
      if (!frame && fract(idx * .6180339 + floor(vW.z * 10.0) * .1373) > .45) totalEmissiveRadiance += vec3(1.0, .72, .38) * uNight * 1.4;
    } else if (uSchool > .5 && topH > 8.0 && fits && ly < wh && (lx < .2 || lx > .8)) {
      col = texture2D(cladMap, vec2(vW.x / .5, vH));
    }
  }
  diffuseColor *= col;`);
    };
    return m;
  }
  const schoolWallMat = wallMaterial(true), houseWallMat = wallMaterial(false);
  const roofMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .85, side: THREE.DoubleSide, flatShading: true });
  const footprints = [];
  {
    const wallsS = [], wallsH = [], roofs = [];
    const mk = (pos, attr, o = 0) => {
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
      if (attr) { g.setAttribute('wattr', new THREE.Float32BufferAttribute(attr, 3)); g.setAttribute('bofs', new THREE.Float32BufferAttribute(new Float32Array(pos.length / 3).fill(o), 1)); }
      g.computeVertexNormals(); return g;
    };
    LOD2.forEach((b, bi) => {
      if (b.w.length) (b.s ? wallsS : wallsH).push(mk(b.w, b.a, b.o));
      if (b.r.length) {
        const g = mk(b.r), p = g.attributes.position, cols = [];
        seed = 99 + bi;
        const tile = b.s ? [.42, .43, .42] : (rnd() < .6 ? [.32, .14, .1] : [.16, .16, .18]);
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
    });
    for (const [arr, mat] of [[wallsS, schoolWallMat], [wallsH, houseWallMat]]) {
      const m = new THREE.Mesh(mergeGeometries(arr), mat); m.castShadow = m.receiveShadow = true; m.name = 'lod2'; scene.add(m);
    }
    const rm = new THREE.Mesh(mergeGeometries(roofs), roofMat); rm.castShadow = rm.receiveShadow = true; scene.add(rm);
  }
  function insideFootprint(e, n) {
    // footprints are stored as [x, z] with z = -N
    const z = -n;
    for (const t of footprints) {
      const [a, b, c] = t;
      const d1 = (e - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (z - b[1]);
      const d2 = (e - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (z - c[1]);
      const d3 = (e - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (z - a[1]);
      const neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
      if (!(neg && pos)) return true;
    }
    return false;
  }
  
  // ---------- school details placed on the real LoD2 walls
  const GB = new THREE.Group(); scene.add(GB);
  // low wing with the sign: NE wall from (5.7,12.8) to (0.2,21.6), outward normal (0.85,0.52)
  const SW1 = [5.7, 12.8], SW2 = [0.2, 21.6], SWN = [.848, .53];
  const sign = plane(8.6, .62, new THREE.MeshStandardMaterial({ map: signTex, roughness: .7 }));
  sign.name = 'sign'; GB.add(onWall(sign, SW1, SW2, .5, 3.45, SWN));
  for (let i = 0; i < 4; i++) {
    const w = plane(1.55, 1.55, new THREE.MeshStandardMaterial({ map: decoTex(i + 1), roughness: .8, emissive: 0x000000 }));
    w.name = 'deco'; GB.add(onWall(w, SW1, SW2, .16 + i * .225, 1.75, SWN));
  }
  // notice boards near the corner
  GB.add(onWall(box(.5, .42, .04, new THREE.MeshStandardMaterial({ color: 0xece8de })), SW1, SW2, .035, 2.0, SWN, .08));
  GB.add(onWall(box(.5, .55, .04, new THREE.MeshStandardMaterial({ color: 0x6fa3d6 })), SW1, SW2, .035, 1.4, SWN, .08));
  
  // entrance: recessed connector wall from (6.7,7.4) to (5.0,9.7), outward normal (0.8,0.6)
  const EW1 = [6.7, 7.4], EW2 = [5.0, 9.7], EWN = [.8, .6];
  const EB = new THREE.Group(); scene.add(EB);
  {
    const door = new THREE.Group();
    add(box(2.6, 2.75, .12, M.frame), 0, 1.375, 0, door);
    for (const x of [-.62, .62]) {
      add(plane(1.12, 2.45, M.glass), x, 1.28, .07, door);
      add(plane(.3, .4, new THREE.MeshStandardMaterial({ color: x < 0 ? 0xe9e6dc : 0x8db8e2, roughness: .9 })), x - .2, 1.7, .08, door);
      add(box(.04, .7, .05, M.metal), x > 0 ? .1 : -.1, 1.1, .12, door);
    }
    add(box(.07, 2.5, .09, M.frame), 0, 1.28, .08, door);
    EB.add(onWall(door, EW1, EW2, .5, 0, EWN, .02));
    // canopy over the door
    const can = new THREE.Group();
    add(box(3.4, .22, 1.6, M.roofEave), 0, 3.15, .8, can);
    add(box(3.5, .1, 1.7, M.cladTrim), 0, 3.31, .8, can);
    EB.add(onWall(can, EW1, EW2, .5, 0, EWN, 0));
  }
  const canLamp = new THREE.Mesh(new THREE.CylinderGeometry(.15, .15, .05, 20), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffe2a8, emissiveIntensity: .6 }));
  EB.add(onWall(canLamp, EW1, EW2, .5, 3.02, EWN, .8));
  const porchLight = new THREE.PointLight(0xffc888, 0, 9, 1.6);
  EB.add(onWall(porchLight, EW1, EW2, .5, 2.8, EWN, 1.0));
  
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
  const hedgeMats = [
    new THREE.MeshStandardMaterial({ color: 0x3f5a2e, roughness: 1, flatShading: true }),
    new THREE.MeshStandardMaterial({ color: 0x6d4a32, roughness: 1, flatShading: true }),
    new THREE.MeshStandardMaterial({ color: 0x55402f, roughness: 1, flatShading: true }),
  ];
  const blockers = [];   // [e, n, radius] areas the hero cannot enter
  // hedge between two E/N points
  function hedge(a, b, h, mix = .5, snow = true) {
    const len = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.ceil(len / .55);
    const geos = [[], [], []], snowG = [];
    for (let i = 0; i <= n; i++) {
      const t = i / n, e = a[0] + (b[0] - a[0]) * t, nn = a[1] + (b[1] - a[1]) * t;
      blockers.push([e, nn, .75]);
      for (let k = 0; k < 2; k++) {
        const r = rr(.45, .7), g = noisySphere(r, 1, .35);
        g.scale(1, h / 1.2, 1);
        g.translate(e + rr(-.2, .2), h * rr(.45, .7) + H(e, nn), -nn + rr(-.3, .3));
        geos[rnd() < mix ? (rnd() < .5 ? 1 : 2) : 0].push(g);
        if (snow && rnd() < .5) { const s = noisySphere(r * .55, 1, .3); s.scale(1, .25, 1); s.translate(e + rr(-.2, .2), h * 1.02 + H(e, nn), -nn + rr(-.2, .2)); snowG.push(s); }
      }
    }
    geos.forEach((arr, i) => { if (arr.length) { const m = new THREE.Mesh(mergeGeometries(arr), hedgeMats[i]); m.castShadow = m.receiveShadow = true; scene.add(m); } });
    if (snowG.length) { const m = new THREE.Mesh(mergeGeometries(snowG), M.snow); m.receiveShadow = true; scene.add(m); }
  }
  const off = (p, d) => [p[0] + SWN[0] * d, p[1] + SWN[1] * d];
  hedge(off([5.2, 13.6], 1.0), off([.8, 20.6], 1.0), .8, .45);       // in front of the sign wing
  hedge([13.3, 11.1], [17.1, 13.4], .8, .3);                           // along the tall building
  // evergreen bush beside the entrance
  {
    const arr = [];
    for (const [y, r] of [[.6, .75], [1.35, .7], [2.0, .52], [2.5, .3]]) { const g = noisySphere(r, 2, .25); g.translate(8.7, y + H(8.7, 9.7), -9.7); arr.push(g); }
    const m = new THREE.Mesh(mergeGeometries(arr), new THREE.MeshStandardMaterial({ color: 0x2f4a26, roughness: 1, flatShading: true }));
    m.castShadow = true; scene.add(m); blockers.push([8.7, 9.7, 1.0]);
    const s = noisySphere(.28, 1, .3); s.scale(1, .4, 1); add(new THREE.Mesh(s, M.snow), 8.75, 2.75, -9.7);
  }
  // raised hedge bed on the right (NW of the camera)
  {
    const pts = [[2, 26], [6, 29.5], [11, 32], [17, 34]];
    for (let i = 0; i < pts.length - 1; i++) {
      const [ax, an] = pts[i], [bx, bn] = pts[i + 1], len = Math.hypot(bx - ax, bn - an);
      const wall = box(.35, .55, len, M.conc); wall.position.copy(W3((ax + bx) / 2, (an + bn) / 2, .27));
      wall.rotation.y = Math.atan2(bx - ax, -(bn - an)); scene.add(wall);
    }
    hedge([2.5, 27.5], [16, 35.5], 1.9, .55);
    hedge([0, 30], [12, 37], 2.0, .5);
  }
  // bare trees (procedural, merged)
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
  [[-6, 27, 15], [-15, 24, 14], [-24, 19, 16], [-3, 36, 15], [-14, 33, 13], [12, 38.5, 17], [-30, 28, 14], [22, 41, 14], [-8, 46, 16]]
    .forEach(([e, n, h], i) => tree(e, n, h, .75, i));
  seed = 4242;
  
  // ---------- street furniture
  {
    const g = new THREE.Group();
    add(box(1.8, .08, .45, M.wood), 0, .45, 0, g);
    for (const x of [-.75, .75]) add(box(.08, .45, .4, M.dark), x, .22, 0, g);
    g.position.copy(W3(11.8, 11.4)); g.rotation.y = .544; g.name = 'bench'; scene.add(g);
    blockers.push([11.8, 11.4, 1.0]);
  }
  add(new THREE.Mesh(new THREE.CylinderGeometry(.28, .26, .8, 20), M.metal), 7.4, .4, -12.2).castShadow = true;
  blockers.push([7.4, 12.2, .45]);
  const lampLights = [];
  function lamp(e, n, h, rot = 0) {
    const g = new THREE.Group();
    add(new THREE.Mesh(new THREE.CylinderGeometry(.07, .1, h, 12), M.metal), 0, h / 2, 0, g).castShadow = true;
    add(box(1.1, .12, .35, M.metal), -.45, h, 0, g);
    const bulb = add(new THREE.Mesh(new THREE.BoxGeometry(.8, .03, .25), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffd9a0, emissiveIntensity: 0 })), -.45, h - .07, 0, g);
    const L = new THREE.SpotLight(0xffc888, 0, 18, .95, .6, 1.4); L.position.set(-.45, h - .1, 0);
    L.target.position.set(-.45, 0, 0); g.add(L, L.target);
    lampLights.push({ L, bulb });
    g.position.copy(W3(e, n)); g.rotation.y = rot; scene.add(g);
    blockers.push([e, n, .4]);
  }
  lamp(18, 30.5, 6.5, -.6);
  lamp(26, 14, 5.5, 2.2);
  add(new THREE.Mesh(new THREE.CylinderGeometry(.05, .07, 10, 10), M.metal), 18.5, 5, -15).castShadow = true;
  blockers.push([18.5, 15, .3]);
  
  return { scene, H, W3, camVec, blockers, footprints, M, box, plane, add, canvasTex, onWall, EW1, EW2, EWN, SW1, SW2, SWN, GB, skyTex, hemi, sun, wallUniforms, lampLights, canLamp, porchLight, ground };
}
