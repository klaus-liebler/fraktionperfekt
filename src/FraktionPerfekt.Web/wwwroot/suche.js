// Alles-Suche in der Kopfzeile (aus SchoolCraft FA-3231): Vorschläge beim Tippen, ↑/↓ wählen, Enter öffnet, Esc schließt,
// Strg+K (Mac: ⌘K) springt von überall ins Suchfeld. Nur am Rechner; auf dem Handy bleibt die Suchseite /suche.
(() => {
    const amRechner = () => window.matchMedia('(min-width: 768px) and (pointer: fine)').matches;
    let liste = null, eingabe = null, treffer = [], markiert = -1, zeitgeber = 0, anfrageNr = 0;

    const schliessen = () => {
        if (liste) { liste.hidden = true; liste.innerHTML = ''; }
        if (eingabe) { eingabe.setAttribute('aria-expanded', 'false'); eingabe.removeAttribute('aria-activedescendant'); }
        treffer = []; markiert = -1;
    };

    const markieren = (i) => {
        markiert = i;
        liste.querySelectorAll('[role=option]').forEach((el, n) => el.setAttribute('aria-selected', n === i ? 'true' : 'false'));
        if (i >= 0) { eingabe.setAttribute('aria-activedescendant', 'fp-vorschlag-' + i); }
    };

    const zeigen = (daten) => {
        treffer = daten; markiert = -1;
        liste.innerHTML = '';
        if (daten.length === 0) {
            const leer = document.createElement('div');
            leer.className = 'fp-vorschlag-leer';
            leer.textContent = 'Nichts gefunden – Enter sucht ausführlich.';
            liste.appendChild(leer);
        }
        let bereich = null;
        daten.forEach((t, i) => {
            if (t.bereich !== bereich) {
                bereich = t.bereich;
                const kopf = document.createElement('div');
                kopf.className = 'fp-vorschlag-bereich';
                kopf.textContent = bereich;
                liste.appendChild(kopf);
            }
            const a = document.createElement('a');
            a.id = 'fp-vorschlag-' + i;
            a.href = t.ziel;
            a.setAttribute('role', 'option');
            a.setAttribute('aria-selected', 'false');
            a.className = 'fp-vorschlag';
            const titel = document.createElement('span');
            titel.textContent = t.titel;
            a.appendChild(titel);
            if (t.text) { const s = document.createElement('small'); s.textContent = t.text; a.appendChild(s); }
            a.addEventListener('mousedown', (e) => e.preventDefault());
            liste.appendChild(a);
        });
        const hinweis = document.createElement('div');
        hinweis.className = 'fp-vorschlag-hinweis';
        hinweis.innerHTML = '<kbd>↑</kbd> <kbd>↓</kbd> wählen · <kbd>Enter</kbd> öffnet · <kbd>Esc</kbd> schließt';
        liste.appendChild(hinweis);
        liste.hidden = false;
        eingabe.setAttribute('aria-expanded', 'true');
    };

    const laden = async () => {
        const q = eingabe.value.trim();
        if (q.length < 2) { schliessen(); return; }
        const nr = ++anfrageNr;
        try {
            const antwort = await fetch('suche/vorschlaege?q=' + encodeURIComponent(q), { headers: { 'Accept': 'application/json' } });
            if (!antwort.ok || nr !== anfrageNr) { return; }
            zeigen(await antwort.json());
        } catch { /* ohne Netz: Enter führt weiter zur Suchseite */ }
    };

    const einrichten = () => {
        const feld = document.querySelector('.fp-kopfsuche input[name=q]');
        if (!feld || feld === eingabe) { return; }
        eingabe = feld;
        liste = document.createElement('div');
        liste.className = 'fp-vorschlaege';
        liste.id = 'fp-vorschlaege';
        liste.setAttribute('role', 'listbox');
        liste.hidden = true;
        feld.parentElement.appendChild(liste);
        feld.setAttribute('role', 'combobox');
        feld.setAttribute('aria-controls', 'fp-vorschlaege');
        feld.setAttribute('aria-expanded', 'false');
        feld.setAttribute('autocomplete', 'off');
        feld.addEventListener('input', () => {
            if (!amRechner()) { return; }
            clearTimeout(zeitgeber);
            zeitgeber = setTimeout(laden, 150);
        });
        feld.addEventListener('keydown', (e) => {
            if (liste.hidden) { return; }
            if (e.key === 'ArrowDown') { e.preventDefault(); markieren(Math.min(markiert + 1, treffer.length - 1)); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); markieren(Math.max(markiert - 1, -1)); }
            else if (e.key === 'Escape') { e.preventDefault(); schliessen(); }
            else if (e.key === 'Enter' && markiert >= 0) { e.preventDefault(); const ziel = treffer[markiert].ziel; schliessen(); window.location.href = ziel; }
        });
        feld.addEventListener('blur', () => setTimeout(schliessen, 150));
    };

    // Blazor rendert die Kopfzeile neu; das Feld wird bei Bedarf neu eingerichtet.
    new MutationObserver(einrichten).observe(document.documentElement, { childList: true, subtree: true });
    document.addEventListener('DOMContentLoaded', einrichten);
    einrichten();

    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && !e.altKey && e.key.toLowerCase() === 'k') {
            const feld = document.querySelector('.fp-kopfsuche input[name=q]');
            if (feld) { e.preventDefault(); feld.focus(); feld.select(); }
        }
    });
})();
