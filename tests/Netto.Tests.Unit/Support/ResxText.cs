using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Netto.Tests.Support;

// The texts of one component's .resx, read as IStringLocalizer<TComponent> reads them (River/RentCard.resx
// under « Netto.Web.Client.River.RentCard »), in the invariant culture the site is written in, for a test
// to hold the keys a helper builds to the texts that are there. The one reader of a .resx in the tests.
internal sealed class ResxText
{
    private const string Suffix = ".resources";

    private readonly ResourceManager _resources;

    private ResxText(string baseName, ResourceManager resources)
    {
        BaseName = baseName;
        _resources = resources;
    }

    // The name IStringLocalizer<TComponent> reads the .resx under: the component's full name.
    public string BaseName { get; }

    public static ResxText For<TComponent>() => For(typeof(TComponent));

    // Fails at once for a type that has no .resx: every key would look missing, and a test that a text is
    // gone would pass for nothing.
    public static ResxText For(Type component)
    {
        var baseName = component.FullName ?? throw new InvalidOperationException($"{component} has no full name, so no .resx is named after it.");
        var texts = new ResxText(baseName, new ResourceManager(baseName, component.Assembly));
        _ = texts.Entries;
        return texts;
    }

    // The .resx of every component the assembly embeds, found as the build embedded them.
    public static IEnumerable<ResxText> InAssembly(Assembly assembly) =>
        assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(Suffix, StringComparison.Ordinal))
            .Select(name => name[..^Suffix.Length])
            .Select(baseName => new ResxText(baseName, new ResourceManager(baseName, assembly)));

    // Every text the .resx holds, with its key.
    public IReadOnlyList<(string Key, string Value)> Entries
    {
        get
        {
            var set = _resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)
                ?? throw new MissingManifestResourceException($"No .resx is embedded under {BaseName}.");
            var entries = new List<(string, string)>();
            foreach (DictionaryEntry entry in set)
            {
                if (entry.Key is string key && entry.Value is string value)
                    entries.Add((key, value));
            }

            return entries;
        }
    }

    // Every key the .resx holds a text under.
    public IReadOnlyList<string> Keys => [.. Entries.Select(entry => entry.Key)];

    // The text under the key, or null when there is none.
    public string? Get(string key) => _resources.GetString(key, CultureInfo.InvariantCulture);

    // The text with its placeholders filled, as a viewer reads it; the key itself where there is no text,
    // as the localizer shows a missing one.
    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, Get(key) ?? key, arguments);

    // Each of the keys that has no text, once: what a misspelt key would show on screen as the key itself.
    public IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Distinct().Where(key => Get(key) is null);
}
