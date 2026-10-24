using System.Net;
using Netto.Web.Client.River;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The frame every card shares, drawn open with its details open: the line a card waits on while the
// company's year is skipped, and the fold under its figures holding what the card gives (Details) and the
// rules its steps cite (Sources), under a title worded once for every card unless the card words its own.
public class ModuleCardTests
{
    private static readonly ResxText Chrome = ResxText.For<ModuleCard>();

    // The chrome's own texts, read once each and never a fallback: a missing text fails here, not in a Contains.
    private static string Waiting => Chrome.Get("Waiting").ShouldNotBeNull();

    private static string DetailsLabel => Chrome.Get("DetailsLabel").ShouldNotBeNull();

    // The example's corporate tax and its surcharge, two steps with their sources.
    private static readonly IReadOnlyList<StepDetail> Sources = CorporateTaxView.From(RiverInputs.Example.Run().Year.ShouldNotBeNull()).Details;

    [Fact]
    public async Task Waiting_card_saysItWaitsInsteadOfItsContentAndIsDashed()
    {
        var html = await RenderOpenAsync(new() { ["Waiting"] = true, ["ChildContent"] = Text("Fields"), ["Sources"] = Sources, ["IncomeYear"] = 2026 });

        (html.Contains(Waiting), html.Contains("Fields"), html.Contains("module-card--skipped"), html.Contains("module-card__detail"))
            .ShouldBe((true, false, true, false));
    }

    // An optional module still says how to take it out of the plan while it waits.
    [Fact]
    public async Task Waiting_optionalModule_keepsItsWordToTakeItOut()
    {
        var html = await RenderOpenAsync(new() { ["Waiting"] = true, ["RemoveLabel"] = "Remove it" });

        html.Contains("Remove it").ShouldBeTrue();
    }

    [Fact]
    public async Task Sources_given_foldsThemUnderTheRulesWithTheIncomeYearsAmounts()
    {
        var html = await RenderOpenAsync(new() { ["ChildContent"] = Text("Fields"), ["Sources"] = Sources, ["IncomeYear"] = 2026 });

        (html.Contains(Chrome.Get("DetailsLabel") ?? "-"), html.Contains($"<h4 class=\"module-card__sources\""), html.Contains(Chrome.Format("SourcesTitle", 2026)),
                html.Contains("Fields"), html.Contains("module-card--skipped"))
            .ShouldBe((true, true, true, true, false));
    }

    // What the card gives comes first in its details, then the rules.
    [Fact]
    public async Task Details_givenWithSources_comeBeforeTheRules()
    {
        var html = await RenderOpenAsync(new() { ["Details"] = Text("The pay slip"), ["Sources"] = Sources, ["IncomeYear"] = 2026 });

        html.IndexOf("The pay slip", StringComparison.Ordinal).ShouldBeInRange(0, html.IndexOf("module-card__sources", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Details_givenWithoutSources_foldWithNoRules()
    {
        var html = await RenderOpenAsync(new() { ["Details"] = Text("The pay slip") });

        (html.Contains("The pay slip"), html.Contains(Chrome.Get("DetailsLabel") ?? "-"), html.Contains("module-card__sources")).ShouldBe((true, true, false));
    }

    [Fact]
    public async Task NoDetailsNorSources_drawNoFold()
    {
        var html = await RenderOpenAsync(new() { ["ChildContent"] = Text("Fields"), ["Sources"] = Array.Empty<StepDetail>(), ["IncomeYear"] = 2026 });

        (html.Contains("module-card__detail"), html.Contains(Chrome.Get("DetailsLabel") ?? "-")).ShouldBe((false, false));
    }

    // A card that words its details its own way: Corporate tax's one rule, Director pay's pay slip.
    [Fact]
    public async Task OwnWords_given_nameTheDetailsAndTheRules()
    {
        var html = await RenderOpenAsync(new() { ["Sources"] = Sources, ["IncomeYear"] = 2026, ["DetailsLabel"] = "The rule's source", ["SourcesTitle"] = "The rule, 2026" });

        (html.Contains("The rule's source"), html.Contains("The rule, 2026"), html.Contains(Chrome.Get("DetailsLabel") ?? "-"), html.Contains(Chrome.Format("SourcesTitle", 2026)))
            .ShouldBe((true, true, false, false));
    }

    // The card named « test-card », opened with its details by the viewer, its HTML decoded.
    private static async Task<string> RenderOpenAsync(Dictionary<string, object?> parameters)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("test-card", true);
        folds.Set("test-card-details", true);
        parameters["Name"] = "test-card";
        parameters["Title"] = "Test card";

        return WebUtility.HtmlDecode(await RenderAsync<ModuleCard>(folds, parameters));
    }
}
