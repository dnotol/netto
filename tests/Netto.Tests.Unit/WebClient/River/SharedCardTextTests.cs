using System.Net;
using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The read-only card of the node « To share » builds resource keys from the royalty beside the pay and the
// engine's figures. A key without a text in its .resx would show on screen as the key itself.
public class SharedCardTextTests
{
    // The name IStringLocalizer<SharedCard> reads River/SharedCard.resx under.
    private static readonly ResxText Resources = ResxText.For<SharedCard>();

    // Every label the card's lines can carry: its sides with and without a royalty, and with the contributions
    // the company pays and the fund's fee; its title, summary, hint and the links to where each amount is set.
    [Fact]
    public void SharedCardResx_everyLabelOfItsLines_hasAText()
    {
        RiverInputs[] rivers =
        [
            RiverInputs.Example, RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m),
            RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m),
        ];
        var keys = rivers.SelectMany(inputs =>
        {
            var run = inputs.Run();
            return SharedFigures.Sides(SplitView.From(inputs, run), run.Year);
        }).Select(line => line.Key);

        Resources.Missing(["Title", "Summary", "Hint", "PayLink", "ReserveLink", SharedCardText.RoyaltyLink, .. keys]).ShouldBeEmpty();
    }

    // The summary says what is shared as every card's summary and the line into the node say an amount, to the
    // cent, in either language: « €100,000.00 to share », « 100.000,00 € à partager ».
    [Fact]
    public async Task SharedCard_summary_saysTheAmountToTheCentInEnglish()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example), "split");

        WebUtility.HtmlDecode(CardHtml.Summary(html).ShouldNotBeNull()).ShouldBe("€100,000.00 to share");
    }

    [Fact]
    public async Task SharedCard_summary_saysTheAmountToTheCentInFrench()
    {
        using var french = InLanguage.French();

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example), "split");

        var expected = ResxText.French<SharedCard>().Format("Summary", Formats.Euros(100_000m));

        WebUtility.HtmlDecode(CardHtml.Summary(html).ShouldNotBeNull()).Replace(' ', ' ').ShouldBe(WebUtility.HtmlDecode(expected).Replace(' ', ' '));
    }

    // The note says how the royalty is set, the Copyright royalty card deciding it: that it follows the
    // pay, or that it is a fixed amount whatever the pay.
    public static TheoryData<string, RiverInputs, string> RoyaltyNotes => new()
    {
        { "following", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m), "RoyaltyNote_Follows" },
        { "following without a share", RiverInputs.Example.AddToPlan(OptionalModule.Copyright), "RoyaltyNote_Follows" },
        { "fixed", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m), "RoyaltyNote_Fixed" },
    };

    [Theory]
    [MemberData(nameof(RoyaltyNotes))]
    public void RoyaltyNote_saysHowTheRoyaltyIsSet(string _, RiverInputs inputs, string key)
    {
        SharedCardText.RoyaltyNote(SplitView.From(inputs, inputs.Run()).Royalty.ShouldNotBeNull()).ShouldBe(key);
        Resources.Missing([key]).ShouldBeEmpty();
    }

    // The card has no slider (the pay is typed on Director pay), so no sentence of it points at one.
    [Theory]
    [InlineData("RoyaltyNote_Follows")]
    [InlineData("RoyaltyNote_Fixed")]
    public void SharedCardResx_royaltyNote_pointsAtNoSlider(string key)
    {
        string?[] texts = [Resources.Get(key), ResxText.French<SharedCard>().Get(key)];

        texts.ShouldAllBe(text => text != null && !text.Contains("slider", StringComparison.OrdinalIgnoreCase) && !text.Contains("curseur", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SharedCardResx_followingRoyaltyNote_saysItChangesWithThePay()
    {
        (Resources.Get("RoyaltyNote_Follows"), ResxText.French<SharedCard>().Get("RoyaltyNote_Follows"))
            .ShouldBe(("Your royalty follows your pay: it changes with it.", "Vos droits d'auteur suivent votre rémunération : ils changent avec elle."));
    }
}
