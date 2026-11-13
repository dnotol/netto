namespace Netto.Core;

// The calendar days of an income year something is at the director's disposal, from First to Last, both
// counted: Days of the year's DaysInYear. A benefit valued for a whole year is reduced prorata temporis by
// these days, the first day of disposal counted and the day it is given back not (the SPF's FAQ on the
// company car, question 4). A date before the year counts from its first day, a return after it to its last.
public sealed record DaysHeld(DateOnly First, DateOnly Last, int Days, int DaysInYear)
{
    // Held all year.
    public bool AllYear => Days == DaysInYear;

    // The span held in the income year, from the day given (none: the year's first) to the day before the one
    // it is given back on (none: the year's last), or why no day of the year is held.
    public static (DaysHeld? Value, DaysHeldFault? Fault) Of(int incomeYear, DateOnly? from, DateOnly? returnedOn)
    {
        var (yearStart, yearEnd) = (new DateOnly(incomeYear, 1, 1), new DateOnly(incomeYear, 12, 31));
        if (from > yearEnd)
            return (null, DaysHeldFault.StartsAfterYear);
        if (returnedOn <= from)
            return (null, DaysHeldFault.ReturnedBeforeStart);
        if (returnedOn <= yearStart)
            return (null, DaysHeldFault.ReturnedBeforeYear);

        var first = from is { } start && start > yearStart ? start : yearStart;
        var last = returnedOn is { } back && back <= yearEnd ? back.AddDays(-1) : yearEnd;
        return (new DaysHeld(first, last, Count(first, last), Count(yearStart, yearEnd)), null);
    }

    // The days a part of the year, from its first day to its last, shares with the span held.
    public int Within(DateOnly from, DateOnly to)
    {
        var (first, last) = (from > First ? from : First, to < Last ? to : Last);
        return last < first ? 0 : Count(first, last);
    }

    // A yearly amount for the days held, unrounded: rounding is the rule's.
    public decimal Prorate(decimal yearly) => yearly * Days / DaysInYear;

    private static int Count(DateOnly first, DateOnly last) => last.DayNumber - first.DayNumber + 1;
}

// Why no day of the income year is held: the span starts after the year, is given back on or before its first
// day, or on or before the day it starts.
public enum DaysHeldFault
{
    StartsAfterYear,
    ReturnedBeforeYear,
    ReturnedBeforeStart,
}
