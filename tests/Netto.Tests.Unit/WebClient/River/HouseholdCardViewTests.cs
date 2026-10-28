using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the Household card binds (HouseholdCardView): its lines in their order, the joint year's, then the
// dependants', then in a joint year what the household saves altogether; and its summary, parts each with
// its arguments formatted, which the card joins with « · »: the partner's year, how many dependants, and
// what the household saves the river's tax or costs it, the amount always as large as it is and the words
// saying which.
public class HouseholdCardViewTests
{
    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    private static readonly RiverInputs SingleParentOfTwo = WithPartner.WithPartner(false).WithChildren(2).WithSingleParent(true);

    [Fact]
    public void Lines_partnerAndChildren_areTheJointYearsThenTheDependantsThenAltogether()
    {
        var river = WithPartner.WithChildren(2);
        var run = river.Run();

        var view = HouseholdCardView.From(river, run);

        Lines(view.Lines).ShouldBe(Lines(
        [
            .. HouseholdFigures.Of(view.Joint.ShouldNotBeNull()),
            .. DependantsFigures.Of(view.Dependants.ShouldNotBeNull()),
            HouseholdFigures.Altogether(run.Pay.ShouldNotBeNull().HouseholdSaves),
        ]));
    }

    // A parent alone: what the dependants save is what the household saves, said once.
    [Fact]
    public void Lines_singleParent_areTheDependantsAlone()
    {
        var view = HouseholdCardView.From(SingleParentOfTwo, SingleParentOfTwo.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Joint.ShouldBeNull(),
            () => Lines(view.Lines).ShouldBe(Lines(DependantsFigures.Of(view.Dependants.ShouldNotBeNull()))));
    }

    [Fact]
    public void Summary_partnerWithNoIncome_isTheQuotientAndWhatTheHouseholdSaves()
    {
        var run = WithPartner.Run();

        Parts(HouseholdCardView.From(WithPartner, run)).ShouldBe(
            [("Summary_Quotient", "€11,780.00"), ("Summary_LessTax", Formats.Euros(run.Pay.ShouldNotBeNull().HouseholdSaves))]);
    }

    // The dependants counted are the people, the children under 3 among the children.
    [Fact]
    public void Summary_singleParentWithOthers_countsThePeople()
    {
        var river = SingleParentOfTwo.WithChildrenUnder3(1).WithDependantsInCare(1).WithOtherDependants(1);

        Parts(HouseholdCardView.From(river, river.Run()))[0].ShouldBe(("Summary_Dependants", "4"));
    }

    // What the household costs the river's tax reads as more tax, of the amount as large as it is.
    [Fact]
    public void Summary_householdCosting_isMoreTaxOfItsSize()
    {
        var run = WithPartner.Run();
        var costing = run with { Pay = run.Pay.ShouldNotBeNull() with { HouseholdSaves = -8.50m } };

        Parts(HouseholdCardView.From(WithPartner, costing))[^1].ShouldBe(("Summary_MoreTax", "€8.50"));
    }

    // While the engine refuses an input there is no year to read: no line, no summary.
    [Fact]
    public void From_refused_hasNoLineNorSummary()
    {
        var river = WithPartner.WithChildren(-1);

        var view = HouseholdCardView.From(river, river.Run());

        (view.Lines.Count, view.Summary.Count).ShouldBe((0, 0));
    }

    private static IReadOnlyList<(string Key, decimal Amount)> Lines(IEnumerable<FigureLine> lines) => [.. lines.Select(line => (line.Key, line.Amount))];

    private static IReadOnlyList<(string Key, string Args)> Parts(HouseholdCardView view) =>
        [.. view.Summary.Select(part => (part.Key, string.Join(' ', part.Args)))];
}
