using Shouldly;

namespace Netto.Tests.Support;

// The checkout's stylesheets read as data, for a test that holds a stylesheet to the figures or the tokens
// it must draw with: a selector's own declarations, a length as a stylesheet writes it, and the tokens
// app.css declares for each theme.
public class StylesheetTests
{
    private const string Css = """
        .map {
            height: 1rem;
        }

        .map-badge {
            height: 0.875rem;
        }

        @media (max-width: 1200px) {
            .map {
                min-width: 60rem;
            }
        }
        """;

    // A selector's rules, in a media query too, and never a longer selector that starts with it.
    [Fact]
    public void Block_aSelector_givesItsOwnRulesDeclarations()
    {
        var block = Stylesheet.Block(Css, ".map");

        (block.Contains("height: 1rem;", StringComparison.Ordinal), block.Contains("min-width: 60rem;", StringComparison.Ordinal), block.Contains("0.875rem", StringComparison.Ordinal))
            .ShouldBe((true, true, false));
    }

    // Every rule with its selector and its declarations, a media query's own rules among them, the comments
    // left out so a selector or a declaration in one is never read.
    [Fact]
    public void Rules_aStylesheet_givesEveryRuleItsSelectorAndDeclarationsWithoutItsComments()
    {
        var rules = Stylesheet.Rules("/* .gone { color: red; } */\n.map { height: 1rem; }\n@media (max-width: 1200px) {\n    .map-badge { color: blue; }\n}");

        rules.Select(rule => (rule.Selector, rule.Declarations.Trim())).ShouldBe([(".map", "height: 1rem;"), (".map-badge", "color: blue;")]);
    }

    [Fact]
    public void Rem_aLength_isWrittenAsAStylesheetWritesIt()
    {
        (Stylesheet.Rem(3.25m), Stylesheet.Rem(0.1m)).ShouldBe(("3.25rem", "0.1rem"));
    }

    [Fact]
    public void Read_aStylesheetOfTheCheckout_givesItsText()
    {
        Stylesheet.Read("src", "Netto.Web", "wwwroot", "app.css").ShouldContain("--netto-page");
    }

    // Each theme's tokens as app.css declares them: dark's in :root, light's in :root[data-theme="light"].
    [Theory]
    [InlineData("dark", "#0B0A08")]
    [InlineData("light", "#F3EEE2")]
    public void ThemeTokens_aTheme_areTheTokensItsBlockOfAppCssDeclares(string theme, string page)
    {
        var tokens = Stylesheet.ThemeTokens(theme);

        (tokens["--netto-page"], tokens.ContainsKey("--netto-error")).ShouldBe((page, true));
    }

    [Fact]
    public void ThemeTokens_aThemeAppCssHasNoBlockFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Stylesheet.ThemeTokens("sepia"));
    }
}
