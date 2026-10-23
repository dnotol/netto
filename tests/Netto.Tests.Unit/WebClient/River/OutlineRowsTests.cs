using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The cards the river's outline lists (OutlineRows): each card's frame reports itself as it is drawn and
// leaves as it goes; the outline opens and folds them as the viewer's word, and tells which card the
// river has in view from where the cards stand in the window.
public class OutlineRowsTests
{
    // Where the window's line under the bar of buckets is, in pixels from its top.
    private const double Line = 237;

    [Fact]
    public void Rows_cardsReportedOutOfOrder_comeInTheRiversOrder()
    {
        var rows = new OutlineRows(new Folds(new Storage()));

        foreach (var name in new[] { "dividend", "corporate-tax", "director-pay", "copyright", "split", "company-year" })
            rows.Report(Row(name), Nothing);

        rows.Rows.Select(row => row.Name).ShouldBe(["company-year", "split", "copyright", "director-pay", "corporate-tax", "dividend"]);
    }

    // A lever taken out of the plan takes its card off the page, and its row out of the outline.
    [Fact]
    public void Leave_cardGone_hasNoRow()
    {
        var rows = new OutlineRows(new Folds(new Storage()));
        rows.Report(Row("vouchers"), Nothing);
        rows.Report(Row("split"), Nothing);

        rows.Leave("vouchers");

        rows.Rows.Select(row => row.Name).ShouldBe(["split"]);
    }

    // A card the outline has no place for is a card added to the page and forgotten here.
    [Fact]
    public void Report_cardNotInTheRiversOrder_isRefused()
    {
        var rows = new OutlineRows(new Folds(new Storage()));

        Should.Throw<ArgumentOutOfRangeException>(() => rows.Report(Row("another-card"), Nothing));
    }

    // Every card reports itself at every draw of the river: the outline redraws only for what changed.
    [Fact]
    public void Report_sameRowAgain_raisesNoChange()
    {
        var rows = new OutlineRows(new Folds(new Storage()));
        rows.Report(Row("split", "€60,000"), Nothing);
        var changes = 0;
        rows.Changed += () => changes++;

        rows.Report(Row("split", "€60,000"), Nothing);

        changes.ShouldBe(0);
    }

    [Fact]
    public void Report_newGlance_raisesAChange()
    {
        var rows = new OutlineRows(new Folds(new Storage()));
        rows.Report(Row("split", "€60,000"), Nothing);
        var changes = 0;
        rows.Changed += () => changes++;

        rows.Report(Row("split", "€62,000"), Nothing);

        (changes, rows.Rows.Single().Glance?.Figure).ShouldBe((1, "€62,000"));
    }

    [Fact]
    public void Leave_cardNotListed_raisesNoChange()
    {
        var rows = new OutlineRows(new Folds(new Storage()));
        var changes = 0;
        rows.Changed += () => changes++;

        rows.Leave("vouchers");

        changes.ShouldBe(0);
    }

    // Choosing a folded card opens it as the viewer would, so it stays open after a reload, and draws it
    // again open.
    [Fact]
    public void Open_foldedCard_isRememberedOpenAndRedrawn()
    {
        var storage = new Storage();
        var rows = new OutlineRows(new Folds(storage));
        var redrawn = 0;
        rows.Report(Row("vouchers"), () => redrawn++);

        rows.Open("vouchers");

        (new Folds(storage).IsOpen("vouchers", false), redrawn).ShouldBe((true, 1));
    }

    // A card already open for the visit is left as it is: nothing is written over the viewer's folds.
    [Fact]
    public void Open_cardOpenForTheVisit_writesNoFold()
    {
        var storage = new Storage();
        var folds = new Folds(storage);
        folds.Reveal("split");
        var rows = new OutlineRows(folds);
        var redrawn = 0;
        rows.Report(Row("split"), () => redrawn++);

        rows.Open("split");

        (storage.Writes, redrawn).ShouldBe((0, 0));
    }

    [Fact]
    public void Open_cardNotListed_doesNothing()
    {
        var storage = new Storage();
        var rows = new OutlineRows(new Folds(storage));

        rows.Open("vouchers");

        storage.Writes.ShouldBe(0);
    }

    [Fact]
    public void AllOpen_oneCardFolded_isFalse()
    {
        var folds = new Folds(new Storage());
        folds.Set("split", true);
        var rows = new OutlineRows(folds);
        rows.Report(Row("split"), Nothing);
        rows.Report(Row("dividend"), Nothing);

        rows.AllOpen.ShouldBeFalse();
    }

    // Open by the viewer or for the visit: each counts as open.
    [Fact]
    public void AllOpen_everyCardOpen_isTrue()
    {
        var folds = new Folds(new Storage());
        folds.Set("split", true);
        folds.Reveal("vouchers");
        var rows = new OutlineRows(folds);
        rows.Report(Row("split"), Nothing);
        rows.Report(Row("vouchers"), Nothing);

        rows.AllOpen.ShouldBeTrue();
    }

    // « Open all » is the viewer's word on every card listed, remembered for each, and every card is
    // drawn again.
    [Fact]
    public void SetAll_open_remembersEveryListedCardOpenAndRedrawsThem()
    {
        var storage = new Storage();
        var rows = new OutlineRows(new Folds(storage));
        var redrawn = new List<string>();
        foreach (var name in new[] { "company-year", "split", "dividend" })
            rows.Report(Row(name), () => redrawn.Add(name));

        rows.SetAll(true);

        var reloaded = new Folds(storage);
        (reloaded.IsOpen("company-year", false), reloaded.IsOpen("split", false), reloaded.IsOpen("dividend", false), redrawn.Count).ShouldBe((true, true, true, 3));
    }

    // « Fold all » folds a card open for the visit too: the viewer's word replaces the page's.
    [Fact]
    public void SetAll_folded_foldsEveryListedCardEvenOneOpenForTheVisit()
    {
        var storage = new Storage();
        var folds = new Folds(storage);
        folds.Reveal("vouchers");
        folds.Set("split", true);
        var rows = new OutlineRows(folds);
        rows.Report(Row("vouchers"), Nothing);
        rows.Report(Row("split"), Nothing);

        rows.SetAll(false);

        (folds.IsOpen("vouchers", false), folds.IsOpen("split", false), rows.AllOpen).ShouldBe((false, false, false));
    }

    // At the top of the page no card has reached the bar yet: the first card is the one in view.
    [Fact]
    public void InView_noCardPassedTheLineYet_isTheFirstCard()
    {
        OutlineRows.InView([Box("company-year", 500, 578), Box("split", 598, 676)], Line, atEnd: false).ShouldBe("company-year");
    }

    [Fact]
    public void InView_cardAcrossTheLine_isIt()
    {
        OutlineRows.InView([Box("company-year", -300, -100), Box("split", -80, 400), Box("director-pay", 420, 900)], Line, atEnd: false)
            .ShouldBe("split");
    }

    // A card jumped to sits with its top on the line, give or take a pixel's rounding.
    [Fact]
    public void InView_cardJumpedTo_isIt()
    {
        OutlineRows.InView([Box("company-year", -80, 216), Box("split", Line + 0.5, 900)], Line, atEnd: false).ShouldBe("split");
    }

    // Two streams side by side both cross the line: the card whose heading passed under the bar last is
    // the one the viewer has just reached.
    [Fact]
    public void InView_twoStreamsAcrossTheLine_isTheCardWhoseHeadingPassedLast()
    {
        OutlineRows.InView([Box("director-pay", -400, 900), Box("corporate-tax", -400, -100), Box("dividend", 100, 600)], Line, atEnd: false)
            .ShouldBe("dividend");
    }

    // The two streams start on the same line: the first in the river's order, the director's, is in view.
    [Fact]
    public void InView_twoStreamsStartingTogether_isTheFirstInTheRiver()
    {
        OutlineRows.InView([Box("split", -400, -20), Box("copyright", 100, 300), Box("corporate-tax", 100, 500)], Line, atEnd: false)
            .ShouldBe("copyright");
    }

    // Between two cards the line falls in the gap: the card above, the last to pass, is still in view.
    [Fact]
    public void InView_lineBetweenTwoCards_isTheCardAbove()
    {
        OutlineRows.InView([Box("company-year", -300, -100), Box("split", -80, 230), Box("director-pay", 250, 900)], Line, atEnd: false)
            .ShouldBe("split");
    }

    // The last cards may never reach the bar: at the page's end, the last card is in view.
    [Fact]
    public void InView_pageAtItsEnd_isTheLastCard()
    {
        OutlineRows.InView([Box("corporate-tax", -100, 300), Box("dividend", 320, 398)], Line, atEnd: true).ShouldBe("dividend");
    }

    [Fact]
    public void InView_noCards_isNone()
    {
        OutlineRows.InView([], Line, atEnd: false).ShouldBeNull();
    }

    private static OutlineRow Row(string name, string? figure = "€1,000.00") =>
        new(name, name, figure is null ? null : new CardGlance(GlanceKind.ToYou, figure));

    private static CardBox Box(string name, double top, double bottom) => new(name, top, bottom);

    private static void Nothing()
    {
    }

    // The browser's storage, counting what is written to it.
    private sealed class Storage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public int Writes { get; private set; }

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value)
        {
            Writes++;
            _stored[key] = value;
        }
    }
}
