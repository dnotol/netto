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

    // The river's number fields hide their spin buttons (RiverNumericField), the municipal rate's too: a card
    // that showed them would pass HideSpinButtons.
    [Fact]
    public void NumberFields_everyOne_leavesSpinButtonsHidden()
    {
        var fields = Tags("RiverNumericField").Concat(Tags("MudNumericField")).ToList();

        var shown = fields.Where(field => field.Tag.Contains("HideSpinButtons", StringComparison.Ordinal)).Select(field => field.Name).ToList();

        fields.Count.ShouldBeGreaterThan(30);
        shown.ShouldBeEmpty();
    }

    // Every number field of the river is a RiverNumericField: a MudNumericField placed directly would have
    // MudBlazor's spin buttons, its clamping of a refused value and none of the river's look.
    [Fact]
    public void NumberFields_noneIsPlacedBareFromMudBlazor()
    {
        Tags("MudNumericField").ShouldBeEmpty();
    }

    private static List<(string Name, string Tag)> Tags(string component) =>
        [.. RazorSources.Files().SelectMany(file => RazorSources.Tags(file.Text, component).Select(tag => (file.Name, tag)))];
}
