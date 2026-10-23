using Netto.Core.CompanyCar;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Corporate tax's figures as lines (CorporateTaxFigures): what its taxable income adds back to the profit
// the split leaves the company. The example adds nothing back. The vouchers' cards grant 220 days of meal
// vouchers, 250 of eco and 100 of sport/culture vouchers, of which the company cannot deduct 1,430.20
// (ModuleViewsTests). YearSimulationCarTests' electric car adds back 291.43 of its benefit; its petrol
// car, leased in January 2026, 688.50 of its benefit and the 4,950 of its costs the company cannot
// deduct.
public class CorporateTaxFiguresTests
{
    private static readonly RiverInputs AllVouchers = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers)
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    private static readonly RiverInputs PetrolCar = ElectricCar.WithCatalogueValue(45_000m).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));

    // Nothing added back: the taxable income is the profit the summary says.
    [Fact]
    public void Of_example_drawsNoLines()
    {
        Of(RiverInputs.Example).ShouldBeEmpty();
    }

    [Fact]
    public void Of_vouchers_isTheTaxableIncomeThenWhatTheCompanyCannotDeductOfThem()
    {
        Lines(Of(AllVouchers)).ShouldBe([("Figure_TaxableIncome", 49_120.00m, null), ("Figure_AddedBackVouchers", 1_430.20m, null)]);
    }

    [Fact]
    public void Of_electricCar_isTheTaxableIncomeThenTheShareOfItsBenefitAddedBack()
    {
        Lines(Of(ElectricCar)).ShouldBe([("Figure_TaxableIncome", 42_291.43m, null), ("Figure_AddedBackCarBenefit", 291.43m, null)]);
    }

    // An electric car beside the vouchers: each added back in turn, and no costs, deducted in full.
    [Fact]
    public void Of_electricCarAndVouchers_addsBackBothAndNoCosts()
    {
        Of(ElectricCar.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)).Select(line => (line.Key, line.Amount))
            .ShouldBe([("Figure_TaxableIncome", 42_291.43m), ("Figure_AddedBackVouchers", 250m), ("Figure_AddedBackCarBenefit", 291.43m)]);
    }

    // A fuel car the company cannot deduct in full: the costs beyond the benefit beside its add-back.
    [Fact]
    public void Of_petrolCar_addsTheCostsItCannotDeductBesideItsBenefit()
    {
        Lines(Of(PetrolCar))
            .ShouldBe([("Figure_TaxableIncome", 46_638.50m, null), ("Figure_AddedBackCarCosts", 4_950m, null), ("Figure_AddedBackCarBenefit", 688.50m, null)]);
    }

    [Fact]
    public void Of_carAndVouchers_addsBackEachInTurn()
    {
        Of(PetrolCar.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)).Select(line => (line.Key, line.Amount))
            .ShouldBe([("Figure_TaxableIncome", 46_638.50m), ("Figure_AddedBackVouchers", 250m), ("Figure_AddedBackCarCosts", 4_950m), ("Figure_AddedBackCarBenefit", 688.50m)]);
    }

    // Why each part is added back is a rule: its bubble says it, the taxable income needing none.
    [Fact]
    public void Of_carAndVouchers_explainEachPartAddedBackInItsBubble()
    {
        Of(PetrolCar.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)).Select(line => line.Help)
            .ShouldBe([null, "Help_AddedBackVouchers", "Help_AddedBackCarCosts", "Help_AddedBackCarBenefit"]);
    }

    private static IReadOnlyList<FigureLine> Of(RiverInputs inputs) => CorporateTaxFigures.Of(CorporateTaxView.From(inputs.Run().Year.ShouldNotBeNull()));

    private static List<(string Key, decimal Amount, decimal? Share)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, line.Share))];
}
