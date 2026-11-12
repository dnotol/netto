using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// The Copyright royalty card on the river's example (RiverInputsTests), 100,000 of profit before the
// director and 50,000 of pay: a royalty of 10,000 stays within 30% of 60,000, one of 30,000 passes 30%
// of 80,000, 24,000, by 6,000 (CopyrightCalculatorTests). The card comes after the split, which pays
// the royalty beside the pay: it receives the royalty and drains it.
public class CopyrightViewTests
{
    // The royalty fixtures CarAndRoyaltyCardsTests holds the card to as well.
    internal static readonly RiverInputs Within = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m);

    internal static readonly RiverInputs Beyond = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

    // 100,000 before pay and royalty, a royalty that follows the pay at 75% of the working time creating
    // and a pay set at 90,000: a ruling accepts 18.75% of the 100,000 the two share, so the company pays
    // 81,250 beside a royalty of 18,750, and the pay set is held there (YearSimulation.PayCeiling).
    internal static readonly RiverInputs FollowingHeld = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m).WithDirectorPay(90_000m);

    // The same case with the royalty fixed at what the other one comes to: the pay is held at the 81,250
    // the company has left beside it.
    internal static readonly RiverInputs FixedHeld = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(18_750m).WithDirectorPay(90_000m);

    // Added without a share, the royalty follows the pay and is none: nothing to drain, and no steps to
    // list.
    [Fact]
    public void Royalty_addedWithoutAShare_hasNothingToDrain()
    {
        var view = Royalty(RiverInputs.Example.AddToPlan(OptionalModule.Copyright));

        (view.HasRoyalty, view.Details.Count, view.Drains.Count).ShouldBe((false, 0, 0));
    }

    // Its tax apart and the municipal surcharge go to the taxes on pay, the rest of the part within the
    // limits to the pocket, and nothing to Director pay.
    [Fact]
    public void Royalty_withinTheLimits_drainsItsTaxAndPassesTheRestToThePocket()
    {
        var year = Within.Run().Year.ShouldNotBeNull();
        var tax = year.DirectorPay.TaxedApartTax;

        Lines(Royalty(Within).Drains).ShouldBe(new[]
        {
            new Drain("copyright_tax", DrainKind.PaidOut, tax.IncomeTax, "taxes_on_pay"),
            new Drain("copyright_municipal_surcharge", DrainKind.PaidOut, tax.MunicipalSurcharge, "taxes_on_pay"),
            new Drain("copyright_low_earned_income_credit_lost", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("copyright_income_tax_advance_payments", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("copyright_after_tax", DrainKind.PassedOn, 10_000m - tax.Total, "pocket"),
            new Drain("copyright_taxed_as_pay", DrainKind.PassedOn, 0m, "director_pay"),
        });
    }

    // 15% of 10,000, give or take the split's cent, and the 7% surcharge on it.
    [Fact]
    public void Royalty_withinTheLimits_costsAbout16PercentInTax()
    {
        Royalty(Within).Tax.ShouldBeInRange(1_604.99m, 1_605.02m);
    }

    // With an attestation, the flat-rate costs halve what is taxed apart: the tax is about half, and all
    // the rest of the 10,000 reaches the pocket.
    [Fact]
    public void Royalty_withAnAttestation_taxesHalfAndPassesTheRestToThePocket()
    {
        var view = Royalty(Within.WithAttestation(true));

        (view.Result.FlatRateCosts, view.Tax, view.AfterTax).ShouldSatisfyAllConditions(
            figures => figures.FlatRateCosts.ShouldBe(5_000m),
            figures => figures.Tax.ShouldBeInRange(802.49m, 802.51m),
            figures => figures.AfterTax.ShouldBe(10_000m - figures.Tax));
    }

    // The part beyond the limits goes on to Director pay, taxed as pay there.
    [Fact]
    public void Royalty_beyondTheLimits_passesThatPartOnToDirectorPay()
    {
        var view = Royalty(Beyond);

        (view.Result.WithinLimits, Lines(view.Drains).Single(drain => drain.Key == "copyright_taxed_as_pay"))
            .ShouldBe((24_000m, new Drain("copyright_taxed_as_pay", DrainKind.PassedOn, 6_000m, "director_pay")));
    }

    // What it receives is the royalty the split pays; each line says its share of it.
    [Fact]
    public void Royalty_saysItsSharesOfTheRoyalty()
    {
        var view = Royalty(Beyond);

        (view.Result.Royalty, view.Drains.Single(drain => drain.Key == "copyright_taxed_as_pay").Share).ShouldBe((30_000m, (decimal?)0.2m));
    }

    // Every line takes part of the royalty: none is a remainder passed on.
    [Fact]
    public void Royalty_everyLine_saysItsShare()
    {
        Royalty(Within).Drains.ShouldAllBe(drain => drain.Share != null);
    }

    [Fact]
    public void Royalty_listsItsSteps()
    {
        Royalty(Within).Details.Select(detail => detail.RuleKey).ShouldBe([
            "copyright_royalty", "copyright_within_limits", "copyright_flat_rate_costs", "copyright_taxed_as_pay", "copyright_tax", "copyright_municipal_surcharge",
            "copyright_low_earned_income_credit_lost", "copyright_income_tax_advance_payments",
        ]);
    }

    // The tax lines and the part taxed as pay carry their steps; what reaches the pocket has none.
    [Fact]
    public void Royalty_eachLineOfAStepCarriesThatStep()
    {
        Royalty(Within).Drains.Select(drain => drain.Details.SingleOrDefault()?.RuleKey ?? "").ShouldBe([
            "copyright_tax", "copyright_municipal_surcharge", "copyright_low_earned_income_credit_lost", "copyright_income_tax_advance_payments", "",
            "copyright_taxed_as_pay",
        ]);
    }

    // The same plan without the royalty: the company keeps it, so the dividend, after corporate tax
    // and its withholding, takes its place.
    [Fact]
    public void Comparison_royalty_isTheRiverWithAndWithoutIt()
    {
        var with = Within.Run().Year.ShouldNotBeNull();
        var without = Within.TakeOutOfPlan(OptionalModule.Copyright).Run().Year.ShouldNotBeNull();

        var comparison = CopyrightComparison.From(Within, Within.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => (comparison.PocketWith, comparison.PocketWithout, comparison.Gain).ShouldBe((with.NetInPocket, without.NetInPocket, with.NetInPocket - without.NetInPocket)),
            () => (comparison.TaxesOnPayWith - comparison.TaxesOnPayWithout).ShouldBe(with.DirectorPay.TaxedApartTax.Total),
            () => comparison.TaxesOnProfitWith.ShouldBeLessThan(comparison.TaxesOnProfitWithout));
    }

    // Within its limits, 15% apart and the surcharge beat corporate tax and the dividend's 18%.
    [Fact]
    public void Comparison_royaltyWithinTheLimits_leavesMoreInThePocket()
    {
        CopyrightComparison.From(Within, Within.Run()).ShouldNotBeNull().Gain.ShouldBeGreaterThan(0m);
    }

    // The flat-rate costs add to that gain the tax apart they spare.
    [Fact]
    public void Comparison_royaltyWithAnAttestation_gainsMoreThanWithout()
    {
        var holder = Within.WithAttestation(true);

        CopyrightComparison.From(holder, holder.Run()).ShouldNotBeNull().Gain.ShouldBeGreaterThan(CopyrightComparison.From(Within, Within.Run()).ShouldNotBeNull().Gain);
    }

    [Fact]
    public void Comparison_noRoyaltyCounted_isNone()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);

        CopyrightComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    [Fact]
    public void Comparison_refusedRiver_isNone()
    {
        var inputs = Within.WithReserve(-1m);

        CopyrightComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    // Each plan's figures are the bar's buckets for it: with a car, a rent and the royalty, its tax apart
    // and the rent's in the taxes on pay, and without the royalty, the same plan as the bar draws it.
    [Fact]
    public void Comparison_carRentAndRoyalty_holdsWhatTheBarSumsForEachPlan()
    {
        var inputs = Within.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
            .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
        var with = BucketsView.From(inputs.Run()).ShouldNotBeNull();
        var without = BucketsView.From(inputs.TakeOutOfPlan(OptionalModule.Copyright).Run()).ShouldNotBeNull();

        var comparison = CopyrightComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.ShouldBe(new CopyrightComparison(
            with.AmountOf("pocket"), without.AmountOf("pocket"), with.AmountOf("taxes_on_pay"), without.AmountOf("taxes_on_pay"),
            with.AmountOf("taxes_on_profit"), without.AmountOf("taxes_on_profit")));
    }

    // A pay set above what the company can pay beside the royalty is held there in the plan with it, but
    // the river without it would pay all of it (RiverInputs.PayUsed): the comparison sets the plan without
    // the royalty at the pay the plan with it runs at, so that what differs is the royalty alone. The
    // royalty follows the pay or is fixed, the pay held either way.
    public static TheoryData<string, RiverInputs> PayHeld => new()
    {
        { "a royalty that follows the pay", FollowingHeld },
        { "a fixed royalty", FixedHeld },
    };

    [Theory]
    [MemberData(nameof(PayHeld))]
    public void Comparison_payAboveWhatTheCompanyCanPayBesideTheRoyalty_comparesTwoYearsAtThePayWithTheRoyaltyRuns(string _, RiverInputs inputs)
    {
        var with = inputs.Run().Year.ShouldNotBeNull();
        var withoutAtThePaySet = inputs.TakeOutOfPlan(OptionalModule.Copyright).Run().Year.ShouldNotBeNull();
        var without = inputs.TakeOutOfPlan(OptionalModule.Copyright).WithDirectorPay(with.Scenario.DirectorPay).Run().Year.ShouldNotBeNull();

        var comparison = CopyrightComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => (with.Scenario.DirectorPay, with.Copyright.Royalty, withoutAtThePaySet.Scenario.DirectorPay).ShouldBe((81_250m, 18_750m, 90_000m)),
            () => without.Scenario.DirectorPay.ShouldBe(with.Scenario.DirectorPay),
            () => (comparison.PocketWith, comparison.PocketWithout).ShouldBe((with.NetInPocket, without.NetInPocket)),
            () => comparison.Gain.ShouldBe(with.NetInPocket - without.NetInPocket),
            () => comparison.PocketWithout.ShouldNotBe(withoutAtThePaySet.NetInPocket),
            () => comparison.PayHeld.ShouldBe(81_250m));
    }

    // The pay set within what the company can pay beside the royalty, or exactly at it, is the pay of
    // both plans, as it was: the comparison is the river with and without the royalty, no pay held.
    public static TheoryData<string, RiverInputs> PayNotHeld => new()
    {
        { "a fixed royalty at the example's pay", Within },
        { "a royalty that follows the pay at the example's pay", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m) },
        { "a royalty that follows the pay at exactly the pay it allows", FollowingHeld.WithDirectorPay(81_250m) },
        { "a fixed royalty at exactly the pay it allows", FixedHeld.WithDirectorPay(81_250m) },
    };

    [Theory]
    [MemberData(nameof(PayNotHeld))]
    public void Comparison_payWithinWhatTheCompanyCanPayBesideTheRoyalty_isTheRiverWithAndWithoutItAndHoldsNoPay(string _, RiverInputs inputs)
    {
        var with = BucketsView.From(inputs.Run()).ShouldNotBeNull();
        var without = BucketsView.From(inputs.TakeOutOfPlan(OptionalModule.Copyright).Run()).ShouldNotBeNull();

        var comparison = CopyrightComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.ShouldBe(new CopyrightComparison(
            with.AmountOf("pocket"), without.AmountOf("pocket"), with.AmountOf("taxes_on_pay"), without.AmountOf("taxes_on_pay"),
            with.AmountOf("taxes_on_profit"), without.AmountOf("taxes_on_profit")));
    }

    // The plan without the royalty is the river's own, at the pay held: a car and a rent in the plan are in
    // both runs, and both sides are the bar's buckets for each plan.
    [Fact]
    public void Comparison_payHeldWithACarAndARent_keepsThemInBothPlans()
    {
        var inputs = FollowingHeld.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
            .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
        var year = inputs.Run().Year.ShouldNotBeNull();
        var without = inputs.TakeOutOfPlan(OptionalModule.Copyright).WithDirectorPay(year.Scenario.DirectorPay).Run();
        var bars = (With: BucketsView.From(inputs.Run()).ShouldNotBeNull(), Without: BucketsView.From(without).ShouldNotBeNull());

        var comparison = CopyrightComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => year.Scenario.DirectorPay.ShouldBeLessThan(90_000m),
            () => (without.Year.ShouldNotBeNull().Car.Costs > 0m, without.Year.Rent.Rent > 0m).ShouldBe((true, true)),
            () => comparison.ShouldBe(new CopyrightComparison(
                bars.With.AmountOf("pocket"), bars.Without.AmountOf("pocket"), bars.With.AmountOf("taxes_on_pay"), bars.Without.AmountOf("taxes_on_pay"),
                bars.With.AmountOf("taxes_on_profit"), bars.Without.AmountOf("taxes_on_profit")) { PayHeld = year.Scenario.DirectorPay }));
    }

    // With the royalty in the plan, the rent passes on to the split what the pay and the royalty share
    // there, the royalty still in it, on the map's company line (HandOff).
    [Fact]
    public void Rent_withTheRoyalty_passesWhatThePayAndTheRoyaltyShareOnToTheSplit()
    {
        var inputs = Within.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        HandOff.Drain(inputs.Run().Year.ShouldNotBeNull(), OptionalModule.Rent, inputs.Composition)
            .ShouldBe(new Drain("profit_before_pay_and_royalty", DrainKind.PassedOn, 98_620m, "split"));
    }

    private static CopyrightView Royalty(RiverInputs inputs) => CopyrightView.From(inputs.Run().Year.ShouldNotBeNull());

    // The lines without their details and shares, which other tests check.
    private static IEnumerable<Drain> Lines(IEnumerable<Drain> drains) => drains.Select(drain => drain with { Details = [], Share = null });
}
