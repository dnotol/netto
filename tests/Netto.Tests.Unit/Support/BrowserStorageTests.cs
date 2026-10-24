using Shouldly;

namespace Netto.Tests.Support;

// The browser's storage as the tests stand in for it: kept in memory, or refusing as a private window
// does.
public class BrowserStorageTests
{
    [Fact]
    public void Get_nothingStored_isNull()
    {
        new BrowserStorage().Get("netto.folds").ShouldBeNull();
    }

    [Fact]
    public void Get_afterSet_givesTheValue()
    {
        var storage = new BrowserStorage();

        storage.Set("netto.theme", "light");

        storage.Get("netto.theme").ShouldBe("light");
    }

    [Fact]
    public void Set_sameKeyTwice_keepsTheLast()
    {
        var storage = new BrowserStorage();

        storage.Set("netto.theme", "light");
        storage.Set("netto.theme", "dark");

        storage.Get("netto.theme").ShouldBe("dark");
    }

    [Fact]
    public void Writes_eachSet_isCounted()
    {
        var storage = new BrowserStorage();

        storage.Set("netto.theme", "light");
        storage.Set("netto.theme", "light");
        storage.Set("netto.folds", "{}");
        _ = storage.Get("netto.theme");

        storage.Writes.ShouldBe(3);
    }

    [Fact]
    public void Get_refusingStorageAfterSet_isStillNull()
    {
        var storage = new RefusingStorage();

        storage.Set("netto.theme", "light");

        storage.Get("netto.theme").ShouldBeNull();
    }
}
