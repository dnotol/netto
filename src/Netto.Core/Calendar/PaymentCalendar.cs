using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Core.Calendar;

// When the year's payments fall, seen on a date (Today): the company's advance payments on its
// corporate tax and the director's on the income tax on their pay, each by the income year's four
// deadlines. A record beside the ledger, as the pay statement is: a step is the year's total, the
// calendar says when it is paid. Company is null for a river that starts at the pay. Every figure is
// the year's own; the calendar only places it in time.
public sealed record PaymentCalendar(int IncomeYear, DateOnly Today, AdvancePaymentSchedule? Company, AdvancePaymentSchedule Director)
{
    public static PaymentCalendar Of(YearResult year, DateOnly today)
    {
        var incomeYear = year.Scenario.IncomeYear;
        var companyRules = CorporateTaxRules.For(incomeYear)?.AdvancePayments
            ?? throw new ArgumentException($"Income year {incomeYear} has no rules for the company's advance payments.", nameof(year));
        var payments = year.AdvancePayments;

        return new PaymentCalendar(
            incomeYear,
            today,
            AdvancePaymentSchedule.Of(
                companyRules.Deadlines.Select(deadline => deadline.Date).ToList(), year.Scenario.AdvancePayments, payments.TopUps, payments.Surcharge, payments.Exempt, today),
            DirectorSchedule(year.DirectorPay, year.Scenario.TaxPrepayments.Paid, today));
    }

    // A river that starts at the pay: what the director paid in advance by each deadline is the
    // scenario's, which the pay's result does not keep.
    public static PaymentCalendar Of(GrossToNetResult pay, AdvancePayments paid, DateOnly today) =>
        new(pay.IncomeYear, today, null, DirectorSchedule(pay, paid, today));

    // The first deadline on or after the date, the first being 0, none once every one is past: a
    // deadline is open until the end of its day.
    public static int? FirstOpen(IEnumerable<DateOnly> deadlines, DateOnly date) =>
        deadlines.Select((deadline, i) => (deadline, i)).Where(entry => entry.deadline >= date).Select(entry => (int?)entry.i).FirstOrDefault();

    private static AdvancePaymentSchedule DirectorSchedule(GrossToNetResult pay, AdvancePayments paid, DateOnly today)
    {
        var rules = PersonalTaxRules.For(pay.IncomeYear)?.AdvancePayments
            ?? throw new ArgumentException($"Income year {pay.IncomeYear} has no rules for the director's advance payments.", nameof(pay));
        var payments = pay.AdvancePayments;

        return AdvancePaymentSchedule.Of(rules.Deadlines.Select(deadline => deadline.Date).ToList(), paid, payments.TopUps, payments.Surcharge, payments.Exempt, today);
    }
}

// The payments on one tax by the income year's deadlines, in their order, the surcharge they leave,
// and whether the tax is exempt from it (a small company in its first three financial years, a
// director in their first three years as self-employed). Open is the first deadline still open on the
// calendar's date, none once all are past; only its payment says what more cancels the surcharge
// (More), since paying it earlier earns more credit and later is too late to choose.
public sealed record AdvancePaymentSchedule(IReadOnlyList<ScheduledAdvancePayment> Payments, decimal Surcharge, bool Exempt, int? Open)
{
    // What more by the open deadline cancels the surcharge: nothing when none is left or no deadline is.
    public decimal More => Open is { } open ? Payments[open].More : 0m;

    internal static AdvancePaymentSchedule Of(
        IReadOnlyList<RuleDate> deadlines, AdvancePayments paid, IReadOnlyList<decimal> topUps, decimal surcharge, bool exempt, DateOnly today)
    {
        var open = PaymentCalendar.FirstOpen(deadlines.Select(deadline => deadline.Value), today);
        var payments = deadlines
            .Select((deadline, i) => new ScheduledAdvancePayment(deadline, paid.ByDeadline[i], i == open ? topUps[i] : 0m, deadline.Value < today))
            .ToList();

        return new AdvancePaymentSchedule(payments, surcharge, exempt, open);
    }
}

// One deadline of a schedule: its date with its source, what the plan pays by it, what more by it
// cancels the surcharge (only at the first deadline still open), and whether it is past.
public sealed record ScheduledAdvancePayment(RuleDate Deadline, decimal Planned, decimal More, bool Past);
