using System.Text;
using System.Text.RegularExpressions;

namespace Netto.Tests.WebClient;

// The keys a .razor file reads its texts by, as written in it: for each localizer the file injects
// (@inject IStringLocalizer<TComponent> Name), every Name[...] whose first argument holds no call, and in
// that argument every plain string literal: L["Key"], L["Key", args], L[IsDark ? "A" : "B"]. A key a helper
// builds (L[RentCardText.Cap(x)]) or an interpolated one is the helper's test's to hold. What is commented
// out, @* *@ or <!-- -->, is not read.
internal static partial class RazorKeys
{
    // The localizers the file injects, each as the component its .resx is named after and the name it is
    // read by.
    public static IReadOnlyList<(string Component, string Name)> Localizers(string razor) =>
    [
        .. Injection().Matches(WithoutComments(razor))
            .Select(match => (Component: match.Groups["component"].Value.Split('.')[^1], Name: match.Groups["name"].Value)),
    ];

    // Every literal key the file reads, with the component whose .resx holds it.
    public static IReadOnlyList<(string Component, string Key)> Keys(string razor)
    {
        var code = WithoutComments(razor);
        var keys = new List<(string, string)>();
        foreach (var (component, name) in Localizers(razor))
        {
            foreach (Match match in new Regex($@"(?<![\w.]){Regex.Escape(name)}\[").Matches(code))
            {
                var first = FirstArgument(code, match.Index + match.Length);
                if (first.Contains('(', StringComparison.Ordinal))
                    continue;

                keys.AddRange(PlainLiteral().Matches(first).Select(literal => (component, literal.Groups["text"].Value)));
            }
        }

        return keys;
    }

    private static string WithoutComments(string razor) => RazorSources.WithoutComments(razor);

    // The indexer's first argument, from just after its opening bracket to its first comma or its closing
    // bracket outside any nested bracket, parenthesis, brace or string.
    private static string FirstArgument(string code, int start)
    {
        var argument = new StringBuilder();
        var depth = 0;
        for (var i = start; i < code.Length; i++)
        {
            var c = code[i];
            if (c is '"' or '\'')
            {
                var end = EndOfLiteral(code, i);
                argument.Append(code, i, end - i + 1);
                i = end;
                continue;
            }

            if (depth == 0 && c is ']' or ',')
                break;
            depth += c switch
            {
                '[' or '(' or '{' => 1,
                ']' or ')' or '}' => -1,
                _ => 0,
            };
            argument.Append(c);
        }

        return argument.ToString();
    }

    // Where the string or character literal opening at the index ends: its closing quote, past escaped ones.
    private static int EndOfLiteral(string code, int open)
    {
        for (var i = open + 1; i < code.Length; i++)
        {
            if (code[i] == '\\')
                i++;
            else if (code[i] == code[open])
                return i;
        }

        return code.Length - 1;
    }

    [GeneratedRegex(@"@inject\s+IStringLocalizer<\s*(?<component>[\w.]+)\s*>\s+(?<name>\w+)")]
    private static partial Regex Injection();

    [GeneratedRegex(@"(?<![$@\w])""(?<text>(?:[^""\\]|\\.)*)""")]
    private static partial Regex PlainLiteral();
}
