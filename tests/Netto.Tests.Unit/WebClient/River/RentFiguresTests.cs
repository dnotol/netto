using Netto.Core.Rent;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The rent's figure, line by line: what the company pays, the rent, then what it is taxed on, the rent or
// the floor when the rent is under it, less its flat charges, which is what joins the taxable income. At
// the 2026 cap, 1,380.00 for 12% of a cadastral income of 1,200, it is taxed on the rent less 552.00; at
// 400.00, under the floor, on the floor, 463.68, less 185.47 (ModuleViewsTests, RentCalculatorTests).
public class RentFiguresTests
{
    // The name IStringLocalizer<RentCard> reads River/RentCard.resx under.
    private static readonly ResxText Resources = ResxText.For<RentCard>();

    private static readonly RiverInputs RentInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Rent);

    private static readonly RiverInputs RentAtTheCap = RentInPlan.WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

    // The household in the plan, a partner declared and taxed jointly with the director: the example's.
    private static readonly RiverInputs WithAPartner = RentAtTheCap.AddToPlan(OptionalModule.Household);

    [Fact]
    public void From_rentAboveTheFloor_isWhatTheCompanyPaysThenTheRentLessItsFlatChargesJoiningTheTaxableIncome()
    {
        Lines(RentAtTheCap).ShouldBe([
            (RentFigures.CompanyPays, 1_380m), (RentFigures.TaxedOnTheRent, 1_380m), (RentFigures.FlatCharges, -552m), (RentFigures.JoinsTaxableIncome, 828m)]);
    }

    // Under the floor the company still pays the rent, which no other line says: the rent is taxed on the floor.
    [Fact]
    public void From_rentUnderTheFloor_isWhatTheCompanyPaysThenTheFloorLessItsFlatChargesJoiningTheTaxableIncome()
    {
        Lines(RentAtTheCap.WithRent(400m)).ShouldBe([
            (RentFigures.CompanyPays, 400m), (RentFigures.TaxedOnTheFloor, 463.68m), (RentFigures.FlatCharges, -185.47m), (RentFigures.JoinsTaxableIncome, 278.21m)]);
    }

    // Under the floor what joins the taxable income can exceed the rent paid: a cadastral income of 500
    // wholly let, a rent of 600, is taxed on the floor of 1,610.00 (500 x 2.3 x 1.4), less 644.00.
    [Fact]
    public void From_rentUnderTheFloorByEnough_joinsMoreThanTheRent()
    {
        Lines(RentInPlan.WithCadastralIncome(500m).WithShareLet(1m).WithRent(600m)).ShouldBe([
            (RentFigures.CompanyPays, 600m), (RentFigures.TaxedOnTheFloor, 1_610m), (RentFigures.FlatCharges, -644m), (RentFigures.JoinsTaxableIncome, 966m)]);
    }

    // The flat charges' bubble says what they are, and the line the net rent joins the taxable income on,
    // with a partner taxed jointly, that it is all taxed in the director's column.
    [Fact]
    public void From_rentAboveTheFloor_explainsTheFlatChargesAndWhereTheNetRentIsTaxed()
    {
        Helps(WithAPartner).ShouldBe([null, null, RentFigures.FlatChargesHelp, RentFigures.JoinsTaxableIncomeHelp]);
    }

    // So does the floor's, under it.
    [Fact]
    public void From_rentUnderTheFloor_explainsTheFloorAndTheFlatCharges()
    {
        Helps(WithAPartner.WithRent(400m)).ShouldBe([null, RentFigures.FloorHelp, RentFigures.FlatChargesHelp, RentFigures.JoinsTaxableIncomeHelp]);
    }

    // With no household in the plan, alone, or with a partner taxed separately, the whole net rent is the director's whatever the law says
    // of a shared home: nothing to explain.
    [Fact]
    public void From_noPartnerTaxedJointly_doesNotExplainWhereTheNetRentIsTaxed()
    {
        RiverInputs[] taxedAlone = [RentAtTheCap, WithAPartner.WithPartner(false), WithAPartner.WithTaxedSeparately(true)];

        foreach (var inputs in taxedAlone)
            Helps(inputs).ShouldBe([null, null, RentFigures.FlatChargesHelp, null]);
    }

    // The bubbles' amounts are the year's rules': under the 2025 rules the floor is indexed by 2.2446 and
    // the flat charges limited by the cadastral income revalued by 5.63.
    [Fact]
    public void From_ruleSetOfAYear_fillsTheBubblesFromIt()
    {
        var view = RentView.From(RentAtTheCap.WithRent(400m).Run().Year.ShouldNotBeNull(), RentAtTheCap.Composition) with { Rules = RentRules.IncomeYear2025 };

        var lines = RentFigures.From(view);

        lines.ShouldSatisfyAllConditions(
            () => lines.Single(line => line.Help == RentFigures.FloorHelp).HelpArgs.ShouldBe(["2.2446", "40%"]),
            () => lines.Single(line => line.Help == RentFigures.FlatChargesHelp).HelpArgs.ShouldBe(["40%", "2/3", "5.63"]));
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
        new[] { RentFigures.CompanyPays, RentFigures.TaxedOnTheRent, RentFigures.TaxedOnTheFloor, RentFigures.FlatCharges, RentFigures.JoinsTaxableIncome, RentFigures.FloorHelp, RentFigures.FlatChargesHelp, RentFigures.JoinsTaxableIncomeHelp }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    private static IReadOnlyList<FigureLine> Figures(RiverInputs inputs) => RentFigures.From(RentView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition));

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) => [.. Figures(inputs).Select(line => (line.Key, line.Amount))];

    private static List<string?> Helps(RiverInputs inputs) => [.. Figures(inputs).Select(line => line.Help)];
}
