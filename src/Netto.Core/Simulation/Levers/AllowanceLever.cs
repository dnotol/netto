using Netto.Core.Allowances;
using Netto.Core.Rules;

namespace Netto.Core.Simulation.Levers;

// The expense allowances the company pays the director: they come off what it earns, as given, and reach
// the pocket untaxed. The company deducts them in full, so none of them joins its taxable income.
internal sealed record AllowanceLever(AllowanceRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => scenario.Allowances.Total;

    public override (LeverYear? Value, Refusal? Error) Compute(Scenario scenario, decimal left)
    {
        var (allowances, error) = AllowanceCalculator.Accept(Rules, scenario.Allowances);
        return allowances is null ? (null, error) : (new AllowanceYear(Rules, allowances, scenario.Allowances.Any), null);
    }
}

// The allowances as the rules accepted them, Used once the company pays any.
internal sealed record AllowanceYear(AllowanceRules Rules, ExpenseAllowances Allowances, bool Used) : LeverYear
{
    public override decimal ToPocket => Allowances.Total;

    // Each allowance reaches the pocket untaxed, at what the company pays: a reimbursement of its own
    // costs, not pay, within the maxima the administration accepts.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("office_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, Allowances.Office,
            Rules.Reimbursement, [.. Rules.OfficeAllowanceMaxima.Select(maximum => maximum.Amount)]),
        new Step("internet_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, Allowances.Internet,
            Rules.Reimbursement, [Rules.InternetAllowanceMonthlyMaximum]),
        new Step("computer_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, Allowances.Computer,
            Rules.Reimbursement, [Rules.ComputerAllowanceMonthlyMaximum]),
        new Step("phone_costs", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, Allowances.Phone,
            Rules.Reimbursement, []),
    ];
}
