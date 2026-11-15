# Handy-Test im lokalen Netz

Die Handy-Seiten (`/handy`) funktionieren auch über `http://<PC>:5290`. Für „Zum Startbildschirm hinzufügen“ (PWA),
später Service Worker und Web Push verlangt der Handy-Browser aber **HTTPS**. Dafür gibt es das Startprofil **`lan`**
(wie in VibeVHS):

- Die Anwendung lauscht mit HTTPS auf allen Netzwerkschnittstellen, Port **7290**.
- Das Zertifikat ist selbst erzeugt und liegt außerhalb des Repos unter `%USERPROFILE%\.fraktionperfekt\lan`.
- Die öffentliche Adresse (Links in Mails, Push und Kalender-Abo) wird automatisch auf `https://<LAN-IP>:7290/` gesetzt.
- Das Profil nutzt eine **eigene Datenbank** (`src/FraktionPerfekt.Web/data/fraktionperfekt-lan.db`) und eigene Logs
  (`logs/lan`).

## 1. Zertifikat erzeugen

Einmalig, und erneut, wenn sich die LAN-IP ändert:

```powershell
dotnet run tools/LanZertifikat.cs
```

Das Zertifikat wird nicht in den Windows-Zertifikatspeicher eingetragen.

## 2. Starten

```powershell
dotnet run --project src/FraktionPerfekt.Web --launch-profile lan
```

In der Konsole erscheint zum Beispiel `LAN-Betrieb: https://192.168.2.118:7290/`.

## 3. Windows-Firewall (falls das Handy die Seite nicht erreicht)

Den Zugriff nur für **private Netzwerke** erlauben, oder in einer Administrator-PowerShell:

```powershell
New-NetFirewallRule -DisplayName "FraktionPerfekt Handy-Test" -Direction Inbound -Protocol TCP -LocalPort 7290 -Profile Private -Action Allow
```

Nach dem Test wieder entfernen:

```powershell
Remove-NetFirewallRule -DisplayName "FraktionPerfekt Handy-Test"
```

Handy und PC müssen im selben WLAN sein; Gäste-WLANs trennen Geräte oft voneinander.

## 4. Testablauf

1. Am Handy `https://<LAN-IP>:7290/handy` öffnen und die Zertifikatswarnung einmal bestätigen.
2. Als **Anna Wewers** anmelden. Unter „Heute“ steht „Vertretung gesucht“ (HFA). „Ich vertrete“ tippen: Erwartet wird
   „Danke! Sie vertreten am …“.
3. Gegenprobe am PC als Thomas Brinkmann (Vorstand): In **Offene Vertretungen** ist der HFA nicht mehr offen. Im
   **Postausgang** (als Katrin Albers) steht die Mitteilung „Vertretung steht“ an Jens Overmeyer.
4. Am Handy **Abwesend** tippen, einen Zeitraum mit Sitzung eintragen (z. B. den Tag der nächsten ASB-Sitzung) und
   „Vertretung anfragen“ tippen.
5. „Zum Startbildschirm hinzufügen“: Das App-Symbol öffnet direkt `/handy` (Manifest; die Kurzbefehle „Abwesenheit
   melden“ und „Heute“ sind vorbereitet).

## Hinweise

- Die Instanz im Profil `lan` ist nur für Tests im eigenen Netz gedacht, nicht für das Internet.
- Zum Beenden Strg+C. Die Test-Datenbank `data/fraktionperfekt-lan.db` kann gelöscht werden; beim nächsten Start
  entsteht sie neu.
