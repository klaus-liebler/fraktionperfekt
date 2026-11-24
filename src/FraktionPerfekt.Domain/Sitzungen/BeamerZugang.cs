using System.Security.Cryptography;
using FraktionPerfekt.Domain.Gemeinsam;
using FraktionPerfekt.Domain.Termine;

namespace FraktionPerfekt.Domain.Sitzungen;

/// <summary>
/// Geheime Adresse für den Beamer einer Sitzung (aus SchoolCraft „Info-Monitore“ übertragen): Der Beamer zeigt ohne Anmeldung Tagesordnung, laufenden
/// TOP und Abstimmungsergebnisse. Gespeichert wird nur der Prüfwert (SHA-256) des Tokens; ein neuer Link sperrt den alten; gültig bis zum Ende des
/// Sitzungstags (spätestens), sperrbar.
/// </summary>
public sealed class BeamerZugang
{
    private BeamerZugang()
    {
        TokenHash = string.Empty;
    }

    private BeamerZugang(Guid sitzungId, string tokenHash, Guid erstelltVonPersonId, DateTimeOffset erstelltAm, DateTimeOffset gueltigBis)
    {
        Id = Ids.Neu();
        SitzungId = sitzungId;
        TokenHash = tokenHash;
        ErstelltVonPersonId = erstelltVonPersonId;
        ErstelltAm = erstelltAm;
        GueltigBis = gueltigBis;
    }

    public Guid Id { get; private set; }

    public Guid SitzungId { get; private set; }

    /// <summary>SHA-256 des Tokens (hex); das Token selbst steht nur in der Adresse, die einmal angezeigt wird.</summary>
    public string TokenHash { get; private set; }

    public Guid ErstelltVonPersonId { get; private set; }

    public DateTimeOffset ErstelltAm { get; private set; }

    public DateTimeOffset GueltigBis { get; private set; }

    public DateTimeOffset? GesperrtAm { get; private set; }

    public bool Gueltig(DateTimeOffset jetzt) => GesperrtAm is null && jetzt < GueltigBis;

    public static (BeamerZugang Zugang, string Token) Erzeugen(Guid sitzungId, Guid personId, DateTimeOffset jetzt, DateTimeOffset gueltigBis)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (new BeamerZugang(sitzungId, KalenderAbo.Hash(token), personId, jetzt, gueltigBis), token);
    }

    public void Sperren(DateTimeOffset jetzt) => GesperrtAm ??= jetzt;
}
