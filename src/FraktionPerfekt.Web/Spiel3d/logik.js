// Joe & Chani und der Gral der Demokratie – Spiellogik (ohne Darstellung, in Node testbar).
// Ein eigenes Abenteuer im Stil der Pulp-Abenteuer der 30er-Jahre. Alle Figuren sind erfunden.
(function (wurzel) {
    'use strict';

    const VERSION = 1;

    const FIGUREN = {
        joe: {
            name: 'Joe',
            beschreibung: 'Fraktionschef, im Hauptberuf Vertreter für Baudichtstoffe (Remmers). Hut, Lederjacke, immer eine Kartusche dabei.',
            faehigkeiten: ['Kartusche', 'Verkaufsgespräch']
        },
        chani: {
            name: 'Chani',
            beschreibung: 'Social-Media-Referentin der Fraktion, hauptberuflich Kunsthistorikerin. Liest Inschriften wie andere Leute Speisekarten.',
            faehigkeiten: ['Deuten', 'Live-Story']
        }
    };

    const GEGENSTAENDE = {
        brief: { name: 'Anonymer Brief', text: '„Drei Siegel öffnen den Tempel: das, was beschließt, das, was ausführt, das, was richtet. Das erste ruht bei den Räten von 1949.“' },
        stadtplan: { name: 'Stadtplan von 1949 (Fax)', text: 'Ein gefaxter Stadtplan. Leicht verschmiert, aber mit Liebe gefaxt.' },
        spd_antrag: { name: 'SPD-Antrag „Tag der offenen Tür“', text: '„Antrag der SPD-Fraktion: Tag-der-offenen-Tür-Ordnung für öffentliche Gebäude. Vorab abgestimmt mit der CDU-Fraktion.“ Unterschrift: Chris.' },
        geschaeftsordnung: { name: 'Geschäftsordnung (Taschenbuch)', text: 'Die Geschäftsordnung des Rates. Eselsohren auf jeder Seite über Anträge zur Geschäftsordnung.' },
        taschenlampe: { name: 'Taschenlampe', text: 'Eine Werbe-Taschenlampe: „Dicht ist Pflicht“.' },
        paddel: { name: 'Paddel', text: 'Ein Paddel. Vergesellschaftet, aber funktionstüchtig.' },
        siegel_legislative: { name: 'Siegel der Beschlüsse', text: 'Ein Siegel mit einem aufgeschlagenen Buch: das, was beschließt.' },
        siegel_exekutive: { name: 'Siegel der Ausführung', text: 'Ein Siegel mit einem Schlüsselbund: das, was ausführt.' },
        siegel_judikative: { name: 'Siegel des Rechts', text: 'Ein Siegel mit einer Waage: das, was richtet.' },
        tempelschluessel: { name: 'Tempelschlüssel', text: 'Ein schwerer Schlüssel. In den Bart ist „Art. 20“ eingraviert.' },
        handy: { name: 'Joes Handy', text: 'Ein Klapphandy, Baujahr 2004, Akku hält drei Wochen. Im Telefonbuch: „Prof. Relbeil“. Benutzen ruft ihn an.' }
    };

    /** Vorstellung der beiden Figuren zu Spielbeginn (und im Menü „Figuren vorstellen“) – nur Fähigkeiten, die das Spiel wirklich braucht. */
    const VORSTELLUNG = [
        'Joe: „Ich bin Joe, Fraktionschef – im Hauptberuf Vertreter für Baudichtstoffe. Hut, Lederjacke, Kartusche.“',
        'Joe: „Meine Kartusche dichtet ab, was undicht ist: Lecks, Fugen – und wenn es sein muss, den Weg für ungebetene Gäste.“',
        'Joe: „Mein Verkaufsgespräch öffnet Türen. Wer einen Rahmenvertrag wittert, rückt auch mal Helme heraus.“',
        'Joe: „Und mein Handy: Professor Relbeil weiß immer, wie es weitergeht. Einfach anrufen.“',
        'Chani: „Ich bin Chani, Social-Media-Referentin der Fraktion und Kunsthistorikerin.“',
        'Chani: „Deuten: Gemälde, Fresken und alte Inschriften lese ich wie andere Leute Speisekarten.“',
        'Chani: „Live-Story: Vor laufender Kamera vergessen manche Leute alles andere. Sehr nützlich.“',
        'Relbeil: „Und ich bin Professor Relbeil – erreichbar über Joes Handy. Ruft an und sagt „Guten Tag!“, dann plaudern wir ein bisschen.“',
        'Relbeil: „Sagt ihr „Ich brauche deine Hilfe!“, verrate ich euch immer, was als Nächstes zu tun ist. Kostet keine Punkte.“',
        '(Unten wechselt ihr zwischen Joe und Chani; ihre Fähigkeiten stehen bei den Verben. Esc überspringt die Vorstellung.)'
    ];

    /** Einleitung nach der Vorstellung (neues Spiel). */
    const EINLEITUNG = [
        'Greven, Montagabend in der Zukunftsschmiede an der Marktstraße. Die Fraktionssitzung ist vorbei, die Kekse sind alle.',
        'Joe: „Chani, hast du den Umschlag auf dem Tisch gesehen? Ohne Absender.“',
        'Chani: „Und draußen steigen zwei Gestalten in zu engen Anzügen aus einem Wagen … Das gefällt mir nicht.“',
        '(Antippen = gehen und die naheliegende Aktion. Verben unten wählen, Gegenstände mit „Benutze“ oder „Gib“ kombinieren. Langes Drücken öffnet alle Aktionen. Esc: Pause.)'
    ];

    // Orte auf der Reisekarte (Lage der Stecknadeln: karte3d.json)
    const ORTE = {
        buero: { name: 'Zukunftsschmiede' },
        archiv: { name: 'Rathausarchiv' },
        ems: { name: 'Emsdeich (Kanuverleih)' },
        baustelle: { name: 'Baustelle Hallenbad' },
        schule: { name: 'Martin-Luther-Grundschule' }
    };

    const SIEGEL = ['siegel_legislative', 'siegel_exekutive', 'siegel_judikative'];

    function neuerZustand() {
        return {
            version: VERSION,
            szene: 'buero',
            figur: 'joe',
            inventar: ['handy'],
            flags: {},
            orte: ['buero'],
            platten: { links: null, rechts: null },
            kombination: [],
            quiz: 0,
            punkte: 0,
            sekunden: 0,
            ende: false
        };
    }

    // ---------- Hilfen ----------
    const hat = (z, g) => z.inventar.indexOf(g) >= 0;
    // „hatte_…“ merkt sich dauerhaft, dass ein Gegenstand schon einmal da war (für Professor Relbeils Hilfe; Gegenstände werden auch weitergegeben)
    const geben = (z, g) => { if (!hat(z, g)) { z.inventar.push(g); } z.flags['hatte_' + g] = true; };
    const nehmen = (z, g) => { const i = z.inventar.indexOf(g); if (i >= 0) { z.inventar.splice(i, 1); } };
    const ortFrei = (z, o) => { if (z.orte.indexOf(o) < 0) { z.orte.push(o); } };
    const punkte = (z, flag, n) => { if (!z.flags['p_' + flag]) { z.flags['p_' + flag] = true; z.punkte += n || 10; } };
    const istJoe = (z) => z.figur === 'joe';
    const name = (z) => FIGUREN[z.figur].name;

    /**
     * Antwort einer Aktion: Texte, optional Szenenwechsel, Dialog oder Minispiel.
     * Regie: Eine Zeile darf als [text, schlüssel …] stehen. Die Schlüssel (Flag, Gegenstand oder 'ende') nennen die
     * Zustandsänderungen, die mit dieser Zeile sichtbar werden. Die Darstellung zeigt Figuren, Dinge und das Inventar erst
     * dann im neuen Zustand – also erst, nachdem der Grund im Text steht; Figuren kommen und gehen dabei zu Fuß.
     * Ergebnis: { text: [...], regie?: { zeile: [schlüssel] }, szene?, dialog?, minispiel?, ende? }.
     */
    const antwort = (text, extra) => {
        const regie = {};
        const zeilen = (Array.isArray(text) ? text : [text]).map((zl, i) => {
            if (!Array.isArray(zl)) { return zl; }
            regie[i] = zl.slice(1);
            return zl[0];
        });
        const e = Object.assign({ text: zeilen }, extra || {});
        if (Object.keys(regie).length) { e.regie = regie; }
        return e;
    };

    // ---------- Szenen ----------
    // Jeder Hotspot: id, name, form (Art: person, gegner, tuer, pfeil … – bestimmt die naheliegende Aktion), sichtbar(z), Handler je Aktion.
    // Aktionen: ansehen, benutzen, reden, f1, f2 (Fähigkeiten der aktiven Figur), gegenstand (mit z.B. { spd_antrag: fn }).
    const SZENEN = {
        buero: {
            titel: 'Zukunftsschmiede, Marktstraße',
            stimmung: 'buero',
            einstieg: 'Die Zukunftsschmiede in der Marktstraße, Montagabend nach der Fraktionssitzung. Auf dem Tisch liegt ein Umschlag ohne Absender.',
            hotspots: [
                {
                    id: 'brief', name: 'Umschlag', form: 'brief',
                    sichtbar: (z) => !hat(z, 'brief'),
                    ansehen: () => antwort('Ein Umschlag, adressiert an „die Fraktion, die noch Briefe öffnet“.'),
                    benutzen: (z) => {
                        geben(z, 'brief'); z.flags.briefGelesen = true; ortFrei(z, 'archiv'); punkte(z, 'brief');
                        return antwort([[`${name(z)} nimmt den Umschlag vom Tisch, öffnet ihn und liest:`, 'brief'], [GEGENSTAENDE.brief.text, 'briefGelesen'],
                            `${name(z)}: „Die Räte von 1949 – das Rathausarchiv!“`, '(Neuer Ort auf der Karte: Rathausarchiv)']);
                    }
                },
                {
                    id: 'fax', name: 'Faxgerät des Arbeitskreises Digitalisierung', form: 'fax',
                    ansehen: () => antwort('Das Faxgerät des Arbeitskreises Digitalisierung. Es ist das modernste Gerät im Raum. Daneben ein Zettel: „WLAN-Passwort: siehe Fax“.'),
                    benutzen: (z) => {
                        if (hat(z, 'stadtplan') || z.flags.stadtplanWeg) { return antwort('Das Fax rattert zufrieden. Mehr kommt nicht.'); }
                        if (!istJoe(z)) { return antwort('Chani sucht vergeblich nach einem Touchscreen. „Wie wischt man hier?“ – Das ist eher Joes Gerät.'); }
                        geben(z, 'stadtplan'); punkte(z, 'fax');
                        return antwort(['Joe tippt eine Nummer aus dem Gedächtnis – er faxt seit 1998 Bestellungen für Dichtstoffe.', ['Das Stadtarchiv faxt zurück: ein Stadtplan von 1949. Ein Fluss ist rot eingekreist. Joe steckt ihn ein.', 'stadtplan']]);
                    },
                    f1: (z) => antwort(istJoe(z) ? 'Joe dichtet den Papiereinzug ab. Das Fax ist jetzt wasserdicht. Und sonst nichts.' : 'Chani: „Ein Gerät dieser Epoche gehört eigentlich ins Museum.“')
                },
                {
                    id: 'regal', name: 'Bücherregal', form: 'regal',
                    ansehen: () => antwort('Gesammelte Haushaltspläne, drei Ausgaben der Geschäftsordnung und ein Ordner „Ideen 2015“ (leer).'),
                    benutzen: (z) => {
                        if (hat(z, 'geschaeftsordnung')) { return antwort('Der Ordner „Ideen 2015“ ist immer noch leer.'); }
                        geben(z, 'geschaeftsordnung');
                        return antwort([[`${name(z)} nimmt die Geschäftsordnung als Taschenbuch mit. Man weiß nie.`, 'geschaeftsordnung']]);
                    },
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Die Fuge hinter dem Regal ist undicht. Später.“' : 'Chani deutet die Buchrücken: Die Haushaltspläne werden jedes Jahr dicker, die Ideen-Ordner dünner.')
                },
                {
                    id: 'postfach', name: 'Briefkasten', form: 'kasten',
                    ansehen: () => antwort('Der Briefkasten der Fraktion, neben der Ladentür. Ganz unten liegt ein Umschlag mit rotem Stempel.'),
                    benutzen: (z) => {
                        if (hat(z, 'spd_antrag') || z.flags.antragUebergeben) { return antwort('Nur noch Werbung für Wahlplakat-Kabelbinder.'); }
                        geben(z, 'spd_antrag');
                        return antwort(['Im Briefkasten liegt ein Antrag der SPD-Fraktion: „Tag-der-offenen-Tür-Ordnung für öffentliche Gebäude“.',
                            'Daran klebt ein Zettel: „Wie besprochen – vorab abgestimmt mit der CDU-Fraktion. Gruß, Chris“.',
                            [`${name(z)}: „Nett. Den nehmen wir mit.“`, 'spd_antrag']]);
                    }
                },
                {
                    id: 'garderobe', name: 'Garderobe', form: 'garderobe',
                    ansehen: () => antwort('Joes Hut, Joes Lederjacke und eine Taschenlampe mit Werbeaufdruck.'),
                    benutzen: (z) => {
                        if (hat(z, 'taschenlampe')) { return antwort('Der Hut sitzt. Die Jacke knarzt. Alles bereit.'); }
                        geben(z, 'taschenlampe');
                        return antwort([['Ihr nehmt die Taschenlampe mit. Joe setzt den Hut auf und zieht die Lederjacke an. Chani filmt es für die Story.', 'taschenlampe']]);
                    }
                },
                {
                    id: 'tuer', name: 'Wegweiser (zur Karte)', form: 'tuer',
                    ansehen: () => antwort('Ein Wegweiser mitten in der Marktstraße: Rathaus, Ems, Hallenbad. Von hier aus geht es durch Greven.'),
                    benutzen: (z) => z.flags.briefGelesen ? antwort('Ab zur Karte.', { szene: 'karte' }) : antwort(`${name(z)}: „Erst mal den Umschlag lesen.“`)
                }
            ]
        },

        // Rathaus: Chris steht vor dem Eingang (er wollte selbst Bürgermeister werden), das Archiv liegt im Untergeschoss
        archiv: {
            titel: 'Rathaus',
            stimmung: 'archiv',
            einstieg: (z) => (z.flags.archivOffen
                ? 'Das Rathaus an der Rathausstraße. Der Eingang unter dem Sitzungstrakt steht offen; das Archiv liegt im Untergeschoss.'
                : 'Das Rathaus an der Rathausstraße. Vor dem Eingang unter dem Sitzungstrakt steht Genosse Chris von der SPD, ehrenamtlicher Archivpfleger, und verschränkt die Arme.'),
            hotspots: [
                {
                    id: 'chris', name: 'Genosse Chris (SPD)', form: 'person',
                    sichtbar: (z) => !z.flags.archivOffen,
                    ansehen: () => antwort('Chris. Strickjacke, Thermoskanne, Schlüsselbund – der Rathausschlüssel ist auch dabei. Wollte selbst mal ins Rathaus: als Bürgermeister. Gewählt wurde aber Dietrich Aden von der CDU.'),
                    reden: (z) => {
                        if (z.flags.archivOffen) { return antwort('Chris: „Wenn ihr Hilfe braucht: Der Gewerkschaftsbus steht bereit. Solidarität hat einen Diesel.“'); }
                        return antwort(['Chris: „Ins Rathaus? Ausgerechnet ihr?“',
                            'Chris: „Da wollte ich auch mal rein. Als Bürgermeister. Plakate, Hausbesuche, Grünkohlessen – und dann wählen die Leute Dietrich Aden von der CDU.“',
                            'Chris: „Wenn ich nicht reinkomme, kommt hier keiner rein.“ Er stellt sich breitbeinig vor die Glastür.',
                            `${name(z)}: „Aber Chris, wir hatten doch etwas miteinander abgestimmt …“`,
                            'Chris: „Abgestimmt? Mit der CDU? Daran würde ich mich erinnern.“',
                            hat(z, 'spd_antrag') ? `${name(z)}: „Vielleicht hilft ihm sein eigener Antrag auf die Sprünge.“` : `${name(z)}: „Lag im Büro nicht etwas von der SPD im Briefkasten?“`]);
                    },
                    gegenstand: {
                        spd_antrag: (z) => {
                            nehmen(z, 'spd_antrag'); z.flags.antragUebergeben = true; z.flags.archivOffen = true; z.flags.gegnerArchiv = true; punkte(z, 'antrag');
                            return antwort([[`${name(z)} überreicht Chris den Antrag seiner eigenen Fraktion.`, 'spd_antrag', 'antragUebergeben'],
                                'Chris liest halblaut: „Tag-der-offenen-Tür-Ordnung für öffentliche Gebäude … vorab abgestimmt mit der CDU-Fraktion … Gruß, Chris.“ Er wird rot.',
                                'Chris: „Ach, DER Antrag! Ja, natürlich. Ein Rathaus ist ein öffentliches Gebäude. Da muss die Tür selbstverständlich offen sein.“',
                                ['Chris schließt das Rathaus auf: „Archiv im Untergeschoss, Treppe rechts. Und grüßt den Bürgermeister nicht von mir.“ Dann geht er zum Ortsverein – der Gewerkschaftsbus wartet.', 'archivOffen'],
                                ['Kaum ist er weg, drängen sich zwei Gestalten in zu engen Anzügen an euch vorbei ins Rathaus: „Der Gral gehört uns! Wir sind das Original!“ Sie poltern die Treppe zum Archiv hinunter.', 'gegnerArchiv']]);
                        }
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet Chris einen Rahmenvertrag über Fugendichtstoff an. Chris will das erst im Ortsverein beraten.'
                        : 'Chani startet eine Live-Story. Chris winkt und fragt, ob man das auch ausdrucken kann.')
                },
                {
                    id: 'archivtuer', name: 'Rathaustür', form: 'tuer',
                    sichtbar: (z) => !z.flags.archivOffen,
                    ansehen: () => antwort('Die Glastür des Rathauses, abgeschlossen. Ein Zettel: „Heute nur nach Beschluss“.'),
                    benutzen: () => antwort('Chris stellt sich davor: „Zutritt nur nach Beschluss. Und beschlossen ist hier gar nichts.“')
                },
                {
                    id: 'wappen', name: 'Gemälde „Der Rat von 1949“', form: 'bild',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Ein Ölgemälde: der erste Rat nach dem Krieg, darüber das Stadtwappen. Unten eine verblasste Zeile.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Schöner Rahmen. Die Fuge oben links würde ich neu ziehen.“'); }
                        z.flags.hinweisProtokoll = true; punkte(z, 'wappen');
                        return antwort(['Chani tritt nah heran: „Firnis um 1950, Pinselstrich eines Amateurs – und eine Inschrift unter dem Wappen.“', ['„Was beschlossen wird, ruht in Protokoll 47.“', 'hinweisProtokoll']]);
                    }
                },
                {
                    id: 'protokolle', name: 'Regal mit Ratsprotokollen', form: 'regal',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Ratsprotokolle seit 1946, sauber nummeriert.'),
                    benutzen: (z) => {
                        if (hat(z, 'siegel_legislative')) { return antwort('Protokoll 47 ist jetzt eine sehr gründliche Lektüre ohne Siegel.'); }
                        if (!z.flags.hinweisProtokoll) { return antwort('Hunderte Protokolle. Ohne Hinweis sucht ihr bis zur nächsten Kommunalwahl.'); }
                        geben(z, 'siegel_legislative'); punkte(z, 'siegel1', 20);
                        return antwort([['In Protokoll 47 steckt ein Siegel: ein aufgeschlagenes Buch. Das erste Siegel!', 'siegel_legislative']]);
                    }
                },
                {
                    id: 'gegner', name: 'Pinsel und Ledeiw (AfD)', form: 'gegner',
                    sichtbar: (z) => z.flags.gegnerArchiv && !z.flags.gegnerBefreit,
                    ansehen: (z) => antwort(z.flags.gegnerArchivFest
                        ? 'Pinsel und Ledeiw kleben an der Rückwand fest – mitsamt Parkettdiele. Sie diskutieren, ob Dichtstoff „Systemkleber“ ist.'
                        : 'Pinsel und Ledeiw. Großes Auftreten, kleines Leseverständnis. Sie stehen breitbeinig vor der Stahltür – dem einzigen Weg nach draußen.'),
                    reden: (z) => (z.flags.gegnerArchivFest
                        ? antwort(['Pinsel: „Das ist Freiheitsberaubung! Ich rufe meinen Anwalt!“', 'Ledeiw: „Unser Anwalt klebt auch. Der war gestern hier.“'])
                        : antwort(['Pinsel: „Hier steht ‚Ausgang‘. Das heißt bestimmt ‚Ausland‘. Da gehen wir nicht hin.“', 'Ledeiw: „Wir bleiben in der Tür stehen, bis die Altparteien den Gral rausrücken!“'])),
                    f1: (z) => {
                        if (z.flags.gegnerArchivFest) { return antwort('Joe: „Hält. 24 Stunden Haftung, wie versprochen.“'); }
                        if (!istJoe(z)) {
                            return antwort('Chani deutet die Körpersprache: breitbeinig, Arme verschränkt, Blick auf das Handy – sie warten auf eine Sprachnachricht vom Bundesvorstand.');
                        }
                        z.flags.dichtstoffArchiv = true; z.flags.gegnerArchivFest = true; ortFrei(z, 'ems'); punkte(z, 'kartusche1');
                        return antwort([['Joe zieht die Kartusche wie einen Revolver. Zwei saubere Raupen Dichtstoff auf das Parkett – genau vor ihre Schuhspitzen.', 'dichtstoffArchiv'],
                            'Pinsel tritt vor und klebt fest. Ledeiw will ihn retten und klebt auch. „Elastisch, überstreichbar, 24 Stunden Haftung“, sagt Joe.',
                            ['Beim Versuch, sich loszureißen, rutschen beide samt loser Parkettdiele bis an die Rückwand. Der Ausgang ist frei.', 'gegnerArchivFest'],
                            '(Neuer Ort auf der Karte: Emsdeich)']);
                    },
                    f2: (z) => antwort(z.flags.gegnerArchivFest
                        ? (istJoe(z) ? 'Joe bietet Lösungsmittel an. Zum Listenpreis. Sie lehnen ab – „aus Prinzip“.' : 'Chani geht live. Die beiden winken von der Rückwand. Mehr Bewegung ist gerade nicht drin.')
                        : istJoe(z)
                            ? 'Joe will ihnen Fugendichtstoff verkaufen. Sie fragen, ob der auch „aus Deutschland“ ist. Er ist es. Sie kaufen trotzdem nichts.'
                            : 'Chani geht live. Die beiden posieren und erklären zehn Minuten lang das Wort „Altparteien“. Sie stehen dabei aber weiter in der Tür.')
                },
                {
                    // Nach der Flucht über die Ems: Pinsel und Ledeiw sind weg, ihre Schuhsohlen kleben noch am Parkett
                    id: 'sohlen', name: 'Zwei Paar Schuhsohlen im Dichtstoff', form: 'sohlen',
                    sichtbar: (z) => z.flags.gegnerArchivFest && z.flags.gegnerBefreit,
                    ansehen: () => antwort('Zwei Paar Schuhsohlen, Größe 46, fest im Dichtstoff. Pinsel und Ledeiw sind auf Socken weiter.'),
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Elastisch, überstreichbar, 24 Stunden Haftung. Die Sohlen haben gehalten – die Schuhe nicht.“' : 'Chani: „Zeitgenössische Installation. Titel: ‚Abgang‘.“')
                },
                {
                    // Wegweiser vor dem Rathaus: immer zur Karte
                    id: 'flurausgang', name: 'Wegweiser (zur Karte)', form: 'tuer',
                    ansehen: () => antwort('Ein Wegweiser an der Rathausstraße. Von hier aus geht es zurück durch Greven.'),
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                },
                {
                    // Stahltür des Archivs: Solange Pinsel und Ledeiw davorstehen, kommt niemand hinaus
                    id: 'ausgang', name: 'Stahltür (zum Flur)', form: 'tuer',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Die Stahltür nach oben in den Flur.'),
                    benutzen: (z) => (z.flags.gegnerArchiv && !z.flags.gegnerArchivFest)
                        ? antwort('Pinsel und Ledeiw stehen breitbeinig vor der Stahltür.')
                        : antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        ems: {
            titel: 'Emsdeich am Kanuverleih',
            stimmung: 'ems',
            einstieg: 'Der Emsdeich, ein Stück flussauf vom Hallenbad. Am Ufer stehen Skulpturen, daneben der Kanuverleih mit einem Ruderboot am Steg – selbstgemaltes Schild: „Volkseigentum“. Davor Genossin Johanna von der Linken.',
            hotspots: [
                {
                    id: 'joanna', name: 'Genossin Johanna (Linke)', form: 'person',
                    ansehen: () => antwort('Johanna. Jutebeutel, Klemmbrett, Umverteilungsliste. Sie hat den Kanuverleih „in Selbstverwaltung übernommen“.'),
                    reden: (z) => {
                        if (hat(z, 'paddel') || z.flags.bootGewonnen) { return antwort('Johanna: „Viel Erfolg! Und wenn ihr den Gral findet: Der gehört allen.“'); }
                        return antwort(['Johanna: „Den Kanuverleih habe ich vergesellschaftet. Die Kanus sind alle mit dem Ortsverein auf Tour – aber das Ruderboot ist noch da.“', '„Das Paddel kriegt ihr. Aber ich verteile um: Gebt mir etwas aus eurem Inventar. Gerecht ist gerecht.“']);
                    },
                    gegenstand: {
                        stadtplan: (z) => {
                            nehmen(z, 'stadtplan'); z.flags.stadtplanWeg = true; geben(z, 'paddel'); punkte(z, 'joanna');
                            return antwort([['Johanna nimmt den Stadtplan („Kartenmaterial gehört dem Volk“) und gibt euch das Paddel.', 'stadtplan', 'paddel'],
                                ['Sie heftet den Plan an die Preistafel des Kanuverleihs.', 'stadtplanWeg']]);
                        },
                        geschaeftsordnung: () => antwort('Johanna: „Die Geschäftsordnung? Die bleibt bei euch. Die braucht ihr noch, glaubt mir.“'),
                        taschenlampe: () => antwort('Johanna: „Die Lampe braucht ihr selber. Ich verteile nicht von arm zu arm.“'),
                        brief: () => antwort('Johanna: „Persönliche Post verteile ich nicht um. Ich habe Grundsätze.“')
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet ihr Dichtstoff für die Kanus zum Einkaufspreis. Johanna: „Gibt es den auch genossenschaftlich?“'
                        : 'Chani macht eine Story über das Volkseigentum-Boot. 2000 Aufrufe in drei Minuten.')
                },
                {
                    id: 'verleih', name: 'Kanuverleih', form: 'huette',
                    ansehen: () => antwort('Eine Holzhütte mit Schild „KANUVERLEIH“. Das Wort „Preise“ auf der Tafel ist durchgestrichen, darüber steht „Solidarität“.'),
                    benutzen: () => antwort('Die Hütte ist leer. Auf einem Zettel: „Alle Kanus beim Ortsverein. Rückgabe: nach der Revolution.“')
                },
                {
                    id: 'skulptur', name: 'Steinskulptur mit Inschrift', form: 'inschrift',
                    ansehen: () => antwort('Eine Skulptur aus Sandstein, eine von mehreren am Emsdeich. Auf der Rückseite eine Bronzetafel, halb von Moos bedeckt.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Die Fuge zwischen Sockel und Stein ist seit 1949 nicht nachgearbeitet. Unglaublich.“'); }
                        z.flags.hinweisPfahl = true; punkte(z, 'bruecke');
                        return antwort(['Chani kratzt das Moos von der Tafel: „Frühe Nachkriegs-Antiqua, sehr selbstbewusst gesetzt.“', ['„Was ausführt, ruht in der dritten Laterne – flussab, wo das Wasser warm ist.“', 'hinweisPfahl'], 'Chani: „Flussab, wo das Wasser warm ist … das Hallenbad! Und davor die Radbrücke mit den gelben Laternen.“']);
                    }
                },
                {
                    id: 'skulpturen', name: 'Weitere Skulpturen', form: 'inschrift',
                    ansehen: () => antwort('Eine verrostete Stahlwelle und ein Bronzering. Keine Inschrift, aber sehr viel Kunst.'),
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Stahl, verzinkt. Da hält nichts.“' : 'Chani: „Zeitgenössisch. Schön, aber hier steht nichts geschrieben.“')
                },
                {
                    id: 'boot', name: 'Ruderboot „Volkseigentum“', form: 'boot',
                    sichtbar: (z) => !z.flags.bootGewonnen,
                    ansehen: () => antwort('Das Ruderboot des Kanuverleihs. Johanna hat „Volkseigentum“ daraufgemalt.'),
                    benutzen: (z) => {
                        if (!hat(z, 'paddel')) { return antwort('Ohne Paddel wird das eine sehr langsame Fahrt.'); }
                        if (!z.flags.hinweisPfahl) { return antwort(`${name(z)}: „Und wohin? Erst sollten wir wissen, was auf der Tafel an der Skulptur steht.“`); }
                        z.flags.gegnerBefreit = true;
                        return antwort(['Ihr springt ins Boot und legt ab, flussab Richtung Hallenbad.',
                            ['Hinter euch heult ein Motor auf: Pinsel und Ledeiw! Sie haben sich aus dem Dichtstoff befreit – auf Socken. Ihre Schuhsohlen kleben noch im Archiv.', 'gegnerBefreit'],
                            'Pinsel: „Feuer frei! Mit alternativen Fakten!“',
                            '(Ausweichen mit ↑/↓ oder oben/unten tippen. Das Boot hat drei Herzen; beim dritten Treffer kentert es und es geht von vorn los. Gewonnen, wenn Pinsel und Ledeiw die alternativen Fakten ausgehen.)'], { minispiel: 'boot' });
                    }
                },
                {
                    id: 'zurkarte', name: 'Weg zur Straße', form: 'pfeil',
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        baustelle: {
            titel: 'Baustelle Hallenbad',
            stimmung: 'baustelle',
            einstieg: (z) => z.flags.gegnerBaustelleWeg
                ? 'Der Eingang des Hallenbads, mitten in der Sanierung: Bauzaun, eine Grube vor der Tür, daneben das Wandbild der Grevener Bäder. Aus dem Bauwagen ruft es leise „Lügenpresse!“.'
                : z.flags.gegnerBaustelle
                    ? 'Der Eingang des Hallenbads, mitten in der Sanierung: vor der Tür eine Grube, die langsam vollläuft. Pinsel und Ledeiw, noch tropfnass von der Ems, schrauben die Buchstaben vom Schild „EINGANG HALLENBAD“.'
                    : 'Der Eingang des Hallenbads, mitten in der Sanierung: Bauzaun, eine Grube vor der Tür, die langsam vollläuft, daneben das Wandbild der Grevener Bäder.',
            hotspots: [
                {
                    id: 'bauleiter', name: 'Bauleiterin Frau Kemper', form: 'person',
                    ansehen: () => antwort('Frau Kemper, Bauleiterin. Helm, Klemmbrett, wenig Zeit.'),
                    reden: (z) => z.flags.helme ? antwort('Kemper: „Helme auf, dann dürft ihr runter. Und nichts anfassen, was teuer aussieht.“') : antwort('Kemper: „Ohne Helm kommt hier keiner in die Grube. Und Helme hab ich nur für Kunden.“'),
                    f2: (z) => {
                        if (!istJoe(z)) { return antwort('Chani filmt die Baustelle. Frau Kemper: „Bitte nicht die Grube filmen, die ist noch nicht dicht.“'); }
                        z.flags.helme = true; punkte(z, 'helme');
                        return antwort(['Joe: „Noch nicht dicht? Da hätte ich etwas für Sie.“ Zehn Minuten Fachgespräch über Fugen im Nassbereich.', ['Frau Kemper bestellt 40 Kartuschen – und reicht zwei Helme rüber. Joe und Chani setzen sie auf. „Kunden dürfen in die Grube.“', 'helme']]);
                    }
                },
                {
                    id: 'leitung', name: 'Leckende Leitung', form: 'rohr',
                    sichtbar: (z) => !z.flags.leckDicht && !z.flags.grubeTrocken,
                    ansehen: () => antwort('Aus einer alten Leitung sprudelt Wasser in die Grube.'),
                    f1: (z) => {
                        if (!istJoe(z)) { return antwort('Chani: „Spätes 20. Jahrhundert, Gusseisen. Kunsthistorisch uninteressant, aber nass.“'); }
                        z.flags.leckDicht = true; z.flags.grubeTrocken = true; punkte(z, 'leck');
                        return antwort([['Joe setzt die Kartusche an. Eine Raupe, glattgezogen mit dem Daumen. Das Leck ist dicht.', 'leckDicht'], ['Die Pumpe saugt die Grube leer. Unten glänzt etwas.', 'grubeTrocken']]);
                    }
                },
                {
                    id: 'grube', name: 'Baugrube', form: 'grube',
                    ansehen: (z) => antwort(z.flags.grubeTrocken ? 'Die Grube ist trocken. Unten liegt etwas Metallisches.' : 'Die Grube steht unter Wasser. Unten glänzt etwas.'),
                    benutzen: (z) => {
                        if (hat(z, 'siegel_judikative')) { return antwort('In der Grube liegt jetzt nur noch ein vergessener Zollstock.'); }
                        if (!z.flags.grubeTrocken) { return antwort('Zu nass. Erst das Leck stopfen.'); }
                        if (!z.flags.helme) { return antwort('Frau Kemper: „Ohne Helm nicht in die Grube!“'); }
                        geben(z, 'siegel_judikative'); punkte(z, 'siegel3', 20);
                        return antwort([[`${name(z)} steigt mit Helm in die Grube. Im alten Schacht unter dem Pflaster, direkt vor der Eingangstür: ein Siegel mit einer Waage. Das dritte Siegel!`, 'siegel_judikative']]);
                    }
                },
                {
                    id: 'gegner', name: 'Pinsel und Ledeiw (AfD)', form: 'gegner',
                    // Erst nach dem Bootsunglück: Sie stapfen aus der Ems herein (minispielEnde)
                    sichtbar: (z) => z.flags.gegnerBaustelle && !z.flags.gegnerBaustelleWeg,
                    ansehen: () => antwort('Pinsel und Ledeiw, tropfnass und auf Socken. Pinsel steht auf der Leiter und schraubt die Buchstaben vom Schild „EINGANG HALLENBAD“, Ledeiw hält die Leiter und stapelt die Buchstaben vor dem Bauzaun-Tor.'),
                    reden: () => antwort(['Ledeiw: „‚Eingang‘ – klingt nach offenen Grenzen. Muss weg.“', 'Pinsel: „‚Hallenbad‘ darf bleiben. Das ist deutsches Kulturgut. Mit Sprungturm.“']),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe überlegt, sie festzukleben. Aber auf Sand hält selbst der beste Dichtstoff nicht. Berufsehre.'
                        : 'Chani deutet: „Sie suchen nach Bestätigung, nicht nach dem Gral. Eine Kamera würde sie ablenken.“'),
                    f2: (z) => {
                        if (istJoe(z)) { return antwort('Joe will ihnen Dichtstoff verkaufen. „Ist da Gender drin?“, fragt Ledeiw. Gespräch beendet.'); }
                        z.flags.gegnerBaustelleWeg = true; punkte(z, 'story');
                        return antwort(['Chani hebt das Handy: „Wir sind live! Wollen Sie den Menschen etwas sagen?“', ['Beide stürzen zur Kamera, drehen sich ins beste Licht – und laufen dabei rückwärts in den Bauwagen. Die Tür fällt zu.', 'gegnerBaustelleWeg'], 'Von innen: „Lügenpresse!“ Chani: „Ich bin Kunsthistorikerin.“']);
                    }
                },
                {
                    // Das Wandbild „Grevener Bäder“ neben dem Eingang
                    id: 'wandbild', name: 'Wandbild „Grevener Bäder“', form: 'bild',
                    ansehen: () => antwort('Das Wandbild neben dem Eingang: „GREVENER BÄDER“, das E in Schwarz-Rot-Gold. Ein Junge saust lachend eine blaue Rutsche hinunter, daneben taucht ein Schwimmer mit Taucherbrille.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe: „Ein Graffiti abdichten? Niemals. Das ist Kunst am Bau.“'
                        : ['Chani tritt einen Schritt zurück: „Sprühdose, sicherer Strich, um 2020. Der Junge lacht zur Kamera – aber der Taucher schaut nach unten.“', 'Chani: „Genau dahin, wo jetzt die Grube ist.“'])
                },
                {
                    // Das Schild über dem Eingang: Pinsel und Ledeiw schrauben die Buchstaben ab, Joe schraubt sie wieder an
                    id: 'schild', name: 'Schild „EINGANG HALLENBAD“', form: 'inschrift',
                    ansehen: (z) => antwort(z.flags.schildHeil
                        ? 'Das Schild über der Tür: „EINGANG HALLENBAD“. Alle Buchstaben sitzen, frisch verschraubt.'
                        : z.flags.gegnerBaustelle ? 'Das Schild über der Tür: „      HALLENBAD“. Wo „EINGANG“ stand, sind nur noch Schraublöcher.' : 'Das Schild über der Tür: „EINGANG HALLENBAD“, dunkelblau auf hellblau.'),
                    benutzen: (z) => (z.flags.gegnerBaustelle && !z.flags.schildHeil
                        ? antwort('Ohne Buchstaben gibt es nichts anzuschrauben. Die liegen vor dem Tor.')
                        : antwort('Das Schild hängt fest. Daran hat sich schon einmal jemand versucht.'))
                },
                {
                    // Die abgeschraubten Buchstaben: Joe schraubt sie wieder an
                    id: 'buchstaben', name: 'Abgeschraubte Buchstaben „EINGANG“', form: 'kasten',
                    sichtbar: (z) => z.flags.gegnerBaustelleWeg && !z.flags.schildHeil,
                    ansehen: () => antwort('E, I, N, G, A, N, G – sieben Buchstaben auf dem Pflaster, die Schrauben in einem Joghurtbecher daneben.'),
                    benutzen: (z) => {
                        if (!istJoe(z)) { return antwort('Chani hält die Leiter fest: „Joe, du bist dran. Du hast den Akkuschrauber.“'); }
                        z.flags.schildHeil = true; punkte(z, 'schild');
                        return antwort(['Joe zieht den Akkuschrauber aus der Jackentasche – Vertreterausstattung – und steigt auf die Leiter.',
                            ['Schraube für Schraube: E – I – N – G – A – N – G. Über der Tür steht wieder „EINGANG HALLENBAD“.', 'schildHeil'],
                            'Joe: „Abgedichtet hab ich’s nicht. Aber festgeschraubt. Hält länger als jede Legislaturperiode.“']);
                    },
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Buchstaben klebt man nicht. Die schraubt man.“' : 'Chani: „Grotesk, serifenlos, Siebzigerjahre. Gehört über die Tür.“')
                },
                {
                    // Die Radbrücke mit den gelben Laternen steht beim Hallenbad; in der dritten Laterne ruht das Siegel der Ausführung
                    id: 'laterne', name: 'Dritte Laterne der Radbrücke', form: 'laterne',
                    sichtbar: (z) => !hat(z, 'siegel_exekutive'),
                    ansehen: () => antwort('Die dritte der gelben Laternen auf der Radbrücke, vom Hallenbad aus gezählt. Am Sockel sitzt eine Klappe, die nicht ganz schließt.'),
                    benutzen: (z) => {
                        z.flags.klappeAuf = true; geben(z, 'siegel_exekutive'); punkte(z, 'siegel2', 20);
                        return antwort([[`${name(z)} hebelt die lose Klappe am Sockel der dritten Laterne auf.`, 'klappeAuf'], ['Darin: ein Siegel mit einem Schlüsselbund. Das zweite Siegel!', 'siegel_exekutive']]);
                    },
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe: „Die Klappe ist nicht abgedichtet. Da hat jemand etwas versteckt – oder gepfuscht.“'
                        : 'Chani: „Die dritte Laterne. Hier ruht, was ausführt.“')
                },
                {
                    id: 'zurkarte', name: 'Bauzaun-Tor', form: 'pfeil',
                    benutzen: (z) => {
                        if (z.flags.gegnerBaustelle && !z.flags.gegnerBaustelleWeg) { return antwort('Vor dem Tor stapeln sich die abgeschraubten Buchstaben, und Ledeiw hält die Leiter quer davor.'); }
                        if (hat(z, 'siegel_judikative')) { ortFrei(z, 'schule'); }
                        return antwort(hat(z, 'siegel_judikative') ? ['Drei Siegel! Laut Brief führt der Weg zum Tempel unter die Martin-Luther-Grundschule.', '(Neuer Ort auf der Karte: Martin-Luther-Grundschule)'] : 'Zur Karte.', { szene: 'karte' });
                    }
                }
            ]
        },

        schule: {
            titel: 'Hof der Martin-Luther-Grundschule',
            stimmung: 'schule',
            einstieg: 'Ein leerer Schulhof. Im Haupteingang brennt Licht – Elternabend. Davor: Lorette von den Grünen, mit Lastenrad und Gutachten.',
            hotspots: [
                {
                    id: 'lore', name: 'Lorette (Grüne)', form: 'person',
                    ansehen: () => antwort('Lorette. Fahrradhelm, Lastenrad, ein Gutachten unter dem Arm, dick wie ein Haushaltsplan.'),
                    reden: (z) => {
                        if (z.flags.abend && !z.flags.lastenrad) {
                            z.flags.lastenrad = true; punkte(z, 'lastenrad');
                            return antwort(['Lorette: „Die Fledermäuse sind ausgeflogen. Geht ruhig rein.“', '„Und nehmt das Lastenrad mit. Für den Rückweg – man weiß ja nie, was in so einem Tempel herumrollt.“', ['Mit drei Handgriffen klappt Lorette das Lastenrad zusammen – Lenker, Kiste, Räder –, bis es so groß ist wie eine Brotdose. Chani steckt es in ihre Tasche. Lorette: „Faltrad-Gutachten, Seite 213.“', 'lastenrad']]);
                        }
                        if (z.flags.lastenrad) { return antwort('Lorette: „Und bringt mir das Lastenrad mit vollem Akku zurück!“'); }
                        return antwort(['Lorette: „Der Kellergang ist gesperrt. Fledermausgutachten! Hier überwintern Zwergfledermäuse.“', '„Erst wenn sie zur Nacht ausfliegen, ab halb neun, darf jemand durch. Steht auf Seite 212.“']);
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet an, den Gang fledermausfreundlich abzudichten. Lorette: „Dafür brauchen wir ein Gutachten.“'
                        : 'Chani macht eine Story über Zwergfledermäuse. Lorette ist begeistert und teilt sie sofort.')
                },
                {
                    id: 'uhr', name: 'Schulhofuhr', form: 'uhr',
                    ansehen: (z) => antwort(z.flags.abend ? 'Die Uhr zeigt 20:35.' : 'Die Uhr zeigt 18:10.'),
                    benutzen: (z) => {
                        if (z.flags.abend) { return antwort('Es ist schon Abend.'); }
                        z.flags.abend = true; punkte(z, 'warten');
                        return antwort(['Ihr setzt euch auf die Tischtennisplatte und wartet. Joe erzählt von Silikon, Chani von Caravaggio.', ['20:30 Uhr, es ist dunkel geworden: Eine Wolke kleiner Fledermäuse schwirrt aus dem Kellergang in die Nacht.', 'abend']]);
                    }
                },
                {
                    id: 'eingang', name: 'Haupteingang', form: 'tuer',
                    ansehen: () => antwort('Der Haupteingang der Grundschule. Die Tür steht offen, drinnen ist Elternabend.'),
                    benutzen: () => antwort('Ihr geht hinein.', { szene: 'schulflur' })
                },
                {
                    id: 'zurkarte', name: 'Schultor', form: 'pfeil',
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        schulflur: {
            titel: 'Flur der Martin-Luther-Grundschule',
            stimmung: 'schule',
            einstieg: 'Ein langer Flur: Kinderbilder, Turnbeutel, zwei Klassenzimmer und das WC. Am Ende die Kellertür – mit Absperrband.',
            hotspots: [
                {
                    id: 'klasse1a', name: 'Klasse 1a', form: 'tuer',
                    ansehen: () => antwort('Klasse 1a. Ein Schild: „Klassenrat – dienstags in der 3. Stunde“. Drinnen ist es dunkel; die Kinder sind längst zu Hause.'),
                    benutzen: (z) => antwort(istJoe(z)
                        ? 'Abgeschlossen. An der Tür hängt das Protokoll von heute Vormittag: „Klassenrat: längere Hofpause – 23 Ja, 1 Nein.“ Joe: „Mehr Beteiligung als bei uns im Rat.“'
                        : 'Abgeschlossen. Ein Zettel in Kinderschrift: „Anträge an den Klassenrat bitte in den Briefkasten. Ohne Antrag kein Rederecht!“ Chani: „Die lernen das früh.“')
                },
                {
                    id: 'klasse2b', name: 'Klasse 2b', form: 'tuer',
                    ansehen: () => antwort('Klasse 2b. Hinter der Tür: Stimmengewirr.'),
                    benutzen: () => antwort(['Ihr öffnet die Tür einen Spalt. Drinnen tagt seit 19 Uhr der Elternabend – Thema: die Farbe der Turnbeutel.', 'Eine Mutter: „Wollen Sie sich als Klassenpflegschaft aufstellen lassen?“ Ihr schließt die Tür sehr leise.'])
                },
                {
                    id: 'wc', name: 'WC', form: 'tuer',
                    ansehen: () => antwort('Die Toilette. Ein Schild: „Bitte Hände waschen!“'),
                    benutzen: () => antwort('Besetzt. Von drinnen: „Moment noch!“')
                },
                {
                    id: 'kellertuer', name: 'Kellertür', form: 'tuer',
                    ansehen: () => antwort('Eine Stahltür, Aufschrift „KELLER“. Absperrband und ein Zettel: „Fledermausquartier – siehe Gutachten, Seite 212“.'),
                    benutzen: (z) => {
                        if (!z.flags.abend) { return antwort('Auf dem Zettel steht: „Zutritt erst nach dem Ausflug der Fledermäuse (ab 20:30 Uhr)“. Lorette hat ihn unterschrieben.'); }
                        if (!hat(z, 'taschenlampe')) { return antwort('Hinter der Tür ist es stockdunkel. Ohne Licht geht hier niemand runter.'); }
                        return antwort('Ihr knipst die Taschenlampe an und steigt die Kellertreppe hinab …', { szene: 'krypta' });
                    }
                },
                {
                    id: 'zumhof', name: 'Ausgang zum Hof', form: 'pfeil',
                    benutzen: () => antwort('Zurück auf den Schulhof.', { szene: 'schule' })
                }
            ]
        },

        krypta: {
            titel: 'Geheime Krypta unter der Schule',
            stimmung: 'krypta',
            einstieg: 'Ein Gewölbe aus Backstein. An der Wand ein Fresko, darunter ein Schloss mit drei drehbaren Symbolen, daneben ein Tor mit drei Vertiefungen.',
            hotspots: [
                {
                    id: 'fresko', name: 'Wandfresko', form: 'bild',
                    ansehen: () => antwort('Ein Fresko mit drei Bildern übereinander. Sehr verblasst.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Kalkputz. Wer da Silikon drauf macht, hat nichts verstanden.“'); }
                        z.flags.hinweisKrypta = true; punkte(z, 'fresko');
                        return antwort(['Chani leuchtet das Fresko ab: „Unten eine Urne, darüber ein Buch, ganz oben eine Waage.“', ['„Und die Inschrift: Von unten nach oben wird der Staat gebaut.“', 'hinweisKrypta']]);
                    }
                },
                {
                    id: 'schloss', name: 'Symbolschloss', form: 'schloss',
                    sichtbar: (z) => !z.flags.schlossOffen && !hat(z, 'tempelschluessel') && !z.flags.tempelOffen,
                    ansehen: () => antwort('Drei Drehscheiben mit Symbolen: Waage, Buch, Urne.'),
                    benutzen: (z) => {
                        z.kombination = [];
                        return antwort('Welches Symbol kommt zuerst?', { dialog: 'schloss' });
                    }
                },
                {
                    id: 'tor', name: 'Tor mit drei Vertiefungen', form: 'tor',
                    ansehen: () => antwort('Ein steinernes Tor. Drei runde Vertiefungen – genau so groß wie die Siegel. Und ein Schlüsselloch.'),
                    benutzen: (z) => {
                        const fehlend = SIEGEL.filter((s) => !hat(z, s));
                        if (fehlend.length > 0) { return antwort(`Es fehlen noch ${fehlend.length} Siegel.`); }
                        if (!hat(z, 'tempelschluessel')) { return antwort('Die Siegel passen – aber das Schlüsselloch bleibt leer.'); }
                        z.flags.tempelOffen = true; punkte(z, 'tor', 20);
                        return antwort([['Drei Siegel rasten ein. Der Schlüssel dreht sich mit einem tiefen „Klonk“.', 'tempelOffen'], 'Das Tor schwingt auf: der Tempel des Grundgesetzes.'], { szene: 'tempel' });
                    }
                },
                {
                    id: 'zurueck', name: 'Treppe nach oben', form: 'pfeil',
                    benutzen: () => antwort('Zurück in den Schulflur.', { szene: 'schulflur' })
                }
            ]
        },

        tempel: {
            titel: 'Tempel des Grundgesetzes',
            stimmung: 'tempel',
            einstieg: 'Eine Halle aus Sandstein, Säulen, Fackeln. Vor euch ein Fallgitter, links und rechts davon zwei Plattformen auf Drehzapfen und eine Inschrift.',
            hotspots: [
                {
                    id: 'inschrift', name: 'Inschrift über dem Fallgitter', form: 'inschrift',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('„FRAKTIONSZWANG“ steht über dem Gitter. Darunter kleiner Text.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe: „Steht da was von Dichtstoff? Nein? Dann soll Chani das lesen.“'
                        : ['Chani liest: „Wer im Gleichschritt kommt, bleibt draußen. Das Gewissen öffnet das Tor.“', '„Jeder auf seine eigene Plattform, jeder für sich: Auf der schmalen ist ein Hut eingraviert, auf der dicken eine Kamera.“'])
                },
                {
                    id: 'platte_links', name: 'Linke Plattform (schmal, mit Hut)', form: 'platte',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('Eine schmale Steinplattform auf einem Drehzapfen. Sie wippt schon, wenn man sie ansieht. Eingraviert: ein Hut.'),
                    benutzen: (z) => platteBetreten(z, 'links')
                },
                {
                    id: 'platte_rechts', name: 'Rechte Plattform (dick, mit Kamera)', form: 'platte',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('Eine dicke Steinplattform auf einem Drehzapfen. Eingraviert: eine Kamera.'),
                    benutzen: (z) => platteBetreten(z, 'rechts')
                },
                {
                    id: 'gitter', name: 'Fallgitter', form: 'gitter',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('Ein schweres Fallgitter. Es bewegt sich nur, wenn jemand nicht mitmarschiert.'),
                    benutzen: () => antwort('Rütteln hilft nicht. Das Gitter hat Fraktionsdisziplin.'),
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Ein Gitter abdichten? Wozu?“' : 'Chani: „Neogotisch. Und sehr symbolisch.“')
                },
                {
                    id: 'tafel', name: 'Tafel der Geschäftsordnung', form: 'tafel',
                    sichtbar: (z) => z.flags.gitterOffen && !z.flags.goGeloest,
                    ansehen: () => antwort('Eine Steintafel mit Fragen zur Geschäftsordnung. Wer alle richtig beantwortet, darf weiter.'),
                    benutzen: (z) => { z.quiz = 0; return antwort('Die Tafel leuchtet auf.', { dialog: 'quiz' }); }
                },
                {
                    id: 'abgrund', name: 'Abgrund', form: 'abgrund',
                    sichtbar: (z) => z.flags.goGeloest && !z.flags.brueckeDa,
                    ansehen: () => antwort('Ein Abgrund, zu breit zum Springen. Auf der anderen Seite ein Pult mit der Aufschrift: „Die Brücke erscheint für den Antrag, der allen anderen vorgeht.“'),
                    benutzen: () => antwort('Ihr tretet an die Kante und ruft zum Pult hinüber. Welchen Antrag stellt ihr?', { dialog: 'bruecke' })
                },
                {
                    id: 'gang', name: 'Langer abschüssiger Gang', form: 'tuer',
                    sichtbar: (z) => z.flags.gitterOffen && !z.flags.kugelGeschafft,
                    ansehen: () => antwort('Ein langer Gang, leicht abschüssig. Am oberen Ende: eine riesige steinerne Wahlkugel. Sie wackelt.'),
                    benutzen: (z) => !z.flags.brueckeDa ? antwort('Der Gang liegt auf der anderen Seite des Abgrunds. Ohne Brücke kommt ihr da nicht hin.') : antwort(['Ein Knirschen. Die Wahlkugel löst sich und rollt los!', z.flags.lastenrad
                        ? 'Chani zieht das Lastenrad aus der Tasche – ein Ruck, ausgeklappt! Joe tritt in die Pedale, Chani springt in die Kiste. (Tippen oder Leertaste/→ für Tempo.)'
                        : '(Rennen! Tippen oder Leertaste/→ für Tempo.)'], { minispiel: 'kugel' })
                },
                {
                    id: 'pokal', name: 'Goldener Pokal auf einem Podest', form: 'pokal',
                    sichtbar: (z) => z.flags.kugelGeschafft && !z.ende,
                    ansehen: () => antwort('Ein goldener Pokal, mit Edelsteinen. Pinsel und Ledeiw starren ihn mit großen Augen an: „Der Gral! Unserer!“'),
                    benutzen: () => antwort('Pinsel reißt euch den Pokal aus der Hand. „Aus Gold! Wie früher!“ Auf der Unterseite steht: „Bezirksmeister Kegeln 1987“.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe klopft daran: „Hohl. Wie manche Argumente.“'
                        : 'Chani: „Pressgold, Fabrikware, um 1987. Das ist nicht der Gral. Der Gral ist das unscheinbare Ding daneben.“')
                },
                {
                    id: 'urne', name: 'Schlichte Holzkiste', form: 'urne',
                    sichtbar: (z) => z.flags.kugelGeschafft && !z.ende,
                    ansehen: () => antwort('Eine schlichte Holzkiste mit einem Schlitz im Deckel. An einer Schnur hängt ein Bleistift.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe: „Gute Verarbeitung. Dicht. Nichts kommt raus, was nicht rein soll.“'
                        : 'Chani: „Eine Wahlurne. Fichte, schlicht, um 1949. Die wertvollste Kiste im ganzen Raum.“'),
                    benutzen: (z) => finale(z)
                }
            ]
        }
    };

    // Balance-Plattformen: links (schmal, Hut) gehört Joe, rechts (dick, Kamera) gehört Chani; jede Figur steht für sich allein.
    const PLATZ = { links: 'joe', rechts: 'chani' };
    function platteBetreten(z, seite) {
        if (PLATZ[seite] !== z.figur) {
            return antwort(seite === 'links'
                ? ['Chani steigt auf die schmale linke Plattform. Sie kippt sofort weg, Chani springt ab.', 'Chani: „Da ist ein Hut eingraviert. Das ist Joes Platz.“']
                : ['Joe steigt auf die dicke rechte Plattform. Sie rührt sich keinen Millimeter.', 'Joe: „Eine Kamera ist eingraviert. Das ist eher was für Chani.“']);
        }
        z.platten[seite] = z.figur;
        const texte = [`${name(z)} steigt auf die ${seite === 'links' ? 'schmale linke' : 'dicke rechte'} Plattform. Sie senkt sich ein Stück und rastet ein.`];
        if (z.platten.links === 'joe' && z.platten.rechts === 'chani') {
            z.flags.gitterOffen = true; punkte(z, 'gitter', 20);
            texte.push(['Jeder auf seiner Plattform, jeder nach seinem Gewissen: Das Fallgitter hebt sich knirschend.', 'gitterOffen']);
            texte.push('Joe und Chani springen herunter und gehen hindurch. Dahinter: eine Steintafel und ein Abgrund.');
        } else {
            texte.push(`Das Gitter bleibt unten. (Tipp: Figur wechseln – ${z.figur === 'joe' ? 'Chani gehört auf die rechte' : 'Joe gehört auf die linke'} Plattform.)`);
        }
        return antwort(texte);
    }

    /** Die aktive Figur geht woandershin: Sie steigt von ihrer Plattform herunter (die andere bleibt stehen, wo sie ist). */
    function platteVerlassen(z, figur) {
        for (const s of ['links', 'rechts']) { if (z.platten[s] === figur && !z.flags.gitterOffen) { z.platten[s] = null; } }
    }

    function finale(z) {
        z.ende = true; z.flags.ende = true; punkte(z, 'finale', 50);
        const f = z.flags;
        f.pokalGenommen = true; f.urneGetragen = true; f.gegnerAbgang = true;
        // Wer unterwegs geholfen hat, kommt jetzt dazu (jede Figur mit ihrer eigenen Zeile)
        if (f.antragUebergeben) { f.chrisHilft = true; }
        if (f.lastenrad) { f.loretteHilft = true; }
        if (f.stadtplanWeg) { f.johannaHilft = true; }
        return antwort([
            ['Pinsel reißt den Pokal vom Podest und jubelt. Ledeiw ruft: „Den Rest remigrieren wir auch!“ – und versucht, die Wahlurne allein hochzuheben.', 'pokalGenommen'],
            'Sie bewegt sich keinen Millimeter. Eine Inschrift am Boden: „Ich werde nur getragen, wenn viele mitanfassen.“',
            ['Joe hebt eine Seite an, Chani die andere. Und plötzlich ist die Urne ganz leicht.', 'urneGetragen'],
            f.chrisHilft ? ['Genosse Chris kommt herein, den Autoschlüssel des Gewerkschaftsbusses in der Hand: „Solidarität! Der Bus steht oben.“', 'chrisHilft'] : 'Von oben hupt ein Bus.',
            ...(f.loretteHilft ? [['Lorette kommt herein: „Die Urne fährt im Lastenkorb – angeschnallt. Chani, das Lastenrad bitte!“', 'loretteHilft']] : []),
            ...(f.johannaHilft ? [['Johanna kommt mit einem Tragegurt: „Gemeinsam tragen. Alle.“', 'johannaHilft']] : []),
            ['Pinsel und Ledeiw folgen ihrer „alternativen Faktenkarte“ zum Ausgang – und landen in der Fledermaus-Sackgasse. Lorette ruft: „Seite 212!“', 'gegnerAbgang'],
            'Der Gral der Demokratie ist kein Gold. Er ist eine Kiste mit einem Schlitz und ein Bleistift an einer Schnur. Gold wäre einfacher gewesen. Aber nicht so wertvoll.',
            ['ENDE – Danke fürs Spielen!', 'ende'],
            'Kartendaten der Reisekarte: © OpenStreetMap-Mitwirkende (ODbL).'
        ], { ende: true });
    }

    // ---------- Dialoge (Rätsel mit Auswahl) ----------
    const QUIZ = [
        {
            frage: 'Frage 1: Ein Mitglied beantragt mitten in der Debatte „Schluss der Rednerliste“. Wann wird darüber abgestimmt?',
            optionen: ['Sofort, vor der Sache selbst', 'Am Ende der Sitzung', 'Gar nicht, wenn der Vorsitzende dagegen ist'],
            richtig: 0,
            tipp: 'Die Geschäftsordnung (Seite mit Eselsohr): Anträge zur Geschäftsordnung gehen vor.'
        },
        {
            frage: 'Frage 2: Wer leitet die Sitzung des Rates?',
            optionen: ['Wer am lautesten redet', 'Die Bürgermeisterin oder der Bürgermeister', 'Das älteste Mitglied, immer'],
            richtig: 1,
            tipp: 'Die Geschäftsordnung: Den Vorsitz im Rat führt die Bürgermeisterin oder der Bürgermeister.'
        },
        {
            frage: 'Frage 3: Was braucht ein Beschluss in der Regel?',
            optionen: ['Die Mehrheit der abgegebenen Stimmen', 'Einstimmigkeit', 'Ein Gefühl, dass das Volk es so will'],
            richtig: 0,
            tipp: 'Die Geschäftsordnung: Beschlüsse werden mit der Mehrheit der abgegebenen Stimmen gefasst.'
        }
    ];

    const BRUECKE = {
        frage: 'Welchen Antrag stellt ihr am Pult?',
        optionen: ['Sachantrag: „Wir beantragen eine Brücke.“', 'Antrag zur Geschäftsordnung', 'Dringlichkeitsantrag: „Abgrund sofort zuschütten!“', 'Antrag auf alternative Fakten: „Der Abgrund existiert nicht.“'],
        richtig: 1
    };

    const SYMBOLE = ['waage', 'buch', 'urne'];
    const SYMBOLNAMEN = { waage: 'Waage', buch: 'Buch', urne: 'Urne' };
    const RICHTIGE_KOMBINATION = ['urne', 'buch', 'waage'];

    /** Aktueller Dialog: { frage, optionen: [{ id, text }] } oder null. */
    function dialogInhalt(z, art) {
        if (art === 'schloss') {
            const nr = z.kombination.length + 1;
            return { art, frage: `Symbol ${nr} von 3 (bisher: ${z.kombination.map((s) => SYMBOLNAMEN[s]).join(' – ') || '–'})`, optionen: SYMBOLE.map((s) => ({ id: s, text: SYMBOLNAMEN[s] })) };
        }
        if (art === 'quiz') {
            const q = QUIZ[z.quiz];
            return { art, frage: q.frage, optionen: q.optionen.map((t, i) => ({ id: String(i), text: t })) };
        }
        if (art === 'bruecke') {
            return { art, frage: BRUECKE.frage, optionen: BRUECKE.optionen.map((t, i) => ({ id: String(i), text: t })) };
        }
        return null;
    }

    /** Antwort im Dialog verarbeiten. Ergebnis wie bei aktion; mit dialog: art, wenn es weitergeht. */
    function dialogWahl(z, art, wahl) {
        if (art === 'schloss') {
            if (SYMBOLE.indexOf(wahl) < 0) { return antwort('Kein Symbol.'); }
            z.kombination.push(wahl);
            if (z.kombination.length < 3) { return antwort(`Klick: ${SYMBOLNAMEN[wahl]}.`, { dialog: 'schloss' }); }
            const ok = z.kombination.every((s, i) => s === RICHTIGE_KOMBINATION[i]);
            z.kombination = [];
            if (!ok) { return antwort('Klack. Die Scheiben drehen sich zurück. Nichts passiert.'); }
            z.flags.schlossOffen = true; geben(z, 'tempelschluessel'); punkte(z, 'schloss', 20);
            return antwort([['Urne, Buch, Waage – von unten nach oben. Das Schloss springt auf.', 'schlossOffen'], ['Darin: ein schwerer Schlüssel mit der Gravur „Art. 20“. Ihr nehmt ihn.', 'tempelschluessel']]);
        }
        if (art === 'quiz') {
            const q = QUIZ[z.quiz];
            if (Number(wahl) !== q.richtig) {
                z.quiz = 0;
                return antwort(hat(z, 'geschaeftsordnung')
                    ? ['Die Tafel wird dunkel. Alles von vorn.', `Chani blättert in der Geschäftsordnung: ${q.tipp}`]
                    : ['Die Tafel wird dunkel. Alles von vorn. Eine Geschäftsordnung zum Nachschlagen wäre jetzt praktisch.']);
            }
            z.quiz += 1;
            if (z.quiz < QUIZ.length) { return antwort('Richtig! Die nächste Zeile leuchtet auf.', { dialog: 'quiz' }); }
            z.flags.goGeloest = true; z.quiz = 0; punkte(z, 'quiz', 20);
            return antwort([['Alle Fragen richtig. Die Tafel erlischt. Jetzt fehlt nur noch ein Weg über den Abgrund.', 'goGeloest']]);
        }
        if (art === 'bruecke') {
            if (Number(wahl) !== BRUECKE.richtig) {
                return antwort(['Nichts geschieht. Aus der Tiefe ruft ein Echo: „… zur Sache! … zur Sache!“']);
            }
            z.flags.brueckeDa = true; punkte(z, 'bruecke2', 20);
            return antwort([[`„Antrag zur Geschäftsordnung!“ ruft ${name(z)}. Es knirscht: Aus beiden Kanten schieben sich Steinbalken, Bohlen klappen herab, Seile spannen sich – eine Hängebrücke über den Abgrund. Anträge zur Geschäftsordnung gehen eben vor.`, 'brueckeDa'], 'Am anderen Ende: ein langer, abschüssiger Gang.']);
        }
        return antwort('…');
    }

    // ---------- Aktionen ----------
    function sichtbareHotspots(z) {
        const s = SZENEN[z.szene];
        return s ? s.hotspots.filter((h) => !h.sichtbar || h.sichtbar(z)) : [];
    }

    const STANDARD = {
        ansehen: (z, h) => antwort(`${name(z)} betrachtet: ${h.name}.`),
        benutzen: (z) => antwort(istJoe(z) ? 'Joe: „Da kann ich nichts verkaufen und nichts abdichten.“' : 'Chani: „Das lasse ich lieber, wie es ist.“'),
        reden: (z) => antwort(istJoe(z) ? 'Joe räuspert sich. Keine Antwort.' : 'Chani: „Hallo?“ – Keine Antwort.'),
        f1: (z) => antwort(istJoe(z) ? 'Joe setzt die Kartusche an – und überlegt es sich anders. Nicht alles muss abgedichtet werden.' : 'Chani betrachtet es genau. Keine Inschrift, keine Signatur.'),
        f2: (z) => antwort(istJoe(z) ? 'Joe hält ein kurzes Verkaufsgespräch. Niemand kauft.' : 'Chani macht eine Story. Drei Likes.'),
        gegenstand: (z, h, g) => antwort(`${GEGENSTAENDE[g] ? GEGENSTAENDE[g].name : 'Das'} und ${h.name} – das passt nicht zusammen.`)
    };

    /**
     * Führt eine Aktion aus. art: ansehen | benutzen | reden | f1 | f2 | gegenstand; ziel: Hotspot-Id; gegenstand: Id aus dem Inventar.
     * Ergebnis: { text: [...], szene?, dialog?, minispiel?, ende? }. Szenenwechsel übernimmt die Funktion selbst.
     */
    function aktion(z, art, ziel, gegenstand) {
        if (z.ende) { return antwort('Das Abenteuer ist geschafft. Neues Spiel im Menü.'); }
        const h = sichtbareHotspots(z).find((x) => x.id === ziel);
        if (!h) { return antwort('Da ist nichts.'); }
        let ergebnis;
        if (art === 'gegenstand') {
            if (!hat(z, gegenstand)) { return antwort('Das habt ihr nicht dabei.'); }
            const f = h.gegenstand && h.gegenstand[gegenstand];
            ergebnis = f ? f(z) : STANDARD.gegenstand(z, h, gegenstand);
        } else {
            const f = h[art];
            ergebnis = f ? f(z, h) : (STANDARD[art] ? STANDARD[art](z, h) : antwort('Hm.'));
        }
        if (ergebnis.szene) { wechseln(z, ergebnis.szene); }
        return ergebnis;
    }

    function wechseln(z, szene) {
        z.szene = szene;
        if (szene !== 'karte' && SZENEN[szene]) { z.flags['besucht_' + szene] = true; }
    }

    /** Reisen von der Karte aus. */
    function reisen(z, ort) {
        if (z.szene !== 'karte') { return antwort('Erst zur Karte.'); }
        if (z.orte.indexOf(ort) < 0) { return antwort('Diesen Ort kennt ihr noch nicht.'); }
        const ziel = ort === 'schule' && z.flags.tempelOffen ? 'tempel' : ort;
        wechseln(z, ziel);
        return antwort(einstieg(z, ziel), { szene: ziel });
    }

    /** Einstiegstext einer Szene (darf vom Spielstand abhängen). */
    function einstieg(z, szene) {
        const e = SZENEN[szene] && SZENEN[szene].einstieg;
        return typeof e === 'function' ? e(z) : e;
    }

    function figurWechseln(z) {
        z.figur = z.figur === 'joe' ? 'chani' : 'joe';
        return antwort(`Jetzt spielt ${name(z)}. ${FIGUREN[z.figur].faehigkeiten.join(' und ')} stehen bereit.`);
    }

    function minispielEnde(z, art, gewonnen) {
        if (!gewonnen) {
            return antwort(art === 'boot'
                ? ['Der dritte Treffer – das Ruderboot kentert am Ufer. Nasse Füße, aber nichts passiert.', '(Noch einmal von vorn, wieder mit drei Herzen. Treibgut kommt von vorn, alternative Fakten von hinten – rechtzeitig mit ↑/↓ die Spur wechseln.)']
                : 'Die Kugel holt euch ein – ihr springt in eine Nische. Zurück zum Anfang des Gangs.');
        }
        if (art === 'boot') {
            // Nach der Flucht geht es nicht zurück zum Kanuverleih, sondern flussab direkt zum Hallenbad.
            z.flags.bootGewonnen = true; z.flags.gegnerBaustelle = true; punkte(z, 'boot', 30);
            ortFrei(z, 'baustelle');
            wechseln(z, 'baustelle');
            return antwort([
                'Pinsel und Ledeiw gehen die alternativen Fakten aus. Ledeiw: „Schieß doch mit der Wahrheit!“ – Pinsel: „Haben wir nicht an Bord.“',
                'Beide lehnen sich gleichzeitig über dieselbe Bordwand, um nach Nachschub zu suchen. Das Motorboot kippt, läuft voll und geht unter. Blubb. Vor lauter Dummheit.',
                ['Ihr treibt flussab bis unter die Radbrücke beim Hallenbad. Oben stehen die gelben Laternen, eine neben der anderen.', 'bootGewonnen'],
                '(Neuer Ort auf der Karte: Baustelle Hallenbad)',
                'Am Ufer: die Baustelle des Hallenbads. Vor dem Eingang mit dem Wandbild der Grevener Bäder eine Grube, die langsam vollläuft. Bauleiterin Frau Kemper schaut euch skeptisch an.',
                ['Da prusten Pinsel und Ledeiw aus der Ems, stapfen tropfnass und auf Socken zum Eingang, stellen eine Leiter ans Schild – und schrauben die Buchstaben von „EINGANG“ ab.', 'gegnerBaustelle']
            ], { szene: 'baustelle', ohneEinstieg: true });
        }
        z.flags.kugelGeschafft = true; z.flags.gegnerFinale = true; punkte(z, 'kugel', 30);
        return antwort([['Ihr hechtet in die Halle – die Kugel donnert vorbei und bleibt im Torbogen stecken.', 'kugelGeschafft'], 'Vor euch: ein goldener Pokal auf einem Podest. Und daneben eine schlichte Holzkiste.',
            ['Aus einem Seitengang stolpern Pinsel und Ledeiw herein, außer Atem und immer noch auf Socken.', 'gegnerFinale']]);
    }

    // ---------- Minispiele (deterministisch, mit eigenem Zufall) ----------
    function zufall(seed) {
        let s = seed >>> 0 || 1;
        return () => { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; };
    }

    /**
     * Bootsverfolgung: drei Spuren. Das Ruderboot fährt in der Bildmitte nach rechts (flussab); Treibgut kommt von vorn (rechts),
     * Pinsel und Ledeiw sitzen hinten (links) im Motorboot und schießen alternative Fakten in die Spur, in der ihr gerade seid.
     * Sind die Fakten verschossen, geht ihr Boot unter – gewonnen. Drei Treffer erlaubt.
     */
    const BOOT_X = 0.55;
    const ALTERNATIVE_FAKTEN = ['Die Ems fließt bergauf!', 'Wasser ist links-grün!', 'Paddeln ist Gender!', 'Wir sind das Volk!', 'Der Gral ist aus Gold!', 'Das Boot gehört uns!',
        'Klimawandel? Nebel!', 'Früher war mehr Ems!', 'Brücken sind Ideologie!', 'Enten sind Ausländer!', 'Wir haben gewonnen!', 'Fakten sind Meinung!'];

    function bootNeu(seed) {
        return { art: 'boot', zeit: 0, leben: 3, spur: 1, hindernisse: [], schuesse: [], munition: ALTERNATIVE_FAKTEN.length, gegnerSpur: 1,
            naechstes: 1.6, naechsterSchuss: 2.2, sinken: null, rnd: zufall(seed || 7), status: 'laeuft', treffer: 0 };
    }

    const BOOT_ARTEN = ['Treibholz', 'Entenfamilie', 'Schild „Alternative Route“', 'Plakat „Grenzen dicht!“ (treibt)'];

    // Bootsflucht bewusst gutmütig: Treibgut braucht vom Auftauchen bis zum Boot gut drei Sekunden (anfangs 0,16 Flusslängen
    // je Sekunde, später etwas mehr), alternative Fakten von hinten gut zwei Sekunden – Zeit genug zum Ausweichen.
    const BOOT_TEMPO = (s) => 0.16 + Math.min(0.06, s.zeit / 300), SCHUSS_TEMPO = 0.16, NAH = 0.045;

    function bootSchritt(s, dt, eingabe) {
        if (s.status !== 'laeuft') { return s; }
        if (eingabe === 'hoch') { s.spur = Math.max(0, s.spur - 1); }
        if (eingabe === 'runter') { s.spur = Math.min(2, s.spur + 1); }
        s.zeit += dt;
        // Treibgut von vorn
        const tempo = BOOT_TEMPO(s);
        s.naechstes -= dt;
        if (s.naechstes <= 0 && s.sinken === null) {
            s.hindernisse.push({ x: 1.05, spur: Math.floor(s.rnd() * 3), art: BOOT_ARTEN[Math.floor(s.rnd() * BOOT_ARTEN.length)] });
            s.naechstes = 2.3 + s.rnd() * 1.4;
        }
        for (const h of s.hindernisse) { h.x -= tempo * dt; }
        // Verfolger zielen auf eure Spur und schießen alternative Fakten von hinten
        s.gegnerSpur += Math.sign(s.spur - s.gegnerSpur) * Math.min(Math.abs(s.spur - s.gegnerSpur), dt * 2.5);
        s.naechsterSchuss -= dt;
        if (s.naechsterSchuss <= 0 && s.munition > 0) {
            s.munition -= 1;
            s.schuesse.push({ x: 0.2, spur: s.spur, text: ALTERNATIVE_FAKTEN[s.munition] });
            s.naechsterSchuss = 2.6 + s.rnd() * 1.2;
        }
        for (const f of s.schuesse) { f.x += SCHUSS_TEMPO * dt; }
        const nah = (x) => Math.abs(x - BOOT_X) < NAH;
        const getroffen = s.hindernisse.filter((h) => nah(h.x) && h.spur === s.spur);
        const beschossen = s.schuesse.filter((f) => nah(f.x) && f.spur === s.spur);
        const n = getroffen.length + beschossen.length;
        if (n > 0) {
            s.leben -= n; s.treffer += n;
            // Für die Anzeige: was zuletzt getroffen hat
            s.letzterTreffer = getroffen.length ? getroffen[0].art : `alternativer Fakt „${beschossen[0].text}“`;
            s.hindernisse = s.hindernisse.filter((h) => getroffen.indexOf(h) < 0);
            s.schuesse = s.schuesse.filter((f) => beschossen.indexOf(f) < 0);
        }
        s.hindernisse = s.hindernisse.filter((h) => h.x > -0.1);
        s.schuesse = s.schuesse.filter((f) => f.x < 1.1);
        // Keine Fakten mehr und alle verschossen: Das Motorboot geht unter
        if (s.munition === 0 && s.schuesse.length === 0 && s.sinken === null) { s.sinken = 0; }
        if (s.sinken !== null) { s.sinken += dt; }
        if (s.leben <= 0) { s.status = 'verloren'; } else if (s.sinken !== null && s.sinken >= 2.5) { s.status = 'gewonnen'; }
        return s;
    }

    /** Ausweich-Hilfe für Tests und Demo: Zeit bis zum nächsten Treffer je Spur; rechtzeitig in die sicherste Nachbarspur wechseln. */
    function bootAutopilot(s) {
        const tempo = BOOT_TEMPO(s);
        const zeitBis = (spur) => Math.min(Infinity,
            ...s.hindernisse.filter((h) => h.spur === spur && h.x > BOOT_X - 0.06).map((h) => Math.max(0, h.x - (BOOT_X + 0.06)) / tempo),
            ...s.schuesse.filter((f) => f.spur === spur && f.x < BOOT_X + 0.06).map((f) => Math.max(0, (BOOT_X - 0.06) - f.x) / SCHUSS_TEMPO));
        if (zeitBis(s.spur) > 0.45) { return null; }
        const ziel = [0, 1, 2].sort((a, b) => zeitBis(b) - zeitBis(a) || Math.abs(a - s.spur) - Math.abs(b - s.spur))[0];
        if (ziel === s.spur) { return null; }
        const naechste = s.spur + (ziel < s.spur ? -1 : 1);
        return zeitBis(naechste) > 0.05 ? (ziel < s.spur ? 'hoch' : 'runter') : null;
    }

    /** Wahlkugel: Spieler (pos 0..1) muss das Ende erreichen, bevor die Kugel ihn einholt. Tippen gibt Tempo. */
    function kugelNeu(lastenrad) {
        return { art: 'kugel', pos: 0.08, kugel: -0.06, tempo: 0, lastenrad: !!lastenrad, status: 'laeuft', zeit: 0 };
    }

    function kugelSchritt(s, dt, tippen) {
        if (s.status !== 'laeuft') { return s; }
        s.zeit += dt;
        if (tippen) { s.tempo = Math.min(s.lastenrad ? 0.5 : 0.4, s.tempo + (s.lastenrad ? 0.09 : 0.07)); }
        s.tempo = Math.max(0, s.tempo - dt * 0.35);
        s.pos += s.tempo * dt;
        s.kugel += (0.085 + s.zeit * 0.004) * dt;
        if (s.kugel >= s.pos) { s.status = 'verloren'; } else if (s.pos >= 1) { s.status = 'gewonnen'; }
        return s;
    }

    // ---------- Speichern ----------
    function speichern(z) {
        return JSON.stringify({
            version: VERSION, szene: z.szene, figur: z.figur, inventar: z.inventar, flags: z.flags, orte: z.orte,
            platten: z.platten, punkte: z.punkte, sekunden: Math.round(z.sekunden), ende: z.ende, gespeichertAm: new Date().toISOString()
        });
    }

    /** Spielstand aus Text laden; ungültige oder fremde Daten ergeben null. Unbekannte Werte werden verworfen. */
    function laden(text) {
        let d;
        try { d = JSON.parse(text); } catch (_) { return null; }
        if (!d || typeof d !== 'object' || d.version !== VERSION) { return null; }
        const z = neuerZustand();
        if (typeof d.szene === 'string' && (SZENEN[d.szene] || d.szene === 'karte')) { z.szene = d.szene; }
        if (d.figur === 'joe' || d.figur === 'chani') { z.figur = d.figur; }
        if (Array.isArray(d.inventar)) { z.inventar = d.inventar.map((g) => (g === 'tagesordnung' ? 'spd_antrag' : g)).filter((g) => typeof g === 'string' && GEGENSTAENDE[g]); }
        if (z.inventar.indexOf('handy') < 0) { z.inventar.unshift('handy'); }   // ältere Spielstände: Joes Handy gehört immer dazu
        if (d.flags && typeof d.flags === 'object') {
            for (const k of Object.keys(d.flags)) { if (/^[a-zA-Z_0-9]+$/.test(k) && d.flags[k] === true) { z.flags[k] = true; } }
            if (z.flags.agGegruendet) { z.flags.antragUebergeben = true; }   // ältere Spielstände (vorher: Mustertagesordnung)
            // Spielstände von vor der Regie: Teilschritte, die heute eigene Flags haben, nachtragen
            const f = z.flags;
            if (f.bootGewonnen) { f.gegnerBaustelle = true; }
            if (f.kugelGeschafft) { f.gegnerFinale = true; }
            if (f.grubeTrocken) { f.leckDicht = true; }
            if (f.gegnerArchivFest) { f.dichtstoffArchiv = true; }
            if (z.inventar.indexOf('siegel_exekutive') >= 0) { f.klappeAuf = true; }
            if (z.inventar.indexOf('tempelschluessel') >= 0 || f.tempelOffen) { f.schlossOffen = true; }
        }
        if (Array.isArray(d.orte)) { z.orte = d.orte.filter((o) => typeof o === 'string' && ORTE[o]); if (z.orte.indexOf('buero') < 0) { z.orte.unshift('buero'); } }
        if (d.platten && typeof d.platten === 'object') {
            for (const s of ['links', 'rechts']) { if (d.platten[s] === 'joe' || d.platten[s] === 'chani') { z.platten[s] = d.platten[s]; } }
        }
        if (Number.isFinite(d.punkte)) { z.punkte = Math.max(0, Math.floor(d.punkte)); }
        if (Number.isFinite(d.sekunden)) { z.sekunden = Math.max(0, d.sekunden); }
        z.ende = d.ende === true;
        return z;
    }

    /** Speicherplätze im Browser (localStorage); jeder Zugriff abgesichert – ohne Speicher läuft das Spiel trotzdem. */
    function speicherplaetze(speicher, praefix) {
        const p = praefix || 'fp-spiel-';
        const sicher = (f, ersatz) => { try { return f(); } catch (_) { return ersatz; } };
        return {
            PLAETZE: ['auto', '1', '2', '3'],
            schreiben: (platz, z) => sicher(() => { speicher.setItem(p + platz, speichern(z)); return true; }, false),
            lesen: (platz) => sicher(() => { const t = speicher.getItem(p + platz); return t ? laden(t) : null; }, null),
            info: (platz) => sicher(() => {
                const t = speicher.getItem(p + platz);
                if (!t) { return null; }
                const d = JSON.parse(t);
                return { szene: d.szene, punkte: d.punkte, am: d.gespeichertAm };
            }, null),
            loeschen: (platz) => sicher(() => { speicher.removeItem(p + platz); return true; }, false)
        };
    }

    // ---------- Komplettlösung (auch als Test, dass das Spiel lösbar ist) ----------
    const KOMPLETTLOESUNG = [
        ['aktion', 'benutzen', 'brief'],
        ['aktion', 'benutzen', 'fax'],
        ['aktion', 'benutzen', 'regal'],
        ['aktion', 'benutzen', 'postfach'],
        ['aktion', 'benutzen', 'garderobe'],
        ['aktion', 'benutzen', 'tuer'],
        ['reisen', 'archiv'],
        ['aktion', 'gegenstand', 'chris', 'spd_antrag'],
        ['figur'],
        ['aktion', 'f1', 'wappen'],
        ['aktion', 'benutzen', 'protokolle'],
        ['figur'],
        ['aktion', 'f1', 'gegner'],
        ['aktion', 'benutzen', 'ausgang'],
        ['reisen', 'ems'],
        ['aktion', 'gegenstand', 'joanna', 'stadtplan'],
        ['figur'],
        ['aktion', 'f1', 'skulptur'],
        ['aktion', 'benutzen', 'boot'],
        ['minispiel', 'boot', true],
        ['aktion', 'benutzen', 'laterne'],
        ['figur'],
        ['aktion', 'f1', 'leitung'],
        ['aktion', 'f2', 'bauleiter'],
        ['aktion', 'benutzen', 'grube'],
        ['figur'],
        ['aktion', 'f2', 'gegner'],
        ['figur'],
        ['aktion', 'benutzen', 'buchstaben'],
        ['figur'],
        ['aktion', 'benutzen', 'zurkarte'],
        ['reisen', 'schule'],
        ['aktion', 'benutzen', 'uhr'],
        ['aktion', 'reden', 'lore'],
        ['aktion', 'benutzen', 'eingang'],
        ['aktion', 'benutzen', 'kellertuer'],
        ['aktion', 'f1', 'fresko'],
        ['aktion', 'benutzen', 'schloss'],
        ['dialog', 'schloss', 'urne'],
        ['dialog', 'schloss', 'buch'],
        ['dialog', 'schloss', 'waage'],
        ['aktion', 'benutzen', 'tor'],
        ['aktion', 'benutzen', 'platte_rechts'],
        ['figur'],
        ['aktion', 'benutzen', 'platte_links'],
        ['aktion', 'benutzen', 'tafel'],
        ['dialog', 'quiz', '0'],
        ['dialog', 'quiz', '1'],
        ['dialog', 'quiz', '0'],
        ['aktion', 'benutzen', 'abgrund'],
        ['dialog', 'bruecke', '1'],
        ['aktion', 'benutzen', 'gang'],
        ['minispiel', 'kugel', true],
        ['aktion', 'f1', 'pokal'],
        ['aktion', 'benutzen', 'urne']
    ];

    /** Einen Schritt der Komplettlösung ausführen (für Tests und den Demo-Modus). */
    // ---------- Joes Handy: Professor Relbeil (fiktive Figur) ----------
    const PLAUDEREI = [
        'Relbeil: „Guten Tag, ihr beiden! Ich sitze gerade über einer Fußnote. Sie ist länger als der Text. Wie immer.“',
        'Relbeil: „Hallo Joe! Ich habe gehört, ihr sucht einen Gral. Das ist gut. Meine Generation hat nur Parkplätze gesucht.“',
        'Relbeil: „Guten Tag! Kurze Frage: Ist Dichtstoff eigentlich ein Dichter? Nein? Schade. Ich hätte da ein Seminar gehabt.“',
        'Relbeil: „Ah, Joe. Grüß Chani. Sag ihr, Caravaggio hätte bestimmt auch Live-Storys gemacht.“',
        'Relbeil: „Guten Tag! Ich kann gerade nicht lange – mein Tee wird sonst kalt, und das wäre wissenschaftlich unverantwortlich.“'
    ];

    /** Was ein Schritt der Komplettlösung bedeutet, in Worten (für Professor Relbeils Hilfe). */
    function schrittText(vorher, s) {
        const hotspot = (id) => { const sz = SZENEN[vorher.szene]; const h = sz && sz.hotspots.find((x) => x.id === id); return h ? h.name : id; };
        const wer = FIGUREN[vorher.figur].name;
        switch (s[0]) {
            case 'reisen': return `Reist über die Karte zu: ${ORTE[s[1]].name}.`;
            case 'figur': return `Wechselt die Figur – jetzt wird ${vorher.figur === 'joe' ? 'Chani' : 'Joe'} gebraucht.`;
            case 'minispiel': return s[1] === 'boot' ? 'Flieht im Boot die Ems hinab: hoch und runter steuern, dem Treibgut und den alternativen Fakten ausweichen, bis Pinsel und Ledeiw nichts mehr haben.' : 'Rennt vor der Kugel davon – im richtigen Moment springen.';
            case 'dialog': {
                const d = dialogInhalt(vorher, s[1]);
                const o = d && d.optionen.find((x) => x.id === s[2]);
                return s[1] === 'schloss' ? `Am Symbolschloss als Nächstes: ${o ? o.text : s[2]}.` : `Wählt die Antwort: „${o ? o.text : s[2]}“.`;
            }
            default: {
                const [, art, id, g] = s;
                if (art === 'f1' || art === 'f2') { return `${wer} soll „${FIGUREN[vorher.figur].faehigkeiten[art === 'f1' ? 0 : 1]}“ bei ${hotspot(id)} einsetzen.`; }
                if (art === 'gegenstand') { return `Gebt bzw. benutzt „${GEGENSTAENDE[g].name}“ bei ${hotspot(id)}.`; }
                if (art === 'reden') { return `Sprecht mit ${hotspot(id)}.`; }
                if (art === 'ansehen') { return `Schaut euch ${hotspot(id)} genau an.`; }
                return `Benutzt ${hotspot(id)}.`;
            }
        }
    }

    /**
     * Der nächste Schritt im aktuellen Spielstand: Die Komplettlösung wird von vorn nachgespielt; der erste Schritt, der etwas bringt,
     * das dem Spielstand noch fehlt (Merkmal, Gegenstand, Ort), ist der nächste. Davor ggf. der Weg in die richtige Szene.
     */
    function naechsterSchritt(z) {
        if (z.ende) { return ['Ihr habt es geschafft! Das Grundgesetz ist in guten Händen. Ich gehe jetzt meinen Tee trinken.']; }
        // Merkmale und Orte kommen nur hinzu; Gegenstände zählen über „hatte_…“ (sie werden unterwegs auch weitergegeben)
        const hatte = (g) => z.inventar.includes(g) || z.flags['hatte_' + g];
        const hatAlles = (x) => Object.keys(x.flags).every((k) => !x.flags[k] || z.flags[k] || (k.startsWith('hatte_') && hatte(k.slice(6))))
            && x.orte.every((o) => z.orte.includes(o));
        const nach = neuerZustand();
        const merkmale = (x) => Object.keys(x.flags).filter((k) => x.flags[k]).length + x.orte.length;
        let folge = [];   // Schritte seit dem letzten Fortschritt (z. B. Schloss, Bodenplatten, Quiz: erst zusammen bringen sie etwas)
        for (const s of KOMPLETTLOESUNG) {
            const vorher = JSON.parse(JSON.stringify(nach));
            const anzahl = merkmale(nach);
            schritt(nach, s);
            folge.push({ s, vorher });
            if (hatAlles(nach)) {
                if (merkmale(nach) !== anzahl) { folge = []; }
                continue;
            }
            const erster = folge[0].vorher;
            const hinweise = [];
            if (folge[0].s[0] !== 'reisen' && erster.szene !== z.szene) {
                hinweise.push(ORTE[erster.szene] ? `Reist über die Karte zu: ${ORTE[erster.szene].name}.` : `Geht zu: ${SZENEN[erster.szene] ? SZENEN[erster.szene].titel : erster.szene}.`);
            }
            if (folge[0].s[0] !== 'figur' && erster.figur !== z.figur && folge.some((f) => f.s[1] === 'f1' || f.s[1] === 'f2')) {
                hinweise.push(`Wechselt zu ${FIGUREN[erster.figur].name}.`);
            }
            for (const f of folge) {
                const text = schrittText(f.vorher, f.s);
                if (hinweise[hinweise.length - 1] !== text) { hinweise.push(text); }
            }
            return hinweise;
        }
        return ['Ihr seid fast am Ziel. Schaut euch im Tempel um – die letzte Tür wartet.'];
    }

    /** Joe ruft Professor Relbeil an: „hallo“ = Plauderei, „hilfe“ = der nächste Schritt (kostet keine Punkte). */
    function anrufen(z, wahl) {
        if (wahl === 'hilfe') {
            z.flags.hilfeAnrufe = (z.flags.hilfeAnrufe || 0) + 1;
            const h = naechsterSchritt(z);
            return antwort([`Joe: „Professor, ich brauche deine Hilfe!“`, `Relbeil: „Natürlich. ${h.join(' Dann: ')}“`]);
        }
        z.flags.plauderei = ((z.flags.plauderei || 0) + 1) % PLAUDEREI.length;
        return antwort(['Joe: „Guten Tag, Professor!“', PLAUDEREI[z.flags.plauderei]]);
    }

    function schritt(z, s) {
        switch (s[0]) {
            case 'aktion': return aktion(z, s[1], s[2], s[3]);
            case 'reisen': return reisen(z, s[1]);
            case 'figur': return figurWechseln(z);
            case 'dialog': return dialogWahl(z, s[1], s[2]);
            case 'minispiel': return minispielEnde(z, s[1], s[2]);
            default: return antwort('?');
        }
    }

    /**
     * Direkteinstiege (Spickzettel /cheatsheet, Adresse ?start=<name>): wie viele Schritte der
     * Komplettlösung bis dorthin nachgespielt werden, damit alle nötigen Gegenstände und Merkmale da sind.
     */
    const SPRUENGE = {
        buero: 0, marktstrasse: 5, karte: 6, rathaus: 7, rathausflur: 8, archiv: 8, ems: 15, boot: 18, radbruecke: 20, baustelle: 21,
        schule: 32, schulflur: 35, krypta: 36, tempel: 42, gang: 51, finale: 53
    };
    function sprungZustand(name) {
        if (!Object.prototype.hasOwnProperty.call(SPRUENGE, name)) { return null; }
        const z = neuerZustand();
        for (const s of KOMPLETTLOESUNG.slice(0, SPRUENGE[name])) { schritt(z, s); }
        return z;
    }

    const api = {
        VERSION, FIGUREN, VORSTELLUNG, EINLEITUNG, GEGENSTAENDE, ORTE, SZENEN, SIEGEL, QUIZ, KOMPLETTLOESUNG, RICHTIGE_KOMBINATION,
        ALTERNATIVE_FAKTEN, BOOT_X, PLATZ, neuerZustand, sichtbareHotspots, aktion, reisen, einstieg, platteVerlassen, figurWechseln, dialogInhalt, dialogWahl, minispielEnde, anrufen, naechsterSchritt,
        bootNeu, bootSchritt, bootAutopilot, kugelNeu, kugelSchritt, speichern, laden, speicherplaetze, schritt, SPRUENGE, sprungZustand,
        BRUECKE, PLAUDEREI, STANDARD
    };
    wurzel.GralSpiel = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof globalThis !== 'undefined' ? globalThis : this);
