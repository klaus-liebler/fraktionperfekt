# Personendaten mit zeitlicher Gültigkeit (nach VibeVHS)

## Was VibeVHS macht

- `Person` ist im Kern eine **Identität** (Id, Anlage, Benutzerkennung). Alle Stammdaten sind **bitemporal versioniert**
  (`BitemporaleVersion`: *gültig von/bis* = fachliche Gültigkeit, *erfasst/ersetzt am* = Wissensstand):
  `PersonName` (Vorname, Nachname), `PersonMerkmal` (Geburtsdatum, Sprache, Rollen, Beschäftigung, Anrede, Geschlecht,
  Geburtsname, Ansprache), Postadressen, Rufnummern, E-Mail-Adressen, Organisationen, Vollmachten, Einwilligungen, Qualifikationen.
- Zwei Arten von Änderungen (`ZeitlicheHistorie`): **Änderung ab Datum** (`AendernAb`, z. B. Heirat – vorher bleibt es) und
  **Korrektur** (`Korrigieren`, Tippfehler – die am Datum gültige Version wird für ihren ganzen Zeitraum ersetzt; die alte bleibt
  mit früherem Wissensstand abrufbar). Dazu **Beenden ab** (Endemarke).
- **Stichtag**: `StandAm(gültigAm, bekanntAm)` liefert den ganzen Datensatz, wie er an einem Tag galt und zu einem Zeitpunkt bekannt
  war. `NameAm(datum)` wird in Dokumenten benutzt (Name zum damaligen Stand).
- **Zwischenstand**: Anzeigename, E-Mail, Rollen usw. als Felder an der Person für Listen und Suche, nach jeder Änderung neu berechnet.
- Oberfläche: Personendetail mit Formular „Änderung ab Datum“ vs. „Korrektur (Tippfehler)“, Seite **Datenhistorie** mit Stichtag
  („Wie war der Stand am …, was war am … bekannt?“) und Zeitleiste aller Versionen, ersetzte Erfassungen ausblendbar.
- Titel ist in VibeVHS *nicht* versioniert (Feld an der Person).

## Was FraktionPerfekt übernimmt

- Dieselben Bausteine (`BitemporaleVersion`, `ZeitlicheHistorie`, `Erfassung` mit Art und Grund).
- `Person` = Identität (Id, Anmeldename, RIS-Kennung, Anlage). Versioniert:
  - `PersonName`: Vorname, Nachname **und Titel** (auf Klaus' Wunsch: alle Namensbestandteile zeitlich),
  - `PersonMerkmal`: Anrede, Geschlecht, **Funktion in der Fraktion** (z. B. „Fraktionsvorsitzender“),
  - Kontakte wie VibeVHS: `Postadresse` (privat/dienstlich, Zusatz, „bevorzugt“ = für Briefe), `EMailAdresse` (privat/dienstlich,
    „bevorzugt“ = Mails der Fraktion), `Rufnummer` (mobil/Festnetz/dienstlich, Messaging-Dienste, Einwilligung Messaging, „bevorzugt“) –
    je mehrere, jede mit fester Id, Änderung ab Datum, Korrektur und Aufgabe ab Datum (Umzug); ein neuer „bevorzugter“ Kontakt nimmt
    den anderen das Kennzeichen ab demselben Tag,
  - `Einwilligung` je Zweck: *Kontaktdaten in der Fraktion weitergeben*, *Newsletter und Informationen*, *Fotos veröffentlichen*
    (VibeVHS: Kursorganisation, Werbung, Fotos; die Kanal-Dimension entfällt). Eine Geburtstagsliste gibt es in VibeVHS nicht – daher auch hier nicht,
  - `PersonRollen` (Rechte-Rollen, schon vorher bitemporal),
  - Gremienmitgliedschaften (`Mitgliedschaft`, schon vorher bitemporal, mit „Seit“ aus dem RIS).
- `StandAm(tag, bekanntAm)`, `AnzeigenameAm(tag)`, `NameAendernAb`/`NameKorrigieren`, `AngabeAendern(…, korrektur)`.
- Zwischenstand (`Vorname`, `Nachname`, `Titel`, `Anrede`, `EMail`, `Mobil`) für Listen, Suche, Anmeldung und Postausgang;
  zusätzlich ein **Nachtlauf** (Fristenprüfung „Personen-Zwischenstand“), damit künftige Änderungen am Stichtag sichtbar werden.
- Oberfläche (Personenseite): „Angaben – Stand heute“ mit Verlauf je Angabe (Uhr-Symbol: gültig von/bis, Art, erfasst am/von,
  Grund, ersetzte Erfassungen blass), **Stand an einem Stichtag**, Formular **Änderung ab Datum / Korrektur** mit Grund
  (nur Geschäftsführung).
- Alte Vorgänge zeigen Namen **zum damaligen Stand**: Anträge (Tag der Antragstellung), Sitzungen/Protokolle (Sitzungstag).
- Demodaten: Ratsmitglieder gelten ab ihrem Rat-Beginn laut RIS (z. B. Brinkhaus seit 01.06.2014, Kühlert seit 01.10.1999);
  Ämter (Vorstand, Schatzmeister, Medienbeauftragte) ab Beginn der Wahlperiode 05.11.2025, davor „Ratsmitglied“.

## Abweichungen und Gründe

- **Ansprache (Du/Sie, Stil, Titel nennen)** ist in FraktionPerfekt eine *Benutzereinstellung* ohne Historie (gilt sofort);
  VibeVHS führt sie als zeitliches Merkmal. Die *Anrede* (Herr/Frau) ist dagegen ein Personendatum und zeitlich.
- **Sichtbarkeit der Kontakte**: Adressen, Rufnummern und E-Mails sehen die Person selbst und die Geschäftsführung; alle anderen nur
  mit Einwilligung „Kontaktdaten in der Fraktion weitergeben“ (sonst auch nicht in Personenliste und Datenhistorie). Kontakte und
  Einwilligungen pflegen die Person selbst und die Geschäftsführung; Name, Anrede, Funktion und Rollen nur die Geschäftsführung.
- Statt der VibeVHS-Kennzeichen „üblicher Aufenthaltsort/Korrespondenzadresse“ genügt „bevorzugt“ an jeder Adresse.
- **Datenhistorie** (`/personen/{id}/historie`) wie VibeVHS: Stichtag *und* frei wählbarer Wissensstand („Was war bekannt am …“),
  Zeitleiste aller Erfassungen mit altem → neuem Wert, Filter je Datenart, ersetzte Erfassungen ausblendbar.
- Bisherige E-Mail-/Mobil-Merkmale überführt die Migration `PersonKontakte` in bevorzugte Kontakte.
- Demo: echte Personen bekommen keine erfundenen Adressen oder Nummern; nur die fiktiven Mitarbeitenden (Albers mit Umzug, Bäumer, Feldhaus).
- Bestehende Datenbanken: Personen ohne Versionen behalten ihren Zwischenstand; für die Demo wird die Datenbank neu aufgebaut.
