using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The modules the map draws: the cards the river draws with figures, in the river's order, an optional
// module only while it is in the plan, and with the company skipped Company year, drawn skipped, and Director
// pay, the river starting at the pay: the cards that only wait for the company's year are left out. A card
// the river gains without a place on the map fails here.
public class MapModulesTests
{
    // Every one of the 64 compositions of the plan's optional modules, by the bits of a number.
    public static TheoryData<int> Compositions => new(Enumerable.Range(0, 1 << PlanComposition.Modules.Count));

    // The income year, which no card holds, is the one module of the river the map has no place for.
    [Fact]
    public void Order_everyModuleButTheRiver_isInIt()
    {
        MapModules.Order.ShouldBe(Enum.GetValues<RiverModule>().Where(module => module != RiverModule.River), ignoreOrder: true);
    }

    [Fact]
    public void Name_aModuleItHasNoArmFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapModules.Name((RiverModule)99));
    }

    [Fact]
    public void Name_theRiver_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapModules.Name(RiverModule.River));
    }

    [Fact]
    public void ModulesOf_theExample_isItsFiveCards()
    {
        MapView.ModulesOf(RiverInputs.Example).ShouldBe(
            [RiverModule.CompanyYear, RiverModule.Split, RiverModule.DirectorPay, RiverModule.CorporateTax, RiverModule.Dividend]);
    }

    [Theory]
    [MemberData(nameof(Compositions))]
    public void ModulesOf_aComposition_addsTheLeversInThePlanInTheRiversOrder(int bits)
    {
        var river = RiverInputs.Example with { Composition = Composition(bits) };

        MapView.ModulesOf(river).ShouldBe(MapModules.Order.Where(module => RiverModules.Lever(module) is not { } lever || river.InPlan(lever)));
    }

    // The partner beside Director pay while in the plan: the pay alone is taxed jointly.
    [Theory]
    [MemberData(nameof(Compositions))]
    public void ModulesOf_companySkipped_isCompanyYearAndDirectorPayAndThePartner(int bits)
    {
        var river = (RiverInputs.Example with { Composition = Composition(bits) }).SkipCompany();

        MapView.ModulesOf(river).ShouldBe(river.InPlan(OptionalModule.Household)
            ? [RiverModule.CompanyYear, RiverModule.DirectorPay, RiverModule.Household]
            : [RiverModule.CompanyYear, RiverModule.DirectorPay]);
    }

    // The river's cards as the Plan page draws them, all open, against the map's: a card drawn dashed,
    // skipped or waiting for the company's year, holds no figure, and the map leaves it out, but for the
    // skipped Company year, which stays on the map, drawn skipped, so that it can be entered again.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ModulesOf_aRiver_isTheCardsThePlanPageDrawsWithFigures(bool everyLever, bool companySkipped)
    {
        var river = everyLever ? PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module)) : RiverInputs.Example;
        river = companySkipped ? river.SkipCompany() : river;

        var html = await CardHtml.RenderCardsOpenAsync(river);

        var withFigures = Regex.Matches(html, @"<section id=""card-([a-z-]+)"" class=""module-card\s*(module-card--skipped)?\s*""")
            .Where(card => !card.Groups[2].Success).Select(card => card.Groups[1].Value);
        withFigures.ShouldBe(MapView.ModulesOf(river).Where(module => !(companySkipped && module == RiverModule.CompanyYear)).Select(MapModules.Name));
    }

    internal static PlanComposition Composition(int bits) =>
        new((bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0, (bits & 8) != 0, (bits & 16) != 0, (bits & 32) != 0);
}
