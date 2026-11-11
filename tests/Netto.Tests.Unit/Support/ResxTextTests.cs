using System.Globalization;
using System.Resources;
using Netto.Web.Client.Layout;
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
        Texts.Format(PlanTabBarText.Spoken(TabBadgeKind.MoreThanPlan), "€3").ShouldBe("€3 more than the plan on screen");
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

    // A component's French texts, as the page reads them in French: its .fr.resx alone, so a key it lacks reads
    // as missing rather than as its English text. NettoMudLocalizer's stands for any component's.
    [Fact]
    public void French_keyWithAFrenchText_givesTheFrenchText()
    {
        ResxText.French<NettoMudLocalizer>().Get("MudInput_Clear").ShouldBe("Effacer");
    }

    [Fact]
    public void French_keyWithoutAText_isNull()
    {
        ResxText.French<NettoMudLocalizer>().Get("No_such_key").ShouldBeNull();
    }

    [Fact]
    public void French_entries_areTheFrenchFilesOwn()
    {
        ResxText.French<NettoMudLocalizer>().Entries.ShouldContain(("MudInput_Clear", "Effacer"));
    }

    [Fact]
    public void French_culture_isTheFrenchUiCulture()
    {
        (ResxText.French<NettoMudLocalizer>().Culture.Name, ResxText.For<PlanTabBar>().Culture).ShouldBe(("fr", CultureInfo.InvariantCulture));
    }

    // A type with no .resx at all has no French one either: it fails at once, as For does.
    [Fact]
    public void French_typeWithoutAResx_fails()
    {
        Should.Throw<MissingManifestResourceException>(() => ResxText.French<RiverInputs>());
    }

    // The French texts of every component that has them, for a test that holds all the French texts to a rule.
    [Fact]
    public void InAssembly_inFrench_listsTheComponentsWithFrenchTexts()
    {
        var french = ResxText.FrenchInAssembly(typeof(PlanTabBar).Assembly).ToList();

        french.ShouldSatisfyAllConditions(
            () => french.Select(texts => texts.BaseName).ShouldContain(typeof(NettoMudLocalizer).FullName),
            () => french.ShouldAllBe(texts => texts.Culture.Name == "fr"),
            () => french.SelectMany(texts => texts.Entries).ShouldContain(("MudInput_Clear", "Effacer")));
    }
}
