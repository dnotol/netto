using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River.Copyright;

// What the copyright royalty does from the year's royalty steps, at the top of the director's stream:
// it receives the royalty the split pays beside the pay, and drains what its part within the limits
// costs in tax apart, each line of it into the taxes on pay, what of that part reaches the pocket once
// it is paid, and its part beyond the limits, going on to Director pay, taxed as pay. The card reads the
// royalty as the engine took it (Result): the limits, which one set the part within, what the company
// withheld, and for a royalty that follows the pay, how it was set. Its lines say their share of the
// royalty, and the card what reaches the pocket of it from here (ToYou); the part taxed as pay reaches
// it through Director pay, net of its own taxes there. Without a royalty, nothing.
internal sealed record CopyrightView(
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
            royalty,
            tax,
            afterTax,
            ledger.Count > 0 ? Shares.Of(afterTax, royalty.Royalty) : null,
            [.. drains],
            ledger.Select(StepDetail.From).ToList());
    }
}

// The plan with its royalty against the same plan without it, at the pay the plan with it runs at: what
// reaches the pocket in each, and what goes to taxes on pay and on profit, as the bar of buckets draws
// each plan (BucketsView). Without the royalty the company keeps it, its tax and the dividend's take
// their share, and what is left reaches the pocket as the dividend. Gain is what the royalty leaves in
// the pocket more. Two runs of the year, the second the river with the royalty out of the plan. That
// pay is the pay set unless the company cannot pay so much beside the royalty (YearSimulation.PayCeiling,
// a fixed royalty's as well as one that follows the pay): the river with the royalty holds it there,
// but the river without it would pay all of it, so the second run is given the first's pay, or the gain
// would be the pay's as well as the royalty's. PayHeld says so, the pay both plans run at, only then.
// None until a royalty counts, or while the engine refuses an amount.
internal sealed record CopyrightComparison(
    decimal PocketWith,
    decimal PocketWithout,
    decimal TaxesOnPayWith,
    decimal TaxesOnPayWithout,
    decimal TaxesOnProfitWith,
    decimal TaxesOnProfitWithout)
{
    // The pay both plans run at when the pay set is held below, none otherwise.
    public decimal? PayHeld { get; init; }

    public decimal Gain => Difference(PocketWith, PocketWithout);

    public static CopyrightComparison? From(RiverInputs inputs, RiverRun run) => From(run, LeverGain.Of(inputs, run, OptionalModule.Copyright));

    // The same from the royalty's gain the card already measured.
    public static CopyrightComparison? From(RiverRun run, LeverGain? gain)
    {
        // The river without the royalty is the river's own run, so a car or a rent in the plan is in it
        // as in the other, at the pay the run with the royalty took: the royalty's LeverGain.
        if (run.Year is not { Scenario.Copyright.Any: true } || gain is null
            || BucketsView.From(run) is not { } with || BucketsView.From(gain.Without) is not { } without)
            return null;

        return new CopyrightComparison(
            with.AmountOf("pocket"), without.AmountOf("pocket"), with.AmountOf("taxes_on_pay"), without.AmountOf("taxes_on_pay"),
            with.AmountOf("taxes_on_profit"), without.AmountOf("taxes_on_profit")) { PayHeld = gain.PayHeld };
    }

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
