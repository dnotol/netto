using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// On the river's example (RiverInputsTests): 100,000 of profit before pay end up as 61,975.74 in the
// pocket, 20,420.70 of taxes on pay (contributions 8,293.36, income tax 11,333.96, municipal
// surcharge 793.38, and no advance-payment surcharge, the pay being withheld on as the key formula
// says), 17,603.56 of taxes on profit (corporate tax 10,000 and its advance-payment surcharge 675,
// nothing being prepaid, withholding 7,078.50 on the 39,325 left, less the refund of 149.94) and
// nothing kept.
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
            ("pocket", 61_975.74m),
            ("taxes_on_pay", 20_420.70m),
            ("taxes_on_profit", 17_603.56m),
            ("kept_in_company", 0m),
        });
    }

    // At the best pay the tax credit for low earned income comes back out of the taxes on pay:
    // contributions 3,561.68, income tax 1,292.58 and surcharge 90.48, less 910. Taxes on profit are
    // corporate tax 19,800.25, its advance-payment surcharge 1,336.52 and withholding 10,451.56 on the
    // 58,064.23 left, less the refund of 149.94.
    [Fact]
    public void From_bestPay_creditLowersTheTaxesOnPay()
    {
        var view = BucketsView.From(RiverInputs.Example.WithDirectorPay(20_799m).Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 64_526.87m),
            ("taxes_on_pay", 4_034.74m),
            ("taxes_on_profit", 31_438.39m),
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

        view.Buckets.Select(bucket => bucket.Share).ShouldBe(new decimal?[] { 0.6197574m, 0.204207m, 0.1760356m, 0m });
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

    // The company's 9,942.52 of contributions are pay, so the river starts from its cost of the
    // 50,000, 59,942.52: 33,999.21 to the pocket, the formula's withholding leaving no surcharge, and
    // 25,943.31 of taxes on pay (contributions 9,942.52, income tax 14,954.01, surcharge 1,046.78).
    [Fact]
    public void From_payAloneCompanyPays_startsAtTheCompanysCostAndTheBucketsAddUpToIt()
    {
        var view = BucketsView.From(RiverInputs.Example.SkipCompany().WithContributionsPaidBy(ContributionsPaidBy.Company).Run()).ShouldNotBeNull();

        (view.Start, view.StartsAtPay, view.CompanyPaysContributions, view.Buckets.Sum(bucket => bucket.Amount))
            .ShouldBe((59_942.52m, true, true, 59_942.52m));
    }

    [Fact]
    public void From_payAloneCompanyPays_givesEachBucketItsAmount()
    {
        var view = BucketsView.From(RiverInputs.Example.SkipCompany().WithContributionsPaidBy(ContributionsPaidBy.Company).Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 33_999.21m),
            ("taxes_on_pay", 25_943.31m),
            ("taxes_on_profit", 0m),
            ("kept_in_company", 0m),
        });
    }

    [Fact]
    public void From_payAlone_saysTheDirectorPaysTheContributions()
    {
        BucketsView.From(RiverInputs.Example.SkipCompany().Run()).ShouldNotBeNull().CompanyPaysContributions.ShouldBeFalse();
    }

    // With the company there, its cost of the pay comes off its profit: the river still starts from
    // the profit before pay.
    [Fact]
    public void From_companyPays_bucketsAddUpToTheProfitBeforePay()
    {
        var view = BucketsView.From(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((100_000m, 100_000m));
    }

    // With 220 days of meal vouchers (RiverInputsTests) the river still starts from the 100,000 the
    // company earns before paying its director anything, of which the vouchers take 1,960.20.
    [Fact]
    public void From_vouchers_startsBeforeWhatTheCompanyPaysTheDirector()
    {
        BucketsView.From(RiverInputs.Example.WithMealVoucherDays(220).Run()).ShouldNotBeNull().Start.ShouldBe(100_000m);
    }

    // 62,482.64 in the pocket with the vouchers, 20,420.70 of taxes on pay as without them, and
    // 17,096.66 of taxes on profit: corporate tax 9,824.00 and its surcharge 663.12, withholding
    // 6,759.48 on the 37,552.68 left, less the refund of 149.94.
    [Fact]
    public void From_vouchers_putsThemInThePocketAndNoTaxOnPay()
    {
        var view = BucketsView.From(RiverInputs.Example.WithMealVoucherDays(220).Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 62_482.64m),
            ("taxes_on_pay", 20_420.70m),
            ("taxes_on_profit", 17_096.66m),
            ("kept_in_company", 0m),
        });
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithVouchers_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = RiverInputs.Example.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - 20_000m, revenue - 20_000m));
    }

    // The allowances (RiverInputsTests) leave the start where it was, put their 2,418.44 in the pocket,
    // 62,834.46, take nothing from the pay, and lower the taxes on profit to 16,744.84: corporate tax
    // 9,516.31 and its surcharge 642.35, withholding 6,736.12 on the 37,422.90 left, less the refund
    // of 149.94.
    [Fact]
    public void From_allowances_putsThemInThePocketAndNoTaxOnPay()
    {
        var inputs = RiverInputs.Example.WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        view.ShouldSatisfyAllConditions(
            () => view.Start.ShouldBe(100_000m),
            () => view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
            {
                ("pocket", 62_834.46m),
                ("taxes_on_pay", 20_420.70m),
                ("taxes_on_profit", 16_744.84m),
                ("kept_in_company", 0m),
            }));
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithVouchersAndAllowances_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = RiverInputs.Example.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
            .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - 20_000m, revenue - 20_000m));
    }

    [Fact]
    public void From_refusedInput_drawsNothing()
    {
        BucketsView.From(RiverInputs.Example.WithReserve(-1m).Run()).ShouldBeNull();
    }
}
