using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The keys the Household card chooses from what the year is (HouseholdCardText), each with its text in the
// card's .resx, so a key the card builds never shows on screen as itself.
public class HouseholdCardTextTests
{
    private static readonly ResxText Texts = ResxText.For<HouseholdCard>();

    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    public static TheoryData<string, RiverInputs, string?, string?> Years => new()
    {
        { "a quotient", WithPartner, "Summary_Quotient", null },
        { "no quotient", WithPartner.WithPartnerIncome(40_000m), "Summary_NoQuotient", "Note_NoQuotient" },
        { "taxed separately", WithPartner.WithTaxedSeparately(true), "Summary_TaxedSeparately", "Note_TaxedSeparately" },
        { "refused", WithPartner.WithPartnerIncome(-1m), null, null },
    };

    [Theory]
    [MemberData(nameof(Years))]
    public void SummaryAndNote_aYear_areItsKeys(string _, RiverInputs river, string? summary, string? note)
    {
        var view = river.Run().Pay is { } pay ? HouseholdView.From(pay, river.Household.BothAged66.Value) : null;
        var separately = river.Household.TaxedSeparately.Value;

        (HouseholdCardText.Summary(view, separately), HouseholdCardText.Note(view, separately)).ShouldBe((summary, note));
    }

    [Fact]
    public void HouseholdCardResx_everyKeyTheCardChooses_hasAText()
    {
        string[] keys =
        [
            "Summary_Quotient", "Summary_NoQuotient", "Summary_TaxedSeparately", "Note_NoQuotient", "Note_TaxedSeparately",
            HouseholdCardText.Counted, HouseholdCardText.QuotientMeaning, HouseholdFigures.QuotientToPartner, HouseholdFigures.QuotientFromPartner,
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
