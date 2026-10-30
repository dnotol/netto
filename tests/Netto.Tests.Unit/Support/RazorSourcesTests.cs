using Shouldly;

namespace Netto.Tests.Support;

public class RazorSourcesTests
{
    [Fact]
    public void Tags_aSelfClosingTag_isTheWholeTag()
    {
        var tags = RazorSources.Tags("<p>x</p>\n<MudCheckBox T=\"bool\" Label=\"@L[\"Key\"]\" />\n", "MudCheckBox");

        tags.ShouldBe(["<MudCheckBox T=\"bool\" Label=\"@L[\"Key\"]\" />"]);
    }

    [Fact]
    public void Tags_aTagOverSeveralLinesWithALambda_isOneTag()
    {
        var razor = "<MudCheckBox T=\"bool\" ValueChanged=\"@(on => Set(on))\"\r\n    Color=\"Color.Primary\">\r\n    text\r\n</MudCheckBox>";

        var tags = RazorSources.Tags(razor, "MudCheckBox");

        tags.ShouldBe(["<MudCheckBox T=\"bool\" ValueChanged=\"@(on => Set(on))\"\r\n    Color=\"Color.Primary\">"]);
    }

    [Fact]
    public void Tags_aClosingTagAnotherComponentAndAComment_areNotTags()
    {
        var razor = "@* <MudCheckBox T=\"bool\" /> *@\n</MudCheckBox>\n<MudCheckBoxGroup T=\"bool\" />\n<MudCheckBox T=\"bool\" />";

        var tags = RazorSources.Tags(razor, "MudCheckBox");

        tags.ShouldBe(["<MudCheckBox T=\"bool\" />"]);
    }

    [Fact]
    public void Files_theClient_listsItsRazorFilesByPathFromItsFolderAndLeavesBuildOutputOut()
    {
        var names = RazorSources.Files().Select(file => file.Name).ToList();

        names.ShouldContain("River/HouseholdCard.razor");
        names.Where(name => name.StartsWith("obj/", StringComparison.Ordinal) || name.StartsWith("bin/", StringComparison.Ordinal)).ShouldBeEmpty();
    }
}
