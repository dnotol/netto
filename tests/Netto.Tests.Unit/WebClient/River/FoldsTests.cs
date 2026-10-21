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

    private sealed class BrowserStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }

    private sealed class RefusingStorage : IBrowserStorage
    {
        public string? Get(string key) => null;

        public void Set(string key, string value)
        {
        }
    }
}
