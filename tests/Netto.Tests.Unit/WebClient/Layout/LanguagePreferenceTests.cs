using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The language the page starts in: the one the viewer chose, kept in the browser under netto.language, or
// French while they have chosen none. Only the two tags the site writes count as a choice.
public class LanguagePreferenceTests
{
    [Fact]
    public void Stored_nothingChosen_isFrench()
    {
        LanguagePreference.Stored(new BrowserStorage()).ShouldBe(Language.French);
    }

    [Theory]
    [InlineData("en", Language.English)]
    [InlineData("fr", Language.French)]
    public void Stored_chosen_isTheLanguageChosen(string stored, Language language)
    {
        var storage = new BrowserStorage();
        storage.Set(LanguagePreference.StorageKey, stored);

        LanguagePreference.Stored(storage).ShouldBe(language);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EN")]
    [InlineData("english")]
    [InlineData("nl")]
    public void Stored_anythingElse_isFrench(string stored)
    {
        var storage = new BrowserStorage();
        storage.Set(LanguagePreference.StorageKey, stored);

        LanguagePreference.Stored(storage).ShouldBe(Language.French);
    }

    // A browser that refuses storage keeps no choice: French.
    [Fact]
    public void Stored_storageRefused_isFrench()
    {
        LanguagePreference.Stored(new RefusingStorage()).ShouldBe(Language.French);
    }

    [Fact]
    public void StorageKey_isNettoLanguage()
    {
        LanguagePreference.StorageKey.ShouldBe("netto.language");
    }
}
