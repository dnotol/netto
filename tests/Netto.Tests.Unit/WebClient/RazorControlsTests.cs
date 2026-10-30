using Shouldly;

namespace Netto.Tests.WebClient;

// The markup rules the site's look keeps, held on the .razor sources (RazorSources): a rule of the controls a
// component places, which no render of one branch would meet in all of them.
public class RazorControlsTests
{
    // Gold is an accent, never decoration: MudBlazor draws an unchecked box in the checkbox's Color, gold for
    // Primary, unless UncheckedColor says another.
    [Fact]
    public void MudCheckBoxes_everyOne_drawsItsUncheckedBoxInTheDefaultColour()
    {
        var boxes = Tags("MudCheckBox");

        var gold = boxes.Where(box => !box.Tag.Contains("UncheckedColor=\"Color.Default\"", StringComparison.Ordinal)).Select(box => box.Name).ToList();

        boxes.Count.ShouldBeGreaterThanOrEqualTo(14);
        gold.ShouldBeEmpty();
    }

    private static List<(string Name, string Tag)> Tags(string component) =>
        [.. RazorSources.Files().SelectMany(file => RazorSources.Tags(file.Text, component).Select(tag => (file.Name, tag)))];
}
