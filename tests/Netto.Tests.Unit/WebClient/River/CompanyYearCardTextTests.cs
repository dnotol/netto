using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Company year card builds resource keys from the band of INASTI's company contribution, for its
// choice and for the line its disclosure folds to. A key without a text in its .resx would show on
// screen as the key itself.
public class CompanyYearCardTextTests
{
    // The name IStringLocalizer<CompanyYearCard> reads River/CompanyYearCard.resx under.
    private static readonly ResxText Resources = ResxText.For<CompanyYearCard>();

    [Fact]
    public void CompanyYearCardResx_everyBandsChoiceAndLine_hasAText()
    {
        var keys = Enum.GetValues<CompanyContributionBand>().SelectMany(band => new[] { CompanyYearCardText.Band(band), CompanyYearCardText.BandLine(band) });

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // Each band's choice and line state the threshold, the line the year of the balance sheet too.
    [Theory]
    [InlineData(CompanyContributionBand.Lower)]
    [InlineData(CompanyContributionBand.Upper)]
    public void CompanyYearCardResx_everyBand_statesTheThresholdAndTheYear(CompanyContributionBand band)
    {
        var choice = Resources.Format(CompanyYearCardText.Band(band), "€879,163.44");
        var line = Resources.Format(CompanyYearCardText.BandLine(band), "2024", "€879,163.44");

        line.ShouldSatisfyAllConditions(
            () => choice.ShouldContain("€879,163.44"),
            () => line.ShouldContain("2024"),
            () => line.ShouldContain("€879,163.44"));
    }

    [Fact]
    public void Band_eachBand_hasAKeyOfItsOwn()
    {
        Enum.GetValues<CompanyContributionBand>().Select(CompanyYearCardText.Band).Concat(Enum.GetValues<CompanyContributionBand>().Select(CompanyYearCardText.BandLine))
            .ShouldBeUnique();
    }
}
