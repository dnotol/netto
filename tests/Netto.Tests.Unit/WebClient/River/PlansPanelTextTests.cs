using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class PlansPanelTextTests
{
    // The name IStringLocalizer<PlansPanel> reads River/PlansPanel.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.PlansPanel", typeof(PlansPanel).Assembly);

    [Theory]
    [InlineData(0, "Plans: none saved yet")]
    [InlineData(1, "Plans: 1 saved")]
    [InlineData(3, "Plans: 3 saved")]
    public void ButtonLabel_count_saysHowManyPlansAreSaved(int count, string expected)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(PlansPanelText.ButtonLabel(count), CultureInfo.InvariantCulture) ?? "", count);

        text.ShouldBe(expected);
    }

    [Theory]
    [InlineData(GainKind.More)]
    [InlineData(GainKind.Less)]
    [InlineData(GainKind.Same)]
    [InlineData(GainKind.NotComparable)]
    public void PlansPanelResx_everyGain_hasAText(GainKind kind)
    {
        Resources.GetString(PlansPanelText.Gain(kind), CultureInfo.InvariantCulture).ShouldNotBeNull();
    }

    // Saved plans are named Plan 1, Plan 2… until renamed.
    [Fact]
    public void DefaultName_number_isPlanAndTheNumber()
    {
        string.Format(CultureInfo.InvariantCulture, Resources.GetString("DefaultName", CultureInfo.InvariantCulture) ?? "", 2).ShouldBe("Plan 2");
    }
}
