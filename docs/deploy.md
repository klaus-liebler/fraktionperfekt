# Bereitstellung auf dem Webserver (Testsystem)

Ziel: **https://liebler.iui.hs-osnabrueck.de/fraktionperfekt/** auf demselben Server wie VibeVHS. Das Kit in `deploy/server`
folgt dem VibeVHS-Kit (`C:\repos\vibevhs\deploy`): framework-abhängiger Linux-Build (gemeinsame ASP.NET-Core-Runtime aus VibeVHS-Schritt 09; `deploy.ps1 -MitRuntime` = self-contained), Programm in `/opt`, Daten in `/var/lib`,
systemd-Dienst nur auf `127.0.0.1`, nginx davor mit Zugangspasswort, jeder root-Schritt als eigenes Skript mit Sicherung und Rückweg.

| | VibeVHS | FraktionPerfekt |
|---|---|---|
| Pfad | `/vibevhs` | `/fraktionperfekt` |
| Kestrel | `127.0.0.1:5080` | `127.0.0.1:5081` |
| Programm | `/opt/vibevhs` | `/opt/fraktionperfekt` (vorige Version: `/opt/fraktionperfekt.vorher`) |
| Daten | `/var/lib/vibevhs` | `/var/lib/fraktionperfekt` (`fraktionperfekt.db`, `dateien/`, `schluessel/`) |
| Sicherungen | `/var/backups/vibevhs` | `/var/backups/fraktionperfekt` (vor jedem Update + täglich 01:45 Uhr) |
| Umgebung | `/etc/vibevhs/vibevhs.env` | `/etc/fraktionperfekt/fraktionperfekt.env` |
| Zugangspasswort | `/etc/nginx/vibevhs.htpasswd` | `/etc/nginx/fraktionperfekt.htpasswd` (Benutzer `fraktion-test`) |
| Schema | EnsureCreated (noch ohne Migrationen) | EF-Migrationen beim Start |

## Sicherheitsstand – bitte zuerst lesen

- **Es gibt noch keine echte Anmeldung.** Die Testanmeldung lässt jedes Konto ohne Passwort wählen. Deshalb:
  - nginx verlangt für **alles** unter `/fraktionperfekt/` das Zugangspasswort (Basic Auth), auch für Stile und Skripte.
    Ohne Passwort erreichbar sind nur die Kalender-Abos (Token in der Adresse, widerrufbar) und `/gesund` (nur „ok“).
  - Die Anwendung startet außerhalb der Entwicklung **nur**, wenn `Anmeldung__TestanmeldungErlaubt=true` gesetzt ist
    (bewusste Entscheidung in `/etc/fraktionperfekt/fraktionperfekt.env`).
  - `ausrollen.sh` bricht ab, solange Passwortdatei und nginx-Einbindung fehlen oder die Entscheidung nicht gesetzt ist.
  - Wer das Zugangspasswort kennt, kann sich als **jede Person** anmelden, auch als Vorstand oder Administration. Das Passwort
    deshalb nur an Testpersonen geben, die alles sehen dürfen.
- **Demodaten** enthalten echte Namen der Fraktion und die echten Mailadressen der sechs Vorstandsmitglieder; alle Vorgänge sind
  erfunden. Mit Demodaten verschickt der Postausgang nichts (Demo-Schutz). Sie werden nur mit `Demodaten__Anlegen=true` angelegt –
  ohne sie ist die Datenbank leer und die Testanmeldung hat keine Konten.
- **Vor echtem Betrieb fehlt** (VibeVHS hat dafür einen eigenen Zugangscode als zweite Hürde, aber auch noch keine Passkeys):
  echte Anmeldung (Passkeys mit Anmeldelink per Mail als Rückweg, wie in `Program.cs` vorgesehen), Benutzerverwaltung bzw. Import
  der echten Personen ohne Demodaten, ein echter Mailkanal, Löschkonzept für Testdaten. Bis dahin: nur Testsystem hinter Passwort.

## Vor dem ersten Lauf entscheiden bzw. eintragen

1. `deploy/server/konfiguration.env`: Host, Pfad (`/fraktionperfekt`), Port (`5081`, muss frei sein), Benutzer des Zugangspassworts.
2. Zugangspasswort: `01-einrichten.sh` fragt es einmal ab (htpasswd, bcrypt). Weitere Konten: `sudo htpasswd -B /etc/nginx/fraktionperfekt.htpasswd <name>`.
3. In `/etc/fraktionperfekt/fraktionperfekt.env` (Schritt 01 legt die Datei an):
   - `Anmeldung__TestanmeldungErlaubt=true` – nur, solange das Zugangspasswort davor steht.
   - `Demodaten__Anlegen=true` – ja/nein (siehe oben).
   - optional `Ki__Schluessel=…`.
4. Die include-Zeile für nginx setzt Schritt 01 in `/etc/nginx/sites-available/liebler.conf` (vor den VibeVHS-Abschnitt). Spielt das
   VibeVHS-Kit (dort Schritt 03) die Datei neu ein, ist sie weg – dann Schritt 01 hier erneut ausführen. Dauerhaft besser: die Zeile
   `include /etc/nginx/snippets/fraktionperfekt-locations.conf;` auch in `C:\repos\vibevhs\deploy\server\nginx\liebler.conf` aufnehmen.

## Erstinstallation

Voraussetzung: Das VibeVHS-Kit ist eingerichtet (nginx, Zertifikat, Firewall, Pakete `sqlite3`, `apache2-utils`, `libicu`).

```powershell
.\deploy\server\deploy.ps1 -NurKit                                             # Kit nach /tmp/fraktionperfekt-kit
ssh -t liebler "sudo bash /tmp/fraktionperfekt-kit/schritte/01-einrichten.sh"   # Benutzer, Verzeichnisse, Unit, nginx, Passwort
ssh -t liebler "sudo nano /etc/fraktionperfekt/fraktionperfekt.env"             # Entscheidungen eintragen (siehe oben)
.\deploy\server\deploy.ps1                                                     # bauen, hochladen, einspielen
```

Prüfen: `https://liebler.iui.hs-osnabrueck.de/fraktionperfekt/` fragt nach dem Zugangspasswort, danach die Testanmeldung.
Auf dem Server: `curl http://127.0.0.1:5081/fraktionperfekt/gesund` → `ok`, Logs `journalctl -u fraktionperfekt -f` und `/var/log/fraktionperfekt/`.

## Update

```powershell
.\deploy\server\deploy.ps1
```

Gebaut wird in `deploy/ausgabe` (eigener Artefakt-Ordner; die lokal laufende Instanz bleibt unberührt). Übertragen wird **nur** die
Ausgabe von `dotnet publish` – keine Datenbank, keine Dateien, keine Schlüssel. Bau und Server prüfen das zusätzlich und brechen ab,
falls doch `*.db` oder `data/` im Archiv landen. Auf dem Server: Datenbank sichern (`sqlite3 .backup` nach
`/var/backups/fraktionperfekt/vor-update-<Zeit>.db`), `/opt/fraktionperfekt` durch die neue Version ersetzen (die alte bleibt als
`.vorher`), starten – die Migrationen laufen beim Start –, `/gesund` abwarten. Antwortet die neue Version nicht, kommt die vorige
automatisch zurück.

**Regel seit dem ersten Ausrollen (17.11.2026): Die Migration „Initial“ (`20261117075645_Initial`) wird nicht mehr neu erzeugt
oder verändert.** Jede Schemaänderung kommt als **zusätzliche** Migration
(`dotnet ef migrations add <Name> --project src/FraktionPerfekt.Infrastructure --startup-project src/FraktionPerfekt.Infrastructure
--output-dir Datenbank/Migrationen`), damit die Datenbank auf dem Server beim Start nur ergänzt wird und erhalten bleibt. Daten, die
eine Änderung umformt, gehören in dieselbe Migration (SQL in `Up`), getestet gegen eine Kopie einer Server-Sicherung.

Nur im Notfall (Schema doch neu aufgesetzt): Eine vorhandene Server-Datenbank passt dann nicht mehr – die neue Version startet nicht
und das Ausrollen fällt auf die vorige zurück. Dann bewusst neu anlegen (Daten gehen verloren, die Sicherung bleibt):

```bash
sudo systemctl stop fraktionperfekt
sudo /usr/local/sbin/fraktionperfekt-sicherung.sh vor-neuanlage
sudo mv /var/lib/fraktionperfekt/fraktionperfekt.db /var/backups/fraktionperfekt/verworfen-$(date +%Y%m%d-%H%M%S).db
sudo rm -f /var/lib/fraktionperfekt/fraktionperfekt.db-wal /var/lib/fraktionperfekt/fraktionperfekt.db-shm
sudo bash /tmp/fraktionperfekt-kit/schritte/ausrollen.sh /tmp/fraktionperfekt.tar.gz   # oder deploy.ps1 erneut
```


## Sicherung und Rücksicherung

- Täglich 01:45 Uhr (`fraktionperfekt-sicherung.timer`) und vor jedem Update: `sqlite3 .backup` (konsistent im laufenden Betrieb),
  Plausibilitätsprüfung, Aufbewahrung 14 Tage (vor Updates 30 Tage). Anhänge in `/var/lib/fraktionperfekt/dateien` sichert das
  Skript nicht – bei Bedarf in die Serversicherung aufnehmen.
- Rücksicherung: `sudo systemctl stop fraktionperfekt` → gewünschte Sicherung als `/var/lib/fraktionperfekt/fraktionperfekt.db`
  einsetzen (vorher `-wal`/`-shm` beiseitelegen, Besitzer `fraktionperfekt`, Rechte 0640) → `sudo systemctl start fraktionperfekt`.
- Programm zurück: `ssh -t liebler "sudo bash /tmp/fraktionperfekt-kit/schritte/ausrollen.sh --zurueck"`.
- Alles aus (Daten bleiben): `sudo bash /tmp/fraktionperfekt-kit/schritte/01-zurueck.sh`.

## Betriebsdetails

- **Pfad-Basis:** `PathBase=/fraktionperfekt` (Unit). Die Anwendung setzt `<base href>`, Routing läuft nach der Pfad-Basis; der
  Service Worker ermittelt die Basis aus seinem Geltungsbereich (Cache-Version `fp-huelle-v3`, das Spiel wird nie gecacht).
- **Geführte Touren** auf der Hilfeseite: Schalter `Abnahmemodus:Aktiv`, Standard **an**. Sie arbeiten mit Demodaten und Testanmeldung (Kontowechsel) – vor dem Echtbetrieb ausschalten (`Abnahmemodus__Aktiv=false` in der Umgebungsdatei) oder überarbeiten (siehe `docs/konzept.md`).
- **Proxy:** `ReverseProxy__Aktiv=true`, Weiterleitungs-Header nur von Loopback und nur für den eigenen Host.
- **Data-Protection-Schlüssel** (Anmelde-Cookies) in `/var/lib/fraktionperfekt/schluessel` (0700, nur der Dienst); neue Dateien des
  Dienstes entstehen mit `UMask=0027`.
- **Zeitzone:** Die Anwendung rechnet mit der Zeitzone des Mandanten (`Mandant:Zeitzone`, Europe/Berlin) und speichert UTC – die
  Serverzeitzone ist egal, die Uhr muss stimmen (`timedatectl`, Schritt 01 warnt sonst).
- **Mandanten:** vorbereitet über den Hostnamen (`Mandant__Hosts__0` aus der Unit; weitere Mandanten als `Mandanten__N__…` in der
  Umgebungsdatei, jeweils mit eigener Datenbank).
- **sqlite3 von Hand:** Die Namensspalten nutzen die Collation `DIN5007_2`, die nur die Anwendung registriert. Die Kommandozeile kann
  sichern und lesen; Sortieren/Vergleichen nach Namen oder `PRAGMA integrity_check` melden dort „no such collation“. Zum Lesen die
  Sichten `v_…` nehmen (z. B. `sqlite3 -readonly /var/lib/fraktionperfekt/fraktionperfekt.db "SELECT * FROM v_PersonNamen LIMIT 5"`).

## Übungsinstanz (seit 24.11.2026)

Eine Übungsinstanz ist ein zweiter Mandant im selben Prozess mit eigener Datenbank und eigenem Host. Beispiel für
`/etc/fraktionperfekt/appsettings.Testsystem.json` (Hosts anpassen; nginx muss beide Namen an die Anwendung weiterreichen):

```json
"Mandanten": [
  { "Kennung": "greven", "Name": "CDU-Fraktion Greven", "Zusatz": "im Rat der Stadt Greven", "Hosts": [ "fraktion.example.de" ] },
  { "Kennung": "uebung", "Name": "CDU-Fraktion Greven (Übung)", "Uebung": true,
    "Datenbank": "Data Source=/var/lib/fraktionperfekt/uebung.db", "Hosts": [ "uebung.example.de" ] }
]
```

Beim Start wird eine leere Übungsdatenbank mit frischen Demodaten gefüllt; **Übung zurücksetzen** steht auf der Hilfeseite der Übungsinstanz
(Administration, Vorstand). Dort sind Touren und Testanmeldung an und Mails gesperrt; im Echtbetrieb sind die Touren dann aus.
`Anmeldung__TestanmeldungNurInUebung=true` schaltet die Testanmeldung im Echtbetrieb ab – erst setzen, wenn es eine echte Anmeldung gibt.
Die tägliche Sicherung betrifft nur die Hauptdatenbank; die Übungsdatenbank braucht keine.
