using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// Income year 2026 at a 7% municipal rate, on the engine's own figures (GrossToNetTests): at 50,000
// the contributions are 8,293.36, the flat-rate costs 1,251.20, the taxable income 40,455.44, the
// income tax 11,333.96, the surcharge 793.38 and the net 29,579.30.
public class GrossToNetViewTests
{
    [Fact]
    public void From_gross50000_drawsTheCashTrackThenTheTaxBaseTrack()
    {
        var view = View(50_000m);

        view.Tracks.Select(track => track.Key).ShouldBe(["cash", "tax_base"]);
    }

    [Fact]
    public void From_gross50000_cashTrackRunsFromGrossThroughWhatIsPaidOutToNet()
    {
        var cash = View(50_000m).Tracks[0];

        cash.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_pay", RowKind.Gross, 50_000m),
            ("social_contributions", RowKind.PaidOut, 8_293.36m),
            ("income_tax", RowKind.PaidOut, 11_333.96m),
            ("municipal_surcharge", RowKind.PaidOut, 793.38m),
            ("net_in_pocket", RowKind.NetInPocket, 29_579.30m),
        });
    }

    [Fact]
    public void From_gross50000_taxBaseTrackRunsFromGrossThroughTheDeductionsToTaxableIncome()
    {
        var taxBase = View(50_000m).Tracks[1];

        taxBase.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_pay", RowKind.Gross, 50_000m),
            ("social_contributions", RowKind.PaidOut, 8_293.36m),
            ("flat_rate_costs", RowKind.TaxBaseOnly, 1_251.20m),
            ("taxable_income", RowKind.TaxableIncome, 40_455.44m),
        });
    }

    // Each part paid out is drawn where the balance before it ended, so the bars step down from the
    // gross to the net: 41,706.64, then 30,372.68, then 29,579.30 left, as shares of 50,000.
    [Fact]
    public void From_gross50000_cashBarsStepDownFromTheGrossToTheNet()
    {
        var cash = View(50_000m).Tracks[0];

        cash.Rows.Select(row => (row.BarStart, row.BarEnd)).ShouldBe(new[]
        {
            (0m, 100m),
            (83.41328m, 100m),
            (60.74536m, 83.41328m),
            (59.1586m, 60.74536m),
            (0m, 59.1586m),
        });
    }

    [Fact]
    public void From_gross50000_taxBaseBarsShareTheCashTrackScale()
    {
        var taxBase = View(50_000m).Tracks[1];

        taxBase.Rows.Select(row => (row.BarStart, row.BarEnd)).ShouldBe(new[]
        {
            (0m, 100m),
            (83.41328m, 100m),
            (80.91088m, 83.41328m),
            (0m, 80.91088m),
        });
    }

    // At 0 the minimum contributions make the net negative, so the scale runs from -3,561.68 to 0:
    // the gross is a bar of no width at zero, on the right, and the net fills the scale leftwards.
    [Fact]
    public void From_gross0_scaleReachesDownToTheNegativeNet()
    {
        var cash = View(0m).Tracks[0];

        cash.Rows.Select(row => (row.Key, row.BarStart, row.BarEnd)).ShouldBe(new[]
        {
            ("gross_pay", 100m, 100m),
            ("social_contributions", 0m, 100m),
            ("income_tax", 0m, 0m),
            ("municipal_surcharge", 0m, 0m),
            ("net_in_pocket", 0m, 100m),
        });
    }

    // The taxable income cannot be negative, so at 0 it is a bar of no width at zero although the
    // gross less the contributions is below it.
    [Fact]
    public void From_gross0_taxableIncomeStaysAtZero()
    {
        var taxBase = View(0m).Tracks[1];

        taxBase.Rows[^1].ShouldBe(new FunnelRow("taxable_income", RowKind.TaxableIncome, 0m, 100m, 100m));
    }

    [Fact]
    public void From_gross50000_splitsTheNextThousandIntoShares()
    {
        var view = View(50_000m);

        view.NextThousand.ShouldBe(new[]
        {
            new NextThousandPart("social_contributions", 165.88m, 0.16588m),
            new NextThousandPart("income_tax", 364.09m, 0.36409m),
            new NextThousandPart("municipal_surcharge", 25.48m, 0.02548m),
            new NextThousandPart("net_in_pocket", 444.55m, 0.44455m),
        });
    }

    [Fact]
    public void From_gross50000_detailsEveryStepInTheEngineOrder()
    {
        var view = View(50_000m);

        view.Details.Select(detail => (detail.RuleKey, detail.Amount)).ShouldBe(new[]
        {
            ("social_contributions", 8_293.36m),
            ("flat_rate_costs", 1_251.20m),
            ("taxable_income", 40_455.44m),
            ("income_tax", 11_333.96m),
            ("municipal_surcharge", 793.38m),
        });
    }

    // The step's own source comes first, then each other source in the order its amounts appear.
    [Fact]
    public void From_gross50000_groupsTheIncomeTaxAmountsUnderTheirSources()
    {
        var rules = PersonalTaxRules.IncomeYear2026;

        var incomeTax = View(50_000m).Details.Single(detail => detail.RuleKey == "income_tax");

        incomeTax.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (PersonalTaxRules.IncomeTaxScale, "bracket_1_rate bracket_1_upper_limit bracket_2_rate bracket_2_upper_limit "
                + "bracket_3_rate bracket_3_upper_limit bracket_4_rate"),
            (rules.TaxFreeAmount.Source, "tax_free_amount tax_free_amount_rate"),
            (rules.AutonomyFactor.Source, "autonomy_factor"),
            (rules.RegionalAdditionalRate.Source, "regional_additional_rate"),
        });
    }

    [Fact]
    public void From_gross50000_groupsTheFlatRateCostsAmountsUnderTheirSources()
    {
        var rules = PersonalTaxRules.IncomeYear2026;

        var flatRateCosts = View(50_000m).Details.Single(detail => detail.RuleKey == "flat_rate_costs");

        flatRateCosts.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (PersonalTaxRules.DirectorPayDeductions, "flat_rate_costs_rate"),
            (rules.FlatRateCostsMaximum.Source, "flat_rate_costs_maximum"),
        });
    }

    [Fact]
    public void From_gross50000_keepsTheSourceOfAStepWithNoAmountOfItsOwn()
    {
        var taxableIncome = View(50_000m).Details.Single(detail => detail.RuleKey == "taxable_income");

        taxableIncome.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (PersonalTaxRules.DirectorPayDeductions, ""),
        });
    }

    private static string Keys(SourceGroup group) => string.Join(' ', group.Amounts.Select(amount => amount.Key));

    private static GrossToNetView View(decimal grossPay)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.Entered(0.07m));

        error.ShouldBeNull();
        return GrossToNetView.From(value.ShouldNotBeNull());
    }
}
