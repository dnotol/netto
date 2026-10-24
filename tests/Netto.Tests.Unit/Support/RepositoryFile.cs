namespace Netto.Tests.Support;

// A file of the checkout the tests were built from, found by walking up from the folder they run in:
// the official documents, the stylesheets and the sources a test reads as data.
internal static class RepositoryFile
{
    // The path of the first folder above the run that holds the file given, as its parts from the
    // checkout's root: FindUp("src", "Netto.Web", "wwwroot", "app.css").
    public static string FindUp(params string[] path)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var file = Path.Combine([directory.FullName, .. path]);
            if (File.Exists(file))
                return file;
        }

        throw new FileNotFoundException($"No {Path.Combine(path)} above {AppContext.BaseDirectory}.");
    }
}
