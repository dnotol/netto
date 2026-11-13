using Netto.Core.Rules;

namespace Netto.Core.Equipment;

// The IT and phone a company provides its director for private use, for one income year. The director is
// taxed on each piece's flat yearly benefit in kind as pay (art. 18 §3, 10 AR/CIR 92): a PC, fixed or
// portable, a tablet or a mobile phone, the internet connection, fixed or mobile, once whatever the number
// of connections or of devices using it, and a phone subscription, one for each phone number, so for each
// SIM with its own (circulars 2017/C/82 and 2018/C/63 §27–29), less what the director pays for that use
// (§4), which comes off its own benefit only (2018/C/63 §23). The values are for a whole year: provided for
// part of it, they are reduced by its calendar days of provision, the first counted and the last not
// (2018/C/63 §25–26). The benefit taxed is pay (BenefitAsPay), so it is in the income INASTI's
// contributions are owed on (BenefitInContributions). The company deducts what the set costs it as any
// professional cost (art. 49), with no add-back: art. 198 §1, 9° and 9°bis name vehicles only. Beside a PC
// or a tablet it provides, the company pays no flat computer allowance, and beside an internet connection
// no flat internet allowance (circular 2021/C/20, §48 and §52: AllowanceBeside). Only what the company owns
// or holds the contract of is a benefit valued so, which Netto cannot see: the user confirms it.
public sealed record EquipmentRules(
    int IncomeYear,
    Source Benefit,
    Source Contribution,
    Source PerAppliance,
    Source ContributionPerBenefit,
    Source PartOfYear,
    Source SubscriptionPerNumber,
    Source BenefitAsPay,
    Source BenefitInContributions,
    Source Deduction,
    Source AllowanceBeside,
    RuleAmount PcBenefit,
    RuleAmount TabletBenefit,
    RuleAmount MobilePhoneBenefit,
    RuleAmount InternetBenefit,
    RuleAmount PhoneSubscriptionBenefit,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article18ArIncome2026 = SharedSources.Article18ArIncome2026 with { Passage = "equipment_benefits" };

    private static readonly Source Article18ArContribution = SharedSources.Article18ArIncome2026 with { Passage = "contribution" };

    private static readonly Source Circular2017C82 = new(
        "per_appliance",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/149b779e-83e1-4dc9-93e0-b11bf0b5f0c5"),
        "personal-tax/spf-finances_circulaire-2017-c-82-pc-tablette-internet-telephone_2019-10-07.html");

    private static readonly Source Circular2018C63 = new(
        "part_of_the_year",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/e1ef2189-1baf-4820-89bc-f842a5cbce11"),
        "personal-tax/spf-finances_circulaire-2018-c-63-pc-tablette-internet-telephone_2018-05-24.html");

    private static readonly Source Circular2021C20 = SharedSources.Circular2021C20 with { Passage = "allowance_beside_equipment" };

    public static EquipmentRules IncomeYear2026 { get; } = new(
        IncomeYear: 2026,
        Benefit: Article18ArIncome2026,
        Contribution: Article18ArContribution,
        PerAppliance: Circular2017C82,
        ContributionPerBenefit: Circular2018C63 with { Passage = "contribution_per_benefit" },
        PartOfYear: Circular2018C63,
        SubscriptionPerNumber: Circular2018C63 with { Passage = "subscription_per_number" },
        BenefitAsPay: SharedSources.Article31Income2026,
        BenefitInContributions: SharedSources.RoyalDecree38,
        Deduction: SharedSources.Article49Income2026,
        AllowanceBeside: Circular2021C20,
        PcBenefit: new RuleAmount("pc_benefit", 72m, Article18ArIncome2026),
        TabletBenefit: new RuleAmount("tablet_benefit", 36m, Article18ArIncome2026),
        MobilePhoneBenefit: new RuleAmount("mobile_phone_benefit", 36m, Article18ArIncome2026),
        InternetBenefit: new RuleAmount("internet_benefit", 60m, Article18ArIncome2026),
        PhoneSubscriptionBenefit: new RuleAmount("phone_subscription_benefit", 48m, Article18ArIncome2026),
        Conditions:
        [
            new LeverCondition("company_owns_and_contracts", [Circular2017C82]),
            new LeverCondition("private_use_allowed", [Article18ArIncome2026]),
            new LeverCondition("subscription_not_on_invoices", [Circular2017C82, SharedSources.Circular2021C20 with { Passage = "expense_allowances" }]),
            new LeverCondition("contribution_within_each_piece", [Article18ArContribution, Circular2018C63 with { Passage = "contribution_per_benefit" }]),
        ]);

    public static EquipmentRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };
}
