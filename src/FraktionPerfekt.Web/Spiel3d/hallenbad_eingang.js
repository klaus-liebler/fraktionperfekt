// Joe & Chani 3D – Eingang des Hallenbads Greven, nach einem Foto nachgebaut: Wandbild „Grevener Bäder“, Schild
// „EINGANG HALLENBAD“, blaue Pfeiler, rechts der laut Luftbild rund 5,8 m vorspringende Umkleidetrakt; als Baustelle mit
// Grube vor der Tür, Bauzaun, Leiter am Schild und abgeschraubten Buchstaben.
// Ortskoordinaten: x entlang der Fassade nach rechts, y nach oben, z vom Gebäude zum Betrachter; Eingangsfront bei z = 0.
import * as THREE from './three_min.js';
import { meterMaterial } from './texturen3d.js';

const mat = (c, r = .7, m = 0, extra = {}) => new THREE.MeshStandardMaterial({ color: c, roughness: r, metalness: m, ...extra });
function box(w, h, d, m, x, y, z, eltern) { const o = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), m); o.position.set(x, y, z); o.castShadow = o.receiveShadow = true; eltern.add(o); return o; }
function leinwand(w, h, malen) {
    const c = document.createElement('canvas'); c.width = w; c.height = h; malen(c.getContext('2d'), w, h);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; return t;
}
let saat = 99;
const zufall = () => (saat = (saat * 16807) % 2147483647) / 2147483647;

/** Wandbild „Grevener Bäder“: Himmel mit Wolken, Meer, Rutsche mit lachendem Jungen und Spritzwasser, Schwimmer mit Taucherbrille. */
function wandbild() {
    return leinwand(2048, 1040, (g, W, H) => {
        saat = 7;
        const himmel = g.createLinearGradient(0, 0, 0, H * .62); himmel.addColorStop(0, '#3f86d6'); himmel.addColorStop(1, '#cfe6f7');
        g.fillStyle = himmel; g.fillRect(0, 0, W, H);
        // Wolkenbank über dem Horizont
        for (let i = 0; i < 70; i++) {
            const x = W * (.3 + zufall() * .72), y = H * (.36 + zufall() * .2), r = 30 + zufall() * 60;
            g.fillStyle = `rgba(255,255,255,${.55 + zufall() * .4})`; g.beginPath(); g.ellipse(x, y, r * 1.8, r * .8, 0, 0, 7); g.fill();
            g.fillStyle = 'rgba(160,185,210,.25)'; g.beginPath(); g.ellipse(x + 10, y + r * .5, r * 1.6, r * .35, 0, 0, 7); g.fill();
        }
        // Meer
        const meer = g.createLinearGradient(0, H * .6, 0, H); meer.addColorStop(0, '#4aa3e0'); meer.addColorStop(.4, '#1f72c0'); meer.addColorStop(1, '#0b3f86');
        g.fillStyle = meer; g.fillRect(0, H * .6, W, H * .4);
        g.strokeStyle = 'rgba(255,255,255,.55)'; g.lineWidth = 5;
        for (let y = H * .62; y < H * .7; y += 14) { g.beginPath(); for (let x = 0; x <= W; x += 16) { g.lineTo(x, y + Math.sin(x / 40 + y) * 4); } g.stroke(); }
        g.strokeStyle = 'rgba(200,235,255,.18)'; g.lineWidth = 3;
        for (let i = 0; i < 40; i++) { const x = zufall() * W, y = H * (.72 + zufall() * .26); g.beginPath(); g.moveTo(x, y); g.quadraticCurveTo(x + 40, y - 8, x + 90, y); g.stroke(); }
        // Rutsche: breite blaue Röhre mit hellen Rippen von links oben schräg hinunter ins Wasser
        g.save(); g.translate(-W * .04, H * .06); g.rotate(.5);
        const rl = W * .62, rb = H * .44;
        const rg = g.createLinearGradient(0, 0, 0, rb); rg.addColorStop(0, '#9fd2f2'); rg.addColorStop(.45, '#2f7fcf'); rg.addColorStop(1, '#174f9a');
        g.fillStyle = rg; g.beginPath(); g.roundRect(0, 0, rl, rb, 50); g.fill();
        g.strokeStyle = 'rgba(255,255,255,.65)'; g.lineWidth = 9;
        for (let x = 40; x < rl - 20; x += 46) { g.beginPath(); g.moveTo(x, 12); g.quadraticCurveTo(x - 14, rb / 2, x, rb - 12); g.stroke(); }
        g.strokeStyle = '#123f80'; g.lineWidth = 6; g.beginPath(); g.roundRect(0, 0, rl, rb, 50); g.stroke();
        g.restore();
        // Spritzwasser am Ende der Rutsche
        for (let i = 0; i < 70; i++) {
            const a = -Math.PI * (.05 + zufall() * .9), r = 60 + zufall() * 260, x = W * .47 + Math.cos(a) * r, y = H * .92 + Math.sin(a) * r * .7;
            g.fillStyle = `rgba(255,255,255,${.7 + zufall() * .3})`; g.beginPath(); g.ellipse(x, y, 6 + zufall() * 12, 10 + zufall() * 18, a, 0, 7); g.fill();
        }
        // lachender Junge auf der Rutsche (Comic): Kopf, Haare, Arme hoch, rote Badehose
        const hx = 0, hy = 0;
        g.save(); g.translate(W * .4, H * .34); g.scale(1.75, 1.75);
        g.lineWidth = 5; g.strokeStyle = '#3a2a1f';
        g.fillStyle = '#f2c59b'; g.beginPath(); g.ellipse(hx - 30, hy + 150, 48, 80, -.3, 0, 7); g.fill(); g.stroke();      // Oberkörper
        g.fillStyle = '#d23a2b'; g.beginPath(); g.ellipse(hx - 10, hy + 225, 62, 38, -.2, 0, 7); g.fill(); g.stroke();        // Badehose
        for (const [dx, dy, r] of [[-95, 40, -.9], [55, 30, .9]]) { g.fillStyle = '#f2c59b'; g.save(); g.translate(hx + dx, hy + dy); g.rotate(r); g.beginPath(); g.roundRect(-14, -70, 28, 110, 14); g.fill(); g.stroke(); g.beginPath(); g.ellipse(0, -78, 22, 20, 0, 0, 7); g.fill(); g.stroke(); g.restore(); }
        for (const dx of [-20, 70]) { g.fillStyle = '#f2c59b'; g.beginPath(); g.ellipse(hx + dx, hy + 300, 26, 60, .5, 0, 7); g.fill(); g.stroke(); }
        g.fillStyle = '#f2c59b'; g.beginPath(); g.ellipse(hx - 20, hy + 30, 78, 70, 0, 0, 7); g.fill(); g.stroke();             // Kopf
        g.fillStyle = '#7a4a26'; g.beginPath(); g.ellipse(hx - 20, hy - 18, 92, 34, 0, Math.PI, 2 * Math.PI); g.fill(); g.stroke();   // Haare / Sonnenhut
        g.fillStyle = '#ffffff'; g.beginPath(); g.ellipse(hx - 20, hy + 58, 46, 26, 0, 0, Math.PI); g.fill(); g.stroke();         // offenes Lachen
        g.fillStyle = '#7a1e1e'; g.beginPath(); g.ellipse(hx - 20, hy + 70, 28, 12, 0, 0, Math.PI); g.fill();
        g.fillStyle = '#1a1a1a'; for (const dx of [-46, 6]) { g.beginPath(); g.ellipse(hx + dx, hy + 18, 7, 11, 0, 0, 7); g.fill(); }
        g.restore();
        // Schwimmer unter Wasser mit Taucherbrille
        const sx = 0, sy = 0;
        g.save(); g.translate(W * .7, H * .64); g.scale(1.45, 1.45); g.globalAlpha = .92;
        g.fillStyle = '#e8b48c'; g.beginPath(); g.ellipse(sx + 150, sy + 80, 120, 34, .25, 0, 7); g.fill();                // Körper
        g.beginPath(); g.ellipse(sx + 290, sy + 120, 70, 18, .3, 0, 7); g.fill();                                           // Beine
        g.fillStyle = '#e8b48c'; for (const dy of [-30, 40]) { g.beginPath(); g.ellipse(sx - 70, sy + dy, 60, 14, dy < 0 ? -.4 : .4, 0, 7); g.fill(); }   // Arme nach vorn
        g.beginPath(); g.ellipse(sx, sy + 20, 52, 50, 0, 0, 7); g.fill();                                                    // Kopf
        g.fillStyle = '#3b2a1e'; g.beginPath(); g.ellipse(sx + 6, sy - 6, 54, 34, 0, Math.PI, 2 * Math.PI); g.fill();
        g.fillStyle = '#d9f0ff'; g.strokeStyle = '#1b1b1b'; g.lineWidth = 6;
        for (const dx of [-22, 18]) { g.beginPath(); g.ellipse(sx + dx, sy + 18, 18, 14, 0, 0, 7); g.fill(); g.stroke(); }  // Taucherbrille
        g.beginPath(); g.moveTo(sx - 4, sy + 18); g.lineTo(sx, sy + 18); g.stroke();
        g.fillStyle = 'rgba(255,255,255,.7)'; for (let i = 0; i < 9; i++) { g.beginPath(); g.arc(sx - 10 + zufall() * 30, sy - 50 - i * 26, 5 + zufall() * 7, 0, 7); g.fill(); }
        g.restore();
        // Logo „GREVENER BÄDER“: das E als schwarz-rot-goldener Balken
        g.textBaseline = 'alphabetic'; g.font = '800 146px Arial, Helvetica, sans-serif'; g.fillStyle = '#1d4f92';
        const lx = W * .5, ly = H * .22;
        g.fillText('GR', lx, ly); const wGR = g.measureText('GR').width;
        const bx = lx + wGR + 12, bw = 96;
        [['#111111', 0], ['#d0021b', 1], ['#f5c400', 2]].forEach(([c, i]) => { g.fillStyle = c; g.fillRect(bx, ly - 80 + i * 22, bw, 22); });
        g.fillStyle = '#1d4f92'; g.fillText('VENER', bx + bw + 12, ly);
        g.fillText('BÄDER', lx, ly + 150);
        // Rand unten
        g.fillStyle = '#0a3672'; g.fillRect(0, H - 26, W, 26);
    });
}

/** Schild „EINGANG HALLENBAD“ (dunkelblaue Schrift auf hellblauem Band); fehlend: abgeschraubte Buchstaben. */
function schild(fehlend = '') {
    return leinwand(2048, 210, (g, W, H) => {
        const v = g.createLinearGradient(0, 0, 0, H); v.addColorStop(0, '#e2eff9'); v.addColorStop(1, '#b8d4ea');
        g.fillStyle = v; g.fillRect(0, 0, W, H);
        g.strokeStyle = '#6e93b5'; g.lineWidth = 8; g.strokeRect(4, 4, W - 8, H - 8);
        g.font = '500 112px Arial, Helvetica, sans-serif'; g.textBaseline = 'middle'; g.fillStyle = '#29466a';
        const text = 'EINGANG      HALLENBAD'; let x = 190;
        [...text].forEach((ch, i) => {
            const w = g.measureText(ch).width;
            if (fehlend.includes(String(i))) { g.fillStyle = 'rgba(41,70,106,.18)'; g.fillText(ch, x, H / 2 + 6); g.fillStyle = '#29466a'; for (const dy of [-26, 26]) { g.beginPath(); g.arc(x + w / 2, H / 2 + dy, 5, 0, 7); g.fill(); } }
            else { g.fillText(ch, x, H / 2 + 6); }
            x += w + 14;
        });
    });
}

/** Paneelband: gelbe Platten mit dunklen Fugen (Fassade des Hallenbads). */
function paneele(meterBreit, meterHoch, verwittert = true) {
    const px = 64;
    return leinwand(Math.ceil(meterBreit * px), Math.ceil(meterHoch * px), (g, W, H) => {
        saat = 3;
        const plat = 1.7 * px;
        for (let x = 0; x < W; x += plat) {
            for (let y = 0; y < H; y += H / 2) {
                const k = .92 + zufall() * .12;
                g.fillStyle = `rgb(${232 * k | 0},${186 * k | 0},${44 * k | 0})`; g.fillRect(x, y, plat, H / 2);
                if (verwittert) { for (let i = 0; i < 14; i++) { g.fillStyle = `rgba(120,100,60,${zufall() * .12})`; g.fillRect(x + zufall() * plat, y + zufall() * H / 2, 2 + zufall() * 10, 8 + zufall() * 30); } }
            }
            g.fillStyle = '#4a4740'; g.fillRect(x, 0, 4, H);
        }
        g.fillStyle = '#4a4740'; g.fillRect(0, H / 2 - 2, W, 4);
    });
}

function rohr(punkte, r, m, eltern) {
    const pfad = new THREE.CurvePath();
    for (let i = 0; i < punkte.length - 1; i++) { pfad.add(new THREE.LineCurve3(new THREE.Vector3(...punkte[i]), new THREE.Vector3(...punkte[i + 1]))); }
    const o = new THREE.Mesh(new THREE.TubeGeometry(pfad, punkte.length * 8, r, 10, false), m); o.castShadow = true; eltern.add(o); return o;
}
/** Gelber Umlaufbügel (Rohrrahmen) von x0 bis x1, Tiefe z. */
function buegel(x0, x1, z, h, m, eltern) {
    const k = .12;
    rohr([[x0, 0, z], [x0, h - k, z], [x0 + k, h, z], [x1 - k, h, z], [x1, h - k, z], [x1, 0, z]], .045, m, eltern);
}
function kugelleuchte(x, z, eltern, gelb, weiss) {
    const g = new THREE.Group(); g.position.set(x, 0, z); eltern.add(g);
    box(.14, 4.0, .14, gelb, 0, 2.0, 0, g);
    const kugeln = [[0, 4.25, 0], [-.55, 3.85, .1], [.55, 3.85, -.1], [-.25, 3.6, .5], [.3, 3.6, .45], [-.6, 3.15, -.3], [.6, 3.15, .3], [0, 3.1, -.55], [.05, 3.75, -.5]];
    for (const [kx, ky, kz] of kugeln) {
        rohr([[0, ky - .05, 0], [kx, ky - .05, kz]], .03, gelb, g);
        const k = new THREE.Mesh(new THREE.SphereGeometry(.27, 20, 14), weiss); k.position.set(kx, ky + .18, kz); k.castShadow = true; g.add(k);
    }
    return g;
}
/** Hartriegel mit weißen Blüten (der blühende Baum links im Bild). */
function bluetenbaum(x, z, eltern) {
    const g = new THREE.Group(); g.position.set(x, 0, z); eltern.add(g);
    const rinde = mat(0x5a4636, .9);
    rohr([[0, 0, 0], [.2, 2.2, .1], [-.3, 4.5, 0]], .22, rinde, g); rohr([[.2, 2.2, .1], [1.4, 4.2, .4]], .14, rinde, g); rohr([[-.1, 3.2, 0], [-1.6, 5.0, -.2]], .12, rinde, g);
    const bluete = leinwand(256, 256, (c, W, H) => { c.fillStyle = '#6f9a4a'; c.fillRect(0, 0, W, H); for (let i = 0; i < 900; i++) { c.fillStyle = zufall() < .62 ? '#f4f3ec' : (zufall() < .5 ? '#8fb862' : '#4f7a36'); c.beginPath(); c.arc(zufall() * W, zufall() * H, 2 + zufall() * 5, 0, 7); c.fill(); } });
    const krone = mat(0xffffff, .9, 0, { map: bluete });
    saat = 11;
    for (let i = 0; i < 26; i++) {
        const k = new THREE.Mesh(new THREE.IcosahedronGeometry(1.0 + zufall() * .9, 1), krone);
        k.position.set((zufall() - .5) * 6, 3.6 + zufall() * 3.6, (zufall() - .5) * 3.5); k.scale.y = .75; k.castShadow = true; g.add(k);
    }
    return g;
}
function hecke(x0, x1, z0, z1, h, eltern) {
    const laub = leinwand(256, 256, (c, W, H) => { c.fillStyle = '#3d5a2a'; c.fillRect(0, 0, W, H); for (let i = 0; i < 1600; i++) { c.fillStyle = zufall() < .5 ? '#55773a' : '#2c4420'; c.fillRect(zufall() * W, zufall() * H, 3, 3); } });
    laub.wrapS = laub.wrapT = THREE.RepeatWrapping; laub.repeat.set((x1 - x0) / 1.5, h / 1.5);
    const m = mat(0xffffff, .95, 0, { map: laub });
    const o = box(x1 - x0, h, z1 - z0, m, (x0 + x1) / 2, h / 2, (z0 + z1) / 2, eltern);
    for (let x = x0 + .4; x < x1; x += .7) { const k = new THREE.Mesh(new THREE.SphereGeometry(.45, 10, 8), m); k.position.set(x, h - .05, (z0 + z1) / 2 + (zufall() - .5) * .4); k.scale.set(1, .5, (z1 - z0) * 1.1); eltern.add(k); }
    return o;
}
function gelaender(punkte, h, m, eltern) {
    for (let i = 0; i < punkte.length - 1; i++) {
        const [a, b] = [punkte[i], punkte[i + 1]], L = Math.hypot(b[0] - a[0], b[1] - a[1]);
        for (const y of [h, h * .5]) { rohr([[a[0], y, a[1]], [b[0], y, b[1]]], .03, m, eltern); }
        for (let s = 0; s <= L + .01; s += 1.4) { const t = s / L; rohr([[a[0] + (b[0] - a[0]) * t, 0, a[1] + (b[1] - a[1]) * t], [a[0] + (b[0] - a[0]) * t, h, a[1] + (b[1] - a[1]) * t]], .03, m, eltern); }
    }
}
/** Bauzaunfeld (3,5 × 2 m, Gitter) mit Betonfüßen; von [x0,z0] nach [x1,z1]. */
function bauzaun(x0, z0, x1, z1, eltern) {
    const gitter = leinwand(256, 160, (c, W, H) => { c.clearRect(0, 0, W, H); c.strokeStyle = '#c9ccce'; c.lineWidth = 2; for (let x = 0; x <= W; x += 10) { c.beginPath(); c.moveTo(x, 0); c.lineTo(x, H); c.stroke(); } for (let y = 0; y <= H; y += 20) { c.beginPath(); c.moveTo(0, y); c.lineTo(W, y); c.stroke(); } c.lineWidth = 6; c.strokeRect(3, 3, W - 6, H - 6); });
    const L = Math.hypot(x1 - x0, z1 - z0), n = Math.max(1, Math.round(L / 3.5)), ang = Math.atan2(z1 - z0, x1 - x0);
    const gm = mat(0xffffff, .5, .6, { map: gitter, transparent: true, alphaTest: .3, side: THREE.DoubleSide });
    const fuss = mat(0xc0392b, .8), weiss = mat(0xeeeeee, .8);
    for (let i = 0; i < n; i++) {
        const t = (i + .5) / n, x = x0 + (x1 - x0) * t, z = z0 + (z1 - z0) * t;
        const p = new THREE.Mesh(new THREE.PlaneGeometry(L / n - .05, 2), gm); p.position.set(x, 1.12, z); p.rotation.y = -ang; eltern.add(p);
        for (const s of [-.5, .5]) { const fx = x + Math.cos(ang) * s * L / n, fz = z + Math.sin(ang) * s * L / n; box(.65, .14, .22, s < 0 ? fuss : weiss, fx, .07, fz, eltern).rotation.y = -ang; }
    }
}

/**
 * Baut den Eingangsbereich in die Gruppe eltern. baustelle: Vorplatz aufgerissen, Bauzaun, Zettel an der Tür, Leiter am Schild.
 * Liefert markante Punkte (für Figuren und Kamera).
 */
export function eingangHallenbad(eltern, { baustelle = false, welt = false } = {}) {
    saat = 99;
    const weissPutz = meterMaterial('putz', { color: 0xf0efea }), grau = mat(0x8e9296, .8), dunkel = mat(0x2a2c30, .6), dach = mat(0x1d1e21, .7);
    const gelb = mat(0xf2c12e, .45, .35), blau = mat(0x2f6fb0, .5), glas = mat(0x16202a, .15, .5, { envMapIntensity: .6 }), weiss = mat(0xf7f6f0, .35, 0, { emissive: 0xfff1d6, emissiveIntensity: .08 });

    // ---------- hohe Halle dahinter (gelbes Paneelband oben)
    const halle = new THREE.Group(); eltern.add(halle);
    box(26, 5.0, 11, weissPutz, 9, 2.5, -11.5, halle);
    const band = mat(0xffffff, .7, 0, { map: paneele(26, 3.0) });
    const b1 = new THREE.Mesh(new THREE.PlaneGeometry(26, 3.0), band); b1.position.set(9, 6.5, -5.98); halle.add(b1);
    box(26.2, 3.0, 10.9, mat(0xe8b92c, .7), 9, 6.5, -11.5, halle);
    b1.position.z = -5.99 + .0; b1.renderOrder = 1;
    box(26.4, .45, 11.3, dach, 9, 8.2, -11.5, halle);
    for (let x = -2; x < 21; x += 3.1) { box(.03, 1.0, .03, grau, x, 8.9, -6.5, halle); }   // Antennen und Blitzableiter auf dem Dach
    // linker Absatz der Halle (grauer Giebel über dem Pavillon)
    box(4, 6.4, 5, mat(0xd6d7d4, .85), -2.2, 3.2, -8.6, halle);
    box(4.2, .3, 5.2, dach, -2.2, 6.5, -8.6, halle);

    // ---------- Pavillon mit Wandbild (links)
    const pav = new THREE.Group(); eltern.add(pav);
    box(7.2, 3.4, 6, weissPutz, -5.0, 1.7, -3.0, pav);
    box(7.5, .32, 6.3, dach, -5.0, 3.55, -3.0, pav);
    const bild = new THREE.Mesh(new THREE.PlaneGeometry(6.7, 3.05), mat(0xd8d8d8, .9, 0, { map: wandbild() }));
    bild.position.set(-4.95, 1.62, .012); bild.name = 'wandbild'; pav.add(bild);
    box(.9, .6, .05, glas, -8.0, 2.2, -1.2, pav).rotation.y = Math.PI / 2;                     // kleines Fenster an der Seite
    box(.2, 3.4, .2, grau, -8.55, 1.7, 0, pav);

    // ---------- Eingangsnische: Schildband, blaue Pfeiler, Glastüren
    const ein = new THREE.Group(); eltern.add(ein);
    const schildHeil = schild(''), schildKaputt = schild('0123456');
    const schildMat = mat(0xffffff, .55, 0, { map: schildHeil });
    const s = new THREE.Mesh(new THREE.BoxGeometry(5.8, .62, .3), [grau, grau, grau, grau, schildMat, grau]);
    s.position.set(1.35, 2.98, .12); s.castShadow = true; s.name = 'schild'; ein.add(s);
    box(6.0, .32, 1.4, dach, 1.35, 3.45, -.55, ein);
    box(5.8, .2, 1.1, mat(0xdfe3e6, .8), 1.35, 2.58, -.5, ein);                                    // Untersicht
    const pfeilerTex = leinwand(64, 256, (c, W, H) => { const v = c.createLinearGradient(0, 0, 0, H); v.addColorStop(0, '#9fcdf0'); v.addColorStop(1, '#2d6fb6'); c.fillStyle = v; c.fillRect(0, 0, W, H); c.strokeStyle = 'rgba(255,255,255,.5)'; c.lineWidth = 3; for (let y = 20; y < H; y += 26) { c.beginPath(); c.moveTo(0, y); c.quadraticCurveTo(W / 2, y - 8, W, y); c.stroke(); } });
    for (const x of [-1.35, 4.05]) { box(.36, 2.6, .36, mat(0xffffff, .6, 0, { map: pfeilerTex }), x, 1.3, -.1, ein); }
    // zurückgesetzte Türwand: dunkle Glasfront, zwei Türen mit gelben Griffen
    box(5.6, 3.3, 5.0, weissPutz, 1.35, 1.65, -3.55, ein);   // Baukörper hinter der Türwand
    box(5.8, .3, 5.2, dach, 1.35, 3.45, -3.55, ein);
    box(5.0, 2.5, .12, dunkel, 1.35, 1.25, -1.0, ein);
    for (const [x, b] of [[-.3, 1.5], [3.0, 1.6]]) { box(b, 2.3, .05, glas, x, 1.2, -.92, ein); }
    for (const x of [.95, 1.95]) {
        box(.98, 2.25, .06, glas, x, 1.13, -.9, ein);
        box(.05, .7, .06, gelb, x + (x < 1.4 ? .38 : -.38), 1.1, -.83, ein);
        box(.98, .05, .06, dunkel, x, 2.26, -.88, ein);
    }
    // Aushänge an der Tür
    const zettel = (txt, w, h, farbe = '#ffffff') => leinwand(256, Math.round(256 * h / w), (c, W, H) => { c.fillStyle = farbe; c.fillRect(0, 0, W, H); c.fillStyle = '#222'; c.font = 'bold 26px Arial'; c.textAlign = 'center'; txt.split('\n').forEach((t, i, a) => c.fillText(t, W / 2, H / 2 + (i - (a.length - 1) / 2) * 32 + 9)); });
    const aushang = (txt, x, y, w, h, farbe) => { const p = new THREE.Mesh(new THREE.PlaneGeometry(w, h), mat(0xffffff, .9, 0, { map: zettel(txt, w, h, farbe) })); p.position.set(x, y, -.86); ein.add(p); return p; };
    if (baustelle) { aushang('Wegen Sanierung\ngeschlossen\n\nWiedereröffnung:\nbald', 1.45, .7, .55, .75); aushang('Zutritt nur mit\nHelm', 2.45, 1.4, .42, .32, '#f5c400'); }
    else { aushang('Öffnungszeiten', 1.45, 1.55, .3, .4); aushang('Haus-\nordnung', 2.45, 1.55, .3, .4); }

    // ---------- rechter Flügel (Umkleiden): springt laut Luftbild und LoD2 rund 5,8 m vor die Eingangsfront vor.
    // Die Tür liegt in der Ecke; die Seitenwand des Vorsprungs bildet die rechte Wand des Eingangshofs.
    const VOR = 5.8, XL = 4.3, XR = 20.3;
    const fl = new THREE.Group(); eltern.add(fl);
    box(XR - XL, 2.45, VOR + 5, dunkel, (XL + XR) / 2, 1.22, (VOR - 5) / 2, fl);
    // Front (zum Betrachter): dunkle Fensterreihe, gelbes Paneelband
    for (let x = XL + .3; x < XR - 1; x += 1.7) { box(1.62, 2.2, .04, glas, x + .85, 1.15, VOR + .01, fl); box(.07, 2.45, .1, grau, x, 1.22, VOR + .02, fl); }
    const bandR = new THREE.Mesh(new THREE.PlaneGeometry(XR - XL, 1.0), mat(0xffffff, .7, 0, { map: paneele(XR - XL, 1.0) })); bandR.position.set((XL + XR) / 2, 2.95, VOR + .04); fl.add(bandR);
    box(XR - XL, 1.0, VOR + 5, mat(0xe8b92c, .7), (XL + XR) / 2, 2.95, (VOR - 5) / 2, fl);
    box(XR - XL + .4, .35, VOR + 5.6, dach, (XL + XR) / 2, 3.6, (VOR - 5) / 2 + .2, fl);
    // Seitenwand zum Eingang: Paneelband oben, unten zwei Fenster, Schaukasten neben der Tür
    const bandS = new THREE.Mesh(new THREE.PlaneGeometry(VOR + 1, 1.0), mat(0xffffff, .7, 0, { map: paneele(VOR + 1, 1.0) })); bandS.rotation.y = -Math.PI / 2; bandS.position.set(XL - .04, 2.95, (VOR - 1) / 2); fl.add(bandS);
    for (const z of [2.6, 4.4]) { const fw = box(.04, 1.4, 1.5, glas, XL - .02, 1.45, z, fl); fw.name = 'fenster'; }
    const kasten = box(.12, 1.25, .95, gelb, XL - .08, 1.55, .8, fl);
    const plakat = new THREE.Mesh(new THREE.PlaneGeometry(.78, 1.08), mat(0xffffff, .8, 0, { map: zettel('Kursplan\nAquafitness\nSchwimmkurse', .78, 1.08, '#dfeefa') })); plakat.rotation.y = -Math.PI / 2; plakat.position.set(-.065, 0, 0); kasten.add(plakat);
    hecke(XL + .6, XR - 4, VOR + .1, VOR + 1.3, 1.05, fl);
    hecke(XL - 1.1, XL - .1, 1.8, VOR - .2, .9, fl);
    const lampe = new THREE.Group(); lampe.position.set(XL - 1.4, 0, VOR + .6); fl.add(lampe);
    box(.1, 3.4, .1, mat(0x24457e, .5, .4), 0, 1.7, 0, lampe);
    const kugel = new THREE.Mesh(new THREE.SphereGeometry(.26, 20, 14), weiss); kugel.position.y = 3.6; lampe.add(kugel);
    box(.4, .6, .3, gelb, XL + .4, .3, VOR + 1.6, fl); box(.42, .12, .32, grau, XL + .4, .66, VOR + 1.6, fl);         // Kartenautomat
    // Fahne mit Logo
    const fahne = leinwand(128, 640, (c, W, H) => { c.fillStyle = '#2c3550'; c.fillRect(0, 0, W, H); c.strokeStyle = '#f0a43a'; c.lineWidth = 10; c.beginPath(); c.arc(W * 1.1, H * .3, W * .9, Math.PI * .55, Math.PI * 1.35); c.stroke(); c.fillStyle = '#e8e8ea'; c.font = 'bold 40px Arial'; c.save(); c.translate(W / 2 + 10, H * .55); c.rotate(-Math.PI / 2); c.fillText('SPORT', -60, 0); c.restore(); });
    box(.05, 4.6, .05, grau, 13.6, 2.3, VOR + 3.2, fl);
    const f = new THREE.Mesh(new THREE.PlaneGeometry(.75, 3.4), mat(0xffffff, .8, 0, { map: fahne, side: THREE.DoubleSide })); f.position.set(13.98, 2.85, VOR + 3.2); f.rotation.y = .4; fl.add(f);
    gelaender([[12.6, VOR + 1.6], [12.6, VOR + 4.2], [18, VOR + 4.6]], 1.05, gelb, fl);

    // ---------- links: Kugel-Leuchtengruppe, blühender Baum, Hecke, Geländer zum Sportplatz
    const li = new THREE.Group(); eltern.add(li);
    kugelleuchte(-10.4, 3.4, li, gelb, weiss);
    bluetenbaum(-14.5, -.5, li);
    hecke(-13.5, -8.6, .5, 3.0, 1.3, li);
    gelaender([[-19, 4.4], [-13.8, 4.4], [-13.8, 1.2]], 1.0, gelb, li);
    if (!welt) {
        const platz = new THREE.Mesh(new THREE.PlaneGeometry(30, 14), mat(0xb5523e, .9)); platz.rotation.x = -Math.PI / 2; platz.position.set(-30, -.4, -6); platz.receiveShadow = true; li.add(platz);
        const rasen = new THREE.Mesh(new THREE.PlaneGeometry(24, 8), mat(0x5f8a3c, .95)); rasen.rotation.x = -Math.PI / 2; rasen.position.set(-30, -.38, -6); li.add(rasen);
    }
    // Mülltonnen
    const tonne = new THREE.Group(); tonne.position.set(-11.6, 0, 8.5); li.add(tonne);
    box(.75, 1.0, .85, mat(0x2f6b3a, .55), 0, .5, 0, tonne); box(.8, .08, .9, mat(0x2a5f33, .5), 0, 1.04, 0, tonne);
    box(.42, .8, .42, mat(0x1f3f8a, .5), -9.7, .4, 7.2, li); box(.44, .22, .44, mat(0xf5c400, .5), -9.7, .91, 7.2, li);

    // ---------- Vorplatz (graues Pflaster) und die gelben Bügel
    const vor = new THREE.Group(); eltern.add(vor);
    const GRUBE = baustelle ? [-3.6, 3.4, 3.0, 8.2] : null;   // x0, x1, z0, z1
    const pflaster = (x0, x1, z0, z1) => {
        const w = x1 - x0, d = z1 - z0; if (w <= 0 || d <= 0) { return; }
        const p = new THREE.Mesh(new THREE.PlaneGeometry(w, d), meterMaterial('pflaster', { color: 0xa2a2a0 }, [w / 2, d / 2]));
        p.rotation.x = -Math.PI / 2; p.position.set((x0 + x1) / 2, .005, (z0 + z1) / 2); p.receiveShadow = true; p.name = 'ground'; vor.add(p);
    };
    if (!GRUBE) { pflaster(-16, 22, -1, 18); }
    else {
        const [gx0, gx1, gz0, gz1] = GRUBE;
        pflaster(-16, 22, -1, gz0); pflaster(-16, 22, gz1, 18); pflaster(-16, gx0, gz0, gz1); pflaster(gx1, 22, gz0, gz1);
    }
    for (const [x0, x1, z0, z1] of welt ? [] : [[-100, 100, 18, 100], [-100, 100, -100, -1], [-100, -16, -1, 18], [22, 100, -1, 18]]) {
        const um = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, z1 - z0), mat(0x627a3c, .95)); um.rotation.x = -Math.PI / 2; um.position.set((x0 + x1) / 2, -.02, (z0 + z1) / 2); um.receiveShadow = true; vor.add(um);
    }
    buegel(-8.6, -4.9, 10.2, 1.0, gelb, vor);
    buegel(-3.4, 1.6, 10.8, 1.0, gelb, vor);
    buegel(3.8, 8.6, 10.2, 1.0, gelb, vor);

    const punkte = { tuer: [1.45, -.6], vorTuer: [1.4, 1.6], wandbild: [-4.6, 1.5], schild: [1.35, 3.0] };
    const teile = { wandbild: bild, schild: s, schildMat, schildHeil, schildKaputt, punkte };

    // ---------- Baustelle
    if (baustelle) {
        const bs = new THREE.Group(); eltern.add(bs);
        const grubeG = new THREE.Group(); grubeG.name = 'grube'; bs.add(grubeG);
        const [gx0, gx1, gz0, gz1] = GRUBE, tief = 1.6;
        const erde = leinwand(256, 128, (c, W, H) => { c.fillStyle = '#6e5137'; c.fillRect(0, 0, W, H); for (let i = 0; i < 2200; i++) { c.fillStyle = zufall() < .5 ? '#5a412b' : '#86664a'; c.fillRect(zufall() * W, zufall() * H, 2, 2); } c.fillStyle = '#8a8580'; c.fillRect(0, 0, W, 10); });
        const em = mat(0xffffff, .95, 0, { map: erde, side: THREE.DoubleSide });
        const wand = (w, x, z, ry) => { const p = new THREE.Mesh(new THREE.PlaneGeometry(w, tief), em); p.position.set(x, -tief / 2, z); p.rotation.y = ry; p.receiveShadow = true; grubeG.add(p); };
        wand(gx1 - gx0, (gx0 + gx1) / 2, gz0, 0); wand(gx1 - gx0, (gx0 + gx1) / 2, gz1, Math.PI); wand(gz1 - gz0, gx0, (gz0 + gz1) / 2, Math.PI / 2); wand(gz1 - gz0, gx1, (gz0 + gz1) / 2, -Math.PI / 2);
        const sohle = new THREE.Mesh(new THREE.PlaneGeometry(gx1 - gx0, gz1 - gz0), mat(0x5a412b, .95)); sohle.rotation.x = -Math.PI / 2; sohle.position.set((gx0 + gx1) / 2, -tief, (gz0 + gz1) / 2); grubeG.add(sohle);
        // Klickfläche der Grube (unsichtbar, unter der Leitung)
        const klick = new THREE.Mesh(new THREE.BoxGeometry(gx1 - gx0, .9, gz1 - gz0 - 1.2), new THREE.MeshBasicMaterial({ visible: false })); klick.position.set((gx0 + gx1) / 2, -1.1, (gz0 + gz1) / 2 + .6); grubeG.add(klick);
        // das Siegel des Rechts im alten Schacht
        const siegel = new THREE.Mesh(new THREE.CylinderGeometry(.2, .2, .05, 24), mat(0xd4af37, .2, .9, { emissive: 0x5a4200 })); siegel.position.set(-2.6, -tief + .3, 4.6); siegel.castShadow = true; grubeG.add(siegel);
        // alter Schacht: Betonring am Grund, das Siegel liegt obenauf (unter Wasser)
        const ring = new THREE.Mesh(new THREE.CylinderGeometry(.42, .42, .28, 20, 1, true), mat(0x7b7e80, .85, 0, { side: THREE.DoubleSide })); ring.position.set(-2.6, -tief + .14, 4.6); grubeG.add(ring);
        const wasser = new THREE.Mesh(new THREE.PlaneGeometry(gx1 - gx0 - .05, gz1 - gz0 - .05), mat(0x5c6e64, .1, .3, { transparent: true, opacity: .85 })); wasser.rotation.x = -Math.PI / 2; wasser.position.set((gx0 + gx1) / 2, -.55, (gz0 + gz1) / 2); wasser.name = 'grubenwasser'; grubeG.add(wasser);
        // die alte Zuleitung zum Becken quer durch die Grube, mit Leck
        const leitungG = new THREE.Group(); leitungG.name = 'leitung'; bs.add(leitungG);
        const leitung = rohr([[gx0 - .2, -.3, 4.9], [gx1 + .2, -.3, 5.5]], .11, mat(0x6a6f73, .5, .6), leitungG);
        const lk = new THREE.Mesh(new THREE.BoxGeometry(gx1 - gx0 + .4, .6, .8), new THREE.MeshBasicMaterial({ visible: false })); lk.position.set((gx0 + gx1) / 2, -.3, 5.2); lk.rotation.y = -.085; leitungG.add(lk);
        const strahl = new THREE.Group(); strahl.position.set(.6, -.22, 5.25); strahl.name = 'strahl'; bs.add(strahl);
        for (let i = 0; i < 9; i++) { const t = new THREE.Mesh(new THREE.SphereGeometry(.05 + i * .01, 8, 6), mat(0xd8ecf5, .1, 0, { transparent: true, opacity: .7 })); t.position.set(i * .06, Math.sin(i / 3) * .3, i * .05); strahl.add(t); }
        // abgeräumtes Pflaster auf einer Palette, Pumpe mit Schlauch
        box(1.2, .14, 1.0, mat(0xb08a5a, .8), -6.4, .07, 5.2, bs);
        for (let i = 0; i < 4; i++) { box(1.1, .12, .9, meterMaterial('pflaster', { color: 0xb8b4ae }, [.5, .5]), -6.4, .2 + i * .13, 5.2, bs); }
        box(.6, .5, .45, mat(0xd35400, .6), 4.6, .25, 9.2, bs);
        rohr([[4.4, .2, 9.0], [3.9, .02, 8.8], [3.3, .02, 8.4], [3.0, -.5, 7.9]], .05, mat(0x1a1a1a, .7), bs);
        // Bauzaun um die Grube und vor der Tür (Lücke zum Durchgehen links)
        bauzaun(-4.2, 2.4, -4.2, 8.8, bs); bauzaun(-4.2, 8.8, 4.0, 8.8, bs); bauzaun(4.0, 2.4, 4.0, 8.8, bs); bauzaun(-4.2, 2.4, 1.7, 2.4, bs);
        // Einstiegsleiter im Durchgang: von der Kante (z 2,6) schräg hinab auf die Sohle (z 4,0)
        const einstieg = new THREE.Group(); einstieg.position.set(2.55, 0, 2.6); einstieg.rotation.x = Math.atan2(1.4, 1.75); bs.add(einstieg);
        const alu2 = mat(0xc8ccd0, .4, .7);
        for (const x of [-.3, .3]) { box(.06, .06, 2.4, alu2, x, -.1, 1.1, einstieg); }
        for (let k = 0; k < 7; k++) { box(.6, .04, .04, alu2, 0, -.1, .1 + k * .32, einstieg); }
        // Leiter am Schild (hier schrauben Pinsel und Ledeiw)
        const leiter = new THREE.Group(); leiter.name = 'leiter'; bs.add(leiter);
        const leiterKippe = new THREE.Group(); leiterKippe.position.set(-.2, 0, .9); leiterKippe.rotation.x = -.28; leiter.add(leiterKippe);
        const alu = mat(0xc8ccd0, .4, .7);
        for (const x of [-.25, .25]) { box(.06, 3.1, .06, alu, x, 1.55, 0, leiterKippe); }
        for (let y = .3; y < 3.0; y += .3) { box(.5, .04, .04, alu, 0, y, 0, leiterKippe); }
        // abgeschraubte Buchstaben im Stapel am Tor
        const buchstaben = new THREE.Group(); buchstaben.name = 'buchstaben'; buchstaben.position.set(-8.4, 0, 15.6); bs.add(buchstaben);
        [...'EINGANG'].forEach((ch, i) => {
            const t = leinwand(128, 128, (c, W, H) => { c.fillStyle = '#29466a'; c.font = 'bold 116px Arial'; c.textAlign = 'center'; c.fillText(ch, W / 2, H * .86); });
            const p = new THREE.Mesh(new THREE.PlaneGeometry(.42, .42), mat(0xffffff, .6, 0, { map: t, transparent: true, alphaTest: .3, side: THREE.DoubleSide }));
            p.position.set((i % 4) * .4 - .6, .03 + i * .012, Math.floor(i / 4) * .45 - .2); p.rotation.set(-Math.PI / 2, 0, (i * 1.7) % 1 - .5); buchstaben.add(p);
        });
        box(.12, .12, .12, mat(0xf2f2f2, .6), .9, .06, .3, buchstaben);                       // Joghurtbecher mit den Schrauben
        const bk = new THREE.Mesh(new THREE.BoxGeometry(2.0, .5, 1.2), new THREE.MeshBasicMaterial({ visible: false })); bk.position.y = .25; buchstaben.add(bk);
        // Bauschild
        const bauschild = leinwand(512, 320, (c, W, H) => { c.fillStyle = '#ffffff'; c.fillRect(0, 0, W, H); c.fillStyle = '#1d4f92'; c.fillRect(0, 0, W, 70); c.fillStyle = '#fff'; c.font = 'bold 40px Arial'; c.fillText('Stadt Greven', 20, 50); c.fillStyle = '#222'; c.font = 'bold 34px Arial'; c.fillText('Sanierung Hallenbad', 20, 130); c.font = '26px Arial'; c.fillText('Bauleitung: Frau Kemper', 20, 190); c.fillText('Wiedereröffnung: bald', 20, 240); });
        box(.08, 2.6, .08, grau, 6.6, 1.3, 8.6, bs); box(.08, 2.6, .08, grau, 8.2, 1.3, 8.6, bs);
        const bsch = new THREE.Mesh(new THREE.PlaneGeometry(1.8, 1.12), mat(0xffffff, .7, 0, { map: bauschild })); bsch.position.set(7.4, 2.1, 8.65); bs.add(bsch);
        punkte.grube = [(gx0 + gx1) / 2, (gz0 + gz1) / 2]; punkte.leiter = [-.2, 1.4];
        Object.assign(teile, { grube: grubeG, wasser, siegel, leitung: leitungG, strahl, leiter, buchstaben, bauschild: bsch });
    }
    return teile;
}
