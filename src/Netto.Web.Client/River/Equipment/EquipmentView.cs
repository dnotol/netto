using Netto.Core.Equipment;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River.Equipment;

// What the IT and phone cost the company, paid into its expenses beside its running costs, and the director's
// contribution coming back from their pocket, from the year's equipment steps. What it leaves of what the
// company earns, passed on down the river, is the map's company line, no line of the card. The card reads the
// set as the engine took it (Result): each piece's flat value for the days provided, the benefit less the
// contribution, taxed as pay. Without a set, no line.
internal sealed record EquipmentView(EquipmentResult Result, IReadOnlyList<Drain> Drains, IReadOnlyList<StepDetail> Details)
{
    public bool HasSet => Details.Count > 0;

    // What the set takes off what the company earns: its costs, less what the director pays for it.
    public static decimal FromProfit(EquipmentResult set) => set.Costs - set.Contribution;

    public static EquipmentView From(YearResult year)
    {
        var ledger = year.Steps.Where(step => step.Event == TaxableEvent.Equipment).ToList();
        var steps = ledger.ToDictionary(step => step.RuleKey);
        var set = year.Equipment;

        return new EquipmentView(
            set,
            steps.Count > 0
                ?
                [
                    new Drain("equipment_costs", DrainKind.PaidOut, set.Costs, "expenses") { Details = [StepDetail.From(steps["equipment_costs"])] },
                    new Drain("equipment_contribution", DrainKind.Back, set.Contribution, "pocket") { Details = [StepDetail.From(steps["equipment_contribution"])] },
                ]
                : [],
            ledger.Select(StepDetail.From).ToList());
    }
}

// The IT and phone's one disclosure, « When you have it »: the day the company provides the set from and the
// day it is given back on, each by its date, or all year while neither is given, an assumption named so.
internal static class EquipmentPeriodView
{
    public static IReadOnlyList<SettingPhrase> Of(EquipmentInputs set) => set.ProvidedFrom.Value is null && set.ReturnedOn.Value is null
        ? [SettingPhrase.Of("Period_AllYear", set.ProvidedFrom.Origin)]
        :
        [
            .. Day("Period_ProvidedFrom", set.ProvidedFrom),
            .. Day("Period_ReturnedOn", set.ReturnedOn),
        ];

    // Every refusal of a date opens it.
    public static bool Holds(Refusal? refusal) => refusal?.Input is RefusedInput.EquipmentProvidedFrom or RefusedInput.EquipmentReturnedOn;

    private static IEnumerable<SettingPhrase> Day(string key, Input<DateOnly?> date) => date.Value is { } day
        ? [SettingPhrase.Of(key, date.Origin, Formats.Day(day))]
        : [];
}

// The plan with its IT and phone against the same plan without it, at the pay the plan with it runs at: what
// reaches the pocket in each, and what its benefit adds to the taxes on pay. The two runs are the set's
// LeverGain, its second the river with the set out of the plan, so the pay is held as the car's comparison
// holds it (PayHeld). CashCost is what the set takes from the pocket, in cash as every gain is, below zero
// when the plan with it leaves more there. Then the set against the same things bought privately, once the
// user gives what they would cost them for the year (PrivateCost), which no rule reads: AgainstPrivate is that
// cost less the cash the set takes, what it gains above zero, what it costs more below. None until a set
// counts, or while the engine refuses an amount.
internal sealed record EquipmentComparison(decimal PocketWith, decimal PocketWithout, decimal TaxesOnPayAdded)
{
    // As the user gives it, or none until given.
    public decimal? PrivateCost { get; init; }

    // The pay both plans run at when the pay set is held below, none otherwise.
    public decimal? PayHeld { get; init; }

    public decimal CashCost => Difference(PocketWithout, PocketWith);

    // None until the private cost is given, or while it is refused; a plain zero for a tie.
    public decimal? AgainstPrivate => PrivateCost is { } cost && cost >= 0m ? Difference(cost, CashCost) : null;

    // The private cost's refusal, or none: it cannot be below nothing. The card refuses it, the engine never
    // seeing it, with the refusal Core declares for it.
    public Refusal? Refused => PrivateCost < 0m ? EquipmentCalculator.PrivateCostNegative : null;

    public static EquipmentComparison? From(RiverInputs inputs, RiverRun run) => From(inputs, run, LeverGain.Of(inputs, run, OptionalModule.Equipment));

    public static EquipmentComparison? From(RiverInputs inputs, RiverRun run, LeverGain? gain)
    {
        if (run.Year is not { Scenario.Equipment.Any: true } with || gain is not { Without.Year: { } without })
            return null;

        return new EquipmentComparison(gain.PocketWith, gain.PocketWithout, Difference(TaxesOnPay(with), TaxesOnPay(without)))
        {
            PrivateCost = inputs.Equipment.PrivateCost.Value,
            PayHeld = gain.PayHeld,
        };
    }

    // The pay's own taxes, where the set's benefit is taxed with it.
    private static decimal TaxesOnPay(YearResult year) => BucketsView.PaidOut(year.Steps, TaxableEvent.DirectorPay);

    // A plain zero when the two are the same, never a negative one.
    private static decimal Difference(decimal more, decimal less) => more == less ? 0m : more - less;
}
