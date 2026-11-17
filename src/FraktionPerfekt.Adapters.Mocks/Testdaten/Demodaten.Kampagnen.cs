using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Kampagnen;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Zwei Kampagnen: Plakate in Greven (öffentliche Straßen, ungefähre Koordinaten), Gebiete, Termine mit ToDos, Vorlagen, Tickets; dazu die Kassen.</summary>
internal static class KampagnenDemodaten
{
    /// <summary>Kleinstes gültiges PNG (1×1 Pixel) als Platzhalter für Fotos und Belege.</summary>
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static async Task AnlegenAsync(IDaten daten, IDateiablage ablage, IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt,
        CancellationToken abbruch)
    {
        Guid P(string k) => personen[k].Id;

        // Kampagne 1: Wahlkampf mit Wahltag (Abhängliste danach); Kampagne 2: kleine Haustüraktion in Planung
        var wahl = new Kampagne("Kommunalwahl 2027 (Demo)", "Plakate, Infostände und Haustürbesuche in allen Ortsteilen. **Ziel:** jede Straße einmal besucht.",
            heute.AddDays(-30), new DateOnly(2027, 5, 16), new DateOnly(2027, 5, 16), P("kalbers"), jetzt.AddDays(-30));
        wahl.StatusSetzen(Kampagnenstatus.Aktiv);
        var schulen = new Kampagne("Haustüraktion Grundschulen (Demo)", "Gespräche mit Eltern zur Grundschulplanung, Flyer in den Schulbezirken.",
            heute.AddDays(30), heute.AddDays(75), null, P("kalbers"), jetzt.AddDays(-5));
        daten.Hinzufuegen(wahl);
        daten.Hinzufuegen(schulen);

        var plakate = new (double B, double L, string Ort, Plakatart Art, Plakatstatus Status, string Von)[]
        {
            (52.09190, 7.60860, "Marktplatz", Plakatart.Grossflaeche, Plakatstatus.Haengt, "fbaeumer"),
            (52.09310, 7.60690, "Rathausstraße / Ecke Kirchstraße", Plakatart.Hohlkammer, Plakatstatus.Haengt, "fbaeumer"),
            (52.09650, 7.61590, "Bahnhof Greven, Vorplatz", Plakatart.Hohlkammer, Plakatstatus.Haengt, "nfeldhaus"),
            (52.09250, 7.60450, "Emsbrücke", Plakatart.Dreieck, Plakatstatus.Beschaedigt, "nfeldhaus"),
            (52.08870, 7.61200, "Saerbecker Straße", Plakatart.Hohlkammer, Plakatstatus.Haengt, "fbaeumer"),
            (52.09480, 7.60200, "Münsterstraße, Kreisverkehr", Plakatart.Hohlkammer, Plakatstatus.Geplant, "seisenrichter"),
            (52.08600, 7.60100, "Schulzentrum, Zufahrt", Plakatart.Dreieck, Plakatstatus.Abgehaengt, "fbaeumer"),
            (52.13940, 7.62960, "Reckenfeld, Wittlerdamm", Plakatart.Hohlkammer, Plakatstatus.Haengt, "nfeldhaus"),
        };
        var i = 0;
        Plakat? kaputt = null;
        foreach (var x in plakate)
        {
            var p = new Plakat(wahl.Id, x.B, x.L, x.Ort, x.Art, x.Status, P(x.Von), jetzt.AddDays(-10 + i++), "Kommunalwahl (Demo)");
            if (x.Status is Plakatstatus.Haengt or Plakatstatus.Beschaedigt && i % 2 == 1)
            {
                var schluessel = $"Kampagnen/Plakate/{p.Id:N}-demo.png";
                using var strom = new MemoryStream(Png);
                await ablage.SpeichernAsync(schluessel, strom, abbruch);
                p.FotoSetzen(schluessel, "image/png");
            }

            kaputt ??= x.Status == Plakatstatus.Beschaedigt ? p : null;
            daten.Hinzufuegen(p);
        }

        var innenstadt = new Gebiet(wahl.Id, "Greven-Innenstadt", Gebietsart.Plakatieren, "Marktplatz, Rathausstraße, Bahnhofstraße", 52.0919, 7.6086);
        innenstadt.Zuteilen(P("fbaeumer"));
        var reckenfeld = new Gebiet(wahl.Id, "Reckenfeld", Gebietsart.Plakatieren, "Wittlerdamm, Grevener Landstraße", 52.1394, 7.6296);
        reckenfeld.Zuteilen(P("nfeldhaus"));
        reckenfeld.Erledigen(jetzt.AddDays(-2), "12 Plakate hängen");
        var gimbte = new Gebiet(wahl.Id, "Gimbte", Gebietsart.Flyer, "Dorfkern, ca. 300 Haushalte", null, null);
        var schmedehausen = new Gebiet(wahl.Id, "Schmedehausen", Gebietsart.Haustuer, "Haustürbesuche mit Ratsmitglied", null, null);
        schmedehausen.Zuteilen(P("mgries"));
        var marien = new Gebiet(schulen.Id, "Schulbezirk Marienschule", Gebietsart.Haustuer, "Eltern im Schulbezirk, Flyer und Gespräche", null, null);
        foreach (var g in new[] { innenstadt, reckenfeld, gimbte, schmedehausen, marien })
        {
            daten.Hinzufuegen(g);
        }

        // ToDo-Vorlagen (Kopiervorlagen)
        var infostand = new TodoVorlage("Infostand");
        var haustuer = new TodoVorlage("Haustürrunde");
        daten.Hinzufuegen(infostand);
        daten.Hinzufuegen(haustuer);
        var infostandPunkte = new[] { "Genehmigung (Sondernutzung) prüfen", "Pavillon und Tisch im Fraktionsbüro abholen", "Flyer und Giveaways einpacken", "Kaffee und Becher mitbringen", "Rückbau und Müll mitnehmen" };
        for (var n = 0; n < infostandPunkte.Length; n++)
        {
            daten.Hinzufuegen(new TodoVorlagenPunkt(infostand.Id, n, infostandPunkte[n]));
        }

        var haustuerPunkte = new[] { "Straßenliste ausdrucken", "Flyer einpacken (ca. 100)", "Gesprächsnotizen ins Ticket übertragen" };
        for (var n = 0; n < haustuerPunkte.Length; n++)
        {
            daten.Hinzufuegen(new TodoVorlagenPunkt(haustuer.Id, n, haustuerPunkte[n]));
        }

        // Kampagnentermine: normale Kalendertermine mit Kampagnenbezug und Plätzen; Einbuchen = Zusage
        Domain.Termine.Termin? ersterInfostand = null;
        for (var w = 0; w < 3; w++)
        {
            var samstag = heute.AddDays(((int)DayOfWeek.Saturday - (int)heute.DayOfWeek + 7) % 7 + 7 * w);
            var vormittag = new Domain.Termine.Termin("Infostand Marktplatz (Vormittag)", Domain.Termine.Terminart.Kampagne, samstag.ToDateTime(new TimeOnly(9, 0)),
                samstag.ToDateTime(new TimeOnly(11, 0)), "Marktplatz Greven", null, P("kalbers"), jetzt, "Pavillon steht vor dem Rathaus.", kampagneId: wahl.Id, plaetze: 3);
            var mittag = new Domain.Termine.Termin("Infostand Marktplatz (Mittag)", Domain.Termine.Terminart.Kampagne, samstag.ToDateTime(new TimeOnly(11, 0)),
                samstag.ToDateTime(new TimeOnly(13, 0)), "Marktplatz Greven", null, P("kalbers"), jetzt, null, kampagneId: wahl.Id, plaetze: 3);
            daten.Hinzufuegen(vormittag);
            daten.Hinzufuegen(mittag);
            if (w == 0)
            {
                ersterInfostand = vormittag;
                daten.Hinzufuegen(new Domain.Termine.Teilnahme(vormittag.Id, P("fbaeumer"), Domain.Termine.Teilnahmestatus.Zugesagt, jetzt));
                daten.Hinzufuegen(new Domain.Termine.Teilnahme(vormittag.Id, P("jwilp"), Domain.Termine.Teilnahmestatus.Zugesagt, jetzt));
                daten.Hinzufuegen(new Domain.Termine.Teilnahme(mittag.Id, P("nfeldhaus"), Domain.Termine.Teilnahmestatus.Zugesagt, jetzt));
                for (var n = 0; n < infostandPunkte.Length; n++)
                {
                    var todo = new TerminTodo(vormittag.Id, n, infostandPunkte[n], n == 1 ? P("fbaeumer") : null);
                    if (n == 0)
                    {
                        todo.Umschalten(P("kalbers"), jetzt.AddDays(-1));
                    }

                    daten.Hinzufuegen(todo);
                }
            }
        }

        // Tickets
        var nr = 0;
        Ticket NeuesTicket(Kampagne k, string titel, string text, Ticketprioritaet prio, string von, string? zustaendig, int faelligTage)
        {
            var t = new Ticket(k.Id, ++nr, titel, text, prio, P(von), jetzt.AddDays(-3));
            t.Bearbeiten(titel, text, prio, heute.AddDays(faelligTage), jetzt.AddDays(-3));
            if (zustaendig is not null)
            {
                t.Zuweisen(P(zustaendig), jetzt.AddDays(-2));
            }

            daten.Hinzufuegen(t);
            return t;
        }

        var t1 = NeuesTicket(wahl, "Plakat beschädigt: Emsbrücke", "Plakat ist eingerissen, bitte ersetzen.", Ticketprioritaet.Hoch, "nfeldhaus", "fbaeumer", 2);
        t1.Beziehen(null, kaputt?.Id, null);
        t1.StatusSetzen(Ticketstatus.InArbeit, jetzt.AddDays(-1));
        daten.Hinzufuegen(new TicketKommentar(t1.Id, P("fbaeumer"), "Neues Plakat liegt im Büro, hänge ich Samstag auf.", jetzt.AddDays(-1)));
        var t2 = NeuesTicket(wahl, "Flyer für Reckenfeld nachdrucken", "Es fehlen etwa 200 Flyer.", Ticketprioritaet.Normal, "fbaeumer", "kalbers", 7);
        t2.Beziehen(reckenfeld.Id, null, null);
        var t3 = NeuesTicket(wahl, "Zweiter Pavillon für den Infostand", "Bei Regen reicht einer nicht.", Ticketprioritaet.Niedrig, "jwilp", null, 10);
        t3.Beziehen(null, null, ersterInfostand?.Id);
        nr = 0;
        NeuesTicket(schulen, "Flyertext mit den Schulleitungen abstimmen", "Entwurf liegt im Wiki.", Ticketprioritaet.Normal, "kalbers", null, 20);

        // Finanzen: Fraktionskasse (Zuwendung der Stadt) und Wahlkampfkasse, Beträge fiktiv
        var jahr = heute.Year;
        var kassier = P("kalbers"); // Demo-Buchungen trägt die fiktive Geschäftsführung ein, keine Beträge auf echte Personen
        var personal = new Haushaltsposten(Kasse.Fraktion, jahr, "Geschäftsstelle (Personal)", 38_000m);
        var sach = new Haushaltsposten(Kasse.Fraktion, jahr, "Sachkosten und Büro", 4_500m);
        var oeff = new Haushaltsposten(Kasse.Fraktion, jahr, "Öffentlichkeitsarbeit", 3_000m);
        var fortbildung = new Haushaltsposten(Kasse.Fraktion, jahr, "Fortbildung", 1_500m);
        var plakatPosten = new Haushaltsposten(Kasse.Wahlkampf, jahr, "Plakate und Material", 2_500m);
        var flyer = new Haushaltsposten(Kasse.Wahlkampf, jahr, "Flyer und Druck", 1_800m);
        var veranst = new Haushaltsposten(Kasse.Wahlkampf, jahr, "Veranstaltungen und Infostände", 900m);
        // Testwerte mit großen Zahlen (Darstellung von Balken und Kennzahlen prüfen)
        var gross = new Haushaltsposten(Kasse.Fraktion, jahr, "Rücklage Großprojekt (Testwert)", 1_234_567.89m);
        foreach (var h in new[] { personal, sach, oeff, fortbildung, plakatPosten, flyer, veranst, gross })
        {
            daten.Hinzufuegen(h);
        }

        var am = jetzt.AddDays(-30);
        void Buchen(Kasse k, int tage, bool ein, decimal betrag, Haushaltsposten? posten, string text)
            => daten.Hinzufuegen(new Kassenbuchung(k, heute.AddDays(-tage), ein, betrag, posten?.Id, text, kassier, am));
        Buchen(Kasse.Fraktion, 300, true, 47_000m, null, "Zuwendung der Stadt für Fraktionsarbeit (fiktiv)");
        Buchen(Kasse.Fraktion, 280, false, 28_500m, personal, "Personalkosten Geschäftsstelle Jan–Sep");
        Buchen(Kasse.Fraktion, 200, false, 1_240.80m, sach, "Büromaterial und Drucker");
        Buchen(Kasse.Fraktion, 120, false, 2_950m, oeff, "Fraktionszeitung Herbst");
        Buchen(Kasse.Fraktion, 60, false, 420m, fortbildung, "Seminar Kommunalfinanzen");
        Buchen(Kasse.Fraktion, 100, true, 1_600_000m, null, "Sonderzuweisung (Testwert große Zahlen)");
        Buchen(Kasse.Fraktion, 90, false, 1_500_000m, gross, "Ausgabe Großprojekt (Testwert, überzieht den Ansatz)");
        Buchen(Kasse.Wahlkampf, 90, true, 3_000m, null, "Zuschuss Kreisverband (fiktiv)");
        Buchen(Kasse.Wahlkampf, 40, false, 1_980m, plakatPosten, "Plakatdruck 400 Stück");
        Buchen(Kasse.Wahlkampf, 20, false, 1_150m, flyer, "Flyer Landtagswahl");

        foreach (var (wer, anschrift, betrag) in new (string?, string?, decimal)[] { ("Bäckerei Musterhaus (fiktiv)", "Marktplatz 1, Greven", 500m), (null, null, 50m), ("Erika Beispiel (fiktiv)", "Beispielweg 3, Greven", 250m) })
        {
            var s = new Spende(Kasse.Wahlkampf, heute.AddDays(-15), wer, anschrift, betrag, "Wahlkampf", kassier);
            daten.Hinzufuegen(s);
            daten.Hinzufuegen(new Kassenbuchung(Kasse.Wahlkampf, s.Datum, true, betrag, null, $"Spende{(wer is null ? " (anonym)" : "")}: Wahlkampf", kassier, am, Buchungsquelle.Spende, s.Id));
        }

        var kabel = new Auslage(P("fbaeumer"), Kasse.Wahlkampf, heute.AddDays(-3), 23.90m, "Kabelbinder und Plakatdraht", plakatPosten.Id, jetzt.AddDays(-3));
        var belegSchluessel = $"Finanzen/Belege/{kabel.Id:N}-kassenbon.png";
        using (var strom = new MemoryStream(Png))
        {
            await ablage.SpeichernAsync(belegSchluessel, strom, abbruch);
        }

        kabel.BelegSetzen(belegSchluessel, "kassenbon.png", "image/png");
        var kaffee = new Auslage(P("kalbers"), Kasse.Fraktion, heute.AddDays(-8), 18.50m, "Kaffee und Kekse Arbeitskreis", sach.Id, jetzt.AddDays(-8));
        kaffee.BelegSetzen(belegSchluessel, "kassenbon.png", "image/png");
        kaffee.Entscheiden(true, P("djoud"), null); // Freigabe durch den Schatzmeister (Rolle), Betrag gehört der fiktiven Geschäftsführung
        daten.Hinzufuegen(kabel);
        daten.Hinzufuegen(kaffee);
        daten.Hinzufuegen(new Domain.Aufgaben.Aufgabe($"Auslage prüfen: {kabel.Zweck} (23,90 €)", null, Domain.Personen.Rolle.Kassier, "Auslage", kabel.Id, "finanzen",
            jetzt.AddDays(-3), jetzt.AddDays(4)));
        daten.Hinzufuegen(new Domain.Aufgaben.Aufgabe($"Auslage prüfen: {kaffee.Zweck} (18,50 €)", null, Domain.Personen.Rolle.Kassier, "Auslage", kaffee.Id, "finanzen",
            jetzt.AddDays(-8), jetzt.AddDays(-1)));
        await daten.SpeichernAsync(abbruch);
    }
}
