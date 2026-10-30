using System.Text.RegularExpressions;

namespace Netto.Tests.Support;

// The client's .razor files as written, for a test that holds a rule of the markup itself rather than what a
// render draws: the localization keys they read, the controls they place and the parameters those carry. The
// one reader of .razor files on disk in the tests.
internal static class RazorSources
{
    // The client's folder, src/Netto.Web.Client, which every name below is relative to.
    private static readonly string Client =
        new FileInfo(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj")).DirectoryName
        ?? throw new InvalidOperationException("The client's csproj has no folder.");

    // A razor comment, which draws nothing and may name a component.
    private static readonly Regex Comment = new(@"@\*.*?\*@", RegexOptions.Singleline);

    // Every .razor file of the client by its path from the src folder, build output left out, with its text.
    public static IReadOnlyList<(string Name, string Text)> Files()
    {
        var src = Path.GetDirectoryName(Client) ?? throw new InvalidOperationException("The client's folder has no parent.");
        return [.. Directory.EnumerateFiles(Client, "*.razor", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(src, file).Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .Select(file => (Path.GetRelativePath(src, file).Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllText(file)))];
    }

    // The opening tags of a component in a razor text, each whole from its < to its >, over as many lines as it
    // takes. A quoted value is one unit, so a > in it (a lambda, @(a => b)) does not end the tag, nor does a
    // quote inside one (Label="@L["Key"]"). A component whose name only starts with the one given is not it.
    public static IReadOnlyList<string> Tags(string razor, string component) =>
        [.. Regex.Matches(Comment.Replace(razor, ""), $"<{Regex.Escape(component)}\\b(?:[^>\"]|\"[^\"]*\")*>").Select(match => match.Value)];
}
