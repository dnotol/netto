using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// MudBlazor's own words Netto shows, in the page's language, through the localizer the app registers
// (Program.cs): French in French, MudBlazor's own English otherwise.
public class NettoMudLocalizerTests
{
    // The neutral file is MudBlazor's English word for word, so English reads exactly as MudBlazor writes it.
    [Fact]
    public void NeutralTexts_eachKey_isMudBlazorsOwnEnglish()
    {
        var mudBlazor = ResxText.InAssembly(typeof(MudLocalizer).Assembly).Single(texts => texts.BaseName == "MudBlazor.Resources.LanguageResource");

        var texts = ResxText.For<NettoMudLocalizer>().Entries;

        texts.Count.ShouldBe(4);
        texts.Where(text => mudBlazor.Get(text.Key) != text.Value).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Converter_InvalidNumber", "Nombre non valide")]
    [InlineData("MudInput_Clear", "Effacer")]
    [InlineData("MudInput_Increment", "Augmenter")]
    [InlineData("MudInput_Decrement", "Diminuer")]
    public void MudBlazorsWord_inFrench_isTheFrenchText(string key, string expected)
    {
        using var french = InLanguage.French();

        Words()(key).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Converter_InvalidNumber", "Not a valid number")]
    [InlineData("MudInput_Clear", "Clear")]
    public void MudBlazorsWord_inEnglish_isMudBlazorsOwn(string key, string expected)
    {
        Words()(key).ShouldBe(expected);
    }

    // A MudBlazor text Netto has no French for falls back to MudBlazor's English, never its key.
    [Fact]
    public void MudBlazorsWord_inFrenchWithoutAFrenchText_isMudBlazorsEnglish()
    {
        using var french = InLanguage.French();

        Words()("MudAlert_Close").ShouldBe("Close");
    }

    // MudBlazor's words as its components read them (its InternalMudLocalizer, internal, hands every key to the
    // interceptor), with the localizer the app registers.
    private static Func<string, string> Words()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddTransient<MudLocalizer, NettoMudLocalizer>();
        var interceptor = services.BuildServiceProvider().GetRequiredService<ILocalizationInterceptor>();
        return key => interceptor.Handle(key).Value;
    }
}
