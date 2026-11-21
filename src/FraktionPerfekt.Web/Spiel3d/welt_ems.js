// Erzeugt aus dem 3D-Prototyp (ems.html) mit gen_welt.py – Außenschauplatz (Gebäude: LoD2, Geobasis NRW, dl-de/zero-2-0).
import * as THREE from './three_min.js';
import { mergeGeometries } from './buffergeometry.js';
import { RoomEnvironment } from './roomenv.js';
import { fotoMaterial, textur } from './texturen3d.js';

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
  sun.position.set(40, 38, 10);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -60, right: 60, top: 60, bottom: -60, near: 1, far: 200 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = .03;
  scene.add(sun);
  
  // ---------- Emsufer: river, banks, dike paths and the cycle bridge with the yellow lamps
  const W3 = (e, n, y = 0) => new THREE.Vector3(e, y + H(e, n), -n);
  const blockers = [];
  const footprints = [];
  const LOD2 = [];
  const wallUniforms = { uNight: { value: 0 } };
  const riverX = n => 10 * Math.sin(n / 55) + 4 * Math.sin(n / 23);
  // deck height of the bridge (gentle arch, ramps down to the dikes)
  const BL = 48, BR = 62;
  function deckY(e) {
    const a = Math.abs(e);
    if (a <= BL) return 3.4 + 1.8 * (1 - (e / BL) ** 2);
    const t = Math.min(1, (a - BL) / (BR - BL)), g = H(Math.sign(e) * BR, 0);
    return 3.4 + (g - 3.4) * t;
  }
  const onBridge = (e, n) => Math.abs(n) < 2.1 && Math.abs(e) < BR;
  const walkH = (e, n) => onBridge(e, n) ? deckY(e) : H(e, n);
  
  // ---------- ground: meadow, gravel paths on the dikes, the water
  const grassTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#6f8040'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 7000; i++) { g.fillStyle = rnd() < .5 ? '#7f9048' : (rnd() < .6 ? '#5d6e34' : '#97904f'); g.fillRect(rnd() * w, rnd() * h, 1, 3); }
  });
  const gravelTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#b5a68a'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 6000; i++) { const v = 140 + rnd() * 70; g.fillStyle = `rgb(${v},${v - 12},${v - 30})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
  });
  const asphaltTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#55575b'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 4000; i++) { const v = 70 + rnd() * 40; g.fillStyle = `rgb(${v},${v},${v + 3})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
  });
  // Fototexturen (Poly Haven, CC0), siehe texturen3d.js
  const ground = terrainMesh(fotoMaterial('rasen'), textur('rasen').meter);
  const gravelMat = fotoMaterial('kies');
  // dike-top paths on both sides, following the river
  for (const side of [-1, 1]) {
    const pos = [], uv = [];
    for (let n = -160; n < 160; n += 2) {
      const c0 = riverX(n) + side * 48, c1 = riverX(n + 2) + side * 48;
      const v = (e, nn) => { pos.push(e, H(e, nn) + .04, -nn); uv.push(e / 2, nn / 2); };
      v(c0 - 1.6, n); v(c0 + 1.6, n); v(c1 + 1.6, n + 2); v(c0 - 1.6, n); v(c1 + 1.6, n + 2); v(c1 - 1.6, n + 2);
    }
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); g.computeVertexNormals();
    const m = new THREE.Mesh(g, gravelMat); m.receiveShadow = true; m.name = 'ground'; scene.add(m);
  }
  // water surface with a gentle shimmer
  const waterTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#3f5f5c'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 260; i++) { g.strokeStyle = `rgba(200,225,220,${.05 + rnd() * .12})`; g.lineWidth = 1 + rnd() * 2; const x = rnd() * w, y = rnd() * h; g.beginPath(); g.moveTo(x, y); g.lineTo(x + 10 + rnd() * 30, y + rnd() * 2); g.stroke(); }
  });
  const waterMat = new THREE.MeshStandardMaterial({ map: waterTex, color: 0x9fb9b4, roughness: .12, metalness: .35, transparent: true, opacity: .92 });
  {
    const g = new THREE.PlaneGeometry(60, 360, 1, 120); g.rotateX(-Math.PI / 2);
    const p = g.attributes.position;
    for (let i = 0; i < p.count; i++) { const n = -p.getZ(i); p.setX(i, p.getX(i) + riverX(n)); p.setY(i, -2.35); }
    waterTex.repeat.set(4, 30);
    const m = new THREE.Mesh(g, waterMat); m.receiveShadow = true; scene.add(m);
  }
  
  // ---------- the cycle/foot bridge
  const bridge = new THREE.Group(); scene.add(bridge);
  const yellowMat = new THREE.MeshStandardMaterial({ color: 0xd8b13a, roughness: .55, metalness: .35 });
  const railMat = new THREE.MeshStandardMaterial({ color: 0x2c3138, roughness: .5, metalness: .6 });
  const concMat = new THREE.MeshStandardMaterial({ color: 0xa8a59f, roughness: .9 });
  {
    // deck: asphalt strip with a centre line, concrete edge beams underneath
    const pos = [], uv = [], edge = [], line = [];
    for (let e = -BR; e < BR; e += 1) {
      const y0 = deckY(e), y1 = deckY(e + 1);
      pos.push(e, y0, 2, e + 1, y1, 2, e + 1, y1, -2, e, y0, 2, e + 1, y1, -2, e, y0, -2);
      uv.push(e / 2, 1, (e + 1) / 2, 1, (e + 1) / 2, -1, e / 2, 1, (e + 1) / 2, -1, e / 2, -1);
      line.push(e, y0 + .01, .09, e + 1, y1 + .01, .09, e + 1, y1 + .01, -.09, e, y0 + .01, .09, e + 1, y1 + .01, -.09, e, y0 + .01, -.09);
      // concrete girder with sloping sides: wide under the deck, narrower at the bottom
      const q = (x0, ya, za, x1, yb, zb, yc, zc, yd, zd) => edge.push(x0, ya, za, x1, yb, zb, x1, yc, zc, x0, ya, za, x1, yc, zc, x0, yd, zd);
      for (const s of [1, -1]) {
        q(e, y0 + .15, s * 2.35, e + 1, y1 + .15, s * 2.35, y1 - .25, s * 2.35, y0 - .25, s * 2.35);           // edge cap
        q(e, y0 - .25, s * 2.35, e + 1, y1 - .25, s * 2.35, y1 - 1.35, s * 1.3, y0 - 1.35, s * 1.3);         // sloping web
        edge.push(e, y0 + .15, s * 2.35, e + 1, y1 + .15, s * 2.35, e + 1, y1 + .15, s * 2.05, e, y0 + .15, s * 2.35, e + 1, y1 + .15, s * 2.05, e, y0 + .15, s * 2.05);
      }
      edge.push(e, y0 - 1.35, 1.3, e + 1, y1 - 1.35, 1.3, e + 1, y1 - 1.35, -1.3, e, y0 - 1.35, 1.3, e + 1, y1 - 1.35, -1.3, e, y0 - 1.35, -1.3);
    }
    const mk = (p, u) => { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(p, 3)); if (u) g.setAttribute('uv', new THREE.Float32BufferAttribute(u, 2)); g.computeVertexNormals(); return g; };
    const deck = new THREE.Mesh(mk(pos, uv), fotoMaterial('asphalt', { side: THREE.DoubleSide }));
    deck.receiveShadow = true; deck.name = 'ground'; bridge.add(deck);
    bridge.add(new THREE.Mesh(mk(line), new THREE.MeshStandardMaterial({ color: 0xbdbab2, roughness: .8 })));
    const em = new THREE.Mesh(mk(edge), new THREE.MeshStandardMaterial({ color: 0xb9b6ae, roughness: .9, side: THREE.DoubleSide })); em.castShadow = true; em.receiveShadow = true; bridge.add(em);
    // round piers with wide trapezoidal caps
    for (const e of [-40, -20, 0, 20, 40]) {
      const top = deckY(e) - 1.35, bot = H(e, 0) - 1.5, capH = 1.1, h = top - capH - bot;
      const col = new THREE.Mesh(new THREE.CylinderGeometry(.65, .65, h, 24), concMat); col.position.set(e, bot + h / 2, 0); col.castShadow = true; bridge.add(col);
      const cg = new THREE.CylinderGeometry(1.0, .7, capH, 4, 1); cg.rotateY(Math.PI / 4); cg.scale(1.1, 1, 2.4);
      const cap = new THREE.Mesh(cg, concMat); cap.position.set(e, top - capH / 2, 0); cap.castShadow = true; bridge.add(cap);
    }
    // railings: dark vertical bars, top rail and ball finials on the posts
    const geos = [], balls = [];
    for (const z of [2.2, -2.2]) {
      for (let e = -BR; e < BR; e += 1) {
        const y0 = deckY(e) + .15, y1 = deckY(e + 1) + .15;
        const r = new THREE.BoxGeometry(1.02, .07, .07); r.rotateZ(Math.atan2(y1 - y0, 1)); r.translate(e + .5, (y0 + y1) / 2 + 1.1, z); geos.push(r);
        const r2 = new THREE.BoxGeometry(1.02, .05, .05); r2.rotateZ(Math.atan2(y1 - y0, 1)); r2.translate(e + .5, (y0 + y1) / 2 + .12, z); geos.push(r2);
        for (let k = 0; k < 8; k++) { const x = e + k / 8, y = deckY(x) + .15; const b = new THREE.BoxGeometry(.03, 1.0, .03); b.translate(x, y + .6, z); geos.push(b); }
        if (Math.round(e) % 3 === 0) { const p = new THREE.BoxGeometry(.09, 1.2, .09); p.translate(e, deckY(e) + .75, z); geos.push(p); const s = new THREE.SphereGeometry(.09, 10, 8); s.translate(e, deckY(e) + 1.42, z); balls.push(s); }
      }
    }
    const rm = new THREE.Mesh(mergeGeometries(geos), railMat); rm.castShadow = true; bridge.add(rm);
    bridge.add(new THREE.Mesh(mergeGeometries(balls), railMat));
  }
  // the iconic lamps: slim yellow square masts with paired pyramid-shaped lamp heads
  const lampLights = [];
  function bridgeLamp(e, side) {
    const g = new THREE.Group(); g.name = 'blamp';
    const y = deckY(e) + .15, h = 7.2;
    add(box(.2, h, .2, yellowMat), 0, h / 2, 0, g);
    add(box(.16, h - .6, .16, yellowMat), .3, (h - .6) / 2, 0, g);
    add(new THREE.Mesh(new THREE.CylinderGeometry(.07, .07, .12, 12), new THREE.MeshStandardMaterial({ color: 0xbbbbbb, metalness: .7, roughness: .3 })), 0, h + .06, 0, g);
    // box head on the mast and the twin pyramids on an arm reaching over the path
    add(box(.65, .4, .5, yellowMat), 0, h - 1.4, 0, g);
    add(box(1.2, .1, .1, yellowMat), 0, h - 1.4, -side * .7, g).rotation.y = Math.PI / 2;
    const pyr = (yy, s) => { const m = new THREE.Mesh(new THREE.ConeGeometry(.75 * s, .32, 4, 1), yellowMat); m.rotation.y = Math.PI / 4; m.position.set(0, yy, -side * 1.3); g.add(m); const lens = new THREE.Mesh(new THREE.BoxGeometry(.95 * s, .06, .95 * s), new THREE.MeshStandardMaterial({ color: 0xf7f1dc, emissive: 0xffe0a0, emissiveIntensity: 0 })); lens.position.set(0, yy - .19, -side * 1.3); g.add(lens); return lens; };
    const l1 = pyr(h - 1.1, 1), l2 = pyr(h - 1.55, .85);
    const L = new THREE.PointLight(0xffd9a0, 0, 16, 1.5); L.position.set(0, h - 1.9, -side * 1.3); g.add(L);
    lampLights.push({ L, bulb: { material: { set emissiveIntensity(v) { l1.material.emissiveIntensity = v; l2.material.emissiveIntensity = v; } } } });
    g.position.set(e, y, side * 2.45); g.traverse(o => { if (o.isMesh) o.castShadow = true; }); scene.add(g);
  }
  for (let e = -50; e <= 50; e += 20) { bridgeLamp(e, 1); bridgeLamp(e, -1); }
  // traffic sign at the west end (shared foot/cycle path)
  {
    const sTex = canvasTex(128, 128, (g, w, h) => {
      g.fillStyle = '#1f5fbf'; g.beginPath(); g.arc(64, 64, 62, 0, 7); g.fill(); g.strokeStyle = '#fff'; g.lineWidth = 5; g.beginPath(); g.arc(64, 64, 58, 0, 7); g.stroke();
      g.fillStyle = '#fff'; g.fillRect(14, 62, 100, 4);
      g.beginPath(); g.arc(56, 28, 6, 0, 7); g.fill(); g.fillRect(52, 36, 9, 22); g.beginPath(); g.arc(74, 38, 4, 0, 7); g.fill(); g.fillRect(71, 43, 6, 14);
      g.strokeStyle = '#fff'; g.lineWidth = 4; for (const x of [44, 84]) { g.beginPath(); g.arc(x, 92, 12, 0, 7); g.stroke(); } g.beginPath(); g.moveTo(44, 92); g.lineTo(60, 76); g.lineTo(84, 92); g.moveTo(60, 76); g.lineTo(76, 76); g.stroke();
    });
    const s = new THREE.Mesh(new THREE.CircleGeometry(.32, 32), new THREE.MeshStandardMaterial({ map: sTex, roughness: .5 }));
    s.position.set(-BR + 2.2, deckY(-BR + 2.2) + 2.3, -2.62); s.rotation.y = -Math.PI / 2; s.name = 'zsign'; scene.add(s);
  }
  
  // ---------- vegetation along the banks
  function tree(e, n, h, spread, seedOff, leafy) {
    seed = 777 + seedOff;
    const geos = [], tips = [], up = new THREE.Vector3(0, 1, 0);
    function br(p, dir, len, rad, depth) {
      const g = new THREE.CylinderGeometry(rad * .7, rad, len, 5, 1, true);
      g.translate(0, len / 2, 0); g.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(up, dir)); g.translate(p.x, p.y, p.z); geos.push(g);
      const end = p.clone().addScaledVector(dir, len);
      if (depth === 0) { tips.push(end); return; }
      const k = depth > 3 ? 2 : 3;
      for (let i = 0; i < k; i++) { const d = dir.clone().add(new THREE.Vector3(rr(-spread, spread), rr(.1, .5), rr(-spread, spread))).normalize(); br(end, d, len * rr(.62, .78), rad * .62, depth - 1); }
    }
    br(W3(e, n), new THREE.Vector3(0, 1, 0), h * .34, h * .03, 5);
    const m = new THREE.Mesh(mergeGeometries(geos), M.bark); m.castShadow = true; scene.add(m);
    if (leafy) {
      const lg = []; for (const t of tips) if (rnd() < .55) { const s = new THREE.IcosahedronGeometry(rr(.9, 1.6), 1); s.translate(t.x, t.y, t.z); lg.push(s); }
      const lm = new THREE.Mesh(mergeGeometries(lg), new THREE.MeshStandardMaterial({ color: 0x4f7a32, roughness: 1, flatShading: true })); lm.castShadow = true; scene.add(lm);
    }
    blockers.push([e, n, .7]);
  }
  for (let n = -150; n < 150; n += 14) for (const side of [-1, 1]) {
    if (Math.abs(n) < 10) continue;
    const e = riverX(n) + side * (24 + rnd() * 10);
    tree(e, n + rnd() * 6, 11 + rnd() * 7, .75, n * 3 + side, rnd() < .6);
  }
  // reeds at the water's edge
  {
    const geos = [];
    for (let i = 0; i < 900; i++) {
      const n = -160 + rnd() * 320, side = rnd() < .5 ? -1 : 1, e = riverX(n) + side * (10.5 + rnd() * 2.5);
      const h = .8 + rnd() * .9, g = new THREE.ConeGeometry(.03, h, 3); g.translate(e, H(e, n) + h / 2, -n); geos.push(g);
    }
    scene.add(new THREE.Mesh(mergeGeometries(geos), new THREE.MeshStandardMaterial({ color: 0x8a8a4a, roughness: 1 })));
  }
  // benches on the dike
  for (const [e, n] of [[-50, 12], [50, -14]]) {
    const g = new THREE.Group(); g.name = 'bench';
    add(box(1.8, .08, .45, M.wood), 0, .45, 0, g);
    for (const x of [-.75, .75]) add(box(.08, .45, .4, M.dark), x, .22, 0, g);
    g.position.copy(W3(e + riverX(n), n)); g.rotation.y = Math.PI / 2; scene.add(g); blockers.push([e + riverX(n), n, 1]);
  }
  const GB = new THREE.Group(); scene.add(GB);
  const canLamp = new THREE.Mesh(new THREE.SphereGeometry(.01), new THREE.MeshBasicMaterial({ visible: false })); scene.add(canLamp);
  const porchLight = new THREE.PointLight(0xffd9a8, 0, 1, 1); scene.add(porchLight);
  function insideFootprint() { return false; }
  
  return { scene, H, W3, camVec, blockers, M, box, plane, add, canvasTex, riverX, deckY, onBridge, walkH, BL, BR, ground, sun, bridge };
}
