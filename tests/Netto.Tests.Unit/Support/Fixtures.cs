using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Shouldly;

namespace Netto.Tests.Support;

// What the engine's tests share: a made-up company for income year 2026 at a 7% municipal rate, with
// 120,000 of revenue and 19,590.70 of running costs, its balance sheet in INASTI's lower band, so its
// 409.30 of company contribution with them make the 20,000 of costs that leave 100,000 of profit before
// the director; a small company past its first four tax periods; 50,000 of pay; a VVPRbis dividend from
// the third distribution; nothing kept; the director paying their contributions, with no fund fee given;
// and no lever and no partner, until a test gives one. The company prepays enough by 10 April to leave no advance-payment surcharge, and
// withholds more than any director pay's tax, unless a test says otherwise. A new lever is a new
// argument of Scenario, in this one place.
internal static class Fixtures
{
    // An income year no rule set has rules for: 2027's amounts are not published. The tests that hold each
    // rule set to refusing a year outside the ones it has, and the engine's, take this one.
    public const int UnsupportedIncomeYear = 2027;

    public static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    // With the lower band's 409.30 of INASTI's company contribution, the 20,000 of costs the made-up
    // company had when the contribution was counted in them.
    public const decimal RunningCosts = 19_590.70m;

    // Its credit, 9,000, covers the surcharge on any tax these tests reach.
    public static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    // More than the tax on any pay these tests reach, so the pay leaves no surcharge either.
    public static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    public static readonly VoucherGrants MealVouchersOn220Days = new(220, 0m, 0m);

    // 1,960.20 of meal vouchers, 880.00 of it deductible, and the most eco and sport/culture vouchers:
    // 2,310.20 to the pocket and 1,430.20 the company cannot deduct.
    public static readonly VoucherGrants AllVouchers = new(220, 250m, 100m);

    // The most the circular allows for the office, 1,938.44, and for internet and a computer, 240 each:
    // 2,418.44, all of it deducted by the company.
    public static readonly ExpenseAllowances FlatRateAllowances = new(1_938.44m, 240m, 240m, 0m);

    // 12% of a cadastral income of 1,200 let for the 2026 cap, 1,380.00: 828.00 of net rent.
    public static readonly HomeOfficeRent RentAtTheCap = new(1_200m, 0.12m, 1_380m);

    // A spouse or legal cohabitant the director is taxed jointly with, who earns nothing: the marital
    // quotient attributes them 30% of the director's professional income, up to its cap.
    public static readonly Household NonEarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant);

    // One who earns 40,000 of net professional income, more than 30% of the two together beside the
    // pays these tests reach, so no quotient applies with the pay; alone, the partner would be taxed on
    // it, the quotient passing part of it to the director's side.
    public static readonly Household EarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant, 40_000m);

    // An electric car the company puts at the director's disposal all year: a catalogue value of 50,000,
    // first registered in January 2026, so a benefit of 1,714.29 at 4% (CarCalculatorTests), all year at
    // 100%, and 8,000 of yearly costs, which an electric car deducts in full; 17% of the benefit is added
    // back.
    public static readonly CarFacts ElectricCar = new(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 0m);

    // The river's example year as its scenario (RiverInputs.Example, held to it by FixturesTests): nothing
    // prepaid by the company, and the director's pay withheld on as the key formula says.
    public static Scenario RiverExample => Scenario(advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld);

    // The page's example river with a copyright royalty that follows the pay at a creative share of 25%: the
    // plan of the three views' position paper, on 11 November 2026. The figures tests pin on it are the engine's
    // own, a regression pin and no Tax-Calc value, and match what the band and the Optimise view say: it leaves
    // 62,624.32 in the pocket; the Optimiser adds 3,309.67 (the pay and the company's advance payment), to
    // 65,933.99; the vouchers, an idea, take that to 65,996.99, a gain of 63.00. The vouchers' +45.00 and +18.00
    // are derived by hand in PisteSearchTests.
    public static readonly Netto.Web.Client.River.RiverInputs ExampleWithRoyalty = Netto.Web.Client.River.RiverInputs.Example
        .AddToPlan(Netto.Web.Client.River.OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();

    // The made-up company, with the arguments given in place of its own.
    public static Scenario Scenario(
        decimal directorPay = 50_000m,
        decimal reserve = 0m,
        decimal revenue = 120_000m,
        CompanyStatus? company = null,
        WithholdingCategory category = WithholdingCategory.VvprbisThirdYearOnwards,
        decimal earlierProfits = 0m,
        AdvancePayments? advancePayments = null,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director,
        VoucherGrants? vouchers = null,
        ExpenseAllowances? allowances = null,
        HomeOfficeRent? rent = null,
        CarFacts? car = null,
        CopyrightRoyalty? royalty = null,
        CompanyContributionBand band = CompanyContributionBand.Lower,
        decimal? fundFeeRate = null,
        Household? household = null) =>
        new(2026, revenue, RunningCosts, company ?? SmallPastStartUp, directorPay, MunicipalRates.IncomeYear2026.Entered(0.07m), reserve, category, earlierProfits)
        {
            CompanyContributionBand = band,
            AdvancePayments = advancePayments ?? Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
            FundFeeRate = fundFeeRate,
            Vouchers = vouchers ?? VoucherGrants.None,
            Allowances = allowances ?? ExpenseAllowances.None,
            Rent = rent ?? HomeOfficeRent.None,
            Car = car ?? CarFacts.None,
            Copyright = royalty ?? CopyrightRoyalty.None,
            Household = household ?? Household.None,
        };

    // The year the engine runs for the scenario, which it must not refuse.
    public static YearResult Run(Scenario scenario)
    {
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    // The search across pays the engine runs for the scenario, which it must not refuse.
    public static SplitSearchResult Search(Scenario scenario)
    {
        var (value, error) = SplitSearch.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
