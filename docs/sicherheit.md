# Sicherheit: OWASP-Checkliste (Stand Iteration 1)

Orientierung: OWASP Top 10 (2021) und ASVS 4.0 Level 2, Aufbau wie in VibeVHS.
✅ umgesetzt · ⚠️ teilweise/offen · ⏳ mit der echten Anmeldung.

Besonderheit der Fraktion: Es geht um politische Meinungen (Art. 9 DSGVO) und um nichtöffentliche Sitzungsinhalte.
Abwesenheitsgründe können Gesundheitsdaten sein.

| Thema | Stand | Umsetzung / offener Punkt |
|---|---|---|
| A01 Zugriffskontrolle | ✅ | Rollen werden je Aktion im Anwendungsdienst geprüft (`Rechte`), nicht nur in der Oberfläche. Gesperrte Knöpfe und Menüpunkte erklären den Grund. Das Kampagnenteam sieht nur öffentliche und Kampagnentermine. Den Abwesenheitsgrund sehen nur die Person selbst und die Administration. Vertretungsanfragen beantwortet nur die gefragte Person. Endpunkte verlangen eine Anmeldung, außer der Anmeldeseite und dem Kalender-Abo. |
| A02 Kryptografie | ✅/⚠️ | Abo-Tokens: 256 Bit Zufall, gespeichert nur als SHA-256. Sicherungen mit SHA-256. HSTS außerhalb der Entwicklung. ⚠️ Verschlüsselung der Datenbank im Ruhezustand über die Laufwerksverschlüsselung des Servers; Backups verschlüsselt ablegen (Konzept). |
| A03 Injection | ✅ | EF Core mit Parametern, keine zusammengesetzten SQL-Texte aus Benutzereingaben. ICS-Ausgabe maskiert `;`, `,`, `\` und Zeilenumbrüche. |
| A04 Unsicheres Design | ✅ | Nichts wird überschrieben (bitemporal), Protokoll nur zum Anfügen, Postausgang statt Direktversand, Eskalation statt stillem Scheitern. |
| A05 Fehlkonfiguration | ✅ | Sicherheitsheader (`Sicherheitsheader.cs`): CSP ohne `'unsafe-inline'` für Skripte (`script-src 'self'` plus SHA-256 der Blazor-Import-Map), `style-src-elem 'self'`, `frame-ancestors 'none'`, X-Frame-Options DENY, nosniff, Referrer-Policy, Permissions-Policy (Kamera, Mikrofon und Standort aus), COOP. `Cache-Control: no-store` für Personen, Abwesenheit, Vertretungen, Protokoll, Postausgang, Handy-Seiten, Betrieb und Kalender-Abo. Inline-Handler ersetzt durch `verhalten.js`. Verbleibend: `style-src-attr 'unsafe-inline'`, weil Blazor und Radzen Stile per Attribut setzen; ohne Skriptausführung gibt es darüber keinen XSS-Pfad. |
| A06 Veraltete Komponenten | ⚠️ | Zentrale Paketversionen; `dotnet list package --vulnerable` vor jeder Auslieferung (noch kein CI). |
| A07 Authentifizierung | ⏳ | Testanmeldung ohne Passwort (nur Entwicklung, optionaler Zugangscode `Anmeldung__Zugangscode`). Geplant sind Passkeys (ASP.NET Core Identity), Anmeldelink per Mail als Rückfall (15 Minuten, einmalig, Bestätigung per POST wie in VibeVHS) und Zwei-Faktor für alle mit Zugriff auf interne Protokolle. Rate-Limit auf `/anmelden`. |
| A08 Integrität | ✅ | Antiforgery für alle Formulare, auch die statischen Handy-Formulare. Prüfsumme bei Sicherungen. |
| A09 Protokollierung | ✅ | Protokoll nur mit Pseudonymen („Person #ABCD1234“, Hash der Id), ohne Klarnamen, Anmeldenamen oder Abwesenheitsgründe (per Test geprüft). Serilog-Dateilogs ohne Personendaten. |
| A10 SSRF | ✅ | Keine serverseitigen Abrufe von Adressen aus Benutzereingaben. Der spätere RIS-Abruf nutzt eine feste, konfigurierte Adresse. |
| Rate-Limits | ✅ | `/anmelden` und `/kalender/abo`: `Sicherheit:RateLimitProMinute` (Standard 60) je Adresse, Antwort 429 mit Retry-After. |
| Push-Mitteilungen | ✅ | Kurzer Text ohne vertrauliche Details (Sperrbildschirm), z. B. „Vertretung gesucht: HFA am 08.12.“. Details erst nach Anmeldung. |
| Kalender-Abo | ✅ | Enthält nur eigene Termine. Neuer Link widerruft den alten. Abruf wird vermerkt (zuletzt abgerufen). |

Nächste Schritte: echte Anmeldung mit Sitzungs-Timeout, Abhängigkeitsprüfung in einer CI-Stufe, Penetrationstest vor dem
Echtbetrieb, Löschkonzept (Abwesenheiten nach 12 Monaten).
