using System.Text.RegularExpressions;

namespace Netto.Tests.Support;

// The client's .razor files as written, for a test that holds a rule of the markup itself rather than what a
// render draws: the localization keys they read, the controls they place and the parameters those carry, or
// what they construct. The one reader of the client's .razor files on disk in the tests, and of what is
// commented out in them. (The files of Netto.Web, App.razor, are read by name, like its scripts.)
internal static partial class RazorSources
{
    // The client's folder, src/Netto.Web.Client, which every name below is relative to.
    private static readonly string Client =
        new FileInfo(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj")).DirectoryName
        ?? throw new InvalidOperationException("The client's csproj has no folder.");

    // Every .razor file of the client by its path from the client's folder, build output left out, with its text.
    public static IReadOnlyList<(string Name, string Text)> Files() =>
        [.. Directory.EnumerateFiles(Client, "*.razor", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Client, file))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .Select(file => (file.Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllText(Path.Combine(Client, file))))];

    // The text without its comments, @* *@ and <!-- -->, which draw nothing and may name a component or a key.
    public static string WithoutComments(string razor) => HtmlComment().Replace(RazorComment().Replace(razor, ""), "");

    // The opening tags of a component in a razor text, each whole from its < to its >, over as many lines as it
    // takes. A quoted value is one unit, so a > in it (a lambda, @(a => b)) does not end the tag, nor does a
    // quote inside one (Label="@L["Key"]"). A component whose name only starts with the one given is not it.
    public static IReadOnlyList<string> Tags(string razor, string component) =>
        [.. Regex.Matches(WithoutComments(razor), $"<{Regex.Escape(component)}\\b(?:[^>\"]|\"[^\"]*\")*>").Select(match => match.Value)];

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex RazorComment();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlComment();
}
