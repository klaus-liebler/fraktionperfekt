// Fototexturen (Poly Haven, CC0) für die 3D-Welten: Farb- und Normal-Map je Material, Kachelmaße aus texturen3d_daten.js.
// Bilder werden per <img> geladen (CSP: img-src 'self'); bis sie da sind, zeigt das Material seine Grundfarbe.
import * as THREE from './three_min.js';
import { TEXTUREN } from './texturen3d_daten.js';

const lader = new THREE.TextureLoader();
const cache = {};

/** Texturpaar { map, normalMap, meter } für name (backstein, pflaster, kopfstein, putz); Kacheln wiederholen sich. */
export function textur(name) {
    if (!cache[name]) {
        const lade = (karte, srgb) => {
            const t = lader.load(new URL(`tex_${name}_${karte}.jpg`, import.meta.url).href);
            t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8;
            if (srgb) { t.colorSpace = THREE.SRGBColorSpace; }
            return t;
        };
        cache[name] = { map: lade('farbe', true), normalMap: lade('normal', false), meter: TEXTUREN[name].meter };
    }
    return cache[name];
}

/** MeshStandardMaterial mit Fototextur; uv der Geometrie in Metern geteilt durch kachel (Standard: Kachelmaß der Textur). */
// Grundtönung je Textur (Faktor auf die Farbe, > 1 hellt auf): der Rasen von Poly Haven ist sehr braun
const TOENUNG = { rasen: [.8, 1.32, .58] };

export function fotoMaterial(name, extra = {}) {
    const t = textur(name), m = new THREE.MeshStandardMaterial({ map: t.map, normalMap: t.normalMap, roughness: .9, ...extra });
    if (TOENUNG[name] && extra.color === undefined) { m.color.setRGB(...TOENUNG[name]); }
    return m;
}

/**
 * Wie fotoMaterial, aber mit eigener Wiederholung (Kopien der Texturen, das Bild wird geteilt):
 * wiederholung = [u, v]; ohne Angabe 1/Kachelmaß, passend zu Texturkoordinaten in Metern.
 */
export function meterMaterial(name, extra = {}, wiederholung) {
    const m = fotoMaterial(name, extra), t = textur(name);
    const [u, v] = wiederholung || [1 / t.meter, 1 / t.meter];
    m.map = t.map.clone(); m.map.repeat.set(u, v);
    m.normalMap = t.normalMap.clone(); m.normalMap.repeat.set(u, v);
    return m;
}
