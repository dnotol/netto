using Netto.Core.CompanyCar;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The links the map draws, from the cards' own lines of one run of the river: what each module passes on
// to the next, what ends in a bucket, and what only changes a tax base; a line of
// nothing is no link, and a refused amount leaves none. That each one is the very line its card draws is
// MapCardsTests'.
public class MapLinksTests
{
    // Every lever in the plan, each with figures: vouchers of every kind, the internet and computer
    // allowances (an office allowance beside a rent is refused), a rent at the cap, a petrol car whose costs are not all deducted, its private fuel paid by the company
    // and a contribution from the director, and a royalty of 30,000, beyond its limits.
    internal static readonly RiverInputs Full = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .WithInternetAllowance(240m).WithComputerAllowance(240m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(45_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5)).WithPrivateEnergyPaid(true).WithCarContribution(600m)
        .WithRoyalty(30_000m);

    private static readonly RiverInputs EveryLever = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    // The example: Company year hands the split what the company earns before it pays its director, the
    // figure its summary says before the pay.
    [Fact]
    public void Of_theExample_handsCompanyYearsProfitBeforeThePayToTheSplit()
    {
        var run = RiverInputs.Example.Run();

        var link = Between(MapLinks.Of(RiverInputs.Example, run), RiverModule.CompanyYear, RiverModule.Split).ShouldHaveSingleItem();

        (link.Kind, link.Card, link.Carries.Text, link.Carries.Key, link.Carries.Args.Count, link.Amount)
            .ShouldBe((MapLinkKind.PassedOn, RiverModule.CompanyYear, MapText.Map, MapTabText.ProfitBeforeLevers, 0, run.Year!.ProfitBeforeLevers));
    }

    // Every lever in the plan: the chain runs from Company year through each lever to the split, each
    // passing on what its card says it leaves.
    [Fact]
    public void Of_everyLever_chainsCompanyYearThroughEachLeverToTheSplit()
    {
        var links = MapLinks.Of(Full, Full.Run());

        links.Where(link => link.Kind == MapLinkKind.PassedOn && link.From.Module is { } from && Chain.Contains(from))
            .Select(link => (link.From.Module, link.To.Module))
            .ShouldBe([
                (RiverModule.CompanyYear, RiverModule.Vouchers), (RiverModule.Vouchers, RiverModule.Allowances), (RiverModule.Allowances, RiverModule.Rent),
                (RiverModule.Rent, RiverModule.Car), (RiverModule.Car, RiverModule.Split)]);
    }

    [Fact]
    public void Of_aLever_handsOnWhatItsCardSaysItLeaves()
    {
        var run = Full.Run();

        var link = Between(MapLinks.Of(Full, run), RiverModule.Vouchers, RiverModule.Allowances).ShouldHaveSingleItem();

        (link.Carries.Text, link.Carries.Key, link.Amount).ShouldBe((MapText.DrainList, "Drain_left_after_vouchers", VoucherView.From(run.Year!, Full.Composition).Left));
    }

    // The split shares what it receives between the pay, the royalty and the company: a line each of its
    // card, each on to the module that takes it.
    [Fact]
    public void Of_theSplit_passesThePayTheRoyaltyAndTheCompanysProfitOn()
    {
        var year = Full.Run().Year!;

        var links = MapLinks.Of(Full, Full.Run()).Where(link => link.From.Module == RiverModule.Split);

        links.Select(link => (link.Kind, link.To.Module, link.Carries.Text, link.Carries.Key, link.Amount)).ShouldBe([
            (MapLinkKind.PassedOn, RiverModule.DirectorPay, MapText.SplitCard, "Figure_Pay", year.DirectorPay.GrossPay),
            (MapLinkKind.PassedOn, RiverModule.Copyright, MapText.SplitCard, "Figure_Royalty", year.Copyright.Royalty),
            (MapLinkKind.PassedOn, RiverModule.CorporateTax, MapText.SplitCard, "Figure_Company", year.ProfitAfterPay)]);
    }

    // The contributions the company pays on the director's behalf, with the fund's fee, are pay too: the
    // split's lines for them go to Director pay beside the pay.
    [Fact]
    public void Of_theCompanyPayingTheContributions_passesThemOnToDirectorPayWithThePay()
    {
        var river = RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.035m);

        var links = Between(MapLinks.Of(river, river.Run()), RiverModule.Split, RiverModule.DirectorPay);

        links.Select(link => link.Carries.Key).ShouldBe(["Figure_Pay", "Figure_CompanyContributions", "Figure_CompanyFundFee"]);
    }

    // A royalty's part beyond its limits is pay paid in cash besides the payslip: money passed on to
    // Director pay, whose net in pocket holds it.
    [Fact]
    public void Of_aRoyaltyBeyondItsLimits_passesThatPartOnToDirectorPay()
    {
        var year = Full.Run().Year!;
        year.Copyright.TaxedAsPay.ShouldBeGreaterThan(0m);

        var link = Between(MapLinks.Of(Full, Full.Run()), RiverModule.Copyright, RiverModule.DirectorPay).ShouldHaveSingleItem();

        (link.Kind, link.Carries.Key, link.Amount).ShouldBe((MapLinkKind.PassedOn, "Drain_copyright_taxed_as_pay", year.Copyright.TaxedAsPay));
    }

    // Each card's lines into a bucket: the pay's contributions and taxes into the taxes on pay, its net
    // into the pocket.
    [Fact]
    public void Of_directorPaysLines_endInTheirBuckets()
    {
        var links = MapLinks.Of(RiverInputs.Example, RiverInputs.Example.Run()).Where(link => link.From.Module == RiverModule.DirectorPay).ToList();

        links.ShouldSatisfyAllConditions(
            () => links.ShouldAllBe(link => link.Kind == MapLinkKind.IntoBucket && link.Carries.Text == MapText.DrainList),
            () => links.Where(link => link.To == MapEnd.InBucket("pocket")).Select(link => link.Carries.Key).ShouldBe(["Drain_net_pay"]),
            () => links.Where(link => link.To == MapEnd.InBucket("taxes_on_pay")).Select(link => link.Carries.Key).ShouldContain("Drain_social_contributions"));
    }

    // What comes back runs from its bucket: the director's contribution to the car from the pocket, the
    // withholding refunded on the exempt dividend from the taxes on profit.
    [Theory]
    [InlineData("Drain_car_contribution", "pocket", RiverModule.Car)]
    [InlineData("Drain_dividend_exemption_refund", "taxes_on_profit", RiverModule.Dividend)]
    public void Of_aLineComingBack_runsFromItsBucketIntoItsModule(string key, string bucket, RiverModule module)
    {
        var link = MapLinks.Of(Full, Full.Run()).Single(link => link.Carries.Key == key);

        (link.Kind, link.From, link.To, link.Card).ShouldBe((MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), MapEnd.Of(module), module));
    }

    // The car's costs are a cost of the company: they end in its expenses, as its running costs do.
    [Fact]
    public void Of_theCarsCosts_endInTheExpenses()
    {
        var link = MapLinks.Of(Full, Full.Run()).Single(link => link.Carries.Key == "Drain_car_costs");

        (link.Kind, link.From, link.To, link.Amount).ShouldBe((MapLinkKind.IntoBucket, MapEnd.Of(RiverModule.Car), MapEnd.InBucket("expenses"), Full.Run().Year!.Car.Costs));
    }

    // INASTI's company contribution is an expense of the company: Company year's line into the expenses,
    // in the words and at the amount its card's figure line says it.
    [Fact]
    public void Of_theCompanyContribution_runsFromCompanyYearIntoTheExpenses()
    {
        var run = RiverInputs.Example.Run();

        var link = MapLinks.Of(RiverInputs.Example, run).Single(link => link.To == MapEnd.InBucket("expenses"));

        (link.Kind, link.From, link.Card, link.Carries.Text, link.Carries.Key, link.Amount)
            .ShouldBe((MapLinkKind.IntoBucket, MapEnd.Of(RiverModule.CompanyYear), RiverModule.CompanyYear, MapText.CompanyYearCard, CompanyYearFigures.Contribution, run.Year!.CompanyContribution));
    }

    // A company exempt from the contribution owes nothing: Company year has no line into the expenses.
    [Fact]
    public void Of_aCompanyExemptFromTheContribution_drawsNoLineFromCompanyYearIntoTheExpenses()
    {
        var river = RiverInputs.Example.WithCompanyContributionBand(CompanyContributionBand.Exempt);

        MapLinks.Of(river, river.Run()).Where(link => link.From == MapEnd.Of(RiverModule.CompanyYear) && link.To.Bucket is not null).ShouldBeEmpty();
    }

    // What only changes a tax base: what the company cannot deduct of the vouchers, added back by
    // Corporate tax; the car's benefit, taxed as the director's pay; what the company cannot deduct of
    // the car's costs, and the share of its benefit it adds back, both added back by Corporate tax; the
    // marital quotient, from the director's income to their partner with none of their own, the cap of 11,780.
    [Fact]
    public void Of_whatOnlyChangesATaxBase_runsToTheModuleThatTaxesIt()
    {
        var run = Full.Run();
        var car = run.Year!.Car;

        var links = MapLinks.Of(Full, run).Where(link => link.Kind == MapLinkKind.TaxBase);

        links.Select(link => (link.From.Module, link.To.Module, link.Carries.Text, link.Carries.Key, link.Amount)).ShouldBe([
            (RiverModule.Vouchers, RiverModule.CorporateTax, MapText.VoucherCard, VoucherFigures.AddedBack, run.Year.Vouchers.Disallowed),
            (RiverModule.Car, RiverModule.DirectorPay, MapText.CarCard, CarFigures.BenefitTaxed, car.Benefit),
            (RiverModule.Car, RiverModule.CorporateTax, MapText.CarCard, CarFigures.NotDeductible, car.NotDeductibleCosts),
            (RiverModule.Car, RiverModule.CorporateTax, MapText.CarCard, CarFigures.AddBack, car.AddBack),
            (RiverModule.DirectorPay, RiverModule.Household, MapText.HouseholdCard, HouseholdFigures.QuotientToPartner, 11_780m)]);
    }

    // The share of the car's benefit the company adds back is said at the rate its rules set, as the card
    // says it.
    [Fact]
    public void Of_theCarsAddBack_carriesTheRateItsCardStates()
    {
        var run = Full.Run();

        var link = MapLinks.Of(Full, run).Single(link => link.Carries.Key == CarFigures.AddBack);

        link.Carries.Args.ShouldBe(CarFigures.Benefit(run.Year!.Car).Single(line => line.Key == CarFigures.AddBack).Args);
    }

    // A line of nothing moves nothing, and is no link: the eco vouchers left at nothing, the contributions
    // the director pays himself.
    [Fact]
    public void Of_aLineOfNothing_isNoLink()
    {
        var river = Full.WithEcoVouchers(0m);

        var keys = MapLinks.Of(river, river.Run()).Select(link => link.Carries.Key).ToList();

        (keys.Contains("Drain_eco_vouchers"), keys.Contains("Drain_meal_vouchers"), keys.Contains("Figure_CompanyContributions")).ShouldBe((false, true, false));
    }

    // Nothing the map draws reads €0.00, a cent's fraction included.
    [Theory]
    [MemberData(nameof(MapModulesTests.Compositions), MemberType = typeof(MapModulesTests))]
    public void Of_anyComposition_drawsNoAmountOfNothing(int bits)
    {
        var river = Full with { Composition = MapModulesTests.Composition(bits) };

        MapLinks.Of(river, river.Run()).Where(link => Math.Round(link.Amount ?? 0m, 2, MidpointRounding.AwayFromZero) == 0m).ShouldBeEmpty();
    }

    // Each link joins two of the map's modules, or one and a bucket.
    [Theory]
    [MemberData(nameof(MapModulesTests.Compositions), MemberType = typeof(MapModulesTests))]
    public void Of_anyComposition_joinsOnlyModulesTheMapDraws(int bits)
    {
        var river = Full with { Composition = MapModulesTests.Composition(bits) };
        var modules = MapView.ModulesOf(river);

        MapLinks.Of(river, river.Run()).SelectMany(link => new[] { link.From, link.To })
            .Where(end => end.Module is { } module && !modules.Contains(module)).ShouldBeEmpty();
    }

    // With the company skipped, the river starts at the pay: the map draws Director pay's lines, and the
    // partner's marital quotient, the pay alone taxed jointly.
    [Fact]
    public void Of_companySkipped_isDirectorPaysLinesAndThePartnersQuotient()
    {
        var river = EveryLever.SkipCompany();

        var links = MapLinks.Of(river, river.Run());

        links.ShouldSatisfyAllConditions(
            () => links.Where(link => link.Card == RiverModule.DirectorPay).ShouldNotBeEmpty(),
            () => links.Where(link => link.Card == RiverModule.DirectorPay).ShouldAllBe(link => link.Kind == MapLinkKind.IntoBucket),
            () => links.Where(link => link.Card != RiverModule.DirectorPay).Select(link => (link.Card, link.Kind, link.From.Module, link.To.Module))
                .ShouldBe([(RiverModule.Household, MapLinkKind.TaxBase, RiverModule.DirectorPay, RiverModule.Household)]));
    }

    // While the partner attributes part of theirs to the director, the quotient runs from the partner to
    // Director pay, the amount the card adds to the director's income.
    [Fact]
    public void Of_partnerEarningFarMore_runsTheQuotientFromThePartnerToDirectorPay()
    {
        var river = RiverInputs.Example.AddToPlan(OptionalModule.Household).WithDirectorPay(12_000m).WithPartnerIncome(60_000m);
        var run = river.Run();
        var quotient = HouseholdView.From(run.Pay!, bothAged66: false)!.Director.Quotient;

        MapLinks.Of(river, run).Where(link => link.Card == RiverModule.Household).Select(link => (link.From.Module, link.To.Module, link.Carries.Key, link.Amount))
            .ShouldBe([(RiverModule.Household, RiverModule.DirectorPay, HouseholdFigures.QuotientFromPartner, quotient)]);
    }

    // Out of the plan, or in a year taxed separately, the partner moves no tax base: no line.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Of_partnerOutOfThePlanOrTaxedSeparately_drawsNoQuotient(bool inPlan)
    {
        var river = inPlan ? RiverInputs.Example.AddToPlan(OptionalModule.Household).WithTaxedSeparately(true) : RiverInputs.Example;

        MapLinks.Of(river, river.Run()).ShouldNotContain(link => link.Card == RiverModule.Household);
    }

    // A refused amount leaves no figure: no link, as the bar has no amount.
    [Fact]
    public void Of_aRefusedAmount_isNoLink()
    {
        var river = Full.WithReserve(-1m);

        MapLinks.Of(river, river.Run()).ShouldBeEmpty();
    }

    private static readonly RiverModule[] Chain =
        [RiverModule.CompanyYear, RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car];

    private static IEnumerable<MapLink> Between(IEnumerable<MapLink> links, RiverModule from, RiverModule to) =>
        links.Where(link => link.From == MapEnd.Of(from) && link.To == MapEnd.Of(to));
}
