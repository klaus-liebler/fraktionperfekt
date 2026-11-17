// Erzeugt docs/spiel-loesungsweg.md: den kompletten Lösungsweg mit allen Dialogen und Regieanweisungen
// (wann Figuren und Dinge erscheinen oder verschwinden), direkt aus dem Spiel (logik.js, grafik.js).
//   node tools/Grafik/loesungsweg.mjs
// Die Datei darf von Hand bearbeitet werden (Änderungswünsche); danach werden die Änderungen ins Spiel übernommen
// und die Datei neu erzeugt.
import { createRequire } from 'node:module';
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const require = createRequire(import.meta.url);
const S = require(join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel', 'logik.js'));
const G = require(join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel', 'grafik.js'));

const kopie = (z) => JSON.parse(JSON.stringify(z));
const FIGUR = { joe: 'Joe', chani: 'Chani' };
const VERB = { ansehen: 'Schau an', benutzen: 'Benutze', reden: 'Rede mit', f1: null, f2: null, gegenstand: 'Gib/Benutze' };
const PERSON = { christian: 'Chris', joanna: 'Johanna', lore: 'Lorette', kemper: 'Frau Kemper', hohlbein: 'Pinsel', droehn: 'Ledeiw', joe: 'Joe', chani: 'Chani' };
const BILD = {
    buero_zukunftsschmiede_v2: 'Büro mit Umschlag', buero_zukunftsschmiede_v2_ohne_umschlag: 'Büro ohne Umschlag', rathaus_flur_v2: 'Rathausflur vor der Archivtür',
    archiv_v2: 'Archiv (Tür offen)', ems_ufer: 'Emsdeich am Kanuverleih', baustelle_bad: 'Baustelle, Grube unter Wasser', baustelle_bad_grube_trocken: 'Baustelle, Grube trocken',
    schulhof_nacht: 'Schulhof', schulflur: 'Schulflur', krypta: 'Krypta', tempel_eingang: 'Tempel mit Fallgitter und Plattformen', tempel_abgrund: 'Tempel: Tafel und Abgrund (ohne Brücke)',
    tempel_abgrund_bruecke: 'Tempel: Abgrund mit Hängebrücke', tempel_finale: 'Tempel: Pokal und Holzkiste', karte_greven: 'Reisekarte'
};
// Bildänderungen, die nicht am Hintergrund hängen (Überlagerungen und Animationen)
const REGIE = {
    uhr: ['abend', 'Die Uhr springt auf 20:35, Fledermäuse flattern über den Hof (ab jetzt immer)'],
    tor: ['tempelOffen', 'Die drei Siegel leuchten im Tor'],
    leitung: ['grubeTrocken', 'Das Leck hört auf zu sprudeln'],
    boot: ['gegnerBefreit', 'Im Archiv kleben ab jetzt nur noch zwei Paar Schuhsohlen an der Rückwand (Pinsel und Ledeiw sind weg)']
};
const zitat = (zeilen) => zeilen.filter(Boolean).map((t) => `> ${t}`).join('\n>\n');

function fz(z) {
    const name = (n) => FIGUR[n] || n;
    return z.szene === 'karte' ? null : {
        bild: G.hintergrund(z).replace('_256x144', ''),
        hotspots: S.sichtbareHotspots(z).map((h) => h.name),
        personen: G.npcPlaetze(z, S.sichtbareHotspots(z)).map((p) => PERSON[p.name] + (p.hinten ? ' (verkleinert hinten, festgeklebt)' : '')),
        plaetze: G.npcPlaetze(z, S.sichtbareHotspots(z)).map((p) => `${PERSON[p.name]}@${Math.round(p.x)}`),
        plattformen: G.plattformen(z).filter((p) => p.figur).map((p) => `${name(p.figur)} auf der ${p.seite === 'links' ? 'linken' : 'rechten'} Plattform`),
        ueber: G.ueberlagerungen(z, 0).map((o) => o.bild)
    };
}

function differenz(vorher, nachher, a, b) {
    const neu = nachher.filter((x) => !vorher.includes(x)), weg = vorher.filter((x) => !nachher.includes(x));
    return [neu.length ? `${a}: ${neu.join(', ')}` : null, weg.length ? `${b}: ${weg.join(', ')}` : null].filter(Boolean);
}

function schrittName(z, s) {
    const h = (id) => (S.SZENEN[z.szene] ? (S.SZENEN[z.szene].hotspots.find((x) => x.id === id) || {}).name : id) || id;
    const wer = FIGUR[z.figur];
    switch (s[0]) {
        case 'reisen': return `Reise über die Karte nach ${S.ORTE[s[1]].name}`;
        case 'figur': return `Figur wechseln (jetzt ${z.figur === 'joe' ? 'Chani' : 'Joe'})`;
        case 'dialog': {
            const d = S.dialogInhalt(z, s[1]); const o = d && d.optionen.find((x) => x.id === s[2]);
            return `Auswahl: „${o ? o.text : s[2]}“`;
        }
        case 'minispiel': return s[1] === 'boot' ? 'Minispiel Bootsflucht gewinnen' : 'Minispiel Wahlkugel gewinnen';
        default: {
            const [, art, id, g] = s;
            if (art === 'gegenstand') { return `${wer}: ${S.GEGENSTAENDE[g].name} → ${h(id)}`; }
            const v = art === 'f1' || art === 'f2' ? S.FIGUREN[z.figur].faehigkeiten[art === 'f1' ? 0 : 1] : VERB[art];
            return `${wer}: ${v} → ${h(id)}`;
        }
    }
}

const out = [];
const p = (t = '') => out.push(t);
p('# Lösungsweg: „Joe & Chani und der Gral der Demokratie“');
p();
p('Kompletter Lösungsweg mit allen Texten und Regieanweisungen. Die Datei wird mit `node tools/Grafik/loesungsweg.mjs` aus dem Spiel erzeugt;');
p('Änderungswünsche einfach direkt hineinschreiben (Texte ändern, Regie ergänzen, Kommentare als „**Klaus:** …“) – sie werden dann ins Spiel übernommen');
p('und die Datei neu erzeugt.');
p();
p('**Lesart:** `> …` ist ein Text, der im Textfeld erscheint (eine Zeile pro Klick). „Regie“ beschreibt, was sich im Bild ändert. Grundregel:');
p('Figuren verschwinden, tauchen auf oder wechseln den Platz erst, **wenn der zugehörige Text gelesen ist**; Hintergrund und Gegenstände ändern sich sofort.');
p('Ausnahme: Öffnet sich das Fallgitter im Tempel, bleibt der Raum mit den Plattformen stehen, bis der Text gelesen ist; dann Blende in den Raum mit dem Abgrund.');
p();
p('## Vorspann (neues Spiel)');
p();
p('_Regie: Titelbild mit Erkennungsmelodie. „Neues Spiel“ → Zukunftsschmiede; Joe (aktiv) und Chani stehen links vom Schreibtisch._');
p();
p(zitat([...S.VORSTELLUNG, ...S.EINLEITUNG]));
p();

// ---------- Hauptweg ----------
const z = S.neuerZustand();
let szene = null, nr = 0;
const besuch = {};    // Szene → Spielstände (beim Betreten und vor dem Verlassen) für die Nebendialoge
const hauptTexte = new Set();
p(`## Szene: ${S.SZENEN.buero.titel}`);
p();
p(zitat([S.einstieg(z, 'buero')]));
p();
besuch.buero = [kopie(z)];
szene = 'buero';
for (const s of S.KOMPLETTLOESUNG) {
    nr++;
    const vorher = kopie(z), fzV = fz(z), invV = z.inventar.map((g) => S.GEGENSTAENDE[g].name), orteV = [...z.orte];
    const titel = schrittName(z, s);
    const e = S.schritt(z, s);
    (e.text || []).forEach((t) => hauptTexte.add(t));
    const regie = [];
    if (e.minispiel) { regie.push(e.minispiel === 'boot' ? 'Nach dem Text startet das Minispiel Bootsflucht (siehe unten).' : 'Nach dem Text startet das Minispiel Wahlkugel (siehe unten).'); }
    if (e.dialog) { regie.push('Nach dem Text erscheint eine Auswahl.'); }
    const invN = z.inventar.map((g) => S.GEGENSTAENDE[g].name);
    regie.push(...differenz(invV, invN, 'Inventar neu', 'Inventar weg'));
    const neuOrte = z.orte.filter((o) => !orteV.includes(o)).map((o) => S.ORTE[o].name);
    if (neuOrte.length) { regie.push(`Neuer Ort auf der Karte: ${neuOrte.join(', ')}`); }
    if (z.szene === vorher.szene && fzV) {
        const fzN = fz(z);
        const gitter = fzV.bild === 'tempel_eingang' && fzN.bild !== fzV.bild;
        if (gitter) { regie.push('Das Fallgitter hebt sich. Raum, Plattformen und beide Figuren darauf bleiben stehen, bis der Text gelesen ist; dann Blende: Joe und Chani stehen im Raum mit Tafel und Abgrund (links vom Abgrund)'); }
        else if (fzN.bild !== fzV.bild) { regie.push(`Hintergrund wechselt sofort: ${BILD[fzV.bild] || fzV.bild} → ${BILD[fzN.bild] || fzN.bild}`); }
        const basis = (l) => l.map((n) => n.replace(/ \(.*\)$/, '')).sort().join();
        const pers = basis(fzV.personen) === basis(fzN.personen) && fzV.personen.join() !== fzN.personen.join()
            ? [`Wechseln den Platz (nach dem Text): ${fzN.personen.filter((n) => !fzV.personen.includes(n)).join(', ')}`]
            : differenz(fzV.personen, fzN.personen, 'Erscheint (nach dem Text)', 'Verschwindet (nach dem Text)');
        if (pers.length) { regie.push(...pers); }
        else if (fzV.plaetze.join() !== fzN.plaetze.join()) { regie.push(`Wechselt den Platz (nach dem Text): ${fzN.personen.filter((n, i) => fzV.plaetze[i] !== fzN.plaetze[i]).join(', ')}`); }
        if (!gitter) { regie.push(...differenz(fzV.plattformen, fzN.plattformen, 'Sofort (verkleinert im Hintergrund, die andere Figur bleibt, wo sie ist)', 'Steigt herunter')); }
        if (REGIE[s[2]] && !vorher.flags[REGIE[s[2]][0]] && z.flags[REGIE[s[2]][0]]) { regie.push(REGIE[s[2]][1]); }
        regie.push(...differenz(fzV.hotspots, fzN.hotspots, 'Neu anklickbar', 'Nicht mehr anklickbar'));
    }
    if (z.szene !== vorher.szene && s[0] !== 'reisen') {
        if (z.szene === 'karte') { regie.push('Blende → Reisekarte.'); }
        else { regie.push(`Blende → ${S.SZENEN[z.szene].titel}.`); }
    }
    if (s[0] === 'reisen') { regie.unshift('Rote Reiselinie wächst vom letzten Ort zum Ziel, dann Blende.'); }
    if (s[0] === 'reisen' || (z.szene !== vorher.szene && z.szene !== 'karte')) {
        if (szene && besuch[szene]) { besuch[szene].push(vorher); }
        if (z.szene !== 'karte') {
            szene = z.szene; (besuch[szene] = besuch[szene] || []).push(kopie(z));
            p(`## Szene: ${S.SZENEN[z.szene].titel}`);
            p();
            const f = fz(z);
            p(`_Bild: ${BILD[f.bild] || f.bild}. Personen: ${f.personen.length ? f.personen.join(', ') : 'keine'}. Anklickbar: ${f.hotspots.join(', ')}._`);
            p();
        }
    }
    p(`**${nr}. ${titel}**`);
    p();
    if (e.text && e.text.length) {
        const texte = [...e.text];
        if (z.szene !== vorher.szene && z.szene !== 'karte' && !e.ohneEinstieg && !texte.includes(S.einstieg(z, z.szene))) { texte.push(S.einstieg(z, z.szene)); }
        p(zitat(texte)); p();
    }
    if (s[0] === 'minispiel' && s[1] === 'boot') {
        p('_Minispiel Bootsflucht: Das Ruderboot fährt flussab (Bild läuft nach links). Von vorn treibt Treibgut heran (Treibholz, Entenfamilie, Schild „Alternative Route“,');
        p(`Plakat „Grenzen dicht!“). Hinten (links) folgen Pinsel und Ledeiw im Motorboot und schießen ${S.ALTERNATIVE_FAKTEN.length} alternative Fakten als Sprechblasen in eure Spur:`);
        p(`${S.ALTERNATIVE_FAKTEN.map((t) => `„${t}“`).join(', ')}. Anzeige oben: „Alternative Fakten: N“ und drei Herzen. Sind die Fakten verschossen,`);
        p('kippt das Motorboot und sinkt (Blasen, „Blubb!“). Drei Treffer = gekentert, noch einmal._');
        p();
    }
    if (s[0] === 'minispiel' && s[1] === 'kugel') {
        p('_Minispiel Wahlkugel: Joe und Chani rennen (mit Lorettes Lastenrad: fahren) durch den Gang, hinter ihnen rollt die steinerne Wahlkugel. Tippen gibt Tempo._');
        p();
    }
    if (regie.length) { p(`_Regie: ${regie.join(' · ')}_`); p(); }
}
if (szene && besuch[szene]) { besuch[szene].push(kopie(z)); }

// ---------- Nebendialoge ----------
p('---');
p();
p('## Alle weiteren Texte (Nebendialoge, falsche Versuche)');
p();
p('Pro Szene alle Antworten auf alle Verben, Fähigkeiten und passenden Gegenstände – für Joe und für Chani, beim Betreten der Szene und kurz vor dem Verlassen.');
p('Texte, die schon im Lösungsweg stehen, und die Standardantworten (ganz unten) sind weggelassen. ⏎ trennt Textzeilen (ein Klick).');
p('Gegenstände, die nirgends passen, ergeben: „<Gegenstand> und <Ziel> – das passt nicht zusammen.“');
p();
const ARTEN = ['ansehen', 'benutzen', 'reden', 'f1', 'f2'];
for (const id of Object.keys(S.SZENEN)) {
    const sz = S.SZENEN[id];
    p(`### ${sz.titel}`);
    p();
    const staende = besuch[id] || [];
    const proHotspot = new Map();
    for (const st of staende) {
        for (const h of sz.hotspots) {
            const eintraege = proHotspot.get(h.name) || new Map();
            for (const figur of ['joe', 'chani']) {
                const versuche = ARTEN.map((a) => [a, null]).concat(Object.keys(h.gegenstand || {}).map((g) => ['gegenstand', g]));
                for (const [art, g] of versuche) {
                    const zz = kopie(st); zz.figur = figur; if (g && !zz.inventar.includes(g)) { zz.inventar.push(g); }
                    if (!S.sichtbareHotspots(zz).some((x) => x.id === h.id)) { continue; }
                    const e = S.aktion(zz, art, h.id, g);
                    const text = (e.text || []).join(' / ');
                    const standard = ARTEN.map((a) => S.STANDARD[a](zz, h).text[0]);
                    if (!text || (e.text || []).every((t) => hauptTexte.has(t)) || (e.text.length === 1 && standard.includes(e.text[0]))) { continue; }
                    const v = art === 'gegenstand' ? `${S.GEGENSTAENDE[g].name} geben/benutzen` : art === 'f1' || art === 'f2' ? S.FIGUREN[figur].faehigkeiten[art === 'f1' ? 0 : 1] : VERB[art];
                    const schluessel = text;
                    if (!eintraege.has(schluessel)) { eintraege.set(schluessel, new Set()); }
                    eintraege.get(schluessel).add(art === 'f1' || art === 'f2' || art === 'gegenstand' ? `${FIGUR[figur]}: ${v}` : v);
                }
            }
            proHotspot.set(h.name, eintraege);
        }
    }
    for (const [name, eintraege] of proHotspot) {
        if (!eintraege.size) { continue; }
        p(`**${name}**`);
        p();
        for (const [text, wer] of eintraege) { p(`- _${[...new Set(wer)].join(', ')}:_ ${text.split(' / ').join(' ⏎ ')}`); }
        p();
    }
}

p('### Rätsel-Auswahl');
p();
p('**Symbolschloss (Krypta):** drei Symbole nacheinander aus Waage, Buch, Urne. Richtig: Urne, Buch, Waage. Falsch:');
p();
{ const zz = S.neuerZustand(); zz.szene = 'krypta'; ['waage', 'waage'].forEach((x) => S.dialogWahl(zz, 'schloss', x)); p(zitat(S.dialogWahl(zz, 'schloss', 'waage').text)); }
p();
p('**Tafel der Geschäftsordnung (Tempel):** Bei einem Fehler beginnt die Tafel von vorn (mit Geschäftsordnung im Inventar gibt Chani einen Tipp).');
p();
S.QUIZ.forEach((q) => {
    p(`- ${q.frage}`);
    q.optionen.forEach((o, i) => p(`  - ${i === q.richtig ? '**richtig:** ' : ''}${o}`));
    p(`  - Tipp bei Fehler: „Die Tafel wird dunkel. Alles von vorn.“ – „Chani blättert in der Geschäftsordnung: ${q.tipp}“`);
});
p();
p(`**Abgrund:** ${S.BRUECKE.frage}`);
p();
S.BRUECKE.optionen.forEach((o, i) => p(`- ${i === S.BRUECKE.richtig ? '**richtig:** ' : ''}${o}`));
p();
{ const zz = S.neuerZustand(); p(zitat(S.dialogWahl(zz, 'bruecke', '0').text)); }
p();
p('### Minispiele verloren');
p();
{ const zz = S.neuerZustand(); p(zitat([S.minispielEnde(zz, 'boot', false).text[0], S.minispielEnde(zz, 'kugel', false).text[0]])); }
p();
p('### Joes Handy: Professor Relbeil');
p();
p('„Guten Tag!“ – Plauderei (der Reihe nach):');
p();
p(zitat(S.PLAUDEREI));
p();
p('„Ich brauche deine Hilfe!“ – Relbeil nennt den nächsten Schritt aus dem Lösungsweg, z. B. am Anfang:');
p();
p(zitat(S.anrufen(S.neuerZustand(), 'hilfe').text));
p();
p('Chani mit dem Handy: „Chani: „Das ist Joes Handy. Joe, ruf du an – mit Tasten kann ich nicht.““');
p();
p('### Standardantworten (wenn nichts Besonderes passiert)');
p();
for (const figur of ['joe', 'chani']) {
    const zz = S.neuerZustand(); zz.figur = figur;
    const h = { name: '<Ziel>' };
    p(`- ${FIGUR[figur]}: ${['ansehen', 'benutzen', 'reden', 'f1', 'f2'].map((a) => `„${S.STANDARD[a](zz, h).text[0]}“`).join(' · ')}`);
}
p();
writeFileSync(join(wurzel, 'docs', 'spiel-loesungsweg.md'), out.join('\n'));
console.log('docs/spiel-loesungsweg.md geschrieben:', out.length, 'Zeilen');
