using System.Globalization;
using System.Resources;
using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The river page's texts that a test holds to its .resx: a misspelt key would show on screen as the
// key itself.
public class PlanTextTests
{
    // The name IStringLocalizer<Plan> reads Pages/Plan.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Pages.Plan", typeof(Plan).Assembly);

    // The keyboard's first stop on the page.
    [Fact]
    public void PlanResx_skipLink_saysWhereItGoes()
    {
        var text = Resources.GetString(PlanText.SkipToRiver, CultureInfo.InvariantCulture);

        text.ShouldBe("Skip to the river");
    }

    // Under the split, the royalty runs down the director's side with the pay once it is in the plan.
    [Theory]
    [InlineData(false, "↓ Your pay on one side, the company's profit on the other ↓")]
    [InlineData(true, "↓ Your pay and royalty on one side, the company's profit on the other ↓")]
    public void PlanResx_fork_saysWhatRunsDownEachSide(bool royalty, string expected)
    {
        Resources.GetString(PlanText.Fork(royalty), CultureInfo.InvariantCulture).ShouldBe(expected);
    }
}
