using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient;

// A key a .razor file reads its text by, misspelt or left without its text, shows on the page as the key
// itself, and a static render only meets the branches it draws. So every literal key of every .razor file
// under src (RazorKeys) is held to the .resx of the component its localizer is injected for, as the client
// embeds it (ResxText): a text in a refusal's branch or a dialog the tests never open is held too.
public class RazorLocalizationTests
{
    // Every .resx the client embeds, by the component it is named after: the last part of its base name.
    private static readonly ILookup<string, ResxText> Resources =
        ResxText.InAssembly(typeof(BucketBar).Assembly).ToLookup(texts => texts.BaseName.Split('.')[^1]);

    public static TheoryData<string, string[]> Readings => new()
    {
        { "@inject IStringLocalizer<Card> L\n<p>@L[\"Title\"]</p>", ["Card:Title"] },
        { "@inject IStringLocalizer<Card> L\n@L[\"Sum\", Formats.Euros(totals[\"a\"]), year]", ["Card:Sum"] },
        { "@inject IStringLocalizer<Card> L\n@L[ThemePreference.IsDark ? \"Light\" : \"Dark\"]", ["Card:Light", "Card:Dark"] },
        { "@inject IStringLocalizer<Card> L\n@L[confirming ? \"Delete_Confirm\" : \"Delete\", row.Name]", ["Card:Delete_Confirm", "Card:Delete"] },
        { "@inject IStringLocalizer<Card> L\n@L[CardText.Cap(cap, \"x\")]", [] },
        { "@inject IStringLocalizer<Card> L\n@L[key]", [] },
        { "@inject IStringLocalizer<Card> L\n@L[$\"Error_{key}\"]", [] },
        { "@inject IStringLocalizer<Card> L\n@* @L[\"Gone\"] *@\n<!-- @L[\"Gone\"] -->", [] },
        { "@inject IStringLocalizer<Card> L\n@Items[\"Title\"] @XL[\"Title\"] @row.L[\"Title\"]", [] },
        { "@inject IStringLocalizer<Netto.Web.Client.River.Card> L\n@L[\"Title\"]", ["Card:Title"] },
        { "@inject IStringLocalizer<A> L\n@inject IStringLocalizer<B> Other\n@L[\"X\"] @Other[\"Y\"]", ["A:X", "B:Y"] },
    };

    [Theory]
    [MemberData(nameof(Readings))]
    public void Keys_razor_areTheLiteralKeysOfEachInjectedLocalizer(string razor, string[] expected)
    {
        var keys = RazorKeys.Keys(razor);

        keys.Select(key => $"{key.Component}:{key.Key}").ShouldBe(expected);
    }

    [Fact]
    public void RazorFiles_everyInjectedLocalizer_hasOneResxNamedAfterItsComponent()
    {
        var components = RazorFiles().SelectMany(file => RazorKeys.Localizers(file.Text)).Select(localizer => localizer.Component).Distinct().ToList();

        var withoutOne = components.Where(component => Resources[component].Count() != 1).ToList();

        components.Count.ShouldBeGreaterThan(30);
        withoutOne.ShouldBeEmpty();
    }

    [Fact]
    public void RazorFiles_everyLiteralKey_hasATextInItsComponentsResx()
    {
        var keys = RazorFiles().SelectMany(file => RazorKeys.Keys(file.Text).Select(key => (file.Name, key.Component, key.Key))).ToList();
        var texts = keys.Select(key => key.Component).Distinct().ToDictionary(component => component, KeysOf);

        var missing = keys.Where(key => !texts[key.Component].Contains(key.Key)).Select(key => $"{key.Name}: {key.Component}[\"{key.Key}\"]").ToList();

        keys.Count.ShouldBeGreaterThan(350);
        missing.ShouldBeEmpty();
    }

    private static IEnumerable<(string Name, string Text)> RazorFiles() => RazorSources.Files();

    // The keys the component's one .resx holds a text under: none where it has no .resx or more than one,
    // so every key of it reads as missing.
    private static HashSet<string> KeysOf(string component) =>
        Resources[component].ToList() is [var texts] ? [.. texts.Keys] : [];
}
