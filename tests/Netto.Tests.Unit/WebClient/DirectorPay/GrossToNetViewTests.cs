using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// Income year 2026 at a 7% municipal rate, on the engine's own figures (GrossToNetTests): at 50,000
// the contributions are 8,293.36, the flat-rate costs 1,251.20, the taxable income 40,455.44, the
// income tax 11,333.96, the surcharge 793.38, no tax credit for low earned income and the net
// 29,579.30. At 20,000 the credit is 910 in full: the tax 1,098.78, the surcharge 76.91 and the net
// 16,172.63 on the contributions' minimum of 3,561.68.
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
            ("low_earned_income_credit", RowKind.Back, 0m),
            ("net_in_pocket", RowKind.NetInPocket, 29_579.30m),
        });
    }

    [Fact]
    public void From_gross20000_cashTrackShowsTheCreditComingBack()
    {
        var cash = View(20_000m).Tracks[0];

        cash.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_pay", RowKind.Gross, 20_000m),
            ("social_contributions", RowKind.PaidOut, 3_561.68m),
            ("income_tax", RowKind.PaidOut, 1_098.78m),
            ("municipal_surcharge", RowKind.PaidOut, 76.91m),
            ("low_earned_income_credit", RowKind.Back, 910m),
            ("net_in_pocket", RowKind.NetInPocket, 16_172.63m),
        });
    }

    // What comes back is drawn from the balance it finds, 15,262.63, up to the net it leaves.
    [Fact]
    public void From_gross20000_creditBarRisesFromTheBalanceToTheNet()
    {
        var cash = View(20_000m).Tracks[0];

        cash.Rows.Single(row => row.Key == "low_earned_income_credit").ShouldBe(
            new FunnelRow("low_earned_income_credit", RowKind.Back, 910m, 76.31315m, 80.86315m));
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
            (59.1586m, 59.1586m),
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
            ("low_earned_income_credit", 0m, 0m),
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
            new NextThousandPart("social_contributions", 165.88m, 0.16588m, 0.16588m),
            new NextThousandPart("income_tax", 364.09m, 0.36409m, 0.36409m),
            new NextThousandPart("municipal_surcharge", 25.48m, 0.02548m, 0.02548m),
            new NextThousandPart("low_earned_income_credit_lost", 0m, 0m, 0m),
            new NextThousandPart("net_in_pocket", 444.55m, 0.44455m, 0.44455m),
        });
    }

    // At 11,000 the taxable income, 7,215.17, lies where the credit rises: the next 1,000 brings
    // 392.31 more of it (204.31 to 596.62), so the pocket gains 1,392.31. The bar draws only what
    // takes money, here the pocket alone; the shares still say what each part is of the 1,000.
    [Fact]
    public void From_grossWhereCreditRises_barDrawsOnlyThePartsThatTakeMoney()
    {
        var next = View(11_000m).NextThousand;

        next.Select(part => (part.Key, part.Amount, part.Share, part.Width)).ShouldBe(new[]
        {
            ("social_contributions", 0m, 0m, 0m),
            ("income_tax", 0m, 0m, 0m),
            ("municipal_surcharge", 0m, 0m, 0m),
            ("low_earned_income_credit_lost", -392.31m, -0.39231m, 0m),
            ("net_in_pocket", 1_392.31m, 1.39231m, 1m),
        });
    }

    // A part of no width would still draw the gap that separates segments.
    [Fact]
    public void From_gross50000_barLeavesOutThePartsOfNoWidth()
    {
        var bar = View(50_000m).NextThousandBar;

        bar.Select(part => part.Key).ShouldBe(["social_contributions", "income_tax", "municipal_surcharge", "net_in_pocket"]);
    }

    [Fact]
    public void From_grossWhereCreditFalls_drawsTheCreditLostAtItsShare()
    {
        var lost = View(30_000m).NextThousand.Single(part => part.Key == "low_earned_income_credit_lost");

        (lost.Amount > 0m, lost.Width).ShouldBe((true, lost.Share));
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
            ("low_earned_income_credit", 0m),
        });
    }

    [Fact]
    public void From_gross50000_groupsTheCreditAmountsUnderArticle289ter()
    {
        var rules = PersonalTaxRules.IncomeYear2026;

        var credit = View(50_000m).Details.Single(detail => detail.RuleKey == "low_earned_income_credit");

        credit.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (rules.LowEarnedIncomeCredit.Source, "low_earned_income_credit low_earned_income_floor low_earned_income_full_from "
                + "low_earned_income_full_up_to low_earned_income_ceiling"),
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
            (rules.TaxFreeAmount.Source, "tax_free_amount"),
            (rules.TaxFreeAmountRate.Source, "tax_free_amount_rate"),
            (rules.AutonomyFactor.Source, "autonomy_factor"),
            (rules.RegionalAdditionalRate.Source, "regional_additional_rate"),
        });
    }

    [Fact]
    public void From_gross50000_groupsTheFlatRateCostsAmountsUnderTheirSources()
    {
        var flatRateCosts = View(50_000m).Details.Single(detail => detail.RuleKey == "flat_rate_costs");

        flatRateCosts.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (PersonalTaxRules.DirectorPayDeductions, "flat_rate_costs_rate flat_rate_costs_maximum"),
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
