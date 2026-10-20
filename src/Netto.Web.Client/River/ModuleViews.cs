using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River;

// What each module card draws from the one run of the river. They only select, order and regroup
// the engine's figures and steps; no card calculates on its own.

// What the vouchers bring to the pocket, one drain for each kind once the company grants any, what the
// company deducts of them and what it cannot, and the profit before pay they leave for the split, from
// the year's voucher steps. The shares, the yearly maximums and the conditions are the year's rules,
// which the card reads beside it, so they stay while an amount is refused.
internal sealed record VoucherView(decimal Total, decimal Deductible, decimal Disallowed, IReadOnlyList<Drain> Drains, IReadOnlyList<StepDetail> Details)
{
    public static VoucherView From(YearResult year)
    {
        var steps = year.Steps.Where(step => step.Event == TaxableEvent.Vouchers).ToList();

        return new VoucherView(
            year.Vouchers.Total,
            year.Vouchers.Deductible,
            year.Vouchers.Disallowed,
            [
                .. steps.Where(step => step.Track == Track.InPocket).Select(step => new Drain(step.RuleKey, DrainKind.PassedOn, step.Amount, "pocket")),
                new Drain("profit_before_pay", DrainKind.PassedOn, year.ProfitBeforePay, "split"),
            ],
            steps.Select(StepDetail.From).ToList());
    }
}

// The pay slider over the profit before pay, and the company's profit it leaves before tax. While
// another card's amount is refused there is no company profit to show, but the slider keeps its
// place. The exact amount field shows the pay the split gives, or what was typed while that is
// refused.
internal sealed record SplitView(decimal Ceiling, decimal Pay, decimal FieldPay, decimal? CompanyProfit)
{
    public static SplitView From(RiverInputs inputs, RiverRun run)
    {
        var ceiling = inputs.PayCeiling ?? 0m;
        return new SplitView(
            ceiling,
            run.Pay?.GrossPay ?? Math.Clamp(inputs.DirectorPay.Value, 0m, ceiling),
            run.Pay?.GrossPay ?? inputs.DirectorPay.Value,
            run.Year?.ProfitAfterPay);
    }
}

// What the split draws from the engine's search: the net in pocket at every pay, the best pay and
// what it gains over the pay set, how it stands against the reduced corporate rate, and, when no pay
// keeps the reserve, the most the company can keep. Nothing while an amount is refused, the company
// is skipped or there is no profit to split.
internal sealed record SplitSearchView(PayCurveView Curve, BestPayView? Best, RateWeighing? Rate, decimal MostKept)
{
    public static SplitSearchView? From(RiverInputs inputs, RiverRun run, SplitSearchResult? search)
    {
        if (search is null || run.Error is not null || run.Year is not { } year || search.Curve[^1].Pay <= 0m)
            return null;

        var best = search.Best is { } level
            ? new BestPayView(level, level.NetInPocket - year.NetInPocket, year.MaximumDividend >= inputs.Reserve.Value)
            : null;

        return new SplitSearchView(
            PayCurveView.From(search, year.DirectorPay.GrossPay),
            best,
            best is null ? null : RateWeighing.From(search, best.Level),
            search.Curve.Max(level => level.KeptInCompany));
    }
}

// The curve in the plot's own space: X runs from no pay (0) to the profit before pay (1), Y from the
// lowest round amount under the net in pocket (0) to the highest above it (1). The component maps
// that space to its drawing. One band per pay of the curve takes the pointer, from halfway to the
// pay before it to halfway to the next.
internal sealed record PayCurveView(
    IReadOnlyList<CurveRun> Runs,
    IReadOnlyList<CurveBand> Bands,
    IReadOnlyList<AxisTick> PayTicks,
    IReadOnlyList<AxisTick> NetTicks,
    decimal PayAt,
    ChartPoint? BestAt,
    decimal? ThresholdAt,
    bool SomeMissReserve)
{
    // Few enough ticks to fit beside the plot on a phone.
    private const int MaximumIntervals = 4;
    private static readonly decimal[] RoundSteps = [1m, 2m, 2.5m, 5m];

    public static PayCurveView From(SplitSearchResult search, decimal pay)
    {
        var levels = search.Curve;
        var top = levels[^1].Pay;
        var lowest = levels.Min(level => level.NetInPocket);
        var highest = levels.Max(level => level.NetInPocket);
        var netStep = RoundStep(highest - lowest);
        var bottom = Math.Floor(lowest / netStep) * netStep;
        var ceiling = Math.Max(Math.Ceiling(highest / netStep) * netStep, bottom + netStep);

        ChartPoint Point(PayLevel level) => new(level.Pay / top, (level.NetInPocket - bottom) / (ceiling - bottom));

        var points = levels.Select(Point).ToList();
        var payStep = RoundStep(top);

        return new PayCurveView(
            RunsOf(levels, points),
            [.. levels.Select((level, i) => new CurveBand(
                i == 0 ? 0m : (points[i - 1].X + points[i].X) / 2m,
                i == levels.Count - 1 ? 1m : (points[i].X + points[i + 1].X) / 2m,
                level))],
            [.. Ticks(0m, top, payStep).Select(amount => new AxisTick(amount / top, amount))],
            [.. Ticks(bottom, ceiling, netStep).Select(amount => new AxisTick((amount - bottom) / (ceiling - bottom), amount))],
            pay / top,
            search.Best is { } best ? Point(best) : null,
            search.SmallestPayKeepingReducedRate / top,
            levels.Any(level => !level.KeepsReserve));
    }

    // A segment is drawn as keeping the reserve only when the pays at both its ends keep it, so the
    // runs share the pay where they meet.
    private static List<CurveRun> RunsOf(IReadOnlyList<PayLevel> levels, List<ChartPoint> points)
    {
        var runs = new List<(bool KeepsReserve, List<ChartPoint> Points)>();
        for (var i = 1; i < levels.Count; i++)
        {
            var keeps = levels[i - 1].KeepsReserve && levels[i].KeepsReserve;
            if (runs.Count == 0 || runs[^1].KeepsReserve != keeps)
                runs.Add((keeps, [points[i - 1]]));

            runs[^1].Points.Add(points[i]);
        }

        return [.. runs.Select(run => new CurveRun(run.KeepsReserve, run.Points))];
    }

    private static IEnumerable<decimal> Ticks(decimal from, decimal to, decimal step)
    {
        for (var amount = from; amount <= to; amount += step)
            yield return amount;
    }

    private static decimal RoundStep(decimal span)
    {
        for (var scale = 1m; ; scale *= 10m)
        {
            foreach (var round in RoundSteps)
            {
                if (round * scale * MaximumIntervals >= span)
                    return round * scale;
            }
        }
    }
}

// A point of the plot, each coordinate a fraction of its axis.
internal sealed record ChartPoint(decimal X, decimal Y);

internal sealed record CurveRun(bool KeepsReserve, IReadOnlyList<ChartPoint> Points);

internal sealed record CurveBand(decimal From, decimal To, PayLevel Level);

internal sealed record AxisTick(decimal At, decimal Amount);

// The search refines the best pay to the euro, so a pay set within a euro of it is the best: no move
// is worth offering, and the pay set may even leave a few cents more. A pay set whose profit after tax
// cannot keep the reserve is never the best, however much it leaves in the pocket: it gets there by
// eating into the reserve, and the gain over it can be negative.
internal sealed record BestPayView(PayLevel Level, decimal Gain, bool YoursKeepsReserve)
{
    public bool IsYours => YoursKeepsReserve && Gain < 1m;
}

// How the best pay stands against the reduced corporate rate: whether it keeps the rate or gives it
// up, from which pay the rate is kept, and the best pay on the other side with what the best gains
// over it. Not weighed when the rate does not depend on the pay.
internal sealed record RateWeighing(RateChoice Choice, decimal? SmallestPay, PayLevel? Other, decimal? Difference)
{
    public static RateWeighing From(SplitSearchResult search, PayLevel best)
    {
        if (search.SmallestPayKeepingReducedRate is not { } smallest)
            return new RateWeighing(RateChoice.NotWeighed, null, null, null);

        var choice = best.Condition.AppliesReducedRate() ? RateChoice.KeepsIt : RateChoice.GivesItUp;
        var other = choice == RateChoice.KeepsIt ? search.BestLosingReducedRate : search.BestKeepingReducedRate;
        return new RateWeighing(choice, smallest, other, best.NetInPocket - other?.NetInPocket);
    }
}

internal enum RateChoice
{
    // The rate does not depend on the pay: not a small company, or in its first four tax periods.
    NotWeighed,

    KeepsIt,

    GivesItUp,
}

// The pay is derived from the split, or the user's own while the company is skipped. Its detail
// view is the pay slip, drawn from the same result, and its steps' sources. When the company pays
// the contributions on the director's behalf they are pay too, so the card shows what the pay costs
// the company, the gross plus them, and the contributions drain says the company pays them. The
// engine says what the withholding and the advance payments leave on its tax, the bonus they earn,
// and what more by the last deadline leaves no surcharge due.
internal sealed record DirectorPayView(
    InputOrigin PayOrigin,
    bool CompanyPaysContributions,
    decimal CompanyCost,
    TaxSurchargeState Surcharge,
    decimal SurchargeAmount,
    decimal TopUp,
    DateOnly LastDeadline,
    decimal Bonus,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public static DirectorPayView From(RiverInputs inputs, GrossToNetResult pay)
    {
        var payments = pay.AdvancePayments;
        var companyPays = inputs.ContributionsPaidBy.Value == ContributionsPaidBy.Company;

        return new DirectorPayView(
            inputs.Company is null ? inputs.DirectorPay.Origin : InputOrigin.Derived,
            companyPays,
            pay.CompanyCost,
            payments switch
            {
                { Exempt: true } => TaxSurchargeState.Exempt,
                { TaxAt106Percent: 0m } => TaxSurchargeState.NoTax,
                { BelowMinimum: true } => TaxSurchargeState.BelowMinimum,
                { Surcharge: 0m } => TaxSurchargeState.Covered,
                _ => TaxSurchargeState.Due,
            },
            payments.Surcharge,
            payments.LastDeadlineTopUp,
            TaxAdvancePaymentFields.Deadlines(pay.IncomeYear)[^1].Date.Value,
            payments.Bonus,
            [
                .. pay.Steps.Where(step => step.Track == Track.PaidOut).Select(step => Drain.PaidOutTo(step, "taxes_on_pay")).Select(drain => Payer(drain, companyPays)),
                new Drain("net_pay", DrainKind.PassedOn, pay.NetInPocket, "pocket"),
            ],
            pay.Steps.Select(StepDetail.From).ToList());
    }

    // The contributions drain is the same step of the engine whoever pays; its label says when the
    // company does.
    private static Drain Payer(Drain drain, bool companyPays) =>
        companyPays && drain.Key == "social_contributions" ? drain with { Key = "social_contributions_paid_by_company" } : drain;
}

// The card's field for what was withheld on the pay: the amount the user gave, or, while it is left
// to the pay slip, the year's withholding the engine derives from it, twelve times the month's. While
// another input is refused there is no pay slip to derive from, and the field shows nothing.
internal sealed record WithheldFieldView(decimal Amount, InputOrigin Origin)
{
    public bool IsYours => Origin == InputOrigin.Yours;

    public static WithheldFieldView From(RiverInputs inputs, RiverRun run) =>
        new(inputs.TaxPrepayments.Withheld.Value ?? run.Pay?.Assessment.Withheld ?? 0m, inputs.TaxPrepayments.Withheld.Origin);
}

// What the withholding and the advance payments leave on the income tax on the pay.
internal enum TaxSurchargeState
{
    // In the first three years of a first self-employed activity, none is due.
    Exempt,

    // No income tax, nothing to surcharge.
    NoTax,

    // What was withheld and paid in advance covers it.
    Covered,

    // Some is left, but under the minimum, so none is due.
    BelowMinimum,

    // Some is due, which a payment by the last deadline can bring under the minimum.
    Due,
}

// One field per deadline of the income year for the director's own advance payments, with its date,
// drawn from the inputs, so the fields stay while the engine refuses one of them.
internal static class TaxAdvancePaymentFields
{
    public static IReadOnlyList<PaymentField> From(RiverInputs inputs) =>
        [.. Deadlines(inputs.IncomeYear).Select((deadline, i) => new PaymentField(i, deadline.Date.Value, inputs.TaxPrepayments.Paid.ByDeadline[i]))];

    public static IReadOnlyList<IncomeTaxAdvancePaymentDeadline> Deadlines(int incomeYear) =>
        PersonalTaxRules.For(incomeYear)?.AdvancePayments.Deadlines ?? [];
}

// The condition is the engine's: which part of art. 215 decided the rate, against which minimum pay.
// So is the surcharge the advance payments leave, and what more by the last deadline cancels it. What
// comes in is the profit the split leaves the company; its taxable income adds back what it cannot
// deduct of the vouchers. The card lists the corporate layer's steps, that one among them.
internal sealed record CorporateTaxView(
    decimal ProfitAfterPay,
    decimal DisallowedExpenses,
    decimal TaxableIncome,
    ReducedRateCondition Condition,
    decimal MinimumDirectorPay,
    bool NoTaxableProfit,
    SurchargeState Surcharge,
    decimal SurchargeAmount,
    decimal TopUp,
    DateOnly LastDeadline,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public static CorporateTaxView From(YearResult year)
    {
        var steps = year.Steps.Where(step => step.Layer == Layer.Corporate).ToList();
        var payments = year.AdvancePayments;

        return new CorporateTaxView(
            year.ProfitAfterPay,
            year.Vouchers.Disallowed,
            year.Company.TaxableIncome,
            year.Company.Condition,
            year.Company.MinimumDirectorPay,
            year.Company.TaxableIncome <= 0m,
            payments switch
            {
                { Exempt: true } => SurchargeState.Exempt,
                { SurchargeBeforeCredits: 0m } => SurchargeState.NoTax,
                { Surcharge: 0m } => SurchargeState.Covered,
                _ => SurchargeState.Due,
            },
            payments.Surcharge,
            payments.LastDeadlineTopUp,
            AdvancePaymentFields.Deadlines(year.Scenario.IncomeYear)[^1].Date.Value,
            [
                .. steps.Where(step => step.Track == Track.PaidOut).Select(step => Drain.PaidOutTo(step, "taxes_on_profit")),
                new Drain("profit_after_tax", DrainKind.PassedOn, year.ProfitAfterTax, "dividend"),
            ],
            steps.Select(StepDetail.From).ToList());
    }
}

// What the advance payments leave on the corporate tax.
internal enum SurchargeState
{
    // A small company in its first three financial years owes none.
    Exempt,

    // No corporate tax, nothing to surcharge.
    NoTax,

    // The payments' credits cover it.
    Covered,

    // Some is left, which a payment by the last deadline can cancel.
    Due,
}

// One field per deadline of the income year, with its date, holding what the user paid or plans by
// it. Drawn from the inputs, so the fields stay while the engine refuses one of them.
internal static class AdvancePaymentFields
{
    public static IReadOnlyList<PaymentField> From(RiverInputs inputs) =>
        [.. Deadlines(inputs.IncomeYear).Select((deadline, i) => new PaymentField(i, deadline.Date.Value, inputs.AdvancePayments.ByDeadline[i]))];

    public static IReadOnlyList<AdvancePaymentDeadline> Deadlines(int incomeYear) =>
        CorporateTaxRules.For(incomeYear)?.AdvancePayments.Deadlines ?? [];
}

internal sealed record PaymentField(int Deadline, DateOnly Date, Input<decimal> Paid);

// The slider runs over what the company can pay out, this year's profit after tax and the profits
// kept from earlier years, the dividend on one side and what is kept on the other; Kept is where it
// sits, never above what can be paid out.
internal sealed record DividendView(
    decimal Maximum,
    decimal ProfitAfterTax,
    decimal EarlierProfits,
    decimal Kept,
    decimal Dividend,
    decimal Rate,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public static DividendView From(YearResult year)
    {
        var steps = year.Steps.Where(step => step.Event == TaxableEvent.Dividend).ToList();

        return new DividendView(
            year.MaximumDividend,
            year.ProfitAfterTax,
            year.Scenario.EarlierProfits,
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
        VoucherCalculator.MealVoucherDaysNegative or VoucherCalculator.MealVoucherDaysAboveYear or VoucherCalculator.EcoVouchersNegative
            or VoucherCalculator.EcoVouchersAboveMaximum or VoucherCalculator.SportCultureVouchersNegative
            or VoucherCalculator.SportCultureVouchersAboveMaximum => RiverModule.Vouchers,
        GrossToNet.GrossPayNegative => companySkipped ? RiverModule.DirectorPay : RiverModule.Split,
        GrossToNet.MunicipalRateOutOfRange => RiverModule.DirectorPay,
        YearSimulation.ReserveNegative or YearSimulation.EarlierProfitsNegative => RiverModule.Dividend,
        YearSimulation.AdvancePaymentNegative => RiverModule.CorporateTax,
        GrossToNet.TaxWithheldNegative or GrossToNet.TaxAdvancePaymentNegative => RiverModule.DirectorPay,
        _ => RiverModule.River,
    };
}

internal enum RiverModule
{
    // Nothing a card holds: the income year.
    River,

    CompanyYear,

    Vouchers,

    Split,

    DirectorPay,

    CorporateTax,

    Dividend,
}

// One line of what a module passes on: the step or amount, and where it goes, a bucket or the next
// module. Amounts are positive; the kind says which way they go, except what is kept in a loss year.
// Public because Drains, a component, takes it as a parameter.
public sealed record Drain(string Key, DrainKind Kind, decimal Amount, string To)
{
    // A step paid out with a negative amount is money coming back: the refund on exempt dividends, the
    // tax credit for low earned income.
    // The engine negates both, so a refund or a credit of nothing is a negative zero and still comes back.
    internal static Drain PaidOutTo(Step step, string bucket) => decimal.IsNegative(step.Amount)
        ? new Drain(step.RuleKey, DrainKind.Back, -step.Amount, bucket)
        : new Drain(step.RuleKey, DrainKind.PaidOut, step.Amount, bucket);
}

public enum DrainKind
{
    // Leaves for good, into a bucket of taxes.
    PaidOut,

    // Comes back out of a bucket of taxes: the withholding refunded on the exempt dividend, the tax
    // credit for low earned income.
    Back,

    // Passed on whole: to the pocket, to what is kept in the company, or to the next module.
    PassedOn,
}
