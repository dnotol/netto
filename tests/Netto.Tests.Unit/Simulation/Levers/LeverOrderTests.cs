using Netto.Core.Allowances;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.Simulation.Levers;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation.Levers;

// The levers of a year in the law's order, and the year folded over them: a lever the tests add to the
// list joins the year's figures and steps in its place, with no other change to the year's composition.
// The made-up company of the fixtures, 100,000 of profit before the director and 50,000 of pay.
public class LeverOrderTests
{
    private const string Refused = "test_lever_refused";

    public static TheoryData<int> YearsWithoutEveryLeversRules => [2025, UnsupportedIncomeYear];

    [Fact]
    public void For_2026_isTheFiveLeversInTheLawsOrder()
    {
        var levers = LeverOrder.For(2026).ShouldNotBeNull();

        levers.Select(lever => lever.GetType()).ShouldBe(
            [typeof(VoucherLever), typeof(AllowanceLever), typeof(RentLever), typeof(CarLever), typeof(CopyrightLever)]);
    }

    // 2025 has rules for the rent and the royalty, for the Tax-Calc tests, but none for the vouchers.
    [Theory]
    [MemberData(nameof(YearsWithoutEveryLeversRules))]
    public void For_yearWithoutEveryLeversRules_isNone(int incomeYear)
    {
        LeverOrder.For(incomeYear).ShouldBeNull();
    }

    // Taking 1,000 off what the company earns and handing it to the pocket untaxed and deducted in full is
    // what 1,000 of phone costs reimbursed on invoices does.
    [Fact]
    public void Run_aLeverHandingThePocketWhatItTakesUntaxed_leavesTheYearAsPhoneCostsOfThatAmount()
    {
        var withLever = RunWith(Scenario(), new TestLever(Takes: 1_000m, Pocket: 1_000m));
        var withPhone = Run(Scenario(allowances: new ExpenseAllowances(0m, 0m, 0m, 1_000m)));

        (withLever.ProfitBeforePay, withLever.CorporateTax, withLever.SocialContributions, withLever.PersonalTax, withLever.NetInPocket, withLever.KeptInCompany)
            .ShouldBe((withPhone.ProfitBeforePay, withPhone.CorporateTax, withPhone.SocialContributions, withPhone.PersonalTax, withPhone.NetInPocket,
                withPhone.KeptInCompany));
    }

    // What it takes and does not hand the pocket leaves the river before its start, as a car's costs do.
    [Fact]
    public void Run_aLeverAdded_layersPocketAndCompanyAddUpToTheProfitLessWhatItKeeps()
    {
        var result = RunWith(Scenario(), new TestLever(Takes: 1_000m, Pocket: 600m));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany).ShouldBe(100_000m - 400m);
    }

    // Last in the order, beside a rent: its step comes after the rent's and before the company's, and
    // the one it adds after the pay comes after the rent's tax and before the dividend.
    [Fact]
    public void Run_aLeverAdded_listsItsStepsAfterTheOtherLeversAndAfterThePays()
    {
        var alone = Run(Scenario(rent: RentAtTheCap)).Steps.Select(step => step.RuleKey).ToList();
        List<string> expected = [.. alone];
        expected.Insert(expected.IndexOf("rent_taxable_income") + 1, "test_lever");
        expected.Insert(expected.IndexOf("rent_income_tax_advance_payments") + 1, "test_lever_after_pay");

        var result = RunWith(Scenario(rent: RentAtTheCap), new TestLever(Takes: 1_000m, Pocket: 1_000m));

        result.Steps.Select(step => step.RuleKey).ShouldBe(expected);
    }

    [Fact]
    public void Run_aLeverTheCompanyCannotDeductAllOf_addsThatPartToItsTaxableIncome()
    {
        var deducted = RunWith(Scenario(), new TestLever(Takes: 1_000m, Pocket: 1_000m));

        var result = RunWith(Scenario(), new TestLever(Takes: 1_000m, Pocket: 1_000m, NotDeducted: 400m));

        (result.DisallowedExpenses, result.Company.TaxableIncome).ShouldBe((400m, deducted.Company.TaxableIncome + 400m));
    }

    // The car's benefit of 1,714.29, handed by a lever of the tests' own: the pay is taxed on it as
    // the car's is.
    [Fact]
    public void Run_aLeverHandingABenefitInKind_taxesItWithThePay()
    {
        var result = RunWith(Scenario(), new TestLever(Handed: new TaxedWithPay { BenefitInKind = 1_714.29m }));
        var car = Run(Scenario(car: ElectricCar));

        (result.DirectorPay.BenefitInKind, result.DirectorPay.TaxablePay, result.DirectorPay.NetInPocket)
            .ShouldBe((1_714.29m, car.DirectorPay.TaxablePay, car.DirectorPay.NetInPocket));
    }

    [Fact]
    public void Run_aLeverRefusing_refusesTheYearWithItsKeyAndAsksNoLeverAfterIt()
    {
        var (value, error) = YearSimulation.Run(Scenario(), [new TestLever(Refusal: Refused), new TestLever(Throws: true)]);

        (value, error).ShouldBe((null, Refused));
    }

    // The split keeps its place while the rules refuse an amount: what the lever takes counts as given.
    [Fact]
    public void ProfitBeforePay_aRefusedLever_takesWhatItIsGiven()
    {
        YearSimulation.ProfitBeforePay(Scenario(), WithTestLever(new TestLever(Takes: 1_000m, Refusal: Refused))).ShouldBe(99_000m);
    }

    [Fact]
    public void ProfitBeforePay_aLeverAdded_isWhatTheYearRunsWith()
    {
        var levers = WithTestLever(new TestLever(Takes: 1_234.56m, Pocket: 1_000m));

        YearSimulation.ProfitBeforePay(Scenario(), levers).ShouldBe(YearSimulation.Run(Scenario(), levers).Value.ShouldNotBeNull().ProfitBeforePay);
    }

    // A lever taking a tenth of the pay beside it: out of 110,000 the pay is at most 100,000, the two
    // together all of it.
    [Fact]
    public void PayCeiling_aLeverFollowingThePay_leavesThePayWhatTheTwoShare()
    {
        YearSimulation.PayCeiling(Scenario(revenue: 130_000m), WithTestLever(new TestLever(ShareOfPay: 0.1m))).ShouldBe(100_000m);
    }

    // A lever taking a fixed amount comes off before the split shares the rest.
    [Fact]
    public void PayCeiling_aLeverTakingAFixedAmount_leavesThePayTheRest()
    {
        YearSimulation.PayCeiling(Scenario(), WithTestLever(new TestLever(Takes: 1_000m))).ShouldBe(99_000m);
    }

    private static IReadOnlyList<Lever> WithTestLever(TestLever lever) => [.. LeverOrder.For(2026).ShouldNotBeNull(), lever];

    // The year with the river's levers and the test's after them, which the engine must not refuse.
    private static YearResult RunWith(Scenario scenario, TestLever lever)
    {
        var (value, error) = YearSimulation.Run(scenario, WithTestLever(lever));

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    // A lever of the tests' own: it takes Takes off what the company earns, and ShareOfPay of the pay
    // beside it when it follows the pay; it hands Pocket to the pocket untaxed, the company not deducting
    // NotDeducted of it, and Handed to the pay; it lists one step before the company's and one after the
    // pay's. It refuses with Refusal when given one, and throws when asked at all with Throws.
    private sealed record TestLever(
        decimal Takes = 0m, decimal ShareOfPay = 0m, decimal Pocket = 0m, decimal NotDeducted = 0m, TaxedWithPay? Handed = null, string? Refusal = null,
        bool Throws = false) : Lever
    {
        public override decimal FromProfit(Scenario scenario) => Takes + ShareOfPay * scenario.DirectorPay;

        public override bool FollowsPay(Scenario scenario) => ShareOfPay > 0m;

        public override (LeverYear? Value, string? Error) Compute(Scenario scenario, decimal left)
        {
            if (Throws)
                throw new InvalidOperationException("A lever after a refused one is never asked.");

            return Refusal is null ? (new TestYear(Pocket, NotDeducted, Handed ?? TaxedWithPay.None), null) : (null, Refusal);
        }
    }

    private sealed record TestYear(decimal Pocket, decimal NotDeducted, TaxedWithPay Handed) : LeverYear
    {
        private static readonly Source Text = PersonalTaxRules.IncomeYear2026.IncomeTaxScale;

        public override decimal ToPocket => Pocket;

        public override decimal Disallowed => NotDeducted;

        public override TaxedWithPay TaxedWithPay => Handed;

        public override IReadOnlyList<Step> Steps() => [new Step("test_lever", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, Pocket, Text, [])];

        public override IReadOnlyList<Step> StepsAfterPay(GrossToNetResult pay, PersonalTaxRules taxRules, Source municipalSurcharge, RuleAmount municipalRate) =>
            [new Step("test_lever_after_pay", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.PaidOut, 0m, Text, [])];
    }
}
