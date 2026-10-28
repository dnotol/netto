using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class BucketBarTextTests
{
    // The name IStringLocalizer<BucketBar> reads River/BucketBar.resx under.
    private static readonly ResxText Resources = ResxText.For<BucketBar>();

    // Each bucket, with and without a rent among the taxes on pay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BucketBarResx_everyBucket_hasAText(bool taxesOnPayIncludeRent)
    {
        BucketsView.Keys.Select(key => BucketBarText.Bucket(key, taxesOnPayIncludeRent))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Bucket_taxesOnPayWithRent_hasItsOwnText()
    {
        (BucketBarText.Bucket("taxes_on_pay", true), BucketBarText.Bucket("pocket", true)).ShouldBe(("Bucket_taxes_on_pay_and_rent", "Bucket_pocket"));
    }

    // The bucket of what leaves the pay names the social contributions with the tax, so the bar reads
    // without its bubbles; with a rent it names the rent too.
    [Theory]
    [InlineData(false, "Contributions and tax on pay")]
    [InlineData(true, "Contributions and tax on pay and rent")]
    public void BucketBarResx_taxesOnPay_namesTheContributionsWithTheTax(bool taxesOnPayIncludeRent, string name)
    {
        Resources.Get(BucketBarText.Bucket("taxes_on_pay", taxesOnPayIncludeRent)).ShouldBe(name);
    }

    // What each bucket holds, with a copyright royalty's tax apart among the taxes on pay, with or
    // without a rent.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BucketBarResx_everyBucketWithARoyalty_saysWhatItHolds(bool taxesOnPayIncludeRent)
    {
        BucketsView.Keys.Select(key => BucketBarText.Holds(key, taxesOnPayIncludeRent, true))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The royalty's tax is said in what the bucket of what leaves the pay holds, never in its name.
    [Theory]
    [InlineData(false, "Holds_taxes_on_pay_and_royalty")]
    [InlineData(true, "Holds_taxes_on_pay_rent_and_royalty")]
    public void Holds_taxesOnPayWithARoyalty_saysTheRoyaltyToo(bool taxesOnPayIncludeRent, string holds)
    {
        (BucketBarText.Holds("taxes_on_pay", taxesOnPayIncludeRent, true), BucketBarText.Holds("pocket", true, true)).ShouldBe((holds, "Holds_pocket"));
    }

    // What each bucket holds, in the bubble its name opens, with and without a rent among the taxes on pay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BucketBarResx_everyBucket_saysWhatItHolds(bool taxesOnPayIncludeRent)
    {
        BucketsView.Keys.Select(key => BucketBarText.Holds(key, taxesOnPayIncludeRent))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Holds_taxesOnPayWithRent_saysTheRentToo()
    {
        (BucketBarText.Holds("taxes_on_pay", true), BucketBarText.Holds("pocket", true)).ShouldBe(("Holds_taxes_on_pay_and_rent", "Holds_pocket"));
    }

    // Once the river settles, the live region says what reaches the pocket, and that alone: the
    // example's 61,975.74 (BucketsViewTests).
    [Fact]
    public void PocketAnnounced_example_saysThePocketAlone()
    {
        var pocket = BucketBarAnnouncement.Pocket(BucketsView.From(RiverInputs.Example.Run()));

        var text = Resources.Format(BucketBarText.PocketAnnounced, pocket);

        text.ShouldBe("In your pocket: €61,975.74.");
    }

    // Under the bar, what reaches the pocket out of the 100,000 the example's expenses leave, nothing being kept
    // (BucketsViewTests), the share being the term its bubble explains; the rest went to the social
    // contributions and the taxes, as the buckets name them.
    [Fact]
    public void Reach_example_saysWhatReachesThePocketAndWhatGoesToContributionsAndTaxes()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        var text = Resources.Format(BucketBarText.Reach(view), "62%", "38%");

        text.ShouldBe("[62% reaches you], 38% goes to contributions and taxes.");
    }

    // What the company keeps is still the director's: the sentence says the share is of the rest.
    [Fact]
    public void Reach_reserve_saysItIsOfWhatTheCompanyDoesNotKeep()
    {
        var view = BucketsView.From(RiverInputs.Example.WithReserve(15_000m).Run()).ShouldNotBeNull();

        TermText.Split(Resources.Get(BucketBarText.Reach(view)) ?? "")
            .ShouldBe(("Of what the company does not keep, ", "{0} reaches you", ", {1} goes to contributions and taxes."));
    }

    [Fact]
    public void BucketBarResx_reach_saysWhatItMeansAndWhereTheRestOfTheRiverIs()
    {
        new[] { BucketBarText.ReachMeaning, BucketBarText.EnterCompany }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData(GainKind.More)]
    [InlineData(GainKind.Less)]
    [InlineData(GainKind.Same)]
    [InlineData(GainKind.NotComparable)]
    public void BucketBarResx_everyGain_hasAText(GainKind kind)
    {
        Resources.Get(BucketBarText.Gain(kind)).ShouldNotBeNull();
    }

    // The best pay against the example marked as the setup (BaselineGainTests).
    [Fact]
    public void Gain_more_saysHowMuchMoreThanTheSetup()
    {
        var text = Resources.Format(BucketBarText.Gain(GainKind.More), Formats.Euros(2_551.13m));

        text.ShouldBe("€2,551.13 more than Today, your current setup.");
    }

    [Fact]
    public void Gain_less_saysHowMuchLessThanTheSetup()
    {
        var text = Resources.Format(BucketBarText.Gain(GainKind.Less), Formats.Euros(2_551.13m));

        text.ShouldBe("€2,551.13 less than Today, your current setup.");
    }

    [Fact]
    public void BucketBarResx_unsaved_saysTheRiverHasChangesNoPlanHolds()
    {
        Resources.Get(BucketBarText.Unsaved).ShouldBe("Unsaved changes.");
    }

    // With a car, the reach says the pocket and the taxes alone, of what the company does not keep once
    // it keeps some: the car's costs are left out of it, in the expenses.
    [Theory]
    [InlineData(0, "Reach")]
    [InlineData(10_000, "Reach_Kept")]
    public void Reach_car_saysThePocketAndTheTaxesAlone(int reserve, string key)
    {
        var view = BucketsView.From(WithCar.WithReserve(reserve).Run()).ShouldNotBeNull();

        BucketBarText.Reach(view).ShouldBe(key);
    }

    // The reach's bubble says what it is out of: the running costs and the expenses, INASTI's company
    // contribution and a car's costs, are left out.
    [Fact]
    public void BucketBarResx_reachMeaning_saysTheExpensesAreLeftOutLikeTheRunningCosts()
    {
        Resources.Get(BucketBarText.ReachMeaning).ShouldBe(
            "Your pocket, out of what the river starts from less the company's expenses and what it keeps at year end, which is still yours. "
            + "Running costs, INASTI's yearly company contribution and a company car's costs are left out: they are costs of the company, whatever it pays you.");
    }

    // The bar's last bucket, the company's expenses, names in its bubble what it holds: INASTI's company
    // contribution and a car's costs.
    [Fact]
    public void BucketBarResx_expenses_namesTheContributionAndACarsCosts()
    {
        (Resources.Get(BucketBarText.Bucket("expenses", false)), Resources.Get(BucketBarText.Holds("expenses", false))).ShouldBe((
            "Expenses",
            "What the company spends whatever it pays you: INASTI's yearly company contribution and, with a company car, what the car costs it over the year."));
    }

    // What the river starts from, each way it can: the profit before INASTI's company contribution and
    // what the company pays the director, before a car's costs too once one counts, with the profits kept
    // from earlier years; or, with the company skipped, the pay and the contributions the company pays on
    // it.
    public static TheoryData<string, RiverInputs> Starts => new()
    {
        { "FromProfit", RiverInputs.Example },
        { "FromProfitAndEarlier", RiverInputs.Example.WithEarlierProfits(10_000m) },
        { "FromProfitWithCar", WithCar },
        { "FromProfitWithCarAndEarlier", WithCar.WithEarlierProfits(10_000m) },
        { "FromPay", RiverInputs.Example.SkipCompany() },
        { "FromPayCost", RiverInputs.Example.SkipCompany().WithContributionsPaidBy(ContributionsPaidBy.Company) },
    };

    [Theory]
    [MemberData(nameof(Starts))]
    public void From_eachStart_hasItsSentence(string key, RiverInputs inputs)
    {
        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (BucketBarText.From(view), Resources.Get(key) is not null).ShouldBe((key, true));
    }

    // The bar says the river starts before the expenses: the example's 100,409.30 (BucketsViewTests),
    // before its car's costs too once one counts, and beside them what the company kept from earlier
    // years. The expenses' bucket says how much they are.
    [Theory]
    [InlineData(false, 0, "Out of €100,409.30 of profit before INASTI's company contribution and what the company pays you.")]
    [InlineData(false, 10_000, "Out of €100,409.30 of profit before INASTI's company contribution and what the company pays you, and €10,000.00 kept from earlier years.")]
    [InlineData(true, 0, "Out of €100,409.30 of profit before INASTI's company contribution, its car's costs and what the company pays you.")]
    [InlineData(true, 10_000, "Out of €100,409.30 of profit before INASTI's company contribution, its car's costs and what the company pays you, and €10,000.00 kept from earlier years.")]
    public void From_profit_saysTheRiverStartsBeforeTheExpenses(bool car, int earlierProfits, string expected)
    {
        var view = BucketsView.From((car ? WithCar : RiverInputs.Example).WithEarlierProfits(earlierProfits).Run()).ShouldNotBeNull();

        var text = Resources.Format(BucketBarText.From(view), Formats.Euros(view.Start - view.EarlierProfits), Formats.Euros(view.EarlierProfits));

        text.ShouldBe(expected);
    }

    // While the engine refuses an amount there is no figure to announce.
    [Fact]
    public void Pocket_noFigure_isNothing()
    {
        var pocket = BucketBarAnnouncement.Pocket(null);

        pocket.ShouldBeNull();
    }

    // The bar, and its live region once the river settles, say instead that there is no figure until
    // the amount the card marks is corrected.
    [Fact]
    public void BucketBarResx_noFigure_saysWhy()
    {
        var text = Resources.Get(BucketBarText.Refused);

        text.ShouldBe("No figures until the amount marked below is corrected.");
    }

    // YearSimulationCarTests' electric car on the example: 8,000 of yearly costs.
    private static readonly RiverInputs WithCar =
        RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
}
