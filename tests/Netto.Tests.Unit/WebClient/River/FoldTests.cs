using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A fold as the page draws it, rendered to HTML without a browser (RiverRendering): the state the viewer
// sees on first load, before any click. Clicks are Folds' own tests.
public class FoldTests
{
    private const string Body = "the-fold-body";

    // The river opens as a list of its cards' summaries: nothing is open until the viewer opens it.
    [Fact]
    public async Task Fold_neverTouched_isFoldedToItsTitleAndSummary()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split"));

        (IsOpen(html), html.Contains(Body), html.Contains("The split"), html.Contains("50,000 to your pay")).ShouldBe((false, false, true, true));
    }

    [Fact]
    public void Fold_defaultState_isFolded()
    {
        (new Fold().OpenByDefault, new ModuleCard().OpenByDefault).ShouldBe((false, false));
    }

    [Fact]
    public async Task Fold_openedByTheViewer_isOpen()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split", true);

        var html = await RenderAsync<Fold>(folds, Parameters("split"));

        (IsOpen(html), html.Contains(Body)).ShouldBe((true, true));
    }

    // The viewer's word wins over what the card asks for by default.
    [Fact]
    public async Task Fold_foldedByTheViewerOverADefaultOpen_isFolded()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("moves", false);

        var html = await RenderAsync<Fold>(folds, Parameters("moves", ("OpenByDefault", true)));

        (IsOpen(html), html.Contains(Body)).ShouldBe((false, false));
    }

    [Fact]
    public async Task Fold_revealed_isOpen()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Reveal("rent");

        var html = await RenderAsync<Fold>(folds, Parameters("rent"));

        (IsOpen(html), html.Contains(Body)).ShouldBe((true, true));
    }

    // A refusal is never hidden: the card opens over a fold the viewer made, and the toggle cannot fold
    // it again while the refusal stands.
    [Fact]
    public async Task Fold_refused_isOpenWithItsToggleDisabled()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split", false);

        var html = await RenderAsync<Fold>(folds, Parameters("split", ("Refused", true)));

        (IsOpen(html), IsDisabled(html), html.Contains(Body)).ShouldBe((true, true, true));
    }

    [Fact]
    public async Task Fold_notRefused_hasItsToggleEnabled()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("split", ("Refused", false)));

        IsDisabled(html).ShouldBeFalse();
    }

    // Opened by a refusal, the card stays open once the amount is accepted, for the field not to vanish
    // under the cursor, until the viewer folds it.
    [Fact]
    public async Task Fold_refusalEnded_staysOpenAndCanBeFolded()
    {
        var folds = new Folds(new BrowserStorage());
        await RenderAsync<Fold>(folds, Parameters("split", ("Refused", true)));

        var html = await RenderAsync<Fold>(folds, Parameters("split", ("Refused", false)));

        (IsOpen(html), IsDisabled(html), html.Contains(Body)).ShouldBe((true, false, true));
    }

    // A fold inside a card, such as its conditions, is not a card: folded until opened.
    [Fact]
    public async Task Fold_levelThreeNeverTouched_isFolded()
    {
        var html = await RenderAsync<Fold>(new Folds(new BrowserStorage()), Parameters("vouchers-conditions", ("Level", 3)));

        (IsOpen(html), html.Contains(Body)).ShouldBe((false, false));
    }

    private static Dictionary<string, object?> Parameters(string name, params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["Name"] = name,
            ["Title"] = "The split",
            ["Summary"] = "50,000 to your pay",
            ["ChildContent"] = Text(Body),
        };
        foreach (var (key, value) in more)
            parameters[key] = value;

        return parameters;
    }
}
