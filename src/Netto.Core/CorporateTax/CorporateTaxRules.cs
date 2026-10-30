using Netto.Core.Rules;

namespace Netto.Core.CorporateTax;

// Corporate tax of a company whose accounting year is the calendar year, for one income year (taxed
// in the following assessment year), and the surcharge its advance payments did not cover. No losses
// carried forward, and no disallowed expenses but what the company cannot deduct of the vouchers
// (VoucherRules) and what it adds back of a company car's benefit (CarRules): the taxable income is
// the year's profit after director pay with those added back. A small company keeps the reduced rate
// only while its directors' flat-rate benefits are worth at most ExcessBenefitsShare of their total
// pay (art. 215 al. 3, 5°). The pay art. 215 tests is the gross with what the company bears for the
// director and the benefits in kind (PayTested), and the total pay the benefits are held to counts
// them too (ExcessBenefitsMemorandum). Source is art. 215 itself, which the tax's step cites as its
// rule; AssessmentBalance the text the balance on the company's assessment rests on.
public sealed record CorporateTaxRules(
    int IncomeYear,
    RuleAmount StandardRate,
    RuleAmount ReducedRate,
    RuleAmount ReducedRateCeiling,
    RuleAmount MinimumDirectorPay,
    RuleAmount ExcessBenefitsShare,
    Source PayTested,
    Source ExcessBenefitsMemorandum,
    AdvancePaymentRules AdvancePayments,
    Source Source,
    Source AssessmentBalance)
{
    private static readonly Source Article215Income2026 = Source.Fisconet("corporate-tax", "215", 2026, new DateOnly(2026, 8, 3), "7132fb4e-e229-4008-a585-4ad4853edca5", "rates_and_conditions");

    private static readonly Source Article2Income2026 = Source.Fisconet("corporate-tax", "2", 2026, new DateOnly(2026, 7, 30), "8a86d077-36f5-4f5f-ba01-49395a2cf922", "excess_benefits");

    private static readonly Source Article215Commentary = new(
        "pay_tested",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7a4d742d-e4f4-42c2-ae58-e20ab2fc9be0"),
        "corporate-tax/spf-finances_commentaire-cir92-art-215_2020-11-30.html");

    private static readonly Source Doc561243 = new(
        "excess_benefits",
        new Uri("https://www.lachambre.be/FLWB/PDF/56/1243/56K1243001.pdf"),
        "corporate-tax/lachambre_doc-56-1243-001-projet-loi-reforme-ipp_2025-12-17.pdf");

    private static readonly Source IndexationNotice2027 = SharedSources.IndexationNotice2027 with { Passage = "minimum_director_pay" };

    private static readonly Source Article218Income2026 = Source.Fisconet("corporate-tax", "218", 2026, new DateOnly(2026, 8, 3), "604a81d6-b1ac-4cb8-84bd-13cf088ca1b0", "advance_payment_surcharge");

    private static readonly Source Article159Income2026 = SharedSources.Article159Income2026;

    // The statutory 10 October and 20 December 2026 fall on a weekend; the page gives the dates they
    // move to.
    private static readonly Source AdvancePaymentsPage2027 = new(
        "deadlines",
        new Uri("https://finances.belgium.be/fr/entreprises/impot_des_societes/versements_anticipes"),
        "corporate-tax/spf-finances_versements-anticipes-societes_2026-10-20.html");

    // The balance on the company's assessment: its tax less what it paid in advance, the excess
    // refunded. The article is common to every income tax; its second paragraph speaks of companies.
    private static readonly Source Article304Income2026 = SharedSources.Article304Income2026 with { Passage = "company_balance" };

    public static CorporateTaxRules IncomeYear2026 { get; } = new(
        2026,
        new RuleAmount("standard_rate", 0.25m, Article215Income2026),
        new RuleAmount("reduced_rate", 0.20m, Article215Income2026),
        new RuleAmount("reduced_rate_ceiling", 100_000m, Article215Income2026),
        new RuleAmount("minimum_director_pay", 51_000m, IndexationNotice2027),
        new RuleAmount("excess_benefits_rate", 0.20m, Article2Income2026),
        Article215Commentary,
        Doc561243,
        new AdvancePaymentRules(
            Article218Income2026,
            // Art. 161 gives the ECB's marginal lending rate on 1 January 2026 rounded down, below
            // the 3% art. 218 sets for companies.
            new RuleAmount("reference_rate", 0.03m, Article218Income2026),
            new RuleAmount("surcharge_multiplier", 2.25m, Article159Income2026),
            [
                Deadline(new DateOnly(2026, 4, 10), AdvancePaymentsPage2027, Article159Income2026, "first", 3m),
                Deadline(new DateOnly(2026, 7, 10), AdvancePaymentsPage2027, Article159Income2026, "second", 2.5m),
                Deadline(new DateOnly(2026, 10, 12), AdvancePaymentsPage2027, Article159Income2026, "third", 2m),
                Deadline(new DateOnly(2026, 12, 21), AdvancePaymentsPage2027, Article159Income2026, "fourth", 1.5m),
            ]),
        Source: Article215Income2026,
        AssessmentBalance: Article304Income2026);

    public static CorporateTaxRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    // A deadline named by its place in the year, its date and its multiplier keyed alike, as the
    // director's are (IncomeTaxAdvancePaymentRules).
    private static AdvancePaymentDeadline Deadline(DateOnly date, Source dateSource, Source credits, string place, decimal creditMultiplier) =>
        new(new RuleDate($"{place}_deadline", date, dateSource), new RuleAmount($"{place}_credit_multiplier", creditMultiplier, credits));

    public IReadOnlyList<RuleAmount> Amounts => [StandardRate, ReducedRate, ReducedRateCeiling, MinimumDirectorPay, ExcessBenefitsShare];
}
