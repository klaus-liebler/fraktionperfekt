#!/usr/bin/env bash
# Schritt 01: FraktionPerfekt auf dem Server einrichten, auf dem schon das VibeVHS-Kit läuft (nginx, Zertifikat, Firewall, sqlite3).
# Legt an: Dienstbenutzer, Verzeichnisse, Umgebungsdatei, systemd-Unit, nächtliche Sicherung, nginx-Snippets mit Zugangspasswort
# und die eigene nginx-Datei /etc/nginx/liebler.d/fraktionperfekt.conf. Die Anwendung selbst bringt deploy.ps1.
# Start:   ssh -t srvadmin@liebler.iui.hs-osnabrueck.de "sudo bash /tmp/fraktionperfekt-kit/schritte/01-einrichten.sh"
# Rückweg: sudo bash /tmp/fraktionperfekt-kit/schritte/01-zurueck.sh   (Daten in /var/lib/fraktionperfekt bleiben)
source "$(dirname "$0")/gemeinsam.sh"
source "$(dirname "$0")/liebler-nginx.sh"
als_root

command -v nginx >/dev/null && command -v sqlite3 >/dev/null && command -v htpasswd >/dev/null \
    || fehler "nginx, sqlite3 oder htpasswd fehlen – erst das VibeVHS-Kit (Schritt 02/03) einspielen. Nichts geändert."
[[ -f "$NGINX_SITE" ]] || fehler "$NGINX_SITE fehlt (VibeVHS-Schritt 03). Nichts geändert."

# Zeitzone: Die Anwendung rechnet mit der Zeitzone des Mandanten (Europe/Berlin) und speichert UTC – die Zeitzone des Servers
# spielt keine Rolle, die Uhr muss aber stimmen.
if timedatectl show -p NTPSynchronized --value 2>/dev/null | grep -q yes; then ok "Serveruhr synchronisiert"; else warn "Serveruhr nicht per NTP synchronisiert (timedatectl)"; fi

id "$DIENST" >/dev/null 2>&1 || { useradd --system --home-dir "$DATEN" --shell /usr/sbin/nologin "$DIENST"; ok "Dienstbenutzer $DIENST angelegt"; }
install -d -o root      -g root      -m 0755 "$PROGRAMM"
install -d -o "$DIENST" -g "$DIENST" -m 0750 "$DATEN" "$DATEN/dateien" "$LOGS" "$SICHERUNGEN"
install -d -o "$DIENST" -g "$DIENST" -m 0700 "$DATEN/schluessel"   # Data-Protection-Schlüssel (Anmelde-Cookies)
install -d -o root      -g "$DIENST" -m 0750 "/etc/$DIENST"
if [[ ! -f "$UMGEBUNG" ]]; then
    install -o root -g "$DIENST" -m 0640 "$KIT/fraktionperfekt.env.beispiel" "$UMGEBUNG"
    ok "$UMGEBUNG angelegt (Schalter und Geheimnisse dort eintragen, nie ins Repository)"
fi

# systemd: Dienst und nächtliche Sicherung
neu=0
if vorlage_einspielen systemd/fraktionperfekt.service "/etc/systemd/system/$DIENST.service"; then neu=1; fi
if vorlage_einspielen systemd/fraktionperfekt-sicherung.service "/etc/systemd/system/$DIENST-sicherung.service"; then neu=1; fi
if vorlage_einspielen systemd/fraktionperfekt-sicherung.timer "/etc/systemd/system/$DIENST-sicherung.timer"; then neu=1; fi
vorlage_einspielen werkzeuge/sicherung.sh "/usr/local/sbin/$DIENST-sicherung.sh" 0755 || true
if [[ $neu -eq 1 ]]; then systemctl daemon-reload; fi
systemctl enable "$DIENST" >/dev/null 2>&1
systemctl enable --now "$DIENST-sicherung.timer" >/dev/null 2>&1 && ok "nächtliche Sicherung aktiv (01:45 Uhr → $SICHERUNGEN)"

# Zugangspasswort (nginx Basic Auth) – solange es keine echte Anmeldung gibt, Pflicht
if [[ ! -s "$HTPASSWD" ]]; then
    info "Zugangspasswort für $PFAD anlegen (Benutzer $ZUGANG_BENUTZER) – htpasswd fragt nach dem Passwort:"
    htpasswd -c -B "$HTPASSWD" "$ZUGANG_BENUTZER"
    chown root:www-data "$HTPASSWD"
    chmod 0640 "$HTPASSWD"
    ok "$HTPASSWD angelegt"
fi

# nginx: Snippets nach /etc/nginx/snippets, Einbindung als eigene Datei /etc/nginx/liebler.d/$DIENST.conf
# (gemeinsames Schema mit VibeVHS und Podium, schritte/liebler-nginx.sh): die Einträge anderer Apps bleiben unberührt,
# eine frühere include-Zeile direkt in liebler.conf wird nach liebler.d/ umgezogen; bei Fehler (nginx -t) zurück.
vorlage_einspielen nginx/fraktionperfekt-proxy.conf "/etc/nginx/snippets/$DIENST-proxy.conf" || true
vorlage_einspielen nginx/fraktionperfekt-locations.conf "/etc/nginx/snippets/$DIENST-locations.conf" || true
vorlage_einspielen nginx/fraktionperfekt-ohne-passwort.conf "/etc/nginx/snippets/$DIENST-ohne-passwort.conf" || true
eintrag=$(mktemp)
printf '# FraktionPerfekt – Ziel: %s/%s.conf (Kit fraktionperfekt, Schritt 01)\ninclude /etc/nginx/snippets/%s-locations.conf;\n' "$LIEBLER_D" "$DIENST" "$DIENST" > "$eintrag"
liebler_app_setzen "$DIENST" "$eintrag"
rm -f "$eintrag"

if ! grep -qE '^\s*Anmeldung__TestanmeldungErlaubt=true' "$UMGEBUNG"; then
    warn "In $UMGEBUNG ist Anmeldung__TestanmeldungErlaubt noch nicht gesetzt – ohne diese bewusste Entscheidung startet die Anwendung nicht."
    warn "Für Konten zur Testanmeldung außerdem Demodaten__Anlegen=true (siehe docs/deploy.md)."
fi

if [[ -x "$PROGRAMM/FraktionPerfekt.Web" ]]; then
    systemctl restart "$DIENST"
    if gesund 60; then ok "FraktionPerfekt läuft: $GESUND → ok"; else journalctl -u "$DIENST" -n 30 --no-pager | sed 's/^/   /'; fehler "FraktionPerfekt antwortet nicht"; fi
else
    warn "Noch keine Anwendung in $PROGRAMM – jetzt vom Entwicklungsrechner ausrollen:  .\\deploy\\server\\deploy.ps1"
fi
ok "Schritt 01 fertig · https://$HOST$PFAD/ (Zugangspasswort: Benutzer $ZUGANG_BENUTZER)"
