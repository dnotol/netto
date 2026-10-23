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
    };

    [Theory]
    [MemberData(nameof(TaxedOnMore))]
    public void Taxed_partsAndTheGross_addUpToTheTaxablePay(string _, RiverInputs inputs)
    {
        var pay = inputs.Run().Pay.ShouldNotBeNull();

        var lines = DirectorPayFigures.Taxed(pay);

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
        var inputs = ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company);

        Taxed(inputs).Select(line => line.Help).ShouldBe(["PaidByHint", "Help_BenefitInKind", "Help_RequalifiedPay", null]);
    }

    // Nothing withheld: 486.57 on the pay's tax, among the pay's drains, and 14,317.50 by 21 December
    // brings it under the minimum.
    [Fact]
    public void Surcharge_nothingWithheld_isWhatMoreByTheLastDeadlineLeavesNoneDue()
    {
        var lines = Surcharge(RiverInputs.Example.WithTaxWithheld(0m));

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe([("Surcharge_Due", 14_317.50m, null)]),
            () => lines[0].Args.ShouldBe(["21 December 2026"]));
    }

    // With no surcharge due, the card says why in a sentence: no line.
    public static TheoryData<string, RiverInputs> NoneDue => new()
    {
        { "under the minimum", RiverInputs.Example },
        { "covered", RiverInputs.Example.WithTaxWithheld(20_000m) },
        { "exempt", RiverInputs.Example.WithStarter(true) },
        { "no tax", RiverInputs.Example.WithDirectorPay(0m) },
    };

    [Theory]
    [MemberData(nameof(NoneDue))]
    public void Surcharge_noneDue_drawsNoLine(string _, RiverInputs inputs)
    {
        Surcharge(inputs).ShouldBeEmpty();
    }

    private static IReadOnlyList<FigureLine> Taxed(RiverInputs inputs) => DirectorPayFigures.Taxed(inputs.Run().Pay.ShouldNotBeNull());

    private static IReadOnlyList<FigureLine> Surcharge(RiverInputs inputs) =>
        DirectorPayFigures.Surcharge(DirectorPayView.From(inputs, inputs.Run().Pay.ShouldNotBeNull()));

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
