using System.Globalization;
using System.Resources;
using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's shared components build resource keys from origins, drains and the engine's error
// keys. A key without a text in its .resx would show on screen as the key itself.
public class RiverTextTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResourceManager OriginChipResources = new("Netto.Web.Client.River.OriginChip", typeof(OriginChip).Assembly);

    private static readonly ResourceManager DrainListResources = new("Netto.Web.Client.River.DrainList", typeof(DrainList).Assembly);

    private static readonly ResourceManager RefusedMessageResources = new("Netto.Web.Client.River.RefusedMessage", typeof(RefusedMessage).Assembly);

    private static readonly ResourceManager CorporateTaxCardResources = new("Netto.Web.Client.River.CorporateTaxCard", typeof(CorporateTaxCard).Assembly);

    private static readonly ResourceManager DirectorPayCardResources = new("Netto.Web.Client.River.DirectorPayCard", typeof(DirectorPayCard).Assembly);

    private static readonly ResourceManager DividendCardResources = new("Netto.Web.Client.River.DividendCard", typeof(DividendCard).Assembly);

    private static readonly ResourceManager SplitCardResources = new("Netto.Web.Client.River.SplitCard", typeof(SplitCard).Assembly);

    private static readonly ResourceManager VoucherCardResources = new("Netto.Web.Client.River.VoucherCard", typeof(VoucherCard).Assembly);

    private static readonly ResourceManager AllowanceCardResources = new("Netto.Web.Client.River.AllowanceCard", typeof(AllowanceCard).Assembly);

    private static readonly ResourceManager RentCardResources = new("Netto.Web.Client.River.RentCard", typeof(RentCard).Assembly);

    [Fact]
    public void OriginChipResx_everyOrigin_hasAWordAndAHint()
    {
        var keys = Enum.GetValues<InputOrigin>().SelectMany(origin => new[] { OriginChipText.Origin(origin), OriginChipText.Hint(origin) });

        Missing(OriginChipResources, keys).ShouldBeEmpty();
    }

    // The example, with a reserve so every drain has an amount, every kind of voucher and every
    // allowance, is every drain a card can list, the director's contributions paid by either; the
    // rent, which the office allowance excludes, on a year of its own, beside a low pay with nothing
    // withheld so it takes away part of the credit and moves the surcharge.
    [Theory]
    [InlineData(ContributionsPaidBy.Director)]
    [InlineData(ContributionsPaidBy.Company)]
    public void DrainListResx_everyDrainOfEveryCard_hasALabelAndADestination(ContributionsPaidBy paidBy)
    {
        var inputs = RiverInputs.Example.WithReserve(15_000m).WithContributionsPaidBy(paidBy)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
            .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m);
        var run = inputs.Run();
        var year = run.Year.ShouldNotBeNull();
        var rentYear = RiverInputs.Example.WithContributionsPaidBy(paidBy).WithDirectorPay(27_000m).WithTaxWithheld(0m)
            .WithCadastralIncome(400m).WithShareLet(1m).WithRent(3_333.33m).Run().Year.ShouldNotBeNull();
        var drains = VoucherView.From(year).Drains
            .Concat(AllowanceView.From(year).Drains)
            .Concat(RentView.From(rentYear).Drains)
            .Concat(DirectorPayView.From(inputs, run.Pay.ShouldNotBeNull()).Drains)
            .Concat(CorporateTaxView.From(year).Drains)
            .Concat(DividendView.From(year).Drains)
            .ToList();

        Missing(DrainListResources, drains.Select(drain => DrainListText.Drain(drain.Key)).Concat(drains.Select(DrainListText.Destination)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void RefusedMessageResx_everyErrorTheRiverCanReturn_hasAText()
    {
        var errors = new[]
        {
            YearSimulation.IncomeYearNotSupported, YearSimulation.RevenueNegative, YearSimulation.RunningCostsNegative,
            YearSimulation.ReserveNegative, YearSimulation.EarlierProfitsNegative, YearSimulation.AdvancePaymentNegative,
            GrossToNet.GrossPayNegative, GrossToNet.MunicipalRateOutOfRange, GrossToNet.TaxWithheldNegative, GrossToNet.TaxAdvancePaymentNegative,
            VoucherCalculator.MealVoucherDaysNegative, VoucherCalculator.MealVoucherDaysAboveYear, VoucherCalculator.EcoVouchersNegative,
            VoucherCalculator.EcoVouchersAboveMaximum, VoucherCalculator.SportCultureVouchersNegative, VoucherCalculator.SportCultureVouchersAboveMaximum,
            AllowanceCalculator.OfficeAllowanceNegative, AllowanceCalculator.OfficeAllowanceAboveMaximum, AllowanceCalculator.InternetAllowanceNegative,
            AllowanceCalculator.InternetAllowanceAboveMaximum, AllowanceCalculator.ComputerAllowanceNegative, AllowanceCalculator.ComputerAllowanceAboveMaximum,
            AllowanceCalculator.PhoneCostsNegative,
            RentCalculator.CadastralIncomeNegative, RentCalculator.ShareLetOutOfRange, RentCalculator.RentNegative, RentCalculator.RentAboveCap,
            RentCalculator.RentWithOfficeAllowance,
        };

        Missing(RefusedMessageResources, errors.Select(RefusedMessageText.Error)).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everyReducedRateCondition_hasAText()
    {
        Missing(CorporateTaxCardResources, Enum.GetValues<ReducedRateCondition>().Select(CorporateTaxCardText.Condition)).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everySurchargeState_hasAText()
    {
        Missing(CorporateTaxCardResources, Enum.GetValues<SurchargeState>().Select(CorporateTaxCardText.Surcharge)).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_everyTaxSurchargeState_hasAText()
    {
        Missing(DirectorPayCardResources, Enum.GetValues<TaxSurchargeState>().Select(DirectorPayCardText.Surcharge)).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_everyPayerOfTheContributions_hasAText()
    {
        Missing(DirectorPayCardResources, Enum.GetValues<ContributionsPaidBy>().Select(DirectorPayCardText.PaidBy)).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everyFinancialYear_hasAText()
    {
        Missing(CorporateTaxCardResources, Enum.GetValues<FinancialYear>().Select(CorporateTaxCardText.FinancialYear)).ShouldBeEmpty();
    }

    [Fact]
    public void DividendCardResx_everyWithholdingCategory_hasAText()
    {
        Missing(DividendCardResources, Enum.GetValues<WithholdingCategory>().Select(DividendCardText.Category)).ShouldBeEmpty();
    }

    // Each way the best pay can stand against the reduced rate, with or without a pay on the other
    // side that keeps the reserve.
    [Fact]
    public void SplitCardResx_everyWayOfWeighingTheRate_hasAText()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var weighings = Enum.GetValues<RateChoice>()
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) });

        Missing(SplitCardResources, weighings.Select(SplitCardText.Rate)).ShouldBeEmpty();
    }

    [Fact]
    public void VoucherCardResx_everyConditionOfTheVouchers_hasAText()
    {
        Missing(VoucherCardResources, VoucherRules.IncomeYear2026.Conditions.Select(condition => VoucherCardText.Condition(condition.Key))).ShouldBeEmpty();
    }

    [Fact]
    public void AllowanceCardResx_everyConditionOfTheAllowances_hasAText()
    {
        Missing(AllowanceCardResources, AllowanceRules.IncomeYear2026.Conditions.Select(condition => AllowanceCardText.Condition(condition.Key)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void RentCardResx_everyConditionOfTheRent_hasAText()
    {
        Missing(RentCardResources, RentRules.IncomeYear2026.Conditions.Select(condition => RentCardText.Condition(condition.Key))).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(ResourceManager resources, IEnumerable<string> keys) =>
        keys.Distinct().Where(key => resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
