using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.Architecture;

// The test support (Support/) is the one definition of what the tests share: the browser's storage
// fakes, the reader of a component's .resx, and the engine tests' fixtures. A test file that declares
// its own copy of one fails here, naming the file, so the copies do not come back.
public class TestSupportTests
{
    private static readonly string Folder =
        new FileInfo(RepositoryFile.FindUp("tests", "Netto.Tests.Unit", "Netto.Tests.Unit.csproj")).DirectoryName!;

    // A class that implements IBrowserStorage: a fake of the browser's storage.
    [Fact]
    public void TestSources_browserStorageFakes_areDefinedOnlyInSupport()
    {
        Declaring(@"\bclass\s+\w+[^{;]*:\s*[^{;]*\bIBrowserStorage\b").ShouldBeEmpty();
    }

    // A resource manager reading a .resx: ResxText is the one place that does.
    [Fact]
    public void TestSources_resxTexts_areReadOnlyThroughResxText()
    {
        Declaring(@"\bResourceManager\b").ShouldBeEmpty();
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
