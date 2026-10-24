using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The rent's figure, line by line: what it is taxed on, the rent or the floor when the rent is under it,
// less its flat charges, which is what joins the taxable income. At the 2026 cap, 1,380.00 for 12% of a
// cadastral income of 1,200, it is taxed on the rent less 552.00; at 400.00, under the floor, on the
// floor, 463.68, less 185.47 (ModuleViewsTests, RentCalculatorTests).
public class RentFiguresTests
{
    // The name IStringLocalizer<RentCard> reads River/RentCard.resx under.
    private static readonly ResxText Resources = ResxText.For<RentCard>();

    private static readonly RiverInputs RentInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Rent);

    private static readonly RiverInputs RentAtTheCap = RentInPlan.WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

    [Fact]
    public void From_rentAboveTheFloor_isTheRentLessItsFlatChargesJoiningTheTaxableIncome()
    {
        Lines(RentAtTheCap).ShouldBe([(RentFigures.TaxedOnTheRent, 1_380m), (RentFigures.FlatCharges, -552m), (RentFigures.JoinsTaxableIncome, 828m)]);
    }

    [Fact]
    public void From_rentUnderTheFloor_isTheFloorLessItsFlatChargesJoiningTheTaxableIncome()
    {
        Lines(RentAtTheCap.WithRent(400m))
            .ShouldBe([(RentFigures.TaxedOnTheFloor, 463.68m), (RentFigures.FlatCharges, -185.47m), (RentFigures.JoinsTaxableIncome, 278.21m)]);
    }

    // The flat charges' bubble says what they are.
    [Fact]
    public void From_rentAboveTheFloor_explainsTheFlatCharges()
    {
        Helps(RentAtTheCap).ShouldBe([null, RentFigures.FlatChargesHelp, null]);
    }

    // So does the floor's, under it.
    [Fact]
    public void From_rentUnderTheFloor_explainsTheFloorAndTheFlatCharges()
    {
        Helps(RentAtTheCap.WithRent(400m)).ShouldBe([RentFigures.FloorHelp, RentFigures.FlatChargesHelp, null]);
    }

    // No rent paid, nothing taxed: no line of nothing.
    [Fact]
    public void From_noRent_isNoLine()
    {
        Lines(RentInPlan).ShouldBeEmpty();
    }

    [Fact]
    public void RentCardResx_everyLineAndItsBubble_haveTheirTexts()
    {
        new[] { RentFigures.TaxedOnTheRent, RentFigures.TaxedOnTheFloor, RentFigures.FlatCharges, RentFigures.JoinsTaxableIncome, RentFigures.FloorHelp, RentFigures.FlatChargesHelp }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    private static IReadOnlyList<FigureLine> Figures(RiverInputs inputs) => RentFigures.From(RentView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition));

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) => [.. Figures(inputs).Select(line => (line.Key, line.Amount))];

    private static List<string?> Helps(RiverInputs inputs) => [.. Figures(inputs).Select(line => line.Help)];
}
