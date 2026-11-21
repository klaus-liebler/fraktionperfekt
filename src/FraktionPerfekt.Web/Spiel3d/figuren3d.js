// Joe & Chani 3D – Figuren aus einfachen Körpern (Gelenk-Rig: Hüfte, Rumpf, Hals, Kopf, Arme, Beine) mit Posen.
import * as THREE from './three_min.js';

const mat = (c, r = .7, m = 0) => new THREE.MeshStandardMaterial({ color: c, roughness: r, metalness: m });
function mesh(geo, material, parent, x = 0, y = 0, z = 0) { const m = new THREE.Mesh(geo, material); m.position.set(x, y, z); m.castShadow = true; m.receiveShadow = true; parent.add(m); return m; }
const joint = (parent, x, y, z) => { const g = new THREE.Group(); g.position.set(x, y, z); parent.add(g); return g; };
const seg = (parent, len, r0, r1, material) => { const g = new THREE.CylinderGeometry(r1, r0, len, 14, 1); g.translate(0, -len / 2, 0); mesh(new THREE.SphereGeometry(r0, 14, 10), material, parent); return mesh(g, material, parent); };
function torso(profile, material, depth) { const g = new THREE.LatheGeometry(profile.map(([r, y]) => new THREE.Vector2(r, y)), 28); g.scale(1, 1, depth); const m = new THREE.Mesh(g, material); m.castShadow = true; return m; }

/** Grundfigur; o beschreibt Farben, Rumpfprofil und Zubehör. */
function character(o) {
    const root = new THREE.Group(), s = o.scale;
    const skin = mat(o.skin, .55), cloth = mat(o.top, o.topRough || .8, o.topMetal || 0), shirt = mat(o.shirt, .6), legsM = mat(o.legs, .8), shoe = mat(o.shoes, .45);
    const hips = joint(root, 0, .98 * s, 0);
    mesh(new THREE.SphereGeometry(.17 * s, 20, 14).scale(1.05 * (o.bauch || 1), .7, .75 * (o.bauch || 1)), legsM, hips, 0, -.02 * s, 0);
    const spine = joint(hips, 0, .02 * s, 0);
    spine.add(torso(o.torsoProfile.map(([r, y]) => [r * s, y * s]), cloth, o.torsoDepth));
    o.dressFront(spine, s, shirt, cloth);
    const neck = joint(spine, 0, .52 * s, 0);
    mesh(new THREE.CylinderGeometry(.055 * s, .062 * s, .12 * s, 14), skin, neck, 0, .04 * s, 0);
    const head = joint(neck, 0, .09 * s, .01 * s); head.scale.setScalar(1.25);
    mesh(new THREE.SphereGeometry(.105 * s, 28, 20).scale(o.headW, o.headH, 1.08), skin, head, 0, .11 * s, 0);
    const eyeW = mat(0xffffff, .3), iris = mat(o.eyes, .3);
    for (const x of [-.036, .036]) {
        const e = mesh(new THREE.SphereGeometry(.015 * s, 14, 10), eyeW, head, x * s, .125 * s, .1 * s); e.scale.z = .5;
        mesh(new THREE.SphereGeometry(.008 * s, 10, 8), iris, head, x * s, .125 * s, .107 * s);
        const br = mesh(new THREE.BoxGeometry(.034 * s, .006 * s, .01 * s), mat(o.brows, .8), head, x * s, .148 * s, .104 * s); br.rotation.z = -x * 3;
        mesh(new THREE.SphereGeometry(.022 * s, 10, 8).scale(.45, 1, .6), skin, head, Math.sign(x) * .108 * s * o.headW, .115 * s, -.005 * s);
    }
    const nose = new THREE.ConeGeometry(.016 * s, .045 * s, 10); nose.rotateX(-Math.PI / 2 + .35); mesh(nose, skin, head, 0, .1 * s, .118 * s);
    const mouth = mesh(new THREE.TorusGeometry(.022 * s, .004 * s, 6, 16, Math.PI * .8), mat(o.lips, .5), head, 0, .068 * s, .104 * s);
    mouth.rotation.z = Math.PI + Math.PI * .1;
    o.headExtras(head, s);
    const arms = [];
    const sleeve = o.sleeveMat ? mat(o.sleeveMat, .8) : cloth;
    for (const side of [-1, 1]) {
        const sh = joint(spine, side * .2 * s * (o.schulter || 1), .46 * s, 0);
        mesh(new THREE.SphereGeometry(.06 * s, 14, 10), cloth, sh);
        seg(sh, .28 * s, .052 * s, .045 * s, sleeve);
        const el = joint(sh, 0, -.28 * s, 0);
        seg(el, .25 * s, .044 * s, .036 * s, sleeve);
        if (o.cuff) { mesh(new THREE.CylinderGeometry(.037 * s, .037 * s, .03 * s, 12), shirt, el, 0, -.24 * s, 0); }
        const hand = mesh(new THREE.SphereGeometry(.042 * s, 14, 10).scale(.8, 1.25, .55), skin, el, 0, -.3 * s, 0);
        arms.push({ sh, el, hand });
    }
    if (o.handExtras) { o.handExtras(arms, s); }
    const legs = [];
    for (const side of [-1, 1]) {
        const hp = joint(hips, side * .09 * s, -.05 * s, 0);
        seg(hp, .44 * s, .075 * s, .058 * s, legsM);
        const kn = joint(hp, 0, -.44 * s, 0);
        seg(kn, .42 * s, .057 * s, .045 * s, legsM);
        const ft = joint(kn, 0, -.43 * s, 0);
        mesh(new THREE.BoxGeometry(.1 * s, .07 * s, .26 * s), shoe, ft, 0, -.02 * s, .05 * s);
        legs.push({ hp, kn, ft });
    }
    root.userData = { hips, spine, head, neck, arms, legs, s, mouth, phase: Math.random() * 6 };
    return root;
}

const blazer = (cloth, shirt, s, sp, krawatte) => {
    const v = new THREE.Shape([new THREE.Vector2(-.075 * s, .52 * s), new THREE.Vector2(.075 * s, .52 * s), new THREE.Vector2(0, .3 * s)]);
    mesh(new THREE.ShapeGeometry(v), shirt, sp, 0, 0, .152 * s);
    for (const side of [-1, 1]) {
        const lap = mesh(new THREE.BoxGeometry(.06 * s, .2 * s, .015 * s), cloth, sp, side * .078 * s, .4 * s, .153 * s); lap.rotation.z = -side * .35;
    }
    if (krawatte) { const k = mesh(new THREE.BoxGeometry(.03 * s, .2 * s, .008 * s), mat(krawatte, .5), sp, 0, .4 * s, .156 * s); k.rotation.z = .02; }
};

/** Joe: Hut und Lederjacke, randlose Brille, Schnauzer, grauer Haarkranz. Hut erst nach der Garderobe (userData.hut). */
export function joe() {
    const c = character({
        scale: 1.04, skin: 0xe9b896, top: 0x5b3a22, topRough: .45, topMetal: .05, shirt: 0xf6f6f2, legs: 0x3b3f48, shoes: 0x4a3324, eyes: 0x5a4632, brows: 0x8f8a84, lips: 0xb0705e,
        headW: 1.02, headH: 1.08, torsoDepth: .72, schulter: 1.05,
        torsoProfile: [[.175, -.05], [.205, .05], [.21, .2], [.22, .36], [.225, .44], [.165, .5], [.06, .54], [0, .545]],
        dressFront(sp, s, shirt, cloth) {
            // Lederjacke: offener Kragen, Reißverschluss, aufgesetzte Taschen
            const v = new THREE.Shape([new THREE.Vector2(-.07 * s, .52 * s), new THREE.Vector2(.07 * s, .52 * s), new THREE.Vector2(0, .34 * s)]);
            mesh(new THREE.ShapeGeometry(v), shirt, sp, 0, 0, .156 * s);
            const leder = mat(0x4a2e1a, .4, .05);
            for (const side of [-1, 1]) {
                const k = mesh(new THREE.BoxGeometry(.09 * s, .07 * s, .02 * s), leder, sp, side * .07 * s, .5 * s, .145 * s); k.rotation.z = side * .55;
                mesh(new THREE.BoxGeometry(.07 * s, .05 * s, .012 * s), leder, sp, side * .1 * s, .14 * s, .158 * s);
            }
            mesh(new THREE.BoxGeometry(.006 * s, .32 * s, .006 * s), mat(0xb8b0a0, .3, .8), sp, .012 * s, .18 * s, .16 * s);
            sp.userData.leder = [cloth, leder];
        },
        headExtras(h, s) {
            const grey = mat(0xb9b6b0, .9);
            const r = mesh(new THREE.SphereGeometry(.112 * s, 28, 14, Math.PI * .82, Math.PI * 1.36, Math.PI * .4, Math.PI * .24), grey, h, 0, .115 * s, -.004 * s); r.scale.set(1.03, 1.08, 1.08);
            const m = new THREE.CapsuleGeometry(.009 * s, .05 * s, 4, 10); m.rotateZ(Math.PI / 2); mesh(m, mat(0x8a8178, .9), h, 0, .081 * s, .112 * s).scale.set(1, 1.1, .8);
            const lens = new THREE.MeshStandardMaterial({ color: 0xe8f2f8, roughness: .05, metalness: .1, transparent: true, opacity: .25 });
            const metal = mat(0x6b6e72, .3, .8);
            for (const x of [-.037, .037]) {
                mesh(new THREE.BoxGeometry(.05 * s, .028 * s, .003 * s), lens, h, x * s, .124 * s, .118 * s).castShadow = false;
                mesh(new THREE.BoxGeometry(.05 * s, .003 * s, .004 * s), metal, h, x * s, .139 * s, .118 * s);
                mesh(new THREE.BoxGeometry(.003 * s, .003 * s, .11 * s), metal, h, Math.sign(x) * .108 * s, .136 * s, .065 * s);
            }
            // Fedora: Krempe, Krone mit Kniff, Hutband
            const hut = new THREE.Group(); hut.position.set(0, .2 * s, -.005 * s); hut.rotation.x = -.08; h.add(hut);
            const filz = mat(0x5e4630, .85), band = mat(0x2a1d14, .7);
            mesh(new THREE.CylinderGeometry(.19 * s, .19 * s, .012 * s, 32), filz, hut).scale.z = .92;
            const krone = mesh(new THREE.CylinderGeometry(.095 * s, .112 * s, .1 * s, 28), filz, hut, 0, .05 * s, 0); krone.scale.z = .9;
            mesh(new THREE.CylinderGeometry(.113 * s, .114 * s, .025 * s, 28), band, hut, 0, .016 * s, 0).scale.z = .91;
            mesh(new THREE.BoxGeometry(.02 * s, .02 * s, .16 * s), mat(0x4e3a28, .85), hut, 0, .1 * s, 0);
            h.userData.hut = hut;
        }
    });
    c.userData.hut = c.userData.head.userData.hut;
    // Ohne Jacke: Hemdsärmel (hellblaues Hemd); die Lederjacke hängt dann an der Garderobe
    const [cloth, leder] = c.userData.spine.userData.leder;
    c.userData.jacke = (an) => { cloth.color.set(an ? 0x5b3a22 : 0x9fb8cc); cloth.roughness = an ? .45 : .75; leder.color.set(an ? 0x4a2e1a : 0x8aa6bc); };
    return c;
}

export function chani() {
    return character({
        scale: .96, skin: 0xf3d0b6, top: 0x4c5a6c, shirt: 0x3c4858, legs: 0x2e3038, shoes: 0x27262a, eyes: 0x5f86a8, brows: 0x8a6a4c, lips: 0xc7807a,
        headW: .95, headH: 1.12, torsoDepth: .68, sleeveMat: 0x4c5a6c,
        torsoProfile: [[.165, -.05], [.185, .05], [.165, .18], [.175, .3], [.19, .42], [.15, .49], [.055, .535], [0, .54]],
        dressFront(sp, s, shirt) {
            for (const side of [-1, 1]) { const c = mesh(new THREE.BoxGeometry(.08 * s, .055 * s, .012 * s), shirt, sp, side * .045 * s, .515 * s, .125 * s); c.rotation.z = side * .55; c.rotation.y = side * .35; }
            mesh(new THREE.BoxGeometry(.012 * s, .42 * s, .006 * s), shirt, sp, 0, .28 * s, .135 * s);
            // Umhängetasche (darin u. a. das Handy für Live-Storys)
            const gurt = mesh(new THREE.BoxGeometry(.02 * s, .62 * s, .01 * s), mat(0x7a4e2e, .6), sp, 0, .26 * s, .14 * s); gurt.rotation.z = .62;
            mesh(new THREE.BoxGeometry(.2 * s, .16 * s, .06 * s), mat(0x8a5a34, .6), sp, .2 * s, -.02 * s, .05 * s);
        },
        headExtras(h, s) {
            const hair = mat(0xa57a52, .75), hairDark = mat(0x8a6240, .8);
            for (const side of [-1, 1]) {
                const c = mesh(new THREE.SphereGeometry(.116 * s, 22, 14, side > 0 ? 0 : Math.PI, Math.PI, 0, Math.PI * .4), hair, h, side * .003 * s, .118 * s, -.004 * s);
                c.scale.set(.98, 1.12, 1.1); c.rotation.y = -Math.PI / 2;
            }
            mesh(new THREE.CylinderGeometry(.118 * s, .14 * s, .26 * s, 22, 1, true, Math.PI * .32, Math.PI * 1.36), hairDark, h, 0, 0, -.012 * s).material.side = THREE.DoubleSide;
            for (const side of [-1, 1]) { const st = mesh(new THREE.BoxGeometry(.03 * s, .25 * s, .06 * s), hair, h, side * .112 * s, .02 * s, .03 * s); st.rotation.z = side * .08; }
            for (const x of [-1, 1]) { mesh(new THREE.SphereGeometry(.007 * s, 10, 8), mat(0xf4efe2, .2, .2), h, x * .101 * s, .092 * s, .006 * s); }
        }
    });
}

/** Genosse Chris: rote Strickjacke, Vollbart, Thermoskanne, Schlüsselbund. */
export function chris() {
    return character({
        scale: 1.0, skin: 0xe8b493, top: 0xa3392e, shirt: 0xd8d2c4, legs: 0x5a5146, shoes: 0x3a2c22, eyes: 0x4a3a2a, brows: 0x7d756c, lips: 0xa86a5a,
        headW: 1.04, headH: 1.04, torsoDepth: .78, bauch: 1.12,
        torsoProfile: [[.18, -.05], [.215, .06], [.23, .2], [.225, .36], [.215, .44], [.16, .5], [.06, .54], [0, .545]],
        dressFront(sp, s, shirt) {
            mesh(new THREE.BoxGeometry(.06 * s, .4 * s, .008 * s), shirt, sp, 0, .3 * s, .17 * s);
            for (const y of [.42, .32, .22, .12]) { mesh(new THREE.SphereGeometry(.01 * s, 8, 6), mat(0x3a2a20, .5), sp, .04 * s, y * s, .172 * s); }
            mesh(new THREE.CircleGeometry(.018 * s, 16), mat(0xe2001a, .4), sp, .12 * s, .43 * s, .165 * s);   // Anstecker
        },
        headExtras(h, s) {
            const grau = mat(0x8e877e, .9);
            mesh(new THREE.SphereGeometry(.113 * s, 24, 14, 0, Math.PI * 2, 0, Math.PI * .42), grau, h, 0, .118 * s, -.01 * s).scale.set(1.04, 1.05, 1.08);
            const bart = mesh(new THREE.SphereGeometry(.1 * s, 20, 12, Math.PI * .1, Math.PI * .8, Math.PI * .5, Math.PI * .4), grau, h, 0, .1 * s, 0); bart.rotation.y = -Math.PI / 2 + Math.PI * .5; bart.scale.set(1.05, 1.15, 1.12);
        },
        handExtras(arms, s) {
            mesh(new THREE.CylinderGeometry(.035 * s, .035 * s, .26 * s, 16), mat(0x9aa3a8, .3, .8), arms[1].el, 0, -.36 * s, .02 * s);
            mesh(new THREE.TorusGeometry(.025 * s, .004 * s, 6, 14), mat(0xc9b46a, .3, .8), arms[0].el, 0, -.36 * s, 0);
        }
    });
}

/** Pinsel und Ledeiw: zu enge Anzüge. */
export function pinsel() {
    return character({
        scale: 1.08, skin: 0xf0c2a2, top: 0x2b3f86, shirt: 0xf2f2f4, legs: 0x2b3f86, shoes: 0x16161a, eyes: 0x3c5a7a, brows: 0xb09060, lips: 0xb07060,
        headW: .96, headH: 1.12, torsoDepth: .64, cuff: true,
        torsoProfile: [[.155, -.05], [.17, .05], [.165, .2], [.185, .36], [.195, .44], [.15, .5], [.06, .54], [0, .545]],
        dressFront(sp, s, shirt, cloth) { blazer(cloth, shirt, s, sp, 0x1d8fd6); },
        headExtras(h, s) {
            const hair = mat(0xd8b27a, .7);
            const c = mesh(new THREE.SphereGeometry(.112 * s, 24, 14, 0, Math.PI * 2, 0, Math.PI * .34), hair, h, 0, .125 * s, -.005 * s); c.scale.set(1.02, 1.0, 1.1);
            mesh(new THREE.BoxGeometry(.16 * s, .03 * s, .05 * s), hair, h, .02 * s, .2 * s, .08 * s).rotation.z = -.2;   // Scheitel-Tolle
        }
    });
}

export function ledeiw() {
    return character({
        scale: .98, skin: 0xe6b08e, top: 0x3a3e46, shirt: 0xe6eef6, legs: 0x3a3e46, shoes: 0x16161a, eyes: 0x4a3a2a, brows: 0x4a3a2a, lips: 0xa86a5a,
        headW: 1.1, headH: 1.0, torsoDepth: .82, bauch: 1.2, cuff: true,
        torsoProfile: [[.19, -.05], [.23, .06], [.245, .2], [.235, .36], [.22, .44], [.16, .5], [.06, .54], [0, .545]],
        dressFront(sp, s, shirt, cloth) { blazer(cloth, shirt, s, sp, 0x2b3f86); },
        headExtras(h, s) {
            const hair = mat(0x2e2a26, .7);
            mesh(new THREE.SphereGeometry(.114 * s, 24, 14, 0, Math.PI * 2, 0, Math.PI * .26), hair, h, 0, .12 * s, -.01 * s).scale.set(1.1, 1.0, 1.1);
        }
    });
}

/**
 * Pose je Bild: art = stehen | gehen | reden | benutzen | erschrocken | kleben; t = Zeit in Sekunden.
 */
export function pose(c, t, art) {
    const u = c.userData, s = u.s, ph0 = u.phase;
    const w = art === 'gehen' || art === 'rennen' ? 1 : 0, ph = t * (art === 'rennen' ? 13 : 8) + ph0;
    const sw = Math.sin(ph) * .55 * w;
    u.legs[0].hp.rotation.x = sw; u.legs[1].hp.rotation.x = -sw;
    u.legs[0].kn.rotation.x = Math.max(0, -Math.sin(ph - .6)) * .9 * w; u.legs[1].kn.rotation.x = Math.max(0, Math.sin(ph - .6)) * .9 * w;
    u.legs[0].ft.rotation.x = -u.legs[0].kn.rotation.x * .4; u.legs[1].ft.rotation.x = -u.legs[1].kn.rotation.x * .4;
    u.hips.position.y = .98 * s + (w ? Math.abs(Math.cos(ph)) * .025 : Math.sin(t * 1.6 + ph0) * .004);
    u.spine.rotation.y = sw * .12; u.spine.rotation.x = .03 * w;
    const breathe = Math.sin(t * 1.6 + ph0) * .01;
    const [l, r] = u.arms;
    l.sh.rotation.set(-sw * .8, 0, -.08 - breathe); r.sh.rotation.set(sw * .8, 0, .08 + breathe);
    l.el.rotation.set(-.15 - .25 * w, 0, 0); r.el.rotation.set(-.15 - .25 * w, 0, 0);
    u.neck.rotation.set(0, Math.sin(t * .5 + ph0) * .06, 0);
    u.mouth.scale.y = 1;
    if (art === 'reden') {
        l.sh.rotation.x = -.5; l.el.rotation.x = -.9 + Math.sin(t * 3) * .25; r.sh.rotation.x = -.3; r.el.rotation.x = -1.0 + Math.sin(t * 2.3 + 1) * .3;
        u.neck.rotation.y = Math.sin(t * 1.3 + ph0) * .2; u.neck.rotation.x = Math.sin(t * 4) * .04;
        u.mouth.scale.y = 1 + Math.abs(Math.sin(t * 11 + ph0)) * 2.2;
    } else if (art === 'benutzen') {
        r.sh.rotation.x = -1.2; r.el.rotation.x = -.4; u.spine.rotation.x = .12;
    } else if (art === 'erschrocken') {
        l.sh.rotation.z = -1.2; r.sh.rotation.z = 1.2; l.el.rotation.z = -.8; r.el.rotation.z = .8; u.mouth.scale.y = 2.6;
    } else if (art === 'kleben') {
        // Festgeklebt: Oberkörper zerrt vor und zurück, die Füße bleiben stehen
        const k = Math.sin(t * 5 + ph0);
        u.spine.rotation.x = .2 * k; l.sh.rotation.x = -.6 - .5 * k; r.sh.rotation.x = -.6 + .5 * k; u.mouth.scale.y = 1.8;
    } else if (art === 'sitzen' || art === 'rudern' || art === 'zielen' || art === 'radeln') {
        // Sitzen im Boot: Oberschenkel nach vorn, Unterschenkel hängen herab
        for (const b of u.legs) { b.hp.rotation.x = -1.45; b.kn.rotation.x = 1.4; b.ft.rotation.x = 0; }
        if (art === 'rudern') { const k = Math.sin(t * 4); l.sh.rotation.x = r.sh.rotation.x = -1.0 - .5 * k; l.el.rotation.x = r.el.rotation.x = -.6 + .4 * k; u.spine.rotation.x = .15 * k; }
        if (art === 'radeln') { const k = t * 9; u.legs[0].hp.rotation.x = -1.2 + Math.sin(k) * .45; u.legs[1].hp.rotation.x = -1.2 - Math.sin(k) * .45; u.legs[0].kn.rotation.x = 1.3 - Math.sin(k) * .4; u.legs[1].kn.rotation.x = 1.3 + Math.sin(k) * .4; l.sh.rotation.x = r.sh.rotation.x = -1.1; u.spine.rotation.x = .25; }
        if (art === 'zielen') { r.sh.rotation.x = -1.5; r.el.rotation.x = -.1; u.mouth.scale.y = 1 + Math.abs(Math.sin(t * 9)) * 2; }
    } else if (art === 'arme') {
        // Arme verschränkt (Chris vor der Tür)
        l.sh.rotation.set(-.7, 0, -.15); r.sh.rotation.set(-.7, 0, .15); l.el.rotation.set(-1.6, .9, 0); r.el.rotation.set(-1.6, -.9, 0);
    }
}

/** Genossin Johanna (Linke): lila Pulli, Jutebeutel, Klemmbrett, Dutt. */
export function johanna() {
    return character({
        scale: .97, skin: 0xeec3a4, top: 0x7d3c98, shirt: 0xe8e0d0, legs: 0x3a4a6a, shoes: 0x2a2a2a, eyes: 0x4a6a3a, brows: 0x5a3a2a, lips: 0xb87070,
        headW: .96, headH: 1.08, torsoDepth: .7, sleeveMat: 0x7d3c98,
        torsoProfile: [[.168, -.05], [.19, .06], [.172, .2], [.18, .32], [.19, .42], [.15, .49], [.055, .535], [0, .54]],
        dressFront(sp, s) {
            const gurt = mesh(new THREE.BoxGeometry(.02 * s, .6 * s, .01 * s), mat(0xd8cbb0, .9), sp, 0, .26 * s, .15 * s); gurt.rotation.z = -.6;
            const beutel = mesh(new THREE.BoxGeometry(.26 * s, .3 * s, .04 * s), mat(0xd8cbb0, .95), sp, -.2 * s, -.08 * s, .08 * s); beutel.rotation.y = .3;
            mesh(new THREE.CircleGeometry(.05 * s, 5), mat(0xc0392b, .6), sp, -.19 * s, -.06 * s, .105 * s).rotation.y = .3;
        },
        headExtras(h, s) {
            const hair = mat(0x2e1f17, .8);
            mesh(new THREE.SphereGeometry(.113 * s, 22, 14, 0, Math.PI * 2, 0, Math.PI * .45), hair, h, 0, .12 * s, -.008 * s).scale.set(1.02, 1.05, 1.1);
            mesh(new THREE.SphereGeometry(.055 * s, 14, 10), hair, h, 0, .2 * s, -.09 * s);
            for (const x of [-.037, .037]) { mesh(new THREE.TorusGeometry(.019 * s, .003 * s, 6, 16), mat(0x222222, .4, .6), h, x * s, .125 * s, .116 * s); }
        },
        handExtras(arms, s) {
            const brett = mesh(new THREE.BoxGeometry(.2 * s, .27 * s, .012 * s), mat(0x8a5a34, .6), arms[1].el, .02 * s, -.33 * s, .06 * s); brett.rotation.x = -.4;
            mesh(new THREE.PlaneGeometry(.16 * s, .2 * s), mat(0xffffff, .9), brett, 0, -.01 * s, .007 * s);
        }
    });
}

/** Bauleiterin Frau Kemper: orangefarbener Helm, Warnweste, Klemmbrett. */
export function kemper() {
    const c = character({
        scale: 1.0, skin: 0xe2ad8a, top: 0xf39c12, shirt: 0x2e4a6a, legs: 0x2e4a6a, shoes: 0x3a2a1a, eyes: 0x3a4a5a, brows: 0x6a4a2a, lips: 0xb07060,
        headW: 1.0, headH: 1.04, torsoDepth: .74, sleeveMat: 0x2e4a6a,
        torsoProfile: [[.17, -.05], [.2, .06], [.2, .2], [.205, .34], [.21, .44], [.16, .5], [.06, .54], [0, .545]],
        dressFront(sp, s) {
            const silber = mat(0xd8dde2, .3, .6);
            for (const y of [.18, .32]) { mesh(new THREE.CylinderGeometry(.212 * s, .212 * s, .035 * s, 28, 1, true), silber, sp, 0, y * s, 0).scale.z = .76; }
        },
        headExtras(h, s) {
            mesh(new THREE.SphereGeometry(.112 * s, 20, 12, 0, Math.PI * 2, 0, Math.PI * .42), mat(0x6a4a2a, .8), h, 0, .115 * s, -.01 * s);
            helmAuf(h, s, 0xf28c28);
        },
        handExtras(arms, s) {
            const brett = mesh(new THREE.BoxGeometry(.2 * s, .27 * s, .012 * s), mat(0x5a5a5a, .6), arms[0].el, -.02 * s, -.33 * s, .06 * s); brett.rotation.x = -.4;
        }
    });
    return c;
}

function helmAuf(h, s, farbe) {
    const g = new THREE.Group(); g.position.set(0, .15 * s, -.004 * s); h.add(g);
    const m = mat(farbe, .35);
    mesh(new THREE.SphereGeometry(.122 * s, 24, 12, 0, Math.PI * 2, 0, Math.PI / 2), m, g).scale.set(1.0, .95, 1.12);
    mesh(new THREE.CylinderGeometry(.14 * s, .14 * s, .01 * s, 24), m, g).scale.z = 1.15;
    mesh(new THREE.BoxGeometry(.02 * s, .03 * s, .25 * s), m, g, 0, .11 * s, 0);
    return g;
}

/** Bauhelm für Joe oder Chani (Baustelle): einmal anlegen, dann ein- und ausblenden. */
export function helm(c, an) {
    const u = c.userData;
    if (!u.helm && an) { u.helm = helmAuf(u.head, u.s, 0xf4f4f0); }
    if (u.helm) { u.helm.visible = an; }
}

/** Lorette (Grüne): Fahrradhelm, grüne Regenjacke, dickes Gutachten unter dem Arm. */
export function lorette() {
    return character({
        scale: .98, skin: 0xf0c8a8, top: 0x27ae60, topRough: .5, shirt: 0xf2f2e8, legs: 0x4a4038, shoes: 0x5a3a20, eyes: 0x3a6a3a, brows: 0xa07040, lips: 0xc07a70,
        headW: .95, headH: 1.1, torsoDepth: .7, sleeveMat: 0x27ae60,
        torsoProfile: [[.17, -.05], [.195, .06], [.18, .2], [.185, .34], [.195, .44], [.15, .5], [.055, .54], [0, .545]],
        dressFront(sp, s) { mesh(new THREE.BoxGeometry(.008 * s, .4 * s, .006 * s), mat(0xe8e8e0, .3, .6), sp, 0, .28 * s, .16 * s); },
        headExtras(h, s) {
            const hair = mat(0xc08a4a, .8);
            mesh(new THREE.SphereGeometry(.113 * s, 22, 14, 0, Math.PI * 2, 0, Math.PI * .5), hair, h, 0, .115 * s, -.01 * s).scale.set(1.02, 1.05, 1.1);
            const zopf = mesh(new THREE.CylinderGeometry(.03 * s, .015 * s, .22 * s, 8), hair, h, 0, .02 * s, -.12 * s); zopf.rotation.x = .3;
            const helm = new THREE.Group(); helm.position.set(0, .16 * s, 0); h.add(helm);
            mesh(new THREE.SphereGeometry(.125 * s, 20, 10, 0, Math.PI * 2, 0, Math.PI * .45), mat(0x2ecc71, .3), helm).scale.set(1, .9, 1.2);
            for (let i = -2; i <= 2; i++) { mesh(new THREE.BoxGeometry(.015 * s, .02 * s, .2 * s), mat(0x1a1a1a, .5), helm, i * .035 * s, .11 * s, 0); }
        },
        handExtras(arms, s) {
            const gutachten = mesh(new THREE.BoxGeometry(.24 * s, .32 * s, .09 * s), mat(0xf4f0e0, .9), arms[0].el, .05 * s, -.2 * s, .06 * s);
            mesh(new THREE.BoxGeometry(.245 * s, .05 * s, .095 * s), mat(0x27ae60, .6), gutachten, 0, .1 * s, 0);
        }
    });
}

/** Lorettes Lastenrad (faltbar): Holzkiste vorn, zwei Vorderräder, Rahmen, Sattel. */
export function lastenrad() {
    const g = new THREE.Group();
    const rahmen = mat(0x2ecc71, .4, .3), schwarz = mat(0x1a1a1a, .6), holz = mat(0xb08050, .7), metall = mat(0xb0b4b8, .3, .8);
    const rad = (x, z) => { const r = mesh(new THREE.TorusGeometry(.3, .04, 8, 24), schwarz, g, x, .3, z); r.rotation.y = Math.PI / 2; mesh(new THREE.CylinderGeometry(.02, .02, .58, 6), metall, g, x, .3, z).rotation.z = Math.PI / 2; return r; };
    const raeder = [rad(.9, -.3), rad(.9, .3), rad(-.8, 0)];
    mesh(new THREE.BoxGeometry(.8, .5, .75), holz, g, .9, .8, 0);
    mesh(new THREE.BoxGeometry(1.6, .05, .05), rahmen, g, .05, .55, 0);
    const s1 = mesh(new THREE.BoxGeometry(.05, .7, .05), rahmen, g, -.35, .85, 0); s1.rotation.z = .15;
    mesh(new THREE.BoxGeometry(.25, .06, .14), schwarz, g, -.4, 1.22, 0);
    const lenk = mesh(new THREE.BoxGeometry(.05, .75, .05), rahmen, g, .35, .95, 0); lenk.rotation.z = -.25;
    mesh(new THREE.BoxGeometry(.05, .05, .6), metall, g, .27, 1.32, 0);
    const akku = mesh(new THREE.BoxGeometry(.3, .12, .1), mat(0x333333, .4), g, -.1, .65, 0);
    g.userData.raeder = raeder;
    return g;
}
