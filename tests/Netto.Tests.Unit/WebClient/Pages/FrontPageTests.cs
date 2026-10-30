using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The front page lives at /about, for the portfolio: the app opens on the plan. What the header links to
// and what the page scrolls to when it is opened at a section is one list, here.
public class FrontPageTests
{
    [Fact]
    public void Path_isAboutFromTheBase()
    {
        FrontPage.Path.ShouldBe("about");
    }

    [Fact]
    public void Sections_areTheThreeTheHeaderLinksToInPageOrder()
    {
        FrontPage.Sections.ShouldBe(["levers", "how-it-works", "principles"]);
    }

    [Fact]
    public void Href_aSection_isItsFragmentOnTheFrontPage()
    {
        FrontPage.Href("levers").ShouldBe("/about#levers");
    }

    // The path after the base, as NavigationManager.ToBaseRelativePath gives it. The router matches a URL
    // whatever its case and ignores a trailing slash, so the header does too.
    [Theory]
    [InlineData("about")]
    [InlineData("About")]
    [InlineData("about/")]
    [InlineData("about?x=1")]
    [InlineData("about#levers")]
    [InlineData("about/?x=1#levers")]
    public void IsAt_theFrontPagesAddress_isTrue(string relativePath)
    {
        FrontPage.IsAt(relativePath).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("?card=split")]
    [InlineData("plan")]
    [InlineData("plan/optimisation")]
    [InlineData("about/team")]
    [InlineData("aboutus")]
    [InlineData("not-found")]
    public void IsAt_anyOtherAddress_isFalse(string relativePath)
    {
        FrontPage.IsAt(relativePath).ShouldBeFalse();
    }

    // The section a page address opens the front page at, or none: the address is the viewer's to type, so
    // only the page's own sections are scrolled to, never a typed one such as "#1abc".
    [Theory]
    [InlineData("http://localhost/about#principles", "principles")]
    [InlineData("http://localhost/about#levers", "levers")]
    [InlineData("http://localhost/about?x=1#how-it-works", "how-it-works")]
    public void SectionAt_anAddressAtASection_isThatSection(string uri, string section)
    {
        FrontPage.SectionAt(uri).ShouldBe(section);
    }

    [Theory]
    [InlineData("http://localhost/about")]
    [InlineData("http://localhost/about#")]
    [InlineData("http://localhost/about#1abc")]
    [InlineData("http://localhost/about#Principles")]
    [InlineData("http://localhost/about#hero")]
    public void SectionAt_anyOtherAddress_isNone(string uri)
    {
        FrontPage.SectionAt(uri).ShouldBeNull();
    }
}
