using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the band's stylesheet keeps in its columns, at every width, whatever the plan: how it draws in a
// browser is the browser check's; these hold the rules it rests on.
public class BucketBarCssTests
{
    private static readonly string Css = Stylesheet.WithoutComments(Stylesheet.Read("src", "Netto.Web.Client", "River", "BucketBar.razor.css"));

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

    // From 641 to 1,200 px the pocket's column is some 210 to 270 px wide, too narrow for a pocket of
    // €999,999.99, « 60% reaches you » and its (i) side by side: the words go out of sight there, kept for a
    // screen reader, the share and its (i) staying. On a phone the pocket takes the band's width, and from
    // 1,201 px its column has room for them.
    [Fact]
    public void ReachWords_inANarrowPocketsColumn_goOutOfSightForAScreenReader()
    {
        var narrow = Stylesheet.Block(Stylesheet.MediaBlock(Css, "(min-width: 641px) and (max-width: 1200px)"), ".bucket__reach-words");

        narrow.ShouldSatisfyAllConditions(
            () => narrow.ShouldContain("position: absolute;"),
            () => narrow.ShouldContain("clip-path: inset(50%);"),
            () => narrow.ShouldContain("width: 1px;"),
            () => narrow.ShouldNotContain("display: none"));
    }

    // A bucket's amount, a button choosing the bucket, is drawn as the figure it is: the amount's type, size,
    // line height and colour, no padding, frame or ground of the browser's, so the band's rows, measured by
    // bucket-bar.js, keep their height whether it is a button or not.
    [Fact]
    public void Choose_isDrawnAsTheAmountItIs()
    {
        var choose = Stylesheet.Block(Css, ".bucket__choose");

        choose.ShouldSatisfyAllConditions(
            () => choose.ShouldContain("padding: 0;"),
            () => choose.ShouldContain("border: 0;"),
            () => choose.ShouldContain("background: none;"),
            () => choose.ShouldContain("color: inherit;"),
            () => choose.ShouldContain("font: inherit;"),
            () => choose.ShouldContain("line-height: inherit;"),
            () => choose.ShouldContain("cursor: pointer;"));
    }

    // The button reaches over its whole bucket, so the bucket is the target, the bubbles of its name, its gain and
    // its reach staying over it, each its own button.
    [Fact]
    public void Choose_coversItsWholeBucketUnderItsBubbles()
    {
        var (cover, trigger) = (Stylesheet.Block(Css, ".bucket__choose::after"), Stylesheet.Block(Css, ".bucket ::deep .info-bubble__trigger"));

        Css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(Css, ".bucket").ShouldContain("position: relative;"),
            () => cover.ShouldContain("content: \"\";"),
            () => cover.ShouldContain("position: absolute;"),
            () => cover.ShouldContain("inset: 0;"),
            () => trigger.ShouldContain("position: relative;"),
            () => trigger.ShouldContain("z-index: 1;"));
    }

    // What the button does, after its amount, is out of sight, for a screen reader.
    [Fact]
    public void ChooseWords_areOutOfSightForAScreenReader()
    {
        var words = Stylesheet.Block(Css, ".bucket__choose-words");

        words.ShouldSatisfyAllConditions(
            () => words.ShouldContain("position: absolute;"),
            () => words.ShouldContain("clip-path: inset(50%);"),
            () => words.ShouldContain("overflow: hidden;"));
    }
}
