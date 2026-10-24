using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;

namespace Netto.Core.Simulation.Levers;

// Something the company pays the director or puts at their disposal besides pay, as the year composes
// it: a lever holds its income year's rules, and YearSimulation folds the year over the levers in the
// law's order (LeverOrder). A new lever is a Lever, its LeverYear and its place in that order.
// YearSimulation reads a lever's result by its type only where YearResult or PayLevel hands a card a
// typed result, so a new lever whose card reads a result of its own also needs that read there.
//
// FromProfit is what it takes off what the company earns as the scenario gives it, whether or not its
// rules accept it, so the split keeps its place while one refuses: what the year, the profit before pay
// and the pay ceiling all subtract, the one derivation of the profit before pay. When that amount moves
// with the pay (FollowsPay), AtPay sets it at the scenario's own pay before anything reads it, and the
// pay ceiling reads it again at each pay it tries. Compute takes it as the rules take it, beside what the
// levers before it leave of what the company earns (left), or gives what they refuse.
internal abstract record Lever
{
    public abstract decimal FromProfit(Scenario scenario);

    public virtual bool FollowsPay(Scenario scenario) => false;

    public virtual Scenario AtPay(Scenario scenario) => scenario;

    public abstract (LeverYear? Value, Refusal? Error) Compute(Scenario scenario, decimal left);
}

// What one lever did to the year, as its rules took it. ToPocket is what reaches the pocket besides the
// pay, before the personal tax on it, negative for what the director pays the company; Disallowed what the
// company cannot deduct of it or adds back to its taxable income; TaxedWithPay what it hands the pay to be
// taxed with it. Its steps join the ledger only when it is used, so a year without it keeps its steps:
// Steps before the company's, and StepsAfterPay, what it adds to the personal tax, after the pay's. Both
// are built only when the year is run, never at each pay of the split's search.
internal abstract record LeverYear
{
    public abstract decimal ToPocket { get; }

    public virtual decimal Disallowed => 0m;

    public virtual TaxedWithPay TaxedWithPay => TaxedWithPay.None;

    public abstract IReadOnlyList<Step> Steps();

    public virtual IReadOnlyList<Step> StepsAfterPay(GrossToNetResult pay, PersonalTaxRules taxRules, Source municipalSurcharge, RuleAmount municipalRate) => [];
}
