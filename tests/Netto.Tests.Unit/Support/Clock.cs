namespace Netto.Tests.Support;

// The browser's clock as the tests stand in for it: always the instant given, in UTC, for the date a page
// is drawn on.
internal sealed class Clock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
