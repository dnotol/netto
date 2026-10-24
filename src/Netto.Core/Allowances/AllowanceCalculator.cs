using Netto.Core.Rules;

namespace Netto.Core.Allowances;

public static class AllowanceCalculator
{
    public static readonly Refusal OfficeAllowanceNegative = new("office_allowance_negative", RefusedInput.OfficeAllowance);
    public static readonly Refusal OfficeAllowanceAboveMaximum = new("office_allowance_above_maximum", RefusedInput.OfficeAllowance);
    public static readonly Refusal InternetAllowanceNegative = new("internet_allowance_negative", RefusedInput.InternetAllowance);
    public static readonly Refusal InternetAllowanceAboveMaximum = new("internet_allowance_above_maximum", RefusedInput.InternetAllowance);
    public static readonly Refusal ComputerAllowanceNegative = new("computer_allowance_negative", RefusedInput.ComputerAllowance);
    public static readonly Refusal ComputerAllowanceAboveMaximum = new("computer_allowance_above_maximum", RefusedInput.ComputerAllowance);
    public static readonly Refusal PhoneCostsNegative = new("phone_costs_negative", RefusedInput.PhoneCosts);

    // The allowances as the rules accept them: each a yearly amount, at most the year's sum of its
    // monthly maxima; a director who works from home only part of the year enters less. Above it the
    // allowance would no longer be a reimbursement the administration accepts without documents, which
    // is refused rather than taxed. The phone costs, reimbursed on invoices, have no maximum.
    public static (ExpenseAllowances? Value, Refusal? Error) Accept(AllowanceRules rules, ExpenseAllowances allowances)
    {
        if (allowances.Office < 0m)
            return (null, OfficeAllowanceNegative);
        if (allowances.Office > OfficeAllowanceMaximum(rules))
            return (null, OfficeAllowanceAboveMaximum);
        if (allowances.Internet < 0m)
            return (null, InternetAllowanceNegative);
        if (allowances.Internet > InternetAllowanceMaximum(rules))
            return (null, InternetAllowanceAboveMaximum);
        if (allowances.Computer < 0m)
            return (null, ComputerAllowanceNegative);
        if (allowances.Computer > ComputerAllowanceMaximum(rules))
            return (null, ComputerAllowanceAboveMaximum);
        if (allowances.Phone < 0m)
            return (null, PhoneCostsNegative);

        return (allowances, null);
    }

    // Each month at the maximum that holds for it: a maximum holds from its month to the month before
    // the next one starts, the last to December.
    public static decimal OfficeAllowanceMaximum(AllowanceRules rules) =>
        rules.OfficeAllowanceMaxima
            .Select((maximum, i) => (NextFromMonth(rules, i) - maximum.FromMonth) * maximum.Amount.Value)
            .Sum();

    public static decimal InternetAllowanceMaximum(AllowanceRules rules) => YearParts.Months * rules.InternetAllowanceMonthlyMaximum.Value;

    public static decimal ComputerAllowanceMaximum(AllowanceRules rules) => YearParts.Months * rules.ComputerAllowanceMonthlyMaximum.Value;

    // The month the next maximum starts in, or the month after December.
    private static int NextFromMonth(AllowanceRules rules, int i) =>
        i + 1 < rules.OfficeAllowanceMaxima.Count ? rules.OfficeAllowanceMaxima[i + 1].FromMonth : YearParts.Months + 1;
}
