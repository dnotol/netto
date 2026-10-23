using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

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

        var rows = new[] { "taxes", "corporate_tax", "social_contributions", "personal_tax" }.Select(key => Row(view, key)).ToList();

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
    // together; with the pocket, the muddy pocket and what the company keeps they are what the river
    // started from.
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
            (figure("pocket") + figure("muddy") + figure("taxes") + figure("kept")).ShouldBe(buckets.Start);
        }
    }

    [Fact]
    public void Rows_samePlanTwice_isSameEverywhereWithoutANegativeZero()
    {
        var view = Compare(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(50_000m).WithReserve(0m));

        var differences = view.Groups.SelectMany(group => group.Rows).Select(row => row.Difference).ToList();

        differences.ShouldAllBe(difference => difference.Kind == DifferenceKind.Same && difference.Amount == 0m && !decimal.IsNegative(difference.Amount));
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

    // A car on one side: the muddy pocket holds its costs there, nothing on the other, and the difference is
    // the one BaselineGain says beside the gain in cash.
    [Fact]
    public void Rows_carOnOneSide_hasTheMuddyPocketRow()
    {
        var row = Row(Compare(RiverInputs.Example, ElectricCar), "muddy");

        (row.Plan, row.Against, row.Difference).ShouldBe((CompareFigure.Money(0m), CompareFigure.Money(8_000m), new CompareDifference(DifferenceKind.More, 8_000m)));
    }

    [Fact]
    public void Rows_carOnTheFirstSide_isLessInTheMuddyPocketAgainst()
    {
        var row = Row(Compare(ElectricCar, RiverInputs.Example), "muddy");

        row.Difference.ShouldBe(new CompareDifference(DifferenceKind.Less, 8_000m));
    }

    [Fact]
    public void Rows_noCarOnEitherSide_hasNoMuddyPocketRow()
    {
        Rows(Compare(RiverInputs.Example, BestPay)).Select(row => row.Key).ShouldNotContain("muddy");
    }

    // A river without a year of the company has no corporate tax, no kept profits: the rows of its pay.
    [Fact]
    public void Rows_bothStartAtThePay_haveOnlyTheRowsOfAPay()
    {
        var plan = RiverInputs.Example.SkipCompany();
        var view = Compare(plan, plan.WithDirectorPay(40_000m));

        var rows = Rows(view).ToDictionary(row => row.Key);

        view.ShouldSatisfyAllConditions(
            () => rows.Keys.Order().ShouldBe(["personal_tax", "pocket", "reach", "social_contributions", "taxes"]),
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



    private static decimal AmountOf(CompareView view, ComparedPlan column, string key)
    {
        var row = Rows(view).SingleOrDefault(candidate => candidate.Key == key);
        var figure = column.Id == view.Plan!.Id ? row?.Plan : row?.Against;
        return figure is { Present: true } ? figure.Amount : 0m;
    }

    private static CompareRow Row(CompareView view, string key) => Rows(view).Single(row => row.Key == key);

    private static IEnumerable<CompareRow> Rows(CompareView view) => view.Groups.SelectMany(group => group.Rows);

    private static CompareView Compare(RiverInputs plan, RiverInputs against)
    {
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(plan, number => $"Plan {number}");
        plans.Save(against, number => $"Plan {number}");
        var view = CompareView.Of(plans);
        view.State.ShouldBe(CompareState.Compared);
        return view;
    }

    private sealed class MemoryStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }
}
