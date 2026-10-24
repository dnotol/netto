using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The outline's texts, held to RiverOutline.resx: a misspelt key would show on screen as the key itself.
public class RiverOutlineTextTests
{
    // The name IStringLocalizer<RiverOutline> reads River/RiverOutline.resx under.
    private static readonly ResxText Resources = ResxText.For<RiverOutline>();

    [Fact]
    public void RiverOutlineResx_label_namesTheOutline()
    {
        Resources.Get(RiverOutlineText.Label).ShouldBe("River outline");
    }

    // One figure of the card's summary, in the summary's own words.
    [Theory]
    [InlineData(GlanceKind.In, "€120,000.00 in")]
    [InlineData(GlanceKind.ToYou, "€120,000.00 to you")]
    [InlineData(GlanceKind.ToYourPay, "€120,000.00 to your pay")]
    [InlineData(GlanceKind.CarCosts, "€120,000.00 of car costs")]
    [InlineData(GlanceKind.TaxedAsPay, "€120,000.00 taxed as your pay")]
    [InlineData(GlanceKind.LeftAfterTax, "€120,000.00 left after tax")]
    public void RiverOutlineResx_everyGlance_saysItsFigureInTheSummarysWords(GlanceKind kind, string expected)
    {
        Resources.Format(RiverOutlineText.Glance(kind), "€120,000.00").ShouldBe(expected);
    }

    // The toggle in the outline's head says what it will do, and a screen reader hears which cards.
    [Theory]
    [InlineData(false, "Open all", "Open all cards")]
    [InlineData(true, "Fold all", "Fold all cards")]
    public void RiverOutlineResx_toggle_saysWhatItWillDo(bool allOpen, string label, string name)
    {
        (Resources.Get(RiverOutlineText.All(allOpen)), Resources.Get(RiverOutlineText.AllName(allOpen))).ShouldBe((label, name));
    }
}
