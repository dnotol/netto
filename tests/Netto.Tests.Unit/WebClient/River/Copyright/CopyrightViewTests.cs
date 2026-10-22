using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// The Copyright royalty card on the river's example (RiverInputsTests), 100,000 of profit before the
// director and 50,000 of pay: a royalty of 10,000 stays within 30% of 60,000, one of 30,000 passes 30%
// of 80,000, 24,000, by 6,000 (CopyrightCalculatorTests).
public class CopyrightViewTests
{
    private static readonly RiverInputs Within = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m);

    private static readonly RiverInputs Beyond = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

    // Added with nothing: no royalty yet, so the profit goes on whole to the split, and there are no
    // steps to list.
    [Fact]
    public void Royalty_addedWithNothing_passesTheProfitOnToTheSplit()
    {
        var view = Royalty(RiverInputs.Example.AddToPlan(OptionalModule.Copyright));

        (view.HasRoyalty, view.Details.Count, view.ToYou, view.Drains.Single())
            .ShouldBe((false, 0, (decimal?)null, new Drain("profit_before_pay", DrainKind.PassedOn, 100_000m, "split")));
    }

    // Its tax apart and the municipal surcharge go to the taxes on pay, the rest of the part within the
    // limits to the pocket, nothing to Director pay, and the profit before pay, 90,000, on to the split.
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
            new Drain("profit_before_pay", DrainKind.PassedOn, 90_000m, "split"),
        });
    }

    // 15% of 10,000, give or take the split's cent, and the 7% surcharge on it.
    [Fact]
    public void Royalty_withinTheLimits_costsAbout16PercentInTax()
    {
        Royalty(Within).Tax.ShouldBeInRange(1_604.99m, 1_605.02m);
    }

    // The part beyond the limits goes on to Director pay, taxed as pay there.
    [Fact]
    public void Royalty_beyondTheLimits_passesThatPartOnToDirectorPay()
    {
        var view = Royalty(Beyond);

        (view.Result.WithinLimits, Lines(view.Drains).Single(drain => drain.Key == "copyright_taxed_as_pay"))
            .ShouldBe((24_000m, new Drain("copyright_taxed_as_pay", DrainKind.PassedOn, 6_000m, "director_pay")));
    }

    // What it receives is what the levers above leave of what the company earns; each line says its
    // share of the royalty, and the card what reaches the pocket of it from here.
    [Fact]
    public void Royalty_saysItsSharesOfTheRoyalty()
    {
        var view = Royalty(Beyond);

        (view.Received, view.Drains.Single(drain => drain.Key == "copyright_taxed_as_pay").Share, view.ToYou)
            .ShouldBe((100_000m, (decimal?)0.2m, Netto.Web.Client.Shares.Of(view.AfterTax, 30_000m)));
    }

    // The remainder passed on to the split says no share.
    [Fact]
    public void Royalty_profitPassedOn_saysNoShare()
    {
        Royalty(Within).Drains[^1].Share.ShouldBeNull();
    }

    [Fact]
    public void Royalty_listsItsSteps()
    {
        Royalty(Within).Details.Select(detail => detail.RuleKey).ShouldBe([
            "copyright_royalty", "copyright_within_limits", "copyright_taxed_as_pay", "copyright_tax", "copyright_municipal_surcharge",
            "copyright_low_earned_income_credit_lost", "copyright_income_tax_advance_payments",
        ]);
    }

    // The tax lines and the part taxed as pay carry their steps; what reaches the pocket and the profit
    // passed on have none.
    [Fact]
    public void Royalty_eachLineOfAStepCarriesThatStep()
    {
        Royalty(Within).Drains.Select(drain => drain.Details.SingleOrDefault()?.RuleKey ?? "").ShouldBe([
            "copyright_tax", "copyright_municipal_surcharge", "copyright_low_earned_income_credit_lost", "copyright_income_tax_advance_payments", "",
            "copyright_taxed_as_pay", "",
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

    // The rent passes what it leaves on to the royalty once it is in the plan, the royalty still in it.
    [Fact]
    public void Rent_withTheRoyalty_passesWhatItLeavesOnToTheRoyalty()
    {
        var inputs = Within.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        RentView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition).Drains[^1]
            .ShouldBe(new Drain("left_after_rent", DrainKind.PassedOn, 98_620m, "copyright"));
    }

    private static CopyrightView Royalty(RiverInputs inputs) => CopyrightView.From(inputs.Run().Year.ShouldNotBeNull());

    // The lines without their details and shares, which other tests check.
    private static IEnumerable<Drain> Lines(IEnumerable<Drain> drains) => drains.Select(drain => drain with { Details = [], Share = null });
}
