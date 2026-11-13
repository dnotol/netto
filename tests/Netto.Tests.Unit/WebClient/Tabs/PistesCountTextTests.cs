using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// How the ideas' counts are said, in the badge short and in words: only the counts that are not zero, each
// agreeing with its number (PlanTabBarPistesBadgeTests says them on screen).
public class PistesCountTextTests
{
    [Theory]
    [InlineData(1, 0, "1")]
    [InlineData(0, 2, "2?")]
    [InlineData(1, 2, "1 · 2?")]
    public void Short_saysOnlyTheCountsThatAreNotZero(int priced, int questions, string expected)
    {
        PistesCountText.Short(priced, questions).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "Pistes_Priced_One")]
    [InlineData(2, "Pistes_Priced_Many")]
    public void Priced_agreesWithItsNumber(int count, string key)
    {
        PistesCountText.Priced(count).ShouldBe(key);
    }

    [Theory]
    [InlineData(1, "Pistes_Questions_One")]
    [InlineData(3, "Pistes_Questions_Many")]
    public void Questions_agreesWithItsNumber(int count, string key)
    {
        PistesCountText.Questions(count).ShouldBe(key);
    }

    // The component that says the counts holds the four words, in both languages.
    [Fact]
    public void Resx_ofTheBar_holdEveryCountWord()
    {
        var keys = new[] { PistesCountText.Priced(1), PistesCountText.Priced(2), PistesCountText.Questions(1), PistesCountText.Questions(2) };

        new[] { ResxText.For<PlanTabBar>(), ResxText.French<PlanTabBar>() }
            .SelectMany(texts => keys.Where(key => texts.Get(key) is null))
            .ShouldBeEmpty();
    }
}
