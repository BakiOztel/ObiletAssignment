namespace BusJourney.Application.Common;

public static class TimeProviderExtensions
{
    // All journeys are in Turkey, so "today" is always evaluated in Turkish time, regardless of the server's time zone.
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateOnly GetTurkeyToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TurkeyTimeZone).DateTime);

    /// <summary>Current Turkish local time, as sent in the provider request envelope.</summary>
    public static DateTime GetTurkeyNow(this TimeProvider timeProvider) =>
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TurkeyTimeZone).DateTime;
}
