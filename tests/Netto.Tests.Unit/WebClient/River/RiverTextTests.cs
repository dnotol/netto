using System.Globalization;
using System.Resources;
using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
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

    private static readonly ResourceManager ConditionListResources = new("Netto.Web.Client.River.ConditionList", typeof(ConditionList).Assembly);

    private static readonly ResourceManager CompositionMenuResources = new("Netto.Web.Client.River.CompositionMenu", typeof(CompositionMenu).Assembly);

    private static readonly ResourceManager TakenOutNoticeResources = new("Netto.Web.Client.River.TakenOutNotice", typeof(TakenOutNotice).Assembly);

    private static readonly ResourceManager InfoBubbleResources = new("Netto.Web.Client.River.InfoBubble", typeof(InfoBubble).Assembly);

    private static readonly ResourceManager RentCardResources = new("Netto.Web.Client.River.RentCard", typeof(RentCard).Assembly);

    // The conditions of every lever the river shows, which the shared list words through one .resx.
    private static readonly IReadOnlyList<LeverCondition> LeverConditions =
    [
        .. VoucherRules.IncomeYear2026.Conditions,
        .. AllowanceRules.IncomeYear2026.Conditions,
        .. RentRules.IncomeYear2026.Conditions,
    ];

    // The vouchers in the plan with each next lever, and the split after them.
    private static readonly PlanComposition[] Compositions = [new(true, true, true), new(true, false, true), new(true, false, false)];

    [Fact]
    public void OriginChipResx_everyOrigin_hasAWordAndAHint()
    {
        var keys = Enum.GetValues<InputOrigin>().SelectMany(origin => new[] { OriginChipText.Origin(origin), OriginChipText.Hint(origin) });

        Missing(OriginChipResources, keys).ShouldBeEmpty();
    }

    // The example, with a reserve so every drain has an amount, every kind of voucher and every
    // allowance, is every drain a card can list, the director's contributions paid by either; the
    // rent, which the office allowance excludes, on a year of its own, beside a low pay with nothing
    // withheld so it takes away part of the credit and moves the surcharge. What a lever leaves goes on
    // to the next lever in the plan or the split, so the levers' drains are listed for each.
    [Theory]
    [InlineData(ContributionsPaidBy.Director)]
    [InlineData(ContributionsPaidBy.Company)]
    public void DrainListResx_everyDrainOfEveryCard_hasALabelAndADestination(ContributionsPaidBy paidBy)
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).WithReserve(15_000m).WithContributionsPaidBy(paidBy)
            .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
            .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m);
        var run = inputs.Run();
        var year = run.Year.ShouldNotBeNull();
        var rentYear = RiverInputs.Example.AddToPlan(OptionalModule.Rent).WithContributionsPaidBy(paidBy).WithDirectorPay(27_000m).WithTaxWithheld(0m)
            .WithCadastralIncome(400m).WithShareLet(1m).WithRent(3_333.33m).Run().Year.ShouldNotBeNull();
        var drains = Compositions.SelectMany(composition => VoucherView.From(year, composition).Drains.Concat(AllowanceView.From(year, composition).Drains))
            .Concat(RentView.From(rentYear).Drains)
            .Concat(DirectorPayView.From(inputs, run.Pay.ShouldNotBeNull()).Drains)
            .Concat(CorporateTaxView.From(year).Drains)
            .Concat(DividendView.From(year).Drains)
            .ToList();

        Missing(DrainListResources, drains.Select(drain => DrainListText.Drain(drain.Key)).Concat(drains.Select(DrainListText.Destination)))
            .ShouldBeEmpty();
    }

    // A drain's bubble cites each text its step follows; the amounts under it are worded by
    // StepDetails.resx (StepTextTests).
    [Fact]
    public void DrainListResx_textADrainsBubbleCites_hasAText()
    {
        Missing(DrainListResources, [DrainListText.Source]).ShouldBeEmpty();
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

    // Every way the lines of Director pay's disclosures read: either payer, the default rate, a
    // municipality's, a rate of one's own over it, a starter or not, the pay slip's withholding with
    // and without a pay slip, an amount withheld entered, and advance payments or none.
    [Fact]
    public void DirectorPayCardResx_everyPhraseOfItsSettingsLines_hasAText()
    {
        var example = RiverInputs.Example;
        var municipality = MunicipalRates.IncomeYear2026.Municipalities[0];
        RiverInputs[] variations =
        [
            example,
            example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithStarter(true).WithTaxWithheld(1_000m).WithTaxAdvancePayment(0, 1_000m),
            example.WithMunicipality(example.Municipality.Pick(municipality)).WithReserve(-1m),
            example.WithMunicipality(example.Municipality.Pick(municipality).Enter(0.09m)),
        ];
        var phrases = variations.Select(inputs => DirectorPaySettingsView.From(inputs, inputs.Run()))
            .SelectMany(view => view.Situation.Concat(view.Prepaid));

        Missing(DirectorPayCardResources, phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
    }

    // Every way the lines of Corporate tax's disclosures read: a small company or not, in each of its
    // financial years, advance payments or none, and a surcharge left or none.
    [Fact]
    public void CorporateTaxCardResx_everyPhraseOfItsSettingsLines_hasAText()
    {
        RiverInputs[] variations =
        [
            RiverInputs.Example.WithSmallCompany(false).WithAdvancePayment(0, 7_500m),
            .. Enum.GetValues<FinancialYear>().Select(RiverInputs.Example.WithFinancialYear),
        ];
        var phrases = variations.Select(inputs => CorporateTaxSettingsView.From(inputs, inputs.Run()))
            .SelectMany(view => view.Company.Concat(view.Payments));

        Missing(CorporateTaxCardResources, phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
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
    public void ConditionListResx_everyConditionOfEveryLever_hasAText()
    {
        Missing(ConditionListResources, LeverConditions.Select(condition => ConditionListText.Condition(condition.Key))).ShouldBeEmpty();
    }

    // One text per key: a lever reusing another's key would show the other lever's words.
    [Fact]
    public void ConditionListResx_everyConditionOfEveryLever_hasAKeyOfItsOwn()
    {
        LeverConditions.Select(condition => condition.Key).ShouldBeUnique();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ConditionListResx_headingForAnyCount_hasAText(int count)
    {
        Missing(ConditionListResources, [ConditionListText.Heading(count)]).ShouldBeEmpty();
    }

    [Fact]
    public void CompositionMenuResx_everyOptionalModule_hasATitleADescriptionAndWhatItBrings()
    {
        var keys = PlanComposition.Modules.SelectMany(module =>
            new[] { CompositionMenuText.Title(module), CompositionMenuText.Description(module), CompositionMenuText.Brings(module) });

        Missing(CompositionMenuResources, keys).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void CompositionMenuResx_buttonLabelForAnyCount_hasAText(int count)
    {
        Missing(CompositionMenuResources, [CompositionMenuText.ButtonLabel(count)]).ShouldBeEmpty();
    }

    [Fact]
    public void TakenOutNoticeResx_everyOptionalModule_hasAText()
    {
        Missing(TakenOutNoticeResources, PlanComposition.Modules.Select(TakenOutNoticeText.TakenOut)).ShouldBeEmpty();
    }

    // An info bubble's (i) is named by what it says more about.
    [Fact]
    public void InfoBubbleResx_nameOfItsIcon_hasAText()
    {
        Missing(InfoBubbleResources, [InfoBubbleText.MoreAbout]).ShouldBeEmpty();
    }

    // The reduced corporate rate is a term of art: every sentence weighing it marks it, for it to open
    // a bubble that says what it means.
    [Fact]
    public void SplitCardResx_everySentenceWeighingTheRate_marksTheReducedRate()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var keys = new[] { RateChoice.KeepsIt, RateChoice.GivesItUp }
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) })
            .Select(SplitCardText.Rate);

        Terms(SplitCardResources, keys).ShouldAllBe(term => term == "reduced corporate rate");
    }

    [Fact]
    public void SplitCardResx_reducedRate_saysWhatItMeans()
    {
        Missing(SplitCardResources, [SplitCardText.ReducedRateTerm]).ShouldBeEmpty();
    }

    [Fact]
    public void DividendCardResx_whatTheRatesAreFor_marksVvprbis()
    {
        Terms(DividendCardResources, ["CategoryHint"]).ShouldBe(["VVPRbis shares"]);
    }

    [Fact]
    public void DividendCardResx_vvprbis_saysWhatItMeans()
    {
        Missing(DividendCardResources, [DividendCardText.VvprbisTerm]).ShouldBeEmpty();
    }

    [Fact]
    public void RentCardResx_theCapAndWhatTheRentIsTaxedOn_markTheirTerms()
    {
        Terms(RentCardResources, ["Cap", "TaxedOnTheRent", "TaxedOnTheFloor"]).ShouldBe(["cadastral income", "flat charges", "flat charges"]);
    }

    [Fact]
    public void RentCardResx_cadastralIncomeAndFlatCharges_sayWhatTheyMean()
    {
        Missing(RentCardResources, [RentCardText.CadastralIncomeTerm, RentCardText.FlatChargesTerm]).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_withholdingDerivedFromThePaySlip_marksTheKeyFormula()
    {
        Terms(DirectorPayCardResources, ["WithheldDerived"]).ShouldBe(["key formula"]);
    }

    [Fact]
    public void DirectorPayCardResx_keyFormula_saysWhatItMeans()
    {
        Missing(DirectorPayCardResources, [DirectorPayCardText.KeyFormulaTerm]).ShouldBeEmpty();
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(ResourceManager resources, IEnumerable<string> keys) =>
        [.. keys.Select(key => resources.GetString(key, CultureInfo.InvariantCulture) is { } text ? TermText.Split(text).Term : null)];

    private static IEnumerable<string> Missing(ResourceManager resources, IEnumerable<string> keys) =>
        keys.Distinct().Where(key => resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
