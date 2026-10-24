using System.Xml.Linq;
using Shouldly;

namespace Netto.Tests.Architecture;

// The build lets no warning through: a done slice builds with none (CLAUDE.md, « Definition of done »), so
// the build fails on one instead of leaving it for review. The analyzers' rule set is pinned, so a newer
// SDK, which global.json rolls forward to, adds no rule unannounced; a security advisory on a package, and
// the audit's failure to reach the advisories on an offline or flaky restore, stay warnings, so neither
// breaks a build that did not change. Read from the props as XML.
public class BuildPolicyTests
{
    private static readonly string Root =
        Path.GetDirectoryName(RepositoryFile.FindUp("global.json")) ?? throw new InvalidOperationException("global.json has no folder.");

    [Fact]
    public void RootProps_warnings_areErrors()
    {
        var treatWarningsAsErrors = Property("Directory.Build.props", "TreatWarningsAsErrors");

        treatWarningsAsErrors.ShouldBe("true");
    }

    [Fact]
    public void RootProps_analysisLevel_isPinnedToTheRecommendedRulesOfDotNet10()
    {
        var analysisLevel = Property("Directory.Build.props", "AnalysisLevel");

        analysisLevel.ShouldBe("10.0-recommended");
    }

    [Fact]
    public void RootProps_packageVulnerabilityAdvisories_stayWarnings()
    {
        var notAsErrors = Codes("Directory.Build.props", "WarningsNotAsErrors");

        notAsErrors.ShouldBe(["NU1900", "NU1901", "NU1902", "NU1903", "NU1904"], ignoreOrder: true);
    }

    // CA1716 asks that no type be named like a keyword of another .NET language (Step is one in VB); no
    // such language consumes Netto.
    [Fact]
    public void RootProps_keywordNamesOfOtherLanguages_areAllowed()
    {
        var noWarn = Codes("Directory.Build.props", "NoWarn");

        noWarn.ShouldContain("CA1716");
    }

    // CA1707 forbids the underscores of Method_scenario_expectedBehavior.
    [Fact]
    public void TestProps_underscoresInTestNames_areAllowed()
    {
        var noWarn = Codes(Path.Combine("tests", "Directory.Build.props"), "NoWarn");

        noWarn.ShouldContain("CA1707");
    }

    // The property's value in the props file, from the checkout's root; null when it does not set it.
    private static string? Property(string props, string name) =>
        XDocument.Load(Path.Combine(Root, props)).Root?.Elements("PropertyGroup").Elements(name).Select(element => element.Value.Trim()).LastOrDefault();

    // The codes a list property names, the inherited $(…) aside.
    private static List<string> Codes(string props, string name) =>
        [.. (Property(props, name) ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(code => !code.StartsWith("$(", StringComparison.Ordinal))];
}
