using Netto.Core.Calendar;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What more paid by the first deadline still open on a date cancels a surcharge: the deadline and the
// amount, as the calendar's schedule gives them (AdvancePaymentSchedule.Open and More), which is the
// figure the Optimisation tab's payment move proposes and When to pay says, so the Director pay and
// Corporate tax cards say the same. None while no surcharge is left or every deadline is past, as the
// payment finders propose no move then. The date is the browser's, given by the card from its
// TimeProvider.
internal sealed record MoreBy(DateOnly Deadline, decimal Amount)
{
    // The director's, on the income tax on their pay; none for a year or a region without rules. The
    // calendar is the pay's with what the director paid in advance, as the river's When to pay tab builds
    // it.
    public static MoreBy? Director(RiverInputs inputs, GrossToNetResult pay, DateOnly today) =>
        Of(PaymentCalendar.Of(pay, inputs.TaxPrepayments.Values.Paid, today)?.Director);

    // The company's, on its corporate tax.
    public static MoreBy? Company(YearResult year, DateOnly today) => Of(PaymentCalendar.Of(year, today)?.Company);

    // The predicate of the payment finders: a deadline is open and some payment is needed by it.
    public static MoreBy? Of(AdvancePaymentSchedule? schedule) =>
        schedule is { Open: { } open, More: > 0m } ? new MoreBy(schedule.Payments[open].Deadline.Value, schedule.More) : null;
}
