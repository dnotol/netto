using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A fold is a viewer's preference, kept in the browser's storage: a new Folds over the same storage
// is the page after a reload.
public class FoldsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsOpen_foldNeverTouched_hasItsDefault(bool openByDefault)
    {
        var folds = new Folds(new BrowserStorage());

        folds.IsOpen("split", openByDefault).ShouldBe(openByDefault);
    }

    [Fact]
    public void Set_cardFolded_staysFoldedAfterAReload()
    {
        var storage = new BrowserStorage();
        new Folds(storage).Set("split", false);

        new Folds(storage).IsOpen("split", true).ShouldBeFalse();
    }

    [Fact]
    public void Set_conditionsOpened_stayOpenAfterAReload()
    {
        var storage = new BrowserStorage();
        new Folds(storage).Set("vouchers-conditions", true);

        new Folds(storage).IsOpen("vouchers-conditions", false).ShouldBeTrue();
    }

    [Fact]
    public void Set_oneFold_keepsTheOthers()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Set("split", false);
        folds.Set("vouchers-conditions", true);
        folds.Set("split", true);

        var reloaded = new Folds(storage);

        (reloaded.IsOpen("split", false), reloaded.IsOpen("vouchers-conditions", false), reloaded.IsOpen("dividend", true))
            .ShouldBe((true, true, true));
    }

    // A private window or blocked site data: nothing is kept across a reload, but the fold holds
    // while the page is open.
    [Fact]
    public void Set_storageRefused_foldHoldsUntilTheReload()
    {
        var storage = new RefusingStorage();
        var folds = new Folds(storage);

        folds.Set("split", false);

        (folds.IsOpen("split", true), new Folds(storage).IsOpen("split", true)).ShouldBe((false, true));
    }

    // Whatever else sits under Netto's key, from an older version or a hand edit, is ignored.
    [Fact]
    public void IsOpen_storedEntryNotAFold_isIgnored()
    {
        var storage = new BrowserStorage();
        storage.Set(Folds.StorageKey, "split=0;junk;=1;dividend=yes;company-year=1;vouchers=0=1");

        var folds = new Folds(storage);

        (folds.IsOpen("split", true), folds.IsOpen("company-year", false), folds.IsOpen("dividend", true), folds.IsOpen("vouchers", true))
            .ShouldBe((false, true, true, true));
    }

    // A card opened by the page, not by the viewer: a lever just added, a refusal that must be seen.
    // It is open over the card's default, for the session.
    [Fact]
    public void Reveal_foldFoldedByDefault_isOpen()
    {
        var folds = new Folds(new BrowserStorage());

        folds.Reveal("rent");

        folds.IsOpen("rent", false).ShouldBeTrue();
    }

    [Fact]
    public void Reveal_foldTheViewerFolded_isOpen()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split", false);

        folds.Reveal("split");

        folds.IsOpen("split", true).ShouldBeTrue();
    }

    // A reveal is not the viewer's preference: a reload, a new Folds over the same storage, forgets it.
    [Fact]
    public void Reveal_isNotRememberedAfterAReload()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Reveal("rent");
        folds.Set("split", false);

        var reloaded = new Folds(storage);

        (reloaded.IsOpen("rent", false), reloaded.IsOpen("split", true)).ShouldBe((false, false));
        storage.Get(Folds.StorageKey).ShouldBe("split=0");
    }

    [Fact]
    public void Reveal_oneFold_leavesTheOthersAtTheirState()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("split", false);

        folds.Reveal("rent");

        (folds.IsOpen("split", true), folds.IsOpen("car", false), folds.IsOpen("dividend", true)).ShouldBe((false, false, true));
    }

    // The viewer folds a revealed card: their word wins, and is remembered.
    [Fact]
    public void Set_foldedAfterAReveal_isFolded()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Reveal("rent");

        folds.Set("rent", false);

        (folds.IsOpen("rent", true), new Folds(storage).IsOpen("rent", true)).ShouldBe((false, false));
    }

    // The viewer's open replaces the reveal and is remembered, as any open of theirs.
    [Fact]
    public void Set_openedAfterAReveal_staysOpenAfterAReload()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Reveal("rent");

        folds.Set("rent", true);

        new Folds(storage).IsOpen("rent", false).ShouldBeTrue();
    }

    // Folded by the viewer, then revealed again (a refusal back, a lever added again): open again.
    [Fact]
    public void Reveal_afterTheViewerFoldedARevealedFold_opensItAgain()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Reveal("rent");
        folds.Set("rent", false);

        folds.Reveal("rent");

        folds.IsOpen("rent", false).ShouldBeTrue();
    }

    // What reads the folds from outside them, the outline's « Open all » or « Fold all », hears of every
    // fold the viewer opens or closes, and of every fold the page opens.
    [Fact]
    public void Set_aFold_tellsItsReaders()
    {
        var folds = new Folds(new BrowserStorage());
        var told = 0;
        folds.Changed += () => told++;

        folds.Set("split", true);

        told.ShouldBe(1);
    }

    [Fact]
    public void Reveal_aFold_tellsItsReaders()
    {
        var folds = new Folds(new BrowserStorage());
        var told = 0;
        folds.Changed += () => told++;

        folds.Reveal("rent");

        told.ShouldBe(1);
    }

    // A refused card reveals its fold at every draw: only the first opens anything.
    [Fact]
    public void Reveal_foldAlreadyRevealed_tellsNoOne()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Reveal("rent");
        var told = 0;
        folds.Changed += () => told++;

        folds.Reveal("rent");

        told.ShouldBe(0);
    }
}
