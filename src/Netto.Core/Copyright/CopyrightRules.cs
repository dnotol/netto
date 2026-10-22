using Netto.Core.Rules;

namespace Netto.Core.Copyright;

// A copyright royalty the company pays its director for the rights on works they created, for one
// income year; income year 2025 is here for the Tax-Calc tests only, and holds no conditions, no card
// reading it. The royalty is movable income (art. 17 §1, 5°), which the company deducts in full like any
// cost (art. 49) and withholds 15% on (art. 269 §1, 4°). It keeps that quality, and is taxed apart at
// 15% (art. 171, 2°bis) outside INASTI's base (royal decree n° 38, art. 11 §2), only up to 30% of the
// total remuneration, services included, and up to the year's cap (art. 37 al. 2, no phase-in left, art.
// 551); what passes either is professional income, which Netto taxes as director pay (art. 32), and the
// whole royalty is when the four previous years' copyright income averaged more than the cap. The
// withholding is credited and refunded beyond the tax (art. 279, art. 304 §2). From 2026 the flat-rate
// costs on copyright income are only for a holder of an attestation du travail des arts (AR/CIR 92, art.
// 3 al. 2), which Netto does not model yet: the royalty is taxed on its gross (NoFlatRateCosts, none for
// 2025, whose flat-rate costs Tax-Calc takes as an input the tests leave at nothing).
public sealed record CopyrightRules(
    int IncomeYear,
    Source Limits,
    Source PhaseIn,
    Source TaxedApart,
    Source Withholding,
    Source Credited,
    Source Deduction,
    Source TaxedAsPay,
    Source OutsideContributions,
    Source OutsideAdvancePaymentSurcharge,
    Source? NoFlatRateCosts,
    RuleAmount RelativeLimit,
    RuleAmount Cap,
    RuleAmount TaxedApartRate,
    RuleAmount WithholdingRate,
    RuleAmount BeyondLimitsWithholdingRate,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article17Income2026 = Fisconet(17, 2026, "30.07.2026", "91154b02-bee7-4b6b-b039-48f3172766a2", "2026-07-30",
        "§1, 5°: income from the transfer or licence by their original holder of copyright and neighbouring rights on original works, performances or, from 01.01.2026, computer programs, "
        + "for their effective exploitation according to the profession's honest practices, with an attestation du travail des arts or, failing one, transferred or licensed for communication to the public, public performance or reproduction");

    private static readonly Source Article37Income2025 = Fisconet(37, 2025, "01.08.2025", "fe5f86ce-b042-46d3-823f-90e47abd6404", "2025-08-01",
        "Al. 2: copyright income keeps its quality of movable income except to the extent that the royalties exceed 30% of the total remuneration, services included, or 75,360, "
        + "and only while the four previous periods' copyright income averaged at most 75,360");

    private static readonly Source Article37Income2026 = Fisconet(37, 2026, "18.02.2026", "1166a251-1e63-4826-948f-fb6b842e05ac", "2026-02-18",
        "Al. 2: copyright income keeps its quality of movable income except to the extent that the royalties exceed 30% of the total remuneration, services included, or 77,220, "
        + "and only while the four previous periods' copyright income averaged at most 77,220");

    private static readonly Source Article551 = Fisconet(551, 2026, "21.01.2026", "c2769973-9bec-45c6-9393-d99e8cce81f8", "2026-01-21",
        "§1: the 30% was 50% for tax year 2024 and 40% for tax year 2025 only");

    private static readonly Source Article171Income2025 = Fisconet(171, 2025, "19.01.2026", "a5b9bf88-53d5-4c36-90ed-664ab49bbcb5", "2026-01-19",
        "2°bis, b): the first tranche of copyright income, up to 30% or 75,360, is taxed apart at 15%");

    private static readonly Source Article171Income2026 = Fisconet(171, 2026, "31.07.2026", "d9683eeb-c029-4bc0-bb2c-b17f7990f2d4", "2026-07-31",
        "2°bis, b): the first tranche of copyright income, up to 30% or 77,220, is taxed apart at 15%, unless taxing it with the other income gives less");

    private static readonly Source Article269Income2025 = new(
        "CIR 92, art. 269 (revenus 2025), coordinated text on Fisconetplus, last modified 01.08.2025",
        "§1, 4°: 15% on the first tranche of copyright income, up to 30% or 75,360; §1, 1°: 30% on movable income §1 sets no other rate for",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/580606b1-7faa-4683-8861-8da64ff59d25"),
        "personal-tax/spf-finances_cir92-art-269-revenus-2025_2025-08-01.html") { Label = "CIR 92, art. 269" };

    private static readonly Source Article269Income2026 = new(
        "CIR 92, art. 269 (revenus 2026), coordinated text on Fisconetplus, last modified 02.06.2026",
        "§1, 4°: 15% on the first tranche of copyright income, up to 30% or 77,220; §1, 1°: 30% on movable income §1 sets no other rate for",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/fb744b8d-7ddb-4067-903a-f84eae927052"),
        "personal-tax/spf-finances_cir92-art-269-revenus-2026_2026-06-02.html") { Label = "CIR 92, art. 269" };

    private static readonly Source Article279 = Fisconet(279, 2026, "12.02.2026", "0eb762e6-faab-405a-8ff9-b76bdf3db2e3", "2026-02-12",
        "The withholding on movable income art. 269 sets is credited against the tax; art. 304 §2 refunds what it leaves over");

    private static readonly Source Article49 = new(
        "CIR 92, art. 49 (revenus 2026), coordinated text on Fisconetplus, last modified 04.02.2026",
        "The costs made or borne in the period to acquire or keep taxable income are deductible professional costs",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/34fb9ab5-38a2-4f93-b73d-0727e67e54d3"),
        "corporate-tax/spf-finances_cir92-art-49-revenus-2026_2026-02-04.html") { Label = "CIR 92, art. 49" };

    private static readonly Source Article57 = new(
        "CIR 92, art. 57 (revenus 2026), coordinated text on Fisconetplus, last modified 05.02.2026",
        "Al. 1, 4°: copyright income of art. 17 §1, 5° is a professional cost only when justified by individual fiches, the 281.45",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c4a55933-7d39-42c3-9403-fd2ed7c860e3"),
        "corporate-tax/spf-finances_cir92-art-57-revenus-2026_2026-02-05.html") { Label = "CIR 92, art. 57" };

    private static readonly Source Article53Income2026 = new(
        "CIR 92, art. 53 (revenus 2026), coordinated text on Fisconetplus, last modified 18.02.2026",
        "10°: costs are not professional costs to the extent that they exceed professional needs unreasonably",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/796aa044-5b54-4569-88a4-abe7be55e077"),
        "corporate-tax/spf-finances_cir92-art-53-revenus-2026_2026-02-18.html") { Label = "CIR 92, art. 53" };

    private static readonly Source Article313Income2026 = Fisconet(313, 2026, "02.06.2026", "7dbc7a05-c597-479b-b18e-6e3b3cb852af", "2026-06-02",
        "Al. 1, 4°: copyright income must be declared even though it bore withholding");

    private static readonly Source Article32Income2025 = Fisconet(32, 2025, "02.12.2024", "adf7c228-083c-4468-b8d1-151c1e03a067", "2024-12-02",
        "A director's pay is what they obtain for their office or their functions in the company, whatever its name");

    private static readonly Source Article32Income2026 = Fisconet(32, 2026, "29.01.2026", "7628aa2c-168a-4bd8-aec4-288108a91757", "2026-01-29",
        "A director's pay is what they obtain for their office or their functions in the company, whatever its name");

    private static readonly Source RoyalDecree38 = new(
        "Arrêté royal n° 38 du 27.07.1967 organisant le statut social des travailleurs indépendants, consolidated on Justel, updated to 01.06.2026",
        "Art. 11 §1 and §2: the contributions are a percentage of the professional income as the income tax law sets it, which copyright income within art. 37's limits is not",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/1967/07/27/1967072702/justel"),
        "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html") { Label = "Royal decree n° 38, art. 11" };

    private static readonly Source Article157Income2025 = Fisconet(157, 2025, "16.01.2025", "cdf69919-b665-491b-9a02-059fd68dd24f", "2025-01-16",
        "The surcharge is on the tax on profits, benefits and directors' pay, not on movable income");

    private static readonly Source Article157Income2026 = Fisconet(157, 2026, "30.07.2026", "c6e47940-53f0-4608-b567-ae774014a621", "2026-07-30",
        "The surcharge is on the tax on directors' pay (art. 30, 2°) alone, as the law of 15.07.2026 leaves it");

    private static readonly Source ArCir3Income2026 = new(
        "AR/CIR 92, art. 3 (revenus 2026), coordinated text on Fisconetplus, last modified 17.09.2026",
        "Al. 2: from 01.01.2026 the flat-rate costs on copyright income apply only to income from activities for which the holder has an attestation du travail des arts when it is paid",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/88dd058b-58eb-40d1-9346-1dfe4dcd5087"),
        "personal-tax/spf-finances_arcir92-art-3-revenus-2026_2026-09-17.html") { Label = "AR/CIR 92, art. 3" };

    public static CopyrightRules IncomeYear2025 { get; } = Of(
        2025, Article37Income2025, Article171Income2025, Article269Income2025, Article32Income2025, Article157Income2025, null, 75_360m, []);

    public static CopyrightRules IncomeYear2026 { get; } = Of(
        2026, Article37Income2026, Article171Income2026, Article269Income2026, Article32Income2026, Article157Income2026, ArCir3Income2026, 77_220m,
        [
            new LeverCondition("works_you_created", [Article17Income2026]),
            new LeverCondition("for_the_public", [Article17Income2026]),
            new LeverCondition("effectively_exploited", [Article17Income2026]),
            new LeverCondition("arm_length", [Article53Income2026]),
            new LeverCondition("fiche_281_45", [Article57]),
            new LeverCondition("declared", [Article313Income2026]),
        ]);

    public static CopyrightRules? For(int incomeYear) => incomeYear switch
    {
        2025 => IncomeYear2025,
        2026 => IncomeYear2026,
        _ => null,
    };

    // The rates are the same both years; the cap and the texts are the year's.
    private static CopyrightRules Of(
        int incomeYear, Source article37, Source article171, Source article269, Source article32, Source article157, Source? noFlatRateCosts, decimal cap,
        IReadOnlyList<LeverCondition> conditions) => new(
        incomeYear,
        article37,
        Article551,
        article171,
        article269,
        Article279,
        Article49,
        article32,
        RoyalDecree38,
        article157,
        noFlatRateCosts,
        new RuleAmount("copyright_relative_limit_rate", 0.30m, article37),
        new RuleAmount("copyright_cap", cap, article37),
        new RuleAmount("copyright_taxed_apart_rate", 0.15m, article171),
        new RuleAmount("copyright_withholding_rate", 0.15m, article269),
        new RuleAmount("copyright_beyond_limits_withholding_rate", 0.30m, article269),
        conditions);

    private static Source Fisconet(int article, int incomeYear, string modified, string guid, string fileDate, string reference) => new(
        $"CIR 92, art. {article} (revenus {incomeYear}), coordinated text on Fisconetplus, last modified {modified}",
        reference,
        new Uri($"https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/{guid}"),
        $"personal-tax/spf-finances_cir92-art-{article}-revenus-{incomeYear}_{fileDate}.html") { Label = $"CIR 92, art. {article}" };
}
