using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Netto.Web.Client;

namespace Netto.Tests.Support;

// The texts of one component's .resx, read as IStringLocalizer<TComponent> reads them (River/RentCard.resx
// under « Netto.Web.Client.River.RentCard »), for a test to hold the keys a helper builds to the texts that are
// there: in the invariant culture, the neutral file's English, or in French (French), the .fr.resx beside it,
// alone, so a key it lacks reads as missing rather than as its English text. The one reader of a .resx in the
// tests.
internal sealed class ResxText
{
    private const string Suffix = ".resources";

    private readonly ResourceManager _resources;

    private ResxText(string baseName, ResourceManager resources, CultureInfo culture)
    {
        BaseName = baseName;
        _resources = resources;
        Culture = culture;
    }

    // The name IStringLocalizer<TComponent> reads the .resx under: the component's full name.
    public string BaseName { get; }

    // The culture its texts are read in: the invariant one for the neutral .resx, « fr » for the .fr.resx.
    public CultureInfo Culture { get; }

    public static ResxText For<TComponent>() => For(typeof(TComponent));

    // Fails at once for a type that has no .resx: every key would look missing, and a test that a text is
    // gone would pass for nothing.
    public static ResxText For(Type component) => Read(component, CultureInfo.InvariantCulture);

    // The component's French texts, its .fr.resx alone; fails at once for a type that has none.
    public static ResxText French<TComponent>() => French(typeof(TComponent));

    public static ResxText French(Type component) => Read(component, Languages.UiCulture(Language.French));

    // The .resx of every component the assembly embeds, found as the build embedded them.
    public static IEnumerable<ResxText> InAssembly(Assembly assembly) => Embedded(assembly, CultureInfo.InvariantCulture);

    // The French texts of every component of the assembly that has a .fr.resx.
    public static IEnumerable<ResxText> FrenchInAssembly(Assembly assembly) => Embedded(assembly, Languages.UiCulture(Language.French));

    // Every text the .resx holds, with its key.
    public IReadOnlyList<(string Key, string Value)> Entries
    {
        get
        {
            var set = Set() ?? throw new MissingManifestResourceException($"No .resx is embedded under {BaseName} for « {Culture.Name} ».");
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
    public string? Get(string key) => Set()?.GetString(key);

    // The text with its placeholders filled, as a viewer reads it; the key itself where there is no text,
    // as the localizer shows a missing one.
    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, Get(key) ?? key, arguments);

    // Each of the keys that has no text, once: what a misspelt key would show on screen as the key itself.
    public IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Distinct().Where(key => Get(key) is null);

    // The component's texts in the culture, the file of that culture alone; fails at once where there is none.
    private static ResxText Read(Type component, CultureInfo culture)
    {
        var baseName = component.FullName ?? throw new InvalidOperationException($"{component} has no full name, so no .resx is named after it.");
        var texts = new ResxText(baseName, new ResourceManager(baseName, component.Assembly), culture);
        _ = texts.Entries;
        return texts;
    }

    // The components of the assembly that have a file of the culture, each read in it.
    private static IEnumerable<ResxText> Embedded(Assembly assembly, CultureInfo culture) =>
        assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(Suffix, StringComparison.Ordinal))
            .Select(name => name[..^Suffix.Length])
            .Select(baseName => new ResxText(baseName, new ResourceManager(baseName, assembly), culture))
            .Where(texts => texts.Set() is not null);

    // The file of the culture alone, never its parents': none where the culture has no file.
    private ResourceSet? Set()
    {
        try
        {
            return _resources.GetResourceSet(Culture, createIfNotExists: true, tryParents: false);
        }
        catch (MissingManifestResourceException)
        {
            return null;
        }
    }
}
