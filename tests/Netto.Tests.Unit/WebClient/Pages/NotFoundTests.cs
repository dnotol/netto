using System.Text.RegularExpressions;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The page for an address Netto has no page for, drawn without a browser (RiverRendering). The app opens on
// the plan, so the way back from a dead link is the plan, by its own address: / would only send the viewer
// there, and the front page, at /about, is the portfolio's.
public class NotFoundTests
{
    [Fact]
    public async Task NotFound_button_goesBackToThePlan()
    {
        var html = await RenderAsync<NotFound>(new Folds(new BrowserStorage()), []);

        (Regex.IsMatch(html, "<a [^>]*href=\"/plan\"[^>]*>.*?Back to the plan", RegexOptions.Singleline), html.Contains("front page", StringComparison.OrdinalIgnoreCase))
            .ShouldBe((true, false));
    }
}
