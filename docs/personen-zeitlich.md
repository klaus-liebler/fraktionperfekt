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
  - `PersonMerkmal`: Anrede, Geschlecht, E-Mail, Mobil, **Funktion in der Fraktion** (z. B. „Fraktionsvorsitzender“),
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
- **Kontakte**: je Person eine E-Mail und eine Mobilnummer als Merkmal statt mehrerer Adressen/Rufnummern mit Einwilligungen –
  die Fraktion braucht (noch) nicht mehr. Postadressen gibt es bisher nicht.
- **Datenhistorie als eigene Seite** mit Wissensstand-Auswahl gibt es noch nicht; der Verlauf steht je Angabe auf der Personenseite,
  der Stichtag fragt den heutigen Wissensstand ab. Der Wissensstand ist im Dienst (`StandAmAsync(…, bekanntAm)`) schon abfragbar.
- Bestehende Datenbanken: Personen ohne Versionen behalten ihren Zwischenstand; für die Demo wird die Datenbank neu aufgebaut.
