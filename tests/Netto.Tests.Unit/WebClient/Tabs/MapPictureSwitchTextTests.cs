using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The switch's texts, held to MapPictureSwitch.resx and its French: a picture added without its name would show its
// key on screen instead.
public class MapPictureSwitchTextTests
{
    [Fact]
    public void Name_everyPicture_inEnglish()
    {
        var texts = ResxText.For<MapPictureSwitch>();

        Enum.GetValues<PlanPicture>().Select(picture => texts.Get(MapPictureSwitchText.Name(picture))).ShouldBe(["Map", "Sankey"]);
    }

    [Fact]
    public void Name_everyPicture_inFrench()
    {
        var texts = ResxText.French<MapPictureSwitch>();

        Enum.GetValues<PlanPicture>().Select(picture => texts.Get(MapPictureSwitchText.Name(picture))).ShouldBe(["Carte", "Sankey"]);
    }

    [Fact]
    public void Name_aPictureNoArmNames_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapPictureSwitchText.Name((PlanPicture)99));
    }
}
