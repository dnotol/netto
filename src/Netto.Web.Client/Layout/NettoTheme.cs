using MudBlazor;

namespace Netto.Web.Client.Layout;

// Dark and gold. Colours are literal hex because MudBlazor parses each one to derive its hover,
// lighten and darken variants: a "var(--netto-gold)" string cannot be parsed. The :root block in
// Netto.Web's app.css mirrors these for hand-written CSS; change both together.
internal static class NettoTheme
{
    private static readonly string[] Display = ["Fraunces", "Georgia", "serif"];

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
