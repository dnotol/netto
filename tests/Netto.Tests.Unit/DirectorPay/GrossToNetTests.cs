using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// Income year 2026 at a 7% municipal rate. Expected values are derived from the official amounts
// (INASTI « Cotisations 2026 », the assessment year 2027 indexation notice), not checked against
// Tax-Calc, which does not cover 2026 yet; the method is proven on 2025 in PersonalTaxCalculatorTests.
// The company withholds more than any tax these tests reach, so no advance-payment surcharge is due
// and the figures are the tax's own, unless a test says otherwise; the surcharge and the bonus
// follow IncomeTaxAdvancePaymentCalculatorTests. The director pays the contributions unless a test
// says the company does; the company-paid figures are derived the same way (Notion, « The company
// can pay the director's social contributions on their behalf »).
public class GrossToNetTests
{
    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    public static TheoryData<decimal> GrossPays => [0m, 10_000m, 30_000m, 50_000m, 160_000m];

    public static TheoryData<decimal> CompanyPaidGrossPays => [0m, 10_000m, 20_000m, 41_700m, 50_000m, 100_000m, 160_000m];

    public static TheoryData<decimal> GrossPaysInFirstBand => [30_000m, 30_000.01m, 50_000m, 70_000m];

    public static TheoryData<decimal> MunicipalRatesOutOfRange => [-0.0001m, 0.1001m];

    public static TheoryData<decimal> MunicipalRatesAtTheLimits => [0m, 0.10m];

    [Fact]
    public void Compute_2026GrossZero_paysTheMinimumContributionsAndNoTax()
    {
        var result = Compute2026(0m);

        (result.SocialContributions, result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.NetInPocket)
            .ShouldBe((3_561.68m, 0m, 0m, 0m, 0m, -3_561.68m));
    }

    [Fact]
    public void Compute_2026BaseUnderMinimumIncome_paysTheMinimumAndTheNextThousandAddsNoContributions()
    {
        var result = Compute2026(10_000m);

        (result.SocialContributions, result.NextThousand.SocialContributions).ShouldBe((3_561.68m, 0m));
    }

    [Fact]
    public void Compute_2026BaseAboveSecondBandCeiling_paysTheMaximumAndTheNextThousandAddsNoContributions()
    {
        var result = Compute2026(160_000m);

        (result.SocialContributions, result.NextThousand.SocialContributions).ShouldBe((20_412.20m, 0m));
    }

    // The item's worked example, to the cent: contributions 19.885/119.885 of the gross before
    // quarterly rounding, flat-rate costs 3% × 41,706.64, tax on the 45% bracket less 2,887.50.
    [Fact]
    public void Compute_2026Gross50000_matchesTheWorkedExample()
    {
        var result = Compute2026(50_000m);

        (result.SocialContributions, result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.NetInPocket)
            .ShouldBe((8_293.36m, 1_251.20m, 40_455.44m, 11_333.96m, 793.38m, 29_579.30m));
    }

    [Fact]
    public void Compute_2026Gross50000_splitsTheNextThousandAsTheWorkedExample()
    {
        var result = Compute2026(50_000m);

        result.NextThousand.ShouldBe(new NextThousand(
            SocialContributions: 165.88m, IncomeTax: 364.09m, MunicipalSurcharge: 25.48m, LowEarnedIncomeCredit: 0m, AdvancePayments: 0m,
            NetInPocket: 444.55m));
    }

    [Fact]
    public void Compute_2026HighGross_capsFlatRateCostsAt3200()
    {
        var result = Compute2026(160_000m);

        result.FlatRateCosts.ShouldBe(3_200m);
    }

    [Theory]
    [MemberData(nameof(GrossPaysInFirstBand))]
    public void Compute_2026InFirstBand_taxableIncomeIsGrossLessContributionsAndFlatRateCosts(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        result.TaxableIncome.ShouldBe(grossPay - result.SocialContributions - result.FlatRateCosts);
    }

    // Contributions are four rounded quarters, so they move in 0.04 steps, and at some pay levels no
    // amount equals the contributions on its own base to the cent: at 30,000 the engine returns
    // 4,976.00 and the base it leaves gives 4,976.04 (decided 2026-10-19, see the Notion item).
    [Theory]
    [MemberData(nameof(GrossPaysInFirstBand))]
    public void Compute_2026InFirstBand_contributionsRecomputedFromTheirBaseAreWithinFourCents(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        var recomputed = SocialContributionCalculator.Annual(SocialContributionRules.IncomeYear2026, result.TaxableIncome);

        Math.Abs(recomputed - result.SocialContributions).ShouldBeLessThanOrEqualTo(0.04m);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_netInPocketIsGrossLessWhatIsPaidOut(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        result.NetInPocket.ShouldBe(
            grossPay - result.SocialContributions - result.IncomeTax - result.MunicipalSurcharge + result.LowEarnedIncomeCredit
            - result.AdvancePayments.Surcharge + result.AdvancePayments.Bonus);
    }

    // 20,000 leaves 15,945.17 taxable, where the 2026 credit is 910 in full: tax 1,098.78 and
    // surcharge 76.91 on the contributions' minimum of 3,561.68, and the credit refunded on top.
    [Fact]
    public void Compute_2026GrossInFullCreditBand_addsTheWholeCreditToTheNet()
    {
        var result = Compute2026(20_000m);

        (result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.LowEarnedIncomeCredit, result.NetInPocket)
            .ShouldBe((15_945.17m, 1_098.78m, 76.91m, 910m, 16_172.63m));
    }

    [Fact]
    public void Compute_2026GrossInFullCreditBand_creditStepComesBackPaidOut()
    {
        var credit = Compute2026(20_000m).Steps.Single(step => step.RuleKey == "low_earned_income_credit");

        (credit.Track, credit.Amount).ShouldBe((Track.PaidOut, -910m));
    }

    // At 30,000 the taxable income lies where the credit falls, so the next 1,000 loses some of it.
    [Fact]
    public void Compute_2026GrossWhereCreditFalls_nextThousandLosesPartOfIt()
    {
        var (here, next) = (Compute2026(30_000m), Compute2026(31_000m));

        (here.NextThousand.LowEarnedIncomeCredit, here.NextThousand.LowEarnedIncomeCredit > 0m)
            .ShouldBe((here.LowEarnedIncomeCredit - next.LowEarnedIncomeCredit, true));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var next = Compute2026(grossPay).NextThousand;

        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026NothingWithheld_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var next = Compute2026(grossPay, MunicipalRates.Entered(0.07m), TaxPrepayments.None).NextThousand;

        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    // IncomeTaxAdvancePaymentCalculatorTests: 486.57 on the 11,333.96 of tax.
    [Fact]
    public void Compute_2026Gross50000NothingWithheldOrPrepaid_surchargesTheTaxOutOfThePocket()
    {
        var result = Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.None);

        (result.AdvancePayments.Surcharge, result.NetInPocket).ShouldBe((486.57m, 29_092.73m));
    }

    [Fact]
    public void Compute_2026Gross50000NothingWithheldOrPrepaid_saysWhatBy21DecemberLeavesNoneDue()
    {
        Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.None).AdvancePayments.LastDeadlineTopUp.ShouldBe(14_317.50m);
    }

    // 12,014 by 10 April avoids the surcharge and earns 90.11 of bonus.
    [Fact]
    public void Compute_2026Gross50000TheWholeBaseBy10April_addsTheBonusToThePocket()
    {
        var result = Compute2026(50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(0m, new AdvancePayments(12_014m, 0m, 0m, 0m)));

        (result.AdvancePayments.Surcharge, result.AdvancePayments.Bonus, result.NetInPocket).ShouldBe((0m, 90.11m, 29_669.41m));
    }

    [Fact]
    public void Compute_2026NothingWithheldOrPrepaid_surchargeAndBonusAreStepsOnTheDirectorPay()
    {
        var result = Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.None);

        result.Steps.Where(step => step.RuleKey.StartsWith("income_tax_advance_payment", StringComparison.Ordinal))
            .Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount))
            .ShouldBe(new[]
            {
                ("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 486.57m),
                ("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -0m),
            });
    }

    // The assessment: 11,333.96 and 793.38 of tax, no surcharge once 10,000 is withheld (the 90.63 on
    // the 2,014.00 left of the base is under the minimum at 90%), less the 10,000.
    [Fact]
    public void Compute_2026Gross50000Withheld10000_balanceIsTheFinalTaxLessTheWithholding()
    {
        var assessment = Compute2026(50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(10_000m, AdvancePayments.None)).Assessment;

        (assessment.FinalTax, assessment.Withheld, assessment.AdvancePayments, assessment.Balance).ShouldBe((12_127.34m, 10_000m, 0m, 2_127.34m));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026NothingWithheldOrPrepaid_finalTaxIsTheTaxLessTheCreditPlusTheSurchargeAndAllOfItIsToPay(decimal grossPay)
    {
        var result = Compute2026(grossPay, MunicipalRates.Entered(0.07m), TaxPrepayments.None);

        (result.Assessment.FinalTax, result.Assessment.Balance).ShouldBe((
            result.IncomeTax + result.MunicipalSurcharge - result.LowEarnedIncomeCredit + result.AdvancePayments.Surcharge - result.AdvancePayments.Bonus,
            result.Assessment.FinalTax));
    }

    // 12,014 by 10 April: no surcharge, 90.11 of bonus, and 23.23 left to pay.
    [Fact]
    public void Compute_2026Gross50000TheWholeBaseBy10April_setsThePaymentsAgainstTheTaxLessTheBonus()
    {
        var assessment = Compute2026(50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(0m, new AdvancePayments(12_014m, 0m, 0m, 0m))).Assessment;

        (assessment.FinalTax, assessment.AdvancePayments, assessment.Balance).ShouldBe((12_037.23m, 12_014m, 23.23m));
    }

    [Fact]
    public void Compute_2026WithholdingBeyondTheTax_balanceIsNegativeToBeRefunded()
    {
        var assessment = Compute2026(50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(20_000m, AdvancePayments.None)).Assessment;

        assessment.Balance.ShouldBe(-7_872.66m);
    }

    // The river's example: nothing given as withheld, so the company withholds what the key formula
    // says, twelve times the 890.94 on 4,166.67 (PayStatementTests).
    [Fact]
    public void Compute_2026Gross50000WithholdingAsTheKeyFormula_withholdsTwelveTimesTheMonthlyAmount()
    {
        var assessment = Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld).Assessment;

        (assessment.Withheld, assessment.WithheldGiven).ShouldBe((10_691.28m, false));
    }

    // The 10,691.28 counts as prepaid at source: 1,322.72 of base is left, 59.52 at 4.5%, 53.57 at
    // 90%, under the 100 minimum, so no surcharge is due.
    [Fact]
    public void Compute_2026Gross50000WithholdingAsTheKeyFormula_feedsTheSurchargeWhichFallsUnderTheMinimum()
    {
        var payments = Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld).AdvancePayments;

        (payments.Base, payments.SurchargeBeforeKeptRate, payments.BelowMinimum, payments.Surcharge).ShouldBe((1_322.72m, 59.52m, true, 0m));
    }

    // 11,333.96 and 793.38 less the 10,691.28 withheld: 1,436.06 to pay, and the pocket is the 29,579.30
    // of a pay whose tax was withheld, since what is withheld is the tax paid early.
    [Fact]
    public void Compute_2026Gross50000WithholdingAsTheKeyFormula_leaves1436Point06ToPay()
    {
        var result = Compute2026(50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld);

        (result.Assessment.FinalTax, result.Assessment.Balance, result.NetInPocket).ShouldBe((12_127.34m, 1_436.06m, 29_579.30m));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026WithholdingAsTheKeyFormula_withheldIsTwelveTimesTheStatementsMonth(decimal grossPay)
    {
        var result = Compute2026(grossPay, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld);

        (result.Assessment.Withheld, result.Assessment.WithheldGiven).ShouldBe((12 * result.Statement.Months[0].Withholding, false));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026WithholdingAsTheKeyFormula_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var next = Compute2026(grossPay, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld).NextThousand;

        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    // What the user says was withheld replaces the formula's amount in the year, and the result says
    // so; the statement still shows the formula's monthly withholding, what a real payslip shows.
    [Fact]
    public void Compute_2026Withheld10000_replacesTheFormulasAmountAndSaysItWasGiven()
    {
        var result = Compute2026(50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(10_000m, AdvancePayments.None));

        (result.Assessment.Withheld, result.Assessment.WithheldGiven, result.Assessment.Balance, result.Statement.Months[0].Withholding)
            .ShouldBe((10_000m, true, 2_127.34m, 890.94m));
    }

    // Twelve times the 1,194.71 on 4,995.21 (PayStatementTests); 1,514.73 of base is left on the tax at
    // 106%, 15,851.25, which gives 61.34 at 90%, under the minimum.
    [Fact]
    public void Compute_2026CompanyPaysGross50000WithholdingAsTheKeyFormula_withholdsOnTheTaxablePay()
    {
        var result = CompanyPays2026(50_000m, TaxPrepayments.AsWithheld);

        (result.Assessment.Withheld, result.AdvancePayments.Base, result.AdvancePayments.Surcharge, result.Assessment.Balance)
            .ShouldBe((14_336.52m, 1_514.73m, 0m, 1_664.27m));
    }

    [Fact]
    public void Compute_negativeWithholding_isRefused()
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(-0.01m, AdvancePayments.None));

        (value, error).ShouldBe((null, GrossToNet.TaxWithheldNegative));
    }

    [Fact]
    public void Compute_negativeAdvancePayment_isRefused()
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), new TaxPrepayments(0m, new AdvancePayments(0m, -0.01m, 0m, 0m)));

        (value, error).ShouldBe((null, GrossToNet.TaxAdvancePaymentNegative));
    }

    [Fact]
    public void Compute_2026_returnsEveryStepWithItsTrack()
    {
        var result = Compute2026(50_000m);

        result.Steps.Select(step => (step.RuleKey, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("social_contributions", Track.PaidOut, 8_293.36m),
            ("flat_rate_costs", Track.TaxBaseOnly, 1_251.20m),
            ("taxable_income", Track.TaxBaseOnly, 40_455.44m),
            ("income_tax", Track.PaidOut, 11_333.96m),
            ("municipal_surcharge", Track.PaidOut, 793.38m),
            ("low_earned_income_credit", Track.PaidOut, 0m),
            ("income_tax_advance_payment_surcharge", Track.PaidOut, 0m),
            ("income_tax_advance_payment_bonus", Track.PaidOut, -0m),
        });
    }

    [Fact]
    public void Compute_2026_tagsEveryStepWithItsLayerAndDirectorPay()
    {
        var result = Compute2026(50_000m);

        result.Steps.Select(step => (step.RuleKey, step.Layer, step.Event)).ShouldBe(new[]
        {
            ("social_contributions", Layer.Social, TaxableEvent.DirectorPay),
            ("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay),
            ("taxable_income", Layer.Personal, TaxableEvent.DirectorPay),
            ("income_tax", Layer.Personal, TaxableEvent.DirectorPay),
            ("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay),
            ("low_earned_income_credit", Layer.Personal, TaxableEvent.DirectorPay),
            ("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay),
            ("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay),
        });
    }

    [Fact]
    public void Compute_2026_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Compute2026(50_000m);

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Fact]
    public void Compute_2026_everyStepListsTheAmountsItUsed()
    {
        var result = Compute2026(50_000m);

        result.Steps.Select(step => (step.RuleKey, string.Join(' ', step.AmountsUsed.Select(amount => amount.Key)))).ShouldBe(new[]
        {
            ("social_contributions", "minimum_income first_band_rate first_band_ceiling second_band_rate second_band_ceiling"),
            ("flat_rate_costs", "flat_rate_costs_rate flat_rate_costs_maximum"),
            ("taxable_income", ""),
            ("income_tax", "bracket_1_rate bracket_1_upper_limit bracket_2_rate bracket_2_upper_limit bracket_3_rate "
                + "bracket_3_upper_limit bracket_4_rate tax_free_amount tax_free_amount_rate autonomy_factor regional_additional_rate"),
            ("municipal_surcharge", "entered_municipal_rate"),
            ("low_earned_income_credit", "low_earned_income_credit low_earned_income_floor low_earned_income_full_from "
                + "low_earned_income_full_up_to low_earned_income_ceiling"),
            ("income_tax_advance_payment_surcharge", "reference_rate base_factor surcharge_multiplier first_credit_multiplier "
                + "second_credit_multiplier third_credit_multiplier fourth_credit_multiplier kept_rate minimum_surcharge minimum_share_rate"),
            ("income_tax_advance_payment_bonus", "reference_rate base_factor first_bonus_multiplier second_bonus_multiplier "
                + "third_bonus_multiplier fourth_bonus_multiplier"),
        });
    }

    // The surcharge step cites the articles that set it up as its rule, and the rate under its own
    // source: the SPW file for a municipality's rate.
    [Fact]
    public void Compute_2026MunicipalityRate_surchargeStepUsesItWithItsSource()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var surcharge = Compute2026(50_000m, namur.Rate).Steps.Single(step => step.RuleKey == "municipal_surcharge");

        (surcharge.Source, surcharge.AmountsUsed.Single()).ShouldBe((PersonalTaxRules.MunicipalSurcharge, namur.Rate));
    }

    [Fact]
    public void Compute_2026EnteredRate_surchargeStepUsesItWithTheArticlesAsSource()
    {
        var entered = MunicipalRates.Entered(0.07m);

        var surcharge = Compute2026(50_000m, entered).Steps.Single(step => step.RuleKey == "municipal_surcharge");

        surcharge.AmountsUsed.Single().ShouldBe(new RuleAmount("entered_municipal_rate", 0.07m, PersonalTaxRules.MunicipalSurcharge));
    }

    // Namur's 8.5%: where the rate comes from changes its source, never the figures.
    [Fact]
    public void Compute_2026MunicipalityRate_givesTheFiguresOfTheSameRateEntered()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var official = Compute2026(50_000m, namur.Rate);
        var entered = Compute2026(50_000m, MunicipalRates.Entered(0.085m));

        (official.MunicipalRate, official.MunicipalSurcharge, official.NetInPocket)
            .ShouldBe((0.085m, entered.MunicipalSurcharge, entered.NetInPocket));
    }

    [Fact]
    public void Compute_2026_incomeTaxStepUsesThe2026TaxFreeAmount()
    {
        var incomeTax = Compute2026(50_000m).Steps.Single(step => step.RuleKey == "income_tax");

        incomeTax.AmountsUsed.Single(amount => amount.Key == "tax_free_amount").Value.ShouldBe(11_550m);
    }

    // Not saying who pays runs the pay as the director's own: every call so far keeps its figures.
    [Fact]
    public void Compute_2026WhoPaysNotGiven_hasTheDirectorPayTheContributions()
    {
        var result = Compute2026(50_000m);

        (result.SocialContributions, result.ContributionsPaidByCompany, result.TaxablePay, result.CompanyCost)
            .ShouldBe((8_293.36m, 0m, 50_000m, 50_000m));
    }

    // The contributions the company pays on the director's behalf are pay too (SPF return explanations,
    // part 2, cadre XVI, 1) and are deducted as the director's own (cadre XVI, 10; art. 52, 7° CIR 92):
    // the taxable pay, and what the pay costs the company, are the payslip gross plus them.
    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_taxablePayAndCompanyCostAreTheGrossPlusTheContributions(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay);

        (result.GrossPay, result.ContributionsPaidByCompany, result.TaxablePay, result.CompanyCost)
            .ShouldBe((grossPay, result.SocialContributions, grossPay + result.SocialContributions, grossPay + result.SocialContributions));
    }

    // The pay less the own contributions is then the payslip gross itself, so the flat-rate costs are
    // 3% of it, up to 3,200, and the taxable income is the gross less those costs.
    public static TheoryData<decimal, decimal> CompanyPaidFlatRateCosts => new()
    {
        { 0m, 0m },
        { 20_000m, 600m },
        { 41_700m, 1_251m },
        { 50_000m, 1_500m },
        { 100_000m, 3_000m },
        { 160_000m, 3_200m },
    };

    [Theory]
    [MemberData(nameof(CompanyPaidFlatRateCosts))]
    public void Compute_2026CompanyPays_flatRateCostsAre3PercentOfTheGrossUpTo3200(decimal grossPay, decimal flatRateCosts)
    {
        CompanyPays2026(grossPay).FlatRateCosts.ShouldBe(flatRateCosts);
    }

    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_taxableIncomeIsTheGrossLessTheFlatRateCosts(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay);

        result.TaxableIncome.ShouldBe(grossPay - result.FlatRateCosts);
    }

    // No fixed point: the contributions are owed on the taxable income, which does not depend on them,
    // so the amount recomputed from it is the contributions to the cent, with no 0.04 step.
    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_contributionsAreThoseOwedOnTheTaxableIncomeToTheCent(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay);

        result.SocialContributions.ShouldBe(SocialContributionCalculator.Annual(SocialContributionRules.IncomeYear2026, result.TaxableIncome));
    }

    // 50,000 on the payslip: 20.50% of 48,500 is 9,942.50, billed as four quarters of 2,485.63, and the
    // tax is that of 48,500 of taxable income on the 2026 scale, 14,954.00 of State tax, 14,954.01 once
    // its federal share and the Walloon tax are each rounded.
    [Fact]
    public void Compute_2026CompanyPaysGross50000_matchesTheDerivedFigures()
    {
        var result = CompanyPays2026(50_000m);

        (result.SocialContributions, result.TaxablePay, result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge,
                result.NetInPocket, result.CompanyCost)
            .ShouldBe((9_942.52m, 59_942.52m, 1_500m, 48_500m, 14_954.01m, 1_046.78m, 33_999.21m, 59_942.52m));
    }

    // The item's reading of T4: a payslip gross of 41,700 leaves T4's taxable income, 40,449.00, and
    // its tax on the 2026 amounts (PersonalTaxCalculatorTests, the derived T4). The engine's
    // contributions on it are 8,292.04, where T4 declares 8,300.
    [Fact]
    public void Compute_2026CompanyPaysGross41700_taxesTheTaxableIncomeOfTheDerivedT4()
    {
        var result = CompanyPays2026(41_700m);

        (result.SocialContributions, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.NetInPocket)
            .ShouldBe((8_292.04m, 40_449.00m, 11_331.06m, 793.17m, 29_575.77m));
    }

    // The contributions leave the company's cost, not the pocket.
    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_netInPocketIsTheGrossLessTheTaxesOnIt(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay);

        result.NetInPocket.ShouldBe(
            grossPay - result.IncomeTax - result.MunicipalSurcharge + result.LowEarnedIncomeCredit
            - result.AdvancePayments.Surcharge + result.AdvancePayments.Bonus);
    }

    // Nothing withheld: the tax at 106%, 15,851.25, bears the 4.5% surcharge, 713.31, kept at 90%.
    [Fact]
    public void Compute_2026CompanyPaysGross50000NothingWithheldOrPrepaid_surchargesTheTaxOutOfThePocket()
    {
        var result = CompanyPays2026(50_000m, TaxPrepayments.None);

        (result.AdvancePayments.Surcharge, result.NetInPocket).ShouldBe((641.98m, 33_357.23m));
    }

    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPaysNothingWithheld_netInPocketIsTheGrossLessTheTaxesOnItAndTheSurcharge(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay, TaxPrepayments.None);

        result.NetInPocket.ShouldBe(
            grossPay - result.IncomeTax - result.MunicipalSurcharge + result.LowEarnedIncomeCredit
            - result.AdvancePayments.Surcharge + result.AdvancePayments.Bonus);
    }

    // So the river's buckets still add up: the company's cost less the steps paid out is the pocket.
    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_companyCostLessWhatIsPaidOutIsTheNetInPocket(decimal grossPay)
    {
        var result = CompanyPays2026(grossPay);

        (result.CompanyCost - result.Steps.Where(step => step.Track == Track.PaidOut).Sum(step => step.Amount)).ShouldBe(result.NetInPocket);
    }

    [Fact]
    public void Compute_2026CompanyPays_socialContributionsStepKeepsItsLayerAndTrack()
    {
        var step = CompanyPays2026(50_000m).Steps.Single(step => step.RuleKey == "social_contributions");

        (step.Layer, step.Event, step.Track, step.Amount).ShouldBe((Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 9_942.52m));
    }

    // The company-paid case is the director-pays case of a pay of the taxable pay with the contributions
    // deducted as own: the same taxable income and the same tax.
    [Theory]
    [InlineData(20_000)]
    [InlineData(41_700)]
    [InlineData(100_000)]
    public void Compute_2026CompanyPays_taxesLikeADirectorPaidTheTaxablePayWhoDeductsTheContributions(int grossPay)
    {
        var result = CompanyPays2026(grossPay);

        var directorPays = PersonalTaxCalculator.Compute(PersonalTaxRules.IncomeYear2026, result.TaxablePay, result.SocialContributions, 0.07m);

        (result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.LowEarnedIncomeCredit)
            .ShouldBe((directorPays.FlatRateCosts, directorPays.TaxableIncome, directorPays.IncomeTax, directorPays.MunicipalSurcharge, directorPays.LowEarnedIncomeCredit));
    }

    // The next 1,000 on the payslip costs the company 1,000 and the contributions it adds, 198.84, which
    // the pocket does not lose: the parts add up to 1,198.84.
    [Fact]
    public void Compute_2026CompanyPaysGross50000_splitsTheNextThousandWithTheContributionsOnTop()
    {
        var result = CompanyPays2026(50_000m);

        result.NextThousand.ShouldBe(new NextThousand(
            SocialContributions: 198.84m, IncomeTax: 436.49m, MunicipalSurcharge: 30.56m, LowEarnedIncomeCredit: 0m, AdvancePayments: 0m,
            NetInPocket: 532.95m));
    }

    [Theory]
    [MemberData(nameof(CompanyPaidGrossPays))]
    public void Compute_2026CompanyPays_nextThousandPartsOtherThanTheContributionsAddUpToOneThousand(decimal grossPay)
    {
        var next = CompanyPays2026(grossPay).NextThousand;

        (next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket).ShouldBe(1_000m);
    }

    // Another income taxed with the pay, a home office's net rent: 828.00, the 2026 cap's
    // (RentCalculatorTests), and 2,000.00 for a larger part let. Derived from the official amounts by
    // the method Tax-Calc T13 and T14 prove on 2025 (PersonalTaxCalculatorTests): one scale on the
    // total, the surcharge on the pay's own tax less the credit granted.
    public static TheoryData<decimal> OtherIncomes => [828.00m, 2_000.00m];

    [Theory]
    [MemberData(nameof(OtherIncomes))]
    public void Compute_2026OtherIncome_leavesThePaysOwnFiguresAsTheyAre(decimal otherIncome)
    {
        var (alone, with) = (Compute2026(27_000m, TaxPrepayments.None), Compute2026(27_000m, TaxPrepayments.None, otherIncome));

        (with.IncomeTax, with.MunicipalSurcharge, with.LowEarnedIncomeCredit, with.NetInPocket, with.Statement.Months[0].Withholding)
            .ShouldBe((alone.IncomeTax, alone.MunicipalSurcharge, alone.LowEarnedIncomeCredit, alone.NetInPocket, alone.Statement.Months[0].Withholding));
    }

    [Theory]
    [MemberData(nameof(OtherIncomes))]
    public void Compute_2026OtherIncome_leavesThePaysOwnStepsAsTheyAre(decimal otherIncome)
    {
        var (alone, with) = (Compute2026(27_000m, TaxPrepayments.None), Compute2026(27_000m, TaxPrepayments.None, otherIncome));

        with.Steps.Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount))
            .ShouldBe(alone.Steps.Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)));
    }

    // 828.00 more on 40,455.44, all of it at 45%: 372.60 of State tax, 372.60 of tax once split, 26.08
    // of municipal surcharge on the total; no credit to lose, nothing to surcharge.
    [Fact]
    public void Compute_2026Gross50000AndOtherIncome828_saysWhatItAddsToTheTax()
    {
        var result = Compute2026(50_000m, Withheld, 828.00m);

        (result.OtherIncomeTax, result.OtherIncomeTax.Total).ShouldBe((new OtherIncomeTax(828.00m, 372.60m, 26.08m, 0m, 0m), 398.68m));
    }

    // What the other income moves not at all is a plain zero: a negative one would read as money
    // coming back. The river's example withholds as the key formula says, where the surcharge falls
    // under the minimum with or without the rent.
    [Fact]
    public void Compute_2026Gross50000AndOtherIncome828_whatItDoesNotMoveIsAPlainZero()
    {
        var tax = Compute2026(50_000m, TaxPrepayments.AsWithheld, 828.00m).OtherIncomeTax;

        (decimal.IsNegative(tax.LowEarnedIncomeCreditLost), decimal.IsNegative(tax.AdvancePayments)).ShouldBe((false, false));
    }

    // 12,127.34 on the pay alone and 398.68 on the rent, less the 10,000 withheld.
    [Fact]
    public void Compute_2026Gross50000AndOtherIncome828_balanceIncludesItsTax()
    {
        var assessment = Compute2026(50_000m, new TaxPrepayments(10_000m, AdvancePayments.None), 828.00m).Assessment;

        (assessment.FinalTax, assessment.Balance).ShouldBe((12_526.02m, 2_526.02m));
    }

    // 27,000 leaves 21,845.95 taxable, where the credit is 910 in full; 2,000 more takes the total to
    // 23,845.95, so the credit is at most 910 × (29,120 − 23,845.95) / 6,720 = 714.19 (art. 289ter,
    // §2, al. 4): 195.81 lost. The tax on the total, 4,142.88, is 799.99 more, its surcharge 56.00
    // more. Nothing withheld: the surcharge base, the pay's 3,342.89 at 106% less the credit granted,
    // rises from 2,633.46 to 2,829.27, and the surcharge from 106.66 to 114.59, 7.93 more.
    [Fact]
    public void Compute_2026Gross27000AndOtherIncome2000NothingWithheld_takesAwayPartOfTheCreditAndRaisesTheSurcharge()
    {
        var result = Compute2026(27_000m, TaxPrepayments.None, 2_000.00m);

        (result.OtherIncomeTax, result.AdvancePayments.Base, result.AdvancePayments.Surcharge)
            .ShouldBe((new OtherIncomeTax(2_000.00m, 799.99m, 56.00m, 195.81m, 7.93m), 2_829.27m, 114.59m));
    }

    // The pay's own surcharge step stays what the pay alone leaves; the rest is the other income's.
    [Fact]
    public void Compute_2026Gross27000AndOtherIncome2000NothingWithheld_thePaysSurchargeStepIsThePayAlonesSurcharge()
    {
        var result = Compute2026(27_000m, TaxPrepayments.None, 2_000.00m);

        result.Steps.Single(step => step.RuleKey == "income_tax_advance_payment_surcharge").Amount.ShouldBe(106.66m);
    }

    // 4,142.88 + 290.00 − 714.19 + 114.59, all of it to pay.
    [Fact]
    public void Compute_2026Gross27000AndOtherIncome2000NothingWithheld_balanceIsTheYearsFinalTax()
    {
        var assessment = Compute2026(27_000m, TaxPrepayments.None, 2_000.00m).Assessment;

        (assessment.FinalTax, assessment.Balance).ShouldBe((3_833.28m, 3_833.28m));
    }

    // The next 1,000 of pay is taxed on the total too: it moves the other income's tax as well, so its
    // parts are the year's. Without the 2,000 they would be 323.64, 22.66, 34.54 of credit, 15.29 of
    // surcharge and 437.99 to the pocket.
    [Fact]
    public void Compute_2026Gross27000AndOtherIncome2000NothingWithheld_splitsTheNextThousandOnTheYear()
    {
        var result = Compute2026(27_000m, TaxPrepayments.None, 2_000.00m);

        result.NextThousand.ShouldBe(new NextThousand(
            SocialContributions: 165.88m, IncomeTax: 323.64m, MunicipalSurcharge: 22.66m, LowEarnedIncomeCredit: 109.56m, AdvancePayments: 18.33m,
            NetInPocket: 359.93m));
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026OtherIncomeNothingWithheld_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var next = Compute2026(grossPay, TaxPrepayments.None, 2_000.00m).NextThousand;

        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    [Fact]
    public void Compute_2026NoOtherIncome_addsNothingToTheTax()
    {
        Compute2026(50_000m).OtherIncomeTax.ShouldBe(OtherIncomeTax.None);
    }

    // Decimal arithmetic can leave a negative zero, which is no income at all, not a negative one.
    [Fact]
    public void Compute_negativeZeroOtherIncome_isNoOtherIncome()
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), Withheld, ContributionsPaidBy.Director, decimal.Negate(0m));

        (error, value?.OtherIncomeTax).ShouldBe((null, OtherIncomeTax.None));
    }

    // The net rent comes from the engine's own rules, never below zero: a negative one is a bug.
    [Fact]
    public void Compute_negativeOtherIncome_isACallersBug()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), Withheld, ContributionsPaidBy.Director, -0.01m));
    }

    [Fact]
    public void Compute_negativeGrossPay_isRefused()
    {
        var (value, error) = GrossToNet.Compute(2026, -0.01m, MunicipalRates.Entered(0.07m), TaxPrepayments.None);

        (value, error).ShouldBe((null, GrossToNet.GrossPayNegative));
    }

    [Theory]
    [MemberData(nameof(MunicipalRatesOutOfRange))]
    public void Compute_municipalRateOutsideZeroToTenPercent_isRefused(decimal municipalRate)
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(municipalRate), TaxPrepayments.None);

        (value, error).ShouldBe((null, GrossToNet.MunicipalRateOutOfRange));
    }

    [Theory]
    [MemberData(nameof(MunicipalRatesAtTheLimits))]
    public void Compute_municipalRateAtZeroOrTenPercent_isAccepted(decimal municipalRate)
    {
        var (_, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(municipalRate), TaxPrepayments.None);

        error.ShouldBeNull();
    }

    // 2025 has personal-tax amounts for the Tax-Calc tests but no INASTI amounts, and 2027's
    // amounts are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void Compute_incomeYearOtherThan2026_isRefused(int incomeYear)
    {
        var (value, error) = GrossToNet.Compute(incomeYear, 50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.None);

        (value, error).ShouldBe((null, GrossToNet.IncomeYearNotSupported));
    }

    private static GrossToNetResult Compute2026(decimal grossPay) => Compute2026(grossPay, MunicipalRates.Entered(0.07m));

    private static GrossToNetResult Compute2026(decimal grossPay, RuleAmount municipalRate, TaxPrepayments? prepayments = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, municipalRate, prepayments ?? Withheld);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static GrossToNetResult Compute2026(decimal grossPay, TaxPrepayments prepayments, decimal otherIncome = 0m)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.Entered(0.07m), prepayments, ContributionsPaidBy.Director, otherIncome);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static GrossToNetResult CompanyPays2026(decimal grossPay, TaxPrepayments? prepayments = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.Entered(0.07m), prepayments ?? Withheld, ContributionsPaidBy.Company);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
