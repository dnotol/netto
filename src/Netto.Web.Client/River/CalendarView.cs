using Netto.Core.Calendar;

namespace Netto.Web.Client.River;

// What the calendar card draws: the payments the plan in the river implies (PaymentCalendar), seen on
// the date the card is shown. Rows are the deadlines by which the company or the director pays
// something or needs to, in their order; a deadline with nothing paid and nothing needed is left out,
// as a drain of nothing is. Company is null for a river that starts at the pay, and each tax says
// what its payments leave, worded as its card words it (SurchargeStates). Next is the first row still
// open with something to pay. The view only selects and orders the engine's figures.
internal sealed record CalendarView(
    int IncomeYear,
    IReadOnlyList<CalendarRow> Rows,
    CalendarTax<SurchargeState>? Company,
    CalendarTax<TaxSurchargeState> Director,
    CalendarRow? Next)
{
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
            rows.FirstOrDefault(row => !row.Past));
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

// What one tax's payments leave: its state as its card says it, the surcharge left, what more by the
// first deadline still open cancels it, and that deadline, none once every one is past.
internal sealed record CalendarTax<TState>(TState State, decimal Surcharge, decimal More, DateOnly? OpenDeadline) where TState : struct, Enum
{
    // A surcharge is left and no deadline is open to cancel it.
    public bool TooLate => More == 0m && Surcharge > 0m && OpenDeadline is null;
}
