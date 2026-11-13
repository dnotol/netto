using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the bar of buckets draws from one run of the river: where the money ends up, out of what the
// river starts from, what the company earns before it pays INASTI's yearly company contribution, its
// car or its director anything, pay, vouchers, expense allowances or rent, with the profits kept from
// earlier years or, with the company skipped, what the pay costs the company, the gross plus the
// contributions it pays on the director's behalf when it does (CompanyPaysContributions). The
// contribution and what the IT and phone and a car cost the company (LeverExpenses), costs of the company like its running costs
// (operator, 2026-10-23; decided 2026-10-25 for the contribution), are its expenses, the bar's last
// bucket, nothing with the company skipped; the running costs are in no bucket, the river starting after
// them. The vouchers, the allowances and the rent are in the pocket, the director's contribution to a car
// out of it, back in the company's profit. Taxes on pay are the director's personal tax: the director
// pay's steps paid out, a car's benefit and a royalty's part beyond its limits taxed in them, once the
// company pays a rent, the rent's, which it is taxed with (TaxesOnPayIncludeRent), and a royalty's tax
// apart (decided unattended: the director's income tax on money received besides the dividend, as the
// rent's is); taxes on profit, the company's and the dividend's, the refund on the exempt dividend coming
// back. It only regroups the engine's figures and steps, so the buckets add up to the start.
internal sealed record BucketsView(
    decimal Start, decimal EarlierProfits, bool StartsAtPay, bool CompanyPaysContributions, bool TaxesOnPayIncludeRent, IReadOnlyList<Bucket> Buckets)
{
    // In the order the bar shows them, the pocket first; also when there is no figure to show, and
    // whatever the plan holds.
    public static IReadOnlyList<string> Keys { get; } = ["pocket", "taxes_on_pay", "taxes_on_profit", "kept_in_company", "expenses"];

    // The taxes on pay hold a copyright royalty's tax apart too, once there is one.
    public bool TaxesOnPayIncludeRoyalty { get; init; }

    // What the IT and phone and a company car cost the company over the year, in the expenses: nothing until
    // one counts, or with the company skipped.
    public decimal LeverExpenses { get; init; }

    // What reaches the pocket out of what leaves the company: what the river starts from less its
    // expenses and what the company keeps at year end, which is still the director's. None where the bar
    // draws no shares.
    public decimal? Reach => Drawn ? ReachOf(Start, AmountOf("expenses"), AmountOf("pocket"), AmountOf("kept_in_company")) : null;

    // What the river starts from once there is a year of the company, the one start every reach is out
    // of, the split's included (SplitSearchView), its expenses left out: what the company earns before it
    // pays INASTI's company contribution, its car or its director anything, plus the profits kept from
    // earlier years.
    public static decimal StartOf(YearResult year) => year.Scenario.ProfitBeforeDirector + year.Scenario.EarlierProfits;

    // The company's expenses over the year, whatever it pays its director: INASTI's company contribution
    // and what its IT and phone and its car cost it.
    public static decimal ExpensesOf(YearResult year) => year.CompanyContribution + year.Equipment.Costs + year.Car.Costs;

    // What reaches the pocket out of what leaves the company, the start less its expenses and what it
    // keeps: the bar's reach, and the split's at each pay. None where nothing leaves it.
    internal static decimal? ReachOf(decimal start, decimal expenses, decimal pocket, decimal kept) => Shares.Of(pocket, start - expenses - kept);

    public static BucketsView? From(RiverRun run)
    {
        if (run.Pay is not { } pay)
            return null;

        var earlierProfits = run.Year?.Scenario.EarlierProfits ?? 0m;
        var leverExpenses = run.Year is { } ran ? ran.Equipment.Costs + ran.Car.Costs : 0m;
        var (start, expenses, pocket, kept, steps) = run.Year is { } year
            ? (StartOf(year), ExpensesOf(year), year.NetInPocket, year.KeptInCompany, year.Steps)
            : (pay.CompanyCost, 0m, pay.NetInPocket, 0m, pay.Steps);

        (string Key, decimal Amount)[] amounts =
        [
            ("pocket", pocket),
            ("taxes_on_pay", PaidOut(steps, TaxableEvent.DirectorPay) + PaidOut(steps, TaxableEvent.Rent) + PaidOut(steps, TaxableEvent.CopyrightRoyalty)),
            ("taxes_on_profit", PaidOut(steps, TaxableEvent.CompanyProfit) + PaidOut(steps, TaxableEvent.Dividend)),
            ("kept_in_company", kept),
            ("expenses", expenses),
        ];
        var drawn = start > 0m && amounts.All(bucket => bucket.Amount >= 0m);

        return new BucketsView(
            start,
            earlierProfits,
            run.Year is null,
            pay.ContributionsPaidByCompany > 0m,
            steps.Any(step => step.Event == TaxableEvent.Rent),
            amounts.Select(bucket => new Bucket(bucket.Key, bucket.Amount, drawn ? bucket.Amount / start : null)).ToList())
        {
            TaxesOnPayIncludeRoyalty = steps.Any(step => step.Event == TaxableEvent.CopyrightRoyalty),
            LeverExpenses = leverExpenses,
        };
    }

    private bool Drawn => Buckets.All(bucket => bucket.Share is not null);

    // What a bucket holds, by its key; the Plans panel's comparison reads its columns through it.
    internal decimal AmountOf(string key) => Buckets.Single(bucket => bucket.Key == key).Amount;

    // What an event's steps pay out, a step coming back counting against it: the sum every bucket of
    // taxes is made of, and a comparison of two plans reads.
    internal static decimal PaidOut(IReadOnlyList<Step> steps, TaxableEvent taxableEvent) =>
        steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

// Share is a fraction of what the river starts from, or null when shares would not mean anything:
// nothing to start from, or a bucket below zero (a loss kept in the company).
internal sealed record Bucket(string Key, decimal Amount, decimal? Share);
