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

    [Fact]
    public void FindUp_fileThatDoesNotExist_failsNamingIt()
    {
        var failure = Should.Throw<FileNotFoundException>(() => RepositoryFile.FindUp("src", "no-such-file.txt"));

        failure.Message.ShouldContain(Path.Combine("src", "no-such-file.txt"));
    }
}
