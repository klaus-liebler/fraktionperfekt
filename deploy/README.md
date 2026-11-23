# Bereitstellung

Testsystem auf **https://liebler.iui.hs-osnabrueck.de/fraktionperfekt** – derselbe Server wie VibeVHS (nginx als Reverse Proxy,
Hochschulzertifikat, Firewall aus dem VibeVHS-Kit). Gleiches Muster wie `C:\repos\vibevhs\deploy`:

- `server/deploy.ps1` – bauen (linux-x64, framework-abhängig; `-MitRuntime` = self-contained), Kit und Archiv hochladen, auf dem Server einspielen
- `server/schritte/01-einrichten.sh` (Rückweg `01-zurueck.sh`) – einmalig: Dienstbenutzer, Verzeichnisse, Unit, Sicherung, nginx mit Zugangspasswort
- `server/schritte/ausrollen.sh` – Update mit Datenbank-Sicherung, Gesundheitsprüfung und automatischem Rückweg
- `server/konfiguration.env` – Host, Pfad, Port, Dienstname (eine Quelle für Skripte, Unit und nginx-Vorlagen)
- `server/systemd/`, `server/nginx/`, `server/werkzeuge/` – Vorlagen

Ausführlich (Voraussetzungen, Sicherheitsstand, Erstinstallation, Update, Sicherung, Rückweg): [`docs/deploy.md`](../docs/deploy.md).

Die Datenbank wird **nie** übertragen: Sie liegt nur auf dem Server unter `/var/lib/fraktionperfekt`. Geheimnisse und bewusste
Schalter stehen nur auf dem Server (`/etc/fraktionperfekt/fraktionperfekt.env`, `/etc/nginx/fraktionperfekt.htpasswd`).
