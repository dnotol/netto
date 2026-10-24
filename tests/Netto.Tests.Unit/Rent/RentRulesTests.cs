using Netto.Core.Rent;
using Shouldly;

namespace Netto.Tests.Rent;

// Art. 7, 13 and 32 CIR 92 (revenus 2026 and 2025) and the indexation notices for assessment years
// 2027 and 2026, as they stand: 2026 for the river, 2025 for the Tax-Calc tests.
public class RentRulesTests
{
    // Art. 32, al. 2, 3°: 5/3 of the cadastral income revalued by art. 13's coefficient; art. 7, §1,
    // 2°, c: never less than the cadastral income plus 40%, indexed by art. 518's coefficient; art. 13:
    // 40% of charges, at most 2/3 of the revalued cadastral income.
    public static TheoryData<int, decimal, decimal> CoefficientsByYear => new()
    {
        { 2026, 5.75m, 2.3000m },
        { 2025, 5.63m, 2.2446m },
    };

    [Theory]
    [MemberData(nameof(CoefficientsByYear))]
    public void For_year_holdsItsRevaluationAndIndexationCoefficients(int incomeYear, decimal revaluation, decimal indexation)
    {
        var rules = RentRules.For(incomeYear).ShouldNotBeNull();

        (rules.IncomeYear, rules.RevaluationCoefficient.Value, rules.IndexationCoefficient.Value).ShouldBe((incomeYear, revaluation, indexation));
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void For_year_holdsTheCapTheFloorAndTheCharges(int incomeYear)
    {
        var rules = RentRules.For(incomeYear).ShouldNotBeNull();

        (rules.CapMultiplier.Value, rules.FloorUplift.Value, rules.ChargesRate.Value, rules.ChargesLimitMultiplier.Value)
            .ShouldBe((5m / 3m, 0.40m, 0.40m, 2m / 3m));
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_incomeYearWithoutPublishedAmounts_hasNoRules(int incomeYear)
    {
        RentRules.For(incomeYear).ShouldBeNull();
    }

    // Each amount cites the text of its own income year.
    [Fact]
    public void IncomeYear2026_eachAmountCitesItsArticle()
    {
        var rules = RentRules.IncomeYear2026;

        new[] { rules.CapMultiplier, rules.RevaluationCoefficient, rules.FloorUplift, rules.ChargesRate, rules.ChargesLimitMultiplier }
            .Select(amount => amount.Source)
            .ShouldBe([rules.Cap, rules.NetRent, rules.RentAsIncome, rules.NetRent, rules.NetRent]);
    }

    // The conditions the card lists, each with the texts it comes from: the share let is the user's
    // to justify, since the floor and the cap are on its cadastral income (art. 7 and 32), and the
    // office allowance already covers the room, its rent included (circular 2021/C/20, §31).
    [Fact]
    public void Conditions_2026_eachCitesItsText()
    {
        var rules = RentRules.IncomeYear2026;

        rules.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source)).ShouldBe(new[]
        {
            ("share_yours_to_justify", rules.RentAsIncome),
            ("share_yours_to_justify", rules.Cap),
            ("not_with_office_allowance", rules.TeleworkCircular),
        });
    }

    // The circular is cited by the one paragraph the rent's condition rests on.
    [Theory]
    [InlineData(2026)]
    [InlineData(2025)]
    public void Conditions_citeEachTextByAShortLabel(int incomeYear)
    {
        var rules = RentRules.For(incomeYear).ShouldNotBeNull();

        rules.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source.Label)).ShouldBe(new[]
        {
            ("share_yours_to_justify", "CIR 92, art. 7"),
            ("share_yours_to_justify", "CIR 92, art. 32"),
            ("not_with_office_allowance", "Circular 2021/C/20, §31"),
        });
    }
}
