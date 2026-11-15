// Karte mit Leaflet und OpenStreetMap (Kacheln von tile.openstreetmap.org, Namensnennung Pflicht).
// Aufruf aus Blazor: fpKarte.zeigen('element-id', { mitte: [lat, lon], zoom: 14, punkte: [{ breite, laenge, farbe, titel, link }] })
window.fpKarte = {
    zeigen(id, daten) {
        const el = document.getElementById(id);
        if (!el || !window.L) { return; }
        if (el._karte) { el._karte.remove(); }
        const karte = L.map(el).setView(daten.mitte, daten.zoom || 14);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>-Mitwirkende'
        }).addTo(karte);
        const grenzen = [];
        for (const p of daten.punkte || []) {
            const m = L.circleMarker([p.breite, p.laenge], { radius: p.radius || 8, color: '#333', weight: 1, fillColor: p.farbe, fillOpacity: 0.9 }).addTo(karte);
            const titel = document.createElement('div');
            titel.textContent = p.titel;
            if (p.link) {
                const a = document.createElement('a');
                a.href = p.link; a.target = '_blank'; a.textContent = 'Foto';
                titel.appendChild(document.createElement('br'));
                titel.appendChild(a);
            }
            m.bindPopup(titel);
            grenzen.push([p.breite, p.laenge]);
        }
        if (grenzen.length > 1) { karte.fitBounds(grenzen, { padding: [20, 20], maxZoom: 16 }); }
        el._karte = karte;
        // Blazor rendert das Layout evtl. noch fertig: Größe nachmessen, dann Ausschnitt anpassen.
        setTimeout(() => { karte.invalidateSize(); if (grenzen.length > 1) { karte.fitBounds(grenzen, { padding: [20, 20], maxZoom: 16 }); } }, 500);
        if (window.ResizeObserver) { new ResizeObserver(() => karte.invalidateSize()).observe(el); }
    }
};
