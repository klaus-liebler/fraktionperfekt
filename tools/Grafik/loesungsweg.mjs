// Erzeugt docs/spiel-loesungsweg.md: den kompletten Lösungsweg mit allen Dialogen und der Regie (welche Textzeile
// Figuren kommen oder gehen lässt und Dinge verändert), direkt aus der Spiellogik (Spiel3d/logik.js).
//   node tools/Grafik/loesungsweg.mjs
// Die Datei darf von Hand bearbeitet werden (Änderungswünsche); danach werden die Änderungen ins Spiel übernommen
// und die Datei neu erzeugt.
import { createRequire } from 'node:module';
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const require = createRequire(import.meta.url);
const S = require(join(wurzel, 'src', 'FraktionPerfekt.Web', 'Spiel3d', 'logik.js'));

const kopie = (z) => JSON.parse(JSON.stringify(z));
const FIGUR = { joe: 'Joe', chani: 'Chani' };
const VERB = { ansehen: 'Schau an', benutzen: 'Benutze', reden: 'Rede mit', f1: null, f2: null, gegenstand: 'Gib/Benutze' };
// Was im Bild passiert, wenn eine Zeile einen Regie-Schlüssel nennt (Figuren gehen und kommen zu Fuß)
const REGIE = {
    briefGelesen: 'Brief gelesen',
    antragUebergeben: 'Chris hält den Antrag',
    archivOffen: 'Die Rathaustür ist offen; Chris geht zur Straße davon',
    gegnerArchiv: 'Pinsel und Ledeiw kommen von der Straße und drängen sich vorbei ins Rathaus (im Archiv stehen sie dann vor der Stahltür)',
    hinweisProtokoll: 'Hinweis: Protokoll 47',
    dichtstoffArchiv: 'Zwei Raupen Dichtstoff erscheinen vor ihren Schuhspitzen',
    gegnerArchivFest: 'Pinsel und Ledeiw rutschen festgeklebt an die Rückwand (gleiten, ohne zu gehen)',
    stadtplanWeg: 'Der Stadtplan hängt an der Preistafel',
    hinweisPfahl: 'Hinweis: dritte Laterne',
    gegnerBefreit: 'Im Archiv bleiben nur noch die Schuhsohlen',
    bootGewonnen: 'Das Ruderboot ist weg (flussab)',
    gegnerBaustelle: 'Pinsel und Ledeiw kommen durch das Bauzaun-Tor herein; Leiter am Schild, die Buchstaben von „EINGANG“ fehlen',
    helme: 'Joe und Chani tragen Bauhelme',
    leckDicht: 'Das Leck hört auf zu sprudeln',
    grubeTrocken: 'Die Grube läuft leer, unten glänzt das Siegel',
    gegnerBaustelleWeg: 'Pinsel und Ledeiw hauen quer über den Platz durchs Bauzaun-Tor ab; die Buchstaben liegen vor dem Tor',
    schildHeil: 'Über der Tür steht wieder „EINGANG HALLENBAD“; Leiter und Buchstaben sind weg',
    klappeAuf: 'Die Klappe der Laterne steht offen',
    abend: 'Es wird dunkel, die Fledermäuse fliegen aus',
    lastenrad: 'Lorette faltet das Lastenrad, Chani steckt es ein',
    hinweisKrypta: 'Hinweis: Urne, Buch, Waage',
    schlossOffen: 'Die Scheiben des Schlosses drehen sich in die richtige Stellung',
    tempelOffen: 'Die Siegel leuchten im Tor',
    gitterOffen: 'Das Fallgitter hebt sich; Joe und Chani springen herunter und gehen hindurch',
    goGeloest: 'Die Tafel erlischt',
    brueckeDa: 'Die Hängebrücke schiebt sich über den Abgrund',
    kugelGeschafft: 'Die Kugel steckt im Torbogen',
    gegnerFinale: 'Pinsel und Ledeiw stolpern aus dem Seitengang herein',
    pokalGenommen: 'Pinsel nimmt den Pokal vom Podest',
    urneGetragen: 'Joe und Chani gehen zur Urne und heben sie gemeinsam an',
    chrisHilft: 'Chris kommt herein',
    loretteHilft: 'Lorette kommt herein',
    johannaHilft: 'Johanna kommt herein',
    gegnerAbgang: 'Pinsel und Ledeiw gehen mit dem Pokal hinaus',
    ende: 'Abspann „ENDE“ (nach dem letzten Text)'
};
function regieText(k, z) {
    if (S.GEGENSTAENDE[k]) { return `${z.inventar.includes(k) ? 'ins Inventar' : 'aus dem Inventar'}: ${S.GEGENSTAENDE[k].name}`; }
    return REGIE[k] || `Zustand „${k}“`;
}
/** Texte als Zitat; Zeilen mit Regie bekommen darunter, was in diesem Moment im Bild passiert. */
function zitat(zeilen, e, z) {
    return zeilen.filter(Boolean).map((t, i) => {
        const r = e && e.regie && e.regie[i];
        return `> ${t}` + (r ? `\n>\n> ↳ _${r.map((k) => regieText(k, z)).join(' · ')}_` : '');
    }).join('\n>\n');
}
const personen = (z) => S.sichtbareHotspots(z).filter((h) => h.form === 'person' || h.form === 'gegner').map((h) => h.name);
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
p('# Lösungsweg: „Joe & Chani und Grevens Gral der Demokratie“');
p();
p('Kompletter Lösungsweg mit allen Texten und Regieanweisungen. Die Datei wird mit `node tools/Grafik/loesungsweg.mjs` aus dem Spiel erzeugt;');
p('Änderungswünsche einfach direkt hineinschreiben (Texte ändern, Regie ergänzen, Kommentare als „**Klaus:** …“) – sie werden dann ins Spiel übernommen');
p('und die Datei neu erzeugt.');
p();
p('**Lesart:** `> …` ist ein Text, der im Textfeld erscheint (eine Zeile pro Klick). `↳` darunter: was **mit dieser Zeile** im Bild');
p('passiert. Grundregel: Figuren und Dinge ändern sich erst, nachdem der Grund im Text steht. Figuren kommen und gehen dabei zu Fuß,');
p('Genommenes wandert zur Figur, Neues wächst ins Bild. „Regie“ unter einem Schritt fasst Inventar, Orte und Szenenwechsel zusammen.');
p();
p('## Vorspann (neues Spiel)');
p();
p('_Regie: Titelbild (Greven als Stadtmodell auf dem Kartentisch) mit Erkennungsmelodie. „Neues Spiel“ → Zukunftsschmiede; Joe (aktiv) und Chani stehen im Büro._');
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
    const vorher = kopie(z), invV = z.inventar.map((g) => S.GEGENSTAENDE[g].name), orteV = [...z.orte];
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
    if (z.szene === vorher.szene && z.szene !== 'karte') {
        const hV = S.sichtbareHotspots(vorher).map((h) => h.name), hN = S.sichtbareHotspots(z).map((h) => h.name);
        regie.push(...differenz(hV, hN, 'Neu anklickbar', 'Nicht mehr anklickbar'));
    }
    if (z.szene !== vorher.szene && s[0] !== 'reisen') {
        if (z.szene === 'karte') { regie.push('Blende → Reisekarte.'); }
        else { regie.push(`Blende → ${S.SZENEN[z.szene].titel}.`); }
    }
    if (s[0] === 'reisen') { regie.unshift('Joes Hut wandert auf der Karte vom letzten Ort zum Ziel, dann Blende.'); }
    if (s[0] === 'reisen' || (z.szene !== vorher.szene && z.szene !== 'karte')) {
        if (szene && besuch[szene]) { besuch[szene].push(vorher); }
        if (z.szene !== 'karte') {
            szene = z.szene; (besuch[szene] = besuch[szene] || []).push(kopie(z));
            p(`## Szene: ${S.SZENEN[z.szene].titel}`);
            p();
            const pers = personen(z);
            p(`_Personen: ${pers.length ? pers.join(', ') : 'keine'}. Anklickbar: ${S.sichtbareHotspots(z).map((h) => h.name).join(', ')}._`);
            p();
        }
    }
    p(`**${nr}. ${titel}**`);
    p();
    if (e.text && e.text.length) {
        const texte = [...e.text];
        if (z.szene !== vorher.szene && z.szene !== 'karte' && !e.ohneEinstieg && !texte.includes(S.einstieg(z, z.szene))) { texte.push(S.einstieg(z, z.szene)); }
        p(zitat(texte, e, z)); p();
    }
    if (s[0] === 'minispiel' && s[1] === 'boot') {
        p('_Minispiel Bootsflucht: Das Ruderboot fährt flussab. Von vorn treibt Treibgut heran (Treibholz, Entenfamilie, Schild „Alternative Route“,');
        p(`Plakat „Grenzen dicht!“). Hinten folgen Pinsel und Ledeiw im Motorboot und schießen ${S.ALTERNATIVE_FAKTEN.length} alternative Fakten in eure Spur:`);
        p(`${S.ALTERNATIVE_FAKTEN.map((t) => `„${t}“`).join(', ')}. Anzeige oben: „Alternative Fakten: N“ und drei Herzen. Sind die Fakten verschossen,`);
        p('kippt das Motorboot und sinkt („Blubb!“). Drei Treffer = gekentert, noch einmal._');
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
