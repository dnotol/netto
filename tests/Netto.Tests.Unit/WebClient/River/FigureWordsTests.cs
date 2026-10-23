using System.Globalization;
using Microsoft.Extensions.Localization;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A figure list's lines as the card that shows them words them: each key of the view model's lines
// read in the card's texts with its arguments, the rest as the view model gave it.
public class FigureWordsTests
{
    private static readonly CardTexts Texts = new(new()
    {
        ["Figure_Deducted"] = "Costs deducted at {0}",
        ["Help_Deducted"] = "A car ordered from {1} deducts {0} of its costs.",
        ["Figure_Co2"] = "CO2 percentage",
    });

    [Fact]
    public void Of_line_wordsItsLabelWithItsArguments()
    {
        var figures = FigureWords.Of(Texts, [new FigureLine("Figure_Deducted", 4_928m) { Args = ["61.6%"] }]);

        figures.ShouldBe([new Figure("Costs deducted at 61.6%", 4_928m)]);
    }

    [Fact]
    public void Of_lineWithHelp_wordsWhatItsBubbleSaysWithItsArguments()
    {
        var line = new FigureLine("Figure_Deducted", 4_928m) { Args = ["61.6%"], Help = "Help_Deducted", HelpArgs = ["61.6%", "1 July 2023"] };

        var figures = FigureWords.Of(Texts, [line]);

        figures.Select(figure => figure.Help).ShouldBe(["A car ordered from 1 July 2023 deducts 61.6% of its costs."]);
    }

    [Fact]
    public void Of_lineWithoutHelp_hasNone()
    {
        var figures = FigureWords.Of(Texts, [new FigureLine("Figure_Co2", 0.0975m)]);

        figures.Select(figure => figure.Help).ShouldBe([null]);
    }

    [Fact]
    public void Of_line_keepsItsAmountItsShareAndHowItReads()
    {
        var figures = FigureWords.Of(Texts, [new FigureLine("Figure_Co2", 0.0975m) { Share = 0.25m, Format = FigureFormat.Percentage }]);

        figures.ShouldBe([new Figure("CO2 percentage", 0.0975m) { Share = 0.25m, Format = FigureFormat.Percentage }]);
    }

    [Fact]
    public void Of_lines_keepsTheirOrder()
    {
        var figures = FigureWords.Of(Texts, [new FigureLine("Figure_Co2", 0.0975m), new FigureLine("Figure_Deducted", 4_928m) { Args = ["61.6%"] }]);

        figures.Select(figure => figure.Label).ShouldBe(["CO2 percentage", "Costs deducted at 61.6%"]);
    }

    // A card's texts, read as IStringLocalizer<TCard> reads its .resx.
    private sealed class CardTexts(Dictionary<string, string> texts) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            texts.TryGetValue(name, out var text) ? new(name, text) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            texts.TryGetValue(name, out var text) ? new(name, string.Format(CultureInfo.InvariantCulture, text, arguments)) : this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => texts.Select(pair => new LocalizedString(pair.Key, pair.Value));
    }
}
