using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The lever cards before the split, Company year and Dividend read in figures, and what they share with
// the other cards said in as few words: where a drain line passes its money on, and the line their
// conditions fold to.
public class LeverCardsFiguresTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResourceManager DrainListResources = new("Netto.Web.Client.River.DrainList", typeof(DrainList).Assembly);

    private static readonly ResourceManager ConditionListResources = new("Netto.Web.Client.River.ConditionList", typeof(ConditionList).Assembly);

    // What a module passes on goes to the card under it, named in a few words; what a royalty leaves
    // beyond its limits says it is taxed there as pay. Where a line goes into a bucket says the bucket's
    // name as the bar does (BucketNamesTests).
    public static TheoryData<string, string> NextModules => new()
    {
        { "allowances", "to the allowances" },
        { "rent", "to the rent" },
        { "car", "to the car" },
        { "split", "to the split" },
        { "dividend", "to the dividend" },
        { "director_pay", "to Director pay, taxed as your pay" },
    };

    [Theory]
    [MemberData(nameof(NextModules))]
    public void Destination_passedOnToTheNextModule_isToItsShortName(string module, string expected)
    {
        var drain = new Drain("left", DrainKind.PassedOn, 1m, module);

        DrainListResources.GetString(DrainListText.Destination(drain), CultureInfo.InvariantCulture).ShouldBe(expected);
    }

    // A lever's conditions fold to one line that counts them and says why they are listed.
    [Theory]
    [InlineData(1, "1 condition Netto cannot check")]
    [InlineData(5, "5 conditions Netto cannot check")]
    public void ConditionsHeading_count_isTheConditionsNettoCannotCheck(int count, string expected)
    {
        var heading = ConditionListResources.GetString(ConditionListText.Heading(count), CultureInfo.InvariantCulture) ?? "";

        string.Format(CultureInfo.InvariantCulture, heading, count).ShouldBe(expected);
    }
}
