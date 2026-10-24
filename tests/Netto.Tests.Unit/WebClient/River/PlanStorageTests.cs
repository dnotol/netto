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
        .WithSmallCompany(false).WithFinancialYear(FinancialYear.Second).WithCompanyContributionBand(CompanyContributionBand.Upper)
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

    // What the example holds none of is stored as the example's, as the builds before stored it: the
    // example as version 6 wrote it, with version 7's band of INASTI's company contribution, the default
    // lower one, after the financial year, and its fund fee, none given, last.
    [Fact]
    public void Store_example_writesWhatItHoldsNoneOfAsTheBuildBeforeDid()
    {
        var before = PlanInputs(StoredPlanDocuments.Version6, 0).AsObject();
        before.Insert(before.IndexOf("financialYear") + 1, "companyContributionBand", JsonNode.Parse("""{"value":"Lower","origin":"Default"}"""));
        before.Add("fundFeeRate", JsonNode.Parse("""{"value":null,"origin":"Yours"}"""));

        PlanStorage.Store(StoredPlanDocuments.SavedExample).GetRawText().ShouldBe(before.ToJsonString());
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

    // No version came before the first.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Read_versionBeforeTheFirst_refusesTheWholeDocument(int version)
    {
        var json = Edit(StoredPlanDocuments.Version1, document => document["version"] = version);

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

    // A year whose municipal rates Netto does not hold: never another year's rate in their place, in a plan
    // of any version.
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(7)]
    public void Read_planOfAYearWithoutMunicipalRates_makesThatPlanUnreadable(int version)
    {
        var json = Edit(StoredPlanDocuments.Of(version), document => PlanInputs(document, 0)["incomeYear"] = Fixtures.UnsupportedIncomeYear);

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
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

    // Version 7 holds the band of INASTI's company contribution the company's balance sheet is in, and the
    // rate of the fund's management fee.
    [Fact]
    public void Version_isSeven()
    {
        PlanStorage.Version.ShouldBe(7);
    }

    [Fact]
    public void Read_written_givesBackAFundFeeRate()
    {
        var inputs = RiverInputs.Example.WithFundFeeRate(0.0305m);

        PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs)))).Book.Plans.Single().Inputs.ShouldBe(inputs);
    }

    // The fee's rate has no default: none stored is none read back, never a rate.
    [Fact]
    public void Read_written_givesBackNoFundFeeRateAsNone()
    {
        var (read, _) = PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))));

        read.Plans.Single().Inputs.ShouldNotBeNull().FundFeeRate.ShouldBe(RiverInputs.NoFundFeeRate);
    }

    // A plan of this version that does not say its fund's fee is not read as one that gave none.
    [Fact]
    public void Read_planMissingItsFundFeeRate_makesThatPlanUnreadable()
    {
        var json = Edit(PlanStorage.Write(Book(("a", "Plan 1", RiverInputs.Example))),
            document => document["plans"]![0]!["inputs"]!.AsObject().Remove("fundFeeRate"));

        PlanStorage.Read(json).Book.Plans.Single().Readable.ShouldBeFalse();
    }

    // The band read back is the one written, with its origin.
    [Theory]
    [InlineData(CompanyContributionBand.Lower)]
    [InlineData(CompanyContributionBand.Upper)]
    [InlineData(CompanyContributionBand.Exempt)]
    public void Read_written_givesBackTheBand(CompanyContributionBand band)
    {
        var inputs = RiverInputs.Example.WithCompanyContributionBand(band);

        PlanStorage.Read(PlanStorage.Write(Book(("a", "Plan 1", inputs)))).Book.Plans.Single().Inputs.ShouldBe(inputs);
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

    // A document of each version, as that version wrote it (StoredPlanDocuments), opens each plan as it was
    // saved, « Today » and the plan opened kept, with what its format did not hold as the versions after it
    // stored it, never as the river's example is today.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Read_documentOfEachVersion_readsEachPlanAsItWasSaved(int version)
    {
        var (read, unreadable) = PlanStorage.Read(StoredPlanDocuments.Of(version));

        read.ShouldSatisfyAllConditions(
            () => (unreadable, read.Today, read.Opened).ShouldBe((false, "a1", "b2")),
            () => read.Plans.Select(plan => (plan.Id, plan.Name)).ShouldBe([("a1", "Today"), ("b2", "Plan 2")]),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe(StoredPlanDocuments.Plans(version)));
    }

    // Written again, the book is this version's, each plan in its shape, and reads back the same.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Write_afterReadingADocumentOfEachVersion_writesEachPlanInThisVersion(int version)
    {
        var rewritten = PlanStorage.Write(PlanStorage.Read(StoredPlanDocuments.Of(version)).Book);

        var (read, unreadable) = PlanStorage.Read(rewritten);

        read.ShouldSatisfyAllConditions(
            () => (Child(JsonNode.Parse(rewritten), "version").GetValue<int>(), unreadable).ShouldBe((PlanStorage.Version, false)),
            () => read.Plans.Select(plan => plan.Inputs).ShouldBe(StoredPlanDocuments.Plans(version)));
    }

    // A document of this version is written back as it was read, each plan as it was stored.
    [Fact]
    public void Write_afterReadingADocumentOfThisVersion_writesItBackAsRead()
    {
        PlanStorage.Write(PlanStorage.Read(StoredPlanDocuments.Version7).Book).ShouldBe(StoredPlanDocuments.Version7);
    }

    // A version's plan carrying a member a later version brought, as that version stored it, is not a plan it
    // wrote: it is not read, and the other plans are.
    [Theory]
    [InlineData(1, "composition.car", 2)]
    [InlineData(1, "car", 2)]
    [InlineData(1, "composition.copyright", 3)]
    [InlineData(1, "copyright", 3)]
    [InlineData(1, "billedContributions", 4)]
    [InlineData(2, "composition.copyright", 3)]
    [InlineData(2, "copyright", 3)]
    [InlineData(2, "billedContributions", 4)]
    [InlineData(2, "car.privateCost", 6)]
    [InlineData(3, "billedContributions", 4)]
    [InlineData(3, "copyright.followsPay", 5)]
    [InlineData(3, "car.privateCost", 6)]
    [InlineData(4, "copyright.followsPay", 5)]
    [InlineData(4, "car.privateCost", 6)]
    [InlineData(5, "car.privateCost", 6)]
    [InlineData(1, "companyContributionBand", 7)]
    [InlineData(3, "companyContributionBand", 7)]
    [InlineData(5, "companyContributionBand", 7)]
    [InlineData(6, "companyContributionBand", 7)]
    [InlineData(1, "fundFeeRate", 7)]
    [InlineData(4, "fundFeeRate", 7)]
    [InlineData(6, "fundFeeRate", 7)]
    public void Read_documentOfAVersionWithAMemberALaterVersionBrought_makesThatPlanUnreadable(int version, string member, int broughtBy)
    {
        var brought = Member(PlanInputs(StoredPlanDocuments.Of(broughtBy), 1), member).DeepClone();

        var json = Edit(StoredPlanDocuments.Of(version), document => Put(PlanInputs(document, 0), member, brought));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // A plan of any version missing a member its format held is never read with a default in its place,
    // nor with what bringing it to this version would have added to it.
    [Theory]
    [InlineData(1, "reserve")]
    [InlineData(2, "reserve")]
    [InlineData(3, "reserve")]
    [InlineData(4, "reserve")]
    [InlineData(5, "reserve")]
    [InlineData(6, "reserve")]
    [InlineData(7, "reserve")]
    [InlineData(7, "companyContributionBand")]
    [InlineData(7, "fundFeeRate")]
    [InlineData(1, "composition")]
    [InlineData(2, "composition")]
    [InlineData(2, "car")]
    [InlineData(3, "copyright")]
    [InlineData(4, "copyright.royalty")]
    [InlineData(5, "car")]
    public void Read_documentOfAVersionMissingAMember_makesThatPlanUnreadable(int version, string member)
    {
        var json = Edit(StoredPlanDocuments.Of(version), document => Remove(PlanInputs(document, 0), member));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // A plan whose inputs are not an object is not read, and the other plans are.
    [Theory]
    [InlineData(1, "null")]
    [InlineData(3, "5")]
    [InlineData(5, "[]")]
    [InlineData(6, "5")]
    [InlineData(7, "[]")]
    public void Read_planWhoseInputsAreNotAnObject_makesThatPlanUnreadable(int version, string inputs)
    {
        var json = Edit(StoredPlanDocuments.Of(version), document => Child(Child(document, "plans"), 0)["inputs"] = JsonNode.Parse(inputs));

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // A plan of an older version that cannot be read is written back exactly as it was stored: nothing
    // added to read it stays.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Write_unreadablePlanOfAnOlderVersion_keepsItExactlyAsStored(int version)
    {
        var json = Edit(StoredPlanDocuments.Of(version), document => PlanInputs(document, 0)["carLease"] = 850);

        var rewritten = PlanStorage.Write(PlanStorage.Read(json).Book);

        PlanInputs(rewritten, 0).ToJsonString().ShouldBe(PlanInputs(json, 0).ToJsonString());
    }

    // A member stored twice in a plan of an older version, at its top or inside, is not a plan that version
    // wrote: it is read with neither value, and the other plans are.
    [Theory]
    [InlineData(1, "\"reserve\":{\"value\":0,\"origin\":\"Example\"}")]
    [InlineData(2, "\"runningCosts\":{\"value\":20000,\"origin\":\"Example\"}")]
    [InlineData(3, "\"reserve\":{\"value\":0,\"origin\":\"Example\"}")]
    [InlineData(4, "\"runningCosts\":{\"value\":20000,\"origin\":\"Example\"}")]
    [InlineData(5, "\"engine\":{\"value\":\"Electric\",\"origin\":\"Default\"}")]
    [InlineData(6, "\"financialYear\":{\"value\":\"FifthOrLater\",\"origin\":\"Default\"}")]
    public void Read_olderPlanHoldingAMemberTwice_makesThatPlanUnreadable(int version, string member)
    {
        var document = StoredPlanDocuments.Of(version);
        var first = document.IndexOf(member, StringComparison.Ordinal);

        var json = document.Insert(first + member.Length, "," + member);

        PlanStorage.Read(json).Book.Plans.Select(plan => plan.Readable).ShouldBe([false, true]);
    }

    // An example's amount stored at nothing reads back as the example's nothing in a plan of any version,
    // money and a count alike; a nothing the user typed stays theirs (Read_written_givesBackANothingTypedAsYours).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Read_documentOfEachVersion_readsAnExampleNothingAsNone(int version)
    {
        var today = PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans[0].Inputs.ShouldNotBeNull();

        (today.Reserve, today.Vouchers.MealVoucherDays).ShouldBe((new Input<decimal>(0m, InputOrigin.None), new Input<int>(0, InputOrigin.None)));
    }

    // Saved before the car, version 1's plans hold none: out of the plan, its values as version 2 stored the
    // example's.
    [Fact]
    public void Read_documentBeforeTheCar_holdsNoCarInAnyPlan()
    {
        var plans = PlanStorage.Read(StoredPlanDocuments.Version1).Book.Plans;

        plans.ShouldSatisfyAllConditions(
            () => plans.Select(plan => plan.Inputs?.InPlan(OptionalModule.Car)).ShouldAllBe(inPlan => inPlan == false),
            () => plans.Select(plan => plan.Inputs?.Car).ShouldAllBe(car => car == StoredPlanDocuments.SavedExample.Car));
    }

    // Saved before the royalty, the plans of versions 1 and 2 hold none: out of the plan, its values as
    // version 3 stored the example's, following the pay as a royalty added now does.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Read_documentBeforeTheRoyalty_holdsNoRoyaltyInAnyPlan(int version)
    {
        var plans = PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans;

        plans.ShouldSatisfyAllConditions(
            () => plans.Select(plan => plan.Inputs?.InPlan(OptionalModule.Copyright)).ShouldAllBe(inPlan => inPlan == false),
            () => plans.Select(plan => plan.Inputs?.Copyright).ShouldAllBe(copyright => copyright == StoredPlanDocuments.SavedExample.Copyright));
    }

    // Saved before the calendar's input, the plans of versions 1 to 3 have nothing billed given.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Read_documentBeforeTheBill_givesNothingBilledInAnyPlan(int version)
    {
        PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans.Select(plan => plan.Inputs?.BilledContributions)
            .ShouldAllBe(billed => billed == StoredPlanDocuments.NoneGiven);
    }

    // Saved before a royalty could follow the pay, a royalty the user gave opens fixed at it, as it was
    // saved; one never given follows the pay, as a royalty added now does (2026-10-23).
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void Read_documentBeforeTheRoyaltyCouldFollowThePay_opensARoyaltyGivenFixedAtItAndAnyOtherFollowingThePay(int version)
    {
        PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans.Select(plan => plan.Inputs?.Copyright.FollowsPay)
            .ShouldBe([new Input<bool>(true, InputOrigin.Default), new Input<bool>(false, InputOrigin.Yours)]);
    }

    // Saved before the car's private cost, the plans of versions 1 to 5 have none given.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Read_documentBeforeThePrivateCost_givesNoPrivateCostInAnyPlan(int version)
    {
        PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans.Select(plan => plan.Inputs?.Car.PrivateCost)
            .ShouldAllBe(cost => cost == StoredPlanDocuments.NoneGiven);
    }

    // Saved before the band of INASTI's company contribution was asked, the plans of versions 1 to 6 open
    // in the lower band, the default a plan starts from now: one whose running costs held the contribution
    // counts it twice until they are lowered (decided unattended, 2026-10-25).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Read_documentBeforeTheBand_opensEveryPlanInTheLowerBandAsDefault(int version)
    {
        PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans.Select(plan => plan.Inputs?.CompanyContributionBand)
            .ShouldAllBe(band => band == new Input<CompanyContributionBand>(CompanyContributionBand.Lower, InputOrigin.Default));
    }

    // Saved before the fund's fee was asked, the plans of versions 1 to 6 have none given, so their
    // contributions are as they were saved.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Read_documentBeforeTheFee_givesNoFundFeeRateInAnyPlan(int version)
    {
        PlanStorage.Read(StoredPlanDocuments.Of(version)).Book.Plans.Select(plan => plan.Inputs?.FundFeeRate)
            .ShouldAllBe(rate => rate == StoredPlanDocuments.NoneGiven);
    }

    private static PlanBook Book(params (string Id, string Name, RiverInputs Inputs)[] plans) =>
        new([.. plans.Select(plan => new SavedPlan(plan.Id, plan.Name, plan.Inputs, PlanStorage.Store(plan.Inputs)))], null, null);

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var document = JsonNode.Parse(json)!;
        edit(document);
        return document.ToJsonString();
    }

    // The inputs of the plan at that place in a document.
    private static JsonNode PlanInputs(string json, int index) => PlanInputs(JsonNode.Parse(json), index);

    private static JsonNode PlanInputs(JsonNode? document, int index) => Child(Child(Child(document, "plans"), index), "inputs");

    // The member of that name, or the element at that place, which the document has to hold.
    private static JsonNode Child(JsonNode? parent, string name) =>
        parent?[name] ?? throw new InvalidOperationException($"No \"{name}\" in the document.");

    private static JsonNode Child(JsonNode? parent, int index) =>
        parent?[index] ?? throw new InvalidOperationException($"No element {index} in the document.");

    // The member at a dotted path of a plan's inputs: « car.privateCost ».
    private static JsonNode Member(JsonNode inputs, string path) => path.Split('.').Aggregate(inputs, Child);

    private static void Put(JsonNode inputs, string path, JsonNode value)
    {
        var (parent, name) = Parent(inputs, path);
        parent[name] = value;
    }

    private static void Remove(JsonNode inputs, string path)
    {
        var (parent, name) = Parent(inputs, path);
        parent.AsObject().Remove(name).ShouldBeTrue();
    }

    private static (JsonNode Parent, string Name) Parent(JsonNode inputs, string path)
    {
        var names = path.Split('.');
        return (names[..^1].Aggregate(inputs, Child), names[^1]);
    }

    private static Netto.Web.Client.DirectorPay.MunicipalityChoice Namur() =>
        RiverInputs.Example.Municipality.Pick(RiverInputs.Example.Municipality.Search("Namur")[0]);
}
