using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River.Copyright;

// What the copyright royalty does from the year's royalty steps, the last lever before the split:
// what its part within the limits costs in tax apart, each line of it into the taxes on pay, what of
// that part reaches the pocket once it is paid, its part beyond the limits going on to Director pay,
// taxed as pay, and the profit before pay it leaves for the split; what it receives is what the levers
// above leave of what the company earns. The card reads the royalty as the engine took it (Result):
// the limits, which one set the part within, and what the company withheld. Its lines say their share
// of the royalty, and the card what reaches the pocket of it from here (ToYou); the part taxed as pay
// reaches it through Director pay, net of its own taxes there. Without a royalty, nothing but the
// profit passed on.
internal sealed record CopyrightView(
    decimal Received,
    CopyrightResult Result,
    decimal Tax,
    decimal AfterTax,
    decimal? ToYou,
    IReadOnlyList<Drain> Drains,
    IReadOnlyList<StepDetail> Details)
{
    public bool HasRoyalty => Details.Count > 0;

    public static CopyrightView From(YearResult year)
    {
        var ledger = year.Steps.Where(step => step.Event == TaxableEvent.CopyrightRoyalty).ToList();
        var royalty = year.Copyright;
        var paidOut = ledger.Where(step => step.Track == Track.PaidOut).ToList();
        var tax = paidOut.Sum(step => step.Amount);
        var afterTax = ledger.Count > 0 ? royalty.WithinLimits - tax : 0m;
        IEnumerable<Drain> drains = ledger.Count > 0
            ?
            [
                .. paidOut.Select(step => Drain.PaidOutTo(step, "taxes_on_pay").Of(royalty.Royalty)),
                new Drain("copyright_after_tax", DrainKind.PassedOn, afterTax, "pocket").Of(royalty.Royalty),
                new Drain("copyright_taxed_as_pay", DrainKind.PassedOn, royalty.TaxedAsPay, "director_pay")
                {
                    Details = [StepDetail.From(ledger.Single(step => step.RuleKey == "copyright_taxed_as_pay"))],
                }.Of(royalty.Royalty),
            ]
            : [];

        return new CopyrightView(
            year.ProfitBeforePay + royalty.Royalty,
            royalty,
            tax,
            afterTax,
            ledger.Count > 0 ? Shares.Of(afterTax, royalty.Royalty) : null,
            [.. drains, new Drain("profit_before_pay", DrainKind.PassedOn, year.ProfitBeforePay, "split")],
            ledger.Select(StepDetail.From).ToList());
    }
}

// The plan with its royalty against the same plan without it, at the pay set: what reaches the pocket
// in each, and what goes to taxes on pay and on profit. Without the royalty the company keeps it, its
// tax and the dividend's take their share, and what is left reaches the pocket as the dividend. Gain is
// what the royalty leaves in the pocket more. Two runs of the year, the second the river with the
// royalty out of the plan; none until a royalty counts, or while the engine refuses an amount.
internal sealed record CopyrightComparison(
    decimal PocketWith,
    decimal PocketWithout,
    decimal TaxesOnPayWith,
    decimal TaxesOnPayWithout,
    decimal TaxesOnProfitWith,
    decimal TaxesOnProfitWithout)
{
    public decimal Gain => Difference(PocketWith, PocketWithout);

    public static CopyrightComparison? From(RiverInputs inputs, RiverRun run)
    {
        if (run.Error is not null || run.Year is not { Scenario.Copyright.Any: true } with)
            return null;
        if (inputs.TakeOutOfPlan(OptionalModule.Copyright).Run().Year is not { } without)
            return null;

        return new CopyrightComparison(
            with.NetInPocket, without.NetInPocket, TaxesOnPay(with), TaxesOnPay(without), TaxesOnProfit(with), TaxesOnProfit(without));
    }

    // As the bar of buckets sums them (BucketsView).
    private static decimal TaxesOnPay(YearResult year) =>
        PaidOut(year, TaxableEvent.DirectorPay) + PaidOut(year, TaxableEvent.Rent) + PaidOut(year, TaxableEvent.CopyrightRoyalty);

    private static decimal TaxesOnProfit(YearResult year) => PaidOut(year, TaxableEvent.CompanyProfit) + PaidOut(year, TaxableEvent.Dividend);

    private static decimal PaidOut(YearResult year, TaxableEvent taxableEvent) =>
        year.Steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
