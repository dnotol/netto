using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River;

// What each module card draws from the one run of the river. They only select, order and regroup
// the engine's figures and steps; no card calculates on its own.

// The pay slider over the profit before pay, and the company's profit it leaves. While another
// card's amount is refused there is no company profit to show, but the slider keeps its place. The
// exact amount field shows the pay the split gives, or what was typed while that is refused.
internal sealed record SplitView(decimal Ceiling, decimal Pay, decimal FieldPay, decimal? CompanyProfit)
{
    public static SplitView From(RiverInputs inputs, RiverRun run)
    {
        var ceiling = inputs.PayCeiling ?? 0m;
        return new SplitView(
            ceiling,
            run.Pay?.GrossPay ?? Math.Clamp(inputs.DirectorPay.Value, 0m, ceiling),
            run.Pay?.GrossPay ?? inputs.DirectorPay.Value,
            run.Year?.Company.TaxableIncome);
    }
}

// The pay is derived from the split, or the user's own while the company is skipped. Its detail
// view is the pay slip, drawn from the same result, and its steps' sources.
internal sealed record DirectorPayView(InputOrigin PayOrigin, IReadOnlyList<Drain> Drains, IReadOnlyList<StepDetail> Details)
{
    public static DirectorPayView From(RiverInputs inputs, GrossToNetResult pay) => new(
        inputs.Company is null ? inputs.DirectorPay.Origin : InputOrigin.Derived,
        [
            .. pay.Steps.Where(step => step.Track == Track.PaidOut).Select(step => Drain.PaidOutTo(step, "taxes_on_pay")),
            new Drain("net_pay", DrainKind.PassedOn, pay.NetInPocket, "pocket"),
        ],
        pay.Steps.Select(StepDetail.From).ToList());
}

// The condition is the engine's: which part of art. 215 decided the rate, against which minimum pay.
internal sealed record CorporateTaxView(
    decimal TaxableIncome,
    ReducedRateCondition Condition,
    decimal MinimumDirectorPay,
    bool NoTaxableProfit,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public static CorporateTaxView From(YearResult year)
    {
        var steps = year.Steps.Where(step => step.Event == TaxableEvent.CompanyProfit).ToList();

        return new CorporateTaxView(
            year.Company.TaxableIncome,
            year.Company.Condition,
            year.Company.MinimumDirectorPay,
            year.Company.TaxableIncome <= 0m,
            [
                .. steps.Where(step => step.Track == Track.PaidOut).Select(step => Drain.PaidOutTo(step, "taxes_on_profit")),
                new Drain("profit_after_tax", DrainKind.PassedOn, year.ProfitAfterTax, "dividend"),
            ],
            steps.Select(StepDetail.From).ToList());
    }
}

// The slider runs over the profit after tax, the dividend on one side and what is kept on the other;
// Kept is where it sits, never above the profit after tax.
internal sealed record DividendView(decimal Maximum, decimal Kept, decimal Dividend, decimal Rate, IReadOnlyList<Drain> Drains, IReadOnlyList<StepDetail> Details)
{
    public static DividendView From(YearResult year)
    {
        var steps = year.Steps.Where(step => step.Event == TaxableEvent.Dividend).ToList();

        return new DividendView(
            year.MaximumDividend,
            year.MaximumDividend - year.Dividend.Dividend,
            year.Dividend.Dividend,
            year.Dividend.Rate,
            [
                .. steps.Where(step => step.Track == Track.PaidOut).Select(step => Drain.PaidOutTo(step, "taxes_on_profit")),
                new Drain("net_dividend", DrainKind.PassedOn, year.Dividend.NetDividend, "pocket"),
                new Drain("kept_in_company", DrainKind.PassedOn, year.KeptInCompany, "kept_in_company"),
            ],
            steps.Select(StepDetail.From).ToList());
    }
}

// The module whose card shows an engine refusal: the one holding the refused field. The pay is
// the split's while the company is there, Director pay's own once it is skipped.
internal static class RiverErrors
{
    public static RiverModule Owner(string error, bool companySkipped) => error switch
    {
        YearSimulation.RevenueNegative or YearSimulation.RunningCostsNegative => RiverModule.CompanyYear,
        GrossToNet.GrossPayNegative => companySkipped ? RiverModule.DirectorPay : RiverModule.Split,
        GrossToNet.MunicipalRateOutOfRange => RiverModule.DirectorPay,
        YearSimulation.ReserveNegative => RiverModule.Dividend,
        _ => RiverModule.River,
    };
}

internal enum RiverModule
{
    // Nothing a card holds: the income year.
    River,

    CompanyYear,

    Split,

    DirectorPay,

    Dividend,
}

// One line of what a module passes on: the step or amount, and where it goes, a bucket or the next
// module. Amounts are positive; the kind says which way they go, except what is kept in a loss year.
// Public because Drains, a component, takes it as a parameter.
public sealed record Drain(string Key, DrainKind Kind, decimal Amount, string To)
{
    // A step paid out with a negative amount is money coming back: the refund on exempt dividends.
    // The engine negates the refund, so a refund of nothing is a negative zero and still comes back.
    internal static Drain PaidOutTo(Step step, string bucket) => decimal.IsNegative(step.Amount)
        ? new Drain(step.RuleKey, DrainKind.Back, -step.Amount, bucket)
        : new Drain(step.RuleKey, DrainKind.PaidOut, step.Amount, bucket);
}

public enum DrainKind
{
    // Leaves for good, into a bucket of taxes.
    PaidOut,

    // Comes back out of a bucket of taxes: the withholding refunded on the exempt dividend.
    Back,

    // Passed on whole: to the pocket, to what is kept in the company, or to the next module.
    PassedOn,
}
