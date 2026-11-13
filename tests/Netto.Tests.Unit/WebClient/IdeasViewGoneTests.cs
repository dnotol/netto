using System.Text.RegularExpressions;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient;

// The Ideas view (« Pistes ») is gone: levers are found in the panel over the map, a lever's card says its gain once
// added, and Optimise retunes (docs/architecture/decisions/2026-11-13-two-views-and-levers-are-found-in-edit-plan.md).
// Nothing of it is left in the engine or the client, and no text of the app points to it.
public partial class IdeasViewGoneTests
{
    [GeneratedRegex(@"\b(pistes?|ideas?|idées?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex IdeaWords();

    [Fact]
    public void Texts_theAppsEnglishAndFrench_nameNeitherIdeasNorPistes()
    {
        var client = typeof(BucketBar).Assembly;

        var named = ResxText.InAssembly(client).Concat(ResxText.FrenchInAssembly(client))
            .SelectMany(texts => texts.Entries.Where(entry => IdeaWords().IsMatch(entry.Key) || IdeaWords().IsMatch(entry.Value)).Select(entry => $"{texts.BaseName}:{entry.Key}"))
            .ToList();

        named.ShouldBeEmpty();
    }

    [Fact]
    public void Types_theEngineAndTheClient_holdNothingOfTheIdeasView()
    {
        var types = new[] { typeof(MoveSearch).Assembly, typeof(BucketBar).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.StartsWith("Piste", StringComparison.Ordinal) || type.Name == "NotInNetto")
            .Select(type => type.FullName);

        types.ShouldBeEmpty();
    }

    [Fact]
    public void Views_thePlanPage_areTheTwoOfThem()
    {
        Enum.GetNames<PlanTab>().ShouldBe(["Plan", "Optimisation"]);
        Enum.GetNames<TabBadgeKind>().Where(name => name.Contains("Piste", StringComparison.Ordinal)).ShouldBeEmpty();
    }
}
