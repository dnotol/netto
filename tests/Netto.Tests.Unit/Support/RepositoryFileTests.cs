using Shouldly;

namespace Netto.Tests.Support;

// The checkout's own files, found from wherever the tests run, for a test that reads the source.
public class RepositoryFileTests
{
    [Fact]
    public void FindUp_fileOfTheCheckout_givesItsPath()
    {
        var path = RepositoryFile.FindUp("tests", "Netto.Tests.Unit", "Netto.Tests.Unit.csproj");

        File.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public void FindUp_fileOfAnotherFolderLevel_isFoundFromTheSameStart()
    {
        var path = RepositoryFile.FindUp("official-docs", "INDEX.md");

        Path.GetFileName(path).ShouldBe("INDEX.md");
    }

    // A worktree sits inside the main checkout (.claude/worktrees/<name>): a file the worktree deleted is
    // still in the checkout above it, and a test must not read that one. The checkout's root holds .git, a
    // folder in the main checkout and a file in a worktree.
    [Fact]
    public void FindUp_fileOnlyAboveTheCheckoutsRoot_isNotFound()
    {
        var outer = Directory.CreateTempSubdirectory("netto-findup-");
        try
        {
            File.WriteAllText(Path.Combine(outer.FullName, "only-above.txt"), "");
            var checkout = outer.CreateSubdirectory("worktree");
            File.WriteAllText(Path.Combine(checkout.FullName, ".git"), "gitdir: elsewhere");
            var run = checkout.CreateSubdirectory("bin");

            Should.Throw<FileNotFoundException>(() => RepositoryFile.FindUpFrom(run.FullName, "only-above.txt"));
        }
        finally
        {
            outer.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindUp_fileAtTheCheckoutsRoot_isFound()
    {
        var checkout = Directory.CreateTempSubdirectory("netto-findup-");
        try
        {
            Directory.CreateDirectory(Path.Combine(checkout.FullName, ".git"));
            File.WriteAllText(Path.Combine(checkout.FullName, "at-root.txt"), "");
            var run = checkout.CreateSubdirectory("bin");

            RepositoryFile.FindUpFrom(run.FullName, "at-root.txt").ShouldBe(Path.Combine(checkout.FullName, "at-root.txt"));
        }
        finally
        {
            checkout.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindUp_fileThatDoesNotExist_failsNamingIt()
    {
        var failure = Should.Throw<FileNotFoundException>(() => RepositoryFile.FindUp("src", "no-such-file.txt"));

        failure.Message.ShouldContain(Path.Combine("src", "no-such-file.txt"));
    }
}
