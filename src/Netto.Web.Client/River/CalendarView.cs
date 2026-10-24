using Netto.Core.Calendar;
using Netto.Core.Rules;

namespace Netto.Web.Client.River;

// What the When to pay tab draws: the payments the plan in the river implies (PaymentCalendar), seen on
// the date the card is shown. Rows are the deadlines by which the company or the director pays
// something or needs to, in their order; a deadline with nothing paid and nothing needed is left out,
// as a drain of nothing is. Company is null for a river that starts at the pay, and each tax says
// what its payments leave, worded as its card words it (SurchargeStates). Next is the first row still
// open with something to pay. Pay is the pay statement's month, the same every month, and Balances
// what each assessment leaves after the year, the company's first. The view only selects and orders
// the engine's figures.
internal sealed record CalendarView(
    int IncomeYear,
    IReadOnlyList<CalendarRow> Rows,
    CalendarTax<SurchargeState>? Company,
    CalendarTax<TaxSurchargeState> Director,
    CalendarRow? Next,
    CalendarPay Pay,
    ContributionQuarters Contributions,
    AssessmentDue? CompanyBalance,
    AssessmentDue DirectorBalance)
{
    // The engine's refusal of what the user says the fund bills, shown on the card's field; the
    // calendar then shows no regularisation.
    public Refusal? BilledRefused { get; init; }

    // The engine's calendar this view was made from, with what the fund bills given, for what draws the
    // year in time (the timeline), so it and the tables read one calendar.
    public required PaymentCalendar Calendar { get; init; }

    // The quarters of the year not ended yet.
    public int QuartersLeft => Contributions.Quarters.Count(quarter => !quarter.Past);

    // None while the engine refuses an input: there is no year to place.
    public static CalendarView? From(RiverInputs inputs, RiverRun run, DateOnly today)
    {
        var calendar = run switch
        {
            { Year: { } year } => PaymentCalendar.Of(year, today),
            { Pay: { } pay } => PaymentCalendar.Of(pay, inputs.TaxPrepayments.Values.Paid, today),
            _ => null,
        };
        if (calendar is null || run.Pay is not { } directorPay)
            return null;

        var (billed, billedRefused) = calendar.WithBilled(inputs.BilledContributions.Value);
        calendar = billed ?? calendar;

        var company = calendar.Company;
        var director = calendar.Director;
        var rows = director.Payments
            .Select((payment, i) => new CalendarRow(
                payment.Deadline.Value, payment.Past, company?.Payments[i].Planned ?? 0m, company?.Payments[i].More ?? 0m, payment.Planned, payment.More))
            .Where(row => row.Total > 0m)
            .ToList();

        return new CalendarView(
            calendar.IncomeYear,
            rows,
            company is null || run.Year is not { } companyYear ? null : Tax(SurchargeStates.Of(companyYear.AdvancePayments), company),
            Tax(SurchargeStates.Of(directorPay.AdvancePayments), director),
            rows.FirstOrDefault(row => !row.Past),
            CalendarPay.Of(calendar.Months, directorPay.Assessment),
            calendar.Contributions,
            calendar.CompanyBalance,
            calendar.DirectorBalance) { BilledRefused = billedRefused, Calendar = calendar };
    }

    private static CalendarTax<TState> Tax<TState>(TState state, AdvancePaymentSchedule schedule) where TState : struct, Enum =>
        new(state, schedule.Surcharge, schedule.More, schedule.Open is { } open ? schedule.Payments[open].Deadline.Value : null);
}

// One deadline: its date, whether it is past, what the company and the director pay by it as the plan
// stands, and what more by it cancels each surcharge (only at the first deadline still open).
internal sealed record CalendarRow(DateOnly Date, bool Past, decimal CompanyPlanned, decimal CompanyMore, decimal DirectorPlanned, decimal DirectorMore)
{
    public decimal Company => CompanyPlanned + CompanyMore;

    public decimal Director => DirectorPlanned + DirectorMore;

    public decimal Total => Company + Director;
}

// The director's pay each month, the statement's month, the same for all twelve: the gross on the
// payslip, what the key formula withholds and the net paid. Left is the months not ended yet, the
// first and the last, none once the year is over; UnmodelledFrom the first month whose rules Netto
// does not model yet, none when no month is flagged. WithheldGiven is the year's withholding the user
// gave, which the months do not split: they show the key formula's.
internal sealed record CalendarPay(
    decimal GrossPay, decimal Withholding, decimal NetPaid, (DateOnly First, DateOnly Last)? Left, DateOnly? UnmodelledFrom, decimal? WithheldGiven)
{
    public static CalendarPay Of(IReadOnlyList<CalendarMonth> months, Netto.Core.PersonalTax.Assessment assessment)
    {
        var month = months[0].Pay;
        var left = months.Where(each => !each.Past).Select(each => each.Month).ToList();

        return new CalendarPay(
            month.GrossPay,
            month.Withholding,
            month.NetPaid,
            left.Count == 0 ? null : (left[0], left[^1]),
            months.FirstOrDefault(each => each.Pay.UnmodelledRules)?.Month,
            assessment.WithheldGiven ? assessment.Withheld : null);
    }
}

// What one tax's payments leave: its state as its card says it, the surcharge left, what more by the
// first deadline still open cancels it, and that deadline, none once every one is past.
internal sealed record CalendarTax<TState>(TState State, decimal Surcharge, decimal More, DateOnly? OpenDeadline) where TState : struct, Enum
{
    // A surcharge is left and no deadline is open to cancel it.
    public bool TooLate => More == 0m && Surcharge > 0m && OpenDeadline is null;
}
