using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys WhenToPayTab.razor builds from the calendar's view. The component and
// WhenToPayTabTextTests both go through these, so a state without a text in WhenToPayTab.resx fails a
// test instead of showing on screen.
internal static class WhenToPayTabText
{
    // What the company's payments leave on its corporate tax.
    public static string Company(CalendarTax<SurchargeState> tax) => tax.TooLate ? "Company_TooLate" : $"Company_{tax.State}";

    // What the withholding and the director's payments leave on the income tax on their pay.
    public static string Director(CalendarTax<TaxSurchargeState> tax) => tax.TooLate ? "Director_TooLate" : $"Director_{tax.State}";

    // What an assessment leaves after the year: to pay, refunded, or nothing.
    public static string CompanyBalance(decimal balance) => $"CompanyBalance_{Balance(balance)}";

    public static string DirectorBalance(decimal balance) => $"DirectorBalance_{Balance(balance)}";

    // What the year's contributions leave over what the fund bills: more to pay, back, or nothing.
    public static string Regularisation(decimal regularisation) => $"Regularisation_{Balance(regularisation)}";

    private static string Balance(decimal balance) => balance switch
    {
        > 0m => "Pay",
        < 0m => "Refund",
        _ => "Nothing",
    };

    // The Next panel's line: the date of the first row still open and who pays what by it, or that
    // nothing is left to pay in advance.
    public static string Next(CalendarRow? next) => next switch
    {
        null => "Next_Nothing",
        { Company: > 0m, Director: > 0m } => "Next_Both",
        { Company: > 0m } => "Next_Company",
        _ => "Next_Director",
    };
}
