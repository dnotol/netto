using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's shared components build resource keys from origins, drains and the engine's error
// keys. A key without a text in its .resx would show on screen as the key itself.
public class RiverTextTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResxText OriginChipResources = ResxText.For<OriginChip>();

    private static readonly ResxText DrainListResources = ResxText.For<DrainList>();

    private static readonly ResxText RefusedMessageResources = ResxText.For<RefusedMessage>();

    private static readonly ResxText ModuleCardResources = ResxText.For<ModuleCard>();

    private static readonly ResxText ConditionListResources = ResxText.For<ConditionList>();

    private static readonly ResxText CompositionMenuResources = ResxText.For<CompositionMenu>();

    private static readonly ResxText TakenOutNoticeResources = ResxText.For<TakenOutNotice>();

    private static readonly ResxText InfoBubbleResources = ResxText.For<InfoBubble>();

    // The conditions of every lever the river shows, which the shared list words through one .resx.
    private static readonly IReadOnlyList<LeverCondition> LeverConditions =
    [
        .. VoucherRules.IncomeYear2026.Conditions,
        .. AllowanceRules.IncomeYear2026.Conditions,
        .. RentRules.IncomeYear2026.Conditions,
        .. CarRules.IncomeYear2026.Conditions,
        .. CopyrightRules.IncomeYear2026.Conditions,
        .. CompanyContributionRules.IncomeYear2026.Conditions,
    ];

    // The vouchers in the plan with each next lever, and the split after them.
    private static readonly PlanComposition[] Compositions =
        [new(true, true, true, false, false, false), new(true, false, true, false, false, false), new(true, false, false, false, false, false), new(true, true, true, true, false, false), new(true, true, true, true, true, true)];

    // What the example holds none of draws no chip, so it has no word.
    [Fact]
    public void OriginChipResx_everyOriginAChipNames_hasAWordAndAHint()
    {
        var keys = Enum.GetValues<InputOrigin>().Where(origin => origin != InputOrigin.None)
            .SelectMany(origin => new[] { OriginChipText.Origin(origin), OriginChipText.Hint(origin) });

        OriginChipResources.Missing(keys).ShouldBeEmpty();
    }

    // The example, with a reserve so every drain has an amount, every kind of voucher and every
    // allowance, is every drain a card can list, the director's contributions and their fund's fee paid by either; the
    // rent, which the office allowance excludes, on a year of its own, beside a low pay with nothing
    // withheld so it takes away part of the credit and moves the surcharge. What a lever leaves goes on
    // to the next lever in the plan or the split, so the levers' drains are listed for each.
    [Theory]
    [InlineData(ContributionsPaidBy.Director)]
    [InlineData(ContributionsPaidBy.Company)]
    public void DrainListResx_everyDrainOfEveryCard_hasALabelAndADestination(ContributionsPaidBy paidBy)
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).WithReserve(15_000m).WithContributionsPaidBy(paidBy)
            .WithFundFeeRate(0.0305m).WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
            .WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m);
        var run = inputs.Run();
        var year = run.Year.ShouldNotBeNull();
        var rentYear = RiverInputs.Example.AddToPlan(OptionalModule.Rent).WithContributionsPaidBy(paidBy).WithDirectorPay(27_000m).WithTaxWithheld(0m)
            .WithCadastralIncome(400m).WithShareLet(1m).WithRent(3_333.33m).Run().Year.ShouldNotBeNull();
        var carInputs = RiverInputs.Example.AddToPlan(OptionalModule.Rent).AddToPlan(OptionalModule.Car).WithContributionsPaidBy(paidBy)
            .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
            .WithCatalogueValue(50_000m).WithCarCosts(8_000m).WithCarContribution(600m);
        var carYear = carInputs.Run().Year.ShouldNotBeNull();
        var royaltyInputs = carInputs.AddToPlan(OptionalModule.Copyright).WithDirectorPay(27_000m).WithTaxWithheld(0m).WithRoyalty(20_000m);
        var royaltyRun = royaltyInputs.Run();
        var royaltyYear = royaltyRun.Year.ShouldNotBeNull();
        var drains = Compositions.SelectMany(composition => VoucherView.From(year, composition).Drains.Concat(AllowanceView.From(year, composition).Drains))
            .Concat(RentView.From(rentYear, PlanComposition.None.With(OptionalModule.Rent, true)).Drains)
            .Concat(RentView.From(carYear, carInputs.Composition).Drains)
            .Concat(CarView.From(carYear, carInputs.Composition).Drains)
            .Concat(CarView.From(royaltyYear, royaltyInputs.Composition).Drains)
            .Concat(CopyrightView.From(royaltyYear).Drains)
            .Concat(RentView.From(royaltyYear, royaltyInputs.Composition).Drains)
            .Concat(DirectorPayView.From(inputs, run.Pay.ShouldNotBeNull()).Drains)
            .Concat(DirectorPayView.From(royaltyInputs, royaltyRun.Pay.ShouldNotBeNull()).Drains)
            .Concat(CorporateTaxView.From(year).Drains)
            .Concat(DividendView.From(year).Drains)
            .ToList();

        DrainListResources.Missing(drains.Select(drain => DrainListText.Drain(drain.Key)).Concat(drains.Select(DrainListText.Destination)))
            .ShouldBeEmpty();
    }

    // A drain's bubble cites each text its step follows; the amounts under it are worded by
    // StepDetails.resx (StepTextTests).
    [Fact]
    public void DrainListResx_textADrainsBubbleCites_hasAText()
    {
        DrainListResources.Missing([DrainListText.Source]).ShouldBeEmpty();
    }

    // Every refusal the engine declares, read off it (EngineRefusals), the car card's private cost among them.
    [Fact]
    public void RefusedMessageResx_everyErrorTheRiverCanReturn_hasAText()
    {
        EngineRefusals.All.ShouldNotBeEmpty();
        RefusedMessageResources.Missing(EngineRefusals.All.Select(RefusedMessageText.Error)).ShouldBeEmpty();
    }

    [Fact]
    public void ConditionListResx_everyConditionOfEveryLever_hasAText()
    {
        ConditionListResources.Missing(LeverConditions.Select(condition => ConditionListText.Condition(condition.Key))).ShouldBeEmpty();
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
        ConditionListResources.Missing([ConditionListText.Heading(count)]).ShouldBeEmpty();
    }

    [Fact]
    public void CompositionMenuResx_everyOptionalModule_hasATitleADescriptionAndWhatItBrings()
    {
        var keys = PlanComposition.Modules.SelectMany(module =>
            new[] { CompositionMenuText.Title(module), CompositionMenuText.Description(module), CompositionMenuText.Brings(module) });

        CompositionMenuResources.Missing(keys).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void CompositionMenuResx_buttonLabelForAnyCount_hasAText(int count)
    {
        CompositionMenuResources.Missing([CompositionMenuText.ButtonLabel(count)]).ShouldBeEmpty();
    }

    [Fact]
    public void TakenOutNoticeResx_everyOptionalModule_hasAText()
    {
        TakenOutNoticeResources.Missing(PlanComposition.Modules.Select(TakenOutNoticeText.TakenOut)).ShouldBeEmpty();
    }

    // An info bubble's (i) is named by what it says more about.
    [Fact]
    public void InfoBubbleResx_nameOfItsIcon_hasAText()
    {
        InfoBubbleResources.Missing([InfoBubbleText.MoreAbout]).ShouldBeEmpty();
    }

    // The rate in a card's summary says what it measures, worded once for every card, as are the line a
    // card waits on and its details' fold.
    [Fact]
    public void ModuleCardResx_sharedTexts_haveTheirTexts()
    {
        ModuleCardResources.Missing([ModuleCardText.ToYou, ModuleCardText.PassesOn, ModuleCardText.Waiting, ModuleCardText.DetailsLabel, ModuleCardText.SourcesTitle])
            .ShouldBeEmpty();
    }
}
