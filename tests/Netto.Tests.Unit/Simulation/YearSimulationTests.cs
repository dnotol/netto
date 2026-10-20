using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Simulation;

// Income year 2026 at a 7% municipal rate, for a made-up company with 100,000 of profit before pay
// unless a test says otherwise. Director pay figures are GrossToNetTests' (50,000: contributions
// 8,293.36, income tax 11,333.96, surcharge 793.38, net 29,579.30); the company and dividend
// figures follow CorporateTaxCalculatorTests and DividendCalculatorTests. The dividend is what the
// profit after tax leaves once the reserve is kept in the company. The company prepays enough by
// 10 April to leave no advance-payment surcharge unless a test says otherwise, and withholds more
// than the director pay's tax; the surcharge figures follow AdvancePaymentCalculatorTests and
// IncomeTaxAdvancePaymentCalculatorTests. The director pays the contributions unless a test says the
// company does; those figures are GrossToNetTests' too (50,000: contributions 9,942.52, income tax
// 14,954.01, surcharge 1,046.78, net 33,999.21, company cost 59,942.52).
public class YearSimulationTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    // Its credit, 9,000, covers the surcharge on any tax these tests reach.
    private static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    public static TheoryData<decimal, decimal, decimal, WithholdingCategory> Setups => new()
    {
        { 50_000m, 100_000m, 0m, WithholdingCategory.VvprbisThirdYearOnwards },
        { 21_000m, 100_000m, 20_000m, WithholdingCategory.Standard },
        { 160_000m, 50_000m, 0m, WithholdingCategory.Standard },
        { 0m, 300_000m, 500_000m, WithholdingCategory.VvprbisSecondYear },
    };

    // The Lever catalogue's scenario (ii) without the fund fee: taxable income 50,000 does not
    // exceed the pay, so the company keeps 20% although the pay is under 51,000.
    [Fact]
    public void Run_2026Pay50000AndNothingKept_leavesItAllInThePocketOrPaidOut()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        (result.CorporateTax, result.SocialContributions, result.PersonalTax, result.NetInPocket, result.KeptInCompany)
            .ShouldBe((10_000.00m, 8_293.36m, 19_177.40m, 62_529.24m, 0m));
    }

    [Fact]
    public void Run_2026Pay50000AndNothingKept_saysWhichConditionKeptTheReducedRate()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Company.Condition.ShouldBe(ReducedRateCondition.TaxableIncomeNotAbovePay);
    }

    [Fact]
    public void Run_2026PayUnderTheMinimumAndTaxableAbovePay_pays25Percent()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m));

        (result.Company.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 17_500.00m));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    [Fact]
    public void Run_2026_directorPayIsWhatGrossToNetGives()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), Withheld);

        (result.DirectorPay.SocialContributions, result.DirectorPay.IncomeTax, result.DirectorPay.MunicipalSurcharge, result.DirectorPay.NetInPocket)
            .ShouldBe((pay!.SocialContributions, pay.IncomeTax, pay.MunicipalSurcharge, pay.NetInPocket));
    }

    [Fact]
    public void Run_2026_returnsTheStepsInTheLawsOrderWithTheirTags()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Steps.Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 10_000.00m),
            ("advance_payment_surcharge", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 0m),
            ("social_contributions", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 8_293.36m),
            ("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 1_251.20m),
            ("taxable_income", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 40_455.44m),
            ("income_tax", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 11_333.96m),
            ("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 793.38m),
            ("low_earned_income_credit", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 0m),
            ("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -0m),
            ("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, 7_200.00m),
            ("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -149.94m),
        });
    }

    // 6.75% of the 10,000 of corporate tax, which the year counts as a corporate tax of its own.
    [Fact]
    public void Run_2026NothingPrepaid_surchargesTheCorporateTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: AdvancePayments.None));

        (result.Company.CorporateTax, result.AdvancePayments.Surcharge, result.CorporateTax).ShouldBe((10_000.00m, 675.00m, 10_675.00m));
    }

    [Fact]
    public void Run_2026NothingPrepaid_theSurchargeIsACorporateStepOnTheCompanysProfit()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: AdvancePayments.None));

        result.Steps.Single(step => step.RuleKey == "advance_payment_surcharge")
            .ShouldSatisfyAllConditions(
                step => (step.Layer, step.Event, step.Track, step.Amount).ShouldBe((Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 675.00m)),
                step => step.AmountsUsed.Select(amount => amount.Key).ShouldContain("surcharge_multiplier"));
    }

    [Fact]
    public void Run_2026NothingPrepaid_theSurchargeComesOutOfTheProfitAfterTaxAndTheDividend()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: AdvancePayments.None));

        (result.ProfitAfterTax, result.Dividend.Dividend).ShouldBe((39_325.00m, 39_325.00m));
    }

    // The surcharge is not deductible: the taxable income, and the condition of art. 215 tested on
    // it, are those of a company that prepaid.
    [Fact]
    public void Run_2026NothingPrepaid_leavesTheTaxableIncomeAndTheRateAsTheyAre()
    {
        var prepaid = Run(Scenario(directorPay: 30_000m, reserve: 0m));
        var notPrepaid = Run(Scenario(directorPay: 30_000m, reserve: 0m, advancePayments: AdvancePayments.None));

        (notPrepaid.Company.TaxableIncome, notPrepaid.Company.Condition, notPrepaid.Company.CorporateTax)
            .ShouldBe((prepaid.Company.TaxableIncome, prepaid.Company.Condition, prepaid.Company.CorporateTax));
    }

    [Fact]
    public void Run_2026PaidByAprilAndJulyOnly_saysWhatBy21DecemberCancelsTheRest()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: new AdvancePayments(2_000m, 2_000m, 0m, 0m)));

        (result.AdvancePayments.Surcharge, result.AdvancePayments.LastDeadlineTopUp).ShouldBe((345.00m, 7_666.67m));
    }

    [Fact]
    public void Run_2026SmallCompanyInItsFirstThreeFinancialYears_paysNoSurcharge()
    {
        var young = new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true, FirstThreeFinancialYears: true);

        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: AdvancePayments.None) with { Company = young });

        (result.AdvancePayments.Exempt, result.CorporateTax).ShouldBe((true, 10_000.00m));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026NothingPrepaid_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m,
            advancePayments: AdvancePayments.None));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    // The 486.57 surcharge on the pay's 11,333.96 of tax is a personal tax, out of the pocket.
    [Fact]
    public void Run_2026NothingWithheldOrPrepaidOnThePay_surchargesItsTaxOutOfThePocket()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, taxPrepayments: TaxPrepayments.None));

        (result.DirectorPay.AdvancePayments.Surcharge, result.PersonalTax, result.NetInPocket).ShouldBe((486.57m, 19_663.97m, 62_042.67m));
    }

    // Nothing given as withheld: the company withholds the key formula's 10,691.28 on the 50,000
    // (GrossToNetTests), which leaves no surcharge and 1,436.06 to pay on the assessment; the pocket is
    // that of a pay whose tax was withheld.
    [Fact]
    public void Run_2026WithholdingAsTheKeyFormula_withholdsOnThePayAndLeavesItsBalance()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, taxPrepayments: TaxPrepayments.AsWithheld));

        (result.DirectorPay.Assessment.Withheld, result.DirectorPay.Assessment.WithheldGiven, result.DirectorPay.Assessment.Balance, result.NetInPocket)
            .ShouldBe((10_691.28m, false, 1_436.06m, 62_529.24m));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026WithholdingAsTheKeyFormula_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m,
            taxPrepayments: TaxPrepayments.AsWithheld));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026NothingWithheldOrPrepaidOnThePay_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m,
            taxPrepayments: TaxPrepayments.None));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    // The company's cost, 59,942.52, comes off the profit, and the taxable pay, the same amount, meets
    // the 51,000 minimum, so the 40,057.48 left is taxed at 20%.
    [Fact]
    public void Run_2026CompanyPaysContributions_takesItsCostOffTheProfitAndTestsTheMinimumPayOnTheTaxablePay()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, paidBy: ContributionsPaidBy.Company));

        (result.DirectorPay.CompanyCost, result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax)
            .ShouldBe((59_942.52m, 40_057.48m, ReducedRateCondition.MinimumPayMet, 8_011.50m));
    }

    // The contributions are the social layer and leave the company, not the pocket, which gets the
    // pay's net, 33,999.21, and the net dividend out of the 32,045.98 left after tax, withheld at 18%
    // with the refund on the exempt 833.
    [Fact]
    public void Run_2026CompanyPaysContributions_theyAreTheSocialLayerAndLeaveTheCompanyNotThePocket()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, paidBy: ContributionsPaidBy.Company));

        (result.SocialContributions, result.PersonalTax, result.Dividend.Dividend, result.NetInPocket, result.KeptInCompany)
            .ShouldBe((9_942.52m, 21_619.13m, 32_045.98m, 60_426.85m, 0m));
    }

    [Fact]
    public void Run_2026CompanyPaysContributions_socialContributionsStepKeepsItsLayerAndTrack()
    {
        var step = Run(Scenario(directorPay: 50_000m, reserve: 0m, paidBy: ContributionsPaidBy.Company)).Steps.Single(step => step.RuleKey == "social_contributions");

        (step.Layer, step.Event, step.Track, step.Amount).ShouldBe((Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 9_942.52m));
    }

    // 45,000 on the payslip is under the 51,000 minimum, but with the 8,948.24 of contributions the
    // company pays the taxable pay is 53,948.24 and meets it: what the company bears for the director
    // is pay (cadre XVI, 1). 20% of the 96,051.76 left.
    [Fact]
    public void Run_2026CompanyPaysContributions_theMinimumPayIsTestedOnTheTaxablePay()
    {
        var directorPays = Run(Scenario(directorPay: 45_000m, reserve: 0m, revenue: 170_000m));
        var companyPays = Run(Scenario(directorPay: 45_000m, reserve: 0m, revenue: 170_000m, paidBy: ContributionsPaidBy.Company));

        (directorPays.Company.Condition, companyPays.DirectorPay.TaxablePay, companyPays.Company.Condition, companyPays.CorporateTax)
            .ShouldBe((ReducedRateCondition.MinimumPayNotMet, 53_948.24m, ReducedRateCondition.MinimumPayMet, 19_210.35m));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026CompanyPaysContributions_companysTaxableIncomeIsTheProfitBeforePayLessItsCost(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m,
            paidBy: ContributionsPaidBy.Company));

        result.Company.TaxableIncome.ShouldBe(profitBeforePay - result.DirectorPay.CompanyCost);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026CompanyPaysContributions_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m,
            paidBy: ContributionsPaidBy.Company));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    [Fact]
    public void Run_negativeTaxWithheld_isRefusedWithGrossToNetsKey()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, taxPrepayments: new TaxPrepayments(-0.01m, AdvancePayments.None)));

        (value, error).ShouldBe((null, GrossToNet.TaxWithheldNegative));
    }

    [Fact]
    public void Run_negativeAdvancePayment_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, advancePayments: new AdvancePayments(0m, 0m, -0.01m, 0m)));

        (value, error).ShouldBe((null, YearSimulation.AdvancePaymentNegative));
    }

    [Fact]
    public void Run_2026_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Fact]
    public void Run_2026_theMaximumDividendIsTheProfitAfterTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.ProfitAfterTax, result.MaximumDividend).ShouldBe((40_000.00m, 40_000.00m));
    }

    [Fact]
    public void Run_2026NothingKept_paysAllOfTheProfitAfterTaxOut()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((40_000.00m, 0m));
    }

    [Fact]
    public void Run_2026Reserve_keepsItAndPaysOutTheRest()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((25_000.00m, 15_000.00m));
    }

    [Fact]
    public void Run_2026Reserve_taxesTheDividendLeftAtTheDeclaredRate()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.Dividend.Withholding, result.Dividend.ExemptionRefund, result.Dividend.NetDividend)
            .ShouldBe((4_500.00m, 149.94m, 20_649.94m));
    }

    [Fact]
    public void Run_2026ReserveAboveTheProfitAfterTax_paysNoDividendAndKeepsAllOfIt()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 40_000.01m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((0m, 40_000.00m));
    }

    [Fact]
    public void Run_2026Loss_allowsNoDividend()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m, revenue: 40_000m));

        (result.ProfitAfterTax, result.MaximumDividend, result.Dividend.Dividend).ShouldBe((-10_000m, 0m, 0m));
    }

    [Fact]
    public void Run_2026Loss_paysNoCorporateTaxAndKeepsTheLossInTheCompany()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m, revenue: 40_000m));

        (result.CorporateTax, result.KeptInCompany).ShouldBe((0m, -10_000m));
    }

    // 40,000 of this year's profit after tax and 60,000 kept from earlier years: 30,000 stays and
    // 70,000 is paid out, withheld at 18% (12,600) with the refund on the exempt 833 once (149.94).
    [Fact]
    public void Run_2026EarlierProfits_paysOutWhatTheReserveLeavesOfBoth()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 30_000m, earlierProfits: 60_000m));

        (result.Dividend.Dividend, result.Dividend.Withholding, result.Dividend.ExemptionRefund, result.Dividend.NetDividend, result.KeptInCompany)
            .ShouldBe((70_000m, 12_600.00m, 149.94m, 57_549.94m, 30_000m));
    }

    [Fact]
    public void Run_2026EarlierProfits_reachThePocket()
    {
        Run(Scenario(directorPay: 50_000m, reserve: 30_000m, earlierProfits: 60_000m)).NetInPocket.ShouldBe(87_129.24m);
    }

    [Fact]
    public void Run_2026EarlierProfits_canAllBePaidOutWithTheProfitAfterTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, earlierProfits: 60_000m));

        (result.ProfitAfterTax, result.MaximumDividend).ShouldBe((40_000.00m, 100_000.00m));
    }

    // The loss of the year eats into the earlier profits before any of them can be paid out.
    [Fact]
    public void Run_2026LossWithEarlierProfits_paysOutWhatTheLossLeaves()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m, revenue: 40_000m, earlierProfits: 60_000m));

        (result.ProfitAfterTax, result.MaximumDividend, result.Dividend.Dividend, result.KeptInCompany).ShouldBe((-10_000m, 50_000m, 50_000m, 0m));
    }

    public static TheoryData<decimal, decimal, decimal, decimal> SetupsWithEarlierProfits => new()
    {
        { 50_000m, 100_000m, 30_000m, 60_000m },
        { 21_000m, 100_000m, 150_000m, 60_000m },
        { 30_000m, 20_000m, 0m, 60_000m },
        { 0m, 0m, 5_000m, 1_000m },
    };

    [Theory]
    [MemberData(nameof(SetupsWithEarlierProfits))]
    public void Run_2026EarlierProfits_layersPocketAndCompanyAddUpToTheProfitBeforePayAndThem(
        decimal directorPay, decimal profitBeforePay, decimal reserve, decimal earlierProfits)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforePay + 20_000m, earlierProfits: earlierProfits));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay + earlierProfits);
    }

    [Fact]
    public void Run_negativeEarlierProfits_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, earlierProfits: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.EarlierProfitsNegative));
    }

    [Fact]
    public void Run_negativeRevenue_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 0m, reserve: 0m, revenue: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.RevenueNegative));
    }

    [Fact]
    public void Run_negativeRunningCosts_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 0m, reserve: 0m) with { RunningCosts = -0.01m });

        (value, error).ShouldBe((null, YearSimulation.RunningCostsNegative));
    }

    [Fact]
    public void Run_negativeReserve_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.ReserveNegative));
    }

    [Fact]
    public void ProfitBeforePay_isTheRevenueLessTheRunningCosts()
    {
        var scenario = Scenario(directorPay: 50_000m, reserve: 0m, revenue: 15_000m);

        scenario.ProfitBeforePay.ShouldBe(-5_000m);
    }

    [Fact]
    public void Run_negativeDirectorPay_isRefusedWithGrossToNetsKey()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: -0.01m, reserve: 0m));

        (value, error).ShouldBe((null, GrossToNet.GrossPayNegative));
    }

    // 2025 has dividend amounts for the Tax-Calc tests but no corporate or INASTI amounts, and 2027's
    // amounts are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void Run_incomeYearOtherThan2026_isRefused(int incomeYear)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m) with { IncomeYear = incomeYear });

        (value, error).ShouldBe((null, YearSimulation.IncomeYearNotSupported));
    }

    private static Scenario Scenario(
        decimal directorPay,
        decimal reserve,
        WithholdingCategory category = WithholdingCategory.VvprbisThirdYearOnwards,
        decimal revenue = 120_000m,
        decimal earlierProfits = 0m,
        AdvancePayments? advancePayments = null,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director) =>
        new(2026, revenue, 20_000m, SmallPastStartUp, directorPay, MunicipalRates.Entered(0.07m), reserve, category, earlierProfits)
        {
            AdvancePayments = advancePayments ?? Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
        };

    private static YearResult Run(Scenario scenario)
    {
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
