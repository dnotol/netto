using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the river holds that the plan the moves are from does not, beyond what the moves set, by the card
// that holds it (RiverDifferences): read on the facts the search runs (RiverInputs.MoveScenario), so what
// no move reads, and a difference of origin alone, say nothing. The tab words it (OptimisationTabText).
public class RiverDifferencesTests
{
    // Today, as the made-up company saved it: a revenue and running costs of its own and no lever, the plan
    // a royalty was added to in the browser on 2026-10-25.
    private static readonly RiverInputs Today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

    // The same Today with a company car and the meal vouchers in its plan.
    private static readonly RiverInputs TodayWithCarAndMeals =
        Today.WithCatalogueValue(40_000m).WithCarCosts(5_000m).AddToPlan(OptionalModule.Car).WithMealVoucherDays(200).AddToPlan(OptionalModule.Vouchers);

    private static string Said(RiverInputs river, RiverInputs plan) =>
        string.Join(" ", RiverDifferences.Between(river, plan).Select(difference => $"{difference.Module}:{difference.Kind}"));

    // Each fact the river holds that Today does not, with the card that holds it and whether the river or
    // Today holds the lever: the module's name and the kind, as text, since both are internal.
    public static TheoryData<string, RiverInputs, RiverInputs, string> OtherFacts => new()
    {
        { "the revenue", Today, Today.WithRevenue(150_000m), "CompanyYear:Changed" },
        { "the running costs", Today, Today.WithRunningCosts(25_000m), "CompanyYear:Changed" },
        { "the company's year skipped", Today, Today.SkipCompany(), "CompanyYear:Changed" },
        { "the balance sheet's band", Today, Today.WithCompanyContributionBand(CompanyContributionBand.Upper), "CompanyYear:Changed" },
        { "the small company's status", Today, Today.WithSmallCompany(false), "CorporateTax:Changed" },
        { "the company's financial year", Today, Today.WithFinancialYear(FinancialYear.First), "CorporateTax:Changed" },
        { "the municipal rate", Today, Today.WithMunicipality(Today.Municipality.Enter(0.08m)), "DirectorPay:Changed" },
        { "the fund's fee", Today, Today.WithFundFeeRate(0.03m), "DirectorPay:Changed" },
        { "who pays the contributions", Today, Today.WithContributionsPaidBy(ContributionsPaidBy.Company), "DirectorPay:Changed" },
        { "what was withheld on the pay", Today, Today.WithTaxWithheld(1_000m), "DirectorPay:Changed" },
        { "the first self-employed years", Today, Today.WithStarter(true), "DirectorPay:Changed" },
        { "the reserve", Today, Today.WithReserve(1_000m), "Dividend:Changed" },
        { "the earlier profits", Today, Today.WithEarlierProfits(5_000m), "Dividend:Changed" },
        { "meal vouchers added", Today, Today.WithMealVoucherDays(200).AddToPlan(OptionalModule.Vouchers), "Vouchers:Added" },
        { "expense allowances added", Today, Today.WithOfficeAllowance(1_000m).AddToPlan(OptionalModule.Allowances), "Allowances:Added" },
        { "a company car added", Today, Today.WithCatalogueValue(40_000m).WithCarCosts(5_000m).AddToPlan(OptionalModule.Car), "Car:Added" },
        { "a royalty's creative share added with the royalty", Today, Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright), "Copyright:Added" },
        { "the home's cadastral income, the rent out of both plans", Today, Today.WithCadastralIncome(2_000m), "Rent:Changed" },
        { "the home's share let, the rent in both plans", Today.AddToPlan(OptionalModule.Rent), Today.WithShareLet(0.12m).AddToPlan(OptionalModule.Rent), "Rent:Changed" },
        { "the car taken out of the river", TodayWithCarAndMeals, TodayWithCarAndMeals.TakeOutOfPlan(OptionalModule.Car), "Car:LeftOut" },
        { "the meal vouchers taken out of the river", TodayWithCarAndMeals, TodayWithCarAndMeals.TakeOutOfPlan(OptionalModule.Vouchers), "Vouchers:LeftOut" },
        { "the car's costs, the car in both", TodayWithCarAndMeals, TodayWithCarAndMeals.WithCarCosts(6_000m), "Car:Changed" },
        { "a creative share kept after the royalty left the plan", Today, Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright).TakeOutOfPlan(OptionalModule.Copyright), "Copyright:Changed" },
    };

    [Theory]
    [MemberData(nameof(OtherFacts))]
    public void Between_riverWithOtherFacts_namesTheCardAndWhetherItIsInEachPlan(string _, RiverInputs plan, RiverInputs river, string expected)
    {
        Said(river, plan).ShouldBe(expected);
    }

    // Found in the browser on 2026-10-25: Today saved with no lever, then a royalty that follows the pay at
    // 80% added to the river. The split searched the river, the tab Today, and nothing said why.
    [Fact]
    public void Between_todayWithoutALeverAndARiverWithARoyaltyAt80Percent_namesTheRoyaltyAsNotInToday()
    {
        var river = Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

        Said(river, Today).ShouldBe("Copyright:Added");
    }

    [Fact]
    public void Between_riverAsThePlan_isEmpty()
    {
        RiverDifferences.Between(TodayWithCarAndMeals, TodayWithCarAndMeals).ShouldBeEmpty();
    }

    // A lever that holds nothing is the same in the plan or out of it, whichever holds it: the year is the
    // same, so the note names no card. Each lever by hand, since each keeps its own facts while it is out
    // (the car's carry the example's dates, which only a car with a catalogue value counts).
    public static TheoryData<OptionalModule> Levers => new(Enum.GetValues<OptionalModule>());

    [Theory]
    [MemberData(nameof(Levers))]
    public void Between_aLeverHoldingNothingJoiningThePlan_isEmpty(OptionalModule lever)
    {
        RiverDifferences.Between(Today.AddToPlan(lever), Today).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Levers))]
    public void Between_aLeverHoldingNothingLeavingThePlan_isEmpty(OptionalModule lever)
    {
        RiverDifferences.Between(Today, Today.AddToPlan(lever)).ShouldBeEmpty();
    }

    // The engine counts a car only once its catalogue value is given: its other facts typed, the river
    // holds the year as Today does.
    [Fact]
    public void Between_aCarWithoutACatalogueValueJoiningThePlan_isEmpty()
    {
        var river = Today.WithCarCosts(5_000m).WithCarCo2(95).AddToPlan(OptionalModule.Car);

        RiverDifferences.Between(river, Today).ShouldBeEmpty();
    }

    // The cards in the order the river draws them, the income year before them: a reader finds the first
    // where the page starts.
    [Fact]
    public void Between_severalCards_comeInTheRiversOrder()
    {
        var river = Today.WithReserve(1_000m).WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright)
            .WithOfficeAllowance(500m).AddToPlan(OptionalModule.Allowances).WithRevenue(150_000m).WithFundFeeRate(0.03m);

        Said(river, Today).ShouldBe("CompanyYear:Changed Allowances:Added Copyright:Added DirectorPay:Changed Dividend:Changed");
    }

    // What the moves set is the river's to move: the pay, the payments, the vouchers' amounts, the rent, the
    // royalty's amount and mode, and the levers they add. Each by hand; every input is held to it below.
    [Fact]
    public void Between_riverMovedByWhatTheMovesSet_isEmpty()
    {
        var river = Today.WithDirectorPay(40_000m).WithAdvancePayment(3, 5_000m).WithTaxAdvancePayment(3, 5_000m)
            .WithEcoVouchers(250m).WithSportCultureVouchers(100m).AddToPlan(OptionalModule.Vouchers)
            .WithRent(3_000m).AddToPlan(OptionalModule.Rent).WithRoyalty(5_000m).AddToPlan(OptionalModule.Copyright);

        RiverDifferences.Between(river, Today).ShouldBeEmpty();
    }

    // The example's own amounts are labelled as such; typing one again changes the origin and no figure.
    [Fact]
    public void Between_aTypedValueEqualToTheExamples_isEmpty()
    {
        RiverDifferences.Between(RiverInputs.Example.WithRevenue(120_000m), RiverInputs.Example).ShouldBeEmpty();
    }

    // What no move reads says nothing: what the fund bills reaches only the calendar, and what the same car
    // would cost privately only the car card.
    [Fact]
    public void Between_whatNoMoveReads_isEmpty()
    {
        var river = TodayWithCarAndMeals.WithBilledContributions(2_000m).WithCarPrivateCost(9_000m);

        RiverDifferences.Between(river, TodayWithCarAndMeals).ShouldBeEmpty();
    }

    // Every module the tab can name has a place in the river's order, and the split, whose pay and royalty
    // the moves set, is none.
    [Fact]
    public void Nameable_everyModuleButTheSplit_inTheRiversOrder()
    {
        RiverDifferences.Nameable.Select(module => module.ToString()).ShouldBe(
            ["River", "CompanyYear", "Vouchers", "Allowances", "Rent", "Car", "Copyright", "DirectorPay", "CorporateTax", "Dividend"]);
    }

    // The river with all five optional modules in its plan, and a fact in each lever that makes its being in
    // the plan count: a lever that holds nothing is the same in the plan or out of it.
    private static readonly RiverInputs AllInThePlan = RiverInputs.Example
        .WithMealVoucherDays(100).WithOfficeAllowance(500m).WithCatalogueValue(40_000m).WithCarCosts(5_000m)
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent)
        .AddToPlan(OptionalModule.Car).AddToPlan(OptionalModule.Copyright);

    // The inputs the tab names no card for, each with why. A move sets the first: the river moves away from
    // Today by it by design. No move reads the others. A module's flag whose facts the moves keep while it is
    // out, and whose amount the moves set, changes nothing the engine sees.
    private static readonly Dictionary<string, string> Unsaid = new()
    {
        ["DirectorPay"] = "a move sets it: the split's best pay",
        ["AdvancePayments.First"] = "a move sets it: the company's payment by that deadline",
        ["AdvancePayments.Second"] = "a move sets it: the company's payment by that deadline",
        ["AdvancePayments.Third"] = "a move sets it: the company's payment by that deadline",
        ["AdvancePayments.Fourth"] = "a move sets it: the company's payment by that deadline",
        ["TaxPrepayments.Paid.First"] = "a move sets it: the director's payment by that deadline",
        ["TaxPrepayments.Paid.Second"] = "a move sets it: the director's payment by that deadline",
        ["TaxPrepayments.Paid.Third"] = "a move sets it: the director's payment by that deadline",
        ["TaxPrepayments.Paid.Fourth"] = "a move sets it: the director's payment by that deadline",
        ["Vouchers.EcoVouchers"] = "a move sets it: the eco vouchers at their maximum",
        ["Vouchers.SportCultureVouchers"] = "a move sets it: the sport and culture vouchers at their maximum",
        ["Rent.Rent"] = "a move sets it: the rent at the cap",
        ["Copyright.Royalty"] = "a move sets it, with the pay: the royalty's fixed amount",
        ["Copyright.FollowsPay"] = "a move sets it, with the pay: the royalty's mode",
        ["Composition.Rent"] = "the home's facts are kept while Rent is out and its amount is the move's: the engine sees the same in or out",
        ["Composition.Copyright"] = "the creative share and statuses are kept while Copyright royalty is out and its amount and mode are the move's: the same in or out",
        ["BilledContributions"] = "no move reads it: what the fund bills reaches only the calendar",
        ["Car.PrivateCost"] = "no move reads it: only the car card weighs the car against it",
        ["Household.PartnerIncome"] = "the river holds the partner, but hands the engine none yet",
        ["Household.BothAged66"] = "the river holds the partner, but hands the engine none yet",
        ["Household.TaxedSeparately"] = "the river holds the partner, but hands the engine none yet",
    };

    // The guard of this file. Every input of the river is held to the tab: given another value, it is named
    // by exactly one card, or it is one of the inputs above, which are named by none. An input added to the
    // river fails here until a card names it in RiverDifferences or it is listed above with its reason.
    [Fact]
    public void Between_everyInputOfTheRiver_isNamedByOneCardOrIsUnsaidForAReason()
    {
        var leaves = RiverLeaves.Of(AllInThePlan);

        var wrong = leaves
            .Select(leaf => (leaf.Path, Named: RiverDifferences.Between(leaf.WithAnotherValue, AllInThePlan).Count))
            .Where(leaf => leaf.Named != (Unsaid.ContainsKey(leaf.Path) ? 0 : 1))
            .Select(leaf => $"{leaf.Path}: named by {leaf.Named} cards, {(Unsaid.ContainsKey(leaf.Path) ? "unsaid for a reason" : "to be named by one")}")
            .ToList();

        leaves.Count.ShouldBeGreaterThan(50);
        wrong.ShouldBeEmpty();
    }

    // An input listed as unsaid is one the river has, or the list would hide a card's.
    [Fact]
    public void Unsaid_everyInputListed_isAnInputOfTheRiver()
    {
        var paths = RiverLeaves.Of(AllInThePlan).Select(leaf => leaf.Path).ToList();

        Unsaid.Keys.Except(paths).ShouldBeEmpty();
    }

    // An origin is where a value comes from, never what the moves were found for: the same value with
    // another origin, at every input, says nothing.
    [Fact]
    public void Between_everyInputGivenAnotherOriginAlone_isEmpty()
    {
        var named = RiverLeaves.Of(AllInThePlan)
            .Where(leaf => leaf.WithAnotherOrigin is not null && RiverDifferences.Between(leaf.WithAnotherOrigin, AllInThePlan).Count > 0)
            .Select(leaf => leaf.Path)
            .ToList();

        named.ShouldBeEmpty();
    }
}
