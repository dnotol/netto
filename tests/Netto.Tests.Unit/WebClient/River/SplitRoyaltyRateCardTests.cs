using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the royalty does to the smallest pay keeping the reduced corporate rate, on the split's card: two
// figure lines, the pay with the royalty and without it, after the lines the split shares; a note only for
// the pay no one keeps the rate at, which a figure cannot say.
public class SplitRoyaltyRateCardTests
{
    private static readonly ResxText Texts = ResxText.For<SplitCard>();

    private static readonly RiverInputs Fixed = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

    [Fact]
    public void RoyaltyRate_aRoyaltyThatLowersThePay_isThePayWithItThenWithoutIt()
    {
        SplitFigures.RoyaltyRate(new RoyaltyRateView(40_000m, 50_000m)).Select(line => (line.Key, line.Amount))
            .ShouldBe([(SplitFigures.RateWithRoyalty, 40_000m), (SplitFigures.RateWithoutRoyalty, 50_000m)]);
    }

    // The pay is a bound, not a flow: a pay of nothing, which any pay reaches, is a line too.
    [Fact]
    public void RoyaltyRate_aPayOfNothing_isStillALine()
    {
        SplitFigures.RoyaltyRate(new RoyaltyRateView(0m, 50_000m)).Select(line => line.Key)
            .ShouldBe([SplitFigures.RateWithRoyalty, SplitFigures.RateWithoutRoyalty]);
    }

    // Where no pay keeps the rate, there is no figure to draw for it.
    [Fact]
    public void RoyaltyRate_noPayKeepsTheRateWithoutIt_isThePayWithItAlone()
    {
        SplitFigures.RoyaltyRate(new RoyaltyRateView(40_000m, null)).Select(line => (line.Key, line.Amount)).ShouldBe([(SplitFigures.RateWithRoyalty, 40_000m)]);
    }

    [Fact]
    public void RoyaltyRate_noPayKeepsTheRateWithIt_isThePayWithoutItAlone()
    {
        SplitFigures.RoyaltyRate(new RoyaltyRateView(null, 50_000m)).Select(line => (line.Key, line.Amount)).ShouldBe([(SplitFigures.RateWithoutRoyalty, 50_000m)]);
    }

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
        SplitCardText.RoyaltyRateNote(new RoyaltyRateView(with, without)).ShouldBe(key);
    }

    [Fact]
    public void SplitCardResx_everyTextOfTheRoyaltyAndTheRate_hasAText()
    {
        Texts.Missing([SplitFigures.RateWithRoyalty, SplitFigures.RateWithoutRoyalty, "RoyaltyRate_NoPayKeeps", "RoyaltyRate_NoPayKeepsWithout"]).ShouldBeEmpty();
    }

    // The two sentences mark the reduced corporate rate, for it to open the bubble that says what it means.
    [Theory]
    [InlineData("RoyaltyRate_NoPayKeeps")]
    [InlineData("RoyaltyRate_NoPayKeepsWithout")]
    public void SplitCardResx_aNoteOnTheRate_marksTheReducedRate(string key)
    {
        TermText.Split(Texts.Get(key).ShouldNotBeNull()).Term.ShouldBe("reduced corporate rate");
    }

    // The lines stand after the royalty's own line among those the split shares, before the best pay's, and
    // are the engine's smallest pays.
    [Fact]
    public async Task SplitCard_aRoyaltyLoweringThePay_drawsTheTwoPaysAfterTheSplitsSides()
    {
        var view = RoyaltyRateView.From(Fixed.Run().Year).ShouldNotBeNull();

        var lines = CardHtml.Lines(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "split"));

        var labels = lines.Select(line => line.Label).ToList();
        var with = labels.IndexOf(Texts.Get(SplitFigures.RateWithRoyalty).ShouldNotBeNull());
        lines.ShouldSatisfyAllConditions(
            () => with.ShouldBeGreaterThan(labels.IndexOf(Texts.Get("Figure_Royalty").ShouldNotBeNull())),
            () => labels[with + 1].ShouldBe(Texts.Get(SplitFigures.RateWithoutRoyalty).ShouldNotBeNull()),
            () => labels.IndexOf(Texts.Get("Figure_Best").ShouldNotBeNull()).ShouldBeGreaterThan(with + 1),
            () => (lines[with].Amount, lines[with + 1].Amount).ShouldBe((Formats.Euros(view.With!.Value), Formats.Euros(view.Without!.Value))));
    }

    // A costly car on a small profit: not even the whole profit as pay keeps the rate beside the royalty, and
    // 34,285.72 keeps it without. The pay without is a line, the pay with none, said in a note with no amount.
    [Fact]
    public async Task SplitCard_noPayKeepsTheRateBesideTheRoyalty_saysItInANoteAndDrawsTheOtherPay()
    {
        var plan = RiverInputs.Example.WithRevenue(80_000m).AddToPlan(OptionalModule.Car).WithCatalogueValue(250_000m)
            .WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), "split");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Where(line => line.Label.StartsWith("Smallest pay keeping the reduced rate", StringComparison.Ordinal)).Select(line => (line.Label, line.Amount))
                .ShouldBe([(Texts.Get(SplitFigures.RateWithoutRoyalty).ShouldNotBeNull(), "€34,285.72")]),
            () => CardHtml.Prose(html).ShouldContain("No pay keeps the reduced corporate rate beside your royalty."),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // The note says what no figure can: with both pays drawn, there is none.
    [Fact]
    public async Task SplitCard_aRoyaltyLoweringThePay_saysNoNote()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "split");

        CardHtml.Prose(html).ShouldNotContain(prose => prose.StartsWith("No pay keeps", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SplitCard_noRoyaltyInThePlan_drawsNoPayKeepingTheRate()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example), "split");

        CardHtml.Lines(html).Select(line => line.Label).ShouldNotContain(Texts.Get(SplitFigures.RateWithRoyalty).ShouldNotBeNull());
    }

    [Fact]
    public async Task SplitCard_aCompanyInItsFirstFourTaxPeriods_drawsNoPayKeepingTheRate()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed.WithFinancialYear(FinancialYear.First)), "split");

        CardHtml.Lines(html).Select(line => line.Label).ShouldNotContain(Texts.Get(SplitFigures.RateWithRoyalty).ShouldNotBeNull());
    }

    // The lines are the card's: they leave the map's lines, which read the sides alone, as they were.
    [Fact]
    public async Task SplitCard_theTwoPays_leaveNoKeyOnTheCard()
    {
        CardHtml.LeakedKeys(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "split")).ShouldBeEmpty();
    }
}
