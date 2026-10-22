using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.DirectorPay;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// PaySlip.razor builds resource keys from the engine's keys. A key without a text in PaySlip.resx
// would show on screen as the key itself, so every key the engine returns for 2026 must have one,
// with a municipality's rate and with a rate the user entered, the contributions paid by the
// director or by the company, and the withholding derived or given.
public class PaySlipTextTests
{
    // The name IStringLocalizer<PaySlip> reads DirectorPay/PaySlip.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.DirectorPay.PaySlip", typeof(PaySlip).Assembly);

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.Entered(0.07m),
    };

    private static readonly Dictionary<string, TaxPrepayments> WithholdingsByOrigin = new()
    {
        ["derived"] = TaxPrepayments.AsWithheld,
        ["given"] = new TaxPrepayments(10_000m, AdvancePayments.None),
    };

    public static TheoryData<string, ContributionsPaidBy, string> Cases =>
    [
        .. from rateOrigin in RatesByOrigin.Keys
           from paidBy in Enum.GetValues<ContributionsPaidBy>()
           from withholding in WithholdingsByOrigin.Keys
           select (rateOrigin, paidBy, withholding),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void PaySlipResx_everyKeyItBuildsFromThe2026Result_hasAText(string rateOrigin, ContributionsPaidBy paidBy, string withholding)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin], WithholdingsByOrigin[withholding], paidBy);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        var keys = view.Tracks.Select(track => PaySlipText.Track(track.Key))
            .Concat(view.Tracks.Select(track => PaySlipText.TrackHint(track.Key)))
            .Concat(view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)))
            .Concat(view.NextThousand.Select(part => PaySlipText.Row(part.Key)))
            .Append(PaySlipText.Withheld(view.Balance.WithheldGiven))
            .Append(PaySlipText.Balance(view.Balance.Kind));

        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }

    // A pay with a car's benefit adds its row to the tax base and its column to the statement.
    [Fact]
    public void PaySlipResx_everyKeyOfAPayWithABenefit_hasAText()
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld, ContributionsPaidBy.Director, 0m, 2_400m);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)).Append("ColumnBenefit").Append("AddedNote")
            .Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }

    // A royalty beyond its limits adds its row to both tracks and says what the next 1,000 holds; one
    // within them adds its lines to the balance.
    [Fact]
    public void PaySlipResx_everyKeyOfAPayWithARoyalty_hasAText()
    {
        var movable = new Netto.Core.MovableIncome.MovableIncomePaid(
            30_000m, new IncomeTaxedApart(24_000m, Netto.Core.Copyright.CopyrightRules.IncomeYear2026.TaxedApartRate), 6_000m, 5_400m);
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld, ContributionsPaidBy.Director, 0m, 0m, movable);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)).Concat(["FinalTaxOnRoyalty", "WithheldOnRoyalty", "NextRoyaltyNote"])
            .Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }

    // With a rent taxed with the pay, the final tax and the next 1,000 say so.
    [Fact]
    public void PaySlipResx_everyKeyOfARentTaxedWithThePay_hasAText()
    {
        string[] keys = [PaySlipText.FinalTax(false), PaySlipText.FinalTax(true), "FinalTaxOnRent", "NextRentNote"];

        keys.Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }

    [Fact]
    public void PaySlipResx_everyWayTheBalanceCanGo_hasAText()
    {
        Enum.GetValues<BalanceKind>().Select(PaySlipText.Balance)
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }
}
