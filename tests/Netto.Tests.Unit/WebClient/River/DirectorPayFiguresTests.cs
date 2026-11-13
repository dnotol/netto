using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Director pay's figures as lines (DirectorPayFigures), on the river's example (ModuleViewsTests): 50,000
// of pay taxed on its gross alone, its tax withheld as the key formula says, which leaves a surcharge
// under the minimum. YearSimulationCarTests' electric car adds a benefit in kind of 1,714.29; a royalty
// of 30,000 beside the pay passes its limits by 6,000, taxed as pay; the company paying the director's
// 9,942.52 of contributions makes them pay too.
public class DirectorPayFiguresTests
{
    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    private static readonly RiverInputs RoyaltyBeyondItsLimits = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

    // Nothing withheld on the pay, which leaves a surcharge due on its tax.
    private static readonly RiverInputs NothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);

    // A day with one advance payment still to come, the last.
    private static readonly DateOnly On23October = new(2026, 10, 23);

    // The pay taxed on its gross alone: the gross from the split says it all.
    [Fact]
    public void Taxed_example_drawsNoLines()
    {
        Taxed(RiverInputs.Example).ShouldBeEmpty();
    }

    [Fact]
    public void Taxed_companyCar_isItsBenefitInKindThenTheTaxablePay()
    {
        Lines(Taxed(ElectricCar)).ShouldBe([("Figure_BenefitInKind", 1_714.29m, null), ("Figure_TaxablePay", 51_714.29m, null)]);
    }

    [Fact]
    public void Taxed_royaltyBeyondItsLimits_isThePartTaxedAsPayThenTheTaxablePay()
    {
        Lines(Taxed(RoyaltyBeyondItsLimits)).ShouldBe([("Figure_RequalifiedPay", 6_000m, null), ("Figure_TaxablePay", 56_000m, null)]);
    }

    [Fact]
    public void Taxed_companyPaysTheContributions_isThemThenTheTaxablePay()
    {
        Lines(Taxed(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company)))
            .ShouldBe([("Figure_CompanyContributions", 9_942.52m, null), ("Figure_TaxablePay", 59_942.52m, null)]);
    }

    // With the fund's fee at 3.05%, the company pays it with the contributions, pay too: 303.24 on the
    // 9,942.52 (GrossToNetFundFeeTests), a line of its own after them.
    [Fact]
    public void Taxed_companyPaysTheContributionsAndTheFee_isThemThenTheFeeThenTheTaxablePay()
    {
        Lines(Taxed(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m)))
            .ShouldBe([("Figure_CompanyContributions", 9_942.52m, null), ("Figure_CompanyFundFee", 303.24m, null), ("Figure_TaxablePay", 60_245.76m, null)]);
    }

    // The director paying the fee deducts it, but is not taxed on more than the gross.
    [Fact]
    public void Taxed_directorPaysTheFee_drawsNoLines()
    {
        Taxed(RiverInputs.Example.WithFundFeeRate(0.0305m)).ShouldBeEmpty();
    }

    // The contributions the company pays come first, beside the gross they are paid with.
    public static TheoryData<string, RiverInputs, string[]> CompanyPays => new()
    {
        { "beside a company car's benefit", ElectricCar.WithContributionsPaidBy(ContributionsPaidBy.Company),
            ["Figure_CompanyContributions", "Figure_BenefitInKind", "Figure_TaxablePay"] },
        { "beside a royalty's part beyond its limits", RoyaltyBeyondItsLimits.WithContributionsPaidBy(ContributionsPaidBy.Company),
            ["Figure_CompanyContributions", "Figure_RequalifiedPay", "Figure_TaxablePay"] },
    };

    [Theory]
    [MemberData(nameof(CompanyPays))]
    public void Taxed_companyPaysTheContributions_namesThemBeforeTheOtherParts(string _, RiverInputs inputs, string[] keys)
    {
        Taxed(inputs).Select(line => line.Key).ShouldBe(keys);
    }

    // The gross and the parts drawn under it add up to the taxable pay, whoever pays the contributions.
    public static TheoryData<string, RiverInputs> TaxedOnMore => new()
    {
        { "the company paying the contributions", RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "a company car", ElectricCar },
        { "the company paying beside a company car", ElectricCar.WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "the company paying beside a royalty beyond its limits", RoyaltyBeyondItsLimits.WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "all three", ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company) },
        { "all three with the fund's fee", ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company)
            .WithFundFeeRate(0.0305m) },
    };

    [Theory]
    [MemberData(nameof(TaxedOnMore))]
    public void Taxed_partsAndTheGross_addUpToTheTaxablePay(string _, RiverInputs inputs)
    {
        var pay = inputs.Run().Pay.ShouldNotBeNull();

        var lines = DirectorPayFigures.Taxed(pay, inputs.Run().Year);

        (pay.GrossPay + lines.SkipLast(1).Sum(line => line.Amount), lines[^1].Key).ShouldBe((pay.TaxablePay, "Figure_TaxablePay"));
    }

    [Fact]
    public void Taxed_carAndRoyaltyBeyondItsLimits_isBothThenTheTaxablePay()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

        Taxed(inputs).Select(line => line.Key).ShouldBe(["Figure_BenefitInKind", "Figure_RequalifiedPay", "Figure_TaxablePay"]);
    }

    // Why the pay is taxed on more than it pays is a rule: each line's bubble says it, the taxable pay
    // needing none, the contributions the company pays in the words of the payer's field.
    [Fact]
    public void Taxed_everyPart_explainsItselfInItsBubble()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m);

        Taxed(inputs).Select(line => line.Help).ShouldBe(["PaidByHint", "FundFeeHint", "Help_BenefitInKind", "Help_RequalifiedPay", null]);
    }

    // The IT and phone the company provides all year, a PC, a mobile phone with its subscription and internet,
    // 216 (EquipmentCalculatorTests), beside the car's 1,714.29: each benefit by its source, the IT and phone's
    // first, as the river runs, then the taxable pay they make.
    [Fact]
    public void Taxed_equipmentAndCar_isEachBenefitByItsSource()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Equipment).WithPcs(1).WithMobilePhones(1).WithPhoneSubscriptions(1).WithInternet(true);

        Lines(Taxed(inputs)).ShouldBe([("Figure_EquipmentBenefit", 216m, null), ("Figure_BenefitInKind", 1_714.29m, null), ("Figure_TaxablePay", 51_930.29m, null)]);
    }

    private static IReadOnlyList<FigureLine> Taxed(RiverInputs inputs)
    {
        var run = inputs.Run();
        return DirectorPayFigures.Taxed(run.Pay.ShouldNotBeNull(), run.Year);
    }

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
