namespace Netto.Core.Allowances;

// What the company pays the director in expense allowances for the year, as the user declares it: the
// flat-rate office, internet and computer allowances, and the phone costs it reimburses on invoices.
// None unless given.
public sealed record ExpenseAllowances(decimal Office, decimal Internet, decimal Computer, decimal Phone)
{
    public static ExpenseAllowances None { get; } = new(0m, 0m, 0m, 0m);

    public bool Any => this != None;

    // What the company pays for them, and what reaches the pocket: the same amount.
    public decimal Total => Office + Internet + Computer + Phone;
}
