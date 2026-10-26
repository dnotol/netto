using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The line under the sticky header and the bar of buckets, where a card jumped to lands and a control
// reached with Tab stops (--netto-under-bar), is built from two things the page owns: the header's
// height, declared once, and the bar's height, measured by bucket-bar.js whatever its width, its names
// and its tabs make of it. Nothing types the bar's height by hand, so no stylesheet has to agree with
// another by eye. What the script does in a browser is checked there; these hold the names the
// stylesheets, the script and the component use for each other.
public class BarHeightCssTests
{
    private const string HeaderHeight = "--netto-header-height";
    private const string BarHeight = "--netto-bar-height";

    private static readonly string[] Projects = ["Netto.Web.Client", "Netto.Web"];

    [Fact]
    public void HeaderHeight_isDeclaredOnce()
    {
        var declarations = Stylesheets().SelectMany(css => Regex.Matches(css, Regex.Escape(HeaderHeight) + @"\s*:\s*([^;]+);"))
            .Select(match => match.Groups[1].Value.Trim());

        declarations.ShouldBe(["4.25rem"]);
    }

    // Its value is typed in that one declaration and nowhere else: not by the header, nor by the bar that
    // sticks under it, nor by the line under both.
    [Fact]
    public void HeaderHeight_valueIsTypedNowhereElse()
    {
        Stylesheets().Sum(css => Regex.Count(css, @"4\.25rem")).ShouldBe(1);
    }

    [Theory]
    [InlineData("src/Netto.Web.Client/Layout/MainLayout.razor.css")]
    [InlineData("src/Netto.Web.Client/River/BucketBar.razor.css")]
    public void HeaderHeight_aStylesheetThatNeedsIt_readsTheToken(string stylesheet)
    {
        WithoutComments(Read(stylesheet)).ShouldContain($"var({HeaderHeight})");
    }

    [Fact]
    public void UnderBar_isDeclaredOnce_fromTheHeaderAndTheMeasuredBar()
    {
        var declarations = Regex.Matches(WithoutComments(Read("src/Netto.Web/wwwroot/app.css")), @"--netto-under-bar\s*:\s*([^;]+);")
            .Select(match => match.Groups[1].Value);

        var declared = declarations.ShouldHaveSingleItem();

        declared.ShouldSatisfyAllConditions(
            () => declared.ShouldContain($"var({HeaderHeight})"),
            () => declared.ShouldContain($"var({BarHeight})"));
    }

    // Declared in app.css like every token, at nothing until the script has measured the bar, and set
    // by the script under that name.
    [Fact]
    public void BarHeight_isDeclaredInAppCssAndSetByTheScriptUnderTheSameName()
    {
        var appCss = WithoutComments(Read("src/Netto.Web/wwwroot/app.css"));
        var script = Read("src/Netto.Web/wwwroot/bucket-bar.js");

        appCss.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(appCss, Regex.Escape(BarHeight) + @"\s*:\s*0px;").ShouldBeTrue(),
            () => script.ShouldContain($"\"{BarHeight}\""));
    }

    // The page loads the script, the bar calls what it exposes, and the river's outline, whose line is
    // the bar's bottom, measures again when the bar changes size.
    [Fact]
    public void BarScript_isLoadedByThePageAndCalledByTheBar()
    {
        var script = Read("src/Netto.Web/wwwroot/bucket-bar.js");
        var bar = Read("src/Netto.Web.Client/River/BucketBar.razor");

        script.ShouldSatisfyAllConditions(
            () => Read("src/Netto.Web/Components/App.razor").ShouldContain("bucket-bar.js"),
            () => script.ShouldContain("window.nettoBucketBar"),
            () => Regex.IsMatch(script, @"\bwatch\s*\(").ShouldBeTrue(),
            () => Regex.IsMatch(script, @"\bstop\s*\(").ShouldBeTrue(),
            () => bar.ShouldContain("nettoBucketBar.watch"),
            () => bar.ShouldContain("nettoBucketBar.stop"));
    }

    // Each time Blazor loads a page over the one in the browser (after every hot reload in development) it sets
    // the root's attributes to the server's, whose <html> has no style: the height goes with it, so the script
    // watches the root's style while it watches the bar, and stops with it.
    [Fact]
    public void BarScript_setsTheHeightAgain_whenTheRootsStyleIsReplaced()
    {
        var script = Read("src/Netto.Web/wwwroot/bucket-bar.js");

        script.ShouldSatisfyAllConditions(
            () => script.ShouldContain("new MutationObserver("),
            () => Regex.IsMatch(script, @"\.observe\(\s*document\.documentElement\s*,\s*\{[^}]*attributeFilter\s*:\s*\[\s*""style""\s*\]")
                .ShouldBeTrue(),
            () => Regex.Count(script, @"\.disconnect\(\)").ShouldBe(2));
    }

    [Fact]
    public void RiverOutline_script_measuresAgainWhenTheBarChangesSize()
    {
        Read("src/Netto.Web/wwwroot/river-outline.js").ShouldContain(".bucket-bar");
    }

    // The client's and the host's stylesheets, their comments left out.
    private static IEnumerable<string> Stylesheets() =>
        Projects
            .SelectMany(project =>
            {
                var folder = new FileInfo(RepositoryFile.FindUp("src", project, $"{project}.csproj")).DirectoryName
                    ?? throw new InvalidOperationException($"{project}'s csproj has no folder.");
                return Directory.EnumerateFiles(folder, "*.css", SearchOption.AllDirectories)
                    .Where(file => !Regex.IsMatch(Path.GetRelativePath(folder, file), @"^(bin|obj)[\\/]"));
            })
            .Select(file => WithoutComments(File.ReadAllText(file)));

    private static string WithoutComments(string css) => Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);

    // A file of the checkout, by its path from the root.
    private static string Read(string fromTheRoot) => File.ReadAllText(RepositoryFile.FindUp(fromTheRoot.Split('/')));
}
