using Netto.Core.Copyright;

namespace Netto.Core.Simulation;

// The conditions one part of the year sets on another, read from a year the engine ran, each only where
// it applies, in the order the year meets them: the rent and the office allowance, then the IT and phone and
// the computer and internet allowances, between the levers; on
// the corporate rate, the pay test and the benefits' share, as Corporate tax tested them, and what a
// royalty does to the pay that keeps the rate, while the pay is tested and some pay keeps it with the
// royalty or without it, since where none does the royalty plays no part in losing it; then the reserve on
// what the year pays out, once one is kept. Beside the year rather than in it: the royalty's condition runs the search
// for the smallest pay keeping the rate twice, which the year, run at every pay of the split's search and
// of the moves', never needs.
public static class YearConditions
{
    public static IReadOnlyList<YearCondition> Of(YearResult year)
    {
        var scenario = year.Scenario;
        var company = year.Company;
        var conditions = new List<YearCondition>();

        if (scenario.Rent.Any || scenario.Allowances.Office > 0m)
            conditions.Add(new NoOfficeAllowanceBesideRent(year.Rent.Rent, scenario.Allowances.Office));
        if (scenario.Equipment is { } set && (set.Pcs + set.Tablets > 0 || set.Internet))
            conditions.Add(new NoAllowanceBesideEquipment(set.Pcs + set.Tablets, set.Internet));
        if (company.PayTest is { } test)
            conditions.Add(new PayKeepsReducedRate(test, company.MinimumDirectorPay, company.DirectorPay, company.TaxableIncome));
        if (company.BenefitsWithinShare is { } within)
            conditions.Add(new BenefitsWithinShareOfPay(within, company.FlatRateBenefits, company.DirectorPay, company.ExcessBenefitsShare));
        if (company.PayTest is not null && PaysRoyalty(year) && RoyaltyOnThePayTest(scenario) is { } royalty && (royalty.With ?? royalty.Without) is not null)
            conditions.Add(royalty);
        if (scenario.Reserve > 0m)
            conditions.Add(new PayOutCoversReserve(scenario.Reserve, year.Held));

        return conditions;
    }

    // A fixed royalty once it is more than nothing; one that follows the pay once a ruling accepts some at
    // a pay, whatever it comes to at the plan's own.
    private static bool PaysRoyalty(YearResult year) =>
        year.Scenario.Copyright.FollowsPay ? year.Copyright.Following == CopyrightFollowing.AtCeiling : year.Scenario.Copyright.Any;

    private static RoyaltyLowersPayKeepingReducedRate RoyaltyOnThePayTest(Scenario scenario) => new(
        YearSimulation.SmallestPayKeepingReducedRate(scenario),
        YearSimulation.SmallestPayKeepingReducedRate(scenario with { Copyright = CopyrightRoyalty.None }),
        scenario.Copyright.FollowsPay);
}
