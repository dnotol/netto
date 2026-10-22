using Netto.Core.Rules;

namespace Netto.Core.CorporateTax;

// Corporate tax of a company whose accounting year is the calendar year, for one income year (taxed
// in the following assessment year), and the surcharge its advance payments did not cover. No losses
// carried forward, and no disallowed expenses but what the company cannot deduct of the vouchers
// (VoucherRules) and what it adds back of a company car's benefit (CarRules): the taxable income is
// the year's profit after director pay with those added back. A small company keeps the reduced rate
// only while its directors' flat-rate benefits are worth at most ExcessBenefitsShare of their total
// pay (art. 215 al. 3, 5°).
public sealed record CorporateTaxRules(
    int IncomeYear,
    RuleAmount StandardRate,
    RuleAmount ReducedRate,
    RuleAmount ReducedRateCeiling,
    RuleAmount MinimumDirectorPay,
    RuleAmount ExcessBenefitsShare,
    AdvancePaymentRules AdvancePayments)
{
    private static readonly Source Article215Income2026 = new(
        "CIR 92, art. 215 (revenus 2026), coordinated text on Fisconetplus, last modified 03.08.2026",
        "Al. 1: 25%. Al. 2: 20% on the first 100,000 for a small company. Al. 3, 4°: from its fifth tax period, only if a director's pay reaches the indexed minimum, or the taxable income does not exceed that pay",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7132fb4e-e229-4008-a585-4ad4853edca5"),
        "corporate-tax/spf-finances_cir92-art-215-revenus-2026_2026-08-03.html");

    private static readonly Source Article2Income2026 = new(
        "CIR 92, art. 2 (revenus 2026), coordinated text on Fisconetplus, last modified 30.07.2026",
        "§1, 25° (from tax year 2027): the excessive part of the benefits in kind allotted to directors is the part of the total of the flat-rate benefits (art. 36 §1, al. 2 or §2) worth more than 20% of the total pay allotted to directors in the taxable period, which art. 215 al. 3, 5° takes the reduced rate for",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8a86d077-36f5-4f5f-ba01-49395a2cf922"),
        "corporate-tax/spf-finances_cir92-art-2-revenus-2026_2026-07-30.html");

    private static readonly Source IndexationNotice2027 = new(
        "Indexation notice for assessment year 2027, version 03.08.2026, adapted to the law of 15.07.2026",
        "CIR 92 art. 215, al. 3, 4°: minimum director pay, base 25,000, indexed 51,000",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"),
        "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html");

    private static readonly Source Article218Income2026 = new(
        "CIR 92, art. 218 (revenus 2026), coordinated text on Fisconetplus, last modified 03.08.2026",
        "§1: the tax of art. 215 to 217 is increased as art. 157 to 168 provide when advance payments fall short, the reference rate of art. 161 at least 3%, without the 90% limit, the 106% base or the exception of art. 163. §2: none for a small company on the tax of its first three financial years",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/604a81d6-b1ac-4cb8-84bd-13cf088ca1b0"),
        "corporate-tax/spf-finances_cir92-art-218-revenus-2026_2026-08-03.html");

    private static readonly Source Article159Income2026 = new(
        "CIR 92, art. 159 (revenus 2026), coordinated text on Fisconetplus, last modified 09.02.2026",
        "The surcharge: the tax times 2.25 times the reference rate, less the advance payments times 3, 2.5, 2 and 1.5 times it, by the deadline each was paid by",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/ed182474-e88a-4931-91a3-51286d1dc47a"),
        "corporate-tax/spf-finances_cir92-art-159-revenus-2026_2026-02-09.html");

    // The statutory 10 October and 20 December 2026 fall on a weekend; the page gives the dates they
    // move to.
    private static readonly Source AdvancePaymentsPage2027 = new(
        "SPF Finances, « Versements anticipés » for companies, tax year 2027, as read on 20.10.2026",
        "« Quand ma société doit-elle effectuer ses versements anticipés ? »: by 10 April, 10 July, 12 October and 21 December 2026",
        new Uri("https://finances.belgium.be/fr/entreprises/impot_des_societes/versements_anticipes"),
        "corporate-tax/spf-finances_versements-anticipes-societes_2026-10-20.html");

    public static Source Source => Article215Income2026;

    public static CorporateTaxRules IncomeYear2026 { get; } = new(
        2026,
        new RuleAmount("standard_rate", 0.25m, Article215Income2026),
        new RuleAmount("reduced_rate", 0.20m, Article215Income2026),
        new RuleAmount("reduced_rate_ceiling", 100_000m, Article215Income2026),
        new RuleAmount("minimum_director_pay", 51_000m, IndexationNotice2027),
        new RuleAmount("excess_benefits_rate", 0.20m, Article2Income2026),
        new AdvancePaymentRules(
            Article218Income2026,
            // Art. 161 gives the ECB's marginal lending rate on 1 January 2026 rounded down, below
            // the 3% art. 218 sets for companies.
            new RuleAmount("reference_rate", 0.03m, Article218Income2026),
            new RuleAmount("surcharge_multiplier", 2.25m, Article159Income2026),
            [
                new AdvancePaymentDeadline(new RuleDate("first_deadline", new DateOnly(2026, 4, 10), AdvancePaymentsPage2027),
                    new RuleAmount("first_credit_multiplier", 3m, Article159Income2026)),
                new AdvancePaymentDeadline(new RuleDate("second_deadline", new DateOnly(2026, 7, 10), AdvancePaymentsPage2027),
                    new RuleAmount("second_credit_multiplier", 2.5m, Article159Income2026)),
                new AdvancePaymentDeadline(new RuleDate("third_deadline", new DateOnly(2026, 10, 12), AdvancePaymentsPage2027),
                    new RuleAmount("third_credit_multiplier", 2m, Article159Income2026)),
                new AdvancePaymentDeadline(new RuleDate("fourth_deadline", new DateOnly(2026, 12, 21), AdvancePaymentsPage2027),
                    new RuleAmount("fourth_credit_multiplier", 1.5m, Article159Income2026)),
            ]));

    public static CorporateTaxRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> Amounts => [StandardRate, ReducedRate, ReducedRateCeiling, MinimumDirectorPay, ExcessBenefitsShare];
}
