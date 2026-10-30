using System.Globalization;
using Netto.Web.Client;
using Shouldly;

namespace Netto.Tests.WebClient;

// The site speaks French unless the viewer chose English. Its language is the UI culture the page was
// started in: the neutral culture « fr », whose satellite resources hold the French texts, or the invariant
// culture, whose neutral .resx files hold the English ones. Never « fr-BE »: in the browser's invariant mode
// its parent is the invariant culture, so it would never find the French texts.
public class LanguagesTests
{
    [Fact]
    public void Default_isFrench()
    {
        Languages.Default.ShouldBe(Language.French);
    }

    [Fact]
    public void UiCulture_french_isTheNeutralFrenchCulture()
    {
        var culture = Languages.UiCulture(Language.French);

        (culture.Name, culture.IsNeutralCulture).ShouldBe(("fr", true));
    }

    [Fact]
    public void UiCulture_english_isTheInvariantCulture()
    {
        Languages.UiCulture(Language.English).ShouldBe(CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData(Language.French, "fr")]
    [InlineData(Language.English, "en")]
    public void Tag_language_isItsTwoLetterTag(Language language, string tag)
    {
        Languages.Tag(language).ShouldBe(tag);
    }

    [Theory]
    [InlineData("fr", Language.French)]
    [InlineData("en", Language.English)]
    public void Parse_tag_isItsLanguage(string tag, Language language)
    {
        Languages.Parse(tag).ShouldBe(language);
    }

    // Only the two tags Tag writes are a language: anything else is none.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FR")]
    [InlineData("fr-BE")]
    [InlineData("nl")]
    public void Parse_anythingElse_isNone(string? tag)
    {
        Languages.Parse(tag).ShouldBeNull();
    }

    // The tests run on the invariant culture (TestCultureTests).
    [Fact]
    public void Current_invariantUiCulture_isEnglish()
    {
        Languages.Current.ShouldBe(Language.English);
    }

    [Fact]
    public void Current_frenchUiCulture_isFrench()
    {
        using var french = InLanguage.French();

        Languages.Current.ShouldBe(Language.French);
    }

    // A culture the site never starts in reads as English, its neutral texts: Belgian French included.
    [Theory]
    [InlineData("fr-BE")]
    [InlineData("nl-BE")]
    [InlineData("en-GB")]
    public void Current_otherUiCulture_isEnglish(string name)
    {
        using var other = InLanguage.Culture(new CultureInfo(name));

        Languages.Current.ShouldBe(Language.English);
    }
}
