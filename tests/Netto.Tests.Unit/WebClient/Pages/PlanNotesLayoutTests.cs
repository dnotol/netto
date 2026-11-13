using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The two notes under the map, « Assumed » and « Not counted yet », are footnotes: how they look in a window is
// the browser's check; these hold that one muted treatment serves both, with no card frame, and that neither has
// a look of its own, as a bare line and a dashed box had.
public class PlanNotesLayoutTests
{
    private static readonly string Css = Stylesheet.Read("src", "Netto.Web.Client", "Pages", "Plan.razor.css");

    [Fact]
    public void Notes_assumedAndLimits_shareOneFootnoteLookWithNoCardFrame()
    {
        var note = Stylesheet.Rules(Css).Single(rule => Squeezed(rule.Selector) == ".assumed,.limits").Declarations;

        note.ShouldSatisfyAllConditions(
            () => note.ShouldContain("margin-top"),
            () => note.ShouldContain("color: var(--netto-text-muted)"),
            () => note.ShouldNotContain("border"),
            () => note.ShouldNotContain("padding"),
            () => note.ShouldNotContain("background"));
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
    public void Notes_assumedAndLimits_shareOneSmallTitleInTheTextsOwnFace()
    {
        var titles = Stylesheet.Rules(Css).Where(rule => rule.Selector.Contains(".fold__title")).ToList();

        titles.Select(rule => Squeezed(rule.Selector)).ShouldBe([".assumed::deep.fold__heading--card.fold__title,.limits::deep.fold__heading--card.fold__title"]);
        titles.Single().Declarations.ShouldSatisfyAllConditions(
            () => titles.Single().Declarations.ShouldContain("font-family: inherit"),
            () => titles.Single().Declarations.ShouldContain("font-size: 0.85rem"));
    }

    // The toggle stays a target of at least 24 px (WCAG 2.5.8) in the text's muted colour, which is AA on the page
    // in both themes (NettoThemeTests), not the 44 px row of a card.
    [Fact]
    public void Notes_assumedAndLimits_shareOneMutedToggleOfAtLeast24Pixels()
    {
        var toggle = Stylesheet.Rules(Css).Single(rule => Squeezed(rule.Selector) == ".assumed::deep.fold__toggle,.limits::deep.fold__toggle").Declarations;

        toggle.ShouldSatisfyAllConditions(
            () => toggle.ShouldContain("color: var(--netto-text-muted)"),
            () => toggle.ShouldContain("min-height: 2rem"));
    }

    // The assumptions are plain lines like the limits, not pills in a frame.
    [Fact]
    public void Notes_assumptions_areNoPills()
    {
        Stylesheet.Rules(Css).Where(rule => rule.Selector.Contains(".assumed__list"))
            .ShouldAllBe(rule => !rule.Declarations.Contains("border") && !rule.Declarations.Contains("radius"));
    }

    private static string Squeezed(string selector) => Regex.Replace(selector, @"\s+", "");
}
