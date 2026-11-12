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
    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household).WithPartner(true);

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

    // The card cites the rules behind the amounts its bubbles state. A parent alone has no joint year, yet
    // the supplements' amounts are still stated, so the income tax step is cited; the credit for children
    // gives nothing at this pay, so no line of it states its cap and its step is not listed.
    [Fact]
    public void Sources_singleParent_areTheIncomeTaxStepWithTheSupplementsAmounts()
    {
        var view = HouseholdCardView.From(SingleParentOfTwo, SingleParentOfTwo.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Joint.ShouldBeNull(),
            () => view.Sources.Select(source => source.RuleKey).ShouldBe(["income_tax"]),
            () => view.Sources[0].Sources.SelectMany(group => group.Amounts).Select(amount => amount.Value).ShouldContain(5_130m));
    }

    // On a low pay the credit for children is paid and its line states its cap: its step is cited too. The
    // dependants' supplements still state their amounts when the income tax comes to nothing: its step stays
    // for them, with no amount of nothing beside it.
    [Fact]
    public void Sources_singleParentOnALowPay_areTheIncomeTaxWithoutItsNothingAndChildCreditSteps()
    {
        var river = SingleParentOfTwo.SkipCompany().WithDirectorPay(20_000m);

        var sources = HouseholdCardView.From(river, river.Run()).Sources;

        sources.Select(source => (source.RuleKey, source.WithoutAmount)).ShouldBe([("income_tax", true), ("child_tax_credit", false)]);
    }

    [Fact]
    public void Sources_partnerOnly_areTheIncomeTaxStep()
    {
        HouseholdCardView.From(WithPartner, WithPartner.Run()).Sources.Select(source => source.RuleKey).ShouldBe(["income_tax"]);
    }

    [Fact]
    public void Sources_partnerAndChildrenWithNoCreditToPay_areTheIncomeTaxStepAlone()
    {
        var river = WithPartner.WithChildren(2);

        HouseholdCardView.From(river, river.Run()).Sources.Select(source => source.RuleKey).ShouldBe(["income_tax"]);
    }

    // Nobody declared and no partner, or an input refused: nothing is stated, so nothing is cited.
    [Fact]
    public void Sources_nobodyDeclaredOrRefused_areNone()
    {
        var alone = WithPartner.WithPartner(false);
        var refused = WithPartner.WithChildren(-1);

        (HouseholdCardView.From(alone, alone.Run()).Sources, HouseholdCardView.From(refused, refused.Run()).Sources).ShouldBe(([], []));
    }

    private static IReadOnlyList<(string Key, decimal Amount)> Lines(IEnumerable<FigureLine> lines) => [.. lines.Select(line => (line.Key, line.Amount))];

    private static IReadOnlyList<(string Key, string Args)> Parts(HouseholdCardView view) =>
        [.. view.Summary.Select(part => (part.Key, string.Join(' ', part.Args)))];
}
