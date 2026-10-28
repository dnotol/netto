using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The site's column, one width for what runs along the top of every page and the page under it: the site's
// header, the plan page's band and, once they take it, the pages' own containers. Declared once in app.css
// (--netto-column, the .netto-column class), so a page and the band above it never disagree on their edges.
// MudContainer keeps its gutters and has no width of its own (MaxWidth.False): the class gives it one.
public class PageColumnCssTests
{
    private static readonly string AppCss = Stylesheet.Read("src", "Netto.Web", "wwwroot", "app.css");

    [Fact]
    public void Column_isDeclaredOnceInAppCss_atAHundredRem()
    {
        Regex.Matches(Stylesheet.WithoutComments(AppCss), @"--netto-column\s*:\s*([^;]+);").Select(match => match.Groups[1].Value.Trim())
            .ShouldBe(["100rem"]);
    }

    [Fact]
    public void ColumnClass_isAsWideAsTheColumn()
    {
        Stylesheet.Block(Stylesheet.WithoutComments(AppCss), ".netto-column").ShouldContain("max-width: var(--netto-column);");
    }

    // The header's container and the band's take the column, and no width of MudBlazor's; so does the plan
    // page's, under the band, every tab of it.
    [Theory]
    [InlineData("src/Netto.Web.Client/Layout/MainLayout.razor", "site-header__inner")]
    [InlineData("src/Netto.Web.Client/River/BucketBar.razor", "bucket-bar__inner")]
    [InlineData("src/Netto.Web.Client/Pages/Plan.razor", "plan-tab__column")]
    public void Container_alongTheTop_takesTheColumn(string component, string container)
    {
        var tag = Regex.Match(File.ReadAllText(RepositoryFile.FindUp(component.Split('/'))), $"<MudContainer [^>]*{container}[^>]*>").Value;

        (tag.Contains("MaxWidth=\"MaxWidth.False\"", StringComparison.Ordinal), Regex.IsMatch(tag, "Class=\"[^\"]*\\bnetto-column\\b")).ShouldBe((true, true));
    }
}
