using Netto.Core.Calendar;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys WhenToPaySection.razor builds from the calendar's view. The component and
// WhenToPaySectionTextTests both go through these, so a state without a text in WhenToPaySection.resx fails a
// test instead of showing on screen.
internal static class WhenToPaySectionText
{
    // What the company's payments leave on its corporate tax.
    public static string Company(CalendarTax<SurchargeState> tax) => tax.TooLate ? "Company_TooLate" : $"Company_{tax.State}";

    // What the withholding and the director's payments leave on the income tax on their pay.
    public static string Director(CalendarTax<TaxSurchargeState> tax) => tax.TooLate ? "Director_TooLate" : $"Director_{tax.State}";

    // What an assessment leaves after the year: to pay, refunded, or nothing.
    public static string CompanyBalance(decimal balance) => $"CompanyBalance_{Balance(balance)}";

    public static string DirectorBalance(decimal balance) => $"DirectorBalance_{Balance(balance)}";

    // A quarter of the year's contributions: who pays it, and, once a fund fee is given, that the fee is
    // in it.
    public static string Contributions(ContributionQuarters contributions) => (contributions.PaidByCompany, contributions.FundFeeEachQuarter > 0m) switch
    {
        (false, false) => "ContributionsLine",
        (true, false) => "ContributionsLine_Company",
        (false, true) => "ContributionsLine_WithFee",
        (true, true) => "ContributionsLine_Company_WithFee",
    };

    // What the year's contributions leave over what the fund bills: more to pay, back, or nothing.
    public static string Regularisation(decimal regularisation) => $"Regularisation_{Balance(regularisation)}";

    private static string Balance(decimal balance) => balance switch
    {
        > 0m => "Pay",
        < 0m => "Refund",
        _ => "Nothing",
    };

    // Who pays by the next deadline: the one classifier, for the Next panel's line here and for the
    // timeline's label (TimelineText.Next), each wording it by keys of its own, so the two cannot name
    // different payers.
    public static NextPayers Payers(bool company, bool director) => (company, director) switch
    {
        (true, true) => NextPayers.Both,
        (true, false) => NextPayers.Company,
        (false, true) => NextPayers.Director,
        _ => NextPayers.Nothing,
    };

    // The Next panel's line: the date of the first row still open and who pays what by it, or that
    // nothing is left to pay in advance.
    public static string Next(CalendarRow? next) => $"Next_{Payers(next is { Company: > 0m }, next is { Director: > 0m })}";

    // A run of the pay's months: every month while the year pays alike, else the run's months, or its
    // month alone.
    public static string PayLine(PayRun run, int runs) => runs == 1 ? "PayLine" : run.First == run.Last ? "PayLineMonth" : "PayLineMonths";
}

// Whoever pays something by the next deadline, or nobody once nothing is left to pay in advance.
internal enum NextPayers
{
    Nothing,
    Company,
    Director,
    Both,
}
