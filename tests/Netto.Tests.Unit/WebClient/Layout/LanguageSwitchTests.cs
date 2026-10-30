using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Layout;

// The language switch, one component in the site's header and in the plan page's band, before the theme switch:
// a button named by the other language in that language, marked as such for a screen reader, so its name is
// what it shows (WCAG 2.5.3) and is read in the language it is written in (WCAG 3.1.2). What a click does is
// LanguagePreferenceTests'.
public class LanguageSwitchTests
{
    [Fact]
    public async Task LanguageSwitch_inEnglish_offersFrench()
    {
        var button = Button(await RenderAsync(typeof(LanguageSwitch), new Folds(new BrowserStorage()), []));

        (button.Lang, button.Text, button.Label).ShouldBe(("fr", "Français", null));
    }

    [Fact]
    public async Task LanguageSwitch_inFrench_offersEnglish()
    {
        using var french = InLanguage.French();

        var button = Button(await RenderAsync(typeof(LanguageSwitch), new Folds(new BrowserStorage()), []));

        (button.Lang, button.Text, button.Label).ShouldBe(("en", "English", null));
    }

    // The one button drawn: its language, its text and any name given it apart from its text.
    private static (string Lang, string Text, string? Label) Button(string html)
    {
        var buttons = Regex.Matches(html, "<button([^>]*)>(.*?)</button>", RegexOptions.Singleline);
        buttons.Count.ShouldBe(1);
        var attributes = buttons[0].Groups[1].Value;
        var label = Regex.Match(attributes, "aria-label=\"([^\"]*)\"");
        var text = WebUtility.HtmlDecode(Regex.Replace(buttons[0].Groups[2].Value, "<[^>]*>", "")).Trim();
        return (Regex.Match(attributes, "lang=\"([^\"]*)\"").Groups[1].Value, text, label.Success ? label.Groups[1].Value : null);
    }
}
