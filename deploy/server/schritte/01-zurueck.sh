#!/usr/bin/env bash
# Rückweg zu Schritt 01: Dienst und Sicherungs-Timer aus, nginx-Einbindung entfernen. Daten, Sicherungen, Umgebung und
# Passwortdatei bleiben (Löschen nur bewusst von Hand, siehe docs/deploy.md).
source "$(dirname "$0")/gemeinsam.sh"
als_root
systemctl disable --now "$DIENST" "$DIENST-sicherung.timer" 2>/dev/null || true
sichern "$NGINX_SITE"
grep -vF -e "# ---------- FraktionPerfekt (Kit fraktionperfekt) ----------" -e "include /etc/nginx/snippets/$DIENST-locations.conf;" \
    "$NGINX_SITE" > "$NGINX_SITE.neu" && mv "$NGINX_SITE.neu" "$NGINX_SITE"
if nginx -t && systemctl reload nginx; then ok "nginx ohne FraktionPerfekt neu geladen"; else fehler "nginx -t fehlgeschlagen – Sicherung: $SICHERUNG"; fi
ok "FraktionPerfekt ist aus. Daten unverändert in $DATEN, Sicherungen in $SICHERUNGEN."
