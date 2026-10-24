using Shouldly;

namespace Netto.Tests.Support;

// The browser's clock as the tests stand in for it: always the instant given, in UTC.
public class ClockTests
{
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetUtcNow_always_isTheInstantGiven()
    {
        TimeProvider clock = new Clock(On23October);

        (clock.GetUtcNow(), clock.GetUtcNow()).ShouldBe((On23October, On23October));
    }

    [Fact]
    public void GetLocalNow_always_isTheSameInstantInUtc()
    {
        TimeProvider clock = new Clock(On23October);

        (clock.GetLocalNow(), clock.LocalTimeZone).ShouldBe((On23October, TimeZoneInfo.Utc));
    }
}
