using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the band's stylesheet keeps in its columns, at every width, whatever the plan: how it draws in a
// browser is the browser check's; these hold the rules it rests on.
public class BucketBarCssTests
{
    private static readonly string Css = Regex.Replace(
        Stylesheet.Read("src", "Netto.Web.Client", "River", "BucketBar.razor.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    // A name is the trigger of its bubble, a button, which centres what it holds: a name too long for its
    // column would overflow on both sides and lose its start. It is clamped to one line from its left, so it
    // ends on an ellipsis and keeps its start, wrapping where its label does not so the clamp has a line to
    // end on.
    [Fact]
    public void Name_tooLongForItsColumn_keepsItsStartAndEndsOnAnEllipsis()
    {
        var name = Stylesheet.Block(Css, ".bucket__label ::deep .bucket__name");

        name.ShouldSatisfyAllConditions(
            () => name.ShouldContain("text-align: left;"),
            () => name.ShouldContain("display: -webkit-box;"),
            () => name.ShouldContain("-webkit-box-orient: vertical;"),
            () => name.ShouldContain("-webkit-line-clamp: 1;"),
            () => name.ShouldContain("overflow: hidden;"),
            () => name.ShouldContain("white-space: normal;"));
    }
}
