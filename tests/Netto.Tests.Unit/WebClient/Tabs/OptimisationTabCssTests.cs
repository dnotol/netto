using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Optimise view's stylesheet must keep. A display the stylesheet gives an element wins over the hidden
// attribute's display: none, which is only the browser's own style: a move's folded reasons would stay open.
public class OptimisationTabCssTests
{
    private static readonly IReadOnlyList<(string Selector, string Declarations)> Rules =
        Stylesheet.Rules(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "OptimisationTab.razor.css"));

    [Fact]
    public void FoldedReasons_hidden_areNotDisplayed()
    {
        Rules.Where(rule => rule.Selector == ".moves__more[hidden]")
            .Select(rule => Regex.IsMatch(rule.Declarations, @"(^|;)\s*display\s*:\s*none\s*(;|$)"))
            .ShouldBe([true]);
    }
}
