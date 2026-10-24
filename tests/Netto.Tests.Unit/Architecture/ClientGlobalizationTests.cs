using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Netto.Tests.Architecture;

// The client runs in the browser without ICU data (InvariantGlobalization): the site is English only,
// its amounts and dates go through Formats, built on the invariant culture, and its texts through
// neutral .resx files, so no culture-specific data is read and none is downloaded (about 0.2 MB of the
// compressed payload for each visitor). The test host is not the browser, so the suite itself runs with
// ICU: these tests hold what the setting relies on. The day the site is translated, a localized .resx
// or a culture created by name fails here: turn the setting off, or set PredefinedCulturesOnly to
// false, with the translation.
public class ClientGlobalizationTests
{
    [Fact]
    public void ClientProject_globalization_isInvariant()
    {
        var project = XDocument.Load(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj"));

        project.Descendants("InvariantGlobalization").Select(element => element.Value).ShouldBe(["true"]);
    }

    // Texts are in neutral files, Home.resx beside Home.razor; a Home.fr.resx would never be read.
    [Fact]
    public void ClientResources_localizedFiles_donotExist()
    {
        var resources = Files("Netto.Web.Client", "*.resx");

        resources.Count.ShouldBeGreaterThan(30);
        resources.Where(resource => Regex.IsMatch(resource, @"\.[a-z]{2,3}(-[A-Za-z]{2,4})?\.resx$")).ShouldBeEmpty();
    }

    // In invariant mode a culture created by name, new CultureInfo("fr-BE"), throws; the invariant culture
    // and its clone (Formats.Culture) are the only ones the code uses.
    [Theory]
    [InlineData("Netto.Core")]
    [InlineData("Netto.Web.Client")]
    public void Sources_culturesCreatedByName_donotExist(string project)
    {
        var sources = Files(project, "*.cs").Concat(Files(project, "*.razor")).ToList();

        sources.Count.ShouldBeGreaterThan(10);
        sources.Where(source => Regex.IsMatch(File.ReadAllText(Path.Combine(Folder(project), source)),
                @"new\s+CultureInfo\s*\(|CultureInfo\s*\.\s*(?:GetCultureInfo|CreateSpecificCulture)\s*\("))
            .ShouldBeEmpty();
    }

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
