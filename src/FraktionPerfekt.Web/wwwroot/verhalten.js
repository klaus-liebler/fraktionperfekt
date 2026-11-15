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

// Service Worker (App-Hülle offline, Web Push).
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => { navigator.serviceWorker.register('sw.js').catch(() => { }); });
}
//   data-push-aktivieren   Klick: Erlaubnis holen, Push-Abo anlegen (VAPID-Schlüssel in data-schluessel), beim Server anmelden
document.addEventListener('click', async (e) => {
    const knopf = e.target.closest && e.target.closest('[data-push-aktivieren]');
    if (!knopf) { return; }
    const status = document.querySelector('[data-push-status]');
    const melden = (t) => { if (status) { status.textContent = t; } };
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) { melden('Dieser Browser unterstützt kein Web Push.'); return; }
    try {
        if (await Notification.requestPermission() !== 'granted') { melden('Mitteilungen wurden nicht erlaubt.'); return; }
        const reg = await navigator.serviceWorker.register('sw.js');
        await navigator.serviceWorker.ready;
        const b64 = knopf.dataset.schluessel.replace(/-/g, '+').replace(/_/g, '/');
        const roh = atob(b64 + '='.repeat((4 - b64.length % 4) % 4));
        const schluessel = Uint8Array.from(roh, (c) => c.charCodeAt(0));
        const abo = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: schluessel });
        const json = abo.toJSON();
        const antwort = await fetch('push/abo', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ endpoint: json.endpoint, keys: json.keys, geraet: navigator.userAgent.substring(0, 120) })
        });
        melden(antwort.ok ? 'Push ist auf diesem Gerät aktiv. Bitte Seite neu laden.' : 'Anmeldung fehlgeschlagen (' + antwort.status + ').');
    } catch (fehler) {
        melden('Push konnte nicht aktiviert werden: ' + fehler.message);
    }
});
