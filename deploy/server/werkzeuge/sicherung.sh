#!/usr/bin/env bash
# Online-Sicherung der FraktionPerfekt-Datenbank mit sqlite3 .backup (konsistent auch während der Dienst läuft, WAL-Modus),
# Plausibilitätsprüfung, Aufbewahrung 14 Tage. Ziel: /usr/local/sbin/fraktionperfekt-sicherung.sh (Schritt 01).
# Aufruf: fraktionperfekt-sicherung.sh [nacht|vor-update]   → /var/backups/fraktionperfekt/<art>-<Zeit>.db
# Hinweis: Die Namensspalten nutzen die Collation DIN5007_2, die nur die Anwendung kennt. Die sqlite3-Kommandozeile kann trotzdem
# sichern und lesen; Abfragen, die nach Namen sortieren/vergleichen, oder „PRAGMA integrity_check“ melden dort „no such collation“.
# Lesen von Hand daher über die Sichten v_… (COLLATE BINARY), z. B.: sqlite3 -readonly <db> "SELECT * FROM v_PersonNamen LIMIT 5"
set -euo pipefail
DIENST="fraktionperfekt"
DB="/var/lib/$DIENST/fraktionperfekt.db"
ZIEL="/var/backups/$DIENST"
art="${1:-nacht}"
[[ -f "$DB" ]] || { echo "keine Datenbank: $DB"; exit 0; }
install -d -o "$DIENST" -g "$DIENST" -m 0750 "$ZIEL"
datei="$ZIEL/$art-$(date +%Y%m%d-%H%M%S).db"
sqlite3 "$DB" ".backup '$datei'"
# Plausibel? Migrationstabelle lesbar und nicht leer (ohne Collation-Abhängigkeit)
n=$(sqlite3 -readonly "$datei" "SELECT count(*) FROM __EFMigrationsHistory;")
[[ "$n" -ge 1 ]] || { echo "Sicherung unplausibel: $datei"; exit 1; }
chown "$DIENST:$DIENST" "$datei"; chmod 0640 "$datei"
find "$ZIEL" -maxdepth 1 -name 'nacht-*.db' -mtime +14 -delete
find "$ZIEL" -maxdepth 1 -name 'vor-update-*.db' -mtime +30 -delete
echo "$datei"
