using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A card's HTML read as a viewer reads it, for a test to hold a card to what it draws: the summary beside
// its title and the rate it ends on, the text of its paragraphs, the lines of its figure and drain lists
// and the order of the two, the bubbles, and any text a misspelt key leaves; one card cut out of the Plan
// page, and that page drawn with every card open. It reads what the renderer writes (RiverRendering),
// encoded or decoded alike. In sight is what a viewer reads: every text but what a bubble's text or an
// element hidden holds, a folded body or a closed disclosure. What it reads is the card's figures and
// its shape, never how many words it takes to say them.
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

    // An amount as Formats writes it: euros with their sign, or a share.
    private static readonly Regex AmountPattern = new("[−+-]?€[0-9][0-9,]*(?:\\.[0-9]+)?|[0-9]+(?:\\.[0-9]+)?%");

    // What a sentence holds and a figure's label does not.
    private static readonly char[] SentencePunctuation = ['.', ',', ':', ';', '?'];

    // The start of the name an (i) carries, followed by the label it hangs on.
    private const string BubbleTrigger = "More about ";

    // What names or describes an element, where a key can leak as well as into a text.
    private static readonly string[] Naming = ["aria-label", "aria-valuetext", "title", "placeholder", "alt"];

    // The Plan page on the river given, every card in it open as the outline's « Open all » leaves them:
    // the folds inside the cards, their sources and their conditions, and the notes under the river stay
    // folded. No plan is saved, so none is starred Today.
    public static Task<string> RenderPlanOpenAsync(RiverInputs river) => RenderPlanOpenAsync(river, On23October);

    // The same on the browser's clock at that instant, for a card that reads the date.
    public static Task<string> RenderPlanOpenAsync(RiverInputs river, DateTimeOffset on)
    {
        var folds = new Folds(new BrowserStorage());
        foreach (var card in OutlineRows.Order)
            folds.Set(card, true);

        return RiverRendering.RenderAsync(typeof(Plan), folds, [], services =>
        {
            var storage = new BrowserStorage();
            var plans = new SavedPlans(storage);
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = river });
            services.AddSingleton<TimeProvider>(new Clock(on));
        });
    }

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
        .. LineElements(html).Select(line =>
        {
            var list = ListOf(line);
            var parts = InSight(line).ToList();
            return (Part(parts, $"{list}__label") ?? "", Part(parts, $"{list}__amount") ?? "", Part(parts, $"{list}__share"));
        }),
    ];

    public static IReadOnlyList<string> LineAmounts(string html) => [.. Lines(html).Select(line => line.Amount)];

    // The lists the lines in sight come from, run by run in the page's order: « figure » for a figure
    // list, « drain » for a drain list, each with the number of its lines before the other list's follow.
    // A card that draws its figure lines before its drain lines has the two runs in that order.
    public static IReadOnlyList<(string List, int Lines)> LineRuns(string html)
    {
        var runs = new List<(string List, int Lines)>();
        foreach (var list in LineElements(html).Select(ListOf))
        {
            if (runs.Count > 0 && runs[^1].List == list)
                runs[^1] = (list, runs[^1].Lines + 1);
            else
                runs.Add((list, 1));
        }

        return runs;
    }

    // The amounts a text draws, in its order, as Formats writes them: euros with their sign, or a share.
    public static IReadOnlyList<string> Amounts(string text) => [.. AmountPattern.Matches(text).Select(match => match.Value)];

    // The labels of a summary that are not a figure's name. A summary is figures with their labels joined
    // by « · »: once its amounts are taken out, each label holds a letter, a bare figure having none, and
    // none of the punctuation a sentence would. None for a summary of that shape.
    public static IReadOnlyList<string> SummaryFaults(string summary) =>
    [
        .. AmountPattern.Replace(summary, "").Split('·').Select(label => label.Trim())
            .Where(label => label.IndexOfAny(SentencePunctuation) >= 0 || !label.Any(char.IsLetter)),
    ];

    // Every text that looks like a key, in sight or not, in what names or describes an element too.
    public static IReadOnlyList<string> LeakedKeys(string html) =>
        [.. AllTexts(Parse(html)).SelectMany(text => KeyLike.Matches(text).Select(match => match.Value)).Where(key => !NamesLikeKeys.Contains(key))];

    // What the bubble after that label says, as a screen reader is given it: the text its (i), named
    // « More about » the label, is described by, read as one line. A label with no bubble after it fails
    // the test that asks.
    public static string BubbleAfter(string html, string label)
    {
        var elements = Descendants(Parse(html)).ToList();
        var trigger = elements.FirstOrDefault(element => element.Attributes.GetValueOrDefault("aria-label") == $"{BubbleTrigger}{label}");
        trigger.ShouldNotBeNull($"No bubble follows « {label} ».");
        var text = DescribedBy(elements, trigger);
        text.ShouldNotBeNull($"The bubble after « {label} » holds no text.");

        return BubbleText(text);
    }

    // Every bubble of the page in its order, each (i) named « More about » a label: the label and the text
    // it is described by, as a screen reader is given it; none where the page holds no such text, for a
    // test to find a bubble that says nothing.
    public static IReadOnlyList<(string Label, string Text)> Bubbles(string html)
    {
        var elements = Descendants(Parse(html)).ToList();

        return
        [
            .. elements.Where(element => element.Attributes.GetValueOrDefault("aria-label") is { } name && name.StartsWith(BubbleTrigger, StringComparison.Ordinal))
                .Select(trigger => (trigger.Attributes["aria-label"][BubbleTrigger.Length..], DescribedBy(elements, trigger) is { } text ? BubbleText(text) : "")),
        ];
    }

    // The element of that id with all it holds, as written; a test asking for one the page does not draw
    // fails.
    private static string ById(string html, string id)
    {
        var element = Descendants(Parse(html)).FirstOrDefault(element => element.Attributes.GetValueOrDefault("id") == id);
        element.ShouldNotBeNull($"The page draws no element of id « {id} ».");

        return html[element.Start..element.End];
    }

    // The lines in sight of the figure lists and the drain lists, in the page's order.
    private static IEnumerable<Element> LineElements(string html) =>
        InSight(Parse(html)).Where(element => element.Name == "li" && (element.Has("figure") || element.Has("drain")));

    private static string ListOf(Element line) => line.Has("figure") ? "figure" : "drain";

    // The element a bubble's (i) is described by, the text of the bubble.
    private static Element? DescribedBy(List<Element> elements, Element trigger)
    {
        var describedBy = trigger.Attributes.GetValueOrDefault("aria-describedby");

        return elements.FirstOrDefault(element => describedBy is not null && element.Attributes.GetValueOrDefault("id") == describedBy);
    }

    private static string BubbleText(Element text) =>
        Regex.Replace(string.Concat(text.Children.SelectMany(child => TextsInSight(child))), "\\s+", " ").Trim();

    private static Element? FirstInSight(string html, string cssClass) => InSight(Parse(html)).FirstOrDefault(element => element.Has(cssClass));

    private static string? Part(IEnumerable<Element> parts, string cssClass) => parts.FirstOrDefault(part => part.Has(cssClass)) is { } part ? TextOf(part) : null;

    // What a viewer reads of an element, without what that class holds, as one line.
    private static string TextOf(Element element, string? leaving = null) =>
        Regex.Replace(string.Concat(TextsInSight(element, leaving)), "\\s+", " ").Trim();

    // What a viewer does not read: a bubble's text, in the page for a screen reader, and an element hidden,
    // a folded body or a closed disclosure; and what no page shows as text.
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
}
