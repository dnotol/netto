using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A card pinned open, as the Map tab shows the one chosen beside its map: always open, not foldable, and
// never a word on the viewer's folds of the Plan tab. A fold is pinned by the seat its card is in
// (PinnedCard, cascaded to ModuleCard), so no card of the river knows it. What it takes the focus for is
// the page's test (PlanMapPanelTests): a static render runs no after-render hook.
public class PinnedFoldTests
{
    private const string Body = "the-fold-body";

    [Fact]
    public async Task Fold_pinned_isOpenAndHasNoToggle()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split"));

        (html.Contains(Body), html.Contains("<button"), html.Contains("aria-expanded"), html.Contains("The split"), html.Contains("50,000 to your pay"))
            .ShouldBe((true, false, false, true, true));
    }

    // The card's own heading stays the heading, and it takes the focus (it is no tab stop of its own).
    [Fact]
    public async Task Fold_pinned_isAnH2TheFocusCanBePutOn()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split"));

        html.ShouldContain("<h2 class=\"fold__heading fold__heading--card\" tabindex=\"-1\"");
    }

    [Fact]
    public async Task Fold_pinnedWithAHelpBubble_keepsTheBubbleAfterTheHeading()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split", ("Help", "What it is")));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("tabindex=\"-1\""),
            () => html.ShouldContain("info-bubble"),
            () => html.ShouldContain("What it is"));
    }

    // The viewer folded it on the Plan tab: the Map tab still shows it open, and leaves their word as it was.
    [Fact]
    public async Task Fold_pinnedFoldedByTheViewer_isOpenAndTheirWordStays()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Set("split", false);
        var stored = storage.Get(Folds.StorageKey);

        var html = await RenderAsync<Fold>(folds, Parameters("split"));

        (html.Contains(Body), folds.IsOpen("split", openByDefault: false), storage.Get(Folds.StorageKey)).ShouldBe((true, false, stored));
    }

    // A refusal reveals an unpinned fold for the visit, which would open the same card on the Plan tab: a
    // pinned fold is open anyway and reveals nothing.
    [Fact]
    public async Task Fold_pinnedAndRefused_revealsNothingAndStoresNothing()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);

        var html = await RenderAsync<Fold>(folds, Parameters("split", ("Refused", true)));

        (html.Contains(Body), folds.IsOpen("split", openByDefault: false), storage.Get(Folds.StorageKey)).ShouldBe((true, false, null));
    }

    // The pinned heading is a place the page puts the focus, not a control: like the tab's title (app.css), it
    // draws none of the browser's own ring, which the site's focus ring is not (it is for what is pressed).
    [Fact]
    public void Heading_pinnedAndFocused_drawsNoRingOfItsOwn()
    {
        var css = Regex.Replace(File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "River", "Fold.razor.css")), @"/\*.*?\*/", "", RegexOptions.Singleline);

        var rule = Regex.Match(css, @"\.fold__heading\[tabindex=""-1""\]:focus\s*\{([^}]*)\}");

        (rule.Success, rule.Groups[1].Value.Contains("outline: none")).ShouldBe((true, true));
    }

    // Unpinned, a fold is what it always was: folded until opened, a toggle to open it.
    [Fact]
    public async Task Fold_notPinned_staysFoldedWithItsToggle()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split", ("Pinned", false)));

        (IsOpen(html), html.Contains(Body), html.Contains("tabindex")).ShouldBe((false, false, false));
    }

    // The frame of every card, in a pinned seat: open without a toggle, its fold's name untouched, while the
    // folds inside the card (its details) are the viewer's as on the Plan tab.
    [Fact]
    public async Task ModuleCard_inAPinnedSeat_isOpenWithoutAToggleAndTouchesNoFold()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);

        var html = await RenderSeatedAsync(folds, Seated(new PinnedCard(TakesFocus: false), Card()));

        (html.Contains("id=\"card-split\""), html.Contains(Body), html.Contains("aria-expanded"), folds.IsOpen("split", openByDefault: false),
                storage.Get(Folds.StorageKey)).ShouldBe((true, true, false, false, null));
    }

    // Seated, a card holding a refusal reveals nothing either: the Plan tab's card stays as the viewer left it.
    [Fact]
    public async Task ModuleCard_inAPinnedSeatWithARefusal_revealsNothing()
    {
        var folds = new Folds(new BrowserStorage());

        await RenderSeatedAsync(folds, Seated(new PinnedCard(TakesFocus: false), Card(("Refused", Netto.Core.Vouchers.VoucherCalculator.EcoVouchersNegative))));

        folds.IsOpen("split", openByDefault: false).ShouldBeFalse();
    }

    // The fold inside the card keeps its toggle and the viewer's word, as on the Plan tab.
    [Fact]
    public async Task ModuleCard_inAPinnedSeat_keepsItsDetailsFoldTheViewers()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split-details", true);

        var html = await RenderSeatedAsync(folds, Seated(new PinnedCard(TakesFocus: false), Card(("Details", Text("The pay slip")))));

        (html.Contains("The pay slip"), Regex.Count(html, "<button[^>]*aria-expanded=\"true\"[^>]*>")).ShouldBe((true, 1));
    }

    // Out of the seat, the same card is what the Plan tab draws: folded to its title and summary.
    [Fact]
    public async Task ModuleCard_outOfAPinnedSeat_isFoldedWithItsToggle()
    {
        var html = await RenderAsync<ModuleCard>(new Folds(new BrowserStorage()), Card());

        (IsOpen(html), html.Contains(Body)).ShouldBe((false, false));
    }

    private static Task<string> RenderSeatedAsync(Folds folds, Dictionary<string, object?> seat) =>
        RenderAsync(typeof(CascadingValue<PinnedCard>), folds, seat);

    // A module card inside the seat the Map tab's panel gives it.
    private static Dictionary<string, object?> Seated(PinnedCard seat, Dictionary<string, object?> card) => new()
    {
        ["Value"] = seat,
        ["ChildContent"] = (RenderFragment)(builder =>
        {
            builder.OpenComponent<ModuleCard>(0);
            var sequence = 1;
            foreach (var (name, value) in card)
                builder.AddComponentParameter(sequence++, name, value);
            builder.CloseComponent();
        }),
    };

    private static Dictionary<string, object?> Card(params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["Name"] = "split",
            ["Title"] = "The split",
            ["Summary"] = "50,000 to your pay",
            ["ChildContent"] = Text(Body),
        };
        foreach (var (key, value) in more)
            parameters[key] = value;

        return parameters;
    }

    private static Dictionary<string, object?> Parameters(string name, params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["Name"] = name,
            ["Title"] = "The split",
            ["Summary"] = "50,000 to your pay",
            ["ChildContent"] = Text(Body),
            ["Pinned"] = true,
        };
        foreach (var (key, value) in more)
            parameters[key] = value;

        return parameters;
    }
}
