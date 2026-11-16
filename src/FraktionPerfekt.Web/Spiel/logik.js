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
        tagesordnung: { name: 'Mustertagesordnung', text: '„TOP 1: Gründung eines Arbeitskreises. TOP 2: Verschiedenes.“' },
        geschaeftsordnung: { name: 'Geschäftsordnung (Taschenbuch)', text: 'Die Geschäftsordnung des Rates. Eselsohren auf jeder Seite über Anträge zur Geschäftsordnung.' },
        taschenlampe: { name: 'Taschenlampe', text: 'Eine Werbe-Taschenlampe: „Dicht ist Pflicht“.' },
        paddel: { name: 'Paddel', text: 'Ein Paddel. Vergesellschaftet, aber funktionstüchtig.' },
        siegel_legislative: { name: 'Siegel der Beschlüsse', text: 'Ein Siegel mit einem aufgeschlagenen Buch: das, was beschließt.' },
        siegel_exekutive: { name: 'Siegel der Ausführung', text: 'Ein Siegel mit einem Schlüsselbund: das, was ausführt.' },
        siegel_judikative: { name: 'Siegel des Rechts', text: 'Ein Siegel mit einer Waage: das, was richtet.' },
        tempelschluessel: { name: 'Tempelschlüssel', text: 'Ein schwerer Schlüssel. In den Bart ist „Art. 20“ eingraviert.' }
    };

    // Orte auf der Reisekarte (x/y in Prozent der Karte)
    const ORTE = {
        buero: { name: 'Zukunftsschmiede', x: 51.8, y: 27.9 },
        archiv: { name: 'Rathausarchiv', x: 47.2, y: 35 },
        ems: { name: 'Ems-Ufer', x: 35.3, y: 29.9 },
        baustelle: { name: 'Baustelle Hallenbad', x: 40.0, y: 43.0 },
        schule: { name: 'Martin-Luther-Grundschule', x: 47.6, y: 24.2 }
    };

    const SIEGEL = ['siegel_legislative', 'siegel_exekutive', 'siegel_judikative'];

    function neuerZustand() {
        return {
            version: VERSION,
            szene: 'buero',
            figur: 'joe',
            inventar: [],
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
    const geben = (z, g) => { if (!hat(z, g)) { z.inventar.push(g); } };
    const nehmen = (z, g) => { const i = z.inventar.indexOf(g); if (i >= 0) { z.inventar.splice(i, 1); } };
    const ortFrei = (z, o) => { if (z.orte.indexOf(o) < 0) { z.orte.push(o); } };
    const punkte = (z, flag, n) => { if (!z.flags['p_' + flag]) { z.flags['p_' + flag] = true; z.punkte += n || 10; } };
    const istJoe = (z) => z.figur === 'joe';
    const name = (z) => FIGUREN[z.figur].name;

    // Antwort einer Aktion: Texte, optional Szenenwechsel, Dialog oder Minispiel
    const antwort = (text, extra) => Object.assign({ text: Array.isArray(text) ? text : [text] }, extra || {});

    // ---------- Szenen ----------
    // Jeder Hotspot: id, name, x/y/b/h (Prozent der Szene), form (Darstellung), sichtbar(z), Handler je Aktion.
    // Aktionen: ansehen, benutzen, reden, f1, f2 (Fähigkeiten der aktiven Figur), gegenstand (mit z.B. { tagesordnung: fn }).
    const SZENEN = {
        buero: {
            titel: 'Zukunftsschmiede, Marktstraße',
            stimmung: 'buero',
            einstieg: 'Die Zukunftsschmiede in der Marktstraße, Montagabend nach der Fraktionssitzung. Auf dem Tisch liegt ein Umschlag ohne Absender.',
            hotspots: [
                {
                    id: 'brief', name: 'Umschlag', x: 39.8, y: 58.3, b: 10.2, h: 8.3, form: 'brief',
                    sichtbar: (z) => !hat(z, 'brief'),
                    ansehen: () => antwort('Ein Umschlag, adressiert an „die Fraktion, die noch Briefe öffnet“.'),
                    benutzen: (z) => {
                        geben(z, 'brief'); z.flags.briefGelesen = true; ortFrei(z, 'archiv'); punkte(z, 'brief');
                        return antwort([GEGENSTAENDE.brief.text, `${name(z)}: „Die Räte von 1949 – das Rathausarchiv!“`, '(Neuer Ort auf der Karte: Rathausarchiv)']);
                    }
                },
                {
                    id: 'fax', name: 'Faxgerät des Arbeitskreises Digitalisierung', x: 68.8, y: 44.4, b: 14.8, h: 25, form: 'fax',
                    ansehen: () => antwort('Das Faxgerät des Arbeitskreises Digitalisierung. Es ist das modernste Gerät im Raum. Daneben ein Zettel: „WLAN-Passwort: siehe Fax“.'),
                    benutzen: (z) => {
                        if (hat(z, 'stadtplan') || z.flags.stadtplanWeg) { return antwort('Das Fax rattert zufrieden. Mehr kommt nicht.'); }
                        if (!istJoe(z)) { return antwort('Chani sucht vergeblich nach einem Touchscreen. „Wie wischt man hier?“ – Das ist eher Joes Gerät.'); }
                        geben(z, 'stadtplan'); punkte(z, 'fax');
                        return antwort(['Joe tippt eine Nummer aus dem Gedächtnis – er faxt seit 1998 Bestellungen für Dichtstoffe.', 'Das Stadtarchiv faxt zurück: ein Stadtplan von 1949. Ein Fluss ist rot eingekreist.']);
                    },
                    f1: (z) => antwort(istJoe(z) ? 'Joe dichtet den Papiereinzug ab. Das Fax ist jetzt wasserdicht. Und sonst nichts.' : 'Chani: „Ein Gerät dieser Epoche gehört eigentlich ins Museum.“')
                },
                {
                    id: 'regal', name: 'Bücherregal', x: 12.1, y: 27.8, b: 12.1, h: 41.7, form: 'regal',
                    ansehen: () => antwort('Gesammelte Haushaltspläne, drei Ausgaben der Geschäftsordnung und ein Ordner „Ideen 2015“ (leer).'),
                    benutzen: (z) => {
                        if (hat(z, 'geschaeftsordnung')) { return antwort('Der Ordner „Ideen 2015“ ist immer noch leer.'); }
                        geben(z, 'geschaeftsordnung');
                        return antwort('Du nimmst die Geschäftsordnung als Taschenbuch mit. Man weiß nie.');
                    },
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Die Fuge hinter dem Regal ist undicht. Später.“' : 'Chani deutet die Buchrücken: Die Haushaltspläne werden jedes Jahr dicker, die Ideen-Ordner dünner.')
                },
                {
                    id: 'postfach', name: 'Postfach', x: 27.3, y: 36.1, b: 9.4, h: 16.7, form: 'kasten',
                    ansehen: () => antwort('Das Postfach der Fraktion. Ganz unten liegt etwas.'),
                    benutzen: (z) => {
                        if (hat(z, 'tagesordnung') || z.flags.agGegruendet) { return antwort('Nur noch Werbung für Wahlplakat-Kabelbinder.'); }
                        geben(z, 'tagesordnung');
                        return antwort('Eine Mustertagesordnung „Gründung eines Arbeitskreises“. Davon gibt es hier viele.');
                    }
                },
                {
                    id: 'garderobe', name: 'Garderobe', x: 85.9, y: 25, b: 8.2, h: 44.4, form: 'garderobe',
                    ansehen: () => antwort('Joes Hut, Joes Lederjacke und eine Taschenlampe mit Werbeaufdruck.'),
                    benutzen: (z) => {
                        if (hat(z, 'taschenlampe')) { return antwort('Der Hut sitzt. Die Jacke knarzt. Alles bereit.'); }
                        geben(z, 'taschenlampe');
                        return antwort('Ihr nehmt die Taschenlampe mit. Joe setzt den Hut auf. Chani filmt es für die Story.');
                    }
                },
                {
                    id: 'tuer', name: 'Tür zur Marktstraße', x: 51.6, y: 25, b: 10.9, h: 44.4, form: 'tuer',
                    ansehen: () => antwort('Die Ladentür zur Marktstraße.'),
                    benutzen: (z) => z.flags.briefGelesen ? antwort('Ab zur Karte.', { szene: 'karte' }) : antwort(`${name(z)}: „Erst mal den Umschlag lesen.“`)
                }
            ]
        },

        archiv: {
            titel: 'Rathausarchiv',
            stimmung: 'archiv',
            einstieg: 'Staub, Neonlicht, Regale bis zur Decke. Vor der Archivtür sitzt Genosse Christian von der SPD, ehrenamtlicher Archivpfleger.',
            hotspots: [
                {
                    id: 'christian', name: 'Genosse Christian (SPD)', x: 53.9, y: 48.6, b: 9.4, h: 45.8, form: 'person', farbe: '#c0392b',
                    lage: (z) => (z.flags.archivOffen ? { x: 86, y: 48.6, b: 9.4, h: 45.8 } : null),
                    ansehen: () => antwort('Christian. Strickjacke, Thermoskanne, Schlüsselbund. Hält Ordnung für eine Tugend und Arbeitskreise für eine Lebensform.'),
                    reden: (z) => {
                        if (z.flags.archivOffen) { return antwort('Christian: „Wenn ihr Hilfe braucht: Der Gewerkschaftsbus steht bereit. Solidarität hat einen Diesel.“'); }
                        if (z.flags.agGegruendet) {
                            // Die Tür ist TOP 2 – erst wenn die Sitzung dort angekommen ist, schließt Christian auf.
                            z.flags.archivOffen = true; punkte(z, 'top2');
                            return antwort(['Christian: „Dann rufe ich auf: TOP 2, Verschiedenes – Öffnung der Archivtür. Wortmeldungen? Keine. Einstimmig.“', 'Christian schließt die Tür auf. „Das ging ja zügig. Zwölf Minuten!“']);
                        }
                        return antwort(['Christian: „Ins Archiv? Gerne. Aber erst gründen wir einen Arbeitskreis. Ordentlich, mit Tagesordnung.“', `${name(z)}: „Eine Tagesordnung … hatten wir nicht so etwas im Büro?“`]);
                    },
                    gegenstand: {
                        tagesordnung: (z) => {
                            nehmen(z, 'tagesordnung'); z.flags.agGegruendet = true; punkte(z, 'ag');
                            return antwort(['Christian eröffnet die Sitzung. TOP 1: Gründung des Arbeitskreises „Archivöffnung“ – einstimmig.', 'Christian: „Die Tür steht unter TOP 2: Verschiedenes. Vorher schließe ich nicht auf – Ordnung muss sein. Sprecht mich an, dann rufe ich TOP 2 auf.“']);
                        }
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet Christian einen Rahmenvertrag über Fugendichtstoff an. Christian will das erst im Ortsverein beraten.'
                        : 'Chani startet eine Live-Story. Christian winkt und fragt, ob man das auch ausdrucken kann.')
                },
                {
                    id: 'archivtuer', name: 'Archivtür', x: 63.7, y: 13.9, b: 27.3, h: 58.3, form: 'tuer',
                    sichtbar: (z) => !z.flags.archivOffen,
                    ansehen: () => antwort('Eine Stahltür. Schild: „Zutritt nur nach Beschluss“.'),
                    benutzen: (z) => antwort(z.flags.agGegruendet
                        ? 'Christian: „Nicht so hastig! Die Tür ist TOP 2. Sprecht mich an, dann rufe ich ihn auf.“'
                        : 'Abgeschlossen. Christian hat den Schlüssel.')
                },
                {
                    id: 'wappen', name: 'Gemälde „Der Rat von 1949“', x: 63.7, y: 13.9, b: 22.7, h: 29.2, form: 'bild',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Ein Ölgemälde: der erste Rat nach dem Krieg, darüber das Stadtwappen. Unten eine verblasste Zeile.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Schöner Rahmen. Die Fuge oben links würde ich neu ziehen.“'); }
                        z.flags.hinweisProtokoll = true; punkte(z, 'wappen');
                        return antwort(['Chani tritt nah heran: „Firnis um 1950, Pinselstrich eines Amateurs – und eine Inschrift unter dem Wappen.“', '„Was beschlossen wird, ruht in Protokoll 47.“']);
                    }
                },
                {
                    id: 'protokolle', name: 'Regal mit Ratsprotokollen', x: 45.3, y: 19.4, b: 15.6, h: 50, form: 'regal',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Ratsprotokolle seit 1946, sauber nummeriert.'),
                    benutzen: (z) => {
                        if (hat(z, 'siegel_legislative')) { return antwort('Protokoll 47 ist jetzt eine sehr gründliche Lektüre ohne Siegel.'); }
                        if (!z.flags.hinweisProtokoll) { return antwort('Hunderte Protokolle. Ohne Hinweis sucht ihr bis zur nächsten Kommunalwahl.'); }
                        geben(z, 'siegel_legislative'); z.flags.gegnerArchiv = true; punkte(z, 'siegel1', 20);
                        return antwort(['In Protokoll 47 steckt ein Siegel: ein aufgeschlagenes Buch. Das erste Siegel!', 'Da poltert es an der Tür: Zwei Herren in zu engen Anzügen. „Der Gral gehört uns! Wir sind das Original!“']);
                    }
                },
                {
                    id: 'gegner', name: 'Hohlbein und Dröhn (AfD)', x: 11.7, y: 48.6, b: 17.2, h: 45.8, form: 'gegner',
                    sichtbar: (z) => z.flags.gegnerArchiv,
                    // Festgeklebt: verkleinert hinten an der Wand (perspektivisch weiter weg), der Ausgang ist frei
                    hinten: (z) => z.flags.gegnerArchivFest,
                    lage: (z) => (z.flags.gegnerArchivFest ? { x: 25, y: 46, b: 12, h: 30 } : null),
                    ansehen: (z) => antwort(z.flags.gegnerArchivFest
                        ? 'Hohlbein und Dröhn kleben hinten an der Wand fest – mitsamt Parkettdiele. Sie diskutieren, ob Dichtstoff „Systemkleber“ ist.'
                        : 'Kreissprecher Hohlbein und Beisitzer Dröhn. Großes Auftreten, kleines Leseverständnis. Sie versperren den Ausgang.'),
                    reden: (z) => (z.flags.gegnerArchivFest
                        ? antwort(['Hohlbein: „Das ist Freiheitsberaubung! Ich rufe meinen Anwalt!“', 'Dröhn: „Unser Anwalt klebt auch. Der war gestern hier.“'])
                        : antwort(['Hohlbein: „Hier steht ‚Ausgang‘. Das heißt bestimmt ‚Ausland‘. Da gehen wir nicht hin.“', 'Dröhn: „Wir bleiben in der Tür stehen, bis die Altparteien den Gral rausrücken!“'])),
                    f1: (z) => {
                        if (z.flags.gegnerArchivFest) { return antwort('Joe: „Hält. 24 Stunden Haftung, wie versprochen.“'); }
                        if (!istJoe(z)) {
                            return antwort('Chani deutet die Körpersprache: breitbeinig, Arme verschränkt, Blick auf das Handy – sie warten auf eine Sprachnachricht vom Bundesvorstand.');
                        }
                        z.flags.gegnerArchivFest = true; ortFrei(z, 'ems'); punkte(z, 'kartusche1');
                        return antwort(['Joe zieht die Kartusche wie einen Revolver. Zwei saubere Raupen Dichtstoff auf das Parkett – genau vor ihre Schuhspitzen.', 'Hohlbein tritt vor und klebt fest. Dröhn will ihn retten und klebt auch. „Elastisch, überstreichbar, 24 Stunden Haftung“, sagt Joe.', 'Christian schiebt die beiden samt loser Parkettdiele an die Rückwand. „Ordnung muss sein.“ Der Ausgang ist frei.', '(Neuer Ort auf der Karte: Ems-Ufer)']);
                    },
                    f2: (z) => antwort(z.flags.gegnerArchivFest
                        ? (istJoe(z) ? 'Joe bietet Lösungsmittel an. Zum Listenpreis. Sie lehnen ab – „aus Prinzip“.' : 'Chani geht live. Die beiden winken von der Rückwand. Mehr Bewegung ist gerade nicht drin.')
                        : istJoe(z)
                            ? 'Joe will ihnen Fugendichtstoff verkaufen. Sie fragen, ob der auch „aus Deutschland“ ist. Er ist es. Sie kaufen trotzdem nichts.'
                            : 'Chani geht live. Die beiden posieren und erklären zehn Minuten lang das Wort „Altparteien“. Sie stehen dabei aber weiter in der Tür.')
                },
                {
                    id: 'flurausgang', name: 'Tür zum Treppenhaus', x: 39.8, y: 16.7, b: 11.7, h: 55.6, form: 'tuer',
                    sichtbar: (z) => !z.flags.archivOffen,
                    ansehen: () => antwort('Die Tür zum Treppenhaus. Darüber hängt ein grünes Notausgangsschild.'),
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                },
                {
                    id: 'ausgang', name: 'Ausgang', x: 0.8, y: 25, b: 10.2, h: 47.2, form: 'tuer',
                    sichtbar: (z) => z.flags.archivOffen,
                    ansehen: () => antwort('Die Tür zurück in den Flur.'),
                    benutzen: (z) => (z.flags.gegnerArchiv && !z.flags.gegnerArchivFest)
                        ? antwort('Hohlbein und Dröhn stehen breitbeinig im Ausgang.')
                        : antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        ems: {
            titel: 'Ems-Ufer an der alten Brücke',
            stimmung: 'ems',
            einstieg: 'Die Ems glitzert. Am Steg liegt ein Ruderboot mit einem selbstgemalten Schild: „Volkseigentum“. Daneben Genossin Joanna von der Linken.',
            hotspots: [
                {
                    id: 'joanna', name: 'Genossin Joanna (Linke)', x: 12, y: 48.6, b: 9.4, h: 45, form: 'person', farbe: '#8e44ad',
                    ansehen: () => antwort('Joanna. Jutebeutel, Klemmbrett, Umverteilungsliste.'),
                    reden: (z) => {
                        if (hat(z, 'paddel') || z.flags.bootGewonnen) { return antwort('Joanna: „Viel Erfolg! Und wenn ihr den Gral findet: Der gehört allen.“'); }
                        return antwort(['Joanna: „Das Boot habe ich vergesellschaftet. Das Paddel auch. Ihr kriegt es – aber ich verteile um.“', '„Gebt mir etwas aus eurem Inventar, dann gebe ich euch das Paddel. Gerecht ist gerecht.“']);
                    },
                    gegenstand: {
                        stadtplan: (z) => {
                            nehmen(z, 'stadtplan'); z.flags.stadtplanWeg = true; geben(z, 'paddel'); punkte(z, 'joanna');
                            return antwort('Joanna nimmt den Stadtplan („Kartenmaterial gehört dem Volk“) und gibt euch das Paddel. Sie heftet den Plan an den Bürgerbus-Fahrplan.');
                        },
                        geschaeftsordnung: () => antwort('Joanna: „Die Geschäftsordnung? Die bleibt bei euch. Die braucht ihr noch, glaubt mir.“'),
                        taschenlampe: () => antwort('Joanna: „Die Lampe braucht ihr selber. Ich verteile nicht von arm zu arm.“'),
                        brief: () => antwort('Joanna: „Persönliche Post verteile ich nicht um. Ich habe Grundsätze.“')
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet ihr Dichtstoff zum Einkaufspreis. Joanna: „Gibt es den auch genossenschaftlich?“'
                        : 'Chani macht eine Story über das Volkseigentum-Boot. 2000 Aufrufe in drei Minuten.')
                },
                {
                    id: 'bruecke', name: 'Inschrift an der alten Brücke', x: 65, y: 18, b: 17, h: 7, form: 'inschrift',
                    ansehen: () => antwort('In den Brückenpfeiler ist etwas gemeißelt, halb von Moos bedeckt.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Die Fuge im Pfeiler ist seit 1949 nicht nachgearbeitet. Unglaublich.“'); }
                        z.flags.hinweisPfahl = true; punkte(z, 'bruecke');
                        return antwort(['Chani kratzt das Moos ab: „Frühe Nachkriegs-Antiqua, sehr selbstbewusst gesetzt.“', '„Was ausführt, ruht im dritten von sieben Pfählen, flussab.“']);
                    }
                },
                {
                    id: 'boot', name: 'Ruderboot „Volkseigentum“', x: 56.25, y: 64.6, b: 18, h: 9.7, form: 'boot',
                    sichtbar: (z) => !z.flags.bootGewonnen,
                    ansehen: () => antwort('Ein Ruderboot. Flussabwärts tuckert ein Motorboot – Hohlbein und Dröhn haben sich wohl losgeschnitten.'),
                    benutzen: (z) => hat(z, 'paddel')
                        ? antwort(['Ihr springt ins Boot. Hinter euch heult ein Motor auf: Hohlbein und Dröhn!', '(Ausweichen mit ↑/↓ oder oben/unten tippen. Drei Treffer, dann von vorn.)'], { minispiel: 'boot' })
                        : antwort('Ohne Paddel wird das eine sehr langsame Verfolgungsjagd.')
                },
                {
                    id: 'pfaehle', name: 'Sieben Pfähle im Wasser', x: 72.7, y: 56, b: 19.5, h: 14, form: 'pfaehle',
                    sichtbar: (z) => z.flags.bootGewonnen,
                    ansehen: () => antwort('Sieben alte Holzpfähle ragen aus dem Wasser.'),
                    benutzen: (z) => {
                        if (hat(z, 'siegel_exekutive')) { return antwort('Der dritte Pfahl ist jetzt einfach ein Pfahl.'); }
                        if (!z.flags.hinweisPfahl) { return antwort('Sieben Pfähle. Welcher? Vielleicht verrät die Inschrift an der Brücke mehr.'); }
                        geben(z, 'siegel_exekutive'); ortFrei(z, 'baustelle'); punkte(z, 'siegel2', 20);
                        return antwort(['Der dritte Pfahl flussab ist hohl. Darin: ein Siegel mit einem Schlüsselbund. Das zweite Siegel!', '(Neuer Ort auf der Karte: Baustelle Hallenbad)']);
                    }
                },
                {
                    id: 'zurkarte', name: 'Weg zur Straße', x: 90.6, y: 59.7, b: 9.4, h: 18, form: 'pfeil',
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        baustelle: {
            titel: 'Baustelle Hallenbad',
            stimmung: 'baustelle',
            einstieg: 'Das Hallenbad wird saniert: Bauzaun, Bagger, ein leeres Becken – und eine Grube, die langsam vollläuft. Hohlbein und Dröhn schrauben Wegweiser ab.',
            hotspots: [
                {
                    id: 'bauleiter', name: 'Bauleiterin Frau Kemper', x: 11.7, y: 48.6, b: 9.4, h: 45, form: 'person', farbe: '#f39c12',
                    ansehen: () => antwort('Frau Kemper, Bauleiterin. Helm, Klemmbrett, wenig Zeit.'),
                    reden: (z) => z.flags.helme ? antwort('Kemper: „Helme auf, dann dürft ihr runter. Und nichts anfassen, was teuer aussieht.“') : antwort('Kemper: „Ohne Helm kommt hier keiner in die Grube. Und Helme hab ich nur für Kunden.“'),
                    f2: (z) => {
                        if (!istJoe(z)) { return antwort('Chani filmt die Baustelle. Frau Kemper: „Bitte nicht das Becken filmen, das ist noch nicht dicht.“'); }
                        z.flags.helme = true; punkte(z, 'helme');
                        return antwort(['Joe: „Noch nicht dicht? Da hätte ich etwas für Sie.“ Zehn Minuten Fachgespräch über Fugen im Nassbereich.', 'Frau Kemper bestellt 40 Kartuschen – und reicht zwei Helme rüber. „Kunden dürfen in die Grube.“']);
                    }
                },
                {
                    id: 'leitung', name: 'Leckende Leitung', x: 43.75, y: 53.5, b: 15, h: 9, form: 'rohr',
                    sichtbar: (z) => !z.flags.grubeTrocken,
                    ansehen: () => antwort('Aus einer alten Leitung sprudelt Wasser in die Grube.'),
                    f1: (z) => {
                        if (!istJoe(z)) { return antwort('Chani: „Spätes 20. Jahrhundert, Gusseisen. Kunsthistorisch uninteressant, aber nass.“'); }
                        z.flags.grubeTrocken = true; punkte(z, 'leck');
                        return antwort(['Joe setzt die Kartusche an. Eine Raupe, glattgezogen mit dem Daumen. Das Leck ist dicht.', 'Die Pumpe saugt die Grube leer. Unten glänzt etwas.']);
                    }
                },
                {
                    id: 'grube', name: 'Baugrube', x: 39, y: 61, b: 23.4, h: 15.3, form: 'grube',
                    ansehen: (z) => antwort(z.flags.grubeTrocken ? 'Die Grube ist trocken. Unten liegt etwas Metallisches.' : 'Die Grube steht unter Wasser. Unten glänzt etwas.'),
                    benutzen: (z) => {
                        if (hat(z, 'siegel_judikative')) { return antwort('In der Grube liegt jetzt nur noch ein vergessener Zollstock.'); }
                        if (!z.flags.grubeTrocken) { return antwort('Zu nass. Erst das Leck stopfen.'); }
                        if (!z.flags.helme) { return antwort('Frau Kemper: „Ohne Helm nicht in die Grube!“'); }
                        geben(z, 'siegel_judikative'); punkte(z, 'siegel3', 20);
                        return antwort('Unter einer alten Beckenfliese: ein Siegel mit einer Waage. Das dritte Siegel!');
                    }
                },
                {
                    id: 'gegner', name: 'Hohlbein und Dröhn (AfD)', x: 68.75, y: 48.6, b: 17, h: 45, form: 'gegner',
                    sichtbar: (z) => !z.flags.gegnerBaustelleWeg,
                    ansehen: () => antwort('Hohlbein und Dröhn schrauben Wegweiser ab und stapeln sie vor dem Ausgang.'),
                    reden: () => antwort(['Dröhn: „Der Wegweiser nach Münster muss weg. Der zeigt ins Ausland!“', 'Hohlbein: „Wir remigrieren alle Schilder. Auch das hier: ‚Nordwalde‘. Klingt schon so nordisch, der darf bleiben.“']),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe überlegt, sie festzukleben. Aber auf Sand hält selbst der beste Dichtstoff nicht. Berufsehre.'
                        : 'Chani deutet: „Sie suchen nach Bestätigung, nicht nach dem Gral. Eine Kamera würde sie ablenken.“'),
                    f2: (z) => {
                        if (istJoe(z)) { return antwort('Joe will ihnen Dichtstoff verkaufen. „Ist da Gender drin?“, fragt Dröhn. Gespräch beendet.'); }
                        z.flags.gegnerBaustelleWeg = true; punkte(z, 'story');
                        return antwort(['Chani hebt das Handy: „Wir sind live! Wollen Sie den Menschen etwas sagen?“', 'Beide stürzen zur Kamera, drehen sich ins beste Licht – und laufen dabei rückwärts in den Bauwagen. Die Tür fällt zu. Von innen: „Lügenpresse!“ Chani: „Ich bin Kunsthistorikerin.“']);
                    }
                },
                {
                    id: 'zurkarte', name: 'Bauzaun-Tor', x: 88.3, y: 40.3, b: 8.6, h: 32, form: 'pfeil',
                    benutzen: (z) => {
                        if (!z.flags.gegnerBaustelleWeg) { return antwort('Vor dem Tor liegt ein Stapel abgeschraubter Wegweiser. Hohlbein bewacht ihn.'); }
                        if (hat(z, 'siegel_judikative')) { ortFrei(z, 'schule'); }
                        return antwort(hat(z, 'siegel_judikative') ? ['Drei Siegel! Laut Brief führt der Weg zum Tempel unter die Martin-Luther-Grundschule.', '(Neuer Ort auf der Karte: Martin-Luther-Grundschule)'] : 'Zur Karte.', { szene: 'karte' });
                    }
                }
            ]
        },

        schule: {
            titel: 'Hof der Martin-Luther-Grundschule',
            stimmung: 'schule',
            einstieg: 'Ein leerer Schulhof. Im Haupteingang brennt Licht – Elternabend. Davor: Lore von den Grünen, mit Lastenrad und Gutachten.',
            hotspots: [
                {
                    id: 'lore', name: 'Lore (Grüne)', x: 19.5, y: 48.6, b: 9.4, h: 45.8, form: 'person', farbe: '#27ae60',
                    ansehen: () => antwort('Lore. Fahrradhelm, Lastenrad, ein Gutachten unter dem Arm, dick wie ein Haushaltsplan.'),
                    reden: (z) => {
                        if (z.flags.abend && !z.flags.lastenrad) {
                            z.flags.lastenrad = true; punkte(z, 'lastenrad');
                            return antwort(['Lore: „Die Fledermäuse sind ausgeflogen. Geht ruhig rein.“', '„Und nehmt das Lastenrad mit. Für den Rückweg – man weiß ja nie, was in so einem Tempel herumrollt.“']);
                        }
                        if (z.flags.lastenrad) { return antwort('Lore: „Und bringt mir das Lastenrad mit vollem Akku zurück!“'); }
                        return antwort(['Lore: „Der Kellergang ist gesperrt. Fledermausgutachten! Hier überwintern Zwergfledermäuse.“', '„Erst wenn sie zur Nacht ausfliegen, ab halb neun, darf jemand durch. Steht auf Seite 212.“']);
                    },
                    f2: (z) => antwort(istJoe(z)
                        ? 'Joe bietet an, den Gang fledermausfreundlich abzudichten. Lore: „Dafür brauchen wir ein Gutachten.“'
                        : 'Chani macht eine Story über Zwergfledermäuse. Lore ist begeistert und teilt sie sofort.')
                },
                {
                    id: 'uhr', name: 'Schulhofuhr', x: 60.9, y: 15.3, b: 8.6, h: 15.3, form: 'uhr',
                    ansehen: (z) => antwort(z.flags.abend ? 'Die Uhr zeigt 20:35.' : 'Die Uhr zeigt 18:10.'),
                    benutzen: (z) => {
                        if (z.flags.abend) { return antwort('Es ist schon Abend.'); }
                        z.flags.abend = true; punkte(z, 'warten');
                        return antwort(['Ihr setzt euch auf die Tischtennisplatte und wartet. Joe erzählt von Silikon, Chani von Caravaggio.', '20:30 Uhr: Eine Wolke kleiner Fledermäuse schwirrt aus dem Kellergang in die Nacht.']);
                    }
                },
                {
                    id: 'eingang', name: 'Haupteingang', x: 68.75, y: 36, b: 15.6, h: 36, form: 'tuer',
                    ansehen: () => antwort('Der Haupteingang der Grundschule. Die Tür steht offen, drinnen ist Elternabend.'),
                    benutzen: () => antwort('Ihr geht hinein.', { szene: 'schulflur' })
                },
                {
                    id: 'zurkarte', name: 'Schultor', x: 1.6, y: 41.7, b: 10.5, h: 30.6, form: 'pfeil',
                    benutzen: () => antwort('Zur Karte.', { szene: 'karte' })
                }
            ]
        },

        schulflur: {
            titel: 'Flur der Martin-Luther-Grundschule',
            stimmung: 'schule',
            einstieg: 'Ein langer Flur: Kinderbilder, Turnbeutel, drei Klassenzimmer. Am Ende die Kellertür – mit Absperrband.',
            hotspots: [
                {
                    id: 'klasse1a', name: 'Klasse 1a', x: 14, y: 30.6, b: 9.4, h: 41.7, form: 'tuer',
                    ansehen: () => antwort('Klasse 1a. An der Tür hängt ein Zettel: „Bitte nicht stören – Mathearbeit wird korrigiert.“'),
                    benutzen: () => antwort(['Abgeschlossen. Von drinnen ruft eine Lehrerin: „Ich korrigiere! Mit Rotstift!“', 'Ihr tretet einen respektvollen Schritt zurück.'])
                },
                {
                    id: 'klasse2b', name: 'Klasse 2b', x: 34.4, y: 30.6, b: 9.4, h: 41.7, form: 'tuer',
                    ansehen: () => antwort('Klasse 2b. Hinter der Tür: Stimmengewirr.'),
                    benutzen: () => antwort(['Ihr öffnet die Tür einen Spalt. Drinnen tagt seit 19 Uhr der Elternabend – Thema: die Farbe der Turnbeutel.', 'Eine Mutter: „Wollen Sie sich als Klassenpflegschaft aufstellen lassen?“ Ihr schließt die Tür sehr leise.'])
                },
                {
                    id: 'klasse4c', name: 'Klasse 4c', x: 54.7, y: 30.6, b: 9.4, h: 41.7, form: 'tuer',
                    ansehen: () => antwort('Klasse 4c. Ein Schild: „Klassenrat tagt“.'),
                    benutzen: (z) => antwort(istJoe(z)
                        ? 'Joe klopft. Ein Kind öffnet: „Haben Sie einen Antrag? Ohne Antrag kein Rederecht.“ Die Tür geht wieder zu.'
                        : 'Chani klopft. Ein Kind öffnet: „Wir stimmen gerade über die Hofpause ab. Gäste nur mit Tagesordnungspunkt.“ Die Tür geht wieder zu.')
                },
                {
                    id: 'kellertuer', name: 'Kellertür', x: 75.8, y: 27.8, b: 11.7, h: 44.4, form: 'tuer',
                    ansehen: () => antwort('Eine Stahltür, Aufschrift „KELLER“. Absperrband und ein Zettel: „Fledermausquartier – siehe Gutachten, Seite 212“.'),
                    benutzen: (z) => {
                        if (!z.flags.abend) { return antwort('Auf dem Zettel steht: „Zutritt erst nach dem Ausflug der Fledermäuse (ab 20:30 Uhr)“. Lore hat ihn unterschrieben.'); }
                        if (!hat(z, 'taschenlampe')) { return antwort('Hinter der Tür ist es stockdunkel. Ohne Licht geht hier niemand runter.'); }
                        return antwort('Ihr knipst die Taschenlampe an und steigt die Kellertreppe hinab …', { szene: 'krypta' });
                    }
                },
                {
                    id: 'zumhof', name: 'Ausgang zum Hof', x: 0, y: 27.8, b: 8.6, h: 44.4, form: 'pfeil',
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
                    id: 'fresko', name: 'Wandfresko', x: 9.4, y: 12.5, b: 28, h: 33, form: 'bild',
                    ansehen: () => antwort('Ein Fresko mit drei Bildern übereinander. Sehr verblasst.'),
                    f1: (z) => {
                        if (istJoe(z)) { return antwort('Joe: „Kalkputz. Wer da Silikon drauf macht, hat nichts verstanden.“'); }
                        z.flags.hinweisKrypta = true; punkte(z, 'fresko');
                        return antwort(['Chani leuchtet das Fresko ab: „Unten eine Urne, darüber ein Buch, ganz oben eine Waage.“', '„Und die Inschrift: Von unten nach oben wird der Staat gebaut.“']);
                    }
                },
                {
                    id: 'schloss', name: 'Symbolschloss', x: 43.75, y: 38.9, b: 13.3, h: 16.7, form: 'schloss',
                    sichtbar: (z) => !hat(z, 'tempelschluessel') && !z.flags.tempelOffen,
                    ansehen: () => antwort('Drei Drehscheiben mit Symbolen: Waage, Buch, Urne.'),
                    benutzen: (z) => {
                        z.kombination = [];
                        return antwort('Welches Symbol kommt zuerst?', { dialog: 'schloss' });
                    }
                },
                {
                    id: 'tor', name: 'Tor mit drei Vertiefungen', x: 68.4, y: 15.3, b: 21, h: 57, form: 'tor',
                    ansehen: () => antwort('Ein steinernes Tor. Drei runde Vertiefungen – genau so groß wie die Siegel. Und ein Schlüsselloch.'),
                    benutzen: (z) => {
                        const fehlend = SIEGEL.filter((s) => !hat(z, s));
                        if (fehlend.length > 0) { return antwort(`Es fehlen noch ${fehlend.length} Siegel.`); }
                        if (!hat(z, 'tempelschluessel')) { return antwort('Die Siegel passen – aber das Schlüsselloch bleibt leer.'); }
                        z.flags.tempelOffen = true; punkte(z, 'tor', 20);
                        return antwort(['Drei Siegel rasten ein. Der Schlüssel dreht sich mit einem tiefen „Klonk“.', 'Das Tor schwingt auf: der Tempel des Grundgesetzes.'], { szene: 'tempel' });
                    }
                },
                {
                    id: 'zurueck', name: 'Treppe nach oben', x: 0, y: 55.6, b: 10, h: 21, form: 'pfeil',
                    benutzen: () => antwort('Zurück in den Schulflur.', { szene: 'schulflur' })
                }
            ]
        },

        tempel: {
            titel: 'Tempel des Grundgesetzes',
            stimmung: 'tempel',
            einstieg: 'Eine Halle aus Sandstein, Säulen, Fackeln. Vor euch ein Fallgitter, zwei Bodenplatten und eine Inschrift.',
            hotspots: [
                {
                    id: 'inschrift', name: 'Inschrift über dem Fallgitter', x: 33.2, y: 6.9, b: 32, h: 11.1, form: 'inschrift',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('„FRAKTIONSZWANG“ steht über dem Gitter. Darunter kleiner Text.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe: „Steht da was von Dichtstoff? Nein? Dann soll Chani das lesen.“'
                        : ['Chani liest: „Wer im Gleichschritt kommt, bleibt draußen. Das Gewissen öffnet das Tor.“', '„Wir müssen auf verschiedenen Platten stehen – jede für sich.“'])
                },
                {
                    id: 'platte_links', name: 'Linke Bodenplatte', x: 21.9, y: 72.2, b: 14, h: 6.25, form: 'platte',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    benutzen: (z) => platteBetreten(z, 'links')
                },
                {
                    id: 'platte_rechts', name: 'Rechte Bodenplatte', x: 62.1, y: 72.2, b: 14, h: 6.25, form: 'platte',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    benutzen: (z) => platteBetreten(z, 'rechts')
                },
                {
                    id: 'gitter', name: 'Fallgitter', x: 38.3, y: 18, b: 21.9, h: 54, form: 'gitter',
                    sichtbar: (z) => !z.flags.gitterOffen,
                    ansehen: () => antwort('Ein schweres Fallgitter. Es bewegt sich nur, wenn jemand nicht mitmarschiert.'),
                    benutzen: () => antwort('Rütteln hilft nicht. Das Gitter hat Fraktionsdisziplin.'),
                    f1: (z) => antwort(istJoe(z) ? 'Joe: „Ein Gitter abdichten? Wozu?“' : 'Chani: „Neogotisch. Und sehr symbolisch.“')
                },
                {
                    id: 'tafel', name: 'Tafel der Geschäftsordnung', x: 19.1, y: 22.9, b: 15.6, h: 30.6, form: 'tafel',
                    sichtbar: (z) => z.flags.gitterOffen && !z.flags.goGeloest,
                    ansehen: () => antwort('Eine Steintafel mit Fragen zur Geschäftsordnung. Wer alle richtig beantwortet, darf weiter.'),
                    benutzen: (z) => { z.quiz = 0; return antwort('Die Tafel leuchtet auf.', { dialog: 'quiz' }); }
                },
                {
                    id: 'abgrund', name: 'Abgrund', x: 40.6, y: 72.2, b: 37.5, h: 27.8, form: 'abgrund',
                    sichtbar: (z) => z.flags.goGeloest && !z.flags.brueckeDa,
                    ansehen: () => antwort('Ein Abgrund. Auf der anderen Seite ein Pult mit der Aufschrift: „Die Brücke erscheint für den Antrag, der allen anderen vorgeht.“'),
                    benutzen: () => antwort('Ihr tretet ans Pult. Welchen Antrag stellt ihr?', { dialog: 'bruecke' })
                },
                {
                    id: 'gang', name: 'Langer abschüssiger Gang', x: 75.4, y: 27.8, b: 15.6, h: 44.4, form: 'tuer',
                    sichtbar: (z) => z.flags.brueckeDa && !z.flags.kugelGeschafft,
                    ansehen: () => antwort('Ein langer Gang, leicht abschüssig. Am oberen Ende: eine riesige steinerne Wahlkugel. Sie wackelt.'),
                    benutzen: (z) => antwort(['Ein Knirschen. Die Wahlkugel löst sich und rollt los!', z.flags.lastenrad
                        ? '(Lores Lastenrad! Tippen oder Leertaste/→ für Tempo.)'
                        : '(Rennen! Tippen oder Leertaste/→ für Tempo.)'], { minispiel: 'kugel' })
                },
                {
                    id: 'pokal', name: 'Goldener Pokal auf einem Podest', x: 29.3, y: 36.1, b: 11.7, h: 36, form: 'pokal',
                    sichtbar: (z) => z.flags.kugelGeschafft && !z.ende,
                    ansehen: () => antwort('Ein goldener Pokal, mit Edelsteinen. Hohlbein und Dröhn stürzen aus einem Seitengang darauf zu: „Der Gral! Unserer!“'),
                    benutzen: () => antwort('Hohlbein reißt euch den Pokal aus der Hand. „Aus Gold! Wie früher!“ Auf der Unterseite steht: „Bezirksmeister Kegeln 1987“.'),
                    f1: (z) => antwort(istJoe(z)
                        ? 'Joe klopft daran: „Hohl. Wie manche Argumente.“'
                        : 'Chani: „Pressgold, Fabrikware, um 1987. Das ist nicht der Gral. Der Gral ist das unscheinbare Ding daneben.“')
                },
                {
                    id: 'urne', name: 'Schlichte Holzkiste', x: 58.6, y: 44.4, b: 13.3, h: 27.8, form: 'urne',
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

    function platteBetreten(z, seite) {
        const andere = seite === 'links' ? 'rechts' : 'links';
        if (z.platten[andere] === z.figur) { z.platten[andere] = null; }
        z.platten[seite] = z.figur;
        const texte = [`${name(z)} stellt sich auf die ${seite === 'links' ? 'linke' : 'rechte'} Platte.`];
        if (z.platten.links && z.platten.rechts && z.platten.links !== z.platten.rechts) {
            z.flags.gitterOffen = true; punkte(z, 'gitter', 20);
            texte.push('Jeder auf seiner Platte, jeder nach seinem Gewissen: Das Fallgitter hebt sich knirschend.');
            texte.push('Dahinter: eine Steintafel und ein Abgrund.');
        } else {
            texte.push('Das Gitter bleibt unten. (Tipp: Figur wechseln.)');
        }
        return antwort(texte);
    }

    function finale(z) {
        z.ende = true; z.flags.ende = true; punkte(z, 'finale', 50);
        const hilfe = [];
        hilfe.push(z.flags.agGegruendet ? 'Genosse Christian fährt mit dem Gewerkschaftsbus vor: „Solidarität!“' : 'Ein Bus hupt draußen.');
        hilfe.push(z.flags.lastenrad ? 'Lore schiebt das Lastenrad heran: „Die Urne fährt im Lastenkorb – angeschnallt.“' : '');
        hilfe.push(z.flags.stadtplanWeg ? 'Joanna hat den Tragegurt organisiert: „Gemeinsam tragen. Alle.“' : '');
        return antwort([
            'Hohlbein jubelt über seinen Kegelpokal. Dröhn ruft: „Den Rest remigrieren wir auch!“ – und versucht, die Wahlurne allein hochzuheben.',
            'Sie bewegt sich keinen Millimeter. Eine Inschrift am Boden: „Ich werde nur getragen, wenn viele mitanfassen.“',
            'Joe hebt eine Seite an, Chani die andere. Und plötzlich ist die Urne ganz leicht.',
            ...hilfe.filter((h) => h),
            'Hohlbein und Dröhn folgen ihrer „alternativen Faktenkarte“ zum Ausgang – und landen in der Fledermaus-Sackgasse. Lore ruft: „Seite 212!“',
            'Der Gral der Demokratie ist kein Gold. Er ist eine Kiste mit einem Schlitz und ein Bleistift an einer Schnur. Gold wäre einfacher gewesen. Aber nicht so wertvoll.',
            'ENDE – Danke fürs Spielen!',
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
            geben(z, 'tempelschluessel'); punkte(z, 'schloss', 20);
            return antwort(['Urne, Buch, Waage – von unten nach oben. Das Schloss springt auf.', 'Darin: ein schwerer Schlüssel mit der Gravur „Art. 20“.']);
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
            return antwort(['Alle Fragen richtig. Die Tafel gleitet zur Seite und gibt den Blick auf einen Abgrund frei.']);
        }
        if (art === 'bruecke') {
            if (Number(wahl) !== BRUECKE.richtig) {
                return antwort(['Nichts geschieht. Aus der Tiefe ruft ein Echo: „… zur Sache! … zur Sache!“']);
            }
            z.flags.brueckeDa = true; punkte(z, 'bruecke2', 20);
            return antwort(['„Antrag zur Geschäftsordnung!“ ruft Joe. Ein Steg schiebt sich über den Abgrund – Anträge zur Geschäftsordnung gehen eben vor.', 'Am anderen Ende: ein langer, abschüssiger Gang.']);
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
        return antwort(SZENEN[ziel].einstieg, { szene: ziel });
    }

    function figurWechseln(z) {
        z.figur = z.figur === 'joe' ? 'chani' : 'joe';
        return antwort(`Jetzt spielt ${name(z)}. ${FIGUREN[z.figur].faehigkeiten.join(' und ')} stehen bereit.`);
    }

    function minispielEnde(z, art, gewonnen) {
        if (!gewonnen) {
            return antwort(art === 'boot' ? 'Gekentert! Nasse Füße, aber nichts passiert. Noch einmal.' : 'Die Kugel holt euch ein – ihr springt in eine Nische. Zurück zum Anfang des Gangs.');
        }
        if (art === 'boot') {
            z.flags.bootGewonnen = true; punkte(z, 'boot', 30);
            return antwort(['Geschafft! Hohlbein und Dröhn folgen ihrer „alternativen Faktenkarte“ in einen Altarm. Ihr Motor blubbert im Schilf.', 'Ihr legt an den sieben Pfählen an.']);
        }
        z.flags.kugelGeschafft = true; punkte(z, 'kugel', 30);
        return antwort(['Ihr hechtet in die Halle – die Kugel donnert vorbei und bleibt im Torbogen stecken.', 'Vor euch: ein goldener Pokal auf einem Podest. Und daneben eine schlichte Holzkiste.']);
    }

    // ---------- Minispiele (deterministisch, mit eigenem Zufall) ----------
    function zufall(seed) {
        let s = seed >>> 0 || 1;
        return () => { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; };
    }

    /** Bootsverfolgung: drei Spuren, Hindernisse kommen von rechts; 35 Sekunden überstehen, drei Treffer erlaubt. */
    function bootNeu(seed) {
        return { art: 'boot', zeit: 0, dauer: 35, leben: 3, spur: 1, hindernisse: [], naechstes: 1.2, rnd: zufall(seed || 7), status: 'laeuft', treffer: 0 };
    }

    const BOOT_ARTEN = ['Treibholz', 'Entenfamilie', 'Schild „Alternative Route“', 'Plakat „Grenzen dicht!“ (treibt)'];

    function bootSchritt(s, dt, eingabe) {
        if (s.status !== 'laeuft') { return s; }
        if (eingabe === 'hoch') { s.spur = Math.max(0, s.spur - 1); }
        if (eingabe === 'runter') { s.spur = Math.min(2, s.spur + 1); }
        s.zeit += dt;
        const tempo = 0.45 + Math.min(0.4, s.zeit / 60);
        s.naechstes -= dt;
        if (s.naechstes <= 0) {
            const spur = Math.floor(s.rnd() * 3);
            s.hindernisse.push({ x: 1.05, spur, art: BOOT_ARTEN[Math.floor(s.rnd() * BOOT_ARTEN.length)] });
            s.naechstes = 0.9 + s.rnd() * 0.7 - Math.min(0.4, s.zeit / 80);
        }
        for (const h of s.hindernisse) { h.x -= tempo * dt; }
        const getroffen = s.hindernisse.filter((h) => h.x < 0.2 && h.x > 0.08 && h.spur === s.spur);
        if (getroffen.length > 0) {
            s.leben -= getroffen.length; s.treffer += getroffen.length;
            s.hindernisse = s.hindernisse.filter((h) => getroffen.indexOf(h) < 0);
        }
        s.hindernisse = s.hindernisse.filter((h) => h.x > -0.1);
        if (s.leben <= 0) { s.status = 'verloren'; } else if (s.zeit >= s.dauer) { s.status = 'gewonnen'; }
        return s;
    }

    /** Ausweich-Hilfe für Tests und Demo: freie Spur vor dem nächsten Hindernis wählen. */
    function bootAutopilot(s) {
        const gefahr = s.hindernisse.filter((h) => h.x < 0.45 && h.x > 0.08).map((h) => h.spur);
        if (gefahr.indexOf(s.spur) < 0) { return null; }
        for (const kandidat of [s.spur - 1, s.spur + 1]) {
            if (kandidat >= 0 && kandidat <= 2 && gefahr.indexOf(kandidat) < 0) { return kandidat < s.spur ? 'hoch' : 'runter'; }
        }
        return s.spur > 0 ? 'hoch' : 'runter';
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
        if (Array.isArray(d.inventar)) { z.inventar = d.inventar.filter((g) => typeof g === 'string' && GEGENSTAENDE[g]); }
        if (d.flags && typeof d.flags === 'object') {
            for (const k of Object.keys(d.flags)) { if (/^[a-zA-Z_0-9]+$/.test(k) && d.flags[k] === true) { z.flags[k] = true; } }
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
        ['aktion', 'gegenstand', 'christian', 'tagesordnung'],
        ['aktion', 'reden', 'christian'],
        ['figur'],
        ['aktion', 'f1', 'wappen'],
        ['aktion', 'benutzen', 'protokolle'],
        ['figur'],
        ['aktion', 'f1', 'gegner'],
        ['aktion', 'benutzen', 'ausgang'],
        ['reisen', 'ems'],
        ['aktion', 'gegenstand', 'joanna', 'stadtplan'],
        ['figur'],
        ['aktion', 'f1', 'bruecke'],
        ['aktion', 'benutzen', 'boot'],
        ['minispiel', 'boot', true],
        ['aktion', 'benutzen', 'pfaehle'],
        ['aktion', 'benutzen', 'zurkarte'],
        ['reisen', 'baustelle'],
        ['figur'],
        ['aktion', 'f1', 'leitung'],
        ['aktion', 'f2', 'bauleiter'],
        ['aktion', 'benutzen', 'grube'],
        ['figur'],
        ['aktion', 'f2', 'gegner'],
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
        ['aktion', 'benutzen', 'platte_links'],
        ['figur'],
        ['aktion', 'benutzen', 'platte_rechts'],
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

    const api = {
        VERSION, FIGUREN, GEGENSTAENDE, ORTE, SZENEN, SIEGEL, QUIZ, KOMPLETTLOESUNG, RICHTIGE_KOMBINATION,
        neuerZustand, sichtbareHotspots, aktion, reisen, figurWechseln, dialogInhalt, dialogWahl, minispielEnde,
        bootNeu, bootSchritt, bootAutopilot, kugelNeu, kugelSchritt, speichern, laden, speicherplaetze, schritt
    };
    wurzel.GralSpiel = api;
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof globalThis !== 'undefined' ? globalThis : this);
