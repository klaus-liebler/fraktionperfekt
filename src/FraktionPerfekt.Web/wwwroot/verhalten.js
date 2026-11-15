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
