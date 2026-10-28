using Netto.Core.CompanyCar;

namespace Netto.Web.Client.River;

// Whether a lever is in the plan of the river, in the plan the moves are from, or in only one of them: what
// a difference says beside the card's name (OptimisationTabText).
internal enum RiverDifferenceKind
{
    // Both hold it, or neither does, and a fact of it differs: a card of the river's core always.
    Changed,

    // The river holds the lever and the plan does not.
    Added,

    // The plan holds the lever and the river does not.
    LeftOut,
}

// A card whose facts the river holds otherwise than the plan the moves are from.
internal sealed record RiverDifference(RiverModule Module, RiverDifferenceKind Kind);

// What the river holds that the plan the moves are from does not, beyond what the moves set, by the card that
// holds it: the facts the moves were found for against the river's own. The moves are found on the plan's
// facts (RiverInputs.MoveScenario, which the search runs), so the difference is read on the facts of both:
// an origin alone, what no move reads (what the fund bills, what the same car would cost privately) and a
// lever out of both plans whose facts the engine never sees say nothing, and a fact the engine reads is
// never left out, which the guard in RiverDifferencesTests holds to every input of the river.
internal static class RiverDifferences
{
    // The cards a difference can name, in the order the river draws them, the income year, which no card
    // holds, first. The split is none: its pay, and the royalty beside it, are what the moves set.
    public static IReadOnlyList<RiverModule> Nameable { get; } = [.. Enum.GetValues<RiverModule>().Where(module => module != RiverModule.Split)];

    // The cards whose facts differ between the river and the plan, each with whether the river or the plan
    // holds the lever, in the order the river draws them.
    public static IReadOnlyList<RiverDifference> Between(RiverInputs river, RiverInputs plan)
    {
        var (mine, theirs) = (Facts(river), Facts(plan));

        return [.. Nameable.Where(module => !mine[module].Equals(theirs[module])).Select(module => new RiverDifference(module, KindOf(module, river, plan)))];
    }

    // What the search reads of the river, by the card that holds each fact, less the values the moves set:
    // the pay, which the split sets with the royalty beside it, and the royalty's mode and fixed amount, which the Copyright royalty card holds; the company's
    // and the director's advance payments; the eco and sport/culture vouchers; and the rent. Those a move
    // takes from the plan as they are, with the levers a move adds to the plan, stay in the facts: the
    // river and the plan hold the same of them unless the user changed them. A car counts only once its
    // catalogue value is given (CarFacts.Any), and the river's own carry the example's dates where
    // CarFacts.None carries none, so a car that holds no catalogue value is no car in either. A new kind of
    // move adds what it sets here, which MovesViewTests holds to every move applied.
    private static Dictionary<RiverModule, object> Facts(RiverInputs river)
    {
        var scenario = river.MoveScenario();

        return new()
        {
            [RiverModule.River] = scenario.IncomeYear,
            [RiverModule.CompanyYear] = (river.Company is null, scenario.Revenue, scenario.RunningCosts, scenario.CompanyContributionBand),
            [RiverModule.Vouchers] = scenario.Vouchers with { EcoVouchers = 0m, SportCultureVouchers = 0m },
            [RiverModule.Allowances] = scenario.Allowances,
            [RiverModule.Rent] = scenario.Rent with { Rent = 0m },
            [RiverModule.Car] = scenario.Car.Any ? scenario.Car : CarFacts.None,
            [RiverModule.Copyright] = scenario.Copyright with { Royalty = 0m, FollowsPay = false },
            [RiverModule.DirectorPay] =
                (scenario.MunicipalRate.Value, scenario.ContributionsPaidBy, scenario.FundFeeRate, scenario.TaxPrepayments.Withheld, scenario.TaxPrepayments.Starter),
            [RiverModule.CorporateTax] = scenario.Company,
            [RiverModule.Dividend] = (scenario.Reserve, scenario.DividendCategory, scenario.EarlierProfits),
        };
    }

    private static RiverDifferenceKind KindOf(RiverModule module, RiverInputs river, RiverInputs plan)
    {
        if (Lever(module) is not { } lever || river.InPlan(lever) == plan.InPlan(lever))
            return RiverDifferenceKind.Changed;

        return river.InPlan(lever) ? RiverDifferenceKind.Added : RiverDifferenceKind.LeftOut;
    }

    // The optional module a card is, none for a core card; a card no difference names, the split, is refused.
    private static OptionalModule? Lever(RiverModule module) => module == RiverModule.Split
        ? throw new ArgumentOutOfRangeException(nameof(module), module, "A card no difference names has no lever.")
        : RiverModules.Lever(module);
}
