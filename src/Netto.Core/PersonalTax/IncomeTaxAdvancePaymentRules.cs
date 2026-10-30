using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The surcharge on the income tax on director pay that was neither withheld nor paid in advance
// during the year (CIR 92 art. 157 to 168), and the bonus on what was paid beyond what avoids it
// (art. 175 to 177), for one income year. The base is the tax at 106% less what was withheld and the
// tax credits; the surcharge is the base times the surcharge multiplier of the reference rate, less
// each payment times its deadline's credit multiplier of it, kept at 90%, and none is due under the
// minimum or the minimum share of the base. Source is art. 157 itself, which the surcharge's step
// cites as its rule; BonusSource is art. 175, the bonus step's.
public sealed record IncomeTaxAdvancePaymentRules(
    Source Source,
    Source BonusSource,
    RuleAmount ReferenceRate,
    RuleAmount BaseFactor,
    RuleAmount SurchargeMultiplier,
    RuleAmount KeptRate,
    RuleAmount MinimumSurcharge,
    RuleAmount MinimumShareRate,
    IReadOnlyList<IncomeTaxAdvancePaymentDeadline> Deadlines)
{
    public decimal SurchargeRate => SurchargeMultiplier.Value * ReferenceRate.Value;

    public IReadOnlyList<RuleAmount> SurchargeAmounts =>
        [ReferenceRate, BaseFactor, SurchargeMultiplier, .. Deadlines.Select(deadline => deadline.CreditMultiplier), KeptRate, MinimumSurcharge, MinimumShareRate];

    public IReadOnlyList<RuleAmount> BonusAmounts => [ReferenceRate, BaseFactor, .. Deadlines.Select(deadline => deadline.BonusMultiplier)];

    // Art. 161: the ECB's marginal lending rate on 1 January of the income year, rounded down: 3.40%
    // on 1 January 2025, 2.40% on 1 January 2026.
    private static readonly Source EcbKeyRates = new(
        "European Central Bank, key ECB interest rates, as read on 20.10.2026",
        "Marginal lending facility: 3.40% from 18.12.2024, 2.40% from 11.06.2025; CIR 92 art. 161 takes it on 1 January of the income year, rounded down",
        new Uri("https://www.ecb.europa.eu/stats/policy_and_exchange_rates/key_ecb_interest_rates/html/index.en.html"),
        "personal-tax/ecb_key-interest-rates_2026-10-20.html");

    private static readonly Source Article157Income2025 = SharedSources.Article157Income2025 with
    {
        Reference = "The total tax on profits, benefits and directors' pay not withheld or paid in advance during the year is surcharged; art. 158: on that tax, less the withholding and the tax credits on it",
    };

    private static readonly Source Article157Income2026 = SharedSources.Article157Income2026 with
    {
        Reference = "The total tax on directors' pay (art. 30, 2°) not withheld or paid in advance during the year is surcharged, as the law of 15.07.2026 leaves it; art. 158: on that tax, less the withholding and the tax credits on it",
    };

    private static readonly Source Article159Income2025 = Source.Fisconet("personal-tax", "159", 2025, new DateOnly(2025, 1, 16), "36817772-e503-470b-a6f1-e63fc8f93ffa",
        "The surcharge: the tax times 2.25 times the reference rate, less the advance payments times 3, 2.5, 2 and 1.5 times it, by 10 April, 10 July, 10 October and 20 December");

    private static readonly Source Article159Income2026 = SharedSources.Article159Income2026;

    private static readonly Source Article160Income2025 = Source.Fisconet("personal-tax", "160", 2025, new DateOnly(2025, 1, 16), "2dc99977-5705-4843-a89f-f64845844e9f",
        "The surcharge is kept at 90% of its amount");

    private static readonly Source Article160Income2026 = Source.Fisconet("personal-tax", "160", 2026, new DateOnly(2026, 2, 9), "16683e97-29ed-4e3a-b36d-f1b689f0d8ef",
        "The surcharge is kept at 90% of its amount");

    private static readonly Source Article163Income2025 = Source.Fisconet("personal-tax", "163", 2025, new DateOnly(2025, 1, 27), "5d0de455-ba7b-4a43-ad7c-92832f0381ff",
        "No surcharge when it does not reach 0.5% of the tax it is computed on, or the indexed minimum");

    private static readonly Source Article163Income2026 = Source.Fisconet("personal-tax", "163", 2026, new DateOnly(2026, 2, 18), "ea2a99fb-c40c-4dac-bf5b-eb2994b6c9f6",
        "No surcharge when it does not reach 0.5% of the tax it is computed on, or the indexed minimum");

    private static readonly Source Article165Income2025 = Source.Fisconet("personal-tax", "165", 2025, new DateOnly(2025, 1, 16), "8243ed34-15bb-4ce7-a5ec-bd9f02699845",
        "The total tax is taken at 106%");

    private static readonly Source Article165Income2026 = Source.Fisconet("personal-tax", "165", 2026, new DateOnly(2026, 2, 9), "df4c60b7-8de3-487c-836d-56a0ad1297c0",
        "The total tax is taken at 106%");

    private static readonly Source Article175Income2025 = Source.Fisconet("personal-tax", "175", 2025, new DateOnly(2025, 1, 16), "170d765a-d2dc-4691-b3ea-522e7a1262ef",
        "The bonus, on the total tax at 106% less the withholding, the tax credits and the advance payments needed to avoid the surcharge; art. 176: on the payments beyond those; AR/CIR 92 art. 65: the payments count first towards the needed amount");

    private static readonly Source Article175Income2026 = Source.Fisconet("personal-tax", "175", 2026, new DateOnly(2026, 7, 31), "b64c185b-8b1a-4801-84c0-a2aa55de4c3d",
        "The bonus, on the total tax at 106% less the withholding, the tax credits and the advance payments needed to avoid the surcharge; art. 176: on the payments beyond those; AR/CIR 92 art. 65: the payments count first towards the needed amount");

    private static readonly Source Article177Income2025 = Source.Fisconet("personal-tax", "177", 2025, new DateOnly(2025, 1, 16), "201d665d-68b0-4245-b694-0c94455fad98",
        "The bonus: the payments beyond those needed times 1.5, 1.25, 1 and 0.75 times the reference rate, by 10 April, 10 July, 10 October and 20 December");

    private static readonly Source Article177Income2026 = Source.Fisconet("personal-tax", "177", 2026, new DateOnly(2026, 7, 31), "2b763b79-95cd-4103-8617-1f669e4ed673",
        "The bonus: the payments beyond those needed times 1.5, 1.25, 1 and 0.75 times the reference rate, by the deadline each was paid by");

    private static readonly Source IndexationNotice2026 = SharedSources.IndexationNotice2026 with
    {
        Reference = "CIR 92 art. 163: minimum of the advance-payment surcharge, base 50, indexed 100",
    };

    private static readonly Source IndexationNotice2027 = SharedSources.IndexationNotice2027 with
    {
        Reference = "CIR 92 art. 163: minimum of the advance-payment surcharge, base 50, indexed 100",
    };

    // The statutory 10 October and 20 December 2026 fall on a weekend; the page gives the dates they
    // move to.
    private static readonly Source SelfEmployedPage2027 = new(
        "SPF Finances, « Versements anticipés » for the self-employed and directors, tax year 2027, as read on 20.10.2026",
        "« Quand dois-je effectuer mes versements anticipés ? »: by 10 April, 10 July, 12 October and 21 December 2026",
        new Uri("https://finances.belgium.be/fr/independants_professions_liberales/versements_anticipes"),
        "personal-tax/spf-finances_versements-anticipes-independants_2026-10-20.html");

    // Income year 2025 is here for the Tax-Calc tests, which use no date. Its deadlines are art. 159's
    // statutory ones: 20 December 2025 fell on a Saturday, and no document filed gives the day it moved
    // to.
    public static IncomeTaxAdvancePaymentRules IncomeYear2025 { get; } = new(
        Article157Income2025,
        Article175Income2025,
        new RuleAmount("reference_rate", 0.03m, EcbKeyRates),
        new RuleAmount("base_factor", 1.06m, Article165Income2025),
        new RuleAmount("surcharge_multiplier", 2.25m, Article159Income2025),
        new RuleAmount("kept_rate", 0.90m, Article160Income2025),
        new RuleAmount("minimum_surcharge", 100m, IndexationNotice2026),
        new RuleAmount("minimum_share_rate", 0.005m, Article163Income2025),
        [
            Deadline(new DateOnly(2025, 4, 10), Article159Income2025, Article177Income2025, "first", 3m, 1.5m),
            Deadline(new DateOnly(2025, 7, 10), Article159Income2025, Article177Income2025, "second", 2.5m, 1.25m),
            Deadline(new DateOnly(2025, 10, 10), Article159Income2025, Article177Income2025, "third", 2m, 1m),
            Deadline(new DateOnly(2025, 12, 20), Article159Income2025, Article177Income2025, "fourth", 1.5m, 0.75m),
        ]);

    public static IncomeTaxAdvancePaymentRules IncomeYear2026 { get; } = new(
        Article157Income2026,
        Article175Income2026,
        new RuleAmount("reference_rate", 0.02m, EcbKeyRates),
        new RuleAmount("base_factor", 1.06m, Article165Income2026),
        new RuleAmount("surcharge_multiplier", 2.25m, Article159Income2026),
        new RuleAmount("kept_rate", 0.90m, Article160Income2026),
        new RuleAmount("minimum_surcharge", 100m, IndexationNotice2027),
        new RuleAmount("minimum_share_rate", 0.005m, Article163Income2026),
        [
            Deadline(new DateOnly(2026, 4, 10), SelfEmployedPage2027, Article159Income2026, Article177Income2026, "first", 3m, 1.5m),
            Deadline(new DateOnly(2026, 7, 10), SelfEmployedPage2027, Article159Income2026, Article177Income2026, "second", 2.5m, 1.25m),
            Deadline(new DateOnly(2026, 10, 12), SelfEmployedPage2027, Article159Income2026, Article177Income2026, "third", 2m, 1m),
            Deadline(new DateOnly(2026, 12, 21), SelfEmployedPage2027, Article159Income2026, Article177Income2026, "fourth", 1.5m, 0.75m),
        ]);

    private static IncomeTaxAdvancePaymentDeadline Deadline(
        DateOnly date, Source credits, Source bonuses, string place, decimal creditMultiplier, decimal bonusMultiplier) =>
        Deadline(date, credits, credits, bonuses, place, creditMultiplier, bonusMultiplier);

    private static IncomeTaxAdvancePaymentDeadline Deadline(
        DateOnly date, Source dateSource, Source credits, Source bonuses, string place, decimal creditMultiplier, decimal bonusMultiplier) =>
        new(new RuleDate($"{place}_deadline", date, dateSource),
            new RuleAmount($"{place}_credit_multiplier", creditMultiplier, credits),
            new RuleAmount($"{place}_bonus_multiplier", bonusMultiplier, bonuses));
}

// A payment made by the date earns a credit of its credit multiplier of the reference rate against
// the surcharge, and, beyond what avoids the surcharge, a bonus of its bonus multiplier of it.
public sealed record IncomeTaxAdvancePaymentDeadline(RuleDate Date, RuleAmount CreditMultiplier, RuleAmount BonusMultiplier)
{
    public decimal CreditRate(IncomeTaxAdvancePaymentRules rules) => CreditMultiplier.Value * rules.ReferenceRate.Value;

    public decimal BonusRate(IncomeTaxAdvancePaymentRules rules) => BonusMultiplier.Value * rules.ReferenceRate.Value;
}
