using System.Globalization;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// The names the map draws its modules by, as MapTab's localizer gives them, for a test to lay a map out as the
// page does (MapView.From, MapLayout.Grid): MapTab's texts in English or in French, or in the language the test
// reads (InLanguage), as the page would.
internal static class MapNames
{
    private static readonly ResxText EnglishTexts = ResxText.For<MapTab>();
    private static readonly ResxText FrenchTexts = ResxText.French<MapTab>();

    public static string English(RiverModule module) => Name(EnglishTexts, module);

    public static string French(RiverModule module) => Name(FrenchTexts, module);

    // In the language the test reads: French under InLanguage.French(), else English.
    public static string Drawn(RiverModule module) =>
        CultureInfo.CurrentUICulture.Name == Languages.UiCulture(Language.French).Name ? French(module) : English(module);

    private static string Name(ResxText texts, RiverModule module) =>
        texts.Get(MapTabText.Module(module)) ?? throw new InvalidOperationException($"MapTab's texts in « {texts.Culture.Name} » do not name {module}.");
}
