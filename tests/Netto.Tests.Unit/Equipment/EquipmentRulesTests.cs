using Netto.Core.Equipment;
using Netto.Core.Rules;
using Netto.Tests.Rules;
using Shouldly;

namespace Netto.Tests.Equipment;

// Art. 18 §3, 10 and §4 AR/CIR 92 as the administration coordinates it for income year 2026, and the circulars
// that read it (2017/C/82, 2018/C/63) and that exclude the telework allowances beside it (2021/C/20).
public class EquipmentRulesTests
{
    private const string Article18 = "personal-tax/spf-finances_arcir92-art-18-revenus-2026_2026-08-10.html";
    private const string Circular2017C82 = "personal-tax/spf-finances_circulaire-2017-c-82-pc-tablette-internet-telephone_2019-10-07.html";
    private const string Circular2018C63 = "personal-tax/spf-finances_circulaire-2018-c-63-pc-tablette-internet-telephone_2018-05-24.html";
    private const string Circular2021C20 = "personal-tax/spf-finances_circulaire-2021-c-20-teletravail_2026-09-09.html";

    // §3, 10: 72 a PC, 36 a tablet or a mobile phone, 60 the internet connection, 48 a phone subscription, a year,
    // each held to the article by its slot.
    [Fact]
    public void IncomeYear2026_holdsTheFlatValuesOfArticle18()
    {
        RuleSets.Slots<RuleAmount>(EquipmentRules.IncomeYear2026).Select(slot => (slot.Slot, slot.Value!.Key, slot.Value.Value, slot.Value.Source.Document)).ShouldBe(new[]
        {
            ("PcBenefit", "pc_benefit", 72m, Article18),
            ("TabletBenefit", "tablet_benefit", 36m, Article18),
            ("MobilePhoneBenefit", "mobile_phone_benefit", 36m, Article18),
            ("InternetBenefit", "internet_benefit", 60m, Article18),
            ("PhoneSubscriptionBenefit", "phone_subscription_benefit", 48m, Article18),
        });
    }

    // Each rule's text and the passage of it the rule rests on.
    [Fact]
    public void IncomeYear2026_citesEachRulesTextAndPassage()
    {
        RuleSets.Slots<Source>(EquipmentRules.IncomeYear2026).Select(slot => (slot.Slot, slot.Value!.Document, slot.Value.Passage)).ShouldBe(new[]
        {
            ("Benefit", Article18, "equipment_benefits"),
            ("Contribution", Article18, "contribution"),
            ("PerAppliance", Circular2017C82, "per_appliance"),
            ("ContributionPerBenefit", Circular2018C63, "contribution_per_benefit"),
            ("PartOfYear", Circular2018C63, "part_of_the_year"),
            ("SubscriptionPerNumber", Circular2018C63, "subscription_per_number"),
            ("BenefitAsPay", "personal-tax/spf-finances_cir92-art-31-revenus-2026_2026-07-30.html", "benefits_in_kind"),
            ("BenefitInContributions", "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html", "contributions_base"),
            ("Deduction", "corporate-tax/spf-finances_cir92-art-49-revenus-2026_2026-02-04.html", "deductible_costs"),
            ("AllowanceBeside", Circular2021C20, "allowance_beside_equipment"),
        });
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_incomeYearOtherThan2026_hasNoRules(int incomeYear)
    {
        EquipmentRules.For(incomeYear).ShouldBeNull();
    }

    // The conditions the user confirms, each with its texts: only what the company owns or holds the contract of
    // is valued so (2017/C/82); private use is allowed (§3, 10); a subscription the company provides is not also
    // reimbursed on invoices; no piece's share of the contribution exceeds its own value (§4, 2018/C/63 §23).
    [Fact]
    public void Conditions_2026_eachCitesItsTexts()
    {
        EquipmentRules.IncomeYear2026.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source.Document)).ShouldBe(new[]
        {
            ("company_owns_and_contracts", Circular2017C82),
            ("private_use_allowed", Article18),
            ("subscription_not_on_invoices", Circular2017C82),
            ("subscription_not_on_invoices", Circular2021C20),
            ("contribution_within_each_piece", Article18),
            ("contribution_within_each_piece", Circular2018C63),
        });
    }
}
