using System.Collections;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient;

// Every text of the interface, as the client is built with it: each component's .resx, embedded in its
// assembly.
public class InterfaceTextTests
{
    // A company car's costs are a cost of the company, like its running costs, and the gain over Today is
    // in cash alone (operator, 2026-10-23): no text of the page names a muddy pocket any more.
    [Fact]
    public void Resources_everyText_namesNoMuddyPocket()
    {
        var texts = Texts().ToList();

        texts.ShouldSatisfyAllConditions(
            () => texts.ShouldContain(text => text.Key == "Bucket_pocket"),
            () => texts.Where(text => text.Value.Contains("muddy", StringComparison.OrdinalIgnoreCase)).Select(text => $"{text.Resource}: {text.Key}").ShouldBeEmpty());
    }

    // Each text of every resource the client embeds, with the resource and the key it is under.
    private static IEnumerable<(string Resource, string Key, string Value)> Texts()
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
}
