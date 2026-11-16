# Wiki-Editor und Bilder

**Editor:** TOAST UI Editor 3.2.2 (MIT), lokal unter `wwwroot/lib/toastui-editor` (kein CDN, Lizenzen in `LICENSE.txt`,
Bauanleitung in `tools/Editor/README.md`). Markdown- und WYSIWYG-Modus umschaltbar, Vorschau, Tabellen, Aufgabenlisten,
deutsche Oberfläche. Ohne JavaScript bleibt ein einfaches Textfeld. `[[Seitentitel]]` funktioniert weiter (auch nach dem
WYSIWYG-Modus). Gewählt statt EasyMDE wegen des WYSIWYG-Modus für Mitglieder ohne Markdown-Kenntnisse und des eingebauten
Bild-Hooks; er ist CSP-konform (keine eingebetteten Skripte, keine Style-Elemente, `usageStatistics` aus).

**Darstellung:** serverseitig mit Markdig. Roh-HTML wird nie übernommen, Links nur `http`, `https`, `mailto` oder relativ,
Bilder nur aus der eigenen Ablage (`dateien/bild/{guid}`). Überschriften rücken eine Ebene ein (`#` → h2).

**Bilder einfügen:** Strg+V (Zwischenablage), Hineinziehen oder Bild-Knopf in der Werkzeugleiste.

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

**Offen:** Den Editor auch für Anträge und Protokoll-Notizen nutzen (die Komponente `MarkdownEditor` ist wiederverwendbar;
für Bilder dort fehlen Bezug und Rechteprüfung „Antrag“/„Sitzung“ im `BildDienst`). Autovervollständigung für `[[…]]`.
Serverseitiges Verkleinern übernimmt bisher der Browser (keine Bildbibliothek in der erlaubten Paketliste).
