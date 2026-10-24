using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// How a card's figure is built, line by line, as the card worded its lines (FigureWords): each line's
// label and amount, its share when it has one, and an info bubble when its label needs explaining.
public class FigureListTests
{
    // The rent of the example's home office: taxed on the rent, less its flat charges, joining the
    // taxable income.
    private static readonly Figure[] Rent =
    [
        new("Taxed on the rent", 3_333.33m),
        new("Flat charges", -1_333.33m),
        new("Joins your taxable income", 2_000m),
    ];

    [Fact]
    public async Task FigureList_lines_drawsOneListWithAnItemPerLine()
    {
        var html = await RenderAsync(Rent);

        (Regex.Count(html, "<ul class=\"figure-list\""), Regex.Count(html, "<li class=\"figure\"")).ShouldBe((1, 3));
    }

    // In their order; a line that takes from the figure carries its sign in its amount.
    [Fact]
    public async Task FigureList_lines_drawEachLabelThenItsAmountInEuros()
    {
        var html = await RenderAsync(Rent);

        Lines(html).ShouldBe(
        [
            ("Taxed on the rent", Formats.Euros(3_333.33m)),
            ("Flat charges", Formats.Euros(-1_333.33m)),
            ("Joins your taxable income", Formats.Euros(2_000m)),
        ]);
    }

    // A car's CO2 percentage keeps its decimals.
    [Fact]
    public async Task FigureList_percentage_drawsItsAmountAsAPercentage()
    {
        var html = await RenderAsync([new Figure("CO2 percentage", 0.0975m) { Format = FigureFormat.Percentage }]);

        Texts(html, "figure__amount").ShouldBe([Formats.Percentage(0.0975m)]);
    }

    [Fact]
    public async Task FigureList_lineWithAShare_drawsItsShareAfterItsAmount()
    {
        var html = await RenderAsync([new Figure("Your pay", 40_000m) { Share = 0.4m }, new Figure("Company profit before tax", 60_000m)]);

        html.ShouldSatisfyAllConditions(
            () => Texts(html, "figure__share").ShouldBe([Formats.Share(0.4m)]),
            () => Regex.IsMatch(html, "class=\"figure__amount\"[^>]*>[^<]*</span> <span class=\"figure__share\"").ShouldBeTrue());
    }

    [Fact]
    public async Task FigureList_linesWithoutAShare_drawNone()
    {
        var html = await RenderAsync(Rent);

        html.Contains("figure__share").ShouldBeFalse();
    }

    // The bubble's (i) stands after the label, named by it; what it says is the trigger's description.
    [Fact]
    public async Task FigureList_lineWithHelp_opensABubbleNamedMoreAboutItsLabel()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync([new Figure("Flat charges", -1_333.33m) { Help = "What the administration takes as the costs of the rent." }]));

        var label = Regex.Match(html, "<span class=\"figure__label\"[^>]*>Flat charges<span class=\"info-bubble\".*?</button>", RegexOptions.Singleline).Value;
        html.ShouldSatisfyAllConditions(
            () => Regex.Match(label, "aria-label=\"([^\"]*)\"").Groups[1].Value.ShouldBe("More about Flat charges"),
            () => Texts(html, "info-bubble__text").ShouldBe(["What the administration takes as the costs of the rent."]));
    }

    [Fact]
    public async Task FigureList_linesWithoutHelp_drawNoBubble()
    {
        var html = await RenderAsync(Rent);

        html.Contains("info-bubble").ShouldBeFalse();
    }

    [Fact]
    public async Task FigureList_noLines_drawsNoList()
    {
        var html = await RenderAsync([]);

        html.Contains("figure-list").ShouldBeFalse();
    }

    private static Task<string> RenderAsync(Figure[] figures) =>
        RenderAsync<FigureList>(new Folds(new BrowserStorage()), new() { ["Figures"] = figures });

    // The text each element of that class starts with, up to its first child element.
    private static List<string> Texts(string html, string element) =>
        [.. Regex.Matches(html, $"class=\"{element}\"[^>]*>([^<]*)").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value))];

    private static List<(string Label, string Amount)> Lines(string html) =>
        [.. Texts(html, "figure__label").Zip(Texts(html, "figure__amount"))];
}
