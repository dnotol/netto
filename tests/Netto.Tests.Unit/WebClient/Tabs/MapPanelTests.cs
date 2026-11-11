using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The room beside the Map tab's map, drawn without a browser (RiverRendering): the card of the module
// chosen, the same component as on the Plan tab, open and with no toggle, or a place for it while none is.
// What a click does, and where the focus goes, is the page's test (PlanMapPanelTests).
public class MapPanelTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static readonly ResxText Texts = ResxText.For<MapPanel>();

    // Every module of the river that has a card, which the panel must have a case for each of.
    public static TheoryData<RiverModule> Modules => new(Enum.GetValues<RiverModule>().Where(module => module != RiverModule.River));

    // The card of the module chosen, and no other, open with its heading and nothing to press to open it, even
    // where the viewer folded it on the Plan tab; the viewer's word on it is left as it was.
    [Theory]
    [MemberData(nameof(Modules))]
    public async Task Panel_aModule_drawsItsOwnCardOpenWhateverTheViewerFoldedOnThePlanTab(RiverModule module)
    {
        var name = MapModules.Name(module);
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, false);

        var html = await RenderPanelAsync(Full, module, folds);

        html.ShouldSatisfyAllConditions(
            () => Regex.Matches(html, "<section id=\"(card-[a-z-]+)\"").Select(match => match.Groups[1].Value).ShouldBe([$"card-{name}"]),
            () => html.ShouldContain("module-card__body"),
            () => html.ShouldContain("<h2 class=\"fold__heading fold__heading--card\" tabindex=\"-1\""),
            () => Regex.IsMatch(html, "<h2[^>]*><button").ShouldBeFalse(),
            () => folds.IsOpen(name, openByDefault: false).ShouldBeFalse());
    }

    // The folds inside a card are the viewer's, as on the Plan tab: the card's details open where they said.
    [Fact]
    public async Task Panel_aCardsDetails_areFoldedUntilTheViewerOpensThem()
    {
        var folds = new Folds(new BrowserStorage());

        var folded = await RenderPanelAsync(Full, RiverModule.DirectorPay, folds);
        folds.Set("director-pay-details", true);
        var open = await RenderPanelAsync(Full, RiverModule.DirectorPay, folds);

        (folded.Contains("module-card__detail"), open.Contains("module-card__detail")).ShouldBe((false, true));
    }

    // The same card as the Plan tab's, editable: its fields are on it, and every edit goes back to the page.
    [Fact]
    public async Task Panel_theCompanyYear_hasItsAmountsToEdit()
    {
        var html = await RenderPanelAsync(Full, RiverModule.CompanyYear);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("id=\"river-revenue\""),
            () => html.ShouldContain("id=\"river-running-costs\""));
    }

    // The node « To share » draws a read-only card: its sides, no field and no pay curve, which is Optimise's.
    [Fact]
    public async Task Panel_theSplit_drawsItsReadOnlyCardWithNoFieldAndNoCurve()
    {
        var html = await RenderPanelAsync(Full, RiverModule.Split);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("id=\"card-split\""),
            () => html.ShouldNotContain("<input"),
            () => html.ShouldNotContain("pay-curve"));
    }

    // The engine's refusal goes to the card that holds the field, as the Plan page hands it, with its words.
    [Fact]
    public async Task Panel_aRefusedAmountOnItsCard_isSaidThere()
    {
        var refused = Full.WithReserve(-1m);
        var error = refused.Run().Error.ShouldNotBeNull();

        var html = await RenderPanelAsync(refused, RiverModule.Dividend, refused: error);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("refused-message"),
            () => html.ShouldContain(ResxText.For<RefusedMessage>().Get(RefusedMessageText.Error(error)).ShouldNotBeNull()));
    }

    [Fact]
    public async Task Panel_aCardWithNoRefusal_saysNone()
    {
        var html = await RenderPanelAsync(Full, RiverModule.Dividend);

        html.ShouldNotContain("refused-message");
    }

    // Nothing chosen: the card's place, in words, and no card.
    [Fact]
    public async Task Panel_noModuleChosen_savesTheRoomForACardAndDrawsNone()
    {
        var html = await RenderPanelAsync(Full, module: null);

        (html.Contains(Texts.Get("Hint").ShouldNotBeNull()), html.Contains("module-card"), html.Contains("<button")).ShouldBe((true, false, false));
    }

    // The river itself is no module with a card, and a module the panel has no case for is a bug, not a gap.
    [Theory]
    [InlineData(RiverModule.River)]
    [InlineData((RiverModule)99)]
    public async Task Panel_aModuleItHasNoCardFor_isRefusedNotLeftEmpty(RiverModule module)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => RenderPanelAsync(Full, module));
    }

    // Nothing in the panel is a live region: the bar's is the river's only announcement.
    [Theory]
    [MemberData(nameof(Modules))]
    public async Task Panel_aCard_isSaidInNoLiveRegion(RiverModule module)
    {
        var html = await RenderPanelAsync(Full, module);

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    private static async Task<string> RenderPanelAsync(RiverInputs river, RiverModule? module, Folds? folds = null, Refusal? refused = null) =>
        WebUtility.HtmlDecode(await RenderAsync(typeof(MapPanel), folds ?? new Folds(new BrowserStorage()), new()
        {
            [nameof(MapPanel.Chosen)] = MapRendering.Chosen(module),
            [nameof(MapPanel.Inputs)] = river,
            [nameof(MapPanel.Run)] = river.Run(),
            [nameof(MapPanel.Refused)] = refused,
        }));
}
