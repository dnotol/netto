using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A lever's card leads with what the lever adds to the pocket, the plan with it less the plan without at
// the same pay (LeverGain), and ends on that gain over what the company pays out; the car, whose payout is
// its costs, on no rate. No card says what the company has left after it: the company's line is the map's.
public class LeverCardsSummaryTests
{
    // Every lever in the plan with amounts: vouchers, an allowance, a home office rent, CarViewTests' electric
    // car (50,000, first registered in January 2026, 8,000 of costs) and a fixed royalty of 10,000.
    internal static readonly RiverInputs Filled = PlanComposition.Modules.Where(module => module != OptionalModule.Household)
        .Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithEcoVouchers(250m).WithMealVoucherDays(220).WithInternetAllowance(240m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .WithRoyalty(10_000m);

    private static readonly ResxText Chrome = ResxText.For<ModuleCard>();

    public static TheoryData<OptionalModule> Levers => new()
    {
        OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car, OptionalModule.Copyright,
    };

    [Theory]
    [MemberData(nameof(Levers))]
    public async Task Summary_aLeverInThePlan_leadsWithWhatItAddsToThePocket(OptionalModule lever)
    {
        var gain = LeverGain.Of(Filled, Filled.Run(), lever).ShouldNotBeNull().Gain;

        var summary = CardHtml.Summary(await RenderOpenAsync(CardOf(lever), lever, Filled)).ShouldNotBeNull();

        summary.ShouldBe(Texts(lever).Format(LeverGainText.Summary(gain), Formats.Euros(Math.Abs(gain))));
    }

    // The gain over what the company pays out for the lever: the vouchers, the allowances, the rent, the
    // royalty; the car pays out its costs, and ends on no rate.
    [Theory]
    [MemberData(nameof(Levers))]
    public async Task Rate_aLeverInThePlan_isTheGainOverThePayoutAndNoneForTheCar(OptionalModule lever)
    {
        var run = Filled.Run();
        var year = run.Year.ShouldNotBeNull();
        var gain = LeverGain.Of(Filled, run, lever).ShouldNotBeNull().Gain;
        decimal? payout = lever switch
        {
            OptionalModule.Vouchers => year.Vouchers.Total,
            OptionalModule.Allowances => AllowanceView.From(year, Filled.Composition).Total,
            OptionalModule.Rent => year.Rent.Rent,
            OptionalModule.Copyright => year.Copyright.Royalty,
            _ => null,
        };

        var rate = CardHtml.Rate(await RenderOpenAsync(CardOf(lever), lever, Filled));

        rate.ShouldBe(payout is { } paid ? Chrome.Format("OfThePayout", Formats.WholeShare(gain / paid)) : null);
    }

    // A lever that changes nothing in the pocket says so, without a figure.
    [Fact]
    public async Task Summary_vouchersGrantingNothing_sayNoDifference()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

        var summary = CardHtml.Summary(await RenderOpenAsync<VoucherCard>(OptionalModule.Vouchers, inputs));

        summary.ShouldBe(Texts(OptionalModule.Vouchers).Get("Summary_None"));
    }

    [Fact]
    public async Task Summary_noCarYet_saysSoAlone()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Car);

        var summary = CardHtml.Summary(await RenderOpenAsync<CarCard>(OptionalModule.Car, inputs));

        summary.ShouldBe(Texts(OptionalModule.Car).Get("SummaryNoCar"));
    }

    [Theory]
    [InlineData(1, "Summary_More")]
    [InlineData(-1, "Summary_Less")]
    [InlineData(0, "Summary_None")]
    [InlineData(0.004, "Summary_None")]
    public void Summary_aGain_isItsKey(decimal gain, string key)
    {
        LeverGainText.Summary(gain).ShouldBe(key);
    }

    // Each lever's card words the three ways its gain reads, in English and in French.
    [Theory]
    [MemberData(nameof(Levers))]
    public void Texts_eachLever_wordsTheThreeWaysItsGainReads(OptionalModule lever)
    {
        string[] keys = ["Summary_More", "Summary_Less", "Summary_None"];

        keys.Where(key => Texts(lever).Get(key) is null).ShouldBeEmpty();
    }

    private static Type CardOf(OptionalModule lever) => lever switch
    {
        OptionalModule.Vouchers => typeof(VoucherCard),
        OptionalModule.Allowances => typeof(AllowanceCard),
        OptionalModule.Rent => typeof(RentCard),
        OptionalModule.Car => typeof(CarCard),
        OptionalModule.Copyright => typeof(CopyrightCard),
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "No lever card."),
    };

    private static ResxText Texts(OptionalModule lever) => lever switch
    {
        OptionalModule.Vouchers => ResxText.For<VoucherCard>(),
        OptionalModule.Allowances => ResxText.For<AllowanceCard>(),
        OptionalModule.Rent => ResxText.For<RentCard>(),
        OptionalModule.Car => ResxText.For<CarCard>(),
        OptionalModule.Copyright => ResxText.For<CopyrightCard>(),
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "No lever card."),
    };
}
