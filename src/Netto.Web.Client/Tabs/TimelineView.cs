using Netto.Core.Calendar;

namespace Netto.Web.Client.Tabs;

// The income year drawn as a timeline, from the engine's PaymentCalendar: the company's and the director's
// advance-payment deadlines as marks at their dates, with Today as a line across them. Every figure is
// the calendar's own, as CalendarView hands it to the tables beside it; this only places it. A position
// is a fraction of the year, 0 at its first day's start and 1 at its last day's end, so a view draws it
// as a percentage of a plot that stretches (YearScale): a point is the middle of its day, so a deadline
// and Today of the same date meet. The company's lane is null for a river that starts at the pay. Next is
// the date of the next payment as CalendarView.Next gives it, so the timeline, the table and the panel
// above it name the one deadline.
internal sealed record TimelineView(
    int IncomeYear,
    TimelineToday Today,
    IReadOnlyList<TimelineMonth> Axis,
    DeadlineLane? Company,
    DeadlineLane Director,
    SpanLane Pay,
    SpanLane Contributions)
{
    // A label whose mark lies beyond this fraction of the year leans left of it, so it stays inside the
    // plot on the right.
    public const decimal LabelsLeanLeftAfter = 0.6m;

    public static TimelineView Of(PaymentCalendar calendar, DateOnly? next)
    {
        var scale = new YearScale(calendar.IncomeYear);

        return new TimelineView(
            calendar.IncomeYear,
            TimelineToday.Of(calendar.Today, scale),
            [.. Enumerable.Range(1, 12).Select(month => TimelineMonth.Of(scale, month))],
            calendar.Company is { } company ? DeadlineLane.Of(company, scale, next) : null,
            DeadlineLane.Of(calendar.Director, scale, next),
            SpanLane.OfPay(calendar.Months, scale),
            SpanLane.OfContributions(calendar.Contributions, scale));
    }
}

// Where a date falls on the income year: the days it counts, out of the year's.
internal readonly record struct YearScale(int Year)
{
    private DateOnly Start => new(Year, 1, 1);

    private int Days => new DateOnly(Year + 1, 1, 1).DayNumber - Start.DayNumber;

    // The middle of the date's day: where a deadline or Today sits.
    public decimal PointAt(DateOnly date) => (date.DayNumber - Start.DayNumber + 0.5m) / Days;

    // The start of the date's day: where a month or a quarter begins, the end of the year being 1.
    public decimal EdgeAt(DateOnly date) => (decimal)(date.DayNumber - Start.DayNumber) / Days;

    // The share of the year a span from one day's start to another's covers.
    public decimal Span(DateOnly from, DateOnly to) => EdgeAt(to) - EdgeAt(from);
}

internal enum TodayPlace
{
    // Today is before the income year starts: the year is all to come.
    BeforeYear,

    InYear,

    // Today is after the year's end: the year is all past.
    AfterYear,
}

// Today on the year: its date, whether it falls in the income year and, only then, where.
internal sealed record TimelineToday(DateOnly Date, TodayPlace Place, decimal? At)
{
    public static TimelineToday Of(DateOnly today, YearScale scale) => today.Year.CompareTo(scale.Year) switch
    {
        < 0 => new TimelineToday(today, TodayPlace.BeforeYear, null),
        > 0 => new TimelineToday(today, TodayPlace.AfterYear, null),
        _ => new TimelineToday(today, TodayPlace.InYear, scale.PointAt(today)),
    };
}

// One month of the axis, 1 being January: where it starts and how much of the year it covers.
internal sealed record TimelineMonth(int Month, decimal Left, decimal Width)
{
    public static TimelineMonth Of(YearScale scale, int month)
    {
        var from = new DateOnly(scale.Year, month, 1);
        return new TimelineMonth(month, scale.EdgeAt(from), scale.Span(from, from.AddMonths(1)));
    }
}

internal enum MarkState
{
    // The deadline has passed.
    Past,

    // The next payment of the plan falls by this deadline.
    Next,

    // The deadline is still to come, and is not the next payment's.
    ToCome,
}

// One deadline on the year: its date and position, whether it is past, next or to come, and what the plan
// pays by it plus, at the deadline still open, what more cancels the surcharge, which is the table's
// figure for the date.
internal sealed record TimelineMark(DateOnly Date, decimal At, MarkState State, decimal Amount)
{
    // The label leans left of the mark, ending at it, when the mark lies late in the year.
    public bool LabelEnd => At > TimelineView.LabelsLeanLeftAfter;
}

internal enum LaneNote
{
    None,

    // Nothing is left to pay by any deadline still to come.
    NoneDue,

    // The tax carries no surcharge for this payer: a young small company, a starter.
    Exempt,
}

// One payer's deadlines on the year, in their order. The note says what the lane has to say of itself
// when no mark of it is the next payment: that nothing is due, or that the payer is exempt.
internal sealed record DeadlineLane(IReadOnlyList<TimelineMark> Marks, bool Exempt)
{
    public LaneNote Note => Marks.Any(mark => mark.State == MarkState.Next || (mark.State == MarkState.ToCome && mark.Amount > 0m))
        ? LaneNote.None
        : Exempt ? LaneNote.Exempt : LaneNote.NoneDue;

    public static DeadlineLane Of(AdvancePaymentSchedule schedule, YearScale scale, DateOnly? next) =>
        new([.. schedule.Payments.Select(payment => Mark(payment, scale, next))], schedule.Exempt);

    private static TimelineMark Mark(ScheduledAdvancePayment payment, YearScale scale, DateOnly? next)
    {
        var date = payment.Deadline.Value;
        var amount = payment.Planned + payment.More;
        var state = payment.Past ? MarkState.Past : date == next && amount > 0m ? MarkState.Next : MarkState.ToCome;

        return new TimelineMark(date, scale.PointAt(date), state, amount);
    }
}

// One span of the year, a month of the pay or a quarter of INASTI's, numbered from 1: its first day, where
// it starts and how much of the year it covers, whether it has ended, what it carries (the net paid in
// the month, a quarter of the year's contributions) and, for a month, whether it falls in the rules Netto
// does not model yet. Its label stands by its near edge early in the year and its far edge late.
internal sealed record TimelineBlock(int Index, DateOnly From, decimal Left, decimal Width, bool Past, decimal Amount, bool Unmodelled)
{
    public bool LabelEnd => Left > TimelineView.LabelsLeanLeftAfter;

    public decimal LabelAt => LabelEnd ? Left + Width : Left;
}

// The director's pay month by month, or INASTI's quarters, on the year. Next is the first span not ended,
// the month under way or the quarter, which the lane's label stands by; none once the year is over.
// Empty is a lane that carries nothing in any span; PaidByCompany, quarters the company pays on the
// director's behalf.
internal sealed record SpanLane(IReadOnlyList<TimelineBlock> Blocks, bool PaidByCompany)
{
    public TimelineBlock? Next => Blocks.FirstOrDefault(block => !block.Past);

    public bool Empty => Blocks.All(block => block.Amount == 0m);

    public static SpanLane OfPay(IReadOnlyList<CalendarMonth> months, YearScale scale) =>
        new([.. months.Select(month => Block(month.Month.Month, month.Month, month.Month.AddMonths(1), month.Past, month.Pay.NetPaid, month.Pay.UnmodelledRules, scale))], false);

    public static SpanLane OfContributions(ContributionQuarters contributions, YearScale scale) =>
        new([.. contributions.Quarters.Select(quarter =>
            Block((quarter.From.Month - 1) / 3 + 1, quarter.From, quarter.From.AddMonths(3), quarter.Past, contributions.EachQuarter, false, scale))], contributions.PaidByCompany);

    private static TimelineBlock Block(int index, DateOnly from, DateOnly to, bool past, decimal amount, bool unmodelled, YearScale scale) =>
        new(index, from, scale.EdgeAt(from), scale.Span(from, to), past, amount, unmodelled);
}
