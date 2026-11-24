# Markdown-Editor und Bilder

**Editor:** TOAST UI Editor 3.2.2 (MIT), lokal unter `wwwroot/lib/toastui-editor` (kein CDN, Lizenzen in `LICENSE.txt`,
Bauanleitung in `tools/Editor/README.md`). Markdown- und WYSIWYG-Modus umschaltbar, Vorschau, Tabellen, Aufgabenlisten,
deutsche Oberfläche. Ohne JavaScript bleibt ein einfaches Textfeld. `[[Seitentitel]]` funktioniert weiter (auch nach dem
WYSIWYG-Modus). Gewählt statt EasyMDE wegen des WYSIWYG-Modus für Mitglieder ohne Markdown-Kenntnisse und des eingebauten
Bild-Hooks; er ist CSP-konform (keine eingebetteten Skripte, keine Style-Elemente, `usageStatistics` aus).

**Darstellung:** serverseitig mit Markdig. Roh-HTML wird nie übernommen, Links nur `http`, `https`, `mailto` oder relativ,
Bilder nur aus der eigenen Ablage (`dateien/bild/{guid}`). Überschriften rücken eine Ebene ein (`#` → h2).

**Bilder einfügen:** <kbd>Strg</kbd>+<kbd>V</kbd> (Zwischenablage), Hineinziehen oder Bild-Knopf in der Werkzeugleiste.

- Im Browser: größer als 2000 px (längste Kante) wird verkleinert, Fotos (JPEG) werden neu kodiert – EXIF/GPS fällt weg.
- Server (`POST dateien/bild`, mit Antiforgery-Token): Prüfung an der Dateisignatur (PNG, JPEG, GIF, WebP – kein SVG),
  höchstens 10 MB, Metadaten (EXIF/GPS, XMP, PNG-Textblöcke) werden entfernt.
- Speicherung über die Dateiablage (lokal oder Nextcloud) unter `Bilder/{GUID v7}.{Endung}`. Metadaten in der Tabelle
  `Bilder`: Originalname (eingefügte Bilder ohne Namen: „Eingefügt JJJJ-MM-TT HH-MM.png“), MIME-Typ, Größe, Pixelmaße,
  SHA-256, wer/wann. Gleicher Inhalt wird nur einmal gespeichert; `BildVerwendungen` hält fest, auf welchen Seiten ein Bild
  benutzt wird.
- Im Markdown steht `![Originalname](dateien/bild/{guid})`.
- Auslieferung (`GET dateien/bild/{guid}`) nur an Personen, die eine Seite mit dem Bild sehen dürfen (vertrauliche Seiten
  beachtet) oder es hochgeladen haben; `Cache-Control: private, max-age=31536000, immutable`, ETag = SHA-256.
- Mediathek: In der Seitenansicht zeigt „Bilder dieser Seite“ Vorschau, Originalname, Maße, Größe und ob das Bild noch im Text steht.
- Aufräumen: Bilder, die in keinem aktuellen Seitentext mehr vorkommen, werden nach 30 Tagen gelöscht (nächtliche Fristenprüfung).

**Wo der Editor arbeitet:** Wiki-Seiten, Anträge (neuer Antrag und neue Fassung), Anfragen an die Verwaltung (neue Anfrage),
Pressemitteilungen, Bürgeranliegen (Erfassung) und TOP-Notizen im Sitzungsmodus (Fraktions- und Ausschussprotokolle). Angezeigt
wird überall mit derselben bereinigten Markdig-Darstellung.

**Besitzer und Rechte:** Ein Bild gehört zu einem oder mehreren Besitzern (Wiki-Seite, Antrag, Sitzung, Anfrage, Pressemitteilung,
Bürgeranliegen). Abrufen darf es, wer mindestens einen davon sehen darf (Rechte des jeweiligen Moduls; vertrauliche Wiki-Seiten,
Bürgeranliegen nur für Zuständige und Leitung, Anträge nicht für das Kampagnenteam; Kampagnen und Tickets auch für das Kampagnenteam). In Formularen für Neues hängt ein Bild zuerst an
einem „Entwurf“ (nur für die hochladende Person sichtbar); beim Speichern ordnet der Dienst alle Bilder im Text dem neuen Besitzer zu.
Das gilt auch, wenn ein Bild aus einem anderen Text kopiert wird.

**Aufräumen:** über alle Besitzer; Bilder alter Antragsfassungen bleiben, solange die Fassung existiert.

**PDF:** Antrag und Protokoll werden mit Markdig nach MigraDoc übertragen (Überschriften, Listen, Tabellen, Zitate, fett/kursiv,
Links mit Adresse) – eingebettete Bilder erscheinen im PDF (höchstens Satzspiegelbreite).

**Kompakt** (kleine Werkzeugleiste, 200 px) für kurze Texte: Antwort der Verwaltung auf Anfragen, Antwort auf Bürgeranliegen,
Kurzbericht in der Terminbesetzung, Beschreibung neuer Termine. Bilder auch dort, Besitzer „Anfrage“, „Anliegen“, „Besetzung“,
„Termin“ mit den Rechten des jeweiligen Moduls. Im Kalender-Abo (ICS) steht die Beschreibung als Klartext ohne Markdown.

**Offen:** Aufgabenbeschreibungen entstehen bisher nur automatisch (kein Eingabefeld). Eine Einladung als PDF gibt es bisher nicht.
Autovervollständigung für `[[…]]`. Serverseitiges Verkleinern übernimmt bisher der Browser (keine Bildbibliothek in der erlaubten Paketliste).
