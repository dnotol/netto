using System.Collections;
using System.Globalization;
using System.Resources;

namespace Netto.Tests.Support;

// The texts of one component's .resx, read as IStringLocalizer<TComponent> reads them (River/RentCard.resx
// under « Netto.Web.Client.River.RentCard »), in the invariant culture the site is written in, for a test
// to hold the keys a helper builds to the texts that are there.
internal sealed class ResxText
{
    private readonly string _baseName;
    private readonly ResourceManager _resources;

    private ResxText(string baseName, ResourceManager resources)
    {
        _baseName = baseName;
        _resources = resources;
    }

    public static ResxText For<TComponent>() => For(typeof(TComponent));

    // Fails at once for a type that has no .resx: every key would look missing, and a test that a text is
    // gone would pass for nothing.
    public static ResxText For(Type component)
    {
        var baseName = component.FullName!;
        var texts = new ResxText(baseName, new ResourceManager(baseName, component.Assembly));
        _ = texts.Keys;
        return texts;
    }

    // Every key the .resx holds.
    public IReadOnlyList<string> Keys =>
        [.. (_resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)
                ?? throw new MissingManifestResourceException($"No .resx is embedded under {_baseName}."))
            .Cast<DictionaryEntry>()
            .Select(entry => (string)entry.Key)];

    // The text under the key, or null when there is none.
    public string? Get(string key) => _resources.GetString(key, CultureInfo.InvariantCulture);

    // The text with its placeholders filled, as a viewer reads it; the key itself where there is no text,
    // as the localizer shows a missing one.
    public string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, Get(key) ?? key, arguments);

    // Each of the keys that has no text, once: what a misspelt key would show on screen as the key itself.
    public IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Distinct().Where(key => Get(key) is null);
}
