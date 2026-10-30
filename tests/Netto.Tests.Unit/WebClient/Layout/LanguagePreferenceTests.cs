using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The language the page starts in: the one the viewer chose, kept in the browser under netto.language, or
// French while they have chosen none. Only the two tags the site writes count as a choice. Switching keeps the
// other language and reloads the page, since a language's texts load as the app starts; the river is carried
// across the reload first, so no edit is lost.
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

    // The page runs in English in the tests (TestCultureTests): the other language is French.
    [Fact]
    public void Other_inEnglish_isFrench()
    {
        LanguagePreference.Other.ShouldBe(Language.French);
    }

    [Fact]
    public void Other_inFrench_isEnglish()
    {
        using var french = InLanguage.French();

        LanguagePreference.Other.ShouldBe(Language.English);
    }

    [Fact]
    public async Task Switch_inEnglish_keepsFrench()
    {
        var storage = new BrowserStorage();

        await Preference(storage, new BrowserSession(), new PageReloads()).SwitchAsync();

        storage.Get(LanguagePreference.StorageKey).ShouldBe("fr");
    }

    [Fact]
    public async Task Switch_inFrench_keepsEnglish()
    {
        using var french = InLanguage.French();
        var storage = new BrowserStorage();

        await Preference(storage, new BrowserSession(), new PageReloads()).SwitchAsync();

        storage.Get(LanguagePreference.StorageKey).ShouldBe("en");
    }

    // By the time the page reloads, the language is kept and the river carried: the page that loads next starts
    // in the other language, on the river as it was.
    [Fact]
    public async Task Switch_reloadsOnce_afterKeepingTheLanguageAndCarryingTheRiver()
    {
        var storage = new BrowserStorage();
        var session = new BrowserSession();
        (string? Language, string? River) atReload = default;
        var page = new PageReloads(() => atReload = (storage.Get(LanguagePreference.StorageKey), session.Peek(RiverState.CarriedKey)));

        await Preference(storage, session, page).SwitchAsync();

        (page.Reloads, atReload.Language, atReload.River is not null).ShouldBe((1, "fr", true));
    }

    [Fact]
    public async Task Switch_carriesTheRiverAsItIsNow()
    {
        var session = new BrowserSession();
        var edited = RiverInputs.Example.WithDirectorPay(31_000m);
        var river = new RiverState(new SavedPlans(new BrowserStorage()), session) { Inputs = edited };

        await new LanguagePreference(new BrowserStorage(), new PageReloads(), river, new TypedFields()).SwitchAsync();

        new RiverState(new SavedPlans(new BrowserStorage()), session).Inputs.ShouldBe(edited);
    }

    // What a field holds typed and not taken yet is taken first, so the river carried holds it.
    [Fact]
    public async Task Switch_fieldHoldingWhatWasTyped_carriesTheRiverWithIt()
    {
        var session = new BrowserSession();
        var river = new RiverState(new SavedPlans(new BrowserStorage()), session);
        var fields = new TypedFields();
        using var field = fields.Add(() =>
        {
            river.Inputs = river.Inputs.WithRevenue(140_000m);
            return Task.CompletedTask;
        });

        await new LanguagePreference(new BrowserStorage(), new PageReloads(), river, fields).SwitchAsync();

        new RiverState(new SavedPlans(new BrowserStorage()), session).Inputs.Company?.Revenue.Value.ShouldBe(140_000m);
    }

    private static LanguagePreference Preference(BrowserStorage storage, BrowserSession session, PageReloads page) =>
        new(storage, page, new RiverState(new SavedPlans(new BrowserStorage()), session), new TypedFields());
}
