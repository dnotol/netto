using Netto.Core.Allowances;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// YearSimulationTests' made-up company, 100,000 of profit before the director, paying its director a
// copyright royalty for the rights on their works: 10,000 beside 50,000 of pay, within the limits, or
// 20,000 beside 30,000, of which 15,000 within and 5,000 taxed as pay (CopyrightCalculatorTests), with or
// without an attestation du travail des arts, which takes the flat-rate costs off the part within. It
// prepays enough to leave no surcharge, and withholds more than any tax on the pay, unless a test says
// otherwise.
public class YearSimulationCopyrightTests
{
    public static TheoryData<decimal, decimal, decimal, bool, bool> Setups => new()
    {
        { 50_000m, 100_000m, 10_000m, true, false },
        { 30_000m, 100_000m, 20_000m, true, false },
        { 0m, 100_000m, 10_000m, true, false },
        { 160_000m, 300_000m, 100_000m, true, false },
        { 50_000m, 100_000m, 10_000m, false, false },
        { 5_000m, 30_000m, 20_000m, true, false },
        { 50_000m, 100_000m, 10_000m, true, true },
        { 30_000m, 100_000m, 20_000m, true, true },
        { 160_000m, 300_000m, 100_000m, true, true },
        { 50_000m, 100_000m, 10_000m, false, true },
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
        var (pay, _) = GrossToNet.Compute(
            2026, 30_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Director, new TaxedWithPay { Movable = royalty.MovableIncome });

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

    // The flat-rate costs only lower what is taxed: the whole part within reaches the pocket, less a tax
    // on 5,000 of its 10,000.
    [Fact]
    public void Run_2026RoyaltyWithAnAttestation_reachesThePocketWholeLessTheTaxOnItsNet()
    {
        var result = Run(Scenario(royalty: new CopyrightRoyalty(10_000m) { HoldsAttestation = true }));

        (result.DirectorPay.TaxedApartTax.Income, result.NetInPocket)
            .ShouldBe((5_000m, result.DirectorPay.NetInPocket + result.Dividend.NetDividend + 10_000m - result.DirectorPay.TaxedApartTax.Total));
    }

    // With the costs, the tax apart is on half as much, and the pocket gains what it no longer pays.
    [Fact]
    public void Run_2026RoyaltyWithAnAttestation_leavesMoreInThePocketThanWithout()
    {
        var with = Run(Scenario(royalty: new CopyrightRoyalty(10_000m) { HoldsAttestation = true }));
        var without = Run(Scenario(royalty: new CopyrightRoyalty(10_000m)));

        (with.NetInPocket - without.NetInPocket).ShouldBe(without.DirectorPay.TaxedApartTax.Total - with.DirectorPay.TaxedApartTax.Total);
    }

    // The assessment credits what was withheld on the net: 15% of 5,000.
    [Fact]
    public void Run_2026RoyaltyWithAnAttestation_creditsTheWithholdingOnItsNet()
    {
        Run(Scenario(royalty: new CopyrightRoyalty(10_000m) { HoldsAttestation = true })).DirectorPay.Assessment.WithheldBesidesPay.ShouldBe(750m);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026Royalty_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal royalty, bool averageWithinCap, bool attestation)
    {
        var result = Run(Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            royalty: new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap, HoldsAttestation = attestation }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(profitBeforeDirector);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026RoyaltyCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUp(
        decimal directorPay, decimal profitBeforeDirector, decimal royalty, bool averageWithinCap, bool attestation)
    {
        var result = Run(Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            royalty: new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap, HoldsAttestation = attestation }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(profitBeforeDirector);
    }

    // What the royalty brings the ledger, in order: what the company deducts, the part within the
    // limits reaching the pocket, the flat-rate costs taken off it, none without an attestation, the part
    // taxed as pay; then, after the pay's steps, the tax apart and its municipal surcharge, the credit it
    // takes away and the surcharge that adds.
    [Fact]
    public void Run_2026Royalty_listsItsSteps()
    {
        var result = Run(Scenario(directorPay: 30_000m, royalty: new CopyrightRoyalty(20_000m)));
        var tax = result.DirectorPay.TaxedApartTax;

        result.Steps.Where(step => step.Event == TaxableEvent.CopyrightRoyalty).Select(step => (step.RuleKey, step.Layer, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("copyright_royalty", Layer.Corporate, Track.TaxBaseOnly, 20_000m),
            ("copyright_within_limits", Layer.Personal, Track.InPocket, 15_000.00m),
            ("copyright_flat_rate_costs", Layer.Personal, Track.TaxBaseOnly, 0m),
            ("copyright_taxed_as_pay", Layer.Personal, Track.TaxBaseOnly, 5_000.00m),
            ("copyright_tax", Layer.Personal, Track.PaidOut, tax.IncomeTax),
            ("copyright_municipal_surcharge", Layer.Personal, Track.PaidOut, tax.MunicipalSurcharge),
            ("copyright_low_earned_income_credit_lost", Layer.Personal, Track.PaidOut, tax.LowEarnedIncomeCreditLost),
            ("copyright_income_tax_advance_payments", Layer.Personal, Track.PaidOut, tax.AdvancePayments),
        });
    }

    // 50% of the first 20,590 and 25% of the rest of the 24,000 within the limits beside 50,000 of pay.
    [Fact]
    public void Run_2026RoyaltyWithAnAttestation_listsItsFlatRateCosts()
    {
        var result = Run(Scenario(royalty: new CopyrightRoyalty(30_000m) { HoldsAttestation = true }));

        result.Steps.Single(step => step.RuleKey == "copyright_flat_rate_costs").Amount.ShouldBe(11_147.50m);
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
            () => steps["copyright_tax"].AlsoCites.ShouldBeEmpty(),
            () => steps["copyright_flat_rate_costs"].Source.ShouldBe(rules.FlatRateCosts.Bands),
            () => steps["copyright_flat_rate_costs"].AmountsUsed.ShouldBe(rules.FlatRateCosts.Amounts),
            () => steps["copyright_flat_rate_costs"].AlsoCites.ShouldBe([rules.FlatRateCosts.NetIncome, rules.FlatRateCosts.Who, rules.FlatRateCosts.Attestation!]),
            () => steps["copyright_municipal_surcharge"].Source.ShouldBe(MunicipalRates.IncomeYear2026.Surcharge));
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

    // A royalty that follows the pay is what an advance ruling accepts at the scenario's own pay
    // (CopyrightCalculatorTests): 11,538.46 for 75% of the time creating beside 50,000, a third of 30,000
    // for all of it.
    [Theory]
    [InlineData(50_000, 0.75, 11_538.46)]
    [InlineData(30_000, 1, 10_000)]
    [InlineData(0, 0.75, 0)]
    public void Run_2026RoyaltyFollowingThePay_paysWhatARulingAcceptsAtThePay(int pay, double share, double expected)
    {
        Run(Scenario(directorPay: pay, royalty: Following((decimal)share))).Copyright.Royalty.ShouldBe((decimal)expected);
    }

    public static TheoryData<decimal, decimal, decimal, bool, ContributionsPaidBy, bool> FollowingSetups => new()
    {
        { 50_000m, 100_000m, 0.75m, false, ContributionsPaidBy.Director, false },
        { 30_000m, 100_000m, 1m, true, ContributionsPaidBy.Director, false },
        { 0m, 100_000m, 0.75m, false, ContributionsPaidBy.Director, false },
        { 160_000m, 300_000m, 1m, false, ContributionsPaidBy.Company, false },
        { 300_000m, 400_000m, 1m, true, ContributionsPaidBy.Director, true },
        { 40_000m, 100_000m, 0.6m, false, ContributionsPaidBy.Company, true },
    };

    // Every figure of the year, and every step's amount, is the year's with that royalty fixed.
    [Theory]
    [MemberData(nameof(FollowingSetups))]
    public void Run_2026RoyaltyFollowingThePay_isTheYearWithWhatItCameToFixed(
        decimal directorPay, decimal profitBeforeDirector, decimal share, bool attestation, ContributionsPaidBy paidBy, bool asWithheld)
    {
        Scenario With(CopyrightRoyalty royalty) => Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            taxPrepayments: asWithheld ? TaxPrepayments.AsWithheld : null, paidBy: paidBy, royalty: royalty);
        var following = Run(With(Following(share) with { HoldsAttestation = attestation }));

        var fixedYear = Run(With(new CopyrightRoyalty(following.Copyright.Royalty) { CreativeShare = share, HoldsAttestation = attestation }));

        FiguresOf(following).ShouldBe(FiguresOf(fixedYear));
    }

    [Theory]
    [MemberData(nameof(FollowingSetups))]
    public void Run_2026RoyaltyFollowingThePay_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirector(
        decimal directorPay, decimal profitBeforeDirector, decimal share, bool attestation, ContributionsPaidBy paidBy, bool asWithheld)
    {
        var result = Run(Scenario(directorPay: directorPay, revenue: profitBeforeDirector + 20_000m,
            taxPrepayments: asWithheld ? TaxPrepayments.AsWithheld : null, paidBy: paidBy, royalty: Following(share) with { HoldsAttestation = attestation }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(profitBeforeDirector);
    }

    // The year's scenario holds what the royalty came to, still following the pay, so a year run again at
    // another pay follows it there.
    [Fact]
    public void Run_2026RoyaltyFollowingThePay_holdsWhatItCameToInItsScenario()
    {
        Run(Scenario(royalty: Following(0.75m))).Scenario.Copyright.ShouldBe(Following(0.75m) with { Royalty = 11_538.46m });
    }

    [Fact]
    public void Run_2026RoyaltyFollowingThePay_saysItIsWhatARulingAccepts()
    {
        Run(Scenario(royalty: Following(0.75m))).Copyright.Following.ShouldBe(CopyrightFollowing.AtCeiling);
    }

    // The amount the royalty step deducts was set by what a ruling accepts: it cites the amounts that
    // measured it. A royalty the user fixed cites none.
    [Fact]
    public void Run_2026RoyaltyFollowingThePay_itsStepCitesTheAmountsARulingAcceptsItBy()
    {
        var measured = CopyrightCalculator.Compute(CopyrightRules.IncomeYear2026, Following(0.75m), 50_000m).Value.ShouldNotBeNull();

        var step = Run(Scenario(royalty: Following(0.75m))).Steps.Single(step => step.RuleKey == "copyright_royalty");

        step.AmountsUsed.ShouldBe(measured.CeilingAmounts);
    }

    [Fact]
    public void Run_2026FixedRoyalty_itsStepCitesNoAmount()
    {
        Run(Scenario(royalty: new CopyrightRoyalty(10_000m) { CreativeShare = 0.75m })).Steps.Single(step => step.RuleKey == "copyright_royalty")
            .AmountsUsed.ShouldBeEmpty();
    }

    // None is never a refusal: without a share, with the four previous years above the cap, or with no
    // time creating, the year is the year without a royalty, and says why.
    [Theory]
    [InlineData(null, true, CopyrightFollowing.NoCreativeShare)]
    [InlineData(0.75, false, CopyrightFollowing.AverageAboveCap)]
    [InlineData(0.0, true, CopyrightFollowing.AtCeiling)]
    public void Run_2026RoyaltyFollowingWithNothingARulingAccepts_isTheYearWithoutARoyalty(double? share, bool averageWithinCap, CopyrightFollowing why)
    {
        var without = Run(Scenario());

        var result = Run(Scenario(royalty: Following((decimal?)share) with { AverageWithinCap = averageWithinCap }));

        (result.Steps.Any(step => step.Event == TaxableEvent.CopyrightRoyalty), result.NetInPocket, result.Copyright)
            .ShouldBe((false, without.NetInPocket, CopyrightResult.None with { Following = why }));
    }

    // What the split shares is what the company has once it paid what a ruling accepts at the pay.
    [Fact]
    public void ProfitBeforePay_royaltyFollowingThePay_isWhatTheCompanyHasLeftOnceItPaidWhatARulingAccepts()
    {
        YearSimulation.ProfitBeforePay(Scenario(royalty: Following(0.75m))).ShouldBe(88_461.54m);
    }

    // A royalty the user fixes is at most what the company has before pay and royalty, the 100,000 it
    // earns here: one cent more would be paid at a loss.
    [Theory]
    [InlineData(100_000.00, null)]
    [InlineData(100_000.01, CopyrightCalculator.RoyaltyAboveProfit)]
    public void Run_2026FixedRoyaltyAgainstWhatTheCompanyHasBeforePay_isRefusedOnlyAboveIt(decimal royalty, string? error)
    {
        YearSimulation.Run(Scenario(directorPay: 0m, royalty: new CopyrightRoyalty(royalty))).Error.ShouldBe(error);
    }

    // What the levers above leave: 600 of phone costs leave 99,400.
    [Theory]
    [InlineData(99_400.00, null)]
    [InlineData(99_400.01, CopyrightCalculator.RoyaltyAboveProfit)]
    public void Run_2026FixedRoyaltyAgainstWhatTheLeversAboveLeave_isRefusedOnlyAboveIt(decimal royalty, string? error)
    {
        var scenario = Scenario(directorPay: 0m, royalty: new CopyrightRoyalty(royalty)) with { Allowances = new ExpenseAllowances(0m, 0m, 0m, 600m) };

        YearSimulation.Run(scenario).Error.ShouldBe(error);
    }

    // A company earning nothing before pay has nothing to pay a royalty with; without one, its year runs.
    [Theory]
    [InlineData(0, null)]
    [InlineData(1, CopyrightCalculator.RoyaltyAboveProfit)]
    public void Run_2026FixedRoyaltyWhileTheCompanyLosesMoney_isRefusedFromTheFirstEuro(int royalty, string? error)
    {
        YearSimulation.Run(Scenario(revenue: 10_000m, royalty: new CopyrightRoyalty(royalty))).Error.ShouldBe(error);
    }

    // A royalty that follows the pay is bounded by the pay ceiling, which the river holds the pay to: a
    // pay beyond it is not the royalty's to refuse, even where what a ruling accepts there, 31,666.66
    // beside 95,000, is more than the 10,000 the company has before pay and royalty.
    [Fact]
    public void Run_2026RoyaltyFollowingBesideAPayBeyondThePayCeiling_isNotRefused()
    {
        YearSimulation.Run(Scenario(directorPay: 95_000m, revenue: 30_000m, royalty: Following(1m))).Error.ShouldBeNull();
    }

    // The most the split gives as pay beside a fixed royalty is the profit before pay, never below zero.
    [Theory]
    [InlineData(10_000, 90_000)]
    [InlineData(-1_000, 101_000)]
    [InlineData(150_000, 0)]
    public void PayCeiling_fixedRoyalty_isTheProfitBeforePayNeverBelowZero(int royalty, int expected)
    {
        YearSimulation.PayCeiling(Scenario(royalty: new CopyrightRoyalty(royalty))).ShouldBe(expected);
    }

    // Beside a royalty that follows the pay, the largest pay the company pays together with what a ruling
    // accepts beside it: a share r of the envelope leaves the pay 1 − r of what both share, 81,250 of
    // 100,000 at 18.75%, and once the royalty reaches the cap, what both share less the cap.
    [Theory]
    [InlineData(100_000, 0.75, 81_250)]
    [InlineData(100_000, 0.6, 85_000)]
    [InlineData(80_000, 1, 60_000)]
    [InlineData(400_000, 1, 322_780)]
    public void PayCeiling_royaltyFollowingThePay_isTheLargestPayTheProfitPaysWithWhatARulingAccepts(int profit, double share, int expected)
    {
        YearSimulation.PayCeiling(Scenario(revenue: profit + 20_000m, royalty: Following((decimal)share))).ShouldBe(expected);
    }

    // To the cent: the ceiling and its royalty fit what both share, a cent more of pay and its royalty do
    // not.
    [Theory]
    [InlineData(99_999.99, 0.75)]
    [InlineData(123_456.78, 0.33)]
    [InlineData(333_333.33, 1)]
    public void PayCeiling_royaltyFollowingThePay_aCentMoreIsMoreThanTheProfitPays(double profit, double share)
    {
        var royalty = Following((decimal)share);
        decimal WithItsRoyalty(decimal pay) => pay + CopyrightCalculator.Ceiling(CopyrightRules.IncomeYear2026, royalty, pay).ShouldNotBeNull();

        var ceiling = YearSimulation.PayCeiling(Scenario(revenue: (decimal)profit + 20_000m, royalty: royalty)).ShouldNotBeNull();

        (WithItsRoyalty(ceiling) <= (decimal)profit, WithItsRoyalty(ceiling + 0.01m) <= (decimal)profit).ShouldBe((true, false));
    }

    // While a ruling accepts nothing, the pay alone shares what the company has.
    [Fact]
    public void PayCeiling_royaltyFollowingWithoutAShare_isWhatThePayAndTheRoyaltyShare()
    {
        YearSimulation.PayCeiling(Scenario(royalty: Following(null))).ShouldBe(100_000m);
    }

    [Fact]
    public void PayCeiling_royaltyFollowingTheLossBeforePay_isNothing()
    {
        YearSimulation.PayCeiling(Scenario(revenue: 10_000m, royalty: Following(0.75m))).ShouldBe(0m);
    }

    [Fact]
    public void PayCeiling_yearWithoutRules_isNone()
    {
        YearSimulation.PayCeiling(Scenario(royalty: Following(0.75m)) with { IncomeYear = 2024 }).ShouldBeNull();
    }

    private static CopyrightRoyalty Following(decimal? share) => new(0m) { CreativeShare = share, FollowsPay = true };

    // The year's figures, with the royalty as the rules took it but not how it was set, and each step by
    // its key and amount.
    private static YearFigures FiguresOf(YearResult year) => new(
        year.ProfitBeforePay, year.DisallowedExpenses, year.CorporateTax, year.SocialContributions, year.PersonalTax, year.NetInPocket, year.KeptInCompany,
        year.MaximumDividend, year.Copyright with { Following = null, CeilingAmounts = [] }, year.DirectorPay.TaxablePay, year.DirectorPay.NetInPocket,
        year.DirectorPay.TaxedApartTax, year.DirectorPay.Assessment.Balance, string.Join("; ", year.Steps.Select(step => $"{step.RuleKey} {step.Amount}")));

    private sealed record YearFigures(
        decimal ProfitBeforePay, decimal DisallowedExpenses, decimal CorporateTax, decimal SocialContributions, decimal PersonalTax, decimal NetInPocket,
        decimal KeptInCompany, decimal MaximumDividend, CopyrightResult Copyright, decimal TaxablePay, decimal PayNetInPocket, OtherIncomeTax TaxedApartTax,
        decimal Balance, string Steps);
}
