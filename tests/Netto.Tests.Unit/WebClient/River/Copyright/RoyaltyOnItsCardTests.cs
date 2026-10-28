using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Copyright;
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
        var chips = CardHtml.Chips(await CopyrightAsync(inputs));

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
            () => CardHtml.Chips(html).ShouldBeEmpty(),
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

    // The rates the card's hints state are the year's rules' own: the ruling's envelope in the two hints of
    // the royalty that follows the pay, the rate withheld in the one of the amount that is fixed, and the
    // withholding and the tax apart in the card's own.
    [Fact]
    public async Task CopyrightCard_hints_stateTheRatesOfTheYearsRules()
    {
        // No share given yet, so « Your copyright situation » is open, with the share's bubble.
        var following = await CopyrightAsync(Added);
        var fixedHtml = await CopyrightAsync(Fixed);
        var rules = CopyrightRules.IncomeYear2026;

        var envelope = Formats.RuleAmount(rules.EnvelopeRate.ShouldNotBeNull());
        var withholding = Formats.RuleAmount(rules.WithholdingRate);
        var title = Royalty.Get("Title")!;

        following.ShouldSatisfyAllConditions(
            () => CardHtml.BubbleAfter(following, Royalty.Get("PolicyLabel")!).ShouldContain($"at most {envelope} of your pay"),
            () => CardHtml.BubbleAfter(following, Royalty.Get("CreativeShareLabel")!).ShouldContain($"royalty of at most {envelope} of their pay"),
            () => CardHtml.BubbleAfter(following, title).ShouldContain($"withholds {withholding}"),
            () => CardHtml.BubbleAfter(following, title).ShouldContain($"apart at {Formats.RuleAmount(rules.TaxedApartRate)}"),
            () => CardHtml.BubbleAfter(fixedHtml, Royalty.Get("FixedRoyaltyLabel")!).ShouldContain($"before the {withholding} it withholds"));
    }

    // The reading of the details names the 30% and the 15% withheld, the rules' own.
    [Fact]
    public async Task CopyrightCard_readings_stateTheRatesOfTheYearsRules()
    {
        var html = await RenderDetailsOpenAsync(typeof(CopyrightCard), OptionalModule.Copyright, CopyrightViewTests.Within);
        var rules = CopyrightRules.IncomeYear2026;

        CardHtml.Prose(html).ShouldContain(prose => prose.Contains($"the {Formats.RuleAmount(rules.RelativeLimit)} is measured", StringComparison.Ordinal)
            && prose.Contains($"the {Formats.RuleAmount(rules.WithholdingRate)} withheld", StringComparison.Ordinal));
    }

    // A year whose rules hold no rate for what a ruling accepts (2025), or no rules at all, says « the year's
    // rate » in its place, with no placeholder left and no rate made up.
    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public async Task CopyrightCard_yearWithoutAnEnvelopeRate_saysTheYearsRate(int year)
    {
        var html = await CopyrightAsync(Added with { IncomeYear = year });

        html.ShouldSatisfyAllConditions(
            () => CardHtml.BubbleAfter(html, Royalty.Get("PolicyLabel")!).ShouldContain("the year's rate"),
            () => CardHtml.BubbleAfter(html, Royalty.Get("CreativeShareLabel")!).ShouldContain("the year's rate"),
            () => CardHtml.Bubbles(html).Where(bubble => Regex.IsMatch(bubble.Text, @"\{\d\}|(?<![0-9])0%")).ShouldBeEmpty());
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
