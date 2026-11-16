namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Zentraler Zeitdienst: Termine sind Wanduhrzeit des Mandanten; gespeichert wird UTC. Die Zeitzone kommt aus der Mandanten-Konfiguration
/// (<see cref="Festlegen"/>), ersatzweise aus der Systemzeitzone (Server: mit timedatectl setzen). Alle Umrechnungen laufen hier.
/// Grenze der Vorbereitung: eine Zeitzone je Prozess (alle Mandanten eines Servers in derselben Zeitzone).
/// </summary>
public static class Ortszeit
{
    public static TimeZoneInfo Zeitzone { get; private set; } = ZeitzoneFinden(null);

    /// <summary>Zeitzone aus der Konfiguration (IANA-Name, z. B. „Europe/Berlin“); leer = Systemzeitzone.</summary>
    public static void Festlegen(string? zeitzone) => Zeitzone = ZeitzoneFinden(zeitzone);

    public static DateTime Jetzt(TimeProvider uhr)
    {
        ArgumentNullException.ThrowIfNull(uhr);
        return TimeZoneInfo.ConvertTime(uhr.GetUtcNow(), Zeitzone).DateTime;
    }

    public static DateOnly Heute(TimeProvider uhr) => DateOnly.FromDateTime(Jetzt(uhr));

    /// <summary>Wanduhrzeit als Zeitpunkt mit dem in Greven gültigen Versatz (Sommer-/Winterzeit).</summary>
    public static DateTimeOffset Zeitpunkt(DateTime wanduhr)
    {
        var lokal = DateTime.SpecifyKind(wanduhr, DateTimeKind.Unspecified);
        return new DateTimeOffset(lokal, Zeitzone.GetUtcOffset(lokal));
    }

    public static DateTime AlsOrtszeit(DateTimeOffset zeitpunkt) => TimeZoneInfo.ConvertTime(zeitpunkt, Zeitzone).DateTime;

    private static TimeZoneInfo ZeitzoneFinden(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return TimeZoneInfo.Local;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone
            : TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windows) && TimeZoneInfo.TryFindSystemTimeZoneById(windows, out var w) ? w
            : TimeZoneInfo.Local;
    }
}
