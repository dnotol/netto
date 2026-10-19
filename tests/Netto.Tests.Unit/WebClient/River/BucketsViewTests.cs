using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// On the river's example (RiverInputsTests): 100,000 of profit before pay end up as 62,529.24 in the
// pocket, 20,420.70 of taxes on pay (contributions 8,293.36, income tax 11,333.96, surcharge
// 793.38), 17,050.06 of taxes on profit (corporate tax 10,000, withholding 7,200, less the refund of
// 149.94) and nothing kept.
public class BucketsViewTests
{
    public static TheoryData<decimal, decimal, decimal> Years => new()
    {
        { 120_000m, 50_000m, 0m },
        { 120_000m, 50_000m, 15_000m },
        { 220_000m, 21_000m, 30_000m },
        { 60_000m, 50_000m, 0m },
        { 40_000m, 30_000m, 5_000m },
    };

    [Fact]
    public void From_example_givesEachBucketItsAmount()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 62_529.24m),
            ("taxes_on_pay", 20_420.70m),
            ("taxes_on_profit", 17_050.06m),
            ("kept_in_company", 0m),
        });
    }

    // At the best pay the tax credit for low earned income comes back out of the taxes on pay:
    // contributions 3,561.68, income tax 1,292.58 and surcharge 90.48, less 910. Taxes on profit are
    // corporate tax 19,800.25 and withholding 10,692.14, less the refund of 149.94.
    [Fact]
    public void From_bestPay_creditLowersTheTaxesOnPay()
    {
        var view = BucketsView.From(RiverInputs.Example.WithDirectorPay(20_799m).Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 65_622.81m),
            ("taxes_on_pay", 4_034.74m),
            ("taxes_on_profit", 30_342.45m),
            ("kept_in_company", 0m),
        });
    }

    [Fact]
    public void From_example_startsAtTheProfitBeforePay()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        (view.Start, view.StartsAtPay).ShouldBe((100_000m, false));
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYear_bucketsAddUpToTheProfitBeforePay(decimal revenue, decimal pay, decimal reserve)
    {
        var view = BucketsView.From(RiverInputs.Example.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve).Run()).ShouldNotBeNull();

        view.Buckets.Sum(bucket => bucket.Amount).ShouldBe(view.Start);
    }

    // The river starts from the profit before pay and the profits kept from earlier years together.
    [Fact]
    public void From_earlierProfits_startsAtTheProfitBeforePayAndThem()
    {
        var view = BucketsView.From(RiverInputs.Example.WithEarlierProfits(60_000m).WithReserve(30_000m).Run()).ShouldNotBeNull();

        (view.Start, view.EarlierProfits, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((160_000m, 60_000m, 160_000m));
    }

    [Fact]
    public void From_example_hasNoEarlierProfits()
    {
        BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull().EarlierProfits.ShouldBe(0m);
    }

    [Fact]
    public void From_reserve_keepsItInTheCompanyBucket()
    {
        var view = BucketsView.From(RiverInputs.Example.WithReserve(15_000m).Run()).ShouldNotBeNull();

        view.Buckets.Single(bucket => bucket.Key == "kept_in_company").Amount.ShouldBe(15_000m);
    }

    [Fact]
    public void From_example_givesEachBucketItsShareOfTheStart()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => bucket.Share).ShouldBe(new decimal?[] { 0.6252924m, 0.204207m, 0.1705006m, 0m });
    }

    // A loss stays in the company as a negative amount, which a bar of shares cannot draw.
    [Fact]
    public void From_lossYear_drawsNoShares()
    {
        var view = BucketsView.From(RiverInputs.Example.WithRevenue(10_000m).Run()).ShouldNotBeNull();

        view.Buckets.ShouldAllBe(bucket => bucket.Share == null);
    }

    [Fact]
    public void From_payAlone_startsAtTheGrossPay()
    {
        var view = BucketsView.From(RiverInputs.Example.SkipCompany().Run()).ShouldNotBeNull();

        (view.Start, view.StartsAtPay).ShouldBe((50_000m, true));
    }

    [Fact]
    public void From_payAlone_leavesTheCompanyBucketsEmpty()
    {
        var view = BucketsView.From(RiverInputs.Example.SkipCompany().Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 29_579.30m),
            ("taxes_on_pay", 20_420.70m),
            ("taxes_on_profit", 0m),
            ("kept_in_company", 0m),
        });
    }

    [Fact]
    public void From_refusedInput_drawsNothing()
    {
        BucketsView.From(RiverInputs.Example.WithReserve(-1m).Run()).ShouldBeNull();
    }
}
