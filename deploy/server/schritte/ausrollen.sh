#!/usr/bin/env bash
# Neue FraktionPerfekt-Version einspielen (von deploy.ps1 aufgerufen). Die Datenbank wird NIE übertragen oder ersetzt: Sie liegt in
# /var/lib/fraktionperfekt, ausgetauscht wird nur /opt/fraktionperfekt. Vorher Sicherung der Datenbank, danach Gesundheitsprüfung;
# antwortet die neue Version nicht, kommt die vorige automatisch zurück. Migrationen laufen beim Start der Anwendung.
# Aufruf: sudo bash /tmp/fraktionperfekt-kit/schritte/ausrollen.sh /tmp/fraktionperfekt.tar.gz
# Rückweg von Hand: sudo bash /tmp/fraktionperfekt-kit/schritte/ausrollen.sh --zurueck
source "$(dirname "$0")/gemeinsam.sh"
source "$(dirname "$0")/liebler-dotnet.sh"
als_root

if [[ "${1:-}" == "--zurueck" ]]; then
    [[ -d "$PROGRAMM.vorher" ]] || fehler "Keine vorige Version vorhanden."
    systemctl stop "$DIENST"
    rm -rf "$PROGRAMM.defekt" && mv "$PROGRAMM" "$PROGRAMM.defekt" && mv "$PROGRAMM.vorher" "$PROGRAMM"
    if systemctl start "$DIENST" && gesund; then ok "vorige Version läuft wieder"; else fehler "auch die vorige Version startet nicht – journalctl -u $DIENST"; fi
    exit 0
fi

# Voraussetzungen aus Schritt 01 – sonst vor jeder Änderung abbrechen
id "$DIENST" >/dev/null 2>&1 && [[ -f "/etc/systemd/system/$DIENST.service" && -d "$DATEN" && -d "$SICHERUNGEN" ]] \
    || fehler "Schritt 01 fehlt (Dienstbenutzer, $DATEN, $DIENST.service) – erst einrichten, dann erneut ausrollen. Nichts geändert."
# Ohne echte Anmeldung nie ohne Zugangspasswort
zugangsschutz_aktiv || fehler "Zugangspasswort nicht aktiv ($HTPASSWD, include in $NGINX_SITE) – Schritt 01 ausführen. Nichts geändert."
grep -qE '^\s*Anmeldung__TestanmeldungErlaubt=true' "$UMGEBUNG" \
    || fehler "In $UMGEBUNG fehlt die bewusste Entscheidung Anmeldung__TestanmeldungErlaubt=true (siehe docs/deploy.md). Nichts geändert."

archiv="${1:?Pfad zum Archiv fehlt}"
[[ -f "$archiv" ]] || fehler "Archiv $archiv fehlt"
neu=$(mktemp -d "$PROGRAMM.neu.XXXX")
tar -xzf "$archiv" -C "$neu" --strip-components=1
[[ -f "$neu/FraktionPerfekt.Web" ]] || { rm -rf "$neu"; fehler "Archiv enthält kein FraktionPerfekt.Web"; }
# Framework-abhängiges Paket: passende gemeinsame Runtime vorhanden? (VibeVHS-Schritt 09) – sonst nichts ändern
dotnet_pruefen "$neu" "FraktionPerfekt.Web" || { rm -rf "$neu"; fehler "Ausrollen abgebrochen: Runtime fehlt (siehe oben). Nichts geändert."; }
# Sicherheitsnetz: Daten gehören nie ins Programm (würden sonst mit ausgerollt)
if find "$neu" \( -name '*.db' -o -name '*.db-wal' -o -name '*.db-shm' -o -path '*/data/*' \) | grep -q .; then
    rm -rf "$neu"
    fehler "Archiv enthält Datenbank- oder data-Dateien – abgebrochen, nichts geändert."
fi
# Unter Windows gebaute Archive tragen keine Unix-Rechte: Rechte hier festlegen
chown -R root:root "$neu"
find "$neu" -type d -exec chmod 755 {} +
find "$neu" -type f -exec chmod 644 {} +
chmod 755 "$neu/FraktionPerfekt.Web"
if [[ -f "$neu/createdump" ]]; then chmod 755 "$neu/createdump"; fi

# Datenbank vor dem Update sichern (Migrationen laufen beim Start)
if [[ -f "$DATEN/fraktionperfekt.db" ]]; then
    if ziel=$("/usr/local/sbin/$DIENST-sicherung.sh" vor-update); then ok "Datenbank gesichert: $ziel"; else rm -rf "$neu"; fehler "Sicherung fehlgeschlagen – nichts geändert."; fi
fi

systemctl stop "$DIENST" 2>/dev/null || true
rm -rf "$PROGRAMM.vorher"
if [[ -d "$PROGRAMM" ]]; then mv "$PROGRAMM" "$PROGRAMM.vorher"; fi
mv "$neu" "$PROGRAMM"
systemctl start "$DIENST"
if gesund; then
    ok "neue Version läuft · $(curl -fsS "$GESUND")"
    rm -f "$archiv"
else
    warn "neue Version antwortet nicht – zurück zur vorigen"
    journalctl -u "$DIENST" -n 30 --no-pager | sed 's/^/   /'
    bash "$0" --zurueck || true
    fehler "Ausrollen fehlgeschlagen (vorige Version läuft wieder; Datenbank-Sicherung in $SICHERUNGEN). Häufigster Grund: neu erzeugte Initial-Migration passt nicht zur Server-Datenbank – siehe docs/deploy.md."
fi
