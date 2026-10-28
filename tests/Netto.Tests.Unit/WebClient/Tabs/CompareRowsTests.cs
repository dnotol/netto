using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;
using static Netto.Tests.WebClient.River.PlanFixtures;

namespace Netto.Tests.WebClient.Tabs;

// The rows of the Compare plans table, on the river's example against the example at its best pay
// (20,799), whose figures BucketsViewTests and BaselineGainTests give: 61,975.74 in the pocket against
// 64,526.87, 38,024.26 of taxes against 35,473.13. The taxes on pay and on profit the bar draws split
// that into the three layers of the law: corporate tax 10,000 and its advance-payment surcharge 675,
// contributions 8,293.36, and the personal tax, the income tax 11,333.96 and the municipal surcharge
// 793.38 on the pay and 7,078.50 withheld on the dividend less the refund of 149.94; at the best pay
// corporate tax 19,800.25 and 1,336.52, contributions 3,561.68, income tax 1,292.58 and surcharge 90.48
// less the credit of 910 on the pay, and 10,451.56 less 149.94 on the dividend.
public class CompareRowsTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);
    private static readonly RiverInputs ElectricCar =
        RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    public static TheoryData<decimal, decimal, decimal> Years => new()
    {
        { 120_000m, 50_000m, 0m },
        { 120_000m, 50_000m, 15_000m },
        { 220_000m, 21_000m, 30_000m },
        { 60_000m, 50_000m, 0m },
        { 40_000m, 30_000m, 5_000m },
    };

    [Fact]
    public void Rows_pocket_isTheBarsPocketAndTheGainOfBaselineGain()
    {
        var row = Row(Compare(RiverInputs.Example, BestPay), "pocket");

        (row.Plan, row.Against, row.Difference).ShouldBe(
            (CompareFigure.Money(61_975.74m), CompareFigure.Money(64_526.87m), new CompareDifference(DifferenceKind.More, 2_551.13m)));
    }

    [Fact]
    public void Rows_pocket_againstAPlanThatLeavesMore_isLess()
    {
        var row = Row(Compare(BestPay, RiverInputs.Example), "pocket");

        row.Difference.ShouldBe(new CompareDifference(DifferenceKind.Less, 2_551.13m));
    }

    // What reaches the pocket out of what leaves the company, as the bar says it, and the points between
    // the two as drawn, a tenth of a percent: 62.0% and 64.5%.
    [Fact]
    public void Rows_reach_isTheBarsShareAndThePointsBetweenTheShownShares()
    {
        var row = Row(Compare(RiverInputs.Example, BestPay), "reach");

        (row.Plan, row.Against, row.Difference).ShouldBe(
            (CompareFigure.Share(0.6197574m), CompareFigure.Share(0.6452687m), new CompareDifference(DifferenceKind.More, 2.5m)));
    }

    // 62.04% and 62.06% read 62.0% and 62.1%: the column reads as a subtraction, so a tenth of a point,
    // not the two hundredths between the shares themselves.
    [Fact]
    public void Points_sharesAsShown_areSubtractedAsShown()
    {
        (CompareRows.Points(0.6204m, 0.6206m), CompareRows.Points(0.6206m, 0.6204m), CompareRows.Points(0.62m, 0.62m)).ShouldBe((0.1m, -0.1m, 0m));
    }

    [Fact]
    public void Rows_keptInTheCompany_isTheBarsKept()
    {
        var view = Compare(RiverInputs.Example, BestPay.WithReserve(10_000m));

        var row = Row(view, "kept");

        (row.Plan, row.Against.Amount, row.Difference.Kind).ShouldBe((CompareFigure.Money(0m), view.Against!.Run.Year!.KeptInCompany, DifferenceKind.More));
    }

    [Fact]
    public void Rows_taxes_areEachLayerOfTheLawAndTheirTotal()
    {
        var view = Compare(RiverInputs.Example, BestPay);

        string[] keys = ["taxes", "corporate_tax", "social_contributions", "personal_tax"];
        var rows = keys.Select(key => Row(view, key)).ToList();

        rows.Select(row => (row.Plan.Amount, row.Against.Amount, row.Difference.Kind, row.Difference.Amount)).ShouldBe(
        [
            (38_024.26m, 35_473.13m, DifferenceKind.Less, 2_551.13m),
            (10_675.00m, 21_136.77m, DifferenceKind.More, 10_461.77m),
            (8_293.36m, 3_561.68m, DifferenceKind.Less, 4_731.68m),
            (19_055.90m, 10_774.68m, DifferenceKind.Less, 8_281.22m),
        ]);
    }

    // The layers are read as parts of all taxes.
    [Fact]
    public void Rows_taxLayers_areSubRowsOfAllTaxes()
    {
        var view = Compare(RiverInputs.Example, BestPay);

        view.Groups.Single(group => group.Key == "taxes").Rows.Select(row => (row.Key, row.Sub)).ShouldBe(
            [("taxes", false), ("corporate_tax", true), ("social_contributions", true), ("personal_tax", true)]);
    }

    // Taxes are what the engine's layers took: the year's own figures, and the bar's two tax buckets
    // together; with the pocket, what the company keeps and its expenses, the contribution and the car's
    // costs, they are what the river started from.
    [Theory]
    [MemberData(nameof(Years))]
    public void Rows_everyColumn_addsUpToWhatTheRiverStartsFromAndToTheBar(decimal revenue, decimal pay, decimal reserve)
    {
        var river = ElectricCar.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve).WithEarlierProfits(10_000m);
        var view = Compare(river, river.WithDirectorPay(pay / 2m));

        foreach (var column in new[] { view.Plan!, view.Against! })
        {
            var year = column.Run.Year!;
            var buckets = column.Buckets!;
            var figure = (Func<string, decimal>)(key => AmountOf(view, column, key));

            figure("corporate_tax").ShouldBe(year.CorporateTax);
            figure("social_contributions").ShouldBe(year.SocialContributions);
            figure("personal_tax").ShouldBe(year.PersonalTax);
            (figure("corporate_tax") + figure("social_contributions") + figure("personal_tax")).ShouldBe(figure("taxes"));
            figure("taxes").ShouldBe(buckets.Buckets.Single(bucket => bucket.Key == "taxes_on_pay").Amount + buckets.Buckets.Single(bucket => bucket.Key == "taxes_on_profit").Amount);
            figure("expenses").ShouldBe(year.CompanyContribution + year.Car.Costs);
            (figure("pocket") + figure("taxes") + figure("kept") + figure("expenses")).ShouldBe(buckets.Start);
        }
    }

    [Fact]
    public void Rows_samePlanTwice_isSameEverywhereWithoutANegativeZero()
    {
        var view = Compare(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(50_000m).WithReserve(0m));

        var differences = view.Groups.SelectMany(group => group.Rows).Select(row => row.Difference).ToList();

        differences.ShouldAllBe(difference => difference.Kind == DifferenceKind.Same && difference.Amount == 0m && !decimal.IsNegative(difference.Amount));
    }

    // The fund's fee is a social contribution of the layer's: a plan that gives one, 3.05% on the example's
    // 50,000, has the fee among its social contributions, 251.68 on 8,251.60 (GrossToNetFundFeeTests).
    [Fact]
    public void Rows_fundFeeInOnePlan_isAmongItsSocialContributions()
    {
        var view = Compare(RiverInputs.Example, RiverInputs.Example.WithFundFeeRate(0.0305m));

        (AmountOf(view, view.Plan!, "social_contributions"), AmountOf(view, view.Against!, "social_contributions")).ShouldBe((8_293.36m, 8_251.60m + 251.68m));
    }

    // The same cents are the same, and a row that does not differ is not one that differs.
    [Fact]
    public void Rows_equalFigures_doNotDiffer()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(50_000m).WithReserve(0m)), "pocket");

        (row.Difference, row.Differs).ShouldBe((CompareDifference.Same, false));
    }

    [Fact]
    public void Rows_differentFigures_differ()
    {
        Row(Compare(RiverInputs.Example, BestPay), "pocket").Differs.ShouldBeTrue();
    }

    // A car on one side: the result is the pocket, its share, what the company keeps and its expenses, as
    // for any plan. The river starts from 100,409.30, and the car's 8,000 of costs sit in the expenses with
    // INASTI's 409.30 contribution, 8,409.30, costs of the company like its running costs, which leave
    // 92,000: 55,813.08 of them reach the pocket, 6,162.66 less than without the car, and the company keeps
    // nothing.
    [Fact]
    public void Rows_carOnTheSecondSide_sayItsPocketItsShareWhatTheCompanyKeepsAndItsExpenses()
    {
        var result = Result(Compare(RiverInputs.Example, ElectricCar));

        result.Select(row => (row.Key, row.Against)).ShouldBe(CarResult);
    }

    [Fact]
    public void Rows_carOnTheFirstSide_sayItsPocketItsShareWhatTheCompanyKeepsAndItsExpenses()
    {
        var result = Result(Compare(ElectricCar, RiverInputs.Example));

        result.Select(row => (row.Key, row.Plan)).ShouldBe(CarResult);
    }

    private static readonly (string Key, CompareFigure Figure)[] CarResult =
    [
        ("pocket", CompareFigure.Money(55_813.08m)),
        ("reach", CompareFigure.Share(55_813.08m / 92_000m)),
        ("kept", CompareFigure.Money(0m)),
        ("expenses", CompareFigure.Money(8_409.30m)),
    ];

    private static IReadOnlyList<CompareRow> Result(CompareView view) => view.Groups.Single(group => group.Key == "result").Rows;

    // A river without a year of the company has no corporate tax, no kept profits: the rows of its pay.
    [Fact]
    public void Rows_bothStartAtThePay_haveOnlyTheRowsOfAPay()
    {
        var plan = RiverInputs.Example.SkipCompany();
        var view = Compare(plan, plan.WithDirectorPay(40_000m));

        var rows = Rows(view).ToDictionary(row => row.Key);

        view.ShouldSatisfyAllConditions(
            () => rows.Keys.Order().ShouldBe(["director_pay", "personal_tax", "pocket", "reach", "social_contributions", "taxes"]),
            () => (rows["pocket"].Plan.Amount, rows["pocket"].Against.Amount, rows["pocket"].Difference.Kind)
                .ShouldBe((plan.Run().Pay!.NetInPocket, plan.WithDirectorPay(40_000m).Run().Pay!.NetInPocket, DifferenceKind.Less)));
    }

    [Fact]
    public void Rows_payOnlyTaxes_areTheLedgerOfThePay()
    {
        var plan = RiverInputs.Example.SkipCompany();
        var view = Compare(plan, plan.WithDirectorPay(40_000m));
        var pay = plan.Run().Pay!;

        AmountOf(view, view.Plan!, "social_contributions").ShouldBe(pay.Steps.Single(step => step.RuleKey == "social_contributions").Amount);
    }

    // No profit, no share of it: neither plan has a figure, so the row is not drawn.
    [Fact]
    public void Rows_reachWithoutProfit_isLeftOut()
    {
        var empty = RiverInputs.Example.WithRevenue(20_000m).WithRunningCosts(20_000m);
        var view = Compare(empty, empty.WithReserve(1m));

        Rows(view).Select(row => row.Key).ShouldNotContain("reach");
    }

    // The levers the company pays besides the pay come off its profit before the split: a plan without one
    // has none of it, which the engine counts as nothing.
    [Fact]
    public void Rows_voucherOnOneSide_isNotInThePlanOfTheOtherAndCountsAsNothing()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)), "vouchers");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Absent, CompareFigure.Money(250m), new CompareDifference(DifferenceKind.More, 250m)));
    }

    [Fact]
    public void Rows_allowanceOnTheFirstSide_isLessAgainst()
    {
        var row = Row(Compare(RiverInputs.Example.AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m), RiverInputs.Example), "allowances");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(240m), CompareFigure.Absent, new CompareDifference(DifferenceKind.Less, 240m)));
    }

    [Fact]
    public void Rows_rent_isWhatTheCompanyPays()
    {
        var home = RiverInputs.Example.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        var row = Row(Compare(RiverInputs.Example, home), "rent");

        (row.Plan, row.Against.Amount, row.Difference.Kind).ShouldBe((CompareFigure.Absent, home.Run().Year!.Rent.Rent, DifferenceKind.More));
    }

    [Fact]
    public void Rows_carCosts_areWhatTheCompanySpendsOnTheCar()
    {
        var row = Row(Compare(RiverInputs.Example, ElectricCar), "car_costs");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Absent, CompareFigure.Money(8_000m), new CompareDifference(DifferenceKind.More, 8_000m)));
    }

    // In the plan with nothing granted is not out of it: €0.00 against « Not in this plan », the same.
    [Fact]
    public void Rows_leversInThePlanWithNothing_areZeroAndTheSame()
    {
        var everyLever = RiverInputs.Example with { Composition = new PlanComposition(true, true, true, false, false, false) };

        var rows = Rows(Compare(RiverInputs.Example, everyLever)).Where(row => row.Key is "vouchers" or "allowances" or "rent").ToList();

        rows.Select(row => (row.Key, row.Plan.Kind, row.Against, row.Difference)).ShouldBe(
        [
            ("vouchers", FigureKind.Absent, CompareFigure.Money(0m), CompareDifference.Same),
            ("allowances", FigureKind.Absent, CompareFigure.Money(0m), CompareDifference.Same),
            ("rent", FigureKind.Absent, CompareFigure.Money(0m), CompareDifference.Same),
        ]);
    }

    // A lever neither plan has is no row, and a group without a row is no group: before the split, two
    // plans without a lever have only what the company owes INASTI whatever it pays its director.
    [Fact]
    public void Groups_noLeverInEitherPlan_haveOnlyTheContributionBeforeTheSplit()
    {
        var view = Compare(RiverInputs.Example, BestPay);

        view.ShouldSatisfyAllConditions(
            () => view.Groups.Select(group => group.Key).ShouldBe(["result", "taxes", "before_split", "split"]),
            () => view.Groups.Single(group => group.Key == "before_split").Rows.Select(row => row.Key).ShouldBe(["company_contribution"]));
    }

    // INASTI's company contribution comes off what the company earns before the split: the lower band's
    // 409.30 against the upper band's 1,022.38, 613.08 more.
    [Fact]
    public void Rows_companyContribution_isWhatEachPlansBandOwes()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.WithCompanyContributionBand(Netto.Core.SocialContributions.CompanyContributionBand.Upper)),
            "company_contribution");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(409.30m), CompareFigure.Money(1_022.38m), new CompareDifference(DifferenceKind.More, 613.08m)));
    }

    // A company the law exempts owes nothing: €0.00 against the lower band's 409.30.
    [Fact]
    public void Rows_companyContribution_exemptIsNothing()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.WithCompanyContributionBand(Netto.Core.SocialContributions.CompanyContributionBand.Exempt)),
            "company_contribution");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(409.30m), CompareFigure.Money(0m), new CompareDifference(DifferenceKind.Less, 409.30m)));
    }

    // It is a cost of the company, in no layer's taxes: the taxes of the two bands differ only by the
    // corporate tax the larger contribution saves.
    [Fact]
    public void Rows_companyContribution_isInNoLayersTaxes()
    {
        var upper = RiverInputs.Example.WithCompanyContributionBand(Netto.Core.SocialContributions.CompanyContributionBand.Upper);
        var view = Compare(RiverInputs.Example, upper);

        AmountOf(view, view.Against!, "taxes").ShouldBe(upper.Run().Year!.Steps.Where(step => step.Track == Track.PaidOut).Sum(step => step.Amount));
    }

    [Fact]
    public void Groups_aLeverInOnePlan_hasTheBeforeTheSplitGroupBetweenTheTaxesAndTheSplit()
    {
        var view = Compare(RiverInputs.Example, ElectricCar);

        view.Groups.Select(group => group.Key).ShouldBe(["result", "taxes", "before_split", "split"]);
    }

    [Fact]
    public void Rows_directorPay_isTheGrossPayTheSplitGave()
    {
        var row = Row(Compare(RiverInputs.Example, BestPay), "director_pay");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(50_000m), CompareFigure.Money(20_799m), new CompareDifference(DifferenceKind.Less, 29_201m)));
    }

    [Fact]
    public void Rows_directorPayOfAPayOnlyRiver_isThePaySet()
    {
        var plan = RiverInputs.Example.SkipCompany();

        var row = Row(Compare(plan, plan.WithDirectorPay(40_000m)), "director_pay");

        (row.Plan, row.Against).ShouldBe((CompareFigure.Money(50_000m), CompareFigure.Money(40_000m)));
    }

    [Fact]
    public void Rows_dividend_isTheDividendPaidOut()
    {
        var row = Row(Compare(RiverInputs.Example, BestPay), "dividend");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(39_325m), CompareFigure.Money(58_064.23m), new CompareDifference(DifferenceKind.More, 18_739.23m)));
    }

    [Fact]
    public void Rows_royalty_isNotInThePlanOfThePlanWithoutOne()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m)), "royalty");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Absent, CompareFigure.Money(10_000m), new CompareDifference(DifferenceKind.More, 10_000m)));
    }

    // The example pays enough to keep the reduced corporate rate; at the best pay it is given up.
    [Fact]
    public void Rows_reducedRate_isKeptOrGivenUpAndChangedBetweenThem()
    {
        var row = Row(Compare(RiverInputs.Example, BestPay), "reduced_rate");

        (row.Plan, row.Against, row.Difference, row.Differs).ShouldBe(
            (CompareFigure.Rate(RateStance.Kept), CompareFigure.Rate(RateStance.GivenUp), CompareDifference.Changed, true));
    }

    [Fact]
    public void Rows_reducedRateOfACompanyThatIsNotSmall_isNotOpen()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.WithSmallCompany(false)), "reduced_rate");

        (row.Plan, row.Against).ShouldBe((CompareFigure.Rate(RateStance.Kept), CompareFigure.Rate(RateStance.NotOpen)));
    }

    [Fact]
    public void Rows_reducedRateTheSameOnBothSides_isTheSame()
    {
        var row = Row(Compare(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(55_000m)), "reduced_rate");

        (row.Difference, row.Differs).ShouldBe((CompareDifference.Same, false));
    }

    [Theory]
    [InlineData(ReducedRateCondition.NotSmallCompany, "NotOpen")]
    [InlineData(ReducedRateCondition.FirstFourTaxPeriods, "Kept")]
    [InlineData(ReducedRateCondition.MinimumPayMet, "Kept")]
    [InlineData(ReducedRateCondition.TaxableIncomeNotAbovePay, "Kept")]
    [InlineData(ReducedRateCondition.MinimumPayNotMet, "GivenUp")]
    [InlineData(ReducedRateCondition.ExcessBenefits, "GivenUp")]
    public void StanceOf_condition_isWhatTheRateDoes(ReducedRateCondition condition, string expected)
    {
        CompareRows.StanceOf(condition).ToString().ShouldBe(expected);
    }

    // A river that starts at the pay has no dividend, no company, no rate: its rows are not drawn.
    [Fact]
    public void Rows_payOnly_haveNoDividendNorRate()
    {
        var plan = RiverInputs.Example.SkipCompany();

        Rows(Compare(plan, plan.WithDirectorPay(40_000m))).Select(row => row.Key).ShouldBe(
            ["pocket", "reach", "taxes", "social_contributions", "personal_tax", "director_pay"]);
    }

    private static decimal AmountOf(CompareView view, ComparedPlan column, string key)
    {
        var row = Rows(view).SingleOrDefault(candidate => candidate.Key == key);
        var figure = column.Id == view.Plan!.Id ? row?.Plan : row?.Against;
        return figure is { Present: true } ? figure.Amount : 0m;
    }

    private static CompareRow Row(CompareView view, string key) => Rows(view).Single(row => row.Key == key);

    private static IEnumerable<CompareRow> Rows(CompareView view) => view.Groups.SelectMany(group => group.Rows);
}
