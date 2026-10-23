using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;

namespace Netto.Tests.WebClient;

// Every text of the interface as the client is built with it, each component's .resx embedded in its
// assembly, for a test to hold the texts to a rule: all of them, or one component's; and how a text's
// words and placeholders are counted.
internal static class InterfaceTexts
{
    // Each text of every resource the client embeds, with the resource and the key it is under.
    public static IEnumerable<(string Resource, string Key, string Value)> All()
    {
        var assembly = typeof(BucketBar).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".resources", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"No stream for {resource}.");
            using var reader = new ResourceReader(stream);
            foreach (DictionaryEntry entry in reader)
            {
                if (entry.Value is string value)
                    yield return (resource, (string)entry.Key, value);
            }
        }
    }

    // Each text of one component's .resx, the one IStringLocalizer<TComponent> reads, with its key.
    public static IReadOnlyList<(string Key, string Value)> Of(Type component) =>
        [.. All().Where(text => text.Resource == $"{component.FullName}.resources").Select(text => (text.Key, text.Value))];

    // A word is what lies between two spaces and holds a letter or a digit, an amount or a placeholder
    // counting as one, as the words in sight of a page are counted (CardHtml.VisibleWords).
    public static int WordCount(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Count(word => word.Any(char.IsLetterOrDigit));

    // The index of each placeholder a text holds.
    public static IEnumerable<int> Placeholders(string text) =>
        Regex.Matches(text, @"\{(\d+)").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
}
