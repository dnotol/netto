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
// withholding is credited and refunded beyond the tax (art. 279, art. 304 §2). The part within the
// limits is taxed and withheld on less its flat-rate costs (FlatRateCosts), which from 2026 only a
// holder of an attestation du travail des arts takes. EnvelopeRate is the share of the yearly envelope,
// director pay plus royalty, that the SDA's rulings accept as a royalty for all of a director's time
// spent creating, the basis of the royalty Netto proposes as a move: none for 2025, whose rules serve
// the Tax-Calc tests only.
public sealed record CopyrightRules(
    int IncomeYear,
    Source Limits,
    Source PhaseIn,
    Source TaxedApart,
    Source Deduction,
    Source TaxedAsPay,
    Source OutsideContributions,
    Source OutsideAdvancePaymentSurcharge,
    CopyrightFlatRateCosts FlatRateCosts,
    RuleAmount RelativeLimit,
    RuleAmount Cap,
    RuleAmount TaxedApartRate,
    RuleAmount WithholdingRate,
    RuleAmount BeyondLimitsWithholdingRate,
    RuleAmount? EnvelopeRate,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article17Income2026 = Source.Fisconet("personal-tax", 17, 2026, new DateOnly(2026, 7, 30), "91154b02-bee7-4b6b-b039-48f3172766a2",
        "§1, 5°: income from the transfer or licence by their original holder of copyright and neighbouring rights on original works, performances or, from 01.01.2026, computer programs, "
        + "for their effective exploitation according to the profession's honest practices, with an attestation du travail des arts or, failing one, transferred or licensed for communication to the public, public performance or reproduction");

    private static readonly Source Article37Income2025 = Source.Fisconet("personal-tax", 37, 2025, new DateOnly(2025, 8, 1), "fe5f86ce-b042-46d3-823f-90e47abd6404",
        "Al. 2: copyright income keeps its quality of movable income except to the extent that the royalties exceed 30% of the total remuneration, services included, or 75,360, "
        + "and only while the four previous periods' copyright income averaged at most 75,360");

    private static readonly Source Article37Income2026 = Source.Fisconet("personal-tax", 37, 2026, new DateOnly(2026, 2, 18), "1166a251-1e63-4826-948f-fb6b842e05ac",
        "Al. 2: copyright income keeps its quality of movable income except to the extent that the royalties exceed 30% of the total remuneration, services included, or 77,220, "
        + "and only while the four previous periods' copyright income averaged at most 77,220");

    private static readonly Source Article551 = Source.Fisconet("personal-tax", 551, 2026, new DateOnly(2026, 1, 21), "c2769973-9bec-45c6-9393-d99e8cce81f8",
        "§1: the 30% was 50% for tax year 2024 and 40% for tax year 2025 only");

    private static readonly Source Article171Income2025 = Source.Fisconet("personal-tax", 171, 2025, new DateOnly(2026, 1, 19), "a5b9bf88-53d5-4c36-90ed-664ab49bbcb5",
        "2°bis, b): the first tranche of copyright income, up to 30% or 75,360, is taxed apart at 15%");

    private static readonly Source Article171Income2026 = Source.Fisconet("personal-tax", 171, 2026, new DateOnly(2026, 7, 31), "d9683eeb-c029-4bc0-bb2c-b17f7990f2d4",
        "2°bis, b): the first tranche of copyright income, up to 30% or 77,220, is taxed apart at 15%, unless taxing it with the other income gives less");

    private static readonly Source Article269Income2025 = SharedSources.Article269Income2025 with
    {
        Reference = "§1, 4°: 15% on the first tranche of copyright income, up to 30% or 75,360; §1, 1°: 30% on movable income §1 sets no other rate for",
    };

    private static readonly Source Article269Income2026 = SharedSources.Article269Income2026 with
    {
        Reference = "§1, 4°: 15% on the first tranche of copyright income, up to 30% or 77,220; §1, 1°: 30% on movable income §1 sets no other rate for",
    };

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

    private static readonly Source Article53Income2026 = SharedSources.Article53Income2026 with
    {
        Reference = "10°: costs are not professional costs to the extent that they exceed professional needs unreasonably",
    };

    private static readonly Source Article313Income2026 = Source.Fisconet("personal-tax", 313, 2026, new DateOnly(2026, 6, 2), "7dbc7a05-c597-479b-b18e-6e3b3cb852af",
        "Al. 1, 4°: copyright income must be declared even though it bore withholding");

    private static readonly Source Article32Income2025 = SharedSources.Article32Income2025;

    private static readonly Source Article32Income2026 = SharedSources.Article32Income2026;

    private static readonly Source RoyalDecree38 = SharedSources.RoyalDecree38 with
    {
        Reference = "Art. 11 §1 and §2: the contributions are a percentage of the professional income as the income tax law sets it, which copyright income within art. 37's limits is not",
    };

    private static readonly Source Article157Income2025 = SharedSources.Article157Income2025 with
    {
        Reference = "The surcharge is on the tax on profits, benefits and directors' pay, not on movable income",
    };

    private static readonly Source Article157Income2026 = SharedSources.Article157Income2026 with
    {
        Reference = "The surcharge is on the tax on directors' pay (art. 30, 2°) alone, as the law of 15.07.2026 leaves it",
    };

    private static readonly Source Article22 = Source.Fisconet("personal-tax", 22, 2026, new DateOnly(2026, 2, 2), "81a07df1-57c0-46c0-8871-fd08bdee178a",
        "§3: the net income of copyright is its gross less the costs of acquiring or keeping it; without proof of them, the King sets them at a flat rate");

    private static readonly Source ArCir3Income2025 = new(
        "AR/CIR 92, art. 3 (revenus 2025), coordinated text on Fisconetplus, last modified 15.01.2025",
        "Without proof of real costs, the costs of acquiring or keeping copyright income are a flat rate of its gross, which art. 4 raises",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/f569b4c2-4eb8-4dd1-843d-8dc652a424ea"),
        "personal-tax/spf-finances_arcir92-art-3-revenus-2025_2025-01-15.html") { Label = "AR/CIR 92, art. 3" };

    private static readonly Source ArCir3Income2026 = new(
        "AR/CIR 92, art. 3 (revenus 2026), coordinated text on Fisconetplus, last modified 17.09.2026",
        "Al. 2: from 01.01.2026 the flat-rate costs on copyright income apply only to income from activities for which the holder has an attestation du travail des arts when it is paid",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/88dd058b-58eb-40d1-9346-1dfe4dcd5087"),
        "personal-tax/spf-finances_arcir92-art-3-revenus-2026_2026-09-17.html") { Label = "AR/CIR 92, art. 3" };

    private static readonly Source ArCir4Income2025 = new(
        "AR/CIR 92, art. 4 (revenus 2025), coordinated text on Fisconetplus, last modified 12.01.2026",
        "Al. 1, 1°: for copyright income, the flat-rate costs are 50% of the first tranche and 25% of the second, for every author",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/b2ffb531-81a4-4572-99ba-a4409cd451d8"),
        "personal-tax/spf-finances_arcir92-art-4-revenus-2025_2026-01-12.html") { Label = "AR/CIR 92, art. 4" };

    private static readonly Source ArCir4Income2026 = new(
        "AR/CIR 92, art. 4 (revenus 2026), coordinated text on Fisconetplus, last modified 17.09.2026",
        "Al. 1, 1°: for copyright income, the flat-rate costs are 50% of the first tranche and 25% of the second; al. 2: only for income from activities an attestation du travail des arts covers when it is paid, "
        + "not the starter attestation of art. 17 of the royal decree of 13.03.2023",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/800a668f-5588-4f3e-b2bb-a79c1743008c"),
        "personal-tax/spf-finances_arcir92-art-4-revenus-2026_2026-09-17.html") { Label = "AR/CIR 92, art. 4" };

    // The text of art. 4 for income 2026 still reads a second tranche « de 20.100 euros »: the ceilings
    // are the notice's.
    private static readonly Source IndexationNotice2026 = SharedSources.IndexationNotice2026 with
    {
        Reference = "VIII: the ceilings of the flat-rate costs on copyright income (AR/CIR 92, art. 4, al. 1, 1°) are 20,100 for the first tranche and 40,190 for the second",
    };

    private static readonly Source IndexationNotice2027 = SharedSources.IndexationNotice2027 with
    {
        Reference = "VIII: the ceilings of the flat-rate costs on copyright income (AR/CIR 92, art. 4, al. 1, 1°) are 20,590 for the first tranche and 41,180 for the second",
    };

    private static readonly Source ProgrammeLaw = new(
        "Loi-programme du 30.05.2026, Moniteur belge of 01.06.2026, erratum of 19.06.2026, consolidated on Justel",
        "Art. 10 to 12: from income paid or attributed on 01.01.2026, the flat-rate costs on copyright income are only for income from activities an attestation du travail des arts covers when it is paid, "
        + "never the starter attestation of art. 17 of the royal decree of 13.03.2023; for the withholding, from income paid or attributed on 11.06.2026",
        new Uri("https://www.ejustice.just.fgov.be/eli/loi/2026/05/30/2026003986/justel"),
        "personal-tax/ejustice_loi-programme-2026-05-30_2026-06-01.html") { Label = "Programme law of 30.05.2026" };

    private static readonly Source Ruling2026_0484 = new(
        "Décision anticipée n° 2026.0484 du 18.08.2026 of the Service des Décisions Anticipées, on Fisconetplus, last modified 17.09.2026",
        "Points 11 and 31: a director's copyright royalty is at most 25% of the yearly envelope, director pay plus royalty, times the share of their working time spent creating protected works; "
        + "the pay is the gross actually paid or attributed in cash or in kind, without the company car's benefit in kind",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c0b563e3-ccbe-47ff-b26b-e4bd671c0bc7"),
        "personal-tax/spf-finances_decision-anticipee-2026-0484_2026-09-17.html") { Label = "Advance ruling 2026.0484" };

    public static CopyrightRules IncomeYear2025 { get; } = Of(
        incomeYear: 2025,
        article37: Article37Income2025,
        article171: Article171Income2025,
        article269: Article269Income2025,
        article32: Article32Income2025,
        article157: Article157Income2025,
        flatRateCosts: FlatRateCostsOf(
            article3: ArCir3Income2025, article4: ArCir4Income2025, attestation: null, notice: IndexationNotice2026, firstCeiling: 20_100m, secondCeiling: 40_190m),
        cap: 75_360m,
        envelopeRate: null,
        conditions: []);

    public static CopyrightRules IncomeYear2026 { get; } = Of(
        incomeYear: 2026,
        article37: Article37Income2026,
        article171: Article171Income2026,
        article269: Article269Income2026,
        article32: Article32Income2026,
        article157: Article157Income2026,
        flatRateCosts: FlatRateCostsOf(
            article3: ArCir3Income2026, article4: ArCir4Income2026, attestation: ProgrammeLaw, notice: IndexationNotice2027, firstCeiling: 20_590m, secondCeiling: 41_180m),
        cap: 77_220m,
        envelopeRate: new RuleAmount("copyright_envelope_rate", 0.25m, Ruling2026_0484),
        conditions:
        [
            new LeverCondition("works_you_created", [Article17Income2026]),
            new LeverCondition("for_the_public", [Article17Income2026]),
            new LeverCondition("effectively_exploited", [Article17Income2026]),
            new LeverCondition("arm_length", [Article53Income2026]),
            new LeverCondition("fiche_281_45", [Article57]),
            new LeverCondition("declared", [Article313Income2026]),
            new LeverCondition("attestation", [ProgrammeLaw, ArCir3Income2026, ArCir4Income2026]),
        ]);

    public static CopyrightRules? For(int incomeYear) => incomeYear switch
    {
        2025 => IncomeYear2025,
        2026 => IncomeYear2026,
        _ => null,
    };

    // The rates are the same both years; the cap and the texts are the year's.
    private static CopyrightRules Of(
        int incomeYear, Source article37, Source article171, Source article269, Source article32, Source article157, CopyrightFlatRateCosts flatRateCosts,
        decimal cap, RuleAmount? envelopeRate, IReadOnlyList<LeverCondition> conditions) => new(
        IncomeYear: incomeYear,
        Limits: article37,
        PhaseIn: Article551,
        TaxedApart: article171,
        Deduction: Article49,
        TaxedAsPay: article32,
        OutsideContributions: RoyalDecree38,
        OutsideAdvancePaymentSurcharge: article157,
        FlatRateCosts: flatRateCosts,
        RelativeLimit: new RuleAmount("copyright_relative_limit_rate", 0.30m, article37),
        Cap: new RuleAmount("copyright_cap", cap, article37),
        TaxedApartRate: new RuleAmount("copyright_taxed_apart_rate", 0.15m, article171),
        WithholdingRate: new RuleAmount("copyright_withholding_rate", 0.15m, article269),
        BeyondLimitsWithholdingRate: new RuleAmount("copyright_beyond_limits_withholding_rate", 0.30m, article269),
        EnvelopeRate: envelopeRate,
        Conditions: conditions);

    // The rates are art. 4's both years, the ceilings the year's indexation notice's.
    private static CopyrightFlatRateCosts FlatRateCostsOf(
        Source article3, Source article4, Source? attestation, Source notice, decimal firstCeiling, decimal secondCeiling) => new(
        NetIncome: Article22,
        Who: article3,
        Bands: article4,
        Attestation: attestation,
        FirstBandRate: new RuleAmount("copyright_flat_rate_costs_first_band_rate", 0.50m, article4),
        FirstBandCeiling: new RuleAmount("copyright_flat_rate_costs_first_band", firstCeiling, notice),
        SecondBandRate: new RuleAmount("copyright_flat_rate_costs_second_band_rate", 0.25m, article4),
        SecondBandCeiling: new RuleAmount("copyright_flat_rate_costs_second_band", secondCeiling, notice));
}
