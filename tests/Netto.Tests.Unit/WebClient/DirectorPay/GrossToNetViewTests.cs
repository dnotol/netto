using Netto.Core.DirectorPay;
using Netto.Core.MovableIncome;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.WebClient.DirectorPay;

// Income year 2026 at a 7% municipal rate, on the engine's own figures (GrossToNetTests): at 50,000
// the contributions are 8,293.36, the flat-rate costs 1,251.20, the taxable income 40,455.44, the
// income tax 11,333.96, the surcharge 793.38, no tax credit for low earned income and the net
// 29,579.30. At 20,000 the credit is 910 in full: the tax 1,098.78, the surcharge 76.91 and the net
// 16,172.63 on the contributions' minimum of 3,561.68. With the company paying the contributions
// on the 50,000, 9,942.52, they are pay: the pay costs the company 59,942.52, which is also the
// taxable pay, the flat-rate costs are 1,500, the taxable income 48,500, the tax 14,954.01, the
// surcharge 1,046.78 and the net 33,999.21. The statement and the assessment are PayStatementTests'
// and GrossToNetTests': 4,166.67 a month withheld on by 890.94 to October and 882.47 from November,
// 10,674.34 in the year, set against a final tax of 12,127.34, leaving 1,453.00 to pay.
public class GrossToNetViewTests
{
    [Fact]
    public void From_gross50000_cashTrackStartsFromTheGrossPay()
    {
        var view = View(50_000m);

        (view.CompanyPaysContributions, view.Tracks[0].Rows[0].Key, view.Tracks[1].Rows[0].Key).ShouldBe((false, "gross_pay", "gross_pay"));
    }

    // The contributions the company pays are pay that goes straight back out: the track starts from
    // what the pay costs the company and the contributions stay paid out, down to the same net the
    // gross alone leaves after the tax.
    [Fact]
    public void From_companyPaysGross50000_cashTrackStartsFromTheCompanysCost()
    {
        var cash = CompanyPaysView(50_000m).Tracks[0];

        cash.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("company_cost", RowKind.Gross, 59_942.52m),
            ("social_contributions", RowKind.PaidOut, 9_942.52m),
            ("income_tax", RowKind.PaidOut, 14_954.01m),
            ("municipal_surcharge", RowKind.PaidOut, 1_046.78m),
            ("low_earned_income_credit", RowKind.Back, 0m),
            ("income_tax_advance_payment_surcharge", RowKind.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", RowKind.Back, 0m),
            ("net_in_pocket", RowKind.NetInPocket, 33_999.21m),
        });
    }

    // The director declares the gross plus them and deducts them as their own contributions, so the
    // tax base track starts from that taxable pay too.
    [Fact]
    public void From_companyPaysGross50000_taxBaseTrackStartsFromTheTaxablePay()
    {
        var taxBase = CompanyPaysView(50_000m).Tracks[1];

        taxBase.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("company_cost", RowKind.Gross, 59_942.52m),
            ("social_contributions", RowKind.PaidOut, 9_942.52m),
            ("flat_rate_costs", RowKind.TaxBaseOnly, 1_500m),
            ("taxable_income", RowKind.TaxableIncome, 48_500m),
        });
    }

    [Fact]
    public void From_companyPaysGross50000_saysTheCompanyPaysTheContributions()
    {
        CompanyPaysView(50_000m).CompanyPaysContributions.ShouldBeTrue();
    }

    // The next 1,000 adds 198.84 to the contributions the company bears on top: the other five parts
    // add up to 1,000 and the shares, as today, say what each is of the 1,000, so together they go
    // over it; the bar's widths are shares of what takes money, which add up to one.
    [Fact]
    public void From_companyPaysGross50000_nextThousandSharesGoOverTheThousandByTheContributions()
    {
        var next = CompanyPaysView(50_000m).NextThousand;

        (next.Select(part => part.Amount), next.Sum(part => part.Share), next.Sum(part => part.Width))
            .ShouldSatisfyAllConditions(
                parts => parts.Item1.ShouldBe([198.84m, 436.49m, 30.56m, 0m, 0m, 532.95m]),
                parts => parts.Item2.ShouldBe(1.19884m),
                parts => parts.Item3.ShouldBe(1m));
    }

    [Fact]
    public void From_gross50000_statementListsTheTwelveMonthsOfTheYear()
    {
        var statement = View(50_000m).Statement;

        (statement.Months.Count, statement.Months[0].Month, statement.Months[0].GrossPay, statement.Months[0].Withholding, statement.Months[0].NetPaid)
            .ShouldBe((12, 1, 4_166.67m, 890.94m, 3_275.73m));
    }

    [Fact]
    public void From_gross50000_statementShowsNovemberAndDecemberByTheRulesFrom1November()
    {
        var statement = View(50_000m).Statement;

        statement.Months.Skip(10).Select(month => (month.Month, month.Withholding, month.NetPaid)).ShouldBe([(11, 882.47m, 3_284.20m), (12, 882.47m, 3_284.20m)]);
    }

    [Fact]
    public void From_gross50000_statementHasNoColumnForContributionsTheCompanyPays()
    {
        View(50_000m).Statement.CompanyPaysContributions.ShouldBeFalse();
    }

    [Fact]
    public void From_companyPaysGross50000_statementShowsTheContributionsTheCompanyPays()
    {
        var statement = CompanyPaysView(50_000m).Statement;

        (statement.CompanyPaysContributions, statement.Months[0].ContributionsPaidByCompany, statement.Months[0].TaxablePay)
            .ShouldBe((true, 828.54m, 4_995.21m));
    }

    // One withholding detail for each period of the year's rules, with the months it covers.
    [Fact]
    public void From_gross50000_withholdingHasADetailForEachPeriodWithItsMonths()
    {
        var withholding = View(50_000m).Withholding;

        withholding.Select(period => (period.FirstMonth, period.LastMonth, period.Detail.RuleKey, period.Detail.Amount))
            .ShouldBe([(1, 10, "withholding", 890.94m), (11, 12, "withholding", 882.47m)]);
    }

    // PayStatementTests: the formula's lines on 4,166.67, to October and from November.
    public static TheoryData<int, decimal, decimal, decimal> WithholdingByPeriod => new()
    {
        { 0, 2_987.98m, 10_691.27m, 890.94m },
        { 1, 3_089.63m, 10_589.62m, 882.47m },
    };

    [Theory]
    [MemberData(nameof(WithholdingByPeriod))]
    public void From_gross50000_withholdingDetailCarriesTheFormulasLinesForAMonthOfItsPeriod(int period, decimal taxFreeAmountReduction, decimal annualTax, decimal monthly)
    {
        var detail = View(50_000m).Withholding[period].Detail;

        detail.Lines.ShouldBe(
        [
            new StepLine("monthly_taxable_pay", 4_166.67m),
            new StepLine("reduction", 965.86m),
            new StepLine("annual_gross", 38_409.72m),
            new StepLine("flat_rate_costs", 1_152.29m),
            new StepLine("annual_net_taxable", 37_257.43m),
            new StepLine("basic_tax", 13_679.25m),
            new StepLine("tax_free_amount_reduction", taxFreeAmountReduction),
            new StepLine("annual_tax", annualTax),
            new StepLine("monthly_withholding", monthly),
        ]);
    }

    // The key formula first, with no amount of its own, then the annex's amounts under the number of
    // the annex each comes from; from November n° 32 under the coordinated annex.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 11)]
    public void From_gross50000_withholdingDetailCitesTheKeyFormulaThenTheAnnexsAmounts(int period, int month)
    {
        var rules = WithholdingRules.IncomeYear2026.InMonth(month);

        var detail = View(50_000m).Withholding[period].Detail;

        detail.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (rules.Source, ""),
            (rules.Reduction[0].UpperLimit!.Source, "reduction_minimum reduction_band_1_upper_limit reduction_band_2_rate reduction_band_2_upper_limit "
                + "reduction_band_3_base reduction_band_3_rate reduction_band_3_upper_limit reduction_maximum"),
            (rules.FlatRateCostsRate.Source, "flat_rate_costs_rate flat_rate_costs_maximum"),
            (rules.Scale[0].Rate!.Source, "withholding_bracket_1_rate withholding_bracket_1_upper_limit withholding_bracket_2_base withholding_bracket_2_rate "
                + "withholding_bracket_2_upper_limit withholding_bracket_3_base withholding_bracket_3_rate withholding_bracket_3_upper_limit "
                + "withholding_bracket_4_base withholding_bracket_4_rate"),
            (rules.TaxFreeAmountReduction.Source, "tax_free_amount_reduction"),
        });
    }

    // GrossToNetWithholdingQuotientTests: for a partner without professional income the formula's lines
    // on 4,166.67 run to the annual net taxable as for a single person, then the marital quotient's: the
    // income imputed to the partner, the tax on it, the tax on the rest, twice the tax on the tax-free
    // amount taken off both.
    public static TheoryData<int, decimal, decimal, decimal> QuotientWithholdingByPeriod => new()
    {
        { 0, 5_975.96m, 5_494.33m, 457.86m },
        { 1, 6_179.26m, 5_291.03m, 440.92m },
    };

    [Theory]
    [MemberData(nameof(QuotientWithholdingByPeriod))]
    public void From_gross50000PartnerWithoutIncome_withholdingDetailCarriesTheQuotientsLines(
        int period, decimal jointTaxFreeAmountReduction, decimal annualTax, decimal monthly)
    {
        var detail = HouseholdView(50_000m, NonEarningPartner).Withholding[period].Detail;

        detail.Lines.ShouldBe(
        [
            new StepLine("monthly_taxable_pay", 4_166.67m),
            new StepLine("reduction", 965.86m),
            new StepLine("annual_gross", 38_409.72m),
            new StepLine("flat_rate_costs", 1_152.29m),
            new StepLine("annual_net_taxable", 37_257.43m),
            new StepLine("imputed_to_partner", 11_177.23m),
            new StepLine("partner_tax", 2_989.91m),
            new StepLine("rest_tax", 8_480.38m),
            new StepLine("joint_tax_free_amount_reduction", jointTaxFreeAmountReduction),
            new StepLine("annual_tax", annualTax),
            new StepLine("monthly_withholding", monthly),
        ]);
    }

    // GrossToNetWithholdingDependantsTests: a single parent of two takes off the year's tax n° 38's
    // reduction for two children and n° 40, 1's, each its own line after the tax-free amount's; a
    // reduction that does not apply has no line.
    public static TheoryData<int, decimal, decimal, decimal, decimal, decimal> DependantsWithholdingByPeriod => new()
    {
        { 0, 2_987.98m, 1_656m, 624m, 8_411.27m, 700.94m },
        { 1, 3_089.63m, 1_644m, 636m, 8_309.62m, 692.47m },
    };

    [Theory]
    [MemberData(nameof(DependantsWithholdingByPeriod))]
    public void From_gross50000SingleParentOfTwo_withholdingDetailCarriesTheReductionsLines(
        int period, decimal taxFreeAmountReduction, decimal childrenReduction, decimal singleParentReduction, decimal annualTax, decimal monthly)
    {
        var detail = HouseholdView(50_000m, SingleParentOfTwo).Withholding[period].Detail;

        detail.Lines.ShouldBe(
        [
            new StepLine("monthly_taxable_pay", 4_166.67m),
            new StepLine("reduction", 965.86m),
            new StepLine("annual_gross", 38_409.72m),
            new StepLine("flat_rate_costs", 1_152.29m),
            new StepLine("annual_net_taxable", 37_257.43m),
            new StepLine("basic_tax", 13_679.25m),
            new StepLine("tax_free_amount_reduction", taxFreeAmountReduction),
            new StepLine("children_reduction", childrenReduction),
            new StepLine("single_parent_reduction", singleParentReduction),
            new StepLine("annual_tax", annualTax),
            new StepLine("monthly_withholding", monthly),
        ]);
    }

    // The other dependants' reduction of n° 39 under the quotient, after its lines.
    [Fact]
    public void From_gross50000PartnerWithoutIncomeOtherDependant_withholdingDetailCarriesTheDependantsReduction()
    {
        var detail = HouseholdView(50_000m, NonEarningPartner with { Dependants = new Dependants(OtherDependants: 1) }).Withholding[0].Detail;

        detail.Lines.Select(line => line.Key).SkipWhile(key => key != "joint_tax_free_amount_reduction").ShouldBe(
            ["joint_tax_free_amount_reduction", "dependants_reduction", "annual_tax", "monthly_withholding"]);
    }

    // The reductions' amounts last, each under its number of the annex: to October as decreed, from
    // November the coordinated annex.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 11)]
    public void From_gross50000SingleParentOfTwo_withholdingDetailCitesTheReductionsAmountsLast(int period, int month)
    {
        var rules = WithholdingRules.IncomeYear2026.InMonth(month);

        var detail = HouseholdView(50_000m, SingleParentOfTwo).Withholding[period].Detail;

        detail.Sources.TakeLast(2).Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (rules.ChildReductions[1].Source, "withholding_child_reduction_2"),
            (rules.SingleParentReduction.Source, "withholding_single_parent_reduction"),
        });
    }

    // The quotient's amounts in place of n° 32: to October all under the annex as decreed, from
    // November under the coordinated annex, the maximum for partners both 66 the one cited for them.
    [Theory]
    [InlineData(0, 1, false, "withholding_quotient_rate withholding_quotient_maximum withholding_joint_tax_free_amount_reduction")]
    [InlineData(1, 11, false, "withholding_quotient_rate withholding_quotient_maximum withholding_joint_tax_free_amount_reduction")]
    [InlineData(1, 11, true, "withholding_quotient_rate withholding_quotient_maximum_both_aged_66 withholding_joint_tax_free_amount_reduction")]
    public void From_gross50000PartnerWithoutIncome_withholdingDetailCitesTheQuotientsAmountsLast(int period, int month, bool bothAged66, string keys)
    {
        var rules = WithholdingRules.IncomeYear2026.InMonth(month);

        var detail = HouseholdView(50_000m, NonEarningPartner with { BothAged66 = bothAged66 }).Withholding[period].Detail;

        (detail.Sources[^1].Source, Keys(detail.Sources[^1])).ShouldBe((rules.QuotientRate.Source, keys));
    }

    [Fact]
    public void From_gross50000WithholdingAsTheKeyFormula_balanceIsTheFinalTaxLessWhatWasWithheldToPay()
    {
        var balance = View(50_000m, TaxPrepayments.AsWithheld).Balance;

        (balance.FinalTax, balance.Withheld, balance.WithheldGiven, balance.AdvancePayments, balance.Balance, balance.Kind)
            .ShouldBe((12_127.34m, 10_674.34m, false, 0m, 1_453.00m, BalanceKind.ToPay));
    }

    // GrossToNetTests: 20,000 withheld is 7,872.66 beyond the tax.
    [Fact]
    public void From_gross50000Withheld20000_balanceIsRefundedAndSaysTheAmountWasGiven()
    {
        var balance = View(50_000m, new TaxPrepayments(20_000m, AdvancePayments.None)).Balance;

        (balance.Withheld, balance.WithheldGiven, balance.Balance, balance.Kind, balance.Amount)
            .ShouldBe((20_000m, true, -7_872.66m, BalanceKind.Refunded, 7_872.66m));
    }

    [Fact]
    public void From_gross50000WithheldTheFinalTax_balanceIsSettled()
    {
        var balance = View(50_000m, new TaxPrepayments(12_127.34m, AdvancePayments.None)).Balance;

        (balance.Balance, balance.Kind).ShouldBe((0m, BalanceKind.Settled));
    }

    // 12,014 by 10 April: no surcharge, a bonus of 90.11 and 23.23 left to pay.
    [Fact]
    public void From_gross50000PaidInAdvance_balanceSetsThePaymentsAgainstTheTax()
    {
        var balance = View(50_000m, new TaxPrepayments(0m, new AdvancePayments(12_014m, 0m, 0m, 0m))).Balance;

        (balance.FinalTax, balance.AdvancePayments, balance.Balance).ShouldBe((12_037.23m, 12_014m, 23.23m));
    }

    // A rent's 828 of net rent taxed with the pay (GrossToNetTests): the final tax is the year's,
    // 398.68 of it the rent's, and the balance sets the pay's withholding against all of it.
    [Fact]
    public void From_gross50000WithRent_balanceSaysHowMuchOfTheFinalTaxIsTheRents()
    {
        var balance = View(50_000m, TaxPrepayments.AsWithheld, otherIncome: 828m).Balance;

        (balance.FinalTax, balance.OnOtherIncome, balance.Balance).ShouldBe((12_526.02m, 398.68m, 1_851.68m));
    }

    [Fact]
    public void From_gross50000_balanceHasNothingOnOtherIncome()
    {
        View(50_000m, TaxPrepayments.AsWithheld).Balance.OnOtherIncome.ShouldBe(0m);
    }

    // The next 1,000 is the year's once a rent is taxed with the pay: the slip says so.
    [Theory]
    [InlineData(0, false)]
    [InlineData(828, true)]
    public void From_gross50000_saysWhetherTheNextThousandIsTaxedWithARent(decimal otherIncome, bool taxedWithOtherIncome)
    {
        View(50_000m, Withheld, otherIncome: otherIncome).TaxedWithOtherIncome.ShouldBe(taxedWithOtherIncome);
    }

    [Fact]
    public void From_gross50000_balanceDetailCitesArticle304()
    {
        var detail = View(50_000m, TaxPrepayments.AsWithheld).Balance.Detail;

        detail.ShouldSatisfyAllConditions(
            balance => (balance.RuleKey, balance.Amount, balance.Lines.Count).ShouldBe(("assessment_balance", 1_453.00m, 0)),
            balance => balance.Sources.Select(group => (group.Source, Keys(group))).ShouldBe([(PersonalTaxRules.IncomeYear2026.AssessmentBalance, "")]));
    }

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
            ("income_tax_advance_payment_surcharge", RowKind.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", RowKind.Back, 0m),
            ("net_in_pocket", RowKind.NetInPocket, 29_579.30m),
        });
    }

    // Each row's share of the gross the track starts from, the tax base's as well as the cash's.
    [Fact]
    public void From_gross50000_eachRowSaysItsShareOfTheGross()
    {
        var view = View(50_000m);

        view.Tracks.Select(track => track.Rows.Select(row => (row.Key, row.Share))).ShouldBe(new[]
        {
            new (string, decimal?)[]
            {
                ("gross_pay", 1m), ("social_contributions", 0.1658672m), ("income_tax", 0.2266792m), ("municipal_surcharge", 0.0158676m),
                ("low_earned_income_credit", 0m), ("income_tax_advance_payment_surcharge", 0m), ("income_tax_advance_payment_bonus", 0m),
                ("net_in_pocket", 0.591586m),
            },
            new (string, decimal?)[] { ("gross_pay", 1m), ("social_contributions", 0.1658672m), ("flat_rate_costs", 0.025024m), ("taxable_income", 0.8091088m) },
        });
    }

    // With the company paying the contributions both tracks start from what the pay costs it.
    [Fact]
    public void From_companyPaysGross50000_sharesAreOfTheCompanysCost()
    {
        var cash = CompanyPaysView(50_000m).Tracks[0];

        cash.Rows[^1].Share.ShouldBe(33_999.21m / 59_942.52m);
    }

    // A pay of nothing has no share to take of it.
    [Fact]
    public void From_gross0_saysNoShares()
    {
        View(0m).Tracks.SelectMany(track => track.Rows).ShouldAllBe(row => row.Share == null);
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
            ("income_tax_advance_payment_surcharge", RowKind.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", RowKind.Back, 0m),
            ("net_in_pocket", RowKind.NetInPocket, 16_172.63m),
        });
    }

    // What comes back is drawn from the balance it finds, 15,262.63, up to the net it leaves; it is
    // 4.55% of the gross.
    [Fact]
    public void From_gross20000_creditBarRisesFromTheBalanceToTheNet()
    {
        var cash = View(20_000m).Tracks[0];

        cash.Rows.Single(row => row.Key == "low_earned_income_credit").ShouldBe(
            new FunnelRow("low_earned_income_credit", RowKind.Back, 910m, 76.31315m, 80.86315m) { Share = 0.0455m });
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
            (59.1586m, 59.1586m),
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
            ("income_tax_advance_payment_surcharge", 0m, 0m),
            ("income_tax_advance_payment_bonus", 0m, 0m),
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

    // The fund's fee at 3.05% is deducted with the contributions (GrossToNetFundFeeTests): the tax base
    // track takes it off right after them, and the cash track pays it out there too.
    [Fact]
    public void From_gross50000WithAFundFee_bothTracksTakeTheFeeOffAfterTheContributions()
    {
        var view = View(50_000m, Withheld, fundFeeRate: 0.0305m);

        view.ShouldSatisfyAllConditions(
            () => view.Tracks[1].Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
            {
                ("gross_pay", RowKind.Gross, 50_000m),
                ("social_contributions", RowKind.PaidOut, 8_251.60m),
                ("fund_fee", RowKind.PaidOut, 251.68m),
                ("flat_rate_costs", RowKind.TaxBaseOnly, 1_244.90m),
                ("taxable_income", RowKind.TaxableIncome, 40_251.82m),
            }),
            () => view.Tracks[0].Rows.Select(row => row.Key).Take(3).ShouldBe(["gross_pay", "social_contributions", "fund_fee"]));
    }

    // Whether the pay holds the fund's fee, for the next 1,000's note to name it.
    [Theory]
    [InlineData(null, false)]
    [InlineData(0.0305, true)]
    public void From_gross50000_saysWhetherThePayHoldsTheFundsFee(double? rate, bool withFee)
    {
        View(50_000m, Withheld, ContributionsPaidBy.Company, fundFeeRate: (decimal?)rate).WithFundFee.ShouldBe(withFee);
    }

    // The next 1,000 adds to the fee too, a part of its own after the contributions', and the parts still
    // make the 1,000; without a fee there is no such part (above).
    [Fact]
    public void From_gross50000WithAFundFee_nextThousandHasTheFeesPartAfterTheContributions()
    {
        var parts = View(50_000m, Withheld, fundFeeRate: 0.0305m).NextThousand;

        parts.ShouldSatisfyAllConditions(
            () => parts.Select(part => part.Key).Take(3).ShouldBe(["social_contributions", "fund_fee", "income_tax"]),
            () => parts.Sum(part => part.Amount).ShouldBe(1_000m));
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
            new NextThousandPart("income_tax_advance_payments", 0m, 0m, 0m),
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
            ("income_tax_advance_payments", 0m, 0m, 0m),
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

    // A single parent of two, one under 3 (GrossToNetDependantsTests): at 24,000 the credit for children is
    // 241.05, which the next 1,000 takes away whole, a part of its own after the credit for low earned
    // income's; the parts still make the 1,000.
    [Fact]
    public void From_creditForChildrenTheNextThousandTakesAway_isAPartAndThePartsMakeTheThousand()
    {
        var parts = HouseholdView(24_000m, SingleParentOfTwo).NextThousand;

        parts.ShouldSatisfyAllConditions(
            () => parts.Select(part => part.Key).ShouldBe(
                ["social_contributions", "income_tax", "municipal_surcharge", "low_earned_income_credit_lost", "child_tax_credit_lost", "income_tax_advance_payments", "net_in_pocket"]),
            () => parts.Single(part => part.Key == "child_tax_credit_lost").Amount.ShouldBe(241.05m),
            () => parts.Sum(part => part.Amount).ShouldBe(1_000m));
    }

    // At 20,000 the credit reaches its cap, 1,100, and comes back to the pocket after the taxes.
    [Fact]
    public void From_singleParentOnALowPay_drawsTheCreditForChildrenComingBack()
    {
        var row = HouseholdView(20_000m, SingleParentOfTwo).Tracks[0].Rows.Single(row => row.Key == "child_tax_credit");

        (row.Kind, row.Amount).ShouldBe((RowKind.Back, 1_100m));
    }

    // In a joint year the pay's share of the household's credit can be less than nothing: what the pay takes
    // away of it is paid out, under words of its own.
    [Fact]
    public void From_creditForChildrenThePayTakesAway_drawsItPaidOut()
    {
        var (value, _) = GrossToNet.Compute(2026, 20_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: SingleParentOfTwo);
        var pay = value.ShouldNotBeNull();
        var takingAway = pay with { Steps = [.. pay.Steps.Select(step => step.RuleKey == "child_tax_credit" ? step with { Amount = 120m } : step)] };

        var row = GrossToNetView.From(takingAway).Tracks[0].Rows.Single(row => row.Key.StartsWith("child_tax_credit", StringComparison.Ordinal));

        (row.Key, row.Kind, row.Amount).ShouldBe(("child_tax_credit_taken_away", RowKind.PaidOut, 120m));
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
            ("income_tax_advance_payment_surcharge", 0m),
            ("income_tax_advance_payment_bonus", 0m),
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
            (PersonalTaxRules.IncomeYear2026.IncomeTaxScale, "bracket_1_rate bracket_1_upper_limit bracket_2_rate bracket_2_upper_limit "
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
            (PersonalTaxRules.IncomeYear2026.DirectorPayDeductions, "flat_rate_costs_rate flat_rate_costs_maximum"),
        });
    }

    [Fact]
    public void From_gross50000_keepsTheSourceOfAStepWithNoAmountOfItsOwn()
    {
        var taxableIncome = View(50_000m).Details.Single(detail => detail.RuleKey == "taxable_income");

        taxableIncome.Sources.Select(group => (group.Source, Keys(group))).ShouldBe(new[]
        {
            (PersonalTaxRules.IncomeYear2026.DirectorPayDeductions, ""),
        });
    }

    // A car's benefit of 2,400 joins the tax base after the gross, never the cash: the rest of the tax
    // base track is that of a cash pay of 52,400 (GrossToNetBenefitInKindTests).
    [Fact]
    public void From_gross50000WithABenefit_taxBaseTrackAddsTheBenefitToTheGross()
    {
        var asCash = View(52_400m).Tracks[1].Rows;

        var taxBase = View(50_000m, Withheld, benefit: 2_400m).Tracks[1].Rows;

        taxBase.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_pay", RowKind.Gross, 50_000m),
            ("benefit_in_kind", RowKind.Added, 2_400m),
            ("social_contributions", RowKind.PaidOut, asCash[1].Amount),
            ("flat_rate_costs", RowKind.TaxBaseOnly, asCash[2].Amount),
            ("taxable_income", RowKind.TaxableIncome, asCash[3].Amount),
        });
    }

    // The benefit's bar rises from the gross to the gross with it.
    [Fact]
    public void From_gross50000WithABenefit_benefitBarRisesFromTheGross()
    {
        var rows = View(50_000m, Withheld, benefit: 2_400m).Tracks[1].Rows;

        (rows[1].BarStart, rows[1].BarEnd).ShouldBe((rows[0].BarEnd, rows[0].BarEnd + (rows[0].BarEnd - rows[0].BarStart) * 2_400m / 50_000m));
    }

    // The cash track starts from the gross and ends on what is paid, the benefit's tax taken.
    [Fact]
    public void From_gross50000WithABenefit_cashTrackHasNoBenefit()
    {
        var cash = View(50_000m, Withheld, benefit: 2_400m).Tracks[0].Rows;

        (cash[0].Amount, cash.Any(row => row.Key == "benefit_in_kind"), cash[^1].Kind).ShouldBe((50_000m, false, RowKind.NetInPocket));
    }

    [Fact]
    public void From_gross50000WithABenefit_statementShowsItsTwelfth()
    {
        var statement = View(50_000m, TaxPrepayments.AsWithheld, benefit: 2_400m).Statement;

        (statement.HasBenefit, statement.Months[0].BenefitInKind, statement.Months[0].TaxablePay).ShouldBe((true, 200m, 4_366.67m));
    }

    [Fact]
    public void From_gross50000_hasNoBenefitRowNorColumn()
    {
        var view = View(50_000m);

        (view.Statement.HasBenefit, view.Tracks[1].Rows.Any(row => row.Key == "benefit_in_kind")).ShouldBe((false, false));
    }

    // A royalty's part beyond its limits, 5,000, is pay paid besides the payslip: it joins both tracks
    // after the gross, and the rest of each is that of a cash pay of 55,000 (GrossToNetMovableIncomeTests).
    [Fact]
    public void From_gross50000WithARoyaltyBeyondItsLimits_bothTracksAddThePartTaxedAsPay()
    {
        var view = View(50_000m, Withheld, movable: new MovableIncomePaid(5_000m, null, 5_000m, 1_500m));

        view.Tracks.Select(track => track.Rows.Take(2).Select(row => (row.Key, row.Kind, row.Amount)).ToList()).ShouldBe(new[]
        {
            new List<(string, RowKind, decimal)> { ("gross_pay", RowKind.Gross, 50_000m), ("requalified_pay", RowKind.Added, 5_000m) },
            [("gross_pay", RowKind.Gross, 50_000m), ("requalified_pay", RowKind.Added, 5_000m)],
        });
    }

    [Fact]
    public void From_gross50000WithARoyaltyBeyondItsLimits_endsOnTheCashPaysNetAndTaxableIncome()
    {
        var asCash = View(55_000m);

        var view = View(50_000m, Withheld, movable: new MovableIncomePaid(5_000m, null, 5_000m, 1_500m));

        (view.Tracks[0].Rows[^1].Amount, view.Tracks[1].Rows[^1].Amount).ShouldBe((asCash.Tracks[0].Rows[^1].Amount, asCash.Tracks[1].Rows[^1].Amount));
    }

    // The shares are out of what the tracks start from, the gross and that part.
    [Fact]
    public void From_gross50000WithARoyaltyBeyondItsLimits_sharesAreOutOfTheGrossAndThatPart()
    {
        var view = View(50_000m, Withheld, movable: new MovableIncomePaid(5_000m, null, 5_000m, 1_500m));

        view.Tracks[0].Rows[1].Share.ShouldBe(5_000m / 55_000m);
    }

    // With a royalty within its limits, the balance says how much of the final tax is the royalty's
    // and what the company withheld on it.
    [Fact]
    public void From_gross50000WithARoyaltyWithinItsLimits_balanceSaysTheRoyaltysTaxAndWithholding()
    {
        var (pay, _) = GrossToNet.Compute(
            2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Director, new TaxedWithPay { Movable = Royalty10000() });
        var balance = GrossToNetView.From(pay!).Balance;

        (balance.OnTaxedApart, balance.WithheldBesidesPay).ShouldBe((pay!.TaxedApartTax.Total, 1_500m));
    }

    [Fact]
    public void From_gross50000_hasNoRoyaltyOnTheBalance()
    {
        var balance = View(50_000m).Balance;

        (balance.OnTaxedApart, balance.WithheldBesidesPay).ShouldBe((0m, 0m));
    }

    private static MovableIncomePaid Royalty10000() =>
        new(10_000m, new IncomeTaxedApart(10_000m, Netto.Core.Copyright.CopyrightRules.IncomeYear2026.TaxedApartRate), 0m, 1_500m);

    private static string Keys(SourceGroup group) => string.Join(' ', group.Amounts.Select(amount => amount.Key));

    private static readonly Household SingleParentOfTwo = Household.None with { Dependants = new Dependants(Children: 2, ChildrenUnder3: 1, SingleParent: true) };

    private static GrossToNetView View(decimal grossPay) => View(grossPay, Withheld);

    private static GrossToNetView CompanyPaysView(decimal grossPay) => View(grossPay, Withheld, ContributionsPaidBy.Company);

    private static readonly Household SingleParentOfTwo = Household.None with { Dependants = new Dependants(Children: 2, SingleParent: true) };

    private static GrossToNetView HouseholdView(decimal grossPay, Household household)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: household);

        error.ShouldBeNull();
        return GrossToNetView.From(value.ShouldNotBeNull());
    }

    private static GrossToNetView View(
        decimal grossPay, TaxPrepayments prepayments, ContributionsPaidBy paidBy = ContributionsPaidBy.Director, decimal otherIncome = 0m, decimal benefit = 0m,
        MovableIncomePaid? movable = null, decimal? fundFeeRate = null)
    {
        var (value, error) = GrossToNet.Compute(
            2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments, paidBy,
            new TaxedWithPay { OtherIncome = otherIncome, BenefitInKind = benefit, Movable = movable ?? MovableIncomePaid.None }, fundFeeRate: fundFeeRate);

        error.ShouldBeNull();
        return GrossToNetView.From(value.ShouldNotBeNull());
    }
}
