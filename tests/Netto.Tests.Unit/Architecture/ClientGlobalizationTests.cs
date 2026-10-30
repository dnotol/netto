using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Netto.Tests.Architecture;

// The client runs in the browser without ICU data (InvariantGlobalization), so no culture data is downloaded
// (about 0.2 MB of the compressed payload for each visitor): its amounts and dates go through Formats, whose
// French culture is built by hand on an invariant clone. It still speaks French: the UI culture « fr » finds
// the French texts, each .fr.resx beside its neutral English one, built into a satellite assembly. Creating
// that culture by name needs PredefinedCulturesOnly off, which InvariantGlobalization turns on by default:
// left on, new CultureInfo("fr") throws in the browser. The test host is not the browser and runs with ICU,
// so it would never throw here: these tests hold what the browser relies on, and the browser check the rest.
public class ClientGlobalizationTests
{
    [Fact]
    public void ClientProject_globalization_isInvariant()
    {
        Property("InvariantGlobalization").ShouldBe(["true"]);
    }

    [Fact]
    public void ClientProject_culturesCreatedByName_areAllowed()
    {
        Property("PredefinedCulturesOnly").ShouldBe(["false"]);
    }

    // In invariant mode a culture created by name has the invariant culture as its parent: a Home.fr-BE.resx
    // would be read only by fr-BE, which the site never starts in (Languages), and fr-BE would never find
    // Home.fr.resx. The French texts are in .fr.resx files alone.
    [Fact]
    public void ClientResources_localizedFiles_areNeutralFrench()
    {
        var resources = ResxFiles.In(Folder("Netto.Web.Client"));

        resources.Count.ShouldBeGreaterThan(30);
        resources.Where(resource => ResxFiles.IsLocalized(resource) && !resource.EndsWith(".fr.resx", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    // The one culture created by name is the UI culture the page starts in (Languages); the culture of numbers
    // is Formats', built on the invariant culture.
    [Theory]
    [InlineData("Netto.Core", new string[0])]
    [InlineData("Netto.Web.Client", new[] { "Language.cs" })]
    public void Sources_culturesCreatedByName_areOnlyTheLanguages(string project, string[] creating)
    {
        var sources = Files(project, "*.cs").Concat(Files(project, "*.razor")).ToList();

        sources.Count.ShouldBeGreaterThan(10);
        sources.Where(source => Regex.IsMatch(File.ReadAllText(Path.Combine(Folder(project), source)),
                @"new\s+CultureInfo\s*\(|CultureInfo\s*\.\s*(?:GetCultureInfo|CreateSpecificCulture)\s*\("))
            .ShouldBe(creating);
    }

    private static IEnumerable<string> Property(string name) =>
        XDocument.Load(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj")).Descendants(name).Select(element => element.Value);

    private static string Folder(string project) =>
        new FileInfo(RepositoryFile.FindUp("src", project, $"{project}.csproj")).DirectoryName
        ?? throw new InvalidOperationException($"{project}'s csproj has no folder.");

    // The project's files of a kind, from its folder, build output left out.
    private static List<string> Files(string project, string pattern)
    {
        var folder = Folder(project);

        return [.. Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file))
            .Where(file => !file.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))];
    }
}
