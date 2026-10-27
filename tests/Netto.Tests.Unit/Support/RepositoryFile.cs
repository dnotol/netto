namespace Netto.Tests.Support;

// A file of the checkout the tests were built from, found by walking up from the folder they run in:
// the official documents, the stylesheets and the sources a test reads as data.
internal static class RepositoryFile
{
    // The path of the first folder above the run that holds the file given, as its parts from the
    // checkout's root: FindUp("src", "Netto.Web", "wwwroot", "app.css").
    public static string FindUp(params string[] path) => FindUpFrom(AppContext.BaseDirectory, path);

    // The same from a folder given. The walk stops at the checkout's root, the folder holding .git (a folder
    // in the main checkout, a file in a worktree): a worktree sits inside the main checkout, and a file it
    // deleted must not be read from the checkout above it.
    public static string FindUpFrom(string start, params string[] path)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var file = Path.Combine([directory.FullName, .. path]);
            if (File.Exists(file))
                return file;

            var git = Path.Combine(directory.FullName, ".git");
            if (File.Exists(git) || Directory.Exists(git))
                break;
        }

        throw new FileNotFoundException($"No {Path.Combine(path)} above {start} in its checkout.");
    }
}
