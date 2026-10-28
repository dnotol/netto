using Netto.Core.PersonalTax;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Household card's lines for the dependants (DependantsFigures), keys of the card's texts with the
// engine's amounts: each supplement to the tax-free amount with its count, whose tax-free amount they raise,
// the refundable credit for children, and what the dependants save the river's tax; a line of nothing left
// out. The supplements are 2026's, from the indexation notice for tax year 2027: two children 5,130, a child
// under 3 740, a single parent 1,980, a dependant aged 66 or more in care 5,950, any other 1,980. On a pay of
// 20,000 a single parent of two, one under 3, gets the credit's cap of 1,100 (GrossToNetDependantsTests).
public class DependantsFiguresTests
{
    private static readonly RiverInputs Household = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    private static readonly RiverInputs SingleParentOfTwo = Household.WithPartner(false).WithChildren(2).WithChildrenUnder3(1).WithSingleParent(true);

    [Fact]
    public void Of_singleParentOfTwoOnALowPay_isEachSupplementTheirTotalTheCreditAndWhatTheySave()
    {
        var view = View(SingleParentOfTwo.SkipCompany().WithDirectorPay(20_000m));

        Lines(DependantsFigures.Of(view)).ShouldBe(
        [
            ("Figure_SupplementChildren", 5_130m, "2"),
            ("Figure_SupplementUnder3", 740m, "1"),
            ("Figure_SupplementSingleParent", 1_980m, ""),
            ("Figure_SupplementsToYou", 7_850m, ""),
            ("Figure_ChildTaxCredit", 1_100m, ""),
            ("Figure_DependantsSave", view.Saves, ""),
        ]);
    }

    // On the example's 50,000 the tax absorbs the supplements: no credit is refunded, no line says one.
    [Fact]
    public void Of_otherDependantsOnTheExamplesPay_isTheirSupplementsAndWhatTheySave()
    {
        var view = View(Household.WithDependantsInCare(1).WithOtherDependants(2));

        Lines(DependantsFigures.Of(view)).ShouldBe(
        [
            ("Figure_SupplementInCare", 5_950m, "1"),
            ("Figure_SupplementOthers", 3_960m, "2"),
            ("Figure_SupplementsToYou", 9_910m, ""),
            ("Figure_DependantsSave", view.Saves, ""),
        ]);
    }

    // In a joint return the supplements go to the partner with the higher taxable income: the line says so.
    [Fact]
    public void Of_partnerEarningMore_addsTheSupplementsToTheirTaxFreeAmount()
    {
        var view = View(Household.WithPartnerIncome(150_000m).WithChildren(2));

        DependantsFigures.Of(view).Select(line => line.Key).ShouldContain("Figure_SupplementsToPartner");
    }

    // A single parent's supplement is never a joint return's, whatever was ticked.
    [Fact]
    public void Of_partnerAndSingleParentTicked_hasNoSingleParentsSupplement()
    {
        var view = View(Household.WithChildren(2).WithSingleParent(true));

        DependantsFigures.Of(view).Select(line => line.Key).ShouldNotContain("Figure_SupplementSingleParent");
    }

    // In a joint year the pay's share of the household's credit can be less than nothing, and declaring the
    // dependants could cost the river's tax: each says so with the amount, never « saves » a negative one.
    [Fact]
    public void Of_creditTakenAwayAndDependantsCosting_sayWhatTheyTakeWithTheAmount()
    {
        var view = View(SingleParentOfTwo.SkipCompany().WithDirectorPay(20_000m)) with { ChildTaxCredit = -120m, Saves = -8.50m };

        Lines(DependantsFigures.Of(view)).TakeLast(2).ShouldBe([("Figure_ChildTaxCreditTakenAway", 120m, ""), ("Figure_DependantsCost", 8.50m, "")]);
    }

    // Each supplement's bubble says the year's amounts behind it, the credit's its cap per child.
    [Fact]
    public void Of_eachLine_saysItsRulesAmountsInItsBubble()
    {
        var view = View(SingleParentOfTwo.SkipCompany().WithDirectorPay(20_000m).WithDependantsInCare(1).WithOtherDependants(1));

        DependantsFigures.Of(view).Where(line => line.HelpArgs.Count > 0).Select(line => (line.Help, string.Join(' ', line.HelpArgs))).ShouldBe(
        [
            ("Help_SupplementChildren", "€2,130.00 €5,130.00 €11,440.00 €18,510.00 €7,070.00"),
            ("Help_SupplementUnder3", "€740.00"),
            ("Help_SupplementSingleParent", "€1,980.00"),
            ("Help_SupplementInCare", "€5,950.00"),
            ("Help_SupplementOthers", "€1,980.00"),
            ("Help_ChildTaxCredit", "€550.00"),
        ]);
    }

    [Fact]
    public void From_noDependantDeclared_isNone()
    {
        DependantsView.From(Household.Run().Pay.ShouldNotBeNull(), Household.Household.Dependants.Values).ShouldBeNull();
    }

    internal static DependantsView View(RiverInputs river) =>
        DependantsView.From(river.Run().Pay.ShouldNotBeNull(), river.Household.Dependants.Values).ShouldNotBeNull();

    private static IReadOnlyList<(string Key, decimal Amount, string Args)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, string.Join(' ', line.Args)))];
}
