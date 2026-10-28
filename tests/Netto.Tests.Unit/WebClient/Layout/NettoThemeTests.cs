using System.Text.RegularExpressions;
using MudBlazor;
using MudBlazor.Utilities;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The two palettes MudBlazor draws with, dark and light, and their mirror in app.css for hand-written
// CSS: every text colour readable on every ground (WCAG 2.2 AA), and the CSS kept equal to the palette
// by hand, so the one changed without the other fails here rather than on screen.
public class NettoThemeTests
{
    public static TheoryData<string> Themes => ["dark", "light"];

    [Theory]
    [MemberData(nameof(Themes))]
    public void Palette_textOnEveryGround_reachesFourAndAHalfToOne(string theme)
    {
        var palette = Palette(theme);
        MudColor[] grounds = [palette.Background, palette.Surface, palette.BackgroundGray];
        MudColor[] texts = [palette.TextPrimary, palette.TextSecondary, palette.Primary, palette.Secondary, palette.Warning, palette.Error];

        var below = from text in texts from ground in grounds where Contrast(text, ground) < 4.5 select $"{Hex(text)} on {Hex(ground)}";

        below.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Palette_framesAndIcons_reachThreeToOne(string theme)
    {
        var palette = Palette(theme);
        MudColor[] grounds = [palette.Background, palette.Surface];
        MudColor[] marks = [palette.LinesInputs, palette.ActionDefault];

        var below = from mark in marks from ground in grounds where Contrast(mark, ground) < 3 select $"{Hex(mark)} on {Hex(ground)}";

        below.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Palette_textOnAPrimaryFill_reachesFourAndAHalfToOne(string theme)
    {
        var palette = Palette(theme);

        Contrast(palette.PrimaryContrastText, palette.Primary).ShouldBeGreaterThanOrEqualTo(4.5);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void AppCss_mirrorsThePalette(string theme)
    {
        var palette = Palette(theme);
        var mirrored = new Dictionary<string, MudColor>
        {
            ["--netto-page"] = palette.Background,
            ["--netto-surface"] = palette.Surface,
            ["--netto-text"] = palette.TextPrimary,
            ["--netto-text-muted"] = palette.TextSecondary,
            ["--netto-line"] = palette.LinesDefault,
            ["--netto-line-input"] = palette.LinesInputs,
            ["--netto-gold"] = palette.Primary,
            ["--netto-error"] = palette.Error,
            ["--netto-expenses"] = new MudColor(Expenses(theme)),
        };

        var css = AppCssColours(theme);

        mirrored.Select(token => (token.Key, css.GetValueOrDefault(token.Key)))
            .ShouldBe(mirrored.Select(token => (token.Key, (string?)Hex(token.Value))));
    }

    // The expenses' mark, a colour of its own MudBlazor never draws, mirrored in app.css with the palette:
    // 3:1 on the page and on a card (WCAG 1.4.11).
    [Theory]
    [MemberData(nameof(Themes))]
    public void Expenses_onThePageAndACard_reachThreeToOne(string theme)
    {
        var (palette, mark) = (Palette(theme), new MudColor(Expenses(theme)));

        new[] { palette.Background, palette.Surface }.Where(ground => Contrast(mark, ground) < 3).Select(Hex).ShouldBeEmpty();
    }

    private static string Expenses(string theme) => theme == "dark" ? NettoTheme.ExpensesDark : NettoTheme.ExpensesLight;

    private static Palette Palette(string theme) =>
        theme == "dark" ? NettoTheme.Theme.PaletteDark : NettoTheme.Theme.PaletteLight;

    // The hex colours declared in the theme's block of app.css: :root for dark, :root[data-theme="light"]
    // for light, which overrides them.
    private static Dictionary<string, string> AppCssColours(string theme) =>
        Stylesheet.ThemeTokens(theme).Where(token => Regex.IsMatch(token.Value, "^#[0-9A-Fa-f]{6}$"))
            .ToDictionary(token => token.Key, token => token.Value.ToLowerInvariant());

    private static string Hex(MudColor colour) => $"#{colour.R:x2}{colour.G:x2}{colour.B:x2}";

    // WCAG 2's contrast ratio, a translucent colour taken over its ground.
    private static double Contrast(MudColor foreground, MudColor ground)
    {
        var alpha = foreground.A / 255d;
        double Over(byte front, byte back) => front * alpha + back * (1 - alpha);
        var front = Luminance(Over(foreground.R, ground.R), Over(foreground.G, ground.G), Over(foreground.B, ground.B));
        var back = Luminance(ground.R, ground.G, ground.B);

        return (Math.Max(front, back) + 0.05) / (Math.Min(front, back) + 0.05);
    }

    private static double Luminance(double red, double green, double blue)
    {
        static double Channel(double value)
        {
            var c = value / 255;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(red) + 0.7152 * Channel(green) + 0.0722 * Channel(blue);
    }
}
