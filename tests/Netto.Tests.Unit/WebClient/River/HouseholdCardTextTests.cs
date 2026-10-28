using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The keys the Household card chooses from what the year is (HouseholdCardText), each with its text in the
// card's .resx, so a key the card builds never shows on screen as itself. Its summary is parts joined by
// « · » (HouseholdCardView): the partner's year, the dependants, and what the household saves or costs
// the river's tax.
public class HouseholdCardTextTests
{
    private static readonly ResxText Texts = ResxText.For<HouseholdCard>();

    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    public static TheoryData<string, RiverInputs, string[], string?> Years => new()
    {
        { "a quotient", WithPartner, ["Summary_Quotient", "Summary_LessTax"], null },
        { "taxed separately", WithPartner.WithTaxedSeparately(true), ["Summary_TaxedSeparately"], "Note_TaxedSeparately" },
        { "taxed separately with children", WithPartner.WithTaxedSeparately(true).WithChildren(2), ["Summary_TaxedSeparately", "Summary_Dependants", "Summary_LessTax"], "Note_TaxedSeparately" },
        { "a single parent", WithPartner.WithPartner(false).WithChildren(2).WithSingleParent(true), ["Summary_Dependants", "Summary_LessTax"], null },
        { "a quotient and children", WithPartner.WithChildren(2), ["Summary_Quotient", "Summary_Dependants", "Summary_LessTax"], null },
        { "no partner, the year once taxed separately", WithPartner.WithTaxedSeparately(true).WithPartner(false), [], null },
        { "no partner, raising alone ticked with no child", WithPartner.WithPartner(false).WithSingleParent(true), [], null },
        { "refused", WithPartner.WithPartnerIncome(-1m), [], null },
    };

    [Theory]
    [MemberData(nameof(Years))]
    public void SummaryAndNote_aYear_areItsKeys(string _, RiverInputs river, string[] summary, string? note)
    {
        var view = HouseholdCardView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Summary.Select(part => part.Key).ShouldBe(summary),
            () => HouseholdCardText.Note(view.Joint, HouseholdCardText.TaxedSeparately(river.Household)).ShouldBe(note));
    }

    // A partner earning a third of the two: no quotient goes either way, the note says why.
    [Fact]
    public void SummaryAndNote_noQuotient_sayItTakesNone()
    {
        var river = WithPartner.WithPartnerIncome(40_000m);

        var view = HouseholdCardView.From(river, river.Run());

        (view.Summary[0].Key, HouseholdCardText.Note(view.Joint, HouseholdCardText.TaxedSeparately(river.Household))).ShouldBe(("Summary_NoQuotient", "Note_NoQuotient"));
    }

    [Fact]
    public void HouseholdCardResx_everyKeyTheCardChooses_hasAText()
    {
        string[] keys =
        [
            "Summary_Quotient", "Summary_NoQuotient", "Summary_TaxedSeparately", "Summary_Dependants", "Summary_LessTax", "Summary_MoreTax",
            "Note_NoQuotient", "Note_TaxedSeparately", HouseholdCardText.Counted, HouseholdCardText.QuotientMeaning, HouseholdFigures.QuotientToPartner,
            HouseholdFigures.QuotientFromPartner, "Figure_QuotientCosts", "Figure_HouseholdSaves", "Figure_HouseholdCosts", "Help_Altogether",
            "Figure_SupplementChildren", "Figure_SupplementUnder3", "Figure_SupplementSingleParent", "Figure_SupplementInCare", "Figure_SupplementOthers",
            "Figure_SupplementsToYou", "Figure_SupplementsToPartner", "Figure_ChildTaxCredit", "Figure_ChildTaxCreditTakenAway", "Figure_DependantsSave",
            "Figure_DependantsCost", "Help_SupplementChildren", "Help_SupplementUnder3", "Help_SupplementSingleParent", "Help_SupplementInCare",
            "Help_SupplementOthers", "Help_SupplementsToYou", "Help_SupplementsToPartner", "Help_ChildTaxCredit", "Help_ChildTaxCreditTakenAway",
            "Help_DependantsSave",
        ];

        Texts.Missing(keys).ShouldBeEmpty();
    }

    // « Marital quotient » is a term of art: the notes that name it mark it, for it to open its bubble.
    [Theory]
    [InlineData("Note_NoQuotient")]
    [InlineData("Note_TaxedSeparately")]
    public void Note_namingTheQuotient_marksItAsATerm(string key)
    {
        TermText.Split(Texts.Format(key, "30%", "€11,780.00")).Term.ShouldBe("marital quotient");
    }
}
