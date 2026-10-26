using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The neighbourhood of the module chosen as a rendered Map tab draws it, read as a viewer reads it: its
// heading, its groups (what comes in, what passes through, what goes out, the rules) and the rows of each,
// their text with the markup left out, and the ends that are buttons; and what its tests share besides: the
// plan whose levers in the chain move nothing, the reading of a link as money, and the neighbourhood the tab
// holds as plain lines.
internal static class MapNeighbourhoodHtml
{
    // The expense allowances, the rent and the car left empty, the vouchers kept: the quiet levers follow one
    // another down the chain.
    public static readonly RiverInputs ChainQuiet =
        MapQuietTests.Emptied(MapLinksTests.Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

    // A link that moves money: the others change a tax base or are a condition.
    public static bool IsMoney(MapLink link) => link.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket;

    // A link's amount, which every link but a condition has.
    public static decimal Money(MapLink link) => link.Amount.ShouldNotBeNull();

    // Each line and rule the neighbourhood the tab holds lists, as a viewer tells them apart.
    public static List<string> Listed(MapTab map)
    {
        var hood = map.Neighbourhood.ShouldNotBeNull();
        return
        [
            .. hood.ComesIn.Concat(hood.PassesThrough).Concat(hood.GoesOut).Select(row => $"{row.Words} {row.Amount} {row.From.Name} {row.To.Name}"),
            .. hood.Rules.Select(rule => $"rule: {rule.Words} {rule.Amount} {rule.Other.Name}"),
        ];
    }

    // The whole section of a rendered tab: it holds no section of its own, so the first closing tag ends it.
    public static string Section(string html) => Regex.Match(html, "<section class=\"map-hood\".*?</section>", RegexOptions.Singleline).Value;

    public static string Heading(string html) =>
        Text(Regex.Match(Section(html), "<h2 [^>]*class=\"map-hood__title\"[^>]*>(.*?)</h2>", RegexOptions.Singleline).Groups[1].Value);

    // The single line the section says in place of its groups: how to choose, no figure, nothing moving.
    public static string Note(string html) =>
        Text(Regex.Match(Section(html), "<p class=\"map-hood__note\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value);

    // The groups drawn, by name, in the order drawn.
    public static List<string> Groups(string html) =>
        [.. Regex.Matches(Section(html), "<div class=\"map-hood__group\" data-group=\"([a-z-]+)\"").Select(match => match.Groups[1].Value)];

    // The heading of each group, in the order drawn.
    public static List<string> GroupHeadings(string html) =>
        [.. Regex.Matches(Section(html), "<h3 class=\"map-hood__heading\"[^>]*>(.*?)</h3>", RegexOptions.Singleline).Select(match => Text(match.Groups[1].Value))];

    // The rows of a group (every group when none is named), each as the text a viewer reads.
    public static List<string> Rows(string html, string? group = null) =>
        [.. RowTags(html, group).Select(row => Text(row.Inner))];

    // The inner HTML of the rows of a group, with its opening tag.
    public static List<(string Tag, string Inner)> RowTags(string html, string? group = null)
    {
        var section = Section(html);
        if (group is not null)
            section = Regex.Match(section, $"<div class=\"map-hood__group\" data-group=\"{group}\".*?</ul>", RegexOptions.Singleline).Value;

        return [.. Regex.Matches(section, "(<li class=\"map-hood__row[^\"]*\"[^>]*>)(.*?)</li>", RegexOptions.Singleline).Select(match => (match.Groups[1].Value, match.Groups[2].Value))];
    }

    // Each end the viewer can go to: the module's name in the map's elements, and the name it is drawn with.
    public static List<(string Module, string Name)> GoButtons(string html) =>
        [.. Regex.Matches(Section(html), "<button type=\"button\" class=\"map-hood__go\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[1].Value, Text(match.Groups[2].Value)))];

    // The width each bar is drawn with, in the order of the rows that have one.
    public static List<string> BarWidths(string html) =>
        [.. Regex.Matches(Section(html), "<span class=\"map-hood__bar[^\"]*\"[^>]*><i style=\"width: ([^\";]*);?\"").Select(match => match.Groups[1].Value)];

    public static string Text(string html) => Regex.Replace(Regex.Replace(html, "<[^>]*>", " "), "\\s+", " ").Trim();
}
