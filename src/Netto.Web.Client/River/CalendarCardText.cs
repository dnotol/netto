namespace Netto.Web.Client.River;

// The resource keys CalendarCard.razor builds from the calendar's view. The component and
// CalendarCardTextTests both go through these, so a state without a text in CalendarCard.resx fails a
// test instead of showing on screen.
internal static class CalendarCardText
{
    // What the company's payments leave on its corporate tax.
    public static string Company(CalendarTax<SurchargeState> tax) => tax.TooLate ? "Company_TooLate" : $"Company_{tax.State}";

    // What the withholding and the director's payments leave on the income tax on their pay.
    public static string Director(CalendarTax<TaxSurchargeState> tax) => tax.TooLate ? "Director_TooLate" : $"Director_{tax.State}";

    // What an assessment leaves after the year: to pay, refunded, or nothing.
    public static string CompanyBalance(decimal balance) => $"CompanyBalance_{Balance(balance)}";

    public static string DirectorBalance(decimal balance) => $"DirectorBalance_{Balance(balance)}";

    private static string Balance(decimal balance) => balance switch
    {
        > 0m => "Pay",
        < 0m => "Refund",
        _ => "Nothing",
    };

    // The folded card's line: who pays next, by the date of the first row still open.
    public static string Summary(CalendarRow? next) => next switch
    {
        null => "Summary_Nothing",
        { Company: > 0m, Director: > 0m } => "Summary_Both",
        { Company: > 0m } => "Summary_Company",
        _ => "Summary_Director",
    };
}
