using Netto.Core.Calendar;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The income year drawn as a timeline, from the engine's PaymentCalendar: the company's and the director's
// advance-payment deadlines as marks at their dates, with Today as a line across them. Every figure is
// the calendar's own, as CalendarView hands it to the tables beside it; this only places it. A position
// is a fraction of the year, 0 at its first day's start and 1 at its last day's end, so a view draws it
// as a percentage of a plot that stretches (YearScale): a point is the middle of its day, so a deadline
// and Today of the same date meet. The company's lane is null for a river that starts at the pay. Next is
// the date of the next payment as CalendarView.Next gives it, so the timeline, the table and the panel
// above it name the one deadline. PayRuns is the pay's months in runs that pay the same, as the table
// beside it words them (PayRun), for the picture's label.
internal sealed record TimelineView(
    int IncomeYear,
    TimelineToday Today,
    IReadOnlyList<TimelineMonth> Axis,
    DeadlineLane? Company,
    DeadlineLane Director,
    SpanLane Pay,
    IReadOnlyList<PayRun> PayRuns,
    SpanLane Contributions)
{
    // Today's label, and the caption of a lane of blocks, stand left of their line or block when it lies
    // beyond this fraction of the year, so they stay inside the plot on the right.
    public const decimal LabelsLeanLeftAfter = 0.6m;

    // A deadline's label stands centred on its mark, unless the mark is within this fraction of the
    // year's start or end: there a centred label would leave the plot, so it leans inward.
    public const decimal LabelsAnchorWithin = 0.02m;

    // Each payer's next payment, if the next deadline carries one of theirs.
    public TimelineMark? NextCompany => Company?.Marks.FirstOrDefault(mark => mark.State == MarkState.Next);

    public TimelineMark? NextDirector => Director.Marks.FirstOrDefault(mark => mark.State == MarkState.Next);

    // What the picture draws, in the order its key names it: only what is drawn.
    public IReadOnlyList<KeyItem> Key
    {
        get
        {
            var marks = (Company?.Marks ?? []).Concat(Director.Marks).ToList();
            var blocks = Pay.Blocks.Concat(Contributions.Blocks).ToList();
            var drawn = new Dictionary<KeyItem, bool>
            {
                [KeyItem.PastDeadline] = marks.Any(mark => mark.State == MarkState.Past),
                [KeyItem.NextPayment] = marks.Any(mark => mark.State == MarkState.Next),
                [KeyItem.LaterPayment] = marks.Any(mark => mark.State == MarkState.ToCome && mark.Amount > 0m),
                [KeyItem.NothingToPay] = marks.Any(mark => mark.State == MarkState.ToCome && mark.Amount == 0m),
                [KeyItem.EndedSpan] = blocks.Any(block => block.Past),
                [KeyItem.ComingSpan] = blocks.Any(block => !block.Past),
                [KeyItem.Today] = Today.Place == TodayPlace.InYear,
            };

            return [.. drawn.Where(item => item.Value).Select(item => item.Key)];
        }
    }

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
            PayRun.Of(calendar.Months),
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

// What the key of the picture can name, in its order.
internal enum KeyItem
{
    PastDeadline,
    NextPayment,
    LaterPayment,
    NothingToPay,
    EndedSpan,
    ComingSpan,
    Today,
}

// Today on the year: its date, whether it falls in the income year and, only then, where.
internal sealed record TimelineToday(DateOnly Date, TodayPlace Place, decimal? At)
{
    // Its label leans left of the line late in the year, as a mark's does.
    public bool LabelEnd => At > TimelineView.LabelsLeanLeftAfter;

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
    // The label stands centred on the mark. A mark so near an edge of the year that a centred label would
    // leave the plot anchors it to the side that has room, its first edge at the mark early in the year,
    // its last late in it: none of the income year's deadlines is (the first is April's, the last
    // December's), the rule only keeps a label inside the plot whatever the date.
    public LabelAnchor Anchor => At < TimelineView.LabelsAnchorWithin ? LabelAnchor.Start
        : At > 1m - TimelineView.LabelsAnchorWithin ? LabelAnchor.End
        : LabelAnchor.Centre;
}

// Which edge of a deadline's label stands at its mark: the left (leaning right of it), the right (leaning
// left of it), or neither, the label centred on it.
internal enum LabelAnchor
{
    Start,
    Centre,
    End,
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
// it starts and how much of the year it covers, whether it has ended and what it carries (the net paid in
// the month, a quarter of the year's contributions). Its label stands by its near edge early in the year
// and its far edge late.
internal sealed record TimelineBlock(int Index, DateOnly From, decimal Left, decimal Width, bool Past, decimal Amount)
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
        new([.. months.Select(month => Block(month.Month.Month, month.Month, month.Month.AddMonths(1), month.Past, month.Pay.NetPaid, scale))], false);

    public static SpanLane OfContributions(ContributionQuarters contributions, YearScale scale) =>
        new([.. contributions.Quarters.Select(quarter =>
            Block((quarter.From.Month - 1) / 3 + 1, quarter.From, quarter.From.AddMonths(3), quarter.Past, contributions.EachQuarter, scale))], contributions.PaidByCompany);

    private static TimelineBlock Block(int index, DateOnly from, DateOnly to, bool past, decimal amount, YearScale scale) =>
        new(index, from, scale.EdgeAt(from), scale.Span(from, to), past, amount);
}
