using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The line above the chosen module's card, saying what it receives and from where, as a rendered Map tab
// draws it, read as a viewer reads it: its text with the markup left out, and the ends that are buttons; and
// what the Map tab's tests share besides: the plan whose levers in the chain move nothing, the reading of a
// link as money, and what the tab holds of the line as plain parts.
internal static class MapReceivedHtml
{
    // The expense allowances, the rent and the car left empty, the vouchers kept: the quiet levers follow one
    // another down the chain.
    public static readonly RiverInputs ChainQuiet =
        MapQuietTests.Emptied(MapLinksTests.Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

    // A link that moves money: the others change a tax base or are a condition.
    public static bool IsMoney(MapLink link) => MapLinkStyle.IsMoney(link.Kind);

    // A link's amount, which every link but a condition has.
    public static decimal Money(MapLink link) => link.Amount.ShouldNotBeNull();

    // Each part of the line the tab holds, its amount and the end it comes from.
    public static List<string> Listed(MapTab map) =>
        [.. (map.Received?.Lines ?? []).Select(line => $"{line.Amount} from {line.From}")];

    // How many such lines a rendered tab draws: one at most.
    public static int Count(string html) => Regex.Count(html, "<p class=\"map-tab__receives\"");

    // The line's text, as a viewer reads it, or none where it is not drawn.
    public static string? Line(string html) =>
        Regex.Match(html, "<p class=\"map-tab__receives\"[^>]*>(.*?)</p>", RegexOptions.Singleline) is { Success: true } match ? Text(match.Groups[1].Value) : null;

    // Each end of the line the viewer can go to: the module's name in the map's elements, and the name it is
    // drawn with.
    public static List<(string Module, string Name)> GoButtons(string html) =>
        [.. Regex.Matches(html, "<button type=\"button\" class=\"map-tab__go\"[^>]*data-received-from=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[1].Value, Text(match.Groups[2].Value)))];

    public static string Text(string html) => Regex.Replace(Regex.Replace(html, "<[^>]*>", " "), "\\s+", " ").Trim();
}
