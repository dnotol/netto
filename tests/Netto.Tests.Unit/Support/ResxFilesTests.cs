using System.Xml.Linq;
using Shouldly;

namespace Netto.Tests.Support;

// The .resx files as written, each French file held to its neutral English one: read from folders written for
// each test, the client's own files being ResxTranslationTests'.
public class ResxFilesTests
{
    [Fact]
    public void In_folder_listsEveryResxByItsPathInOrder()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"));
        folder.Write(Path.Combine("River", "Card.fr.resx"), ("Title", "Votre plan"));

        ResxFiles.In(folder.Path).ShouldBe(["Card.resx", Path.Combine("River", "Card.fr.resx")]);
    }

    [Fact]
    public void In_buildOutput_isLeftOut()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"));
        folder.Write(Path.Combine("obj", "Card.resx"), ("Title", "Your plan"));
        folder.Write(Path.Combine("bin", "Card.resx"), ("Title", "Your plan"));

        ResxFiles.In(folder.Path).ShouldBe(["Card.resx"]);
    }

    [Theory]
    [InlineData("Home.fr.resx", true)]
    [InlineData("Home.fr-BE.resx", true)]
    [InlineData("Home.resx", false)]
    [InlineData("NettoMudLocalizer.resx", false)]
    public void IsLocalized_file_isWhetherItIsOfACulture(string file, bool localized)
    {
        ResxFiles.IsLocalized(file).ShouldBe(localized);
    }

    [Fact]
    public void Differences_matchingPair_areNone()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"), ("Sum", "{0} out of {1}"), ("Term", "The [split] shares it"));
        folder.Write("Card.fr.resx", ("Title", "Votre plan"), ("Sum", "sur {1}, {0}"), ("Term", "La [répartition] le partage"));

        ResxFiles.Differences(folder.Path).ShouldBeEmpty();
    }

    [Fact]
    public void Differences_frenchFileWithoutItsNeutralFile_isNamed()
    {
        using var folder = new Folder();
        folder.Write("Card.fr.resx", ("Title", "Votre plan"));

        ResxFiles.Differences(folder.Path).ShouldBe(["Card.fr.resx: no Card.resx beside it"]);
    }

    [Fact]
    public void Differences_keyOnlyInOneFile_isNamedWithItsFile()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"), ("Help", "What it is"));
        folder.Write("Card.fr.resx", ("Title", "Votre plan"), ("Extra", "En trop"));

        ResxFiles.Differences(folder.Path).ShouldBe(["Card.fr.resx: Extra has no English text", "Card.fr.resx: Help has no French text"]);
    }

    [Theory]
    [InlineData("{0} out of {1}", "sur {0}")]
    [InlineData("{0} out of {1}", "{0} sur {2}")]
    [InlineData("{0:N2}", "{0}")]
    [InlineData("{0} and {0}", "{0}")]
    public void Differences_placeholdersThatDiffer_areNamed(string english, string french)
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Sum", english));
        folder.Write("Card.fr.resx", ("Sum", french));

        ResxFiles.Differences(folder.Path).ShouldBe(["Card.fr.resx: Sum has other placeholders"]);
    }

    [Theory]
    [InlineData("The [split] shares it", "La répartition le partage")]
    [InlineData("It is shared", "La [répartition] le partage")]
    public void Differences_termsMarkedThatDiffer_areNamed(string english, string french)
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Term", english));
        folder.Write("Card.fr.resx", ("Term", french));

        ResxFiles.Differences(folder.Path).ShouldBe(["Card.fr.resx: Term marks another number of terms"]);
    }

    [Fact]
    public void WithoutFrench_neutralFilesWithoutTheirFrenchFile_areNamed()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"));
        folder.Write("Other.resx", ("Title", "Other"));
        folder.Write("Other.fr.resx", ("Title", "Autre"));

        ResxFiles.WithoutFrench(folder.Path).ShouldBe(["Card.resx"]);
    }

    // A folder of .resx files written for one test, and gone after it.
    private sealed class Folder : IDisposable
    {
        public Folder() => Directory.CreateDirectory(Path);

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netto-resx-{Guid.NewGuid():N}");

        public void Write(string name, params (string Key, string Value)[] texts)
        {
            var file = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file) ?? Path);
            new XDocument(new XElement("root", texts.Select(text =>
                    new XElement("data", new XAttribute("name", text.Key), new XElement("value", text.Value)))))
                .Save(file);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
