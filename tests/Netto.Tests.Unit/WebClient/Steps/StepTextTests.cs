using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.WebClient.Steps;

// StepDetails.razor builds resource keys from the engine's keys. A key without a text in
// StepDetails.resx would show on screen as the key itself, so every step a card lists for 2026,
// with every amount it used, must have one, with a municipality's rate and with a rate the user
// entered.
public class StepTextTests
{
    // The name IStringLocalizer<StepDetails> reads Steps/StepDetails.resx under.
    private static readonly ResxText Resources = ResxText.For<StepDetails>();

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.IncomeYear2026.Entered(0.07m),
    };

    public static TheoryData<string> RateOrigins => [.. RatesByOrigin.Keys];

    [Theory]
    [MemberData(nameof(RateOrigins))]
    public void StepDetailsResx_everyKeyOfTheDirectorPaySteps_hasAText(string rateOrigin)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin], TaxPrepayments.None);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The river's steps: the company's, the director pay's and the dividend's, nothing kept so a
    // dividend is paid, under each withholding rate the user can declare.
    [Theory]
    [InlineData(WithholdingCategory.Standard)]
    [InlineData(WithholdingCategory.VvprbisSecondYear)]
    [InlineData(WithholdingCategory.VvprbisThirdYearOnwards)]
    public void StepDetailsResx_everyKeyOfTheYearsSteps_hasAText(WithholdingCategory category)
    {
        var scenario = Unprepaid(50_000m, category);

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // Every kind of voucher, and what the company cannot deduct of them.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheVouchersSteps_hasAText()
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with { Vouchers = new VoucherGrants(220, 250m, 100m) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // Every allowance, with the office's three monthly maxima among their amounts.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheAllowancesSteps_hasAText()
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with { Allowances = new ExpenseAllowances(1_938.44m, 240m, 240m, 600m) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The rent's steps, with the amounts of the cap, the floor, the charges and the taxes it moves.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheRentsSteps_hasAText()
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with { Rent = new HomeOfficeRent(1_200m, 0.12m, 1_380m) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The car's steps, with every age band a benefit can use, both add-back rates and a contribution.
    [Theory]
    [InlineData(2026, false)]
    [InlineData(2025, true)]
    [InlineData(2024, false)]
    [InlineData(2023, true)]
    [InlineData(2022, false)]
    [InlineData(2018, true)]
    public void StepDetailsResx_everyKeyOfTheCarsSteps_hasAText(int registeredIn, bool privateEnergy)
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with
        {
            Car = new CarFacts(50_000m, new DateOnly(registeredIn, 6, 1), 8_000m, privateEnergy, 600m),
        };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // A car that emits CO2: its percentage against each fuel's reference, held to the minimum or the
    // maximum, without CO2 data, or a fake hybrid without a matching car; its deduction by each
    // period, with each coefficient, each bound and the texts' rates.
    public static TheoryData<CarFacts> FuelCars => new()
    {
        FuelCar with { Engine = CarEngine.Petrol, Co2 = 120 },
        FuelCar with { Engine = CarEngine.Diesel, Co2 = 250 },
        FuelCar with { Engine = CarEngine.Diesel, Co2 = 150 },
        FuelCar with { Engine = CarEngine.Lpg, NoCo2Data = true },
        FuelCar with { Engine = CarEngine.Lpg, NoCo2Data = true, Acquired = new DateOnly(2024, 3, 1) },
        FuelCar with { Engine = CarEngine.PetrolPlugInHybrid, Co2 = 20 },
        FuelCar with { Engine = CarEngine.DieselPlugInHybrid, Co2 = 40, Acquired = new DateOnly(2024, 3, 1), FakeHybrid = true, NoMatchingCar = true },
        FuelCar with { Engine = CarEngine.DieselHybrid, Co2 = 110 },
        FuelCar with { Engine = CarEngine.NaturalGas, Co2 = 100, SmallNaturalGas = true },
        FuelCar with { Engine = CarEngine.Diesel, Co2 = 130, Acquired = new DateOnly(2026, 3, 1) },
    };

    private static readonly CarFacts FuelCar = new(50_000m, new DateOnly(2026, 1, 1), 8_000m, false, 600m);

    [Theory]
    [MemberData(nameof(FuelCars))]
    public void StepDetailsResx_everyKeyOfAFuelCarsSteps_hasAText(CarFacts car)
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with { Car = car };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The royalty's steps, within its limits beside a low pay with nothing withheld, so it takes away
    // part of the credit and moves the surcharge, and beyond them.
    [Theory]
    [InlineData(27_000, 8_000)]
    [InlineData(30_000, 20_000)]
    public void StepDetailsResx_everyKeyOfTheRoyaltysSteps_hasAText(int pay, int royalty)
    {
        var scenario = Unprepaid(pay, WithholdingCategory.Standard) with { Copyright = new Netto.Core.Copyright.CopyrightRoyalty(royalty) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // A royalty that follows the pay, whose step cites the amounts that measured what a ruling accepts.
    [Fact]
    public void StepDetailsResx_everyKeyOfAFollowingRoyaltysSteps_hasAText()
    {
        var scenario = Unprepaid(50_000m, WithholdingCategory.Standard) with
        {
            Copyright = new Netto.Core.Copyright.CopyrightRoyalty(0m) { CreativeShare = 0.75m, FollowsPay = true },
        };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The pay slip lists two steps of its own beside the engine's: the month's withholding, with the
    // key formula's lines and the annex's amounts, and the balance on the assessment.
    [Theory]
    [InlineData(ContributionsPaidBy.Director)]
    [InlineData(ContributionsPaidBy.Company)]
    public void StepDetailsResx_everyKeyOfTheWithholdingAndTheBalance_hasAText(ContributionsPaidBy paidBy)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld, paidBy);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        MissingTexts([view.WithholdingDetail, view.Balance.Detail]).ShouldBeEmpty();
    }

    // The company of the river's example with nothing prepaid and nothing withheld, so the surcharges are among its steps.
    private static Scenario Unprepaid(decimal directorPay, WithholdingCategory category) =>
        Scenario(directorPay: directorPay, category: category, advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.None);

    private static IEnumerable<string> MissingTexts(IEnumerable<Step> steps) => MissingTexts(steps.Select(StepDetail.From));

    private static IEnumerable<string> MissingTexts(IEnumerable<StepDetail> details) =>
        Resources.Missing(details.SelectMany(detail => new[] { StepText.Title(detail.RuleKey), StepText.Rule(detail.RuleKey) }
            .Concat(detail.Lines.Select(line => StepText.Line(line.Key)))
            .Concat(detail.Sources.SelectMany(group => group.Amounts).Select(amount => StepText.Amount(amount.Key)))));
}
