using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Household card's figures, line by line (HouseholdFigures), on the river's example (RiverInputsTests),
// 50,000 of pay, with the partner in the plan: keys of the card's texts with the engine's amounts, the
// joint assessment's two columns, as the engine gives them (JointAssessmentTests); a line of nothing left
// out. The marital quotient goes to the partner who earns less: to a partner with no professional income or
// a small one, from a partner who earns far more.
public class HouseholdFiguresTests
{
    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household).WithPartner(true);

    // A partner with no professional income: 30% of the director's goes to them, within the cap, which is
    // all their taxable income; what the quotient saves last.
    [Fact]
    public void Of_partnerWithNoIncome_isYourIncomeTheQuotientToThemEachTaxableIncomeAndWhatItSaves()
    {
        var view = View(WithPartner);

        Lines(HouseholdFigures.Of(view)).ShouldBe(
        [
            ("Figure_YourIncome", view.Director.ProfessionalIncome),
            ("Figure_QuotientToPartner", -view.Partner.Quotient),
            ("Figure_YourIncomeOnTheScale", view.Director.ProfessionalIncome - view.Partner.Quotient),
            ("Figure_PartnerIncomeOnTheScale", view.Partner.Quotient),
            ("Figure_QuotientSaves", view.QuotientSaves),
        ]);
    }

    // Each partner's income on the scale is not the pay's net professional income: it is that income with the
    // quotient moved, and says so in its bubble.
    [Fact]
    public void Of_incomeOnTheScale_isNotYourNetProfessionalIncomeAndExplainsItself()
    {
        var lines = HouseholdFigures.Of(View(WithPartner)).ToDictionary(line => line.Key);

        (lines["Figure_YourIncomeOnTheScale"].Amount, lines["Figure_YourIncomeOnTheScale"].Help, lines["Figure_PartnerIncomeOnTheScale"].Help).ShouldBe(
            (lines["Figure_YourIncome"].Amount - 11_780m, "Help_IncomeOnTheScale", "Help_IncomeOnTheScale"));
    }

    // 30% of the example's net professional income, past 40,000, is past the cap of 11,780; with both 66 or
    // over, within the higher one of 12,790.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Of_partnerWithNoIncome_attributesThirtyPercentOfYoursWithinTheCapAndSavesTax(bool bothAged66)
    {
        var view = View(WithPartner.WithBothAged66(bothAged66));

        (view.Partner.Quotient, view.QuotientSaves > 0m)
            .ShouldBe((Math.Min(Netto.Core.Money.RoundToCent(0.30m * view.Director.ProfessionalIncome), view.Cap.Value), true));
    }

    // A partner with a small income of their own is topped up to 30% of the two together.
    [Fact]
    public void Of_partnerWithASmallIncome_saysTheirIncomeBeforeTheirTaxableIncome()
    {
        var view = View(WithPartner.WithPartnerIncome(5_000m));

        Lines(HouseholdFigures.Of(view)).Select(line => line.Key).ShouldBe(
            ["Figure_YourIncome", "Figure_QuotientToPartner", "Figure_YourIncomeOnTheScale", "Figure_PartnerIncome", "Figure_PartnerIncomeOnTheScale", "Figure_QuotientSaves"]);
    }

    // A partner earning far more attributes part of theirs to a director on a small pay: the line adds to yours.
    [Fact]
    public void Of_partnerEarningFarMore_isTheQuotientFromThemAddedToYourIncome()
    {
        var view = View(WithPartner.WithDirectorPay(12_000m).WithPartnerIncome(60_000m));

        HouseholdFigures.Quotient(view).ShouldNotBeNull().ShouldSatisfyAllConditions(
            line => line.Key.ShouldBe(HouseholdFigures.QuotientFromPartner),
            line => line.Amount.ShouldBe(view.Director.Quotient),
            line => line.Amount.ShouldBeGreaterThan(0m));
    }

    // A partner with 30% or more of the two incomes together gets nothing: no quotient line, and nothing saved.
    [Fact]
    public void Of_partnerEarningAThirdOfTheTwo_hasNoQuotientLine()
    {
        var view = View(WithPartner.WithPartnerIncome(40_000m));

        view.ShouldSatisfyAllConditions(
            () => HouseholdFigures.Quotient(view).ShouldBeNull(),
            () => Lines(HouseholdFigures.Of(view)).Select(line => line.Key)
                .ShouldBe(["Figure_YourIncome", "Figure_YourIncomeOnTheScale", "Figure_PartnerIncome", "Figure_PartnerIncomeOnTheScale"]));
    }

    // The quotient's bubble says its rate and the cap of the year, the higher one once both are 66 or over.
    [Theory]
    [InlineData(false, "€11,780.00")]
    [InlineData(true, "€12,790.00")]
    public void Quotient_saysItsRateAndTheCapThatApplies(bool bothAged66, string cap)
    {
        var line = HouseholdFigures.Quotient(View(WithPartner.WithBothAged66(bothAged66))).ShouldNotBeNull();

        line.ShouldSatisfyAllConditions(
            () => line.Help.ShouldBe("Help_Quotient"),
            () => line.HelpArgs.ShouldBe(["30%", cap]));
    }

    // The quotient can lower the credit for children more than it lowers the tax: one child, no pay, a partner
    // earning 14,000, and it costs the household 8.50. The line says what it costs, never « saves » a
    // negative amount.
    [Fact]
    public void Of_quotientCostingTheHousehold_saysWhatItCosts()
    {
        var view = View(WithPartner) with { QuotientSaves = -8.50m };

        Lines(HouseholdFigures.Of(view))[^1].ShouldBe(("Figure_QuotientCosts", 8.50m));
    }

    // Altogether, what declaring the household saves the river's tax, or what it costs.
    [Theory]
    [InlineData(1_234.56, "Figure_HouseholdSaves", 1_234.56)]
    [InlineData(-8.50, "Figure_HouseholdCosts", 8.50)]
    public void Altogether_saysWhatTheHouseholdSavesOrCosts(decimal saves, string key, decimal amount)
    {
        var line = HouseholdFigures.Altogether(saves);

        (line.Key, line.Amount, line.Help).ShouldBe((key, amount, "Help_Altogether"));
    }

    // Taxed separately, the director is taxed alone: the card has no joint year to draw.
    [Fact]
    public void From_yearTaxedSeparately_isNone()
    {
        var river = WithPartner.WithTaxedSeparately(true);

        HouseholdView.From(river.Run().Pay.ShouldNotBeNull(), bothAged66: false).ShouldBeNull();
    }

    internal static HouseholdView View(RiverInputs river) =>
        HouseholdView.From(river.Run().Pay.ShouldNotBeNull(), river.Household.BothAged66.Value).ShouldNotBeNull();

    private static IReadOnlyList<(string Key, decimal Amount)> Lines(IEnumerable<FigureLine> lines) => [.. lines.Select(line => (line.Key, line.Amount))];
}
