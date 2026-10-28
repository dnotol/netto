using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River.Copyright;

// The royalty is decided on the Copyright royalty card (operator, 2026-10-28, reversing the one-place
// decision of 2026-10-23): whether it follows the pay or is a fixed amount, that amount, and the refusal of
// it. The split keeps the royalty's line among its sides, a note saying how it is set, and a link to the
// card. Read as a viewer meets the cards first drawn (RiverRendering); what a click does is
// CopyrightPolicyViewTests and RiverInputsTests'.
public class RoyaltyOnItsCardTests
{
    private const string FixedField = "river-copyright-royalty";

    private static readonly RiverInputs Added = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);

    private static readonly RiverInputs Following = Added.WithCreativeShare(0.75m);

    private static readonly RiverInputs Fixed = Added.WithRoyalty(10_000m);

    // More than the 100,000 the company has before pay and royalty.
    private static readonly RiverInputs Above = Added.WithRoyalty(150_000m);

    // A chip MudBlazor draws: a button saying whether it is pressed, its text inside.
    private static readonly Regex Chips = new(
        "<button class=\"mud-chip[^\"]*\"[^>]*aria-pressed=\"(?<pressed>true|false)\"[^>]*><span class=\"mud-chip-content\">(?<text>[^<]*)</span>");

    private static readonly ResxText Royalty = ResxText.For<CopyrightCard>();

    private static readonly ResxText Split = ResxText.For<SplitCard>();

    // Its two ways, one chip each, the way the royalty is set pressed.
    public static TheoryData<string, RiverInputs, string> Ways => new()
    {
        { "added", Added, "Follows your pay" },
        { "following", Following, "Follows your pay" },
        { "fixed", Fixed, "A fixed amount" },
    };

    [Theory]
    [MemberData(nameof(Ways))]
    public async Task CopyrightCard_royalty_hasTwoWaysAndPressesTheOneItIsSetBy(string _, RiverInputs inputs, string pressed)
    {
        var chips = Chips.Matches(await CopyrightAsync(inputs)).Select(chip => (Text: chip.Groups["text"].Value, Pressed: chip.Groups["pressed"].Value == "true")).ToList();

        chips.ShouldBe([("Follows your pay", pressed == "Follows your pay"), ("A fixed amount", pressed == "A fixed amount")]);
    }

    // The amount is typed only while it is fixed: one that follows the pay is what the pay makes it.
    [Fact]
    public async Task CopyrightCard_followingRoyalty_hasNoFieldForAnAmount()
    {
        (await CopyrightAsync(Following)).ShouldNotContain(FixedField);
    }

    [Fact]
    public async Task CopyrightCard_fixedRoyalty_hasItsAmountFieldWithTheAmount()
    {
        var html = await CopyrightAsync(Fixed);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain($"id=\"{FixedField}\""),
            () => html.ShouldContain("value=\"10,000.00\""),
            () => FieldHead(html, FixedField).ShouldContain(Royalty.Get("FixedRoyaltyLabel").ShouldNotBeNull()));
    }

    // A royalty that follows the pay and is none says why, in the card's own words.
    [Fact]
    public async Task CopyrightCard_followingRoyaltyWithoutAShare_saysTheShareIsNeeded()
    {
        (await CopyrightAsync(Added)).ShouldContain(Royalty.Get("Following_NoCreativeShare").ShouldNotBeNull());
    }

    // The refusal of the fixed amount marks its field and is said in sight, on this card.
    [Fact]
    public async Task CopyrightCard_fixedRoyaltyAboveWhatTheCompanyHas_marksItsFieldAndSaysWhy()
    {
        var run = Above.Run();
        var error = run.Error.ShouldNotBeNull();
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(OptionalModule.Copyright), true);

        var html = WebUtility.HtmlDecode(await RenderAsync<CopyrightCard>(folds, new() { ["Inputs"] = Above, ["Run"] = run, ["Refused"] = error }));

        html.ShouldSatisfyAllConditions(
            () => error.Input.ShouldBe(RefusedInput.Royalty),
            () => html.ShouldContain(Refusals.Get($"Error_{error.Key}").ShouldNotBeNull()),
            () => Regex.IsMatch(html, $"river-number--refused\"[^>]*>.*?<input[^>]*id=\"{FixedField}\"", RegexOptions.Singleline).ShouldBeTrue());
    }

    // The river as the plan draws it hands the refusal to the Copyright card alone.
    [Fact]
    public async Task Cards_fixedRoyaltyAboveWhatTheCompanyHas_isRefusedOnTheCopyrightCardNotTheSplit()
    {
        var html = await CardHtml.RenderCardsOpenAsync(Above);
        var message = Refusals.Get($"Error_{Above.Run().Error.ShouldNotBeNull().Key}").ShouldNotBeNull();

        (CardHtml.Card(html, "copyright").Contains(message, StringComparison.Ordinal), CardHtml.Card(html, "split").Contains(message, StringComparison.Ordinal))
            .ShouldBe((true, false));
    }

    // The split holds neither the way the royalty is set nor its amount any more.
    [Theory]
    [MemberData(nameof(Ways))]
    public async Task SplitCard_royalty_hasNoChipsAndNoFieldForIt(string _, RiverInputs inputs, string _1)
    {
        var html = await SplitAsync(inputs);

        html.ShouldSatisfyAllConditions(
            () => Chips.Matches(html).ShouldBeEmpty(),
            () => html.ShouldNotContain("river-split-royalty"),
            () => html.ShouldNotContain(FixedField));
    }

    // It keeps the royalty's line among its sides, a note saying how it is set, and a link to the card that
    // sets it.
    [Theory]
    [InlineData(true, "RoyaltyNote_Follows")]
    [InlineData(false, "RoyaltyNote_Fixed")]
    public async Task SplitCard_royalty_keepsItsLineASayingHowItIsSetAndALinkToItsCard(bool following, string note)
    {
        var html = await SplitAsync(following ? Following : Fixed);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldContain(line => line.Label == Split.Get("Figure_Royalty")),
            () => CardHtml.Prose(html).ShouldContain(Split.Get(note).ShouldNotBeNull() + " " + Split.Get("RoyaltyLink").ShouldNotBeNull()),
            () => Regex.IsMatch(html, "<a href=\"plan\\?card=copyright\"[^>]*>").ShouldBeTrue(),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // No royalty in the plan, no note and no link.
    [Fact]
    public async Task SplitCard_noRoyaltyInThePlan_saysNothingOfOne()
    {
        var html = await SplitAsync(RiverInputs.Example);

        html.ShouldNotContain("card=copyright");
    }

    private static ResxText Refusals => ResxText.For<RefusedMessage>();

    private static Task<string> CopyrightAsync(RiverInputs inputs) => RenderOpenAsync<CopyrightCard>(OptionalModule.Copyright, inputs);

    private static async Task<string> SplitAsync(RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split", true);

        return WebUtility.HtmlDecode(await RenderAsync<SplitCard>(folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null, ["Search"] = inputs.SearchSplit() }));
    }
}
