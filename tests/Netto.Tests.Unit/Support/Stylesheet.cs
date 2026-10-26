using System.Globalization;
using System.Text.RegularExpressions;

namespace Netto.Tests.Support;

// A stylesheet of the checkout read as data, the one reader of it the tests share: a test that holds a
// stylesheet to the figures the layout keeps room for, or to the tokens both themes declare, reads it here.
internal static class Stylesheet
{
    // The text of a stylesheet of the checkout, by its path's parts from the checkout's root.
    public static string Read(params string[] path) => File.ReadAllText(RepositoryFile.FindUp(path));

    // The declarations of a stylesheet's rules for a selector alone, in a media query or not, never those of
    // a longer selector that starts with it.
    public static string Block(string css, string selector) =>
        string.Concat(Regex.Matches(css, $@"(?m)^\s*{Regex.Escape(selector)}\s*\{{([^}}]*)\}}").Select(match => match.Groups[1].Value));

    // A media query's whole block, from its query, written as the stylesheet writes it ("(min-width: 1201px)"),
    // to the brace that closes it.
    public static string MediaBlock(string css, string query)
    {
        var start = css.IndexOf($"@media {query}", StringComparison.Ordinal);
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(query), query, "A media query the stylesheet does not write.");

        var depth = 0;
        for (var index = css.IndexOf('{', start); index < css.Length; index++)
        {
            depth += css[index] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
                return css[start..(index + 1)];
        }

        throw new InvalidOperationException($"The block of {query} is never closed.");
    }

    // Every rule of a stylesheet, a media query's own rules among them, with its selector and its
    // declarations, the comments left out.
    public static IReadOnlyList<(string Selector, string Declarations)> Rules(string css) =>
    [
        .. Regex.Matches(Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline), @"([^{}]+)\{([^{}]*)\}")
            .Select(match => (match.Groups[1].Value.Trim(), match.Groups[2].Value)),
    ];

    // A length in rem as a stylesheet writes it: 3.25rem.
    public static string Rem(decimal value) => value.ToString(CultureInfo.InvariantCulture) + "rem";

    // The tokens app.css declares for a theme, each with its value as written: dark's in :root, light's in
    // :root[data-theme="light"], which overrides them, each the block holding the palette's mirror.
    public static IReadOnlyDictionary<string, string> ThemeTokens(string theme)
    {
        var selector = theme switch
        {
            "dark" => @":root\s*\{",
            "light" => @":root\[data-theme=""light""\]\s*\{",
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "A theme app.css declares no tokens for."),
        };
        var css = Read("src", "Netto.Web", "wwwroot", "app.css");
        var block = Regex.Matches(css, selector + @"([^}]*)\}").Select(match => match.Groups[1].Value).FirstOrDefault(body => body.Contains("--netto-page", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"app.css has no block of the {theme} theme's tokens.");

        return Regex.Matches(block, @"(--netto-[a-z-]+)\s*:\s*([^;]+);").ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value.Trim());
    }
}
