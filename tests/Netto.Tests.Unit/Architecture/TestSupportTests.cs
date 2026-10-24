using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.Architecture;

// The test support (Support/) is the one definition of what the tests share: the browser's storage
// fakes, the reader of a component's .resx, and the engine tests' fixtures. A test file that declares
// its own copy of one fails here, naming the file, so the copies do not come back.
public class TestSupportTests
{
    private static readonly string Folder =
        new FileInfo(RepositoryFile.FindUp("tests", "Netto.Tests.Unit", "Netto.Tests.Unit.csproj")).DirectoryName
        ?? throw new InvalidOperationException("The test project's csproj has no folder.");

    // A class that implements IBrowserStorage: a fake of the browser's storage.
    [Fact]
    public void TestSources_browserStorageFakes_areDefinedOnlyInSupport()
    {
        Declaring(@"\bclass\s+\w+[^{;]*:\s*[^{;]*\bIBrowserStorage\b").ShouldBeEmpty();
    }

    // A reader of a .resx, a resource manager or a resource reader over the assembly's stream: ResxText is the
    // one place that is, InterfaceTexts included, which reads every text through it.
    [Fact]
    public void TestSources_resxTexts_areReadOnlyThroughResxText()
    {
        Declaring(@"\bResourceManager\b|\bResourceReader\b|GetManifestResourceStream").ShouldBeEmpty();
    }

    // The made-up company the engine tests share, and the levers they put in its year.
    [Fact]
    public void TestSources_madeUpCompanyFixtures_areDeclaredOnlyInFixtures()
    {
        Declaring(@"\bstatic\s+readonly\s+(?:CompanyStatus\s+SmallPastStartUp|AdvancePayments\s+Prepaid|TaxPrepayments\s+Withheld"
            + @"|VoucherGrants\s+(?:AllVouchers|MealVouchersOn220Days)|ExpenseAllowances\s+FlatRateAllowances|HomeOfficeRent\s+RentAtTheCap|CarFacts\s+ElectricCar)\b")
            .ShouldBeEmpty();
    }

    // A factory of the made-up company's scenario, and a wrapper that runs the year or the split and asserts no error.
    [Fact]
    public void TestSources_scenarioFactoriesAndRunWrappers_areDefinedOnlyInFixtures()
    {
        Declaring(@"\bstatic\s+(?:Scenario\s+Scenario\s*\(|(?:YearResult\s+(?:Run|Year)|SplitSearchResult\s+Search)\s*\(\s*Scenario\s)").ShouldBeEmpty();
    }

    // A scenario typed out field by field: the river's example is Fixtures.RiverExample, the rest Fixtures.Scenario.
    [Fact]
    public void TestSources_scenarios_areNotTypedOutOutsideFixtures()
    {
        Declaring(@"\bnew\s+Scenario\s*\(|\bScenario\s+\w+\s*=\s*new\s*\(").ShouldBeEmpty();
    }

    // The income year the rule sets have no rules for is Fixtures.UnsupportedIncomeYear, not a literal 2027 here and there.
    [Fact]
    public void TestSources_unsupportedIncomeYear_isNamedOnce()
    {
        Declaring(@"InlineData\(\s*2027\s*\)|IncomeYear\s*=\s*2027\b").ShouldBeEmpty();
    }

    // A clock standing in for the browser's: a TimeProvider that answers the instant it was given.
    [Fact]
    public void TestSources_clocks_areDefinedOnlyInSupport()
    {
        Declaring(@"\bclass\s+\w+[^{;]*:\s*[^{;]*\bTimeProvider\b").ShouldBeEmpty();
    }

    // The test sources outside Support, and this file, whose patterns would match themselves.
    private static IEnumerable<(string Name, string Text)> Sources() =>
        Directory.EnumerateFiles(Folder, "*.cs", SearchOption.AllDirectories)
            .Select(file => (Name: Path.GetRelativePath(Folder, file).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(file)))
            .Where(source => !source.Name.StartsWith("obj/", StringComparison.Ordinal) && !source.Name.StartsWith("bin/", StringComparison.Ordinal)
                && !source.Name.StartsWith("Support/", StringComparison.Ordinal) && source.Name != "Architecture/TestSupportTests.cs");

    // The files outside Support whose text matches the pattern.
    private static List<string> Declaring(string pattern)
    {
        var sources = Sources().ToList();

        sources.Count.ShouldBeGreaterThan(100);
        return [.. sources.Where(source => Regex.IsMatch(source.Text, pattern)).Select(source => source.Name)];
    }
}
