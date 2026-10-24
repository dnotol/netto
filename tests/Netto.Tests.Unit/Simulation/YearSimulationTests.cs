using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

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
// 14,954.01, surcharge 1,046.78, net 33,999.21, company cost 59,942.52). The company grants no
// vouchers, pays no expense allowances and no rent unless a test says it does; their figures follow
// VoucherCalculatorTests, AllowanceCalculatorTests and RentCalculatorTests, and the rent's tax
// GrossToNetTests.
public class YearSimulationTests
{
    // With 600 of phone costs reimbursed on invoices: 3,018.44.
    private static readonly ExpenseAllowances AllAllowances = new(1_938.44m, 240m, 240m, 600m);

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
        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld);

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

    // The corporate tax cites art. 215 of its year's rules.
    [Fact]
    public void Run_2026_corporateTaxCitesItsYearsArticle()
    {
        var step = Run(Scenario(directorPay: 50_000m, reserve: 0m)).Steps.Single(step => step.RuleKey == "corporate_tax");

        step.Source.ShouldBe(CorporateTaxRules.IncomeYear2026.Source);
    }

    // The 149.94 the return gives back of the withholding on the dividend's exempt part cites the
    // explanations that say how it is claimed, its year's rules' own.
    [Fact]
    public void Run_2026Dividend_exemptionRefundCitesTheReturnsExplanations()
    {
        var step = Run(Scenario(directorPay: 50_000m, reserve: 0m)).Steps.Single(step => step.RuleKey == "dividend_exemption_refund");

        (step.Amount, step.Source).ShouldBe((-149.94m, DividendRules.IncomeYear2026.ExemptionRefund));
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

        (result.AdvancePayments.Surcharge, result.AdvancePayments.LastDeadlineTopUp).ShouldBe((345.00m, 7_666.56m));
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

    // 220 days with a meal voucher (VoucherCalculatorTests): the company pays 1,960.20 for them, which
    // leaves 98,039.80 for the split, and deducts 880.00 of it, so the 1,080.20 it cannot deduct is
    // added back: 49,120.00 of taxable income, which does not exceed the 50,000 pay, at 20%.
    [Fact]
    public void Run_2026MealVouchersOn220Days_comeOffTheProfitBeforePayAndAddTheirDisallowedPartToTheTaxableIncome()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: MealVouchersOn220Days));

        (result.ProfitBeforePay, result.ProfitAfterPay, result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax)
            .ShouldBe((98_039.80m, 48_039.80m, 49_120.00m, ReducedRateCondition.TaxableIncomeNotAbovePay, 9_824.00m));
    }

    [Fact]
    public void Run_2026MealVouchersOn220Days_raiseTheTaxableIncomeByTheirDisallowedPartOverADeductibleCost()
    {
        var vouchers = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: MealVouchersOn220Days));
        var deductibleCost = Run(Scenario(directorPay: 50_000m, reserve: 0m) with { RunningCosts = 21_960.20m });

        (vouchers.ProfitBeforePay - deductibleCost.ProfitBeforePay, vouchers.Company.TaxableIncome - deductibleCost.Company.TaxableIncome)
            .ShouldBe((0m, 1_080.20m));
    }

    // The disallowed part was spent: the profit after tax is 49,120.00 less the 9,824.00 of tax and the
    // 1,080.20, all paid out at 18% (withholding 6,878.84, refund 149.94). The pocket gets the pay's
    // 29,579.30, that dividend's 31,486.90 and the vouchers' 1,960.20.
    [Fact]
    public void Run_2026MealVouchersOn220Days_reachThePocketBesideThePayAndTheDividend()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: MealVouchersOn220Days));

        (result.ProfitAfterTax, result.Dividend.NetDividend, result.Vouchers.Total, result.NetInPocket)
            .ShouldBe((38_215.80m, 31_486.90m, 1_960.20m, 63_026.40m));
    }

    // Eco and sport/culture vouchers are not deductible at all: the taxable income is that of a company
    // without them, and the profit after tax is 350 lower.
    [Fact]
    public void Run_2026EcoAndSportCultureVouchers_reachThePocketAndNoneIsDeducted()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: new VoucherGrants(0, 250m, 100m)));

        (result.ProfitBeforePay, result.Company.TaxableIncome, result.ProfitAfterTax, result.Vouchers.Total, result.Vouchers.Deductible)
            .ShouldBe((99_650m, 50_000m, 39_650m, 350m, 0m));
    }

    // Exempt, they are neither taxed as pay nor income INASTI is owed on.
    [Fact]
    public void Run_2026Vouchers_leaveThePaysTaxesAndContributionsAsTheyAre()
    {
        var without = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var with = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers));

        (with.SocialContributions, with.DirectorPay.IncomeTax, with.DirectorPay.MunicipalSurcharge, with.DirectorPay.NetInPocket)
            .ShouldBe((without.SocialContributions, without.DirectorPay.IncomeTax, without.DirectorPay.MunicipalSurcharge, without.DirectorPay.NetInPocket));
    }

    // With 1,000 more revenue the 1,080.20 the company cannot deduct takes its taxable income to
    // 50,120.00, above the 50,000 pay: 25% on all of it. Deductible, they would have left 49,039.80 and
    // the reduced rate; and the 51,960.20 of pay and vouchers would meet the 51,000 minimum, but
    // vouchers are not pay.
    [Fact]
    public void Run_2026MealVouchers_art215ReadsTheTaxableIncomeWithTheirDisallowedPartAndNeverCountsThemAsPay()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, revenue: 121_000m, vouchers: MealVouchersOn220Days));

        (result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax)
            .ShouldBe((50_120.00m, ReducedRateCondition.MinimumPayNotMet, 12_530.00m));
    }

    // What reaches the pocket comes first, as the vouchers come before the pay, then what the company
    // cannot deduct of them, the vouchers as the corporate layer taxes them.
    [Fact]
    public void Run_2026Vouchers_putTheirStepsBeforeTheCorporateTaxWithTheirTags()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers));

        result.Steps.Take(5).Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("meal_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, 1_960.20m),
            ("eco_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, 250m),
            ("sport_culture_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, 100m),
            ("disallowed_vouchers", Layer.Corporate, TaxableEvent.Vouchers, Track.TaxBaseOnly, 1_430.20m),
            ("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 9_824.00m),
        });
    }

    [Fact]
    public void Run_2026Vouchers_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers));

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    // What the company pays the director besides pay reaches the pocket, so the layers, the pocket and
    // the company still add up to what it earned before paying its director anything.
    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026Vouchers_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            vouchers: AllVouchers));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026VouchersCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            vouchers: AllVouchers));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    [Fact]
    public void Run_2026VouchersAndEarlierProfits_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirectorAndThem()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 30_000m, earlierProfits: 60_000m, vouchers: AllVouchers));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(160_000m);
    }

    public static TheoryData<int, decimal, string> VouchersOutsideTheRules => new()
    {
        { -1, 0m, VoucherCalculator.MealVoucherDaysNegative },
        { 0, 250.01m, VoucherCalculator.EcoVouchersAboveMaximum },
    };

    [Theory]
    [MemberData(nameof(VouchersOutsideTheRules))]
    public void Run_vouchersOutsideTheRules_isRefusedWithTheCalculatorsKey(int days, decimal eco, string expected)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: new VoucherGrants(days, eco, 0m)));

        (value, error).ShouldBe((null, expected));
    }

    // The most the circular allows for the office and for internet and a computer at home
    // (AllowanceCalculatorTests): the company pays 2,418.44 for them, which leaves 97,581.56 for the
    // split, and deducts all of it: 47,581.56 of taxable income, which does not exceed the 50,000 pay,
    // at 20%.
    [Fact]
    public void Run_2026FlatRateAllowances_comeOffTheProfitBeforePayAndTheTaxableIncomeAlike()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: FlatRateAllowances));

        (result.ProfitBeforePay, result.ProfitAfterPay, result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax, result.DisallowedExpenses)
            .ShouldBe((97_581.56m, 47_581.56m, 47_581.56m, ReducedRateCondition.TaxableIncomeNotAbovePay, 9_516.31m, 0m));
    }

    [Fact]
    public void Run_2026FlatRateAllowances_lowerTheProfitBeforePayAndTheTaxableIncomeByWhatTheyCost()
    {
        var without = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var with = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: FlatRateAllowances));

        (without.ProfitBeforePay - with.ProfitBeforePay, without.Company.TaxableIncome - with.Company.TaxableIncome).ShouldBe((2_418.44m, 2_418.44m));
    }

    // Deducted in full, like a running cost: the company's figures are those of one that spent as much
    // on its own costs.
    [Fact]
    public void Run_2026Allowances_leaveTheCompanyAsADeductibleCostWould()
    {
        var allowances = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: AllAllowances));
        var deductibleCost = Run(Scenario(directorPay: 50_000m, reserve: 0m) with { RunningCosts = 23_018.44m });

        (allowances.ProfitBeforePay, allowances.Company.TaxableIncome, allowances.CorporateTax, allowances.ProfitAfterTax)
            .ShouldBe((deductibleCost.ProfitBeforePay, deductibleCost.Company.TaxableIncome, deductibleCost.CorporateTax, deductibleCost.ProfitAfterTax));
    }

    // The profit after tax is 47,581.56 less the 9,516.31 of tax, all paid out at 18% (withholding
    // 6,851.75, refund 149.94). The pocket gets the pay's 29,579.30, that dividend's 31,363.44 and the
    // allowances' 2,418.44: 831.94 more than the 62,529.24 without them.
    [Fact]
    public void Run_2026FlatRateAllowances_reachThePocketBesideThePayAndTheDividend()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: FlatRateAllowances));

        (result.ProfitAfterTax, result.Dividend.NetDividend, result.NetInPocket).ShouldBe((38_065.25m, 31_363.44m, 63_361.18m));
    }

    // A reimbursement of the company's own costs is not pay: neither taxed as pay nor income INASTI is
    // owed on.
    [Fact]
    public void Run_2026Allowances_leaveThePaysTaxesAndContributionsAsTheyAre()
    {
        var without = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var with = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: AllAllowances));

        (with.SocialContributions, with.DirectorPay.IncomeTax, with.DirectorPay.MunicipalSurcharge, with.DirectorPay.NetInPocket)
            .ShouldBe((without.SocialContributions, without.DirectorPay.IncomeTax, without.DirectorPay.MunicipalSurcharge, without.DirectorPay.NetInPocket));
    }

    // 102,000 before the director and a 50,000 pay leave 52,000 of taxable income, above the pay and
    // under the 51,000 minimum: 25%. The allowances take it to 49,581.56, which does not exceed the pay:
    // 20%.
    [Fact]
    public void Run_2026FlatRateAllowances_art215ReadsTheTaxableIncomeTheyLower()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, revenue: 122_000m, allowances: FlatRateAllowances));

        (result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax)
            .ShouldBe((49_581.56m, ReducedRateCondition.TaxableIncomeNotAbovePay, 9_916.31m));
    }

    // 105,000 before the director: 52,581.56 of taxable income once the allowances are paid, above the
    // pay, 25% on all of it. The 52,418.44 of pay and allowances would meet the 51,000 minimum, but
    // allowances are not pay.
    [Fact]
    public void Run_2026FlatRateAllowances_art215NeverCountsThemAsPay()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, revenue: 125_000m, allowances: FlatRateAllowances));

        (result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax)
            .ShouldBe((52_581.56m, ReducedRateCondition.MinimumPayNotMet, 13_145.39m));
    }

    // What reaches the pocket comes first, the vouchers' steps then the allowances', each allowance as
    // it is paid; the company deducts the allowances in full, so none of them joins its taxable income.
    // 100,000 less 2,310.20 of vouchers and 3,018.44 of allowances, less the pay, with the 1,430.20 of
    // vouchers it cannot deduct added back: 46,101.56 at 20%.
    [Fact]
    public void Run_2026VouchersAndAllowances_putTheAllowancesStepsAfterTheVouchersAndBeforeTheCorporateTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers, allowances: AllAllowances));

        result.Steps.Skip(4).Take(5).Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("office_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, 1_938.44m),
            ("internet_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, 240m),
            ("computer_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, 240m),
            ("phone_costs", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, 600m),
            ("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 9_220.31m),
        });
    }

    [Fact]
    public void Run_2026VouchersAndAllowances_disallowsOnlyWhatTheCompanyCannotDeductOfTheVouchers()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers, allowances: AllAllowances));

        (result.DisallowedExpenses, result.Company.TaxableIncome).ShouldBe((1_430.20m, 46_101.56m));
    }

    // Each allowance cites what makes it no pay, and the amounts the administration accepts for it.
    [Fact]
    public void Run_2026Allowances_citeTheReimbursementAndTheirMaxima()
    {
        var rules = AllowanceRules.IncomeYear2026;

        var steps = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: AllAllowances)).Steps
            .Where(step => step.Event == TaxableEvent.ExpenseAllowances).ToList();

        steps.ShouldAllBe(step => step.Source == rules.Reimbursement);
        steps.Select(step => step.AmountsUsed.Count).ShouldBe([3, 1, 1, 0]);
    }

    [Fact]
    public void Run_2026NoAllowances_listsNoStepOfThem()
    {
        Run(Scenario(directorPay: 50_000m, reserve: 0m)).Steps.ShouldNotContain(step => step.Event == TaxableEvent.ExpenseAllowances);
    }

    [Fact]
    public void Run_2026OnlyPhoneCosts_listsEveryAllowance()
    {
        var steps = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: new ExpenseAllowances(0m, 0m, 0m, 600m))).Steps;

        steps.Where(step => step.Event == TaxableEvent.ExpenseAllowances).Select(step => (step.RuleKey, step.Amount)).ShouldBe(new[]
        {
            ("office_allowance", 0m), ("internet_allowance", 0m), ("computer_allowance", 0m), ("phone_costs", 600m),
        });
    }

    [Fact]
    public void Run_2026Allowances_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: AllAllowances));

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026VouchersAndAllowances_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            vouchers: AllVouchers, allowances: AllAllowances));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026AllowancesCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            allowances: AllAllowances));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    [Fact]
    public void Run_2026VouchersAllowancesAndEarlierProfits_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirectorAndThem()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 30_000m, earlierProfits: 60_000m, vouchers: AllVouchers, allowances: AllAllowances));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(160_000m);
    }

    public static TheoryData<decimal, decimal, string> AllowancesOutsideTheRules => new()
    {
        { 1_938.45m, 0m, AllowanceCalculator.OfficeAllowanceAboveMaximum },
        { 0m, -0.01m, AllowanceCalculator.PhoneCostsNegative },
    };

    [Theory]
    [MemberData(nameof(AllowancesOutsideTheRules))]
    public void Run_allowancesOutsideTheRules_isRefusedWithTheCalculatorsKey(decimal office, decimal phone, string expected)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: new ExpenseAllowances(office, 0m, 0m, phone)));

        (value, error).ShouldBe((null, expected));
    }

    // The 2026 cap on 12% of a cadastral income of 1,200 (RentCalculatorTests): 1,380.00 a year, the
    // company deducts all of it, which leaves 98,620 for the split and 48,620 of taxable income beside
    // the 50,000 pay, not above it: 20%.
    [Fact]
    public void Run_2026RentAtTheCap_comesOffTheProfitBeforePayAndTheTaxableIncomeAlike()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap));

        (result.ProfitBeforePay, result.ProfitAfterPay, result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax, result.DisallowedExpenses)
            .ShouldBe((98_620m, 48_620m, 48_620m, ReducedRateCondition.TaxableIncomeNotAbovePay, 9_724.00m, 0m));
    }

    // Its 828 of net rent is taxed with the pay's 40,455.44, at 45%: 372.60 of income tax and 26.08 of
    // municipal surcharge (GrossToNetTests). The pocket gets the pay's 29,579.30 less those 398.68, the
    // rent's 1,380.00 and the dividend of the 38,896.00 left after tax at 18%, 32,044.66: 76.04 more
    // than the 62,529.24 without it. The personal tax is the pay's 12,127.34, the rent's 398.68 and the
    // dividend's 7,001.28 less the 149.94 refunded.
    [Fact]
    public void Run_2026RentAtTheCap_reachesThePocketLessItsTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap));

        (result.ProfitAfterTax, result.Dividend.NetDividend, result.PersonalTax, result.NetInPocket).ShouldBe((38_896.00m, 32_044.66m, 19_377.36m, 62_605.28m));
    }

    // Income from immovable property, not professional income: no INASTI on it, and the pay's own
    // figures stay the pay's.
    [Fact]
    public void Run_2026RentAtTheCap_leavesThePaysContributionsAndOwnTaxesAsTheyAre()
    {
        var without = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var with = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap));

        (with.SocialContributions, with.DirectorPay.IncomeTax, with.DirectorPay.MunicipalSurcharge, with.DirectorPay.NetInPocket)
            .ShouldBe((without.SocialContributions, without.DirectorPay.IncomeTax, without.DirectorPay.MunicipalSurcharge, without.DirectorPay.NetInPocket));
    }

    // The rent and what it leaves taxable come first, after the other levers' steps; its taxes come
    // after the pay's, on the total the pay's own tax leaves.
    [Fact]
    public void Run_2026RentAtTheCap_putsTheRentBeforeTheCorporateTaxAndItsTaxesAfterThePays()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap));

        result.Steps.Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("rent", Layer.Personal, TaxableEvent.Rent, Track.InPocket, 1_380.00m),
            ("rent_charges", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, 552.00m),
            ("rent_taxable_income", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, 828.00m),
            ("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 9_724.00m),
            ("advance_payment_surcharge", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 0m),
            ("social_contributions", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 8_293.36m),
            ("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 1_251.20m),
            ("taxable_income", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 40_455.44m),
            ("income_tax", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 11_333.96m),
            ("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 793.38m),
            ("low_earned_income_credit", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 0m),
            ("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -0m),
            ("rent_income_tax", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, 372.60m),
            ("rent_municipal_surcharge", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, 26.08m),
            ("rent_low_earned_income_credit_lost", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, 0m),
            ("rent_income_tax_advance_payments", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, 0m),
            ("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, 7_001.28m),
            ("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -149.94m),
        });
    }

    // 2,000.00 of net rent beside a 27,000 pay, nothing withheld or prepaid on it (GrossToNetTests):
    // the rent takes away 195.81 of the credit and adds 7.93 to the surcharge that credit no longer
    // lowers. 400 of cadastral income let whole, for 3,333.33 less its 40%, under the cap of 3,833.33.
    [Fact]
    public void Run_2026RentBesideALowPay_takesAwayPartOfTheCreditAndRaisesTheSurcharge()
    {
        var result = Run(Scenario(directorPay: 27_000m, reserve: 0m, taxPrepayments: TaxPrepayments.None, rent: new HomeOfficeRent(400m, 1m, 3_333.33m)));

        result.Steps.Where(step => step.Event == TaxableEvent.Rent && step.Track == Track.PaidOut).Select(step => (step.RuleKey, step.Amount)).ShouldBe(new[]
        {
            ("rent_income_tax", 799.99m),
            ("rent_municipal_surcharge", 56.00m),
            ("rent_low_earned_income_credit_lost", 195.81m),
            ("rent_income_tax_advance_payments", 7.93m),
        });
    }

    // The year's balance on the assessment includes the rent's tax: 12,127.34 on the pay alone and
    // 398.68 on the rent, set against the 10,000 withheld.
    [Fact]
    public void Run_2026RentAtTheCap_theBalanceOnTheAssessmentIncludesItsTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, taxPrepayments: new TaxPrepayments(10_000m, AdvancePayments.None), rent: RentAtTheCap));

        (result.DirectorPay.Assessment.FinalTax, result.DirectorPay.Assessment.Balance).ShouldBe((12_526.02m, 2_526.02m));
    }

    // 105,000 before the director: 53,620 of taxable income once the rent is paid, above the 50,000
    // pay, 25% on all of it. Pay and rent, 51,380, would meet the 51,000 minimum, but rent is not pay.
    [Fact]
    public void Run_2026RentAtTheCap_art215NeverCountsItAsPay()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, revenue: 125_000m, rent: RentAtTheCap));

        (result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax).ShouldBe((53_620m, ReducedRateCondition.MinimumPayNotMet, 13_405.00m));
    }

    [Fact]
    public void Run_2026NoRent_listsNoStepOfIt()
    {
        Run(Scenario(directorPay: 50_000m, reserve: 0m)).Steps.ShouldNotContain(step => step.Event == TaxableEvent.Rent);
    }

    // The card's fields are filled one by one: a cadastral income and a share with no rent yet let
    // nothing, and the year runs as without them.
    [Fact]
    public void Run_2026CadastralIncomeWithoutRent_runsTheYearWithoutIt()
    {
        var without = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: new HomeOfficeRent(1_200m, 0.12m, 0m)));

        (result.NetInPocket, result.Steps.Count(step => step.Event == TaxableEvent.Rent)).ShouldBe((without.NetInPocket, 0));
    }

    [Fact]
    public void Run_2026RentAtTheCap_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap));

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    // The rent cites the cap it is kept under, what it leaves taxable the floor of art. 7 and its
    // charges art. 13's 40% and their limit; the tax it adds the year's scale, and the surcharge on
    // that tax the year's articles that set up the municipal surcharge.
    [Fact]
    public void Run_2026RentAtTheCap_citesItsArticles()
    {
        var rules = RentRules.IncomeYear2026;

        var steps = Run(Scenario(directorPay: 50_000m, reserve: 0m, rent: RentAtTheCap)).Steps.Where(step => step.Event == TaxableEvent.Rent).Take(5);

        steps.Select(step => step.Source)
            .ShouldBe([rules.Cap, rules.NetRent, rules.RentAsIncome, PersonalTaxRules.IncomeYear2026.IncomeTaxScale, MunicipalRates.IncomeYear2026.Surcharge]);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026RentAtTheCap_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            vouchers: AllVouchers, rent: RentAtTheCap));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026RentCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            rent: new HomeOfficeRent(400m, 1m, 3_333.33m)));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector);
    }

    public static TheoryData<HomeOfficeRent, decimal, string> RentsOutsideTheRules => new()
    {
        { new HomeOfficeRent(1_200m, 0.12m, 1_380.01m), 0m, RentCalculator.RentAboveCap },
        { new HomeOfficeRent(1_200m, 0.12m, 1_000m), 1_938.44m, RentCalculator.RentWithOfficeAllowance },
        { new HomeOfficeRent(1_200m, 1.20m, 1_000m), 0m, RentCalculator.ShareLetOutOfRange },
    };

    [Theory]
    [MemberData(nameof(RentsOutsideTheRules))]
    public void Run_rentOutsideTheRules_isRefusedWithTheCalculatorsKey(HomeOfficeRent rent, decimal officeAllowance, string expected)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m, allowances: new ExpenseAllowances(officeAllowance, 0m, 0m, 0m), rent: rent));

        (value, error).ShouldBe((null, expected));
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
    public void ProfitBeforeDirector_isTheRevenueLessTheRunningCosts()
    {
        var scenario = Scenario(directorPay: 50_000m, reserve: 0m, revenue: 15_000m);

        scenario.ProfitBeforeDirector.ShouldBe(-5_000m);
    }

    [Fact]
    public void ProfitBeforePay_noVouchers_isTheRevenueLessTheRunningCosts()
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, revenue: 15_000m)).ShouldBe(-5_000m);
    }

    // What the split shares is what is left once the company has paid for the vouchers: 220 × 8.91,
    // 250 and 100.
    [Fact]
    public void ProfitBeforePay_vouchers_isWhatTheCompanyHasLeftOnceItPaidForThem()
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: new VoucherGrants(220, 250m, 100m)))
            .ShouldBe(97_689.80m);
    }

    // The split's slider keeps its place while a voucher is refused: it takes them as given.
    [Fact]
    public void ProfitBeforePay_refusedVouchers_takesThemAsGiven()
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: new VoucherGrants(0, 300m, 0m))).ShouldBe(99_700m);
    }

    // 100,000 less the 2,418.44 of allowances and, with the vouchers, their 2,310.20 too.
    [Fact]
    public void ProfitBeforePay_allowances_isWhatTheCompanyHasLeftOnceItPaidForThemAndTheVouchers()
    {
        (YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, allowances: FlatRateAllowances)),
                YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, vouchers: AllVouchers, allowances: FlatRateAllowances)))
            .ShouldBe(((decimal?)97_581.56m, (decimal?)95_271.36m));
    }

    [Fact]
    public void ProfitBeforePay_refusedAllowance_takesItAsGiven()
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, allowances: new ExpenseAllowances(2_000m, 0m, 0m, 0m))).ShouldBe(98_000m);
    }

    // 100,000 less the 1,380.00 of rent, and taken as given while it is refused above the cap.
    [Theory]
    [InlineData(1_380.00, 98_620.00)]
    [InlineData(2_000.00, 98_000.00)]
    public void ProfitBeforePay_rent_isWhatTheCompanyHasLeftOnceItPaidIt(decimal rent, decimal profitBeforePay)
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m, rent: new HomeOfficeRent(1_200m, 0.12m, rent))).ShouldBe(profitBeforePay);
    }

    [Fact]
    public void ProfitBeforePay_unsupportedIncomeYear_isUnknown()
    {
        YearSimulation.ProfitBeforePay(Scenario(directorPay: 50_000m, reserve: 0m) with { IncomeYear = Fixtures.UnsupportedIncomeYear }).ShouldBeNull();
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
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void Run_incomeYearOtherThan2026_isRefused(int incomeYear)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m) with { IncomeYear = incomeYear });

        (value, error).ShouldBe((null, YearSimulation.IncomeYearNotSupported));
    }
}
