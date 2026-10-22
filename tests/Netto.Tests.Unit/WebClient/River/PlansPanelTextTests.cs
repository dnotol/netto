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

    // Beside a plan's gain in cash, what its muddy pocket holds more or less than Today's.
    [Theory]
    [InlineData(8_000, "€8,000.00 more in the muddy pocket")]
    [InlineData(-8_000, "€8,000.00 less in the muddy pocket")]
    public void Muddy_difference_saysHowMuchMoreOrLessThanToday(int difference, string expected)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(PlansPanelText.Muddy(difference), CultureInfo.InvariantCulture) ?? "",
            Netto.Web.Client.Formats.Euros(Math.Abs(difference)));

        text.ShouldBe(expected);
    }

    // Saved plans are named Plan 1, Plan 2… until renamed.
    [Fact]
    public void DefaultName_number_isPlanAndTheNumber()
    {
        string.Format(CultureInfo.InvariantCulture, Resources.GetString("DefaultName", CultureInfo.InvariantCulture) ?? "", 2).ShouldBe("Plan 2");
    }
}
