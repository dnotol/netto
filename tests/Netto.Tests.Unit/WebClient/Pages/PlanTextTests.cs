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
}
