using MudBlazor;

namespace Netto.Web.Client.Layout;

// Dark and gold, and a light theme designed beside it (the design system, CLAUDE.md's docs map).
// Colours are literal hex because MudBlazor parses each one to derive its hover, lighten and darken
// variants: a "var(--netto-gold)" string cannot be parsed. The :root blocks in Netto.Web's app.css
// mirror these for hand-written CSS; change both together (NettoThemeTests holds them equal).
internal static class NettoTheme
{
    private static readonly string[] Display = ["Fraunces", "Georgia", "serif"];

    // The mark of the company's expenses in the bar of buckets, which MudBlazor never draws: a slate grey,
    // neutral, the one cool colour among the warm ones, so it never reads as the pocket's gold or as what
    // the company keeps (operator, 2026-10-28). app.css mirrors it as --netto-expenses.
    public const string ExpensesDark = "#8C9AA6";
    public const string ExpensesLight = "#5B6670";

    public static readonly MudTheme Theme = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#D4AF37",
            PrimaryContrastText = "#0B0A08",
            Secondary = "#E9D9A6",
            SecondaryContrastText = "#0B0A08",
            Tertiary = "#9C7A2E",
            Background = "#0B0A08",
            BackgroundGray = "#12100C",
            Surface = "#15130F",
            AppbarBackground = "#0B0A08",
            AppbarText = "#F3EEE2",
            DrawerBackground = "#15130F",
            TextPrimary = "#F3EEE2",
            TextSecondary = "#A9A190",
            TextDisabled = "#6B655A",
            ActionDefault = "#C9C1AE",
            LinesDefault = "#2B2619",
            // The frame of a field and of an unselected chip, the boundary that shows where it is: 3:1
            // at least on the cards and the page (WCAG 1.4.11).
            LinesInputs = "#726A56",
            Divider = "#2B2619",
            DividerLight = "#1E1B13",
            // What does not hold, a rule the plan loses: MudBlazor's own dark red, pinned here so app.css
            // can mirror it.
            Error = "#F64E62",
        },
        // The inks swap: the page is the dark theme's text colour. Gold deepens for what is read, as
        // #D4AF37 reads 1.8:1 on this page, so MudBlazor's own gold fills take the card's colour as
        // text; the main button keeps the brand gold through its class (Home.razor.css).
        PaletteLight = new PaletteLight
        {
            Primary = "#7E5F0B",
            PrimaryContrastText = "#FBF9F4",
            Secondary = "#5E4D1F",
            SecondaryContrastText = "#FBF9F4",
            Tertiary = "#9C7A2E",
            Background = "#F3EEE2",
            BackgroundGray = "#EAE4D6",
            Surface = "#FBF9F4",
            AppbarBackground = "#F3EEE2",
            AppbarText = "#1B1812",
            DrawerBackground = "#FBF9F4",
            TextPrimary = "#1B1812",
            TextSecondary = "#5F5747",
            TextDisabled = "#9D9584",
            ActionDefault = "#57503F",
            LinesDefault = "#DCD4C2",
            LinesInputs = "#8A806C",
            Divider = "#DCD4C2",
            DividerLight = "#E9E3D5",
            Warning = "#8A5600",
            Error = "#B42335",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"],
            },
            H1 = new H1Typography
            {
                FontFamily = Display,
                FontWeight = "600",
                FontSize = "clamp(2.6rem, 6.2vw, 4.75rem)",
                LineHeight = "1.04",
                LetterSpacing = "-0.02em",
            },
            H2 = new H2Typography
            {
                FontFamily = Display,
                FontWeight = "600",
                FontSize = "clamp(2rem, 4vw, 3rem)",
                LineHeight = "1.12",
                LetterSpacing = "-0.015em",
            },
            H3 = new H3Typography
            {
                FontFamily = Display,
                FontWeight = "600",
                FontSize = "1.5rem",
                LineHeight = "1.25",
                LetterSpacing = "0",
            },
            Button = new ButtonTypography
            {
                FontWeight = "600",
                TextTransform = "none",
                LetterSpacing = "0.01em",
            },
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px" },
    };
}
