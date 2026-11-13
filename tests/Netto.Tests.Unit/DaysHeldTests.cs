using System.Globalization;
using Netto.Core;
using Shouldly;

namespace Netto.Tests;

// The calendar days something is at the director's disposal in an income year, as the SPF's FAQ on the
// company car counts them (question 4): the first day counted, the day it is given back not. The 2020 cases
// are the FAQ's own spans and day counts; the 2026 cases are derived the same way on a year of 365 days.
public class DaysHeldTests
{
    [Fact]
    public void Of_noDates_isTheWholeYear()
    {
        var (held, fault) = DaysHeld.Of(2026, null, null);

        (held, fault).ShouldBe((new DaysHeld(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 365, 365), null));
        held.ShouldNotBeNull().AllYear.ShouldBeTrue();
    }

    // The FAQ's example 1: held from 17.9.2020, 106 days of the leap year's 366.
    [Fact]
    public void Of_faqFromADate_countsTheFirstDay()
    {
        DaysHeld.Of(2020, new DateOnly(2020, 9, 17), null).Value
            .ShouldBe(new DaysHeld(new DateOnly(2020, 9, 17), new DateOnly(2020, 12, 31), 106, 366));
    }

    // The FAQ's example 2: given back on 8.8.2020, held to 7.8, 121 + 99 = 220 days.
    [Fact]
    public void Of_faqReturnedOnADate_stopsTheDayBefore()
    {
        DaysHeld.Of(2020, null, new DateOnly(2020, 8, 8)).Value
            .ShouldBe(new DaysHeld(new DateOnly(2020, 1, 1), new DateOnly(2020, 8, 7), 220, 366));
    }

    // The FAQ's example 3: from 1.3.2020, given back on 31.3.2020, 30 days.
    [Fact]
    public void Of_faqFromAndReturned_countsTheDaysBetween()
    {
        DaysHeld.Of(2020, new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 31)).Value
            .ShouldBe(new DaysHeld(new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 30), 30, 366));
    }

    // Held since before the year and given back after it: the whole year.
    [Fact]
    public void Of_datesOutsideTheYear_areClippedToIt()
    {
        DaysHeld.Of(2026, new DateOnly(2024, 3, 3), new DateOnly(2028, 6, 1)).Value
            .ShouldBe(new DaysHeld(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 365, 365));
    }

    // Given back on 1 January of the year after: held to its last day.
    [Fact]
    public void Of_returnedOnTheFirstDayOfTheNextYear_isHeldToTheYearsEnd()
    {
        DaysHeld.Of(2026, new DateOnly(2026, 7, 1), new DateOnly(2027, 1, 1)).Value.ShouldNotBeNull().Days.ShouldBe(184);
    }

    [Theory]
    [InlineData("2027-01-01", null, DaysHeldFault.StartsAfterYear)]
    [InlineData(null, "2026-01-01", DaysHeldFault.ReturnedBeforeYear)]
    [InlineData(null, "2025-11-30", DaysHeldFault.ReturnedBeforeYear)]
    [InlineData("2026-05-01", "2026-05-01", DaysHeldFault.ReturnedBeforeStart)]
    [InlineData("2026-05-01", "2026-04-01", DaysHeldFault.ReturnedBeforeStart)]
    [InlineData("2027-02-01", "2027-01-15", DaysHeldFault.StartsAfterYear)]
    [InlineData("2025-06-01", "2025-12-01", DaysHeldFault.ReturnedBeforeYear)]
    public void Of_noDayOfTheYearHeld_isAFault(string? from, string? returnedOn, DaysHeldFault expected)
    {
        DaysHeld.Of(2026, Date(from), Date(returnedOn)).ShouldBe((null, expected));
    }

    // The days a part of the year shares with the span held: none outside it.
    [Theory]
    [InlineData("2026-09-01", "2026-09-30", 14)]
    [InlineData("2026-10-01", "2026-10-31", 31)]
    [InlineData("2026-08-01", "2026-08-31", 0)]
    public void Within_aPart_countsTheDaysItShares(string from, string to, int expected)
    {
        var held = DaysHeld.Of(2026, new DateOnly(2026, 9, 17), null).Value.ShouldNotBeNull();

        held.Within(Day(from), Day(to)).ShouldBe(expected);
    }

    // A yearly amount for the days held, unrounded: 1,360 × 30/366 is the FAQ's 111.48 once rounded.
    [Fact]
    public void Prorate_yearlyAmount_isItsShareOfTheDaysHeld()
    {
        var held = DaysHeld.Of(2020, new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 31)).Value.ShouldNotBeNull();

        (Money.RoundToCent(held.Prorate(1_360m)), DaysHeld.Of(2026, null, null).Value.ShouldNotBeNull().Prorate(1_690m)).ShouldBe((111.48m, 1_690m));
    }

    private static DateOnly? Date(string? text) => text is null ? null : Day(text);

    private static DateOnly Day(string text) => DateOnly.Parse(text, CultureInfo.InvariantCulture);
}
