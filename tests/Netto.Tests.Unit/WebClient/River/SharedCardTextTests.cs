using Netto.Core.SocialContributions;
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
}
