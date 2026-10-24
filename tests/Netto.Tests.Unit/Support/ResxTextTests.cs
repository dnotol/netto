using System.Resources;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.Support;

// A component's .resx as IStringLocalizer<TComponent> reads it, for a test to hold the keys a helper
// builds to the texts that are there. PlanTabBar.resx stands for any component's.
public class ResxTextTests
{
    private static readonly ResxText Texts = ResxText.For<PlanTabBar>();

    [Fact]
    public void Get_keyWithAText_givesTheText()
    {
        Texts.Get(PlanTabBarText.Label).ShouldBe("Your plan");
    }

    [Fact]
    public void Get_keyWithoutAText_isNull()
    {
        Texts.Get("No_such_key").ShouldBeNull();
    }

    [Fact]
    public void Format_placeholder_isFilledAsTheSiteWritesIt()
    {
        Texts.Format(PlanTabBarText.Spoken(TabBadgeKind.SavedPlans), "3").ShouldBe("3 saved plans to compare");
    }

    // What a viewer would read where the text is missing: the key itself, as the localizer shows it.
    [Fact]
    public void Format_keyWithoutAText_isTheKey()
    {
        Texts.Format("No_such_key", "3").ShouldBe("No_such_key");
    }

    [Fact]
    public void Missing_keys_listsEachKeyWithoutATextOnce()
    {
        Texts.Missing([PlanTabBarText.Label, "No_such_key", "Another_missing_key", "No_such_key"]).ShouldBe(["No_such_key", "Another_missing_key"]);
    }

    [Fact]
    public void Keys_ofTheComponent_includeItsKeys()
    {
        Texts.Keys.ShouldContain(PlanTabBarText.Label);
    }

    [Fact]
    public void Entries_ofTheComponent_pairEachKeyWithItsText()
    {
        Texts.Entries.ShouldContain((PlanTabBarText.Label, "Your plan"));
    }

    // Every component of the client, found as the build embedded it: what a test that holds all the interface's
    // texts to a rule reads.
    [Fact]
    public void InAssembly_theClient_listsTheResxOfEachComponent()
    {
        var components = ResxText.InAssembly(typeof(PlanTabBar).Assembly).Select(texts => texts.BaseName).ToList();

        components.ShouldContain(typeof(PlanTabBar).FullName);
        components.ShouldContain(typeof(InfoBubble).FullName);
    }

    // The other component's texts are not this one's.
    [Fact]
    public void For_otherComponent_readsItsOwnResx()
    {
        ResxText.For<InfoBubble>().Get(PlanTabBarText.Label).ShouldBeNull();
    }

    // A type that has no .resx would make every key look missing, and a test of a text that is gone
    // (ShouldBeNull) pass for nothing: it fails at once instead.
    [Fact]
    public void For_typeWithoutAResx_fails()
    {
        Should.Throw<MissingManifestResourceException>(() => ResxText.For<RiverInputs>());
    }
}
