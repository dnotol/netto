using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Tests.Rules;
using Shouldly;

namespace Netto.Tests.SocialContributions;

// The contributions of 2026 as INASTI's leaflet gives them, and the social insurance fund's management
// fee: a rate of each fund's own, which the minister sets every year after hearing the fund and the fund
// states on its notice (AR n° 38, art. 20 §4), so the user gives it; the director deducts it with the
// contributions as their own (CIR 92, art. 52, 7°, read with Com.IR 52/124: every contribution the law
// makes them pay to their fund), Netto's reading where no text names the fee.
public class SocialContributionRulesTests
{
    [Fact]
    public void FundFeeRate_rateGiven_isTheUsersRateCitingTheFeesLegalBase()
    {
        var rules = SocialContributionRules.IncomeYear2026;

        var rate = rules.FundFeeRate(0.0305m);

        rate.ShouldBe(new RuleAmount("fund_fee_rate", 0.0305m, rules.FundFee));
    }

    // Each slot holds the text named for it. The record of art. 52 is the one the forager read (text from
    // 01.01.2021), standing in until the coordinated article for income year 2026 is filed.
    [Fact]
    public void IncomeYear2026_eachTextSlotHoldsItsDocument()
    {
        RuleSets.Slots<Source>(SocialContributionRules.IncomeYear2026).Select(slot => (slot.Slot, slot.Value?.Document)).ShouldBe(
        [
            ("Source", "social-contributions/inasti_cotisations-sociales-2026_2026-01-05.pdf"),
            ("FundFee", "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html"),
            ("FundFeeDeduction", "personal-tax/spf-finances_cir92-art-52-revenus-2026_2025-11-17.html"),
            ("FundFeeCommentary", "personal-tax/spf-finances_commentaire-cir92-art-52_2020-11-09.html"),
        ]);
    }

    // A file's citations share one name, which one of them carries bare: the fee's, whose reference says
    // its article, while the car's and the royalty's keep the article 11 they rest on.
    [Fact]
    public void RoyalDecree38_isCitedByItsNameForTheFeeAndByArticle11ForTheCarAndTheRoyalty()
    {
        (SocialContributionRules.IncomeYear2026.FundFee.Label, Netto.Core.CompanyCar.CarRules.IncomeYear2026.BenefitInContributions.Label,
                Netto.Core.Copyright.CopyrightRules.IncomeYear2026.OutsideContributions.Label)
            .ShouldBe(("Royal decree n° 38", "Royal decree n° 38, art. 11", "Royal decree n° 38, art. 11"));
    }
}
