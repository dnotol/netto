using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Rent;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A rent above its cap, refused by the engine: the rent card says why once in sight, in its alert
// (RefusedMessage). The field keeps its own copy for a screen reader, out of sight (RiverField's style).
public class RentCardRefusalTests
{
    private static readonly ResxText Refusals = ResxText.For<RefusedMessage>();

    // 1,200 of cadastral income, 12% let: a cap of 1,380.00, one cent above it refused.
    private static readonly RiverInputs AboveTheCap = RiverInputs.Example.AddToPlan(OptionalModule.Rent)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380.01m);

    [Fact]
    public async Task Card_rentAboveItsCap_saysWhyOnceInSight()
    {
        var run = AboveTheCap.Run();
        var message = Refusals.Get($"Error_{RentCalculator.RentAboveCap.Key}").ShouldNotBeNull();

        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(RentCard), new Folds(new BrowserStorage()),
            new() { ["Inputs"] = AboveTheCap, ["Run"] = run, ["Refused"] = run.Error.ShouldNotBeNull() }));

        (Regex.Count(html, Regex.Escape(message)), OutOfSight(html, message)).ShouldBe((2, 1));
    }

    // On the Plan page, the rent card chosen beside the map: one alert says the refusal, the card's, none above
    // the map.
    [Fact]
    public async Task Plan_rentAboveItsCapWithItsCardChosen_saysWhyInOneAlert()
    {
        await using var page = Pages.LivePlan.At("http://localhost/plan?card=rent", new BrowserStorage(), river: AboveTheCap);
        await page.LoadAsync();

        var alerts = (await page.FindAllAsync<RefusedMessage>()).Where(alert => alert.Error is not null).ToList();
        var cards = await page.FindAllAsync<RentCard>();

        (cards.Count, alerts.Count).ShouldBe((1, 1));
    }

    // The copies inside a refused field's helper, which RiverField keeps out of sight.
    private static int OutOfSight(string html, string message) =>
        Regex.Matches(html, "river-number--refused\".*?</div>\\s*</div>\\s*</div>", RegexOptions.Singleline).Count(match => match.Value.Contains(message, StringComparison.Ordinal));
}
