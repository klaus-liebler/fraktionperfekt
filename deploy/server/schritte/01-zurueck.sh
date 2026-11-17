#!/usr/bin/env bash
# Rückweg zu Schritt 01: Dienst und Sicherungs-Timer aus, nginx-Einbindung entfernen. Daten, Sicherungen, Umgebung und
# Passwortdatei bleiben (Löschen nur bewusst von Hand, siehe docs/deploy.md).
source "$(dirname "$0")/gemeinsam.sh"
source "$(dirname "$0")/liebler-nginx.sh"
als_root
systemctl disable --now "$DIENST" "$DIENST-sicherung.timer" 2>/dev/null || true
# frühere Einbindung direkt in liebler.conf (vor liebler.d) ebenfalls entfernen
if grep -qF "include /etc/nginx/snippets/$DIENST-locations.conf;" "$NGINX_SITE"; then
    sichern "$NGINX_SITE"
    grep -vF -e "# ---------- FraktionPerfekt (Kit fraktionperfekt) ----------" -e "include /etc/nginx/snippets/$DIENST-locations.conf;" \
        "$NGINX_SITE" > "$NGINX_SITE.neu" && mv "$NGINX_SITE.neu" "$NGINX_SITE"
fi
liebler_app_entfernen "$DIENST"
ok "FraktionPerfekt ist aus. Daten unverändert in $DATEN, Sicherungen in $SICHERUNGEN."
