namespace Netto.Web.Client.Pages;

// Where the front page, the portfolio's (Home), lives: /about, since the app opens on the plan (PlanEntry),
// and the sections the header links to, which the page scrolls to by itself when it is opened at one.
internal static class FrontPage
{
    // The page's address from the base (<base href="/">), as PlanTabRoutes writes its own. Home's @page
    // names it too, in the one place a route is written, and PageRoutesTests holds the two together.
    public const string Path = "about";

    // The ids of the page's sections, in page order: the header links to each, and the page scrolls to one
    // when opened at it, the fragment becoming a CSS selector, so only these are taken.
    public static IReadOnlyList<string> Sections { get; } = ["levers", "how-it-works", "principles"];

    // A link to one of the page's sections, from anywhere.
    public static string Href(string section) => $"/{Path}#{section}";

    // Whether the address, as NavigationManager.ToBaseRelativePath gives it, is the front page's: the router
    // matches whatever its case and ignores a trailing slash, so this does too.
    public static bool IsAt(string relativePath) =>
        string.Equals(relativePath.Split('?', '#')[0].TrimEnd('/'), Path, StringComparison.OrdinalIgnoreCase);
}
