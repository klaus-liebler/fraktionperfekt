// Verhalten statt Inline-Handlern (Content-Security-Policy ohne 'unsafe-inline'): Ereignisse werden am Dokument abgefangen,
// funktionieren also auch für Elemente, die Blazor später rendert.
//   data-auto-absenden     Auswahl ändert sich → Formular absenden (Rollenwechsel)
//   data-markieren         Klick markiert den Inhalt (z. B. Kalenderadresse zum Kopieren)
document.addEventListener('change', (e) => {
    const el = e.target.closest && e.target.closest('[data-auto-absenden]');
    if (el && el.form) { el.form.submit(); }
});
document.addEventListener('click', (e) => {
    const markieren = e.target.closest && e.target.closest('[data-markieren]');
    if (markieren && typeof markieren.select === 'function') { markieren.select(); }
});
//   data-standort          Klick ermittelt den Standort (GPS) und füllt die Felder name="breite"/"laenge" im selben Formular
document.addEventListener('click', (e) => {
    const knopf = e.target.closest && e.target.closest('[data-standort]');
    if (!knopf || !knopf.form || !navigator.geolocation) { return; }
    e.preventDefault();
    const anzeige = knopf.form.querySelector('[data-standort-anzeige]');
    if (anzeige) { anzeige.textContent = 'Standort wird ermittelt …'; }
    navigator.geolocation.getCurrentPosition((pos) => {
        knopf.form.elements['breite'].value = pos.coords.latitude.toFixed(6);
        knopf.form.elements['laenge'].value = pos.coords.longitude.toFixed(6);
        if (anzeige) { anzeige.textContent = 'Standort: ' + pos.coords.latitude.toFixed(5) + ', ' + pos.coords.longitude.toFixed(5) + ' (± ' + Math.round(pos.coords.accuracy) + ' m)'; }
    }, (fehler) => {
        if (anzeige) { anzeige.textContent = 'Kein Standort: ' + fehler.message + '. Bitte Koordinaten oder Ort eintragen.'; }
    }, { enableHighAccuracy: true, timeout: 15000 });
});
// Formulare mit data-standort-auto ermitteln den Standort beim Laden (Abhängliste: nächste zuerst).
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-standort-auto]').forEach((f) => {
        if (!navigator.geolocation || f.elements['breite'].value) { return; }
        navigator.geolocation.getCurrentPosition((pos) => {
            f.elements['breite'].value = pos.coords.latitude.toFixed(6);
            f.elements['laenge'].value = pos.coords.longitude.toFixed(6);
            f.submit();
        }, () => { }, { timeout: 10000 });
    });
});
