using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the royalty does to the reduced corporate rate, on the Copyright royalty card: a note only where no pay
// keeps the rate beside the royalty, or without it, which no figure can say. The smallest pays keeping it are
// Corporate tax's to say, in its own terms: the card draws neither, for its lines not to be read as that card's
// pay taken into account, minimum pay or taxable income.
public class CopyrightRateTests
{
    private static readonly ResxText Texts = ResxText.For<CopyrightCard>();

    private static readonly RiverInputs Fixed = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

    public static TheoryData<decimal?, decimal?, string?> Notes => new()
    {
        { 40_000m, 50_000m, null },
        { 51_000m, 51_000m, null },
        { 40_000m, null, "RoyaltyRate_NoPayKeepsWithout" },
        { null, 50_000m, "RoyaltyRate_NoPayKeeps" },
    };

    [Theory]
    [MemberData(nameof(Notes))]
    public void RoyaltyRateNote_isSaidOnlyForAPayNoOneKeepsTheRateAt(decimal? with, decimal? without, string? key)
    {
        CopyrightCardText.RoyaltyRateNote(new RoyaltyRateView(with, without)).ShouldBe(key);
    }

    [Fact]
    public void CopyrightCardResx_everyNoteOnTheRate_hasAText()
    {
        Texts.Missing(["RoyaltyRate_NoPayKeeps", "RoyaltyRate_NoPayKeepsWithout"]).ShouldBeEmpty();
    }

    // The two sentences mark the reduced corporate rate, for it to open the bubble that says what it means.
    [Theory]
    [InlineData("RoyaltyRate_NoPayKeeps")]
    [InlineData("RoyaltyRate_NoPayKeepsWithout")]
    public void CopyrightCardResx_aNoteOnTheRate_marksTheReducedRate(string key)
    {
        TermText.Split(Texts.Get(key).ShouldNotBeNull()).Term.ShouldBe("reduced corporate rate");
    }

    // A royalty lowering the smallest pay keeping the rate: the card draws neither pay, nor a note.
    [Fact]
    public async Task CopyrightCard_aRoyaltyLoweringThePay_drawsNeitherPayNorANote()
    {
        var view = RoyaltyRateView.From(Fixed.Run().Year).ShouldNotBeNull();

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "copyright");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.LineAmounts(html).ShouldNotContain(Formats.Euros(view.With!.Value)),
            () => CardHtml.LineAmounts(html).ShouldNotContain(Formats.Euros(view.Without!.Value)),
            () => CardHtml.Prose(html).ShouldNotContain(prose => prose == Said("RoyaltyRate_NoPayKeeps") || prose == Said("RoyaltyRate_NoPayKeepsWithout")));
    }

    // A costly car on a small profit: not even the whole profit as pay keeps the rate beside the royalty, and
    // 34,285.72 keeps it without. A note says no pay keeps it, with no amount; the pay without is no line.
    [Fact]
    public async Task CopyrightCard_noPayKeepsTheRateBesideTheRoyalty_saysItInANote()
    {
        var plan = RiverInputs.Example.WithRevenue(80_000m).AddToPlan(OptionalModule.Car).WithCatalogueValue(250_000m)
            .WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), "copyright");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain(Said("RoyaltyRate_NoPayKeeps")),
            () => CardHtml.LineAmounts(html).ShouldNotContain("€34,285.72"),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // A note as a viewer reads it: its text with the term's brackets gone.
    private static string Said(string key) => Texts.Format(key).Replace("[", "").Replace("]", "");
}
