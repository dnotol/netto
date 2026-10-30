using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Netto.Tests.Support;

// The .resx files of a folder as written, build output left out, for a test that holds the files themselves
// rather than the texts the build embedded (ResxText): which there are, and each French file held to its
// neutral English one. Every text is a <data> entry's <value>. The one reader of .resx files on disk in the tests.
internal static class ResxFiles
{
    private const string French = ".fr.resx";

    // The .resx files of the folder, by their path from it, in order.
    public static IReadOnlyList<string> In(string folder) =>
        [.. Directory.EnumerateFiles(folder, "*.resx", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)];

    // What each French file says otherwise than its neutral file: a French file with no neutral one beside it, a
    // key only one of them holds, a text with other placeholders ({0}, {1:…}, in any order, since French may turn
    // a sentence round) or another number of terms marked ([term], TermSentence). Each names the French file.
    public static IReadOnlyList<string> Differences(string folder)
    {
        var differences = new List<string>();
        foreach (var french in In(folder).Where(file => file.EndsWith(French, StringComparison.Ordinal)))
        {
            var neutral = french[..^French.Length] + ".resx";
            if (!File.Exists(Path.Combine(folder, neutral)))
            {
                differences.Add($"{french}: no {Path.GetFileName(neutral)} beside it");
                continue;
            }

            var english = Texts(Path.Combine(folder, neutral));
            var translated = Texts(Path.Combine(folder, french));
            differences.AddRange(translated.Keys.Except(english.Keys).Order(StringComparer.Ordinal).Select(key => $"{french}: {key} has no English text"));
            differences.AddRange(english.Keys.Except(translated.Keys).Order(StringComparer.Ordinal).Select(key => $"{french}: {key} has no French text"));
            foreach (var key in english.Keys.Intersect(translated.Keys).Order(StringComparer.Ordinal))
            {
                if (!Placeholders(english[key]).SequenceEqual(Placeholders(translated[key])))
                    differences.Add($"{french}: {key} has other placeholders");
                if (Terms(english[key]) != Terms(translated[key]))
                    differences.Add($"{french}: {key} marks another number of terms");
            }
        }

        return differences;
    }

    // The neutral files that have no French file beside them.
    public static IReadOnlyList<string> WithoutFrench(string folder)
    {
        var files = In(folder).ToHashSet(StringComparer.Ordinal);
        return [.. files.Where(file => !IsLocalized(file) && !files.Contains(file[..^".resx".Length] + French)).Order(StringComparer.Ordinal)];
    }

    // A file of a culture, Home.fr.resx or Home.fr-BE.resx, rather than a neutral one.
    public static bool IsLocalized(string file) => Regex.IsMatch(file, @"\.[A-Za-z]{2,3}(-[A-Za-z]{2,4})?\.resx$");

    private static Dictionary<string, string> Texts(string file) =>
        XDocument.Load(file).Root?.Elements("data")
            .Where(data => data.Attribute("type") is null)
            .ToDictionary(data => (string?)data.Attribute("name") ?? "", data => (string?)data.Element("value") ?? "", StringComparer.Ordinal)
        ?? [];

    // Each placeholder with its format, in order, so a text may place them otherwise but never drop, add or
    // change one.
    private static IEnumerable<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+(?:[,:][^}]*)?\}").Select(match => match.Value).Order(StringComparer.Ordinal);

    private static int Terms(string text) => Regex.Count(text, @"\[[^\]]+\]");
}
