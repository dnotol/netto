using System.Text.Json.Nodes;
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
        .WithRevenue(150_000m).WithRunningCosts(30_500.50m)
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(48_500m).WithFirstRegistration(new DateOnly(2025, 6, 21)).WithCarCosts(9_250.50m).WithPrivateEnergyPaid(true).WithCarContribution(600m)
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

    // Version 2 holds the company car.
    [Fact]
    public void Version_isTwo()
    {
        PlanStorage.Version.ShouldBe(2);
    }

    // A version 2 plan without its car comes from a format this version cannot read.
    [Fact]
    public void Read_version2PlanMissingItsCar_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))), document => document["plans"]![0]!["inputs"]!.AsObject().Remove("car"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
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

    // Written again, the book is version 2, each plan in its shape, and reads back the same.
    [Fact]
    public void Write_afterReadingVersion1_writesEachPlanInVersion2()
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(Version1Book).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (JsonNode.Parse(rewritten)!["version"]!.GetValue<int>(), unreadable).ShouldBe((2, false)),
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
