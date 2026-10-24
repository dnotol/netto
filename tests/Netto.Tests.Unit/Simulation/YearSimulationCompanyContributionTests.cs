using Netto.Core.Copyright;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// INASTI's yearly contribution every company owes (CompanyContributionRulesTests): 409.30 in the lower band,
// 1,022.38 in the upper one, for 2026. It is a cost of the company, like its running costs: it comes off what
// the company earns before the levers and the split, the company deducts it, and it leaves the river before
// its start, in no layer's taxes. The made-up company's 120,000 of revenue less its 19,590.70 of running
// costs, 100,409.30, less the lower band's 409.30 leave the 100,000 the split shares (YearSimulationTests);
// the upper band leaves 99,386.92.
public class YearSimulationCompanyContributionTests
{
    private static readonly CompanyContributionRules Rules = CompanyContributionRules.IncomeYear2026;

    public static TheoryData<CompanyContributionBand, decimal> Bands => new()
    {
        { CompanyContributionBand.Lower, 409.30m },
        { CompanyContributionBand.Upper, 1_022.38m },
    };

    // A pay, what the company earns before it pays its director anything, a reserve, and its band.
    public static TheoryData<decimal, decimal, decimal, CompanyContributionBand> Setups => new()
    {
        { 50_000m, 100_409.30m, 0m, CompanyContributionBand.Lower },
        { 50_000m, 100_409.30m, 0m, CompanyContributionBand.Upper },
        { 21_000m, 140_000m, 20_000m, CompanyContributionBand.Upper },
        { 0m, 1_000m, 0m, CompanyContributionBand.Upper },
    };

    [Fact]
    public void Run_2026_takesTheLowerBandOffWhatTheCompanyEarnsBeforeTheSplit()
    {
        var result = Run(Scenario());

        (result.Scenario.ProfitBeforeDirector, result.CompanyContribution, result.ProfitBeforeLevers, result.ProfitBeforePay)
            .ShouldBe((100_409.30m, 409.30m, 100_000m, 100_000m));
    }

    [Theory]
    [MemberData(nameof(Bands))]
    public void Run_2026Band_takesThatBandsAmount(CompanyContributionBand band, decimal contribution)
    {
        var result = Run(Scenario(band: band));

        (result.CompanyContribution, result.ProfitBeforePay).ShouldBe((contribution, 100_409.30m - contribution));
    }

    // The upper band costs 613.08 more and the company deducts it all: its taxable income at 50,000 of pay is
    // 49,386.92, and every figure of the year is that of a company in the lower band with 613.08 more of
    // running costs.
    [Fact]
    public void Run_2026UpperBand_isDeductedAsTheRunningCostsAre()
    {
        var upper = Run(Scenario(band: CompanyContributionBand.Upper));
        var moreCosts = Run(Scenario() with { RunningCosts = RunningCosts + 613.08m });

        upper.ShouldSatisfyAllConditions(
            () => upper.Company.TaxableIncome.ShouldBe(49_386.92m),
            () => (upper.ProfitBeforePay, upper.Company.TaxableIncome, upper.CorporateTax, upper.SocialContributions, upper.PersonalTax, upper.NetInPocket,
                    upper.KeptInCompany)
                .ShouldBe((moreCosts.ProfitBeforePay, moreCosts.Company.TaxableIncome, moreCosts.CorporateTax, moreCosts.SocialContributions,
                    moreCosts.PersonalTax, moreCosts.NetInPocket, moreCosts.KeptInCompany)));
    }

    // The year's first step, before the levers': a cost of the company that sets its tax base, on the
    // corporate layer, citing INASTI's amounts for the year, the law and the article it is deducted under.
    [Theory]
    [MemberData(nameof(Bands))]
    public void Run_2026_theContributionIsTheYearsFirstStep(CompanyContributionBand band, decimal contribution)
    {
        var step = Run(Scenario(band: band, vouchers: AllVouchers)).Steps[0];

        step.ShouldSatisfyAllConditions(
            () => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)
                .ShouldBe(("company_contribution", Layer.Corporate, TaxableEvent.CompanyContribution, Track.TaxBaseOnly, contribution)),
            () => step.Source.ShouldBe(Rules.Source),
            () => step.AmountsUsed.ShouldBe([Rules.Contribution(band), Rules.BalanceSheetThreshold]),
            () => step.AlsoCites.ShouldBe([Rules.Law, Rules.Deduction]));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026_layersPocketAndCompanyAddUpToWhatTheCompanyEarnsLessTheContribution(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, CompanyContributionBand band)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforeDirector + RunningCosts, band: band));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector - Rules.Contribution(band).Value);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026NothingPrepaidWithACar_layersPocketAndCompanyAddUpToWhatTheCompanyEarnsLessTheContributionAndTheCar(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, CompanyContributionBand band)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforeDirector + RunningCosts, band: band,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company, car: ElectricCar));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector - Rules.Contribution(band).Value - result.Car.Costs);
    }

    // What the split shares has one derivation (ProfitBeforePayParityTests): the year, the profit before pay
    // and the pay ceiling take the contribution off alike, beside a fixed royalty or one that follows the pay.
    [Theory]
    [MemberData(nameof(Bands))]
    public void ProfitBeforePayAndPayCeiling_band_areTheYearsProfitBeforePay(CompanyContributionBand band, decimal contribution)
    {
        var scenario = Scenario(band: band, vouchers: AllVouchers, royalty: new CopyrightRoyalty(10_000m));

        (YearSimulation.ProfitBeforePay(scenario), YearSimulation.PayCeiling(scenario))
            .ShouldBe((Run(scenario).ProfitBeforePay, Run(scenario).ProfitBeforePay));
        Run(scenario).ProfitBeforePay.ShouldBe(100_409.30m - contribution - 2_310.20m - 10_000m);
    }

    [Theory]
    [MemberData(nameof(Bands))]
    public void PayCeiling_bandBesideAFollowingRoyalty_leavesTheYearAtItThatPay(CompanyContributionBand band, decimal contribution)
    {
        var scenario = Scenario(band: band, royalty: new CopyrightRoyalty(0m) { CreativeShare = 0.75m, FollowsPay = true });
        var ceiling = YearSimulation.PayCeiling(scenario).ShouldNotBeNull();

        (Run(scenario with { DirectorPay = ceiling }).ProfitBeforePay >= ceiling, Run(scenario with { DirectorPay = ceiling + 0.01m }).ProfitBeforePay < ceiling + 0.01m,
                Run(scenario with { DirectorPay = ceiling }).CompanyContribution)
            .ShouldBe((true, true, contribution));
    }

    // The split's search reads the year at each pay through Level, which takes the contribution off as Run does.
    [Theory]
    [MemberData(nameof(Bands))]
    public void Level_band_isTheYearAtThatPay(CompanyContributionBand band, decimal contribution)
    {
        var scenario = Scenario(directorPay: 30_000m, band: band, car: ElectricCar);

        var level = YearSimulation.Level(scenario).ShouldNotBeNull();

        var year = Run(scenario);
        (level.NetInPocket, level.Dividend, level.KeptInCompany, year.CompanyContribution).ShouldBe((year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, contribution));
    }

    // Art. 215 compares the pay with the taxable income the company has at no pay, which the contribution
    // lowers: the upper band's 1,022.38 keeps the rate from half of 99,386.92, 49,693.46.
    [Fact]
    public void SmallestPayKeepingReducedRate_upperBand_isHalfTheTaxableIncomeItLeaves()
    {
        YearSimulation.SmallestPayKeepingReducedRate(Scenario(band: CompanyContributionBand.Upper)).ShouldBe(49_693.46m);
    }

    // A company that earns less than the contribution has a loss before it pays its director: the split
    // gives nothing.
    [Fact]
    public void ProfitBeforePay_contributionAboveWhatTheCompanyEarns_leavesALossAndNoPay()
    {
        var scenario = Scenario(revenue: RunningCosts + 100m);

        (YearSimulation.ProfitBeforePay(scenario), YearSimulation.PayCeiling(scenario)).ShouldBe(((decimal?)(-309.30m), (decimal?)0m));
    }

    // A year whose rule sets have no contribution has no year at all: 2027's amounts are not published.
    [Fact]
    public void Run_unsupportedIncomeYear_isRefusedAndHasNoProfitBeforePay()
    {
        var scenario = Scenario() with { IncomeYear = UnsupportedIncomeYear };

        (YearSimulation.Run(scenario).Error, YearSimulation.ProfitBeforePay(scenario), YearSimulation.PayCeiling(scenario), YearSimulation.Level(scenario))
            .ShouldBe((YearSimulation.IncomeYearNotSupported, null, null, null));
    }
}
