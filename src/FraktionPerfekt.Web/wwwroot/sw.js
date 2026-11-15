// Service Worker von FraktionPerfekt: App-Hülle (Stil, Logo, Leaflet) offline vorhalten, Seiten immer frisch vom Server
// (Netz zuerst, ohne Netz die Offline-Seite), Web-Push-Mitteilungen anzeigen und beim Antippen die passende Seite öffnen.
const HUELLE = 'fp-huelle-v1';
const DATEIEN = ['offline.html', 'app.css', 'verhalten.js', 'karte.js', 'manifest.webmanifest', 'marke/logo.png', 'marke/favicon.png',
    'marke/icon-192.png', 'marke/icon-512.png', 'lib/leaflet/leaflet.css', 'lib/leaflet/leaflet.js'];

self.addEventListener('install', (e) => {
    e.waitUntil(caches.open(HUELLE).then((c) => c.addAll(DATEIEN)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
    e.waitUntil(caches.keys().then((namen) => Promise.all(namen.filter((n) => n !== HUELLE).map((n) => caches.delete(n))))
        .then(() => self.clients.claim()));
});

self.addEventListener('fetch', (e) => {
    const anfrage = e.request;
    const url = new URL(anfrage.url);
    if (anfrage.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/_blazor')) {
        return;
    }

    if (anfrage.mode === 'navigate') {
        // Seiten nie aus dem Cache (Rechte, aktuelle Daten) – nur ohne Netz die Offline-Seite.
        e.respondWith(fetch(anfrage).catch(() => caches.match('offline.html')));
        return;
    }

    const statisch = /\.(css|js|png|svg|webp|woff2?)$/.test(url.pathname) && !url.pathname.startsWith('/_framework');
    if (statisch) {
        e.respondWith(caches.match(anfrage).then((treffer) => {
            const frisch = fetch(anfrage).then((antwort) => {
                if (antwort.ok) {
                    const kopie = antwort.clone();
                    caches.open(HUELLE).then((c) => c.put(anfrage, kopie));
                }
                return antwort;
            }).catch(() => treffer);
            return treffer || frisch;
        }));
    }
});

self.addEventListener('push', (e) => {
    let daten = { titel: 'FraktionPerfekt', text: '', link: 'handy' };
    try {
        daten = Object.assign(daten, e.data ? e.data.json() : {});
    } catch (_) {
        daten.text = e.data ? e.data.text() : '';
    }
    e.waitUntil(self.registration.showNotification(daten.titel, {
        body: daten.text,
        icon: 'marke/icon-192.png',
        badge: 'marke/favicon.png',
        data: { link: daten.link || 'handy' },
        lang: 'de'
    }));
});

self.addEventListener('notificationclick', (e) => {
    e.notification.close();
    const ziel = new URL(e.notification.data && e.notification.data.link ? e.notification.data.link : 'handy', self.registration.scope).href;
    e.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((fenster) => {
        for (const f of fenster) {
            if (f.url === ziel && 'focus' in f) { return f.focus(); }
        }
        return self.clients.openWindow(ziel);
    }));
});
