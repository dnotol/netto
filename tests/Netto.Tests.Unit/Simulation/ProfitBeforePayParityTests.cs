using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// What the split shares is written twice: YearSimulation.ProfitBeforePay and PayCeiling take it off the
// scenario as given, lever by lever, and Run takes it off what the company earns through what the rules
// accepted. A lever added to one sum and not the other makes the split's ceiling disagree with the year,
// so each case is one combination of the levers paid besides pay (the vouchers, the expense allowances
// without the office allowance, which a rent excludes, the rent, a car the director pays a contribution
// for), with no royalty, a fixed one or one following the pay, whoever pays the contributions, at a few
// pays; the made-up company prepays enough to leave no surcharge.
public class ProfitBeforePayParityTests
{
    private static readonly ExpenseAllowances AllowancesBesideARent = new(0m, 240m, 240m, 300m);

    private static readonly CarFacts CarWithAContribution = ElectricCar with { PersonalContribution = 1_200m };

    private static readonly CopyrightRoyalty FixedRoyalty = new(10_000m);

    private static readonly CopyrightRoyalty FollowingRoyalty = new(0m) { CreativeShare = 0.75m, FollowsPay = true };

    private static readonly bool[] WithoutAndWith = [false, true];

    private static readonly ContributionsPaidBy[] Payers = [ContributionsPaidBy.Director, ContributionsPaidBy.Company];

    private static readonly decimal[] Pays = [0m, 30_000m, 60_000m];

    public enum Royalty
    {
        None,
        Fixed,
        Following,
    }

    public static TheoryData<bool, bool, bool, bool, Royalty, ContributionsPaidBy, decimal> EveryCombination => Combinations([Royalty.None, Royalty.Fixed, Royalty.Following]);

    public static TheoryData<bool, bool, bool, bool, Royalty, ContributionsPaidBy, decimal> EveryCombinationBesideAFixedRoyalty => Combinations([Royalty.None, Royalty.Fixed]);

    public static TheoryData<bool, bool, bool, bool, Royalty, ContributionsPaidBy, decimal> EveryCombinationBesideAFollowingRoyalty => Combinations([Royalty.Following]);

    [Theory]
    [MemberData(nameof(EveryCombination))]
    public void ProfitBeforePay_anyLevers_isTheYearsProfitBeforePay(
        bool vouchers, bool allowances, bool rent, bool car, Royalty royalty, ContributionsPaidBy paidBy, decimal pay)
    {
        var scenario = With(vouchers, allowances, rent, car, royalty, paidBy, pay);

        var profitBeforePay = YearSimulation.ProfitBeforePay(scenario);

        profitBeforePay.ShouldBe(Run(scenario).ProfitBeforePay);
    }

    // Beside no royalty or a fixed one, the split gives as pay all the year leaves before pay.
    [Theory]
    [MemberData(nameof(EveryCombinationBesideAFixedRoyalty))]
    public void PayCeiling_anyLeversBesideAFixedRoyalty_isTheYearsProfitBeforePayNeverBelowZero(
        bool vouchers, bool allowances, bool rent, bool car, Royalty royalty, ContributionsPaidBy paidBy, decimal pay)
    {
        var scenario = With(vouchers, allowances, rent, car, royalty, paidBy, pay);

        var ceiling = YearSimulation.PayCeiling(scenario);

        ceiling.ShouldBe(Math.Max(0m, Run(scenario).ProfitBeforePay));
    }

    // Beside a royalty that follows the pay, the year at the ceiling leaves at least that pay before pay.
    [Theory]
    [MemberData(nameof(EveryCombinationBesideAFollowingRoyalty))]
    public void PayCeiling_anyLeversBesideAFollowingRoyalty_leavesTheYearAtItThatPay(
        bool vouchers, bool allowances, bool rent, bool car, Royalty royalty, ContributionsPaidBy paidBy, decimal pay)
    {
        var scenario = With(vouchers, allowances, rent, car, royalty, paidBy, pay);
        var ceiling = YearSimulation.PayCeiling(scenario).ShouldNotBeNull();

        var year = Run(scenario with { DirectorPay = ceiling });

        year.ProfitBeforePay.ShouldBeGreaterThanOrEqualTo(ceiling);
    }

    // And the year a cent above it leaves less than that pay before pay: the royalty a ruling accepts there
    // takes more than the company has. The creative share always gives a royalty, so the ceiling is never
    // all the two share.
    [Theory]
    [MemberData(nameof(EveryCombinationBesideAFollowingRoyalty))]
    public void PayCeiling_anyLeversBesideAFollowingRoyalty_leavesTheYearACentAboveItLessThanThatPay(
        bool vouchers, bool allowances, bool rent, bool car, Royalty royalty, ContributionsPaidBy paidBy, decimal pay)
    {
        var scenario = With(vouchers, allowances, rent, car, royalty, paidBy, pay);
        var aCentAbove = YearSimulation.PayCeiling(scenario).ShouldNotBeNull() + 0.01m;

        var year = Run(scenario with { DirectorPay = aCentAbove });

        year.ProfitBeforePay.ShouldBeLessThan(aCentAbove);
    }

    private static Scenario With(bool vouchers, bool allowances, bool rent, bool car, Royalty royalty, ContributionsPaidBy paidBy, decimal pay) =>
        Scenario(
            directorPay: pay,
            paidBy: paidBy,
            vouchers: vouchers ? AllVouchers : VoucherGrants.None,
            allowances: allowances ? AllowancesBesideARent : ExpenseAllowances.None,
            rent: rent ? RentAtTheCap : null,
            car: car ? CarWithAContribution : null,
            royalty: royalty switch
            {
                Royalty.Fixed => FixedRoyalty,
                Royalty.Following => FollowingRoyalty,
                _ => CopyrightRoyalty.None,
            });

    private static TheoryData<bool, bool, bool, bool, Royalty, ContributionsPaidBy, decimal> Combinations(Royalty[] royalties)
    {
        var data = new TheoryData<bool, bool, bool, bool, Royalty, ContributionsPaidBy, decimal>();
        var combinations =
            from vouchers in WithoutAndWith
            from allowances in WithoutAndWith
            from rent in WithoutAndWith
            from car in WithoutAndWith
            from royalty in royalties
            from paidBy in Payers
            from pay in Pays
            select (vouchers, allowances, rent, car, royalty, paidBy, pay);
        foreach (var (vouchers, allowances, rent, car, royalty, paidBy, pay) in combinations)
            data.Add(vouchers, allowances, rent, car, royalty, paidBy, pay);

        return data;
    }
}
