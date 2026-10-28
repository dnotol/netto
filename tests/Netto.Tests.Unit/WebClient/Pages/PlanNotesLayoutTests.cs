using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The two notes under the map, « Assumed » and « Not counted yet », look alike: how they look in a window is
// the browser's check; these hold that one framed treatment, with one title, serves both and that neither
// has a look of its own, as a bare line and a dashed box had.
public class PlanNotesLayoutTests
{
    private static readonly string Css = Stylesheet.Read("src", "Netto.Web.Client", "Pages", "Plan.razor.css");

    [Fact]
    public void Notes_assumedAndLimits_shareOneFrame()
    {
        var frame = Stylesheet.Rules(Css).Single(rule => Squeezed(rule.Selector) == ".assumed,.limits").Declarations;

        frame.ShouldSatisfyAllConditions(
            () => frame.ShouldContain("border: 1px solid var(--netto-line)"),
            () => frame.ShouldContain("border-radius"),
            () => frame.ShouldContain("padding"),
            () => frame.ShouldContain("margin-top"));
    }

    [Fact]
    public void Notes_assumedAndLimits_haveNoLookOfTheirOwn()
    {
        var own = Stylesheet.Rules(Css).Where(rule => rule.Selector is ".assumed" or ".limits")
            .Where(rule => Regex.IsMatch(rule.Declarations, "border|padding|margin|radius")).ToList();

        own.ShouldBeEmpty();
        Css.ShouldNotContain("dashed");
    }

    [Fact]
    public void Notes_assumedAndLimits_shareOneTitle()
    {
        var titles = Stylesheet.Rules(Css).Where(rule => rule.Selector.Contains(".fold__title")).Select(rule => Squeezed(rule.Selector)).ToList();

        titles.ShouldBe([".assumed::deep.fold__heading--card.fold__title,.limits::deep.fold__heading--card.fold__title"]);
    }

    private static string Squeezed(string selector) => Regex.Replace(selector, @"\s+", "");
}
