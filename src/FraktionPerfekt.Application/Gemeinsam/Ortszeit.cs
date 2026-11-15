namespace FraktionPerfekt.Application.Gemeinsam;

/// <summary>
/// Termine sind Wanduhrzeit in Greven. Diese Hilfen rechnen unabhängig von der Zeitzone des Servers (und der Testuhr)
/// zwischen Zeitpunkt und Ortszeit um.
/// </summary>
public static class Ortszeit
{
    public static TimeZoneInfo Zeitzone { get; } = ZeitzoneFinden();

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

    private static TimeZoneInfo ZeitzoneFinden()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
