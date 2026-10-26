using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The neighbourhood's texts, held to MapNeighbourhood.resx: a misspelt key would show on screen as the key
// itself, and RazorLocalizationTests holds every literal key of the component to this .resx.
public class MapNeighbourhoodTextTests
{
    // The name IStringLocalizer<MapNeighbourhood> reads Tabs/MapNeighbourhood.resx under.
    private static readonly ResxText Resources = ResxText.For<MapNeighbourhood>();

    [Fact]
    public void MapNeighbourhoodResx_theHeading_namesTheModuleChosenOrWhatTheColumnIsWhileNoneIs()
    {
        (Resources.Format("Title", "Company car"), Resources.Get("TitleNone")).ShouldBe(("Around Company car", "Around a module"));
    }

    [Fact]
    public void MapNeighbourhoodResx_whileNoneIsChosen_saysHowToChoose()
    {
        Resources.Get("Hint").ShouldBe("Choose a module on the map to see what comes into it, what goes out and the rules it sets or is under.");
    }

    [Fact]
    public void MapNeighbourhoodResx_whenNothingIsToBeListed_saysSoInWords()
    {
        (Resources.Get("NoLinks"), Resources.Get("NoFigures")).ShouldBe(("No money moves through it in this plan.", "No figures while an amount is refused."));
    }

    // The groups are named by what they hold, a line by where it comes from or goes to, a rule by which way it
    // acts, and a condition that does not hold says so.
    [Theory]
    [InlineData("ComesIn", "Comes in")]
    [InlineData("PassesThrough", "Passes through")]
    [InlineData("GoesOut", "Goes out")]
    [InlineData("Rules", "Rules")]
    [InlineData("From", "from")]
    [InlineData("To", "to")]
    [InlineData("SetsOn", "Sets on")]
    [InlineData("SetBy", "Set by")]
    [InlineData("NotHolding", "Does not hold")]
    public void MapNeighbourhoodResx_aWord_saysWhatItNames(string key, string words)
    {
        Resources.Get(key).ShouldBe(words);
    }

    // A line that runs past quiet levers names them.
    [Fact]
    public void MapNeighbourhoodResx_aLineRunningPastQuietLevers_namesThem()
    {
        Resources.Format("Past", "Expense allowances, Home office rent").ShouldBe("Runs past, holding nothing in this plan: Expense allowances, Home office rent");
    }
}
