// Markdown-Editor (TOAST UI Editor 3.2.2, lokal unter lib/toastui-editor) für Wiki-Seiten.
// Bilder: Einfügen aus der Zwischenablage, Ziehen oder Bild-Knopf → im Browser auf höchstens 2000 px verkleinern
// (Fotos werden dabei neu kodiert, EXIF/GPS fällt weg) → POST dateien/bild → Markdown ![Originalname](dateien/bild/{guid}).
(function () {
    'use strict';
    const MAX_KANTE = 2000;

    function warten(fn, ms) { let t = null; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; }

    /** [[Wiki-Links]] nach dem WYSIWYG-Modus wieder unmaskiert zurückgeben. */
    function bereinigt(md) { return (md || '').replace(/\\\[\\\[/g, '[[').replace(/\\\]\\\]/g, ']]').replace(/\\\[\[/g, '[[').replace(/\]\\\]/g, ']]'); }

    async function verkleinern(blob) {
        if (!/^image\/(png|jpeg|webp)$/.test(blob.type) || typeof createImageBitmap !== 'function') { return blob; }
        let bild;
        try { bild = await createImageBitmap(blob); } catch (_) { return blob; }
        const gross = Math.max(bild.width, bild.height);
        if (gross <= MAX_KANTE && blob.type !== 'image/jpeg') { bild.close(); return blob; }
        const f = Math.min(1, MAX_KANTE / gross);
        const c = document.createElement('canvas');
        c.width = Math.round(bild.width * f); c.height = Math.round(bild.height * f);
        c.getContext('2d').drawImage(bild, 0, 0, c.width, c.height);
        bild.close();
        const typ = blob.type === 'image/png' ? 'image/png' : 'image/jpeg';
        const neu = await new Promise((ok) => c.toBlob(ok, typ, 0.9));
        return neu || blob;
    }

    async function hochladen(blob, optionen) {
        const datei = await verkleinern(blob);
        const fd = new FormData();
        fd.append('datei', datei, blob.name || '');
        fd.append('bezugArt', optionen.bezugArt);
        fd.append('bezugId', optionen.bezugId);
        if (optionen.token) { fd.append(optionen.tokenFeld || '__RequestVerificationToken', optionen.token); }
        const antwort = await fetch('dateien/bild', { method: 'POST', body: fd, credentials: 'same-origin' });
        let daten = null;
        try { daten = await antwort.json(); } catch (_) { daten = null; }
        if (!antwort.ok) { throw new Error((daten && (daten.fehler || daten.Fehler)) || `Hochladen fehlgeschlagen (${antwort.status}).`); }
        return daten;
    }

    window.fpEditor = {
        start(element, dotnet, text, optionen) {
            if (!window.toastui || !window.toastui.Editor) { return false; }
            const melden = warten(() => dotnet.invokeMethodAsync('TextGeaendert', bereinigt(editor.getMarkdown())), 400);
            const editor = new window.toastui.Editor({
                el: element,
                height: optionen.hoehe || '520px',
                initialEditType: optionen.modus || 'markdown',
                previewStyle: 'tab',
                language: 'de-DE',
                usageStatistics: false,
                autofocus: false,
                initialValue: text || '',
                placeholder: 'Text der Seite … Bilder einfach einfügen (Strg+V) oder hineinziehen. [[Seitentitel]] verlinkt eine Wiki-Seite.',
                hooks: {
                    addImageBlobHook: (blob, fertig) => {
                        dotnet.invokeMethodAsync('Meldung', 'Bild wird hochgeladen …', false);
                        hochladen(blob, optionen)
                            .then((d) => { fertig(d.adresse, d.name); dotnet.invokeMethodAsync('BildHochgeladen', d.name); })
                            .catch((e) => dotnet.invokeMethodAsync('Meldung', e.message, true));
                        return false;
                    }
                },
                events: { change: () => melden() }
            });
            element.fpEditor = editor;
            return true;
        },
        text(element) { return element && element.fpEditor ? bereinigt(element.fpEditor.getMarkdown()) : null; },
        ende(element) { if (element && element.fpEditor) { element.fpEditor.destroy(); element.fpEditor = null; } }
    };
})();
