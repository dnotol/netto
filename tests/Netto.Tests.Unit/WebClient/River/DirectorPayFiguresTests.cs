using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Director pay's figures as lines (DirectorPayFigures), on the river's example (ModuleViewsTests): 50,000
// of pay taxed on its gross alone, its tax withheld as the key formula says, which leaves a surcharge
// under the minimum. YearSimulationCarTests' electric car adds a benefit in kind of 1,714.29; a royalty
// of 30,000 beside the pay passes its limits by 6,000, taxed as pay.
public class DirectorPayFiguresTests
{
    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    private static readonly RiverInputs RoyaltyBeyondItsLimits = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

    // The pay taxed on its gross alone: the gross from the split says it all.
    [Fact]
    public void Taxed_example_drawsNoLines()
    {
        Taxed(RiverInputs.Example).ShouldBeEmpty();
    }

    [Fact]
    public void Taxed_companyCar_isItsBenefitInKindThenTheTaxablePay()
    {
        Lines(Taxed(ElectricCar)).ShouldBe([("Figure_BenefitInKind", 1_714.29m, null), ("Figure_TaxablePay", 51_714.29m, null)]);
    }

    [Fact]
    public void Taxed_royaltyBeyondItsLimits_isThePartTaxedAsPayThenTheTaxablePay()
    {
        Lines(Taxed(RoyaltyBeyondItsLimits)).ShouldBe([("Figure_RequalifiedPay", 6_000m, null), ("Figure_TaxablePay", 56_000m, null)]);
    }

    [Fact]
    public void Taxed_carAndRoyaltyBeyondItsLimits_isBothThenTheTaxablePay()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        Taxed(inputs).Select(line => line.Key).ShouldBe(["Figure_BenefitInKind", "Figure_RequalifiedPay", "Figure_TaxablePay"]);
    }

    // Why the pay is taxed on more than it pays is a rule: each line's bubble says it, the taxable pay
    // needing none.
    [Fact]
    public void Taxed_carAndRoyaltyBeyondItsLimits_explainEachPartInItsBubble()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        Taxed(inputs).Select(line => line.Help).ShouldBe(["Help_BenefitInKind", "Help_RequalifiedPay", null]);
    }

    // Nothing withheld: 486.57 on the pay's tax, among the pay's drains, and 14,317.50 by 21 December
    // brings it under the minimum.
    [Fact]
    public void Surcharge_nothingWithheld_isWhatMoreByTheLastDeadlineLeavesNoneDue()
    {
        var lines = Surcharge(RiverInputs.Example.WithTaxWithheld(0m));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Surcharge_Due", 14_317.50m, null)]),
            () => lines[0].Args.ShouldBe(["21 December 2026"]));
    }

    private static readonly Dictionary<string, RiverInputs> NoneDue = new()
    {
        ["under the minimum"] = RiverInputs.Example,
        ["covered"] = RiverInputs.Example.WithTaxWithheld(20_000m),
        ["exempt"] = RiverInputs.Example.WithStarter(true),
        ["no tax"] = RiverInputs.Example.WithDirectorPay(0m),
    };

    // With no surcharge due, the card says why in a sentence: no line.
    [Theory]
    [InlineData("under the minimum")]
    [InlineData("covered")]
    [InlineData("exempt")]
    [InlineData("no tax")]
    public void Surcharge_noneDue_drawsNoLine(string state)
    {
        Surcharge(NoneDue[state]).ShouldBeEmpty();
    }

    private static IReadOnlyList<FigureLine> Taxed(RiverInputs inputs) => DirectorPayFigures.Taxed(inputs.Run().Pay.ShouldNotBeNull());

    private static IReadOnlyList<FigureLine> Surcharge(RiverInputs inputs) =>
        DirectorPayFigures.Surcharge(DirectorPayView.From(inputs, inputs.Run().Pay.ShouldNotBeNull()));

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
