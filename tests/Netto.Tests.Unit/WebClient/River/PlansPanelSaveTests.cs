using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// « Save this plan » says itself whether the river has changes no saved plan holds (SavedPlans): a gold dot
// after its name, and in its name for a screen reader, so no line of the band says it and the band's height
// never depends on it. The dot's room is kept while there is nothing to save, so nothing beside it moves.
public class PlansPanelSaveTests
{
    [Fact]
    public async Task Save_riverWithChangesNoPlanHolds_showsTheDotAndSaysSo()
    {
        var save = SaveButton(await RenderPanelAsync(RiverInputs.Example.WithDirectorPay(40_000m)));

        (Regex.IsMatch(save, "<span class=\"plans__unsaved\" aria-hidden=\"true\"[^>]*></span>"), Spoken(save)).ShouldBe((true, "(unsaved changes)"));
    }

    // The river opens on the example, which no plan needs to hold.
    [Fact]
    public async Task Save_riverAsOpened_keepsTheDotsRoomAndSaysNothingMore()
    {
        var save = SaveButton(await RenderPanelAsync(RiverInputs.Example));

        (Regex.IsMatch(save, "<span class=\"plans__unsaved plans__unsaved--saved\" aria-hidden=\"true\"[^>]*></span>"), Spoken(save)).ShouldBe((true, null));
    }

    // The button's name as written, the dot and what it means included.
    private static string SaveButton(string html) =>
        Regex.Match(html, "<button [^>]*class=\"[^\"]*plans__save[^\"]*\"[^>]*>.*?</button>", RegexOptions.Singleline).Value;

    private static string? Spoken(string button) =>
        Regex.Match(button, "<span class=\"plans__spoken\"[^>]*>([^<]*)</span>") is { Success: true } spoken ? spoken.Groups[1].Value : null;

    private static async Task<string> RenderPanelAsync(RiverInputs river) =>
        WebUtility.HtmlDecode(await RenderAsync(typeof(PlansPanel), new Folds(new BrowserStorage()), new() { ["Inputs"] = river, ["CanSave"] = true },
            services =>
            {
                var plans = new SavedPlans(new BrowserStorage());
                services.AddSingleton(plans);
                services.AddSingleton(new RiverState(plans, new BrowserSession()));
            }));
}
