using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The river page as a viewer meets it on first load, drawn whole without a browser (RiverRendering):
// every card folded to its summary, the notes under it folded to one line each.
public class PlanPageTests
{
    // The example opens with Your moves (no Today yet), Company year, the split, Director pay, Corporate
    // tax, Dividend and When to pay: seven cards, and the two notes under them.
    [Fact]
    public async Task Plan_firstLoad_foldsEveryCardAndBothNotes()
    {
        var toggles = Toggles(await RenderPlanAsync(new Folds(new BrowserStorage())));

        (toggles.Count, toggles.Count(toggle => toggle.Contains("aria-expanded=\"false\""))).ShouldBe((9, 9));
    }

    [Fact]
    public async Task Plan_notes_sayWhatTheyHold()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("8 assumptions Netto makes"), html.Contains("4 things the figures leave out")).ShouldBe((true, true));
    }

    // The notes are folds like the cards: opened, they show their lines.
    [Fact]
    public async Task Plan_notesOpenedByTheViewer_showTheirLines()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(PlanText.AssumedFold, true);
        folds.Set(PlanText.LimitsFold, true);

        var html = await RenderPlanAsync(folds);

        (html.Contains("Resident in Wallonia"), html.Contains("management fee")).ShouldBe((true, true));
    }

    [Fact]
    public async Task Plan_notesFolded_hideTheirLines()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        (html.Contains("Resident in Wallonia"), html.Contains("management fee")).ShouldBe((false, false));
    }

    // The page's headings stay as they were: each note's title is the h2 holding its button, the id its
    // aside is named by.
    [Fact]
    public async Task Plan_notes_areNamedByTheirH2()
    {
        var html = await RenderPlanAsync(new Folds(new BrowserStorage()));

        Regex.Matches(html, "<aside class=\"(assumed|limits)\" aria-labelledby=\"(assumed|limits)-title\"[^>]*><h2 [^>]*><button [^>]*><span id=\"(assumed|limits)-title\"")
            .Count.ShouldBe(2);
    }

    private static List<string> Toggles(string html) => [.. Regex.Matches(html, "<button[^>]*class=\"fold__toggle\"[^>]*>").Select(match => match.Value)];

    private static Task<string> RenderPlanAsync(Folds folds) =>
        RenderAsync(typeof(Plan), folds, [], services =>
        {
            services.AddSingleton<IBrowserStorage>(new BrowserStorage());
            services.AddSingleton<SavedPlans>();
            services.AddSingleton<RiverState>();
            services.AddSingleton(TimeProvider.System);
        });
}
