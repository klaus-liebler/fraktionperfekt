// Erzeugt aus dem 3D-Prototyp (bad.html) mit gen_welt.py – Außenschauplatz (Gebäude: LoD2, Geobasis NRW, dl-de/zero-2-0).
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
  sun.position.set(30, 40, -35);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -60, right: 60, top: 60, bottom: -60, near: 1, far: 200 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = .03;
  scene.add(sun);
  
  // ---------- real-world helpers (E = east, N = north, metres from the Hallenbad centre)
  const W3 = (e, n, y = 0) => new THREE.Vector3(e, y + H(e, n), -n);
  function onWall(obj, p1, p2, t, y, nrm, off = .06) {
    const e = p1[0] + (p2[0] - p1[0]) * t, n = p1[1] + (p2[1] - p1[1]) * t;
    obj.position.set(e + nrm[0] * off, y + H(e, n), -(n + nrm[1] * off));
    obj.rotation.y = Math.atan2(nrm[0], -nrm[1]);
    return obj;
  }
  const blockers = [];
  
  // ---------- ground: lawn, paved forecourt, paths, running track
  const grassTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#6f7d40'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 6000; i++) { g.fillStyle = rnd() < .5 ? '#7f8c48' : (rnd() < .6 ? '#5e6c36' : '#8c7d52'); g.fillRect(rnd() * w, rnd() * h, 1, 3); }
  });
  const greyPaveTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#6d6560'; g.fillRect(0, 0, w, h);
    for (let r = 0; r < 16; r++) for (let c = -1; c < 9; c++) {
      const x = c * 32 + (r % 2 ? 16 : 0), y = r * 16; g.fillStyle = jitter([150, 138, 128], .18); g.fillRect(x + 1, y + 1, 30, 14);
    }
  });
  const trackTex = canvasTex(256, 64, (g, w, h) => {
    g.fillStyle = '#b5523e'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 1500; i++) { g.fillStyle = `rgba(0,0,0,${rnd() * .12})`; g.fillRect(rnd() * w, rnd() * h, 1, 1); }
    g.fillStyle = '#f2f2f2'; for (let y = 0; y < h; y += 16) g.fillRect(0, y, w, 1);
  });
  // Fototexturen (Poly Haven, CC0), siehe texturen3d.js
  const ground = terrainMesh(fotoMaterial('rasen'), textur('rasen').meter);
  function flatPoly(pts, mat, y, tile, name = 'ground') {
    // grid of 1.5 m cells clipped to the polygon, laid onto the terrain
    const inside = (e, n) => { let c = false; for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) { const [xi, yi] = pts[i], [xj, yj] = pts[j]; if ((yi > n) !== (yj > n) && e < (xj - xi) * (n - yi) / (yj - yi) + xi) c = !c; } return c; };
    const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]), st = 1.5, pos = [], uv = [];
    for (let e = Math.min(...xs); e < Math.max(...xs); e += st) for (let n = Math.min(...ys); n < Math.max(...ys); n += st) {
      if (!inside(e + st / 2, n + st / 2)) continue;
      const v = (a, b) => { pos.push(a, H(a, b) + y + .02, -b); uv.push(a / tile, b / tile); };
      v(e, n); v(e + st, n); v(e + st, n + st); v(e, n); v(e + st, n + st); v(e, n + st);
    }
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2)); g.computeVertexNormals();
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true; m.name = name; scene.add(m); return m;
  }
  const paveMat = fotoMaterial('pflaster');   // graues Fischgrätpflaster
  flatPoly([[-30, -10.5], [-18.6, -8.0], [-16.6, -15], [-12, -26], [-24, -36], [-36, -30]], paveMat, .01, 2);   // forecourt at the entrance
  flatPoly([[-12, 26], [20, 33], [19, 36], [-13, 29]], paveMat, .012, 2);            // path along the lawn side
  flatPoly([[18, 44], [22, 44], [44, 90], [40, 90]], paveMat, .012, 2);          // path to the north-east
  flatPoly([[30, -10], [34, -10], [50, -60], [46, -60]], paveMat, .012, 2);      // path along the lawn
  // running track (stadium) to the west
  {
    const g = new THREE.RingGeometry(36.5, 46, 64, 1);
    const p = g.attributes.position;
    for (let i = 0; i < p.count; i++) { const x = p.getX(i); p.setX(i, x + Math.sign(x) * 42); }
    g.rotateX(-Math.PI / 2);
    const m = new THREE.Mesh(g, new THREE.MeshStandardMaterial({ map: trackTex, roughness: .9 })); m.position.set(-80, 0, 72); m.rotation.y = 1.33; m.receiveShadow = true; scene.add(m); drape(m, .05);
  }
  
  // ---------- buildings from LoD2 (Geobasis NRW); the pool gets its own facade
  const LOD2 = DATEN.lod2;
  const wallUniforms = { uNight: { value: 0 } };
  function wallMaterial(kind) {   // 0 brick houses, 1 Hallenbad (white base, glass band, yellow panels)
    const m = new THREE.MeshStandardMaterial({ map: brickTex, roughness: .85, side: THREE.DoubleSide });
    m.onBeforeCompile = sh => {
      sh.uniforms.uBackstein = { value: textur('backstein').map }; sh.uniforms.uKachelB = { value: textur('backstein').meter };
      sh.uniforms.uKind = { value: kind };
      sh.uniforms.uNight = wallUniforms.uNight;
      sh.vertexShader = sh.vertexShader
        .replace('#include <common>', '#include <common>\nattribute vec3 wattr;\nattribute float bofs;\nvarying vec3 vW;\nvarying float vH;')
        .replace('#include <begin_vertex>', '#include <begin_vertex>\nvW = wattr; vH = position.y - bofs;');
      sh.fragmentShader = sh.fragmentShader
        .replace('#include <common>', '#include <common>\nuniform sampler2D uBackstein; uniform float uKachelB;')
        .replace('#include <common>', '#include <common>\nuniform float uKind; uniform float uNight; varying vec3 vW; varying float vH;')
        .replace('#include <map_fragment>', `
  vec4 col; float topH = vH + vW.y; bool glow = false;
  if (uKind < .5) {
    col = texture2D(uBackstein, vec2(vW.x, vH) / uKachelB);
    if (vW.z > 3.5 && topH > 2.6) {
      float per = 2.7, sh = mod(vW.z, per) * .5, lx = mod(vW.x - sh, per) / per, ly = mod(vH - .9, 2.8), wh = 1.45;
      bool fits = vH > .9 && vW.x > sh && vW.x < vW.z - sh && vW.y > (wh - ly) + .25;
      if (fits && lx > .2 && lx < .8 && ly < wh) {
        bool fr = lx < .24 || lx > .76 || ly < .07 || ly > wh - .07;
        col = vec4(fr ? vec3(.85) : vec3(.12, .15, .18), 1.);
      }
    }
  } else {
    vec3 white = vec3(.9, .9, .88);
    col = vec4(white * (.96 + .04 * step(.5, fract(vH / 1.2))), 1.);
    float px = mod(vW.x, 1.35), tall = step(9.0, topH);
    float yb = tall > .5 ? 2.6 : 1.1;            // depth of the yellow panel band below the roof edge
    float gb = tall > .5 ? 6.6 : 3.2;            // bottom of the glass band (measured from the top)
    if (vW.y < .25) col = vec4(.16, .17, .2, 1.);                                   // dark roof edge
    else if (vW.y < yb) {
      bool grid = px < .06 || mod(vW.y - .25, 1.15) < .06;
      col = vec4(grid ? vec3(.2, .2, .22) : vec3(.95, .74, .16) * (.9 + .1 * fract(sin(floor(vW.x / 1.35) * 7.1) * 43.7)), 1.);
    } else if (vW.y < gb && vW.z > 3.0) {
      bool grid = px < .07 || mod(vW.y - yb, 1.3) < .07;
      col = vec4(grid ? vec3(.12, .13, .15) : mix(vec3(.12, .2, .3), vec3(.42, .52, .6), fract(vW.y / 4.0)), 1.);
      glow = !grid;
    } else if (vH > .9 && vH < 2.0 && vW.z > 6.0 && mod(vW.x, 5.0) < 2.6 && vW.x > 1.0 && vW.x < vW.z - 1.0) {
      bool fr = mod(vW.x, 5.0) < .08 || mod(vW.x, 5.0) > 2.52 || vH < .97 || vH > 1.93;
      col = vec4(fr ? vec3(.15) : vec3(.1, .14, .18), 1.);
    }
    if (vH < .35) col.rgb *= .8;                                                    // plinth
  }
  if (glow) totalEmissiveRadiance += vec3(.55, .85, 1.0) * uNight * .9;
  diffuseColor *= col;`);
    };
    return m;
  }
  const mats = [wallMaterial(0), wallMaterial(1)];
  const roofMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .85, side: THREE.DoubleSide, flatShading: true });
  const footprints = [];
  {
    const walls = [[], []], roofs = [];
    const mk = (pos, attr, o = 0) => {
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
      if (attr) { g.setAttribute('wattr', new THREE.Float32BufferAttribute(attr, 3)); g.setAttribute('bofs', new THREE.Float32BufferAttribute(new Float32Array(pos.length / 3).fill(o), 1)); }
      g.computeVertexNormals(); return g;
    };
    LOD2.forEach((b, bi) => {
      seed = 31 + bi * 7;
      if (b.w.length) walls[b.s ? 1 : 0].push(mk(b.w, b.a, b.o));
      if (b.r.length) {
        const g = mk(b.r), p = g.attributes.position, cols = [];
        const tile = rnd() < .5 ? [.36, .15, .1] : [.17, .17, .19];
        const v = new THREE.Vector3(), a = new THREE.Vector3(), c = new THREE.Vector3();
        for (let i = 0; i < p.count; i += 3) {
          v.fromBufferAttribute(p, i); a.fromBufferAttribute(p, i + 1); c.fromBufferAttribute(p, i + 2);
          const n = a.clone().sub(v).cross(c.clone().sub(v)).normalize();
          const flat = Math.abs(n.y) > .97, k = .9 + rnd() * .2;
          const col = (flat || b.s) ? [.34 * k, .34 * k, .33 * k] : tile.map(x => x * k);
          for (let j = 0; j < 3; j++) cols.push(...col);
        }
        g.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3)); roofs.push(g);
      }
      for (const t of b.g) footprints.push(t);
    });
    walls.forEach((arr, k) => { if (arr.length) { const m = new THREE.Mesh(mergeGeometries(arr), mats[k]); m.castShadow = m.receiveShadow = true; m.name = k ? 'pool' : 'lod2'; scene.add(m); } });
    const rm = new THREE.Mesh(mergeGeometries(roofs), roofMat); rm.castShadow = rm.receiveShadow = true; scene.add(rm);
  }
  // roof overhang along the entrance side (LoD2 overhang structure)
  {
    const N1 = [-14.9, 18.7], N2 = [22.6, 27.3], L = Math.hypot(N2[0] - N1[0], N2[1] - N1[1]);
    const g = new THREE.Group();
    add(box(L, .35, 1.6, new THREE.MeshStandardMaterial({ color: 0x2a2c30, roughness: .7 })), 0, 0, 0, g);
    add(box(L, .5, .08, new THREE.MeshStandardMaterial({ color: 0xf2bf2a, roughness: .6 })), 0, -.3, .8, g);
    onWall(g, N1, N2, .5, 4.4, [-.225, .974], .8); scene.add(g);
  }
  
  // ---------- entrance pavilion: mural wall, canopy "EINGANG HALLENBAD", doors
  const PV1 = [-15.2, 20.1], PV2 = [-18.3, 32.5], PVN = [.97, .243];
  const muralTex = canvasTex(1024, 320, (g, w, h) => {
    const sky = g.createLinearGradient(0, 0, 0, h); sky.addColorStop(0, '#6fb4ea'); sky.addColorStop(.55, '#bfe2f7'); sky.addColorStop(.56, '#1f6fb8'); sky.addColorStop(1, '#0c3f7a');
    g.fillStyle = sky; g.fillRect(0, 0, w, h);
    g.fillStyle = 'rgba(255,255,255,.9)';
    for (let i = 0; i < 9; i++) { const x = 380 + rnd() * 600, y = 40 + rnd() * 90; for (let k = 0; k < 4; k++) { g.beginPath(); g.ellipse(x + k * 22, y + rnd() * 6, 30, 12, 0, 0, 7); g.fill(); } }
    g.fillStyle = '#ffd64a'; g.beginPath(); g.arc(120, 70, 42, 0, 7); g.fill();
    // waves
    g.strokeStyle = 'rgba(255,255,255,.7)'; g.lineWidth = 4;
    for (let y = 190; y < h; y += 28) { g.beginPath(); for (let x = 0; x <= w; x += 10) g.lineTo(x, y + Math.sin(x / 30 + y) * 5); g.stroke(); }
    // a stylised slide tube and a splash
    g.strokeStyle = '#2b8fd6'; g.lineWidth = 46; g.lineCap = 'round';
    g.beginPath(); g.moveTo(40, 120); g.bezierCurveTo(200, 120, 220, 260, 330, 270); g.stroke();
    g.strokeStyle = 'rgba(255,255,255,.6)'; g.lineWidth = 10; g.beginPath(); g.moveTo(40, 104); g.bezierCurveTo(200, 104, 220, 244, 330, 252); g.stroke();
    g.fillStyle = '#ffffff'; for (let i = 0; i < 26; i++) { g.beginPath(); g.arc(340 + rnd() * 70, 230 + rnd() * 60, 3 + rnd() * 7, 0, 7); g.fill(); }
    // fish and bubbles
    for (let i = 0; i < 6; i++) {
      const x = 520 + rnd() * 440, y = 230 + rnd() * 70; g.fillStyle = ['#ff8a3d', '#ffd23f', '#ff5d73'][i % 3];
      g.beginPath(); g.ellipse(x, y, 22, 11, 0, 0, 7); g.fill(); g.beginPath(); g.moveTo(x - 20, y); g.lineTo(x - 36, y - 10); g.lineTo(x - 36, y + 10); g.fill();
    }
    g.fillStyle = '#ffffff'; g.font = 'bold 54px Arial, sans-serif'; g.fillText('HALLENBAD', 560, 80);
  });
  const signTex2 = canvasTex(768, 96, (g, w, h) => {
    g.fillStyle = '#cfe6f6'; g.fillRect(0, 0, w, h);
    g.fillStyle = '#123a66'; g.font = '600 52px Arial, sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle';
    g.fillText('EINGANG      HALLENBAD', w / 2, h / 2 + 3);
  });
  // entrance at the south end of the west wing: mural on the wall, canopy "EINGANG HALLENBAD" and doors at the corner to the hall
  const MW1 = [-25.5, -9.9], MW2 = [-19.1, -8.3], MWN = [.243, -.97], MROT = Math.atan2(.243, .97);
  const EW1 = [-18.5, -8.1], EW2 = [-17.1, -13.8], EWN = [-.971, -.239];
  const sfp = (t, d) => [MW1[0] + (MW2[0] - MW1[0]) * t + MWN[0] * d, MW1[1] + (MW2[1] - MW1[1]) * t + MWN[1] * d];
  {
    const mural = plane(3.9, 3.2, new THREE.MeshStandardMaterial({ map: muralTex, roughness: .8 })); mural.name = 'mural';
    const [e, n] = sfp(.31, .06); mural.position.copy(W3(e, n, 1.75)); mural.rotation.y = MROT; scene.add(mural);
  }
  const doorGroup = new THREE.Group(); doorGroup.name = 'door';
  {
    // doors in the same wall as the mural, under a blue canopy with the sign
    const [ce, cn] = sfp(.8, 1.45);
    const cg = new THREE.Group();
    add(box(3.2, .62, 2.9, new THREE.MeshStandardMaterial({ color: 0x9cc7e8, roughness: .7 })), 0, 3.3, 0, cg);
    const s = plane(3.1, .55, new THREE.MeshStandardMaterial({ map: signTex2, roughness: .6 })); s.position.set(0, 3.3, 1.46); s.name = 'esign'; cg.add(s);
    for (const x of [-1.45, 1.45]) add(box(.35, 3.0, .35, new THREE.MeshStandardMaterial({ color: 0x3f7fc0, roughness: .6 })), x, 1.5, -1.2, cg);
    const dl = add(new THREE.Mesh(new THREE.CylinderGeometry(.12, .12, .04, 16), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0xffe2a8, emissiveIntensity: .4 })), 0, 2.97, .3, cg); dl.name = 'canLamp';
    cg.position.copy(W3(ce, cn)); cg.rotation.y = MROT; scene.add(cg);
    add(box(2.2, 2.5, .1, M.dark), 0, 1.25, 0, doorGroup);
    for (const x of [-.53, .53]) { add(plane(.95, 2.3, M.glass), x, 1.17, .06, doorGroup); add(box(.05, .6, .06, new THREE.MeshStandardMaterial({ color: 0xf2c12e })), x > 0 ? .1 : -.1, 1.1, .12, doorGroup); }
    const [de, dn] = sfp(.8, .06); doorGroup.position.copy(W3(de, dn)); doorGroup.rotation.y = MROT; scene.add(doorGroup);
  }
  const canLamp = scene.getObjectByName('canLamp');
  const porchLight = new THREE.PointLight(0xffd9a8, 0, 12, 1.4); porchLight.position.copy(W3(-20.1, -10.0, 2.8)); scene.add(porchLight);
  
  // ---------- water slide: tube on yellow columns, out of the pool hall and back in
  const slideCurve = new THREE.CatmullRomCurve3([
    [18.4, 24.4, 8.6], [18.8, 26.8, 8.5], [19, 29.5, 8.0], [16, 32.5, 7.2], [9, 34, 6.3], [2, 34, 5.5], [-4, 33, 4.7],
    [-8.5, 30.5, 3.9], [-9, 26.5, 3.2], [-6.5, 23, 2.7], [-5.0, 20.6, 2.45], [-4.4, 18.8, 2.4]
  ].map(([e, n, y]) => W3(e, n, y)));
  {
    const tube = new THREE.TubeGeometry(slideCurve, 220, .62, 18, false);
    const p = tube.attributes.position, nrm = tube.attributes.normal, cols = [];
    const bands = [[.25, .62, .62], [.12, .32, .72], [.85, .28, .12], [.9, .55, .12]];
    for (let i = 0; i < p.count; i++) {
      const seg = Math.floor(i / 19) / 220;
      let c = seg < .45 ? bands[0] : seg < .7 ? bands[1] : seg < .85 ? bands[2] : bands[3];
      if (nrm.getY(i) > .25) c = [.88, .9, .9];            // translucent white roof of the tube
      cols.push(...c);
    }
    tube.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3));
    const m = new THREE.Mesh(tube, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .35, metalness: .1 }));
    m.castShadow = true; m.name = 'slide'; scene.add(m);
    // columns
    const colMat = new THREE.MeshStandardMaterial({ color: 0xf2c12e, roughness: .5, metalness: .2 });
    const geos = [];
    for (let t = .06; t < .93; t += .085) {
      const q = slideCurve.getPointAt(t); const e = q.x, n = -q.z;
      if (insideFootprint(e, n)) continue;
      const gr = H(e, n), h = q.y - .6 - gr, g = new THREE.CylinderGeometry(.16, .16, h, 12); g.translate(q.x, gr + h / 2, q.z); geos.push(g);
      const y = new THREE.BoxGeometry(1.5, .12, .12); y.translate(q.x, q.y - .62, q.z); geos.push(y);
      blockers.push([e, n, .45]);
    }
    const cm = new THREE.Mesh(mergeGeometries(geos), colMat); cm.castShadow = true; scene.add(cm);
  }
  
  // ---------- trees, railings, globe lamp, benches
  function tree(e, n, h, spread, seedOff) {
    seed = 777 + seedOff;
    const geos = [], up = new THREE.Vector3(0, 1, 0);
    function br(p, dir, len, rad, depth) {
      const g = new THREE.CylinderGeometry(rad * .7, rad, len, 5, 1, true);
      g.translate(0, len / 2, 0); g.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(up, dir)); g.translate(p.x, p.y, p.z); geos.push(g);
      const end = p.clone().addScaledVector(dir, len);
      if (depth === 0) return;
      const k = depth > 3 ? 2 : (depth > 1 ? 3 : 4);
      for (let i = 0; i < k; i++) {
        const d = dir.clone().add(new THREE.Vector3(rr(-spread, spread), rr(.1, .5), rr(-spread, spread))).normalize();
        br(end, d, len * rr(.62, .78), rad * .62, depth - 1);
      }
    }
    br(W3(e, n), new THREE.Vector3(rr(-.05, .05), 1, rr(-.05, .05)).normalize(), h * .36, h * .03, 5);
    const m = new THREE.Mesh(mergeGeometries(geos), M.bark); m.castShadow = true; scene.add(m);
    blockers.push([e, n, .7]);
  }
  [[-30, 40, 16], [-38, 22, 15], [32, 40, 14], [44, 22, 16], [40, -20, 15], [26, -44, 14], [-26, -30, 15], [52, 52, 15], [-12, 52, 13]]
    .forEach(([e, n, h], i) => tree(e, n, h, .75, i + 3));
  seed = 4242;
  const yellow = new THREE.MeshStandardMaterial({ color: 0xf2c12e, roughness: .5, metalness: .3 });
  function railing(pts, h = 1.0) {
    const geos = [];
    for (let i = 0; i < pts.length - 1; i++) {
      const [a, b] = [pts[i], pts[i + 1]], L = Math.hypot(b[0] - a[0], b[1] - a[1]), ang = Math.atan2(b[1] - a[1], b[0] - a[0]);
      for (const y of [h, h * .5]) { const g = new THREE.BoxGeometry(L, .05, .05); g.rotateY(ang); g.translate((a[0] + b[0]) / 2, y + H((a[0] + b[0]) / 2, (a[1] + b[1]) / 2), -(a[1] + b[1]) / 2); geos.push(g); }
      for (let s = 0; s <= L; s += 1.6) { const g = new THREE.BoxGeometry(.05, h, .05); g.translate(a[0] + (b[0] - a[0]) * s / L, h / 2 + H(a[0] + (b[0] - a[0]) * s / L, a[1] + (b[1] - a[1]) * s / L), -(a[1] + (b[1] - a[1]) * s / L)); geos.push(g); blockers.push([a[0] + (b[0] - a[0]) * s / L, a[1] + (b[1] - a[1]) * s / L, .5]); }
    }
    const m = new THREE.Mesh(mergeGeometries(geos), yellow); m.castShadow = true; scene.add(m);
  }
  railing([[-34, -14], [-36, -24], [-30, -32]]);
  // bike barriers in front of the entrance
  for (const [e, n] of [[-24, -20], [-21, -19.2]]) {
    const g = new THREE.TorusGeometry(.5, .04, 6, 16, Math.PI); const m = new THREE.Mesh(g, yellow);
    m.position.copy(W3(e, n, 0)); m.rotation.y = .3; m.scale.set(1.6, 1.8, 1); scene.add(m); blockers.push([e, n, .7]);
  }
  const lampLights = [];
  {
    // globe cluster lamp (as at the forecourt)
    const g = new THREE.Group(); g.name = 'globelamp';
    add(new THREE.Mesh(new THREE.CylinderGeometry(.08, .1, 4, 12), yellow), 0, 2, 0, g);
    const globeMat = new THREE.MeshStandardMaterial({ color: 0xf6f3ea, roughness: .4, emissive: 0xffe2b0, emissiveIntensity: 0 });
    for (let i = 0; i < 6; i++) {
      const a = i * Math.PI / 3, r = .75;
      add(new THREE.Mesh(new THREE.CylinderGeometry(.03, .03, r, 6), yellow), Math.cos(a) * r / 2, 3.2 + (i % 2) * .5, Math.sin(a) * r / 2, g).rotation.set(0, -a, Math.PI / 2);
      add(new THREE.Mesh(new THREE.SphereGeometry(.28, 18, 12), globeMat), Math.cos(a) * r, 3.2 + (i % 2) * .5, Math.sin(a) * r, g);
    }
    add(new THREE.Mesh(new THREE.SphereGeometry(.3, 18, 12), globeMat), 0, 4.2, 0, g);
    const L = new THREE.PointLight(0xffd9a0, 0, 18, 1.5); L.position.set(0, 3.6, 0); g.add(L);
    lampLights.push({ L, bulb: { material: globeMat } });
    g.position.copy(W3(-31, -16)); g.traverse(o => { if (o.isMesh) o.castShadow = true; }); scene.add(g); blockers.push([-31, -16, .5]);
  }
  {
    const g = new THREE.Group(); g.name = 'bench';
    add(box(1.8, .08, .45, M.wood), 0, .45, 0, g);
    for (const x of [-.75, .75]) add(box(.08, .45, .4, M.dark), x, .22, 0, g);
    g.position.copy(W3(4, 40)); g.rotation.y = -.25; scene.add(g); blockers.push([4, 40, 1]);
  }
  // a green waste bin and a ticket machine by the entrance
  add(box(.9, 1.0, .7, new THREE.MeshStandardMaterial({ color: 0x2f6b3a, roughness: .6 })), -9, .5, 25).castShadow = true; blockers.push([-9, -25, .7]);
  const GB = new THREE.Group(); scene.add(GB);
  
  // aus dem Eingabeteil des Prototyps (dort per Hoisting verfügbar)
  function insideFootprint(e, n) {
    const z = -n;
    for (const [a, b, c] of footprints) {
      const d1 = (e - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (z - b[1]);
      const d2 = (e - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (z - c[1]);
      const d3 = (e - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (z - a[1]);
      if (!((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0))) return true;
    }
    return false;
  }
  return { scene, H, W3, camVec, blockers, footprints, M, box, plane, add, canvasTex, onWall, MW1, MW2, MWN, sfp, ground, sun };
}
