using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Company year's figure, line by line: what the company owes INASTI for the year whatever it pays its
// director, taken off what it earns before the levers and the split. The river's example is in the lower
// band, 409.30; the upper band owes 1,022.38, above a balance-sheet total of 879,163.44.
public class CompanyYearFiguresTests
{
    // The name IStringLocalizer<CompanyYearCard> reads River/CompanyYearCard.resx under.
    private static readonly ResxText Resources = ResxText.For<CompanyYearCard>();

    [Fact]
    public void Of_example_takesTheLowerBandsContributionOff()
    {
        Lines(RiverInputs.Example).ShouldBe([(CompanyYearFigures.Contribution, -409.30m)]);
    }

    [Fact]
    public void Of_upperBand_takesItsContributionOff()
    {
        Lines(RiverInputs.Example.WithCompanyContributionBand(CompanyContributionBand.Upper)).ShouldBe([(CompanyYearFigures.Contribution, -1_022.38m)]);
    }

    // A company the law exempts owes nothing: no line of nothing.
    [Fact]
    public void Of_exempt_isNoLine()
    {
        Lines(RiverInputs.Example.WithCompanyContributionBand(CompanyContributionBand.Exempt)).ShouldBeEmpty();
    }

    // Its bubble says what each band owes and the balance-sheet total between them, as the year's rules hold them.
    [Fact]
    public void Of_example_bubbleNamesEachBandsAmountAndTheThreshold()
    {
        var line = Of(RiverInputs.Example).Single();

        line.ShouldSatisfyAllConditions(
            () => line.Help.ShouldBe(CompanyYearFigures.ContributionHelp),
            () => line.HelpArgs.ShouldBe(["€409.30", "€879,163.44", "€1,022.38"]));
    }

    // Where its figure comes from: the contribution's step, with INASTI's amounts for the year, the law and
    // the article the company deducts it under.
    [Fact]
    public void Sources_example_areTheContributionsStep()
    {
        var details = CompanyYearFigures.Sources(RiverInputs.Example.Run().Year.ShouldNotBeNull());

        details.ShouldSatisfyAllConditions(
            () => details.Select(detail => (detail.RuleKey, detail.Amount)).ShouldBe([("company_contribution", 409.30m)]),
            () => details.Single().Sources.Select(group => group.Source).ShouldBe(
                [CompanyContributionRules.IncomeYear2026.Source, CompanyContributionRules.IncomeYear2026.Law, CompanyContributionRules.IncomeYear2026.Deduction]));
    }

    [Fact]
    public void CompanyYearCardResx_theLineAndItsBubble_haveATextTakingTheirArguments()
    {
        var line = Of(RiverInputs.Example).Single();

        line.ShouldSatisfyAllConditions(
            () => Resources.Missing([line.Key, line.Help!]).ShouldBeEmpty(),
            () => Resources.Format(line.Help!, [.. line.HelpArgs]).ShouldContain("€879,163.44"));
    }

    private static IReadOnlyList<FigureLine> Of(RiverInputs inputs) => CompanyYearFigures.Of(inputs.Run().Year.ShouldNotBeNull());

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) => [.. Of(inputs).Select(line => (line.Key, line.Amount))];
}
