using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// What app.css does for a viewer who asks for reduced motion, as every browser on a machine with Windows'
// animation effects off does. A transition's property is all by default, so a duration above zero given to
// every element turns each change of any of its computed values into a transition, and a transition ends only
// at a frame the page draws: in a page that draws none, a background tab or a browser pane not on screen, the
// Map tab's map and column kept the offset under the bar they had before the bar was measured. How the page
// behaves is the browser's check; this holds the rule it rests on.
public class ReducedMotionCssTests
{
    [Fact]
    public void ReducedMotion_everyElementsTransition_endsAtOnceAndNoneStarts()
    {
        var css = Stylesheet.Read("src", "Netto.Web", "wwwroot", "app.css");

        var durations = Stylesheet.Rules(css)
            .Where(rule => rule.Selector.Split(',').Any(part => part.Trim().StartsWith('*')))
            .Select(rule => Regex.Match(rule.Declarations, @"transition-duration\s*:\s*([^;]+);"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value.Trim());

        durations.ShouldHaveSingleItem().ShouldBe("0s !important");
    }
}
