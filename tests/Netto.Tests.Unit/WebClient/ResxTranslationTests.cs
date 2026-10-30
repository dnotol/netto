using Shouldly;

namespace Netto.Tests.WebClient;

// Each component's French texts are a .fr.resx beside its neutral English one, and the two say the same things:
// the same keys, each text with the same placeholders and as many terms of art marked (ResxFiles.Differences).
// A key the French file lacks would show in English in the middle of a French page; one the neutral file lacks,
// or a placeholder the other text lacks, would show a text that is not the one the code fills.
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
        ResxFiles.Differences(Client).ShouldBeEmpty();
    }

    [Fact]
    public void ClientResources_everyNeutralFile_hasItsFrenchFile()
    {
        Assert.SkipUnless(FrenchIsComplete, "The French translation is under way: FrenchIsComplete turns this on.");

        ResxFiles.WithoutFrench(Client).ShouldBeEmpty();
    }
}
