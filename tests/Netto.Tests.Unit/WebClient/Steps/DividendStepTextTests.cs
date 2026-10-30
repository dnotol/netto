using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.Steps;

// The dividend withholding's reduced rates count accounting years after the year of the capital contribution
// (CIR 92 art. 269 §2), not distributions: the rule's text and the labels of the two VVPRbis rates in its
// sources say so.
public class DividendStepTextTests
{
    // The name IStringLocalizer<StepDetails> reads Steps/StepDetails.resx under.
    private static readonly ResxText Resources = ResxText.For<StepDetails>();

    [Fact]
    public void Rule_dividendWithholding_namesTheAccountingYearAfterTheContribution()
    {
        var rule = Resources.Get("Rule_dividend_withholding").ShouldNotBeNull();

        rule.ShouldSatisfyAllConditions(
            () => rule.ShouldContain("from the second accounting year after the one of the capital contribution"),
            () => rule.ShouldNotContain("distribution"));
    }

    [Theory]
    [InlineData("Amount_vvprbis_second_year_rate", "VVPRbis rate, second accounting year after the contribution")]
    [InlineData("Amount_vvprbis_third_year_onwards_rate", "VVPRbis rate, third accounting year after the contribution or later")]
    public void Amount_vvprbisRates_nameTheAccountingYearAfterTheContribution(string key, string label)
    {
        Resources.Get(key).ShouldBe(label);
    }
}
