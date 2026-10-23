using System.Text.Json.Nodes;
using Netto.Core.CompanyCar;
using Netto.Core.Dividends;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class PlanStorageTests
{
    // Every kind of value the river holds away from the example's: each lever in the plan with its
    // amounts, the user's own statuses and payments, a municipality picked and its rate typed over,
    // an amount withheld typed in.
    private static readonly RiverInputs Everything = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent).AddToPlan(OptionalModule.Car)
        .AddToPlan(OptionalModule.Copyright)
        .WithRevenue(150_000m).WithRunningCosts(30_500.50m)
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(48_500m).WithFirstRegistration(new DateOnly(2025, 6, 21)).WithCarCosts(9_250.50m).WithPrivateEnergyPaid(true).WithCarContribution(600m)
        .WithCarEngine(CarEngine.DieselPlugInHybrid).WithCarCo2(45).WithNoCo2Data(true).WithCarAcquired(new DateOnly(2025, 3, 1))
        .WithFakeHybrid(true).WithMatchingCarCo2(150).WithNoMatchingCar(true).WithSmallNaturalGas(true).WithCarPrivateCost(7_200m)
        .WithRoyalty(12_500.50m).WithAverageWithinCap(false).WithAttestation(true).WithCreativeShare(0.75m)
        .WithSmallCompany(false).WithFinancialYear(FinancialYear.Second)
        .WithAdvancePayment(0, 3_000m).WithAdvancePayment(3, 1_250.25m)
        .WithDirectorPay(42_000m)
        .WithMunicipality(Namur().Enter(0.075m))
        .WithTaxWithheld(9_000m).WithTaxAdvancePayment(1, 500m).WithStarter(true)
        .WithReserve(10_000m).WithDividendCategory(WithholdingCategory.Standard).WithEarlierProfits(5_000m)
        .WithContributionsPaidBy(ContributionsPaidBy.Company);

    [Fact]
    public void Read_nothingStored_isNoPlanAndReadable()
    {
        PlanStorage.Read(null).ShouldBe((PlanBook.Empty, false));
    }

    [Fact]
    public void Read_written_givesBackEachPlanTheStarAndTheOpenedOne()
    {
        var book = Book(("a", "Today", RiverInputs.Example), ("b", "Best pay", RiverInputs.Example.WithDirectorPay(20_799m))) with { Today = "a", Opened = "b" };

        var (read, unreadable) = PlanStorage.Read(PlanStorage.Write(book));

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a", "b")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs))
                .ShouldBe([("a", "Today", RiverInputs.Example), ("b", "Best pay", RiverInputs.Example.WithDirectorPay(20_799m))]));
    }

    [Fact]
    public void Read_written_givesBackEveryInputWithItsOrigin()
    {
        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", Everything))));

        read.Plans.Single().Inputs.ShouldBe(Everything);
    }

    // The company skipped, the river starting at the pay; the pay slip's withholding, derived; a
    // municipality picked at its official rate.
    [Fact]
    public void Read_written_givesBackASkippedCompanyAndDerivedValues()
    {
        var inputs = RiverInputs.Example.SkipCompany().WithMunicipality(Namur()).WithTaxWithheld(1m).WithTaxWithheldAsThePaySlip();

        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs))));

        read.Plans.Single().Inputs.ShouldBe(inputs);
    }

    // What the example holds none of is stored as the example's, as the build before stored it, so a plan
    // saved here still opens there: the example as version 5 wrote it, with version 6's private cost.
    [Fact]
    public void Store_example_writesWhatItHoldsNoneOfAsTheBuildBeforeDid()
    {
        var before = JsonNode.Parse(Version5Book)!["plans"]![0]!["inputs"]!;
        before["car"]!["privateCost"] = JsonNode.Parse("""{"value":null,"origin":"Yours"}""");

        PlanStorage.Store(RiverInputs.Example).GetRawText().ShouldBe(before.ToJsonString());
    }

    // A nothing the user typed is theirs, stored and read back as theirs: only an example's amount stored
    // at nothing is read back as the example's nothing.
    [Fact]
    public void Read_written_givesBackANothingTypedAsYours()
    {
        var inputs = RiverInputs.Example.WithEcoVouchers(0m).WithCarCo2(0).WithReserve(0m);

        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs))));

        read.Plans.Single().Inputs.ShouldBe(inputs);
    }

    [Fact]
    public void Read_newerVersion_refusesTheWholeDocument()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["version"] = PlanStorage.Version + 1);

        PlanStorage.Read(json).ShouldBe((PlanBook.Empty, true));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"version":1,"plans":[{"id":"a"}]}""")]
    public void Read_notADocumentOfPlans_refusesIt(string json)
    {
        PlanStorage.Read(json).ShouldBe((PlanBook.Empty, true));
    }

    // A municipality code the year's rates no longer list: never another rate in its place.
    [Fact]
    public void Read_planWithAnUnknownMunicipality_makesThatPlanUnreadableAndKeepsTheOthers()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example.WithMunicipality(Namur())), ("b", "Plan 2", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["municipality"]!["insCode"] = 1);

        var (read, unreadable) = PlanStorage.Read(json);

        read.ShouldSatisfyAllConditions(
            () => unreadable.ShouldBeFalse(),
            () => read.Plans.Select(plan => (plan.Name, plan.Readable)).ShouldBe([("Plan 1", false), ("Plan 2", true)]));
    }

    // A field missing is never read as its default.
    [Fact]
    public void Read_planMissingAField_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!.AsObject().Remove("reserve"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A field this version does not know comes from a format it cannot read.
    [Fact]
    public void Read_planWithAFieldItDoesNotKnow_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!["carLease"] = 850);

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    [Fact]
    public void Read_planWithAnOptionItDoesNotKnow_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["dividendCategory"]!["value"] = "LiquidationReserve");

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // What could not be read is written back as it was, not dropped.
    [Fact]
    public void Write_unreadablePlan_keepsItAsStored()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!["carLease"] = 850);

        var rewritten = PlanStorage.Write(PlanStorage.Read(json).Book);

        JsonNode.Parse(rewritten)!["plans"]![0]!["inputs"]!["carLease"]!.GetValue<int>().ShouldBe(850);
    }

    [Fact]
    public void Write_document_carriesItsVersion()
    {
        JsonNode.Parse(PlanStorage.Write(PlanBook.Empty))!["version"]!.GetValue<int>().ShouldBe(PlanStorage.Version);
    }

    // Version 6 holds what the same car would cost privately.
    [Fact]
    public void Version_isSix()
    {
        PlanStorage.Version.ShouldBe(6);
    }

    // The private cost has no default: none stored is none read back, never an amount.
    [Fact]
    public void Read_written_givesBackNoPrivateCostAsNone()
    {
        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))));

        read.Plans.Single().Inputs.ShouldNotBeNull().Car.PrivateCost.ShouldBe(CarInputs.NoPrivateCost);
    }

    // A plan of this version that does not say what its car would cost privately is not read as one
    // that gave none.
    [Fact]
    public void Read_planMissingItsCarsPrivateCost_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["car"]!.AsObject().Remove("privateCost"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A document version 5 of the format wrote (captured from it, 2026-10-23): the example as « Today »,
    // and a plan holding a petrol car with a contribution and a royalty that follows the pay, opened.
    private const string Version5Book =
        """{"version":5,"today":"a1","opened":"b2","plans":[{"id":"a1","name":"Today","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false,"copyright":false},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":0,"origin":"Example"},"followsPay":{"value":true,"origin":"Default"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":false,"origin":"Default"},"creativeShare":{"value":null,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":50000,"origin":"Example"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"},"billedContributions":{"value":null,"origin":"Yours"}}},{"id":"b2","name":"Plan 2","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":true,"copyright":true},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":45000,"origin":"Yours"},"firstRegistration":{"value":"2025-03-01","origin":"Yours"},"yearlyCosts":{"value":9000,"origin":"Yours"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":600,"origin":"Yours"},"engine":{"value":"Petrol","origin":"Yours"},"co2":{"value":120,"origin":"Yours"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2025-02-01","origin":"Yours"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":0,"origin":"Example"},"followsPay":{"value":true,"origin":"Default"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":false,"origin":"Default"},"creativeShare":{"value":0.6,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":42000,"origin":"Yours"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"},"billedContributions":{"value":null,"origin":"Yours"}}}]}""";

    // What the second plan of the version 5 document held, as the river builds it.
    private static readonly RiverInputs Version5Plan2 = RiverInputs.Example
        .AddToPlan(OptionalModule.Car).WithCatalogueValue(45_000m).WithFirstRegistration(new DateOnly(2025, 3, 1)).WithCarCosts(9_000m).WithCarContribution(600m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2025, 2, 1))
        .AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.6m)
        .WithDirectorPay(42_000m);

    // Saved before the car's private cost was asked, each plan opens as it was saved, with none given,
    // « Today » and the plan opened kept.
    [Fact]
    public void Read_version5_readsEachPlanAsItWasSavedWithNoPrivateCost()
    {
        var (read, unreadable) = PlanStorage.Read(Version5Book);

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs)).ShouldBe([("a1", "Today", RiverInputs.Example), ("b2", "Plan 2", Version5Plan2)]),
            () => read.Plans.ShouldAllBe(plan => plan.Inputs!.Car.PrivateCost == CarInputs.NoPrivateCost));
    }

    // Written again, the book is version 6, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion5_writesEachPlanInVersion6()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version5Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((6, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe([RiverInputs.Example, Version5Plan2]));
    }

    // Version 5 never asked what the car would cost privately: a version 5 plan saying it is not a plan
    // it wrote.
    [Fact]
    public void Read_version5PlanWithAPrivateCost_makesThatPlanUnreadable()
    {
        var json = Edit(Version5Book, document => document["plans"]![1]!["inputs"]!["car"]!["privateCost"] =
            JsonNode.Parse("""{"value":7200,"origin":"Yours"}"""));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([true, false]);
    }

    // A royalty that follows the pay keeps the amount fixed before it, for when it is fixed again.
    [Fact]
    public void Read_written_givesBackARoyaltyThatFollowsThePay()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(5_000m).WithRoyaltyFollowingPay().WithCreativeShare(0.4m);

        PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs)))).Book.Plans.Single().Inputs.ShouldBe(inputs);
    }

    // A plan of this version that does not say whether its royalty follows the pay is not read as one
    // that does.
    [Fact]
    public void Read_planMissingWhetherItsRoyaltyFollowsThePay_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["copyright"]!.AsObject().Remove("followsPay"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A document version 4 of the format wrote (captured from it, 2026-10-23): the example as « Today »,
    // and a plan holding a royalty of 10,000 given, with an attestation, a creative share and what the
    // fund bills, opened.
    internal const string Version4Book =
        """{"version":4,"today":"a1","opened":"b2","plans":[{"id":"a1","name":"Today","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false,"copyright":false},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":0,"origin":"Example"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":false,"origin":"Default"},"creativeShare":{"value":null,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":50000,"origin":"Example"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"},"billedContributions":{"value":null,"origin":"Yours"}}},{"id":"b2","name":"Plan 2","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false,"copyright":true},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":10000,"origin":"Yours"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":true,"origin":"Yours"},"creativeShare":{"value":0.6,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":42000,"origin":"Yours"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"},"billedContributions":{"value":900,"origin":"Yours"}}}]}""";

    // What the second plan of the version 4 document held, as the river builds it: a royalty typed is
    // a fixed one.
    internal static readonly RiverInputs Version4Plan2 = RiverInputs.Example
        .AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).WithAttestation(true).WithCreativeShare(0.6m).WithDirectorPay(42_000m)
        .WithBilledContributions(900m);

    // Saved before a royalty could follow the pay, a royalty the user gave opens fixed at it, as it was
    // saved; one never given follows the pay, as a royalty added now does (2026-10-23).
    [Fact]
    public void Read_version4_opensARoyaltyGivenFixedAtItAndAnyOtherFollowingThePay()
    {
        var (read, unreadable) = PlanStorage.Read(Version4Book);

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs)).ShouldBe([("a1", "Today", RiverInputs.Example), ("b2", "Plan 2", Version4Plan2)]),
            () => read.Plans.Select(plan => plan.Inputs!.Copyright.FollowsPay)
                .ShouldBe([new Input<bool>(true, InputOrigin.Default), new Input<bool>(false, InputOrigin.Yours)]));
    }

    // Written again, the book is version 6, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion4_writesEachPlanInVersion6()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version4Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((6, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe([RiverInputs.Example, Version4Plan2]));
    }

    // Version 4 never said whether the royalty follows the pay: a version 4 plan saying it is not a plan
    // it wrote.
    [Fact]
    public void Read_version4PlanSayingWhetherTheRoyaltyFollowsThePay_makesThatPlanUnreadable()
    {
        var json = Edit(Version4Book, document => document["plans"]![0]!["inputs"]!["copyright"]!["followsPay"] =
            JsonNode.Parse("""{"value":true,"origin":"Default"}"""));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // A document version 3 of the format wrote (captured from it, 2026-10-22): the example as
    // « Today », and a plan holding a copyright royalty with an attestation and a creative share, opened.
    private const string Version3Book =
        """{"version":3,"today":"a1","opened":"b2","plans":[{"id":"a1","name":"Today","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false,"copyright":false},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":0,"origin":"Example"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":false,"origin":"Default"},"creativeShare":{"value":null,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":50000,"origin":"Example"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"}}},{"id":"b2","name":"Plan 2","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false,"copyright":true},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"copyright":{"royalty":{"value":10000,"origin":"Yours"},"averageWithinCap":{"value":true,"origin":"Default"},"holdsAttestation":{"value":true,"origin":"Yours"},"creativeShare":{"value":0.6,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":42000,"origin":"Yours"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"}}}]}""";

    // What the second plan of the version 3 document held, as the river builds it.
    private static readonly RiverInputs Version3Plan2 = RiverInputs.Example
        .AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).WithAttestation(true).WithCreativeShare(0.6m).WithDirectorPay(42_000m);

    // Saved before the calendar's input, each plan opens as it was saved, with nothing billed given.
    [Fact]
    public void Read_version3_readsEachPlanAsItWasSavedWithNothingBilled()
    {
        var (read, unreadable) = PlanStorage.Read(Version3Book);

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs)).ShouldBe([("a1", "Today", RiverInputs.Example), ("b2", "Plan 2", Version3Plan2)]),
            () => read.Plans.ShouldAllBe(plan => plan.Inputs!.BilledContributions == RiverInputs.NoBilledContributions));
    }

    // Written again, the book is version 6, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion3_writesEachPlanInVersion6()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version3Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((6, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe([RiverInputs.Example, Version3Plan2]));
    }

    // Version 3 never held what the fund bills: a version 3 plan carrying it is not a plan it wrote.
    [Fact]
    public void Read_version3PlanWithBilledContributions_makesThatPlanUnreadable()
    {
        var json = Edit(Version3Book, document => document["plans"]![0]!["inputs"]!["billedContributions"] = JsonNode.Parse("""{"value":900,"origin":"Yours"}"""));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    [Fact]
    public void Store_billedContributions_readsBackAsGiven()
    {
        var inputs = RiverInputs.Example.WithBilledContributions(1_234.56m);

        var read = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs)))).Book.Plans.Single().Inputs;

        read.ShouldBe(inputs);
    }

    // A plan saved with the car's first build, before its engine was asked, is not read with an
    // electric car in its place: the operator accepted it does not open (2026-10-22).
    [Fact]
    public void Read_planMissingItsCarsEngine_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!["car"]!.AsObject().Remove("engine"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A plan saved with the royalty's first build, before its attestation was asked, is not read as a
    // plan without one: it does not open, as the car's first build's did not (decided unattended,
    // 2026-10-22).
    [Fact]
    public void Read_planMissingItsRoyaltysAttestation_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["copyright"]!.AsObject().Remove("holdsAttestation"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // The creative share has no default: none stored is none read back, never a share.
    [Fact]
    public void Read_written_givesBackNoCreativeShareAsNone()
    {
        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))));

        read.Plans.Single().Inputs.ShouldNotBeNull().Copyright.CreativeShare.Value.ShouldBeNull();
    }

    // A plan saved before the share was asked is not read as one without it.
    [Fact]
    public void Read_planMissingItsRoyaltysCreativeShare_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!["copyright"]!.AsObject().Remove("creativeShare"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A plan of this version without its car or its royalty comes from a format it cannot read.
    [Theory]
    [InlineData("car")]
    [InlineData("copyright")]
    public void Read_planMissingALever_makesThatPlanUnreadable(string lever)
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!.AsObject().Remove(lever));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // A document version 2 of the format wrote (captured from it, 2026-10-22): the example as
    // « Today », and a plan holding the vouchers and a petrol car, opened.
    private const string Version2Book =
        """{"version":2,"today":"a1","opened":"b2","plans":[{"id":"a1","name":"Today","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false,"car":false},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":0,"origin":"Example"},"firstRegistration":{"value":"2026-01-01","origin":"Example"},"yearlyCosts":{"value":0,"origin":"Example"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Electric","origin":"Default"},"co2":{"value":0,"origin":"Example"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2026-01-01","origin":"Example"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":50000,"origin":"Example"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"}}},{"id":"b2","name":"Plan 2","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":true,"allowances":false,"rent":false,"car":true},"vouchers":{"mealVoucherDays":{"value":200,"origin":"Yours"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"car":{"catalogueValue":{"value":45000,"origin":"Yours"},"firstRegistration":{"value":"2025-03-01","origin":"Yours"},"yearlyCosts":{"value":9000,"origin":"Yours"},"companyPaysPrivateEnergy":{"value":false,"origin":"Default"},"personalContribution":{"value":0,"origin":"Example"},"engine":{"value":"Petrol","origin":"Yours"},"co2":{"value":120,"origin":"Yours"},"noCo2Data":{"value":false,"origin":"Default"},"acquired":{"value":"2025-02-01","origin":"Yours"},"fakeHybrid":{"value":false,"origin":"Default"},"matchingCarCo2":{"value":0,"origin":"Example"},"noMatchingCar":{"value":false,"origin":"Default"},"smallNaturalGas":{"value":false,"origin":"Default"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":42000,"origin":"Yours"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"}}}]}""";

    // What the second plan of the version 2 document held, as the river builds it.
    private static readonly RiverInputs Version2Plan2 = RiverInputs.Example
        .AddToPlan(OptionalModule.Car).AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(200)
        .WithCatalogueValue(45_000m).WithFirstRegistration(new DateOnly(2025, 3, 1)).WithCarCosts(9_000m).WithCarEngine(CarEngine.Petrol).WithCarCo2(120)
        .WithCarAcquired(new DateOnly(2025, 2, 1)).WithDirectorPay(42_000m);

    // Saved before the royalty, each plan opens as it was saved, without one, « Today » and the plan
    // opened kept.
    [Fact]
    public void Read_version2_readsEachPlanAsItWasSavedWithoutARoyalty()
    {
        var (read, unreadable) = PlanStorage.Read(Version2Book);

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs)).ShouldBe([("a1", "Today", RiverInputs.Example), ("b2", "Plan 2", Version2Plan2)]));
    }

    [Fact]
    public void Read_version2_holdsNoRoyaltyInAnyPlan()
    {
        PlanStorage.Read(Version2Book).Book.Plans
            .ShouldAllBe(plan => !plan.Inputs!.InPlan(OptionalModule.Copyright) && plan.Inputs.Copyright == RiverInputs.Example.Copyright);
    }

    // Written again, the book is version 6, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion2_writesEachPlanInVersion6()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version2Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((6, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe([RiverInputs.Example, Version2Plan2]));
    }

    // Version 2 never held a royalty: a version 2 plan carrying one is not a plan it wrote.
    [Fact]
    public void Read_version2PlanWithARoyalty_makesThatPlanUnreadable()
    {
        var json = Edit(Version2Book, document => document["plans"]![0]!["inputs"]!["copyright"] =
            JsonNode.Parse(PlanStorage.Store(RiverInputs.Example).GetProperty("copyright").GetRawText()));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // A document version 1 of the format wrote (captured from it, 2026-10-22): the example as
    // « Today », and a plan holding the vouchers and the rent, opened.
    private const string Version1Book =
        """{"version":1,"today":"a1","opened":"b2","plans":[{"id":"a1","name":"Today","inputs":{"incomeYear":2026,"company":{"revenue":{"value":120000,"origin":"Example"},"runningCosts":{"value":20000,"origin":"Example"}},"composition":{"vouchers":false,"allowances":false,"rent":false},"vouchers":{"mealVoucherDays":{"value":0,"origin":"Example"},"ecoVouchers":{"value":0,"origin":"Example"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":0,"origin":"Example"},"shareLet":{"value":0,"origin":"Example"},"rent":{"value":0,"origin":"Example"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":50000,"origin":"Example"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":null,"origin":"Derived"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":0,"origin":"Example"},"dividendCategory":{"value":"VvprbisThirdYearOnwards","origin":"Default"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Director","origin":"Default"}}},{"id":"b2","name":"Plan 2","inputs":{"incomeYear":2026,"company":{"revenue":{"value":150000,"origin":"Yours"},"runningCosts":{"value":30500.50,"origin":"Yours"}},"composition":{"vouchers":true,"allowances":false,"rent":true},"vouchers":{"mealVoucherDays":{"value":220,"origin":"Yours"},"ecoVouchers":{"value":250,"origin":"Yours"},"sportCultureVouchers":{"value":0,"origin":"Example"}},"allowances":{"office":{"value":0,"origin":"Example"},"internet":{"value":0,"origin":"Example"},"computer":{"value":0,"origin":"Example"},"phone":{"value":0,"origin":"Example"}},"rent":{"cadastralIncome":{"value":1200,"origin":"Yours"},"shareLet":{"value":0.12,"origin":"Yours"},"rent":{"value":1380,"origin":"Yours"}},"smallCompany":{"value":true,"origin":"Default"},"financialYear":{"value":"FifthOrLater","origin":"Default"},"advancePayments":{"first":{"value":3000,"origin":"Yours"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"directorPay":{"value":42000,"origin":"Yours"},"municipality":{"insCode":null,"rate":0.07,"origin":"Default"},"taxPrepayments":{"withheld":{"value":9000,"origin":"Yours"},"paid":{"first":{"value":0,"origin":"Example"},"second":{"value":0,"origin":"Example"},"third":{"value":0,"origin":"Example"},"fourth":{"value":0,"origin":"Example"}},"starter":{"value":false,"origin":"Default"}},"reserve":{"value":10000,"origin":"Yours"},"dividendCategory":{"value":"Standard","origin":"Yours"},"earlierProfits":{"value":0,"origin":"Example"},"contributionsPaidBy":{"value":"Company","origin":"Yours"}}}]}""";

    // What the second plan of the version 1 document held, as the river builds it.
    private static readonly RiverInputs Version1Plan2 = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Rent)
        .WithRevenue(150_000m).WithRunningCosts(30_500.50m)
        .WithMealVoucherDays(220).WithEcoVouchers(250m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithAdvancePayment(0, 3_000m)
        .WithDirectorPay(42_000m)
        .WithTaxWithheld(9_000m)
        .WithReserve(10_000m).WithDividendCategory(WithholdingCategory.Standard)
        .WithContributionsPaidBy(ContributionsPaidBy.Company);

    // Saved before the car, each plan opens as it was saved, without one, « Today » and the plan
    // opened kept.
    [Fact]
    public void Read_version1_readsEachPlanAsItWasSavedWithoutACar()
    {
        var (read, unreadable) = PlanStorage.Read(Version1Book);

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name, plan.Inputs)).ShouldBe([("a1", "Today", RiverInputs.Example), ("b2", "Plan 2", Version1Plan2)]));
    }

    [Fact]
    public void Read_version1_holdsNoCarInAnyPlan()
    {
        PlanStorage.Read(Version1Book).Book.Plans.ShouldAllBe(plan => !plan.Inputs!.InPlan(OptionalModule.Car) && plan.Inputs.Car == RiverInputs.Example.Car);
    }

    [Fact]
    public void Read_version1_holdsNoRoyaltyInAnyPlan()
    {
        PlanStorage.Read(Version1Book).Book.Plans
            .ShouldAllBe(plan => !plan.Inputs!.InPlan(OptionalModule.Copyright) && plan.Inputs.Copyright == RiverInputs.Example.Copyright);
    }

    // Written again, the book is version 6, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion1_writesEachPlanInVersion6()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version1Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((6, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe([RiverInputs.Example, Version1Plan2]));
    }

    // Version 1 never held a car: a version 1 plan carrying one is not a plan it wrote.
    [Fact]
    public void Read_version1PlanWithACar_makesThatPlanUnreadable()
    {
        var json = Edit(Version1Book, document => document["plans"]![0]!["inputs"]!["car"] = JsonNode.Parse(PlanStorage.Store(RiverInputs.Example).GetProperty("car").GetRawText()));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // What could not be read in a version 1 book is kept as it was stored.
    [Fact]
    public void Write_unreadableVersion1Plan_keepsItAsStored()
    {
        var json = Edit(Version1Book, document => document["plans"]![0]!["inputs"]!["carLease"] = 850);

        var rewritten = PlanStorage.Write(PlanStorage.Read(json).Book);

        JsonNode.Parse(rewritten)!["plans"]![0]!["inputs"]!["carLease"]!.GetValue<int>().ShouldBe(850);
    }

    private static PlanBook Book(params (string Id, string Name, RiverInputs Inputs)[] plans) =>
        new([.. plans.Select(plan => new SavedPlan(plan.Id, plan.Name, plan.Inputs, PlanStorage.Store(plan.Inputs)))], null, null);

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var document = JsonNode.Parse(json)!;
        edit(document);
        return document.ToJsonString();
    }

    private static Netto.Web.Client.DirectorPay.MunicipalityChoice Namur() =>
        RiverInputs.Example.Municipality.Pick(RiverInputs.Example.Municipality.Search("Namur").First());
}
