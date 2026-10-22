using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Simulation;

// YearSimulationTests' made-up company, 100,000 of profit before the director, paying its director a
// copyright royalty for the rights on their works: 10,000 beside 50,000 of pay, within the limits, or
// 20,000 beside 30,000, of which 15,000 within and 5,000 taxed as pay (CopyrightCalculatorTests). It
// prepays enough to leave no surcharge, and withholds more than any tax on the pay, unless a test says
// otherwise.
public class YearSimulationCopyrightTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    private static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    public static TheoryData<decimal, decimal, decimal, bool> Setups => new()
    {
        { 50_000m, 100_000m, 10_000m, true },
        { 30_000m, 100_000m, 20_000m, true },
        { 0m, 100_000m, 10_000m, true },
        { 160_000m, 300_000m, 100_000m, true },
        { 50_000m, 100_000m, 10_000m, false },
        { 5_000m, 30_000m, 20_000m, true },
    };

    // The company deducts the royalty in full like any cost (art. 49): it comes off what the split
    // shares, and nothing of it is added back.
    [Fact]
    public void Run_2026Royalty_comesOffTheProfitTheSplitShares()
    {
        var result = Run(Scenario(royalty: new CopyrightRoyalty(10_000m)));

        (result.ProfitBeforePay, result.DisallowedExpenses).ShouldBe((90_000m, 0m));
    }

    // The pay's figures are GrossToNet's on the pay with the royalty's plain figures.
    [Fact]
    public void Run_2026RoyaltyBeyondTheLimits_taxesThePartBeyondThemWithThePay()
    {
        var royalty = CopyrightCalculator.Compute(CopyrightRules.IncomeYear2026, new CopyrightRoyalty(20_000m), 30_000m).Value!;
        var (pay, _) = GrossToNet.Compute(2026, 30_000m, MunicipalRates.Entered(0.07m), Withheld, ContributionsPaidBy.Director, 0m, 0m, royalty.MovableIncome);

        var result = Run(Scenario(directorPay: 30_000m, royalty: new CopyrightRoyalty(20_000m)));

        (result.DirectorPay.RequalifiedPay, result.DirectorPay.TaxablePay, result.DirectorPay.NetInPocket, result.DirectorPay.TaxedApartTax)
            .ShouldBe((5_000m, 35_000m, pay!.NetInPocket, pay.TaxedApartTax));
    }

    [Fact]
    public void Run_2026Royalty_holdsTheRoyaltyAsTheRulesTookIt()
    {
        var result = Run(Scenario(directorPay: 30_000m, royalty: new CopyrightRoyalty(20_000m)));

        (result.Copyright.WithinLimits, result.Copyright.TaxedAsPay, result.Copyright.Withheld).ShouldBe((15_000.00m, 5_000.00m, 3_750.00m));
    }

    // What reaches the pocket of the part within the limits is that part less its tax apart: the pocket
    // is the pay's own net, the dividend's, and the part within less what it adds to the tax.
    [Fact]
    public void Run_2026RoyaltyWithinTheLimits_reachesThePocketLessItsTax()
    {
        var result = Run(Scenario(royalty: new CopyrightRoyalty(10_000m)));

        result.NetInPocket.ShouldBe(result.DirectorPay.NetInPocket + result.Dividend.NetDividend + 10_000m - result.DirectorPay.TaxedApartTax.Total);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026Royalty_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal royalty, bool averageWithinCap)
    {
        var result = Run(Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            royalty: new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(profitBeforeDirector);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026RoyaltyCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUp(
        decimal directorPay, decimal profitBeforeDirector, decimal royalty, bool averageWithinCap)
    {
        var result = Run(Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            royalty: new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(profitBeforeDirector);
    }

    // What the royalty brings the ledger, in order: what the company deducts, the part within the
    // limits reaching the pocket, the part taxed as pay; then, after the pay's steps, the tax apart and
    // its municipal surcharge, the credit it takes away and the surcharge that adds.
    [Fact]
    public void Run_2026Royalty_listsItsSteps()
    {
        var result = Run(Scenario(directorPay: 30_000m, royalty: new CopyrightRoyalty(20_000m)));
        var tax = result.DirectorPay.TaxedApartTax;

        result.Steps.Where(step => step.Event == TaxableEvent.CopyrightRoyalty).Select(step => (step.RuleKey, step.Layer, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("copyright_royalty", Layer.Corporate, Track.TaxBaseOnly, 20_000m),
            ("copyright_within_limits", Layer.Personal, Track.InPocket, 15_000.00m),
            ("copyright_taxed_as_pay", Layer.Personal, Track.TaxBaseOnly, 5_000.00m),
            ("copyright_tax", Layer.Personal, Track.PaidOut, tax.IncomeTax),
            ("copyright_municipal_surcharge", Layer.Personal, Track.PaidOut, tax.MunicipalSurcharge),
            ("copyright_low_earned_income_credit_lost", Layer.Personal, Track.PaidOut, tax.LowEarnedIncomeCreditLost),
            ("copyright_income_tax_advance_payments", Layer.Personal, Track.PaidOut, tax.AdvancePayments),
        });
    }

    // The tax steps come after the pay's own, the company's first.
    [Fact]
    public void Run_2026Royalty_listsItsTaxAfterThePay()
    {
        var keys = Run(Scenario(royalty: new CopyrightRoyalty(10_000m))).Steps.Select(step => step.RuleKey).ToList();

        (keys.IndexOf("copyright_royalty") < keys.IndexOf("corporate_tax"), keys.IndexOf("income_tax") < keys.IndexOf("copyright_tax"))
            .ShouldBe((true, true));
    }

    [Fact]
    public void Run_2026Royalty_citesItsTexts()
    {
        var rules = CopyrightRules.IncomeYear2026;
        var steps = Run(Scenario(royalty: new CopyrightRoyalty(10_000m))).Steps.Where(step => step.Event == TaxableEvent.CopyrightRoyalty).ToDictionary(step => step.RuleKey);

        steps.ShouldSatisfyAllConditions(
            () => steps["copyright_royalty"].Source.ShouldBe(rules.Deduction),
            () => steps["copyright_within_limits"].Source.ShouldBe(rules.Limits),
            () => steps["copyright_within_limits"].AmountsUsed.ShouldBe([rules.RelativeLimit, rules.Cap]),
            () => steps["copyright_within_limits"].AlsoCites.ShouldBe([rules.PhaseIn, rules.OutsideContributions, rules.OutsideAdvancePaymentSurcharge]),
            () => (steps["copyright_taxed_as_pay"].Source, steps["copyright_taxed_as_pay"].AlsoCites.Single()).ShouldBe((rules.TaxedAsPay, rules.Limits)),
            () => (steps["copyright_tax"].Source, steps["copyright_tax"].AmountsUsed[0]).ShouldBe((rules.TaxedApart, rules.TaxedApartRate)),
            () => steps["copyright_tax"].AlsoCites.ShouldBe([rules.NoFlatRateCosts!]),
            () => steps["copyright_municipal_surcharge"].Source.ShouldBe(PersonalTaxRules.MunicipalSurcharge));
    }

    [Fact]
    public void Run_2026Royalty_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        Run(Scenario(royalty: new CopyrightRoyalty(10_000m))).Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    // Art. 215's minimum pay never counts the royalty, nor the part of it taxed as pay (decided
    // unattended): 49,000 of pay beside 30,000 of royalty, 6,300 of it beyond 30% of 79,000, is 55,300
    // of pay taxed, but 49,000 is what art. 215 tests, and 101,000 of taxable income is above it.
    [Fact]
    public void Run_2026RoyaltyBeyondTheLimitsBesideAPayUnderTheMinimum_losesTheRate()
    {
        var result = Run(Scenario(directorPay: 49_000m, revenue: 200_000m, royalty: new CopyrightRoyalty(30_000m)));

        (result.DirectorPay.TaxablePay, result.Company.DirectorPay, result.Company.Condition).ShouldBe((55_300m, 49_000m, ReducedRateCondition.MinimumPayNotMet));
    }

    // The assessment adds the royalty's tax and surcharge and credits what the company withheld on it.
    [Fact]
    public void Run_2026Royalty_creditsItsWithholdingOnTheAssessment()
    {
        var with = Run(Scenario(royalty: new CopyrightRoyalty(10_000m)));
        var without = Run(Scenario(revenue: 110_000m));

        (with.DirectorPay.Assessment.WithheldBesidesPay, with.DirectorPay.Assessment.Balance)
            .ShouldBe((1_500m, without.DirectorPay.Assessment.Balance + with.DirectorPay.TaxedApartTax.Total - 1_500m));
    }

    [Fact]
    public void Run_2026NoRoyalty_listsNoStepOfIt()
    {
        Run(Scenario()).Steps.ShouldNotContain(step => step.Event == TaxableEvent.CopyrightRoyalty);
    }

    [Fact]
    public void Run_2026NoRoyalty_holdsNone()
    {
        Run(Scenario()).Copyright.ShouldBe(CopyrightResult.None);
    }

    [Fact]
    public void Run_negativeRoyalty_isRefusedWithTheCalculatorsKey()
    {
        YearSimulation.Run(Scenario(royalty: new CopyrightRoyalty(-0.01m))).ShouldBe((null, CopyrightCalculator.RoyaltyNegative));
    }

    // Taken as given even while the rules refuse it, so the split keeps its place.
    [Theory]
    [InlineData(10_000, 90_000)]
    [InlineData(-1_000, 101_000)]
    public void ProfitBeforePay_royalty_isWhatTheCompanyHasLeftOnceItPaidIt(double royalty, double expected)
    {
        YearSimulation.ProfitBeforePay(Scenario(royalty: new CopyrightRoyalty((decimal)royalty))).ShouldBe((decimal)expected);
    }

    private static Scenario Scenario(
        decimal directorPay = 50_000m,
        decimal revenue = 120_000m,
        AdvancePayments? advancePayments = null,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director,
        CopyrightRoyalty? royalty = null) =>
        new(2026, revenue, 20_000m, SmallPastStartUp, directorPay, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.VvprbisThirdYearOnwards)
        {
            AdvancePayments = advancePayments ?? Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
            Copyright = royalty ?? CopyrightRoyalty.None,
        };

    private static YearResult Run(Scenario scenario)
    {
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
