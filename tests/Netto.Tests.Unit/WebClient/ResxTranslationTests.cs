using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Netto.Tests.WebClient;

// Each component's French texts are a .fr.resx beside its neutral English one, and the two say the same things:
// the same keys, each text with the same placeholders ({0}, {1:…}, in any order, since French may turn a sentence
// round) and as many terms of art marked ([term], TermSentence). A key the French file lacks would show in
// English in the middle of a French page; one the neutral file lacks, or a placeholder the other text lacks,
// would show a text that is not the one the code fills. Read from the files as written (ResxPairs).
public class ResxTranslationTests
{
    // On once every neutral .resx has its French file: until then the translation is under way, and a file not
    // yet translated shows its English.
    private const bool FrenchIsComplete = false;

    private static readonly string Client =
        Path.GetDirectoryName(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj"))
        ?? throw new InvalidOperationException("The client's csproj has no folder.");

    [Fact]
    public void ClientResources_frenchFiles_sayWhatTheirNeutralFilesSay()
    {
        ResxPairs.Differences(Client).ShouldBeEmpty();
    }

    [Fact]
    public void ClientResources_everyNeutralFile_hasItsFrenchFile()
    {
        Assert.SkipUnless(FrenchIsComplete, "The French translation is under way: FrenchIsComplete turns this on.");

        ResxPairs.WithoutFrench(Client).ShouldBeEmpty();
    }

    [Fact]
    public void Differences_matchingPair_areNone()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"), ("Sum", "{0} out of {1}"), ("Term", "The [split] shares it"));
        folder.Write("Card.fr.resx", ("Title", "Votre plan"), ("Sum", "sur {1}, {0}"), ("Term", "La [répartition] le partage"));

        ResxPairs.Differences(folder.Path).ShouldBeEmpty();
    }

    [Fact]
    public void Differences_frenchFileWithoutItsNeutralFile_isNamed()
    {
        using var folder = new Folder();
        folder.Write("Card.fr.resx", ("Title", "Votre plan"));

        ResxPairs.Differences(folder.Path).ShouldBe(["Card.fr.resx: no Card.resx beside it"]);
    }

    [Fact]
    public void Differences_keyOnlyInOneFile_isNamedWithItsFile()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"), ("Help", "What it is"));
        folder.Write("Card.fr.resx", ("Title", "Votre plan"), ("Extra", "En trop"));

        ResxPairs.Differences(folder.Path).ShouldBe(["Card.fr.resx: Extra has no English text", "Card.fr.resx: Help has no French text"]);
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

        ResxPairs.Differences(folder.Path).ShouldBe(["Card.fr.resx: Sum has other placeholders"]);
    }

    [Theory]
    [InlineData("The [split] shares it", "La répartition le partage")]
    [InlineData("It is shared", "La [répartition] le partage")]
    public void Differences_termsMarkedThatDiffer_areNamed(string english, string french)
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Term", english));
        folder.Write("Card.fr.resx", ("Term", french));

        ResxPairs.Differences(folder.Path).ShouldBe(["Card.fr.resx: Term marks another number of terms"]);
    }

    [Fact]
    public void WithoutFrench_neutralFilesWithoutTheirFrenchFile_areNamed()
    {
        using var folder = new Folder();
        folder.Write("Card.resx", ("Title", "Your plan"));
        folder.Write("Other.resx", ("Title", "Other"));
        folder.Write("Other.fr.resx", ("Title", "Autre"));

        ResxPairs.WithoutFrench(folder.Path).ShouldBe(["Card.resx"]);
    }

    // A folder of .resx files written for one test, and gone after it.
    private sealed class Folder : IDisposable
    {
        public Folder() => Directory.CreateDirectory(Path);

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netto-resx-{Guid.NewGuid():N}");

        public void Write(string name, params (string Key, string Value)[] texts) =>
            new XDocument(new XElement("root", texts.Select(text =>
                    new XElement("data", new XAttribute("name", text.Key), new XElement("value", text.Value)))))
                .Save(System.IO.Path.Combine(Path, name));

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

// The French files of a folder of .resx files held to their neutral English ones, as written: every text a
// <data> entry's <value>, build output left out. Each difference names the French file and what differs.
internal static class ResxPairs
{
    private const string French = ".fr.resx";

    public static IReadOnlyList<string> Differences(string folder)
    {
        var differences = new List<string>();
        foreach (var french in Files(folder).Where(file => file.EndsWith(French, StringComparison.Ordinal)))
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
        var files = Files(folder).ToHashSet(StringComparer.Ordinal);
        return [.. files.Where(file => !file.EndsWith(French, StringComparison.Ordinal) && !Regex.IsMatch(file, @"\.[A-Za-z]{2,3}(-[A-Za-z]{2,4})?\.resx$")
                && !files.Contains(file[..^".resx".Length] + French))
            .Order(StringComparer.Ordinal)];
    }

    // The .resx files of the folder, by their path from it, build output left out.
    private static IEnumerable<string> Files(string folder) =>
        Directory.EnumerateFiles(folder, "*.resx", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal);

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
