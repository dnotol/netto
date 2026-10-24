using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// Each amount cites the administration's coordinated article of its own income year on Fisconetplus,
// as filed in official-docs/personal-tax/, never a text written for another year.
public class PersonalTaxRulesTests
{
    private const string Fisconetplus = "https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/";

    // Article 130 sets the scale, 131 the tax-free amount, 134 the 25% its tax is computed at, and 51
    // the director's flat-rate costs.
    public static TheoryData<int, string, string> ArticleOfEachAmount => new()
    {
        { 2025, "bracket_1_rate", "68216320-026d-4a35-a81d-1221bfe21b53" },
        { 2025, "bracket_3_upper_limit", "68216320-026d-4a35-a81d-1221bfe21b53" },
        { 2025, "bracket_4_rate", "68216320-026d-4a35-a81d-1221bfe21b53" },
        { 2025, "tax_free_amount", "d888718e-4593-44a2-bb28-7e26d17baa94" },
        { 2025, "tax_free_amount_rate", "09b708c4-3d54-468c-a652-9053bd16e2eb" },
        { 2025, "flat_rate_costs_rate", "1c87b91e-3a27-418d-9743-bfa0fecdc5e2" },
        { 2025, "flat_rate_costs_maximum", "1c87b91e-3a27-418d-9743-bfa0fecdc5e2" },
        { 2026, "bracket_1_rate", "aff72054-004a-4858-8747-1d70964c8e3d" },
        { 2026, "bracket_3_upper_limit", "aff72054-004a-4858-8747-1d70964c8e3d" },
        { 2026, "bracket_4_rate", "aff72054-004a-4858-8747-1d70964c8e3d" },
        { 2026, "tax_free_amount", "2fb89ed0-b93e-47d7-816c-85f1ac679b8a" },
        { 2026, "tax_free_amount_rate", "56f7c2a3-94ab-45dd-85d0-2a0c7f6ddebc" },
        { 2026, "flat_rate_costs_rate", "21dd9b61-2989-4125-a2e0-6142c607eec8" },
        { 2026, "flat_rate_costs_maximum", "21dd9b61-2989-4125-a2e0-6142c607eec8" },
    };

    [Theory]
    [MemberData(nameof(ArticleOfEachAmount))]
    public void For_eachAmount_citesTheArticleOfItsIncomeYear(int incomeYear, string key, string document)
    {
        var rules = PersonalTaxRules.For(incomeYear, Region.Wallonia).ShouldNotBeNull();

        var amount = rules.IncomeTaxAmounts.Concat(rules.FlatRateCostsAmounts).Single(amount => amount.Key == key);

        amount.Source.Url.ShouldBe(new Uri(Fisconetplus + document));
    }

    // The rules the steps cite are each year's own: art. 130 for the scale, art. 51 for the deductions
    // from the pay, and art. 304 for the balance on the assessment, whose §2, al. 1 holds for 2025 too.
    public static TheoryData<int, string, string> SourceOfEachStepRule => new()
    {
        { 2025, nameof(PersonalTaxRules.IncomeTaxScale), "68216320-026d-4a35-a81d-1221bfe21b53" },
        { 2025, nameof(PersonalTaxRules.DirectorPayDeductions), "1c87b91e-3a27-418d-9743-bfa0fecdc5e2" },
        { 2025, nameof(PersonalTaxRules.AssessmentBalance), "bda433e6-7da6-4dc9-882c-0c9d92591877" },
        { 2026, nameof(PersonalTaxRules.IncomeTaxScale), "aff72054-004a-4858-8747-1d70964c8e3d" },
        { 2026, nameof(PersonalTaxRules.DirectorPayDeductions), "21dd9b61-2989-4125-a2e0-6142c607eec8" },
        { 2026, nameof(PersonalTaxRules.AssessmentBalance), "bda433e6-7da6-4dc9-882c-0c9d92591877" },
    };

    [Theory]
    [MemberData(nameof(SourceOfEachStepRule))]
    public void For_eachStepRule_citesTheArticleOfItsIncomeYear(int incomeYear, string rule, string document)
    {
        var rules = PersonalTaxRules.For(incomeYear, Region.Wallonia).ShouldNotBeNull();

        var source = rule switch
        {
            nameof(PersonalTaxRules.IncomeTaxScale) => rules.IncomeTaxScale,
            nameof(PersonalTaxRules.DirectorPayDeductions) => rules.DirectorPayDeductions,
            _ => rules.AssessmentBalance,
        };

        source.Url.ShouldBe(new Uri(Fisconetplus + document));
    }

    // The regional additional tax is the region's own: Wallonia's is the decree's 33.257%, on each
    // year's rules for a resident of Wallonia.
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void For_wallonia_isTheRulesOfAResidentOfWallonia(int incomeYear)
    {
        var rules = PersonalTaxRules.For(incomeYear, Region.Wallonia).ShouldNotBeNull();

        (rules.IncomeYear, rules.Region, rules.RegionalAdditionalRate.Value).ShouldBe((incomeYear, Region.Wallonia, 0.33257m));
    }

    // Netto's director lives in Wallonia: another region has no rules yet, and none is guessed for it.
    public static TheoryData<int, Region> RegionsWithoutRules => new()
    {
        { 2025, Region.Flanders },
        { 2025, Region.Brussels },
        { 2026, Region.Flanders },
        { 2026, Region.Brussels },
    };

    [Theory]
    [MemberData(nameof(RegionsWithoutRules))]
    public void For_regionWithoutRules_isNone(int incomeYear, Region region)
    {
        PersonalTaxRules.For(incomeYear, region).ShouldBeNull();
    }

    [Fact]
    public void For_incomeYearWithoutRules_isNone()
    {
        PersonalTaxRules.For(Fixtures.UnsupportedIncomeYear, Region.Wallonia).ShouldBeNull();
    }
}
