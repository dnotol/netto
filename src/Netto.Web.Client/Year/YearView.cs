using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.Year;

// What the overview draws from one year result: where the profit before pay goes, layer by layer;
// one ledger per party (the company, the director's pay, the dividend), each from its starting
// amount to what it ends with; and every step with its sources. It only selects, orders and scales
// the engine's figures and steps.
internal sealed record YearView(
    IReadOnlyList<SplitPart> Split,
    IReadOnlyList<Ledger> Ledgers,
    ReducedRateCondition Condition,
    IReadOnlyList<StepDetail> Details)
{
    public static YearView From(YearResult result)
    {
        var parts = new (string Key, decimal Amount)[]
        {
            ("corporate_tax", result.CorporateTax),
            ("social_contributions", result.SocialContributions),
            ("personal_tax", result.PersonalTax),
            ("net_in_pocket", result.NetInPocket),
            ("kept_in_company", result.KeptInCompany),
        };
        var drawn = result.ProfitBeforePay > 0m && parts.All(part => part.Amount >= 0m);

        return new YearView(
            parts.Select(part => new SplitPart(part.Key, part.Amount, drawn ? part.Amount / result.ProfitBeforePay : null)).ToList(),
            [
                CompanyLedger(result),
                PartyLedger("director_pay", result.Steps, TaxableEvent.DirectorPay,
                    new LedgerRow("gross_pay", LedgerRowKind.Start, result.DirectorPay.GrossPay),
                    new LedgerRow("net_pay", LedgerRowKind.End, result.DirectorPay.NetInPocket)),
                PartyLedger("dividend", result.Steps, TaxableEvent.Dividend,
                    new LedgerRow("gross_dividend", LedgerRowKind.Start, result.Dividend.Dividend),
                    new LedgerRow("net_dividend", LedgerRowKind.End, result.Dividend.NetDividend)),
            ],
            result.Company.Condition,
            result.Steps.Select(StepDetail.From).ToList());
    }

    private static Ledger CompanyLedger(YearResult result) => new("company",
    [
        new LedgerRow("profit_before_pay", LedgerRowKind.Start, result.ProfitBeforePay),
        new LedgerRow("director_pay", LedgerRowKind.Out, result.DirectorPay.GrossPay),
        new LedgerRow("company_taxable_income", LedgerRowKind.Subtotal, result.Company.TaxableIncome),
        .. result.Steps.Where(step => step.Event == TaxableEvent.CompanyProfit && step.Track == Track.PaidOut).Select(PaidOut),
        new LedgerRow("profit_after_tax", LedgerRowKind.Subtotal, result.ProfitAfterTax),
        new LedgerRow("dividend", LedgerRowKind.Out, result.Dividend.Dividend),
        new LedgerRow("kept_in_company", LedgerRowKind.End, result.KeptInCompany),
    ]);

    // From the gross amount through the steps of that money paid out, or back, to what is left.
    private static Ledger PartyLedger(string key, IReadOnlyList<Step> steps, TaxableEvent taxableEvent, LedgerRow start, LedgerRow end) =>
        new(key, [start, .. steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Select(PaidOut), end]);

    private static LedgerRow PaidOut(Step step) => step.Amount < 0m
        ? new LedgerRow(step.RuleKey, LedgerRowKind.Back, -step.Amount)
        : new LedgerRow(step.RuleKey, LedgerRowKind.PaidOut, step.Amount);
}

// Share is a fraction of the profit before pay, or null when shares would not mean anything: a
// year without profit, or one where a part is negative.
internal sealed record SplitPart(string Key, decimal Amount, decimal? Share);

internal sealed record Ledger(string Key, IReadOnlyList<LedgerRow> Rows);

// Amounts are positive; the kind says which way they go.
internal sealed record LedgerRow(string Key, LedgerRowKind Kind, decimal Amount);

internal enum LedgerRowKind
{
    // What the ledger starts from: the profit before pay, the gross pay, the gross dividend.
    Start,

    // Passed on to the director: the pay and the dividend, out of the company's ledger.
    Out,

    // Leaves for good: a tax or a contribution.
    PaidOut,

    // Comes back: the withholding refunded on exempt dividends.
    Back,

    // A balance on the way: the company's taxable income and its profit after tax.
    Subtotal,

    // What the ledger ends with: what the company keeps, the net pay, the net dividend.
    End,
}
