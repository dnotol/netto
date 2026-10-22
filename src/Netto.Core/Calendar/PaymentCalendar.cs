using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;

namespace Netto.Core.Calendar;

// When the year's payments fall, seen on a date (Today): the company's advance payments on its
// corporate tax and the director's on the income tax on their pay, each by the income year's four
// deadlines; the director's pay month by month, as the pay statement gives it; INASTI's quarters;
// and after the year, the balance on each assessment of the following tax year, to pay when positive,
// refunded when negative, on the day the administration sets. A record beside the ledger, as the pay
// statement is: a step is the year's total, the calendar says when it is paid. Company and
// CompanyBalance are null for a river that starts at the pay. Every figure is the year's own; the
// calendar only places it in time, but for the company's balance, its corporate tax with the
// surcharge less what it paid in advance (art. 304, §2, al. 2, which refunds the excess), and the
// contributions' regularisation once the user gives what the fund bills (WithBilled).
public sealed record PaymentCalendar(
    int IncomeYear,
    DateOnly Today,
    AdvancePaymentSchedule? Company,
    AdvancePaymentSchedule Director,
    IReadOnlyList<CalendarMonth> Months,
    ContributionQuarters Contributions,
    AssessmentDue? CompanyBalance,
    AssessmentDue DirectorBalance)
{
    public const string BilledContributionsNegative = "billed_contributions_negative";

    // The calendar with what the social insurance fund bills each quarter as provisional
    // contributions, from its notice, none until given: they rest on the income of three years
    // before, which Netto does not hold, and the decisions keep the final contribution on the year's
    // income. Their regularisation is what the year's contributions leave over four of them.
    public (PaymentCalendar? Value, string? Error) WithBilled(decimal? billedEachQuarter) => billedEachQuarter switch
    {
        < 0m => (null, BilledContributionsNegative),
        { } billed => (this with { Contributions = Contributions with { Billed = billed, Regularisation = Contributions.EachQuarter * 4 - billed * 4 } }, null),
        null => (this with { Contributions = Contributions with { Billed = null, Regularisation = null } }, null),
    };

    public static PaymentCalendar Of(YearResult year, DateOnly today)
    {
        var incomeYear = year.Scenario.IncomeYear;
        var companyRules = CorporateTaxRules.For(incomeYear)?.AdvancePayments
            ?? throw new ArgumentException($"Income year {incomeYear} has no rules for the company's advance payments.", nameof(year));
        var payments = year.AdvancePayments;
        var paid = year.Scenario.AdvancePayments;

        return Of(year.DirectorPay, year.Scenario.TaxPrepayments.Paid, today) with
        {
            Company = AdvancePaymentSchedule.Of(
                companyRules.Deadlines.Select(deadline => deadline.Date).ToList(), paid, payments.TopUps, payments.Surcharge, payments.Exempt, today),
            CompanyBalance = new AssessmentDue(
                incomeYear + 1, year.Company.CorporateTax + payments.Surcharge - paid.ByDeadline.Sum(), CorporateTaxRules.AssessmentBalance),
        };
    }

    // A river that starts at the pay: what the director paid in advance by each deadline is the
    // scenario's, which the pay's result does not keep.
    public static PaymentCalendar Of(GrossToNetResult pay, AdvancePayments paid, DateOnly today)
    {
        var rules = PersonalTaxRules.For(pay.IncomeYear)?.AdvancePayments
            ?? throw new ArgumentException($"Income year {pay.IncomeYear} has no rules for the director's advance payments.", nameof(pay));
        var payments = pay.AdvancePayments;

        return new PaymentCalendar(
            pay.IncomeYear,
            today,
            null,
            AdvancePaymentSchedule.Of(rules.Deadlines.Select(deadline => deadline.Date).ToList(), paid, payments.TopUps, payments.Surcharge, payments.Exempt, today),
            [.. pay.Statement.Months.Select(month => CalendarMonth.Of(pay.IncomeYear, month, today))],
            ContributionQuarters.Of(pay, today),
            null,
            new AssessmentDue(pay.IncomeYear + 1, pay.Assessment.Balance, pay.Assessment.Source));
    }

    // The first deadline on or after the date, the first being 0, none once every one is past: a
    // deadline is open until the end of its day.
    public static int? FirstOpen(IEnumerable<DateOnly> deadlines, DateOnly date) =>
        deadlines.Select((deadline, i) => (deadline, i)).Where(entry => entry.deadline >= date).Select(entry => (int?)entry.i).FirstOrDefault();
}

// One month of the director's pay, from its first day, as the pay statement gives it, and whether it
// has ended on the calendar's date.
public sealed record CalendarMonth(DateOnly Month, PayStatementMonth Pay, bool Past)
{
    internal static CalendarMonth Of(int incomeYear, PayStatementMonth pay, DateOnly today)
    {
        var month = new DateOnly(incomeYear, pay.Month, 1);
        return new CalendarMonth(month, pay, month.AddMonths(1) <= today);
    }
}

// INASTI's quarters of the year, each from its first day and past once ended, and a quarter of the
// final contribution on the year's income, which INASTI bills as four rounded quarters
// (SocialContributionCalculator.Billed), so four of them are the year's: what pays the year as it
// goes. PaidByCompany when the company pays them on the director's behalf. Billed is what the fund
// bills each quarter as provisional contributions, once the user gives it, and Regularisation what
// the year's contributions leave over four of those, to pay when positive, refunded when negative,
// once the fund knows the year's income; the day of each quarter's payment is not modelled. Source is
// INASTI's leaflet, which gives both kinds of contribution.
public sealed record ContributionQuarters(
    IReadOnlyList<CalendarQuarter> Quarters, decimal EachQuarter, bool PaidByCompany, decimal? Billed, decimal? Regularisation, Source Source)
{
    private const int QuartersInYear = 4;

    internal static ContributionQuarters Of(GrossToNetResult pay, DateOnly today)
    {
        var rules = SocialContributionRules.For(pay.IncomeYear)
            ?? throw new ArgumentException($"Income year {pay.IncomeYear} has no social contribution rules.", nameof(pay));
        var quarters = Enumerable.Range(0, QuartersInYear)
            .Select(i => new DateOnly(pay.IncomeYear, 1 + 3 * i, 1))
            .Select(from => new CalendarQuarter(from, from.AddMonths(3) <= today))
            .ToList();

        return new ContributionQuarters(quarters, pay.SocialContributions / QuartersInYear, pay.ContributionsPaidByCompany > 0m, null, null, rules.Source);
    }
}

// One quarter of the year, from its first day, and whether it has ended on the calendar's date.
public sealed record CalendarQuarter(DateOnly From, bool Past);

// The balance an assessment of the tax year leaves, to pay when positive, refunded when negative, with
// the text it follows. The administration sets its day.
public sealed record AssessmentDue(int TaxYear, decimal Balance, Source Source);

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
