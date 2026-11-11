using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The picture My plan draws, the viewer's, kept in the browser: the map unless they picked the Sankey.
public class PicturePreferenceTests
{
    [Fact]
    public void Current_nothingKept_isTheMap()
    {
        new PicturePreference(new BrowserStorage()).Current.ShouldBe(PlanPicture.Map);
    }

    [Theory]
    [InlineData("map", PlanPicture.Map)]
    [InlineData("sankey", PlanPicture.Sankey)]
    [InlineData("Sankey", PlanPicture.Map)]
    [InlineData("river", PlanPicture.Map)]
    [InlineData("", PlanPicture.Map)]
    public void Current_whatIsKept_onlyTheTwoWordsCount(string kept, PlanPicture picture)
    {
        var storage = new BrowserStorage();
        storage.Set(PicturePreference.StorageKey, kept);

        new PicturePreference(storage).Current.ShouldBe(picture);
    }

    [Theory]
    [InlineData(PlanPicture.Sankey, "sankey")]
    [InlineData(PlanPicture.Map, "map")]
    public void Set_keepsThePictureUnderItsKey(PlanPicture picture, string kept)
    {
        var storage = new BrowserStorage();
        var preference = new PicturePreference(storage);

        preference.Set(picture);

        (preference.Current, storage.Get("netto.plan.picture")).ShouldBe((picture, kept));
    }

    [Fact]
    public void Set_aPictureKept_isReadByTheNextLoad()
    {
        var storage = new BrowserStorage();
        new PicturePreference(storage).Set(PlanPicture.Sankey);

        new PicturePreference(storage).Current.ShouldBe(PlanPicture.Sankey);
    }
}
