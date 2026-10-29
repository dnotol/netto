using System.Globalization;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Every amount the map draws is a figure the card it belongs to already shows, from the same run, or the
// sum of the card's figures between the same two ends and of one kind (MapBundle.Amount): each link's
// amount is read off the Plan page drawn with every card open, among the lines of that card, or in its
// summary for what Company year hands on. The sign a drain list draws (−, +) says which way the money
// goes, which the map's arrow says instead.
public class MapCardsTests
{
    public static TheoryData<string> Rivers => new() { "every lever", "the company paying the contributions", "the example", "the company skipped" };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task Of_everyLink_isAFigureItsCardDraws(string name)
    {
        var river = MapRendering.River(name);
        var links = MapLinks.Of(river, river.Run());

        var html = await CardHtml.RenderCardsOpenAsync(river);

        links.ShouldNotBeEmpty();
        links.Where(link => !MapRendering.Drawn(html, link.Card).Contains(MapRendering.Unsigned(Formats.Euros(link.Amount!.Value))))
            .Select(link => $"{link.Card} {link.Carries.Key} {Formats.Euros(link.Amount!.Value)}").ShouldBeEmpty();
    }

    // Each line of the map draws one amount: a figure its card draws or, for a line holding several of its
    // card's lines between the same two ends and of one kind, their sum, each part a figure the card draws,
    // so a viewer finds it by adding the card's lines.
    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task View_everyLinesAmount_isAFigureItsCardDrawsOrTheSumOfThem(string name)
    {
        var river = MapRendering.River(name);
        var lines = MapView.From(river, river.Run()).Bundles().Where(line => line.Amount is not null).ToList();

        var html = await CardHtml.RenderCardsOpenAsync(river);

        lines.ShouldSatisfyAllConditions(
            () => lines.ShouldContain(line => line.Links.Count > 1),
            () => lines.Where(line => !IsItsCardsFigureOrTheirSum(html, line))
                .Select(line => $"{line.Card} {line.From.Id} to {line.To.Id} {Formats.Euros(line.Amount!.Value)}").ShouldBeEmpty());
    }

    private static bool IsItsCardsFigureOrTheirSum(string html, MapBundle line)
    {
        var drawn = MapRendering.Drawn(html, line.Card);
        var parts = line.Links.Select(link => MapRendering.Unsigned(Formats.Euros(link.Amount!.Value))).ToList();
        var sum = MapRendering.Unsigned(Formats.Euros(parts.Sum(part => decimal.Parse(part, NumberStyles.Currency, Formats.Culture))));

        return line.Links.All(link => link.Card == line.Card) && parts.All(drawn.Contains) && MapRendering.Unsigned(Formats.Euros(line.Amount!.Value)) == sum;
    }
}
