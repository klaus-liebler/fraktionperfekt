using System.Data.Common;
using FraktionPerfekt.Domain.Personen;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FraktionPerfekt.Infrastructure.Datenbank;

/// <summary>
/// SQLite-Collation „DIN5007_2“ für Namenslisten (DIN 5007-2: ä = ae, ö = oe, ü = ue, ß = ss, ohne Groß-/Kleinschreibung). Es gibt
/// keinen gespeicherten Sortierschlüssel; Spalten und Index der Namen tragen diese Collation, sortiert wird in SQL.
/// Die Collation muss auf jeder Verbindung registriert sein – zentral über <see cref="Abfangen"/> (alle DbContexte, Tests, Migrationen)
/// und <see cref="Registrieren"/> (eigene Verbindungen, z. B. Sicherungstest). Externe Werkzeuge kennen sie nicht (siehe Doku).
/// </summary>
public static class Din5007Kollation
{
    public const string Name = "DIN5007_2";

    public static int Vergleichen(string? a, string? b) => string.CompareOrdinal(PersonName.Normalisieren(a), PersonName.Normalisieren(b));

    public static void Registrieren(SqliteConnection verbindung)
    {
        ArgumentNullException.ThrowIfNull(verbindung);
        verbindung.CreateCollation(Name, Vergleichen);
    }

    /// <summary>Registriert die Collation bei jedem Öffnen einer Verbindung eines DbContext.</summary>
    public static DbConnectionInterceptor Abfangen { get; } = new Abfaenger();

    private sealed class Abfaenger : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (connection is SqliteConnection s)
            {
                Registrieren(s);
            }
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            ConnectionOpened(connection, eventData);
            return Task.CompletedTask;
        }
    }
}
