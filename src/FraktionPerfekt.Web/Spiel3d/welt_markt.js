// Erzeugt aus dem 3D-Prototyp (markt.html) mit gen_welt.py – Außenschauplatz (Gebäude: LoD2, Geobasis NRW, dl-de/zero-2-0).
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
  sun.position.set(40, 38, 10);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -60, right: 60, top: 60, bottom: -60, near: 1, far: 200 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = .03;
  scene.add(sun);
  
  // ---------- real-world helpers (E = east, N = north, metres from Marktstraße 19)
  const W3 = (e, n, y = 0) => new THREE.Vector3(e, y + H(e, n), -n);
  function onWall(obj, p1, p2, t, y, nrm, off = .06) {
    const e = p1[0] + (p2[0] - p1[0]) * t, n = p1[1] + (p2[1] - p1[1]) * t;
    obj.position.set(e + nrm[0] * off, y + H(e, n), -(n + nrm[1] * off));
    obj.rotation.y = Math.atan2(nrm[0], -nrm[1]);
    return obj;
  }
  // street axis of the pedestrian zone (Marktstraße), roughly NNW–SSE
  const AX = [-.377, .926], AXN = [.926, .377];
  const ST = (a, c) => [12.5 + AX[0] * a + AXN[0] * c, 5 + AX[1] * a + AXN[1] * c];
  const STROT = Math.atan2(-AX[1], AX[0]) + Math.PI / 2;
  
  // ---------- ground: pedestrian zone
  const ground = terrainMesh(M.pave, 1.6);
  {
    const len = 120, sg = new THREE.PlaneGeometry(2.2, len, 1, 80); sg.rotateX(-Math.PI / 2);
    const uv = sg.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * 2.2 / .8, uv.getY(i) * len / .8);
    const st = new THREE.Mesh(sg, M.cob); st.receiveShadow = true; st.name = 'ground';
    const [e, n] = ST(0, 0); st.position.set(e, 0, -n); st.rotation.y = Math.atan2(-AX[0], AX[1]); scene.add(st); drape(st, .03);
  }
  
  // ---------- textures for the shop front
  const plasterTex = canvasTex(256, 256, (g, w, h) => {
    g.fillStyle = '#e8e2d6'; g.fillRect(0, 0, w, h);
    for (let i = 0; i < 2500; i++) { g.fillStyle = `rgba(90,80,60,${rnd() * .05})`; g.fillRect(rnd() * w, rnd() * h, 2, 2); }
  });
  const fasciaTex = canvasTex(1024, 128, (g, w, h) => {
    const gr = g.createLinearGradient(0, 0, 0, h); gr.addColorStop(0, '#24b4d6'); gr.addColorStop(1, '#138fb3');
    g.fillStyle = gr; g.fillRect(0, 0, w, h);
    g.fillStyle = '#fff'; g.font = '600 62px Arial, sans-serif'; g.textBaseline = 'middle';
    g.fillText('Zukunftsschmiede', 40, h / 2 + 2);
    g.font = '400 40px Arial, sans-serif'; g.textAlign = 'right'; g.fillText('Greven', w - 40, h / 2 + 4);
  });
  const interiorTex = canvasTex(1024, 384, (g, w, h) => {
    g.fillStyle = '#2b2a2c'; g.fillRect(0, 0, w, h);
    const gr = g.createLinearGradient(0, 0, 0, h); gr.addColorStop(0, '#5a5048'); gr.addColorStop(.75, '#3b342f'); gr.addColorStop(1, '#6a5c4e');
    g.fillStyle = gr; g.fillRect(0, 0, w, h);
    // ceiling lamp
    g.fillStyle = '#d9c9a0'; g.beginPath(); g.ellipse(300, 40, 60, 14, 0, 0, 7); g.fill();
    // chalkboard
    g.fillStyle = '#1f2421'; g.fillRect(380, 70, 300, 170); g.strokeStyle = '#8a7a60'; g.lineWidth = 8; g.strokeRect(380, 70, 300, 170);
    g.fillStyle = '#e8e8e0'; g.font = '30px "Comic Sans MS", cursive'; g.fillText('Zukunftsschmiede', 410, 120);
    g.font = '20px "Comic Sans MS", cursive'; g.fillText('Ideen für Greven', 445, 160); g.fillText('– komm rein! –', 455, 200);
    // roll-up banners (neutral)
    for (const x of [120, 820]) { g.fillStyle = '#f2f4f6'; g.fillRect(x, 60, 110, 300); g.fillStyle = '#1aa3c8'; g.fillRect(x, 300, 110, 60); g.fillStyle = '#1aa3c8'; g.fillRect(x + 20, 90, 70, 12); }
    // bicycle
    g.strokeStyle = '#1d3a6a'; g.lineWidth = 6;
    for (const x of [470, 620]) { g.beginPath(); g.arc(x, 320, 48, 0, 7); g.stroke(); }
    g.beginPath(); g.moveTo(470, 320); g.lineTo(540, 250); g.lineTo(620, 320); g.moveTo(540, 250); g.lineTo(600, 250); g.lineTo(620, 320); g.moveTo(540, 250); g.lineTo(530, 230); g.stroke();
    // table and chairs
    g.fillStyle = '#c9a77a'; g.fillRect(720, 260, 80, 8); g.fillRect(756, 268, 8, 70);
    // flowers
    g.fillStyle = '#f4f2ea'; for (let i = 0; i < 12; i++) { g.beginPath(); g.arc(560 + rnd() * 30, 360 + rnd() * 14, 6, 0, 7); g.fill(); }
  });
  const swooshTex = canvasTex(512, 128, (g, w, h) => {
    g.clearRect(0, 0, w, h);
    g.fillStyle = 'rgba(30,170,210,.95)'; g.beginPath(); g.moveTo(0, h); g.lineTo(w, h * .15); g.lineTo(w, h); g.fill();
    g.fillStyle = '#ffffff'; g.font = 'italic 34px Arial, sans-serif'; g.fillText('Zukunftsschmiede', 30, h - 22);
  });
  swooshTex.colorSpace = THREE.SRGBColorSpace;
  
  // ---------- buildings from LoD2 (Geobasis NRW)
  const LOD2 = DATEN.lod2;
  const wallUniforms = { uNight: { value: 0 } };
  function wallMaterial(kind) {   // 0 brick, 1 plaster (shop house), 2 plaster (other houses)
    const m = new THREE.MeshStandardMaterial({ map: kind ? plasterTex : brickTex, roughness: .9, side: THREE.DoubleSide });
    m.onBeforeCompile = sh => {
      sh.uniforms.uKind = { value: kind };
      sh.uniforms.uNight = wallUniforms.uNight;
      sh.vertexShader = sh.vertexShader
        .replace('#include <common>', '#include <common>\nattribute vec3 wattr;\nattribute float bofs;\nvarying vec3 vW;\nvarying float vH;')
        .replace('#include <begin_vertex>', '#include <begin_vertex>\nvW = wattr; vH = position.y - bofs;');
      sh.fragmentShader = sh.fragmentShader
        .replace('#include <common>', '#include <common>\nuniform float uKind; uniform float uNight; varying vec3 vW; varying float vH;')
        .replace('#include <map_fragment>', `
  vec4 col = uKind < .5 ? texture2D(map, vec2(vW.x, vH / .6)) : texture2D(map, vec2(vW.x / 2.0, vH / 2.0));
  vec3 tint = uKind > 1.5 ? vec3(.97, .93, .86) : vec3(1.0);
  col.rgb *= tint;
  float topH = vH + vW.y; bool glow = false;
  if (vW.z > 3.5 && topH > 2.6) {
    // ground floor: shop windows; upper floors: punched windows
    float per = 2.6, sh = mod(vW.z, per) * .5, lx = mod(vW.x - sh, per) / per;
    if (vH < 3.4 && uKind < .5 && vW.z > 6.0) {
      if (vH > .45 && vH < 2.9 && vW.x > .6 && vW.x < vW.z - .6) {
        bool fr = mod(vW.x, 2.2) < .1 || vH < .55 || vH > 2.8;
        col = vec4(fr ? vec3(.2) : mix(vec3(.12,.13,.14), vec3(.42,.46,.48), (vH - .5) / 2.4), 1.);
        glow = !fr && fract(floor(vW.x / 2.2) * .37 + vW.z * .11) > .3;
      }
    } else if (vH > 3.6) {
      float ly = mod(vH - 4.2, 3.0), wh = 1.55;
      float idx = floor((vW.x - sh) / per) + 31.0 * floor((vH - 4.2) / 3.0);
      bool fits = vH > 4.2 && vW.x > sh && vW.x < vW.z - sh && vW.y > (wh - ly) + .3;
      if (fits && lx > .25 && lx < .75 && ly < wh) {
        bool fr = lx < .29 || lx > .71 || ly < .07 || ly > wh - .07 || abs(lx - .5) < .012 || abs(ly - wh * .7) < .03;
        col = vec4(fr ? vec3(.93) : mix(vec3(.04,.05,.07), vec3(.3,.34,.38), ly / wh), 1.);
        glow = !fr && fract(idx * .618 + floor(vW.z * 10.) * .137) > .45;
      }
    }
  }
  if (glow) totalEmissiveRadiance += vec3(1.0, .74, .42) * uNight * 1.3;
  diffuseColor *= col;`);
    };
    return m;
  }
  const mats = [wallMaterial(0), wallMaterial(1), wallMaterial(2)];
  const roofMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: .85, side: THREE.DoubleSide, flatShading: true });
  const footprints = [];
  {
    const walls = [[], [], []], roofs = [];
    const mk = (pos, attr, o = 0) => {
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
      if (attr) { g.setAttribute('wattr', new THREE.Float32BufferAttribute(attr, 3)); g.setAttribute('bofs', new THREE.Float32BufferAttribute(new Float32Array(pos.length / 3).fill(o), 1)); }
      g.computeVertexNormals(); return g;
    };
    LOD2.forEach((b, bi) => {
      seed = 31 + bi * 7;
      const kind = b.s === 1 ? 1 : (rnd() < .3 ? 2 : 0);
      if (b.w.length) walls[kind].push(mk(b.w, b.a, b.o));
      if (b.r.length) {
        const g = mk(b.r), p = g.attributes.position, cols = [];
        const tile = b.s === 3 ? [.3, .52, .45] : (rnd() < .5 ? [.36, .15, .1] : [.17, .17, .19]);
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
    walls.forEach((arr, k) => { if (arr.length) { const m = new THREE.Mesh(mergeGeometries(arr), mats[k]); m.castShadow = m.receiveShadow = true; m.name = 'lod2'; scene.add(m); } });
    const rm = new THREE.Mesh(mergeGeometries(roofs), roofMat); rm.castShadow = rm.receiveShadow = true; scene.add(rm);
  }
  
  // ---------- Zukunftsschmiede shop front (Marktstraße 19), front wall (3.5,8.2) → (8.3,-3.6)
  const F1 = [3.5, 8.2], F2 = [8.3, -3.6], FN = [.926, .377];
  const FL = Math.hypot(F2[0] - F1[0], F2[1] - F1[1]);
  const shop = new THREE.Group(); scene.add(shop);
  const pl = (w, h, mat, x, y, z = 0) => { const m = plane(w, h, mat); m.position.set(x, y, z); return m; };
  {
    const g = new THREE.Group();
    const plaster = new THREE.MeshStandardMaterial({ map: plasterTex, roughness: .9 });
    // white frame around the shop window
    g.add(box(FL, .45, .25, plaster).translateY(.22));
    g.add(box(FL, .5, .25, plaster).translateY(3.55));
    g.add(box(.6, 3.6, .25, plaster).translateX(-FL / 2 + .3).translateY(1.8));
    g.add(box(.5, 3.6, .25, plaster).translateX(FL / 2 - .25).translateY(1.8));
    // blue fascia with name
    g.add(box(FL - 1.0, .72, .1, new THREE.MeshStandardMaterial({ color: 0x1597bb, roughness: .5 })).translateY(3.02).translateZ(.13));
    const fascia = plane(FL - 1.0, .72, new THREE.MeshStandardMaterial({ map: fasciaTex, roughness: .5 }));
    fascia.position.set(0, 3.02, .185); g.add(fascia);
    // interior behind the glass (lit)
    const intMat = new THREE.MeshStandardMaterial({ map: interiorTex, roughness: .9, emissive: 0xffffff, emissiveMap: interiorTex, emissiveIntensity: .08 });
    const inner = pl(FL - 1.1, 2.3, intMat, -.6, 1.55, .04); inner.name = 'window'; g.add(inner);
    // glass panes with a reflection sheen
    const glass = new THREE.MeshStandardMaterial({ color: 0x9fb4c2, roughness: .05, metalness: .2, transparent: true, opacity: .28 });
    g.add(pl(FL - 3.2, 2.3, glass, -1.1, 1.55, .07));
    // mullion and the door on the right
    g.add(box(.12, 2.3, .14, M.dark).translateX(FL / 2 - 2.25).translateY(1.55));
    const door = new THREE.Group(); door.name = 'door';
    add(box(1.3, 2.4, .08, new THREE.MeshStandardMaterial({ color: 0xf2f2f2, roughness: .5 })), 0, 1.2, 0, door);
    add(plane(1.0, 2.05, new THREE.MeshStandardMaterial({ color: 0x2e3a42, roughness: .1, metalness: .4 })), 0, 1.2, .05, door);
    add(box(.04, .5, .05, M.metal), .38, 1.1, .1, door);
    door.position.set(FL / 2 - 1.4, 0, .06); g.add(door);
    // blue swoosh decal across the lower window
    const sw = pl(FL - 3.4, .75, new THREE.MeshBasicMaterial({ map: swooshTex, transparent: true }), -1.1, .9, .09); g.add(sw);
    onWall(g, F1, F2, .5, 0, FN, .02); shop.add(g);
  }
  // upper-floor windows of the shop house are drawn by the facade shader; add a small sign with the house number
  {
    const numTex = canvasTex(128, 128, (g, w, h) => { g.fillStyle = '#1a4f8a'; g.fillRect(0, 0, w, h); g.fillStyle = '#fff'; g.font = 'bold 70px Arial'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('19', 64, 68); });
    shop.add(onWall(plane(.3, .3, new THREE.MeshStandardMaterial({ map: numTex })), F1, F2, .97, 2.6, FN, .2));
  }
  
  // ---------- Eiscafé on the opposite side (west face of Marktstraße 42/44)
  const E1 = [16.0, 15.8], E2 = [20.6, 4.1], EN = [-.931, -.366];
  {
    const eisTex = canvasTex(512, 96, (g, w, h) => {
      g.fillStyle = '#f7efe1'; g.fillRect(0, 0, w, h); g.fillStyle = '#c0392b'; g.font = 'italic bold 56px Georgia, serif';
      g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('Eiscafé', w / 2, h / 2 + 3);
    });
    const sign = plane(3.4, .62, new THREE.MeshStandardMaterial({ map: eisTex, roughness: .6 })); sign.name = 'eis';
    scene.add(onWall(sign, E1, E2, .5, 3.4, EN, .1));
    // striped awning
    const awTex = canvasTex(256, 64, (g, w, h) => { for (let x = 0; x < w; x += 32) { g.fillStyle = (x / 32) % 2 ? '#f4f0e6' : '#b8322a'; g.fillRect(x, 0, 32, h); } });
    const aw = plane(8.6, 1.6, new THREE.MeshStandardMaterial({ map: awTex, roughness: .8, side: THREE.DoubleSide }));
    const ag = new THREE.Group(); aw.rotation.x = -1.1; aw.position.set(0, 2.85, .65); ag.add(aw);
    scene.add(onWall(ag, E1, E2, .5, 0, EN, .05));
    // tables and chairs outside
    const tm = new THREE.MeshStandardMaterial({ color: 0xd8d4cc, roughness: .5, metalness: .4 });
    for (let i = 0; i < 3; i++) {
      const t = new THREE.Group();
      add(new THREE.Mesh(new THREE.CylinderGeometry(.35, .35, .04, 20), tm), 0, .74, 0, t);
      add(new THREE.Mesh(new THREE.CylinderGeometry(.03, .03, .72, 8), M.metal), 0, .37, 0, t);
      for (const [dx, dz] of [[.55, 0], [-.55, 0]]) {
        const c = new THREE.Group(); add(box(.4, .04, .4, M.wood), 0, .45, 0, c); add(box(.4, .45, .04, M.wood), 0, .68, -.2 * Math.sign(dx), c);
        for (const lx of [-.17, .17]) for (const lz of [-.17, .17]) add(box(.03, .45, .03, M.metal), lx, .22, lz, c);
        c.position.set(dx, 0, dz); c.rotation.y = dx > 0 ? -Math.PI / 2 : Math.PI / 2; t.add(c);
      }
      const [e, n] = [E1[0] + (E2[0] - E1[0]) * (.2 + i * .3) + EN[0] * 1.9, E1[1] + (E2[1] - E1[1]) * (.2 + i * .3) + EN[1] * 1.9];
      t.position.copy(W3(e, n)); t.traverse(o => { if (o.isMesh) o.castShadow = true; }); scene.add(t); blockersPush(e, n, .9);
    }
  }
  function blockersPush(e, n, r) { (__bl || (__bl = [])).push([e, n, r]); }
  
  // ---------- street furniture & trees
  const blockers = __bl || [];
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
    br(W3(e, n), new THREE.Vector3(0, 1, 0), h * .38, h * .025, 5);
    const m = new THREE.Mesh(mergeGeometries(geos), M.bark); m.castShadow = true; scene.add(m);
    // tree grate and guard
    const gr = new THREE.Mesh(new THREE.PlaneGeometry(1.8, 1.8), new THREE.MeshStandardMaterial({ color: 0x2c2c2c, roughness: .6, metalness: .5 }));
    gr.rotation.x = -Math.PI / 2; gr.position.copy(W3(e, n, .04)); gr.receiveShadow = true; scene.add(gr);
    for (const [dx, dn] of [[.6, .6], [-.6, .6], [.6, -.6], [-.6, -.6]]) add(new THREE.Mesh(new THREE.CylinderGeometry(.025, .025, 1.1, 6), M.metal), e + dx, .55, -(n + dn));
    blockers.push([e, n, 1.0]);
  }
  for (const [a, c] of [[-24, 3.6], [-6, 3.8], [16, 3.8], [30, 3.4], [-15, -3.4], [26, -3.6]]) { const [e, n] = ST(a, c); tree(e, n, 10, .7, Math.round(a)); }
  seed = 4242;
  // benches, A-boards, bike stand, lamps
  const lampLights = [];
  function lamp(e, n, h) {
    const g = new THREE.Group();
    add(new THREE.Mesh(new THREE.CylinderGeometry(.06, .09, h, 12), new THREE.MeshStandardMaterial({ color: 0x3a3d40, roughness: .5, metalness: .5 })), 0, h / 2, 0, g).castShadow = true;
    const bulb = add(new THREE.Mesh(new THREE.SphereGeometry(.25, 16, 12), new THREE.MeshStandardMaterial({ color: 0xf4f4f4, emissive: 0xffd9a0, emissiveIntensity: 0 })), 0, h + .2, 0, g);
    const L = new THREE.PointLight(0xffc888, 0, 14, 1.6); L.position.set(0, h, 0); g.add(L);
    lampLights.push({ L, bulb }); g.position.copy(W3(e, n)); scene.add(g); blockers.push([e, n, .3]);
  }
  for (const a of [-20, 4, 24]) { const [e, n] = ST(a, 1.6); lamp(e, n, 4.2); }
  function aboard(e, n, rot, col, name) {
    const g = new THREE.Group(); g.name = name || '';
    for (const s of [-1, 1]) { const p = box(.6, .9, .03, new THREE.MeshStandardMaterial({ color: col, roughness: .7 })); p.position.set(0, .5, s * .12); p.rotation.x = -s * .2; g.add(p); }
    g.position.copy(W3(e, n)); g.rotation.y = rot; scene.add(g); blockers.push([e, n, .5]);
  }
  aboard(10.6, 4.0, .4, 0x1aa3c8, 'aboard');
  aboard(17.4, 9.4, -.3, 0x3b2f2a);
  {
    const g = new THREE.Group(); g.name = 'bench';
    add(box(1.8, .08, .45, M.wood), 0, .45, 0, g);
    for (const x of [-.75, .75]) add(box(.08, .45, .4, M.dark), x, .22, 0, g);
    const [e, n] = ST(-10, -3.2); g.position.copy(W3(e, n)); g.rotation.y = Math.atan2(-AX[0], AX[1]) + Math.PI / 2; scene.add(g); blockers.push([e, n, 1]);
  }
  // bicycle leaning in front of the shop
  {
    const bm = new THREE.MeshStandardMaterial({ color: 0x1d3a6a, roughness: .4, metalness: .5 });
    const g = new THREE.Group(); g.name = 'bike';
    for (const x of [-.52, .52]) { const w = new THREE.Mesh(new THREE.TorusGeometry(.34, .025, 6, 24), M.dark); w.position.set(x, .34, 0); g.add(w); }
    const tube = (a, b) => { const v = new THREE.Vector3().subVectors(b, a), m = new THREE.Mesh(new THREE.CylinderGeometry(.02, .02, v.length(), 6), bm); m.position.copy(a).addScaledVector(v, .5); m.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), v.normalize()); g.add(m); };
    const P = (x, y) => new THREE.Vector3(x, y, 0);
    tube(P(-.52, .34), P(-.05, .72)); tube(P(-.05, .72), P(.4, .72)); tube(P(.4, .72), P(.52, .34)); tube(P(-.05, .72), P(.08, .34)); tube(P(-.52, .34), P(.08, .34)); tube(P(.4, .72), P(.36, .95)); tube(P(-.05, .72), P(-.1, .88));
    add(box(.22, .05, .1, M.dark), -.1, .9, 0, g); add(box(.04, .04, .5, M.metal), .36, .96, 0, g);
    onWall(g, F1, F2, .32, 0, FN, .45); scene.add(g);
    g.traverse(o => { if (o.isMesh) o.castShadow = true; });
  }
  const GB = new THREE.Group(); scene.add(GB);
  const canLamp = new THREE.Mesh(new THREE.SphereGeometry(.01), new THREE.MeshBasicMaterial({ visible: false })); scene.add(canLamp);
  const porchLight = new THREE.PointLight(0xffd9a8, 0, 10, 1.4); scene.add(onWall(porchLight, F1, F2, .45, 2.2, FN, 1.6));
  
  return { scene, H, W3, camVec, blockers, footprints, M, box, plane, add, canvasTex, onWall, F1, F2, FN, ST, E1, E2, EN, sun };
}
