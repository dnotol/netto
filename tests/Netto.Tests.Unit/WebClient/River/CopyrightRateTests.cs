using System.Net;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the royalty does to the smallest pay keeping the reduced corporate rate, on the Copyright royalty card: two
// figure lines, the pay with the royalty and without it, after the lines of the royalty's limits; a note only for
// the pay no one keeps the rate at, which a figure cannot say.
public class CopyrightRateTests
{
    private static readonly ResxText Texts = ResxText.For<CopyrightCard>();

    private static readonly RiverInputs Fixed = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

    [Fact]
    public void RoyaltyRate_aRoyaltyThatLowersThePay_isThePayWithItThenWithoutIt()
    {
        CopyrightFigures.RoyaltyRate(new RoyaltyRateView(40_000m, 50_000m)).Select(line => (line.Key, line.Amount))
            .ShouldBe([(CopyrightFigures.RateWithRoyalty, 40_000m), (CopyrightFigures.RateWithoutRoyalty, 50_000m)]);
    }

    // The pay is a bound, not a flow: a pay of nothing, which any pay reaches, is a line too.
    [Fact]
    public void RoyaltyRate_aPayOfNothing_isStillALine()
    {
        CopyrightFigures.RoyaltyRate(new RoyaltyRateView(0m, 50_000m)).Select(line => line.Key)
            .ShouldBe([CopyrightFigures.RateWithRoyalty, CopyrightFigures.RateWithoutRoyalty]);
    }

    // Where no pay keeps the rate, there is no figure to draw for it.
    [Fact]
    public void RoyaltyRate_noPayKeepsTheRateWithoutIt_isThePayWithItAlone()
    {
        CopyrightFigures.RoyaltyRate(new RoyaltyRateView(40_000m, null)).Select(line => (line.Key, line.Amount)).ShouldBe([(CopyrightFigures.RateWithRoyalty, 40_000m)]);
    }

    [Fact]
    public void RoyaltyRate_noPayKeepsTheRateWithIt_isThePayWithoutItAlone()
    {
        CopyrightFigures.RoyaltyRate(new RoyaltyRateView(null, 50_000m)).Select(line => (line.Key, line.Amount)).ShouldBe([(CopyrightFigures.RateWithoutRoyalty, 50_000m)]);
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
        CopyrightCardText.RoyaltyRateNote(new RoyaltyRateView(with, without)).ShouldBe(key);
    }

    [Fact]
    public void CopyrightCardResx_everyTextOfTheRoyaltyAndTheRate_hasAText()
    {
        Texts.Missing([CopyrightFigures.RateWithRoyalty, CopyrightFigures.RateWithoutRoyalty, "RoyaltyRate_NoPayKeeps", "RoyaltyRate_NoPayKeepsWithout"]).ShouldBeEmpty();
    }

    // The two sentences mark the reduced corporate rate, for it to open the bubble that says what it means.
    [Theory]
    [InlineData("RoyaltyRate_NoPayKeeps")]
    [InlineData("RoyaltyRate_NoPayKeepsWithout")]
    public void CopyrightCardResx_aNoteOnTheRate_marksTheReducedRate(string key)
    {
        TermText.Split(Texts.Get(key).ShouldNotBeNull()).Term.ShouldBe("reduced corporate rate");
    }

    // The lines stand after the royalty's own line, and are the engine's smallest pays.
    [Fact]
    public async Task CopyrightCard_aRoyaltyLoweringThePay_drawsTheTwoPaysAfterTheRoyaltysLines()
    {
        var view = RoyaltyRateView.From(Fixed.Run().Year).ShouldNotBeNull();

        var lines = CardHtml.Lines(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "copyright"));

        var labels = lines.Select(line => line.Label).ToList();
        var with = labels.IndexOf(Texts.Get(CopyrightFigures.RateWithRoyalty).ShouldNotBeNull());
        lines.ShouldSatisfyAllConditions(
            () => with.ShouldBeGreaterThan(labels.IndexOf(Texts.Get("Figure_Royalty").ShouldNotBeNull())),
            () => labels[with + 1].ShouldBe(Texts.Get(CopyrightFigures.RateWithoutRoyalty).ShouldNotBeNull()),
            () => (lines[with].Amount, lines[with + 1].Amount).ShouldBe((Formats.Euros(view.With!.Value), Formats.Euros(view.Without!.Value))));
    }

    // A costly car on a small profit: not even the whole profit as pay keeps the rate beside the royalty, and
    // 34,285.72 keeps it without. The pay without is a line, the pay with none, said in a note with no amount.
    [Fact]
    public async Task CopyrightCard_noPayKeepsTheRateBesideTheRoyalty_saysItInANoteAndDrawsTheOtherPay()
    {
        var plan = RiverInputs.Example.WithRevenue(80_000m).AddToPlan(OptionalModule.Car).WithCatalogueValue(250_000m)
            .WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), "copyright");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Where(line => line.Label == Texts.Get(CopyrightFigures.RateWithRoyalty) || line.Label == Texts.Get(CopyrightFigures.RateWithoutRoyalty)).Select(line => (line.Label, line.Amount))
                .ShouldBe([(Texts.Get(CopyrightFigures.RateWithoutRoyalty).ShouldNotBeNull(), "€34,285.72")]),
            () => CardHtml.Prose(html).ShouldContain(Said("RoyaltyRate_NoPayKeeps")),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // The note says what no figure can: with both pays drawn, there is none.
    [Fact]
    public async Task CopyrightCard_aRoyaltyLoweringThePay_saysNoNote()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "copyright");

        CardHtml.Prose(html).ShouldNotContain(prose => prose == Said("RoyaltyRate_NoPayKeeps") || prose == Said("RoyaltyRate_NoPayKeepsWithout"));
    }

    [Fact]
    public async Task CopyrightCard_noRoyaltyYet_drawsNoPayKeepingTheRate()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.AddToPlan(OptionalModule.Copyright)), "copyright");

        CardHtml.Lines(html).Select(line => line.Label).ShouldNotContain(Texts.Get(CopyrightFigures.RateWithRoyalty).ShouldNotBeNull());
    }

    [Fact]
    public async Task CopyrightCard_aCompanyInItsFirstFourTaxPeriods_drawsNoPayKeepingTheRate()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed.WithFinancialYear(FinancialYear.First)), "copyright");

        CardHtml.Lines(html).Select(line => line.Label).ShouldNotContain(Texts.Get(CopyrightFigures.RateWithRoyalty).ShouldNotBeNull());
    }

    // The lines are the card's: they leave the map's lines as they were.
    [Fact]
    public async Task CopyrightCard_theTwoPays_leaveNoKeyOnTheCard()
    {
        CardHtml.LeakedKeys(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Fixed), "copyright")).ShouldBeEmpty();
    }

    // A note as a viewer reads it: its text with the term's brackets gone.
    private static string Said(string key) => Texts.Format(key).Replace("[", "").Replace("]", "");
}
