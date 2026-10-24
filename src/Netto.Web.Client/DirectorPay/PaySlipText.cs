namespace Netto.Web.Client.DirectorPay;

// The resource keys PaySlip.razor builds from GrossToNetView's keys. The component and
// PaySlipTextTests both go through these, so a key the engine returns without a text in
// PaySlip.resx fails a test instead of showing on screen.
internal static class PaySlipText
{
    public static string Track(string key) => $"Track_{key}";

    public static string TrackHint(string key) => $"TrackHint_{key}";

    public static string Row(string key) => $"Row_{key}";

    // The balance's line for what was withheld says where the amount comes from.
    public static string Withheld(bool given) => given ? "Withheld_Given" : "Withheld_Derived";

    public static string Balance(BalanceKind kind) => $"Balance_{kind}";

    // When the company pays, the next 1,000's note names what it adds on top of the 1,000: the
    // contributions, with the fund's fee once its part is drawn.
    public static string NextCompanyPaysNote(bool withFundFee) => withFundFee ? "NextCompanyPaysNote_WithFee" : "NextCompanyPaysNote";

    // The months a period of the year's withholding rules covers, named above its withholding when the
    // year has more than one: a run of months, or a month alone.
    public static string WithholdingMonths(int firstMonth, int lastMonth) => firstMonth == lastMonth ? "WithholdingMonth" : "WithholdingMonths";

    // The final tax says it is on the rent too once a rent is taxed with the pay, and on a copyright
    // royalty once one is taxed apart.
    public static string FinalTax(bool withOtherIncome, bool withRoyalty = false) => (withOtherIncome, withRoyalty) switch
    {
        (true, true) => "FinalTax_WithRentAndRoyalty",
        (true, false) => "FinalTax_WithRent",
        (false, true) => "FinalTax_WithRoyalty",
        _ => "FinalTax",
    };
}
