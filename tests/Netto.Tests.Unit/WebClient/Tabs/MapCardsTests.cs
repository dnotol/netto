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
// goes, which the map's arrow says instead. The two lines no card draws are running totals: the pocket
// line's, each stretch what has reached the director so far, the sum of the lines joining it above less
// those leaving it, each a line its card draws, and its last the pocket the bar says; and the company's
// line, each stretch what the company has left after a lever, the stretch above less the lever's share, and
// its last what the split shares, as « To share » says it.
public class MapCardsTests
{
    public static TheoryData<string> Rivers => new()
    {
        "every lever", "the company paying the contributions", "the example", "the company skipped", "a rent under its floor", "a contribution beyond the car's costs",
    };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task Of_everyLink_isAFigureItsCardDraws(string name)
    {
        var river = MapRendering.River(name);
        var links = MapLinks.Of(river, river.Run());

        var html = await CardHtml.RenderCardsOpenAsync(river);

        links.ShouldNotBeEmpty();
        links.Where(link => !StretchOfTheCompanysLine(link.From, link.To) && !MapRendering.Drawn(html, link.Card).Contains(MapRendering.Unsigned(Formats.Euros(link.Amount!.Value))))
            .Select(link => $"{link.Card} {link.Carries.Key} {Formats.Euros(link.Amount!.Value)}").ShouldBeEmpty();
    }

    // Each stretch of the company's line carries what the company has left at its top: every line into that
    // point, the stretch above or Company year's line, and a car's contribution from the pocket, less every
    // other line out of it, the lever's share across to its box or a car's costs into the expenses; the last,
    // into the split, is what the split shares, the very amount « To share » says.
    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task Of_eachStretchOfTheCompanysLine_isWhatJoinsItsTopLessWhatLeavesItAndTheLastWhatTheSplitShares(string name)
    {
        var river = MapRendering.River(name);
        var links = MapLinks.Of(river, river.Run());
        var stretches = links.Where(link => StretchOfTheCompanysLine(link.From, link.To)).ToList();

        var html = await CardHtml.RenderCardsOpenAsync(river);

        stretches.ShouldSatisfyAllConditions(
            () => stretches.Where(stretch => stretch.Amount != links.Where(link => link.To == stretch.From).Sum(link => link.Amount)
                    - links.Where(link => link.From == stretch.From && link != stretch).Sum(link => link.Amount))
                .Select(stretch => $"{stretch.From.Id} to {stretch.To.Id} {Formats.Euros(stretch.Amount!.Value)}").ShouldBeEmpty(),
            () => stretches.Where(stretch => stretch.To == MapEnd.Of(RiverModule.Split))
                .Where(last => !MapRendering.Drawn(html, RiverModule.Split).Contains(Formats.Euros(last.Amount!.Value)))
                .Select(last => Formats.Euros(last.Amount!.Value)).ShouldBeEmpty(),
            () => stretches.Count.ShouldBe(river.Company is null ? 0 : BeforeTheSplit.Count(river.InPlan)));
    }

    // Each line of the map draws one amount: a figure its card draws or, for a line holding several of its
    // card's lines between the same two ends and of one kind, their sum, each part a figure the card draws,
    // so a viewer finds it by adding the card's lines.
    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task View_everyLinesAmount_isAFigureItsCardDrawsOrTheSumOfThem(string name)
    {
        var river = MapRendering.River(name);
        var lines = MapView.From(river, river.Run(), MapNames.English).Bundles()
            .Where(line => line.Amount is not null && !StretchOfThePocketLine(line) && !StretchOfTheCompanysLine(line.From, line.To)).ToList();

        var html = await CardHtml.RenderCardsOpenAsync(river);

        lines.ShouldSatisfyAllConditions(
            () => lines.ShouldContain(line => line.Links.Count > 1),
            () => lines.Where(line => !IsItsCardsFigureOrTheirSum(html, line))
                .Select(line => $"{line.Card} {line.From.Id} to {line.To.Id} {Formats.Euros(line.Amount!.Value)}").ShouldBeEmpty());
    }

    // Each stretch of the pocket line carries what has reached the director at its top: every line joining the
    // line there or above, less every line leaving it there or above, where they meet it; the last ends in the
    // pocket, the very amount the bar's pocket says.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void View_eachStretchOfThePocketLine_isTheLinesJoiningItAboveLessThoseLeavingItAndTheLastThePockets(string name)
    {
        var river = MapRendering.River(name);
        var bundles = MapView.From(river, river.Run(), MapNames.English).Bundles();
        var stretches = bundles.Where(StretchOfThePocketLine).ToList();
        var joins = bundles.Where(line => line.To.PocketLine is not null && line.From.PocketLine is null).Select(line => (Y: line.Route.End.Y, Amount: line.Amount!.Value)).ToList();
        var leaves = bundles.Where(line => line.From.PocketLine is not null && !StretchOfThePocketLine(line)).Select(line => (Y: line.Route.Points[0].Y, Amount: line.Amount!.Value)).ToList();

        stretches.ShouldSatisfyAllConditions(
            () => stretches.ShouldNotBeEmpty(),
            () => stretches.Where(stretch => stretch.Amount != joins.Where(join => join.Y <= stretch.Route.Points[0].Y).Sum(join => join.Amount)
                    - leaves.Where(leave => leave.Y <= stretch.Route.Points[0].Y).Sum(leave => leave.Amount))
                .Select(stretch => $"{stretch.From.Id} to {stretch.To.Id} {Formats.Euros(stretch.Amount!.Value)}").ShouldBeEmpty(),
            () => (stretches[^1].To, stretches[^1].Amount).ShouldBe((MapEnd.InBucket("pocket"), BucketsView.From(river.Run())!.AmountOf("pocket"))));
    }

    // The levers the company's line runs past, each handing on one stretch of it.
    private static readonly OptionalModule[] BeforeTheSplit = [OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car];

    // A stretch of the company's line: from the point beside a lever to the next one's, or into the split.
    private static bool StretchOfTheCompanysLine(MapEnd from, MapEnd to) => from.Lever is not null && (to.Lever is not null || to == MapEnd.Of(RiverModule.Split));

    private static bool StretchOfThePocketLine(MapBundle line) =>
        line.From.PocketLine is not null && (line.To.PocketLine is not null || line.To == MapEnd.InBucket("pocket"));

    private static bool IsItsCardsFigureOrTheirSum(string html, MapBundle line)

    {
        var drawn = MapRendering.Drawn(html, line.Card);
        var parts = line.Links.Select(link => MapRendering.Unsigned(Formats.Euros(link.Amount!.Value))).ToList();
        var sum = MapRendering.Unsigned(Formats.Euros(parts.Sum(part => decimal.Parse(part, NumberStyles.Currency, Formats.Culture))));

        return line.Links.All(link => link.Card == line.Card) && parts.All(drawn.Contains) && MapRendering.Unsigned(Formats.Euros(line.Amount!.Value)) == sum;
    }
}
