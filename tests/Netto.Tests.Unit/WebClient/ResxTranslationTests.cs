using Shouldly;

namespace Netto.Tests.WebClient;

// Each component's French texts are a .fr.resx beside its neutral English one, and the two say the same things:
// the same keys, each text with the same placeholders and as many terms of art marked (ResxFiles.Differences).
// A key the French file lacks would show in English in the middle of a French page; one the neutral file lacks,
// or a placeholder the other text lacks, would show a text that is not the one the code fills.
public class ResxTranslationTests
{
    private static readonly string Client =
        Path.GetDirectoryName(RepositoryFile.FindUp("src", "Netto.Web.Client", "Netto.Web.Client.csproj"))
        ?? throw new InvalidOperationException("The client's csproj has no folder.");

    [Fact]
    public void ClientResources_frenchFiles_sayWhatTheirNeutralFilesSay()
    {
        ResxFiles.Differences(Client).ShouldBeEmpty();
    }

    // A component with no French file would show its English in the middle of a French page.
    [Fact]
    public void ClientResources_everyNeutralFile_hasItsFrenchFile()
    {
        ResxFiles.WithoutFrench(Client).ShouldBeEmpty();
    }
}
