using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A card's HTML read as a viewer reads it, for a test to hold a card to what it draws: the summary beside
// its title and the rate it ends on, the text of its paragraphs, the lines of its figure and drain lists,
// the words in sight, and any text a misspelt key leaves; one card or the river cut out of the Plan page,
// and that page drawn with every card open. It reads what the renderer writes (RiverRendering), encoded
// or decoded alike. In sight is what the hivemind counts in the browser: every text but what a bubble's
// text or an element hidden holds, a folded body or a closed disclosure.
internal static class CardHtml
{
    // A day with an advance payment still to come, as the Plan page's own tests take.
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    // An element's start or end tag, its attributes quoted as they may hold a > once decoded, a comment,
    // or a text, a < that starts no tag being text too.
    private static readonly Regex Tokens = new(
        "<!--.*?-->|<(?<end>/)?(?<name>[A-Za-z][A-Za-z0-9:-]*)(?<attributes>(?:[^>\"']|\"[^\"]*\"|'[^']*')*)>|(?<text>[^<]+)|(?<text><)",
        RegexOptions.Singleline);

    private static readonly Regex Attributes = new("(?<name>[^\\s\"'>/=]+)(?:\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)'|(?<value>[^\\s\"'>]+)))?");

    private static readonly HashSet<string> Void = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];

    // A text a misspelt key leaves, the key itself: words joined by an underscore (Drain_social_contributions,
    // Summary_Royalty) or run together each with its capital (CostsLabel, SummaryNoCar), which no sentence
    // of the site holds. A key of one plain word (Title) reads as a word and is not found.
    private static readonly Regex KeyLike = new("\\b(?:[A-Za-z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)+|[A-Z][a-z0-9]+(?:[A-Z][a-z0-9]+)+)\\b");

    // The site's own words run together as a key's are: the tax administration's portal.
    private static readonly HashSet<string> NamesLikeKeys = ["MyMinfin"];

    // What names or describes an element, where a key can leak as well as into a text.
    private static readonly string[] Naming = ["aria-label", "aria-valuetext", "title", "placeholder", "alt"];

    // The Plan page on the river given, every card in it open as the outline's « Open all » leaves them:
    // the folds inside the cards, their sources and their conditions, and the notes under the river stay
    // folded. No plan is saved, so none is starred Today.
    public static Task<string> RenderPlanOpenAsync(RiverInputs river)
    {
        var folds = new Folds(new RiverRendering.BrowserStorage());
        foreach (var card in OutlineRows.Order)
            folds.Set(card, true);

        return RiverRendering.RenderAsync(typeof(Plan), folds, [], services =>
        {
            var storage = new RiverRendering.BrowserStorage();
            var plans = new SavedPlans(storage);
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = river });
            services.AddSingleton<TimeProvider>(new Clock(On23October));
        });
    }

    // The river of the Plan page, its cards and what stands between them, without the page's head, the
    // bar of buckets with its tabs, or the notes under it.
    public static string River(string html) => ById(html, "river");

    // One card of a page by its fold's name (OutlineRows.Order), with all it holds.
    public static string Card(string html, string name) => ById(html, $"card-{name}");

    // What the card's heading says beside its title, without the rate it ends on; none without a summary.
    public static string? Summary(string html) => FirstInSight(html, "fold__summary") is { } summary ? TextOf(summary, leaving: "fold__rate") : null;

    // The share the summary ends on, as ModuleCard words it: « 60% to you ».
    public static string? Rate(string html) => FirstInSight(html, "fold__rate") is { } rate ? TextOf(rate) : null;

    // The text of each paragraph in sight, in the page's order, its whitespace read as one space.
    public static IReadOnlyList<string> Prose(string html) =>
        [.. InSight(Parse(html)).Where(element => element.Name == "p").Select(paragraph => TextOf(paragraph)).Where(text => text.Length > 0)];

    // Each line in sight of a figure list (FigureList) or a drain list (DrainList): its label, its amount
    // and its share as drawn, none for a line without one. Where a drain goes is not read.
    public static IReadOnlyList<(string Label, string Amount, string? Share)> Lines(string html) =>
    [
        .. InSight(Parse(html)).Where(element => element.Name == "li" && (element.Has("figure") || element.Has("drain"))).Select(line =>
        {
            var list = line.Has("figure") ? "figure" : "drain";
            var parts = InSight(line).ToList();
            return (Part(parts, $"{list}__label") ?? "", Part(parts, $"{list}__amount") ?? "", Part(parts, $"{list}__share"));
        }),
    ];

    public static IReadOnlyList<string> LineAmounts(string html) => [.. Lines(html).Select(line => line.Amount)];

    // The words in sight, in the page's order: each text read on its own, a word being what lies between
    // two spaces and holds a letter or a digit, so a separator alone, a middle dot, a dash or a quotation
    // mark, is none.
    public static IReadOnlyList<string> VisibleWords(string html) =>
        [.. TextsInSight(Parse(html)).SelectMany(text => Regex.Split(text, "\\s+")).Where(word => word.Any(char.IsLetterOrDigit))];

    // Every text that looks like a key, in sight or not, in what names or describes an element too.
    public static IReadOnlyList<string> LeakedKeys(string html) =>
        [.. AllTexts(Parse(html)).SelectMany(text => KeyLike.Matches(text).Select(match => match.Value)).Where(key => !NamesLikeKeys.Contains(key))];

    // The element of that id with all it holds, as written; a test asking for one the page does not draw
    // fails.
    private static string ById(string html, string id)
    {
        var element = Descendants(Parse(html)).FirstOrDefault(element => element.Attributes.GetValueOrDefault("id") == id);
        element.ShouldNotBeNull($"The page draws no element of id « {id} ».");

        return html[element.Start..element.End];
    }

    private static Element? FirstInSight(string html, string cssClass) => InSight(Parse(html)).FirstOrDefault(element => element.Has(cssClass));

    private static string? Part(IEnumerable<Element> parts, string cssClass) => parts.FirstOrDefault(part => part.Has(cssClass)) is { } part ? TextOf(part) : null;

    // What a viewer reads of an element, without what that class holds, as one line.
    private static string TextOf(Element element, string? leaving = null) =>
        Regex.Replace(string.Concat(TextsInSight(element, leaving)), "\\s+", " ").Trim();

    // What the hivemind leaves out of its count in the browser: a bubble's text, in the page for a screen
    // reader, and an element hidden, a folded body or a closed disclosure; and what no page shows as text.
    private static bool OutOfSight(Element element) =>
        element.Attributes.ContainsKey("hidden") || element.Has("info-bubble__text") || element.Name is "style" or "script" or "template";

    private static IEnumerable<string> TextsInSight(Node node, string? leaving = null) => node switch
    {
        Text text => [text.Value],
        Element element when OutOfSight(element) || (leaving is not null && element.Has(leaving)) => [],
        Element element => element.Children.SelectMany(child => TextsInSight(child, leaving)),
        _ => [],
    };

    private static IEnumerable<string> AllTexts(Node node) => node switch
    {
        Text text => [text.Value],
        Element element => Naming.Select(element.Attributes.GetValueOrDefault).OfType<string>().Concat(element.Children.SelectMany(AllTexts)),
        _ => [],
    };

    // The elements under the node, in the page's order.
    private static IEnumerable<Element> Descendants(Element element) =>
        element.Children.OfType<Element>().SelectMany(child => Descendants(child).Prepend(child));

    // Those of them in sight: an element out of sight is left with all it holds.
    private static IEnumerable<Element> InSight(Element element) =>
        element.Children.OfType<Element>().Where(child => !OutOfSight(child)).SelectMany(child => InSight(child).Prepend(child));

    // The HTML as a tree of elements and texts, each text decoded. An end tag closes the nearest element
    // of its name still open, with what is left open inside it; one that matches none closes nothing.
    private static Element Parse(string html)
    {
        var root = new Element("", [], 0) { End = html.Length };
        var open = new Stack<Element>([root]);
        foreach (Match token in Tokens.Matches(html))
        {
            if (token.Groups["text"].Success)
            {
                open.Peek().Children.Add(new Text(WebUtility.HtmlDecode(token.Value)));
                continue;
            }

            if (!token.Groups["name"].Success)
                continue;

            var name = token.Groups["name"].Value.ToLowerInvariant();
            var end = token.Index + token.Length;
            if (token.Groups["end"].Success)
            {
                if (open.Any(element => element.Name == name))
                {
                    Element closed;
                    do
                    {
                        closed = open.Pop();
                        closed.End = end;
                    }
                    while (closed.Name != name);
                }

                continue;
            }

            var attributes = token.Groups["attributes"].Value;
            var opened = new Element(name, AttributesOf(attributes), token.Index) { End = end };
            open.Peek().Children.Add(opened);
            if (!Void.Contains(name) && !attributes.TrimEnd().EndsWith('/'))
                open.Push(opened);
        }

        foreach (var unclosed in open)
            unclosed.End = html.Length;

        return root;
    }

    private static Dictionary<string, string> AttributesOf(string attributes)
    {
        var parsed = new Dictionary<string, string>();
        foreach (Match attribute in Attributes.Matches(attributes))
            parsed.TryAdd(attribute.Groups["name"].Value.ToLowerInvariant(), WebUtility.HtmlDecode(attribute.Groups["value"].Value));

        return parsed;
    }

    private abstract class Node;

    private sealed class Text(string value) : Node
    {
        public string Value { get; } = value;
    }

    // An element with its attributes, a boolean one such as hidden holding an empty value, and where it
    // starts and ends in the HTML.
    private sealed class Element(string name, Dictionary<string, string> attributes, int start) : Node
    {
        public string Name { get; } = name;

        public Dictionary<string, string> Attributes { get; } = attributes;

        public int Start { get; } = start;

        public int End { get; set; }

        public List<Node> Children { get; } = [];

        // One of the classes it carries, whole.
        public bool Has(string cssClass) =>
            Attributes.GetValueOrDefault("class") is { } classes && classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(cssClass);
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
