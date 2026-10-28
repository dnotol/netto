using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// On the river's example (RiverInputsTests): 100,409.30 of profit before INASTI's company contribution
// and pay end up as 61,975.74 in the pocket, 20,420.70 of taxes on pay (contributions 8,293.36, income
// tax 11,333.96, municipal surcharge 793.38, and no advance-payment surcharge, the pay being withheld on
// as the key formula says), 17,603.56 of taxes on profit (corporate tax 10,000 and its advance-payment
// surcharge 675, nothing being prepaid, withholding 7,078.50 on the 39,325 left, less the refund of
// 149.94), nothing kept, and the contribution's 409.30 in the expenses. The example holds no lever in its
// plan; with every one in it and its amounts still nothing, its figures stay those.
public class BucketsViewTests
{
    private static readonly RiverInputs EveryLever = RiverInputs.Example with { Composition = new PlanComposition(true, true, true, false, false, false) };

    // The example's company: 120,000 of revenue and 19,590.70 of running costs, which every start is after.
    private static readonly CompanyFigures ExampleCompany = RiverInputs.Example.Company.ShouldNotBeNull();

    private static readonly decimal RunningCosts = ExampleCompany.RunningCosts.Value;

    // What the example's company earns before it pays INASTI or its director anything: 100,409.30.
    private static readonly decimal ExampleStart = ExampleCompany.Revenue.Value - RunningCosts;

    // The bar's five buckets in its order, the expenses last, whatever the plan holds.
    [Fact]
    public void Keys_areTheFiveBucketsTheExpensesLast()
    {
        BucketsView.Keys.ShouldBe(["pocket", "taxes_on_pay", "taxes_on_profit", "kept_in_company", "expenses"]);
    }

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
            ("expenses", 409.30m),
        });
    }

    // The fund's fee leaves the pay with the contributions, into « Contributions and tax on pay », and the
    // buckets still add up to what the river starts from.
    [Fact]
    public void From_fundFee_isInTheTaxesOnPay()
    {
        var run = RiverInputs.Example.WithFundFeeRate(0.0305m).Run();
        var view = BucketsView.From(run).ShouldNotBeNull();
        var pay = run.Pay.ShouldNotBeNull();

        view.ShouldSatisfyAllConditions(
            () => view.AmountOf("taxes_on_pay").ShouldBe(pay.Steps.Where(step => step.Track == Netto.Core.Simulation.Track.PaidOut).Sum(step => step.Amount)),
            () => pay.Steps.Single(step => step.RuleKey == "fund_fee").Amount.ShouldBe(251.68m),
            () => view.Buckets.Sum(bucket => bucket.Amount).ShouldBe(view.Start));
    }

    // What reaches the pocket out of what leaves the company, the start less its expenses and what it
    // keeps there; none where nothing leaves it.
    [Fact]
    public void ReachOf_whatLeavesTheCompany_isThePocketsShareOfItTheExpensesLeftOut()
    {
        (BucketsView.ReachOf(110_000m, 10_000m, 60_000m, 20_000m), BucketsView.ReachOf(110_000m, 10_000m, 0m, 100_000m))
            .ShouldBe(((decimal?)0.75m, (decimal?)null));
    }

    // The expenses are INASTI's company contribution and what a car costs the company: the example's
    // 409.30, and with YearSimulationCarTests' electric car its 8,000 besides.
    [Fact]
    public void ExpensesOf_year_isTheContributionAndTheCarsCosts()
    {
        (BucketsView.ExpensesOf(RiverInputs.Example.Run().Year.ShouldNotBeNull()), BucketsView.ExpensesOf(ElectricCar(RiverInputs.Example).Run().Year.ShouldNotBeNull()))
            .ShouldBe((409.30m, 8_409.30m));
    }

    // The example's steps paid out on the pay are its taxes on pay, 20,420.70, and the company's its tax
    // and surcharge, 10,675; the vouchers reach the pocket and pay nothing out, what the company cannot
    // deduct of them only joining its taxable income.
    [Fact]
    public void PaidOut_steps_sumsWhatTheEventsStepsPayOut()
    {
        var steps = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(220).Run().Year.ShouldNotBeNull().Steps;

        (BucketsView.PaidOut(steps, TaxableEvent.DirectorPay), BucketsView.PaidOut(steps, TaxableEvent.CompanyProfit), BucketsView.PaidOut(steps, TaxableEvent.Vouchers))
            .ShouldBe((20_420.70m, 9_824m + 663.12m, 0m));
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
            ("expenses", 409.30m),
        });
    }

    // The river starts before INASTI's company contribution, which the expenses hold.
    [Fact]
    public void From_example_startsAtTheProfitBeforeTheContributionAndPay()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        (view.Start, view.StartsAtPay).ShouldBe((ExampleStart, false));
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

        (view.Start, view.EarlierProfits, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((160_409.30m, 60_000m, 160_409.30m));
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

        view.Buckets.Select(bucket => bucket.Share).ShouldBe(new decimal?[]
        {
            61_975.74m / ExampleStart, 20_420.70m / ExampleStart, 17_603.56m / ExampleStart, 0m, 409.30m / ExampleStart,
        });
    }

    // A loss stays in the company as a negative amount, which a bar of shares cannot draw.
    [Fact]
    public void From_lossYear_drawsNoShares()
    {
        var view = BucketsView.From(RiverInputs.Example.WithRevenue(10_000m).Run()).ShouldNotBeNull();

        view.Buckets.ShouldAllBe(bucket => bucket.Share == null);
    }

    // What reaches the pocket out of the 100,000 the contribution leaves, nothing being kept.
    [Fact]
    public void From_example_reachIsThePocketOutOfTheStart()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        view.Reach.ShouldBe(0.6197574m);
    }

    // What the company keeps is still the director's: the reach is out of what leaves it.
    [Fact]
    public void From_reserve_reachLeavesOutWhatTheCompanyKeeps()
    {
        var view = BucketsView.From(RiverInputs.Example.WithReserve(15_000m).Run()).ShouldNotBeNull();

        view.Reach.ShouldBe(view.Buckets[0].Amount / 85_000m);
    }

    [Fact]
    public void From_lossYear_hasNoReach()
    {
        var view = BucketsView.From(RiverInputs.Example.WithRevenue(10_000m).Run()).ShouldNotBeNull();

        view.Reach.ShouldBeNull();
    }

    // With the company skipped the river starts at the pay: the reach is the net out of the gross.
    [Fact]
    public void From_payAlone_reachIsTheNetOutOfTheGross()
    {
        var view = BucketsView.From(RiverInputs.Example.SkipCompany().Run()).ShouldNotBeNull();

        view.Reach.ShouldBe(0.591586m);
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
            ("expenses", 0m),
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
            ("expenses", 0m),
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

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((ExampleStart, ExampleStart));
    }

    // With 220 days of meal vouchers (RiverInputsTests) the river still starts from the 100,409.30 the
    // company earns before paying INASTI or its director anything, of which the vouchers take 1,960.20.
    [Fact]
    public void From_vouchers_startsBeforeWhatTheCompanyPaysTheDirector()
    {
        BucketsView.From(EveryLever.WithMealVoucherDays(220).Run()).ShouldNotBeNull().Start.ShouldBe(ExampleStart);
    }

    // 62,482.64 in the pocket with the vouchers, 20,420.70 of taxes on pay as without them, and
    // 17,096.66 of taxes on profit: corporate tax 9,824.00 and its surcharge 663.12, withholding
    // 6,759.48 on the 37,552.68 left, less the refund of 149.94.
    [Fact]
    public void From_vouchers_putsThemInThePocketAndNoTaxOnPay()
    {
        var view = BucketsView.From(EveryLever.WithMealVoucherDays(220).Run()).ShouldNotBeNull();

        view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
        {
            ("pocket", 62_482.64m),
            ("taxes_on_pay", 20_420.70m),
            ("taxes_on_profit", 17_096.66m),
            ("kept_in_company", 0m),
            ("expenses", 409.30m),
        });
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithVouchers_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = EveryLever.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - RunningCosts, revenue - RunningCosts));
    }

    // The allowances (RiverInputsTests) leave the start where it was, put their 2,418.44 in the pocket,
    // 62,834.46, take nothing from the pay, and lower the taxes on profit to 16,744.84: corporate tax
    // 9,516.31 and its surcharge 642.35, withholding 6,736.12 on the 37,422.90 left, less the refund
    // of 149.94.
    [Fact]
    public void From_allowances_putsThemInThePocketAndNoTaxOnPay()
    {
        var inputs = EveryLever.WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        view.ShouldSatisfyAllConditions(
            () => view.Start.ShouldBe(ExampleStart),
            () => view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
            {
                ("pocket", 62_834.46m),
                ("taxes_on_pay", 20_420.70m),
                ("taxes_on_profit", 16_744.84m),
                ("kept_in_company", 0m),
                ("expenses", 409.30m),
            }));
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithVouchersAndAllowances_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = EveryLever.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
            .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - RunningCosts, revenue - RunningCosts));
    }

    // The rent at the 2026 cap (RiverInputsTests): its 398.68 of tax joins the taxes on pay, 20,819.38,
    // the rest of it the pocket, 62,067.06; the taxes on profit fall to 17,113.56: corporate tax
    // 9,724.00 and its surcharge 656.37, withholding 6,883.13 on the 38,239.63 left, less the refund of
    // 149.94.
    [Fact]
    public void From_rent_putsItsTaxInTheTaxesOnPayAndTheRestInThePocket()
    {
        var view = BucketsView.From(RentAtTheCap(RiverInputs.Example).Run()).ShouldNotBeNull();

        view.ShouldSatisfyAllConditions(
            () => view.Start.ShouldBe(ExampleStart),
            () => view.Buckets.Select(bucket => (bucket.Key, bucket.Amount)).ShouldBe(new[]
            {
                ("pocket", 62_067.06m),
                ("taxes_on_pay", 20_819.38m),
                ("taxes_on_profit", 17_113.56m),
                ("kept_in_company", 0m),
                ("expenses", 409.30m),
            }));
    }

    // The bucket says it holds the rent's tax too once there is one.
    [Fact]
    public void From_rent_saysTheTaxesOnPayIncludeIt()
    {
        (BucketsView.From(RentAtTheCap(RiverInputs.Example).Run())?.TaxesOnPayIncludeRent, BucketsView.From(RiverInputs.Example.Run())?.TaxesOnPayIncludeRent)
            .ShouldBe((true, false));
    }

    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithVouchersAndRent_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = RentAtTheCap(EveryLever.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m).WithInternetAllowance(240m));

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - RunningCosts, revenue - RunningCosts));
    }

    [Fact]
    public void From_refusedInput_drawsNothing()
    {
        BucketsView.From(RiverInputs.Example.WithReserve(-1m).Run()).ShouldBeNull();
    }

    // A car's costs are a cost of the company, like its running costs (operator, 2026-10-23): the bar
    // keeps its five buckets, the car's 8,000 joining the contribution's 409.30 in the expenses, and the
    // river still starts before them, from 100,409.30.
    [Fact]
    public void From_car_putsItsCostsInTheExpensesAndStartsBeforeThem()
    {
        var run = ElectricCar(RiverInputs.Example).Run();
        var year = run.Year.ShouldNotBeNull();

        var view = BucketsView.From(run).ShouldNotBeNull();

        view.ShouldSatisfyAllConditions(
            () => view.Buckets.Select(bucket => bucket.Key).ShouldBe(BucketsView.Keys),
            () => (view.Start, view.CarCosts, view.AmountOf("expenses"), view.Buckets[0].Amount).ShouldBe((ExampleStart, 8_000m, 8_409.30m, year.NetInPocket)));
    }

    // The director's 600 for the car leave the pocket for the company, so they stay in the buckets: they
    // add up, to the cent, to what the company earns before paying INASTI, its car or its director.
    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithACar_bucketsAddUpToTheProfitBeforeTheExpenses(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = ElectricCar(EveryLever.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve).WithMealVoucherDays(220))
            .WithCarContribution(600m).WithPrivateEnergyPaid(true).WithContributionsPaidBy(ContributionsPaidBy.Company);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - RunningCosts, revenue - RunningCosts));
    }

    // Until the car counts, the expenses are the contribution alone: in the plan with nothing given, or
    // out of it.
    [Fact]
    public void From_noCarCounted_holdsTheContributionAloneInTheExpenses()
    {
        var views = new[] { RiverInputs.Example.AddToPlan(OptionalModule.Car), ElectricCar(RiverInputs.Example).TakeOutOfPlan(OptionalModule.Car) }
            .Select(inputs => BucketsView.From(inputs.Run()).ShouldNotBeNull());

        views.Select(view => (view.Start, view.CarCosts, view.AmountOf("expenses"))).ShouldBe([(ExampleStart, 0m, 409.30m), (ExampleStart, 0m, 409.30m)]);
    }

    // The reach is out of what leaves the company once the expenses are paid, the 100,409.30 less the
    // car's 8,000 and the contribution's 409.30.
    [Fact]
    public void From_car_saysTheReachOutOfWhatLeavesTheCompanyAfterIt()
    {
        var view = BucketsView.From(ElectricCar(RiverInputs.Example).WithReserve(10_000m).Run()).ShouldNotBeNull();
        var leaves = 92_000m - view.Buckets.Single(bucket => bucket.Key == "kept_in_company").Amount;

        view.Reach.ShouldBe(view.Buckets[0].Amount / leaves);
    }

    // A royalty's tax apart is the director's income tax on money received besides the dividend: it
    // lands in the taxes on pay, as the rent's does (decided unattended).
    [Fact]
    public void From_royalty_putsItsTaxApartInTheTaxesOnPay()
    {
        var withRoyalty = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).Run();
        var year = withRoyalty.Year.ShouldNotBeNull();
        var taxes = year.Steps.Where(step => step.Track == Track.PaidOut && step.Event is TaxableEvent.DirectorPay or TaxableEvent.CopyrightRoyalty).Sum(step => step.Amount);

        BucketsView.From(withRoyalty).ShouldNotBeNull().Buckets.Single(bucket => bucket.Key == "taxes_on_pay").Amount.ShouldBe(taxes);
    }

    // The bucket says it holds the royalty's tax too once there is one.
    [Fact]
    public void From_royalty_saysTheTaxesOnPayIncludeIt()
    {
        (BucketsView.From(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).Run())?.TaxesOnPayIncludeRoyalty,
                BucketsView.From(RiverInputs.Example.Run())?.TaxesOnPayIncludeRoyalty)
            .ShouldBe((true, false));
    }

    // A royalty of 15,000 fits every year's 18,620 and more before pay and royalty: one above what the
    // company has there is refused.
    [Theory]
    [MemberData(nameof(Years))]
    public void From_anyYearWithARoyalty_bucketsAddUpToWhatTheCompanyEarnsBeforePayingTheDirector(decimal revenue, decimal pay, decimal reserve)
    {
        var inputs = RentAtTheCap(EveryLever.WithRevenue(revenue).WithDirectorPay(pay).WithReserve(reserve)).AddToPlan(OptionalModule.Copyright)
            .WithRoyalty(15_000m).WithContributionsPaidBy(ContributionsPaidBy.Company);

        var view = BucketsView.From(inputs.Run()).ShouldNotBeNull();

        (view.Start, view.Buckets.Sum(bucket => bucket.Amount)).ShouldBe((revenue - RunningCosts, revenue - RunningCosts));
    }

    // 12% of a cadastral income of 1,200 let for the 2026 cap, 1,380.00 (RentCalculatorTests).
    private static RiverInputs RentAtTheCap(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

    // YearSimulationCarTests' electric car: 8,000 of yearly costs.
    private static RiverInputs ElectricCar(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
}
