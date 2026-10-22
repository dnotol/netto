using System.Globalization;
using System.Resources;
using Netto.Core.Allowances;
using Netto.Core.Car;
using Netto.Core.CorporateTax;
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

namespace Netto.Tests.WebClient.Steps;

// StepDetails.razor builds resource keys from the engine's keys. A key without a text in
// StepDetails.resx would show on screen as the key itself, so every step a card lists for 2026,
// with every amount it used, must have one, with a municipality's rate and with a rate the user
// entered.
public class StepTextTests
{
    // The name IStringLocalizer<StepDetails> reads Steps/StepDetails.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Steps.StepDetails", typeof(StepDetails).Assembly);

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.Entered(0.07m),
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
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 0m, category);

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // Every kind of voucher, and what the company cannot deduct of them.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheVouchersSteps_hasAText()
    {
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.Standard) { Vouchers = new VoucherGrants(220, 250m, 100m) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // Every allowance, with the office's three monthly maxima among their amounts.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheAllowancesSteps_hasAText()
    {
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.Standard) { Allowances = new ExpenseAllowances(1_938.44m, 240m, 240m, 600m) };

        var (value, _) = YearSimulation.Run(scenario);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    // The rent's steps, with the amounts of the cap, the floor, the charges and the taxes it moves.
    [Fact]
    public void StepDetailsResx_everyKeyOfTheRentsSteps_hasAText()
    {
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.Standard) { Rent = new HomeOfficeRent(1_200m, 0.12m, 1_380m) };

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
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.Standard)
        {
            Car = new CompanyCar(50_000m, new DateOnly(registeredIn, 6, 1), 8_000m, privateEnergy, 600m),
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
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), TaxPrepayments.AsWithheld, paidBy);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        MissingTexts([view.WithholdingDetail, view.Balance.Detail]).ShouldBeEmpty();
    }

    private static IEnumerable<string> MissingTexts(IEnumerable<Step> steps) => MissingTexts(steps.Select(StepDetail.From));

    private static IEnumerable<string> MissingTexts(IEnumerable<StepDetail> details) =>
        details.SelectMany(detail => new[] { StepText.Title(detail.RuleKey), StepText.Rule(detail.RuleKey) }
                .Concat(detail.Lines.Select(line => StepText.Line(line.Key)))
                .Concat(detail.Sources.SelectMany(group => group.Amounts).Select(amount => StepText.Amount(amount.Key))))
            .Distinct()
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
