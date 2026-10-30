using System.Globalization;

namespace Netto.Web.Client;

// The two languages the site speaks: French unless the viewer chose English.
public enum Language
{
    French,
    English,
}

// A language as the page runs in it. The page is started in one (Program.cs sets the UI culture before the app
// runs, from the viewer's choice, LanguagePreference): the neutral culture « fr », whose satellite resources
// hold the French texts (Home.fr.resx), or the invariant culture, whose neutral .resx files hold the English
// ones. Never « fr-BE »: the client runs without ICU data (InvariantGlobalization), where a culture created by
// name has the invariant culture as its parent, so fr-BE would never find fr's texts. Creating « fr » needs
// PredefinedCulturesOnly off (ClientGlobalizationTests). The culture of numbers is never set, so a number
// formatted without a culture, in an attribute or a style, stays invariant: what a viewer reads goes through
// Formats, which picks its culture by language.
internal static class Languages
{
    public const Language Default = Language.French;

    private const string French = "fr";

    // The language the page runs in, by its UI culture's name: any culture but « fr » reads its neutral texts,
    // which are English.
    public static Language Current =>
        string.Equals(CultureInfo.CurrentUICulture.Name, French, StringComparison.Ordinal) ? Language.French : Language.English;

    public static CultureInfo UiCulture(Language language) =>
        language == Language.French ? new CultureInfo(French) : CultureInfo.InvariantCulture;

    // The language's tag, as the viewer's choice is kept and as <html lang> names it.
    public static string Tag(Language language) => language == Language.French ? French : "en";

    // Only the two tags Tag writes are a language; anything else is none.
    public static Language? Parse(string? tag) => tag switch
    {
        French => Language.French,
        "en" => Language.English,
        _ => null,
    };

    // The page starts in the language: its texts are read in it from then on, for every thread. Once, before the
    // app runs, which is when Blazor loads the culture's satellite resources (WebAssemblyHost.RunAsync).
    public static void Apply(Language language)
    {
        var culture = UiCulture(language);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
