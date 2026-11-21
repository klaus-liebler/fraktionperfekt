// Joe & Chani 3D – Texte der Spiellogik, die in der 3D-Fassung an die neuen Schauplätze angepasst werden.
// logik.js bleibt unverändert (gleiche Regeln, Spielstände, Komplettlösung); hier ändern sich nur Namen und Beschreibungen,
// und zwar ausschließlich auf der 3D-Seite. Chris steht in 3D vor dem Rathaus statt vor der Archivtür.
(function () {
    'use strict';
    const S = window.GralSpiel;
    const h = (szene, id) => S.SZENEN[szene].hotspots.find((x) => x.id === id);
    const antwort = (text) => ({ text: Array.isArray(text) ? text : [text] });
    const ersetzen = (zeilen, alt, neu) => zeilen.map((z) => (z.indexOf(alt) === 0 ? neu : z));

    // ---------- Akt 1: Zukunftsschmiede ----------
    const tuer = h('buero', 'tuer');
    tuer.name = 'Wegweiser (zur Karte)';
    tuer.ansehen = () => antwort('Ein Wegweiser mitten in der Marktstraße: Rathaus, Ems, Hallenbad. Von hier aus geht es durch Greven.');
    h('buero', 'postfach').ansehen = () => antwort('Der Briefkasten der Fraktion, neben der Ladentür. Ganz unten liegt ein Umschlag mit rotem Stempel.');

    // ---------- Akt 2: Rathaus ----------
    const archiv = S.SZENEN.archiv;
    archiv.titel = 'Rathaus';
    archiv.einstieg = (z) => (z.flags.archivOffen
        ? 'Das Rathaus an der Rathausstraße. Der Eingang unter dem Sitzungstrakt steht offen; das Archiv liegt im Untergeschoss.'
        : 'Das Rathaus an der Rathausstraße. Vor dem Eingang unter dem Sitzungstrakt steht Genosse Chris von der SPD, ehrenamtlicher Archivpfleger, und verschränkt die Arme.');

    const chris = h('archiv', 'chris');
    chris.ansehen = () => antwort('Chris. Strickjacke, Thermoskanne, Schlüsselbund – der Rathausschlüssel ist auch dabei. Wollte selbst mal ins Rathaus: als Bürgermeister. Gewählt wurde aber Dietrich Aden von der CDU.');
    // In 3D steht Chris vor dem Rathaus: Er wollte Bürgermeister werden, die Wahl gewann Dietrich Aden (CDU) – jetzt lässt er niemanden rein.
    chris.reden = (z) => {
        if (z.flags.archivOffen) { return antwort('Chris: „Wenn ihr Hilfe braucht: Der Gewerkschaftsbus steht bereit. Solidarität hat einen Diesel.“'); }
        const wer = S.FIGUREN[z.figur].name;
        return antwort(['Chris: „Ins Rathaus? Ausgerechnet ihr?“',
            'Chris: „Da wollte ich auch mal rein. Als Bürgermeister. Plakate, Hausbesuche, Grünkohlessen – und dann wählen die Leute Dietrich Aden von der CDU.“',
            'Chris: „Wenn ich nicht reinkomme, kommt hier keiner rein.“ Er stellt sich breitbeinig vor die Glastür.',
            `${wer}: „Aber Chris, wir hatten doch etwas miteinander abgestimmt …“`,
            'Chris: „Abgestimmt? Mit der CDU? Daran würde ich mich erinnern.“',
            z.inventar.includes('spd_antrag') ? `${wer}: „Vielleicht hilft ihm sein eigener Antrag auf die Sprünge.“` : `${wer}: „Lag im Büro nicht etwas von der SPD im Briefkasten?“`]);
    };
    const abgeben = chris.gegenstand.spd_antrag;
    chris.gegenstand.spd_antrag = (z) => {
        const e = abgeben(z);
        e.text = ersetzen(e.text, 'Chris schließt die Archivtür auf', 'Chris schließt das Rathaus auf: „Archiv im Untergeschoss, Treppe rechts. Und grüßt den Bürgermeister nicht von mir.“ Er geht zum Ortsverein, der Gewerkschaftsbus wartet.');
        e.text = ersetzen(e.text, 'Kaum ist er weg, poltert es', 'Kaum ist er weg, drängen sich zwei Gestalten in zu engen Anzügen an euch vorbei ins Rathaus: „Der Gral gehört uns! Wir sind das Original!“ Sie poltern die Treppe zum Archiv hinunter.');
        return e;
    };

    const rathausTuer = h('archiv', 'archivtuer');
    rathausTuer.name = 'Rathaustür';
    rathausTuer.ansehen = () => antwort('Die Glastür des Rathauses, abgeschlossen. Ein Zettel: „Heute nur nach Beschluss“.');

    for (const id of ['flurausgang', 'ausgang']) {
        const a = h('archiv', id);
        a.name = 'Wegweiser (zur Karte)';
        a.ansehen = () => antwort('Ein Wegweiser an der Rathausstraße. Von hier aus geht es zurück durch Greven.');
    }
    h('archiv', 'gegner').ansehen = (z) => antwort(z.flags.gegnerArchivFest
        ? 'Pinsel und Ledeiw kleben an der Rückwand fest – mitsamt Parkettdiele. Sie diskutieren, ob Dichtstoff „Systemkleber“ ist.'
        : 'Pinsel und Ledeiw. Großes Auftreten, kleines Leseverständnis. Sie stehen breitbeinig vor der Stahltür – dem einzigen Weg nach draußen.');
    h('archiv', 'wappen').ansehen = () => antwort('Ein Ölgemälde: der erste Rat nach dem Krieg, darüber das Stadtwappen. Unten eine verblasste Zeile.');
})();
