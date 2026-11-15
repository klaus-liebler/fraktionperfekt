using FraktionPerfekt.Application;
using FraktionPerfekt.Application.Wissen;
using FraktionPerfekt.Domain.Finanzen;
using FraktionPerfekt.Domain.Wahlkampf;
using Person = FraktionPerfekt.Domain.Personen.Person;

namespace FraktionPerfekt.Adapters.Mocks.Testdaten;

/// <summary>Plakate in Greven (Standorte an öffentlichen Straßen, ungefähre Koordinaten), Gebiete in den Ortsteilen, Schichten, Kassen.</summary>
internal static class WahlkampfDemodaten
{
    /// <summary>Kleinstes gültiges PNG (1×1 Pixel) als Platzhalter für Fotos und Belege.</summary>
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static async Task AnlegenAsync(IDaten daten, IDateiablage ablage, IReadOnlyDictionary<string, Person> personen, DateOnly heute, DateTimeOffset jetzt,
        CancellationToken abbruch)
    {
        Guid P(string k) => personen[k].Id;

        var plakate = new (double B, double L, string Ort, Plakatart Art, Plakatstatus Status, string Von)[]
        {
            (52.09190, 7.60860, "Marktplatz", Plakatart.Grossflaeche, Plakatstatus.Haengt, "fbaeumer"),
            (52.09310, 7.60690, "Rathausstraße / Ecke Kirchstraße", Plakatart.Hohlkammer, Plakatstatus.Haengt, "fbaeumer"),
            (52.09650, 7.61590, "Bahnhof Greven, Vorplatz", Plakatart.Hohlkammer, Plakatstatus.Haengt, "nfeldhaus"),
            (52.09250, 7.60450, "Emsbrücke", Plakatart.Dreieck, Plakatstatus.Beschaedigt, "nfeldhaus"),
            (52.08870, 7.61200, "Saerbecker Straße", Plakatart.Hohlkammer, Plakatstatus.Haengt, "fbaeumer"),
            (52.09480, 7.60200, "Münsterstraße, Kreisverkehr", Plakatart.Hohlkammer, Plakatstatus.Geplant, "mkock"),
            (52.08600, 7.60100, "Schulzentrum, Zufahrt", Plakatart.Dreieck, Plakatstatus.Abgehaengt, "fbaeumer"),
            (52.13940, 7.62960, "Reckenfeld, Wittlerdamm", Plakatart.Hohlkammer, Plakatstatus.Haengt, "nfeldhaus"),
        };
        var i = 0;
        foreach (var x in plakate)
        {
            var p = new Plakat(x.B, x.L, x.Ort, x.Art, x.Status, P(x.Von), jetzt.AddDays(-10 + i++), "Landtagswahl (Demo)");
            if (x.Status is Plakatstatus.Haengt or Plakatstatus.Beschaedigt && i % 2 == 1)
            {
                var schluessel = $"Wahlkampf/Plakate/{p.Id:N}-demo.png";
                using var strom = new MemoryStream(Png);
                await ablage.SpeichernAsync(schluessel, strom, abbruch);
                p.FotoSetzen(schluessel, "image/png");
            }

            daten.Hinzufuegen(p);
        }

        var innenstadt = new Gebiet("Greven-Innenstadt", Gebietsart.Plakatieren, "Marktplatz, Rathausstraße, Bahnhofstraße", 52.0919, 7.6086);
        innenstadt.Zuteilen(P("fbaeumer"));
        var reckenfeld = new Gebiet("Reckenfeld", Gebietsart.Plakatieren, "Wittlerdamm, Grevener Landstraße", 52.1394, 7.6296);
        reckenfeld.Zuteilen(P("nfeldhaus"));
        reckenfeld.Erledigen(jetzt.AddDays(-2), "12 Plakate hängen");
        var gimbte = new Gebiet("Gimbte", Gebietsart.Flyer, "Dorfkern, ca. 300 Haushalte", null, null);
        var schmedehausen = new Gebiet("Schmedehausen", Gebietsart.Haustuer, "Haustürbesuche mit Ratsmitglied", null, null);
        schmedehausen.Zuteilen(P("dschulze"));
        foreach (var g in new[] { innenstadt, reckenfeld, gimbte, schmedehausen })
        {
            daten.Hinzufuegen(g);
        }

        for (var w = 0; w < 3; w++)
        {
            var samstag = heute.AddDays(((int)DayOfWeek.Saturday - (int)heute.DayOfWeek + 7) % 7 + 7 * w);
            var vormittag = new Schicht("Infostand Marktplatz", samstag.ToDateTime(new TimeOnly(9, 0)), samstag.ToDateTime(new TimeOnly(11, 0)), 3, null);
            var mittag = new Schicht("Infostand Marktplatz", samstag.ToDateTime(new TimeOnly(11, 0)), samstag.ToDateTime(new TimeOnly(13, 0)), 3, null);
            daten.Hinzufuegen(vormittag);
            daten.Hinzufuegen(mittag);
            if (w == 0)
            {
                daten.Hinzufuegen(new Schichteintrag(vormittag.Id, P("fbaeumer"), jetzt));
                daten.Hinzufuegen(new Schichteintrag(vormittag.Id, P("tbrinkmann"), jetzt));
                daten.Hinzufuegen(new Schichteintrag(mittag.Id, P("nfeldhaus"), jetzt));
            }
        }

        // Finanzen: Fraktionskasse (Zuwendung der Stadt) und Wahlkampfkasse, Beträge fiktiv
        var jahr = heute.Year;
        var kassier = P("mterhaar");
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
        var kaffee = new Auslage(P("awewers"), Kasse.Fraktion, heute.AddDays(-8), 18.50m, "Kaffee und Kekse Arbeitskreis", sach.Id, jetzt.AddDays(-8));
        kaffee.BelegSetzen(belegSchluessel, "kassenbon.png", "image/png");
        kaffee.Entscheiden(true, kassier, null);
        daten.Hinzufuegen(kabel);
        daten.Hinzufuegen(kaffee);
        daten.Hinzufuegen(new Domain.Aufgaben.Aufgabe($"Auslage prüfen: {kabel.Zweck} (23,90 €)", null, Domain.Personen.Rolle.Kassier, "Auslage", kabel.Id, "finanzen",
            jetzt.AddDays(-3), jetzt.AddDays(4)));
        daten.Hinzufuegen(new Domain.Aufgaben.Aufgabe($"Auslage prüfen: {kaffee.Zweck} (18,50 €)", null, Domain.Personen.Rolle.Kassier, "Auslage", kaffee.Id, "finanzen",
            jetzt.AddDays(-8), jetzt.AddDays(-1)));
        await daten.SpeichernAsync(abbruch);
    }
}
