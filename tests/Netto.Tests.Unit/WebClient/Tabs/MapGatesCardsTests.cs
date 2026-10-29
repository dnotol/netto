using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Every figure a gate's box says is one its card already draws, from the same run, read off the Plan page
// drawn with every card open: what crosses a figure line of the gate's card, what the viewer pays for the car
// a drain line of it, and what reaches the viewer the sum of the card's lines into the pocket, each of them a
// line the card draws (that the sum is the map's is MapGatesTests'). The sign a drain list draws (−, +) says
// which way the money goes, which the box's words say instead.
public class MapGatesCardsTests
{
    public static TheoryData<string> Rivers => new() { "every lever", "the company paying the contributions", "a rent under its floor", "a car without contribution" };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task Of_everyFigure_isOneItsCardDraws(string name)
    {
        var river = MapRendering.River(name);
        var run = river.Run();
        var gates = MapGates.Of(river, run);
        var links = MapLinks.Of(river, run);

        var html = await CardHtml.RenderCardsOpenAsync(river);

        var amounts = gates.Gates.SelectMany(gate => gate.Lines.Where(line => line.Amount is not null).SelectMany(line => line.ToYou
            ? links.Where(link => link.From == MapEnd.Of(gate.Module) && link.To == MapEnd.InBucket("pocket")).Select(link => (gate.Module, link.Amount!.Value))
            : [(gate.Module, line.Amount!.Value)])).ToList();
        amounts.ShouldSatisfyAllConditions(
            () => amounts.Count.ShouldBeGreaterThan(gates.Gates.Count),
            () => amounts.Where(amount => !MapRendering.Drawn(html, amount.Module).Contains(MapRendering.Unsigned(Formats.Euros(amount.Value))))
                .Select(amount => $"{amount.Module} {Formats.Euros(amount.Value)}").ShouldBeEmpty());
    }
}
