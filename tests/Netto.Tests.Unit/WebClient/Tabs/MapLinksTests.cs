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

    // Every lever in the plan: what the company has left runs down its own line, from Company year past each
    // lever to the split, each stretch after a lever what its card says it leaves, and its card's.
    [Fact]
    public void Of_everyLever_runsTheCompanysLineFromCompanyYearPastEachLeverToTheSplit()
    {
        var links = MapLinks.Of(Full, Full.Run());

        CompanysLine(links).Select(link => (link.From.Id, link.To.Id, link.Card, link.Carries.Key)).ShouldBe([
            ("company-year", "company-line-vouchers", RiverModule.CompanyYear, MapTabText.ProfitBeforeLevers),
            ("company-line-vouchers", "company-line-allowances", RiverModule.Vouchers, "Drain_left_after_vouchers"),
            ("company-line-allowances", "company-line-rent", RiverModule.Allowances, "Drain_left_after_allowances"),
            ("company-line-rent", "company-line-car", RiverModule.Rent, "Drain_left_after_rent"),
            ("company-line-car", "split", RiverModule.Car, "Drain_profit_before_pay_and_royalty")]);
    }

    [Fact]
    public void Of_aLever_leavesOnTheCompanysLineWhatItsCardSaysItLeaves()
    {
        var run = Full.Run();

        var link = Between(MapLinks.Of(Full, run), MapEnd.Beside(RiverModule.Vouchers), MapEnd.Beside(RiverModule.Allowances)).ShouldHaveSingleItem();

        (link.Kind, link.Carries.Text, link.Carries.Key, link.Amount)
            .ShouldBe((MapLinkKind.PassedOn, MapText.DrainList, "Drain_left_after_vouchers", VoucherView.From(run.Year!, Full.Composition).Left));
    }

    // Each lever but the car takes its share off the company's line across to its box: what its card says the
    // company pays, in its words.
    public static TheoryData<RiverModule, string, string> Shares => new()
    {
        { RiverModule.Vouchers, nameof(MapText.VoucherCard), VoucherFigures.CompanyPays },
        { RiverModule.Allowances, nameof(MapText.AllowanceCard), AllowanceFigures.CompanyPays },
        { RiverModule.Rent, nameof(MapText.RentCard), RentFigures.CompanyPays },
    };

    [Theory]
    [MemberData(nameof(Shares))]
    public void Of_aLever_takesWhatItsCardSaysTheCompanyPaysOffTheCompanysLine(RiverModule lever, string text, string key)
    {
        var year = Full.Run().Year!;

        var link = Between(MapLinks.Of(Full, Full.Run()), MapEnd.Beside(lever), MapEnd.Of(lever)).ShouldHaveSingleItem();

        (link.Kind, link.Card, link.Carries.Text, link.Carries.Key, link.Amount)
            .ShouldBe((MapLinkKind.PassedOn, lever, Enum.Parse<MapText>(text), key, HandOff.Cost(year, RiverModules.Lever(lever)!.Value)));
    }

    // Under its floor the rent is taxed on the floor, but the company pays the rent: the share is the rent.
    [Fact]
    public void Of_aRentUnderItsFloor_takesTheRentOffTheCompanysLine()
    {
        var river = MapRendering.River("a rent under its floor");

        Between(MapLinks.Of(river, river.Run()), MapEnd.Beside(RiverModule.Rent), MapEnd.Of(RiverModule.Rent)).ShouldHaveSingleItem().Amount.ShouldBe(1m);
    }

    // What the company has left after a lever is what it had before less the lever's share; the car takes none,
    // its costs leaving the line into the expenses and its contribution joining it from the pocket.
    [Fact]
    public void Of_theCompanysLine_losesEachSharePastItsLeverAndTheCarsCostsLessItsContribution()
    {
        var links = MapLinks.Of(Full, Full.Run());
        var line = CompanysLine(links).ToList();
        var car = MapEnd.Beside(RiverModule.Car);

        line.ShouldSatisfyAllConditions(
            () => new[] { RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent }
                .Where(lever => Into(line, MapEnd.Beside(lever)) - Between(links, MapEnd.Beside(lever), MapEnd.Of(lever)).Single().Amount != Out(line, MapEnd.Beside(lever)))
                .ShouldBeEmpty(),
            () => (Into(line, car) - links.Single(link => link.From == car && link.To == MapEnd.InBucket("expenses")).Amount
                + links.Single(link => link.From == MapEnd.InBucket("pocket") && link.To == car).Amount).ShouldBe(Out(line, car)),
            () => links.ShouldNotContain(link => link.From == car && link.To == MapEnd.Of(RiverModule.Car)));
    }

    // A lever out of the plan leaves no point on the company's line: Company year's line runs to the first in it.
    [Fact]
    public void Of_theRentAloneInThePlan_runsCompanyYearsLinePastTheRentToTheSplit()
    {
        var river = RiverInputs.Example.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        CompanysLine(MapLinks.Of(river, river.Run())).Select(link => (link.From.Id, link.To.Id)).ShouldBe([("company-year", "company-line-rent"), ("company-line-rent", "split")]);
    }

    // The split shares what it receives between the pay, the royalty and the company: a line each of its
    // card, each on to the module that takes it.
    [Fact]
    public void Of_theSplit_passesThePayTheRoyaltyAndTheCompanysProfitOn()
    {
        var year = Full.Run().Year!;

        var links = MapLinks.Of(Full, Full.Run()).Where(link => link.From.Module == RiverModule.Split);

        links.Select(link => (link.Kind, link.To.Module, link.Carries.Text, link.Carries.Key, link.Amount)).ShouldBe([
            (MapLinkKind.PassedOn, RiverModule.DirectorPay, MapText.SharedCard, "Figure_Pay", year.DirectorPay.GrossPay),
            (MapLinkKind.PassedOn, RiverModule.Copyright, MapText.SharedCard, "Figure_Royalty", year.Copyright.Royalty),
            (MapLinkKind.PassedOn, RiverModule.CorporateTax, MapText.SharedCard, "Figure_Company", year.ProfitAfterPay)]);
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

    // What comes back runs from its bucket: the withholding refunded on the exempt dividend from the taxes on
    // profit.
    [Fact]
    public void Of_aLineComingBack_runsFromItsBucketIntoItsModule()
    {
        var link = MapLinks.Of(Full, Full.Run()).Single(link => link.Carries.Key == "Drain_dividend_exemption_refund");

        (link.Kind, link.From, link.To, link.Card).ShouldBe((MapLinkKind.IntoBucket, MapEnd.InBucket("taxes_on_profit"), MapEnd.Of(RiverModule.Dividend), RiverModule.Dividend));
    }

    // The director's contribution to the car is paid to the company: it joins the company's line beside the
    // car, from the pocket.
    [Fact]
    public void Of_theCarsContribution_joinsTheCompanysLineFromThePocket()
    {
        var link = MapLinks.Of(Full, Full.Run()).Single(link => link.Carries.Key == "Drain_car_contribution");

        (link.Kind, link.From, link.To, link.Card, link.Amount)
            .ShouldBe((MapLinkKind.IntoBucket, MapEnd.InBucket("pocket"), MapEnd.Beside(RiverModule.Car), RiverModule.Car, Full.Run().Year!.Car.Contribution));
    }

    // The car's costs are a cost of the company: they leave the company's line beside the car into its
    // expenses, as its running costs do.
    [Fact]
    public void Of_theCarsCosts_leaveTheCompanysLineIntoTheExpenses()
    {
        var link = MapLinks.Of(Full, Full.Run()).Single(link => link.Carries.Key == "Drain_car_costs");

        (link.Kind, link.From, link.To, link.Amount).ShouldBe((MapLinkKind.IntoBucket, MapEnd.Beside(RiverModule.Car), MapEnd.InBucket("expenses"), Full.Run().Year!.Car.Costs));
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
            .Where(end => (end.Module ?? end.Lever) is { } module && !modules.Contains(module)).ShouldBeEmpty();
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

    private static IEnumerable<MapLink> Between(IEnumerable<MapLink> links, RiverModule from, RiverModule to) => Between(links, MapEnd.Of(from), MapEnd.Of(to));

    private static IEnumerable<MapLink> Between(IEnumerable<MapLink> links, MapEnd from, MapEnd to) => links.Where(link => link.From == from && link.To == to);

    // The stretches of the company's line, top to bottom: from Company year or a point of it, to a point of
    // it or the split.
    internal static IEnumerable<MapLink> CompanysLine(IEnumerable<MapLink> links) =>
        links.Where(link => link.Kind == MapLinkKind.PassedOn && (link.From == MapEnd.Of(RiverModule.CompanyYear) || link.From.Lever is not null)
            && (link.To == MapEnd.Of(RiverModule.Split) || link.To.Lever is not null));

    // What the company's line carries into a point of it, and on out of it.
    private static decimal? Into(IEnumerable<MapLink> line, MapEnd point) => line.Single(link => link.To == point).Amount;

    private static decimal? Out(IEnumerable<MapLink> line, MapEnd point) => line.Single(link => link.From == point).Amount;
}
