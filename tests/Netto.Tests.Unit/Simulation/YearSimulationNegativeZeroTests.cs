using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.Vouchers;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// A step or a balance of nothing is a plain zero, unless it is money coming back: the engine negates a credit
// or a refund, so one of nothing is a negative zero on purpose, and the drains and the step details read it as
// coming back (decimal.IsNegative). Any other amount of nothing that is a negative zero, a chain of subtractions
// ending on 0 - 0.00, would read as money back (« +€0.00 ») where nothing comes back. The years below are the
// ones where amounts are nothing, with the zeros typed with more or fewer decimals.
public class YearSimulationNegativeZeroTests
{
    private static readonly string[] ComingBack =
    [
        "low_earned_income_credit", "child_tax_credit", "income_tax_advance_payment_bonus", "dividend_exemption_refund", "car_contribution",
    ];

    public static TheoryData<decimal, int> Years
    {
        get
        {
            var data = new TheoryData<decimal, int>();
            foreach (var pay in new[] { 0m, 0.00m, 1_000m, 3_000m, 6_000m, 9_000m, 10_000m, 11_000m, 12_000m, 14_000m, 20_000m, 50_000m })
                for (var prepayments = 0; prepayments < Prepayments.Length; prepayments++)
                    data.Add(pay, prepayments);
            return data;
        }
    }

    // Nothing withheld as 0, 0.00 or 0.0000, or what the key formula withholds.
    private static readonly TaxPrepayments[] Prepayments =
    [
        new(0m, AdvancePayments.None), new(0.00m, AdvancePayments.None), new(0.0000m, AdvancePayments.None), TaxPrepayments.AsWithheld, TaxPrepayments.None,
    ];

    [Theory]
    [MemberData(nameof(Years))]
    public void Run_amountsOfNothing_areAPlainZeroUnlessTheyComeBack(decimal pay, int prepayments)
    {
        var year = Run(Scenario(directorPay: pay, taxPrepayments: Prepayments[prepayments]));

        var assessment = year.DirectorPay.Assessment;
        var negative = year.Steps.Where(step => step.Amount == 0m && decimal.IsNegative(step.Amount) && !ComingBack.Contains(step.RuleKey)).Select(step => step.RuleKey)
            .Concat(assessment.Balance == 0m && decimal.IsNegative(assessment.Balance) ? ["assessment balance"] : [])
            .Concat(assessment.FinalTax == 0m && decimal.IsNegative(assessment.FinalTax) ? ["assessment final tax"] : []);

        string.Join(", ", negative).ShouldBe("");
    }

    // Every lever at nothing besides: the car's, the royalty's, the rent's, the vouchers' and the allowances'.
    [Fact]
    public void Run_everyLeverGivenNothing_hasNoStepOfNothingThatIsANegativeZero()
    {
        var year = Run(Scenario(directorPay: 0.00m, taxPrepayments: new TaxPrepayments(0.00m, AdvancePayments.None),
            vouchers: new VoucherGrants(0, 0.00m, 0.00m), allowances: new ExpenseAllowances(0.00m, 0.00m, 0.00m, 0.00m), rent: new HomeOfficeRent(0.00m, 0.12m, 0.00m),
            car: new CarFacts(0.00m, new DateOnly(2026, 1, 10), 0.00m, false, 0.00m), royalty: new CopyrightRoyalty(0.00m)));

        year.Steps.Where(step => step.Amount == 0m && decimal.IsNegative(step.Amount) && !ComingBack.Contains(step.RuleKey)).Select(step => step.RuleKey).ShouldBeEmpty();
    }
}
